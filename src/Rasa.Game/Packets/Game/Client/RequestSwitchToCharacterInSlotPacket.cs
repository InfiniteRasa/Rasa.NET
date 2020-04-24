namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class RequestSwitchToCharacterInSlotPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestSwitchToCharacterInSlot;

        public uint SlotNum { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            SlotNum = pr.ReadUInt();
            pr.ReadZeroStruct();
        }
    }
}
