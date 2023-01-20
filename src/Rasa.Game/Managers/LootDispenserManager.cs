using System;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.LootDispenser.Server;
    using Rasa.Packets.ClientMethod.Server;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.LootDispenser.Client;
    using Structures;

    public class LootDispenserManager
    {
        /*      LootDispenser Packets
         * - LootInfo(self, lootItems):
         * - AttachInfo(self, attachedEntityId):
         * - OverallQuality(self, overallQualityId):
         * - CanLootItems(self, isLootable, canLootPerItem):
         * - TakenInfo(self, takenItems):
         * - ActorGotLoot(self, actorId, lootEntityIds):
         * - Use(self, actorId, curStateId, * args):
         * - LootCorpse(self, actorId, lootItems):
         * 
         *      LootDispenser Handlers:
         * - RequestCorpseLooting (self.entityId,)
         * - CancelCorpseLooting (self.entityId,)
         * - RequestLootAllFromCorpse (self.entityId, autoLootOnly)
         * - RequestLootItemFromCorpse (self.entityId, itemId, destSlot)
         */

        private static LootDispenserManager _instance;
        private static readonly object InstanceLock = new object();

        public static LootDispenserManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new LootDispenserManager();
                    }
                }

                return _instance;
            }
        }

        private LootDispenserManager()
        {
        }

        internal void AttachInfo(Client client, LootDispenser loot)
        {
            client.CallMethod(loot.EntityId, new AttachInfoPacket(loot.AttachedTo));
        }

        internal void LootInfo(Client client, LootDispenser loot)
        {
            client.CallMethod(loot.EntityId, new LootInfoPacket(loot.LootItems));
        }

        internal void OverallQuality(Client client, LootDispenser loot)
        {
            client.CallMethod(loot.EntityId, new OverallQualityPacket(loot.LootQuality));
        }

        internal void CanLootItems(Client client, LootDispenser loot)
        {
            client.CallMethod(loot.EntityId, new CanLootItemsPacket(loot.IsLootable, loot.LootItems));
        }

        internal void GotLoot(Client client, LootDispenser loot)
        {
            client.CallMethod(SysEntity.ClientMethodId, new GotLootPacket(loot));
        }

        internal LootDispenser Create(Client killer, Creature creature)
        {
            var mapChannel = killer.Player.MapChannel;
            var loot = new LootDispenser();
            loot.IsLootable = true;
            loot.AttachedTo = creature.EntityId;
            loot.Owner = killer.Player.EntityId;

            CreateLoot(killer, loot);

            mapChannel.LootDispensers.Add(loot.EntityId, loot);

            return loot;
        }

        private LootDispenser CreateLoot(Client killer, LootDispenser loot)
        {
            var giveLoot = new Random().Next(0, 2);
            if (giveLoot > 0)
                loot.LootItems.Add(new LootItem(28, 3147, (uint)giveLoot*3, killer.Player.EntityId, 0));

            loot.Credits = new Random().Next(1, 10);
            loot.LootQuality = (LootQuality)new Random().Next(1, 7);

            return loot;
        }

        internal void Loot(Client client, Creature creature)
        {
            var loot = Create(client, creature);

            client.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(loot.EntityId, loot.EntityClassId));

            AttachInfo(client, loot);
            LootInfo(client, loot);
            OverallQuality(client, loot);
            CanLootItems(client, loot);
        }

        internal void RequestLootAllFromCorpse(Client client, RequestLootAllFromCorpsePacket packet)
        {
            var loot = client.Player.MapChannel.LootDispensers[packet.EntityId];

            if (loot.Owner != client.Player.EntityId)
                return;

            if (loot.FullyLooted)
                return;

            client.CallMethod(loot.EntityId, new ActorGotLootPacket(loot));
            client.CallMethod(loot.EntityId, new TakenInfoPacket(client.Player.EntityId, loot.LootItems));

            loot.IsLootable = false;
            CanLootItems(client, loot);
            GotLoot(client, loot);

            if (loot.LootItems.Count > 0)
                foreach (var lootItem in loot.LootItems)
                {
                    var item = ItemManager.Instance.CreateFromTemplateId(lootItem.ItemTemplateId, lootItem.ItemQuantity);
                    InventoryManager.Instance.AddItemToInventory(client, item);
                }

            if (loot.Credits > 0)
                CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Credits, loot.Credits);
        }

        internal void RequestCorpseLooting(Client client, RequestCorpseLootingPacket packet)
        {
            // ToDo
        }
    }
}
