using System;

namespace Rasa.Structures.Missions
{
    using Data;
    using Structures.World;

    public sealed class MissionActionDefinition
    {
        public uint MissionId { get; init; }
        public string ContentRevision { get; init; }
        public uint ObjectiveId { get; init; }
        public uint TransitionId { get; init; }
        public uint ActionId { get; init; }
        public MissionContentRequirement Requirement { get; init; }
        public MissionActionKind Kind { get; init; }
        public uint Sequence { get; init; }
        public uint? TargetObjectiveId { get; init; }
        public byte? ObjectiveStateValue { get; init; }
        public uint? RewardId { get; init; }
        public uint? SpawnGroupId { get; init; }
        public uint? ScenarioId { get; init; }
        public uint? IndicatorId { get; init; }
        public uint? PlayerFlagId { get; init; }
        public uint? PlayerFlagValue { get; init; }
        public uint? NpcPackageId { get; init; }
        public string Comment { get; init; }
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public global::Rasa.Missions.Scenes.CharacterIntent ItemIntent { get; init; }

        public bool HasDefinedKind() =>
            Enum.IsDefined(typeof(MissionActionKind), Kind);

        public bool TryGetObjectiveState(out MissionObjectiveState state)
        {
            if (ObjectiveStateValue.HasValue &&
                Enum.IsDefined(typeof(MissionObjectiveState), (int)ObjectiveStateValue.Value))
            {
                state = (MissionObjectiveState)ObjectiveStateValue.Value;
                return true;
            }

            state = default;
            return false;
        }
    }
}
