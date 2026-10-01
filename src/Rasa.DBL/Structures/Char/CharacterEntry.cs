using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

using Microsoft.EntityFrameworkCore;

using JetBrains.Annotations;

namespace Rasa.Structures.Char
{
    using System.Numerics;
    using Interfaces;

    [Table(CharacterEntry.TableName)]
    [Index(nameof(CharacterEntry.AccountId), Name = "character_index_account")]
    [Index(nameof(CharacterEntry.AccountId), nameof(CharacterEntry.Slot), IsUnique = true, Name = "character_index_account_slot")]
    public class CharacterEntry : IHasId
    {
        public const string TableName = "character";

        [Key]
        [Column("id")]
        public uint Id { get; set; }

        [Column("account_id")]
        [Required]
        public uint AccountId { get; set; }

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

        [Column("credit")]
        [Required]
        public int Credit { get; set; }

        [Column("prestige")]
        [Required]
        public int Prestige { get; set; }

        [Column("active_weapon")]
        [Required]
        public byte ActiveWeapon { get; set; }

        /// <summary>The armed ability drawer slot, 0 to 24 (five loadouts of five).</summary>
        [Column("current_ability_slot")]
        [Required]
        public byte CurrentAbilitySlot { get; set; }

        /// <summary>The title the character wears (titledata id), 0 for none. One of character_title's.</summary>
        [Column("current_title_id")]
        [Required]
        public uint CurrentTitleId { get; set; }

        [Column("body")]
        [Required]
        public int Body { get; set; }

        [Column("mind")]
        [Required]
        public int Mind { get; set; }

        [Column("spirit")]
        [Required]
        public int Spirit { get; set; }

        [Column("clone_credits")]
        [Required]
        public uint CloneCredits { get; set; }

        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        [Column("coord_x", TypeName = "double")]
        [Required]
        public double CoordX { get; set; }

        [Column("coord_y", TypeName = "double")]
        [Required]
        public double CoordY { get; set; }

        [Column("coord_z", TypeName = "double")]
        [Required]
        public double CoordZ { get; set; }

        [Column("rotation", TypeName = "double")]
        [Required]
        public double Rotation { get; set; }

        [Column("run_state", TypeName = "bit")]
        [Required]
        public byte RunState { get; set; }

        [Column("crouch_state", TypeName = "bit")]
        [Required]
        public byte CrouchState { get; set; }

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

        [Column("last_pvp_clan")]
        [Required]
        public DateTime LastPvPClan { get; set; }

        /// <summary>
        /// Health the character left the world with; <see cref="VitalNotSaved"/> for none, which
        /// loads it on full. Written on leaving the world, so that logging out and back in is no
        /// longer a free heal.
        /// </summary>
        [Column("current_health")]
        [Required]
        public int CurrentHealth { get; set; } = VitalNotSaved;

        /// <summary>Armour the character left the world with; <see cref="VitalNotSaved"/> for none.</summary>
        [Column("current_armor")]
        [Required]
        public int CurrentArmor { get; set; } = VitalNotSaved;

        /// <summary>Power the character left the world with; <see cref="VitalNotSaved"/> for none.</summary>
        [Column("current_power")]
        [Required]
        public int CurrentPower { get; set; } = VitalNotSaved;

        /// <summary>
        /// Rez Trauma the character left the world with: how many deaths' worth (0 for none), and
        /// when it wears off (Unix milliseconds, UTC). On wall-clock time, as the cooldowns are:
        /// time away counts, logging out does not end it.
        /// </summary>
        [Column("rez_trauma_stacks")]
        [Required]
        public uint RezTraumaStacks { get; set; }

        [Column("rez_trauma_ends_at")]
        [Required]
        public long RezTraumaEndsAt { get; set; }

        /// <summary>When the no-healing that follows a revive wears off (Unix milliseconds, UTC); 0 for none.</summary>
        [Column("no_heal_ends_at")]
        [Required]
        public long NoHealEndsAt { get; set; }

        /// <summary>The value of the current_* columns for a character that has not saved one.</summary>
        public const int VitalNotSaved = -1;

        [CanBeNull]
        public ClanMemberEntry MemberOfClan { get; set; }

        [CanBeNull]
        public ClanEntry Clan => MemberOfClan?.Clan;

        public List<CharacterAppearanceEntry> CharacterAppearance { get; set; }

        public IDictionary<uint, CharacterAppearanceEntry> GetCharacterAppearanceWithSlot()
        {
            return CharacterAppearance.ToDictionary(e => e.Slot, e => e);
        }

        public bool IsRunning()
        {
            return RunState == 1;
        }

        public bool IsCrouching()
        {
            return CrouchState == 1;
        }

        public Vector3 GetPositionVector()
        {
            return new Vector3((float)CoordX, (float)CoordY, (float)CoordZ);
        }
    }
}
