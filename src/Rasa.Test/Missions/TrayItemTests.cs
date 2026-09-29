using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;

    [TestClass]
    [DoNotParallelize]
    public class TrayItemTests
    {
        private const uint PineOckTemplate = 111117;       // Companion Pine-Ock: 460 level 1

        private static readonly FieldInfo AbilityManagerInstance =
            typeof(AbilityManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);

        [TestMethod]
        public void AnItemsActionArrivesAsPythonIntsAndIsRead()
        {
            var packet = Read(writer =>
            {
                writer.WriteTuple(4);
                writer.WriteInt(2);
                writer.WriteInt(460);       // 0x1E: an item's action, from its template, is an int
                writer.WriteInt(1);
                writer.WriteULong(123456);
            });

            Assert.AreEqual(2, packet.SlotId);
            Assert.AreEqual(460L, packet.AbilityId);
            Assert.AreEqual(1L, packet.AbilityLevel);
            Assert.AreEqual(123456UL, packet.ItemId);
        }

        [TestMethod]
        public void AClearedSlotIsAllNone()
        {
            var packet = Read(writer =>
            {
                writer.WriteTuple(4);
                writer.WriteInt(5);
                writer.WriteNoneStruct();
                writer.WriteNoneStruct();
                writer.WriteNoneStruct();
            });

            Assert.AreEqual(5, packet.SlotId);
            Assert.AreEqual(0L, packet.AbilityId);
            Assert.AreEqual(0L, packet.AbilityLevel);
            Assert.AreEqual(0UL, packet.ItemId);
        }

        [TestMethod]
        public void APetDraggedOntoTheTrayKeepsItsItemAndGoesBackWithIt()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var pet = ToyTests.Grant(harness, PineOckTemplate);
            using var abilities = UseAbilityManager(ToyTests.CreateManager(harness, 460, 1, PineOckTemplate));
            harness.Drain();

            ManifestationManager.Instance.RequestSetAbilitySlot(harness.Client,
                new RequestSetAbilitySlotPacket { SlotId = 2, AbilityId = 460, AbilityLevel = 1, ItemId = pet.EntityId });

            var slot = harness.Client.Player.Abilities[2];
            Assert.AreEqual(460, slot.AbilityId);
            Assert.AreEqual(1U, slot.AbilityLevel);
            Assert.AreEqual(pet.Id, slot.ItemId);

            using (var unit = harness.Context.CreateChar())
                Assert.AreEqual(pet.Id, unit.CharacterAbilityDrawers.GetCharacterAbilities(harness.Client.Player.Id)
                    .Single(row => row.AbilitySlot == 2).ItemId);

            var drawer = harness.Drain().OfType<AbilityDrawerPacket>().Single();
            Assert.AreEqual(pet.EntityId, drawer.ItemEntities[2]);

            // And from the database, as a login loads it.
            var loaded = harness.Maps.GetPlayerAbilities(harness.Client.Player.Id)[2];
            Assert.AreEqual(pet.Id, loaded.ItemId);
            Assert.AreEqual(pet.EntityId, AbilityDrawerItems.EntityOf(harness.Client.Player, loaded));
        }

        [TestMethod]
        public void AnItemCannotBringAnActionItDoesNotPerform()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var pet = ToyTests.Grant(harness, PineOckTemplate);
            using var abilities = UseAbilityManager(ToyTests.CreateManager(harness, 460, 1, PineOckTemplate));

            ManifestationManager.Instance.RequestSetAbilitySlot(harness.Client,
                new RequestSetAbilitySlotPacket { SlotId = 2, AbilityId = 460, AbilityLevel = 10, ItemId = pet.EntityId });

            Assert.IsFalse(harness.Client.Player.Abilities.ContainsKey(2));
        }

        [TestMethod]
        public void AGoneItemIsStoodInForByAnotherThatPerformsTheSameAction()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var pet = ToyTests.Grant(harness, PineOckTemplate);
            using var abilities = UseAbilityManager(ToyTests.CreateManager(harness, 460, 1, PineOckTemplate));

            var gone = new AbilityDrawerData(2, 460, 1, pet.Id + 100000);
            Assert.AreEqual(pet.EntityId, AbilityDrawerItems.EntityOf(harness.Client.Player, gone));

            var otherAction = new AbilityDrawerData(3, 482, 1, pet.Id + 100000);
            Assert.IsNull(AbilityDrawerItems.EntityOf(harness.Client.Player, otherAction));

            var skill = new AbilityDrawerData(4, 460, 1);
            Assert.IsNull(AbilityDrawerItems.EntityOf(harness.Client.Player, skill));
        }

        private static RequestSetAbilitySlotPacket Read(System.Action<PythonWriter> write)
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, Encoding.UTF8, true)))
                write(writer);
            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            var packet = new RequestSetAbilitySlotPacket();
            packet.Read(reader);
            return packet;
        }

        private static Restore UseAbilityManager(AbilityManager manager)
        {
            var previous = AbilityManagerInstance.GetValue(null);
            AbilityManagerInstance.SetValue(null, manager);
            return new Restore(() => AbilityManagerInstance.SetValue(null, previous));
        }

        private sealed class Restore : System.IDisposable
        {
            private readonly System.Action _undo;
            public Restore(System.Action undo) => _undo = undo;
            public void Dispose() => _undo();
        }
    }
}
