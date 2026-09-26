using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;

namespace Rasa.Structures.Missions
{
    using Data;
    using Structures;
    using Structures.World;

    internal sealed class MissionContentSnapshot
    {
        public IReadOnlyDictionary<uint, MissionContentDefinition> Definitions { get; }
        public IReadOnlyDictionary<uint, IReadOnlyList<string>> RevisionsByMission { get; }

        public MissionContentSnapshot(
            IReadOnlyDictionary<uint, MissionContentDefinition> definitions,
            IReadOnlyDictionary<uint, IReadOnlyList<string>> revisionsByMission)
        {
            Definitions = new ReadOnlyDictionary<uint, MissionContentDefinition>(
                new Dictionary<uint, MissionContentDefinition>(
                    definitions ?? new Dictionary<uint, MissionContentDefinition>()));
            RevisionsByMission = new ReadOnlyDictionary<uint, IReadOnlyList<string>>(
                new Dictionary<uint, IReadOnlyList<string>>(
                    revisionsByMission ?? new Dictionary<uint, IReadOnlyList<string>>()));
        }
    }

    internal sealed class MissionContentDefinition
    {
        public uint MissionId { get; }
        public string ContentRevision { get; }
        public MissionContentRequirement Requirement { get; }
        public MissionAbandonmentPolicy AbandonmentPolicy { get; }
        public Mission Mission { get; }
        public IReadOnlyDictionary<uint, MissionObjectiveDefinition> Objectives => Mission.Objectives;
        public IReadOnlyList<uint> DuplicateObjectiveIds { get; }
        public IReadOnlyList<MissionPrerequisiteDefinition> Prerequisites { get; }
        public IReadOnlyDictionary<(uint ObjectiveId, uint TransitionId), MissionObjectiveTransitionDefinition> Transitions { get; }
        public IReadOnlyDictionary<uint, MissionAuthoringRewardDefinition> Rewards { get; }
        public IReadOnlyDictionary<uint, MissionAreaDefinition> Areas { get; }
        public IReadOnlyDictionary<uint, MissionSpawnGroupDefinition> SpawnGroups { get; }
        public IReadOnlyDictionary<uint, MissionScenarioDefinition> Scenarios { get; }

        public MissionContentDefinition(
            uint missionId,
            string contentRevision,
            MissionContentRequirement requirement,
            Mission mission,
            IEnumerable<uint> duplicateObjectiveIds,
            IEnumerable<MissionPrerequisiteDefinition> prerequisites,
            IReadOnlyDictionary<(uint ObjectiveId, uint TransitionId), MissionObjectiveTransitionDefinition> transitions,
            IReadOnlyDictionary<uint, MissionAuthoringRewardDefinition> rewards,
            IReadOnlyDictionary<uint, MissionAreaDefinition> areas,
            IReadOnlyDictionary<uint, MissionSpawnGroupDefinition> spawnGroups,
            IReadOnlyDictionary<uint, MissionScenarioDefinition> scenarios,
            MissionAbandonmentPolicy abandonmentPolicy = MissionAbandonmentPolicy.Allowed)
        {
            MissionId = missionId;
            ContentRevision = contentRevision;
            Requirement = requirement;
            AbandonmentPolicy = abandonmentPolicy;
            Mission = mission;
            DuplicateObjectiveIds = Array.AsReadOnly(
                (duplicateObjectiveIds ?? Array.Empty<uint>())
                .Distinct()
                .OrderBy(value => value)
                .ToArray());
            Prerequisites = Array.AsReadOnly(
                (prerequisites ?? Array.Empty<MissionPrerequisiteDefinition>())
                .OrderBy(prerequisite => prerequisite.PrerequisiteId)
                .ToArray());
            Transitions = new ReadOnlyDictionary<(uint ObjectiveId, uint TransitionId), MissionObjectiveTransitionDefinition>(
                new Dictionary<(uint ObjectiveId, uint TransitionId), MissionObjectiveTransitionDefinition>(
                    transitions ?? new Dictionary<(uint ObjectiveId, uint TransitionId), MissionObjectiveTransitionDefinition>()));
            Rewards = new ReadOnlyDictionary<uint, MissionAuthoringRewardDefinition>(
                new Dictionary<uint, MissionAuthoringRewardDefinition>(
                    rewards ?? new Dictionary<uint, MissionAuthoringRewardDefinition>()));
            Areas = new ReadOnlyDictionary<uint, MissionAreaDefinition>(
                new Dictionary<uint, MissionAreaDefinition>(
                    areas ?? new Dictionary<uint, MissionAreaDefinition>()));
            SpawnGroups = new ReadOnlyDictionary<uint, MissionSpawnGroupDefinition>(
                new Dictionary<uint, MissionSpawnGroupDefinition>(
                    spawnGroups ?? new Dictionary<uint, MissionSpawnGroupDefinition>()));
            Scenarios = new ReadOnlyDictionary<uint, MissionScenarioDefinition>(
                new Dictionary<uint, MissionScenarioDefinition>(
                    scenarios ?? new Dictionary<uint, MissionScenarioDefinition>()));
        }
    }

