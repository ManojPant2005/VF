using System;
using System.Data.SqlClient;
using System.IO;
using System.ServiceProcess;
using System.Timers;

namespace Service
{
    public partial class Service1 : ServiceBase
    {
        private Timer _timer;
        private readonly string _connectionString = "Server=MP\\SQLEXPRESS;Database=VF;Trusted_Connection=True;";
        private readonly string _logFilePath = "D:\\IN\\VF\\VF_SMS.txt";

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _timer = new Timer(300000); // 5 minutes
            _timer.Elapsed += OnElapsedTime;
            _timer.Start();

            LogMessage("VF SMS Service started successfully.");
        }

        protected override void OnStop()
        {
            _timer.Stop();
            LogMessage("VF SMS Service stopped.");
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            ProcessSmsMessages();
        }

        private void ProcessSmsMessages()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Fetch SMS records
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

                                // Log the message
                                string logMessage = $"The message {smsId} was sent to {mobileNumber} at {DateTime.Now}";
                                LogMessage(logMessage);

                                // Update the SMS_transmitted_on
                                UpdateSmsTransmittedOn(smsId);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error processing SMS messages: {ex.Message}");
            }
        }

        private void UpdateSmsTransmittedOn(int smsId)
        {
            try
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
            catch (Exception ex)
            {
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
    }
}
