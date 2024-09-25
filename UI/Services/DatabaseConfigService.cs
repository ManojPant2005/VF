using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

public class DatabaseConfigService
{
    public string ConnectionString { get; set; }
    public string TableName { get; set; }
    public string DatabaseType { get; set; } // Add this property

    // Load the configuration 
    public async Task LoadConfigurationAsync(string configPath)
    {
        if (File.Exists(configPath))
        {
            try
            {
                Console.WriteLine($"Loading configuration from {configPath}...");

                var json = await File.ReadAllTextAsync(configPath);
                Console.WriteLine($"JSON content: {json}");

                var config = JsonSerializer.Deserialize<DatabaseConfigService>(json);

                if (config == null)
                {
                    throw new InvalidOperationException("Failed to deserialize the configuration.");
                }

                ConnectionString = config.ConnectionString;
                TableName = config.TableName;
                DatabaseType = config.DatabaseType; // Initialize DatabaseType from the config
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
                throw;
            }
        }
        else
        {
            throw new FileNotFoundException($"Configuration file not found at {configPath}");
        }
    }

    public string GenerateConnectionString()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException("Connection string is not set.");
        }
        return ConnectionString;
    }
}