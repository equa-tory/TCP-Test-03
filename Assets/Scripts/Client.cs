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
    private int connectionTimeout = 1000;

    // private int clientId = -16;

    private TcpClient tcpClient;
    private NetworkStream stream;
    
    private UdpClient udpClient;

    Dictionary<string, System.Action<string>> actions = new Dictionary<string, System.Action<string>>();
    #endregion
    //--------------------------------------------------------------------------------------------
    #region Main Functions
    private void Awake() => Init(); // TODO: via bootstrap
    void Update() {
        if(Input.GetKeyDown(KeyCode.T))
        {
            UDP(Utils.CreateMessage("LOG", new Log("dwadawd")));
        }
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
        tcpClient = new TcpClient();
        try
        {
            var cl = tcpClient.ConnectAsync(ip, port);
            if(await Task.WhenAny(cl, Task.Delay(connectionTimeout)) == cl)
            {
                stream = tcpClient.GetStream();
                isConnected = true;
                Print($"[LOG] Connected to server {ip}:{port}!");

                Task t = new Task(ReceiveTCP); // TODO: revieve TCP
                t.Start();

                udpClient = new UdpClient();
                udpClient.Connect(IPAddress.Parse(ip), port + 1);
                udpClient.BeginReceive(new AsyncCallback(ReceiveUDP), null);
                return true;
            }
            else {
                Print($"[ERR] Can't connect to server {ip}:{port}");
                return false;
            }
        }
        catch(SocketException e)
        {
            Print("[ERR] TCP Connect: " + e);
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
    private void TcpDisconnect(){
        // TCP(Utils.CreateMessage("disconnect", localPlayer));
        try{
            tcpClient.Client.Shutdown(SocketShutdown.Both);
            stream?.Close();
            tcpClient?.Close();
        }catch(SocketException e){
            Debug.LogError($"[ERR] TCP Disconnect: {e}");
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
 
            // Debug recieved data check
            // Debug.Log($"[Server] {data}");

            // Trim data if multiple messages in one
            BaseMessage msg = Utils.TrimData(data);

            actions.TryGetValue(msg.Type, out var action);
            action?.Invoke(msg.Data.ToString());
        }
    }
    
    public void TCP(string message)
    {
        message += "\n";
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        stream.Write(buffer, 0, buffer.Length);
    }
    #endregion

    #region UDP
    private void ReceiveUDP(IAsyncResult ar)
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(ip), port + 1);
        byte[] data = udpClient.EndReceive(ar, ref remoteEP);
        string message = Encoding.UTF8.GetString(data);
        //--------------------------------------------------------------------------------------------

        // Debug.Log("[SERVER] UDP: " + data);
        // playersData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlayerData>>(message); // Update players data

        // Trim data if multiple messages in one
        BaseMessage msg = Utils.TrimData(message);
        actions.TryGetValue(msg.Type, out var action);
        action?.Invoke(msg.Data.ToString());

        //--------------------------------------------------------------------------------------------
        // Снова начинаем асинхронное получение данных
        udpClient.BeginReceive(new AsyncCallback(ReceiveUDP), null);
    }

    private void UDP(string message)
    {
        message += "\n";
        byte[] data = Encoding.UTF8.GetBytes(message);
        udpClient.Send(data, data.Length);
    }
    #endregion

    #region Functions
    private void Print(string message)
    {
        Debug.Log(message);
    }

    private void InitActions() {
        actions = new Dictionary<string, System.Action<string>>
        {
            { "LOG", Log },
            // { "UPDATEPLAYERSLIST", UpdatePlayersList },
            // { "CONNECTPLAYER", ConnectPlayer },
            // { "DISCONNECTPLAYER", DisconnectPlayer },
            // { "RPC", Rpc }
        };
    }
    //--------------------------------------------------------------------------------------------
    private void Log(string data)
    {
        Log obj = Utils.Desirialize<Log>(data);
        Print($"[LOG] {obj.message}");
    }

    #endregion
}
