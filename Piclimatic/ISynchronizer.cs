using System.Threading;
using System.Threading.Tasks;

namespace Piclimatic
{
    public interface ISynchronizer
    {
        Task<bool> WhenRelayCommandPosted(CancellationToken cancellationToken);

        void SignalRelay(bool requestedState);
    }
}
