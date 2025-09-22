using UnityEngine;

public class BookFloater : MonoBehaviour
{
    public Transform bookTransform;
    public float floatHeight = 0.15f; // How high to float (1 unit)
    public float floatSpeed = 2f;  // Speed of floating motion

    private Vector3 startPosition;

    void Start()
    {
        // Store the initial position
        startPosition = bookTransform.position;
    }

    void Update()
    {
        // Calculate the new Y position using a sine wave
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;

        // Apply the new position (keeping X and Z unchanged)
        bookTransform.position = new Vector3(
            startPosition.x,
            newY,
            startPosition.z
        );
    }
}