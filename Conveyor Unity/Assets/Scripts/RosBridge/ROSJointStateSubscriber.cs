using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

namespace UR5eScanningBridge
{
    /// <summary>
    /// Subscribes to /joint_states and mirrors joint angles onto the Unity
    /// URDF robot.
    ///
    /// IMPORTANT: This version has NO dependency on the URDF Importer package,
    /// so it compiles and runs on Android / Meta Quest builds. Joints are
    /// identified by GameObject name using a link->joint mapping table,
    /// instead of reading UrdfJoint.jointName at runtime.
    ///
    /// Setup:
    ///   - Attach to the robot root GameObject.
    ///   - Leave autoFindJoints checked (matches by GameObject name), OR
    ///     assign the ArticulationBodies manually in the joints[] array
    ///     in the order listed in ExpectedJointNames.
    /// </summary>
    public class ROSJointStateSubscriber : MonoBehaviour
    {
        [Header("ROS")]
        public string jointStatesTopic = "/joint_states";

        [Header("Joints")]
        [Tooltip("Assign ArticulationBodies in ExpectedJointNames order, or use auto-find.")]
        public ArticulationBody[] joints;

        [Header("Behavior")]
        public bool autoFindJoints = true;

        [Tooltip("Higher = snappier tracking. 20 is a good default.")]
        public float smoothing = 20f;

        [Tooltip("Enable verbose logging of every received message.")]
        public bool debugLogMessages = false;

        [Header("Drive Configuration (applied to all joints)")]
        public float driveStiffness = 100000f;
        public float driveDamping = 10000f;
        public float driveForceLimit = 10000f;
        public bool configureDrives = true;

        // ROS joint names — also the order for manual array assignment
        private static readonly string[] ExpectedJointNames = new string[]
        {
            "shoulder_pan_joint",
            "shoulder_lift_joint",
            "elbow_joint",
            "wrist_1_joint",
            "wrist_2_joint",
            "wrist_3_joint",
            "robotiq_85_left_knuckle_joint",
            "robotiq_85_right_knuckle_joint",
            "robotiq_85_left_inner_knuckle_joint",
            "robotiq_85_right_inner_knuckle_joint",
            "robotiq_85_left_finger_tip_joint",
            "robotiq_85_right_finger_tip_joint",
        };

        // Maps the GameObject (link) name created by URDF Importer to the
        // ROS joint name. The ArticulationBody sits on the child link of
        // each joint, so the link name identifies the joint.
        private static readonly Dictionary<string, string> LinkToJoint =
            new Dictionary<string, string>
        {
            // UR5e arm: link GameObject name -> joint name
            { "shoulder_link",   "shoulder_pan_joint" },
            { "upper_arm_link",  "shoulder_lift_joint" },
            { "forearm_link",    "elbow_joint" },
            { "wrist_1_link",    "wrist_1_joint" },
            { "wrist_2_link",    "wrist_2_joint" },
            { "wrist_3_link",    "wrist_3_joint" },
            // Robotiq 2F-85: link -> joint (names line up here)
            { "robotiq_85_left_knuckle_link",        "robotiq_85_left_knuckle_joint" },
            { "robotiq_85_right_knuckle_link",       "robotiq_85_right_knuckle_joint" },
            { "robotiq_85_left_inner_knuckle_link",  "robotiq_85_left_inner_knuckle_joint" },
            { "robotiq_85_right_inner_knuckle_link", "robotiq_85_right_inner_knuckle_joint" },
            { "robotiq_85_left_finger_tip_link",     "robotiq_85_left_finger_tip_joint" },
            { "robotiq_85_right_finger_tip_link",    "robotiq_85_right_finger_tip_joint" },
        };

        private Dictionary<string, ArticulationBody> jointMap = new Dictionary<string, ArticulationBody>();
        private Dictionary<string, float> targetAngles = new Dictionary<string, float>();

        private ROSConnection rosConnection;
        private bool initialized = false;
        private int messageCount = 0;

