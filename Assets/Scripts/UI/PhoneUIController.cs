using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using StreamerGame.Core;
using StreamerGame.Audio;

namespace StreamerGame.UI
{
    public class PhoneUIController : MonoBehaviour
    {
        [Header("Phone Structure")]
        public GameObject phoneRoot;
        public RectTransform phoneFrameRect;
        public Text phoneClockText;
        public Text batteryPercentText;

        [Header("Screens & Navigation")]
        public GameObject homeScreenPanel;
        public Button homeIndicatorButton;

        [Header("Apps Grid Buttons")]
        public Button appBtnMessages;
        public Button appBtnSocial;
        public Button appBtnCamera;
        public Button appBtnSchedule;
        public Button appBtnPhotos;
        public Button appBtnNotes;

        [Header("App Panels")]
        public GameObject messagesPanel;
        public GameObject socialPanel;
        public GameObject cameraPanel;
        public GameObject schedulePanel;
        public GameObject photosPanel;
        public GameObject notesPanel;

        [Header("Social App Elements")]
        public Text socialFollowersText;
        public Text socialStatusText;
        public Button postButton;

        [Header("Camera App Elements")]
        public Button shutterButton;
        public GameObject cameraFlashImage;

        [Header("Schedule App Elements")]
        public Text scheduleSummaryText;

        private Coroutine phoneAnimCoroutine;
        private bool isAnimating = false;

        private void Start()
        {
            if (phoneRoot != null) phoneRoot.SetActive(false);

            if (homeIndicatorButton != null) homeIndicatorButton.onClick.AddListener(OnHomeIndicatorClicked);

            if (appBtnMessages != null) appBtnMessages.onClick.AddListener(OpenMessagesApp);
            if (appBtnSocial != null) appBtnSocial.onClick.AddListener(OpenSocialApp);
            if (appBtnCamera != null) appBtnCamera.onClick.AddListener(OpenCameraApp);
            if (appBtnSchedule != null) appBtnSchedule.onClick.AddListener(OpenScheduleApp);
            if (appBtnPhotos != null) appBtnPhotos.onClick.AddListener(OpenPhotosApp);
            if (appBtnNotes != null) appBtnNotes.onClick.AddListener(OpenNotesApp);

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
            if (phoneRoot == null || isAnimating) return;

            bool willOpen = !phoneRoot.activeSelf;

            if (willOpen)
            {
                phoneRoot.SetActive(true);
                GameManager.Instance?.SetGameState(GameState.PhoneOpen);
                SoundManager.Instance?.PlayClick();

                QuestManager.Instance?.CompleteQuest("q_phone_morning");
                QuestManager.Instance?.CompleteQuest("q_night_phone");

                ShowHomeScreen();

                if (phoneAnimCoroutine != null) StopCoroutine(phoneAnimCoroutine);
                phoneAnimCoroutine = StartCoroutine(AnimatePhoneSlide(true));
            }
            else
            {
                SoundManager.Instance?.PlayClick();
                if (phoneAnimCoroutine != null) StopCoroutine(phoneAnimCoroutine);
                phoneAnimCoroutine = StartCoroutine(AnimatePhoneSlide(false));
            }
        }

        private IEnumerator AnimatePhoneSlide(bool slideUp)
        {
            isAnimating = true;

            if (phoneFrameRect != null)
            {
                Vector2 startPos = slideUp ? new Vector2(0f, -900f) : Vector2.zero;
                Vector2 targetPos = slideUp ? Vector2.zero : new Vector2(0f, -900f);
                float timer = 0f;
                float duration = 0.28f;

                phoneFrameRect.anchoredPosition = startPos;

                while (timer < duration)
                {
                    timer += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(timer / duration);
                    // Smooth ease-out quad
                    float ease = 1f - (1f - t) * (1f - t);
                    phoneFrameRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, ease);
                    yield return null;
                }

                phoneFrameRect.anchoredPosition = targetPos;
            }

            if (!slideUp)
            {
                phoneRoot.SetActive(false);
                GameManager.Instance?.SetGameState(GameState.Walking);
            }

            isAnimating = false;
        }

        public void ShowHomeScreen()
        {
            HideAllApps();
            if (homeScreenPanel != null) homeScreenPanel.SetActive(true);
        }

        private void OnHomeIndicatorClicked()
        {
            SoundManager.Instance?.PlayClick();

            // If an app is open, minimize back to Home Screen
            if (IsAnyAppOpen())
            {
                ShowHomeScreen();
            }
            else
            {
                // If already on Home Screen, swipe down to close phone
                TogglePhone();
            }
        }

