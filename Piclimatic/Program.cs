using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Piclimatic
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var host = 
                Host.CreateDefaultBuilder(args)
                    .ConfigureServices
                    (
                        (hostContext, services) =>
                        {
                            services.AddHostedService<Dht11HostedService>();
                            services.AddHostedService<RelayHostedService>();
                        }
                    );

            return host;
        }
    }
}
