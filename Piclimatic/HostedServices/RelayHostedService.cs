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
        private readonly ISynchronizer _synchronizer;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly ILogger<RelayHostedService> _logger;

        private Task _clickTask;
        private bool _state = false;

        public RelayHostedService
        (
            ISynchronizer synchronizer,
            IHostApplicationLifetime applicationLifetime,
            ILogger<RelayHostedService> logger
        )
        {
            _synchronizer = synchronizer;
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

            using var controller = new GpioController();
            controller.OpenPin(pin, PinMode.Output);

            try
            {

                while (!_applicationLifetime.ApplicationStopping.IsCancellationRequested)
                {
                    var requestedState = await _synchronizer.WhenRelayCommandPosted;
                    
                    if (_state != requestedState)
                    {
                        if (requestedState is true)
                        {
                            _logger.LogInformation($"Engaging relay");

                            controller.Write(pin, PinValue.Low);
                        }
                        if (requestedState is false)
                        {
                            _logger.LogInformation($"Disengaging relay");
                            
                            controller.Write(pin, PinValue.High);
                        }
                    
                        _state = requestedState;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected relay failure.");
            }
        }
    }
}
