using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    /// <summary>
    /// Which class grants a skill, and the level it takes to train it. A verbatim copy of the
    /// client's <c>generated.client.skilldata.skillCharacter</c>, which is keyed by skill and
    /// holds exactly this pair.
    ///
    /// The client already refuses to draw the spend buttons for a skill outside the player's own
    /// line of classes, and refuses one above their level; the server had no copy of either rule,
    /// which is what let a level 1 Recruit train a Tier 4 skill.
    /// </summary>
    [Table(TableName)]
    public class SkillCharacterEntry : IHasId
    {
        public const string TableName = "skill_character";

        /// <summary>The skill, from generated/client/skilldata.</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        /// <summary>The character class that grants it, from generated/client/characterclass.</summary>
        [Column("class_id")]
        [Required]
        public uint ClassId { get; set; }

        /// <summary>
        /// The character level the skill may first be trained at: 1, 5, 15 or 30, which are the
        /// four tiers. Every skill of a class carries the same one.
        /// </summary>
        [Column("required_level")]
        [Required]
        public uint RequiredLevel { get; set; }
    }
}
