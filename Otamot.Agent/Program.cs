using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Otamot.Agent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton(_ => Channel.CreateUnbounded<IServiceEvent>(new UnboundedChannelOptions
                    {
                        SingleReader = true,
                        SingleWriter = true
                    }));
                    services.AddHostedService<PoliciesResolverService>();
                    services.AddHostedService<ServicesWatcher>();
                });
    }
}