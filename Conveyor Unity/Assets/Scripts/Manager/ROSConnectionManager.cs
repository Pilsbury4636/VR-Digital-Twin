using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

namespace UR5eScanningBridge
{
    /// <summary>
    /// Lets you change the ROS IP address and port at runtime and reconnect,
    /// without restarting the app. Call SetConnection(ip, port) from any UI
    /// (Canvas button, VR keyboard, IMGUI, etc.).
    ///
    /// The actual UI is intentionally separate — this script just exposes
    /// methods. Wire your input field + button to these.
    /// </summary>
    public class ROSConnectionManager : MonoBehaviour
    {
        [Header("Current Connection (read-only at runtime)")]
        [SerializeField] private string currentIP = "127.0.0.1";
        [SerializeField] private int currentPort = 10000;
        [SerializeField] private bool connected = false;

        private ROSConnection ros;

        void Start()
        {
            ros = ROSConnection.GetOrCreateInstance();
            currentIP = ros.RosIPAddress;
            currentPort = ros.RosPort;
        }

        /// <summary>
        /// Change the ROS endpoint and reconnect. Returns immediately;
        /// connection happens asynchronously.
        /// </summary>
        public void SetConnection(string ip, int port)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                Debug.LogWarning("[ROSConnectionManager] Empty IP, ignoring");
                return;
            }

            ip = ip.Trim();
            Debug.Log($"[ROSConnectionManager] Switching to {ip}:{port}");

            ros = ROSConnection.GetOrCreateInstance();

            // Disconnect from the current endpoint if connected
            try { ros.Disconnect(); }
            catch { /* not connected yet — fine */ }

            // Update endpoint
            ros.RosIPAddress = ip;
            ros.RosPort = port;
            currentIP = ip;
            currentPort = port;

            // Reconnect to the new endpoint
            ros.Connect(ip, port);
            connected = true;

            Debug.Log($"[ROSConnectionManager] Now targeting {ip}:{port}");
        }

        /// <summary>Convenience overload — keep current port, change IP only.</summary>
        public void SetIP(string ip)
        {
            SetConnection(ip, currentPort);
        }

        /// <summary>Convenience overload — change port only.</summary>
        public void SetPort(int port)
        {
            SetConnection(currentIP, port);
        }

        /// <summary>Parse "192.168.1.5" or "192.168.1.5:10000" and connect.</summary>
        public void SetConnectionFromString(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;
            input = input.Trim();

            string ip = input;
            int port = currentPort;

            if (input.Contains(":"))
            {
                string[] parts = input.Split(':');
                ip = parts[0];
                if (parts.Length > 1 && int.TryParse(parts[1], out int parsedPort))
                    port = parsedPort;
            }

            SetConnection(ip, port);
        }

        public string GetCurrentIP() => currentIP;
        public int GetCurrentPort() => currentPort;
    }
}
