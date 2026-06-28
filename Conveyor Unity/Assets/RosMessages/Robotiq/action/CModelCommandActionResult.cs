using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Robotiq
{
    public class CModelCommandActionResult : ActionResult<CModelCommandResult>
    {
        public const string k_RosMessageName = "robotiq_msgs/CModelCommandActionResult";
        public override string RosMessageName => k_RosMessageName;


        public CModelCommandActionResult() : base()
        {
            this.result = new CModelCommandResult();
        }

        public CModelCommandActionResult(HeaderMsg header, GoalStatusMsg status, CModelCommandResult result) : base(header, status)
        {
            this.result = result;
        }
        public static CModelCommandActionResult Deserialize(MessageDeserializer deserializer) => new CModelCommandActionResult(deserializer);

        CModelCommandActionResult(MessageDeserializer deserializer) : base(deserializer)
        {
            this.result = CModelCommandResult.Deserialize(deserializer);
        }
        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.header);
            serializer.Write(this.status);
            serializer.Write(this.result);
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
