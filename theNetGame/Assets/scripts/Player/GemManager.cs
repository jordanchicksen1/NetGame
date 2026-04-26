using UnityEngine;
using Unity.Netcode;

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

    public void OnGemCollected(PlayerController2D player, bool isWorldGem)
    {
        if (!IsServer) return;

        player.AddGem();

        // ONLY reset + spawn if this was a world gem
        if (isWorldGem)
        {
            ResetWorld();

            if (player.GetGemCount() >= 10)
            {
                Debug.Log($"PLAYER {player.OwnerClientId} WINS!");
                ShowWinClientRpc(player.OwnerClientId);
            }
            else
            {
                SpawnGem();
            }
        }
    }

    [ClientRpc]
    void ShowWinClientRpc(ulong winnerId)
    {
        WinUI.Instance.ShowWin(winnerId);
    }

    void SpawnGem()
    {
        if (!IsServer) return;

        if (spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points assigned!");
            return;
        }

        int index = Random.Range(0, spawnPoints.Length);
        Transform spawn = spawnPoints[index];

        GameObject gem = Instantiate(gemPrefab, spawn.position, Quaternion.identity);

        var netObj = gem.GetComponent<NetworkObject>();
        netObj.Spawn();

        var gemScript = gem.GetComponent<Gem>();
        if (gemScript != null)
        {
            gemScript.SetAsWorldGem(); // mark as map gem
        }
    }

    void ResetWorld()
    {
        // Reset coins (including inactive ones)
        foreach (var coin in FindObjectsByType<Coin>(FindObjectsInactive.Include, FindObjectsSortMode.None))
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