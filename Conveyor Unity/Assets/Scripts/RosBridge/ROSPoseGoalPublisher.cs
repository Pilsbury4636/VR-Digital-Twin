using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using PoseStampedMsg = RosMessageTypes.Geometry.PoseStampedMsg;
using PoseMsg = RosMessageTypes.Geometry.PoseMsg;
using PointMsg = RosMessageTypes.Geometry.PointMsg;
using QuaternionMsg = RosMessageTypes.Geometry.QuaternionMsg;
using HeaderMsg = RosMessageTypes.Std.HeaderMsg;
using StringMsg = RosMessageTypes.Std.StringMsg;

namespace UR5eScanningBridge
{
    public class ROSPoseGoalPublisher : MonoBehaviour
    {
        [Header("ROS Topics")]
        public string poseGoalTopic = "/unity_pose_goal";
        public string poseNameTopic = "/unity_pose_name";
        public string gripperCommandTopic = "/unity_gripper_command";
        public string statusTopic = "/moveit_status";

        [Header("Status")]
        [SerializeField] private string lastStatus = "";
        [SerializeField] private bool isConnected = false;

        private ROSConnection rosConnection;

        private static readonly Dictionary<string, double[]> PipelinePoses =
            new Dictionary<string, double[]>
            {
                ["detection"] = new double[] {
                0.690398642353487, -0.4544008592926506, 0.3179552280288786,
                2.0981462565656837, 2.233353342391994, 0.10530858123762818
            },
                ["alignment"] = new double[] {
                0.6903473182675144, -0.28089662559990497, 0.3011487407394824,
                -2.140999779761, -2.275538554387, 0.014255567780
            },
                ["scan"] = new double[] {
                0.6610677446645701, -0.25213018972147994, 0.3110014858014649,
                -2.1122079403913987, -2.277887700813789, -0.016120645481089695
            },
                ["pick"] = new double[] {
                0.734823016731494, -0.21915711719682887, 0.12135140386866344,
                -2.011756263641095, -2.354511390758949, 0.013929554301622842
            },
                ["drop"] = new double[] {
                0.35408779470312507, -0.6277985314083585, 0.09163414487131588,
                -2.11324724252493, -2.2819505729846736, -0.02765755486127989
            },
            };

        void Start()
        {
            rosConnection = ROSConnection.GetOrCreateInstance();
            rosConnection.RegisterPublisher<PoseStampedMsg>(poseGoalTopic);
            rosConnection.RegisterPublisher<StringMsg>(poseNameTopic);
            rosConnection.RegisterPublisher<StringMsg>(gripperCommandTopic);
            rosConnection.Subscribe<StringMsg>(statusTopic, OnStatusReceived);
            isConnected = true;
        }

        private void OnStatusReceived(StringMsg msg)
        {
            lastStatus = msg.data;
            Debug.Log($"[MoveIt Status] {msg.data}");
        }

        // ─── Public API ─────────────────────────────────────────

        public void GoReady()
        {
            PublishName("ready");
            Debug.Log("[ROSPoseGoal] Triggered Go Ready");
        }

        public void GoToDetection() => SendNamedPose("detection");
        public void GoToAlignment() => SendNamedPose("alignment");
        public void GoToScan() => SendNamedPose("scan");
        public void GoToPick() => SendNamedPose("pick");
        public void GoToDrop() => SendNamedPose("drop");

        public void RunPickAndPlace()
        {
            PublishName("pick_and_place");
            Debug.Log("[ROSPoseGoal] Triggered pick & place sequence");
        }

        public void RunFullSequence()
        {
            PublishName("full_sequence");
            Debug.Log("[ROSPoseGoal] Triggered full scanning sequence");
        }

        // ─── Gripper ─────────────────────────────────────────────

        public void GripperOpen() => PublishGripper("open");
        public void GripperClose() => PublishGripper("close");

        /// <summary>
        /// Send a custom gripper position (0.0 = fully open, 0.7929 = fully closed).
        /// </summary>
        public void GripperPosition(float radians)
        {
            PublishGripper(radians.ToString("F4"));
        }

        // Legacy compatibility (in case anything still references these)
        public void SuctionOn() => GripperClose();
        public void SuctionOff() => GripperOpen();

        // ─── Core methods ─────────────────────────────────────────

        public void SendNamedPose(string poseName)
        {
            if (!PipelinePoses.ContainsKey(poseName))
            {
                Debug.LogError($"[ROSPoseGoal] Unknown pose: {poseName}");
                return;
            }
            double[] pose = PipelinePoses[poseName];
            PublishPose(pose);
            PublishName(poseName);
            Debug.Log($"[ROSPoseGoal] Sent pose: {poseName}");
        }

        public void SendCustomPose(double x, double y, double z,
                                    double rx, double ry, double rz,
                                    string label = "custom")
        {
            double[] pose = { x, y, z, rx, ry, rz };
            PublishPose(pose);
            PublishName(label);
            Debug.Log($"[ROSPoseGoal] Sent custom pose: {label}");
        }

        public string GetLastStatus() => lastStatus;

        // ─── Internal ─────────────────────────────────────────────

        private void PublishPose(double[] pose6d)
        {
            PoseStampedMsg msg = new PoseStampedMsg
            {
                header = new HeaderMsg { frame_id = "base" },
                pose = AxisAngleToPoseMsg(pose6d)
            };
            rosConnection.Publish(poseGoalTopic, msg);
        }

        private void PublishName(string name)
        {
            rosConnection.Publish(poseNameTopic, new StringMsg { data = name });
        }

        private void PublishGripper(string command)
        {
            rosConnection.Publish(gripperCommandTopic, new StringMsg { data = command });
            Debug.Log($"[ROSPoseGoal] Gripper: {command}");
        }

        private static PoseMsg AxisAngleToPoseMsg(double[] pose)
        {
            double rx = pose[3], ry = pose[4], rz = pose[5];
            double angle = Math.Sqrt(rx * rx + ry * ry + rz * rz);

            double qx, qy, qz, qw;
            if (angle < 1e-10)
            {
                qx = 0; qy = 0; qz = 0; qw = 1;
            }
            else
            {
                double halfAngle = angle / 2.0;
                double s = Math.Sin(halfAngle) / angle;
                qx = rx * s;
                qy = ry * s;
                qz = rz * s;
                qw = Math.Cos(halfAngle);
            }

            return new PoseMsg
            {
                position = new PointMsg { x = pose[0], y = pose[1], z = pose[2] },
                orientation = new QuaternionMsg { x = qx, y = qy, z = qz, w = qw }
            };
        }
    }
}