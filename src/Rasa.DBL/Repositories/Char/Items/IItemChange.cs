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

    }
}
