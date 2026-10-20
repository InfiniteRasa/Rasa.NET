using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Missions.Content;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Structures.World;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets.Mission.Server;
    using Rasa.Structures;

    /// <summary>
    /// A mission's items required (MissionSceneDefinition.ItemsRequired): the offer carries them
    /// in its itemsRequired, where the client lists them and holds Accept back without them, and
    /// the acceptance is refused to a character who carries none of one of them.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MissionItemsRequiredTests
    {
        private const uint Keycard = 990301;
        private const uint Datapad = 990302;

        [TestMethod]
        public void TheOfferCarriesTheItemsRequiredInItsSlot()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var catalog = new MissionContentCatalog(context, context.Manager.LoadedMissions,
                new Dictionary<uint, MissionRewardDefinition>());
            var protocol = new MissionProtocolAdapter(context, catalog, new MissionJournalAdapter(catalog),
                () => DateTime.UtcNow, null, context.Manager.Sharing.CanShare);

            Assert.IsEmpty(protocol.BuildOfferInfo(catalog.Missions[321]).ItemRequired, "none authored: none sent");

            catalog.SceneBindings[321] = new MissionSceneDefinition { ItemsRequired = new List<uint> { Keycard, Datapad } };
            var offer = protocol.BuildOfferInfo(catalog.Missions[321]);
            CollectionAssert.AreEqual(new[] { (int)Keycard, (int)Datapad }, offer.ItemRequired);

            // (level, rewardInfo, offerVOAudioSetId, itemsRequired, objectives, groupType): the fourth.
            using var stream = new MemoryStream(MissionTestContext.Encode(new DispenseRadioMissionPacket(321, offer, false)));
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(321U, reader.ReadUInt());
            Assert.AreEqual(6, reader.ReadTuple());
            reader.ReadUInt();
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(2, reader.ReadTuple());
            var currencies = reader.ReadList();
            for (var index = 0; index < currencies; index++)
            {
                Assert.AreEqual(2, reader.ReadTuple());
                reader.ReadInt();
                reader.ReadUInt();
            }
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(0, reader.ReadList());
            reader.ReadNoneStruct();
            Assert.AreEqual(2, reader.ReadList());
            Assert.AreEqual((int)Keycard, reader.ReadInt());
            Assert.AreEqual((int)Datapad, reader.ReadInt());
        }

        [TestMethod]
        public void TheAcceptanceIsRefusedUntilTheCharacterCarriesOneOfEach()
        {
            using var harness = BootcampRuntimeTestHarness.Create(configureScenes: scenes =>
                scenes[1990].ItemsRequired = new List<uint> { Keycard, Datapad });
            var giver = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            harness.Drain();
            var items = new List<Item>();
            try
            {
                Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1990), "neither");

                items.Add(Carry(harness.Client, Keycard));
                Assert.AreEqual(Datapad, harness.Manager.MissingRequiredItem(harness.Client.Player, 1990));
                Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1990), "one of the two");
                Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(1990));

                items.Add(Carry(harness.Client, Datapad));
                Assert.IsNull(harness.Manager.MissingRequiredItem(harness.Client.Player, 1990));
                Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1990));
                Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[1990].State);
                Assert.IsTrue(items.All(item => harness.Client.Player.Inventory.PersonalInventory.Contains(item.EntityId)), "none taken");
            }
            finally
            {
                foreach (var item in items)
                    EntityManager.Instance.UnregisterItem(item.EntityId);
            }
        }

        [TestMethod]
        public void AnItemRequiredOfClassNoneOrTwiceFailsReadiness()
        {
            foreach (var required in new[] { new List<uint> { 0 }, new List<uint> { Keycard, Keycard }, new List<uint>() })
            {
                using var harness = BootcampRuntimeTestHarness.Create();
                Content.MissionContentTestSupport.ConfigureScenes(harness.WorldContext,
                    scenes => scenes[1990].ItemsRequired = required);
                Assert.ThrowsExactly<Rasa.Missions.Runtime.MissionRuleException>(() => harness.Manager.LoadMissions(),
                    string.Join(",", required));
            }
        }

        /// <summary>An item of the class put in the first free slot of the personal inventory.</summary>
        private static Item Carry(Client client, uint classId)
        {
            var template = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = classId, ItemClass = classId });
            var item = new Item { ItemTemplate = template, ItemTemplateId = classId, StackSize = 1 };
            EntityManager.Instance.RegisterItem(item.EntityId, item);

            var slots = client.Player.Inventory.PersonalInventory;
            var free = slots.IndexOf(0);
            if (free >= 0)
                slots[free] = item.EntityId;
            else
                slots.Add(item.EntityId);

            return item;
        }
    }
}
