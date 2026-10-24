using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;
using Rasa.Structures.World;

namespace Rasa.Test.Database
{
    // Shahrbaraz, the Fithik Hive Monarch and Hive Master, and the Mox Alpha Male swing their weapon
    // for a boss's damage (Boss_weapons), not the placeholder 10-24 of the rows they shared.
    [TestClass]
    public class BossWeaponsMigrationTests
    {
        private const string Migration = "20261222000000_Boss_weapons";
        private const string Previous = "20261221000000_Family_abilities";

        [TestMethod]
        public void MySqlWidensCreatureActionDescriptionBeforeInsertingBossWeapons()
        {
            const string widen = "20261221050000_WidenCreatureActionDescriptionBeforeBossWeapons";
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrations = context.Database.GetMigrations().ToArray();
            Assert.AreEqual("text", context.Model.FindEntityType(typeof(CreatureActionEntry))?
                .FindProperty(nameof(CreatureActionEntry.Description))?.GetColumnType(),
                "The MySQL runtime EF model must agree with the widened column.");
            var family = Array.IndexOf(migrations, Previous);
            var widening = Array.IndexOf(migrations, widen);
            var boss = Array.IndexOf(migrations, Migration);
            Assert.IsTrue(family >= 0 && widening > family && boss > widening,
                "The MySQL column must widen after Family_abilities but before Boss_weapons.");

            var change = new Rasa.Migrations.MySqlWorld.WidenCreatureActionDescriptionBeforeBossWeapons();
            var operation = change.UpOperations.Single();
            Assert.IsInstanceOfType<AlterColumnOperation>(operation);
            var column = (AlterColumnOperation)operation;
            Assert.AreEqual("creature_action", column.Table);
            Assert.AreEqual("description", column.Name);
            Assert.AreEqual("varchar(50)", column.OldColumn.ColumnType);
            Assert.AreEqual("text", column.ColumnType);
            Assert.IsFalse(column.IsNullable);
            Assert.IsEmpty(change.DownOperations, "A rollback must not silently truncate stored action descriptions.");

            Assert.IsTrue(BossWeapons.Weapons.Any(w => w.Description.Length > 50),
                "The widen protects an actual boss description, not an artificial example.");
        }

        [TestMethod]
        [DataRow(520044u, 77001L, 10L, 190L, 285L, 540007u, DisplayName = "Shahrbaraz over the Palisades Boargar")]
        [DataRow(520038u, 77003L, 34L, 190L, 285L, 540006u, DisplayName = "Hive Master over the Palisades Fithik")]
        [DataRow(520053u, 77004L, 13L, 269L, 404L, 540011u, DisplayName = "Mox Alpha Male over the Pools Mox")]
        public void EachBossSwingsItsWeaponHarderThanItsKin(uint boss, long row, long arg, long min, long max, uint kin)
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate();

            Assert.AreEqual(row, Slot1(database, boss));
            Assert.AreEqual(174L, Column(database, row, "action_id"));
            Assert.AreEqual(arg, Column(database, row, "action_arg_id"));
            Assert.AreEqual(min, Column(database, row, "min_damage"));
            Assert.AreEqual(max, Column(database, row, "max_damage"));

            var kinMax = Query(database, "SELECT a.max_damage AS Value FROM creature c JOIN creature_action a ON a.action_id = 174 AND " +
                $"a.id IN (c.action1, c.action2, c.action3, c.action4, c.action5, c.action6, c.action7, c.action8) WHERE c.id = {kin}").Single();
            Assert.IsTrue(max > kinMax, $"{max} over {kinMax}");
        }

        [TestMethod]
        public void TheMonarchBitesForItsLevel()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate();

            Assert.AreEqual(77002L, Slot1(database, 89));
            Assert.AreEqual(40L, Column(database, 77002, "min_damage"));
            Assert.AreEqual(60L, Column(database, 77002, "max_damage"));
        }

        [TestMethod]
        public void DownPutsTheOldRowsBackAndTheUpIsGuarded()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(new long[] { 4, 44, 44, 22 }, BossWeapons.Weapons.Select(w => Slot1(database, w.Creature)).ToArray());

            // One changed by hand is left.
            database.Context.Database.ExecuteSqlRaw("UPDATE creature SET action1 = 55010 WHERE id = 520044");

            migrator.Migrate(Migration);
            CollectionAssert.AreEqual(new long[] { 55010, 77002, 77003, 77004 }, BossWeapons.Weapons.Select(w => Slot1(database, w.Creature)).ToArray());

            foreach (var sql in BossWeapons.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(4L, Query(database, $"SELECT COUNT(*) AS Value FROM creature_action WHERE id BETWEEN {BossWeapons.IdMin} AND {BossWeapons.IdMax}").Single(), "once");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(new long[] { 55010, 44, 44, 22 }, BossWeapons.Weapons.Select(w => Slot1(database, w.Creature)).ToArray());
            Assert.AreEqual(0L, Query(database, $"SELECT COUNT(*) AS Value FROM creature_action WHERE id BETWEEN {BossWeapons.IdMin} AND {BossWeapons.IdMax}").Single());
            Assert.AreEqual(15L, Column(database, 44, "min_damage"), "the shared rows are as they were");
        }

        private static long Slot1(OrdinaryLootTestDatabase database, uint creature) =>
            Query(database, $"SELECT action1 AS Value FROM creature WHERE id = {creature}").Single();

        private static long Column(OrdinaryLootTestDatabase database, long row, string column) =>
            Query(database, $"SELECT {column} AS Value FROM creature_action WHERE id = {row}").Single();

        private static long[] Query(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).ToArray();
    }
}
