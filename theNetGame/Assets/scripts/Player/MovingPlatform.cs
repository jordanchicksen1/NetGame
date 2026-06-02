using Unity.Netcode;
using UnityEngine;

public class MovingPlatform : NetworkBehaviour
{
    [Header("Path")]
    [SerializeField] Transform pointA;
    [SerializeField] Transform pointB;

    [Header("Movement")]
    [SerializeField] float speed = 3f;
    


    bool movingToB = true;

    Vector3 previousPosition;
    public Vector3 DeltaMovement { get; private set; }

    void Start()
    {
        if (pointA != null)
        {
            transform.position = pointA.position;
        }

        previousPosition = transform.position;
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

        if (Vector3.Distance(transform.position, target.position) < 0.05f)
        {
            movingToB = !movingToB;
        }
    }

    void LateUpdate()
    {
        DeltaMovement = transform.position - previousPosition;

        previousPosition = transform.position;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        foreach (var contact in collision.contacts)
        {
            if (contact.normal.y < -0.5f)
            {
                PlayerController2D player =
                    collision.gameObject.GetComponent<PlayerController2D>();

                if (player != null)
                {
                    player.SetCurrentPlatform(this);
                }

                break;
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        PlayerController2D player =
            collision.gameObject.GetComponent<PlayerController2D>();

        if (player != null)
        {
            player.ClearCurrentPlatform();
        }
    }

}