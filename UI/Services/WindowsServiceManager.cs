using System;
using System.ServiceProcess;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UI.Services
{
    public class WindowsServiceManager
    {
        private readonly string _serviceName;
        private readonly ILogger<WindowsServiceManager> _logger;

        public WindowsServiceManager(IConfiguration configuration, ILogger<WindowsServiceManager> logger)
        {
            _serviceName = configuration["ServiceName"] ?? throw new ArgumentNullException("ServiceName configuration is missing.");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        //public bool StartService()
        //{
        //    try
        //    {
        //        using (ServiceController serviceController = new ServiceController(_serviceName))
        //        {
        //            if (serviceController.Status != ServiceControllerStatus.Running)
        //            {
        //                _logger.LogInformation($"Attempting to start the service: {_serviceName}");
        //                serviceController.Start();
        //                serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
        //                _logger.LogInformation($"Service {_serviceName} started successfully.");
        //            }
        //        }
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"An error occurred while attempting to start the service: {_serviceName}");
        //        return false;
        //    }
        //}

        //public bool StopService()
        //{
        //    try
        //    {
        //        using (ServiceController serviceController = new ServiceController(_serviceName))
        //        {
        //            if (serviceController.Status != ServiceControllerStatus.Stopped)
        //            {
        //                _logger.LogInformation($"Attempting to stop the service: {_serviceName}");
        //                serviceController.Stop();
        //                serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
        //                _logger.LogInformation($"Service {_serviceName} stopped successfully.");
        //            }
        //        }
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"An error occurred while attempting to stop the service: {_serviceName}");
        //        return false;
        //    }
        //}

        //public bool IsServiceRunning()
        //{
        //    try
        //    {
        //        using (ServiceController serviceController = new ServiceController(_serviceName))
        //        {
        //            bool isRunning = serviceController.Status == ServiceControllerStatus.Running;
        //            _logger.LogInformation($"Service {_serviceName} is {(isRunning ? "running" : "not running")}.");
        //            return isRunning;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"An error occurred while checking the status of the service: {_serviceName}");
        //        return false;
        //    }
        //}
    }
}
