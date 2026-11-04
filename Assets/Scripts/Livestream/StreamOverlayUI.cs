using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using StreamerGame.Core;
using StreamerGame.Audio;

namespace StreamerGame.Livestream
{
    public class StreamOverlayUI : MonoBehaviour
    {
        public static StreamOverlayUI Instance { get; private set; }

        [Header("Root Panel")]
        public GameObject overlayRoot;

        [Header("Metrics")]
        public Text viewersText;
        public Text likesText;
        public Text durationText;
        public Text liveEarningsText;

        [Header("Live Chat")]
        public Transform chatContainer;
        public Text[] chatLineTexts; // Pre-pooled 8 chat lines
        private readonly Queue<string> chatHistory = new Queue<string>();

        [Header("Interactive Question Modal")]
        public GameObject questionModal;
        public Text questionTopicText;
        public Button choiceBtn1;
        public Text choiceText1;
        public Button choiceBtn2;
        public Text choiceText2;
        public Button choiceBtn3;
        public Text choiceText3;

        [Header("Incident Alert Banner")]
        public GameObject incidentBanner;
        public Text incidentTitleText;
        public Text incidentDescText;
        public Slider incidentTimerSlider;
        public Button incidentActionButton;
        public Text incidentActionBtnText;

        [Header("Controls")]
        public Button endStreamButton;

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
            if (overlayRoot != null) overlayRoot.SetActive(false);
            if (questionModal != null) questionModal.SetActive(false);
            if (incidentBanner != null) incidentBanner.SetActive(false);

            if (LivestreamManager.Instance != null)
            {
                LivestreamManager.Instance.OnStreamStarted += ShowOverlay;
                LivestreamManager.Instance.OnStreamEnded += HideOverlay;
                LivestreamManager.Instance.OnInteractiveChoicePrompt += OpenChoiceModal;
            }

            if (LiveChatManager.Instance != null)
            {
                LiveChatManager.Instance.OnNewCommentReceived += HandleNewComment;
            }

            if (StreamIncidentManager.Instance != null)
            {
                StreamIncidentManager.Instance.OnIncidentStarted += ShowIncidentBanner;
                StreamIncidentManager.Instance.OnIncidentResolved += OnIncidentResolved;
            }

            if (endStreamButton != null)
            {
                endStreamButton.onClick.AddListener(OnEndStreamClicked);
            }

            if (incidentActionButton != null)
            {
                incidentActionButton.onClick.AddListener(OnIncidentActionClicked);
            }
        }

        private void OnDestroy()
        {
            if (LivestreamManager.Instance != null)
            {
                LivestreamManager.Instance.OnStreamStarted -= ShowOverlay;
                LivestreamManager.Instance.OnStreamEnded -= HideOverlay;
                LivestreamManager.Instance.OnInteractiveChoicePrompt -= OpenChoiceModal;
            }
            if (LiveChatManager.Instance != null)
            {
                LiveChatManager.Instance.OnNewCommentReceived -= HandleNewComment;
            }
            if (StreamIncidentManager.Instance != null)
            {
                StreamIncidentManager.Instance.OnIncidentStarted -= ShowIncidentBanner;
                StreamIncidentManager.Instance.OnIncidentResolved -= OnIncidentResolved;
            }
        }

        private void Update()
        {
            if (LivestreamManager.Instance == null || !LivestreamManager.Instance.isStreaming) return;

            // Update stats
            if (viewersText != null && StatsManager.Instance != null)
            {
                viewersText.text = $"👁 {StatsManager.Instance.currentViewers:N0}";
            }

            if (likesText != null && StatsManager.Instance != null)
            {
                likesText.text = $"❤️ {StatsManager.Instance.currentLikes:N0}";
            }

            if (durationText != null)
            {
                TimeSpan t = TimeSpan.FromSeconds(LivestreamManager.Instance.streamDuration);
                durationText.text = $"⏱ {t.Minutes:D2}:{t.Seconds:D2}";
            }

            if (liveEarningsText != null)
            {
                liveEarningsText.text = $"⭐ +{LivestreamManager.Instance.totalSessionDonations:N0} đ";
            }

            // Update incident timer slider
            if (StreamIncidentManager.Instance != null && StreamIncidentManager.Instance.isIncidentActive && incidentTimerSlider != null)
            {
                if (StreamIncidentManager.Instance.currentIncident != null)
                {
                    incidentTimerSlider.value = StreamIncidentManager.Instance.incidentTimer / StreamIncidentManager.Instance.currentIncident.duration;
                }
            }
        }

