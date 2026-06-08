using UnityEngine;
using Unity.Netcode;

public class NetworkManagerSpawner : MonoBehaviour
{
    [SerializeField] GameObject networkManagerPrefab;

    
    
        
 
    public void Start()
    {
        

        if (NetworkManager.Singleton != null)
        {
            NetworkManagerPersist.ResetPersistence();

            Destroy(NetworkManager.Singleton.gameObject);
        }

        Instantiate(networkManagerPrefab);
    }


}