namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class ClanInvitationResponsePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.ClanInvitationResponse;

        public uint InvitedCharacterId { get; set; }
        public bool Accepted { get; set; }
        public uint ClanId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            InvitedCharacterId = pr.ReadUInt();
            Accepted = pr.ReadBool();
            ClanId = pr.ReadUInt();
        }
    }
}
