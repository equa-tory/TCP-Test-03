using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Client : MonoBehaviour
{
    #region Variables
    public static Client Instance;

    [SerializeField] private string ip = "127.0.0.1";
    [SerializeField] private int port = 3108;

    private bool isConnected = false;
    private int connectionTimeout = 10000;

    // private int clientId = -16;

    private UdpClient udpClient;
    private TcpClient tcpClient;
    private NetworkStream stream;

    Dictionary<string, System.Action<string>> actions = new Dictionary<string, System.Action<string>>();
    #endregion
    //--------------------------------------------------------------------------------------------
    #region Main Functions
    private void Awake() => Init(); // TODO: via bootstrap
    void Update() {
        if(Input.GetKeyDown(KeyCode.T))
            TCP("LOG", new Log("tcp test"));
        if(Input.GetKey(KeyCode.U))
            UDP("LOG", new Log("udp test")); 
    }
    public async void Init()
    {
        if(Instance == null) Instance = this;
        else Destroy(this.gameObject);
        DontDestroyOnLoad(this.gameObject);
        
        InitActions();
        await ConnectToServer();
    }
    private void OnApplicationQuit() => Disconnect();

    private async Task<bool> ConnectToServer()
    {
        // Tmp client for timeout
        tcpClient = new TcpClient();
        try
        {
            var cl = tcpClient.ConnectAsync(ip, port);
            if(await Task.WhenAny(cl, Task.Delay(connectionTimeout)) == cl)
            {
                stream = tcpClient.GetStream();
                isConnected = true;
                Debug.Log($"Connected to server {ip}:{port}!");

                // TCP start recieve
                Task t = new Task(ReceiveTCP);
                t.Start();

                // UDP start recieve
                udpClient = new UdpClient();
                udpClient.Connect(IPAddress.Parse(ip), port + 1);
                udpClient.BeginReceive(new AsyncCallback(ReceiveUDP), null);
                UDP("Login", " "); // Blank msg for udp login on server otherwise client could not recieve UDP
                return true;
            }
            else {
                Debug.LogError($"[ERR] Timeout connecting to server {ip}:{port}");
                return false;
            }
        }
        catch(SocketException e)
        {
            Debug.LogError("[ERR] TCP Connect: " + e);
            return false;
        }
    }

    public void Disconnect(){
        if (tcpClient != null && isConnected)
        {
            isConnected = false;
            TcpDisconnect();
            // TODO: UdpDisconnect();
        }
    }
    #endregion
    //--------------------------------------------------------------------------------------------
    #region TCP
    private void ReceiveTCP()
    {
        byte[] buffer = new byte[1024];
        while (isConnected)
        {
            int byteCount = stream.Read(buffer, 0, buffer.Length);
            if (byteCount == 0) continue;
            string data = Encoding.UTF8.GetString(buffer, 0, byteCount);
 
            // Debug recieved data
            // Debug.Log($"[TCP] {data}");

            // Trim data if multiple messages in one
            BaseMessage msg = Utils.TrimData(data);
            if(actions.TryGetValue(msg.Type, out var action)) action?.Invoke(msg.Data.ToString());
        }
    }

    private void TcpDisconnect()
    {
        // TCP("disconnect", localPlayer);
        try{
            tcpClient.Client.Shutdown(SocketShutdown.Both);
            stream?.Close();
            tcpClient?.Close();
        }catch(SocketException e){
            Debug.LogError($"[ERR] TCP Disconnect: {e}");
        }
    }
    
    public void TCP(string type, object message)
    {
        string data = Utils.CreateMessage(type, message);
        data += "\n";
        byte[] buffer = Encoding.UTF8.GetBytes(data);
        stream.Write(buffer, 0, buffer.Length);
    }
    #endregion

    #region UDP
    private void ReceiveUDP(IAsyncResult ar)
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(ip), port + 1);
        byte[] data = udpClient.EndReceive(ar, ref remoteEP);
        string message = Encoding.UTF8.GetString(data);

        // Debug recieved data
        // Debug.Log($"[UDP] {data}");

        // Trim data if multiple messages in one
        BaseMessage msg = Utils.TrimData(message);
        if(actions.TryGetValue(msg.Type, out var action)) action?.Invoke(msg.Data.ToString());

        // Start recieveng data again
        udpClient.BeginReceive(new AsyncCallback(ReceiveUDP), null);
    }

    private void UdpDisconnect()
    {
        // Udp("disconnect", localPlayer);
        try{
            udpClient.Client.Shutdown(SocketShutdown.Both);
            udpClient?.Close();
        }catch(SocketException e){
            Debug.LogError($"[ERR] UDP Disconnect: {e}");
        }
    }

    private void UDP(string type, object message)
    {
        string data = Utils.CreateMessage(type, message);
        data += "\n";
        byte[] buffer = Encoding.UTF8.GetBytes(data);
        udpClient.Send(buffer, buffer.Length);
    }
    #endregion

    #region Functions
    private void InitActions()
    {
        actions = new Dictionary<string, Action<string>>
        {
            { "LOG", Log },
        };
    }

    private void Log(string data)
    {
        var obj = Utils.Deserialize<Log>(data);
        Debug.LogError($"[LOG] {obj.message}"); // Error for show up in dev build ver
    }

    #endregion
}
