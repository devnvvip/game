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

        [Header("Window Lighting & Atmosphere")]
        public Renderer[] windowRenderers;

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

            Camera mainCam = Camera.main;

            switch (phase)
            {
                case DayPhase.Morning:
                    sunLight.transform.rotation = Quaternion.Euler(24f, 55f, 0f);
                    sunLight.color = new Color(1f, 0.92f, 0.80f);
                    sunLight.intensity = 1.1f;
                    RenderSettings.ambientLight = new Color(0.62f, 0.65f, 0.72f);
                    if (mainCam != null) mainCam.backgroundColor = new Color(0.35f, 0.50f, 0.72f, 1f);

                    if (roomCeilingLight) { roomCeilingLight.intensity = 0.4f; roomCeilingLight.color = new Color(1f, 0.95f, 0.85f); }
                    if (streamerRingLight) streamerRingLight.intensity = 0f;
                    if (bedsideLamp) { bedsideLamp.intensity = 0.3f; bedsideLamp.color = new Color(1f, 0.82f, 0.60f); }
                    UpdateWindows(new Color(1f, 0.95f, 0.8f) * 1.5f);
                    break;

                case DayPhase.Midday:
                    sunLight.transform.rotation = Quaternion.Euler(65f, 135f, 0f);
                    sunLight.color = new Color(1f, 0.98f, 0.95f);
                    sunLight.intensity = 1.25f;
                    RenderSettings.ambientLight = new Color(0.68f, 0.72f, 0.78f);
                    if (mainCam != null) mainCam.backgroundColor = new Color(0.40f, 0.65f, 0.90f, 1f);

                    if (roomCeilingLight) roomCeilingLight.intensity = 0.2f;
                    if (streamerRingLight) streamerRingLight.intensity = 0f;
                    if (bedsideLamp) bedsideLamp.intensity = 0f;
                    UpdateWindows(new Color(0.9f, 0.95f, 1f) * 2.0f);
                    break;

                case DayPhase.Afternoon:
                    sunLight.transform.rotation = Quaternion.Euler(18f, 225f, 0f);
                    sunLight.color = new Color(1f, 0.72f, 0.45f); // Golden sunset
                    sunLight.intensity = 1.05f;
                    RenderSettings.ambientLight = new Color(0.65f, 0.52f, 0.55f);
                    if (mainCam != null) mainCam.backgroundColor = new Color(0.65f, 0.40f, 0.35f, 1f);

                    if (roomCeilingLight) { roomCeilingLight.intensity = 0.6f; roomCeilingLight.color = new Color(1f, 0.90f, 0.78f); }
                    if (streamerRingLight) streamerRingLight.intensity = 0f;
                    if (bedsideLamp) bedsideLamp.intensity = 0.4f;
                    UpdateWindows(new Color(1f, 0.65f, 0.35f) * 2.0f);
                    break;

                case DayPhase.Evening:
                    sunLight.transform.rotation = Quaternion.Euler(-12f, 250f, 0f);
                    sunLight.color = new Color(0.12f, 0.15f, 0.30f);
                    sunLight.intensity = 0.15f;
                    RenderSettings.ambientLight = new Color(0.25f, 0.26f, 0.35f);
                    if (mainCam != null) mainCam.backgroundColor = new Color(0.08f, 0.10f, 0.20f, 1f);

                    if (roomCeilingLight) { roomCeilingLight.intensity = 0.9f; roomCeilingLight.color = new Color(0.95f, 0.95f, 1f); }
                    if (streamerRingLight) { streamerRingLight.intensity = 2.0f; streamerRingLight.color = new Color(1f, 0.95f, 0.92f); }
                    if (bedsideLamp) bedsideLamp.intensity = 0.5f;
                    UpdateWindows(new Color(0.15f, 0.25f, 0.5f) * 1.2f);
                    break;

                case DayPhase.Night:
                    sunLight.transform.rotation = Quaternion.Euler(-35f, 260f, 0f);
                    sunLight.color = new Color(0.05f, 0.08f, 0.20f);
                    sunLight.intensity = 0.08f;
                    RenderSettings.ambientLight = new Color(0.15f, 0.16f, 0.24f);
                    if (mainCam != null) mainCam.backgroundColor = new Color(0.04f, 0.06f, 0.12f, 1f);

                    if (roomCeilingLight) roomCeilingLight.intensity = 0.25f;
                    if (streamerRingLight) streamerRingLight.intensity = 0f;
                    if (bedsideLamp) { bedsideLamp.intensity = 0.9f; bedsideLamp.color = new Color(1f, 0.75f, 0.50f); }
                    UpdateWindows(new Color(0.05f, 0.08f, 0.2f) * 0.8f);
                    break;
            }
        }

        private void UpdateWindows(Color emissiveColor)
        {
            if (windowRenderers == null) return;
            foreach (var wr in windowRenderers)
            {
                if (wr != null && wr.material != null)
                {
                    wr.material.EnableKeyword("_EMISSION");
                    wr.material.SetColor("_EmissionColor", emissiveColor);
                }
            }
        }
    }
}
