using System;
using System.Device.Gpio;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Piclimatic
{
    public class RelayHostedService : IHostedService
    {
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly ILogger<RelayHostedService> _logger;

        private Task _clickTask;

        public RelayHostedService
        (
            IHostApplicationLifetime applicationLifetime,
            ILogger<RelayHostedService> logger
        )
        {
            _applicationLifetime = applicationLifetime;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _clickTask = ClickRelayContinuously();

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _clickTask;
        }

        private async Task ClickRelayContinuously()
        {
            var pin = 4;

            try
            {
                using var controller = new GpioController();
                controller.OpenPin(pin, PinMode.Output);

                while (!_applicationLifetime.ApplicationStopping.IsCancellationRequested)
                {
                    await Task.Delay(5000);
                    controller.Write(pin, PinValue.Low);
                    
                    await Task.Delay(5000);
                    controller.Write(pin, PinValue.High);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected relay failure.");
            }
        }
    }
}
