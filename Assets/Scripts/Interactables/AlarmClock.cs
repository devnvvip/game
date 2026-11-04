using UnityEngine;
using StreamerGame.Audio;
using StreamerGame.UI;

namespace StreamerGame.Interactables
{
    public class AlarmClock : InteractableObject
    {
        [Header("Alarm Clock Setup")]
        public GameObject displayScreen;
        public bool isRinging = true;

        private void Start()
        {
            promptText = "[E] Tắt chuông báo thức";
            associatedQuestId = "q_alarm";

            if (isRinging)
            {
                SoundManager.Instance?.StartAlarm();
            }
        }

        protected override void OnInteracted()
        {
            if (isRinging)
            {
                isRinging = false;
                SoundManager.Instance?.StopAlarm();
                HUDManager.Instance?.ShowNotification("Đã tắt báo thức! Chào ngày mới đầy năng lượng.");
            }
        }

        public override bool CanInteract => isRinging;
    }
}
