using System.Threading.Tasks;

namespace Piclimatic
{
    public interface ISynchronizer
    {
        Task<bool> WhenRelayCommandPosted { get; }

        void SignalRelay(bool requestedState);
    }
}
