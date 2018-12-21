namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class SetStackCountPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetStackCount;

        public uint StackSize { get; set; }

        public SetStackCountPacket(uint stackSize)
        {
            StackSize = stackSize;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(StackSize);
        }
    }
}
