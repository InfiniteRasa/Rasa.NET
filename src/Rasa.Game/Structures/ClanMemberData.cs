namespace Rasa.Structures
{
    using Memory;

    public class ClanMemberData : IPythonDataStruct
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

        public void Read(PythonReader pr)
        {
            pr.ReadTuple();
            IsOnline = pr.ReadBool();
            ContextId = pr.ReadUInt();
            Level = pr.ReadUInt();
            FamilyName = pr.ReadString();
            UserId = pr.ReadUInt();
            ClanId = pr.ReadUInt();
            Rank = pr.ReadString();
            Note = pr.ReadString();
            IsAfk = pr.ReadBool();
            CharacterId = pr.ReadUInt();
            Map = pr.ReadString();
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(11);
            pw.WriteBool(IsOnline);
            pw.WriteUInt(ContextId);
            pw.WriteUInt(Level);
            pw.WriteString(FamilyName);
            pw.WriteUInt(UserId);
            pw.WriteUInt(ClanId);
            pw.WriteString(Rank);
            pw.WriteString(Note);
            pw.WriteBool(IsAfk);
            pw.WriteUInt(CharacterId);
            pw.WriteString(Map);
        }
    }
}
