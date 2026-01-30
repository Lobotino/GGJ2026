using UnityEngine;

public class RotateYFast : MonoBehaviour
{
    public float DegreesPerSecond = 360f;

    private void Update()
    {
        transform.Rotate(0f, DegreesPerSecond * Time.deltaTime, 0f, Space.Self);
    }
}
