namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class SetStackCountPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetStackCount;

        public int StackSize { get; set; }

        public SetStackCountPacket(int stackSize)
        {
            StackSize = stackSize;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(StackSize);
        }
    }
}
