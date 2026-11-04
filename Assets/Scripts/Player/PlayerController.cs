using UnityEngine;
using UnityEngine.InputSystem;
using StreamerGame.Core;

namespace StreamerGame.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movement Settings")]
        public float walkSpeed = 3.5f;
        public float sprintSpeed = 5.5f;
        public float gravity = -15f;

        [Header("Look Settings")]
        public Transform cameraHolder;
        public float mouseSensitivity = 0.12f;
        public float minPitch = -80f;
        public float maxPitch = 80f;

        [Header("Interaction Raycast")]
        public float interactionDistance = 2.8f;
        public LayerMask interactionLayers = ~0;

        private CharacterController characterController;
        private float pitch = 0f;
        private float verticalVelocity = 0f;
        private bool isSitting = false;
        private Vector3 standPosition;
        private Quaternion standRotation;
        private Transform sitTargetTransform;

        public bool IsSitting => isSitting;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            characterController = GetComponent<CharacterController>();
        }

        private void Start()
        {
            if (cameraHolder != null)
            {
                pitch = cameraHolder.localEulerAngles.x;
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.currentState == GameState.Paused)
            {
                return;
            }

            HandleInputShortcuts();

            if (isSitting)
            {
                HandleSittingCamera();
                return;
            }

            if (GameManager.Instance != null && GameManager.Instance.currentState != GameState.Walking)
            {
                return;
            }

            HandleRotation();
            HandleMovement();
        }

        private void HandleInputShortcuts()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame)
                {
                    bool isPaused = GameManager.Instance.currentState == GameState.Paused;
                    GameManager.Instance.PauseGame(!isPaused);
                }

                if (kb.tabKey.wasPressedThisFrame)
                {
                    UI.HUDManager.Instance?.TogglePhone();
                }
            }
        }

        private void HandleRotation()
        {
            Vector2 look = Vector2.zero;

            Mouse mouse = Mouse.current;
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                look = mouse.delta.ReadValue() * mouseSensitivity;
            }

            if (MobileInputController.Instance != null)
            {
                look += MobileInputController.Instance.LookInput;
            }

            // Yaw (Player body)
            transform.Rotate(Vector3.up * look.x);

            // Pitch (Camera)
            pitch -= look.y;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            if (cameraHolder != null)
            {
                cameraHolder.localEulerAngles = new Vector3(pitch, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            Vector2 input = Vector2.zero;
            bool isSprinting = false;

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed) input.y += 1f;
                if (kb.sKey.isPressed) input.y -= 1f;
                if (kb.aKey.isPressed) input.x -= 1f;
                if (kb.dKey.isPressed) input.x += 1f;
                isSprinting = kb.leftShiftKey.isPressed;
            }

            if (MobileInputController.Instance != null)
            {
                Vector2 mob = MobileInputController.Instance.MoveInput;
                if (mob.sqrMagnitude > 0.01f)
                {
                    input = mob;
                }
            }

            input.Normalize();

            // Calculate movement vector
            float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
            Vector3 moveDir = (transform.right * input.x + transform.forward * input.y);

            // Ground & gravity
            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 finalMove = moveDir * currentSpeed + Vector3.up * verticalVelocity;
            characterController.Move(finalMove * Time.deltaTime);
        }

        private void HandleSittingCamera()
        {
            // Allow slight camera head rotation while sitting
            Vector2 look = Vector2.zero;
            Mouse mouse = Mouse.current;
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                look = mouse.delta.ReadValue() * mouseSensitivity * 0.5f;
            }

            pitch = Mathf.Clamp(pitch - look.y, -30f, 30f);
            float yaw = Mathf.Clamp(cameraHolder.localEulerAngles.y + look.x, -45f, 45f);
            if (cameraHolder != null)
            {
                cameraHolder.localEulerAngles = new Vector3(pitch, yaw, 0f);
            }

            // Check if player presses space to stand up
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame && GameManager.Instance.currentState != GameState.Livestreaming)
            {
                StandUp();
            }
        }

        public void SitDown(Transform sitAnchor)
        {
            if (isSitting) return;

            isSitting = true;
            sitTargetTransform = sitAnchor;
            standPosition = transform.position;
            standRotation = transform.rotation;

            characterController.enabled = false;
            transform.position = sitAnchor.position;
            transform.rotation = sitAnchor.rotation;
            if (cameraHolder != null)
            {
                cameraHolder.localRotation = Quaternion.identity;
                pitch = 0f;
            }

            GameManager.Instance?.SetGameState(GameState.SittingAtDesk);
        }

        public void StandUp()
        {
            if (!isSitting) return;

            isSitting = false;
            transform.position = standPosition;
            transform.rotation = standRotation;
            characterController.enabled = true;

            GameManager.Instance?.SetGameState(GameState.Walking);
        }
    }
}
