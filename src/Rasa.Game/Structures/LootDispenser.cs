using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;
    using Managers;
    public class LootDispenser
    {
        public LootDispenser()
        {
            EntityId = EntityManager.Instance.AllocateUnrecycledEntityId();
            EntityClassId = (Data.EntityClasses)10000035;
        }

        public ulong EntityId { get; set; }
        public Data.EntityClasses EntityClassId { get; set; }
        public List<LootItem> LootItems = new List<LootItem>();
        public int Credits { get; set; }
        public ulong Owner { get; set; }
        public ulong AttachedTo { get; set; }
        public bool FullyLooted { get; set; }
        public bool IsLootable { get; set; }
        public LootQuality LootQuality { get; set; }

        /// <summary>
        /// The manifestation with the corpse window open, or 0. Set by RequestCorpseLooting and
        /// cleared by CancelCorpseLooting, which is what the client sends when the window closes.
        /// </summary>
        public ulong CurrentLooter { get; set; }
        internal Game.Client OwnerClient { get; set; }
        internal Manifestation Player { get; set; }
        internal MapChannel Map { get; set; }
        internal Creature Corpse { get; set; }

        /// <summary>
        /// Set instead of <see cref="Corpse"/> when this dispenser is attached to a scripted
        /// prop (a mission reward crate) rather than something a player killed. Exactly one of
        /// the two is set.
        /// </summary>
        internal DynamicObject AttachedObject { get; set; }
        internal uint CharacterId { get; set; }
        internal uint AccountId { get; set; }
        internal Rasa.Repositories.UnitOfWork.IGameUnitOfWorkFactory UnitOfWorkFactory { get; set; }

        /// <summary>Whether anything is left to take.</summary>
        public bool HasLoot => Credits > 0 || LootItems.Exists(i => !i.Taken);

        public LootItem Find(ulong itemEntityId) => LootItems.Find(i => i.EntityId == itemEntityId);

        /// <summary>What a looting player should be shown: everything nobody has taken yet.</summary>
        public List<LootItem> Remaining() => LootItems.FindAll(i => !i.Taken);
    }
}
