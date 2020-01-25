using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    public partial class ItemTemplateEntry
    {
        public uint ItemTemplateId { get; set; }
        public int QualityId { get; set; }
        public bool HasSellableFlag { get; set; }
        public bool NotTradableFlag { get; set; }
        public bool HasCharacterUniqueFlag { get; set; }
        public bool HasAccountUniqueFlag { get; set; }
        public bool HasBoEFlag { get; set; }
        public bool BoundToCharacterFlag { get; set; }
        public bool NotPlaceableInLockBoxFlag { get; set; }
        public int InventoryCategory { get; set; }
        public int BuyPrice { get; set; }
        public int SellPrice { get; set; }
    }
}
