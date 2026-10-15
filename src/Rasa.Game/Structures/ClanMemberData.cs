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
        public byte Rank { get; set; }
        public string Note { get; set; }
        public bool IsAfk { get; set; }
        public uint CharacterId { get; set; }

        /// <summary>
        /// Set only in the copy of a member's line that goes to that member's own client, where it is
        /// their manifestation's entity id; 0 everywhere else. See <see cref="Write"/>.
        /// </summary>
        public ulong CharacterEntityId { get; set; }

        /// <summary>
        /// Online as an invisible GM: offline in the line a reader who is not a GM is sent
        /// (ClanManager.ForReader). Not written; the line itself says only online or not.
        /// </summary>
        internal bool InvisibleGm { get; set; }

        public ClanMemberData()
        {
        }

        public ClanMemberData(uint userId, uint characterId, ulong characterEntityId, string characterName, string familyName, uint clanId, uint level, uint contextId, byte rank, bool isOnline, bool isAfk, string note)
        {
            UserId = userId;
            CharacterId = characterId;
            CharacterEntityId= characterEntityId;
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
            CharacterEntityId = pr.ReadULong();
            CharacterName = pr.ReadString();
            FamilyName = pr.ReadString();
            ClanId = pr.ReadUInt();
            Level = pr.ReadUInt();
            ContextId = pr.ReadUInt();
            Rank = (byte)pr.ReadUInt();
            IsOnline = pr.ReadBool();
            IsAfk = pr.ReadBool();
            Note = pr.ReadString();
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(11);
            pw.WriteUInt(UserId);

            // The client's characterId (shared/clandefs.py), which it files the member under and
            // sends back to kick, promote, demote or hand leadership to them. In a player's own line
            // it is their manifestation's entity id - client/clan.py compares it with
            // GetCurrentManifestationId to find itself - and in everyone else's the character id.
            //
            // This wrote CharacterEntityId alone, which nothing sets for anyone but the reader, so
            // every other member went out as 0: the client filed them all under the same key and
            // kept whichever came last, and the clan window sent 0 for every action on them.
            pw.WriteULong(CharacterEntityId != 0 ? CharacterEntityId : CharacterId);
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
