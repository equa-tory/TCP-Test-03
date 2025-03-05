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
    // private UdpClient udpListener;
    private Dictionary<int, TcpClient> tcpClients = new Dictionary<int, TcpClient>();
    // private Dictionary<int, UdpClient> udpClients = new Dictionary<int, UdpClient>();

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
    }

    public void Run()
    {
        Console.WriteLine("Starting server...");
        tcpListener = new TcpListener(IPAddress.Parse(ip), port);
        // udpListener = new UdpClient(port + 1);
        
        tcpListener.Start();
        Console.WriteLine($"Server ({ip}) started on ports {port} (TCP) and {port + 1} (UDP)");
        isRunning = true;
        
        Thread tcpThread = new Thread(TCPAcceptThread);
        tcpThread.Start();

        // Thread udpThread = new Thread(UDPReceiveThread);
        // udpThread.Start();
    }

    public void Stop()
    {
        isRunning = false;
        tcpListener.Stop();
        // udpListener.Close();
        Console.Clear();
    }
    #endregion
    //--------------------------------------------------------------------------------------------
    #region Accept Thread
    private void TCPAcceptThread()
    {
        while(isRunning)
        {
            TcpClient client = tcpListener.AcceptTcpClient();
            int clientId;
            lock (lockObj) { clientId = nextClientId++; tcpClients[clientId] = client; }

            Thread thread = new Thread(() => HandleClient(client, clientId));
            thread.Start();
        }
    }
    #endregion

    #region Receive Threads
    private void HandleClient(TcpClient client, int clientId) // TCP
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        
        while (client.Connected)
        {
            int bytesCount = stream.Read(buffer, 0, buffer.Length);
            if (bytesCount == 0) break; // Client disconnected
            string data = Encoding.UTF8.GetString(buffer, 0, bytesCount);
            
            // Console.WriteLine($"DATA: {data}");

            // Trim data if multiple messages in one
            string[] messages = data.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
            int brackets = 0;
            foreach(char c in messages[0]){
                if(c == '{') brackets++;
                else if(c == '}') brackets--;
            }
            if(brackets != 0) continue;
            
            // TODO: Base Message Deserialization


            // TODO: Action invoke

        }
        
        DisconnectTcpClient(client);
    }

    // private void UDPReceiveThread() // TODO: redo
    // {
    //     while (true)
    //     {
    //         IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), port + 1);
    //         byte[] data = udpListener.Receive(ref endPoint);
    //         string message = Encoding.UTF8.GetString(data);
            
    //         Console.WriteLine($"UDP Received: {message} from {endPoint}");
    //         BroadcastUDP(data, endPoint);
    //     }
    // }
    #endregion

    #region Broadcasting
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

    // private void BroadcastUDP(byte[] data, IPEndPoint sender) // TODO: redo
    // {
    //     lock (lockObj)
    //     {
    //         foreach (TcpClient client in tcpClients.Values)
    //             client.GetStream().Write(data, 0, data.Length);
    //     }
    // }
    #endregion

    #region Functions
    private void DisconnectTcpClient(TcpClient client)
    {
        try{
            if(client.Connected) 
            {
                client.Client.Shutdown(SocketShutdown.Both);
                // tcpClients.Remove(client); // TODO: Remove client
                // client.GetStream().Close();
                client.Close();
                // Console.WriteLine("[LOG] Client disconnected!");

                // TcpBroadcast(Newtonsoft.Json.JsonConvert.SerializeObject(players.ToArray())); // Send new players list
            }
        }
        catch(Exception e){
            Console.WriteLine("[ERR] TCP Disconnect: " + e);
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
        string message = JsonConvert.DeserializeObject<string>(data);
        Console.WriteLine("[LOG] " + message);
    }
    #endregion

}