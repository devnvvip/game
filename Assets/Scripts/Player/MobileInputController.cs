using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace StreamerGame.Player
{
    public class MobileInputController : MonoBehaviour
    {
        public static MobileInputController Instance { get; private set; }

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool InteractTriggered { get; private set; }

        [Header("Touch Look Sensitivity")]
        [Range(0.05f, 1.0f)]
        public float touchLookSensitivity = 0.18f;

        [Header("Smoothing")]
        public bool enableSmoothing = true;
        [Range(5f, 35f)]
        public float smoothingSpeed = 22f;

        private int activeLookFingerId = -1;
        private Vector2 rawLookDelta = Vector2.zero;
        private Vector2 currentLookInput = Vector2.zero;

        private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            activeLookFingerId = -1;
            rawLookDelta = Vector2.zero;
            LookInput = Vector2.zero;
        }

        private void Update()
        {
            ProcessTouchLook();
            ProcessEditorMouseLook();

            if (enableSmoothing)
            {
                currentLookInput = Vector2.Lerp(currentLookInput, rawLookDelta, Time.unscaledDeltaTime * smoothingSpeed);
                LookInput = currentLookInput;
            }
            else
            {
                LookInput = rawLookDelta;
            }

            // Decay delta after frame consumption
            rawLookDelta = Vector2.zero;
        }

        private void LateUpdate()
        {
            InteractTriggered = false;
        }

        public void SetMoveInput(Vector2 input)
        {
            MoveInput = input;
        }

        public void TriggerInteract()
        {
            InteractTriggered = true;
        }

        private void ProcessTouchLook()
        {
            var activeTouches = Touch.activeTouches;
            if (activeTouches.Count == 0)
            {
                activeLookFingerId = -1;
                return;
            }

            bool foundActiveFinger = false;

            for (int i = 0; i < activeTouches.Count; i++)
            {
                Touch touch = activeTouches[i];
                int fingerId = touch.finger.index;

                if (touch.phase == TouchPhase.Began)
                {
                    // If we don't have an active look touch and this touch is NOT over UI, claim it
                    if (activeLookFingerId == -1 && !IsPointerOverUI(touch.screenPosition))
                    {
                        activeLookFingerId = fingerId;
                        foundActiveFinger = true;
                    }
                }
                else if (fingerId == activeLookFingerId)
                {
                    if (touch.phase == TouchPhase.Moved)
                    {
                        rawLookDelta += touch.delta * touchLookSensitivity;
                        foundActiveFinger = true;
                    }
                    else if (touch.phase == TouchPhase.Stationary)
                    {
                        foundActiveFinger = true;
                    }
                    else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        activeLookFingerId = -1;
                    }
                }
            }

            if (!foundActiveFinger && activeLookFingerId != -1)
            {
                activeLookFingerId = -1;
            }
        }

        private void ProcessEditorMouseLook()
        {
            // Allow testing look-drag in Editor or standalone without touch
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            // If mouse left-click is dragged anywhere not on UI and cursor isn't locked
            if (mouse.leftButton.isPressed && Cursor.lockState != CursorLockMode.Locked)
            {
                Vector2 mousePos = mouse.position.ReadValue();
                if (!IsPointerOverUI(mousePos))
                {
                    Vector2 mouseDelta = mouse.delta.ReadValue();
                    rawLookDelta += mouseDelta * touchLookSensitivity * 0.75f;
                }
            }
        }

        private bool IsPointerOverUI(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };

            raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, raycastResults);

            for (int i = 0; i < raycastResults.Count; i++)
            {
                GameObject hitObj = raycastResults[i].gameObject;
                if (hitObj == null) continue;

                // Ignore background canvas if it doesn't block rays
                if (hitObj.name == "UICanvas" || hitObj.name == "SafeAreaContainer" || hitObj.name == "ReticleDot")
                {
                    continue;
                }

                // Any actual UI element (Button, Joystick, Card, etc.) blocks look
                return true;
            }

            return false;
        }
    }
}
