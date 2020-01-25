using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    public partial class ItemClassEntry
    {
        public uint ClassId { get; set; }
        public int InventoryIconStringId { get; set; }
        public int LootValue { get; set; }
        public bool HiddenInventoryFlag { get; set; }
        public bool IsConsumableFlag { get; set; }
        public int MaxHitPoints { get; set; }
        public uint StackSize { get; set; }
        public int DragAudioSetId { get; set; }
        public int DropAudioSetId { get; set; }
    }
}
