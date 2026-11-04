using UnityEngine;
using UnityEngine.EventSystems;

namespace StreamerGame.Player
{
    public class MobileInputController : MonoBehaviour
    {
        public static MobileInputController Instance { get; private set; }

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool InteractTriggered { get; private set; }

        [Header("Touch Settings")]
        public float touchLookSensitivity = 0.2f;

        private int lookTouchId = -1;
        private Vector2 lastLookTouchPos;

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
            HandleTouchLook();
        }

        private void LateUpdate()
        {
            // Reset one-frame triggers
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

        private void HandleTouchLook()
        {
            LookInput = Vector2.zero;

            #if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);

                // Ignore UI touches on touch start
                if (t.phase == TouchPhase.Began)
                {
                    if (t.position.x > Screen.width * 0.4f && !EventSystem.current.IsPointerOverGameObject(t.fingerId))
                    {
                        lookTouchId = t.fingerId;
                        lastLookTouchPos = t.position;
                    }
                }
                else if (t.fingerId == lookTouchId)
                {
                    if (t.phase == TouchPhase.Moved)
                    {
                        Vector2 delta = t.position - lastLookTouchPos;
                        LookInput = delta * touchLookSensitivity;
                        lastLookTouchPos = t.position;
                    }
                    else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        lookTouchId = -1;
                    }
                }
            }
            #endif
        }
    }
}
