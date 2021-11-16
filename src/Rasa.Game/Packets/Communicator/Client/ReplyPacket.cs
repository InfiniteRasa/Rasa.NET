namespace Rasa.Packets.Communicator.Client
{
    using Data;
    using Memory;

    public class ReplyPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Reply;

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, pr.ToString());
        }
    }
}
