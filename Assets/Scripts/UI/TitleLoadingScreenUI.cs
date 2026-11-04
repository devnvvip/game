using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using StreamerGame.Audio;

namespace StreamerGame.UI
{
    public class TitleLoadingScreenUI : MonoBehaviour
    {
        public static TitleLoadingScreenUI Instance { get; private set; }

        public CanvasGroup canvasGroup;
        public Text titleText;
        public Text subtitleText;
        public Button startButton;
        public Slider loadingProgressBar;

        private bool hasStarted = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Start()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartClicked);
            }
            StartCoroutine(AutoTransitionRoutine());
        }

        private IEnumerator AutoTransitionRoutine()
        {
            float timer = 0f;
            float duration = 2.2f;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                if (loadingProgressBar != null)
                {
                    loadingProgressBar.value = Mathf.Clamp01(timer / duration);
                }
                yield return null;
            }

            if (!hasStarted)
            {
                OnStartClicked();
            }
        }

        public void OnStartClicked()
        {
            if (hasStarted) return;
            hasStarted = true;

            SoundManager.Instance?.PlayNotification();
            StartCoroutine(FadeOutRoutine());
        }

        private IEnumerator FadeOutRoutine()
        {
            if (canvasGroup == null)
            {
                gameObject.SetActive(false);
                yield break;
            }

            float t = 1f;
            while (t > 0f)
            {
                t -= Time.unscaledDeltaTime * 2.5f;
                canvasGroup.alpha = Mathf.Clamp01(t);
                yield return null;
            }

            gameObject.SetActive(false);
        }
    }
}
