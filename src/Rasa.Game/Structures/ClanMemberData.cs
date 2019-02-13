namespace Rasa.Structures
{
    using Memory;

    public class ClanMemberData : IPythonDataStruct
    {
        public bool IsOnline { get; set; }
        public uint ContextId { get; set; }
        public uint Level { get; set; }
        public string CharacterName { get; set; }
        public string FamilyName { get; set; }
        public uint UserId { get; set; }
        public uint ClanId { get; set; }
        public uint Rank { get; set; }
        public string Note { get; set; }
        public bool IsAfk { get; set; }
        public uint CharacterId { get; set; }

        public ClanMemberData(uint userId, uint characterId, string characterName, string familyName, uint clanId, uint level, uint contextId, uint rank, bool isOnline, bool isAfk, string note)
        {
            UserId = userId;
            CharacterId = characterId;
            CharacterName = characterName;
            FamilyName = familyName;
            ClanId = clanId;
            Level = level;
            ContextId = contextId;
            Rank = rank;
            IsOnline = isOnline;
            IsAfk = isAfk;
            Note = note;
        }

        public void Read(PythonReader pr)
        {
            pr.ReadTuple();
            UserId = pr.ReadUInt();
            CharacterId = pr.ReadUInt();
            CharacterName = pr.ReadString();
            FamilyName = pr.ReadString();
            ClanId = pr.ReadUInt();
            Level = pr.ReadUInt();
            ContextId = pr.ReadUInt();
            Rank = pr.ReadUInt();
            IsOnline = pr.ReadBool();
            IsAfk = pr.ReadBool();
            Note = pr.ReadString();
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(11);
            pw.WriteUInt(UserId);
            pw.WriteUInt(CharacterId);
            pw.WriteString(CharacterName);
            pw.WriteString(FamilyName);
            pw.WriteUInt(ClanId);
            pw.WriteUInt(Level);
            pw.WriteUInt(ContextId);
            pw.WriteUInt(Rank);
            pw.WriteBool(IsOnline);
            pw.WriteBool(IsAfk);
            pw.WriteString(Note);            
        }
    }
}
