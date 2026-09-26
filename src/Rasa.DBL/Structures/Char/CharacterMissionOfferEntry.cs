using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Rasa.Missions.Definitions;

namespace Rasa.Structures.Char
{
    public enum MissionOfferState { Pending, Consumed, Cancelled }

    [Table("character_mission_offer")]
    public sealed class CharacterMissionOfferEntry
    {
        [Column("character_id")] public uint CharacterId { get; set; }
        [Column("mission_id")] public uint MissionId { get; set; }
        [Required, Column("offer_id", TypeName = "varchar(32)")] public string OfferId { get; set; }
        [Required, Column("content_revision", TypeName = "varchar(32)")] public string ContentRevision { get; set; }
        [Column("account_id")] public uint AccountId { get; set; }
        [Column("player_entity_id")] public ulong PlayerEntityId { get; set; }
        [Column("session_id", TypeName = "char(36)")] public Guid SessionId { get; set; }
        [Column("player_epoch", TypeName = "char(36)")] public Guid PlayerEpoch { get; set; }
        [Column("map_epoch", TypeName = "char(36)")] public Guid MapEpoch { get; set; }
        [Column("source_kind")] public MissionOfferSourceKind SourceKind { get; set; }
        [Required, Column("source_key", TypeName = "varchar(96)")] public string SourceKey { get; set; }
        [Required, Column("source_instance_id", TypeName = "varchar(96)")] public string SourceInstanceId { get; set; }
        [Column("source_generation")] public uint SourceGeneration { get; set; }
        [Column("source_assignment_id", TypeName = "varchar(32)")] public string SourceAssignmentId { get; set; }
        [Column("source_assignment_generation")] public uint SourceAssignmentGeneration { get; set; }
        [Column("party_source", TypeName = "text")] public MissionPartyOfferSource PartySource { get; set; }
        [Column("prior_assignment_id", TypeName = "varchar(32)")] public string PriorAssignmentId { get; set; }
        [Column("prior_assignment_generation")] public uint PriorAssignmentGeneration { get; set; }
        [Column("prior_assignment_revision", TypeName = "varchar(32)")] public string PriorAssignmentRevision { get; set; }
        [Column("prior_history_id", TypeName = "varchar(32)")] public string PriorHistoryId { get; set; }
        [Column("created_at_utc")] public DateTime CreatedAtUtc { get; set; }
        [Column("expires_at_utc")] public DateTime ExpiresAtUtc { get; set; }
        [Column("state")] public MissionOfferState State { get; set; }
        [Column("consumed_assignment_id", TypeName = "varchar(32)")] public string ConsumedAssignmentId { get; set; }
        [Column("consumed_assignment_generation")] public uint ConsumedAssignmentGeneration { get; set; }
        [ConcurrencyCheck, Column("version")] public long Version { get; set; }
    }
}
