using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class ConversePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Converse;

        public Dictionary<ConversationType, object> ConvoDataDict { get; set; }
        
        public ConversePacket(Dictionary<ConversationType, object> convoDataDict)
        {
            ConvoDataDict = convoDataDict;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(ConvoDataDict.Count);
            foreach (var entry in ConvoDataDict)
            {
                pw.WriteInt((int)entry.Key);
                switch (entry.Key)
                {
                    case ConversationType.Greeting:
                        var greetingId = (int)entry.Value;

                        pw.WriteInt(greetingId);
                        break;

                    case ConversationType.ForceTopic:
                        var forceTopic = (ForceTopic)entry.Value;

                        pw.WriteTuple(2);
                        pw.WriteInt((int)forceTopic.ForceTopicId);
                        pw.WriteInt(forceTopic.MissionId);

                        break;

                    case ConversationType.MissionDispense:
                        var dispensableMissions = (Dictionary<uint, MissionInfo>)entry.Value;

                        pw.WriteDictionary(dispensableMissions.Count);
                        foreach (var mission in dispensableMissions)
                        {
                            pw.WriteUInt(mission.Key);
                            pw.WriteTuple(6);
                            pw.WriteInt(mission.Value.MissionConstantData.Level);
                            pw.WriteStruct(mission.Value.MissionConstantData.RewardInfo);
                            pw.WriteNoneStruct();                                       // offerVOAudioSetId (NoneStruct for no-audio)  // ToDo
                            pw.WriteList(mission.Value.ItemRequired.Count);       // itemsRequired
                            foreach (var item in mission.Value.ItemRequired)
                                pw.WriteInt(item);                                      // itemClassId
                            pw.WriteList(mission.Value.ObjectivesList.Count);  // objectives
                            foreach (var objective in mission.Value.ObjectivesList)
                            {
                                pw.WriteTuple(2);
                                pw.WriteNoneStruct();                   // ordinal      (not used by client)
                                pw.WriteInt(objective.ObjectiveId);     // objectiveId
                            }
                            pw.WriteInt(mission.Value.MissionConstantData.GroupType);         // groupType
                        }

                        break;

                    case ConversationType.MissionComplete:
                        var completeableMissions = (Dictionary<uint, RewardInfo>)entry.Value;

                        pw.WriteDictionary(completeableMissions.Count);
                        foreach ( var reward in completeableMissions)
                        {
                            pw.WriteUInt(reward.Key);
                            pw.WriteStruct(reward.Value);
                        }

                        break;

                    case ConversationType.MissionReminder:
                        var remindableMissions = (List<uint>)entry.Value;

                        pw.WriteList(remindableMissions.Count);
                        foreach (var missionId in remindableMissions)
                            pw.WriteUInt(missionId);

                        break;

                    case ConversationType.ObjectiveAmbient:
                        var ambientObjectives = (List<AmbientObjectives>)entry.Value;

                        pw.WriteList(ambientObjectives.Count);
                        foreach (var objective in ambientObjectives)
                        {
                            pw.WriteTuple(3);
                            pw.WriteInt(objective.MissionId);
                            pw.WriteInt(objective.ObjectiveId);
                            pw.WriteInt(objective.PlayerFlagId);
                        }

                        break;

                    case ConversationType.ObjectiveComplete:
                        var completeableObjectives = (List<CompleteableObjectives>)entry.Value;

                        pw.WriteList(completeableObjectives.Count);
                        foreach ( var objective in completeableObjectives)
                        {
                            pw.WriteTuple(3);
                            pw.WriteInt(objective.MissionId);
                            pw.WriteInt(objective.ObjectiveId);
                            pw.WriteInt(objective.PlayerFlagId);
                        }

                        break;

                    case ConversationType.MissionReward:
                        var rewardableMissions = (List<RewardableMissions>)entry.Value;

                        pw.WriteDictionary(rewardableMissions.Count);
                        foreach (var mission in rewardableMissions)
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
                        var training = (TrainingConverse)entry.Value;

                        pw.WriteTuple(2);
                        pw.WriteBool(training.CanTrain);
                        pw.WriteInt(training.DialogId);

                        break;

                    case ConversationType.Vending:
                        var vendorConverse = (List<uint>)entry.Value;

                        pw.WriteList(1);    // appearantly there can be only 1 vendorPackage per npc
                        pw.WriteUInt(vendorConverse[0]);

                        break;

                    case ConversationType.ImportantGreering:
                        Logger.WriteLog(LogType.Debug, $"ConversationType resived = {entry.Key}");
                        break;

                    case ConversationType.Clan:
                        var isClanMaster = (bool)entry.Value;
                        pw.WriteBool(isClanMaster);
                        break;

                    case ConversationType.Auctioneer:
                        var isAuctioneer = (bool)entry.Value;
                        pw.WriteBool(isAuctioneer);
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
