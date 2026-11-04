using System;
using System.Collections.Generic;
using UnityEngine;

namespace StreamerGame.Livestream
{
    public class LiveChatManager : MonoBehaviour
    {
        public static LiveChatManager Instance { get; private set; }

        public event Action<LiveComment> OnNewCommentReceived;

        private readonly string[] fanNames = new string[]
        {
            "HaAnh_FC", "MinhKhoi_99", "BaoNgoc_Cute", "GiaHuy_Gamer", "PhuongThao_Idol",
            "TuanKiet_Pro", "ThuyTien_FanCung", "DucThinh_Vlog", "KhanhLinh_02", "QuangMinh_VN",
            "HuongGiang_Star", "HoangLong_Beat", "MaiAnh_Lovely", "ThanhTung_IdolFan", "YenNhi_Smile"
        };

        private readonly string[] fanNormalComments = new string[]
        {
            "Chào anh idol, tối nay đẹp trai quá ạ! <3",
            "Vừa tan học là vào xem live anh liền!",
            "Background phòng stream xịn xò quá anh ơi!",
            "Hôm nay stream game hay hát hò vậy anh?",
            "Anh ăn tối chưa đấy, giữ gìn sức khỏe nhé!",
            "Tóc mới nhìn cưng xỉu luôn anh ơi!",
            "Chúc anh một buổi livestream thật bùng nổ nha!",
            "Thả tim nhiệt tình cho anh nhà mình nè!",
            "Góc cam hôm nay đỉnh thật sự!",
            "Ánh sáng ring light làm da anh sáng lung linh luôn."
        };

        private readonly string[] interactiveQuestions = new string[]
        {
            "Fan hỏi: 'Bí quyết để anh luôn giữ được nguồn năng lượng tích cực mỗi ngày là gì vậy ạ?'",
            "Fan hỏi: 'Anh có thể bật mí một chút về dự án âm nhạc hoặc video sắp tới không anh ơi?'",
            "Fan hỏi: 'Tối nay anh có thể hát tặng cộng đồng fan một đoạn điệp khúc được không ạ?'",
            "Fan hỏi: 'Kỷ niệm đáng nhớ nhất từ khi anh bắt đầu làm streamer là gì ạ?'"
        };

        private readonly string[] trollComments = new string[]
        {
            "Ủa sao tiếng micro bị rè với giật thế anh?",
            "Hình như mạng bị drop fps rồi kìa idol ơi!",
            "Hôm nay nhìn mặt có vẻ hơi mệt mỏi đấy nha.",
            "Góc camera bị méo rồi chỉnh lại đi anh."
        };

        private readonly string[] superchatMessages = new string[]
        {
            "Gửi tặng anh cốc trà sữa full topping, chúc anh live vui!",
            "Ủng hộ idol 500k, chúc anh sớm đạt 1 triệu followers!",
            "Tặng idol quả tên lửa siêu to khổng lồ!",
            "Chúc buổi stream triệu view nha anh ơi!"
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public LiveComment GenerateRandomComment()
        {
            float roll = UnityEngine.Random.value;
            string user = fanNames[UnityEngine.Random.Range(0, fanNames.Length)];

            if (roll < 0.60f)
            {
                // Normal fan comment
                string msg = fanNormalComments[UnityEngine.Random.Range(0, fanNormalComments.Length)];
                return new LiveComment(user, msg, CommentType.FanNormal, 0, "[FAN]");
            }
            else if (roll < 0.78f)
            {
                // Question
                string msg = interactiveQuestions[UnityEngine.Random.Range(0, interactiveQuestions.Length)];
                return new LiveComment(user, msg, CommentType.Question, 0, "[VIP]");
            }
            else if (roll < 0.88f)
            {
                // Troll / Incident alert
                string msg = trollComments[UnityEngine.Random.Range(0, trollComments.Length)];
                return new LiveComment(user, msg, CommentType.Troll, 0, "[MEM]");
            }
            else
            {
                // Superchat / Gift
                long[] amounts = new long[] { 50000, 100000, 200000, 500000, 1000000 };
                long amt = amounts[UnityEngine.Random.Range(0, amounts.Length)];
                string msg = superchatMessages[UnityEngine.Random.Range(0, superchatMessages.Length)];
                CommentType cType = amt >= 500000 ? CommentType.DonationRocket : CommentType.DonationSuperchat;
                return new LiveComment(user, msg, cType, amt, "[TOP FAN]");
            }
        }

        public void PushComment(LiveComment comment)
        {
            OnNewCommentReceived?.Invoke(comment);
        }
    }
}
