namespace Rasa.Packets.Communicator.Client
{
    using Data;
    using Memory;

    public class ToggleAfkPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ToggleAfk;

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, pr.ToString());
        }
    }
}
