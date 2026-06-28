using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Robotiq
{
    public class CModelCommandActionFeedback : ActionFeedback<CModelCommandFeedback>
    {
        public const string k_RosMessageName = "robotiq_msgs/CModelCommandActionFeedback";
        public override string RosMessageName => k_RosMessageName;


        public CModelCommandActionFeedback() : base()
        {
            this.feedback = new CModelCommandFeedback();
        }

        public CModelCommandActionFeedback(HeaderMsg header, GoalStatusMsg status, CModelCommandFeedback feedback) : base(header, status)
        {
            this.feedback = feedback;
        }
        public static CModelCommandActionFeedback Deserialize(MessageDeserializer deserializer) => new CModelCommandActionFeedback(deserializer);

        CModelCommandActionFeedback(MessageDeserializer deserializer) : base(deserializer)
        {
            this.feedback = CModelCommandFeedback.Deserialize(deserializer);
        }
        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.header);
            serializer.Write(this.status);
            serializer.Write(this.feedback);
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
