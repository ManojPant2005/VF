using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace UI.Services
{
    public class SqlServerConfigurationService : IDatabaseConfigurationService
    {
        public async Task<bool> CreateDatabaseAsync(string serverName, string databaseName, string connectionString, bool trustServerCertificate)
        {
            var createDbQuery = $"CREATE DATABASE {databaseName}";

            try
            {
                // Modify connection string to include server name and trust certificate option
                var connectionStringWithServer = $"{connectionString};Server={serverName};";
                if (trustServerCertificate)
                {
                    connectionStringWithServer += "TrustServerCertificate=True;";
                }

                using (var connection = new SqlConnection(connectionStringWithServer))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(createDbQuery, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                // Log and handle the exception
                return false;
            }
        }

        public async Task<bool> CreateTableAsync(string serverName, string databaseName, string tableName, IEnumerable<string> columns)
        {
            var columnDefinitions = string.Join(", ", columns.Select(c => $"{c} NVARCHAR(MAX)"));
            var createTableQuery = $"CREATE TABLE {tableName} (SmsID INT PRIMARY KEY IDENTITY(1,1), {columnDefinitions})";

            try
            {
                using (var connection = new SqlConnection($"Server={serverName};Database={databaseName};Trusted_Connection=True;"))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(createTableQuery, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                // Log and handle the exception
                return false;
            }
        }

        public async Task<bool> CreateStoredProceduresAsync(string serverName, string databaseName)
        {
            var createFetchProcQuery = @"
                CREATE PROCEDURE USP_VF_FETCH_SMS
                AS
                BEGIN
                    SELECT * FROM SmsTable WHERE SMS_Process_on IS NULL;
                END";

            var createUpdateProcQuery = @"
                CREATE PROCEDURE USP_VF_UPDATE_SMS
                @SmsID INT
                AS
                BEGIN
                    UPDATE SmsTable SET SMS_transmitted_on = GETDATE() WHERE SmsID = @SmsID;
                END";

            try
            {
                using (var connection = new SqlConnection($"Server={serverName};Database={databaseName};Trusted_Connection=True;"))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(createFetchProcQuery, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }

                    using (var command = new SqlCommand(createUpdateProcQuery, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }

}
