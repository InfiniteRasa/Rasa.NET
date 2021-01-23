namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    public class IsRunningPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.IsRunning;

        public bool IsRunning { get; set; }

        public IsRunningPacket(bool isRunning)
        {
            IsRunning = isRunning;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteBool(IsRunning);
        }
    }
}
