using UnityEngine;
using StreamerGame.Core;
using StreamerGame.UI;

namespace StreamerGame.Interactables
{
    public class Bed : InteractableObject
    {
        private void Start()
        {
            promptText = "[E] Lên giường đi ngủ";
            associatedQuestId = "q_sleep";
        }

        public override bool CanInteract
        {
            get
            {
                if (TimeOfDayManager.Instance == null) return false;
                return TimeOfDayManager.Instance.currentPhase == DayPhase.Night;
            }
        }

        public override string PromptText
        {
            get
            {
                if (TimeOfDayManager.Instance != null && TimeOfDayManager.Instance.currentPhase == DayPhase.Night)
                {
                    return "[E] Đi ngủ để kết thúc một ngày";
                }
                return "Chưa đến giờ đi ngủ (Hãy hoàn thành các hoạt động trong ngày)";
            }
        }

        protected override void OnInteracted()
        {
            StatsManager.Instance?.ModifyEnergy(100f);
            HUDManager.Instance?.ShowNotification("Chúc ngủ ngon! Một ngày làm việc của idol streamer đã kết thúc.");
            GameManager.Instance?.CompleteDay();
        }
    }
}
