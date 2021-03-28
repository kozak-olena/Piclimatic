using System;
using System.Device.Gpio;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Piclimatic
{
    class RelayHostedService : IHostedService
    {
        private readonly IEventHub _eventHub;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly ILogger<RelayHostedService> _logger;

        private Task _clickTask;
        private bool _state = false;

        public RelayHostedService
        (
            IEventHub eventHub,
            IHostApplicationLifetime applicationLifetime,
            ILogger<RelayHostedService> logger
        )
        {
            _eventHub = eventHub;
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
                    var command = await _eventHub.ReceiveRelayCommand(_applicationLifetime.ApplicationStopping);
                    
                    if (_state != command.RequestedState)
                    {
                        if (command.RequestedState is true)
                        {
                            _logger.LogInformation($"Engaging relay");

                            controller.Write(pin, PinValue.Low);
                        }
                        if (command.RequestedState is false)
                        {
                            _logger.LogInformation($"Disengaging relay");
                            
                            controller.Write(pin, PinValue.High);
                        }
                    
                        _state = command.RequestedState;
                    }
                }
            }
            catch (OperationCanceledException) when (_applicationLifetime.ApplicationStopping.IsCancellationRequested)
            {
                _logger.LogInformation("Stopped listening for relay commands.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected relay failure.");
            }
        }
    }
}
