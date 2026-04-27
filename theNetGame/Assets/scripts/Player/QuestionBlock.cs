using UnityEngine;
using Unity.Netcode;

public class QuestionBlock : NetworkBehaviour
{
    [Header("Coin")]
    [SerializeField] GameObject coinVisualPrefab;

    [Header("Power Ups")]
    [SerializeField] GameObject fireSpellPrefab;
    [SerializeField] GameObject iceSpellPrefab;
    [SerializeField] GameObject poisonSpellPrefab;

    [SerializeField] float powerUpChance = 0.1f; 

    bool used = false;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (used) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            var player = collision.gameObject.GetComponent<PlayerController2D>();

        
            if (player != null && player.IsOwner)
            {
                HitBlockServerRpc(player.OwnerClientId);
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void HitBlockServerRpc(ulong playerId)
    {
        if (used) return;

        used = true;

        Vector3 spawnPos = transform.position + Vector3.up * 1.5f;

      
        if (Random.value < powerUpChance)
        {
            GameObject prefab = GetRandomSpellPrefab();

            GameObject spell = Instantiate(prefab, spawnPos, Quaternion.identity);
            spell.GetComponent<NetworkObject>().Spawn();
        }
        else
        {
            
            var player = NetworkManager.Singleton.ConnectedClients[playerId].PlayerObject
                .GetComponent<PlayerController2D>();

            if (player != null)
            {
                player.AddCoin();
            }

            
            GameObject coin = Instantiate(coinVisualPrefab, spawnPos, Quaternion.identity);
            coin.GetComponent<NetworkObject>().Spawn();

            coin.GetComponent<BlockCoinVisual>().Pop();
        }
    }

    GameObject GetRandomSpellPrefab()
    {
        int rand = Random.Range(0, 3);

        switch (rand)
        {
            case 0: return fireSpellPrefab;
            case 1: return iceSpellPrefab;
            case 2: return poisonSpellPrefab;
        }

        return fireSpellPrefab;
    }

    public void ResetBlock()
    {
        if (!IsServer) return;

        used = false;
    }
}