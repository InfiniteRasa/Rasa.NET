using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Services.Preloader;
    using Rasa.Test.Database;

    /// <summary>Alia Das after a second look round (Alia_das_second_look, AliaDasSecondLook).</summary>
    [TestClass]
    [DoNotParallelize]
    public class AliaDasSecondLookTests
    {
        private const string Migration = "20270105000000_Alia_das_second_look";
        private const string Previous = "20270104000000_Alia_das_touchups";

        [TestMethod]
        public void TheyLookAsTheGmAskedAndThePadsAreTheSmallTeleporter()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);

            foreach (var look in AliaDasSecondLook.Looks)
                foreach (var slot in look.Is)
                    Assert.AreEqual($"{slot.ClassId} {slot.Color}",
                        Strings(database, $"SELECT Class_id || ' ' || color AS Value FROM creature_appearance WHERE id = {look.Creature} AND slot_id = {slot.Id}").Single(), $"{look.Who}: slot {slot.Id}");

            // Salter keeps the rest of what he had: eight slots, the rifle, hair and face among them.
            Assert.AreEqual(8, Strings(database, $"SELECT slot_id AS Value FROM creature_appearance WHERE id = {AliaDasNpcs.Salter}").Count);

            foreach (var pad in AliaDasSecondLook.Pads)
                Assert.AreEqual($"{AliaDasSecondLook.HumanTeleporter} 1 {pad.MapContextId} {F(pad.ToX)} {F(pad.Z)}",
                    Strings(database, $"SELECT class_id || ' ' || type || ' ' || map_context_id || ' ' || printf('%.3f %.3f', pos_x, pos_z) AS Value FROM teleporter WHERE id = {pad.Id}").Single());
        }

        [TestMethod]
        public void TheAliaDasPadClearsTheWallAndStaysInsideTheLink()
        {
            // arch_hum_teleporter.geo: x -1.34 to 1.34, z -1.32 to 1.36. The room's floor ends at x 748.5.
            var pad = AliaDasSecondLook.Pads.Single(p => p.Id == 990);
            Assert.IsTrue(pad.ToX - 1.34 >= 748.5, "clear of the wall");

            var link = AliaDasCellarLink.Link;
            Assert.AreEqual(link.X, pad.FromX, 1e-4);
            Assert.IsTrue(Math.Abs(pad.ToX - link.X) + 0.01 < link.Radius, "its middle inside the link's circle");

            // The arena end is not moved.
            var arena = AliaDasSecondLook.Pads.Single(p => p.Id == 991);
            Assert.AreEqual(arena.FromX, arena.ToX);
        }

        [TestMethod]
        public void TheUpLeavesWhatAGmChangedAndTheDownPutsBackWhatItChanged()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);

            // A GM recoloured Elise's hair and gave Spaulding a helmet by hand before the migration ran.
            database.Context.Database.ExecuteSqlRaw($"UPDATE creature_appearance SET color = 123 WHERE id = {AliaDasTouchups.Elise} AND slot_id = 14");
            database.Context.Database.ExecuteSqlRaw($"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({AliaDasNpcs.Spaulding}, 1, 4006, 5)");
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            Assert.AreEqual("9782 123", Strings(database, $"SELECT Class_id || ' ' || color AS Value FROM creature_appearance WHERE id = {AliaDasTouchups.Elise} AND slot_id = 14").Single(), "the GM's hair kept");
            Assert.AreEqual("4006 5", Strings(database, $"SELECT Class_id || ' ' || color AS Value FROM creature_appearance WHERE id = {AliaDasNpcs.Spaulding} AND slot_id = 1").Single(), "the GM's helmet kept");
            Assert.AreEqual((long)AliaDasSecondLook.Light11Torso, Count(database, $"SELECT Class_id AS Value FROM creature_appearance WHERE id = {AliaDasNpcs.Salter} AND slot_id = 15"));

            var once = Snapshot(database);
            foreach (var sql in AliaDasSecondLook.Up())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down puts back what it changed");
        }

        [TestMethod]
        public void OnMySqlTheChangesGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in AliaDasSecondLook.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in AliaDasSecondLook.Down())
                StringAssert.Contains(down, sql);
        }

        private static string F(double value) => value.ToString("F3", CultureInfo.InvariantCulture);

        private static long Count(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).AsEnumerable().Single();

        private static List<string> Strings(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<string>(sql).AsEnumerable().ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database)
        {
            var creatures = string.Join(", ", AliaDasSecondLook.Looks.Select(l => l.Creature));
            string[] queries =
            {
                $"SELECT 'look ' || id || ' ' || slot_id || ' ' || Class_id || ' ' || color AS Value FROM creature_appearance WHERE id IN ({creatures})",
                "SELECT 'teleporter ' || id || ' ' || class_id || ' ' || type || ' ' || map_context_id || ' ' || printf('%.3f %.3f %.3f', pos_x, pos_y, pos_z) AS Value FROM teleporter",
            };

            return queries.SelectMany(sql => database.Context.Database.SqlQueryRaw<string>(sql).AsEnumerable()).OrderBy(s => s, StringComparer.Ordinal).ToList();
        }
    }
}
