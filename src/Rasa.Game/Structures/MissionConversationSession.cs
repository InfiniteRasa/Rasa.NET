using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Structures
{
    using Data;
    using Game;
    using Rasa.Missions.Scenes;

    internal enum MissionConversationTopicKind
    {
        Acceptance,
        ObjectiveCompletion,
        MissionCompletion,
        LegacyReward,
        ObjectiveChoice,
        MissionReminder,
        ObjectiveAmbient
    }

    internal sealed record MissionConversationTopicKey(
        MissionConversationTopicKind Kind, uint MissionId, uint ObjectiveId = 0, uint PlayerFlagId = 0);

    internal sealed record MissionConversationTopic(
        MissionConversationTopicKey Key,
        string ContentRevision,
        string AssignmentId,
        string AssignmentRevision,
        uint Generation,
        MissionState? AssignmentState,
        uint ProgressionObjectiveId,
        Rasa.Missions.Definitions.MissionDialogueTopicDefinition Dialogue = null,
        string AdmissionHistoryId = null);

    internal sealed class MissionConversationTarget
    {
        internal Creature Creature { get; }
        internal DynamicObject Object { get; }
        internal ulong EntityId { get; }
        internal uint NpcPackageId { get; }
        internal Npc Npc { get; }
        internal uint CreatureId { get; }
        internal SpawnPool SpawnPool { get; }
        internal SceneObjectConversation ObjectBinding { get; }
        internal uint OwnerCharacterId { get; }
        internal string SceneRunId { get; }
        internal uint SceneGeneration { get; }
        internal string ActorRole { get; }
        internal EntityClasses ClassId { get; }
        internal ActorHandle LeaseHandle { get; }

        internal MissionConversationTarget(Creature creature, ActorHandle leaseHandle = null)
        {
            Creature = creature;
            EntityId = creature.EntityId;
            Npc = creature.Npc;
            NpcPackageId = creature.Npc.NpcPackageId;
            CreatureId = creature.DbId;
            SpawnPool = creature.SpawnPool;
            OwnerCharacterId = SpawnPool?.ScenarioOwnerCharacterId ?? 0;
            SceneRunId = SpawnPool?.SceneRunId;
            SceneGeneration = SpawnPool?.SceneGeneration ?? 0;
            ActorRole = SpawnPool?.SceneActorRole;
            ClassId = creature.EntityClass;
            LeaseHandle = leaseHandle;
        }

        internal MissionConversationTarget(DynamicObject obj)
        {
            Object = obj;
            EntityId = obj.EntityId;
            ObjectBinding = obj.MissionConversation;
            NpcPackageId = ObjectBinding.NpcPackageId;
            OwnerCharacterId = obj.SceneOwnerCharacterId;
            SceneRunId = obj.SceneRunId;
            SceneGeneration = obj.SceneGeneration;
            ActorRole = obj.SceneActorRole;
            ClassId = obj.EntityClassId;
        }

        internal bool Matches(MissionConversationTarget other) =>
            other != null && EntityId == other.EntityId &&
            ReferenceEquals(Creature, other.Creature) && ReferenceEquals(Object, other.Object) &&
            ReferenceEquals(Npc, other.Npc) && NpcPackageId == other.NpcPackageId &&
            CreatureId == other.CreatureId && ReferenceEquals(SpawnPool, other.SpawnPool) &&
            ObjectBinding == other.ObjectBinding && OwnerCharacterId == other.OwnerCharacterId &&
            SceneRunId == other.SceneRunId && SceneGeneration == other.SceneGeneration &&
            ActorRole == other.ActorRole && ClassId == other.ClassId;
    }

    internal sealed class MissionConversationSession
    {
        internal Manifestation Player { get; }
        internal uint CharacterId { get; }
        internal ulong PlayerEntityId { get; }
        internal uint AccountId { get; }
        internal MapChannel Map { get; }
        internal Guid MapEpoch { get; }
        internal MissionConversationTarget Target { get; }
        internal IReadOnlyList<MissionConversationTopic> Topics { get; }

        internal MissionConversationSession(Client client, MissionConversationTarget target,
            IEnumerable<MissionConversationTopic> topics)
        {
            Player = client.Player;
            CharacterId = Player.Id;
            PlayerEntityId = Player.EntityId;
            AccountId = client.AccountEntry.Id;
            Map = Player.MapChannel;
            MapEpoch = Map.MissionEpoch;
            Target = target;
            Topics = Array.AsReadOnly(topics.ToArray());
        }
    }
}
