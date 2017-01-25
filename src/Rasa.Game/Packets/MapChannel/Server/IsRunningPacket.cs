namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    public class IsRunningPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.IsRunning;

        public bool IsRunning { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteBool(IsRunning);
        }
    }
}