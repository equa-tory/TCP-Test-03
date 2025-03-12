using Newtonsoft.Json;
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
    public static T Deserialize<T>(string data) {
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
    public static string CreateMessage(int id, string type, object message)
    {
        return Serialize(new BaseMessage(id, type, message));
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
        BaseMessage msg = Deserialize<BaseMessage>(messages[0]);
        return msg;
    }
}

public class BaseMessage
{
    public BaseMessage(int id, string type, object message) 
    {
        this.ID = id;
        this.Type = type;
        this.Data = message;
    }

    public int ID { get; set; }
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

public class ViewData
{
    public string id;
    public string path;

    public float posX;
    public float posY;
    public float posZ;
    public float rotX;
    public float rotY;
    public float rotZ;
    public float rotW;
    public float scaleX;
    public float scaleY;
    public float scaleZ;
}