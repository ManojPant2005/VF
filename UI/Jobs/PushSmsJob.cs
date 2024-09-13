using Microsoft.Extensions.Hosting;
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
        private readonly PeriodicTimer _periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(60));

        private readonly string _configPath;

        // Inject the DatabaseConfigService and the config file path
        public PushSmsJob(DatabaseConfigService databaseConfigService, string configPath)
        {
            _databaseConfigService = databaseConfigService;
            _configPath = configPath; 
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Starting SMS Service...");

            // Load the configuration before starting the service
            await _databaseConfigService.LoadConfigurationAsync(_configPath);

            // Now start the actual background service
            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                Console.WriteLine("SMS Service is starting.");
                LogMessage("SMS Service started successfully.");

                while (!stoppingToken.IsCancellationRequested)
                {
                    // Process SMS messages
                    ProcessSmsMessages();

                    // Wait for the next interval
                    await _periodicTimer.WaitForNextTickAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("SMS Service cancellation requested.");
                LogMessage("SMS Service cancellation requested. Stopping the Push SMS Job.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                LogMessage($"An unexpected error occurred: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("SMS Service has stopped.");
                LogMessage("Push SMS Job has stopped.");
            }
        }

        private void ProcessSmsMessages()
        {
            // Fetch dynamic connection string from the configuration service
            string connectionString = _databaseConfigService.GenerateConnectionString();

            // Log the connection string for debugging purposes (be careful with sensitive info in production)
            Console.WriteLine($"Using Connection String: {connectionString}");

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
                                Console.WriteLine(logMessage);
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
                Console.WriteLine($"Error processing SMS messages: {ex.Message}");
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
                Console.WriteLine($"Error updating SMS record {smsId}: {ex.Message}");
                LogMessage($"Error updating SMS record {smsId}: {ex.Message}");
            }
        }

        private void LogMessage(string message)
        {
            using (StreamWriter writer = new StreamWriter(_logFilePath, true))
            {
                writer.WriteLine($"{DateTime.Now}: {message}");
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Stopping SMS Service...");
            return base.StopAsync(cancellationToken);
        }
    }
}
