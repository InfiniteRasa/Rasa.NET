using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            var entries = ConvoDataDict?.ToArray()
                ?? throw new InvalidDataException("Conversation data is required.");
            foreach (var entry in entries)
                Validate(entry.Key, entry.Value);

            pw.WriteTuple(1);
            pw.WriteDictionary(entries.Length);
            foreach (var entry in entries)
            {
                pw.WriteInt((int)entry.Key);
                switch (entry.Key)
                {
                    case ConversationType.Greeting:
                    case ConversationType.ImportantGreering:
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
                            mission.Value.WriteOffer(pw);
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
                        var choices = (List<ChoiceObjectives>)entry.Value;
                        pw.WriteList(choices.Count);
                        foreach (var choice in choices)
                        {
                            pw.WriteTuple(3);
                            pw.WriteInt(choice.MissionId);
                            pw.WriteInt(choice.ObjectiveId);
                            pw.WriteInt(choice.PlayerFlagId);
                        }
                        break;

                    case ConversationType.EndConversation:
                    case ConversationType.ForcedByScript:
                        pw.WriteBool((bool)entry.Value);
                        break;

                    case ConversationType.Training:
                        var training = (TrainingConverse)entry.Value;

                        // Two elements. npc.py's Converse unpacks this branch as
                        // "(bCanTrain, dialogId) = convoDataDict[CONVO_TYPE_TRAINING]", and
                        // CanTrain() unpacks it the same way, so a one-element tuple raises
                        // inside the client's own conversation handler and no window opens.
                        //
                        // conversationwindow.py does unpack it as "(bCanTrain,)", but that is
                        // not this data: it reads back the payload _CreateConversationLinkWidget
                        // stored on the topic link, which the window itself built as a 1-tuple.
                        // Only npc.py sees what goes on the wire.
                        pw.WriteTuple(2);
                        pw.WriteBool(training.CanTrain);
                        pw.WriteInt(training.DialogId);

                        break;

                    case ConversationType.Vending:
                        var vendorConverse = (List<uint>)entry.Value;

                        pw.WriteList(1);    // appearantly there can be only 1 vendorPackage per npc
                        pw.WriteUInt(vendorConverse[0]);

                        break;

                    case ConversationType.Clan:
                        var isClanMaster = (bool)entry.Value;
                        pw.WriteBool(isClanMaster);
                        break;

                    case ConversationType.Auctioneer:
                        var isAuctioneer = (bool)entry.Value;
                        pw.WriteBool(isAuctioneer);
                        break;

                    default:
                        throw new InvalidDataException($"Unsupported conversation type {entry.Key}.");
                }
            }
        }

        private static void Validate(ConversationType kind, object value)
        {
            var valid = kind switch
            {
                ConversationType.Greeting or ConversationType.ImportantGreering => value is int greeting && greeting > 0,
                ConversationType.ForceTopic => value is ForceTopic topic &&
                    Enum.IsDefined(typeof(ConversationType), topic.ForceTopicId) && topic.MissionId > 0,
                ConversationType.MissionDispense => value is Dictionary<uint, MissionInfo> offers &&
                    offers.All(entry => ValidId(entry.Key) && ValidOffer(entry.Value)),
                ConversationType.MissionComplete => value is Dictionary<uint, RewardInfo> rewards &&
                    rewards.All(entry => ValidId(entry.Key) && ValidReward(entry.Value)),
                ConversationType.MissionReminder => value is List<uint> reminders && reminders.All(ValidId),
                ConversationType.ObjectiveAmbient => value is List<AmbientObjectives> ambient &&
                    ambient.All(entry => entry != null && ValidObjective(entry.MissionId, entry.ObjectiveId, entry.PlayerFlagId)),
                ConversationType.ObjectiveComplete => value is List<CompleteableObjectives> objectives &&
                    objectives.All(entry => entry != null && ValidObjective(entry.MissionId, entry.ObjectiveId, entry.PlayerFlagId)),
                ConversationType.ObjectiveChoice => value is List<ChoiceObjectives> choices &&
                    choices.All(entry => entry != null && ValidObjective(entry.MissionId, entry.ObjectiveId, entry.PlayerFlagId)),
                ConversationType.MissionReward => value is List<RewardableMissions> rewardable &&
                    rewardable.All(entry => entry != null && entry.MissionId > 0 && ValidReward(entry.RewardInfo)) &&
                    rewardable.Select(entry => entry.MissionId).Distinct().Count() == rewardable.Count,
                ConversationType.Training => value is TrainingConverse training && training.DialogId > 0,
                ConversationType.Vending => value is List<uint> vendor && vendor.Count == 1 && ValidId(vendor[0]),
                ConversationType.EndConversation or ConversationType.Clan or ConversationType.Auctioneer or
                    ConversationType.ForcedByScript => value is bool,
                _ => false
            };
            if (!valid)
                throw new InvalidDataException($"Invalid payload for conversation type {kind}.");
        }

        private static bool ValidId(uint id) => id > 0 && id <= int.MaxValue;
        private static bool ValidObjective(int missionId, int objectiveId, int flagId) =>
            missionId > 0 && objectiveId > 0 && flagId >= 0;

        private static bool ValidOffer(MissionInfo offer) =>
            offer?.MissionConstantData != null && ValidReward(offer.MissionConstantData.RewardInfo) &&
            offer.AudioSetId >= 0 && offer.ItemRequired != null && offer.ItemRequired.All(id => id > 0) &&
            offer.ObjectivesList != null && offer.ObjectivesList.All(objective => objective != null && ValidId(objective.ObjectiveId));

        private static bool ValidReward(RewardInfo reward) =>
            reward?.FixedReward?.Credits != null && reward.FixedReward.FixedItems != null &&
            reward.SelectableReward != null &&
            reward.FixedReward.FixedItems.Concat(reward.SelectableReward).All(item => item?.ModuleIds != null);
    }
}
