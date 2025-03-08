using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RPC_Test : MonoBehaviour
{
    [SerializeField] private GameObject ketya;
    
    //--------------------------------------------------------------------------------------------

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Q))
            RpcHandler.Instance.RPC("Spawn", "testdata12121121");

        if(Input.GetKeyDown(KeyCode.D))
            RpcHandler.Instance.RPC("Dest");
    }

    //--------------------------------------------------------------------------------------------

    [RPC]
    public void Spawn(string data) {
        Debug.Log(data);
        if(ketya) Instantiate(ketya, ketya.transform.position, ketya.transform.rotation);
    }

    [RPC]
    public void Dest() {
        Destroy(gameObject);
    }
}
