using Microsoft.Extensions.Logging;
using System.Data.SqlClient;
using System;

namespace UI.Jobs.Services
{
    using System;
    using System.Data.SqlClient;

    public class SqlServerDatabaseService : IDatabaseService
    {
        private readonly string _connectionString;

        public SqlServerDatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void ProcessSmsMessages()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand fetchCommand = new SqlCommand("USP_VF_FETCH_SMS", connection))
                {
                    fetchCommand.CommandType = System.Data.CommandType.StoredProcedure;

                    using (SqlDataReader reader = fetchCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int smsId = reader.GetInt32(0);
                            string mobileNumber = reader.GetString(1);
                            string messageText = reader.GetString(2);
                            Console.WriteLine($"SQL: Message ID {smsId} sent to {mobileNumber}: {messageText}");

                            // Update SMS_transmitted_on
                            UpdateSmsTransmittedOn(smsId);
                        }
                    }
                }
            }
        }

        public void UpdateSmsTransmittedOn(int smsId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand updateCommand = new SqlCommand("USP_VF_UPDATE_SMS", connection))
                {
                    updateCommand.CommandType = System.Data.CommandType.StoredProcedure;
                    updateCommand.Parameters.AddWithValue("@SmsID", smsId);
                    updateCommand.ExecuteNonQuery();
                }
            }
        }
    }
}
