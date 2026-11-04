using UnityEngine;
using UnityEngine.InputSystem;
using StreamerGame.Interactables;
using StreamerGame.UI;

namespace StreamerGame.Player
{
    public class PlayerInteraction : MonoBehaviour
    {
        public static PlayerInteraction Instance { get; private set; }

        public Transform raycastOrigin;
        public float maxDistance = 3.0f;
        public LayerMask interactLayer = ~0;

        private IInteractable currentTarget;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            CheckForInteractable();
            HandleInteractionInput();
        }

        private void CheckForInteractable()
        {
            if (raycastOrigin == null) return;

            Ray ray = new Ray(raycastOrigin.position, raycastOrigin.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactLayer))
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null && interactable.CanInteract)
                {
                    if (currentTarget != interactable)
                    {
                        currentTarget = interactable;
                        HUDManager.Instance?.ShowInteractionPrompt(currentTarget.PromptText);
                    }
                    return;
                }
            }

            if (currentTarget != null)
            {
                currentTarget = null;
                HUDManager.Instance?.HideInteractionPrompt();
            }
        }

        private void HandleInteractionInput()
        {
            bool trigger = false;

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame)
            {
                trigger = true;
            }

            if (MobileInputController.Instance != null && MobileInputController.Instance.InteractTriggered)
            {
                trigger = true;
            }

            if (trigger && currentTarget != null)
            {
                currentTarget.Interact();
            }
        }

        public void ForceInteractWithCurrent()
        {
            if (currentTarget != null)
            {
                currentTarget.Interact();
            }
        }
    }
}
