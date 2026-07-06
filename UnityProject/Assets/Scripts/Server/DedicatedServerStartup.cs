using Mirror;
using UnityEngine;

public class DedicatedServerStartup : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Starting game component");
        if (Application.isBatchMode)
        {
            Debug.Log("Starting dedicated server...");
            NetworkManager.singleton.StartServer();
        }
    }
}