    internal sealed class MissionPrerequisiteDefinition
    {
        public uint MissionId { get; }
        public string ContentRevision { get; }
        public uint PrerequisiteId { get; }
        public MissionContentRequirement Requirement { get; }
        public MissionPrerequisiteKind Kind { get; }
        public uint? RequiredMissionId { get; }
        public byte? RequiredMissionStateValue { get; }
        public uint? RequiredLevel { get; }
        public uint? PlayerFlagId { get; }
        public uint? PlayerFlagValue { get; }
        public string Comment { get; }

        public MissionPrerequisiteDefinition(MissionPrerequisiteEntry entry)
        {
            MissionId = entry.MissionId;
            ContentRevision = entry.ContentRevision;
            PrerequisiteId = entry.PrerequisiteId;
            Requirement = entry.Requirement;
            Kind = entry.Kind;
            RequiredMissionId = entry.RequiredMissionId;
            RequiredMissionStateValue = entry.RequiredMissionState;
            RequiredLevel = entry.RequiredLevel;
            PlayerFlagId = entry.PlayerFlagId;
            PlayerFlagValue = entry.PlayerFlagValue;
            Comment = entry.Comment;
        }
    }

    internal sealed class MissionObjectiveTransitionDefinition
    {
        public uint MissionId { get; }
        public string ContentRevision { get; }
        public uint ObjectiveId { get; }
        public uint TransitionId { get; }
        public MissionContentRequirement Requirement { get; }
        public uint Sequence { get; }
        public byte? FromStateValue { get; }
        public byte? ToStateValue { get; }
        public string Comment { get; }
        public IReadOnlyList<MissionTriggerDefinition> Triggers { get; }
        public IReadOnlyList<MissionActionDefinition> Actions { get; }

        public MissionObjectiveTransitionDefinition(
            MissionObjectiveTransitionEntry entry,
            IEnumerable<MissionTriggerDefinition> triggers,
            IEnumerable<MissionActionDefinition> actions)
        {
            MissionId = entry.MissionId;
            ContentRevision = entry.ContentRevision;
            ObjectiveId = entry.ObjectiveId;
            TransitionId = entry.TransitionId;
            Requirement = entry.Requirement;
            Sequence = entry.Sequence;
            FromStateValue = entry.FromState;
            ToStateValue = entry.ToState;
            Comment = entry.Comment;
            Triggers = Array.AsReadOnly(
                (triggers ?? Array.Empty<MissionTriggerDefinition>())
                .OrderBy(trigger => trigger.Sequence)
                .ThenBy(trigger => trigger.TriggerId)
                .ToArray());
            Actions = Array.AsReadOnly(
                (actions ?? Array.Empty<MissionActionDefinition>())
                .OrderBy(action => action.Sequence)
                .ThenBy(action => action.ActionId)
                .ToArray());
        }

        public bool TryGetFromState(out MissionObjectiveState state)
        {
            if (FromStateValue.HasValue &&
                Enum.IsDefined(typeof(MissionObjectiveState), (int)FromStateValue.Value))
            {
                state = (MissionObjectiveState)FromStateValue.Value;
                return true;
            }

            state = default;
            return false;
        }

        public bool TryGetToState(out MissionObjectiveState state)
        {
            if (ToStateValue.HasValue &&
                Enum.IsDefined(typeof(MissionObjectiveState), (int)ToStateValue.Value))
            {
                state = (MissionObjectiveState)ToStateValue.Value;
                return true;
            }

            state = default;
            return false;
        }
    }

    internal sealed class MissionAuthoringRewardDefinition
    {
        public uint MissionId { get; }
        public string ContentRevision { get; }
        public uint RewardId { get; }
        public MissionContentRequirement Requirement { get; }
        public uint Experience { get; }
        public uint Credits { get; }
        public uint Prestige { get; }
        public byte SelectionCount { get; }
        public string Comment { get; }
        public IReadOnlyList<MissionAuthoringRewardItemDefinition> FixedItems { get; }
        public IReadOnlyList<MissionAuthoringRewardItemDefinition> SelectableItems { get; }

