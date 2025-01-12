using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using FksNetworking.Events;

namespace FksNetworking.Udp;

public class FksUdpClient
{
    public FksAddress Address { get; }
    public uint Tickrate { get; set; } = 60;
    public bool IsConnected { get; internal set; } = false;

    // events
    public EventsManager Events { get; } = new EventsManager();
    public Action<ClientDisconnectedEventArgs>? OnDisconnected { get; set; }
    public Action<SocketError>? OnConnectionClosed { get; set; }

    private UdpClient? client;
    private Thread? listenThread;
    private Thread? heartbeatThread;
    private bool running = false;
    private bool tryingToConnect = false;

    public FksUdpClient(string ip, int port)
    {
        Address = new FksAddress(ip, port);
    }

    /// <summary>
    /// Tries to connect to remote server (blocks the main thread during the attempts)
    /// </summary>
    /// <param name="trials">The amount of times client tries to establish connection</param>
    /// <param name="trialTime">The time between each trial (default is 1000ms)</param>
    public void Connect(uint trials = 3, TimeSpan? trialTime = null)
    {
        client = new UdpClient();
        client.Connect(Address.ToEndPoint());
        running = true;

        listenThread = new Thread(new ThreadStart(() =>
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);

            while (running)
            {
                Listen(client, endPoint);
                Thread.Sleep(FksNetworkUtils.CalculateTickrateMs(Tickrate));
            }
        }))
        {
            IsBackground = false,
            Name = "FksClient"
        };
        listenThread.Start();

        heartbeatThread = new Thread(new ThreadStart(() =>
        {
            while (running)
            {
                Thread.Sleep(1000);
                Send("heartbeat", null);
            }
        }))
        {
            IsBackground = false,
            Name = "FksClientHbListener"
        };

        const string handshakeEv = "clt_handshake";
        int ms = trialTime != null ? (int)trialTime.Value.TotalMilliseconds : 1000;
        int connTries = 0;
        tryingToConnect = true;

        while (!IsConnected && connTries <= trials)
        {
            Send(handshakeEv);

            if (connTries == 0)
                Thread.Sleep(100); // first time wait shorter
            else
                Thread.Sleep(ms);

            connTries++;
        }

        if (connTries >= trials)
        {
            running = false;
        }

        tryingToConnect = false;

        heartbeatThread.Start();
    }

    public void Send(string eventName, FksNetworkWriter? writer = null)
    {
        Debug.Assert(client != null, "Cannot send data: client is not initialized");

        FksNetworkWriter finalWriter = new FksNetworkWriter();
        finalWriter.WriteString(eventName);

        if (writer != null)
            finalWriter.Append(writer);

        client.Send(finalWriter, finalWriter.GetLength());
    }

    public void Shutdown()
    {
        Send("clt_drop");
    }

    private void Listen(UdpClient clt, IPEndPoint remoteEndPoint)
    {
        byte[] received;

        try
        {
            received = clt.Receive(ref remoteEndPoint);
        }
        catch (SocketException error)
        {
            if (tryingToConnect) return;

            OnConnectionClosed?.Invoke(error.SocketErrorCode);
            listenThread?.Interrupt();
            running = false;
            received = [];
        }

        if (received.Length == 0) return;

        FksNetworkReader reader = new FksNetworkReader(received);

        string eventName = reader.ReadString();

        if (!ProcessInternalEvents(eventName, reader))
        {
            Events.Dispatch(eventName, new Events.EventArgs(reader, Address));
        }
    }

    private bool ProcessInternalEvents(string eventName, FksNetworkReader reader)
    {
        switch (eventName)
        {
            case "srv_handshake":
                {
                    IsConnected = true;

                    return true;
                }
            case "srv_drop_confirm":
                {
                    running = false;
                    listenThread?.Interrupt();
                    OnDisconnected?.Invoke(new ClientDisconnectedEventArgs(DisconnectReason.ClientShutdown, Address));
                    return true;
                }
            case "srv_drop":
                {
                    running = false;
                    listenThread?.Interrupt();
                    OnDisconnected?.Invoke(new ClientDisconnectedEventArgs(DisconnectReason.ServerShutdown, Address));
                    return true;
                }
            default: return false;
        }
    }
}
