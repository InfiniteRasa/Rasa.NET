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

        /// <summary>
        /// A class trainer: talking to one opens the tier advancement window.
        ///
        /// There is no Trainer augmentation to read this from - the client's list has none - so
        /// it comes from the entity class the trainers are built on, the way the clan master is
        /// recognised by its name id.
        /// </summary>
        public bool NpcIsTrainer = false;

        /// <summary>
        /// The line this NPC greets a player with, an id of the client's npcgreetinglanguage
        /// (npc_greeting), or 0 if it has none of its own and says the default (NpcGreetings).
        /// </summary>
        public uint GreetingId { get; set; }

        /// <summary>
        /// Whether its line is marked important (npc_greeting.important): it stands with the
        /// client's speech bubble over its head while it has nothing else for the player.
        /// </summary>
        public bool GreetingImportant { get; set; }
    }
}
