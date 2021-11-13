namespace Rasa.Packets.Clan.Client
{
    using Data;
    using Memory;

    public class KickPlayerFromClanPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.KickPlayerFromClan;

        public uint CharacterId { get; set; }
        public uint ClanId { get; set; }

        public KickPlayerFromClanPacket()
        {
        }

        public KickPlayerFromClanPacket(uint characterId, uint clanId)
        {
            CharacterId = characterId;
            ClanId = clanId;
        }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            CharacterId = pr.ReadUInt();
            ClanId = pr.ReadUInt();
        }
    }
}
