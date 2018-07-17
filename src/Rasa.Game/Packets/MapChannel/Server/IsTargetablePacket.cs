namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class IsTargetablePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.IsTargetable;

        public bool IsTargetable { get; set; }

        public IsTargetablePacket(bool isTargetable)
        {
            IsTargetable = isTargetable;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteBool(IsTargetable);
        }
    }
}
