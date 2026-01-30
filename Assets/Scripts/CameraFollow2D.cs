using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Smoothing")]
    [Range(0.01f, 1f)]
    [SerializeField] private float smoothSpeed = 0.1f;

    [Header("World Bounds (min/max corners of the level)")]
    [SerializeField] private Vector2 worldMin = new Vector2(-20f, -12f);
    [SerializeField] private Vector2 worldMax = new Vector2(20f, 12f);

    private Camera cam;
    private Vector3 velocity;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (target == null || cam == null) return;

        // Desired position follows the target, keep camera Z unchanged
        Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);

        // Smooth follow using SmoothDamp
        Vector3 smoothed = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothSpeed);

        // Clamp camera so it never shows area outside world bounds
        float camHalfHeight = cam.orthographicSize;
        float camHalfWidth = camHalfHeight * cam.aspect;

        float clampedX = Mathf.Clamp(smoothed.x, worldMin.x + camHalfWidth, worldMax.x - camHalfWidth);
        float clampedY = Mathf.Clamp(smoothed.y, worldMin.y + camHalfHeight, worldMax.y - camHalfHeight);

        transform.position = new Vector3(clampedX, clampedY, smoothed.z);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 center = new Vector3(
            (worldMin.x + worldMax.x) * 0.5f,
            (worldMin.y + worldMax.y) * 0.5f,
            0f);
        Vector3 size = new Vector3(
            worldMax.x - worldMin.x,
            worldMax.y - worldMin.y,
            0f);
        Gizmos.DrawWireCube(center, size);
    }
}
