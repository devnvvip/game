using UnityEngine;
using StreamerGame.Audio;
using StreamerGame.UI;

namespace StreamerGame.Interactables
{
    public class StudioRingLight : InteractableObject
    {
        [Header("Ring Light Setup")]
        public Light ringSpotLight;
        public Renderer ringMeshRenderer;
        public bool isOn = false;

        private void Start()
        {
            promptText = "[E] Bật/Tắt Đèn Ring Light Studio";
            associatedQuestId = "q_check_gear";
            UpdateVisuals();
        }

        protected override void OnInteracted()
        {
            isOn = !isOn;
            UpdateVisuals();
            SoundManager.Instance?.PlayClick();
            HUDManager.Instance?.ShowNotification(isOn ? "Đã bật Ring Light! Ánh sáng studio hoàn hảo." : "Đã tắt Ring Light.");
        }

        private void UpdateVisuals()
        {
            if (ringSpotLight != null)
            {
                ringSpotLight.enabled = isOn;
            }
            if (ringMeshRenderer != null && ringMeshRenderer.material != null)
            {
                if (isOn)
                {
                    ringMeshRenderer.material.EnableKeyword("_EMISSION");
                    ringMeshRenderer.material.SetColor("_EmissionColor", new Color(1f, 0.95f, 0.85f) * 2.5f);
                }
                else
                {
                    ringMeshRenderer.material.DisableKeyword("_EMISSION");
                    ringMeshRenderer.material.SetColor("_EmissionColor", Color.black);
                }
            }
        }
    }
}
