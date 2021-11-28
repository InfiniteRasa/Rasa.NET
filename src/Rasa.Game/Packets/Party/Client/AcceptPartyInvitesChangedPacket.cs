namespace Rasa.Packets.Party.Client
{
    using Data;
    using Memory;

    public class AcceptPartyInvitesChangedPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AcceptPartyInvitesChanged;

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, $"AcceptPartyInvitesChangedPacket {pr.ToString()}");
            pr.ReadTuple();
            pr.ReadTrueStruct();
        }
    }
}
