using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Piclimatic
{
    class ControlHostedService : IHostedService
    {
        private readonly IEventHub _eventHub;
        private readonly IRelayService _relayService;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly ILogger<ControlHostedService> _logger;

        private Task _mainLoopTask;

        public ControlHostedService
        (
            IEventHub eventHub,
            IRelayService relayService,
            IHostApplicationLifetime applicationLifetime,
            ILogger<ControlHostedService> logger
        )
        {
            _eventHub = eventHub;
            _relayService = relayService;
            _applicationLifetime = applicationLifetime;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _mainLoopTask = MainLoop();

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _mainLoopTask;
        }

        private async Task MainLoop()
        {
            var cancellationToken = _applicationLifetime.ApplicationStopping;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var turnOnRequest = await _eventHub.ReceiveTurnOnRequestedMessage(cancellationToken);

                    if (turnOnRequest.RequestedOperationalMode == OperationalMode.Timed)
                    {
                        _relayService.TurnOn();

                        CancellationTokenSource turnOffRequestAwaitCancellationTokenSource = new CancellationTokenSource();

                        var turnOffRequestTask = 
                            _eventHub.ReceiveTurnOffRequestedMessage
                            (
                                CancellationTokenSource.CreateLinkedTokenSource
                                (
                                    cancellationToken, 
                                    turnOffRequestAwaitCancellationTokenSource.Token
                                ).Token
                            );

                        var desiredTurnOnDurationElapsedTask = Task.Delay(turnOnRequest.DesiredDuration, cancellationToken);

                        var completedTask = await Task.WhenAny(turnOffRequestTask, desiredTurnOnDurationElapsedTask);

                        _relayService.TurnOff();

                        if (completedTask == desiredTurnOnDurationElapsedTask)
                        {
                            turnOffRequestAwaitCancellationTokenSource.Cancel();

                            _eventHub.PostTimedTurnOffNotificationMessage(new TimedTurnOffNotificationMessage());
                        }
                    }

                    if (turnOnRequest.RequestedOperationalMode == OperationalMode.Thermostat)
                    {
                        var turnOffTask = _eventHub.ReceiveTurnOffRequestedMessage(cancellationToken);

                        while (!turnOffTask.IsCompleted)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

                            var temperatureMeasurement = await _eventHub.ReceiveTemperatureMeasurement(cancellationToken);

                            if (turnOnRequest.DesiredTemperature < temperatureMeasurement.Temperature)
                            {
                                _relayService.TurnOn();
                            }
                            else
                            {
                                _relayService.TurnOff();
                            }
                        }

                        _relayService.TurnOff();
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Stopped listening for events.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected control failure.");
            }
        }
    }
}
