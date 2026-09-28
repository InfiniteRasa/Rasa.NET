using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Rasa.Structures.Missions
{
    using Data;
    using Rasa.Structures.Char;
    using Structures.World;

    internal sealed class MissionScenarioDefinition
    {
        public uint MissionId { get; }
        public string ContentRevision { get; }
        public uint ScenarioId { get; }
        public MissionContentRequirement Requirement { get; }
        public MissionScenarioStartPolicy StartPolicy { get; }
        public string Name { get; }
        public string Comment { get; }
        public IReadOnlyList<MissionScenarioStepDefinition> Steps { get; }

        public MissionScenarioDefinition(
            MissionScenarioEntry entry,
            IEnumerable<MissionScenarioStepDefinition> steps)
        {
            MissionId = entry.MissionId;
            ContentRevision = entry.ContentRevision;
            ScenarioId = entry.ScenarioId;
            Requirement = entry.Requirement;
            StartPolicy = entry.StartPolicy;
            Name = entry.Name;
            Comment = entry.Comment;
            Steps = new ReadOnlyCollection<MissionScenarioStepDefinition>(
                (steps ?? Array.Empty<MissionScenarioStepDefinition>())
                .OrderBy(step => step.Sequence)
                .ThenBy(step => step.StepId)
                .ToArray());
        }
    }

    internal sealed class MissionScenarioStepDefinition
    {
        public uint MissionId { get; }
        public string ContentRevision { get; }
        public uint ScenarioId { get; }
        public uint StepId { get; }
        public MissionContentRequirement Requirement { get; }
        public MissionScenarioStepKind Kind { get; }
        public uint Sequence { get; }
        public uint? TargetObjectiveId { get; }
        public uint? RewardId { get; }
        public uint? SpawnGroupId { get; }
        public uint? SpawnId { get; }
        public string DynamicObjectKey { get; }
        public uint? EntityClassId { get; }
        public uint? TargetScenarioId { get; }
        public uint? DelayMilliseconds { get; }
        public uint? SkillId { get; }
        public uint? AbilityId { get; }
        public byte? SkillLevel { get; }
        public byte? AbilitySlot { get; }
        public uint? TutorialId { get; }
        public uint? AudioSetId { get; }
        public string AttemptKey { get; }
        public uint? ScenarioEventId { get; }
        public uint? MapContextId { get; }
        public double? PosX { get; }
        public double? PosY { get; }
        public double? PosZ { get; }
        public double? Orientation { get; }
        public bool? InitialInteractionEnabled { get; }
        public CharacterQualificationKey? QualificationKey { get; }
        public byte? QualificationValue { get; }
        public bool? AccountSkipEntitlement { get; }
        public string Comment { get; }

        public MissionScenarioStepDefinition(MissionScenarioStepEntry entry)
        {
            MissionId = entry.MissionId;
            ContentRevision = entry.ContentRevision;
            ScenarioId = entry.ScenarioId;
            StepId = entry.StepId;
            Requirement = entry.Requirement;
            Kind = entry.Kind;
            Sequence = entry.Sequence;
            TargetObjectiveId = entry.TargetObjectiveId;
            RewardId = entry.RewardId;
            SpawnGroupId = entry.SpawnGroupId;
            SpawnId = entry.SpawnId;
            DynamicObjectKey = entry.DynamicObjectKey;
            EntityClassId = entry.EntityClassId;
            TargetScenarioId = entry.TargetScenarioId;
            DelayMilliseconds = entry.DelayMilliseconds;
            SkillId = entry.SkillId;
            AbilityId = entry.AbilityId;
            SkillLevel = entry.SkillLevel;
            AbilitySlot = entry.AbilitySlot;
            TutorialId = entry.TutorialId;
            AudioSetId = entry.AudioSetId;
            AttemptKey = entry.AttemptKey;
            ScenarioEventId = entry.ScenarioEventId;
            MapContextId = entry.MapContextId;
            PosX = entry.PosX;
            PosY = entry.PosY;
            PosZ = entry.PosZ;
            Orientation = entry.Orientation;
            InitialInteractionEnabled = entry.InitialInteractionEnabled;
            QualificationKey = entry.QualificationKey;
            QualificationValue = entry.QualificationValue;
            AccountSkipEntitlement = entry.AccountSkipEntitlement;
            Comment = entry.Comment;
        }

        public bool HasDefinedKind() =>
            Enum.IsDefined(typeof(MissionScenarioStepKind), Kind);

        public bool TryGetTutorialId(out global::Rasa.Data.TutorialId tutorialId)
        {
            if (TutorialId.HasValue &&
                TutorialId.Value <= int.MaxValue &&
                Enum.IsDefined(typeof(global::Rasa.Data.TutorialId), (int)TutorialId.Value))
            {
                tutorialId = (global::Rasa.Data.TutorialId)(int)TutorialId.Value;
                return true;
            }

            tutorialId = default;
            return false;
        }

        public bool TryGetQualificationKey(out CharacterQualificationKey qualificationKey)
        {
            if (QualificationKey.HasValue &&
                Enum.IsDefined(typeof(CharacterQualificationKey), QualificationKey.Value))
            {
                qualificationKey = QualificationKey.Value;
                return true;
            }

            qualificationKey = default;
            return false;
        }
    }
}
