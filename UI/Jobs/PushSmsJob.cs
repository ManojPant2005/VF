using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client; // Oracle data access
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UI.Services;

namespace UI.Jobs
{
    public class PushSmsJob : BackgroundService
    {
        private readonly DatabaseConfigService _databaseConfigService;
        private readonly string _logFilePath = "D:\\IN\\VF\\VF_SMS.txt";
        private PeriodicTimer _periodicTimer;
        private bool _isConfigLoaded = false;
        private bool _isRunning = false;
        public string ErrorMessage { get; private set; }
        private readonly ILogger<PushSmsJob> _logger;
        private readonly string _configPath;

        public bool IsConfigLoaded => _isConfigLoaded;
        public bool IsRunning => _isRunning;

        public PushSmsJob(DatabaseConfigService databaseConfigService, string configPath, ILogger<PushSmsJob> logger)
        {
            _databaseConfigService = databaseConfigService;
            _configPath = configPath;
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _isRunning = true;
                _logger.LogInformation("Push SMS Service starting...");

                await _databaseConfigService.LoadConfigurationAsync(_configPath);

                if (string.IsNullOrEmpty(_databaseConfigService.ConnectionString))
                {
                    throw new InvalidOperationException("Connection string is not set.");
                }

                _isConfigLoaded = true;
                _logger.LogInformation("Configuration loaded successfully.");
                await base.StartAsync(cancellationToken);
            }
            catch (FileNotFoundException)
            {
                ErrorMessage = "Database configuration is missing. Please configure the database first.";
                _logger.LogError(ErrorMessage);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred while starting the service: {ex.Message}";
                _logger.LogError(ErrorMessage);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_isConfigLoaded)
            {
                LogMessage(ErrorMessage ?? "Configuration not loaded. Cannot start SMS processing.");
                return;
            }

            _logger.LogInformation("Push SMS Job is running.");
            _periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(60));

            while (await _periodicTimer.WaitForNextTickAsync(stoppingToken))
            {
                stoppingToken.ThrowIfCancellationRequested();
                try
                {
                    ProcessSmsMessages();
                }
                catch (Exception smsEx)
                {
                    _logger.LogError($"Error while processing SMS messages: {smsEx.Message}");
                    LogMessage($"Error while processing SMS messages: {smsEx.Message}");
                }
            }
        }

        private void ProcessSmsMessages()
        {
            string connectionString = _databaseConfigService.GenerateConnectionString();
            _logger.LogInformation($"Using Connection String: {connectionString}");

            try
            {
                if (_databaseConfigService.DatabaseType == "SQLServer")
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        ProcessSmsForSqlServer(connection);
                    }
                }
                else if (_databaseConfigService.DatabaseType == "Oracle")
                {
                    using (OracleConnection connection = new OracleConnection(connectionString))
                    {
                        connection.Open();
                        ProcessSmsForOracle(connection);
                    }
                }
            }
            catch (OracleException ex)
            {
                _logger.LogError($"Oracle connection error: {ex.Message} - {ex.StackTrace}");
                LogMessage($"Oracle connection error: {ex.Message}");
            }
            catch (SqlException ex)
            {
                _logger.LogError($"SQL Server connection error: {ex.Message} - {ex.StackTrace}");
                LogMessage($"SQL Server connection error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing SMS messages: {ex.Message}");
                LogMessage($"Error processing SMS messages: {ex.Message}");
            }
        }

        private void ProcessSmsForSqlServer(SqlConnection connection)
        {
            using (SqlCommand fetchCommand = new SqlCommand("USP_VF_FETCH_SMS", connection))
            {
                fetchCommand.CommandType = CommandType.StoredProcedure;

                using (SqlDataReader reader = fetchCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int smsId = reader.GetInt32(0);
                        string mobileNumber = reader.GetString(1);
                        string messageText = reader.GetString(2);
                        string logMessage = $"Message ID {smsId} sent to {mobileNumber} at {DateTime.Now}: {messageText}";
                        _logger.LogInformation(logMessage);
                        LogMessage(logMessage);
                        UpdateSmsTransmittedOn(smsId, connection.ConnectionString, "SQLServer");
                    }
                }
            }
        }

        private void ProcessSmsForOracle(OracleConnection connection)
        {
            using (OracleCommand fetchCommand = new OracleCommand("USP_VF_FETCH_SMS", connection))
            {
                fetchCommand.CommandType = CommandType.StoredProcedure;
                fetchCommand.Parameters.Add("p_cursor", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                _logger.LogInformation("Executing USP_VF_FETCH_SMS on Oracle database...");

                using (OracleDataReader reader = fetchCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int smsId = reader.GetInt32(0);
                        string mobileNumber = reader.GetString(1);
                        string messageText = reader.GetString(2);
                        string logMessage = $"Processing Message ID {smsId} sent to {mobileNumber} at {DateTime.Now}: {messageText}";
                        _logger.LogInformation(logMessage);
                        LogMessage(logMessage);
                        UpdateSmsTransmittedOn(smsId, connection.ConnectionString, "Oracle");
                    }
                }
                _logger.LogInformation("Reader finished processing.");
            }
        }

        private void UpdateSmsTransmittedOn(int smsId, string connectionString, string dbType)
        {

            string cs = _databaseConfigService.GenerateConnectionString();
            _logger.LogInformation($"Using Connection String: {cs}");
            try
            {
                _logger.LogInformation($"Attempting to update SMS ID: {smsId} using {dbType} database.");

                if (dbType == "SQLServer")
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        using (SqlCommand updateCommand = new SqlCommand("USP_VF_UPDATE_SMS", connection))
                        {
                            updateCommand.CommandType = CommandType.StoredProcedure;
                            updateCommand.Parameters.AddWithValue("@SmsID", smsId);
                            updateCommand.ExecuteNonQuery();
                        }
                    }
                }
                else if (dbType == "Oracle")
                {
                    using (OracleConnection connection = new OracleConnection(cs))
                    {
                        connection.Open();
                        using (OracleCommand updateCommand = new OracleCommand("USP_VF_UPDATE_SMS", connection))
                        {
                            updateCommand.CommandType = CommandType.StoredProcedure;
                            updateCommand.Parameters.Add("SmsID", OracleDbType.Int32).Value = smsId;
                            updateCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating SMS record {smsId}: {ex.Message}");
                LogMessage($"Error updating SMS record {smsId}: {ex.Message}");
            }
        }

        private void LogMessage(string message)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(_logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now}: {message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error logging message to file: {ex.Message}");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _isRunning = false;
            _logger.LogInformation("Push SMS Service stopping...");
            _periodicTimer?.Dispose();
            await base.StopAsync(cancellationToken);
        }
    }
}
