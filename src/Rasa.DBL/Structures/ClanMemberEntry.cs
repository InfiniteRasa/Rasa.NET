namespace Rasa.Structures
{
    public partial class ClanMemberEntry
    {
        public uint ClanId { get; set; }
        public uint CharacterId { get; set; }
        public byte Rank { get; set; }

        public virtual CharacterEntry Character { get; set; }
        public virtual ClanEntry Clan { get; set; }
    }
}