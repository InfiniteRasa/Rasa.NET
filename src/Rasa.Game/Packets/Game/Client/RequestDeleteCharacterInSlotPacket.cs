namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class RequestDeleteCharacterInSlotPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.RequestDeleteCharacterInSlot;

        public byte Slot { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            Slot = (byte) pr.ReadUInt();
        }
    }
}
