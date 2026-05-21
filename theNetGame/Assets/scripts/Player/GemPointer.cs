using UnityEngine;
using Unity.Netcode;

public class GemPointer : NetworkBehaviour
{
    [SerializeField] Transform arrowVisual;

    [Header("Settings")]
    [SerializeField] float radius = 1.5f;

    [SerializeField] float rotationSpeed = 10f;

    [SerializeField] float moveSmoothness = 15f;

    Transform targetGem;

    void Start()
    {
        // Detach arrow from player at runtime
        // so it doesn't inherit player flipping
        arrowVisual.SetParent(null);

        // Hide other players' arrows
        if (!IsOwner)
        {
            arrowVisual.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Only run for local player
        if (!IsOwner) return;

        FindClosestGem();

        // NO GEMS → hide arrow
        if (targetGem == null)
        {
            if (arrowVisual.gameObject.activeSelf)
            {
                arrowVisual.gameObject.SetActive(false);
            }

            return;
        }

        // GEMS EXIST → show arrow
        if (!arrowVisual.gameObject.activeSelf)
        {
            arrowVisual.gameObject.SetActive(true);
        }

        Vector2 dir =
            (targetGem.position - transform.position).normalized;

        // ROTATION
        float angle =
            Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        Quaternion targetRot =
            Quaternion.Euler(0, 0, angle);

        arrowVisual.rotation = Quaternion.RotateTowards(
            arrowVisual.rotation,
            targetRot,
            rotationSpeed * 360f * Time.deltaTime
        );

        // POSITION
        Vector3 targetPos =
            transform.position + (Vector3)(dir * radius);

        arrowVisual.position = Vector3.Lerp(
            arrowVisual.position,
            targetPos,
            moveSmoothness * Time.deltaTime
        );
    }

    void FindClosestGem()
    {
        Gem[] gems = FindObjectsByType<Gem>(
            FindObjectsSortMode.None
        );

        float closestDist = Mathf.Infinity;

        Transform closest = null;

        foreach (Gem gem in gems)
        {
            float dist = Vector2.Distance(
                transform.position,
                gem.transform.position
            );

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = gem.transform;
            }
        }

        targetGem = closest;
    }
}