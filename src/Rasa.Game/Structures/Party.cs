using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;
    public class Party
    {
        internal uint Id { get; set; }
        internal uint PartyLeaderId { get; set; }
        internal List<PartyMember> Members { get; set; }
        internal PartyLootMethod LootMethod { get; set; }
        internal PartyLootThreshold LootThreshold { get; set; }

        public Party(uint partyId, uint partyLeaderId, List<PartyMember> partyMembers)
        {
            Id = partyId;
            PartyLeaderId = partyLeaderId;
            Members = partyMembers;
        }
    }
}
