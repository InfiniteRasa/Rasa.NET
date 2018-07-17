namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestSetAbilitySlotPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestSetAbilitySlot;

        public int SlotId { get; set; }
        public long AbilityId { get; set; }
        public long AbilityLevel { get; set; }
        //public int ItemId { get; set; }   // ToDo check later maybe it's id of item if it's draged into abilityDrawer, not used for skills

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            SlotId = pr.ReadInt();
            if (pr.PeekType() == PythonType.Long)
                AbilityId = pr.ReadLong();
            else
                pr.ReadNoneStruct();

            if (pr.PeekType() == PythonType.Long)
                AbilityLevel = pr.ReadLong();
            else
                pr.ReadNoneStruct();
            pr.ReadNoneStruct();
        }
    }
}
