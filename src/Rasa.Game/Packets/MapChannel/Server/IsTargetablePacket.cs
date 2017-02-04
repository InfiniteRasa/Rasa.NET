namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class IsTargetablePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.IsTargetable;

        public bool IsTargetable { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteBool(IsTargetable);
        }
    }
}
