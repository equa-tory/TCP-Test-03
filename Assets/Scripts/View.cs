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

        if(data.id.Split('_')[0] != $"{clientID}") Destroy(gameObject?.GetComponent<Rigidbody>());
    }

    private void Update()
    {
        if(client && $"{clientID}" == data.id.Split('_')[0])
        {
            data.UpdateData(transform);
            client.UDP("VIEW_UPD", this.data);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, new Vector3(data.posX, data.posY, data.posZ), 0.1f);
            transform.rotation = Quaternion.Lerp(transform.rotation, new Quaternion(data.rotX, data.rotY, data.rotZ, data.rotW), 0.1f);
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(data.scaleX, data.scaleY, data.scaleZ), 0.1f);
        }
    }

    private void OnDestroy() => Delete();

    //--------------------------------------------------------------------------------------------

    public void Delete() {
        try{client.TCP("VIEW_DEL", this.data);}catch{}
    }
}