using UnityEngine;
using StreamerGame.Core;
using StreamerGame.UI;

namespace StreamerGame.Interactables
{
    public class Wardrobe : InteractableObject
    {
        private int outfitIndex = 0;
        private readonly string[] outfits = new string[]
        {
            "Set Đồ Hoodie Năng Động (Trendy Streetwear)",
            "Áo Sơ Mi Idol Lịch Lãm (Smart Chic)",
            "Trang Phục Livestream Nổi Bật (Cyber Glam)"
        };

        private void Start()
        {
            promptText = "[E] Thay đổi trang phục Idol";
            associatedQuestId = "q_wardrobe";
            moodGain = 20f;
        }

        protected override void OnInteracted()
        {
            outfitIndex = (outfitIndex + 1) % outfits.Length;
            string selected = outfits[outfitIndex];
            StatsManager.Instance?.SetOutfit(selected);
            HUDManager.Instance?.ShowNotification($"Đã đổi sang: {selected} (+Mood)");
        }
    }
}
