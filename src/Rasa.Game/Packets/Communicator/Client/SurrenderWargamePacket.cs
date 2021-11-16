namespace Rasa.Packets.Communicator.Client
{
    using Data;
    using Memory;

    public class SurrenderWargamePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SurrenderWargame;

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, pr.ToString());
        }
    }
}
