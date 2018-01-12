using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class ConversePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Converse;

        public Dictionary<ConversationType, ConvoDataDict> ConvoDataDict { get; set; }
        
        public ConversePacket(Dictionary<ConversationType, ConvoDataDict> convoDataDict)
        {
            ConvoDataDict = convoDataDict;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(ConvoDataDict.Count);
            foreach (var entry in ConvoDataDict)
            {
                var convoDataDict = entry.Value;
                pw.WriteInt((int)entry.Key);
                switch (entry.Key)
                {
                    case ConversationType.Greeting:
                        pw.WriteInt(convoDataDict.GreetingId);
                        break;

                    case ConversationType.ForceTopic:
                        pw.WriteTuple(2);
                        pw.WriteInt((int)convoDataDict.ForceTopic.ForceTopicId);
                        pw.WriteInt(convoDataDict.ForceTopic.MissionId);
                        break;

                    case ConversationType.MissionDispense:
                        pw.WriteDictionary(convoDataDict.DispensableMissions.Count);                   // = mission list
                        foreach (var mission in convoDataDict.DispensableMissions)                     // ==
                        {                                                                               // ==
                            pw.WriteInt(mission.MissionId);                                             // == missionID
                            pw.WriteTuple(6);                                                           // == mission info
                            pw.WriteInt(mission.MissionInfo.Level);                                     // === level
                            pw.WriteTuple(2);                                                           // === rewardInfo
                            pw.WriteTuple(2);                                                           // ==== (fixed redward tuple)
                            pw.WriteList(mission.MissionInfo.RewardInfo.FixedReward.Credits.Count);     // ===== (credits)
                            foreach (var credit in mission.MissionInfo.RewardInfo.FixedReward.Credits)  // =====
                            {                                                                           // =====
                                pw.WriteTuple(2);                                                       // ===== 
                                pw.WriteInt((int)credit.CurencyType);                                   // ===== credit type
                                pw.WriteInt(credit.Amount);                                             // ===== credit ammount
                            }                                                                           // ====
                            pw.WriteList(mission.MissionInfo.RewardInfo.FixedReward.FixedItems.Count);  // ===== (fixed reward items)
                            foreach (var item in mission.MissionInfo.RewardInfo.FixedReward.FixedItems) // =====
                            {                                                                           // =====
                                pw.WriteTuple(6);                                                       // =====
                                pw.WriteLong(item.ItemTemplateId);                                      // ===== itemTemplateId
                                pw.WriteInt(item.ItemClassId);                                          // ===== itemClassId
                                pw.WriteInt(item.Quantity);                                             // ===== quantity
                                pw.WriteNoneStruct();                                                   // ===== hue    (not used by client)
                                pw.WriteList(item.ModuleIds.Count);                                     // ===== moduleIds
                                foreach (var module in item.ModuleIds)                                  // ======
                                    pw.WriteInt(module);                                                // ====== moduleId
                                pw.WriteInt(item.QualityId);                                            // ===== qualityId
                            }                                                                           // ===
                            pw.WriteList(mission.MissionInfo.RewardInfo.SelectableReward.Count);        // === selection list (selectable reward)
                            foreach (var item in mission.MissionInfo.RewardInfo.SelectableReward)
                            {
                                pw.WriteTuple(6);
                                pw.WriteLong(item.ItemTemplateId);                  // itemTemplateId
                                pw.WriteInt(item.ItemClassId);                      // itemClassId
                                pw.WriteInt(item.Quantity);                         // quantity
                                pw.WriteNoneStruct();                               // hue      (not used by client)
                                pw.WriteList(item.ModuleIds.Count);                 // moduleIds
                                foreach (var module in item.ModuleIds)
                                    pw.WriteInt(module);
                                pw.WriteInt(item.QualityId);                        // qualityId
                            }

                            pw.WriteNoneStruct();                               // offerVOAudioSetId (NoneStruct for no-audio)  // ToDo
                                                                                // itemsRequired
                            pw.WriteList(mission.MissionInfo.ItemRequired.Count);
                            foreach (var item in mission.MissionInfo.ItemRequired)
                                pw.WriteInt(item.ItemClassId);                      // itemClassId
                                                                                    // objectives
                            pw.WriteList(mission.MissionInfo.MissionObjectives.Count);
                            foreach (var objective in mission.MissionInfo.MissionObjectives)
                            {
                                pw.WriteTuple(2);
                                pw.WriteNoneStruct();                   // ordinal      (not used by client)
                                pw.WriteInt(objective.ObjectiveId);     // objectiveId
                            }
                            pw.WriteInt(mission.MissionInfo.GroupType);         // groupType
                        }
                        break;

                    case ConversationType.MissionComplete:
                        pw.WriteDictionary(convoDataDict.CompleteableMissions.Count);
                        foreach ( var mission in convoDataDict.CompleteableMissions)
                        {
                            pw.WriteInt(mission.MissionId);
                            pw.WriteTuple(2);
                            pw.WriteTuple(2);
                            pw.WriteList(mission.RewardInfo.FixedReward.Credits.Count);
                            foreach (var credit in mission.RewardInfo.FixedReward.Credits)
                            {
                                pw.WriteTuple(2);
                                pw.WriteInt((int)credit.CurencyType);
                                pw.WriteInt(credit.Amount);
                            }
                            pw.WriteList(mission.RewardInfo.FixedReward.FixedItems.Count);
                            foreach (var item in mission.RewardInfo.FixedReward.FixedItems)
                            {
                                pw.WriteTuple(6);
                                pw.WriteLong(item.ItemTemplateId);
                                pw.WriteInt(item.ItemClassId);
                                pw.WriteInt(item.Quantity);
                                pw.WriteNoneStruct();
                                pw.WriteList(item.ModuleIds.Count);
                                foreach (var module in item.ModuleIds)
                                    pw.WriteInt(module);
                                pw.WriteInt(item.QualityId);
                            }
                            pw.WriteList(mission.RewardInfo.SelectableReward.Count);
                            foreach (var item in mission.RewardInfo.SelectableReward)
                            {
                                pw.WriteTuple(6);
                                pw.WriteLong(item.ItemTemplateId);
                                pw.WriteInt(item.ItemClassId);
                                pw.WriteInt(item.Quantity);
                                pw.WriteNoneStruct();
                                pw.WriteList(item.ModuleIds.Count);
                                foreach (var module in item.ModuleIds)
                                    pw.WriteInt(module);
                                pw.WriteInt(item.QualityId);
                            }
                        }
                        break;

                    case ConversationType.MissionReminder:
                        pw.WriteList(convoDataDict.RemindableMissions.Count);
                        foreach (var missionId in convoDataDict.RemindableMissions)
                            pw.WriteInt(missionId);
                        break;

                    case ConversationType.ObjectiveAmbient:
                        pw.WriteList(convoDataDict.AmbientObjectives.Count);
                        foreach (var objective in convoDataDict.AmbientObjectives)
                        {
                            pw.WriteTuple(3);
                            pw.WriteInt(objective.MissionId);
                            pw.WriteInt(objective.ObjectiveId);
                            pw.WriteInt(objective.PlayerFlagId);
                        }
                        break;

                    case ConversationType.ObjectiveComplete:
                        pw.WriteList(convoDataDict.CompleteableObjectives.Count);
                        foreach ( var objective in convoDataDict.CompleteableObjectives)
                        {
                            pw.WriteTuple(3);
                            pw.WriteInt(objective.MissionId);
                            pw.WriteInt(objective.ObjectiveId);
                            pw.WriteInt(objective.PlayerFlagId);
                        }
                        break;

                    case ConversationType.MissionReward:
                        pw.WriteDictionary(convoDataDict.RewardableMissions.Count);
                        foreach (var mission in convoDataDict.RewardableMissions)
                        {
                            pw.WriteInt(mission.MissionId);
                            pw.WriteTuple(2);
                            pw.WriteTuple(2);
                            pw.WriteList(mission.RewardInfo.FixedReward.Credits.Count);
                            foreach (var credit in mission.RewardInfo.FixedReward.Credits)
                            {
                                pw.WriteTuple(2);
                                pw.WriteInt((int)credit.CurencyType);
                                pw.WriteInt(credit.Amount);
                            }
                            pw.WriteList(mission.RewardInfo.FixedReward.FixedItems.Count);
                            foreach (var item in mission.RewardInfo.FixedReward.FixedItems)
                            {
                                pw.WriteTuple(6);
                                pw.WriteLong(item.ItemTemplateId);
                                pw.WriteInt(item.ItemClassId);
                                pw.WriteInt(item.Quantity);
                                pw.WriteNoneStruct();
                                pw.WriteList(item.ModuleIds.Count);
                                foreach (var module in item.ModuleIds)
                                    pw.WriteInt(module);
                                pw.WriteInt(item.QualityId);
                            }
                            pw.WriteList(mission.RewardInfo.SelectableReward.Count);
                            foreach (var item in mission.RewardInfo.SelectableReward)
                            {
                                pw.WriteTuple(6);
                                pw.WriteLong(item.ItemTemplateId);
                                pw.WriteInt(item.ItemClassId);
                                pw.WriteInt(item.Quantity);
                                pw.WriteNoneStruct();
                                pw.WriteList(item.ModuleIds.Count);
                                foreach (var module in item.ModuleIds)
                                    pw.WriteInt(module);
                                pw.WriteInt(item.QualityId);
                            }
                        }
                        break;

                    case ConversationType.ObjectiveChoice:
                        Logger.WriteLog(LogType.Debug, $"ConversationType resived = {entry.Key}");
                        break;

                    case ConversationType.EndConversation:
                        Logger.WriteLog(LogType.Debug, $"ConversationType resived = {entry.Key}");
                        break;

                    case ConversationType.Training:
                        Logger.WriteLog(LogType.Debug, $"ConversationType resived = {entry.Key}");
                        pw.WriteTuple(2);
                        pw.WriteBool(convoDataDict.Training.CanTrain);
                        pw.WriteInt(convoDataDict.Training.DialogId);
                        break;

                    case ConversationType.Vending:
                        pw.WriteList(convoDataDict.VendorPackageIds.Count);
                        foreach (var vendorPackageId in convoDataDict.VendorPackageIds)
                            pw.WriteInt(vendorPackageId);
                        Logger.WriteLog(LogType.Debug, $"ConversationType resived = {entry.Key}");
                        break;

                    case ConversationType.ImportantGreering:
                        Logger.WriteLog(LogType.Debug, $"ConversationType resived = {entry.Key}");
                        break;

                    case ConversationType.Clan:
                        Logger.WriteLog(LogType.Debug, $"ConversationType resived = {entry.Key}");
                        break;

                    case ConversationType.Auctioneer:
                        pw.WriteBool(convoDataDict.IsAuctioneer);
                        Logger.WriteLog(LogType.Debug, $"ConversationType resived = {entry.Key}");
                        break;

                    case ConversationType.ForcedByScript:
                        Logger.WriteLog(LogType.Debug, $"ConversationType resived = {entry.Key}");
                        break;

                    default:
                        Logger.WriteLog(LogType.Error, $"Uncnown ConversationType resived = {entry.Key}");
                        break;
                }
            }
        }
    }
}
