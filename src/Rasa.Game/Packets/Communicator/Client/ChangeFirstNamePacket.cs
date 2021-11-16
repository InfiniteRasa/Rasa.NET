namespace Rasa.Packets.Communicator.Client
{
    using Data;
    using Memory;

    public class ChangeFirstNamePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChangeFirstName;

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, pr.ToString());
        }
    }
}
