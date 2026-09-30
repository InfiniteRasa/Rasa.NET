namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// AbilityDrawerSlot (394), on the player's own manifestation: the armed ability drawer slot.
    /// client/augmentations/manifestation.py Recv_AbilityDrawerSlot(slotNum, bRequested=True)
    /// sets the armed slot. With bRequested false - the server restoring it rather than answering
    /// a request - it also sets the requested slot and the loadout page the drawer shows
    /// (slot / NUM_ABILITY_DRAWER_SLOTS), which a newly created manifestation has at 0.
    /// </summary>
    public class AbilityDrawerSlotPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AbilityDrawerSlot;

        public int AbilityDrawerSlot { get; set; }
        public bool Requested { get; set; }

        public AbilityDrawerSlotPacket(int abilityDrawerSlot, bool requested = true)
        {
            AbilityDrawerSlot = abilityDrawerSlot;
            Requested = requested;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt(AbilityDrawerSlot);
            pw.WriteBool(Requested);
        }
    }
}
