using UnityEngine;

public class MouseLookController : MonoBehaviour
{
    [Header("Look Settings")]
    public float mouseSensitivity = 2.0f;
    public float smoothing = 2.0f;
    public Transform playerBody;
    public bool lockCursor = true;

    [Header("Rotation Limits")]
    public float minVerticalAngle = -90f;
    public float maxVerticalAngle = 90f;

    private Vector2 smoothedVelocity;
    private Vector2 currentLookingPos;
    private float xRotation = 0f;
    private bool cursorWasLocked = false;

    void Start()
    {
        // Als playerBody niet is toegewezen, probeer het parent object te gebruiken
        if (playerBody == null)
        {
            playerBody = transform.parent;
            if (playerBody == null)
            {
                Debug.LogWarning("PlayerBody niet toegewezen aan MouseLookController. Ken een playerBody toe in de Inspector.");
            }
        }

        // Cursor alleen locken als het script enabled is
        if (enabled && lockCursor)
        {
            LockCursor();
        }
    }

    void OnEnable()
    {
        // Cursor locken wanneer script wordt ingeschakeld
        if (lockCursor)
        {
            LockCursor();
        }
    }

    void OnDisable()
    {
        // Cursor vrijgeven wanneer script wordt uitgeschakeld
        if (cursorWasLocked)
        {
            UnlockCursor();
        }
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cursorWasLocked = true;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorWasLocked = false;
    }

    void Update()
    {
        if (playerBody == null)
            return;

        // Muis input ophalen
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Smoothing toepassen (optioneel)
        Vector2 targetVelocity = new Vector2(mouseX, mouseY);
        smoothedVelocity = Vector2.Lerp(smoothedVelocity, targetVelocity, 1f / smoothing);

        // Huidige kijkpositie bijwerken
        currentLookingPos += smoothedVelocity;

        // Verticale rotatie beperken
        xRotation -= smoothedVelocity.y;
        xRotation = Mathf.Clamp(xRotation, minVerticalAngle, maxVerticalAngle);

        // Camera verticaal roteren (omhoog/omlaag kijken)
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Speler horizontaal roteren (links/rechts kijken)
        playerBody.Rotate(Vector3.up * smoothedVelocity.x);
    }
}