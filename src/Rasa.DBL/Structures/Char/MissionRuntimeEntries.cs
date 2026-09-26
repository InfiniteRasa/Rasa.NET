using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    [Table("character_mission_history")]
    public sealed class CharacterMissionHistoryEntry
    {
        [Column("character_id")] public uint CharacterId { get; set; }
        [Column("mission_id")] public uint MissionId { get; set; }
        [Required, Column("assignment_id", TypeName = "varchar(32)")] public string AssignmentId { get; set; } = "";
        [Column("assignment_generation")] public uint AssignmentGeneration { get; set; } = 1;
        [Column("content_revision", TypeName = "varchar(32)")] public string ContentRevision { get; set; } = "";
        [Column("completed_at_utc")] public DateTime CompletedAtUtc { get; set; }
        [Column("rewarded")] public bool Rewarded { get; set; }
        [Column("rewarded_at_utc")] public DateTime? RewardedAtUtc { get; set; }
        [Column("reward_window_start_utc")] public DateTime? RewardWindowStartUtc { get; set; }
        [Column("outcome")] public uint Outcome { get; set; } = 4;
    }

    [Table("mission_scene")]
    public sealed class MissionSceneEntry
    {
        [Key, Column("run_id", TypeName = "varchar(32)")] public string RunId { get; set; } = "";
        [Column("release", TypeName = "varchar(32)")] public string Release { get; set; } = "";
        [Column("script_key", TypeName = "varchar(64)")] public string ScriptKey { get; set; } = "";
        [Column("state_version")] public int StateVersion { get; set; }
        [Column("owner_character_id")] public uint OwnerCharacterId { get; set; }
        [Column("mission_id")] public uint MissionId { get; set; }
        [Column("map_key", TypeName = "varchar(64)")] public string MapKey { get; set; } = "";
        [Required, Column("assignment_id", TypeName = "varchar(32)")] public string AssignmentId { get; set; } = "";
        [Column("checkpoint", TypeName = "text")] public string Checkpoint { get; set; } = "{}";
        [Column("status", TypeName = "varchar(16)")] public string Status { get; set; } = "Running";
        [Column("generation")] public uint Generation { get; set; } = 1;
        [ConcurrencyCheck, Column("version")] public long Version { get; set; }
        [Column("fault", TypeName = "text")] public string Fault { get; set; }
    }

    [Table("mission_scene_participant")]
    public sealed class MissionSceneParticipantEntry
    {
        [Column("run_id", TypeName = "varchar(32)")] public string RunId { get; set; } = "";
        [Column("character_id")] public uint CharacterId { get; set; }
        [Column("assignment_id", TypeName = "varchar(32)")] public string AssignmentId { get; set; } = "";
        [Column("assignment_generation")] public uint AssignmentGeneration { get; set; }
        [Column("active")] public bool Active { get; set; } = true;
    }

    [Table("mission_actor_lease")]
    public sealed class MissionActorLeaseEntry
    {
        [Column("map_key", TypeName = "varchar(64)")] public string MapKey { get; set; } = "";
        [Column("spawn_key", TypeName = "varchar(64)")] public string SpawnKey { get; set; } = "";
        [Column("run_id", TypeName = "varchar(32)")] public string RunId { get; set; } = "";
        [Column("actor_role", TypeName = "varchar(64)")] public string ActorRole { get; set; } = "";
        [Column("generation")] public uint Generation { get; set; }
        [Column("state", TypeName = "varchar(16)")] public string State { get; set; } = "Reserved";
        [ConcurrencyCheck, Column("version")] public long Version { get; set; }
    }

    [Table("mission_timer")]
    public sealed class MissionTimerEntry
    {
        [Column("run_id", TypeName = "varchar(32)")] public string RunId { get; set; } = "";
        [Column("name", TypeName = "varchar(64)")] public string Name { get; set; } = "";
        [Column("generation")] public uint Generation { get; set; }
        [Column("mission_id")] public uint? MissionId { get; set; }
        [Column("objective_id")] public uint? ObjectiveId { get; set; }
        [Column("sequence_id")] public uint SequenceId { get; set; }
        [Column("clock_policy", TypeName = "varchar(16)")] public string ClockPolicy { get; set; } = "WallClock";
        [Column("due_at_utc")] public DateTime? DueAtUtc { get; set; }
        [Column("remaining_ticks")] public long? RemainingTicks { get; set; }
        [Column("disposition", TypeName = "varchar(16)")] public string Disposition { get; set; } = "Pending";
        [ConcurrencyCheck, Column("version")] public long Version { get; set; }
    }

    [Table("mission_receipt")]
    public sealed class MissionReceiptEntry
    {
        [Column("owner_id", TypeName = "varchar(32)")] public string OwnerId { get; set; } = "";
        [Column("generation")] public uint Generation { get; set; }
        [Column("operation_key", TypeName = "varchar(96)")] public string OperationKey { get; set; } = "";
        [Column("kind", TypeName = "varchar(16)")] public string Kind { get; set; } = "";
        [Column("created_at_utc")] public DateTime CreatedAtUtc { get; set; }
    }

    [Table("mission_world_effect")]
    public sealed class MissionWorldEffectEntry
    {
        [Column("run_id", TypeName = "varchar(32)")] public string RunId { get; set; } = "";
        [Column("generation")] public uint Generation { get; set; }
        [Column("operation_key", TypeName = "varchar(96)")] public string OperationKey { get; set; } = "";
        [Column("source_run_id", TypeName = "varchar(32)")] public string SourceRunId { get; set; }
        [Column("source_generation")] public uint? SourceGeneration { get; set; }
        [Column("source_assignment_id", TypeName = "varchar(32)")] public string SourceAssignmentId { get; set; }
        [Column("source_assignment_generation")] public uint? SourceAssignmentGeneration { get; set; }
        [Column("payload", TypeName = "text")] public string Payload { get; set; } = "";
        [Column("status", TypeName = "varchar(16)")] public string Status { get; set; } = "Pending";
        [Column("failure", TypeName = "text")] public string Failure { get; set; }
        [ConcurrencyCheck, Column("version")] public long Version { get; set; }
    }

    [Table("mission_outcome")]
    public sealed class MissionOutcomeEntry
    {
        [Key, Column("event_id", TypeName = "varchar(32)")] public string EventId { get; set; } = "";
        [Column("run_id", TypeName = "varchar(32)")] public string RunId { get; set; } = "";
        [Column("generation")] public uint Generation { get; set; }
        [Column("created_at_utc")] public DateTime CreatedAtUtc { get; set; }
    }

    [Table("mission_credit_delivery")]
    public sealed class MissionCreditDeliveryEntry
    {
        [Column("event_id", TypeName = "varchar(32)")] public string EventId { get; set; } = "";
        [Column("assignment_id", TypeName = "varchar(32)")] public string AssignmentId { get; set; } = "";
        [Column("assignment_generation")] public uint AssignmentGeneration { get; set; }
        [Column("character_id")] public uint CharacterId { get; set; }
        [Column("mission_id")] public uint MissionId { get; set; }
        [Column("objective_id")] public uint ObjectiveId { get; set; }
        [Column("payload", TypeName = "text")] public string Payload { get; set; } = "";
        [Column("status", TypeName = "varchar(16)")] public string Status { get; set; } = "Pending";
        [ConcurrencyCheck, Column("version")] public long Version { get; set; }
    }

    [Table("mission_actor_state")]
    public sealed class MissionActorStateEntry
    {
        [Required, Column("run_id", TypeName = "varchar(32)")] public string RunId { get; set; } = "";
        [Required, Column("actor_role", TypeName = "varchar(64)")] public string ActorRole { get; set; } = "";
        [Column("generation")] public uint Generation { get; set; }
        [Column("owner_character_id")] public uint OwnerCharacterId { get; set; }
        [Column("map_context_id")] public uint MapContextId { get; set; }
        [Column("shared_key", TypeName = "varchar(64)")] public string SharedKey { get; set; }
        [Required, Column("outcome", TypeName = "varchar(16)")] public string Outcome { get; set; } = "Defeated";
    }

    [Table("mission_scene_message")]
    public sealed class MissionSceneMessageEntry
    {
        [Key, Column("id")] public long Id { get; set; }
        [Required, Column("run_id", TypeName = "varchar(32)")] public string RunId { get; set; } = "";
        [Column("generation")] public uint Generation { get; set; }
        [Required, Column("operation_key", TypeName = "varchar(96)")] public string OperationKey { get; set; } = "";
        [Column("sequence_id")] public uint SequenceId { get; set; }
        [Required, Column("status", TypeName = "varchar(16)")] public string Status { get; set; } = "Pending";
        [ConcurrencyCheck, Column("version")] public long Version { get; set; }
    }
}
