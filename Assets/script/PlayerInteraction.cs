using UnityEngine;
using UnityEngine.UI;
using InteractableSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 3f;
    public LayerMask interactableLayer = -1; // -1 means all layers
    public Transform rayOrigin;
    public Text interactionText;

    private IInteractable currentInteractable;
    private bool debugMode = true; // Toggle this to enable/disable extra debugging

    private void Start()
    {
        // Validate components at start
        if (interactionText == null)
        {
            Debug.LogError("InteractionText is not assigned!");
            return;
        }

        if (rayOrigin == null)
        {
            Debug.LogError("RayOrigin is not assigned!");
            return;
        }

        // Initial state
        interactionText.gameObject.SetActive(false);
    }

    private void Update()
    {
        CheckForInteractable();

        // Handle interaction input
        if (currentInteractable != null && Input.GetKeyDown(KeyCode.E))
        {
            if (debugMode)
                Debug.Log($"Interacting with: {(currentInteractable as MonoBehaviour)?.name}");
            currentInteractable.Interact();
        }
    }

    private void CheckForInteractable()
    {
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        RaycastHit hit;

        // Use SphereCast for better detection
        if (Physics.SphereCast(ray, 0.1f, out hit, interactRange))
        {
            //if (debugMode)
            //    Debug.Log($"Raycast hit: {hit.collider.name} at distance: {hit.distance}");

            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable == null)
            {
                // Also check parent objects
                interactable = hit.collider.GetComponentInParent<IInteractable>();
            }

            if (interactable != null)
            {
                UpdateInteractionUI(interactable);
            }
            else
            {
                ClearInteractionUI("Hit object is not interactable");
            }
        }
        else
        {
            ClearInteractionUI("No raycast hit");
        }

        // Draw debug ray
        if (debugMode)
        {
            Debug.DrawRay(rayOrigin.position, rayOrigin.forward * interactRange, Color.red, 0.1f);
        }
    }

    private void UpdateInteractionUI(IInteractable interactable)
    {
        // Handle focus changes
        if (currentInteractable != interactable)
        {
            // Lose focus on previous object
            if (currentInteractable != null)
            {
                InteractableObject prevObj = currentInteractable as InteractableObject;
                if (prevObj != null)
                    prevObj.OnLoseFocus();
            }

            // Gain focus on new object
            InteractableObject newObj = interactable as InteractableObject;
            if (newObj != null)
                newObj.OnFocus();
        }

        currentInteractable = interactable;
        string prompt = currentInteractable.GetInteractionPrompt();

        if (debugMode)
        {
            string objName = (currentInteractable as MonoBehaviour)?.name ?? "Unknown";
            //Debug.Log($"Interactable found: {objName}. Prompt: '{prompt}'");
        }

        if (!string.IsNullOrEmpty(prompt))
        {
            SetInteractionTextActive(true, prompt);
        }
        else
        {
            SetInteractionTextActive(false, "");
            //if (debugMode)
            //    Debug.Log("Prompt is empty or null");
        }
    }

    private void ClearInteractionUI(string reason)
    {
        // Handle losing focus
        if (currentInteractable != null)
        {
            InteractableObject obj = currentInteractable as InteractableObject;
            if (obj != null)
                obj.OnLoseFocus();
        }

        currentInteractable = null;
        SetInteractionTextActive(false, "");

        //if (debugMode)
        //    Debug.Log($"Clearing UI - Reason: {reason}");
    }

    private void SetInteractionTextActive(bool active, string text = "")
    {
        if (interactionText == null)
        {
            Debug.LogError("InteractionText is null!");
            return;
        }

        // Set the text first
        if (active && !string.IsNullOrEmpty(text))
        {
            interactionText.text = text;
        }

        // Then activate/deactivate the GameObject
        interactionText.gameObject.SetActive(active);

        // Additional debugging
        //if (debugMode)
        //{
        //    Debug.Log($"SetInteractionTextActive - Active: {active}, Text: '{text}'");
        //    Debug.Log($"GameObject Active After Set: {interactionText.gameObject.activeSelf}");

        //    // Check parent hierarchy
        //    Transform parent = interactionText.transform.parent;
        //    while (parent != null)
        //    {
        //        Debug.Log($"Parent '{parent.name}' active: {parent.gameObject.activeSelf}");
        //        parent = parent.parent;
        //    }
        //}

        // Force canvas update
        Canvas canvas = interactionText.GetComponentInParent<Canvas>();
        if (canvas != null && active)
        {
            canvas.enabled = false;
            canvas.enabled = true;
        }
    }

    // Force update method for external calls (improved)
    public void ForceUpdateInteractionText()
    {
        if (debugMode)
            Debug.Log("ForceUpdateInteractionText called");

        // Force a fresh check
        StartCoroutine(ForceUpdateCoroutine());
    }

    private System.Collections.IEnumerator ForceUpdateCoroutine()
    {
        // Wait a frame to ensure everything is updated
        yield return null;

        // Clear current state
        currentInteractable = null;

        // Force a new check
        CheckForInteractable();

        if (debugMode)
            Debug.Log("Force update completed");
    }

    // Public method to toggle debug mode
    public void SetDebugMode(bool enabled)
    {
        debugMode = enabled;
    }

    // Method to test UI directly
    [ContextMenu("Test UI Show")]
    public void TestUIShow()
    {
        SetInteractionTextActive(true, "TEST MESSAGE");
    }

    [ContextMenu("Test UI Hide")]
    public void TestUIHide()
    {
        SetInteractionTextActive(false);
    }
}