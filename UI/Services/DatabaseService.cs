using System.Data.SqlClient;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;

namespace UI.Services
{
    public class DatabaseService
    {
        private readonly ILogger _logger;
        public DatabaseService(ILogger<DatabaseService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> TestOracleConnectionAsync(string connectionString)
        {
            try
            {
                using (var connection = new OracleConnection(connectionString))
                {
                    await connection.OpenAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to connect to Oracle: {ex.Message}");
                return false;
            }
        }

        //public async Task<bool> CreateOracleTableAsync(string connectionString, string tableName, Dictionary<string, string> columnMappings)
        //{
        //    using (var connection = new OracleConnection(connectionString))
        //    {
        //        await connection.OpenAsync();
        //        var columns = string.Join(", ", columnMappings.Select(kvp => $"{kvp.Key} {kvp.Value}"));
        //        var createTableSql = $"CREATE TABLE {tableName} ({columns})";

        //        Console.WriteLine($"Create Table SQL: {createTableSql}"); // Log the SQL statement

        //        using (var command = new OracleCommand(createTableSql, connection))
        //        {
        //            try
        //            {
        //                await command.ExecuteNonQueryAsync();
        //                return true;
        //            }
        //            catch (OracleException ex)
        //            {
        //                Console.WriteLine($"Failed to create table in Oracle: {ex.Message}");
        //                return false;
        //            }
        //        }
        //    }
        //}

        public async Task<bool> CreateOracleTableAsync(string connectionString, string tableName, Dictionary<string, string> columnNames, Dictionary<string, string> columnDataTypes)
        {
            using (var connection = new OracleConnection(connectionString))
            {
                await connection.OpenAsync();

                // Construct the SQL query to create the table with column names and data types
                var createTableQuery = $"CREATE TABLE {tableName} (";

                Console.WriteLine("Column Names: ");
                foreach (var column in columnNames)
                {
                    Console.WriteLine($"Key: {column.Key}, Value: {column.Value}");

                    var columnName = column.Value;
                    var columnDataType = columnDataTypes[column.Key];

                    // Ensure column name is valid
                    if (!IsValidOracleIdentifier(columnName))
                    {
                        Console.WriteLine($"Invalid column name: {columnName}");
                        return false; // Invalid column name
                    }

                    createTableQuery += $"{columnName} {columnDataType},";
                }

                // Remove the trailing comma and add closing parenthesis
                createTableQuery = createTableQuery.TrimEnd(',') + ")";

                using (var command = new OracleCommand(createTableQuery, connection))
                {
                    try
                    {
                        await command.ExecuteNonQueryAsync();
                        Console.WriteLine($"Table {tableName} created successfully.");
                        return true; // Table created successfully
                    }
                    catch (OracleException ex)
                    {
                        Console.WriteLine($"Error creating table: {ex.Message}");
                        return false;
                    }
                }
            }
        }

        // Helper method to validate Oracle identifiers
        private bool IsValidOracleIdentifier(string identifier)
        {
            // Check if the identifier is a valid Oracle identifier
            if (string.IsNullOrEmpty(identifier) || identifier.Length > 30)
            {
                return false; // Identifier must not be null or exceed 30 characters
            }

            if (!char.IsLetter(identifier[0]))
            {
                return false; // Must start with a letter
            }

            // Check for invalid characters (only letters, digits, and underscores are allowed)
            foreach (char c in identifier)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    return false; // Invalid character found
                }
            }

            return true; // Identifier is valid
        }


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
                Console.WriteLine($"SQL Exception: {ex.Message} - Error code: {ex.Number}, Line: {ex.LineNumber}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Exception: {ex.Message}");
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

        public async Task<bool> CreateTableAsync(string connectionString, string tableName, Dictionary<string, string> columnMappings)
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

        //public async Task<bool> CreateStoredProceduresAsync(string connectionString, string tableName, string columnSmsID,
        //    string columnMobileNumber, string columnMessageText, string columnSmsProcessOn, string columnSmsTransmittedOn)
        //{
        //    var createFetchProcedureQuery = $@"
        //    IF OBJECT_ID('dbo.USP_VF_FETCH_SMS', 'P') IS NULL
        //    BEGIN
        //        EXEC('
        //        CREATE PROCEDURE [dbo].[USP_VF_FETCH_SMS]
        //        AS
        //        BEGIN
        //            SET NOCOUNT ON;

        //            SELECT [{columnSmsID}], [{columnMobileNumber}], [{columnMessageText}]
        //            FROM [{tableName}]
        //            WHERE [{columnSmsProcessOn}] IS NULL;

