using System;
using System.Data;
using System.Data.SqlClient;
using WpfApp1.Models;

namespace WpfApp1.Data
{
    public sealed class UserRepository
    {
        public AuthenticatedUser Authenticate(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            using (var connection = DbConnectionFactory.CreateOpen())
            using (var command = connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = BuildAuthenticateSql(connection);

                command.Parameters.Add(new SqlParameter("@login", SqlDbType.NVarChar, 100) { Value = login });
                command.Parameters.Add(new SqlParameter("@password", SqlDbType.NVarChar, 200) { Value = password });

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    var id = reader.GetInt32(reader.GetOrdinal("Id"));
                    var dbLogin = reader.GetString(reader.GetOrdinal("Login"));
                    var role = reader.IsDBNull(reader.GetOrdinal("Role")) ? string.Empty : reader.GetString(reader.GetOrdinal("Role"));
                    var fullName = ReadOptionalDisplayName(reader);
                    return new AuthenticatedUser(id, dbLogin, role, fullName);
                }
            }
        }

        private static string BuildAuthenticateSql(SqlConnection connection)
        {
            var displaySelect = string.Empty;

            if (DatabaseSchemaHelper.ColumnExists(connection, "Users", "FullName"))
            {
                displaySelect = ", FullName AS DisplayName";
            }
            else if (DatabaseSchemaHelper.ColumnExists(connection, "Users", "FIO"))
            {
                displaySelect = ", FIO AS DisplayName";
            }
            else if (DatabaseSchemaHelper.ColumnExists(connection, "Users", "UserFIO"))
            {
                displaySelect = ", UserFIO AS DisplayName";
            }

            return
                "SELECT TOP (1) Id, Login, Role" + displaySelect + " " +
                "FROM Users " +
                "WHERE Login = @login AND Password = @password";
        }

        private static string ReadOptionalDisplayName(SqlDataReader reader)
        {
            try
            {
                var ordinal = reader.GetOrdinal("DisplayName");
                if (reader.IsDBNull(ordinal))
                {
                    return null;
                }

                return reader.GetString(ordinal);
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
        }
    }
}
