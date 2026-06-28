using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;


namespace RosMessageTypes.Robotiq
{
    public class CModelCommandAction : Action<CModelCommandActionGoal, CModelCommandActionResult, CModelCommandActionFeedback, CModelCommandGoal, CModelCommandResult, CModelCommandFeedback>
    {
        public const string k_RosMessageName = "robotiq_msgs/CModelCommandAction";
        public override string RosMessageName => k_RosMessageName;


        public CModelCommandAction() : base()
        {
            this.action_goal = new CModelCommandActionGoal();
            this.action_result = new CModelCommandActionResult();
            this.action_feedback = new CModelCommandActionFeedback();
        }

        public static CModelCommandAction Deserialize(MessageDeserializer deserializer) => new CModelCommandAction(deserializer);

        CModelCommandAction(MessageDeserializer deserializer)
        {
            this.action_goal = CModelCommandActionGoal.Deserialize(deserializer);
            this.action_result = CModelCommandActionResult.Deserialize(deserializer);
            this.action_feedback = CModelCommandActionFeedback.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.action_goal);
            serializer.Write(this.action_result);
            serializer.Write(this.action_feedback);
        }

    }
}
