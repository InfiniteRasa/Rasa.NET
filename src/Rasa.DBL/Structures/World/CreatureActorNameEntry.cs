using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    /// <summary>
    /// A name of the server's own for a creature, for an NPC the client's creaturenamelanguage has
    /// no entry for. The creature row carries name_id 0, so CreatureInfo goes out with no name id
    /// and the client falls back to the actor name (creature.py GetName), which is this, sent with
    /// Recv_ActorName.
    /// </summary>
    [Table(TableName)]
    public class CreatureActorNameEntry : IHasId
    {
        public const string TableName = "creature_actor_name";

        /// <summary>The creature row.</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("actor_name", TypeName = "varchar(64)")]
        [Required]
        public string ActorName { get; set; }
    }
}
