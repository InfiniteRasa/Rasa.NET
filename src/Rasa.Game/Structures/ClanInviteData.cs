namespace Rasa.Structures
{
    public class ClanInviteData
    {
        public string InviterFamilyName { get; set; }
        public uint ClanId { get; set; }
        public string ClanName { get; set; }
        public bool IsPvP { get; set; }
        public uint InvitedCharacterId { get; set; }

        public ClanInviteData(string inviterFamilyName, uint clanId, string clanName, bool isPvP, uint invitedCharacterId)
        {
            InviterFamilyName = inviterFamilyName;
            ClanId = clanId;
            ClanName = clanName;
            IsPvP = isPvP;
            InvitedCharacterId = invitedCharacterId;
        }
    }
}
