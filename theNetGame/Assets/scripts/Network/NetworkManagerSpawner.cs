using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class NetworkManagerSpawner : MonoBehaviour
{
    [SerializeField] GameObject networkManagerPrefab;

    IEnumerator Start()
    {
        yield return null;

        if (NetworkManager.Singleton != null)
        {
            NetworkManagerPersist.ResetPersistence();

            Destroy(NetworkManager.Singleton.gameObject);

            yield return null;
        }

        Instantiate(networkManagerPrefab);
    }
}