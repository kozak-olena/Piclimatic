using System.Threading;
using System.Threading.Tasks;

namespace Piclimatic
{
    interface IEventHub
    {
        void PostRelayControlMessage(RelayControlMessage message);
        Task<RelayControlMessage> ReceiveRelayCommand(CancellationToken cancellationToken);

        void PostTemperatureMeasurement(TemperatureMeasurementMessage message);
        Task<TemperatureMeasurementMessage> ReceiveTemperatureChangedEvent(CancellationToken cancellationToken);
    }
}
