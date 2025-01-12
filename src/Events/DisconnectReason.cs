namespace FksNetworking.Events;

public enum DisconnectReason
{
    Timeout = 0,
    ServerForced,
    ClientForced,
    ServerShutdown,
    ClientShutdown
}
