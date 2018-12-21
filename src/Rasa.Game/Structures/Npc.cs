using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;

    public class Npc
    {
        public uint NpcPackageId { get; set; }
        public uint ConvoStatus { get; set; }
        public Vendor Vendor { get; set; }
        public List<uint> NpcMissionIds { get; set; }
        public Dictionary<ConversationType,object> NpcConvoDataDict { get; set; }
        public bool NpcHasMultiConvo = false;
        public bool NpcIsClanMaster = false;
        public bool NpcIsAuctioneer = false;
    }
}
