namespace Rasa.Packets.Party.Client
{
    using Data;
    using Memory;

    public class MakeUserPartyLeaderByIdPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.MakeUserPartyLeaderById;

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, $"MakeUserPartyLeaderByIdPacket: {pr.ToString()}");
        }
    }
}
