using UnityEngine;
using StreamerGame.Audio;
using StreamerGame.Core;
using StreamerGame.UI;

namespace StreamerGame.Interactables
{
    public class CoffeeMachine : InteractableObject
    {
        private void Start()
        {
            promptText = "[E] Pha Cà Phê Espresso & Bữa Sáng";
            associatedQuestId = "q_coffee";
            energyCost = -30f; // gives 30 energy
            moodGain = 20f;
        }

        protected override void OnInteracted()
        {
            SoundManager.Instance?.PlayCoffee();
            HUDManager.Instance?.ShowNotification("Đã pha 1 ly Iced Americano đậm vị! (+30 Năng lượng, +20 Tâm trạng)");
            StatsManager.Instance.hasCoffee = true;
            StatsManager.Instance.hasEaten = true;
        }
    }
}
