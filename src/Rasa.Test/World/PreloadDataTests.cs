using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;

    /// <summary>
    /// PreloadData names the weapon in the player's hand by its class, which is what the client's
    /// Recv_PreloadData hands to gameclient.PreloadAssetsForClassId, not by its entity id.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PreloadDataTests
    {
        private const uint PistolClass = 6271;

        [TestMethod]
        public void TheWeaponInHandIsNamedByItsClass()
        {
            using var world = new WorldTestContext();
            var player = world.CreateClient();
            var onlooker = world.CreateClient(x: 5);
            var pistol = Hold(player, PistolClass);

            foreach (var recipient in new[] { player, onlooker })
            {
                var preload = ManifestationManager.Instance.CreatePlayerEntityData(player, recipient).OfType<PreloadDataPacket>().Single();

                Assert.AreEqual(PistolClass, preload.WeaponClassId);
                Assert.AreNotEqual(pistol.EntityId, (ulong)preload.WeaponClassId, "not the item's entity id");
            }
        }

        [TestMethod]
        public void AnEmptyHandNamesNothing()
        {
            using var world = new WorldTestContext();
            var player = world.CreateClient();
            player.Player.Inventory.EquippedInventory[13] = 0;

            Assert.AreEqual(0u, ManifestationManager.Instance.CreatePlayerEntityData(player, player).OfType<PreloadDataPacket>().Single().WeaponClassId);
        }

        [TestMethod]
        public void TheClassAndTheAbilitiesAreWhatGoesOut()
        {
            var abilities = new Dictionary<int, AbilityDrawerData> { [0] = new AbilityDrawerData(0, 177, 3, null) };

            using var stream = new MemoryStream(MissionTestContext.Encode(new PreloadDataPacket(PistolClass, abilities)));
            using var reader = new PythonReader(new BinaryReader(stream));

            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual((int)PistolClass, reader.ReadInt());
            Assert.AreEqual(1, reader.ReadList());
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(177, reader.ReadInt());
            Assert.AreEqual(3, reader.ReadInt());
        }

        private static Item Hold(Client client, uint classId)
        {
            var item = new Item
            {
                ItemTemplate = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = 900001, ItemClass = classId })
            };
            EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(item.EntityId, item);
            client.Player.Inventory.EquippedInventory[13] = item.EntityId;
            return item;
        }
    }
}
