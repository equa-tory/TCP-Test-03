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

    Dictionary<string, Action<string>> actions = new Dictionary<string, Action<string>>();

    private int nextClientId = 1;
    private object lockObj = new object();

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
        tcpListener.Stop();
        udpListener.Close();
        Console.Clear();
    }
    #endregion
    //--------------------------------------------------------------------------------------------
    #region Accept Threads
    private void TcpAcceptThread()
    {
        while(isRunning)
        {
            TcpClient client = tcpListener.AcceptTcpClient();
            int clientId;
            lock (lockObj) { clientId = nextClientId++; tcpClients[clientId] = client; }

            Console.WriteLine($"[TCP] Connection from {client.Client.RemoteEndPoint}");

            // DirectTcp(Utils.CreateMessage("ID", clientId), client);

            Thread thread = new Thread(() => HandleClient(client));
            thread.Start();
        }
    }
    
    private void UdpAcceptThread()
    {
        while(isRunning)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 7778);

            // Registration (add if need secure check)
            int clientId = -16;
            lock (lockObj)
            {
                // if(!tcpClients.ContainsKey(id)) // id
                //     return;
                // else udpClients[id] = endPoint;
                if(!udpClients.ContainsValue(endPoint))
                {
                    clientId = nextClientId++;
                    udpClients[clientId] = endPoint;
                }
                else {
                    clientId = udpClients.FirstOrDefault(x => x.Value.Equals(endPoint)).Key;
                }
                
            }

            byte[] data = udpListener.Receive(ref endPoint);
            string message = Encoding.UTF8.GetString(data);
            BroadcastUDP(data, clientId);

            // Debug recieved data check
            // Console.WriteLine($"[UDP] {endPoint} DATA: {message}");

            // Trim data if multiple messages in one
            BaseMessage msg = Utils.TrimData(message);
            actions.TryGetValue(msg.Type, out var action);
            action?.Invoke(msg.Data.ToString());
            
        }
    }
    #endregion

    #region Receive Threads
    private void HandleClient(TcpClient client) // TCP
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        while (client.Connected)
        {
            int byteCount = stream.Read(buffer, 0, buffer.Length);
            if (byteCount == 0) continue;
            string data = Encoding.UTF8.GetString(buffer, 0, byteCount);
 
            // Debug recieved data check
            // Debug.Log($"[TCP] {data}");

            // Trim data if multiple messages in one
            BaseMessage msg = Utils.TrimData(data);
            actions.TryGetValue(msg.Type, out var action);
            action?.Invoke(msg.Data.ToString());
        }
        
        DisconnectTcpClient(client);
    }
    #endregion

    #region Sending
    private void BroadcastTCP(string message)
    {
        message += "\n";
        byte[] data = Encoding.UTF8.GetBytes(message);
        lock (lockObj)
        {
            foreach (TcpClient client in tcpClients.Values)
                client.GetStream().Write(data, 0, data.Length);
        }
    }

    // Send data to specific connected client
    private void DirectTcp(string data, TcpClient client)
    {
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
    
    // TODO: UDP
    private void BroadcastUDP(byte[] data, int senderId)
    {
        lock (lockObj)
        {
            foreach (var entry in udpClients)
            {
                // if (entry.Key != senderId)
                    udpListener.Send(data, data.Length, entry.Value);
            }
        }
    }
    #endregion

    #region Functions
    private void DisconnectTcpClient(TcpClient client)
    {
        try{
            if(client.Connected) 
            {
                client.Client.Shutdown(SocketShutdown.Both);

                var keyToRemove = tcpClients.FirstOrDefault(x => x.Value.Equals(client)).Key;
                tcpClients.Remove(keyToRemove); // TODO: Remove client

                // client.GetStream().Close();
                Console.WriteLine($"[LOG] Client {client.Client.RemoteEndPoint} disconnected!");
                client.Close();
            }
        }
        catch(Exception e){
            Console.WriteLine("[ERR] TCP Disconnect: " + e);
        }
    }
    private void DisconnectUdpClient(IPEndPoint clientEndPoint)
    {
        int keyToRemove = udpClients.FirstOrDefault(x => x.Value.Equals(clientEndPoint)).Key;
        if (udpClients.Remove(keyToRemove))
        {
            Console.WriteLine($"Client {clientEndPoint} disconnected.");
        }
    }

    private void InitActions()
    {
        actions = new Dictionary<string, Action<string>>
        {
            { "LOG", Log },
            // { "CONNECT", Connect },
            // { "DISCONNECT", Disconnect },
            // { "UPDATEPLAYER", UpdatePlayer },
            // { "RPC", Rpc }
        };
    }
    
    private void Log(string data)
    {
        Log obj = Utils.Desirialize<Log>(data);
        Console.WriteLine($"[LOG] {obj.message}");
    }
    #endregion

}