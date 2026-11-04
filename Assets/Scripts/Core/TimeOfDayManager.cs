using System;
using UnityEngine;

namespace StreamerGame.Core
{
    public enum DayPhase
    {
        Morning,    // 07:00 - 11:30
        Midday,     // 11:30 - 14:00
        Afternoon,  // 14:00 - 18:30
        Evening,    // 19:00 - 22:30 (Core Livestream)
        Night       // 22:30 - 24:00 (End of Day & Sleep)
    }

    public class TimeOfDayManager : MonoBehaviour
    {
        public static TimeOfDayManager Instance { get; private set; }

        public DayPhase currentPhase = DayPhase.Morning;

        [Header("Lighting References")]
        public Light sunLight;
        public Light roomCeilingLight;
        public Light streamerRingLight;
        public Light bedsideLamp;

        public event Action<DayPhase> OnPhaseChanged;

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
            ApplyPhaseLighting(currentPhase);
        }

        public void AdvancePhase()
        {
            if (currentPhase < DayPhase.Night)
            {
                SetPhase(currentPhase + 1);
            }
            else
            {
                // Day completed
                GameManager.Instance?.CompleteDay();
            }
        }

        public void SetPhase(DayPhase newPhase)
        {
            currentPhase = newPhase;
            ApplyPhaseLighting(currentPhase);
            OnPhaseChanged?.Invoke(currentPhase);
        }

        public string GetPhaseName(DayPhase phase)
        {
            switch (phase)
            {
                case DayPhase.Morning: return "Buổi Sáng (07:00 - 11:30)";
                case DayPhase.Midday: return "Buổi Trưa (11:30 - 14:00)";
                case DayPhase.Afternoon: return "Buổi Chiều (14:00 - 18:30)";
                case DayPhase.Evening: return "Buổi Tối: LIVESTREAM (19:00 - 22:30)";
                case DayPhase.Night: return "Đêm Khuya (22:30 - 24:00)";
                default: return "";
            }
        }

        public void ApplyPhaseLighting(DayPhase phase)
        {
            if (sunLight == null) return;

            switch (phase)
            {
                case DayPhase.Morning:
                    sunLight.transform.rotation = Quaternion.Euler(28f, 50f, 0f);
                    sunLight.color = new Color(1f, 0.92f, 0.82f);
                    sunLight.intensity = 1.1f;
                    RenderSettings.ambientLight = new Color(0.6f, 0.65f, 0.75f);
                    if (roomCeilingLight) roomCeilingLight.intensity = 0.4f;
                    if (streamerRingLight) streamerRingLight.intensity = 0f;
                    if (bedsideLamp) bedsideLamp.intensity = 0.2f;
                    break;

                case DayPhase.Midday:
                    sunLight.transform.rotation = Quaternion.Euler(68f, 130f, 0f);
                    sunLight.color = new Color(1f, 0.98f, 0.95f);
                    sunLight.intensity = 1.3f;
                    RenderSettings.ambientLight = new Color(0.7f, 0.72f, 0.75f);
                    if (roomCeilingLight) roomCeilingLight.intensity = 0.2f;
                    if (streamerRingLight) streamerRingLight.intensity = 0f;
                    if (bedsideLamp) bedsideLamp.intensity = 0f;
                    break;

                case DayPhase.Afternoon:
                    sunLight.transform.rotation = Quaternion.Euler(22f, 220f, 0f);
                    sunLight.color = new Color(1f, 0.78f, 0.55f);
                    sunLight.intensity = 1.0f;
                    RenderSettings.ambientLight = new Color(0.65f, 0.55f, 0.6f);
                    if (roomCeilingLight) roomCeilingLight.intensity = 0.6f;
                    if (streamerRingLight) streamerRingLight.intensity = 0f;
                    if (bedsideLamp) bedsideLamp.intensity = 0.3f;
                    break;

                case DayPhase.Evening:
                    sunLight.transform.rotation = Quaternion.Euler(-10f, 240f, 0f);
                    sunLight.color = new Color(0.1f, 0.12f, 0.25f);
                    sunLight.intensity = 0.1f;
                    RenderSettings.ambientLight = new Color(0.2f, 0.22f, 0.3f);
                    if (roomCeilingLight) roomCeilingLight.intensity = 1.0f;
                    if (streamerRingLight) streamerRingLight.intensity = 1.8f;
                    if (bedsideLamp) bedsideLamp.intensity = 0.5f;
                    break;

                case DayPhase.Night:
                    sunLight.transform.rotation = Quaternion.Euler(-30f, 260f, 0f);
                    sunLight.color = new Color(0.05f, 0.08f, 0.18f);
                    sunLight.intensity = 0.05f;
                    RenderSettings.ambientLight = new Color(0.12f, 0.14f, 0.2f);
                    if (roomCeilingLight) roomCeilingLight.intensity = 0.3f;
                    if (streamerRingLight) streamerRingLight.intensity = 0f;
                    if (bedsideLamp) bedsideLamp.intensity = 0.8f;
                    break;
            }
        }
    }
}
