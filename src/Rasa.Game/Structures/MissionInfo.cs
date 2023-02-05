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
            pw.WriteInt(0);                         // changeTime ToDo: seems that client dont use 'changeTime'
            pw.WriteList(ObjectivesList.Count);     // objectiveList
            foreach (var objective in ObjectivesList)
            {
                pw.WriteTuple(8);
                pw.WriteUInt(objective.ObjectiveId);                // objectiveId
                pw.WriteUInt(objective.ObjectiveStatus);            // objStatus
                pw.WriteUInt(objective.Ordinal);                    // ordinal
                pw.WriteUInt(objective.TimeRemaining);              // objTime
                pw.WriteNoneStruct();                               // counters
                pw.WriteDictionary(objective.ItemCounters.Count);   // itemCountDict
                {
                    foreach (var entry in objective.ItemCounters)
                    {
                        pw.WriteUInt(entry.Key);
                        pw.WriteTuple(2);
                        pw.WriteUInt(entry.Value.Count);
                        pw.WriteUInt(entry.Value.MaxCount);
                    }
                }
                pw.WriteBool(objective.IsRequired);                 // isRequired
                pw.WriteList(objective.IndicatorList.Count);        // indicatorList
                foreach (var indicator in objective.IndicatorList)
                {
                    pw.WriteTuple(4);
                    pw.WriteTuple(3);                               // position
                        pw.WriteDouble(indicator.Position.X);
                        pw.WriteDouble(indicator.Position.X);
                        pw.WriteDouble(indicator.Position.X);
                    pw.WriteDouble(indicator.Radius);               // radius
                    pw.WriteUInt(indicator.IndicatorId);            // indicatorId
                    pw.WriteBool(indicator.Show3DEffect);           // bShow3DEffect
                }
            }
        }
    }

    public class MissionConstantData : IPythonDataStruct
    {
        public uint Level { get; set; }  // it's mission level, not required XP level
        public byte GroupType { get; set; }
        public byte CategoryId { get; set; }
        public bool Shareable { get; set; }
        public bool RadioCompletable { get; set; }
        public RewardInfo RewardInfo = new RewardInfo();

        public void Read(PythonReader pr)
        {
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(6);
            pw.WriteUInt(Level);
            pw.WriteUInt(GroupType);
            pw.WriteUInt(CategoryId);
            pw.WriteBool(Shareable);
            pw.WriteBool(RadioCompletable);
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
        public Dictionary<CurencyType, uint> Credits = new Dictionary<CurencyType, uint>();
        public List<RewardItem> FixedItems = new List<RewardItem>();

        public void Read(PythonReader pr)
        {
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteList(Credits.Count);
            foreach (var credit in Credits)
            {
                pw.WriteTuple(2);
                pw.WriteInt((int)credit.Key);
                pw.WriteUInt(credit.Value);
            }
            pw.WriteList(FixedItems.Count);
            foreach (var item in FixedItems)
                pw.WriteStruct(item);
        }
    }

    public class RewardItem : IPythonDataStruct
    {
        public uint ItemTemplateId { get; set; }
        public EntityClasses Class { get; set; }
        public uint Quantity { get; set; }
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
            pw.WriteUInt(Quantity);
            pw.WriteNoneStruct();                  // hue      (not used by client)
            pw.WriteList(ModuleIds.Count);
            foreach (var module in ModuleIds)
                pw.WriteInt(module);
            pw.WriteInt(QualityId);
        }
    }
}
