using UnityEngine;
using Unity.Netcode;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] GameObject playerPrefab;
    [SerializeField] Transform[] spawnPoints;

    bool hasSpawnedInitial = false;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // Prevent double spawning
        if (!hasSpawnedInitial)
        {
            hasSpawnedInitial = true;

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                SpawnPlayer(client.ClientId);
            }
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        // Prevent spawning if player already exists
        if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null)
            return;

        SpawnPlayer(clientId);
    }

    void SpawnPlayer(ulong clientId)
    {
        Vector3 spawnPos = Vector3.zero;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // LOCKED SPAWN LOGIC
            int index = (int)clientId;

            if (index >= spawnPoints.Length)
                index = 0; // fallback (just in case)

            spawnPos = spawnPoints[index].position;
        }

        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        Debug.Log($"Spawned Player {clientId + 1} at {spawnPos}");
    }

    public override void OnDestroy()
    {
        base.OnDestroy(); 

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}