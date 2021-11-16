namespace Rasa.Packets.Communicator.Client
{
    using Data;
    using Memory;

    public class ChangeLastNamePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChangeLastName;

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, pr.ToString());
        }
    }
}
