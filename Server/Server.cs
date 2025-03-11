using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;
using System.Net.WebSockets;
using Newtonsoft.Json;
using System.Text;

namespace Server;

public class Server
{
    #region Variables
    private bool isRunning = false;
    private string ip;
    private int port;

    private TcpListener tcpListener;
    private UdpClient udpListener;
    private Dictionary<int, TcpClient> tcpClients = new Dictionary<int, TcpClient>();
    private Dictionary<int, IPEndPoint> udpClients = new Dictionary<int, IPEndPoint>();

    private int nextClientId = 1;
    private object lockObj = new object();

    Dictionary<string, Action<string>> actions = new Dictionary<string, Action<string>>();
    Dictionary<string, ViewData> views = new Dictionary<string, ViewData>();

    #endregion
    //--------------------------------------------------------------------------------------------
    #region Main Functions
    public Server(string ip, int port)
    {
        this.ip = ip;
        this.port = port;

        Console.CancelKeyPress += (sender, args) => Stop();
        InitActions();
    }

    public void Run()
    {
        Console.WriteLine("Starting server...");
        tcpListener = new TcpListener(IPAddress.Parse(ip), port);
        udpListener = new UdpClient(port + 1);
        
        tcpListener.Start();
        Console.WriteLine($"Server ({ip}) started on ports {port} (TCP) and {port + 1} (UDP)");
        isRunning = true;
        
        Thread tcpThread = new Thread(TcpAcceptThread);
        tcpThread.Start();

        Thread udpThread = new Thread(UdpAcceptThread);
        udpThread.Start();
    }

    public void Stop()
    {
        isRunning = false;
        tcpListener?.Stop();
        // tcpListener = null;
        udpListener?.Close();
        // udpListener = null;
        Console.Clear();
    }
    #endregion
    //--------------------------------------------------------------------------------------------
    #region Threads
    private void TcpAcceptThread()
    {
        while(isRunning)
        {
            TcpClient client = tcpListener.AcceptTcpClient();
            int clientId;
            lock (lockObj) { clientId = nextClientId++; tcpClients[clientId] = client; }

            Console.WriteLine($"[TCP] Connection from {client.Client.RemoteEndPoint}");

            DirectTCP("CLID", clientId, client); // Send ID for UDP secure check

            Thread thread = new Thread(() => HandleClient(client));
            thread.Start();
        }
    }
    
    private void HandleClient(TcpClient client) // TCP
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[4096];

