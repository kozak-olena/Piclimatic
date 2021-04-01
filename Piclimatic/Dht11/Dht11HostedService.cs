using System;
using System.Threading;
using System.Threading.Tasks;

using Iot.Device.DHTxx;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Piclimatic
{
    class Dht11HostedService : IHostedService
    {
        private readonly IEventHub _eventHub;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly ILogger<Dht11HostedService> _logger;

        private Task _pollTask;

        public Dht11HostedService
        (
            IEventHub eventHub,
            IHostApplicationLifetime applicationLifetime,
            ILogger<Dht11HostedService> logger
        )
        {
            _eventHub = eventHub;
            _applicationLifetime = applicationLifetime;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _pollTask = PollDht11Continuously();

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _pollTask;
        }

        private async Task PollDht11Continuously()
        {
            try
            {
                using var dht11 = new Dht11(24);

                while (!_applicationLifetime.ApplicationStopping.IsCancellationRequested)
                {
                    var humidity = dht11.Humidity;
                    var temperature = dht11.Temperature;

                    if (dht11.IsLastReadSuccessful)
                    {
                        _logger.LogInformation($"H = {humidity}, T = {temperature}");

                        _eventHub.PostTemperatureMeasurement(new TemperatureMeasurementMessage(temperature.DegreesCelsius));

                        await Task.Delay(10000);
                    }
                    else
                    {
                        _logger.LogInformation("Read failed.");

                        await Task.Delay(2000);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected DHT11 read failure.");
            }
        }
    }
}