        private bool IsAnyAppOpen()
        {
            return (messagesPanel != null && messagesPanel.activeSelf) ||
                   (socialPanel != null && socialPanel.activeSelf) ||
                   (cameraPanel != null && cameraPanel.activeSelf) ||
                   (schedulePanel != null && schedulePanel.activeSelf) ||
                   (photosPanel != null && photosPanel.activeSelf) ||
                   (notesPanel != null && notesPanel.activeSelf);
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
                UpdateScheduleSummary();
            }
            SoundManager.Instance?.PlayClick();
        }

        public void OpenPhotosApp()
        {
            HideAllApps();
            if (photosPanel != null) photosPanel.SetActive(true);
            SoundManager.Instance?.PlayClick();
        }

        public void OpenNotesApp()
        {
            HideAllApps();
            if (notesPanel != null) notesPanel.SetActive(true);
            SoundManager.Instance?.PlayClick();
        }

        private void HideAllApps()
        {
            if (homeScreenPanel != null) homeScreenPanel.SetActive(false);
            if (messagesPanel != null) messagesPanel.SetActive(false);
            if (socialPanel != null) socialPanel.SetActive(false);
            if (cameraPanel != null) cameraPanel.SetActive(false);
            if (schedulePanel != null) schedulePanel.SetActive(false);
            if (photosPanel != null) photosPanel.SetActive(false);
            if (notesPanel != null) notesPanel.SetActive(false);
        }

        private void OnPostSocialClicked()
        {
            SoundManager.Instance?.PlayNotification();
            StatsManager.Instance?.AddFame(50);
            StatsManager.Instance?.AddFollowers(1200);
            StatsManager.Instance?.ModifyMood(15f);

            QuestManager.Instance?.CompleteQuest("q_social_post");

            if (socialStatusText != null)
            {
                socialStatusText.text = "★ BÀI VIẾT ĐÃ ĐĂNG THÀNH CÔNG!\nNhận được 12,400 lượt thả tim & bão bình luận!";
            }
            if (socialFollowersText != null && StatsManager.Instance != null)
            {
                socialFollowersText.text = $"Followers: {StatsManager.Instance.followers:N0}";
            }

            HUDManager.Instance?.ShowNotification("Đã đăng bài lên mạng xã hội! Fan tương tác bùng nổ (+1,200 Fan)");
        }

        private void OnShutterClicked()
        {
            SoundManager.Instance?.PlayShutter();
            QuestManager.Instance?.CompleteQuest("q_record_short");
            HUDManager.Instance?.ShowNotification("Đã quay xong video ngắn cho kênh Idol! Đạt triệu view.");

            if (cameraFlashImage != null)
            {
                StartCoroutine(CameraFlashRoutine());
            }
        }

        private IEnumerator CameraFlashRoutine()
        {
            cameraFlashImage.SetActive(true);
            yield return new WaitForSecondsRealtime(0.08f);
            cameraFlashImage.SetActive(false);
        }

        private void UpdateScheduleSummary()
        {
            if (scheduleSummaryText == null) return;
            DayPhase phase = TimeOfDayManager.Instance != null ? TimeOfDayManager.Instance.currentPhase : DayPhase.Morning;
            string schedule = "<b>LỊCH TRÌNH IDOL HÔM NAY:</b>\n\n";

            schedule += phase == DayPhase.Morning ? "● <b>[07:00 - 11:30] BUỔI SÁNG:</b> (Đang diễn ra)\nThức dậy, vệ sinh diện mạo, ăn sáng, duyệt tin nhắn FC.\n\n" : "✓ [07:00 - 11:30] BUỔI SÁNG: Hoàn thành\n\n";
            schedule += phase == DayPhase.Midday ? "● <b>[12:00 - 14:30] BUỔI TRƯA:</b> (Đang diễn ra)\nDọn studio, test PC, micro, camera, ring light, kịch bản live.\n\n" : (phase > DayPhase.Midday ? "✓ [12:00 - 14:30] BUỔI TRƯA: Hoàn thành\n\n" : "○ [12:00 - 14:30] BUỔI TRƯA: Chuẩn bị thiết bị studio\n\n");
            schedule += phase == DayPhase.Afternoon ? "● <b>[15:00 - 18:30] BUỔI CHIỀU:</b> (Đang diễn ra)\nQuay video ngắn, selfie mạng xã hội, kiểm tra trending.\n\n" : (phase > DayPhase.Afternoon ? "✓ [15:00 - 18:30] BUỔI CHIỀU: Hoàn thành\n\n" : "○ [15:00 - 18:30] BUỔI CHIỀU: Sản xuất nội dung\n\n");
            schedule += phase == DayPhase.Evening ? "● <b>[19:30 - 22:30] BUỔI TỐI:</b> (Đang diễn ra)\nLên sóng LIVESTREAM chính thức! Giữ chân người xem & tương tác.\n\n" : (phase > DayPhase.Evening ? "✓ [19:30 - 22:30] BUỔI TỐI: Hoàn thành\n\n" : "○ [19:30 - 22:30] BUỔI TỐI: Livestream bùng nổ\n\n");
            schedule += phase == DayPhase.Night ? "● <b>[23:00+] ĐÊM MUỘN:</b> (Đang diễn ra)\nTổng kết doanh thu, tắt hệ thống PC, đi ngủ nạp năng lượng.\n" : "○ [23:00+] ĐÊM MUỘN: Nghỉ ngơi & tổng kết ngày\n";

            scheduleSummaryText.text = schedule;
        }
    }
}
