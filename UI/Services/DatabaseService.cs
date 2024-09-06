using System.Data.SqlClient;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UI.Services
{
    public class DatabaseService
    {
        public async Task<bool> TestSqlConnectionAsync(string connectionString)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                Console.WriteLine("Connection successful.");
                return true;
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Exception: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CreateDatabaseIfNotExistsAsync(string connectionStringWithoutDb, string databaseName)
        {
            try
            {
                using var connection = new SqlConnection(connectionStringWithoutDb);
                await connection.OpenAsync();

                var createDatabaseQuery = $@"
            IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '{databaseName}')
            BEGIN
                EXEC('CREATE DATABASE [{databaseName}]')
            END";

                using var command = new SqlCommand(createDatabaseQuery, connection);
                await command.ExecuteNonQueryAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating database: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CreateTableAsync(string connectionString, string tableName,
            Dictionary<string, string> columnMappings)
        {
            try
            {
                var columnDefinitions = string.Join(", ", columnMappings
                    .Where(c => c.Key != "SmsID")
                    .Select(c => $"[{c.Value}] NVARCHAR(MAX)"));

                var createTableQuery = $@"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}')
            BEGIN
                CREATE TABLE [{tableName}] (
                    [SmsID] INT IDENTITY(1,1) PRIMARY KEY,
                {columnDefinitions}
                )
            END";

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(createTableQuery, connection);
                await command.ExecuteNonQueryAsync();

                Console.WriteLine("Table created successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing SQL: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CreateStoredProceduresAsync(
            string connectionString,
            string tableName,
            string columnSmsID,
            string columnMobileNumber,
            string columnMessageText,
            string columnSmsProcessOn,
            string columnSmsTransmittedOn)
        {
            var createFetchProcedureQuery = $@"
        IF OBJECT_ID('dbo.USP_VF_FETCH_SMS', 'P') IS NULL
        BEGIN
            EXEC('
            CREATE PROCEDURE [dbo].[USP_VF_FETCH_SMS]
            AS
            BEGIN
                SET NOCOUNT ON;

                SELECT [{columnSmsID}], [{columnMobileNumber}], [{columnMessageText}]
                FROM [{tableName}]
                WHERE [{columnSmsProcessOn}] IS NULL;

                UPDATE [{tableName}]
                SET [{columnSmsProcessOn}] = GETDATE()
                WHERE [{columnSmsProcessOn}] IS NULL;
            END')
        END";

            var createUpdateProcedureQuery = $@"
        IF OBJECT_ID('dbo.USP_VF_UPDATE_SMS', 'P') IS NULL
        BEGIN
            EXEC('
            CREATE PROCEDURE [dbo].[USP_VF_UPDATE_SMS]
            @SmsID INT
            AS
            BEGIN
                SET NOCOUNT ON;

                UPDATE [{tableName}]
                SET [{columnSmsTransmittedOn}] = GETDATE()
                WHERE [{columnSmsID}] = @SmsID;
            END')
        END";

            return await ExecuteSqlAsync(connectionString, createFetchProcedureQuery) &&
                   await ExecuteSqlAsync(connectionString, createUpdateProcedureQuery);
        }

        private async Task<bool> ExecuteSqlAsync(string connectionString, string sql)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                using var command = new SqlCommand(sql, connection);
                await command.ExecuteNonQueryAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing SQL: {ex.Message}");
                return false;
            }
        }

        public string GenerateConnectionString(string serverName, string databaseName, string username, string password,
            bool isSQLAuthSelected, bool trustServerCertificate)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = serverName,
                IntegratedSecurity = !isSQLAuthSelected,
                TrustServerCertificate = trustServerCertificate
            };

            if (isSQLAuthSelected)
            {
                builder.UserID = username;
                builder.Password = password;
            }

            if (!string.IsNullOrWhiteSpace(databaseName))
            {
                builder.InitialCatalog = databaseName;
            }

            return builder.ConnectionString;
        }
    }
}