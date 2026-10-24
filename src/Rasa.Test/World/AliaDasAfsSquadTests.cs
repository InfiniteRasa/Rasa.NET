using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;
using Rasa.Test.Database;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.World
{
    using A = Rasa.Services.Preloader.AliaDasAfsSquad;
    using S = Rasa.Managers.NpcSquads;

    /// <summary>
    /// Alia Das's AFS squad (AliaDasAfsSquad; NpcSquads): its rows and the columns that came with
    /// them, and the squad in the Wilderness - teleported in on the waypoint pad one after another,
    /// run off it, walked down the road to the valley, holding there to fight at 45 m, and sent
    /// again two minutes after it is wiped out.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AliaDasAfsSquadTests
    {
        private const string Migration = "20270903000000_Alia_das_afs_squad";
        private const string Previous = "20270902000000_Npc_squads";

        private static readonly string[] Tables =
        {
            "creature", "creature_appearance", "creature_stat",
            "npc_squad_type", "npc_squad_member", "npc_squad_path", "npc_squad_path_point", "npc_squad_spawn"
        };

        private static readonly Vector3 Pad = new((float)A.SpawnX, (float)A.SpawnY, (float)A.SpawnZ);

        private long _now = 1_000_000;

        [TestInitialize]
        public void Clock()
        {
            S.Load(null, null, null, null, null);
            S.Now = () => _now;
            KaelRushingBlow.Now = () => _now;
        }

        [TestCleanup]
        public void Restore()
        {
            S.Load(null, null, null, null, null);
            S.Now = () => Environment.TickCount64;
            KaelRushingBlow.Now = () => Environment.TickCount64;
        }

        #region The rows

        [TestMethod]
        public void TheSquadGoesInAndDownTakesItOutExactly()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Tables.ToDictionary(table => table, table => Dump(database, table));

            migrator.Migrate(Migration);

            CollectionAssert.AreEqual(new[]
            {
                $"{A.Leader}|29423|1|5|350|8716|7|2|510218",
                $"{A.MachineGunner}|29433|1|5|350|8719|7|2|510219",
                $"{A.Rifleman}|29423|1|5|350|8717|7|2|72003"
            }, Rows(database, "SELECT id, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, action1 FROM creature WHERE id BETWEEN 670001 AND 670999 ORDER BY id"));
            CollectionAssert.AreEqual(new[] { "670001|13|6271", "670002|13|20535", "670003|13|27220" },
                Rows(database, "SELECT id, slot_id, Class_id FROM creature_appearance WHERE id BETWEEN 670001 AND 670999 ORDER BY id"));
            Assert.AreEqual(3, Count(database, "SELECT COUNT(*) FROM creature_stat WHERE id BETWEEN 670001 AND 670003 AND health = 350 AND body = 12 AND armor = 60"));

            CollectionAssert.AreEqual(new[] { "670001|Alia Das AFS squad|0" }, Rows(database, "SELECT id, name, formation FROM npc_squad_type"));
            CollectionAssert.AreEqual(new[] { "0|670001|1|1", "1|670002|1|1", "2|670003|3|3" },
                Rows(database, "SELECT slot, creature_id, min_count, max_count FROM npc_squad_member WHERE type_id = 670001 ORDER BY slot"), "five: a leader, a gunner and three riflemen");
            CollectionAssert.AreEqual(new[] { "670001|1220|2" }, Rows(database, "SELECT id, map_context_id, mode FROM npc_squad_path"));
            CollectionAssert.AreEqual(new[] { "0|1|0", "1|1|0", "2|0|0", "3|0|0", "4|0|0", "5|0|0", "6|0|45" },
                Rows(database, "SELECT step, run, engage_range FROM npc_squad_path_point WHERE path_id = 670001 ORDER BY step"), "run off the pad, walk the road, fight in the valley");
            CollectionAssert.AreEqual(new[] { "670001|670001|670001|120|1|1" },
                Rows(database, "SELECT id, type_id, path_id, respawn_seconds, enabled, arrival FROM npc_squad_spawn"));

            foreach (var sql in A.Up())
                Assert.AreEqual(0, database.Context.Database.ExecuteSqlRaw(sql), sql);

            migrator.Migrate(Previous);

            foreach (var table in Tables)
                CollectionAssert.AreEqual(before[table], Dump(database, table), $"Down puts {table} back as it was");

            Assert.AreEqual(0, Count(database, "SELECT COUNT(*) FROM pragma_table_info('npc_squad_path_point') WHERE name IN ('run', 'engage_range')"));
            Assert.AreEqual(0, Count(database, "SELECT COUNT(*) FROM pragma_table_info('npc_squad_spawn') WHERE name = 'arrival'"));
        }

        [TestMethod]
        public void OnMySqlTheSquadGoesInAndComesOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            StringAssert.Contains(up, "ALTER TABLE `npc_squad_path_point` ADD `run` tinyint unsigned NOT NULL DEFAULT 0;");
            StringAssert.Contains(up, "ALTER TABLE `npc_squad_path_point` ADD `engage_range` double NOT NULL DEFAULT 0.0;");
            StringAssert.Contains(up, "ALTER TABLE `npc_squad_spawn` ADD `arrival` tinyint unsigned NOT NULL DEFAULT 0;");
            foreach (var sql in A.Up())
                StringAssert.Contains(up, sql);
            foreach (var sql in A.Down())
                StringAssert.Contains(down, sql);
            StringAssert.Contains(down, "ALTER TABLE `npc_squad_spawn` DROP COLUMN `arrival`;");
        }

        [TestMethod]
        public void TheRouteGoesOverTheNavmeshFromThePad()
        {
            using var harness = Create();
            var nav = harness.Map.NavMesh;
            var from = nav.Nearest(Pad) ?? throw new AssertFailedException("the pad is off the navmesh");

            Assert.IsTrue(Vector3.Distance(from, Pad) < 0.5f);

            foreach (var point in A.Points)
            {
                var to = nav.Nearest(new Vector3((float)point.X, (float)point.Y, (float)point.Z)) ?? throw new AssertFailedException($"{point.Where} is off the navmesh");
                nav.FindPath(from, to, out var complete);
                Assert.IsTrue(complete, $"a way to {point.Where}");
                from = to;
            }
        }

        #endregion

        #region In the world

        [TestMethod]
        public void TheyTeleportInOnThePadOneAfterAnotherAndRunOffIt()
        {
            using var harness = Create();
            harness.MoveTo(Pad + new Vector3(6, 0, 6));
            harness.Drain();

            Step(harness, 250);
            var squad = S.On(harness.Map)[A.Squad];

            Assert.AreEqual(S.Phase.Arriving, squad.Phase);
            Assert.AreEqual(1, squad.Members.Count, "the leader first");
            Assert.AreEqual(A.Leader, squad.Members[0].Creature.DbId);
            Assert.IsTrue(Vector3.Distance(squad.Members[0].Creature.Position, Pad) < 0.6f, "on the pad");

            var arrivals = harness.Drain().OfType<TeleportArrivalPacket>().Count();

            for (var i = 0; i < 16; i++)
            {
                Step(harness, 250);
                arrivals += harness.Drain().OfType<TeleportArrivalPacket>().Count();
            }

            Assert.AreEqual(5, squad.Members.Count);
            Assert.AreEqual(5, arrivals, "each of them seen arriving");
            CollectionAssert.AreEqual(new[] { A.Leader, A.MachineGunner, A.Rifleman, A.Rifleman, A.Rifleman },
                squad.Members.OrderBy(m => m.Place).Select(m => m.Creature.DbId).ToArray());
            Assert.AreEqual(S.Phase.Moving, squad.Phase);
            Assert.AreEqual(0, squad.PathIndex);

            var walk = squad.Members.Min(m => m.Creature.WalkSpeed);
            var run = squad.Members.Min(m => m.Creature.RunSpeed);
            Assert.IsTrue(run > walk);

            // They run the first two legs, and walk from there.
            Step(harness, 250);
            Assert.AreEqual(run, squad.Leader.Speed, 0.01, "off the pad at a run");

            for (var i = 0; i < 4 * 30 && squad.PathIndex < 2; i++)
                Step(harness, 250);

            Assert.AreEqual(2, squad.PathIndex, "both run legs done");
            Step(harness, 250);
            Assert.AreEqual(walk, squad.Leader.Speed, 0.01, "up the road at a walk");
        }

        [TestMethod]
        public void TheyWalkDownToTheValleyAndFightWhatTheyFindThere()
        {
            using var harness = Create();
            Step(harness, 250);
            var squad = S.On(harness.Map)[A.Squad];
            var end = A.Points[^1];
            var valley = new Vector3((float)end.X, (float)end.Y, (float)end.Z);
            var reached = new List<int>();

            for (var i = 0; i < 4 * 900 && squad.Phase != S.Phase.Holding; i++)
            {
                Step(harness, 250);
                if (squad.PathIndex >= 0 && (reached.Count == 0 || reached[^1] != squad.PathIndex))
                    reached.Add(squad.PathIndex);
            }

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6 }, reached.ToArray(), "every point in turn");
            Assert.AreEqual(S.Phase.Holding, squad.Phase, "at the end of the route, holding");
            Assert.AreEqual(5, squad.Living.Count());
            Assert.IsTrue(Vector2.Distance(new(squad.Leader.Creature.Position.X, squad.Leader.Creature.Position.Z), new(valley.X, valley.Z)) < S.PointReached + 0.5f);
            Assert.IsTrue(squad.Members.All(m => Vector3.Distance(m.Creature.Position, valley) < 15), "all of them there");
            Assert.AreEqual(45f, S.EngageRange(squad.Leader.Creature), "looking 45 m out");

            Step(harness, 10_000);
            Assert.AreEqual(S.Phase.Holding, squad.Phase, "and they stay");

            // A Thrax 40 m off across the valley: they go for it.
            var thrax = harness.Creatures.CreateCreature(3, null);
            var at = harness.Map.NavMesh.Nearest(valley + Vector3.Normalize(new Vector3(343.4f - valley.X, 0, 164f - valley.Z)) * 40) ?? valley;
            CreatureManager.Instance.SetLocation(thrax, at, 0, A.Map);
            thrax.AggroRange = 0.5f;
            CellManager.Instance.AddToWorld(harness.Map, thrax);

            for (var i = 0; i < 4 * 6 && squad.Phase != S.Phase.Fighting; i++)
                Step(harness, 250);

            Assert.AreEqual(S.Phase.Fighting, squad.Phase);
            Step(harness, 250);
            Assert.IsTrue(squad.Living.All(m => m.Creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting &&
                m.Creature.Controller.ActionFighting.TargetEntityId == thrax.EntityId), "all of them on it");
        }

        [TestMethod]
        public void WipedOutTheyComeBackOnThePadTwoMinutesLater()
        {
            using var harness = Create();
            Step(harness, 250);
            var squad = S.On(harness.Map)[A.Squad];

            for (var i = 0; i < 4 * 30; i++)
                Step(harness, 250);

            var first = squad.Members.Select(m => m.Creature).ToList();
            Assert.AreEqual(5, first.Count);

            foreach (var creature in first)
            {
                creature.Attributes[Attributes.Health].Current = 0;
                creature.State = CharacterState.Dead;
            }

            Step(harness, 250);
            Assert.AreEqual(S.Phase.Gone, squad.Phase);

            Step(harness, 119_000);
            Assert.AreEqual(S.Phase.Gone, squad.Phase, "not yet");

            Step(harness, 1_500);
            Assert.AreEqual(S.Phase.Arriving, squad.Phase, "back");
            Assert.IsTrue(Vector3.Distance(squad.Leader.Creature.Position, Pad) < 0.6f, "on the pad");
            Assert.IsTrue(squad.Members.All(m => !first.Contains(m.Creature)));

            Step(harness, 3_000);
            Assert.AreEqual(5, squad.Living.Count());
            Assert.IsTrue(squad.PathIndex is 0 or 1, $"the route from its start: {squad.PathIndex}");
        }

        [TestMethod]
        public void AGmSetsHowAPointIsWalkedAndHowASpawnArrives()
        {
            using var harness = Create();

            harness.MoveTo(Pad);
            Say(harness, $".squad path {A.Squad} set 2 run engage 30 4");
            var point = S.Paths[A.Squad].Points[2];
            Assert.AreEqual((true, 30f, 4000u), (point.Run, point.EngageRange, point.PauseMs));

            Say(harness, $".squad path {A.Squad} insert 3 walk");
            Assert.AreEqual(8, S.Paths[A.Squad].Points.Count);
            Assert.IsTrue(Vector3.Distance(S.Paths[A.Squad].Points[3].Position, harness.Client.Player.Position) < 0.01f);
            Say(harness, $".squad path {A.Squad} remove 3");

            Say(harness, $".squad spawn {A.Squad} arrival none");
            Assert.AreEqual(S.Arrival.None, S.Spawns[A.Squad].Arrival);

            harness.World.ChangeTracker.Clear();
            var saved = harness.World.NpcSquadPathPointEntries.AsNoTracking().Single(p => p.PathId == A.Squad && p.Step == 2);
            Assert.AreEqual(((byte)1, 30.0, 4000u), (saved.Run, saved.EngageRange, saved.PauseMs));
            Assert.AreEqual(7, harness.World.NpcSquadPathPointEntries.AsNoTracking().Count(p => p.PathId == A.Squad));
            Assert.AreEqual((byte)0, harness.World.NpcSquadSpawnEntries.AsNoTracking().Single(s => s.Id == A.Squad).Arrival);

            var sql = S.ExportSql(A.Map);
            StringAssert.Contains(sql, ", 4000, 1, 30);");
            StringAssert.Contains(sql, "'Alia Das AFS squad', 0);");

            // Sent with no arrival: all five at once, in their places.
            Step(harness, 250);
            Assert.AreEqual(5, S.On(harness.Map)[A.Squad].Members.Count);
        }

        #endregion

        #region Helpers

        private WildernessRuntimeTestHarness Create()
        {
            var harness = WildernessRuntimeTestHarness.Create();
            try
            {
                S.Init(harness);
                Assert.IsTrue(S.Spawns.ContainsKey(A.Squad), "the squad is in the world database");
                harness.MoveTo(Pad + new Vector3(0, 0, 300));
                harness.Drain();
                return harness;
            }
            catch
            {
                harness.Dispose();
                throw;
            }
        }

        private static List<string> Say(WildernessRuntimeTestHarness harness, string command) =>
            S.Command(harness.Map, harness.Client.Player, command.Split(' '));

        private void Step(WildernessRuntimeTestHarness harness, long milliseconds)
        {
            for (var left = milliseconds; left > 0; left -= 250)
            {
                var step = Math.Min(250, left);
                _now += step;
                KaelRushingBlow.Worker(harness.Map);
                S.Worker(harness.Map);
                harness.Tick(step);
            }
        }

        private static List<string> Rows(OrdinaryLootTestDatabase database, string sql)
        {
            using var command = database.Context.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            database.Context.Database.OpenConnection();
            using var reader = command.ExecuteReader();
            var rows = new List<string>();

            while (reader.Read())
                rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => Convert.ToString(reader.GetValue(i), System.Globalization.CultureInfo.InvariantCulture))));

            return rows;
        }

        private static List<string> Dump(OrdinaryLootTestDatabase database, string table) => Rows(database, $"SELECT * FROM {table} ORDER BY 1, 2");

        private static int Count(OrdinaryLootTestDatabase database, string sql) => int.Parse(Rows(database, sql).Single());

        #endregion
    }
}
