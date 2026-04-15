using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using WpfApp1.Models;

namespace WpfApp1.Data
{
    public sealed class CatalogDataRepository
    {
        private const int MaxRows = 5000;

        public DataTable LoadTable(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException("Имя таблицы не задано.", nameof(tableName));
            }

            var safeName = tableName.Trim();
            ValidateTableNameToken(safeName);

            using (var connection = DbConnectionFactory.CreateOpen())
            {
                if (!DatabaseSchemaHelper.TableExists(connection, safeName))
                {
                    throw new InvalidOperationException("Таблица «" + safeName + "» не найдена в базе данных.");
                }

                var sql = "SELECT TOP (" + MaxRows + ") * FROM [" + safeName + "]";

                using (var adapter = new SqlDataAdapter(sql, connection))
                {
                    var table = new DataTable();
                    adapter.Fill(table);
                    return table;
                }
            }
        }

        public IReadOnlyList<CatalogColumnInfo> GetColumnInfos(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException("Имя таблицы не задано.", nameof(tableName));
            }

            var safeName = tableName.Trim();
            ValidateTableNameToken(safeName);

            using (var connection = DbConnectionFactory.CreateOpen())
            {
                if (!DatabaseSchemaHelper.TableExists(connection, safeName))
                {
                    throw new InvalidOperationException("Таблица «" + safeName + "» не найдена в базе данных.");
                }

                var schemaName = ResolveTableSchema(connection, safeName);
                var pkSet = new HashSet<string>(GetPrimaryKeyColumnNames(connection, schemaName, safeName), StringComparer.OrdinalIgnoreCase);
                return LoadColumnMetadata(connection, schemaName, safeName, pkSet);
            }
        }

        public void InsertRow(string tableName, IReadOnlyDictionary<string, object> columnValues)
        {
            if (columnValues == null)
            {
                throw new ArgumentNullException(nameof(columnValues));
            }

            var safeName = ValidateAndResolveTable(tableName);

            using (var connection = DbConnectionFactory.CreateOpen())
            {
                var schemaName = ResolveTableSchema(connection, safeName);
                var pkSet = new HashSet<string>(GetPrimaryKeyColumnNames(connection, schemaName, safeName), StringComparer.OrdinalIgnoreCase);
                var columns = LoadColumnMetadata(connection, schemaName, safeName, pkSet);
                var insertable = columns.Where(c => !c.IsComputed && !c.IsIdentity).ToList();

                if (insertable.Count == 0)
                {
                    throw new InvalidOperationException("Нет столбцов для вставки (все вычисляемые или identity).");
                }

                var qualifiedTable = "[" + schemaName + "].[" + safeName + "]";
                var nameList = new List<string>();
                var paramList = new List<string>();
                var index = 0;

                using (var command = connection.CreateCommand())
                {
                    foreach (var col in insertable)
                    {
                        if (!columnValues.TryGetValue(col.Name, out var value))
                        {
                            value = DBNull.Value;
                        }

                        if (value == null || value == DBNull.Value)
                        {
                            if (!col.IsNullable)
                            {
                                throw new InvalidOperationException("Для столбца «" + col.Name + "» нужно указать значение.");
                            }
                        }

                        var paramName = "@p" + index.ToString(CultureInfo.InvariantCulture);
                        nameList.Add("[" + col.Name + "]");
                        paramList.Add(paramName);
                        command.Parameters.Add(CreateParameter(paramName, value));
                        index++;
                    }

                    command.CommandText = "INSERT INTO " + qualifiedTable + " (" + string.Join(", ", nameList) + ") VALUES (" + string.Join(", ", paramList) + ")";
                    command.ExecuteNonQuery();
                }
            }
        }

        public void UpdateRow(
            string tableName,
            IReadOnlyDictionary<string, object> originalPrimaryKeyValues,
            IReadOnlyDictionary<string, object> columnValuesToSet)
        {
            if (originalPrimaryKeyValues == null)
            {
                throw new ArgumentNullException(nameof(originalPrimaryKeyValues));
            }

            if (columnValuesToSet == null)
            {
                throw new ArgumentNullException(nameof(columnValuesToSet));
            }

            var safeName = ValidateAndResolveTable(tableName);

            using (var connection = DbConnectionFactory.CreateOpen())
            {
                var schemaName = ResolveTableSchema(connection, safeName);
                var pkColumns = GetPrimaryKeyColumnNames(connection, schemaName, safeName);

                if (pkColumns.Count == 0)
                {
                    throw new InvalidOperationException("У таблицы нет первичного ключа — обновление невозможно.");
                }

                var pkSet = new HashSet<string>(pkColumns, StringComparer.OrdinalIgnoreCase);
                var columns = LoadColumnMetadata(connection, schemaName, safeName, pkSet);
                var updatable = columns.Where(c => !c.IsComputed && !c.IsIdentity && !c.IsPrimaryKey).ToList();

                var qualifiedTable = "[" + schemaName + "].[" + safeName + "]";
                var setParts = new List<string>();
                var index = 0;

                using (var command = connection.CreateCommand())
                {
                    foreach (var col in updatable)
                    {
                        if (!columnValuesToSet.TryGetValue(col.Name, out var value))
                        {
                            value = DBNull.Value;
                        }

                        if (value == null || value == DBNull.Value)
                        {
                            if (!col.IsNullable)
                            {
                                throw new InvalidOperationException("Для столбца «" + col.Name + "» нужно указать значение.");
                            }
                        }

                        var paramName = "@s" + index.ToString(CultureInfo.InvariantCulture);
                        setParts.Add("[" + col.Name + "] = " + paramName);
                        command.Parameters.Add(CreateParameter(paramName, value));
                        index++;
                    }

                    if (setParts.Count == 0)
                    {
                        throw new InvalidOperationException("Нет редактируемых столбцов (кроме ключа) для обновления.");
                    }

                    var whereParts = new List<string>();
                    var w = 0;

                    foreach (var pkCol in pkColumns)
                    {
                        if (!originalPrimaryKeyValues.TryGetValue(pkCol, out var keyVal))
                        {
                            throw new InvalidOperationException("Не задано значение ключа «" + pkCol + "».");
                        }

                        var paramName = "@k" + w.ToString(CultureInfo.InvariantCulture);
                        whereParts.Add("[" + pkCol + "] = " + paramName);
                        command.Parameters.Add(CreateParameter(paramName, keyVal));
                        w++;
                    }

                    command.CommandText = "UPDATE " + qualifiedTable + " SET " + string.Join(", ", setParts) + " WHERE " + string.Join(" AND ", whereParts);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteRow(string tableName, DataRow dataRow)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException("Имя таблицы не задано.", nameof(tableName));
            }

            if (dataRow == null)
            {
                throw new ArgumentNullException(nameof(dataRow));
            }

            var safeName = tableName.Trim();
            ValidateTableNameToken(safeName);

            using (var connection = DbConnectionFactory.CreateOpen())
            {
                if (!DatabaseSchemaHelper.TableExists(connection, safeName))
                {
                    throw new InvalidOperationException("Таблица «" + safeName + "» не найдена в базе данных.");
                }

                var schemaName = ResolveTableSchema(connection, safeName);
                var keyColumns = GetPrimaryKeyColumnNames(connection, schemaName, safeName);

                if (keyColumns.Count == 0)
                {
                    throw new InvalidOperationException(
                        "У таблицы «" + safeName + "» нет первичного ключа. Удаление через приложение невозможно.");
                }

                var qualifiedTable = "[" + schemaName + "].[" + safeName + "]";
                var whereParts = new List<string>();

                for (var i = 0; i < keyColumns.Count; i++)
                {
                    whereParts.Add("[" + keyColumns[i] + "] = @k" + i.ToString(CultureInfo.InvariantCulture));
                }

                var deleteSql = "DELETE FROM " + qualifiedTable + " WHERE " + string.Join(" AND ", whereParts);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = deleteSql;

                    for (var i = 0; i < keyColumns.Count; i++)
                    {
                        var columnName = keyColumns[i];
                        var parameterName = "@k" + i.ToString(CultureInfo.InvariantCulture);

                        if (!dataRow.Table.Columns.Contains(columnName))
                        {
                            throw new InvalidOperationException("В строке нет столбца первичного ключа «" + columnName + "».");
                        }

                        var value = dataRow[columnName, DataRowVersion.Current];
                        command.Parameters.Add(new SqlParameter(parameterName, value ?? DBNull.Value));
                    }

                    command.ExecuteNonQuery();
                }
            }
        }

        private static string ValidateAndResolveTable(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException("Имя таблицы не задано.", nameof(tableName));
            }

            var safeName = tableName.Trim();
            ValidateTableNameToken(safeName);
            return safeName;
        }

        private static List<CatalogColumnInfo> LoadColumnMetadata(
            SqlConnection connection,
            string schemaName,
            string tableName,
            HashSet<string> primaryKeyColumns)
        {
            var list = new List<CatalogColumnInfo>();
            var qualifiedObject = "[" + schemaName + "].[" + tableName + "]";

            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH, c.IS_NULLABLE, " +
                    "COLUMNPROPERTY(OBJECT_ID(@obj), c.COLUMN_NAME, 'IsIdentity') AS IsIdentity, " +
                    "COLUMNPROPERTY(OBJECT_ID(@obj), c.COLUMN_NAME, 'IsComputed') AS IsComputed " +
                    "FROM INFORMATION_SCHEMA.COLUMNS AS c " +
                    "WHERE c.TABLE_SCHEMA = @schema AND c.TABLE_NAME = @table " +
                    "ORDER BY c.ORDINAL_POSITION";

                command.Parameters.Add(new SqlParameter("@schema", SqlDbType.NVarChar, 128) { Value = schemaName });
                command.Parameters.Add(new SqlParameter("@table", SqlDbType.NVarChar, 128) { Value = tableName });
                command.Parameters.Add(new SqlParameter("@obj", SqlDbType.NVarChar, 512) { Value = schemaName + "." + tableName });

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var name = reader.GetString(0);
                        var dataType = reader.GetString(1);
                        int? maxLen = null;

                        if (!reader.IsDBNull(2))
                        {
                            maxLen = reader.GetInt32(2);
                        }

                        var nullable = string.Equals(reader.GetString(3), "YES", StringComparison.OrdinalIgnoreCase);
                        var isIdentity = !reader.IsDBNull(4) && Convert.ToInt32(reader.GetValue(4), CultureInfo.InvariantCulture) == 1;
                        var isComputed = !reader.IsDBNull(5) && Convert.ToInt32(reader.GetValue(5), CultureInfo.InvariantCulture) == 1;

                        list.Add(new CatalogColumnInfo
                        {
                            Name = name,
                            DataType = dataType,
                            CharacterMaxLength = maxLen,
                            IsNullable = nullable,
                            IsIdentity = isIdentity,
                            IsComputed = isComputed,
                            IsPrimaryKey = primaryKeyColumns.Contains(name),
                        });
                    }
                }
            }

            return list;
        }

        private static SqlParameter CreateParameter(string name, object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return new SqlParameter(name, DBNull.Value);
            }

            return new SqlParameter(name, value);
        }

        private static void ValidateTableNameToken(string safeName)
        {
            if (safeName.Contains(";") || safeName.Contains(" ") || safeName.Contains("]"))
            {
                throw new ArgumentException("Недопустимое имя таблицы.", nameof(safeName));
            }
        }

        private static string ResolveTableSchema(SqlConnection connection, string tableName)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT TOP (1) TABLE_SCHEMA FROM INFORMATION_SCHEMA.TABLES " +
                    "WHERE TABLE_NAME = @name ORDER BY TABLE_SCHEMA";
                command.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 128) { Value = tableName });

                var result = command.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                {
                    return "dbo";
                }

                return Convert.ToString(result);
            }
        }

        private static List<string> GetPrimaryKeyColumnNames(SqlConnection connection, string schemaName, string tableName)
        {
            var columns = new List<string>();

            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT ku.COLUMN_NAME " +
                    "FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS tc " +
                    "INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS ku " +
                    "ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME " +
                    "AND tc.TABLE_SCHEMA = ku.TABLE_SCHEMA " +
                    "AND tc.TABLE_NAME = ku.TABLE_NAME " +
                    "WHERE tc.CONSTRAINT_TYPE = N'PRIMARY KEY' " +
                    "AND tc.TABLE_SCHEMA = @schema " +
                    "AND tc.TABLE_NAME = @table " +
                    "ORDER BY ku.ORDINAL_POSITION";

                command.Parameters.Add(new SqlParameter("@schema", SqlDbType.NVarChar, 128) { Value = schemaName });
                command.Parameters.Add(new SqlParameter("@table", SqlDbType.NVarChar, 128) { Value = tableName });

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        columns.Add(reader.GetString(0));
                    }
                }
            }

            return columns;
        }
    }
}
