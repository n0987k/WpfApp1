using System;
using System.Data;
using System.Data.SqlClient;

namespace WpfApp1.Data
{
    public static class DatabaseSchemaHelper
    {
        public static bool TableExists(SqlConnection connection, string tableName)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                return false;
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText =
                    "SELECT 1 FROM INFORMATION_SCHEMA.TABLES " +
                    "WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME = @name";
                command.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 128) { Value = tableName });

                var scalar = command.ExecuteScalar();
                return scalar != null;
            }
        }

        public static bool ColumnExists(SqlConnection connection, string tableName, string columnName)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName))
            {
                return false;
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText =
                    "SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS " +
                    "WHERE TABLE_NAME = @table AND COLUMN_NAME = @column";
                command.Parameters.Add(new SqlParameter("@table", SqlDbType.NVarChar, 128) { Value = tableName });
                command.Parameters.Add(new SqlParameter("@column", SqlDbType.NVarChar, 128) { Value = columnName });

                var scalar = command.ExecuteScalar();
                return scalar != null;
            }
        }

        public static string ResolveFirstExistingTable(SqlConnection connection, params string[] candidates)
        {
            if (candidates == null)
            {
                return null;
            }

            foreach (var name in candidates)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (TableExists(connection, name))
                {
                    return name;
                }
            }

            return null;
        }
    }
}
