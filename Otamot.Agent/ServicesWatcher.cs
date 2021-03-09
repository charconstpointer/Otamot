using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Otamot.Agent
{
    public class ServicesWatcher : BackgroundService
    {
        private readonly ILogger<ServicesWatcher> _logger;
        private readonly ICollection<Service> _services = new HashSet<Service>();
        private readonly ChannelWriter<IServiceEvent> _events;

        public ServicesWatcher(ILogger<ServicesWatcher> logger, Channel<IServiceEvent> channel)
        {
            _logger = logger;
            _events = channel.Writer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{nameof(ServicesWatcher)} service is now running");
            LoadAvailableServices();
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var service in _services)
                {
                    service.Controller.Refresh();

                    if (service.Controller.Status == service.Status)
                    {
                        continue;
                    }

                    service.Status = service.Controller.Status;
                    _logger.LogInformation(
                        $"Service {service.Controller.ServiceName} has changed its status to {service.Status}");
                    await _events.WriteAsync(new ServiceStatusChanged
                    {
                        Service = service
                    }, stoppingToken);
                }
            }
        }

        private void LoadAvailableServices()
        {
            _logger.LogInformation("Loading available Windows services");
            var services = ServiceController.GetServices();
            foreach (var service in services)
            {
                _logger.LogInformation($"Loaded {service.ServiceName} service");
                var s = new Service(service);
                if (service.ServiceName == "BTAGService")
                {
                    s.AddPolicy(new RestartPolicy
                    {
                        MaxRestarts = 3,
                        WaitTime = TimeSpan.FromSeconds(3),
                        Strategy = RetryStrategy.Incremental
                    });
                }

                _services.Add(s);
            }

            _logger.LogInformation($"Loaded {_services.Count} services");
        }
    }
}