namespace Rasa.Structures
{
    using Data;

    public class ForceTopic
    {
        public ConversationType ForceTopicId { get; set; }
        public int MissionId { get; set; }

        public ForceTopic(ConversationType forceTopicId, int missionId)
        {
            ForceTopicId = forceTopicId;
            MissionId = missionId;
        }
    }
    
    public class AmbientObjectives
    {
        public int MissionId { get; set; }
        public int ObjectiveId { get; set; }
        public int PlayerFlagId { get; set; }

        public AmbientObjectives(int missionId, int objectiveId, int playerFlagId)
        {
            MissionId = missionId;
            ObjectiveId = objectiveId;
            PlayerFlagId = playerFlagId;
        }
    }

    public class CompleteableObjectives
    {
        public int MissionId { get; set; }
        public int ObjectiveId { get; set; }
        public int PlayerFlagId { get; set; }

        public CompleteableObjectives(int missionId, int objectiveId, int playerFlagId)
        {
            MissionId = missionId;
            ObjectiveId = objectiveId;
            PlayerFlagId = playerFlagId;
        }
    }

    public class RewardableMissions
    {
        public int MissionId { get; set; }
        public RewardInfo RewardInfo { get; set; }

        public RewardableMissions(int missionId, RewardInfo rewardInfo)
        {
            MissionId = missionId;
            RewardInfo = rewardInfo;
        }
    }

    public class TrainingConverse
    {
        public bool CanTrain { get; set; }
        public int DialogId { get; set; }

        public TrainingConverse(bool canTrain, int dialogId)
        {
            CanTrain = canTrain;
            DialogId = dialogId;
        }
    }

    public class MissionObjectives
    {
        // public int Ordinal { get; set; }    // not used by client
        public int ObjectiveId { get; set; }

        public MissionObjectives(int objectiveId)
        {
            ObjectiveId = objectiveId;
        }
    }
}
