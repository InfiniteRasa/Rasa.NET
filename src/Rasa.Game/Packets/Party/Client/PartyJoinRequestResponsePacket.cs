namespace Rasa.Packets.Party.Client
{
    using Data;
    using Memory;

    public class PartyJoinRequestResponsePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PartyJoinRequestResponse;

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, $"PartyJoinRequestResponserPacket: {pr.ToString()}");
        }
    }
}
