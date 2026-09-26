using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    using Interfaces;

    /// <summary>
    /// One filed petition: either a help request (Help -> Customer Services -> Petition, or
    /// /petition) or a bug report (Report Bug, or /bug). The client discards the id it is
    /// acknowledged with and has no window that reads a petition back, so this table is the
    /// only place the text survives - it is written for whoever is running the server.
    /// </summary>
    [Table(TableName)]
    public class PetitionEntry : IHasId
    {
        public const string TableName = "petition";

        public PetitionEntry()
        {
        }

        public PetitionEntry(uint accountId, uint characterId, byte type, string summary, string body,
            uint mapContextId, double posX, double posY, double posZ)
        {
            AccountId = accountId;
            CharacterId = characterId;
            Type = type;
            Summary = summary;
            Body = body;
            MapContextId = mapContextId;
            PosX = posX;
            PosY = posY;
            PosZ = posZ;
            CreatedAt = DateTime.UtcNow;
            Resolution = string.Empty;
        }

        [Key]
        [Column("id")]
        public uint Id { get; set; }

        [Column("account_id")]
        [Required]
        public uint AccountId { get; set; }

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        /// <summary>0 = help request, 1 = bug report. See PetitionType.</summary>
        [Column("type")]
        [Required]
        public byte Type { get; set; }

        [Column("summary", TypeName = "varchar(255)")]
        [Required]
        public string Summary { get; set; }

        [Column("body", TypeName = "text")]
        [Required]
        public string Body { get; set; }

        // Where the player was standing. A bug report without a location is usually not
        // actionable, and a help request is often about the spot the player is stuck in.
        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        [Column("pos_x", TypeName = "double")]
        [Required]
        public double PosX { get; set; }

        [Column("pos_y", TypeName = "double")]
        [Required]
        public double PosY { get; set; }

        [Column("pos_z", TypeName = "double")]
        [Required]
        public double PosZ { get; set; }

        /// <summary>0 open, 1 resolved, 2 cancelled by the player. See PetitionStatus.</summary>
        [Column("status")]
        [Required]
        public byte Status { get; set; }

        /// <summary>
        /// What was done about it, written when a petition is resolved from the console. Empty
        /// while it is open, and empty for one the player withdrew.
        /// </summary>
        [Column("resolution", TypeName = "varchar(255)")]
        [Required]
        public string Resolution { get; set; }

        [Column("created_at")]
        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
