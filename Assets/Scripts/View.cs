using UnityEngine;

public class View : MonoBehaviour
{
    public ViewData data;
    private Client client;
    private int clientID;

    //--------------------------------------------------------------------------------------------

    public void Init(ViewData data)
    {
        this.data = data;
        client = Client.Instance;
        clientID = client.GetID();
    }

    private void Update()
    {
        if(client && $"{clientID}" == data.id.Split('_')[0])
        {
            data.UpdateData(transform);
            client.TCP("VIEW_UPD", this.data);
        }
        else
        {
            transform.position = new Vector3(data.posX, data.posY, data.posZ);
            transform.rotation = new Quaternion(data.rotX, data.rotY, data.rotZ, data.rotW);
            transform.localScale = new Vector3(data.scaleX, data.scaleY, data.scaleZ);
        }
    }
}