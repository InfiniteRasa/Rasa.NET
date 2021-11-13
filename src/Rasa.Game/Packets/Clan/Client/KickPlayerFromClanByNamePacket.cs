namespace Rasa.Packets.Clan.Client
{
    using Data;
    using Memory;

    class KickPlayerFromClanByNamePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.KickPlayerFromClanByName;

        public string Name { get; set; }
        public uint ClanId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            Name = pr.ReadUnicodeString();
            ClanId = pr.ReadUInt();
        }
    }
}
