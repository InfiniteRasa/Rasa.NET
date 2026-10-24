using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Structures.Char
{
    /// <summary>
    /// A player's apartment (the game server's Managers.Apartments): whose it is, which plot it
    /// stands on, its name, and who besides the owner may come in. One per owner; the unique
    /// index keeps that so. The owner is a character today (<see cref="OwnerKindCharacter"/>);
    /// the kind is there so that a clan's base can be one more owner kind later without a new
    /// table.
    /// </summary>
    [Table(TableName)]
    [Index(nameof(OwnerKind), nameof(OwnerId), IsUnique = true, Name = "apartment_index_owner")]
    public class ApartmentEntry
    {
        public const string TableName = "apartment";

        /// <summary>An apartment of a character: <see cref="OwnerId"/> is the character's id.</summary>
        public const byte OwnerKindCharacter = 1;

        /// <summary>The longest name an apartment may be given, in characters.</summary>
        public const int NameLength = 48;

        [Key]
        [Column("id")]
        public uint Id { get; set; }

        [Column("owner_kind")]
        [Required]
        public byte OwnerKind { get; set; }

        [Column("owner_id")]
        [Required]
        public uint OwnerId { get; set; }

        /// <summary>The plot (the game server's apartment plots, by id) the apartment stands on.</summary>
        [Column("plot_id")]
        [Required]
        public uint PlotId { get; set; }

        /// <summary>What the owner named it; empty for none, when it is shown by its owner's name.</summary>
        [Column("name", TypeName = "varchar(48)")]
        [Required]
        public string Name { get; set; } = "";

        /// <summary>
        /// The passcode's hash (SHA-256 of the apartment id and the lower-cased word, in hex), or
        /// null for none. The word itself is never kept.
        /// </summary>
        [Column("passcode_hash", TypeName = "varchar(64)")]
        public string PasscodeHash { get; set; }

        /// <summary>Whether the owner's friends may come in without an invitation.</summary>
        [Column("friends_may_enter")]
        [Required]
        public bool FriendsMayEnter { get; set; }

        /// <summary>The number the next item placed is given: items are told apart in chat by these, from 1.</summary>
        [Column("next_item_number")]
        [Required]
        public uint NextItemNumber { get; set; } = 1;

        /// <summary>Unix time in milliseconds, UTC.</summary>
        [Column("created_at")]
        [Required]
        public long CreatedAt { get; set; }
    }

    /// <summary>
    /// An item placed in an apartment: a retail entity class from the decoration catalogue,
    /// where it stands, which way it faces and how large it is. A packed item is kept but not
    /// put down: one that was outside the build box of the plot it was moved to.
    /// </summary>
    [Table(TableName)]
    [Index(nameof(ApartmentId), nameof(Number), IsUnique = true, Name = "apartment_item_index_apartment_number")]
    public class ApartmentItemEntry
    {
        public const string TableName = "apartment_item";

        [Key]
        [Column("id")]
        public uint Id { get; set; }

        [Column("apartment_id")]
        [Required]
        public uint ApartmentId { get; set; }

        /// <summary>The item's number in its apartment, shown in chat; never given twice in one apartment.</summary>
        [Column("number")]
        [Required]
        public uint Number { get; set; }

        [Column("class_id")]
        [Required]
        public uint ClassId { get; set; }

        [Column("pos_x")]
        [Required]
        public double PosX { get; set; }

        [Column("pos_y")]
        [Required]
        public double PosY { get; set; }

        [Column("pos_z")]
        [Required]
        public double PosZ { get; set; }

        /// <summary>The yaw in radians, as a DynamicObject's Rotation.</summary>
        [Column("rotation")]
        [Required]
        public double Rotation { get; set; }

        [Column("scale")]
        [Required]
        public double Scale { get; set; } = 1;

        [Column("packed")]
        [Required]
        public bool Packed { get; set; }

        /// <summary>The character who placed it.</summary>
        [Column("placed_by")]
        [Required]
        public uint PlacedBy { get; set; }

        /// <summary>Unix time in milliseconds, UTC.</summary>
        [Column("placed_at")]
        [Required]
        public long PlacedAt { get; set; }
    }

    /// <summary>A character the owner has invited into their apartment, and whether they may build there.</summary>
    [Table(TableName)]
    [Index(nameof(CharacterId), Name = "apartment_visitor_index_character")]
    public class ApartmentVisitorEntry
    {
        public const string TableName = "apartment_visitor";

        [Column("apartment_id")]
        [Required]
        public uint ApartmentId { get; set; }

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        [Column("may_build")]
        [Required]
        public bool MayBuild { get; set; }

        /// <summary>Unix time in milliseconds, UTC.</summary>
        [Column("added_at")]
        [Required]
        public long AddedAt { get; set; }
    }

    /// <summary>
    /// A character who is in an apartment: which one, and where they came from - the place
    /// .leave takes them back to. Written as they go in from outside, kept while they go from
    /// one apartment to another, deleted as they leave; a character who logs out inside is put
    /// back in by it, or at the place they came from.
    /// </summary>
    [Table(TableName)]
    public class ApartmentReturnEntry
    {
        public const string TableName = "apartment_return";

        [Key]
        [Column("character_id")]
        public uint CharacterId { get; set; }

        [Column("apartment_id")]
        [Required]
        public uint ApartmentId { get; set; }

        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        [Column("pos_x")]
        [Required]
        public double PosX { get; set; }

        [Column("pos_y")]
        [Required]
        public double PosY { get; set; }

        [Column("pos_z")]
        [Required]
        public double PosZ { get; set; }

        [Column("rotation")]
        [Required]
        public double Rotation { get; set; }
    }
}
