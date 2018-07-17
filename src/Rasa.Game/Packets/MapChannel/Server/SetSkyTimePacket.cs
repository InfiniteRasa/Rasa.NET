namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    public class SetSkyTimePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetSkyTime;

        public int RunningTime { get; set; }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(RunningTime);
        }
    }
}
