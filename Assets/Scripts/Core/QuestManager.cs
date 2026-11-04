using System;
using System.Collections.Generic;
using UnityEngine;

namespace StreamerGame.Core
{
    [System.Serializable]
    public class QuestItem
    {
        public string id;
        public string title;
        public string description;
        public bool isCompleted;
        public DayPhase phase;
        public int rewardFame;
        public float rewardMood;

        public QuestItem(string id, string title, string description, DayPhase phase, int fame = 50, float mood = 10f)
        {
            this.id = id;
            this.title = title;
            this.description = description;
            this.phase = phase;
            this.rewardFame = fame;
            this.rewardMood = mood;
            this.isCompleted = false;
        }
    }

    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        public List<QuestItem> allQuests = new List<QuestItem>();
        public int currentQuestIndex = 0;

        public event Action<QuestItem> OnQuestCompleted;
        public event Action<QuestItem> OnCurrentQuestUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeQuests();
        }

        private void Start()
        {
            UpdateCurrentActiveQuest();
        }

        public void InitializeQuests()
        {
            allQuests.Clear();

            // Morning
            allQuests.Add(new QuestItem("q_alarm", "Tắt chuông báo thức", "Thức dậy và nhấn nút tắt chuông báo thức bên cạnh giường.", DayPhase.Morning, 30, 5f));
            allQuests.Add(new QuestItem("q_bathroom", "Rửa mặt & Đánh răng", "Đến bồn rửa mặt trong phòng tắm để tỉnh táo đón ngày mới.", DayPhase.Morning, 40, 15f));
            allQuests.Add(new QuestItem("q_wardrobe", "Chọn trang phục Idol", "Đến tủ quần áo chọn một set đồ thật phong cách.", DayPhase.Morning, 50, 15f));
            allQuests.Add(new QuestItem("q_coffee", "Pha cà phê & Ăn sáng", "Đến bếp pha một ly espresso nạp năng lượng tràn đầy.", DayPhase.Morning, 50, 20f));
            allQuests.Add(new QuestItem("q_phone_morning", "Kiểm tra điện thoại & Tin nhắn fan", "Mở smartphone xem thông báo và tin nhắn từ FC.", DayPhase.Morning, 60, 15f));

            // Midday
            allQuests.Add(new QuestItem("q_tidy_room", "Dọn dẹp phòng Livestream", "Sắp xếp lại khu vực phòng thu streamer cho ngăn nắp.", DayPhase.Midday, 60, 10f));
            allQuests.Add(new QuestItem("q_check_gear", "Kiểm tra thiết bị & Bật Ring Light", "Kiểm tra webcam, micro và bật đèn studio sẵn sàng.", DayPhase.Midday, 80, 15f));
            allQuests.Add(new QuestItem("q_prep_script", "Lên kịch bản nội dung Stream", "Sử dụng máy tính để vạch sẵn chủ đề buổi live.", DayPhase.Midday, 90, 15f));
            allQuests.Add(new QuestItem("q_midday_snack", "Nghỉ ngơi & Nạp năng lượng", "Lấy đồ uống từ tủ lạnh để giữ năng lượng tốt nhất.", DayPhase.Midday, 50, 10f));

            // Afternoon
            allQuests.Add(new QuestItem("q_record_video", "Quay video ngắn hậu trường", "Mở camera quay một đoạn clip ngắn vui tươi.", DayPhase.Afternoon, 120, 20f));
            allQuests.Add(new QuestItem("q_selfie_post", "Chụp ảnh & Đăng mạng xã hội", "Chụp một bức selfie thật cuốn hút và đăng bài lên MXH.", DayPhase.Afternoon, 150, 25f));
            allQuests.Add(new QuestItem("q_check_social", "Kiểm tra tương tác & Phản hồi fan", "Xem phản ứng của fan hâm mộ trên bảng tin mạng xã hội.", DayPhase.Afternoon, 100, 15f));
            allQuests.Add(new QuestItem("q_vocal_prep", "Luyện giọng & Chọn chủ đề Live", "Chuẩn bị tinh thần và phong độ cao nhất cho buổi tối.", DayPhase.Afternoon, 110, 15f));

            // Evening (Livestream)
            allQuests.Add(new QuestItem("q_start_stream", "Ngồi vào bàn & Bắt đầu Livestream!", "Ngồi vào ghế gaming, khởi động buổi phát sóng trực tiếp.", DayPhase.Evening, 200, 30f));
            allQuests.Add(new QuestItem("q_stream_interact", "Tương tác & Trả lời bình luận fan", "Chọn các câu trả lời thú vị để giao lưu với khán giả.", DayPhase.Evening, 300, 35f));
            allQuests.Add(new QuestItem("q_stream_incidents", "Xử lý các sự cố bất ngờ", "Giải quyết trục trặc âm thanh, mạng và góc máy quay.", DayPhase.Evening, 350, 40f));
            allQuests.Add(new QuestItem("q_end_stream", "Chào khán giả & Kết thúc Livestream", "Cảm ơn người xem và tắt buổi phát sóng trực tiếp.", DayPhase.Evening, 250, 25f));

            // Night
            allQuests.Add(new QuestItem("q_stream_summary", "Xem bảng tổng kết thành tích Stream", "Kiểm tra doanh thu quà tặng và lượng follower mới.", DayPhase.Night, 150, 20f));
            allQuests.Add(new QuestItem("q_cleanup_desk", "Tắt thiết bị & Tắt đèn phòng", "Tắt PC và đèn ring light sau một buổi làm việc hăng say.", DayPhase.Night, 80, 10f));
            allQuests.Add(new QuestItem("q_night_phone", "Đọc tin nhắn chúc ngủ ngon từ fan", "Xem những lời yêu thương ấm áp từ cộng đồng fan.", DayPhase.Night, 100, 25f));
            allQuests.Add(new QuestItem("q_sleep", "Lên giường đi ngủ", "Nghỉ ngơi thật ngon để kết thúc một ngày ý nghĩa.", DayPhase.Night, 200, 50f));
        }

        public QuestItem GetCurrentQuest()
        {
            if (currentQuestIndex >= 0 && currentQuestIndex < allQuests.Count)
            {
                return allQuests[currentQuestIndex];
            }
            return null;
        }

        public void CompleteQuest(string questId)
        {
            QuestItem q = allQuests.Find(x => x.id == questId);
            if (q != null && !q.isCompleted)
            {
                q.isCompleted = true;
                StatsManager.Instance?.AddFame(q.rewardFame);
                StatsManager.Instance?.ModifyMood(q.rewardMood);
                Audio.SoundManager.Instance?.PlayNotification();

                OnQuestCompleted?.Invoke(q);

                CheckPhaseProgression();
                UpdateCurrentActiveQuest();
            }
        }

        private void CheckPhaseProgression()
        {
            if (TimeOfDayManager.Instance == null) return;

            DayPhase currentPhase = TimeOfDayManager.Instance.currentPhase;
            List<QuestItem> phaseQuests = allQuests.FindAll(x => x.phase == currentPhase);

            bool allPhaseDone = true;
            foreach (var pq in phaseQuests)
            {
                if (!pq.isCompleted)
                {
                    allPhaseDone = false;
                    break;
                }
            }

            if (allPhaseDone)
            {
                TimeOfDayManager.Instance.AdvancePhase();
            }
        }

        private void UpdateCurrentActiveQuest()
        {
            for (int i = 0; i < allQuests.Count; i++)
            {
                if (!allQuests[i].isCompleted)
                {
                    currentQuestIndex = i;
                    OnCurrentQuestUpdated?.Invoke(allQuests[i]);
                    return;
                }
            }

            OnCurrentQuestUpdated?.Invoke(null);
        }
    }
}