        public void ShowOverlay()
        {
            if (overlayRoot != null) overlayRoot.SetActive(true);
            chatHistory.Clear();
            RefreshChatDisplay();
        }

        public void HideOverlay()
        {
            if (overlayRoot != null) overlayRoot.SetActive(false);
            if (questionModal != null) questionModal.SetActive(false);
            if (incidentBanner != null) incidentBanner.SetActive(false);
        }

        private void HandleNewComment(LiveComment c)
        {
            string colorHex = "#FFFFFF";
            if (c.type == CommentType.DonationRocket || c.type == CommentType.DonationSuperchat) colorHex = "#FFD700";
            else if (c.type == CommentType.Question) colorHex = "#00FFFF";
            else if (c.type == CommentType.Troll) colorHex = "#FF6B6B";

            string formatted = $"<color={colorHex}><b>{c.badge} {c.username}:</b></color> {c.message}";
            chatHistory.Enqueue(formatted);

            while (chatHistory.Count > (chatLineTexts != null ? chatLineTexts.Length : 8))
            {
                chatHistory.Dequeue();
            }

            RefreshChatDisplay();
        }

        private void RefreshChatDisplay()
        {
            if (chatLineTexts == null) return;

            string[] array = chatHistory.ToArray();
            for (int i = 0; i < chatLineTexts.Length; i++)
            {
                if (i < array.Length)
                {
                    chatLineTexts[i].text = array[i];
                    chatLineTexts[i].gameObject.SetActive(true);
                }
                else
                {
                    chatLineTexts[i].gameObject.SetActive(false);
                }
            }
        }

        private void OpenChoiceModal(string topic, string[] choices, Action<int> onSelected)
        {
            if (questionModal == null) return;

            questionModal.SetActive(true);
            if (questionTopicText != null) questionTopicText.text = topic;

            SetupChoiceButton(choiceBtn1, choiceText1, choices.Length > 0 ? choices[0] : "", () => {
                questionModal.SetActive(false);
                onSelected?.Invoke(0);
                SoundManager.Instance?.PlayClick();
            });

            SetupChoiceButton(choiceBtn2, choiceText2, choices.Length > 1 ? choices[1] : "", () => {
                questionModal.SetActive(false);
                onSelected?.Invoke(1);
                SoundManager.Instance?.PlayClick();
            });

            SetupChoiceButton(choiceBtn3, choiceText3, choices.Length > 2 ? choices[2] : "", () => {
                questionModal.SetActive(false);
                onSelected?.Invoke(2);
                SoundManager.Instance?.PlayClick();
            });
        }

        private void SetupChoiceButton(Button btn, Text txt, string label, Action onClick)
        {
            if (btn == null) return;

            if (string.IsNullOrEmpty(label))
            {
                btn.gameObject.SetActive(false);
                return;
            }

            btn.gameObject.SetActive(true);
            if (txt != null) txt.text = label;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClick());
        }

        private void ShowIncidentBanner(StreamIncident inc)
        {
            if (incidentBanner == null) return;

            incidentBanner.SetActive(true);
            if (incidentTitleText != null) incidentTitleText.text = inc.title;
            if (incidentDescText != null) incidentDescText.text = inc.description;
            if (incidentActionBtnText != null) incidentActionBtnText.text = inc.actionButtonText;
            if (incidentTimerSlider != null) incidentTimerSlider.value = 1f;
        }

        private void OnIncidentResolved(bool success)
        {
            if (incidentBanner != null) incidentBanner.SetActive(false);
        }

        private void OnIncidentActionClicked()
        {
            StreamIncidentManager.Instance?.ResolveIncident(true);
        }

        private void OnEndStreamClicked()
        {
            LivestreamManager.Instance?.EndLivestream();
        }
    }
}
