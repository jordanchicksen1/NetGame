using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] GameObject playerPrefab;
    [SerializeField] Transform[] spawnPoints;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // Listen for scene load completion
        NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
    }

    void OnSceneLoaded(string sceneName, LoadSceneMode mode, System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut)
    {
        if (!IsServer) return;

        Debug.Log("All clients loaded scene → spawning players");

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            SpawnPlayerIfNeeded(client.ClientId);
        }
    }

    void SpawnPlayerIfNeeded(ulong clientId)
    {
        var client = NetworkManager.Singleton.ConnectedClients[clientId];

        if (client.PlayerObject != null)
            return;

        SpawnPlayer(clientId);
    }

    void SpawnPlayer(ulong clientId)
    {
        Vector3 spawnPos = Vector3.zero;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int index = (int)clientId;

            if (index >= spawnPoints.Length)
                index = 0;

            spawnPos = spawnPoints[index].position;
        }

        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        Debug.Log($"Spawned Player {clientId + 1} at {spawnPos}");
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (NetworkManager != null && NetworkManager.SceneManager != null)
        {
            NetworkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
        }
    }
}