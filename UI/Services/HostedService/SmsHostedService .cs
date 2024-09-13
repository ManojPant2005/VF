//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Data.SqlClient;
//using System.IO;
//using System.Threading;
//using System.Threading.Tasks;

//namespace UI.Services
//{
//    public class SmsHostedService : IHostedService, IDisposable
//    {
//        private readonly ILogger<SmsHostedService> _logger;
//        private readonly string _connectionString = "Server=MP\\SQLEXPRESS;Database=VF;Trusted_Connection=True;";
//        private readonly string _logFilePath = "D:\\IN\\VF\\VF_SMS.txt";
//        private Timer _timer;
//        private CancellationTokenSource _cancellationTokenSource;

//        public SmsHostedService(ILogger<SmsHostedService> logger)
//        {
//            _logger = logger;
//        }

//        // Called by the host to start the service
//        public Task StartAsync(CancellationToken cancellationToken)
//        {
//            _cancellationTokenSource = new CancellationTokenSource();
//            _logger.LogInformation("VF SMS Hosted Service is starting.");

//            // Start the timer to trigger the background task every 5 minutes
//            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));

//            return Task.CompletedTask;
//        }

//        // Background task logic
//        private void DoWork(object state)
//        {
//            if (_cancellationTokenSource.IsCancellationRequested)
//            {
//                _logger.LogInformation("Hosted service task cancelled.");
//                return;
//            }

//            // Process SMS messages
//            ProcessSmsMessages();
//        }

//        private void ProcessSmsMessages()
//        {
//            try
//            {
//                using (SqlConnection connection = new SqlConnection(_connectionString))
//                {
//                    connection.Open();

//                    // Fetch SMS records
//                    using (SqlCommand fetchCommand = new SqlCommand("USP_VF_FETCH_SMS", connection))
//                    {
//                        fetchCommand.CommandType = System.Data.CommandType.StoredProcedure;

//                        using (SqlDataReader reader = fetchCommand.ExecuteReader())
//                        {
//                            while (reader.Read())
//                            {
//                                int smsId = reader.GetInt32(0);
//                                string mobileNumber = reader.GetString(1);
//                                string messageText = reader.GetString(2);

//                                // Log the message
//                                string logMessage = $"The message {smsId} was sent to {mobileNumber} at {DateTime.Now}";
//                                LogMessage(logMessage);

//                                // Update the SMS_transmitted_on column
//                                UpdateSmsTransmittedOn(smsId);
//                            }
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                LogMessage($"Error processing SMS messages: {ex.Message}");
//            }
//        }

//        private void UpdateSmsTransmittedOn(int smsId)
//        {
//            try
//            {
//                using (SqlConnection connection = new SqlConnection(_connectionString))
//                {
//                    connection.Open();

//                    using (SqlCommand updateCommand = new SqlCommand("USP_VF_UPDATE_SMS", connection))
//                    {
//                        updateCommand.CommandType = System.Data.CommandType.StoredProcedure;
//                        updateCommand.Parameters.AddWithValue("@SmsID", smsId);

//                        updateCommand.ExecuteNonQuery();
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                LogMessage($"Error updating SMS record {smsId}: {ex.Message}");
//            }
//        }

//        private void LogMessage(string message)
//        {
//            using (StreamWriter writer = new StreamWriter(_logFilePath, true))
//            {
//                writer.WriteLine($"{DateTime.Now}: {message}");
//            }
//        }

//        // Called by the host to stop the service
//        public Task StopAsync(CancellationToken cancellationToken)
//        {
//            _logger.LogInformation("VF SMS Hosted Service is stopping.");
//            _cancellationTokenSource.Cancel();  // Cancel the task
//            _timer?.Change(Timeout.Infinite, 0);  // Stop the timer
//            return Task.CompletedTask;
//        }

//        // Called when the service is disposed
//        public void Dispose()
//        {
//            _timer?.Dispose();
//            _cancellationTokenSource?.Dispose();
//        }
//    }
//}
