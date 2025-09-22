    // InteractableSystem.cs - Deze versie bevat alleen de interface en basis klassen
    using UnityEngine;

    public interface IInteractable
    {
        void Interact();
        string GetInteractionPrompt();
    }

    namespace InteractableSystem
    {
        public abstract class InteractableObject : MonoBehaviour, IInteractable
        {
            [Header("Interaction Settings")]
            public string interactionPrompt = "Interact";
            public bool canInteract = true;

        public abstract void Interact();

        public LayerMask interactableLayer;


        public virtual string GetInteractionPrompt()
            {
                return interactionPrompt;
            }

            public virtual void OnFocus() { }
            public virtual void OnLoseFocus() { }
        }

        public class Door : InteractableObject
        {
            [Header("Door Settings")]
            public bool isOpen = false;
            public float openAngle = 90f;
            public float openSpeed = 2.0f;

            private Quaternion initialRotation;
            private Quaternion targetRotation;

            private void Start()
            {
                initialRotation = transform.rotation;
                interactionPrompt = isOpen ? "Close Door" : "Open Door";
                targetRotation = initialRotation;
            }

            private void Update()
            {
                if (transform.rotation != targetRotation)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * openSpeed);
                }
            }

            public override void Interact()
            {
                isOpen = !isOpen;
                targetRotation = isOpen
                    ? initialRotation * Quaternion.Euler(0, openAngle, 0)
                    : initialRotation;
                interactionPrompt = isOpen ? "Close Door" : "Open Door";
                Debug.Log(isOpen ? "Door opened" : "Door closed");
            }
        }
    }