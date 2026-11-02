using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;

namespace Rasa.Test.Database
{
    // Casso's channel and the instances' Hunters' shields (Instance_creature_kits).
    [TestClass]
    public class InstanceCreatureKitsTests
    {
        private const string Migration = "20271021000000_Instance_creature_kits";
        private const string Previous = "20271020000000_Marker_map_scopes";

        private const uint Casso = InstanceCreatureKits.Casso;

        [TestMethod]
        public void UpGivesCassoHisChannelAndTheHuntersTheirShieldsAndDownTakesThemOff()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            Assert.AreEqual(0L, Action4(database), "put in without it");
            foreach (var hunter in InstanceCreatureKits.Hunters)
                Assert.AreEqual(0, Slot(database, hunter).Length, $"hunter {hunter}");

            migrator.Migrate(Migration);
            Assert.AreEqual((long)InstanceCreatureKits.LinkerChannel, Action4(database));
            Assert.AreEqual(410L, Query(database, $"SELECT action_id AS Value FROM creature_action WHERE id = {InstanceCreatureKits.LinkerChannel}").Single(),
                "linker_channel");
            foreach (var hunter in InstanceCreatureKits.Hunters)
                CollectionAssert.AreEqual(new long[] { HunterShields.Shield }, Slot(database, hunter), $"hunter {hunter}");

            foreach (var sql in InstanceCreatureKits.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual((long)InstanceCreatureKits.LinkerChannel, Action4(database), "Up twice");
            foreach (var hunter in InstanceCreatureKits.Hunters)
                CollectionAssert.AreEqual(new long[] { HunterShields.Shield }, Slot(database, hunter), $"hunter {hunter}: one shield, not two");

            migrator.Migrate(Previous);
            Assert.AreEqual(0L, Action4(database));
            foreach (var hunter in InstanceCreatureKits.Hunters)
                Assert.AreEqual(0, Slot(database, hunter).Length, $"hunter {hunter}");
        }

        [TestMethod]
        public void WhatIsThereByHandIsLeftAloneBothWays()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var hunter = InstanceCreatureKits.Hunters[0];

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw($"UPDATE creature SET action4 = 710007 WHERE id = {Casso}");
            database.Context.Database.ExecuteSqlRaw($"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({hunter}, 13, 3782, 7)");

            migrator.Migrate(Migration);
            Assert.AreEqual(710007L, Action4(database));
            CollectionAssert.AreEqual(new long[] { 3782 }, Slot(database, hunter));
            CollectionAssert.AreEqual(new long[] { HunterShields.Shield }, Slot(database, InstanceCreatureKits.Hunters[1]), "the other is given it");

            migrator.Migrate(Previous);
            Assert.AreEqual(710007L, Action4(database));
            CollectionAssert.AreEqual(new long[] { 3782 }, Slot(database, hunter));
        }

        [TestMethod]
        public void OnMySqlItIsTheSameStatements()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in InstanceCreatureKits.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in InstanceCreatureKits.Down())
                StringAssert.Contains(down, sql);
        }

        private static long Action4(OrdinaryLootTestDatabase database) =>
            Query(database, $"SELECT action4 AS Value FROM creature WHERE id = {Casso}").Single();

        private static long[] Slot(OrdinaryLootTestDatabase database, uint id) =>
            Query(database, $"SELECT Class_id AS Value FROM creature_appearance WHERE id = {id} AND slot_id = 13");

        private static long[] Query(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).ToArray();
    }
}
