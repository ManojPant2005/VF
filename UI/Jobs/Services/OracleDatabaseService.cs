using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System;

namespace UI.Jobs.Services
{
    public class OracleDatabaseService : IDatabaseService
    {
        private readonly string _connectionString;

        public OracleDatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void ProcessSmsMessages()
        {
            using (OracleConnection connection = new OracleConnection(_connectionString))
            {
                connection.Open();
                using (OracleCommand fetchCommand = new OracleCommand("USP_VF_FETCH_SMS", connection))
                {
                    fetchCommand.CommandType = System.Data.CommandType.StoredProcedure;

                    using (OracleDataReader reader = fetchCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int smsId = reader.GetInt32(0);
                            string mobileNumber = reader.GetString(1);
                            string messageText = reader.GetString(2);
                            Console.WriteLine($"Oracle: Message ID {smsId} sent to {mobileNumber}: {messageText}");

                            // Update SMS_transmitted_on
                            UpdateSmsTransmittedOn(smsId);
                        }
                    }
                }
            }
        }

        public void UpdateSmsTransmittedOn(int smsId)
        {
            using (OracleConnection connection = new OracleConnection(_connectionString))
            {
                connection.Open();
                using (OracleCommand updateCommand = new OracleCommand("USP_VF_UPDATE_SMS", connection))
                {
                    updateCommand.CommandType = System.Data.CommandType.StoredProcedure;
                    updateCommand.Parameters.Add("SmsID", smsId);
                    updateCommand.ExecuteNonQuery();
                }
            }
        }
    }
}
