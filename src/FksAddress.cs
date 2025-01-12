using System.Diagnostics;
using System.Net;

namespace FksNetworking;

public class FksAddress : IEquatable<FksAddress>
{
    public string Ip { get; }
    public int Port { get; }

    private IPEndPoint endPoint;

    /// <summary>
    /// Initializes new FksAddress object
    /// </summary>
    /// <param name="ip">The ip address, as string (eg. 127.0.0.1), however by passing an empty string, the address will by bound to 'Any'</param>
    /// <param name="port">The port number, must be positive or equal to 0 and less than 65535</param>
    public FksAddress(string ip, int port)
    {
        Debug.Assert(port >= 0 && port < ushort.MaxValue, $"Port number is equal to 0 or greater than {ushort.MaxValue}");

        Ip = ip;
        Port = port;

        if (ip == string.Empty || !IPAddress.TryParse(ip, out IPAddress? address))
            endPoint = new IPEndPoint(IPAddress.Any, port);
        else
            endPoint = new IPEndPoint(address, port);
    }

    public FksAddress(IPEndPoint endPoint)
    {
        Ip = endPoint.Address.MapToIPv4().ToString();
        Port = endPoint.Port;

        this.endPoint = endPoint;
    }

    public IPEndPoint ToEndPoint()
    {
        return endPoint;
    }

    public override string ToString()
    {
        return $"{Ip}:{Port}";
    }

    public static bool operator ==(FksAddress left, FksAddress right)
    {
        return left.endPoint == right.endPoint;
    }

    public static bool operator !=(FksAddress left, FksAddress right)
    {
        return left.endPoint != right.endPoint;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;

        return Equals(obj as FksAddress);
    }

    public bool Equals(FksAddress? other)
    {
        if (other is null) return false;

        return other.endPoint == endPoint;
    }

    public override int GetHashCode() => endPoint.GetHashCode();
}
