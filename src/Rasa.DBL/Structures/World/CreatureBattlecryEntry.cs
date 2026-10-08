using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// Which of the client's battle cry packages a creature cries with (the game server's
    /// Battlecries; the client's battlecrypackages maps a package and a cry type to an audio
    /// set). The assignment was the 2009 server's and the client carries none, so every row
    /// here is given: a creature with no row is silent, as every creature was before the table.
    ///
    /// A row is for an entity class - every creature made from it - or for one creature row,
    /// which comes before its class's: the humans and the Brann share a few swapset classes, and
    /// a voice is one NPC's.
    ///
    /// The key is the scope and the target, set in WorldContext: a composite key cannot be
    /// declared with attributes.
    /// </summary>
    [Table(TableName)]
    public class CreatureBattlecryEntry
    {
        public const string TableName = "creature_battlecry";

        /// <summary>target_id is an entity class id.</summary>
        public const uint ScopeClass = 1;

        /// <summary>target_id is a creature row's id (creature.id).</summary>
        public const uint ScopeCreature = 2;

        /// <summary>What target_id names: ScopeClass or ScopeCreature.</summary>
        [Column("scope")]
        [Required]
        public uint Scope { get; set; }

        /// <summary>The entity class, or the creature row.</summary>
        [Column("target_id")]
        [Required]
        public uint TargetId { get; set; }

        /// <summary>The package, as in the client's generated/client/battlecrypackages.</summary>
        [Column("package_id")]
        [Required]
        public uint PackageId { get; set; }
    }
}
