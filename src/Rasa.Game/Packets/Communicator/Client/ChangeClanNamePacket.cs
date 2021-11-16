namespace Rasa.Packets.Communicator.Client
{
    using Data;
    using Memory;

    public class ChangeClanNamePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChangeClanName;

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, pr.ToString());
        }
    }
}
