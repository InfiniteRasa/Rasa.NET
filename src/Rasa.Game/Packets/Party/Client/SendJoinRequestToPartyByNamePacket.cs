namespace Rasa.Packets.Party.Client
{
    using Data;
    using Memory;

    public class SendJoinRequestToPartyByNamePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SendJoinRequestToPartyByName;

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, $"SendJoinRequestToPartyByNamePacket: {pr.ToString()}");
        }
    }
}
