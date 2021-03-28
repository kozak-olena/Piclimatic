using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Piclimatic
{
    public class Synchronizer : ISynchronizer
    {
        private readonly BufferBlock<bool> _relayControlBufferBlock;

        public Synchronizer()
        {
            _relayControlBufferBlock = new BufferBlock<bool>();
        }

        public void SignalRelay(bool requestedState)
        {
            _relayControlBufferBlock.Post(requestedState);
        }

        public Task<bool> WhenRelayCommandPosted => _relayControlBufferBlock.ReceiveAsync();
    }
}
