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
using Rasa.Repositories.World;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Database;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.World
{
    using S = Rasa.Managers.NpcSquads;

    /// <summary>
    /// NPC squads (NpcSquads; npc_squad_*): the tables, the formations, a GM making a squad in
    /// game and exporting it, and squads in the Wilderness - sent, walking a route in formation,
    /// joining a fight, gathering after it, losing a leader, and sent again after a wipe.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class NpcSquadTests
    {
        private const string Migration = "20270902000000_Npc_squads";
        private const string Previous = "20270901000000_Forget_restamped_migration_ids";

        private static readonly string[] Tables = { "npc_squad_type", "npc_squad_member", "npc_squad_path", "npc_squad_path_point", "npc_squad_spawn" };

        /// <summary>An AFS soldier (Redshirt_Human_Soldier_Light_Male) and a Bane Thrax Soldier, both in the Wilderness.</summary>
        private const uint AfsSoldier = 39, ThraxSoldier = 3;

        /// <summary>Where the harness puts the player in the Wilderness.</summary>
        private static readonly Vector3 Start = new(884.11f, 305.8f, 347.81f);

        /// <summary>Open ground 130 m north of it, where a 24 m square walks straight round.</summary>
        private static readonly Vector3 Origin = new(884.11f, 302.08f, 479.81f);

        private long _now = 1_000_000;

        [TestInitialize]
        public void Clock()
        {
            S.Load(null, null, null, null, null);
            S.Now = () => _now;
            S.Roll = (min, max) => max;
            KaelRushingBlow.Now = () => _now;
        }

        [TestCleanup]
        public void Restore()
        {
            S.Load(null, null, null, null, null);
            S.Now = () => Environment.TickCount64;
            S.Roll = (min, max) => Random.Shared.Next(min, max + 1);
            KaelRushingBlow.Now = () => Environment.TickCount64;
        }

        #region The tables

        [TestMethod]
        public void TheTablesGoInEmptyAndDownTakesThemOut()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            Assert.IsTrue(Tables.All(table => !Exists(database, table)));

            migrator.Migrate(Migration);
            foreach (var table in Tables)
                Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) FROM {table}"), table);

            var squads = new NpcSquadRepository(database.Context);

            Assert.IsTrue(squads.SaveType(new NpcSquadTypeEntry { Id = 1, Name = "Rifles", Formation = 1, Spacing = 4, EngageRange = 25 },
                new[] { Member(1, 0, AfsSoldier, 1, 1), Member(1, 1, AfsSoldier, 2, 4) }));
            Assert.IsTrue(squads.SaveType(new NpcSquadTypeEntry { Id = 1, Name = "Rifle squad", Formation = 0, Spacing = 3, EngageRange = 0 },
                new[] { Member(1, 0, AfsSoldier, 2, 3) }));
            Assert.IsTrue(squads.SavePath(new NpcSquadPathEntry { Id = 7, MapContextId = 1220, Name = "Road", Mode = 1 },
                new[] { Point(7, 0, 1, 2, 3, 0), Point(7, 1, 4, 5, 6, 2500), Point(7, 2, 7, 8, 9, 0) }));
            Assert.IsTrue(squads.SaveSpawn(Spawn(3, 1, 7)));

            Assert.AreEqual("Rifle squad", squads.GetTypes().Single().Name);
            Assert.AreEqual(3, squads.GetMembers().Single().MaxCount, "the type's members replaced, not added to");
            CollectionAssert.AreEqual(new uint[] { 0, 1, 2 }, squads.GetPathPoints().Select(point => point.Step).ToArray());
            Assert.AreEqual(2500u, squads.GetPathPoints()[1].PauseMs);

            var spawn = Spawn(3, 1, 7);
            spawn.Enabled = 0;
            spawn.Comment = "changed";
            Assert.IsTrue(squads.SaveSpawn(spawn));
            Assert.AreEqual("changed", squads.GetSpawns().Single().Comment);

            Assert.IsTrue(squads.DeletePath(7));
            Assert.AreEqual(0u, squads.GetSpawns().Single().PathId, "its spawns hold their spots");
            Assert.AreEqual(0, squads.GetPathPoints().Count);

            Assert.IsTrue(squads.DeleteType(1));
            Assert.AreEqual(0, squads.GetSpawns().Count + squads.GetMembers().Count + squads.GetTypes().Count, "the type goes with its members and spawns");


            migrator.Migrate(Previous);
            Assert.IsTrue(Tables.All(table => !Exists(database, table)));
        }

        [TestMethod]
        public void OnMySqlTheTablesGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var table in Tables)
            {
                StringAssert.Contains(up, $"CREATE TABLE `{table}`");
                StringAssert.Contains(down, $"DROP TABLE `{table}`");
            }

            StringAssert.Contains(up, "PRIMARY KEY (`type_id`, `slot`)");
            StringAssert.Contains(up, "PRIMARY KEY (`path_id`, `step`)");
        }

        #endregion

        #region Formations

        [TestMethod]
        public void AWedgeFansOutBehindAColumnFilesAndALineStandsAbreast()
        {
            Assert.AreEqual(new Vector2(-3, -3), S.Offset(S.Formation.Wedge, 3, 1));
            Assert.AreEqual(new Vector2(3, -3), S.Offset(S.Formation.Wedge, 3, 2));
            Assert.AreEqual(new Vector2(-6, -6), S.Offset(S.Formation.Wedge, 3, 3));
            Assert.AreEqual(new Vector2(0, -8), S.Offset(S.Formation.Column, 4, 2));
            Assert.AreEqual(new Vector2(-2, 0), S.Offset(S.Formation.Line, 2, 1));
            Assert.AreEqual(new Vector2(2, 0), S.Offset(S.Formation.Line, 2, 2));
            Assert.AreEqual(new Vector2(-4, 0), S.Offset(S.Formation.Line, 2, 3));

            var column = new S.SquadType { Id = 1, Name = "c", Formation = S.Formation.Column, Spacing = 3 };
            var leader = new Vector3(10, 0, 10);

            Assert.AreEqual(leader, S.PlaceOf(null, column, 0, leader, 0));

            // A yaw of 0 looks along -z, so the file is behind him along +z; turned to face +x
            // (yaw -pi/2), it is behind him along -x.
            AssertNear(new Vector3(10, 0, 19), S.PlaceOf(null, column, 3, leader, 0));
            AssertNear(new Vector3(1, 0, 10), S.PlaceOf(null, column, 3, leader, -MathF.PI / 2));

            // In a wedge facing -z the first two of them are behind him to either side.
            var wedge = new S.SquadType { Id = 2, Name = "w", Formation = S.Formation.Wedge, Spacing = 3 };
            AssertNear(new Vector3(7, 0, 13), S.PlaceOf(null, wedge, 1, leader, 0));
            AssertNear(new Vector3(13, 0, 13), S.PlaceOf(null, wedge, 2, leader, 0));
        }

        #endregion

        #region A GM's

        [TestMethod]
        public void AGmMakesASquadInGameAndEveryChangeIsSaved()
        {
            using var harness = Create();
            var road = Road(harness);

            Say(harness, ".squad type new AFS rifle squad");
            Say(harness, ".squad type 1 add 39 1 1");
            Say(harness, ".squad type 1 add 39 2 3");
            Say(harness, ".squad type 1 formation column");
            Say(harness, ".squad type 1 spacing 4");
            Assert.IsTrue(Say(harness, ".squad type 1 add 999999").Single().Contains("No creature"));

            harness.MoveTo(road[0]);
            Say(harness, ".squad path new North road back");
            Say(harness, ".squad point");
            harness.MoveTo(road[1]);
            Say(harness, ".squad point 3");
            harness.MoveTo(road[2]);
            Say(harness, ".squad point");
            harness.MoveTo(road[3]);
            Say(harness, ".squad point");
            Say(harness, ".squad path 1 undo");

            harness.MoveTo(road[0]);
            harness.Client.Player.Rotation = 1.25;
            Assert.IsFalse(Say(harness, ".squad spawn add 1 1 90 North patrol").Single().Contains("NOT SAVED"));

            var world = harness.World;
            world.ChangeTracker.Clear();

            var type = world.NpcSquadTypeEntries.AsNoTracking().Single();
            Assert.AreEqual(("AFS rifle squad", (byte)1, 4.0), (type.Name, type.Formation, type.Spacing));
            CollectionAssert.AreEqual(new[] { "0:39:1-1", "1:39:2-3" },
                world.NpcSquadMemberEntries.AsNoTracking().OrderBy(m => m.Slot).AsEnumerable().Select(m => $"{m.Slot}:{m.CreatureId}:{m.MinCount}-{m.MaxCount}").ToArray());

            var path = world.NpcSquadPathEntries.AsNoTracking().Single();
            Assert.AreEqual(("North road", (byte)S.PathMode.Back, 1220u), (path.Name, path.Mode, path.MapContextId));
            var points = world.NpcSquadPathPointEntries.AsNoTracking().OrderBy(p => p.Step).ToList();
            Assert.AreEqual(3, points.Count, "the fourth point undone");
            Assert.AreEqual(3000u, points[1].PauseMs);
            AssertNear(road[2], points[2].Position);

            var spawn = world.NpcSquadSpawnEntries.AsNoTracking().Single();
            Assert.AreEqual((1u, 1u, 90u, (byte)1, "North patrol"), (spawn.TypeId, spawn.PathId, spawn.RespawnSeconds, spawn.Enabled, spawn.Comment));
            Assert.AreEqual(1.25, spawn.Rotation, 1e-6);

            // The export, run on an empty set of tables, gives the same squads back.
            var sql = S.ExportSql(1220);
            StringAssert.Contains(sql, "INSERT INTO npc_squad_spawn");

            foreach (var table in Tables)
                world.Database.ExecuteSqlRaw($"DELETE FROM {table}");
            foreach (var line in sql.Split('\n').Where(line => line.StartsWith("INSERT")))
                world.Database.ExecuteSqlRaw(line);

            S.Init(harness);
            Assert.AreEqual(3, S.Paths[1].Points.Count);
            Assert.AreEqual(2, S.Types[1].Members.Count);
            Assert.AreEqual(S.Formation.Column, S.Types[1].Formation);
            Assert.AreEqual("North patrol", S.Spawns[1].Comment);
            AssertNear(road[0], S.Spawns[1].Position);

            // Taken apart again: a path's spawns hold their spots, a type goes with its spawns.
            Say(harness, ".squad path 1 delete");
            Assert.AreEqual(0u, S.Spawns[1].PathId);
            Say(harness, ".squad type 1 delete");
            Assert.AreEqual(0, S.Spawns.Count);
            world.ChangeTracker.Clear();
            Assert.AreEqual(0, world.NpcSquadSpawnEntries.AsNoTracking().Count() + world.NpcSquadPathEntries.AsNoTracking().Count() + world.NpcSquadTypeEntries.AsNoTracking().Count());
        }

        #endregion

        #region In the world

        [TestMethod]
        public void ASquadIsSentAndWalksItsRouteInFormation()
        {
            using var harness = Create();
            var road = Road(harness);

            Squad(harness, AfsSoldier, 1, 3, S.Formation.Column, road, S.PathMode.Loop);

            Step(harness, 250);
            var squad = S.On(harness.Map)[1];

            Assert.AreEqual(4, squad.Members.Count, "the leader and three more");
            Assert.IsTrue(squad.Members.All(m => m.Creature.Controller.CurrentAction == BehaviorManager.BehaviorActionSquad));
            Assert.IsTrue(squad.Members.All(m => m.Creature.SquadMember == m));
            Assert.IsTrue(squad.Members.All(m => Vector3.Distance(m.Creature.Position, road[0]) < 14), "sent about its spawn");

            var visited = new HashSet<int>();
            var spread = 0f;

            for (var i = 0; i < 4 * 120; i++)
            {
                Step(harness, 250);
                visited.Add(squad.PathIndex);

                if (squad.Phase == S.Phase.Moving && i > 40)
                {
                    var leader = squad.Leader.Creature;
                    spread = Math.Max(spread, squad.Members.Skip(1).Max(m =>
                        Flat(m.Creature.Position - S.PlaceOf(harness.Map, S.Types[1], m.Place, leader.Position, squad.Heading))));
                }
            }

            CollectionAssert.IsSubsetOf(new[] { 0, 1, 2, 3 }, visited.ToArray(), "every point of the route reached: " + string.Join(",", visited));
            Assert.IsTrue(spread < S.WaitDistance + 2, $"they keep their places as they go: {spread:0.0} m at most");
            Assert.IsTrue(squad.Members.All(m => m.Creature.Controller.CurrentAction == BehaviorManager.BehaviorActionSquad));
        }

        [TestMethod]
        public void ARouteWalkedOnceEndsAndOneWalkedBackTurnsAround()
        {
            using var harness = Create();
            var road = Road(harness);

            Squad(harness, AfsSoldier, 0, 1, S.Formation.Wedge, road, S.PathMode.Back);
            Step(harness, 250);
            var squad = S.On(harness.Map)[1];

            var order = new List<int>();
            for (var i = 0; i < 4 * 150 && order.Count < 6; i++)
            {
                Step(harness, 250);
                if (order.Count == 0 || order[^1] != squad.PathIndex)
                    order.Add(squad.PathIndex);
            }

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 2, 1, 0 }, order.Take(6).ToArray(), "back the way it came: " + string.Join(",", order));

            S.Paths[1].Mode = S.PathMode.Once;
            for (var i = 0; i < 4 * 150 && squad.Phase != S.Phase.Holding; i++)
                Step(harness, 250);

            Assert.AreEqual(S.Phase.Holding, squad.Phase);
            var at = squad.Leader.Creature.Position;
            Step(harness, 5000);
            Assert.IsTrue(Flat(squad.Leader.Creature.Position - at) < 1f, "it stays at the end");
        }

        [TestMethod]
        public void WhenOneOfThemFightsTheOthersJoinAndAfterwardsTheyGatherAndGoOn()
        {
            using var harness = Create();
            var road = Road(harness);

            Squad(harness, AfsSoldier, 0, 3, S.Formation.Line, road, S.PathMode.Loop);
            S.Types[1].EngageRange = 0.5f;
            Step(harness, 250);
            var squad = S.On(harness.Map)[1];

            for (var i = 0; i < 8; i++)
                Step(harness, 250);

            // A Thrax well off to one side, that nobody has noticed yet.
            var thrax = harness.Creatures.CreateCreature(ThraxSoldier, null);
            var near = harness.Map.NavMesh.Nearest(squad.Leader.Creature.Position + new Vector3(25, 0, 0)) ?? squad.Leader.Creature.Position;
            CreatureManager.Instance.SetLocation(thrax, near, 0, 1220);
            thrax.AggroRange = 0.5f;
            CellManager.Instance.AddToWorld(harness.Map, thrax);
            harness.MoveTo(Start);

            var first = squad.Members[2].Creature;
            first.Hate.Ensure(thrax.EntityId, Threat.NoticedThreat);
            BehaviorManager.Instance.SetActionFighting(first, thrax.EntityId);

            S.Worker(harness.Map);
            Assert.AreEqual(S.Phase.Fighting, squad.Phase);
            Assert.IsTrue(squad.Members.All(m => m.Creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting &&
                m.Creature.Controller.ActionFighting.TargetEntityId == thrax.EntityId), "all of them on the Thrax");

            // They go at it; the squad stands meanwhile.
            var leaderAt = squad.Leader.Creature.HomePos.Position;
            for (var i = 0; i < 16; i++)
                Step(harness, 250);
            Assert.AreEqual(S.Phase.Fighting, squad.Phase);
            AssertNear(leaderAt, squad.Leader.Creature.HomePos.Position, 0.01f);

            // The Thrax gone, the fight is over: they gather on the leader and go on.
            CellManager.Instance.RemoveCreatureFromWorld(harness.Map, thrax);
            foreach (var member in squad.Members)
                BehaviorManager.Instance.GiveUp(member.Creature);

            // Two of them ended it well off their places.
            foreach (var member in squad.Members.Skip(2))
                member.Creature.Position = harness.Map.NavMesh.Nearest(member.Creature.Position + new Vector3(10, 0, 4)) ?? member.Creature.Position;

            var phases = new List<S.Phase>();
            for (var i = 0; i < 4 * 30 && squad.Phase != S.Phase.Moving; i++)
            {
                Step(harness, 250);
                if (phases.Count == 0 || phases[^1] != squad.Phase)
                    phases.Add(squad.Phase);
            }

            CollectionAssert.Contains(phases, S.Phase.Regrouping, string.Join(",", phases));
            Assert.AreEqual(S.Phase.Moving, squad.Phase);
            Assert.IsTrue(squad.Members.All(m => m.Creature.Controller.CurrentAction == BehaviorManager.BehaviorActionSquad), "back with the squad: " +
                string.Join(", ", squad.Members.Select(m => $"{m.Creature.Controller.CurrentAction}/{m.Creature.Controller.ActionWander.State}")) + " after " + string.Join(",", phases) + $" {phases.Count}");
        }

        [TestMethod]
        public void ALeaderWhoFallsIsReplacedAndAWipedSquadIsSentAgain()
        {
            using var harness = Create();
            var road = Road(harness);

            Squad(harness, AfsSoldier, 0, 2, S.Formation.Wedge, road, S.PathMode.Loop);
            S.Spawns[1].RespawnSeconds = 60;
            Step(harness, 250);
            var squad = S.On(harness.Map)[1];
            var first = squad.Members.Select(m => m.Creature).ToList();

            Kill(first[0]);
            Step(harness, 250);
            Assert.AreSame(first[1], squad.Leader.Creature, "the next leads");
            CollectionAssert.AreEqual(new[] { 0, 1 }, squad.Members.Select(m => m.Place).ToArray(), "the places closed up");

            Kill(first[1]);
            Kill(first[2]);
            Step(harness, 250);
            Assert.AreEqual(S.Phase.Gone, squad.Phase);
            Assert.AreEqual(_now + 60_000, squad.RespawnAt, 300);

            Step(harness, 59_000);
            Assert.AreEqual(S.Phase.Gone, squad.Phase, "not yet");

            Step(harness, 1_500);
            Assert.AreEqual(3, squad.Living.Count(), "sent again");
            Assert.IsTrue(squad.Members.All(m => !first.Contains(m.Creature)));
        }

        [TestMethod]
        public void AGmDisablesMovesAndDeletesASpawnAndItsSquadGoes()
        {
            using var harness = Create();
            var road = Road(harness);

            Squad(harness, AfsSoldier, 0, 1, S.Formation.Wedge, road, S.PathMode.Loop);
            Step(harness, 250);
            var squad = S.On(harness.Map)[1];
            var sent = squad.Members.Select(m => m.Creature).ToList();

            Say(harness, ".squad spawn 1 disable");
            Step(harness, 250);
            Assert.AreEqual(0, squad.Living.Count());
            Assert.IsTrue(sent.All(c => !EntityManager.Instance.Creatures.ContainsKey(c.EntityId) && c.SquadMember == null), "out of the world");

            Say(harness, ".squad spawn 1 enable");
            Step(harness, 250);
            Assert.AreEqual(2, squad.Living.Count(), "back at once");

            harness.MoveTo(road[2]);
            Say(harness, ".squad spawn 1 move");
            Step(harness, 250);
            Assert.IsTrue(squad.Members.All(m => Vector3.Distance(m.Creature.Position, road[2]) < 8), "sent again where the GM stands");

            Say(harness, ".squad spawn 1 path 0");
            for (var i = 0; i < 20; i++)
                Step(harness, 250);
            Assert.AreEqual(S.Phase.Holding, squad.Phase, "no route: it holds its spot");

            Say(harness, ".squad spawn 1 delete");
            Step(harness, 250);
            Assert.IsFalse(S.On(harness.Map).ContainsKey(1));
            Assert.AreEqual(0, squad.Living.Count());
        }

        #endregion

        #region Helpers

        private WildernessRuntimeTestHarness Create()
        {
            var harness = WildernessRuntimeTestHarness.Create();
            try
            {
                S.Init(harness);
                harness.Drain();
                return harness;
            }
            catch
            {
                harness.Dispose();
                throw;
            }
        }

        /// <summary>Four points of a square on open ground, each on the navmesh.</summary>
        private static Vector3[] Road(WildernessRuntimeTestHarness harness)
        {
            var corners = new[] { new Vector3(0, 0, 0), new Vector3(24, 0, 0), new Vector3(24, 0, 24), new Vector3(0, 0, 24) };
            var road = corners.Select(c => harness.Map.NavMesh.Nearest(Origin + c) ?? throw new AssertFailedException($"no navmesh at {Origin + c}")).ToArray();

            for (var i = 0; i < road.Length; i++)
                Assert.IsTrue(Flat(road[i] - (Origin + corners[i])) < 1, $"point {i} is where it was meant to be: {road[i]}");

            return road;
        }

        /// <summary>A squad type of a leader and <paramref name="more"/> of the creature, a route on the road, and a spawn at its start.</summary>
        private void Squad(WildernessRuntimeTestHarness harness, uint creatureId, byte min, byte more, S.Formation formation, Vector3[] road, S.PathMode mode)
        {
            harness.MoveTo(road[0]);
            Say(harness, ".squad type new Test squad");
            Say(harness, $".squad type 1 add {creatureId}");
            Say(harness, $".squad type 1 add {creatureId} {min} {more}");
            Say(harness, $".squad type 1 formation {formation}");
            Say(harness, $".squad path new Road {mode}");

            foreach (var point in road)
            {
                harness.MoveTo(point);
                Say(harness, ".squad point");
            }

            harness.MoveTo(road[0]);
            Say(harness, ".squad spawn add 1 1 120");
            harness.MoveTo(Start);
            Assert.AreEqual(1, S.Spawns.Count);
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

        private static void Kill(Creature creature)
        {
            creature.Attributes[Attributes.Health].Current = 0;
            creature.State = CharacterState.Dead;
        }

        private static float Flat(Vector3 v) => new Vector2(v.X, v.Z).Length();

        private static void AssertNear(Vector3 expected, Vector3 actual, float tolerance = 0.01f) =>
            Assert.IsTrue(Vector3.Distance(expected, actual) <= tolerance, $"expected {expected}, was {actual}");

        private static NpcSquadMemberEntry Member(uint type, byte slot, uint creature, byte min, byte max) =>
            new() { TypeId = type, Slot = slot, CreatureId = creature, MinCount = min, MaxCount = max };

        private static NpcSquadPathPointEntry Point(uint path, uint step, double x, double y, double z, uint pause) =>
            new() { PathId = path, Step = step, PosX = x, PosY = y, PosZ = z, PauseMs = pause };

        private static NpcSquadSpawnEntry Spawn(uint id, uint type, uint path) => new()
        {
            Id = id, MapContextId = 1220, TypeId = type, PathId = path, PosX = 1, PosY = 2, PosZ = 3, Rotation = 0.5,
            RespawnSeconds = 120, Enabled = 1, Comment = "test"
        };

        private static bool Exists(OrdinaryLootTestDatabase database, string table) =>
            Count(database, $"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '{table}'") == 1;

        private static int Count(OrdinaryLootTestDatabase database, string sql)
        {
            using var command = database.Context.Database.GetDbConnection().CreateCommand();
            if (command.Connection.State != System.Data.ConnectionState.Open)
                command.Connection.Open();
            command.CommandText = sql;
            return Convert.ToInt32(command.ExecuteScalar());
        }

        #endregion
    }
}
