using System;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Otamot.Agent
{
    public class PoliciesResolverService : BackgroundService
    {
        private readonly ILogger<PoliciesResolverService> _logger;
        private readonly ChannelReader<IServiceEvent> _events;

        public PoliciesResolverService(ILogger<PoliciesResolverService> logger, Channel<IServiceEvent> channel)
        {
            _events = channel.Reader;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogCritical($"{nameof(PoliciesResolverService)} service is now running");
            await foreach (var @event in _events.ReadAllAsync(stoppingToken))
            {
                _logger.LogCritical($"Received new event for service {@event.Service.Controller.ServiceName}");
                var policies = @event.Service.Policies;
                foreach (var policy in policies)
                {
                    _logger.LogInformation($"{policy} <<<<<");
                    switch (policy)
                    {
                        case RestartPolicy restartPolicy:
                            ExecuteRestartPolicy(stoppingToken, @event, restartPolicy);
                            break;
                        default:
                            _logger.LogWarning($"Unknown policy {policy}");
                            break;
                    }
                }
            }
        }

        private void ExecuteRestartPolicy(CancellationToken stoppingToken, IServiceEvent @event,
            RestartPolicy restartPolicy)
        {
            if (@event.Service.Status == ServiceControllerStatus.Stopped)
            {
                var _ = Task.Run(async () =>
                {
                    for (var i = 0; i < restartPolicy.MaxRestarts; i++)
                    {
                        await ExecutePolicy(stoppingToken, restartPolicy, @event, i);
                        return;
                    }
                }, stoppingToken);
                return;
            }

            _logger.LogInformation("Restart policy does not apply for current service state");
        }

        private async Task ExecutePolicy(CancellationToken stoppingToken, RestartPolicy restartPolicy,
            IServiceEvent @event, int i)
        {
            try
            {
                var waitTime = restartPolicy.WaitTime;
                switch (restartPolicy.Strategy)
                {
                    case RetryStrategy.Constant:
                        break;
                    case RetryStrategy.Incremental:
                        restartPolicy.WaitTime = restartPolicy.WaitTime.Add(waitTime);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                await Task.Delay(waitTime, stoppingToken);
                @event.Service.Controller.Start();
                _logger.LogInformation($"Successfully executed policy {restartPolicy}");
                restartPolicy.RestartsWithoutSuccess = 0;
                return;
            }
            catch (Exception e)
            {
                _logger.LogError(
                    $"Could not execute policy [{i} / {restartPolicy.MaxRestarts}]");
                _logger.LogInformation($"Retrying in {restartPolicy.WaitTime}");
                _logger.LogError(e.Message);
            }
        }
    }
}