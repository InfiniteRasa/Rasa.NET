namespace Rasa.Structures
{
    public class ClanMemberData
    {
        public bool IsOnline { get; set; }
        public uint ContextId { get; set; }
        public uint Level { get; set; }
        public string FamilyName { get; set; }
        public uint UserId { get; set; }
        public uint ClanId { get; set; }
        public string Rank { get; set; }
        public string Note { get; set; }
        public bool IsAfk { get; set; }
        public uint CharacterId { get; set; }
        public string Map { get; set; }

        public ClanMemberData(bool isOnline, uint contextId, uint level, string familyName, uint userId, uint clanId,
                              string rank, string note, bool isAfk, uint characterId, string map)
        {
            IsOnline = isOnline;
            ContextId = contextId;
            Level = level;
            FamilyName = familyName;
            UserId = userId;
            ClanId = clanId;
            Rank = rank;
            Note = note;
            IsAfk = isAfk;
            CharacterId = characterId;
            Map = map;
        }
    }
}
