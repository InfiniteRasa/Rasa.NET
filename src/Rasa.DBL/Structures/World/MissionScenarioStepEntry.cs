using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Rasa.Structures.Char;

    public enum MissionScenarioStepKind : byte
    {
        SpawnGroup = 1,
        DespawnGroup = 2,
        EnableInteraction = 3,
        DisableInteraction = 4,
        RevealObjective = 5,
        ActivateObjective = 6,
        CompleteObjective = 7,
        FailObjective = 8,
        StartDeadline = 9,
        CancelDeadline = 10,
        GrantRewardPackage = 11,
        GrantSkillAbility = 12,
        PlayTutorial = 13,
        ScheduleScenario = 14,
        ResetAttempt = 15,
        EmitScenarioEvent = 16,
        TransferPlayer = 17,
        SetQualification = 18,
        SetAccountSkipEntitlement = 19,
        SpawnDynamicObject = 20,
        DespawnDynamicObject = 21,
        EscortSpawnGroup = 22,
        SatisfyDeadline = 23
    }

    [Table(TableName)]
    public class MissionScenarioStepEntry
    {
        public const string TableName = "mission_scenario_step";
        public const uint MaxDelayMilliseconds = 86_400_000;
        public const uint MaxAbilityId = int.MaxValue;
        public const byte MaxSkillLevel = 5;
        public const byte MaxAbilitySlot = 24;
        public const byte MinimumQualificationKey = 1;
        public const byte MaximumQualificationKey = byte.MaxValue;
        public const byte RemovedQualificationValue = 0;
        public const byte GrantedQualificationValue = 1;

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("content_revision", TypeName = "varchar(32)")]
        [Required]
        public string ContentRevision { get; set; } = string.Empty;

        [Column("scenario_id")]
        [Required]
        public uint ScenarioId { get; set; }

        [Column("step_id")]
        [Required]
        public uint StepId { get; set; }

        [Column("requirement")]
        [Required]
        public MissionContentRequirement Requirement { get; set; } =
            MissionContentRequirement.Required;

        [Column("kind")]
        [Required]
        public MissionScenarioStepKind Kind { get; set; }

        [Column("sequence")]
        [Required]
        public uint Sequence { get; set; }

        [Column("target_objective_id")]
        public uint? TargetObjectiveId { get; set; }

        [Column("reward_id")]
        public uint? RewardId { get; set; }

        [Column("spawn_group_id")]
        public uint? SpawnGroupId { get; set; }

        [Column("spawn_id")]
        public uint? SpawnId { get; set; }

        [Column("dynamic_object_key", TypeName = "varchar(64)")]
        public string DynamicObjectKey { get; set; }

        [Column("entity_class_id")]
        public uint? EntityClassId { get; set; }

        [Column("target_scenario_id")]
        public uint? TargetScenarioId { get; set; }

        [Column("delay_milliseconds")]
        public uint? DelayMilliseconds { get; set; }

        [Column("skill_id")]
        public uint? SkillId { get; set; }

        [Column("ability_id")]
        public uint? AbilityId { get; set; }

        [Column("skill_level")]
        public byte? SkillLevel { get; set; }

        [Column("ability_slot")]
        public byte? AbilitySlot { get; set; }

        [Column("tutorial_id")]
        public uint? TutorialId { get; set; }

        [Column("audio_set_id")]
        public uint? AudioSetId { get; set; }

        [Column("attempt_key", TypeName = "varchar(64)")]
        public string AttemptKey { get; set; }

        [Column("scenario_event_id")]
        public uint? ScenarioEventId { get; set; }

        [Column("map_context_id")]
        public uint? MapContextId { get; set; }

        [Column("pos_x")]
        public double? PosX { get; set; }

        [Column("pos_y")]
        public double? PosY { get; set; }

        [Column("pos_z")]
        public double? PosZ { get; set; }

        [Column("orientation")]
        public double? Orientation { get; set; }

        [Column("initial_interaction_enabled", TypeName = "bit")]
        public bool? InitialInteractionEnabled { get; set; }

        [Column("qualification_key")]
        public CharacterQualificationKey? QualificationKey { get; set; }

        [Column("qualification_value")]
        public byte? QualificationValue { get; set; }

        [Column("account_skip_entitlement", TypeName = "bit")]
        public bool? AccountSkipEntitlement { get; set; }

        [Column("comment", TypeName = "varchar(64)")]
        [Required]
        public string Comment { get; set; } = string.Empty;

        public MissionScenarioEntry Scenario { get; set; }
    }
}
