namespace Rasa.Packets.Clan.Client
{
    using Data;
    using Memory;

    public class ClanChangeRankTitlePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.ClanChangeRankTitle;

        public uint Rank { get; set; }
        public string Title { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            Rank = pr.ReadUInt();
            Title = pr.ReadUnicodeString();
        }
    }
}
