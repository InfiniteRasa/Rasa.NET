using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;

    public class ConvoDataDict
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

        public ConvoDataDict(int greetingId)
        {
            GreetingId = greetingId;
        }

        public ConvoDataDict(ForceTopic forceTopic)
        {
            ForceTopic = forceTopic;
        }

        public ConvoDataDict(List<DispensableMissions> dispensableMissions)
        {
            DispensableMissions = dispensableMissions;
        }

        public ConvoDataDict(List<CompleteableMissions> completeableMissions)
        {
            CompleteableMissions = completeableMissions;
        }

        public ConvoDataDict(List<int> remindableMissions)
        {
            RemindableMissions = remindableMissions;
        }

        public ConvoDataDict(List<CompleteableObjectives> completeableObjectives)
        {
            CompleteableObjectives = completeableObjectives;
        }

        public ConvoDataDict(List<AmbientObjectives> ambientObjectives)
        {
            AmbientObjectives = ambientObjectives;
        }

        public ConvoDataDict(List<RewardableMissions> rewardableMissions)
        {
            RewardableMissions = rewardableMissions;
        }

        public ConvoDataDict(TrainingConverse training)
        {
            Training = training;
        }

        public ConvoDataDict()
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
    
    public class FixedReward
    {
        public List<Curency> Credits { get; set; } = new List<Curency>();
        public List<RewardItem> FixedItems { get; set; } = new List<RewardItem>();

        public FixedReward(List<Curency> credits, List<RewardItem> fixedItems)
        {
            Credits = credits;
            FixedItems = fixedItems;
        }
    }

    public class MissionInfo
    {
        public int Level { get; set; }
        public RewardInfo RewardInfo { get; set; }
        public List<MissionObjectives> MissionObjectives { get; set; } = new List<MissionObjectives>();
        public int AudioSetId { get; set; }
        public List<RewardItem> ItemRequired { get; set; }
        public int GroupType { get; set; }

        public MissionInfo (int level, RewardInfo rewardInfo, List<MissionObjectives> missionObjectives, List<RewardItem> itemRequired, int groupType)
        {
            Level = level;
            RewardInfo = rewardInfo;
            MissionObjectives = missionObjectives;
            ItemRequired = itemRequired;
            GroupType = groupType;
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

    public class RewardInfo
    {
        public FixedReward FixedReward { get; set; }
        public List<RewardItem> SelectableReward { get; set; }

        public RewardInfo(FixedReward fixedReward, List<RewardItem> selectableReward)
        {
            FixedReward = fixedReward;
            SelectableReward = selectableReward;
        }
    }

    public class RewardItem
    {
        public int ItemTemplateId { get; set; }
        public int ItemClassId { get; set; }
        public int Quantity { get; set; }
        // public int Hue { get; set; }    // not used by client
        public List<int> ModuleIds { get; set; }
        public int QualityId { get; set; }

        public RewardItem(int itemTemplateId, int itemClassId, int quantity, List<int> moduleIds, int qualityId)
        {
            ItemTemplateId = itemTemplateId;
            ItemClassId = itemClassId;
            Quantity = quantity;
            ModuleIds = moduleIds;
            QualityId = qualityId;
        }

        // item required
        public RewardItem(int itemClassId)
        {
            ItemClassId = itemClassId;
        }
    }

    public class SelectableReward
    {
        public int ItemTemplateId { get; set; }
        public int ItemClassId { get; set; }
        public int Quantity { get; set; }
        // public int Hue { get; set; }    // not used by client
        public List<int> ModuleIds { get; set; }
        public int QualityId { get; set; }

        public SelectableReward(int itemTemplateId, int itemClassId, int quantity, List<int> moduleIds, int qualityId)
        {
            ItemTemplateId = itemTemplateId;
            ItemClassId = itemClassId;
            Quantity = quantity;
            ModuleIds = moduleIds;
            QualityId = qualityId;
        }
    }

    
}
