using UnityEngine;
using Unity.Netcode;

public class BlockCoinVisual : NetworkBehaviour
{
    [SerializeField] float popForce = 6f;
    [SerializeField] float gravity = 20f;

    float velocity;

    public void Pop()
    {
        velocity = popForce;
    }

    void Update()
    {
        if (!IsServer) return;

        velocity -= gravity * Time.deltaTime;

        transform.position += Vector3.up * velocity * Time.deltaTime;

        // When it starts falling → despawn
        if (velocity <= 0)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }
}