using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// An item a container (world_container) always holds when it is opened: the template and how
    /// many. More than a stack's worth is given as several stacks. The key is the container and
    /// the template, set in WorldContext.
    /// </summary>
    [Table(TableName)]
    public class WorldContainerItemEntry
    {
        public const string TableName = "world_container_item";

        /// <summary>The container (world_container.id).</summary>
        [Column("container_id")]
        [Required]
        public uint ContainerId { get; set; }

        /// <summary>The item, as an item template id.</summary>
        [Column("item_template_id")]
        [Required]
        public uint ItemTemplateId { get; set; }

        [Column("quantity")]
        [Required]
        public uint Quantity { get; set; }
    }
}
