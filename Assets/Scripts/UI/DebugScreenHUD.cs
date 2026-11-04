using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace StreamerGame.UI
{
    public class DebugScreenHUD : MonoBehaviour
    {
        public static DebugScreenHUD Instance { get; private set; }

        private Text debugText;
        private string lastErrorMessage = "None";
        private string lastErrorStackTrace = "";
        private float updateInterval = 0.5f;
        private float nextUpdateTime = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Application.logMessageReceived += HandleLogMessage;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLogMessage;
        }

        private void Start()
        {
            CreateHUDUI();
        }

        private void HandleLogMessage(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                lastErrorMessage = $"[{DateTime.Now:HH:mm:ss}] ({type}) {logString}";
                lastErrorStackTrace = stackTrace;
                UpdateHUD();
            }
        }

        private void CreateHUDUI()
        {
            // Create a small container in the bottom-left corner
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            GameObject hudPanel = new GameObject("DebugHUD_Panel", typeof(RectTransform));
            hudPanel.transform.SetParent(transform, false);

            RectTransform rt = hudPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(10f, 10f);
            rt.sizeDelta = new Vector2(480f, 160f);

            Image bg = hudPanel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.65f);
            bg.raycastTarget = false;

            GameObject textObj = new GameObject("DebugHUD_Text", typeof(RectTransform));
            textObj.transform.SetParent(hudPanel.transform, false);

            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.pivot = new Vector2(0.5f, 0.5f);
            textRt.anchoredPosition = Vector2.zero;
            textRt.sizeDelta = new Vector2(-12f, -12f);

            debugText = textObj.AddComponent<Text>();
            debugText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            debugText.fontSize = 11;
            debugText.color = new Color(0.2f, 1f, 0.4f, 1f); // Lime green
            debugText.alignment = TextAnchor.UpperLeft;
            debugText.horizontalOverflow = HorizontalWrapMode.Wrap;
            debugText.verticalOverflow = VerticalWrapMode.Truncate;
            debugText.raycastTarget = false;

            UpdateHUD();
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextUpdateTime)
            {
                nextUpdateTime = Time.unscaledTime + updateInterval;
                UpdateHUD();
            }
        }

        public void UpdateHUD()
        {
            if (debugText == null) return;

            Camera mainCam = Camera.main;
            int totalCameras = Camera.allCamerasCount;
            bool camEnabled = mainCam != null && mainCam.enabled;
            string clearFlags = mainCam != null ? mainCam.clearFlags.ToString() : "N/A";
            Vector3 camPos = mainCam != null ? mainCam.transform.position : Vector3.zero;

            int activeRenderers = 0;
            Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            if (allRenderers != null)
            {
                activeRenderers = allRenderers.Count(r => r != null && r.enabled && r.gameObject.activeInHierarchy);
            }

            string graphicsAPI = SystemInfo.graphicsDeviceType.ToString();
            string deviceModel = SystemInfo.deviceModel;
            int qualityLevel = QualitySettings.GetQualityLevel();
            string qualityName = qualityLevel < QualitySettings.names.Length ? QualitySettings.names[qualityLevel] : "Unknown";
            string srpName = GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.name : "Built-in / None";

            string text = $"<b>[DEBUG ON-SCREEN MONITOR]</b>\n" +
                          $"• Cameras: {totalCameras} | MainCam Active: {camEnabled} | Clear: {clearFlags}\n" +
                          $"• Cam Pos: ({camPos.x:F1}, {camPos.y:F1}, {camPos.z:F1}) | Renderers: {activeRenderers}\n" +
                          $"• Graphics API: {graphicsAPI} | Device: {deviceModel}\n" +
                          $"• Quality: [{qualityLevel}] {qualityName} | Pipeline: {srpName}\n" +
                          $"• Last Log/Err: <color=#FF6666>{lastErrorMessage}</color>";

            debugText.text = text;
        }
    }
}
