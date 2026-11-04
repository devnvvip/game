using UnityEngine;
using UnityEngine.UI;
using StreamerGame.Core;
using StreamerGame.Audio;

namespace StreamerGame.UI
{
    public class PauseMenuUI : MonoBehaviour
    {
        public GameObject pauseMenuRoot;
        public Button resumeButton;
        public Button restartButton;
        public Slider volumeSlider;
        public Text volumeValueText;

        private void Start()
        {
            if (pauseMenuRoot != null) pauseMenuRoot.SetActive(false);

            if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
            if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
            if (volumeSlider != null)
            {
                volumeSlider.value = AudioListener.volume;
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }
        }

        public void TogglePause()
        {
            if (pauseMenuRoot == null) return;

            bool willPause = !pauseMenuRoot.activeSelf;
            pauseMenuRoot.SetActive(willPause);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.PauseGame(willPause);
            }
        }

        private void OnResumeClicked()
        {
            SoundManager.Instance?.PlayClick();
            TogglePause();
        }

        private void OnRestartClicked()
        {
            SoundManager.Instance?.PlayClick();
            Time.timeScale = 1f;
            GameManager.Instance?.RestartDay();
        }

        private void OnVolumeChanged(float val)
        {
            AudioListener.volume = val;
            if (volumeValueText != null) volumeValueText.text = $"{Mathf.RoundToInt(val * 100)}%";
        }
    }
}
