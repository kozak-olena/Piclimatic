namespace Piclimatic
{
    class RelayControlMessage
    {
        public RelayControlMessage(bool requestedState)
        {
            RequestedState = requestedState;
        }

        public bool RequestedState { get; }
    }
}
