using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using WpfApp1.Models;

namespace WpfApp1.Data
{
    public sealed class ProductRepository
    {
        private sealed class ProductTableLayout
        {
            public string SchemaName { get; set; }
            public string TableName { get; set; }
            public string IdColumn { get; set; }
            public string NameColumn { get; set; }
            public string ProductTypeIdColumn { get; set; }
            public string PriceColumn { get; set; }
            public string ArticleColumn { get; set; }
            public bool IsProductIdIdentity { get; set; }
        }

        public bool IsAvailable()
        {
            using (var connection = DbConnectionFactory.CreateOpen())
            {
                return ResolveProductLayoutOrNull(connection) != null;
            }
        }

        public List<ProductItem> GetAll()
        {
            var products = new List<ProductItem>();

            using (var connection = DbConnectionFactory.CreateOpen())
            {
                var layout = ResolveProductLayoutOrNull(connection);
                if (layout == null)
                {
                    throw new InvalidOperationException("Таблица продукции (Product) не найдена в базе данных.");
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BuildSelectSql(layout);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(ReadProduct(reader));
                        }
                    }
                }
            }

            return products;
        }

        public List<ProductItem> GetByTypeId(int typeId)
        {
            var products = new List<ProductItem>();

            using (var connection = DbConnectionFactory.CreateOpen())
            {
                var layout = ResolveProductLayoutOrNull(connection);
                if (layout == null)
                {
                    return products;
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BuildSelectByTypeSql(layout);

                    command.Parameters.Add(new SqlParameter("@typeId", SqlDbType.Int) { Value = typeId });

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(ReadProduct(reader));
                        }
                    }
                }
            }

            return products;
        }

        public void Insert(ProductItem product)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product));
            }

            using (var connection = DbConnectionFactory.CreateOpen())
            {
                var layout = ResolveProductLayoutOrNull(connection);
                if (layout == null)
                {
                    throw new InvalidOperationException("Таблица продукции не найдена.");
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BuildInsertSql(layout);
                    AddInsertParameters(command, layout, product);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void Update(ProductItem product, string originalId)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product));
            }

            using (var connection = DbConnectionFactory.CreateOpen())
            {
                var layout = ResolveProductLayoutOrNull(connection);
                if (layout == null)
                {
                    throw new InvalidOperationException("Таблица продукции не найдена.");
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BuildUpdateSql(layout);
                    AddUpdateParameters(command, layout, product);
                    AddIdParameter(command, "@originalId", originalId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void Delete(string productId)
        {
            using (var connection = DbConnectionFactory.CreateOpen())
            {
                var layout = ResolveProductLayoutOrNull(connection);
                if (layout == null)
                {
                    throw new InvalidOperationException("Таблица продукции не найдена.");
                }

                using (var command = connection.CreateCommand())
                {
                    var table = QualifyTable(layout);
                    command.CommandText = "DELETE FROM " + table + " WHERE [" + layout.IdColumn + "] = @id";
                    AddIdParameter(command, "@id", productId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public bool IsProductIdIdentity()
        {
            using (var connection = DbConnectionFactory.CreateOpen())
            {
                var layout = ResolveProductLayoutOrNull(connection);
                return layout != null && layout.IsProductIdIdentity;
            }
        }

        public ProductEditMetadata GetEditMetadata()
        {
            using (var connection = DbConnectionFactory.CreateOpen())
            {
                var layout = ResolveProductLayoutOrNull(connection);
                if (layout == null)
                {
                    return new ProductEditMetadata();
                }

                return new ProductEditMetadata
                {
                    IsAvailable = true,
                    IsProductIdIdentity = layout.IsProductIdIdentity,
                    ShowPrice = !string.IsNullOrEmpty(layout.PriceColumn),
                    ShowArticle = !string.IsNullOrEmpty(layout.ArticleColumn),
                };
            }
        }

        private static ProductTableLayout ResolveProductLayoutOrNull(SqlConnection connection)
        {
            var candidates = new List<(string Schema, string Name)>();

            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES " +
                    "WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME IN ('Product', 'Products') " +
                    "ORDER BY CASE TABLE_NAME WHEN 'Product' THEN 0 ELSE 1 END";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        candidates.Add((reader.GetString(0), reader.GetString(1)));
                    }
                }
            }

            foreach (var candidate in candidates)
            {
                var columns = GetColumns(connection, candidate.Schema, candidate.Name);
                var idCol = PickFirst(columns, new[] { "ProductID", "Id", "ID" });
                var nameCol = PickFirst(columns, new[] { "ProductName", "Name" });
                var typeCol = PickFirst(columns, new[] { "ProductTypeID", "ProductTypeId" });

                if (string.IsNullOrEmpty(idCol) || string.IsNullOrEmpty(nameCol) || string.IsNullOrEmpty(typeCol))
                {
                    continue;
                }

                var priceCol = PickFirst(columns, new[] { "MinPrice", "Price", "ProductPrice" });
                var articleCol = PickFirst(columns, new[] { "ArticleNumber", "Article", "VendorCode" });

                var isIdentity = IsIdentityColumn(connection, candidate.Schema, candidate.Name, idCol);

                return new ProductTableLayout
                {
                    SchemaName = candidate.Schema,
                    TableName = candidate.Name,
                    IdColumn = idCol,
                    NameColumn = nameCol,
                    ProductTypeIdColumn = typeCol,
                    PriceColumn = priceCol,
                    ArticleColumn = articleCol,
                    IsProductIdIdentity = isIdentity,
                };
            }

            return null;
        }

        private static bool IsIdentityColumn(SqlConnection connection, string schema, string table, string column)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT COLUMNPROPERTY(OBJECT_ID(@fullName), @column, 'IsIdentity')";
                var fullName = schema + "." + table;
                command.Parameters.Add(new SqlParameter("@fullName", SqlDbType.NVarChar, 256) { Value = fullName });
                command.Parameters.Add(new SqlParameter("@column", SqlDbType.NVarChar, 128) { Value = column });

                var result = command.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                {
                    return false;
                }

                return Convert.ToInt32(result, CultureInfo.InvariantCulture) == 1;
            }
        }

        private static Dictionary<string, string> GetColumns(SqlConnection connection, string schemaName, string tableName)
        {
            var columns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS " +
                    "WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table";
                command.Parameters.Add(new SqlParameter("@schema", SqlDbType.NVarChar, 128) { Value = schemaName });
                command.Parameters.Add(new SqlParameter("@table", SqlDbType.NVarChar, 128) { Value = tableName });

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var colName = reader.GetString(0);
                        if (!columns.ContainsKey(colName))
                        {
                            columns[colName] = colName;
                        }
                    }
                }
            }

            return columns;
        }

        private static string PickFirst(Dictionary<string, string> columns, string[] candidates)
        {
            foreach (var candidate in candidates)
            {
                if (columns.TryGetValue(candidate, out var realName))
                {
                    return realName;
                }
            }

            return null;
        }

        private static string QualifyTable(ProductTableLayout layout)
        {
            return "[" + layout.SchemaName + "].[" + layout.TableName + "]";
        }

        private static string BuildSelectSql(ProductTableLayout layout)
        {
            var table = QualifyTable(layout);
            var idExpr = "[" + layout.IdColumn + "]";
            var nameExpr = "[" + layout.NameColumn + "]";
            var typeExpr = "[" + layout.ProductTypeIdColumn + "]";
            var priceExpr = layout.PriceColumn == null ? "CAST(NULL AS DECIMAL(18,2))" : "[" + layout.PriceColumn + "]";
            var articleExpr = layout.ArticleColumn == null ? "CAST(NULL AS NVARCHAR(100))" : "[" + layout.ArticleColumn + "]";

            return
                "SELECT " +
                "CAST(" + idExpr + " AS NVARCHAR(100)) AS Id, " +
                nameExpr + " AS Name, " +
                typeExpr + " AS ProductTypeId, " +
                priceExpr + " AS Price, " +
                "CAST(" + articleExpr + " AS NVARCHAR(200)) AS ArticleNumber " +
                "FROM " + table + " ORDER BY " + nameExpr;
        }

        private static string BuildSelectByTypeSql(ProductTableLayout layout)
        {
            var table = QualifyTable(layout);
            var idExpr = "[" + layout.IdColumn + "]";
            var nameExpr = "[" + layout.NameColumn + "]";
            var typeExpr = "[" + layout.ProductTypeIdColumn + "]";
            var priceExpr = layout.PriceColumn == null ? "CAST(NULL AS DECIMAL(18,2))" : "[" + layout.PriceColumn + "]";
            var articleExpr = layout.ArticleColumn == null ? "CAST(NULL AS NVARCHAR(100))" : "[" + layout.ArticleColumn + "]";

            return
                "SELECT " +
                "CAST(" + idExpr + " AS NVARCHAR(100)) AS Id, " +
                nameExpr + " AS Name, " +
                typeExpr + " AS ProductTypeId, " +
                priceExpr + " AS Price, " +
                "CAST(" + articleExpr + " AS NVARCHAR(200)) AS ArticleNumber " +
                "FROM " + table + " WHERE " + typeExpr + " = @typeId ORDER BY " + nameExpr;
        }

        private static ProductItem ReadProduct(SqlDataReader reader)
        {
            var idOrdinal = reader.GetOrdinal("Id");
            var idValue = reader.IsDBNull(idOrdinal) ? string.Empty : reader.GetString(idOrdinal);

            var priceOrdinal = reader.GetOrdinal("Price");
            decimal? price = null;
            if (!reader.IsDBNull(priceOrdinal))
            {
                price = reader.GetDecimal(priceOrdinal);
            }

            var typeOrdinal = reader.GetOrdinal("ProductTypeId");
            int? typeId = null;
            if (!reader.IsDBNull(typeOrdinal))
            {
                typeId = reader.GetInt32(typeOrdinal);
            }

            var articleOrdinal = reader.GetOrdinal("ArticleNumber");
            string article = null;
            if (!reader.IsDBNull(articleOrdinal))
            {
                article = reader.GetString(articleOrdinal);
            }

            return new ProductItem
            {
                Id = idValue,
                Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? string.Empty : reader.GetString(reader.GetOrdinal("Name")),
                ProductTypeId = typeId,
                Price = price,
                ArticleNumber = article,
            };
        }

        private static string BuildInsertSql(ProductTableLayout layout)
        {
            var columns = new List<string>();
            var values = new List<string>();

            if (!layout.IsProductIdIdentity)
            {
                columns.Add("[" + layout.IdColumn + "]");
                values.Add("@id");
            }

            columns.Add("[" + layout.NameColumn + "]");
            values.Add("@name");

            columns.Add("[" + layout.ProductTypeIdColumn + "]");
            values.Add("@typeId");

            if (!string.IsNullOrEmpty(layout.PriceColumn))
            {
                columns.Add("[" + layout.PriceColumn + "]");
                values.Add("@price");
            }

            if (!string.IsNullOrEmpty(layout.ArticleColumn))
            {
                columns.Add("[" + layout.ArticleColumn + "]");
                values.Add("@article");
            }

            var table = QualifyTable(layout);
            return "INSERT INTO " + table + " (" + string.Join(", ", columns) + ") VALUES (" + string.Join(", ", values) + ")";
        }

        private static string BuildUpdateSql(ProductTableLayout layout)
        {
            var sets = new List<string>();
            sets.Add("[" + layout.NameColumn + "] = @name");
            sets.Add("[" + layout.ProductTypeIdColumn + "] = @typeId");

            if (!string.IsNullOrEmpty(layout.PriceColumn))
            {
                sets.Add("[" + layout.PriceColumn + "] = @price");
            }

            if (!string.IsNullOrEmpty(layout.ArticleColumn))
            {
                sets.Add("[" + layout.ArticleColumn + "] = @article");
            }

            var table = QualifyTable(layout);
            return "UPDATE " + table + " SET " + string.Join(", ", sets) + " WHERE [" + layout.IdColumn + "] = @originalId";
        }

        private static void AddInsertParameters(SqlCommand command, ProductTableLayout layout, ProductItem product)
        {
            if (!layout.IsProductIdIdentity)
            {
                command.Parameters.Add(new SqlParameter("@id", SqlDbType.NVarChar, 100) { Value = product.Id ?? string.Empty });
            }

            command.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 200) { Value = (object)product.Name ?? DBNull.Value });
            command.Parameters.Add(new SqlParameter("@typeId", SqlDbType.Int) { Value = (object)product.ProductTypeId ?? DBNull.Value });

            if (!string.IsNullOrEmpty(layout.PriceColumn))
            {
                command.Parameters.Add(new SqlParameter("@price", SqlDbType.Decimal)
                {
                    Precision = 18,
                    Scale = 2,
                    Value = (object)product.Price ?? DBNull.Value,
                });
            }

            if (!string.IsNullOrEmpty(layout.ArticleColumn))
            {
                command.Parameters.Add(new SqlParameter("@article", SqlDbType.NVarChar, 200) { Value = (object)product.ArticleNumber ?? DBNull.Value });
            }
        }

        private static void AddUpdateParameters(SqlCommand command, ProductTableLayout layout, ProductItem product)
        {
            command.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 200) { Value = (object)product.Name ?? DBNull.Value });
            command.Parameters.Add(new SqlParameter("@typeId", SqlDbType.Int) { Value = (object)product.ProductTypeId ?? DBNull.Value });

            if (!string.IsNullOrEmpty(layout.PriceColumn))
            {
                command.Parameters.Add(new SqlParameter("@price", SqlDbType.Decimal)
                {
                    Precision = 18,
                    Scale = 2,
                    Value = (object)product.Price ?? DBNull.Value,
                });
            }

            if (!string.IsNullOrEmpty(layout.ArticleColumn))
            {
                command.Parameters.Add(new SqlParameter("@article", SqlDbType.NVarChar, 200) { Value = (object)product.ArticleNumber ?? DBNull.Value });
            }
        }

        private static void AddIdParameter(SqlCommand command, string paramName, string id)
        {
            command.Parameters.Add(new SqlParameter(paramName, SqlDbType.NVarChar, 100) { Value = id ?? string.Empty });
        }
    }
}