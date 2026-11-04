using UnityEngine;

namespace StreamerGame.Audio
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        private AudioSource sfxSource;
        private AudioSource bgmSource;
        private AudioSource alarmSource;

        private AudioClip clickClip;
        private AudioClip chimeClip;
        private AudioClip alarmClip;
        private AudioClip shutterClip;
        private AudioClip coffeeClip;
        private AudioClip donationClip;
        private AudioClip alertClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.volume = 0.35f;

            alarmSource = gameObject.AddComponent<AudioSource>();
            alarmSource.playOnAwake = false;
            alarmSource.loop = true;

            GenerateProceduralClips();
        }

        private void GenerateProceduralClips()
        {
            clickClip = CreateToneClip("Click", 1200f, 0.04f, 0.4f);
            chimeClip = CreateArpeggioClip("Chime", new float[] { 523.25f, 659.25f, 783.99f }, 0.12f, 0.5f);
            alarmClip = CreateAlarmClip("AlarmBeep", 880f, 0.6f);
            shutterClip = CreateNoiseClip("Shutter", 0.08f, 0.6f);
            coffeeClip = CreateNoiseClip("CoffeeBrew", 0.8f, 0.3f);
            donationClip = CreateArpeggioClip("Donation", new float[] { 587.33f, 739.99f, 880f, 1174.66f }, 0.10f, 0.7f);
            alertClip = CreateToneClip("Alert", 440f, 0.25f, 0.8f);
        }

        public void PlayClick() => PlayClip(clickClip);
        public void PlayNotification() => PlayClip(chimeClip);
        public void PlayShutter() => PlayClip(shutterClip);
        public void PlayCoffee() => PlayClip(coffeeClip);
        public void PlayDonation() => PlayClip(donationClip);
        public void PlayAlert() => PlayClip(alertClip);

        public void StartAlarm()
        {
            if (alarmSource != null && alarmClip != null)
            {
                alarmSource.clip = alarmClip;
                alarmSource.volume = 0.6f;
                alarmSource.Play();
            }
        }

        public void StopAlarm()
        {
            if (alarmSource != null && alarmSource.isPlaying)
            {
                alarmSource.Stop();
            }
        }

        private void PlayClip(AudioClip clip, float volume = 0.7f)
        {
            if (sfxSource != null && clip != null)
            {
                sfxSource.PlayOneShot(clip, volume);
            }
        }

        private AudioClip CreateToneClip(string name, float frequency, float duration, float volume)
        {
            int sampleRate = 44100;
            int samplesCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Clamp01(1f - (t / duration));
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(name, samplesCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateArpeggioClip(string name, float[] notes, float noteDuration, float volume)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.CeilToInt(sampleRate * noteDuration * notes.Length);
            float[] samples = new float[totalSamples];

            for (int noteIdx = 0; noteIdx < notes.Length; noteIdx++)
            {
                float freq = notes[noteIdx];
                int start = Mathf.CeilToInt(sampleRate * noteDuration * noteIdx);
                int count = Mathf.CeilToInt(sampleRate * noteDuration);

                for (int i = 0; i < count && (start + i) < totalSamples; i++)
                {
                    float t = (float)i / sampleRate;
                    float envelope = Mathf.Clamp01(1f - (t / noteDuration));
                    samples[start + i] += Mathf.Sin(2 * Mathf.PI * freq * t) * envelope * volume;
                }
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateAlarmClip(string name, float freq, float duration)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                bool beepOn = (Mathf.Repeat(t, 0.25f) < 0.12f);
                samples[i] = beepOn ? (Mathf.Sin(2 * Mathf.PI * freq * t) * 0.5f) : 0f;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateNoiseClip(string name, float duration, float volume)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin(Mathf.PI * (t / duration));
                samples[i] = (Random.value * 2f - 1f) * env * volume;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
