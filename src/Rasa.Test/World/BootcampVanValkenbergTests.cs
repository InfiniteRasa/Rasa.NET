using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Services.Preloader;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;

    /// <summary>Corporal Van Valkenberg's Stealth 6 armour and his line (Bootcamp_van_valkenberg, BootcampVanValkenberg).</summary>
    [TestClass]
    [DoNotParallelize]
    public class BootcampVanValkenbergTests
    {
        private const string Migration = "20270114000000_Bootcamp_van_valkenberg";
        private const string Previous = "20270113000000_Wilderness_reward_consumables";

        [TestMethod]
        public void HeWearsTheStealthSixArmourAndHasTheDropshipLine()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);

            CollectionAssert.AreEqual(
                new[]
                {
                    "1 23540 4278190080", "2 23524 4278190080", "3 23532 4278190080", "13 6271 1",
                    "14 9781 4278655809", "15 23556 4278190080", "16 23548 4278190080", "17 24008 4286690539"
                },
                Look(database));
            Assert.AreEqual("1648 0", Strings(database,
                $"SELECT greeting_id || ' ' || important AS Value FROM npc_greeting WHERE id = {BootcampVanValkenberg.VanValkenberg}").Single());

            // Each piece where the client wears it.
            foreach (var (slot, classId) in new[] { (1u, 23540u), (2u, 23524u), (3u, 23532u), (15u, 23556u), (16u, 23548u) })
                Assert.AreEqual((long)slot, Count(database, $"SELECT slot_id AS Value FROM equipableclass WHERE id = {classId}"), $"class {classId}");

            // The other quest NPCs keep the olive uniform.
            Assert.AreEqual(18L, Count(database,
                $"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id BETWEEN 510203 AND 510208 AND color = {BootcampBaseNpcs.OfficerOlive}"));
        }

        [TestMethod]
        public void TheUpRunsTwiceAndTheDownPutsTheUniformBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Snapshot(database);
            CollectionAssert.AreEqual(
                new[]
                {
                    $"look 13 6271 1", $"look 14 9781 4278655809", $"look 15 4023 {BootcampBaseNpcs.OfficerOlive}",
                    $"look 16 4022 {BootcampBaseNpcs.OfficerOlive}", $"look 17 24008 4286690539", $"look 2 4021 {BootcampBaseNpcs.OfficerOlive}"
                },
                before, "the olive officer's uniform, no greeting");

            migrator.Migrate(Migration);
            var once = Snapshot(database);

            foreach (var sql in BootcampVanValkenberg.Up())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down puts the uniform back");
        }

        [TestMethod]
        public void ALookOrLineChangedSinceIsLeftAlone()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var id = BootcampVanValkenberg.VanValkenberg;

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw($"UPDATE creature_appearance SET color = 4278190335 WHERE id = {id} AND slot_id = 15;");
            database.Context.Database.ExecuteSqlRaw($"INSERT INTO npc_greeting (id, greeting_id, important) VALUES ({id}, 464, 1);");

            migrator.Migrate(Migration);

            Assert.AreEqual("4023 4278190335", Strings(database, $"SELECT Class_id || ' ' || color AS Value FROM creature_appearance WHERE id = {id} AND slot_id = 15").Single());
            Assert.AreEqual("464 1", Strings(database, $"SELECT greeting_id || ' ' || important AS Value FROM npc_greeting WHERE id = {id}").Single());

            migrator.Migrate(Previous);
            Assert.AreEqual("464 1", Strings(database, $"SELECT greeting_id || ' ' || important AS Value FROM npc_greeting WHERE id = {id}").Single(), "a GM's line stays");
        }

        [TestMethod]
        public void OnMySqlTheRowsGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in BootcampVanValkenberg.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in BootcampVanValkenberg.Down())
                StringAssert.Contains(down, sql);
        }

        [TestMethod]
        public void OnceThePlayerHasCheckedInHeSaysTheDropshipLine()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            BootcampCallingForReinforcementsTests.StartCrashSiteScene(harness);
            harness.UseObjectAndRecover(BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-conrad-corpse"));
            harness.UseObjectAndRecover(BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-dropship-debris"));
            harness.UtcNow += TimeSpan.FromSeconds(5);
            harness.Manager.TickScenarios(harness.Client);
            harness.UtcNow += TimeSpan.FromSeconds(2);
            harness.Manager.TickScenarios(harness.Client);

            var van = BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, BootcampRuntimeTestHarness.CorporalVanValkenbergPackageId);
            Assert.IsNotNull(van, "he arrives with the extraction team");
            Assert.AreEqual(BootcampVanValkenberg.StealthHelmet, van.AppearanceData[EquipmentData.Helmet].Class);
            Assert.AreEqual(BootcampVanValkenberg.StealthVest, van.AppearanceData[EquipmentData.Torso].Class);
            Assert.AreEqual(BootcampVanValkenberg.StealthLegs, van.AppearanceData[EquipmentData.Legs].Class);
            Assert.AreEqual(BootcampVanValkenberg.Unhued, van.AppearanceData[EquipmentData.Torso].Color.Hue);

            BootcampExtractionAssaultTests.DefeatAll(harness);
            harness.MovePlayerTo(van);
            harness.Drain();
            var npcs = new NpcManager(harness.Context, harness.Manager);

            // The check-in first: the objective alone, no greeting with it.
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = van.EntityId });
            var checkIn = harness.Drain().OfType<ConversePacket>().Single().ConvoDataDict;
            CollectionAssert.AreEquivalent(new[] { ConversationType.ObjectiveComplete }, checkIn.Keys.ToArray());

            npcs.CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = van.EntityId, MissionId = 1995, ObjectiveId = 4, PlayerFlagId = 1
            });
            Assert.IsTrue(harness.Client.Player.Missions[1995].Completeable);
            harness.Drain();

            // Then his line, and nothing else.
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = van.EntityId });
            var after = harness.Drain().OfType<ConversePacket>().Single().ConvoDataDict;
            CollectionAssert.AreEquivalent(new[] { ConversationType.Greeting }, after.Keys.ToArray());
            Assert.AreEqual((int)BootcampVanValkenberg.Greeting, after[ConversationType.Greeting]);
        }

        private static List<string> Look(OrdinaryLootTestDatabase database) =>
            Strings(database, $"SELECT slot_id || ' ' || Class_id || ' ' || color AS Value FROM creature_appearance WHERE id = {BootcampVanValkenberg.VanValkenberg} ORDER BY slot_id");

        private static long Count(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).AsEnumerable().Single();

        private static List<string> Strings(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<string>(sql).AsEnumerable().ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database)
        {
            var id = BootcampVanValkenberg.VanValkenberg;
            string[] queries =
            {
                $"SELECT 'look ' || slot_id || ' ' || Class_id || ' ' || color AS Value FROM creature_appearance WHERE id = {id}",
                $"SELECT 'greeting ' || greeting_id || ' ' || important AS Value FROM npc_greeting WHERE id = {id}",
            };

            return queries.SelectMany(q => Strings(database, q)).OrderBy(s => s, StringComparer.Ordinal).ToList();
        }
    }
}
