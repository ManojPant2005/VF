using Microsoft.Extensions.DependencyInjection;
using System;
using UI.Services;

namespace UI.Data
{
    public class DatabaseConfigurationFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DatabaseConfigurationFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IDatabaseConfigurationService GetService(string databaseType)
        {
            return databaseType switch
            {
                "SQL" => _serviceProvider.GetRequiredService<SqlServerConfigurationService>(),
                // Add more  (MySQL, Oracle, SQLite) 
                _ => throw new NotImplementedException($"Database type '{databaseType}' is not supported."),
            };
        }
    }
}