namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// RequestSetAbilitySlot(slot, abilityId, pumpLevel, itemId): Manifestation.SetAbilitySlot.
    /// abilityId and pumpLevel are None to clear the slot; itemId is None for a skill's ability,
    /// and the item's entity id for a usable item dragged from the inventory onto the tray.
    /// </summary>
    public class RequestSetAbilitySlotPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestSetAbilitySlot;

        public int SlotId { get; set; }
        public long AbilityId { get; set; }
        public long AbilityLevel { get; set; }

        /// <summary>The entity id of the item whose action this is; 0 for a skill's ability.</summary>
        public ulong ItemId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            SlotId = pr.ReadInt();

            // Each may come as a Python int or long, or None. A skill's ability arrives as a long,
            // an item's action (its GetItemActionInfo, from the item template) as an int - reading
            // only the long form and taking anything else for None threw on every item dragged
            // onto the tray, and disconnected the player.
            AbilityId = ReadNumberOrNone(pr);
            AbilityLevel = ReadNumberOrNone(pr);
            ItemId = (ulong)ReadNumberOrNone(pr);
        }

        private static long ReadNumberOrNone(PythonReader pr)
        {
            switch (pr.PeekType())
            {
                case PythonType.Long:
                    return pr.ReadLong();
                case PythonType.Int:
                    return pr.ReadInt();
                default:
                    pr.ReadNoneStruct();
                    return 0;
            }
        }
    }
}
