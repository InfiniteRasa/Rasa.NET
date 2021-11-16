namespace Rasa.Packets.Communicator.Client
{
    using Data;
    using Memory;

    public class RevokeClanFeudPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RevokeClanFeud;

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, pr.ToString());
        }
    }
}
