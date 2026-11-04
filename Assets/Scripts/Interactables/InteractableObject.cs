using UnityEngine;
using StreamerGame.Core;
using StreamerGame.Audio;

namespace StreamerGame.Interactables
{
    public class InteractableObject : MonoBehaviour, IInteractable
    {
        [Header("Interaction Setup")]
        [SerializeField] protected string promptText = "Tương tác";
        [SerializeField] protected string associatedQuestId = "";
        [SerializeField] protected float energyCost = 0f;
        [SerializeField] protected float moodGain = 5f;
        [SerializeField] protected bool singleUse = false;

        protected bool hasBeenUsed = false;

        public virtual string PromptText => promptText;

        public virtual bool CanInteract => !singleUse || !hasBeenUsed;

        public virtual void Interact()
        {
            if (!CanInteract) return;

            hasBeenUsed = true;

            if (energyCost != 0) StatsManager.Instance?.ModifyEnergy(-energyCost);
            if (moodGain != 0) StatsManager.Instance?.ModifyMood(moodGain);

            if (!string.IsNullOrEmpty(associatedQuestId))
            {
                QuestManager.Instance?.CompleteQuest(associatedQuestId);
            }

            SoundManager.Instance?.PlayClick();
            OnInteracted();
        }

        protected virtual void OnInteracted()
        {
            // Override in derived classes
        }
    }
}
