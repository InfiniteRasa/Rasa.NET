using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;
    using Memory;

    public class MissionInfo : IPythonDataStruct
    {
        public MissionState MissionState { get; set; }
        public bool Completeable { get; set; }
        public MissionConstantData MissionConstantData { get; set; } = new MissionConstantData();
        public int ChangeTime { get; set; }
        public List<MissionObjective> ObjectivesList = new List<MissionObjective>();
        // required only for DispensableMissions
        public int AudioSetId { get; set; }
        public List<int> ItemRequired = new List<int>();

        public void Read(PythonReader pr)
        {
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(5);                       // missionInfo = (missionStatus, bCompleteable, constantData, changeTime, objectiveList)
            pw.WriteInt((int)MissionState);         // missionStatus
            pw.WriteBool(Completeable);             // bCompleteable
            pw.WriteStruct(MissionConstantData);    // constantData = (missionLevel, groupType, missionCategoryId, bShareable, bRadioCompleteable, rewardInfo)
            pw.WriteInt(0);                         // changeTime ToDo: sims that client dont use 'changeTime'
            pw.WriteList(0);                        // ToDo
        }
    }

    public class MissionConstantData : IPythonDataStruct
    {
        public int Level { get; set; }  // it's mission level, not required XP level
        public int GroupType { get; set; }
        public int MissionCategoryId { get; set; }
        public bool Shareable { get; set; }
        public bool RadioCompleteable { get; set; }
        public RewardInfo RewardInfo = new RewardInfo();

        public void Read(PythonReader pr)
        {
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(6);
            pw.WriteInt(Level);
            pw.WriteInt(GroupType);
            pw.WriteInt(MissionCategoryId);
            pw.WriteBool(Shareable);
            pw.WriteBool(RadioCompleteable);
            pw.WriteStruct(RewardInfo);
        }
    }

    public class RewardInfo : IPythonDataStruct
    {
        public FixedReward FixedReward = new FixedReward();
        public List<RewardItem> SelectableReward = new List<RewardItem>();

        public void Read(PythonReader pr)
        {
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteStruct(FixedReward);
            pw.WriteList(SelectableReward.Count); // selectionList
            foreach (var item in SelectableReward)
                pw.WriteStruct(item);
        }
    }

    public class FixedReward :IPythonDataStruct
    {
        public List<Curency> Credits = new List<Curency>();
        public List<RewardItem> FixedItems = new List<RewardItem>();

        public void Read(PythonReader pr)
        {
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteList(Credits.Count);
            foreach (var credit in Credits)
                pw.WriteStruct(credit);
            pw.WriteList(FixedItems.Count);
            foreach (var item in FixedItems)
                pw.WriteStruct(item);
        }
    }

    public class RewardItem : IPythonDataStruct
    {
        public uint ItemTemplateId { get; set; }
        public EntityClassId Class { get; set; }
        public int Quantity { get; set; }
        // public int Hue { get; set; }    // not used by client
        public List<int> ModuleIds = new List<int>();
        public int QualityId { get; set; }

        public void Read(PythonReader pr)
        {
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(6);
            pw.WriteLong(ItemTemplateId);
            pw.WriteUInt((uint)Class);
            pw.WriteInt(Quantity);
            pw.WriteNoneStruct();                  // hue      (not used by client)
            pw.WriteList(ModuleIds.Count);
            foreach (var module in ModuleIds)
                pw.WriteInt(module);
            pw.WriteInt(QualityId);
        }
    }
}
