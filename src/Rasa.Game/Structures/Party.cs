using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;

    public class Party
    {
        internal uint Id { get; set; }
        internal System.Guid LifetimeId { get; } = System.Guid.NewGuid();

        /// <summary>Account id of the leader.</summary>
        internal uint PartyLeaderId { get; set; }

        /// <summary>In join order, including members whose spot is being held while they are offline.</summary>
        internal List<PartyMember> Members { get; set; }

        internal PartyLootMethod LootMethod { get; set; }
        internal PartyLootThreshold LootThreshold { get; set; }

        public Party(uint partyId, uint partyLeaderId, List<PartyMember> partyMembers)
        {
            Id = partyId;
            PartyLeaderId = partyLeaderId;
            Members = partyMembers;
        }

        internal PartyMember Find(uint userId) => Members.Find(m => m.UserId == userId);
    }
}
