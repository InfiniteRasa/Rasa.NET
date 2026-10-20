using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;

    /// <summary>Medic Schramm, a medical vendor at the Proving Grounds' fort (Bootcamp_fort_medic, BootcampFortMedic).</summary>
    [TestClass]
    [DoNotParallelize]
    public class BootcampFortMedicTests
    {
        private const string Migration = "20270109000000_Bootcamp_fort_medic";
        private const string Previous = "20270108000000_Meadow_boargar";

        [TestMethod]
        public void SheGoesInWithTheTealUniformAndTheHospitalStock()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);

            Assert.AreEqual("Medic Schramm - Proving Grounds fort 20972 1 5 555 10577 0 0 2",
                Strings(database, $"SELECT comment || ' ' || class_id || ' ' || faction || ' ' || level || ' ' || max_hp || ' ' || name_id || ' ' || " +
                    $"run_speed || ' ' || walk_speed || ' ' || action1 AS Value FROM creature WHERE id = {BootcampFortMedic.Schramm}").Single());

            CollectionAssert.AreEqual(
                new[] { "2 4021 4287138862", "13 6271 1", "14 9782 4280433242", "15 4023 4287138862", "16 4022 4287138862", "17 24009 4286690539" },
                Strings(database, $"SELECT slot_id || ' ' || Class_id || ' ' || color AS Value FROM creature_appearance WHERE id = {BootcampFortMedic.Schramm} ORDER BY slot_id"));

            // The same teal as the Wilderness medics wear.
            Assert.AreEqual(WildernessPostNpcs.Teal, 4287138862u);

            Assert.AreEqual($"{BootcampFortMedic.Schramm} 1985 112.531 109.434 111.273 5.392 20",
                Strings(database, "SELECT creature_1_Id || ' ' || map_context_id || ' ' || printf('%.3f %.3f %.3f %.3f', pos_x, pos_y, pos_z, rotation) || ' ' || respown_time AS Value " +
                    $"FROM spawnpool WHERE id = {BootcampFortMedic.SchrammPool}").Single());

            Assert.AreEqual(87L, Count(database, $"SELECT package_id AS Value FROM vendor WHERE id = {BootcampFortMedic.Schramm}"));

            var stock = Longs(database, $"SELECT item_template_id AS Value FROM vendor_item WHERE id = {BootcampFortMedic.Schramm}");
            Assert.IsNotEmpty(stock);
            CollectionAssert.AreEquivalent(Longs(database, $"SELECT item_template_id AS Value FROM vendor_item WHERE id = {BootcampFortMedic.Solomon}"), stock,
                "Medical Supply Sgt. Solomon's stock");
        }

        [TestMethod]
        public void SheStandsAtTheFortInTheTealUniform()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            harness.Client.Player.GmFlagAlwaysFriendly = true;
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);

            var schramm = harness.BootcampMap.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList).Distinct()
                .Single(creature => creature.SpawnPool?.DbId == BootcampFortMedic.SchrammPool);

            Assert.AreEqual(BootcampFortMedic.SchrammName, schramm.NameId);
            Assert.AreEqual(new Vector3(112.5312f, 109.4336f, 111.2734f), schramm.Position);
            Assert.AreEqual(TargetCategory.Friendly, schramm.TargetCategory);
            Assert.IsTrue(BehaviorManager.NeverMoves(schramm));

            Assert.IsNotNull(schramm.Npc, "an NPC");

            Assert.AreEqual(WildernessPostNpcs.Teal, schramm.AppearanceData[EquipmentData.Torso].Color.Hue);
            Assert.AreEqual(WildernessPostNpcs.Teal, schramm.AppearanceData[EquipmentData.Legs].Color.Hue);
            Assert.AreEqual(WildernessPostNpcs.BrownHair, schramm.AppearanceData[EquipmentData.Hair].Color.Hue);
            Assert.AreEqual(24009u, schramm.AppearanceData[EquipmentData.Face].Class);

            // Safe ground, and no camp comes within reach of it.
            var found = SpawnPoolManager.Instance.PoolsTooClose(new List<(uint, Vector3, string)>())
                .Where(f => f.Map == BootcampFortMedic.BootcampMapContextId).Select(f => f.Message).ToList();
            Assert.IsFalse(found.Any(m => m.Contains($"pool {BootcampFortMedic.SchrammPool},") || m.EndsWith($"pool {BootcampFortMedic.SchrammPool}, which should be safe ground.")),
                string.Join("\n", found));
        }

        [TestMethod]
        public void TheServerLoadsHerAsAMedicalVendorWithTheHospitalStock()
        {
            // The boot camp harness makes its creatures itself, with no counters; the server's own load is this one.
            using var harness = Rasa.Test.Missions.Wilderness.WildernessRuntimeTestHarness.Create();
            var loaded = harness.Creatures.LoadedCreatures;
            var schramm = loaded[BootcampFortMedic.Schramm];

            Assert.IsNotNull(schramm.Npc?.Vendor, "a vendor");
            Assert.AreEqual(BootcampFortMedic.MedicalPackage, schramm.Npc.Vendor.VendorPackageId);
            Assert.IsNotEmpty(schramm.Npc.Vendor.VendorItems);
            CollectionAssert.AreEquivalent(loaded[BootcampFortMedic.Solomon].Npc.Vendor.VendorItems, schramm.Npc.Vendor.VendorItems);
            Assert.AreEqual(WildernessPostNpcs.Teal, schramm.AppearanceData[EquipmentData.Torso].Color.Hue);
        }

        [TestMethod]
        public void TheUpRunsTwiceAndTheDownTakesHerOut()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Snapshot(database);
            Assert.IsEmpty(before);

            migrator.Migrate(Migration);
            var once = Snapshot(database);
            Assert.IsNotEmpty(once);

            foreach (var sql in BootcampFortMedic.Up())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down takes her out");
            Assert.AreEqual(10L, Count(database, $"SELECT COUNT(*) AS Value FROM vendor_item WHERE id = {BootcampFortMedic.Solomon}"), "Solomon's stock left");
        }

        [TestMethod]
        public void OnMySqlTheRowsGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in BootcampFortMedic.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in BootcampFortMedic.Down())
                StringAssert.Contains(down, sql);
        }

        private static long Count(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).AsEnumerable().Single();

        private static List<long> Longs(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).AsEnumerable().ToList();

        private static List<string> Strings(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<string>(sql).AsEnumerable().ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database)
        {
            var id = BootcampFortMedic.Schramm;
            string[] queries =
            {
                $"SELECT 'creature ' || id || ' ' || class_id || ' ' || name_id AS Value FROM creature WHERE id = {id}",
                $"SELECT 'look ' || slot_id || ' ' || Class_id || ' ' || color AS Value FROM creature_appearance WHERE id = {id}",
                $"SELECT 'pool ' || id || ' ' || creature_1_Id AS Value FROM spawnpool WHERE id = {BootcampFortMedic.SchrammPool}",
                $"SELECT 'vendor ' || package_id AS Value FROM vendor WHERE id = {id}",
                $"SELECT 'stock ' || item_template_id AS Value FROM vendor_item WHERE id = {id}",
            };

            return queries.SelectMany(q => Strings(database, q)).OrderBy(s => s, System.StringComparer.Ordinal).ToList();
        }
    }
}
