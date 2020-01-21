using System;

namespace Rasa.Structures
{
    public partial class ItemsEntry
    {
        public uint ItemId { get; set; }
        public uint ItemTemplateId { get; set; }
        public uint StackSize { get; set; }
        public int CurrentHitPoints { get; set; }
        public uint Color { get; set; }
        public uint AmmoCount { get; set; }
        public string CrafterName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
