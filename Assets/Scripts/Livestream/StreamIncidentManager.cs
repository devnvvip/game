using System;
using UnityEngine;
using StreamerGame.Core;
using StreamerGame.Audio;
using StreamerGame.UI;

namespace StreamerGame.Livestream
{
    public enum IncidentType
    {
        MicGlitch,
        NetworkLag,
        CameraDrift,
        TrollRaid,
        ViralRaid
    }

    [System.Serializable]
    public class StreamIncident
    {
        public IncidentType type;
        public string title;
        public string description;
        public string actionButtonText;
        public float duration; // seconds to resolve

        public StreamIncident(IncidentType type, string title, string description, string actionText, float duration = 12f)
        {
            this.type = type;
            this.title = title;
            this.description = description;
            this.actionButtonText = actionText;
            this.duration = duration;
        }
    }

    public class StreamIncidentManager : MonoBehaviour
    {
        public static StreamIncidentManager Instance { get; private set; }

        public StreamIncident currentIncident = null;
        public float incidentTimer = 0f;
        public bool isIncidentActive = false;

        public event Action<StreamIncident> OnIncidentStarted;
        public event Action<bool> OnIncidentResolved; // true = success, false = timed out

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (isIncidentActive && currentIncident != null)
            {
                incidentTimer -= Time.deltaTime;
                if (incidentTimer <= 0f)
                {
                    // Timed out, penalty!
                    ResolveIncident(false);
                }
            }
        }

        public void TriggerRandomIncident()
        {
            if (isIncidentActive) return;

            int pick = UnityEngine.Random.Range(0, 5);
            StreamIncident inc;

            switch (pick)
            {
                case 0:
                    inc = new StreamIncident(
                        IncidentType.MicGlitch,
                        "CẢNH BÁO: Âm thanh Micro bị rè!",
                        "Người xem đang phàn nàn tiếng ồn. Hãy hiệu chỉnh lại Audio Interface ngay!",
                        "Chỉnh Âm Thanh & Bật Khử Ồn",
                        12f);
                    break;
                case 1:
                    inc = new StreamIncident(
                        IncidentType.NetworkLag,
                        "CẢNH BÁO: Mạng bị drop bitrate!",
                        "Khung hình bị giật lag, lượng người xem đang giảm nhanh. Hãy chuyển sang kênh mạng dự phòng!",
                        "Kích Hoạt Đường Truyền Dự Phòng",
                        10f);
                    break;
                case 2:
                    inc = new StreamIncident(
                        IncidentType.CameraDrift,
                        "CẢNH BÁO: Góc Camera bị lệch!",
                        "Webcam bị trượt góc quay làm khuôn mặt bị khuất. Hãy căn chỉnh lại góc máy quay!",
                        "Căn Chỉnh Góc Camera 4K",
                        12f);
                    break;
                case 3:
                    inc = new StreamIncident(
                        IncidentType.TrollRaid,
                        "CHÚ Ý: Lượng bình luận spam ác ý tràn vào!",
                        "Một nhóm troll đang làm phiền livestream. Hãy kích hoạt bộ lọc bảo vệ phòng live!",
                        "Bật Chế Độ Lọc & Chặn Kẻ Quấy Rối",
                        14f);
                    break;
                default:
                    inc = new StreamIncident(
                        IncidentType.ViralRaid,
                        "BẤT NGỜ: Streamer bạn thân tổ chức Raid 5,000 người!",
                        "Cơ hội vàng để tăng follow! Hãy tương tác bùng nổ để chào đón khách quý!",
                        "Bật Nhạc Sôi Động & Chào Mừng Raid",
                        15f);
                    break;
            }

            currentIncident = inc;
            incidentTimer = inc.duration;
            isIncidentActive = true;

            SoundManager.Instance?.PlayAlert();
            OnIncidentStarted?.Invoke(inc);
        }

        public void ResolveIncident(bool success)
        {
            if (!isIncidentActive) return;

            isIncidentActive = false;

            if (success)
            {
                SoundManager.Instance?.PlayNotification();
                StatsManager.Instance?.AddFame(100);
                StatsManager.Instance?.ModifyMood(15f);
                QuestManager.Instance?.CompleteQuest("q_stream_incidents");

                if (currentIncident.type == IncidentType.ViralRaid)
                {
                    LivestreamManager.Instance?.BoostViewers(3500);
                    StatsManager.Instance?.AddFollowers(1200);
                }
                else
                {
                    LivestreamManager.Instance?.BoostViewers(500);
                }
            }
            else
            {
                // Failed/timed out
                StatsManager.Instance?.ModifyMood(-20f);
                LivestreamManager.Instance?.DropViewers(800);
            }

            OnIncidentResolved?.Invoke(success);
            currentIncident = null;
        }
    }
}
