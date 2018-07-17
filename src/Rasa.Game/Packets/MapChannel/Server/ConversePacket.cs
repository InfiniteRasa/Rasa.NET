using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class ConversePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Converse;

        public Dictionary<ConversationType, ConvoData> ConvoDataDict { get; set; }
        
        public ConversePacket(Dictionary<ConversationType, ConvoData> convoDataDict)
        {
            ConvoDataDict = convoDataDict;
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
                        pw.WriteDictionary(convoDataDict.DispensableMissions.Count);
                        foreach (var mission in convoDataDict.DispensableMissions)
                        {
                            pw.WriteInt(mission.MissionId);
                            pw.WriteTuple(6);
                            pw.WriteInt(mission.MissionInfo.MissionConstantData.Level);
                            pw.WriteStruct(mission.MissionInfo.MissionConstantData.RewardInfo);
                            pw.WriteNoneStruct();                                       // offerVOAudioSetId (NoneStruct for no-audio)  // ToDo
                            pw.WriteList(mission.MissionInfo.ItemRequired.Count);       // itemsRequired
                            foreach (var item in mission.MissionInfo.ItemRequired)
                                pw.WriteInt(item);                                      // itemClassId
                            pw.WriteList(mission.MissionInfo.ObjectivesList.Count);  // objectives
                            foreach (var objective in mission.MissionInfo.ObjectivesList)
                            {
                                pw.WriteTuple(2);
                                pw.WriteNoneStruct();                   // ordinal      (not used by client)
                                pw.WriteInt(objective.ObjectiveId);     // objectiveId
                            }
                            pw.WriteInt(mission.MissionInfo.MissionConstantData.GroupType);         // groupType
                        }
                        break;

                    case ConversationType.MissionComplete:
                        pw.WriteDictionary(convoDataDict.CompleteableMissions.Count);
                        foreach ( var mission in convoDataDict.CompleteableMissions)
                        {
                            pw.WriteInt(mission.MissionId);
                            pw.WriteStruct(mission.RewardInfo);
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
                            pw.WriteStruct(mission.RewardInfo);
                        }
                        break;

                    case ConversationType.ObjectiveChoice:
                        Logger.WriteLog(LogType.Debug, $"ConversationType resived = {entry.Key}");
                        break;

                    case ConversationType.EndConversation:
                        Logger.WriteLog(LogType.Debug, $"ConversationType resived = {entry.Key}");
                        break;

                    case ConversationType.Training:
                        pw.WriteTuple(2);
                        pw.WriteBool(convoDataDict.Training.CanTrain);
                        pw.WriteInt(convoDataDict.Training.DialogId);
                        break;

                    case ConversationType.Vending:
                        pw.WriteList(1);    // appearantly there can be only 1 vendorPackage per npc
                        pw.WriteInt(convoDataDict.VendorConverse[0]);
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
