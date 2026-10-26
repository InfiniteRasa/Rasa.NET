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
    using Rasa.Structures;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// Twin Pillars and its neighbours as the GM placed them (Twin_pillars_npcs, TwinPillarsNpcs),
    /// and the lines Duncan and Moore say once their missions are behind the player
    /// (NpcGreetings.AfterMission).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class TwinPillarsNpcsTests
    {
        private const string Migration = "20270205000000_Twin_pillars_npcs";
        private const string Previous = "20270204000000_Region_floor_ceilings";

        private static readonly string[] Tables =
        {
            "SELECT 'creature ' || id || ' ' || class_id || ' ' || faction || ' ' || level || ' ' || max_hp || ' ' || name_id || ' ' || run_speed || ' ' || walk_speed || ' ' || action1 || ' ' || comment AS Value FROM creature ORDER BY id",
            "SELECT 'look ' || id || ' ' || slot_id || ' ' || Class_id || ' ' || color AS Value FROM creature_appearance ORDER BY id, slot_id",
            "SELECT 'action ' || id || ' ' || action_id || ' ' || action_arg_id || ' ' || min_damage || ' ' || max_damage AS Value FROM creature_action ORDER BY id",
            "SELECT 'stat ' || id || ' ' || body || ' ' || health || ' ' || armor AS Value FROM creature_stat ORDER BY id",
            "SELECT 'pool ' || id || ' ' || mode || ' ' || anim_type || ' ' || respown_time || ' ' || pos_x || ' ' || pos_y || ' ' || pos_z || ' ' || rotation || ' ' || creature_1_Id || ' ' || creature_1_max_count || ' ' || radius AS Value FROM spawnpool ORDER BY id",
            "SELECT 'pose ' || id || ' ' || pose AS Value FROM spawnpool_pose ORDER BY id",
            "SELECT 'patrol ' || pool_id || ' ' || step || ' ' || pos_x || ' ' || pos_z || ' ' || pause_ms AS Value FROM spawnpool_patrol ORDER BY pool_id, step",
            "SELECT 'greeting ' || id || ' ' || greeting_id || ' ' || important AS Value FROM npc_greeting ORDER BY id",
            "SELECT 'indicator ' || mission_id || ' ' || objective_id || ' ' || indicator_id || ' ' || pos_x || ' ' || pos_y || ' ' || pos_z AS Value FROM mission_indicator ORDER BY mission_id, content_revision, objective_id, indicator_id",
            "SELECT 'scene ' || mission_id || ' ' || bindings AS Value FROM mission_scene_binding ORDER BY mission_id, content_revision",
            "SELECT 'destructible ' || id || ' ' || class_id || ' ' || pos_x || ' ' || pos_z || ' ' || side AS Value FROM world_destructible ORDER BY id",
            "SELECT 'usable ' || id || ' ' || class_id || ' ' || usable AS Value FROM world_usable ORDER BY id",
            "SELECT 'teleporter ' || id || ' ' || class_id || ' ' || type || ' ' || pos_y AS Value FROM teleporter ORDER BY id",
            "SELECT 'link ' || from_teleporter_id || ' ' || to_teleporter_id AS Value FROM local_teleporter_link ORDER BY from_teleporter_id",
            "SELECT 'station ' || id || ' ' || rotation AS Value FROM kraftwerks ORDER BY id",
            "SELECT 'loot ' || map_context_id || ' ' || creature_id || ' ' || profile_key AS Value FROM creature_loot_binding ORDER BY map_context_id, creature_id",
        };

        [TestMethod]
        public void TheSetGoesInGuardedAndComesOutExactly()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Snapshot(database);

            migrator.Migrate(Migration);

            // The new NPCs, the squad, Slater, the Thrax and Pierre after her rescue, each in a pool.
            Assert.AreEqual(13, Count(database, $"SELECT CAST(COUNT(*) AS TEXT) AS Value FROM creature WHERE id BETWEEN {TwinPillarsNpcs.FirstCreature} AND {TwinPillarsNpcs.LastCreature}"));
            Assert.AreEqual(16, Count(database, $"SELECT CAST(COUNT(*) AS TEXT) AS Value FROM spawnpool WHERE id BETWEEN {TwinPillarsNpcs.FirstPool} AND {TwinPillarsNpcs.LastPool}"));
            CollectionAssert.AreEqual(
                Strings(database, "SELECT 'look ' || id || ' ' || slot_id || ' ' || Class_id || ' ' || color AS Value FROM creature_appearance WHERE id = 630070 ORDER BY slot_id").Select(r => r.Replace("look 630070 ", "")).ToList(),
                Strings(database, $"SELECT 'look ' || id || ' ' || slot_id || ' ' || Class_id || ' ' || color AS Value FROM creature_appearance WHERE id = {TwinPillarsNpcs.RescuedPierre} ORDER BY slot_id").Select(r => r.Replace($"look {TwinPillarsNpcs.RescuedPierre} ", "")).ToList(),
                "the rescued Pierre looks as the captive one");

            // The drop-offs come by Bane dropship: count, respawn and ground.
            foreach (var d in TwinPillarsNpcs.DropOffs)
                Assert.AreEqual($"0 1 {d.Respawn} {d.Creature} {d.Count} 8.0",
                    Strings(database, $"SELECT mode || ' ' || anim_type || ' ' || respown_time || ' ' || creature_1_Id || ' ' || creature_1_max_count || ' ' || radius AS Value FROM spawnpool WHERE id = {d.Pool}").Single(), d.Comment);

            // Gone: the six specialty trainers and Test Vendor 3; the Class Trainer stays.
            Assert.AreEqual(0, Count(database, "SELECT CAST(COUNT(*) AS TEXT) AS Value FROM spawnpool WHERE id BETWEEN 501007 AND 501012 OR id = 31"));
            Assert.AreEqual(1, Count(database, "SELECT CAST(COUNT(*) AS TEXT) AS Value FROM spawnpool WHERE id = 500002"));

            // Victor walks his beat; Duncan is a woman; Wood is on the swapset body.
            Assert.AreEqual(4, Count(database, "SELECT CAST(COUNT(*) AS TEXT) AS Value FROM spawnpool_patrol WHERE pool_id = 218"));
            Assert.AreEqual("6339 2", Strings(database, "SELECT class_id || ' ' || walk_speed AS Value FROM creature WHERE id = 140").Single());
            Assert.AreEqual("3848", Strings(database, "SELECT CAST(class_id AS TEXT) AS Value FROM creature WHERE id = 125").Single());
            Assert.AreEqual("3846", Strings(database, "SELECT CAST(class_id AS TEXT) AS Value FROM creature WHERE id = 121").Single());

            // The lines, marked where the GM heard them as important.
            CollectionAssert.AreEquivalent(new[] { "121 157 0", "640101 555 1", "640102 469 1", "640110 710 1" },
                Strings(database, "SELECT id || ' ' || greeting_id || ' ' || important AS Value FROM npc_greeting WHERE id IN (121, 640101, 640102, 640110)"));

            // The towers' pads, linked floor to floor both ways.
            CollectionAssert.AreEqual(new[] { "984 985", "985 984", "986 987", "987 986" },
                Strings(database, "SELECT from_teleporter_id || ' ' || to_teleporter_id AS Value FROM local_teleporter_link WHERE from_teleporter_id BETWEEN 984 AND 987 ORDER BY from_teleporter_id"));

            // Quarantine's analyzer is the two-screen workstation; the supplies crate where it was found.
            StringAssert.Contains(Scene(database, 442), "\"templateId\":21087,\"position\":{\"x\":-132.4961");
            StringAssert.Contains(Scene(database, 428), "\"position\":{\"x\":406.3594,\"y\":234.7891,\"z\":114.4219}");
            Assert.AreEqual("3.1416", Strings(database, "SELECT CAST(rotation AS TEXT) AS Value FROM kraftwerks WHERE id = 6").Single());

            // Twice is once.
            var once = Snapshot(database);
            foreach (var sql in TwinPillarsNpcs.Up())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down puts back what it changed");
        }

        [TestMethod]
        public void AMovedOrChangedRowIsLeftAsAGmLeftIt()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw("UPDATE spawnpool SET pos_x = -100 WHERE id = 207");
            database.Context.Database.ExecuteSqlRaw("UPDATE kraftwerks SET rotation = 1.0 WHERE id = 6");
            database.Context.Database.ExecuteSqlRaw("INSERT INTO npc_greeting (id, greeting_id, important) VALUES (121, 435, 0)");

            migrator.Migrate(Migration);

            Assert.AreEqual("-100.0 5.28", Strings(database, "SELECT pos_x || ' ' || rotation AS Value FROM spawnpool WHERE id = 207").Single(), "Taylor where the GM put him");
            Assert.AreEqual("1.0", Strings(database, "SELECT CAST(rotation AS TEXT) AS Value FROM kraftwerks WHERE id = 6").Single());
            Assert.AreEqual("435", Strings(database, "SELECT CAST(greeting_id AS TEXT) AS Value FROM npc_greeting WHERE id = 121").Single(), "Wood's line as the GM gave it");
        }

        [TestMethod]
        public void OnMySqlTheChangesGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in TwinPillarsNpcs.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in TwinPillarsNpcs.Down())
                StringAssert.Contains(down, sql);
        }

        [TestMethod]
        public void TheWorldLoadsWithThemAndTheMissionsStillCheckOut()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness, "the analyzer's class is NPC-capable");

            harness.SpawnWorld(TwinPillarsNpcs.ByrnesPool, TwinPillarsNpcs.SquadPistolPool, TwinPillarsNpcs.RescuedPierrePool, 218);

            var byrnes = harness.Npc(TwinPillarsNpcs.ByrnesPool);
            Assert.IsNotNull(byrnes?.Npc);
            Assert.AreEqual(555u, byrnes.Npc.GreetingId);
            Assert.IsTrue(byrnes.Npc.GreetingImportant);

            var victor = harness.Map.MapCellInfo.Cells.Values.SelectMany(c => c.CreatureList).Distinct().Single(c => c.SpawnPool?.DbId == 218);
            Assert.IsFalse(BehaviorManager.NeverMoves(victor), "Victor walks his beat");
        }

        [TestMethod]
        public void DuncanAndMooreSayTheirLinesOnceTheirMissionsAreBehindThePlayer()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var duncan = context.AddNpc(125);
            var moore = context.AddNpc(131);
            var npcs = new NpcManager(context, context.Manager);
            var player = context.Client.Player;

            // Before: the default, and nothing to be spoken to for.
            Assert.IsNull(NpcGreetings.LineFor(player, duncan));
            Assert.AreEqual(NpcGreetings.Default, NpcGreetings.For(player, moore));
            context.Drain();
            npcs.UpdateConversationStatus(context.Client, duncan);
            Assert.AreEqual(ConversationStatus.None, context.Drain().OfType<NPCConversationStatusPacket>().Single().ConvoStatusId);

            // Duncan once spoken to in Quarantine (its first objective done): her line, marked.
            player.Missions[442] = new MissionLog(442, MissionState.Active, false, new Dictionary<uint, MissionObjectiveLog>
            {
                [1] = new MissionObjectiveLog(1, MissionObjectiveState.Completed, new Dictionary<uint, uint>(), new Dictionary<uint, uint>()),
                [2] = new MissionObjectiveLog(2, MissionObjectiveState.Incomplete, new Dictionary<uint, uint>(), new Dictionary<uint, uint>()),
            });
            Assert.AreEqual(((uint)557, true), NpcGreetings.LineFor(player, duncan).Value);

            npcs.UpdateConversationStatus(context.Client, duncan);
            Assert.AreEqual(ConversationStatus.ImportantGreeting, context.Drain().OfType<NPCConversationStatusPacket>().Single().ConvoStatusId);
            npcs.RequestNpcConverse(context.Client, new RequestNPCConversePacket { EntityId = duncan.EntityId });
            Assert.AreEqual(557, context.Drain().OfType<ConversePacket>().Single().ConvoDataDict[ConversationType.ImportantGreering]);
            Assert.IsTrue(NpcGreetings.HasRead(player, duncan), "read once said");

            // Moore once Smuggler's Blues is done, and not while it is under way.
            player.Missions[623] = new MissionLog(623, MissionState.Active, false);
            Assert.IsNull(NpcGreetings.LineFor(player, moore));
            player.Missions.Remove(623);
            player.MissionSuccessHistory.Add(623);
            Assert.AreEqual(553, NpcGreetings.For(player, moore));

            // Anyone else, and a creature that is no NPC: as before.
            Assert.IsNull(NpcGreetings.LineFor(player, new Creature { DbId = 125 }));
            Assert.AreEqual(NpcGreetings.Default, NpcGreetings.For(null, duncan));
        }

        private static string Scene(OrdinaryLootTestDatabase database, uint mission) =>
            Strings(database, $"SELECT bindings AS Value FROM mission_scene_binding WHERE mission_id = {mission}").Single();

        private static int Count(OrdinaryLootTestDatabase database, string sql) => int.Parse(Strings(database, sql).Single());

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            Tables.SelectMany(sql => Strings(database, sql)).ToList();

        private static List<string> Strings(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<string>(sql).AsEnumerable().ToList();
    }
}
