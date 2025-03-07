using Newtonsoft.Json;
using System.Collections.Generic;
using System;

#if UNITY
namespace Server;
#endif

public static class Utils
{
    public static string Serialize<T>(T obj) {
        try
        {
            return JsonConvert.SerializeObject(obj);
        }
        catch (Exception ex) 
        {
            Console.WriteLine($"Serialization err: {ex.Message}");
            return string.Empty;
        }
    }
    public static T Desirialize<T>(string data) {
        try
        {
            return JsonConvert.DeserializeObject<T>(data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Deserialization error: {ex.Message}");
            return default(T);
        }
    }
    public static string CreateMessage(string type, object message)
    {
        return Serialize(new BaseMessage(type, message));
    }

    public static BaseMessage TrimData(string data)
    {
        string[] messages = data.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        int brackets = 0;
        foreach(char i in messages[0]){
            if(i == '{') brackets++;
            else if(i == '}') brackets--;
        }
        if(brackets != 0) return null;
        BaseMessage msg = Utils.Desirialize<BaseMessage>(messages[0]);
        return msg;
    }
}

public class BaseMessage
{
    public BaseMessage(string type, object message) 
    {
        this.Type = type;
        this.Data = message;
    }

    public string Type { get; set; }
    public object Data { get; set; }
}

//--------------------------------------------------------------------------------------------

public class Log
{
    public Log(string message)
    {
        this.message = message;
    }

    public string message { get; set; }
}

// public class PlayerData
// {
//     public PlayerData(int id, string name, float X=0, float Y=0, float Z=0, float rotX=0, float rotY=0, float rotZ=0)
//     {
//         this.id = id;
//         this.name = name;
//         this.X = X;
//         this.Y = Y;
//         this.Z = Z;
//         this.rotX = rotX;
//         this.rotY = rotY;
//         this.rotZ = rotZ;
//     }

//     public int id { get; set; }
//     public string name { get; set; }
//     public float X { get; set; }
//     public float Y { get; set; }
//     public float Z { get; set; }
//     public float rotX { get; set; }
//     public float rotY { get; set; }
//     public float rotZ { get; set; }
// }

public class RPC
{
    public RPC(string methodName, string data = null)
    {
        this.methodName = methodName;
        this.data = data;
    }

    public string methodName { get; set; }
    public string data { get; set; }
}