namespace Rasa.Structures
{
    public partial class CharacterInventoryEntry
    {
        public uint AccountId { get; set; }
        public uint CharacterSlot { get; set; }
        public int InventoryType { get; set; }
        public uint ItemId { get; set; }
        public uint SlotId { get; set; }
    }
}
