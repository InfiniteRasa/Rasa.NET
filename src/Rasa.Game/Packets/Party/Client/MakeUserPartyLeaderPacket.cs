namespace Rasa.Packets.Party.Client
{
    using Data;
    using Memory;

    public class MakeUserPartyLeaderPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.MakeUserPartyLeader;

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, $"MakeUserPartyLeaderPacket: {pr.ToString()}");
        }
    }
}
