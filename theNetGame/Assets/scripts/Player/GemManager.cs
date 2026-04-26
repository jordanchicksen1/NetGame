using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class GemManager : NetworkBehaviour
{
    public static GemManager Instance;

    [SerializeField] public GameObject gemPrefab;
    [SerializeField] Transform[] spawnPoints;

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SpawnGem();
        }
    }

    public void SpawnGem()
    {
        if (!IsServer) return;

        int index = Random.Range(0, spawnPoints.Length);
        Transform spawn = spawnPoints[index];

        GameObject gem = Instantiate(gemPrefab, spawn.position, Quaternion.identity);
        gem.GetComponent<NetworkObject>().Spawn();
    }

    public void OnGemCollected(PlayerController2D player)
    {
        if (!IsServer) return;

        player.AddGem();

        ResetWorld();

        if (player.GetGemCount() >= 10)
        {
            Debug.Log($"PLAYER {player.OwnerClientId} WINS!");
            // TODO: win screen later
        }
        else
        {
            SpawnGem();
        }
    }

    void ResetWorld()
    {
        // Reset coins
        foreach (var coin in FindObjectsByType<Coin>(FindObjectsSortMode.None))
        {
            coin.ResetCoin();
        }

        // Reset question blocks
        foreach (var block in FindObjectsByType<QuestionBlock>(FindObjectsSortMode.None))
        {
            block.ResetBlock();
        }
    }
}