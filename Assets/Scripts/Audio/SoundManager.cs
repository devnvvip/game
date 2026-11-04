using UnityEngine;

namespace StreamerGame.Audio
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        private const string PrefKeyMusic = "IdolSound_MusicEnabled";
        private const string PrefKeySfx = "IdolSound_SfxEnabled";
        private const string PrefKeyVolMusic = "IdolSound_MusicVolume";
        private const string PrefKeyVolSfx = "IdolSound_SfxVolume";

        private AudioSource sfxSource;
        private AudioSource ambientSource;
        private AudioSource alarmSource;

        private AudioClip clickClip;
        private AudioClip chimeClip;
        private AudioClip alarmClip;
        private AudioClip shutterClip;
        private AudioClip coffeeClip;
        private AudioClip donationClip;
        private AudioClip alertClip;
        private AudioClip roomToneClip;

        public bool IsMusicEnabled { get; private set; } = true;
        public bool IsSfxEnabled { get; private set; } = true;
        public float MusicVolume { get; private set; } = 0.35f;
        public float SfxVolume { get; private set; } = 0.5f;

        public event System.Action OnAudioSettingsChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadPreferences();

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            ambientSource.loop = true;

            alarmSource = gameObject.AddComponent<AudioSource>();
            alarmSource.playOnAwake = false;
            alarmSource.loop = true;

            GenerateProceduralAudioClips();
            ApplyAudioSettings();
        }

        private void Start()
        {
            // Start quiet subtle room tone
            if (IsMusicEnabled && ambientSource != null && roomToneClip != null)
            {
                ambientSource.clip = roomToneClip;
                ambientSource.Play();
            }
        }

        private void LoadPreferences()
        {
            IsMusicEnabled = PlayerPrefs.GetInt(PrefKeyMusic, 1) == 1;
            IsSfxEnabled = PlayerPrefs.GetInt(PrefKeySfx, 1) == 1;
            MusicVolume = PlayerPrefs.GetFloat(PrefKeyVolMusic, 0.35f);
            SfxVolume = PlayerPrefs.GetFloat(PrefKeyVolSfx, 0.5f);
        }

        private void SavePreferences()
        {
            PlayerPrefs.SetInt(PrefKeyMusic, IsMusicEnabled ? 1 : 0);
            PlayerPrefs.SetInt(PrefKeySfx, IsSfxEnabled ? 1 : 0);
            PlayerPrefs.SetFloat(PrefKeyVolMusic, MusicVolume);
            PlayerPrefs.SetFloat(PrefKeyVolSfx, SfxVolume);
            PlayerPrefs.Save();
        }

        public void ToggleMusic()
        {
            IsMusicEnabled = !IsMusicEnabled;
            ApplyAudioSettings();
            SavePreferences();
            OnAudioSettingsChanged?.Invoke();
        }

        public void ToggleSFX()
        {
            IsSfxEnabled = !IsSfxEnabled;
            ApplyAudioSettings();
            SavePreferences();
            OnAudioSettingsChanged?.Invoke();
        }

        public void SetMusicVolume(float vol)
        {
            MusicVolume = Mathf.Clamp01(vol);
            ApplyAudioSettings();
            SavePreferences();
            OnAudioSettingsChanged?.Invoke();
        }

        public void SetSfxVolume(float vol)
        {
            SfxVolume = Mathf.Clamp01(vol);
            ApplyAudioSettings();
            SavePreferences();
            OnAudioSettingsChanged?.Invoke();
        }

        private void ApplyAudioSettings()
        {
            if (ambientSource != null)
            {
                ambientSource.volume = IsMusicEnabled ? MusicVolume * 0.4f : 0f;
                if (!IsMusicEnabled && ambientSource.isPlaying)
                {
                    ambientSource.Pause();
                }
                else if (IsMusicEnabled && !ambientSource.isPlaying && roomToneClip != null)
                {
                    ambientSource.Play();
                }
            }

            if (alarmSource != null)
            {
                alarmSource.volume = IsSfxEnabled ? SfxVolume * 0.45f : 0f;
            }
        }

        public void PlayClick() => PlayClip(clickClip, 0.18f);
        public void PlayNotification() => PlayClip(chimeClip, 0.22f);
        public void PlayShutter() => PlayClip(shutterClip, 0.25f);
        public void PlayCoffee() => PlayClip(coffeeClip, 0.20f);
        public void PlayDonation() => PlayClip(donationClip, 0.28f);
        public void PlayAlert() => PlayClip(alertClip, 0.25f);

        public void StartAlarm()
        {
            if (!IsSfxEnabled) return;
            if (alarmSource != null && alarmClip != null)
            {
                alarmSource.clip = alarmClip;
                alarmSource.volume = SfxVolume * 0.4f;
                if (!alarmSource.isPlaying) alarmSource.Play();
            }
        }

        public void StopAlarm()
        {
            if (alarmSource != null && alarmSource.isPlaying)
            {
                alarmSource.Stop();
            }
        }

        private void PlayClip(AudioClip clip, float baseVol = 0.5f)
        {
            if (!IsSfxEnabled || sfxSource == null || clip == null) return;
            sfxSource.PlayOneShot(clip, baseVol * SfxVolume);
        }

        private void GenerateProceduralAudioClips()
        {
            // 1. Soft UI Tap / Click (low wooden click, organic pop, 25ms)
            clickClip = CreateSoftClickClip("SoftClick", 0.025f);

            // 2. Notification Chime (warm marimba-like bell, 200ms)
            chimeClip = CreateWarmChimeClip("WarmChime", new float[] { 587.33f, 880f }, 0.20f);

            // 3. Gentle Digital Alarm (pleasant modern polyphonic ringtone, 1.2s)
            alarmClip = CreateGentleAlarmClip("GentleAlarm", 1.2f);

            // 4. Camera Shutter (mechanical double tap, 90ms)
            shutterClip = CreateShutterClip("CameraShutter", 0.09f);

            // 5. Coffee Espresso Machine (filtered steam / bubbling hiss, 1.2s)
            coffeeClip = CreateFilteredHissClip("CoffeeEspresso", 1.2f, 0.18f);

            // 6. Fan Donation Chime (warm harmonic chord, 450ms)
            donationClip = CreateWarmChimeClip("DonationFanfare", new float[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.45f);

            // 7. Incident Alert (gentle warning ping, 220ms)
            alertClip = CreateSoftWarningClip("SoftAlert", 0.22f);

            // 8. Room Tone / Gentle Airflow Ambient Loop (4.0s seamless filtered low-frequency hum)
            roomToneClip = CreateRoomToneClip("ApartmentRoomTone", 4.0f);
        }

        private AudioClip CreateSoftClickClip(string name, float duration)
        {
            int sampleRate = 44100;
            int count = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float env = Mathf.Exp(-progress * 22f); // rapid exponential decay
                // 240Hz soft wooden thud + tiny burst of noise
                float tone = Mathf.Sin(2f * Mathf.PI * 240f * t);
                float noise = (Random.value * 2f - 1f) * Mathf.Exp(-progress * 50f) * 0.2f;
                samples[i] = (tone + noise) * env * 0.45f;
            }

            AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateWarmChimeClip(string name, float[] freqs, float duration)
        {
            int sampleRate = 44100;
            int count = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[count];

            for (int f = 0; f < freqs.Length; f++)
            {
                float freq = freqs[f];
                float offsetSec = f * 0.06f;
                int startSample = Mathf.CeilToInt(sampleRate * offsetSec);

                for (int i = startSample; i < count; i++)
                {
                    float t = (float)(i - startSample) / sampleRate;
                    float env = Mathf.Exp(-t * 8f);
                    // Fundamental + soft 2nd harmonic
                    float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f + Mathf.Sin(2f * Mathf.PI * (freq * 2f) * t) * 0.2f;
                    samples[i] += wave * env * 0.25f;
                }
            }

            AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateGentleAlarmClip(string name, float duration)
        {
            int sampleRate = 44100;
            int count = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[count];

            // 4-note melodic pattern (A4, C#5, E5, A5)
            float[] pattern = new float[] { 440f, 554.37f, 659.25f, 880f };
            float noteDuration = 0.22f;

            for (int n = 0; n < pattern.Length; n++)
            {
                float freq = pattern[n];
                int start = Mathf.CeilToInt(sampleRate * n * noteDuration);
                int noteCount = Mathf.CeilToInt(sampleRate * noteDuration);

                for (int i = 0; i < noteCount && (start + i) < count; i++)
                {
                    float t = (float)i / sampleRate;
                    float env = Mathf.Exp(-t * 9f);
                    float wave = Mathf.Sin(2f * Mathf.PI * freq * t) + Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t) * 0.3f;
                    samples[start + i] += wave * env * 0.35f;
                }
            }

            AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateShutterClip(string name, float duration)
        {
            int sampleRate = 44100;
            int count = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[count];

            // Click 1 (Mirror up)
            int firstClickLen = Mathf.CeilToInt(sampleRate * 0.02f);
            for (int i = 0; i < firstClickLen; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 80f);
                samples[i] += (Random.value * 2f - 1f) * env * 0.4f;
            }

            // Click 2 (Curtain close, 40ms later)
            int secondStart = Mathf.CeilToInt(sampleRate * 0.045f);
            int secondLen = Mathf.CeilToInt(sampleRate * 0.035f);
            for (int i = 0; i < secondLen && (secondStart + i) < count; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 60f);
                samples[secondStart + i] += (Random.value * 2f - 1f) * env * 0.5f;
            }

            AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateFilteredHissClip(string name, float duration, float volume)
        {
            int sampleRate = 44100;
            int count = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[count];

            float lastSample = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin(Mathf.PI * (t / duration)); // smooth fade in and fade out
                float white = Random.value * 2f - 1f;
                // Low-pass filter to remove harsh highs
                lastSample = Mathf.Lerp(lastSample, white, 0.12f);
                samples[i] = lastSample * env * volume;
            }

            AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateSoftWarningClip(string name, float duration)
        {
            int sampleRate = 44100;
            int count = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 12f);
                float wave = Mathf.Sin(2f * Mathf.PI * 370f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 554f * t) * 0.4f;
                samples[i] = wave * env * 0.35f;
            }

            AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateRoomToneClip(string name, float duration)
        {
            int sampleRate = 44100;
            int count = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[count];

            // Gentle low-pass brown noise room tone with seamless looping crossfade
            float brown = 0f;
            for (int i = 0; i < count; i++)
            {
                float white = Random.value * 2f - 1f;
                brown = (brown + (0.02f * white)) / 1.02f;
                samples[i] = brown * 0.12f;
            }

            // Crossfade 150ms at loop boundary for seamless looping
            int crossfadeSamples = Mathf.CeilToInt(sampleRate * 0.15f);
            for (int i = 0; i < crossfadeSamples; i++)
            {
                float blend = (float)i / crossfadeSamples;
                samples[i] = Mathf.Lerp(samples[count - crossfadeSamples + i], samples[i], blend);
            }

            AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
