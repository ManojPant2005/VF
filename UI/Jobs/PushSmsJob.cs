using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
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

        private readonly string _configPath;
        public string ErrorMessage { get; private set; }

        public bool IsConfigLoaded => _isConfigLoaded;
        private readonly ILogger<PushSmsJob> _logger;
        public bool IsRunning => _isRunning;
        // Inject DatabaseConfigService and the config file path
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

                // Load the configuration before starting the service
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

            try
            {
                // Initialize PeriodicTimer
                _periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(60));

                // Main processing loop
                while (await _periodicTimer.WaitForNextTickAsync(stoppingToken))
                {
                    // Check if cancellation is requested
                    stoppingToken.ThrowIfCancellationRequested();

                    // Safely process SMS messages
                    try
                    {
                        ProcessSmsMessages();
                    }
                    catch (Exception smsEx)
                    {
                        _logger.LogError($"Error while processing SMS messages: {smsEx.Message}");
                        LogMessage($"Error while processing SMS messages: {smsEx.Message}");
                        // Optionally, you can rethrow or handle this error depending on your needs.
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Push SMS Job cancellation requested.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred: {ex.Message}");
                LogMessage($"An unexpected error occurred: {ex.Message}");
            }
            finally
            {
                _logger.LogInformation("Push SMS Job has stopped.");
                LogMessage("Push SMS Job has stopped.");

                // Properly dispose of the timer when done
                _periodicTimer?.Dispose();
            }
        }

        private void ProcessSmsMessages()
        {
            string connectionString = _databaseConfigService.GenerateConnectionString();
            _logger.LogInformation($"Using Connection String: {connectionString}");

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Fetch SMS
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

                                string logMessage = $"Message ID {smsId} sent to {mobileNumber} at {DateTime.Now}: {messageText}";
                                _logger.LogInformation(logMessage);
                                LogMessage(logMessage);

                                // Update SMS_transmitted_on
                                UpdateSmsTransmittedOn(smsId, connectionString);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing SMS messages: {ex.Message}");
                LogMessage($"Error processing SMS messages: {ex.Message}");
            }
        }

        private void UpdateSmsTransmittedOn(int smsId, string connectionString)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
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
            _periodicTimer?.Dispose(); // Dispose of the timer to stop further execution
            await base.StopAsync(cancellationToken);
        }
    }
}
