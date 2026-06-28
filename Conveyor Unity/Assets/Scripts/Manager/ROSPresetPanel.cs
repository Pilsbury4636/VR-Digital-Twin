using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace UR5eScanningBridge
{
    /// <summary>
    /// Builds a world-space Canvas of preset ROS connection buttons for VR.
    /// Each preset is a label + IP + port. Tapping a button reconnects ROS
    /// to that endpoint via ROSConnectionManager.
    ///
    /// Setup:
    ///   1. Put a ROSConnectionManager somewhere in the scene.
    ///   2. Attach this script to an empty GameObject positioned where you
    ///      want the panel to float (e.g. 1.5m in front of the player, at
    ///      eye height). The panel builds itself at that transform.
    ///   3. Fill in the Presets list in the Inspector.
    ///   4. Make sure your scene has an XR UI Input Module (the XR
    ///      Interaction Toolkit rig provides one) so VR rays can click UI.
    ///
    /// If your rig uses Meta's Interaction SDK pokes instead of XRI rays,
    /// the buttons still work as standard UI — you just need a poke
    /// interactor pointed at the canvas.
    /// </summary>
    public class ROSPresetPanel : MonoBehaviour
    {
        [Serializable]
        public class Preset
        {
            public string label = "Lab PC";
            public string ip = "192.168.1.10";
            public int port = 10000;
        }

        [Header("References")]
        [Tooltip("Leave empty to auto-find in the scene.")]
        public ROSConnectionManager connectionManager;

        [Header("Presets")]
        public List<Preset> presets = new List<Preset>()
        {
            new Preset { label = "Localhost",  ip = "127.0.0.1",    port = 10000 },
            new Preset { label = "Lab PC",     ip = "192.168.1.10", port = 10000 },
            new Preset { label = "Laptop",     ip = "192.168.1.20", port = 10000 },
        };

        [Header("Panel Size (world meters)")]
        public float panelWidth = 0.4f;
        public float buttonHeight = 0.07f;
        public float spacing = 0.015f;

        private TextMeshProUGUI statusText;

        void Start()
        {
            if (connectionManager == null)
                connectionManager = FindObjectOfType<ROSConnectionManager>();

            if (connectionManager == null)
            {
                Debug.LogError("[ROSPresetPanel] No ROSConnectionManager found.");
                enabled = false;
                return;
            }

            BuildPanel();
        }

        private void BuildPanel()
        {
            // --- Canvas ---
            GameObject canvasGO = new GameObject("ROSPresetCanvas");
            canvasGO.transform.SetParent(transform, false);

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            float totalHeight = (buttonHeight + spacing) * (presets.Count + 2) + spacing;
            canvasRect.sizeDelta = new Vector2(panelWidth * 1000f, totalHeight * 1000f);
            canvasRect.localScale = Vector3.one * 0.001f; // 1000 units = 1 meter

            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // XR raycaster so VR controllers can click the buttons
            try
            {
                canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ROSPresetPanel] Could not add TrackedDeviceGraphicRaycaster: " + e.Message);
            }

            // Background
            Image bg = canvasGO.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.12f, 0.85f);

            // --- Title ---
            CreateLabel(canvasRect, "ROS Connection", totalHeight, 0, true);

            // --- Preset buttons ---
            for (int i = 0; i < presets.Count; i++)
            {
                Preset p = presets[i];
                CreateButton(canvasRect, $"{p.label}\n{p.ip}:{p.port}", totalHeight, i + 1,
                    () => OnPresetSelected(p));
            }

            // --- Status line ---
            statusText = CreateLabel(canvasRect, "Not connected", totalHeight, presets.Count + 1, false);
        }

        private void CreateButton(RectTransform parent, string text, float totalHeight,
                                   int row, Action onClick)
        {
            GameObject btnGO = new GameObject("Button_" + row);
            btnGO.transform.SetParent(parent, false);

            RectTransform rect = btnGO.AddComponent<RectTransform>();
            LayoutRow(rect, totalHeight, row);

            Image img = btnGO.AddComponent<Image>();
            img.color = new Color(0.2f, 0.35f, 0.55f, 1f);

            Button btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick());

            // Label
            GameObject txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            RectTransform txtRect = txtGO.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        private TextMeshProUGUI CreateLabel(RectTransform parent, string text,
                                             float totalHeight, int row, bool isTitle)
        {
            GameObject txtGO = new GameObject(isTitle ? "Title" : "Status");
            txtGO.transform.SetParent(parent, false);

            RectTransform rect = txtGO.AddComponent<RectTransform>();
            LayoutRow(rect, totalHeight, row);

            TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = isTitle ? 30 : 20;
            tmp.fontStyle = isTitle ? FontStyles.Bold : FontStyles.Normal;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = isTitle ? Color.white : Color.cyan;
            return tmp;
        }

        private void LayoutRow(RectTransform rect, float totalHeight, int row)
        {
            // Rows stack top-to-bottom in canvas units (1000 = 1m)
            float h = buttonHeight * 1000f;
            float s = spacing * 1000f;
            float totalH = totalHeight * 1000f;

            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(panelWidth * 1000f - s * 2, h);
            float y = -(s + row * (h + s));
            rect.anchoredPosition = new Vector2(0, y);
        }

        private void OnPresetSelected(Preset p)
        {
            Debug.Log($"[ROSPresetPanel] Selected '{p.label}' -> {p.ip}:{p.port}");
            connectionManager.SetConnection(p.ip, p.port);
            if (statusText != null)
                statusText.text = $"Connecting to\n{p.label} ({p.ip})";
        }
    }
}
