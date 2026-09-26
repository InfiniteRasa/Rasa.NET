namespace Rasa.Structures
{
    using Game;
    using Memory;

    /// <summary>
    /// One squad member, as the client's party tuples describe it:
    /// (userId, name, classId, level, isAfk) - client/party.py Recv_AddPartyMember,
    /// Recv_PartyMemberList, Recv_UpdatePartyMemberInfo, and the squad info in InviteToParty.
    ///
    /// userId is the account id. The client keys g_partyMembers, the party window, kick and
    /// pass-leadership requests, SetPartyLeader and PartyChat's senderUserId by it, and compares
    /// it with gameclient.GetCurrentUserId(). It used to be the manifestation's entity id, which
    /// changes every time a character enters the world, so a returning player matched nothing
    /// and was listed a second time.
    /// </summary>
    public class PartyMember : IPythonDataStruct
    {
        internal System.Guid MembershipId { get; private set; } = System.Guid.NewGuid();
        internal uint CharacterId { get; private set; }
        public uint UserId { get; set; }
        public string MemberName { get; set; }
        public uint MemberClassId { get; set; }
        public uint MemberLevel { get; set; }
        public bool IsAfk { get; set; }

        /// <summary>The member's manifestation while they are in the world; 0 while their spot is held.</summary>
        public ulong EntityId { get; set; }

        /// <summary>Environment.TickCount64 when the member left the world.</summary>
        public long OfflineSinceTick { get; set; }

        public bool IsOnline => EntityId != 0;

        public PartyMember(Client client)
        {
            UserId = client.AccountEntry.Id;
            Refresh(client);
        }

        public PartyMember(uint userId, string memberName, uint memberClassId, uint memberLevel, bool isAfk)
        {
            UserId = userId;
            MemberName = memberName;
            MemberClassId = memberClassId;
            MemberLevel = memberLevel;
            IsAfk = isAfk;
        }

        /// <summary>Copies the live character: a member can come back on another character of the same account.</summary>
        public void Refresh(Client client)
        {
            if (EntityId != client.Player.EntityId || CharacterId != client.Player.Id)
                InvalidateMissionMembership();
            CharacterId = client.Player.Id;
            EntityId = client.Player.EntityId;
            MemberName = client.Player.FamilyName;
            MemberClassId = client.Player.Class;
            MemberLevel = client.Player.Level;
            IsAfk = client.Player.IsAFK;
        }

        internal void InvalidateMissionMembership() => MembershipId = System.Guid.NewGuid();

        public void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, $"PartyMember: {pr.ToString()}");
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(5);
            pw.WriteUInt(UserId);
            pw.WriteUnicodeString(MemberName);
            pw.WriteUInt(MemberClassId);
            pw.WriteUInt(MemberLevel);
            pw.WriteBool(IsAfk);
        }
    }
}
