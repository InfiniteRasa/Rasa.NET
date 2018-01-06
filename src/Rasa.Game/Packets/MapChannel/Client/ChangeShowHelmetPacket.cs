namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class ChangeShowHelmetPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChangeShowHelmet;

        public bool ShowHelmet { get; set; }

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, $"ChangeShowHelmet:\n{pr.ToString()}");
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
