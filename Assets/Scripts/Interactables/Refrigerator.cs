using UnityEngine;
using StreamerGame.Audio;
using StreamerGame.UI;

namespace StreamerGame.Interactables
{
    public class Refrigerator : InteractableObject
    {
        private void Start()
        {
            promptText = "[E] Mở tủ lạnh lấy nước tăng lực & đồ ăn nhẹ";
            associatedQuestId = "q_midday_snack";
            energyCost = -20f;
            moodGain = 10f;
        }

        protected override void OnInteracted()
        {
            SoundManager.Instance?.PlayClick();
            HUDManager.Instance?.ShowNotification("Đã dùng nước ép hoa quả & snack protein! (+Năng lượng)");
        }
    }
}