        //            UPDATE [{tableName}]
        //            SET [{columnSmsProcessOn}] = GETDATE()
        //            WHERE [{columnSmsProcessOn}] IS NULL;
        //        END')
        //    END";

        //    var createUpdateProcedureQuery = $@"
        //    IF OBJECT_ID('dbo.USP_VF_UPDATE_SMS', 'P') IS NULL
        //    BEGIN
        //        EXEC('
        //        CREATE PROCEDURE [dbo].[USP_VF_UPDATE_SMS]
        //        @SmsID INT
        //        AS
        //        BEGIN
        //            SET NOCOUNT ON;

        //            UPDATE [{tableName}]
        //            SET [{columnSmsTransmittedOn}] = GETDATE()
        //            WHERE [{columnSmsID}] = @SmsID;
        //        END')
        //    END";

        //    return await ExecuteSqlAsync(connectionString, createFetchProcedureQuery) &&
        //           await ExecuteSqlAsync(connectionString, createUpdateProcedureQuery);
        //}


        public async Task<bool> CreateSqlStoredProceduresAsync(string connectionString, string tableName, string columnSmsID,
    string columnMobileNumber, string columnMessageText, string columnSmsProcessOn, string columnSmsTransmittedOn)
        {
            // Existing SQL Server procedure creation code (renamed)
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



        public async Task<bool> CreateOracleStoredProceduresAsync(string connectionString, string tableName, string columnSmsID,
            string columnMobileNumber, string columnMessageText, string columnSmsProcessOn, string columnSmsTransmittedOn)
        {
            // Create Fetch Procedure Query using correct Oracle syntax
            var createFetchProcedureQuery = $@"
CREATE OR REPLACE PROCEDURE USP_VF_FETCH_SMS AS
    v_SmsID {columnSmsID}%TYPE;
    v_MobileNumber {columnMobileNumber}%TYPE;
    v_MessageText {columnMessageText}%TYPE;
BEGIN
    -- Select one record where SMS_process_on is NULL
    BEGIN
        SELECT {columnSmsID}, {columnMobileNumber}, {columnMessageText}
        INTO v_SmsID, v_MobileNumber, v_MessageText
        FROM {tableName}
        WHERE {columnSmsProcessOn} IS NULL
        AND ROWNUM = 1;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            NULL; -- No matching records found
    END;

    -- Update the processed record
    UPDATE {tableName}
    SET {columnSmsProcessOn} = SYSDATE
    WHERE {columnSmsID} = v_SmsID
    AND {columnSmsProcessOn} IS NULL;
END;
";

            // Create Update Procedure Query
            var createUpdateProcedureQuery = $@"
CREATE OR REPLACE PROCEDURE USP_VF_UPDATE_SMS (p_SmsID IN {columnSmsID}%TYPE) AS
BEGIN
    UPDATE {tableName}
    SET {columnSmsTransmittedOn} = SYSDATE
    WHERE {columnSmsID} = p_SmsID;
END;
";

            // Log the queries for debugging
            Console.WriteLine("Fetch Procedure Query: " + createFetchProcedureQuery);
            Console.WriteLine("Update Procedure Query: " + createUpdateProcedureQuery);

            // Execute the procedure creation queries
            var fetchProcedureResult = await ExecuteOracleSqlAsync(connectionString, createFetchProcedureQuery);
            var updateProcedureResult = await ExecuteOracleSqlAsync(connectionString, createUpdateProcedureQuery);

            return fetchProcedureResult && updateProcedureResult;
        }



        private async Task<bool> ExecuteOracleSqlAsync(string connectionString, string sql)
        {
            try
            {
                using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync();
                using var command = new OracleCommand(sql, connection);
                await command.ExecuteNonQueryAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing Oracle SQL: {ex.Message}");
                return false;
            }
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

        // Method to save configuration to a JSON file
        public async Task<bool> SaveConfigurationToJsonAsync(
            string connectionString,
            string tableName,
            Dictionary<string, string> columnMappings,
            string databaseType) // Add this parameter
        {
            try
            {
                // Define the configuration object
                var config = new
                {
                    ConnectionString = connectionString,
                    TableName = tableName,
                    ColumnMappings = columnMappings,
                    DatabaseType = databaseType // Include database type
                };

                // Define the path to the JSON file
                var filePath = "dbconfig.json";

                // Serialize the configuration to JSON format
                var jsonConfig = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

                // Save the JSON to a file asynchronously
                await File.WriteAllTextAsync(filePath, jsonConfig);

                Console.WriteLine("Configuration saved to dbconfig.json.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration to JSON: {ex.Message}");
                return false;
            }
        }

    }
}
