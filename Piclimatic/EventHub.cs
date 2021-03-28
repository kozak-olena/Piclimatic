using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Piclimatic
{
    class EventHub : IEventHub
    {
        private readonly BufferBlock<RelayControlMessage> _relayControlBufferBlock;
        private readonly BufferBlock<TemperatureChangedMessage> _temperatureChangedBufferBlock;

        public EventHub()
        {
            _relayControlBufferBlock = new BufferBlock<RelayControlMessage>();
            _temperatureChangedBufferBlock = new BufferBlock<TemperatureChangedMessage>();
        }

        public void PostRelayControlMessage(RelayControlMessage message)
        {
            _relayControlBufferBlock.Post(message);
        }

        public Task<RelayControlMessage> ReceiveRelayCommand(CancellationToken cancellationToken)
        {
            return _relayControlBufferBlock.ReceiveAsync(cancellationToken);
        }

        public void PostTemperatureChangedMessage(TemperatureChangedMessage message)
        {
            _temperatureChangedBufferBlock.Post(message);
        }

        public Task<TemperatureChangedMessage> ReceiveTemperatureChangedEvent(CancellationToken cancellationToken)
        {
            return _temperatureChangedBufferBlock.ReceiveAsync(cancellationToken);
        }
    }
}
