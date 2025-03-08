using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[System.AttributeUsage(System.AttributeTargets.Method)]
public class RPCAttribute : System.Attribute
{
    public string MethodName { get; }

    public RPCAttribute(string methodName = null)
    {
        MethodName = methodName;
    }
}

public class RpcHandler : MonoBehaviour
{
    public static RpcHandler Instance;
    private readonly Queue<System.Action> mainThreadActions = new Queue<System.Action>();
    
    private void Awake() {
        if(Instance == null) Instance = this;
        else Destroy(this.gameObject);
    }
    private void Update() {
        lock (mainThreadActions) {
            while (mainThreadActions.Count > 0) {
                mainThreadActions.Dequeue()?.Invoke();
            }
        }
    }

    public void InvokeRPC(RPC rpc)
    {
        lock (mainThreadActions) mainThreadActions.Enqueue(() =>
        {
            MonoBehaviour[] mbs = FindObjectsOfType<MonoBehaviour>();

            for(int i=0;i<mbs.Length;i++) {
                
                MethodInfo method = mbs[i].GetType().GetMethod(rpc.methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (method != null && method.GetCustomAttribute<RPCAttribute>() != null)
                {
                    var ps = method.GetParameters();

                    // if method has no parameters
                    if (ps.Length == 0) method.Invoke(mbs[i], null);
                    else
                    {
                        var pType = ps[0].ParameterType;

                        // if data is null
                        if (rpc.data == null)
                        {
                            method.Invoke(mbs[i], new object[] { null });
                        }
                        // if data is string
                        else if (pType == typeof(string))
                        {
                            method.Invoke(mbs[i], new[] { rpc.data.Trim('\"') });
                        }
                        else
                        {
                            method.Invoke(mbs[i], new[] { JsonUtility.FromJson(rpc.data, pType) });
                        }
                    }
                    break;
                }
                if(i == mbs.Length-1) {
                    Debug.LogError($"[RPC] Method '{rpc.methodName}' not found or not marked with [RPC].");
                }
            }
        });
    }

    public void RPC(string methodName, object data = null)
    {
        string param = data is string strData ? $"\"{strData}\"" : JsonUtility.ToJson(data);
        Client.Instance.TCP("RPC", new RPC(methodName, param));
    }
}