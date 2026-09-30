using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Missions.Definitions;
using Rasa.Packets.Communicator.Server;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;
using Rasa.Structures.Missions;
using Rasa.Test.Missions;

namespace Rasa.Test.World
{
    [TestClass]
    [DoNotParallelize]
    public class TitleTests
    {
        private const uint Shadow = 905;
        private const uint SpecialForces = 941;

        [TestMethod]
        public void NoTitleIsNoneOnTheWire()
        {
            using (var reader = Reader(new TitleChangedPacket(0)))
            {
                Assert.AreEqual(1, reader.ReadTuple());
                reader.ReadNoneStruct();
            }

            using (var reader = Reader(new TitleChangedPacket(Shadow)))
            {
                Assert.AreEqual(1, reader.ReadTuple());
                Assert.AreEqual(Shadow, reader.ReadUInt());
            }
        }

        [TestMethod]
        public void AWornTitleIsSavedAndShownToEveryoneAround()
        {
            using var context = Context();
            var manager = new ManifestationManager(context);
            var watcher = context.CreateAdditionalClient(2);
            var player = context.Client.Player;
            GiveTitles(context, player, Shadow, SpecialForces);
            context.Drain();
            MissionTestContext.Drain(watcher);

            manager.ChangeTitle(context.Client, Shadow);

            Assert.AreEqual(Shadow, player.CurrentTitle);
            Assert.AreEqual(Shadow, SavedTitle(context, player.Id));
            Assert.AreEqual(Shadow, context.Drain().OfType<TitleChangedPacket>().Single().TitleId);
            Assert.AreEqual(Shadow, MissionTestContext.Drain(watcher).OfType<TitleChangedPacket>().Single().TitleId, "The others see it.");

            // Someone who meets them later has it in the player's creation data.
            Assert.AreEqual(Shadow, manager.CreatePlayerEntityData(context.Client, watcher).OfType<TitleChangedPacket>().Single().TitleId);

            manager.ChangeTitle(context.Client, 0);

            Assert.AreEqual(0U, SavedTitle(context, player.Id));
            Assert.AreEqual(0U, MissionTestContext.Drain(watcher).OfType<TitleChangedPacket>().Single().TitleId);
            Assert.IsEmpty(manager.CreatePlayerEntityData(context.Client, watcher).OfType<TitleChangedPacket>().ToArray(), "None: nothing to send.");
        }

        [TestMethod]
        public void ATitleNotHeldIsRefusedAndNothingChanges()
        {
            using var context = Context();
            var manager = new ManifestationManager(context);
            var player = context.Client.Player;
            GiveTitles(context, player, Shadow);
            manager.ChangeTitle(context.Client, Shadow);
            context.Drain();

            manager.ChangeTitle(context.Client, SpecialForces);

            Assert.AreEqual(Shadow, player.CurrentTitle);
            Assert.AreEqual(Shadow, SavedTitle(context, player.Id));
            Assert.IsEmpty(context.Drain().OfType<TitleChangedPacket>().ToArray());
        }

        [TestMethod]
        public void TheTitlesAndTheOneWornAreThereAfterARelog()
        {
            using var harness = BootcampRuntimeTestHarness.CreateFromPendingSelection();
            var player = harness.Client.Player;
            using (var unit = harness.Context.CreateChar())
            {
                unit.CharacterTitles.Add(player.Id, Shadow);
                unit.CharacterTitles.Add(player.Id, SpecialForces);
            }
            player.Titles = new List<uint> { Shadow, SpecialForces };
            ManifestationManager.Instance.ChangeTitle(harness.Client, SpecialForces);

            harness.ReconnectFromSelection();

            CollectionAssert.AreEquivalent(new[] { Shadow, SpecialForces }, harness.Client.Player.Titles.ToArray());
            Assert.AreEqual(SpecialForces, harness.Client.Player.CurrentTitle);
        }

        [TestMethod]
        public void AWornTitleNoLongerHeldIsNotPutBackOn()
        {
            using var harness = BootcampRuntimeTestHarness.CreateFromPendingSelection();
            var player = harness.Client.Player;
            using (var unit = harness.Context.CreateChar())
                unit.Characters.UpdateCharacterCurrentTitle(player.Id, Shadow);

            harness.ReconnectFromSelection();

            Assert.AreEqual(0U, harness.Client.Player.CurrentTitle);
        }

        [TestMethod]
        public void AddTitleSavesTheTitle()
        {
            using var harness = BootcampRuntimeTestHarness.CreateFromPendingSelection();
            harness.Client.AccountEntry.Level = (byte)GmLevel.Admin;
            var commands = new ChatCommandsManager(null);
            commands.RegisterChatCommands();
            harness.Drain();

            commands.ProcessCommand(harness.Client, $".addtitle {Shadow}");

            Assert.AreEqual(Shadow, harness.Drain().OfType<TitleAddedPacket>().Single().TitleId);
            CollectionAssert.Contains(harness.Client.Player.Titles, Shadow);
            using (var unit = harness.Context.CreateChar())
                CollectionAssert.Contains(unit.CharacterTitles.Get(harness.Client.Player.Id), Shadow);

            commands.ProcessCommand(harness.Client, $".addtitle {Shadow}");
            var packets = harness.Drain();
            Assert.IsEmpty(packets.OfType<TitleAddedPacket>().ToArray(), "Once.");
            Assert.IsTrue(packets.OfType<SystemMessagePacket>().Any(m => m.TextMessage.Contains("already")));

            harness.ReconnectFromSelection();
            CollectionAssert.Contains(harness.Client.Player.Titles, Shadow);
        }

        private static MissionTestContext Context() =>
            MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission>(),
                new Dictionary<uint, MissionRewardDefinition>());

        private static void GiveTitles(MissionTestContext context, Manifestation player, params uint[] titles)
        {
            using (var unit = context.CreateChar())
                foreach (var title in titles)
                    unit.CharacterTitles.Add(player.Id, title);

            player.Titles = titles.ToList();
        }

        private static uint SavedTitle(MissionTestContext context, uint characterId)
        {
            using var unit = context.CreateChar();
            return unit.Characters.Get(characterId).CurrentTitleId;
        }

        private static PythonReader Reader(Rasa.Packets.PythonPacket packet) =>
            new PythonReader(new BinaryReader(new MemoryStream(MissionTestContext.Encode(packet))));
    }
}
