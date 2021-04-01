using System;

namespace Piclimatic
{
    class TurnOnRequestedMessage
    {
        private readonly int? _desiredTemperature;
        private readonly TimeSpan? _desiredDuration;

        private TurnOnRequestedMessage(OperationalMode requestedOperationalMode, int? desiredTemperature, TimeSpan? desiredDuration)
        {
            RequestedOperationalMode = requestedOperationalMode;

            _desiredDuration = desiredDuration;
            _desiredTemperature = desiredTemperature;
        }

        public OperationalMode RequestedOperationalMode { get; }

        public int DesiredTemperature => _desiredTemperature.Value;
        public TimeSpan DesiredDuration => _desiredDuration.Value;

        public static TurnOnRequestedMessage CreateThermostatTurnOnRequestedMessage(int desiredTemperature)
        {
            return new TurnOnRequestedMessage(OperationalMode.Thermostat, desiredTemperature, null);
        }

        public static TurnOnRequestedMessage CreateTimedTurnOnRequestedMessage(TimeSpan desiredDuration)
        {
            return new TurnOnRequestedMessage(OperationalMode.Timed, null, desiredDuration);
        }
    }
}
