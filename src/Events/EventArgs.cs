namespace FksNetworking.Events;

public class EventArgs
{
    public FksNetworkReader Reader { get; }
    public FksAddress Address { get; }

    public EventArgs(FksNetworkReader reader, FksAddress address)
    {
        Reader = reader;
        Address = address;
    }
}

public class ClientDisconnectedEventArgs
{
    public DisconnectReason Reason { get; }
    public FksAddress Address { get; }

    public ClientDisconnectedEventArgs(DisconnectReason reason, FksAddress address)
    {
        Reason = reason;
        Address = address;
    }
}
