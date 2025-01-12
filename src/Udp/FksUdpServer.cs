using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using FksNetworking.Events;

namespace FksNetworking.Udp;

public class FksUdpServer
{
    public FksAddress Address { get; }
    public uint Tickrate { get; set; } = 60;
    public uint TimeoutTime { get; set; } = 10;

    // events
    public EventsManager Events { get; } = new EventsManager();
    public Action<FksAddress>? OnClientConnected { get; set; }
    public Action<ClientDisconnectedEventArgs>? OnClientDisconnected { get; set; }

    private UdpClient? server;
    private Thread? listenThread;
    private Thread? heartbeatThread;
    private bool running = false;
    private ConcurrentDictionary<IPEndPoint, DateTime> clients = new ConcurrentDictionary<IPEndPoint, DateTime>();

    public FksUdpServer(int port)
    {
        Address = new FksAddress(string.Empty, port);
    }

    public void Start()
    {
        server = new UdpClient(Address.Port);
        running = true;

        listenThread = new Thread(new ThreadStart(() =>
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
            while (running)
            {
                Listen(server, endPoint);
                Thread.Sleep(FksNetworkUtils.CalculateTickrateMs(Tickrate));
            }
        }))
        {
            IsBackground = false,
            Name = "FksServer"
        };
        listenThread.Start();

        heartbeatThread = new Thread(new ThreadStart(() =>
        {
            while (running)
            {
                Thread.Sleep(1000);
                UpdateHeartbeats();
            }
        }))
        {
            IsBackground = false,
            Name = "FksServerHbListener"
        };
        heartbeatThread.Start();
    }

    public void Send(string eventName, FksAddress address, FksNetworkWriter? writer = null)
    {
        Debug.Assert(server != null, "Cannot send data to client " + address.ToString() + ": server is not initialized");

        FksNetworkWriter finalWriter = new FksNetworkWriter();

        finalWriter.WriteString(eventName);
        if (writer != null)
            finalWriter.Append(writer);

        server.Send(finalWriter, finalWriter.GetLength(), address.ToEndPoint());
    }

    public void DisconnectClient(FksAddress address)
    {
        Debug.Assert(server != null, "Cannot disconnect client " + address.ToString() + ": server is not initialized");

        Send("srv_drop", address);
        OnClientDisconnected?.Invoke(new ClientDisconnectedEventArgs(DisconnectReason.ServerForced, address));
    }

    public void Shutdown()
    {
        Debug.Assert(server != null, "Cannot shutdown: server is not initialized");

        foreach (var client in clients)
        {
            var address = new FksAddress(client.Key);

            Send("srv_drop", address);
            OnClientDisconnected?.Invoke(new ClientDisconnectedEventArgs(DisconnectReason.ServerShutdown, address));
        }

        running = false;
        listenThread?.Interrupt();
        heartbeatThread?.Interrupt();
    }

    private void Listen(UdpClient srv, IPEndPoint remoteEndPoint)
    {
        byte[] received = srv.Receive(ref remoteEndPoint);

        FksNetworkReader reader = new FksNetworkReader(received);
        string eventName = reader.ReadString();
        FksAddress address = new FksAddress(remoteEndPoint);

        if (!ProcessInternalEvents(eventName, address, reader))
            Events.Dispatch(eventName, new Events.EventArgs(reader, address));
    }

    private void UpdateHeartbeats()
    {
        if (clients.IsEmpty) return;

        DateTime now = DateTime.UtcNow;
        foreach (var client in clients)
        {
            if ((now - client.Value).TotalSeconds > TimeoutTime)
            {
                OnClientDisconnected?.Invoke(new ClientDisconnectedEventArgs(DisconnectReason.Timeout, new FksAddress(client.Key)));
                clients.TryRemove(client);
            }
        }
    }

    private bool ProcessInternalEvents(string evName, FksAddress address, FksNetworkReader reader)
    {
        switch (evName)
        {
            case "clt_handshake":
                {
                    OnClientConnected?.Invoke(address);
                    clients.TryAdd(address.ToEndPoint(), DateTime.UtcNow);
                    Send("srv_handshake", address);
                    return true;
                }
            case "heartbeat":
                {
                    var endPoint = address.ToEndPoint();

                    if (clients.ContainsKey(endPoint))
                    {
                        clients[endPoint] = DateTime.UtcNow;
                    }

                    return true;
                }
            case "clt_drop":
                {
                    var endPoint = address.ToEndPoint();

                    OnClientDisconnected?.Invoke(new ClientDisconnectedEventArgs(DisconnectReason.ClientShutdown, address));

                    clients.TryRemove(endPoint, out _);
                    Send("srv_drop_confirm", address);

                    return true;
                }
            default: return false;
        }
    }
}
