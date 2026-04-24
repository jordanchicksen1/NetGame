using UnityEngine;
using Unity.Netcode;

public class FallingPowerUp : NetworkBehaviour
{
    [SerializeField] float fallSpeed = 2f;

    void Update()
    {
        if (!IsServer) return;

        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
    }
}