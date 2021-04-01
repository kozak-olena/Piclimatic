using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Piclimatic
{
    class EventHub : IEventHub
    {
        private readonly BufferBlock<RelayControlMessage> _relayControlBufferBlock;
        private readonly BroadcastBlock<TemperatureMeasurementMessage> _temperatureChangedBroadcastBlock;

        public EventHub()
        {
            _relayControlBufferBlock = new BufferBlock<RelayControlMessage>();
            _temperatureChangedBroadcastBlock = new BroadcastBlock<TemperatureMeasurementMessage>(cloningFunction: null);
        }

        public void PostRelayControlMessage(RelayControlMessage message)
        {
            _relayControlBufferBlock.Post(message);
        }

        public Task<RelayControlMessage> ReceiveRelayCommand(CancellationToken cancellationToken)
        {
            return _relayControlBufferBlock.ReceiveAsync(cancellationToken);
        }

        public void PostTemperatureMeasurement(TemperatureMeasurementMessage message)
        {
            _temperatureChangedBroadcastBlock.Post(message);
        }

        public Task<TemperatureMeasurementMessage> ReceiveTemperatureChangedEvent(CancellationToken cancellationToken)
        {
            return _temperatureChangedBroadcastBlock.ReceiveAsync(cancellationToken);
        }
    }
}
