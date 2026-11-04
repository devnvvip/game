using UnityEngine;
using StreamerGame.Core;
using StreamerGame.Player;
using StreamerGame.Livestream;
using StreamerGame.UI;

namespace StreamerGame.Interactables
{
    public class StreamingDesk : InteractableObject
    {
        [Header("Desk Setup")]
        public Transform chairSitPoint;
        public GameObject monitorScreens;
        public Material screenActiveMaterial;

        private void Start()
        {
            promptText = "[E] Ngồi vào bàn máy tính Streamer";
        }

        public override string PromptText
        {
            get
            {
                if (PlayerController.Instance != null && PlayerController.Instance.IsSitting)
                {
                    if (TimeOfDayManager.Instance != null && TimeOfDayManager.Instance.currentPhase == DayPhase.Evening)
                    {
                        return "[E] Khởi động buổi phát sóng trực tiếp (BẮT ĐẦU LIVESTREAM)";
                    }
                    else if (TimeOfDayManager.Instance != null && TimeOfDayManager.Instance.currentPhase == DayPhase.Midday)
                    {
                        return "[E] Soạn kịch bản & kiểm tra thông số máy tính";
                    }
                    return "[Phím Cách / Nút Rời Ghế] Đứng dậy";
                }
                return "[E] Ngồi vào bàn máy tính Streamer";
            }
        }

        protected override void OnInteracted()
        {
            if (PlayerController.Instance == null) return;

            if (!PlayerController.Instance.IsSitting)
            {
                PlayerController.Instance.SitDown(chairSitPoint != null ? chairSitPoint : transform);
                HUDManager.Instance?.ShowNotification("Đã ngồi vào vị trí streamer! Bạn có thể thao tác với PC.");
            }
            else
            {
                // Already sitting, trigger PC action based on phase
                DayPhase phase = TimeOfDayManager.Instance != null ? TimeOfDayManager.Instance.currentPhase : DayPhase.Morning;

                if (phase == DayPhase.Midday)
                {
                    QuestManager.Instance?.CompleteQuest("q_prep_script");
                    HUDManager.Instance?.ShowNotification("Đã hoàn thành kịch bản livestream tối nay! (+Fame, +Mood)");
                }
                else if (phase == DayPhase.Evening)
                {
                    QuestManager.Instance?.CompleteQuest("q_start_stream");
                    LivestreamManager.Instance?.StartLivestream();
                }
                else if (phase == DayPhase.Night)
                {
                    QuestManager.Instance?.CompleteQuest("q_cleanup_desk");
                    HUDManager.Instance?.ShowNotification("Đã sao lưu dữ liệu và tắt hệ thống PC.");
                }
            }
        }
    }
}
