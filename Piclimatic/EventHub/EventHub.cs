using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Piclimatic
{
    class EventHub : IEventHub
    {
        private readonly BufferBlock<TurnOnRequestedMessage> _turnOnRequestBufferBlock;
        private readonly BufferBlock<TurnOffRequestedMessage> _turnOffRequestBufferBlock;
        
        private readonly BroadcastBlock<TemperatureMeasurementMessage> _temperatureChangedBroadcastBlock;

        public EventHub()
        {
            _turnOnRequestBufferBlock = new BufferBlock<TurnOnRequestedMessage>();
            _turnOffRequestBufferBlock = new BufferBlock<TurnOffRequestedMessage>();

            _temperatureChangedBroadcastBlock = new BroadcastBlock<TemperatureMeasurementMessage>(cloningFunction: null);
        }

        public void PostTemperatureMeasurement(TemperatureMeasurementMessage message)
        {
            _temperatureChangedBroadcastBlock.Post(message);
        }

        public Task<TemperatureMeasurementMessage> ReceiveTemperatureMeasurement(CancellationToken cancellationToken)
        {
            return _temperatureChangedBroadcastBlock.ReceiveAsync(cancellationToken);
        }

        public void PostTurnOnRequestedMessage(TurnOnRequestedMessage message)
        {
            _turnOnRequestBufferBlock.Post(message);
        }

        public Task<TurnOnRequestedMessage> ReceiveTurnOnRequestedMessage(CancellationToken cancellationToken)
        {
            return _turnOnRequestBufferBlock.ReceiveAsync(cancellationToken);
        }

        public void PostTurnOffRequestedMessage(TurnOffRequestedMessage message)
        {
            _turnOffRequestBufferBlock.Post(message);
        }

        public Task<TurnOffRequestedMessage> ReceiveTurnOffRequestedMessage(CancellationToken cancellationToken)
        {
            return _turnOffRequestBufferBlock.ReceiveAsync(cancellationToken);
        }
    }
}
