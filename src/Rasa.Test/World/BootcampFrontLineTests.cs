using System;
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
    using Rasa.Services.Preloader.Missions;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;

    /// <summary>The Rifleman and the Forean Shaman Initiate at the Proving Grounds' front (Bootcamp_front_line, BootcampFrontLine).</summary>
    [TestClass]
    [DoNotParallelize]
    public class BootcampFrontLineTests
    {
        private const string Migration = "20270109000000_Bootcamp_front_line";
        private const string Previous = "20270108000000_Meadow_boargar";

        [TestMethod]
        public void TheyStandWhereTheGmPutThemWithTheirWeaponsAndAttacks()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);

            foreach (var n in BootcampFrontLine.Npcs)
            {
                Assert.AreEqual($"{n.ClassId} 1 {n.Level} {n.MaxHp} {n.NameId} 0 0 {n.Action}",
                    Strings(database, $"SELECT class_id || ' ' || faction || ' ' || level || ' ' || max_hp || ' ' || name_id || ' ' || run_speed || ' ' || walk_speed || ' ' || action1 AS Value FROM creature WHERE id = {n.Id}").Single(), n.Comment);
                Assert.AreEqual($"13 {n.Weapon} 1", Strings(database, $"SELECT slot_id || ' ' || Class_id || ' ' || color AS Value FROM creature_appearance WHERE id = {n.Id}").Single());
                Assert.AreEqual($"{n.Body} {n.Mind} {n.Spirit} {n.MaxHp} {n.Armor}",
                    Strings(database, $"SELECT body || ' ' || mind || ' ' || spirit || ' ' || health || ' ' || armor AS Value FROM creature_stat WHERE id = {n.Id}").Single());
            }

            foreach (var p in BootcampFrontLine.Posts)
            {
                Assert.AreEqual($"{p.Creature} {BootcampFrontLine.Respawn} {F(p.X)} {F(p.Y)} {F(p.Z)} {F(p.Rotation)} 1985",
                    Strings(database, $"SELECT creature_1_Id || ' ' || respown_time || ' ' || printf('%.4f %.4f %.4f %.4f', pos_x, pos_y, pos_z, rotation) || ' ' || map_context_id AS Value FROM spawnpool WHERE id = {p.Pool}").Single(), p.Who);
                Assert.AreEqual(p.Pose.ToString(), Strings(database, $"SELECT pose AS Value FROM spawnpool_pose WHERE id = {p.Pool}").Single());
            }

            // Their attacks are there: the bridge soldiers' rifle and the Capture the Flag shaman's staff.
            Assert.AreEqual("1 134", Strings(database, $"SELECT action_id || ' ' || action_arg_id AS Value FROM creature_action WHERE id = {BootcampFrontLine.RifleAttack}").Single());
            Assert.AreEqual("1 146", Strings(database, $"SELECT action_id || ' ' || action_arg_id AS Value FROM creature_action WHERE id = {BootcampFrontLine.StaffAttack}").Single());
        }

        [TestMethod]
        public void TheyAreTheNearestToTheRockfallAndBesideTheThraxWayToTheBridge()
        {
            var rocks = new Vector2(BootcampCaveInBreachV9.CaveInPosition.X, BootcampCaveInBreachV9.CaveInPosition.Z);
            var sandbags = new[] { new Vector2(314.9492f, 59.4219f), new Vector2(316.2148f, 60.2383f) };

            foreach (var p in BootcampFrontLine.Posts)
            {
                var at = new Vector2((float)p.X, (float)p.Z);
                Assert.IsTrue(sandbags.All(post => Vector2.Distance(rocks, at) < Vector2.Distance(rocks, post) - 10), $"{p.Who} ahead of the sandbag post");
            }

            // The Rifleman is 5 m from where the charge comes out of the doorwell.
            var charge = new Vector2(BootcampCaveInBreachV9.Charge[0].X, BootcampCaveInBreachV9.Charge[0].Z);
            var rifleman = BootcampFrontLine.Posts.Single(p => p.Creature == BootcampFrontLine.RiflemanId);
            Assert.IsTrue(Vector2.Distance(charge, new Vector2((float)rifleman.X, (float)rifleman.Z)) < 5.5f);
        }

        [TestMethod]
        public void TheProvingGroundsPutsThemOnTheirPosts()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            harness.Client.Player.GmFlagAlwaysFriendly = true;
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);

            Rasa.Structures.Creature Of(uint poolId) => harness.BootcampMap.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList).Distinct()
                .Single(creature => creature.SpawnPool?.DbId == poolId);

            var rifleman = Of(BootcampFrontLine.RiflemanPoolId);
            var shaman = Of(BootcampFrontLine.ShamanPoolId);

            Assert.AreEqual(BootcampFrontLine.RiflemanName, rifleman.NameId);
            Assert.AreEqual(NpcPose.WeaponOut, rifleman.Pose);
            Assert.AreEqual(BootcampFrontLine.Rifle, rifleman.AppearanceData[EquipmentData.Weapon].Class);

            Assert.AreEqual(BootcampFrontLine.ShamanInitiateName, shaman.NameId);
            Assert.AreEqual(NpcPose.Standing, shaman.Pose);
            Assert.AreEqual(BootcampFrontLine.Staff, shaman.AppearanceData[EquipmentData.Weapon].Class);

            foreach (var npc in new[] { rifleman, shaman })
            {
                Assert.IsTrue(BehaviorManager.NeverMoves(npc));
                Assert.AreEqual(npc.SpawnPool.Position, npc.Position);
                Assert.AreEqual(TargetCategory.Friendly, npc.TargetCategory);
            }
        }

        [TestMethod]
        public void TheUpLeavesTakenIdsAloneAndTheDownTakesOutOnlyItsOwn()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            var once = Snapshot(database);
            foreach (var sql in BootcampFrontLine.Up())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down takes them out");

            // A GM's own NPC under the Rifleman's id: left alone, and no pool goes in for it.
            database.Context.Database.ExecuteSqlRaw($"INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8) " +
                $"VALUES ({BootcampFrontLine.RiflemanId}, 'a GM''s', 29423, 1, 1, 100, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)");
            migrator.Migrate(Migration);
            Assert.AreEqual("a GM's", Strings(database, $"SELECT comment AS Value FROM creature WHERE id = {BootcampFrontLine.RiflemanId}").Single());
            Assert.AreEqual(0, Strings(database, $"SELECT slot_id AS Value FROM creature_appearance WHERE id = {BootcampFrontLine.RiflemanId}").Count);
            migrator.Migrate(Previous);
            Assert.AreEqual("a GM's", Strings(database, $"SELECT comment AS Value FROM creature WHERE id = {BootcampFrontLine.RiflemanId}").Single(), "Down leaves it");
        }

        [TestMethod]
        public void OnMySqlTheRowsGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in BootcampFrontLine.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in BootcampFrontLine.Down())
                StringAssert.Contains(down, sql);
        }

        private static string F(double value) => value.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);

        private static List<string> Strings(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<string>(sql).AsEnumerable().ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database)
        {
            var ids = string.Join(", ", BootcampFrontLine.Npcs.Select(n => n.Id).Concat(BootcampFrontLine.Posts.Select(p => p.Pool)).Distinct());
            string[] queries =
            {
                $"SELECT 'creature ' || id || ' ' || comment AS Value FROM creature WHERE id IN ({ids})",
                $"SELECT 'look ' || id || ' ' || slot_id || ' ' || Class_id AS Value FROM creature_appearance WHERE id IN ({ids})",
                $"SELECT 'stat ' || id || ' ' || health AS Value FROM creature_stat WHERE id IN ({ids})",
                $"SELECT 'pool ' || id || ' ' || creature_1_Id AS Value FROM spawnpool WHERE id IN ({ids})",
                $"SELECT 'pose ' || id || ' ' || pose AS Value FROM spawnpool_pose WHERE id IN ({ids})",
            };
            return queries.SelectMany(sql => Strings(database, sql)).OrderBy(s => s, StringComparer.Ordinal).ToList();
        }
    }
}
