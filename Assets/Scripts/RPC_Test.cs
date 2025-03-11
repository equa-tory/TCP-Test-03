using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RPC_Test : MonoBehaviour
{
    [SerializeField] private View ketya;
    
    //--------------------------------------------------------------------------------------------

    void Update()
    {
        // if(Input.GetKeyDown(KeyCode.Q))
        //     RpcHandler.Instance.RPC("Spawn");

        if(Input.GetKeyDown(KeyCode.D))
            RpcHandler.Instance.RPC("Dest");

        if(Input.GetKeyDown(KeyCode.W))
            Client.Instance.Spawn("Ketya", Vector3.zero, Quaternion.identity);
    }

    //--------------------------------------------------------------------------------------------

    // [RPC]
    // public void Spawn() {
    //     if(ketya) Instantiate(ketya, ketya.transform.position, ketya.transform.rotation).Init();
    // }

    [RPC]
    public void Dest() {
        Destroy(gameObject);
    }
}
