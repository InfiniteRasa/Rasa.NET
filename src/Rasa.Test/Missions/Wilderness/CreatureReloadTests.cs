using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Wilderness
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Communicator.Server;

    /// <summary>
    /// .reloadcreatures reads the creature table again (CreatureManager.CreatureInit), and the new
    /// table takes the place of the old one only once every row is in it: a reload that fails part
    /// way keeps the creatures loaded before, and the GM is told rather than disconnected.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureReloadTests
    {
        [TestMethod]
        public void A_reload_reads_every_creature_into_a_new_table()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var before = harness.Creatures.LoadedCreatures;
            Assert.IsNotEmpty(before);

            var said = Reload(harness);

            var after = harness.Creatures.LoadedCreatures;
            Assert.AreNotSame(before, after);
            CollectionAssert.AreEquivalent(before.Keys.ToList(), after.Keys.ToList());
            Assert.HasCount(1, said);
            Assert.AreEqual($"Creatures Reloaded ({after.Count}).", said[0]);
        }

        [TestMethod]
        public void A_reload_that_fails_keeps_the_creatures_loaded_before()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var before = harness.Creatures.LoadedCreatures;
            var count = before.Count;
            var classes = EntityClassManager.Instance.LoadedEntityClasses;

            // A creature row whose class is not loaded: CreatureInit throws reading it.
            var missing = before.Values.Select(creature => creature.EntityClass).First(classes.ContainsKey);
            var entityClass = classes[missing];
            classes.Remove(missing);

            List<string> said;
            try
            {
                Assert.ThrowsExactly<KeyNotFoundException>(() => harness.Creatures.CreatureInit());
                Assert.AreSame(before, harness.Creatures.LoadedCreatures);
                Assert.HasCount(count, before);

                said = Reload(harness);
            }
            finally
            {
                classes[missing] = entityClass;
            }

            Assert.AreSame(before, harness.Creatures.LoadedCreatures);
            Assert.HasCount(count, harness.Creatures.LoadedCreatures);
            Assert.HasCount(1, said);
            StringAssert.StartsWith(said[0], "Creatures were not reloaded (KeyNotFoundException");
            StringAssert.Contains(said[0], $"the {count} creatures loaded before are kept");
        }

        private static List<string> Reload(WildernessRuntimeTestHarness harness)
        {
            harness.Client.AccountEntry.Level = (byte)GmLevel.Admin;

            var commands = new ChatCommandsManager(null);
            commands.RegisterChatCommands();

            harness.Drain();
            commands.ProcessCommand(harness.Client, ".reloadcreatures");

            return harness.Drain().OfType<SystemMessagePacket>().Select(message => message.TextMessage).ToList();
        }
    }
}
