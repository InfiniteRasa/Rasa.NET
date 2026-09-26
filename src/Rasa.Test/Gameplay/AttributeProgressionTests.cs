using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Structures;
    using Rasa.Structures.World;

    [TestClass]
    [DoNotParallelize]
    public class AttributeProgressionTests
    {
        [TestMethod]
        public void LevelTwentySixRetainsFractionalChiAndRegeneration()
        {
            using var context = new ProgressionTestContext();
            var client = context.CreateClient(26);
            client.Player.Inventory.EquippedInventory = Enumerable.Repeat(0UL, 21).ToList();

            ManifestationManager.Instance.UpdateStatsValues(client, true);

            Assert.AreEqual(178, client.Player.Attributes[Attributes.Chi].Current);
            Assert.AreEqual(152, client.Player.Attributes[Attributes.Regen].CurrentMax);
            Assert.AreEqual(3, client.Player.Attributes[Attributes.Health].RefreshAmount);
            Assert.AreEqual(60, client.Player.Attributes[Attributes.Body].Current);
            Assert.AreEqual(60, client.Player.Attributes[Attributes.Mind].Current);
        }

        [TestMethod]
        [DataRow((byte)1, Race.Human, 10, 10, 10, 286, 103, 102)]
        [DataRow((byte)20, Race.Human, 48, 48, 48, 1486, 160, 140)]
        [DataRow((byte)26, Race.Human, 60, 60, 60, 2500, 178, 152)]
        [DataRow((byte)50, Race.Human, 108, 108, 108, 20000, 250, 200)]
        [DataRow((byte)20, Race.Forean, 29, 67, 48, 1094, 181, 158)]
        [DataRow((byte)20, Race.Brann, 29, 29, 86, 1486, 96, 195)]
        [DataRow((byte)20, Race.Thrax, 67, 29, 48, 1878, 138, 121)]
        public void RecalculationPreservesRaceBaselinesAndHealthTable(byte level, Race race,
            int body, int mind, int spirit, int health, int chi, int regen)
        {
            using var context = new ProgressionTestContext();
            var client = context.CreateClient(level, race);

            ManifestationManager.Instance.UpdateStatsValues(client, true);

            Assert.AreEqual(body, client.Player.Attributes[Attributes.Body].Current);
            Assert.AreEqual(mind, client.Player.Attributes[Attributes.Mind].Current);
            Assert.AreEqual(spirit, client.Player.Attributes[Attributes.Spirit].Current);
            Assert.AreEqual(health, client.Player.Attributes[Attributes.Health].Current);
            Assert.AreEqual(chi, client.Player.Attributes[Attributes.Chi].Current);
            Assert.AreEqual(regen, client.Player.Attributes[Attributes.Regen].CurrentMax);
        }

        [TestMethod]
        [DataRow((byte)0)]
        [DataRow((byte)51)]
        [DataRow(byte.MaxValue)]
        public void InvalidPersistedLevelsAreDiagnosedBeforeChangingStats(byte level)
        {
            using var context = new ProgressionTestContext();
            var client = context.CreateClient(level);

            var error = Assert.ThrowsExactly<InvalidProgressionLevelException>(() =>
                ManifestationManager.Instance.UpdateStatsValues(client, true));

            StringAssert.Contains(error.Message, $"Character {client.Player.Id}");
            StringAssert.Contains(error.Message, $"level {level}");
            Assert.IsTrue(client.Player.Attributes.Values.All(attribute => attribute.CurrentMax == 0));
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(17)]
        [DataRow(22)]
        public void RecalculationRespectsTheInitializedEquipmentLength(int length)
        {
            using var context = new ProgressionTestContext();
            var client = context.CreateClient();
            client.Player.Inventory.EquippedInventory = Enumerable.Repeat(0UL, length).ToList();

            ManifestationManager.Instance.UpdateStatsValues(client, true);

            Assert.AreEqual(103, client.Player.Attributes[Attributes.Chi].Current);
            Assert.AreEqual(0, client.Player.Attributes[Attributes.Armor].Current);
        }

        [TestMethod]
        [DataRow(16)]
        [DataRow(21)]
        public void RecalculationSkipsMissingEquipmentAndWeaponAndPreservesCurrentArmor(int armorSlot)
        {
            using var context = new ProgressionTestContext();
            var client = context.CreateClient();
            client.Player.Inventory.EquippedInventory = Enumerable.Repeat(0UL, armorSlot + 1).ToList();
            var armorClass = (EntityClasses)900001;
            context.World.AddClass(armorClass);
            EntityClassManager.Instance.LoadedEntityClasses[armorClass].ArmorClassInfo =
                new ArmorClassInfo(new ArmorClassEntry { RegenRate = 7 });
            var armor = new Item
            {
                ItemTemplate = new ItemTemplate(new ItemTemplateItemClassEntry { ItemClass = (uint)armorClass })
                {
                    ArmorValue = 100
                }
            };
            EntityManager.Instance.RegisterItem(armor.EntityId, armor);
            try
            {
                client.Player.Inventory.EquippedInventory[1] = ulong.MaxValue;
                client.Player.Inventory.EquippedInventory[13] = armor.EntityId;
                client.Player.Inventory.EquippedInventory[armorSlot] = armor.EntityId;
                client.Player.Attributes[Attributes.Health].Current = 200;
                client.Player.Attributes[Attributes.Chi].Current = 20;
                client.Player.Attributes[Attributes.Power].Current = 40;
                client.Player.Attributes[Attributes.Armor].Current = 90;

                ManifestationManager.Instance.UpdateStatsValues(client, false);

                Assert.AreEqual(107, client.Player.Attributes[Attributes.Armor].CurrentMax);
                Assert.AreEqual(90, client.Player.Attributes[Attributes.Armor].Current);
                Assert.AreEqual(7, client.Player.Attributes[Attributes.Armor].RefreshAmount);
                Assert.AreEqual(200, client.Player.Attributes[Attributes.Health].Current);
                Assert.AreEqual(20, client.Player.Attributes[Attributes.Chi].Current);
                Assert.AreEqual(40, client.Player.Attributes[Attributes.Power].Current);

                client.Player.Attributes[Attributes.Armor].Current = 1000;
                client.Player.Attributes[Attributes.Health].Current = 1000;
                client.Player.Attributes[Attributes.Chi].Current = 1000;
                client.Player.Attributes[Attributes.Power].Current = 1000;
                ManifestationManager.Instance.UpdateStatsValues(client, false);
                Assert.AreEqual(107, client.Player.Attributes[Attributes.Armor].Current);
                Assert.AreEqual(286, client.Player.Attributes[Attributes.Health].Current);
                Assert.AreEqual(103, client.Player.Attributes[Attributes.Chi].Current);
                Assert.AreEqual(100, client.Player.Attributes[Attributes.Power].Current);
                ManifestationManager.Instance.UpdateStatsValues(client, true);
                Assert.AreEqual(107, client.Player.Attributes[Attributes.Armor].Current);
                Assert.AreEqual(286, client.Player.Attributes[Attributes.Health].Current);
                Assert.AreEqual(103, client.Player.Attributes[Attributes.Chi].Current);
                Assert.AreEqual(100, client.Player.Attributes[Attributes.Power].Current);
            }
            finally
            {
                EntityManager.Instance.UnregisterItem(armor.EntityId);
                EntityManager.Instance.FreeEntity(armor.EntityId);
            }
        }
    }
}