        try
        {
            while (client.Connected)
            {
                int byteCount = stream.Read(buffer, 0, buffer.Length);
                if (byteCount == 0)
                {
                    Console.WriteLine($"[TCP] Client {client.Client.RemoteEndPoint} disconnected.");
                    break;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, byteCount);

                // Debug recieved data
                // Console.WriteLine($"[TCP] {client.Client.RemoteEndPoint} DATA: {message}");

                BaseMessage msg = Utils.TrimData(message);
                if (actions.TryGetValue(msg.Type, out var action)) action?.Invoke(msg.Data.ToString());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERR] HandleClient: {ex.Message}");
        }
        finally
        {
            DisconnectTcpClient(client);
        }
    }
    
    private void UdpAcceptThread() // UDP
    {
        while(isRunning)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port + 1); // IP any because those are clients
            byte[] data = udpListener.Receive(ref endPoint); // have to be here or server connects to itself

            // Registration (add if need secure check)
            int clientId = -16; // Can be removed and Broadcast via recieved UDP functions
            lock (lockObj)
            {
                // if(!tcpClients.ContainsKey(id)) // secure check
                //     return;
                // else udpClients[id] = endPoint;
                if(!udpClients.ContainsValue(endPoint))
                {
                    clientId = nextClientId++;
                    udpClients[clientId] = endPoint;
                    Console.WriteLine($"[UDP] Client {endPoint} connected.");
                }
                else {
                    clientId = udpClients.FirstOrDefault(x => x.Value.Equals(endPoint)).Key;
                    // Console.WriteLine($"[UDP] Client {endPoint} already connected.");
                }
            }

            string message = Encoding.UTF8.GetString(data);

            // Debug recieved data
            // Console.WriteLine($"[UDP] {endPoint} DATA: {message}");

            // Trim data if multiple messages in one
            BaseMessage msg = Utils.TrimData(message);
            if(actions.TryGetValue(msg.Type, out var action)) action?.Invoke(msg.Data.ToString());

            // Broadcast TODO: 
            BroadcastUDP(data, clientId);
        }
    }
    #endregion

    #region Sending
    private void BroadcastTCP(string type, object message)
    {
        string data = Utils.CreateMessage(type, message);
        data += "\n";
        byte[] buffer = Encoding.UTF8.GetBytes(data);
        lock (lockObj)
        {
            foreach (TcpClient client in tcpClients.Values)
                client.GetStream().Write(buffer, 0, buffer.Length);
        }
    }

    // Send data to specific connected client
    private void DirectTCP(string type, object message, TcpClient client) // TODO
    {
        string data = Utils.CreateMessage(type, message);
        data += "\n";
        byte[] buffer = Encoding.UTF8.GetBytes(data);
        lock(tcpClients)
        {
            try{
                NetworkStream stream = client.GetStream();
                stream.Write(buffer, 0, buffer.Length);
            }
            catch{}
        }
    }
    
    private void BroadcastUDP(byte[] buffer, int senderId)
    {
        lock (lockObj)
        {
            foreach (var entry in udpClients)
            {
                // if (entry.Key != senderId)
                    udpListener.Send(buffer, buffer.Length, entry.Value);
            }
        }
    }
    private void BroadcastUDP(string type, object message, int senderId = -16)
    {
        string data = Utils.CreateMessage(type, message);
        data += "\n";
        byte[] buffer = Encoding.UTF8.GetBytes(data);
        lock (lockObj)
        {
            foreach (var entry in udpClients)
            {
                // if (entry.Key != senderId)
                    udpListener.Send(buffer, buffer.Length, entry.Value);
            }
        }
    }

    // private void DirectUDP

    #endregion

    #region Functions
    private void DisconnectTcpClient(TcpClient client)
    {
        if (client == null || !client.Connected) return;

        int clientId;
        lock (lockObj)
        {
            clientId = tcpClients.FirstOrDefault(x => x.Value.Equals(client)).Key;
            if (clientId == 0)
            {
                Console.WriteLine($"[TCP] Attempted to disconnect unknown client.");
                return;
            }
            tcpClients.Remove(clientId);
        }

        // Console.WriteLine($"[TCP] Client {client.Client.RemoteEndPoint} (ID: {clientId}) disconnecting...");

        try
        {
            client.Client.Shutdown(SocketShutdown.Both);
            client.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ERR] Error disconnecting client {client.Client.RemoteEndPoint}: {e.Message}");
        }

        // Console.WriteLine($"[TCP] Client {client.Client.RemoteEndPoint} (ID: {clientId}) disconnected.");
    }

    private void DisconnectUdpClient(IPEndPoint clientEndPoint)
    {
        lock (lockObj) // Ensure thread safety
        {
            if (udpClients.ContainsValue(clientEndPoint))
            {
                int keyToRemove = udpClients.FirstOrDefault(x => x.Value.Equals(clientEndPoint)).Key;
                udpClients.Remove(keyToRemove);
                Console.WriteLine($"[UDP] Client {clientEndPoint} disconnected.");
            }
            else
            {
                Console.WriteLine($"[UDP] Attempted to disconnect non-existent client: {clientEndPoint}");
            }
        }
    }

    private void InitActions()
    {
        actions = new Dictionary<string, Action<string>>
        {
            { "LOG", Log },
            { "RPC", RPC },
            { "VIEW_UPD", ViewUpdate },
        };
    }
    
    private void Log(string data)
    {
        var obj = Utils.Deserialize<Log>(data);
        Console.WriteLine($"[LOG] {obj.message}");
    }
    
    private void RPC(string data)
    {
        var obj = Utils.Deserialize<RPC>(data);
        Console.WriteLine($"[RPC] {obj.methodName} {obj.data}");
        BroadcastTCP("RPC", new RPC(obj.methodName, obj.data));
    }
 
    private void ViewUpdate(string data)
    {
        var obj = Utils.Deserialize<ViewData>(data);
        views[obj.id] = obj;
        // Console.WriteLine($"vsc: {views.Count}");
        // Console.WriteLine($"----");
        // foreach(var v in views) Console.WriteLine($"View: {v.Key} {v.Value.posX}");
        BroadcastUDP("VIEWS_UPD", views);
    }
    #endregion

}