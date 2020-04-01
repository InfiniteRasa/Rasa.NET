namespace Rasa.Structures
{
    using Memory;

    public class ClanInviteData : IPythonDataStruct
    {       
        public string InviterFamilyName { get; set; }
        public uint ClanId { get; set; }
        public string ClanName { get; set; }
        public bool IsPvP { get; set; }
        public uint InvitedCharacterId { get; set; }

        public ClanInviteData()
        {

        }

        public ClanInviteData(string inviterFamilyName, uint clanId, string clanName, bool isPvP, uint invitedCharacterId)
        {
            InviterFamilyName = inviterFamilyName;
            ClanId = clanId;
            ClanName = clanName;
            IsPvP = isPvP;
            InvitedCharacterId = invitedCharacterId;
        }

        public void Read(PythonReader pr)
        {
            pr.ReadTuple();
            InviterFamilyName = pr.ReadString();
            ClanId = pr.ReadUInt();
            ClanName = pr.ReadString();
            IsPvP = pr.ReadBool();
            InvitedCharacterId = pr.ReadUInt();
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(5);
            pw.WriteString(InviterFamilyName);
            pw.WriteUInt(ClanId);
            pw.WriteString(ClanName);
            pw.WriteBool(IsPvP);
            pw.WriteUInt(InvitedCharacterId);
        }
    }
}
