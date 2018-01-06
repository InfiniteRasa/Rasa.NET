namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class SaveUserOptionsPacket :PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SaveUserOptions;

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, $"SaveUserOptions:\n{pr.ToString()}");
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