        void Start()
        {
            if (autoFindJoints || joints == null || joints.Length == 0)
                AutoDiscoverJoints();
            else
                BuildJointMapFromArray();

            if (configureDrives)
                ConfigureDrives();

            foreach (var kv in jointMap)
                targetAngles[kv.Key] = kv.Value.xDrive.target;

            rosConnection = ROSConnection.GetOrCreateInstance();
            rosConnection.Subscribe<JointStateMsg>(jointStatesTopic, OnJointStateReceived);

            initialized = true;
            Debug.Log($"[ROSJointStateSubscriber] Subscribed to {jointStatesTopic}. " +
                      $"Tracking {jointMap.Count} joints.");
        }

        /// <summary>
        /// Find joints by GameObject name using the LinkToJoint map.
        /// No URDF Importer dependency — works on Android.
        /// </summary>
        private void AutoDiscoverJoints()
        {
            jointMap.Clear();
            ArticulationBody[] allBodies = GetComponentsInChildren<ArticulationBody>();

            foreach (var body in allBodies)
            {
                string goName = body.name;

                // Strategy 1: GameObject name exactly matches a known link name
                if (LinkToJoint.TryGetValue(goName, out string jointName))
                {
                    jointMap[jointName] = body;
                    Debug.Log($"[ROSJointStateSubscriber] Matched link '{goName}' -> joint '{jointName}'");
                    continue;
                }

                // Strategy 2: GameObject name contains a known link name
                bool matched = false;
                foreach (var kv in LinkToJoint)
                {
                    if (goName.Contains(kv.Key))
                    {
                        jointMap[kv.Value] = body;
                        Debug.Log($"[ROSJointStateSubscriber] Matched (contains) '{goName}' -> joint '{kv.Value}'");
                        matched = true;
                        break;
                    }
                }
                if (matched) continue;

                // Strategy 3: GameObject name directly matches a joint name
                foreach (string expected in ExpectedJointNames)
                {
                    if (goName == expected || goName.Contains(expected))
                    {
                        jointMap[expected] = body;
                        Debug.Log($"[ROSJointStateSubscriber] Matched joint-name '{goName}' -> '{expected}'");
                        break;
                    }
                }
            }

            if (jointMap.Count < 6)
            {
                Debug.LogWarning($"[ROSJointStateSubscriber] Only found {jointMap.Count} joints. " +
                                 "GameObject names of all ArticulationBodies:");
                foreach (var body in allBodies)
                    Debug.Log($"  ArticulationBody GameObject: '{body.name}'");
                Debug.LogWarning("If names don't match, uncheck autoFindJoints and assign " +
                                 "the joints[] array manually in ExpectedJointNames order.");
            }
        }

        private void BuildJointMapFromArray()
        {
            jointMap.Clear();
            for (int i = 0; i < joints.Length && i < ExpectedJointNames.Length; i++)
            {
                if (joints[i] != null)
                    jointMap[ExpectedJointNames[i]] = joints[i];
            }
        }

        private void ConfigureDrives()
        {
            foreach (var kv in jointMap)
            {
                ArticulationBody body = kv.Value;
                ArticulationDrive drive = body.xDrive;
                drive.stiffness = driveStiffness;
                drive.damping = driveDamping;
                drive.forceLimit = driveForceLimit;
                body.xDrive = drive;
            }
        }

        private void OnJointStateReceived(JointStateMsg msg)
        {
            if (!initialized) return;

            messageCount++;
            if (debugLogMessages || messageCount <= 3)
            {
                Debug.Log($"[ROSJointStateSubscriber] Msg #{messageCount}: " +
                          $"{msg.name.Length} joints, " +
                          $"first pos = {(msg.position.Length > 0 ? msg.position[0].ToString("F3") : "N/A")}");
            }

            for (int i = 0; i < msg.name.Length && i < msg.position.Length; i++)
            {
                string jointName = msg.name[i];
                if (jointMap.ContainsKey(jointName))
                {
                    float degrees = (float)(msg.position[i] * Mathf.Rad2Deg);
                    targetAngles[jointName] = degrees;
                }
            }
        }

        void Update()
        {
            if (!initialized) return;

            foreach (var kv in jointMap)
            {
                string jointName = kv.Key;
                ArticulationBody body = kv.Value;
                if (!targetAngles.ContainsKey(jointName)) continue;

                float target = targetAngles[jointName];
                ArticulationDrive drive = body.xDrive;
                drive.target = Mathf.Lerp(drive.target, target, Time.deltaTime * smoothing);
                body.xDrive = drive;
            }
        }
    }
}