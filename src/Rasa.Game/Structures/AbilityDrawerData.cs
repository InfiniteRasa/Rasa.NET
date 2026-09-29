namespace Rasa.Structures
{
    public class AbilityDrawerData
    {
        public int AbilityId { get; set; }
        public int AbilitySlotId { get; set; }
        public uint AbilityLevel { get; set; }

        /// <summary>
        /// For a usable item dragged onto the tray (a pet, a medpack): the item it came from, by
        /// item.id - its entity id changes with every login. Null for a skill's ability. The
        /// client is sent the item's entity id (Managers.AbilityDrawerItems), and fires the slot
        /// with it, which is how the server knows which pet or which stack is meant.
        /// </summary>
        public uint? ItemId { get; set; }

        public AbilityDrawerData(int abilitySlotId, int abilityId, uint abilityLevel, uint? itemId = null)
        {
            AbilityId = abilityId;
            AbilitySlotId = abilitySlotId;
            AbilityLevel = abilityLevel;
            ItemId = itemId;
        }
    }
}
