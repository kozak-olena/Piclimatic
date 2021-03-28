using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Piclimatic
{
    class Program
    {
        public static string BotToken;

        public static async Task Main(string[] args)
        {
            BotToken = args[0];

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
                            services.AddHostedService<TelegramBotHostedService>();

                            services.AddSingleton<ISynchronizer, Synchronizer>();
                        }
                    );

            return host;
        }
    }
}
