using UnityEngine;
using UnityEngine.UI;
using StreamerGame.Core;
using StreamerGame.Livestream;
using StreamerGame.Audio;

namespace StreamerGame.UI
{
    public class DaySummaryUI : MonoBehaviour
    {
        public static DaySummaryUI Instance { get; private set; }

        public GameObject summaryRoot;
        public Text rankText;
        public Text peakViewersText;
        public Text followersGainedText;
        public Text sessionEarningsText;
        public Text totalFameText;
        public Text managerCommentText;
        public Button playAgainButton;

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
            if (summaryRoot != null) summaryRoot.SetActive(false);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            }

            if (playAgainButton != null)
            {
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.DaySummary)
            {
                ShowSummary();
            }
        }

        public void ShowSummary()
        {
            if (summaryRoot != null) summaryRoot.SetActive(true);

            SoundManager.Instance?.PlayDonation();

            int peak = LivestreamManager.Instance != null ? LivestreamManager.Instance.peakViewers : 5000;
            long earnings = LivestreamManager.Instance != null ? LivestreamManager.Instance.totalSessionDonations : 2500000;
            int followersGained = Mathf.RoundToInt((peak * 0.15f) + 1800);

            if (peakViewersText != null) peakViewersText.text = $"Lượng xem đỉnh cao: {peak:N0} người xem";
            if (followersGainedText != null) followersGainedText.text = $"Lượng fan mới theo dõi: +{followersGained:N0} followers";
            if (sessionEarningsText != null) sessionEarningsText.text = $"Thu nhập quà tặng buổi live: +{earnings:N0} VNĐ";
            if (totalFameText != null && StatsManager.Instance != null) totalFameText.text = $"Độ nổi tiếng hiện tại: {StatsManager.Instance.fame:N0} Điểm";

            // Determine Rank
            string rank = "HẠNG S (XUẤT SẮC)";
            string comment = "Quản lý nhận xét: 'Một ngày làm việc hoàn hảo! Bạn xử lý các sự cố trên livestream cực kỳ thông minh, hình tượng idol ngày càng tỏa sáng!'";

            if (peak < 3000)
            {
                rank = "HẠNG B (ĐẠT YÊU CẦU)";
                comment = "Quản lý nhận xét: 'Buổi live khá ổn nhưng cần giao lưu năng động hơn với khán giả để bùng nổ view!'";
            }
            else if (peak < 5000)
            {
                rank = "HẠNG A (RẤT TỐT)";
                comment = "Quản lý nhận xét: 'Phong độ rất vững vàng! Các nhà tài trợ đang rất hài lòng về chỉ số tương tác.'";
            }

            if (rankText != null) rankText.text = rank;
            if (managerCommentText != null) managerCommentText.text = comment;
        }

        private void OnPlayAgainClicked()
        {
            SoundManager.Instance?.PlayClick();
            GameManager.Instance?.RestartDay();
        }
    }
}
