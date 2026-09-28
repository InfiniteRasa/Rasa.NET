using System.Collections.Generic;
using System.Linq;

namespace Rasa.Structures
{
    using Data;
    using Game.Missions.Protocol;

    internal sealed class MissionConversationState
    {
        private readonly IReadOnlyDictionary<uint, MissionInfo> _dispensable;
        internal IReadOnlyList<MissionDialoguePresentation> Dialogue { get; }
        private readonly IReadOnlyDictionary<uint, RewardInfo> _completeable;
        private readonly IReadOnlyList<RewardableMissions> _rewardable;

        internal MissionConversationState(
            IReadOnlyDictionary<uint, MissionInfo> dispensable,
            IReadOnlyList<MissionDialoguePresentation> dialogue,
            IReadOnlyDictionary<uint, RewardInfo> completeable,
            IReadOnlyList<RewardableMissions> rewardable)
        {
            _dispensable = dispensable;
            Dialogue = dialogue;
            _completeable = completeable;
            _rewardable = rewardable;
        }

        internal Dictionary<ConversationType, object> CreateConversationData()
        {
            var data = new Dictionary<ConversationType, object>();
            if (_dispensable.Count > 0)
                data.Add(ConversationType.MissionDispense, new Dictionary<uint, MissionInfo>(_dispensable));
            MissionConversationProjection.AddPayloads(data, Dialogue);
            if (_completeable.Count > 0)
                data.Add(ConversationType.MissionComplete, new Dictionary<uint, RewardInfo>(_completeable));
            if (_rewardable.Count > 0)
                data.Add(ConversationType.MissionReward, _rewardable.ToList());
            return data;
        }

        internal IEnumerable<MissionConversationTopicKey> OfferedTopics()
        {
            foreach (var missionId in _dispensable.Keys)
                yield return new(MissionConversationTopicKind.Acceptance, missionId);
            foreach (var key in Dialogue.Select(topic => topic.Key).Distinct())
                yield return key;
            foreach (var missionId in _completeable.Keys)
                yield return new(MissionConversationTopicKind.MissionCompletion, missionId);
            foreach (var reward in _rewardable)
                yield return new(MissionConversationTopicKind.LegacyReward, (uint)reward.MissionId);
        }

        internal MissionConversationState RetainTopics(IReadOnlyCollection<MissionConversationTopicKey> keys) =>
            new(_dispensable.Where(entry => keys.Contains(new(MissionConversationTopicKind.Acceptance, entry.Key)))
                    .ToDictionary(entry => entry.Key, entry => entry.Value),
                Dialogue.Where(entry => keys.Contains(entry.Key)).ToArray(),
                _completeable.Where(entry => keys.Contains(new(MissionConversationTopicKind.MissionCompletion, entry.Key)))
                    .ToDictionary(entry => entry.Key, entry => entry.Value),
                _rewardable.Where(entry => keys.Contains(new(MissionConversationTopicKind.LegacyReward, (uint)entry.MissionId)))
                    .ToArray());

        internal bool TryGetStatus(
            out ConversationStatus status,
            out List<uint> missionIds)
        {
            if (_rewardable.Count > 0)
            {
                status = ConversationStatus.Reward;
                missionIds = _rewardable.Select(mission => (uint)mission.MissionId).ToList();
                return true;
            }
            if (_completeable.Count > 0)
            {
                status = ConversationStatus.MissionComplete;
                missionIds = _completeable.Keys.ToList();
                return true;
            }
            if (TryGetDialogueStatus(MissionConversationTopicKind.ObjectiveChoice, ConversationStatus.ObjectivChoice,
                    out status, out missionIds) ||
                TryGetDialogueStatus(MissionConversationTopicKind.ObjectiveCompletion, ConversationStatus.ObjectivComplete,
                    out status, out missionIds))
                return true;
            if (_dispensable.Count > 0)
            {
                status = ConversationStatus.Available;
                missionIds = _dispensable.Keys.ToList();
                return true;
            }
            if (TryGetDialogueStatus(MissionConversationTopicKind.MissionReminder, ConversationStatus.MissionReminder,
                    out status, out missionIds) ||
                TryGetDialogueStatus(MissionConversationTopicKind.ObjectiveAmbient, ConversationStatus.ObjectivAMB,
                    out status, out missionIds))
                return true;

            status = ConversationStatus.None;
            missionIds = new List<uint>();
            return false;
        }

        private bool TryGetDialogueStatus(MissionConversationTopicKind kind, ConversationStatus candidate,
            out ConversationStatus status, out List<uint> missionIds)
        {
            status = candidate;
            missionIds = Dialogue.Where(topic => topic.Key.Kind == kind).Select(topic => topic.Key.MissionId).Distinct().ToList();
            return missionIds.Count > 0;
        }
    }
}
