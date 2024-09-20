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
    }
}