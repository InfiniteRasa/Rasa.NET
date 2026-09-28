using System;

namespace Rasa.Structures.Missions
{
    using Data;
    using Structures.World;

    internal sealed class MissionTriggerDefinition
    {
        public uint MissionId { get; }
        public string ContentRevision { get; }
        public uint ObjectiveId { get; }
        public uint TransitionId { get; }
        public uint TriggerId { get; }
        public MissionContentRequirement Requirement { get; }
        public MissionTriggerKind Kind { get; }
        public uint Sequence { get; }
        public uint? RelatedObjectiveId { get; }
        public byte? RelatedStateValue { get; }
        public byte? EventKindValue { get; }
        public uint? SubjectId { get; }
        public uint? CounterId { get; }
        public uint? InitialValue { get; }
        public uint? TargetValue { get; }
        public uint? AreaId { get; }
        public uint? DurationSeconds { get; }
        public uint? NpcPackageId { get; }
        public uint? PlayerFlagId { get; }
        public bool? SourceSpawnResolved { get; }
        public string Comment { get; }

        public MissionTriggerDefinition(MissionTriggerEntry entry)
        {
            MissionId = entry.MissionId;
            ContentRevision = entry.ContentRevision;
            ObjectiveId = entry.ObjectiveId;
            TransitionId = entry.TransitionId;
            TriggerId = entry.TriggerId;
            Requirement = entry.Requirement;
            Kind = entry.Kind;
            Sequence = entry.Sequence;
            RelatedObjectiveId = entry.RelatedObjectiveId;
            RelatedStateValue = entry.RelatedState;
            EventKindValue = entry.EventKind;
            SubjectId = entry.SubjectId;
            CounterId = entry.CounterId;
            InitialValue = entry.InitialValue;
            TargetValue = entry.TargetValue;
            AreaId = entry.AreaId;
            DurationSeconds = entry.DurationSeconds;
            NpcPackageId = entry.NpcPackageId;
            PlayerFlagId = entry.PlayerFlagId;
            SourceSpawnResolved = entry.SourceSpawnResolved;
            Comment = entry.Comment;
        }

        public bool HasDefinedKind() =>
            Enum.IsDefined(typeof(MissionTriggerKind), Kind);

        public bool TryGetRelatedState(out MissionObjectiveState state)
        {
            if (RelatedStateValue.HasValue &&
                Enum.IsDefined(typeof(MissionObjectiveState), (int)RelatedStateValue.Value))
            {
                state = (MissionObjectiveState)RelatedStateValue.Value;
                return true;
            }

            state = default;
            return false;
        }

        public bool TryGetEventKind(out MissionProgressEventKind kind)
        {
            if (EventKindValue.HasValue &&
                Enum.IsDefined(typeof(MissionProgressEventKind), (int)EventKindValue.Value))
            {
                kind = (MissionProgressEventKind)EventKindValue.Value;
                return true;
            }

            kind = default;
            return false;
        }
    }
}
