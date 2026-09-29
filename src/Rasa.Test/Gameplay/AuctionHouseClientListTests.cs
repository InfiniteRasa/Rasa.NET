using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.Inventory.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Test.Missions;

    // The client's inbox and auction lists (client/inventory.py g_inboxItems, g_auctionItems)
    // are cleared by the client only on the way back to the login screen, and
    // AuctionStatusSuccess only adds to the auction list. The server resets and refills them.
    [TestClass]
    [DoNotParallelize]
    public class AuctionHouseClientListTests
    {
        private const uint AmmoTemplateId = 28;
        private const uint AmmoClassId = 3147;

        [TestMethod]
        public void CharacterEntryResetsTheAuctionHouseListsBeforeFillingThem()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var waiting = harness.Context.CreateInventoryItem(AmmoTemplateId, AmmoClassId, 5);
            var listed = harness.Context.CreateInventoryItem(AmmoTemplateId, AmmoClassId, 7);
            using (var unit = harness.Context.CreateChar())
            {
                unit.CharacterInventories.AddInvItem(harness.Client.AccountEntry.Id, harness.Client.Player.Id,
                    (uint)InventoryType.InboxInventory, 0, waiting.Id);
                unit.CharacterInventories.AddInvItem(harness.Client.AccountEntry.Id, harness.Client.Player.Id,
                    (uint)InventoryType.AuctionInventory, 0, listed.Id);
            }
            harness.Drain();

            harness.ReconnectFresh(drainPackets: false);
            var packets = harness.Drain();
            var inventory = harness.Client.Player.Inventory;
            try
            {
                var inboxEntity = inventory.InboxItems.Single();
                var auctionEntity = inventory.AuctionItems.Single();
                var resetInbox = IndexOf<ResetInboxInventoryPacket>(packets, _ => true);
                var resetAuction = IndexOf<ResetAuctionInventoryPacket>(packets, _ => true);
                var addInbox = IndexOf<AddInboxItemPacket>(packets, packet => packet.EntityId == inboxEntity);
                var addAuction = IndexOf<AddAuctionItemPacket>(packets, packet => packet.EntityId == auctionEntity);

                Assert.IsTrue(resetInbox >= 0 && resetInbox < addInbox);
                Assert.IsTrue(resetAuction >= 0 && resetAuction < addAuction);
            }
            finally
            {
                foreach (var entityId in inventory.AuctionItems.Concat(inventory.InboxItems))
                    EntityManager.Instance.UnregisterItem(entityId);
            }
        }

        [TestMethod]
        public void MapTransferResendsTheAuctionHouseItemsTheClientForgot()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var waiting = harness.Context.CreateInventoryItem(AmmoTemplateId, AmmoClassId, 5);
            var listed = harness.Context.CreateInventoryItem(AmmoTemplateId, AmmoClassId, 7);
            var inventory = harness.Client.Player.Inventory;
            inventory.InboxItems.Add(waiting.EntityId);
            inventory.AuctionItems.Add(listed.EntityId);
            const ulong gone = ulong.MaxValue - 7;
            inventory.AuctionItems.Add(gone);
            harness.Drain();
            try
            {
                InventoryManager.Instance.ResendForMap(harness.Client);
                var packets = harness.Drain();

                var resetInbox = IndexOf<ResetInboxInventoryPacket>(packets, _ => true);
                var resetAuction = IndexOf<ResetAuctionInventoryPacket>(packets, _ => true);
                foreach (var (item, reset, add) in new[]
                         {
                             (waiting, resetInbox, IndexOf<AddInboxItemPacket>(packets, packet => packet.EntityId == waiting.EntityId)),
                             (listed, resetAuction, IndexOf<AddAuctionItemPacket>(packets, packet => packet.EntityId == listed.EntityId))
                         })
                {
                    var created = IndexOf<CreatePhysicalEntityPacket>(packets, packet => packet.EntityId == item.EntityId);
                    Assert.IsTrue(created >= 0 && created < add, $"Entity {item.EntityId} must be recreated before it is listed.");
                    Assert.IsTrue(reset >= 0 && reset < add, $"Entity {item.EntityId} must be listed after its list is reset.");
                }

                Assert.AreEqual(-1, IndexOf<AddAuctionItemPacket>(packets, packet => packet.EntityId == gone));
            }
            finally
            {
                EntityManager.Instance.UnregisterItem(waiting.EntityId);
                EntityManager.Instance.UnregisterItem(listed.EntityId);
            }
        }

        [TestMethod]
        public void AuctionStatusReplacesTheClientsAuctionList()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            harness.Drain();

            AuctionHouseManager.Instance.RequestAuctionStatus(harness.Client, new RequestAuctionStatusPacket());
            var packets = harness.Drain();

            var reset = IndexOf<ResetAuctionInventoryPacket>(packets, _ => true);
            var status = IndexOf<AuctionStatusSuccessPacket>(packets, _ => true);
            Assert.IsTrue(reset >= 0 && reset < status);
        }

        private static int IndexOf<T>(IReadOnlyList<PythonPacket> packets, System.Func<T, bool> match) where T : PythonPacket
        {
            for (var index = 0; index < packets.Count; index++)
                if (packets[index] is T packet && match(packet))
                    return index;
            return -1;
        }
    }
}
