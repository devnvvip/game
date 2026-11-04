using System;
using UnityEngine;
using StreamerGame.Core;
using StreamerGame.Audio;
using StreamerGame.UI;

namespace StreamerGame.Livestream
{
    public class LivestreamManager : MonoBehaviour
    {
        public static LivestreamManager Instance { get; private set; }

        [Header("Livestream State")]
        public bool isStreaming = false;
        public float streamDuration = 0f;
        public int baseViewers = 3200;
        public int targetViewers = 3200;
        public int peakViewers = 0;
        public int totalLikesReceived = 0;
        public long totalSessionDonations = 0;

        private float commentTimer = 0f;
        private float questionTimer = 0f;
        private float incidentCooldown = 25f;

        public event Action OnStreamStarted;
        public event Action OnStreamEnded;
        public event Action<string, string[], Action<int>> OnInteractiveChoicePrompt;

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
            if (!isStreaming) return;

            streamDuration += Time.deltaTime;

            // Smoothly move viewer count toward targetViewers
            if (StatsManager.Instance != null)
            {
                int current = StatsManager.Instance.currentViewers;
                int diff = targetViewers - current;
                int step = Mathf.CeilToInt(diff * Time.deltaTime * 1.5f);
                StatsManager.Instance.SetViewers(current + step);

                if (StatsManager.Instance.currentViewers > peakViewers)
                {
                    peakViewers = StatsManager.Instance.currentViewers;
                }
            }

            // Continuous stream of comments
            commentTimer -= Time.deltaTime;
            if (commentTimer <= 0f)
            {
                commentTimer = UnityEngine.Random.Range(0.8f, 2.2f);
                if (LiveChatManager.Instance != null)
                {
                    LiveComment c = LiveChatManager.Instance.GenerateRandomComment();
                    LiveChatManager.Instance.PushComment(c);

                    // Add likes organically
                    int newLikes = UnityEngine.Random.Range(5, 30);
                    totalLikesReceived += newLikes;
                    StatsManager.Instance?.AddLikes(newLikes);

                    if (c.donationVND > 0)
                    {
                        totalSessionDonations += c.donationVND;
                        StatsManager.Instance?.AddRevenue(c.donationVND);
                        SoundManager.Instance?.PlayDonation();
                        HUDManager.Instance?.ShowNotification($"Nhận Donation: {c.donationVND:N0} VNĐ từ {c.username}!");
                    }
                }
            }

            // Spawn interactive fan question choices periodically
            questionTimer -= Time.deltaTime;
            if (questionTimer <= 0f && StreamIncidentManager.Instance != null && !StreamIncidentManager.Instance.isIncidentActive)
            {
                questionTimer = UnityEngine.Random.Range(18f, 28f);
                TriggerInteractiveQuestion();
            }

            // Periodic surprise incidents
            incidentCooldown -= Time.deltaTime;
            if (incidentCooldown <= 0f && StreamIncidentManager.Instance != null && !StreamIncidentManager.Instance.isIncidentActive)
            {
                incidentCooldown = UnityEngine.Random.Range(30f, 45f);
                StreamIncidentManager.Instance.TriggerRandomIncident();
            }
        }

        public void StartLivestream()
        {
            if (isStreaming) return;

            isStreaming = true;
            streamDuration = 0f;
            targetViewers = UnityEngine.Random.Range(2800, 4200);
            peakViewers = targetViewers;
            totalLikesReceived = 0;
            totalSessionDonations = 0;

            if (StatsManager.Instance != null)
            {
                StatsManager.Instance.SetViewers(targetViewers);
            }

            commentTimer = 0.5f;
            questionTimer = 10f;
            incidentCooldown = 22f;

            GameManager.Instance?.SetGameState(GameState.Livestreaming);
            OnStreamStarted?.Invoke();
            SoundManager.Instance?.PlayDonation();
            HUDManager.Instance?.ShowNotification("LIVESTREAM ĐÃ BẮT ĐẦU! Hàng ngàn khán giả đang theo dõi.");
        }

        public void EndLivestream()
        {
            if (!isStreaming) return;

            isStreaming = false;
            QuestManager.Instance?.CompleteQuest("q_end_stream");

            // Calculate followers gained based on stream performance
            int newFollowers = Mathf.RoundToInt((peakViewers * 0.15f) + (totalLikesReceived * 0.05f));
            StatsManager.Instance?.AddFollowers(newFollowers);
            StatsManager.Instance?.AddFame(500);

            OnStreamEnded?.Invoke();

            if (TimeOfDayManager.Instance != null)
            {
                TimeOfDayManager.Instance.SetPhase(DayPhase.Night);
            }

            GameManager.Instance?.SetGameState(GameState.SittingAtDesk);
            HUDManager.Instance?.ShowNotification($"Buổi Livestream kết thúc rực rỡ! +{newFollowers:N0} người theo dõi mới.");
        }

        public void BoostViewers(int count)
        {
            targetViewers += count;
        }

        public void DropViewers(int count)
        {
            targetViewers = Mathf.Max(500, targetViewers - count);
        }

        private void TriggerInteractiveQuestion()
        {
            string topic = "Fan muốn bạn chia sẻ: 'Động lực nào giúp anh theo đuổi đam mê idol streamer?'";
            string[] choices = new string[]
            {
                "A. 'Chính nụ cười và sự ủng hộ của các bạn là động lực lớn nhất!' (Ấm áp, +Followers)",
                "B. 'Đam mê được chia sẻ niềm vui và quậy hết mình cùng anh em!' (Hài hước, +Viewers)",
                "C. 'Để chứng minh rằng chỉ cần nỗ lực thì ước mơ sẽ thành hiện thực!' (Truyền cảm hứng, +Mood)"
            };

            OnInteractiveChoicePrompt?.Invoke(topic, choices, (chosenIdx) =>
            {
                QuestManager.Instance?.CompleteQuest("q_stream_interact");
                if (chosenIdx == 0)
                {
                    BoostViewers(800);
                    StatsManager.Instance?.AddFollowers(350);
                    StatsManager.Instance?.ModifyMood(15f);
                }
                else if (chosenIdx == 1)
                {
                    BoostViewers(1500);
                    StatsManager.Instance?.AddLikes(500);
                    StatsManager.Instance?.ModifyMood(10f);
                }
                else
                {
                    BoostViewers(1000);
                    StatsManager.Instance?.AddFame(150);
                    StatsManager.Instance?.ModifyMood(25f);
                }
            });
        }
    }
}
