using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace Rasa.Structures
{
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces;
    using JetBrains.Annotations;

    [Table(CharacterEntry.TableName)]
    [Index(nameof(CharacterEntry.AccountId), Name = "character_index_account")]
    public class CharacterEntry : IHasId
    {
        public const string TableName = "characer";

        [Key]
        [Column("id")]
        public uint Id { get; set; }

        [Column("account_id")]
        [Required]
        public uint AccountId { get; set; }

        [ForeignKey(nameof(AccountId))]
        public GameAccountEntry GameAccount { get; set; }

        [Column("slot")]
        [Required]
        public byte Slot { get; set; }

        [Column("name", TypeName = "varchar(64)")]
        [Required]
        public string Name { get; set; }

        [Column("race")]
        [Required]
        public byte Race { get; set; }

        [Column("class")]
        [Required]
        public uint Class { get; set; }

        [Column("gender", TypeName = "bit")]
        [Required]
        public byte Gender { get; set; }

        [Column("scale")]
        [Required]
        public double Scale { get; set; }

        [Column("experience")]
        [Required]
        public uint Experience { get; set; }

        [Column("level")]
        [Required]
        public byte Level { get; set; }


        [Column("body")]
        [Required]
        public uint Body { get; set; }

        [Column("mind")]
        [Required]
        public uint Mind { get; set; }

        [Column("spirit")]
        [Required]
        public uint Spirit { get; set; }

        [Column("clone_credits")]
        [Required]
        public uint CloneCredits { get; set; }

        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        [Column("coord_x")]
        [Required]
        public double CoordX { get; set; }

        [Column("coord_y")]
        [Required]
        public double CoordY { get; set; }

        [Column("coord_z")]
        [Required]
        public double CoordZ { get; set; }

        [Column("rotation")]
        [Required]
        public double Rotation { get; set; }

        [Column("num_logins")]
        [Required]
        public uint NumLogins { get; set; }

        [Column("last_login")]
        [Required]
        public DateTime? LastLogin { get; set; }

        [Column("total_time_played")]
        [Required]
        public uint TotalTimePlayed { get; set; }

        [Column("created_at")]
        [Required]
        public DateTime CreatedAt { get; set; }

        [Column("clan_id")]
        public uint ClanId { get; set; }

        [CanBeNull]
        public ClanMemberEntry MemberOfClan { get; set; }

        [CanBeNull]
        public ClanEntry Clan => MemberOfClan?.Clan;

        public List<CharacterAppearanceEntry> CharacterAppearance { get; set; }

        public IDictionary<uint, CharacterAppearanceEntry> GetCharacterAppearanceWithSlot()
        {
            return CharacterAppearance.ToDictionary(e => e.Slot, e => e);
        }
    }
}
