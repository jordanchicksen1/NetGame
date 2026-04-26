using UnityEngine;
using Unity.Netcode;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] GameObject playerPrefab;
    [SerializeField] Transform[] spawnPoints;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            SpawnPlayer(client.ClientId);
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        SpawnPlayer(clientId);
    }

    void SpawnPlayer(ulong clientId)
    {
        Vector3 spawnPos = Vector3.zero;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int index = Random.Range(0, spawnPoints.Length);
            spawnPos = spawnPoints[index].position;
        }

        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
    }
}