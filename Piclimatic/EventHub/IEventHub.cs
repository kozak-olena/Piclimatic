using System.Threading;
using System.Threading.Tasks;

namespace Piclimatic
{
    interface IEventHub
    {
        void PostTemperatureMeasurement(TemperatureMeasurementMessage message);
        Task<TemperatureMeasurementMessage> ReceiveTemperatureMeasurement(CancellationToken cancellationToken);

        void PostTurnOnRequestedMessage(TurnOnRequestedMessage message);
        Task<TurnOnRequestedMessage> ReceiveTurnOnRequestedMessage(CancellationToken cancellationToken);

        void PostTurnOffRequestedMessage(TurnOffRequestedMessage message);
        Task<TurnOffRequestedMessage> ReceiveTurnOffRequestedMessage(CancellationToken cancellationToken);

        void PostTimedTurnOffNotificationMessage(TimedTurnOffNotificationMessage message);
        Task<TimedTurnOffNotificationMessage> ReceiveTimedTurnOffNotificationMessage(CancellationToken cancellationToken);
    }
}
