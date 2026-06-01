using UnityEngine;
using Unity.Netcode;

public class MovingPlatform : NetworkBehaviour
{
    [Header("Path")]
    [SerializeField] Transform pointA;
    [SerializeField] Transform pointB;

    [Header("Movement")]
    [SerializeField] float speed = 3f;

    bool movingToB = true;

    void Start()
    {
        if (pointA != null)
        {
            transform.position = pointA.position;
        }
    }

    void Update()
    {
        if (!IsServer) return;

        if (pointA == null || pointB == null)
            return;

        Transform target =
            movingToB ? pointB : pointA;

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(
            transform.position,
            target.position) < 0.05f)
        {
            movingToB = !movingToB;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        collision.transform.SetParent(transform);
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        collision.transform.SetParent(null);
    }
}