namespace Rasa.Packets.Party.Client
{
    using Data;
    using Memory;

    public class PartyInvitationResponsePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PartyInvitationResponse;

        internal bool Response { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            Response = pr.ReadBool();
        }
    }
}
