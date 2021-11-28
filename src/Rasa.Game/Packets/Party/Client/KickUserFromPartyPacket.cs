namespace Rasa.Packets.Party.Client
{
    using Data;
    using Memory;

    public class KickUserFromPartyPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.KickUserFromParty;

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, $"KickUserFromPartyPacket: {pr.ToString()}");
        }
    }
}
