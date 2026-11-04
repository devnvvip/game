using UnityEngine;
using StreamerGame.Audio;
using StreamerGame.UI;

namespace StreamerGame.Interactables
{
    public class BathroomSink : InteractableObject
    {
        public ParticleSystem waterParticle;

        private void Start()
        {
            promptText = "[E] Rửa mặt & Đánh răng";
            associatedQuestId = "q_bathroom";
            moodGain = 15f;
            energyCost = -10f; // Restores 10 energy
        }

        protected override void OnInteracted()
        {
            SoundManager.Instance?.PlayCoffee();
            HUDManager.Instance?.ShowNotification("Cảm giác thật sảng khoái! Khuôn mặt idol đã sẵn sàng.");
            if (waterParticle != null)
            {
                waterParticle.Play();
            }
        }
    }
}
