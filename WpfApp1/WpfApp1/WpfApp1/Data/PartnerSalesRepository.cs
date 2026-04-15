using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using WpfApp1.Models;

namespace WpfApp1.Data
{
    public sealed class PartnerSalesRepository
    {
        public List<PartnerSale> GetByPartnerId(int partnerId)
        {
            var sales = new List<PartnerSale>();

            using (var connection = DbConnectionFactory.CreateOpen())
            {
                var priceColumn = ResolveProductPriceColumn(connection);
                var sql = BuildSalesByPartnerSql(priceColumn);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.Parameters.Add(new SqlParameter("@partnerId", SqlDbType.Int) { Value = partnerId });

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            sales.Add(ReadSale(reader));
                        }
                    }
                }
            }

            return sales;
        }

        public List<PartnerSale> GetAll()
        {
            var sales = new List<PartnerSale>();

            using (var connection = DbConnectionFactory.CreateOpen())
            {
                var priceColumn = ResolveProductPriceColumn(connection);
                var sql = BuildAllSalesSql(priceColumn);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            sales.Add(ReadSale(reader));
                        }
                    }
                }
            }

            return sales;
        }

        public List<PartnerSale> GetByProductId(string productId)
        {
            var sales = new List<PartnerSale>();

            if (string.IsNullOrWhiteSpace(productId))
            {
                return sales;
            }

            using (var connection = DbConnectionFactory.CreateOpen())
            {
                var priceColumn = ResolveProductPriceColumn(connection);
                var sql = BuildSalesByProductSql(priceColumn);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.Parameters.Add(new SqlParameter("@productId", SqlDbType.NVarChar, 100) { Value = productId.Trim() });

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            sales.Add(ReadSale(reader));
                        }
                    }
                }
            }

            return sales;
        }

        private static string ResolveProductPriceColumn(SqlConnection connection)
        {
            if (DatabaseSchemaHelper.ColumnExists(connection, "Product", "MinPrice"))
            {
                return "MinPrice";
            }

            if (DatabaseSchemaHelper.ColumnExists(connection, "Product", "Price"))
            {
                return "Price";
            }

            return null;
        }

        private static string BuildSalesByPartnerSql(string priceColumn)
        {
            var priceExpr = string.IsNullOrEmpty(priceColumn) ? "CAST(NULL AS DECIMAL(18,2))" : "p.[" + priceColumn + "]";

            return
                "SELECT " +
                "pp.SaleID, " +
                "pp.PartnerID, " +
                "pp.ProductID, " +
                "pp.Quantity, " +
                "pp.SaleDate, " +
                "p.ProductName, " +
                "pt.ProductTypeName, " +
                "CAST(NULL AS NVARCHAR(200)) AS PartnerName, " +
                priceExpr + " AS UnitPrice " +
                "FROM PartnerProduct pp " +
                "INNER JOIN Product p ON pp.ProductID = p.ProductID " +
                "INNER JOIN ProductType pt ON p.ProductTypeID = pt.ProductTypeID " +
                "WHERE pp.PartnerID = @partnerId " +
                "ORDER BY pp.SaleDate DESC";
        }

        private static string BuildSalesByProductSql(string priceColumn)
        {
            var priceExpr = string.IsNullOrEmpty(priceColumn) ? "CAST(NULL AS DECIMAL(18,2))" : "p.[" + priceColumn + "]";

            return
                "SELECT " +
                "pp.SaleID, " +
                "pp.PartnerID, " +
                "pp.ProductID, " +
                "pp.Quantity, " +
                "pp.SaleDate, " +
                "p.ProductName, " +
                "pt.ProductTypeName, " +
                "pr.PartnerName, " +
                priceExpr + " AS UnitPrice " +
                "FROM PartnerProduct pp " +
                "INNER JOIN Product p ON pp.ProductID = p.ProductID " +
                "INNER JOIN ProductType pt ON p.ProductTypeID = pt.ProductTypeID " +
                "INNER JOIN Partner pr ON pp.PartnerID = pr.PartnerID " +
                "WHERE pp.ProductID = @productId " +
                "ORDER BY pp.SaleDate DESC";
        }

        private static string BuildAllSalesSql(string priceColumn)
        {
            var priceExpr = string.IsNullOrEmpty(priceColumn) ? "CAST(NULL AS DECIMAL(18,2))" : "p.[" + priceColumn + "]";

            return
                "SELECT " +
                "pp.SaleID, " +
                "pp.PartnerID, " +
                "pp.ProductID, " +
                "pp.Quantity, " +
                "pp.SaleDate, " +
                "p.ProductName, " +
                "pt.ProductTypeName, " +
                "pr.PartnerName, " +
                priceExpr + " AS UnitPrice " +
                "FROM PartnerProduct pp " +
                "INNER JOIN Product p ON pp.ProductID = p.ProductID " +
                "INNER JOIN ProductType pt ON p.ProductTypeID = pt.ProductTypeID " +
                "INNER JOIN Partner pr ON pp.PartnerID = pr.PartnerID " +
                "ORDER BY pp.SaleDate DESC";
        }

        private static PartnerSale ReadSale(SqlDataReader reader)
        {
            var unitPrice = ReadDecimal(reader, reader.GetOrdinal("UnitPrice"));
            var quantity = reader.GetInt32(reader.GetOrdinal("Quantity"));
            var lineTotal = Math.Round(unitPrice * quantity, 2, MidpointRounding.AwayFromZero);

            return new PartnerSale
            {
                SaleId = ReadObjectAsString(reader, reader.GetOrdinal("SaleID")),
                PartnerId = reader.GetInt32(reader.GetOrdinal("PartnerID")),
                ProductId = ReadObjectAsString(reader, reader.GetOrdinal("ProductID")),
                Quantity = quantity,
                SaleDate = reader.GetDateTime(reader.GetOrdinal("SaleDate")),
                ProductName = reader.IsDBNull(reader.GetOrdinal("ProductName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ProductName")),
                ProductTypeName = reader.IsDBNull(reader.GetOrdinal("ProductTypeName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ProductTypeName")),
                PartnerName = ReadOptionalString(reader, "PartnerName"),
                UnitPrice = unitPrice,
                LineTotal = lineTotal,
            };
        }

        private static string ReadOptionalString(SqlDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            return reader.GetString(ordinal);
        }

        private static string ReadObjectAsString(SqlDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return string.Empty;
            }

            var value = reader.GetValue(ordinal);
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static decimal ReadDecimal(SqlDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return 0m;
            }

            return reader.GetDecimal(ordinal);
        }
    }
}
