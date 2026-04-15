using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using WpfApp1.Models;

namespace WpfApp1.Data
{
    public sealed class ProductTypeRepository
    {
        public List<ProductTypeOption> GetAll()
        {
            var list = new List<ProductTypeOption>();

            using (var connection = DbConnectionFactory.CreateOpen())
            {
                if (!DatabaseSchemaHelper.TableExists(connection, "ProductType"))
                {
                    return list;
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT ProductTypeID, ProductTypeName FROM ProductType ORDER BY ProductTypeName";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new ProductTypeOption
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            });
                        }
                    }
                }
            }

            return list;
        }
    }
}
