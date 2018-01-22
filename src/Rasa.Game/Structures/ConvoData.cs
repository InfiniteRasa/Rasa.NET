using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;

    public class ConvoData
    {
        public int GreetingId { get; set; }                                         // 0
        public ForceTopic ForceTopic { get; set; }                                  // 1
        public List<DispensableMissions> DispensableMissions { get; set; }          // 2    
        public List<CompleteableMissions> CompleteableMissions { get; set; }        // 3
        public List<int> RemindableMissions { get; set; }                           // 4
        public List<AmbientObjectives> AmbientObjectives { get; set; }              // 5
        public List<CompleteableObjectives> CompleteableObjectives { get; set; }    // 6
        public List<RewardableMissions> RewardableMissions { get; set; }            // 7
        public TrainingConverse Training { get; set; }                              // 10
        public List<int> VendorConverse { get; set; }                               // 11
        public bool IsAuctioneer { get; set; }                                      // 14

        public ConvoData(int greetingId)
        {
            GreetingId = greetingId;
        }

        public ConvoData(ForceTopic forceTopic)
        {
            ForceTopic = forceTopic;
        }

        public ConvoData(List<DispensableMissions> dispensableMissions)
        {
            DispensableMissions = dispensableMissions;
        }

        public ConvoData(List<CompleteableMissions> completeableMissions)
        {
            CompleteableMissions = completeableMissions;
        }

        public ConvoData(List<int> remindableMissions)
        {
            RemindableMissions = remindableMissions;
        }

        public ConvoData(List<CompleteableObjectives> completeableObjectives)
        {
            CompleteableObjectives = completeableObjectives;
        }

        public ConvoData(List<AmbientObjectives> ambientObjectives)
        {
            AmbientObjectives = ambientObjectives;
        }

        public ConvoData(List<RewardableMissions> rewardableMissions)
        {
            RewardableMissions = rewardableMissions;
        }

        public ConvoData(TrainingConverse training)
        {
            Training = training;
        }

        public ConvoData()
        {
        }
    }

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

    public class DispensableMissions
    {
        public int MissionId { get; set; }
        public MissionInfo MissionInfo { get; set; }

        public DispensableMissions(int missionId, MissionInfo missionInfo)
        {
            MissionId = missionId;
            MissionInfo = missionInfo;
        }
    }

    public class CompleteableMissions
    {
        public int MissionId { get; set; }
        public RewardInfo RewardInfo { get; set; }

        public CompleteableMissions(int missionId, RewardInfo rewardInfo)
        {
            MissionId = missionId;
            RewardInfo = rewardInfo;
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
