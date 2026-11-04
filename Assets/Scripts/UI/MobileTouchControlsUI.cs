using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using StreamerGame.Player;

namespace StreamerGame.UI
{
    public class MobileTouchControlsUI : MonoBehaviour
    {
        public static MobileTouchControlsUI Instance { get; private set; }

        [Header("Controls Container")]
        public GameObject controlsRoot;

        [Header("Joystick")]
        public RectTransform joystickBackground;
        public RectTransform joystickHandle;
        public float joystickRadius = 60f;

        [Header("Action Buttons")]
        public Button interactButton;
        public Button phoneButton;
        public Button pauseButton;

        private Vector2 joystickStartPos;
        private int joystickPointerId = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Auto detect mobile device or keep active in editor for simulation
            #if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
            if (controlsRoot != null) controlsRoot.SetActive(true);
            #else
            // In standalone desktop, hide by default, but allow toggle if needed
            if (controlsRoot != null) controlsRoot.SetActive(true);
            #endif

            if (interactButton != null)
            {
                interactButton.onClick.AddListener(OnInteractButtonClicked);
            }

            if (phoneButton != null)
            {
                phoneButton.onClick.AddListener(OnPhoneButtonClicked);
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(OnPauseButtonClicked);
            }

            SetupJoystickTouchEvents();
        }

        private void SetupJoystickTouchEvents()
        {
            if (joystickBackground == null) return;

            EventTrigger trigger = joystickBackground.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = joystickBackground.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener((data) => { OnJoystickDown((PointerEventData)data); });
            trigger.triggers.Add(down);

            EventTrigger.Entry drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            drag.callback.AddListener((data) => { OnJoystickDrag((PointerEventData)data); });
            trigger.triggers.Add(drag);

            EventTrigger.Entry up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener((data) => { OnJoystickUp((PointerEventData)data); });
            trigger.triggers.Add(up);
        }

        private void OnJoystickDown(PointerEventData eventData)
        {
            joystickPointerId = eventData.pointerId;
            OnJoystickDrag(eventData);
        }

        private void OnJoystickDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != joystickPointerId || joystickBackground == null || joystickHandle == null) return;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickBackground, eventData.position, eventData.pressEventCamera, out localPoint);

            localPoint = Vector2.ClampMagnitude(localPoint, joystickRadius);
            joystickHandle.anchoredPosition = localPoint;

            Vector2 normalized = localPoint / joystickRadius;
            MobileInputController.Instance?.SetMoveInput(normalized);
        }

        private void OnJoystickUp(PointerEventData eventData)
        {
            if (eventData.pointerId != joystickPointerId) return;

            joystickPointerId = -1;
            if (joystickHandle != null) joystickHandle.anchoredPosition = Vector2.zero;
            MobileInputController.Instance?.SetMoveInput(Vector2.zero);
        }

        private void OnInteractButtonClicked()
        {
            MobileInputController.Instance?.TriggerInteract();
            PlayerInteraction.Instance?.ForceInteractWithCurrent();
        }

        private void OnPhoneButtonClicked()
        {
            HUDManager.Instance?.TogglePhone();
        }

        private void OnPauseButtonClicked()
        {
            HUDManager.Instance?.TogglePause();
        }
    }
}
