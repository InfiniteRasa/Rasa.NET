using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
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

        /// <summary>
        /// A creature put into the world takes its stats from the rows CreatureInit read, not a
        /// query of its own on the map's loop: a row changed in the database afterwards is not
        /// what it gets, until the creatures are reloaded.
        /// </summary>
        [TestMethod]
        public void A_spawned_creature_takes_the_stats_read_with_the_creatures()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var row = harness.World.CreatureStatEntries.AsNoTracking()
                .Where(stats => harness.Creatures.LoadedCreatures.Keys.Contains(stats.Id))
                .OrderBy(stats => stats.Id)
                .First();
            harness.World.Database.ExecuteSqlRaw($"UPDATE creature_stat SET body = {row.Body + 7} WHERE id = {row.Id}");

            var spawned = harness.Creatures.CreateCreature(row.Id, null);

            Assert.AreEqual(row.Body, spawned.Attributes[Attributes.Body].Current, "the row as CreatureInit read it");
            Assert.AreEqual(row.Health, spawned.Attributes[Attributes.Health].CurrentMax);

            Assert.HasCount(1, Reload(harness));
            var reloaded = harness.Creatures.CreateCreature(row.Id, null);

            Assert.AreEqual(row.Body + 7, reloaded.Attributes[Attributes.Body].Current, "and as a reload reads it");
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
