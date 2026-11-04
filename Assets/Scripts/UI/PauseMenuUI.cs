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

        [Header("Audio Controls")]
        public Button musicToggleButton;
        public Text musicToggleText;
        public Slider musicVolumeSlider;

        public Button sfxToggleButton;
        public Text sfxToggleText;
        public Slider sfxVolumeSlider;

        private void Start()
        {
            if (pauseMenuRoot != null) pauseMenuRoot.SetActive(false);

            if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
            if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);

            if (musicToggleButton != null) musicToggleButton.onClick.AddListener(OnMusicToggleClicked);
            if (sfxToggleButton != null) sfxToggleButton.onClick.AddListener(OnSfxToggleClicked);

            if (musicVolumeSlider != null)
            {
                if (SoundManager.Instance != null) musicVolumeSlider.value = SoundManager.Instance.MusicVolume;
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                if (SoundManager.Instance != null) sfxVolumeSlider.value = SoundManager.Instance.SfxVolume;
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            }

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.OnAudioSettingsChanged += UpdateAudioUI;
            }

            UpdateAudioUI();
        }

        private void OnDestroy()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.OnAudioSettingsChanged -= UpdateAudioUI;
            }
        }

        public void TogglePause()
        {
            if (pauseMenuRoot == null) return;

            bool willPause = !pauseMenuRoot.activeSelf;
            pauseMenuRoot.SetActive(willPause);

            if (willPause)
            {
                UpdateAudioUI();
            }

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

        private void OnMusicToggleClicked()
        {
            SoundManager.Instance?.ToggleMusic();
            SoundManager.Instance?.PlayClick();
        }

        private void OnSfxToggleClicked()
        {
            SoundManager.Instance?.ToggleSFX();
            SoundManager.Instance?.PlayClick();
        }

        private void OnMusicVolumeChanged(float val)
        {
            SoundManager.Instance?.SetMusicVolume(val);
        }

        private void OnSfxVolumeChanged(float val)
        {
            SoundManager.Instance?.SetSfxVolume(val);
        }

        private void UpdateAudioUI()
        {
            if (SoundManager.Instance == null) return;

            if (musicToggleText != null)
            {
                musicToggleText.text = SoundManager.Instance.IsMusicEnabled ? "Âm Nhạc: [BẬT]" : "Âm Nhạc: [TẮT]";
            }

            if (sfxToggleText != null)
            {
                sfxToggleText.text = SoundManager.Instance.IsSfxEnabled ? "Hiệu Ứng: [BẬT]" : "Hiệu Ứng: [TẮT]";
            }
        }
    }
}
