using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using StreamerGame.Core;

namespace StreamerGame.UI
{
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }

        [Header("Top Status Bar")]
        public Text phaseText;
        public Slider energySlider;
        public Text energyText;
        public Slider moodSlider;
        public Text moodText;
        public Text fameText;
        public Text followersText;
        public Text revenueText;

        [Header("Quest Card")]
        public GameObject questContainer;
        public Text questTitleText;
        public Text questDescText;

        [Header("Interaction Reticle")]
        public GameObject reticleDot;
        public GameObject interactionPromptContainer;
        public Text interactionPromptText;

        [Header("Notifications")]
        public GameObject notificationBanner;
        public Text notificationText;

        [Header("Phone & Panels")]
        public PhoneUIController phoneController;
        public PauseMenuUI pauseMenuController;

        private Coroutine notificationCoroutine;

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
            if (StatsManager.Instance != null)
            {
                StatsManager.Instance.OnStatsUpdated += UpdateHUDStats;
                UpdateHUDStats();
            }

            if (TimeOfDayManager.Instance != null)
            {
                TimeOfDayManager.Instance.OnPhaseChanged += OnPhaseChanged;
                OnPhaseChanged(TimeOfDayManager.Instance.currentPhase);
            }

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnCurrentQuestUpdated += OnQuestUpdated;
                OnQuestUpdated(QuestManager.Instance.GetCurrentQuest());
            }

            HideInteractionPrompt();
            if (notificationBanner != null) notificationBanner.SetActive(false);
        }

        private void OnDestroy()
        {
            if (StatsManager.Instance != null) StatsManager.Instance.OnStatsUpdated -= UpdateHUDStats;
            if (TimeOfDayManager.Instance != null) TimeOfDayManager.Instance.OnPhaseChanged -= OnPhaseChanged;
            if (QuestManager.Instance != null) QuestManager.Instance.OnCurrentQuestUpdated -= OnQuestUpdated;
        }

        public void UpdateHUDStats()
        {
            if (StatsManager.Instance == null) return;

            if (energySlider != null) energySlider.value = StatsManager.Instance.energy / 100f;
            if (energyText != null) energyText.text = $"{Mathf.RoundToInt(StatsManager.Instance.energy)}%";

            if (moodSlider != null) moodSlider.value = StatsManager.Instance.mood / 100f;
            if (moodText != null) moodText.text = $"{Mathf.RoundToInt(StatsManager.Instance.mood)}%";

            if (fameText != null) fameText.text = $"★ {StatsManager.Instance.fame:N0}";
            if (followersText != null) followersText.text = $"♥ {StatsManager.Instance.followers:N0}";
            if (revenueText != null) revenueText.text = $"{StatsManager.Instance.revenue:N0} đ";
        }

        private void OnPhaseChanged(DayPhase phase)
        {
            if (phaseText != null && TimeOfDayManager.Instance != null)
            {
                phaseText.text = TimeOfDayManager.Instance.GetPhaseName(phase);
            }
        }

        private void OnQuestUpdated(QuestItem quest)
        {
            if (questContainer == null) return;

            if (quest != null)
            {
                questContainer.SetActive(true);
                if (questTitleText != null) questTitleText.text = $"[NHIỆM VỤ] {quest.title}";
                if (questDescText != null) questDescText.text = quest.description;
            }
            else
            {
                if (questTitleText != null) questTitleText.text = "Đã hoàn thành mọi hoạt động!";
                if (questDescText != null) questDescText.text = "Bạn có thể đi ngủ để kết thúc ngày.";
            }
        }

        public void ShowInteractionPrompt(string text)
        {
            if (interactionPromptContainer != null)
            {
                interactionPromptContainer.SetActive(true);
                if (interactionPromptText != null) interactionPromptText.text = text;
            }
        }

        public void HideInteractionPrompt()
        {
            if (interactionPromptContainer != null)
            {
                interactionPromptContainer.SetActive(false);
            }
        }

        public void ShowNotification(string message, float duration = 3.5f)
        {
            if (notificationCoroutine != null) StopCoroutine(notificationCoroutine);
            notificationCoroutine = StartCoroutine(NotificationRoutine(message, duration));
        }

        private IEnumerator NotificationRoutine(string message, float duration)
        {
            if (notificationBanner != null && notificationText != null)
            {
                notificationText.text = message;
                notificationBanner.SetActive(true);
                yield return new WaitForSeconds(duration);
                notificationBanner.SetActive(false);
            }
        }

        public void TogglePhone()
        {
            if (phoneController != null)
            {
                phoneController.TogglePhone();
            }
        }

        public void TogglePause()
        {
            if (pauseMenuController != null)
            {
                pauseMenuController.TogglePause();
            }
        }
    }
}
