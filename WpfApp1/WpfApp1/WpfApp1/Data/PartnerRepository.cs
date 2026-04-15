using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using WpfApp1.Models;

namespace WpfApp1.Data
{
    public class PartnerRepository
    {
        public PartnerRepository()
        {
        }

        public IEnumerable<Partner> GetAll()
        {
            var partners = new List<Partner>();

            using (var connection = DbConnectionFactory.CreateOpen())
            {
                string query = "SELECT PartnerID, PartnerType, PartnerName, DirectorName, Email, Phone, LegalAddress, INN, Rating FROM Partner";

                using (var command = new SqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        partners.Add(new Partner
                        {
                            Id = reader.GetInt32(0),
                            PartnerType = reader.GetString(1),
                            Name = reader.GetString(2),
                            DirectorName = reader.GetString(3),
                            Email = reader.GetString(4),
                            Phone = reader.GetString(5),
                            Address = reader.IsDBNull(6) ? null : reader.GetString(6),
                            INN = reader.GetString(7),
                            Rating = reader.IsDBNull(8) ? 0 : reader.GetInt32(8)
                        });
                    }
                }
            }

            return partners;
        }

        public void Insert(Partner partner)
        {
            using (var connection = DbConnectionFactory.CreateOpen())
            {
                string query = @"INSERT INTO Partner (PartnerType, PartnerName, DirectorName, Email, Phone, LegalAddress, INN, Rating) 
                               VALUES (@PartnerType, @Name, @DirectorName, @Email, @Phone, @Address, @INN, 0)";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PartnerType", partner.PartnerType ?? "ннн");
                    command.Parameters.AddWithValue("@Name", partner.Name ?? "");
                    command.Parameters.AddWithValue("@DirectorName", partner.DirectorName ?? "");
                    command.Parameters.AddWithValue("@Email", partner.Email ?? "");
                    command.Parameters.AddWithValue("@Phone", partner.Phone ?? "");
                    command.Parameters.AddWithValue("@Address", (object)partner.Address ?? DBNull.Value);
                    command.Parameters.AddWithValue("@INN", partner.INN ?? "0000000000");
                    command.ExecuteNonQuery();
                }
            }
        }

        public void Update(Partner partner)
        {
            using (var connection = DbConnectionFactory.CreateOpen())
            {
                string query = @"UPDATE Partner 
                               SET PartnerType = @PartnerType, PartnerName = @Name, DirectorName = @DirectorName, 
                                   Email = @Email, Phone = @Phone, LegalAddress = @Address, INN = @INN 
                               WHERE PartnerID = @Id";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", partner.Id);
                    command.Parameters.AddWithValue("@PartnerType", partner.PartnerType ?? "ннн");
                    command.Parameters.AddWithValue("@Name", partner.Name ?? "");
                    command.Parameters.AddWithValue("@DirectorName", partner.DirectorName ?? "");
                    command.Parameters.AddWithValue("@Email", partner.Email ?? "");
                    command.Parameters.AddWithValue("@Phone", partner.Phone ?? "");
                    command.Parameters.AddWithValue("@Address", (object)partner.Address ?? DBNull.Value);
                    command.Parameters.AddWithValue("@INN", partner.INN ?? "0000000000");
                    command.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int partnerId)
        {
            using (var connection = DbConnectionFactory.CreateOpen())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string deleteSalesQuery = "DELETE FROM PartnerSales WHERE PartnerId = @PartnerId";
                        using (var cmd = new SqlCommand(deleteSalesQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@PartnerId", partnerId);
                            cmd.ExecuteNonQuery();
                        }

                        string deleteProductLinksQuery = "DELETE FROM PartnerProduct WHERE PartnerId = @PartnerId";
                        using (var cmd = new SqlCommand(deleteProductLinksQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@PartnerId", partnerId);
                            cmd.ExecuteNonQuery();
                        }

                        string deletePartnerQuery = "DELETE FROM Partner WHERE PartnerID = @PartnerId";
                        using (var cmd = new SqlCommand(deletePartnerQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@PartnerId", partnerId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}