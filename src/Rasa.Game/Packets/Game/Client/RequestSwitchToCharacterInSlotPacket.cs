namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class RequestSwitchToCharacterInSlotPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestSwitchToCharacterInSlot;

        public byte SlotNum { get; set; }
        public bool SkipBootcamp { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            SlotNum = (byte)pr.ReadUInt();
            pr.ReadBool();
        }
    }
}
