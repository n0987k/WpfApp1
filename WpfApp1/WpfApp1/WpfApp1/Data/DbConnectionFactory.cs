using System;
using System.Configuration;
using System.Linq;
using System.Data.SqlClient;

namespace WpfApp1.Data
{
    public static class DbConnectionFactory
    {
        public static SqlConnection CreateOpen()
        {
            var candidates = new[]
            {
                "PartnersDb",
                "PartnersDb_Sqlexpress",
                "PartnersDb_Local",
                "PartnersDb_Dot",
            };

            Exception lastError = null;

            foreach (var name in candidates)
            {
                var cs = ConfigurationManager.ConnectionStrings[name]?.ConnectionString;
                if (string.IsNullOrWhiteSpace(cs))
                {
                    continue;
                }

                try
                {
                    var connection = new SqlConnection(cs);
                    connection.Open();
                    return connection;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }
            }

            var existing = ConfigurationManager.ConnectionStrings.Cast<ConnectionStringSettings>()
                .Select(x => x.Name)
                .ToArray();

            if (existing.Length == 0)
            {
                throw new InvalidOperationException(
                    "В App.config нет секции connectionStrings. Добавьте строку подключения к PartnersDB.");
            }

            var message =
                "Не удалось подключиться к SQL Server для базы PartnersDB.\n\n" +
                "Проверьте, что служба SQL Server запущена и имя экземпляра верное.\n" +
                "Проверьте строку подключения в App.config (connectionStrings).\n\n" +
                "Попробованные имена строк подключения: " + string.Join(", ", candidates) + "\n" +
                "Найденные в App.config: " + string.Join(", ", existing) + "\n\n" +
                "Последняя ошибка: " + (lastError?.Message ?? "неизвестно");

            throw new InvalidOperationException(message, lastError);
        }
    }
}
