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
            RpcHandler.Instance.RPC("Test", "testdata12121121");
    }

    //--------------------------------------------------------------------------------------------

    [RPC]
    public void Test(string data) {
        Debug.Log(data);
        if(ketya) Instantiate(ketya, ketya.transform.position, ketya.transform.rotation);
    }
}
