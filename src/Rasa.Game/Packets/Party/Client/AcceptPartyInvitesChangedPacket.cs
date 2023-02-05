namespace Rasa.Packets.Party.Client
{
    using Data;
    using Memory;

    public class AcceptPartyInvitesChangedPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AcceptPartyInvitesChanged;

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            pr.ReadTrueStruct();
        }
    }
}
