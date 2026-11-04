using UnityEngine;
using UnityEngine.UI;
using StreamerGame.Core;
using StreamerGame.Audio;

namespace StreamerGame.UI
{
    public class PhoneUIController : MonoBehaviour
    {
        [Header("Phone Root")]
        public GameObject phoneRoot;
        public Text phoneClockText;

        [Header("Apps")]
        public GameObject messagesPanel;
        public GameObject socialPanel;
        public GameObject cameraPanel;
        public GameObject schedulePanel;

        [Header("Social App Elements")]
        public Text socialFollowersText;
        public Text socialStatusText;
        public Button postButton;

        [Header("Camera App Elements")]
        public Button shutterButton;
        public GameObject cameraFlashImage;

        [Header("Schedule App Elements")]
        public Text scheduleSummaryText;

        private void Start()
        {
            if (phoneRoot != null) phoneRoot.SetActive(false);

            if (postButton != null) postButton.onClick.AddListener(OnPostSocialClicked);
            if (shutterButton != null) shutterButton.onClick.AddListener(OnShutterClicked);
        }

        private void Update()
        {
            if (phoneRoot != null && phoneRoot.activeSelf)
            {
                if (phoneClockText != null)
                {
                    System.DateTime now = System.DateTime.Now;
                    phoneClockText.text = $"{now.Hour:D2}:{now.Minute:D2}";
                }
            }
        }

        public void TogglePhone()
        {
            if (phoneRoot == null) return;

            bool willOpen = !phoneRoot.activeSelf;
            phoneRoot.SetActive(willOpen);

            if (willOpen)
            {
                GameManager.Instance?.SetGameState(GameState.PhoneOpen);
                SoundManager.Instance?.PlayClick();
                OpenMessagesApp();
                QuestManager.Instance?.CompleteQuest("q_phone_morning");
                QuestManager.Instance?.CompleteQuest("q_night_phone");
            }
            else
            {
                GameManager.Instance?.SetGameState(GameState.Walking);
                SoundManager.Instance?.PlayClick();
            }
        }

        public void OpenMessagesApp()
        {
            HideAllApps();
            if (messagesPanel != null) messagesPanel.SetActive(true);
            SoundManager.Instance?.PlayClick();
        }

        public void OpenSocialApp()
        {
            HideAllApps();
            if (socialPanel != null)
            {
                socialPanel.SetActive(true);
                if (socialFollowersText != null && StatsManager.Instance != null)
                {
                    socialFollowersText.text = $"Followers: {StatsManager.Instance.followers:N0}";
                }
            }
            SoundManager.Instance?.PlayClick();
        }

        public void OpenCameraApp()
        {
            HideAllApps();
            if (cameraPanel != null) cameraPanel.SetActive(true);
            SoundManager.Instance?.PlayClick();
        }

        public void OpenScheduleApp()
        {
            HideAllApps();
            if (schedulePanel != null)
            {
                schedulePanel.SetActive(true);
                RefreshScheduleDisplay();
            }
            SoundManager.Instance?.PlayClick();
        }

        private void HideAllApps()
        {
            if (messagesPanel) messagesPanel.SetActive(false);
            if (socialPanel) socialPanel.SetActive(false);
            if (cameraPanel) cameraPanel.SetActive(false);
            if (schedulePanel) schedulePanel.SetActive(false);
        }

        private void OnPostSocialClicked()
        {
            SoundManager.Instance?.PlayNotification();
            StatsManager.Instance?.AddFollowers(1800);
            StatsManager.Instance?.ModifyMood(20f);
            StatsManager.Instance?.AddFame(120);

            if (socialStatusText != null)
            {
                socialStatusText.text = "Bài viết đã đăng tải thành công! Hàng nghìn lượt tim đang đổ về.";
            }

            QuestManager.Instance?.CompleteQuest("q_selfie_post");
            QuestManager.Instance?.CompleteQuest("q_check_social");
            HUDManager.Instance?.ShowNotification("Bài viết IdolGram đã lên xu hướng! (+1,800 Followers)");
        }

        private void OnShutterClicked()
        {
            SoundManager.Instance?.PlayShutter();
            if (cameraFlashImage != null)
            {
                StartCoroutine(CameraFlashRoutine());
            }

            QuestManager.Instance?.CompleteQuest("q_record_video");
            QuestManager.Instance?.CompleteQuest("q_vocal_prep");
            HUDManager.Instance?.ShowNotification("Đã ghi lại khoảnh khắc idol tuyệt đẹp! Sẵn sàng đăng tải.");
        }

        private System.Collections.IEnumerator CameraFlashRoutine()
        {
            cameraFlashImage.SetActive(true);
            yield return new WaitForSeconds(0.12f);
            cameraFlashImage.SetActive(false);
        }

        private void RefreshScheduleDisplay()
        {
            if (scheduleSummaryText == null || QuestManager.Instance == null) return;

            string s = "<b>LỊCH TRÌNH HOẠT ĐỘNG TRONG NGÀY:</b>\n\n";
            foreach (var q in QuestManager.Instance.allQuests)
            {
                string status = q.isCompleted ? "<color=#00FF88>[XONG]</color>" : "<color=#FFAA00>[CHỜ]</color>";
                s += $"{status} {q.title} - {q.description}\n";
            }
            scheduleSummaryText.text = s;
        }
    }
}