        public MissionAuthoringRewardDefinition(
            MissionRewardDefinitionEntry entry,
            IEnumerable<MissionAuthoringRewardItemDefinition> fixedItems,
            IEnumerable<MissionAuthoringRewardItemDefinition> selectableItems)
        {
            MissionId = entry.MissionId;
            ContentRevision = entry.ContentRevision;
            RewardId = entry.RewardId;
            Requirement = entry.Requirement;
            Experience = entry.Experience;
            Credits = entry.Credits;
            Prestige = entry.Prestige;
            SelectionCount = entry.SelectionCount;
            Comment = entry.Comment;
            FixedItems = Array.AsReadOnly(
                (fixedItems ?? Array.Empty<MissionAuthoringRewardItemDefinition>())
                .OrderBy(item => item.ItemId)
                .ToArray());
            SelectableItems = Array.AsReadOnly(
                (selectableItems ?? Array.Empty<MissionAuthoringRewardItemDefinition>())
                .OrderBy(item => item.ItemId)
                .ToArray());
        }
    }

    internal readonly struct MissionAuthoringRewardItemDefinition
    {
        public uint MissionId { get; }
        public string ContentRevision { get; }
        public uint RewardId { get; }
        public uint ItemId { get; }
        public MissionRewardItemKind Kind { get; }
        public uint ItemTemplateId { get; }
        public uint Quantity { get; }

        public MissionAuthoringRewardItemDefinition(MissionRewardItemEntry entry)
        {
            MissionId = entry.MissionId;
            ContentRevision = entry.ContentRevision;
            RewardId = entry.RewardId;
            ItemId = entry.ItemId;
            Kind = entry.Kind;
            ItemTemplateId = entry.ItemTemplateId;
            Quantity = entry.Quantity;
        }
    }

    internal sealed class MissionAreaDefinition
    {
        public uint MissionId { get; }
        public string ContentRevision { get; }
        public uint AreaId { get; }
        public MissionContentRequirement Requirement { get; }
        public uint MapContextId { get; }
        public MissionAreaShape Shape { get; }
        public Vector3 Position { get; }
        public double? Radius { get; }
        public double? ExtentX { get; }
        public double? ExtentY { get; }
        public double? ExtentZ { get; }
        public string Comment { get; }

        public MissionAreaDefinition(MissionAreaEntry entry)
        {
            MissionId = entry.MissionId;
            ContentRevision = entry.ContentRevision;
            AreaId = entry.AreaId;
            Requirement = entry.Requirement;
            MapContextId = entry.MapContextId;
            Shape = entry.Shape;
            Position = new Vector3((float)entry.PosX, (float)entry.PosY, (float)entry.PosZ);
            Radius = entry.Radius;
            ExtentX = entry.ExtentX;
            ExtentY = entry.ExtentY;
            ExtentZ = entry.ExtentZ;
            Comment = entry.Comment;
        }
    }

    internal sealed class MissionSpawnGroupDefinition
    {
        public uint MissionId { get; }
        public string ContentRevision { get; }
        public uint SpawnGroupId { get; }
        public MissionContentRequirement Requirement { get; }
        public uint? AreaId { get; }
        public uint MapContextId { get; }
        public bool Enabled { get; }
        public MissionSpawnGroupPolicy SpawnPolicy { get; }
        public uint? RespawnSeconds { get; }
        public string Comment { get; }
        public IReadOnlyList<MissionSpawnDefinition> Spawns { get; }

        public MissionSpawnGroupDefinition(
            MissionSpawnGroupEntry entry,
            IEnumerable<MissionSpawnDefinition> spawns)
        {
            MissionId = entry.MissionId;
            ContentRevision = entry.ContentRevision;
            SpawnGroupId = entry.SpawnGroupId;
            Requirement = entry.Requirement;
            AreaId = entry.AreaId;
            MapContextId = entry.MapContextId;
            Enabled = entry.Enabled;
            SpawnPolicy = entry.SpawnPolicy;
            RespawnSeconds = entry.RespawnSeconds;
            Comment = entry.Comment;
            Spawns = Array.AsReadOnly(
                (spawns ?? Array.Empty<MissionSpawnDefinition>())
                .OrderBy(spawn => spawn.SpawnId)
                .ToArray());
        }
    }

    internal readonly struct MissionSpawnDefinition
    {
        public uint MissionId { get; }
        public string ContentRevision { get; }
        public uint SpawnGroupId { get; }
        public uint SpawnId { get; }
        public uint CreatureId { get; }
        public Vector3 Position { get; }
        public double Rotation { get; }
        public uint Quantity { get; }

        public MissionSpawnDefinition(MissionSpawnEntry entry)
        {
            MissionId = entry.MissionId;
            ContentRevision = entry.ContentRevision;
            SpawnGroupId = entry.SpawnGroupId;
            SpawnId = entry.SpawnId;
            CreatureId = entry.CreatureId;
            Position = new Vector3((float)entry.PosX, (float)entry.PosY, (float)entry.PosZ);
            Rotation = entry.Rotation;
            Quantity = entry.Quantity;
        }
    }
}
