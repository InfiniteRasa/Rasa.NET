namespace Rasa.Packets.Communicator.Client
{
    using Data;
    using Memory;

    public class SurrenderClanFeudPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SurrenderClanFeud;

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, pr.ToString());
        }
    }
}
