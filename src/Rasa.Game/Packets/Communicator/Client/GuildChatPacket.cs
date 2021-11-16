namespace Rasa.Packets.Communicator.Client
{
    using Data;
    using Memory;

    public class GuildChatPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GuildChat;

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, pr.ToString());
        }
    }
}
