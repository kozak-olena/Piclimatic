using System;
using System.Device.Gpio;

using Microsoft.Extensions.Logging;

namespace Piclimatic
{
    class RelayService : IRelayService, IDisposable
    {
        private readonly ILogger<RelayService> _logger;

        private readonly int _pin;
        private readonly GpioController _gpioController;

        private bool _state = false;

        public RelayService(ILogger<RelayService> logger)
        {
            _pin = 4;

            _gpioController = new GpioController();
            _gpioController.OpenPin(_pin, PinMode.Output);
            _gpioController.Write(_pin, PinValue.High);

            _logger = logger;
        }

        public void TurnOff()
        {
            if (_state is true)
            {
                _logger.LogInformation($"Engaging relay");

                _gpioController.Write(_pin, PinValue.High);

                _state = false;
            }
        }

        public void TurnOn()
        {
            if (_state is false)
            {
                _logger.LogInformation($"Disengaging relay");

                _gpioController.Write(_pin, PinValue.Low);

                _state = true;
            }
        }

        public void Dispose()
        {
            _gpioController.Dispose();
        }
    }
}
