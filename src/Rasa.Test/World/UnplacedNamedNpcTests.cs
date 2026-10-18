using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Managers;
    using Rasa.Services.Preloader;
    using Rasa.Test.Database;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The named NPCs with recorded barks made unplaced (UnplacedNamedNpcs): each goes in with its
    /// look and barks and no spawn pool, once, and Down takes out only them.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class UnplacedNamedNpcTests
    {
        private const string Migration = "20270117000000_Unplaced_named_npcs";
        private const string Before = "20270116000000_Wilderness_reward_modules";

        private static readonly (uint Id, uint NameId, uint ClassId, uint[] Barks)[] Expected =
        {
            (UnplacedNamedNpcs.Johnson, 206, AliaDasNpcs.SwapsetMale, new uint[] { 22 }),
            (UnplacedNamedNpcs.Yates, 214, AliaDasNpcs.SwapsetFemale, new uint[] { 23 }),
            (UnplacedNamedNpcs.Elvers, 3091, AliaDasNpcs.SwapsetMale, new uint[] { 27, 205, 206, 207 }),
            (UnplacedNamedNpcs.Hernandez, 4855, AliaDasNpcs.SwapsetMale, new uint[] { 38 }),
            (UnplacedNamedNpcs.Ryosa, 5257, AliaDasNpcs.ForeanSpearman, new uint[] { 40 }),
            (UnplacedNamedNpcs.Jennings, 5258, AliaDasNpcs.SwapsetMale, new uint[] { 43 })
        };

        [TestCleanup]
        public void Cleanup() => NpcBarks.Load(null);

        [TestMethod]
        public void EachGoesInWithItsLookAndBarksAndNoPool()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Before);
            Assert.IsFalse(database.Context.CreatureEntries.AsNoTracking().Any(c => c.Id >= UnplacedNamedNpcs.First && c.Id <= UnplacedNamedNpcs.Last),
                "the ids are free");

            migrator.Migrate(Migration);
            var context = database.Context;

            foreach (var (id, nameId, classId, barks) in Expected)
            {
                var creature = context.CreatureEntries.AsNoTracking().Single(c => c.Id == id);
                Assert.AreEqual((nameId, classId, 1u, 10u, 1000u, 0u, 0u), (creature.NameId, creature.ClassId, creature.Faction, creature.Level,
                    creature.MaxHitPoints, creature.RunSpeed, creature.WalkSpeed), creature.Comment);

                CollectionAssert.AreEquivalent(barks, context.NpcBarkEntries.AsNoTracking().Where(b => b.CreatureId == id).Select(b => b.BarkId).ToList(), creature.Comment);

                var look = UnplacedNamedNpcs.Npcs.Single(n => n.Creature.Id == id).Look
                    .Select(s => (s.Id, s.ClassId, s.Color)).OrderBy(s => s.Id).ToList();
                var rows = context.CreatureAppearanceEntries.AsNoTracking().Where(a => a.Id == id)
                    .AsEnumerable().Select(a => (a.SlotId, a.ClassId, a.Color)).OrderBy(s => s.SlotId).ToList();
                CollectionAssert.AreEqual(look, rows, creature.Comment);

                Assert.IsFalse(context.SpawnPoolEntries.AsNoTracking().Any(p => p.Creature1Id == id || p.Creature2Id == id || p.Creature3Id == id
                    || p.Creature4Id == id || p.Creature5Id == id || p.Creature6Id == id), creature.Comment + ": unplaced");
            }

            Assert.AreEqual(Expected.Length, UnplacedNamedNpcs.Npcs.Count);
        }

        [TestMethod]
        public void EveryPieceOfALookFitsItsSlot()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);
            var fits = database.Context.EquipableClassEntries.AsNoTracking().AsEnumerable().Select(e => (e.Id, e.SlotId)).ToHashSet();

            foreach (var npc in UnplacedNamedNpcs.Npcs)
                foreach (var slot in npc.Look)
                    Assert.Contains((slot.ClassId, slot.Id), fits, $"{npc.Creature.Comment}: class {slot.ClassId} in slot {slot.Id}");
        }

        [TestMethod]
        public void ASecondRunAddsNothingAndDownTakesOutOnlyThem()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Before);
            var before = Counts(database);

            migrator.Migrate(Migration);
            var after = Counts(database);
            Assert.AreEqual(before.Creatures + 6, after.Creatures);
            Assert.AreEqual(before.Barks + 9, after.Barks);
            Assert.AreEqual(before.Looks + 30, after.Looks);

            foreach (var sql in UnplacedNamedNpcs.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(after, Counts(database), "a second run adds nothing");

            migrator.Migrate(Before);
            Assert.AreEqual(before, Counts(database));
        }

        [TestMethod]
        public void DownKeepsOneThatHasBeenPlaced()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            const uint elvers = UnplacedNamedNpcs.Elvers;

            migrator.Migrate(Migration);
            database.Context.Database.ExecuteSqlRaw(
                "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                $"VALUES (999901, 0, 0, 20, 1, 2, 3, 0, 1985, {elvers}, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);");

            migrator.Migrate(Before);

            Assert.IsTrue(database.Context.CreatureEntries.AsNoTracking().Any(c => c.Id == elvers), "placed, so kept");
            Assert.IsFalse(database.Context.CreatureEntries.AsNoTracking().Any(c => c.Id == UnplacedNamedNpcs.Johnson));
        }

        [TestMethod]
        public void TheServerLoadsThemWithTheirBarks()
        {
            using var harness = WildernessRuntimeTestHarness.Create();

            foreach (var (id, nameId, _, barks) in Expected)
            {
                var creature = harness.Creatures.LoadedCreatures[id];
                Assert.AreEqual(nameId, creature.NameId);
                CollectionAssert.AreEquivalent(barks, NpcBarks.BarksOf(creature).ToArray(), creature.Name);
            }
        }

        [TestMethod]
        public void OnMySqlTheyGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            foreach (var (id, nameId, _, barks) in Expected)
            {
                StringAssert.Contains(up, $"SELECT {id}, ");
                foreach (var bark in barks)
                {
                    StringAssert.Contains(up, $"INSERT INTO npc_bark (creature_id, bark_id) SELECT {id}, {bark} FROM creature");
                    StringAssert.Contains(down, $"DELETE FROM npc_bark WHERE creature_id = {id} AND bark_id = {bark};");
                }
                StringAssert.Contains(down, $"DELETE FROM creature WHERE id = {id} AND name_id = {nameId} ");
            }

            StringAssert.Contains(up, $"'{Migration}'");
        }

        private static (int Creatures, int Barks, int Looks) Counts(OrdinaryLootTestDatabase database) =>
            (database.Context.CreatureEntries.Count(), database.Context.NpcBarkEntries.Count(), database.Context.CreatureAppearanceEntries.Count());
    }
}
