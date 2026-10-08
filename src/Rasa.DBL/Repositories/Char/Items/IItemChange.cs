using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rasa.Repositories.Char.Items
{
    public interface IItemChange
    {
        public uint Id { get; set; }
        public uint ItemTemplateId { get; set; }
        public uint Color { get; set; }
        public string Crafter { get; set; }
        public int CurrentHitPoints { get; set; }
        public uint StackSize { get; set; }
        public uint CurrentAmmo { get; set; }
        public uint BoundCharacterId { get; set; }

        /// <summary>
        /// The module in each of the item's module slots, 0 for an empty one: at most four of
        /// them, and none for an item that carries no modules.
        /// </summary>
        public IReadOnlyList<uint> ModuleIds => Array.Empty<uint>();
    }
}
