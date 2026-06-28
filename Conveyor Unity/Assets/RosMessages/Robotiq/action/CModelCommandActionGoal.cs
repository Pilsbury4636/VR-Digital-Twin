using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Robotiq
{
    public class CModelCommandActionGoal : ActionGoal<CModelCommandGoal>
    {
        public const string k_RosMessageName = "robotiq_msgs/CModelCommandActionGoal";
        public override string RosMessageName => k_RosMessageName;


        public CModelCommandActionGoal() : base()
        {
            this.goal = new CModelCommandGoal();
        }

        public CModelCommandActionGoal(HeaderMsg header, GoalIDMsg goal_id, CModelCommandGoal goal) : base(header, goal_id)
        {
            this.goal = goal;
        }
        public static CModelCommandActionGoal Deserialize(MessageDeserializer deserializer) => new CModelCommandActionGoal(deserializer);

        CModelCommandActionGoal(MessageDeserializer deserializer) : base(deserializer)
        {
            this.goal = CModelCommandGoal.Deserialize(deserializer);
        }
        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.header);
            serializer.Write(this.goal_id);
            serializer.Write(this.goal);
        }


#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        public static void Register()
        {
            MessageRegistry.Register(k_RosMessageName, Deserialize);
        }
    }
}
