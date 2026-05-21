using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class GemManager : NetworkBehaviour
{
    public static GemManager Instance;

    [SerializeField] public GameObject gemPrefab;
    [SerializeField] Transform[] spawnPoints;

    int lastSpawnIndex = -1;

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(SpawnGemWithDelay(0f)); 
        }
    }

    public void OnGemCollected(PlayerController2D player, bool isWorldGem)
    {
        if (!IsServer) return;

        player.AddGem();

        // CHECK WIN FOR ALL GEMS
        if (player.GetGemCount() >= 10)
        {
            Debug.Log($"PLAYER {player.OwnerClientId + 1} WINS!");
            ShowWinClientRpc(player.OwnerClientId);
            return;
        }

        // ONLY reset/spawn if it was a world gem
        if (isWorldGem)
        {
            ResetWorld();
            StartCoroutine(SpawnGemWithDelay(1f));
        }
    }

    [ClientRpc]
    void ShowWinClientRpc(ulong winnerId)
    {
        WinUI.Instance.ShowWin(winnerId);
    }

    IEnumerator SpawnGemWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnGem();
    }

    void SpawnGem()
    {
        if (!IsServer) return;

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points assigned!");
            return;
        }

        int index;

        
        do
        {
            index = Random.Range(0, spawnPoints.Length);
        }
        while (spawnPoints.Length > 1 && index == lastSpawnIndex);

        lastSpawnIndex = index;

        Transform spawn = spawnPoints[index];

        GameObject gem = Instantiate(gemPrefab, spawn.position, Quaternion.identity);

        var netObj = gem.GetComponent<NetworkObject>();
        netObj.Spawn();

        var gemScript = gem.GetComponent<Gem>();
        if (gemScript != null)
        {
            gemScript.SetAsWorldGem();
        }

        Debug.Log($"Gem spawned at point {index}");
    }

    void ResetWorld()
    {
        foreach (var coin in FindObjectsByType<Coin>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            coin.ResetCoin();
        }

        foreach (var block in FindObjectsByType<QuestionBlock>(FindObjectsSortMode.None))
        {
            block.ResetBlock();
        }
    }
}