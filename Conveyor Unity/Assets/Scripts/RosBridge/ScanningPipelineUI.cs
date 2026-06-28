using UnityEngine;

namespace UR5eScanningBridge
{
    /// <summary>
    /// Simple runtime UI that draws buttons for each scanning pipeline pose.
    /// Attach to the same GameObject as ROSPoseGoalPublisher.
    ///
    /// Each button sends the corresponding pose to MoveIt via ROS2.
    /// The status label at the bottom shows feedback from the bridge node.
    ///
    /// For a production UI, replace this with proper Unity UI (Canvas/Buttons)
    /// and wire OnClick events to the ROSPoseGoalPublisher methods.
    /// </summary>
    [RequireComponent(typeof(ROSPoseGoalPublisher))]
    public class ScanningPipelineUI : MonoBehaviour
    {
        private ROSPoseGoalPublisher publisher;

        [Header("UI Layout")]
        public float panelX = 20f;
        public float panelY = 20f;
        public float buttonWidth = 200f;
        public float buttonHeight = 40f;
        public float spacing = 8f;

        private GUIStyle buttonStyle;
        private GUIStyle headerStyle;
        private GUIStyle statusStyle;
        private bool stylesInitialized = false;

        void Start()
        {
            publisher = GetComponent<ROSPoseGoalPublisher>();
        }

        void InitStyles()
        {
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
            };

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = Color.cyan }
            };

            stylesInitialized = true;
        }

        void OnGUI()
        {
            if (!stylesInitialized)
                InitStyles();

            float x = panelX;
            float y = panelY;
            float w = buttonWidth;
            float h = buttonHeight;
            float s = spacing;

            // Background panel
            float panelHeight = 520f;
            GUI.Box(new Rect(x - 10, y - 10, w + 20, panelHeight), "");

            // Header
            GUI.Label(new Rect(x, y, w, 30), "Scanning Pipeline", headerStyle);
            y += 35;

            // ── Individual pose buttons ──

            GUI.Label(new Rect(x, y, w, 20), "Individual Poses:");
            y += 22;

            if (GUI.Button(new Rect(x, y, w, h), "Detection Pose", buttonStyle))
                publisher.GoToDetection();
            y += h + s;

            if (GUI.Button(new Rect(x, y, w, h), "Alignment Pose", buttonStyle))
                publisher.GoToAlignment();
            y += h + s;

            if (GUI.Button(new Rect(x, y, w, h), "Scan Pose", buttonStyle))
                publisher.GoToScan();
            y += h + s;

            if (GUI.Button(new Rect(x, y, w, h), "Pick Pose", buttonStyle))
                publisher.GoToPick();
            y += h + s;

            if (GUI.Button(new Rect(x, y, w, h), "Drop Pose", buttonStyle))
                publisher.GoToDrop();
            y += h + s + 10;

            // ── Sequence buttons ──

            GUI.Label(new Rect(x, y, w, 20), "Sequences:");
            y += 22;

            if (GUI.Button(new Rect(x, y, w, h), "Pick & Place", buttonStyle))
                publisher.RunPickAndPlace();
            y += h + s;

            if (GUI.Button(new Rect(x, y, w, h), "Full Sequence", buttonStyle))
                publisher.RunFullSequence();
            y += h + s + 10;

            // ── Gripper ──

            GUI.Label(new Rect(x, y, w, 20), "Gripper:");
            y += 22;

            float halfW = (w - s) / 2f;
            if (GUI.Button(new Rect(x, y, halfW, h), "Suction ON", buttonStyle))
                publisher.SuctionOn();
            if (GUI.Button(new Rect(x + halfW + s, y, halfW, h), "Suction OFF", buttonStyle))
                publisher.SuctionOff();
            y += h + s + 10;

            // ── Status ──

            string status = publisher.GetLastStatus();
            if (!string.IsNullOrEmpty(status))
            {
                GUI.Label(new Rect(x, y, w, 40), $"Status: {status}", statusStyle);
            }
        }
    }
}
