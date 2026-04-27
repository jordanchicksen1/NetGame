using Unity.Netcode;
using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;

    [Header("Follow Settings")]
    [SerializeField] float smoothSpeed = 5f;
    [SerializeField] float xOffset = 0f;

    [Header("Vertical Dead Zone")]
    [SerializeField] float deadZoneHeight = 2f;
    [SerializeField] float minY = 0f;

    void LateUpdate()
    {
        

        if (target == null) return;

        Vector3 currentPos = transform.position;

        
        float targetX = target.position.x + xOffset;

       
        float targetY = currentPos.y;

        float deltaY = target.position.y - currentPos.y;

        if (Mathf.Abs(deltaY) > deadZoneHeight)
        {
            targetY = target.position.y;
        }

        float clampedY = Mathf.Max(targetY, minY);

        Vector3 desiredPosition = new Vector3(
            targetX,
            clampedY,
            currentPos.z
        );

        transform.position = Vector3.Lerp(
            currentPos,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}