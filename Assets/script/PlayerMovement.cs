using UnityEngine;
using UnityEngine.UI;
using InteractableSystem;
using DG.Tweening;
using System.Threading.Tasks;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 20f;  // per second
    public float staminaRegenRate = 10f;  // per second
    public float runSpeedMultiplier = 1.5f;
    public KeyCode runKey = KeyCode.LeftShift;

    [Header("Sounds")]
    [SerializeField] private AudioClip walkingSound;
    [SerializeField] private AudioClip runningSound;
    private AudioClip currentMovementClip;
    private AudioSource audioSource;


    [Header("UI References")]
    public Slider staminaSlider; // Referentie naar de UI slider voor stamina
    public Image staminaFillImage; // Optioneel: om de kleur te veranderen

    private float currentStamina;
    private bool isRunning;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.3f;

    [Header("Camera Reference")]
    public Transform cameraTransform;

    [Header ("Caught settings")]
    public CanvasGroup canvasGroup;
    public float AmountCaught;
    public TextMeshProUGUI CaughtAmountText;


    private Rigidbody rb;
    private bool isGrounded;
    private bool isCaught = false;
    private Vector3 moveDirection;

    void Start()
    {
        canvasGroup.gameObject.SetActive(false);


        currentStamina = maxStamina;

        rb = GetComponent<Rigidbody>();

        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f;

        // Initialiseer de stamina slider als deze is toegewezen
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }
        else
        {
            Debug.LogWarning("Geen stamina slider toegewezen in de Inspector!");
        }

        // Try to find the camera if not manually set
        if (cameraTransform == null)
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
                Debug.Log("Main camera found and assigned.");
            }
            else
            {
                Camera[] cameras = FindObjectsOfType<Camera>();
                if (cameras.Length > 0)
                {
                    cameraTransform = cameras[0].transform;
                    Debug.Log("Camera found and assigned via FindObjectsOfType.");
                }
                else
                {
                    Debug.LogError("No camera found! Set a camera manually in the Inspector.");
                }
            }
        }

        // Configure rigidbody for better movement
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void Update()
    {

        if (isCaught) return;


        // Check if we have a camera before continuing
        if (cameraTransform == null)
        {
            Debug.LogWarning("No camera reference found. Movement won't work.");
            return;
        }

        // Handle input
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // Calculate movement direction relative to camera
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        // We only want the horizontal component of the camera's direction
        camForward.y = 0;
        camRight.y = 0;

        // Normalize only if the vectors are not zero
        if (camForward.magnitude > 0.001f)
            camForward.Normalize();
        if (camRight.magnitude > 0.001f)
            camRight.Normalize();

        // Calculate movement direction based on camera orientation
        moveDirection = (camForward * v + camRight * h);
        if (moveDirection.magnitude > 0.001f)
            moveDirection.Normalize();

        // Check if player is grounded
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);

        // Handle jumping
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // REMOVED: All interaction handling - let PlayerInteraction handle this

        // Check if trying to run and has stamina
        isRunning = Input.GetKey(runKey) && currentStamina > 0f && (h != 0f || v != 0f);
        bool isMoving = moveDirection.magnitude > 0.1f;

        if (isMoving)
        {
            // Choose sound based on running or walking
            AudioClip desiredClip = isRunning ? runningSound : walkingSound;

            // Only proceed if we have a valid clip
            if (desiredClip != null)
            {
                // Change clip if needed or if audio isn't playing
                if (audioSource.clip != desiredClip || !audioSource.isPlaying)
                {
                    audioSource.Stop(); // Stop current sound
                    audioSource.clip = desiredClip;
                    audioSource.loop = true;
                    audioSource.volume = 1f; // Ensure volume is at full
                    audioSource.Play();
                }
            }
            else
            {
                Debug.LogWarning($"Missing audio clip! Running: {isRunning}");
            }

            // Handle stamina (same as before)
            if (isRunning)
            {
                currentStamina -= staminaDrainRate * Time.deltaTime;
                if (currentStamina < 0f)
                    currentStamina = 0f;
            }
            else
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                if (currentStamina > maxStamina)
                    currentStamina = maxStamina;
            }
        }
        else
        {
            // Stop movement sound if not moving
            if (audioSource.isPlaying)
            {
                audioSource.Stop(); // Simple stop instead of fade for debugging
            }

            // Regenerate stamina
            currentStamina += staminaRegenRate * Time.deltaTime;
            if (currentStamina > maxStamina)
                currentStamina = maxStamina;
        }



        // Update UI
        UpdateStaminaUI();
    }

    // REMOVED: CheckForInteractables method - PlayerInteraction handles this

    void UpdateStaminaUI()
    {
        // Update stamina slider waarde
        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;
        }

        // Optioneel: Verander kleur van stamina bar op basis van waarde
        if (staminaFillImage != null)
        {
            // Verander kleur op basis van hoeveelheid stamina
            if (currentStamina < maxStamina * 0.2f)
            {
                staminaFillImage.color = Color.red; // Bijna leeg
            }
            else if (currentStamina < maxStamina * 0.5f)
            {
                staminaFillImage.color = Color.yellow; // Halfvol
            }
            else
            {
                staminaFillImage.color = Color.green; // Vol/goed
            }
        }
    }

    void FixedUpdate()
    {
        if (isCaught) return;


        float currentSpeed = moveSpeed;
        if (isRunning)
        {
            currentSpeed *= runSpeedMultiplier;
        }

        // Apply movement
        Vector3 velocity = moveDirection * currentSpeed;

        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;
    }

    public async Task OnCaught()
    {
        Debug.Log("Player was caught!");
        canvasGroup.gameObject.SetActive(true);

        AmountCaught++;
        isCaught = true;

        if (AmountCaught <= 3)
        {
            if (AmountCaught == 3)
            {
                CaughtAmountText.text = $"You have used {AmountCaught}/3 of your attempts, this is your last chance";
                transform.position = new Vector3(8.93f, 1.8f, 53.41f);

                await Task.Delay(6000);
                canvasGroup.gameObject.SetActive(false);

                isCaught = false;
                Debug.Log("you have used 3 attemps");
            }
            else
            {
                CaughtAmountText.text = $"You have used {AmountCaught}/3 of your attempts";
                transform.position = new Vector3(8.93f, 1.8f, 53.41f);

                await Task.Delay(4000);
                canvasGroup.gameObject.SetActive(false);
                isCaught = false;
                Debug.Log($"You have used {AmountCaught}/3 of your attempts");
            }
        }
        else
        {
            CaughtAmountText.text = $"That was your last chance";
            await Task.Delay(15000);
            Application.Quit();
        }
    }


}