using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Services.Preloader;
    using Rasa.Structures.World;
    using Rasa.Test.Database;

    /// <summary>
    /// The control points' defences as data (Control_point_defences, ControlPointDefences): the gate
    /// shields linked to their points, the AFS soldiers' roles and posts in their points' garrisons,
    /// Imperial Valley's soldiers, no Rushing Blow; and Down taking out only what Up put in.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ControlPointDefencesTests
    {
        private const string Migration = "20270301000001_Control_point_defences";

        [TestMethod]
        public void EveryPointsGatesHaveTheirShieldsLinkedToIt()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);

            Assert.AreEqual(82L, Count(database, $"SELECT COUNT(*) AS Value FROM world_destructible WHERE id BETWEEN {ControlPointDefences.FirstField} AND {ControlPointDefences.LastField}"));
            Assert.AreEqual(83L, Count(database, $"SELECT COUNT(*) AS Value FROM control_point_link WHERE kind = {ControlPointLinkEntry.KindField}"),
                "and the Landing Zone gate's field upstream had");
            Assert.AreEqual(1L, Count(database, $"SELECT COUNT(*) AS Value FROM control_point_link WHERE kind = {ControlPointLinkEntry.KindField} AND control_point_id = 4 AND object_id = 640016"));

            // Each on its point's map, of a class with its extents, on the side its point starts with.
            Assert.AreEqual(0L, Count(database, "SELECT COUNT(*) AS Value FROM control_point_link l JOIN world_destructible d ON d.id = l.object_id " +
                                                $"JOIN control_point p ON p.id = l.control_point_id WHERE l.kind = {ControlPointLinkEntry.KindField} AND d.map_context_id <> p.map_context_id"));
            Assert.AreEqual(0L, Count(database, "SELECT COUNT(*) AS Value FROM control_point_link l JOIN world_destructible d ON d.id = l.object_id " +
                                                $"JOIN control_point p ON p.id = l.control_point_id WHERE l.kind = {ControlPointLinkEntry.KindField} AND d.id >= {ControlPointDefences.FirstField} " +
                                                $"AND d.side <> (CASE WHEN p.default_owner = 1 THEN {WorldDestructibleEntry.SideAfs} ELSE {WorldDestructibleEntry.SideBane} END)"));
            Assert.AreEqual(0L, Count(database, $"SELECT COUNT(*) AS Value FROM world_destructible WHERE id >= {ControlPointDefences.FirstField} AND class_id NOT IN (20000001, 20000003, 9571, 9572)"));
            Assert.AreEqual(40L, Count(database, $"SELECT COUNT(DISTINCT control_point_id) AS Value FROM control_point_link WHERE kind = {ControlPointLinkEntry.KindField}"),
                "40 of the 41 points: the Iapyx has no gate");
            Assert.AreEqual(0L, Count(database, $"SELECT COUNT(*) AS Value FROM world_destructible WHERE id >= {ControlPointDefences.FirstField} AND hit_points < 6000"));

            foreach (var row in ControlPointDefences.FieldRows)
                Assert.IsNotNull(Rasa.Managers.ForceFields.ClassOf(row[3].ToString()), $"field {row[0]}");
        }

        [TestMethod]
        public void TheSoldiersHaveTheirRolesWeaponsAndStatsAndNoLogos()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);

            var block = $"BETWEEN {ControlPointDefences.FirstRole} AND {ControlPointDefences.LastRole}";

            Assert.AreEqual(120L, Count(database, $"SELECT COUNT(*) AS Value FROM creature WHERE id {block}"));
            Assert.AreEqual(120L, Count(database, $"SELECT COUNT(*) AS Value FROM creature_stat WHERE id {block}"));
            Assert.AreEqual(120L, Count(database, $"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id {block} AND slot_id = 13"));
            Assert.AreEqual(120L, Count(database, $"SELECT COUNT(*) AS Value FROM creature_action WHERE id {block}"));

            // The four soldier bodies the client has, the AFS's side, and none of our own classes.
            Assert.AreEqual(0L, Count(database, $"SELECT COUNT(*) AS Value FROM creature WHERE id {block} AND (class_id NOT IN (29423, 29424, 29432, 29433) OR faction <> 1)"));

            // Every attack named is there, and none is the Rushing Blow (504).
            for (var slot = 1; slot <= 8; slot++)
                Assert.AreEqual(0L, Count(database, $"SELECT COUNT(*) AS Value FROM creature c WHERE c.id {block} AND c.action{slot} <> 0 " +
                                                    $"AND NOT EXISTS (SELECT 1 FROM creature_action a WHERE a.id = c.action{slot})"), $"action{slot}");

            Assert.AreEqual(0L, Count(database, $"SELECT COUNT(*) AS Value FROM creature c JOIN creature_action a ON a.id IN (c.action1, c.action2, c.action3) WHERE c.id {block} AND a.action_id = 504"));

            // The weapons are the client's.
            Assert.AreEqual(0L, Count(database, $"SELECT COUNT(*) AS Value FROM creature_appearance a WHERE a.id {block} AND NOT EXISTS (SELECT 1 FROM entityclass e WHERE e.id = a.Class_id)"));

            // Stats by level: a level 8 rifleman is 24/24/24, 96 armour.
            Assert.AreEqual(24L * 1_000_000 + 600 * 1_000 + 96, Count(database, $"SELECT s.body * 1000000 + s.health * 1000 + s.armor AS Value FROM creature_stat s WHERE s.id = 9700001"));

            // The two soldiers that had the Rushing Blow lose it.
            foreach (var (creature, _) in ControlPointDefences.RushingBlows)
                Assert.AreEqual(0L, Count(database, $"SELECT action2 AS Value FROM creature WHERE id = {creature}"), $"creature {creature}");
        }

        [TestMethod]
        public void EveryPostIsInItsPointsAfsGarrisonOnItsMap()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);

            var pools = $"(id BETWEEN {ControlPointDefences.FirstPool} AND {ControlPointDefences.LastPool} OR id = {ControlPointDefences.ImperialValleyPool})";

            Assert.AreEqual(260L, Count(database, $"SELECT COUNT(*) AS Value FROM spawnpool WHERE {pools}"));
            Assert.AreEqual(260L, Count(database, $"SELECT COUNT(*) AS Value FROM control_point_link l JOIN spawnpool s ON s.id = l.object_id " +
                                                  $"JOIN control_point p ON p.id = l.control_point_id WHERE l.kind = {ControlPointLinkEntry.KindAfsPool} " +
                                                  $"AND s.map_context_id = p.map_context_id AND (s.id BETWEEN {ControlPointDefences.FirstPool} AND {ControlPointDefences.LastPool} OR s.id = {ControlPointDefences.ImperialValleyPool})"));
            Assert.AreEqual(41L, Count(database, $"SELECT COUNT(DISTINCT control_point_id) AS Value FROM control_point_link WHERE kind = {ControlPointLinkEntry.KindAfsPool} AND object_id >= {ControlPointDefences.FirstPool}"));
            Assert.AreEqual(1L, Count(database, $"SELECT COUNT(*) AS Value FROM control_point_link WHERE control_point_id = 3 AND kind = {ControlPointLinkEntry.KindAfsPool} AND object_id = {ControlPointDefences.ImperialValleyPool}"),
                "Imperial Valley's soldiers round its point");

            // Every soldier named is one of the roles.
            for (var slot = 1; slot <= 6; slot++)
                Assert.AreEqual(0L, Count(database, $"SELECT COUNT(*) AS Value FROM spawnpool s WHERE {pools} AND s.creature_{slot}_Id <> 0 " +
                                                    $"AND (s.creature_{slot}_Id NOT BETWEEN {ControlPointDefences.FirstRole} AND {ControlPointDefences.LastRole} " +
                                                    $"OR NOT EXISTS (SELECT 1 FROM creature c WHERE c.id = s.creature_{slot}_Id))"), $"creature_{slot}");

            // The Wilderness AFS posts are given the roles.
            foreach (var (pool, _, _, _, slots) in ControlPointDefences.Recasts)
                Assert.AreEqual((long)slots[0], Count(database, $"SELECT creature_1_Id AS Value FROM spawnpool WHERE id = {pool}"), $"pool {pool}");
        }

        [TestMethod]
        public void DownTakesOutOnlyWhatUpPutIn()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var all = database.Context.Database.GetMigrations().ToList();
            var previous = all[all.IndexOf(Migration) - 1];

            migrator.Migrate(previous);

            var links = Count(database, "SELECT COUNT(*) AS Value FROM control_point_link");
            var spawnpools = Count(database, "SELECT COUNT(*) AS Value FROM spawnpool");
            var creatures = Count(database, "SELECT COUNT(*) AS Value FROM creature");
            var stats = Count(database, "SELECT COUNT(*) AS Value FROM creature_stat");
            var looks = Count(database, "SELECT COUNT(*) AS Value FROM creature_appearance");
            var actions = Count(database, "SELECT COUNT(*) AS Value FROM creature_action");
            var destructibles = Count(database, "SELECT COUNT(*) AS Value FROM world_destructible");
            var pool117 = Count(database, "SELECT creature_1_Id * 10000 + creature_1_min_count * 100 + creature_1_max_count AS Value FROM spawnpool WHERE id = 117");
            var blow39 = Count(database, "SELECT action2 AS Value FROM creature WHERE id = 39");

            migrator.Migrate(Migration);
            migrator.Migrate(previous);

            Assert.AreEqual(links, Count(database, "SELECT COUNT(*) AS Value FROM control_point_link"));
            Assert.AreEqual(spawnpools, Count(database, "SELECT COUNT(*) AS Value FROM spawnpool"));
            Assert.AreEqual(creatures, Count(database, "SELECT COUNT(*) AS Value FROM creature"));
            Assert.AreEqual(stats, Count(database, "SELECT COUNT(*) AS Value FROM creature_stat"));
            Assert.AreEqual(looks, Count(database, "SELECT COUNT(*) AS Value FROM creature_appearance"));
            Assert.AreEqual(actions, Count(database, "SELECT COUNT(*) AS Value FROM creature_action"));
            Assert.AreEqual(destructibles, Count(database, "SELECT COUNT(*) AS Value FROM world_destructible"));
            Assert.AreEqual(pool117, Count(database, "SELECT creature_1_Id * 10000 + creature_1_min_count * 100 + creature_1_max_count AS Value FROM spawnpool WHERE id = 117"));
            Assert.AreEqual(blow39, Count(database, "SELECT action2 AS Value FROM creature WHERE id = 39"));
            Assert.AreEqual(68092L, blow39);
        }

        private static long Count(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).AsEnumerable().Single();
    }
}
