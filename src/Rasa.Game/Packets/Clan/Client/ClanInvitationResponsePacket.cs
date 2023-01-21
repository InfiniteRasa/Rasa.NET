namespace Rasa.Packets.Clan.Client
{
    using Data;
    using Memory;

    public class ClanInvitationResponsePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.ClanInvitationResponse;

        public ulong InvitedCharacterEntityId { get; set; }
        public bool Accepted { get; set; }
        public uint ClanId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            InvitedCharacterEntityId = pr.ReadULong();
            Accepted = pr.ReadBool();
            ClanId = pr.ReadUInt();
        }
    }
}
