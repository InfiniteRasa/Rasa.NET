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
    /// joining a fight, gathering after it, losing a leader, and sent again after a wipe; a
    /// member fighting on a tether to its leader; and a leader who is a bystander his members guard.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class NpcSquadTests
    {
        private const string Migration = "20270902000000_Npc_squads";
        private const string Previous = "20270901000000_Forget_restamped_migration_ids";
        private const string Latest = "20270925000000_Npc_squad_leader_bystander";
        private const string Tethered = "20270919000000_Npc_squad_tether";
        private const string BeforeTether = "20270918000000_Solis_escort_staff";
        private const string BeforeBystander = "20270922021100_Plateau_holy_vengeance";

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

            // The repository works on the tables as the model has them now: with the columns a
            // later migration added (Alia_das_afs_squad, Npc_squad_tether, Npc_squad_leader_bystander), whose squad is cleared
            // away first.
            migrator.Migrate(Latest);
            foreach (var table in Tables)
                database.Context.Database.ExecuteSqlRaw($"DELETE FROM {table}");

            var squads = new NpcSquadRepository(database.Context);

            Assert.IsTrue(squads.SaveType(new NpcSquadTypeEntry { Id = 1, Name = "Rifles", Formation = 1, Spacing = 4, EngageRange = 25 },
                new[] { Member(1, 0, AfsSoldier, 1, 1), Member(1, 1, AfsSoldier, 2, 4) }));
            Assert.IsTrue(squads.SaveType(new NpcSquadTypeEntry { Id = 1, Name = "Rifle squad", Formation = 0, Spacing = 3, EngageRange = 0, Tether = 30, LeaderBystander = 1 },
                new[] { Member(1, 0, AfsSoldier, 2, 3) }));
            Assert.IsTrue(squads.SavePath(new NpcSquadPathEntry { Id = 7, MapContextId = 1220, Name = "Road", Mode = 1 },
                new[] { Point(7, 0, 1, 2, 3, 0), Point(7, 1, 4, 5, 6, 2500), Point(7, 2, 7, 8, 9, 0) }));
            Assert.IsTrue(squads.SaveSpawn(Spawn(3, 1, 7)));

            Assert.AreEqual("Rifle squad", squads.GetTypes().Single().Name);
            Assert.AreEqual(30.0, squads.GetTypes().Single().Tether);
            Assert.AreEqual(1, squads.GetTypes().Single().LeaderBystander);
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

        #region The tether

        [TestMethod]
        public void TheTetherGoesInAtNoneForTheTypesThereAreAndDownTakesItOut()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(BeforeTether);
            Assert.IsFalse(HasColumn(database, "npc_squad_type", "tether"));
            var types = Count(database, "SELECT COUNT(*) FROM npc_squad_type");
            Assert.IsTrue(types > 0, "Alia Das's squad is there to be given one");

            migrator.Migrate(Tethered);
            Assert.AreEqual(types, Count(database, "SELECT COUNT(*) FROM npc_squad_type WHERE tether = 0"), "no tether: as they went before");

            migrator.Migrate(BeforeTether);
            Assert.IsFalse(HasColumn(database, "npc_squad_type", "tether"));
            Assert.AreEqual(types, Count(database, "SELECT COUNT(*) FROM npc_squad_type"));

            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var mysql = context.GetService<IMigrator>();

            StringAssert.Contains(mysql.GenerateScript(BeforeTether, Tethered), "ADD `tether` double NOT NULL DEFAULT 0");
            StringAssert.Contains(mysql.GenerateScript(Tethered, BeforeTether), "DROP COLUMN `tether`");
        }

        [TestMethod]
        public void AGmPutsATetherOnASquadTypeAndItIsSavedAndExported()
        {
            using var harness = Create();

            Say(harness, ".squad type new Guards");
            Say(harness, $".squad type 1 add {AfsSoldier}");
            StringAssert.Contains(Say(harness, ".squad type 1").First(), "no tether");

            var said = Say(harness, ".squad type 1 tether 22.5").Single();
            Assert.IsFalse(said.Contains("NOT SAVED"), said);
            StringAssert.Contains(said, "tethered at 22.5 m");
            Assert.AreEqual(22.5f, S.Types[1].Tether);

            var world = harness.World;
            world.ChangeTracker.Clear();
            Assert.AreEqual(22.5, world.NpcSquadTypeEntries.AsNoTracking().Single().Tether);

            // The export gives it back.
            var sql = S.ExportSql(0);
            StringAssert.Contains(sql, "engage_range, tether, leader_bystander) VALUES (1, 'Guards', 0, 3, 0, 22.5, 0);");

            foreach (var table in Tables)
                world.Database.ExecuteSqlRaw($"DELETE FROM {table}");
            foreach (var line in sql.Split('\n').Where(line => line.StartsWith("INSERT")))
                world.Database.ExecuteSqlRaw(line);

            S.Init(harness);
            Assert.AreEqual(22.5f, S.Types[1].Tether);

            Say(harness, ".squad type 1 tether 0");
            Assert.AreEqual(0f, S.Types[1].Tether);

            // Only a finite length, none below 0; with none given, how to give one.
            foreach (var bad in new[] { ".squad type 1 tether Infinity", ".squad type 1 tether NaN", ".squad type 1 tether -3", ".squad type 1 tether" })
            {
                StringAssert.Contains(Say(harness, bad).Single(), "tether <m>", bad);
                Assert.AreEqual(0f, S.Types[1].Tether, bad);
            }

            // Nor from a row.
            S.Load(new[] { new NpcSquadTypeEntry { Id = 1, Name = "x", Spacing = 3, Tether = double.PositiveInfinity },
                new NpcSquadTypeEntry { Id = 2, Name = "y", Spacing = 3, Tether = double.NaN } }, null, null, null, null);
            Assert.AreEqual((0f, 0f), (S.Types[1].Tether, S.Types[2].Tether));
        }

        [TestMethod]
        public void OnAShortTetherAMemberStillJoinsAFightByItsLeader()
        {
            Assert.AreEqual(25f, S.RejoinDistance(30));
            Assert.AreEqual(5f, S.RejoinDistance(10), "half of it");
            Assert.AreEqual(2f, S.RejoinDistance(4), "half of it, not below nothing");
        }

        [TestMethod]
        public void OnATetherAMemberChasesNoFurtherHoldsEvadingThereThenGoesBackToItsPlaceAndRejoinsWhenTheTargetComesNear()
        {
            using var harness = Create();
            const float tether = 12f;
            var (squad, leader, member, thrax) = TetherFight(harness, tether);

            Assert.AreSame(leader, S.TetherOf(member.Creature, out var length).SquadMember);
            Assert.AreEqual(tether, length);
            Assert.IsNull(S.TetherOf(leader.Creature, out _), "the leader is on none");

            // Another of them is not drawn into a fight with a target that far from the leader:
            // it keeps its place about him.
            var other = squad.Members[2];
            Assert.AreEqual(BehaviorManager.BehaviorActionSquad, other.Creature.Controller.CurrentAction);

            // It chases as far as its tether, and holds there, in the fight, evading.
            var furthest = 0f;
            for (var i = 0; i < 4 * 20 && !S.Evades(member.Creature); i++)
            {
                Step(harness, 250);
                furthest = Math.Max(furthest, Flat(member.Creature.Position - leader.Creature.Position));
            }

            Assert.IsTrue(S.Evades(member.Creature), "it holds at the tether's end");
            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, member.Creature.Controller.CurrentAction, "it does not drop the fight");
            Assert.IsTrue(furthest <= tether + 1f, $"no further than the tether from its leader: {furthest:0.0} m");
            Assert.IsTrue(Flat(member.Creature.Position - leader.Creature.Position) >= tether - S.TetherSlack - 0.5f);
            Assert.IsFalse(BehaviorManager.IsReturning(member.Creature));
            var off = MathF.Abs(MathF.IEEERemainder(member.Creature.LastYaw - AbilityManager.YawTowards(member.Creature.Position, thrax.Position), MathF.PI * 2));
            Assert.IsTrue(off < 0.4f, $"facing its target: {off:0.00} rad off");

            // The target moves round, still out of reach: it turns to it, as the clients are told (LastYaw).
            var round = thrax.Position - member.Creature.Position;
            var turned = Vector3.Transform(round, Quaternion.CreateFromAxisAngle(Vector3.UnitY, 1f));
            var thraxAt = thrax.Position;
            PinAt(thrax, harness.Map.NavMesh.Nearest(member.Creature.Position + turned) ?? throw new AssertFailedException("no navmesh there"));
            Step(harness, 250);
            Assert.IsTrue(S.Evades(member.Creature));
            off = MathF.Abs(MathF.IEEERemainder(member.Creature.LastYaw - AbilityManager.YawTowards(member.Creature.Position, thrax.Position), MathF.PI * 2));
            Assert.IsTrue(off < 0.4f, $"turned to its target: {off:0.00} rad off");
            PinAt(thrax, thraxAt);

            // Holding where it cannot answer, it is not shot down, nor debuffed.
            Assert.IsTrue(DamageImmunity.IsImmune(member.Creature, DamageType.Physical));
            Assert.IsFalse(DamageImmunity.IsImmune(other.Creature, DamageType.Physical), "one not holding is not");
            member.Creature.Attributes[Attributes.Health].Current = 50;

            // Held too long: back to its place, as it is - not run home and made whole.
            for (var i = 0; i < 4 * 12 && member.Creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting; i++)
                Step(harness, 250);

            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, member.Creature.Controller.CurrentAction, "the fight given up");
            Assert.IsFalse(S.Evades(member.Creature));
            Assert.AreEqual(50, member.Creature.Attributes[Attributes.Health].Current);

            for (var i = 0; i < 4 * 8; i++)
                Step(harness, 250);

            Assert.AreEqual(BehaviorManager.BehaviorActionSquad, member.Creature.Controller.CurrentAction, "not drawn back in while the target is beyond its tether");
            var place = S.PlaceOf(harness.Map, S.Types[1], member.Place, leader.Creature.Position, squad.Heading);
            Assert.IsTrue(Flat(member.Creature.Position - place) < 2f, $"back in its place: {Flat(member.Creature.Position - place):0.0} m off it");
            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, leader.Creature.Controller.CurrentAction, "its leader fights on");

            // The target comes within the tether, short of its end, of the leader: they join.
            PinAt(thrax, harness.Map.NavMesh.Nearest(leader.Creature.Position + new Vector3(tether - S.TetherRejoinMargin - 1, 0, 0)).Value);
            Step(harness, 250);

            Assert.IsTrue(squad.Members.All(m => m.Creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting &&
                m.Creature.Controller.ActionFighting.TargetEntityId == thrax.EntityId), "all of them on it");
        }

        [TestMethod]
        public void OnATetherAMemberWhoseLeaderFallsIsTetheredToTheNextOne()
        {
            using var harness = Create();
            const float tether = 12f;
            var (squad, leader, member, _) = TetherFight(harness, tether);
            var next = squad.Members[1];

            for (var i = 0; i < 4 * 20 && !S.Evades(member.Creature); i++)
                Step(harness, 250);
            Assert.IsTrue(S.Evades(member.Creature));

            Kill(leader.Creature);
            Assert.AreSame(next.Creature, S.TetherOf(member.Creature, out _), "the next of them leads");

            // The new leader stands half a tether behind it, toward its old one: it is no longer at
            // its tether's end, and goes on after its target until it is at the end of his.
            Root(next.Creature);
            next.Creature.Position = harness.Map.NavMesh.Nearest(member.Creature.Position - new Vector3(tether / 2, 0, 0)).Value;
            var heldAt = member.Creature.Position;

            var furthest = 0f;
            for (var i = 0; i < 4 * 4; i++)
            {
                Step(harness, 250);
                furthest = Math.Max(furthest, Flat(member.Creature.Position - next.Creature.Position));
            }

            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, member.Creature.Controller.CurrentAction);
            Assert.IsTrue(S.Evades(member.Creature), "it holds at the end of the new leader's tether");
            Assert.IsTrue(Flat(member.Creature.Position - heldAt) >= tether / 2 - 2f, $"further on: {Flat(member.Creature.Position - heldAt):0.0} m");
            Assert.IsTrue(furthest <= tether + 1f, $"no further than the tether from the new leader: {furthest:0.0} m");

            // The last of them standing is his own leader: on no tether, leashed as any creature.
            Kill(next.Creature);
            foreach (var m in squad.Members.Where(m => m != member))
                Kill(m.Creature);
            Assert.IsNull(S.TetherOf(member.Creature, out _));
        }

        [TestMethod]
        public void OnATetherAMemberDoesNotGoLookingForAFightBeyondItAndJoinsOneNearItsLeader()
        {
            using var harness = Create();
            const float tether = 12f;
            var road = Road(harness);

            // They look further than the tether; their creatures' own aggro ranges as they are.
            Squad(harness, AfsSoldier, 0, 3, S.Formation.Line, road, S.PathMode.Loop);
            S.Types[1].EngageRange = 30f;
            S.Types[1].Tether = tether;
            Step(harness, 250);
            var squad = S.On(harness.Map)[1];
            var leader = squad.Leader;
            Root(leader.Creature);

            for (var i = 0; i < 8; i++)
                Step(harness, 250);

            // A Thrax 15 m from the leader: past where they take a fight up, inside where they look.
            var at = harness.Map.NavMesh.Nearest(leader.Creature.Position + new Vector3(15f, 0, 0)).Value;
            var thrax = harness.Creatures.CreateCreature(ThraxSoldier, null);
            CreatureManager.Instance.SetLocation(thrax, at, 0, 1220);
            thrax.AggroRange = 0.5f;
            CellManager.Instance.AddToWorld(harness.Map, thrax);
            PinAt(thrax, at);
            harness.MoveTo(Start);

            for (var i = 0; i < 4 * 15; i++)
            {
                Step(harness, 250);
                PinAt(thrax, at);
                foreach (var member in squad.Members.Skip(1))
                    Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, member.Creature.Controller.CurrentAction,
                        $"member {member.Place} took it up after {i * 250} ms");
            }

            // Brought within the tether, short of its end, of the leader: they take it up.
            at = harness.Map.NavMesh.Nearest(leader.Creature.Position + new Vector3(S.RejoinDistance(tether) - 1, 0, 0)).Value;
            PinAt(thrax, at);
            for (var i = 0; i < 4 * 4 && !squad.Members.All(m => m.Creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting); i++)
            {
                Step(harness, 250);
                PinAt(thrax, at);
            }

            Assert.IsTrue(squad.Members.All(m => m.Creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting));
        }

        [TestMethod]
        public void OnATetherAMemberHeldAtItsEndGivesUpInTimeThoughItsLeaderMoves()
        {
            using var harness = Create();
            const float tether = 12f;
            var (_, leader, member, _) = TetherFight(harness, tether);

            for (var i = 0; i < 4 * 20 && !S.Evades(member.Creature); i++)
                Step(harness, 250);
            Assert.IsTrue(S.Evades(member.Creature));
            var home = leader.Creature.Position;
            var toward = harness.Map.NavMesh.Nearest(home + new Vector3(2.5f, 0, 0)).Value;

            // The leader steps toward the target and back, every second: the tether's end moves.
            var ms = 0;
            for (; ms < S.TetherHoldMs + 3000 && member.Creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting; ms += 250)
            {
                if (ms % 1000 == 0)
                    leader.Creature.Position = ms % 2000 == 0 ? toward : home;

                Step(harness, 250);

                if (member.Creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting)
                {
                    Assert.IsTrue(S.Evades(member.Creature), $"still held, out of reach, after {ms} ms");
                    Assert.IsTrue(Flat(member.Creature.Position - leader.Creature.Position) <= tether + 3.5f);
                }
            }

            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, member.Creature.Controller.CurrentAction, "it gave the fight up");
            Assert.IsTrue(ms <= S.TetherHoldMs + 250, $"in its time: {ms} ms");
        }

        [TestMethod]
        public void OnATetherAMemberWhoseRouteGoesNoNearerHoldsThereAndIsNotSentHomeWhole()
        {
            using var harness = Create();
            var (_, _, member, _) = TetherFight(harness, 12f);
            var controller = member.Creature.Controller;
            member.Creature.Attributes[Attributes.Health].Current = 50;

            // No route that brings it nearer: as on ground the navmesh does not join up.
            void NoRoute()
            {
                controller.Path.Clear();
                controller.PathIndex = 0;
                controller.ActionFighting.RouteIncomplete = true;
                controller.TimerPathUpdateLock = 10_000;
            }

            var evaded = false;
            for (var i = 0; i < 4 * 18 && controller.CurrentAction == BehaviorManager.BehaviorActionFighting; i++)
            {
                NoRoute();
                Step(harness, 250);
                evaded |= S.Evades(member.Creature);
                Assert.IsFalse(BehaviorManager.IsReturning(member.Creature), $"not sent home after {i * 250} ms");
            }

            Assert.IsTrue(evaded, "it held, evading");
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, controller.CurrentAction, "and gave the fight up");
            Assert.IsFalse(BehaviorManager.IsReturning(member.Creature));
            Assert.AreEqual(50, member.Creature.Attributes[Attributes.Health].Current, "as it is");
        }

        [TestMethod]
        public void OnATetherAMemberHeldWithItsTargetRightBesideItStillGivesUpInTime()
        {
            using var harness = Create();
            var (_, _, member, thrax) = TetherFight(harness, 12f);

            for (var i = 0; i < 4 * 20 && !S.Evades(member.Creature); i++)
                Step(harness, 250);
            Assert.IsTrue(S.Evades(member.Creature));

            // Right by it, on the far side of the tether's end, near enough that it does not step
            // up to it, and inside the least range of every attack it has; and it cannot walk
            // backwards off it (CreatureStepBack). It stands, as one near enough does.
            foreach (var action in member.Creature.Actions)
                (action.RangeMin, action.RangeMax) = (5, 10);
            member.Creature.WalkSpeed = 0;
            var away = Vector3.Normalize(new Vector3(thrax.Position.X - member.Creature.Position.X, 0, thrax.Position.Z - member.Creature.Position.Z));
            var at = harness.Map.NavMesh.Nearest(member.Creature.Position + away * 1.5f).Value;

            for (var i = 0; i < 4 * 13 && member.Creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting; i++)
            {
                PinAt(thrax, at);
                Step(harness, 250);
            }

            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, member.Creature.Controller.CurrentAction, "it gave the fight up");
            Assert.IsFalse(S.Evades(member.Creature));
        }

        [TestMethod]
        public void OnATetherTwoMembersAtItsEndDoNotStepOffPastIt()
        {
            using var harness = Create();
            const float tether = 12f;

            // A Bane squad, at an AFS soldier: one of a crowd of the Bane steps off (Spacing.Keeps).
            var (squad, leader, member, thrax) = TetherFight(harness, tether, ThraxSoldier, AfsSoldier);
            var second = squad.Members[2];

            for (var i = 0; i < 4 * 20 && !S.Evades(member.Creature); i++)
                Step(harness, 250);
            Assert.IsTrue(S.Evades(member.Creature));

            // Another of them comes to the same spot: one of a crowd of its side, it would step off
            // round their target to a place of its own (Spacing) - not past the tether.
            second.Creature.Position = harness.Map.NavMesh.Nearest(member.Creature.Position + new Vector3(0, 0, 0.3f)).Value;
            second.Creature.Hate.Ensure(thrax.EntityId, Threat.NoticedThreat);
            BehaviorManager.Instance.SetActionFighting(second.Creature, thrax.EntityId);

            // Held, it does not step off round the target, its hold clock running on; and neither
            // goes past the tether.
            for (var i = 0; i < 4 * 6; i++)
            {
                Step(harness, 250);

                Assert.IsFalse(S.Evades(member.Creature) && member.Creature.Controller.ActionFighting.SpreadingTo.HasValue,
                    $"held, it stepped off after {i * 250} ms");

                foreach (var m in new[] { member, second })
                    Assert.IsTrue(Flat(m.Creature.Position - leader.Creature.Position) <= tether + 1f,
                        $"member {m.Place} {Flat(m.Creature.Position - leader.Creature.Position):0.0} m from its leader after {i * 250} ms");
            }
        }

        [TestMethod]
        public void OnATetherAMemberHeldWhenItsLeaderRunsHomeGivesUpAtOnceAndGoesBack()
        {
            using var harness = Create();
            const float tether = 12f;
            var (squad, leader, member, _) = TetherFight(harness, tether);

            for (var i = 0; i < 4 * 20 && !S.Evades(member.Creature); i++)
                Step(harness, 250);
            Assert.IsTrue(S.Evades(member.Creature));
            member.Creature.Attributes[Attributes.Health].Current = 50;

            // The leader, free again, was dragged too far from home: he runs back to it (his own
            // leash), away from the member, as Goliath did in play.
            var other = squad.Members[1].Creature;
            leader.Creature.WalkSpeed = other.WalkSpeed;
            leader.Creature.RunSpeed = other.RunSpeed;
            leader.Creature.MovementSpeed = 1;
            var home = harness.Map.NavMesh.Nearest(leader.Creature.Position - new Vector3(61f, 0, 0)) ?? throw new AssertFailedException("no navmesh there");
            leader.Creature.HomePos.Position = home;

            var past = -1;
            var gaveUp = -1;
            for (var ms = 0; ms < 20_000 && gaveUp < 0; ms += 250)
            {
                Step(harness, 250);

                if (past < 0 && Flat(member.Creature.Position - leader.Creature.Position) > tether + S.TetherSlack)
                    past = ms;
                if (member.Creature.Controller.CurrentAction != BehaviorManager.BehaviorActionFighting)
                    gaveUp = ms;
            }

            Assert.IsTrue(BehaviorManager.IsReturning(leader.Creature) || Flat(leader.Creature.Position - home) < 3f, "the leader ran home");
            Assert.IsTrue(past >= 0, "left past its tether");
            Assert.IsTrue(gaveUp >= 0 && gaveUp - past <= 500, $"it gave the fight up at once: {gaveUp - past} ms after it was left past its tether");
            Assert.IsTrue(gaveUp < S.TetherHoldMs, $"within the hold time: {gaveUp} ms");
            Assert.AreEqual(0, member.Creature.Hate.Ranked().Count(), "nor does it keep its target");
            Assert.IsFalse(BehaviorManager.IsReturning(member.Creature));
            Assert.AreEqual(50, member.Creature.Attributes[Attributes.Health].Current, "as it is, not made whole");

            // Back to its place about the leader, once he is home.
            var off = float.MaxValue;
            for (var i = 0; i < 4 * 40 && off > 3f; i++)
            {
                Step(harness, 250);
                off = Flat(member.Creature.Position - S.PlaceOf(harness.Map, S.Types[1], member.Place, leader.Creature.Position, squad.Heading));
            }

            Assert.IsTrue(off <= 3f, $"back in its place: {off:0.0} m off it");
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, member.Creature.Controller.CurrentAction);
        }

        [TestMethod]
        public void OnATetherAMemberHeldWhenItsLeaderGoesOnGivesUpOnceItIsLeftPastItsEnd()
        {
            using var harness = Create();
            const float tether = 12f;
            var (_, leader, member, _) = TetherFight(harness, tether);

            for (var i = 0; i < 4 * 20 && !S.Evades(member.Creature); i++)
                Step(harness, 250);
            Assert.IsTrue(S.Evades(member.Creature));

            // The leader walks on away from it, a metre a think.
            var past = -1;
            var gaveUp = -1;
            for (var ms = 0; ms < 6000 && gaveUp < 0; ms += 250)
            {
                leader.Creature.Position = harness.Map.NavMesh.Nearest(leader.Creature.Position - new Vector3(1f, 0, 0)) ?? leader.Creature.Position;
                Step(harness, 250);

                if (past < 0 && Flat(member.Creature.Position - leader.Creature.Position) > tether + S.TetherSlack)
                    past = ms;
                if (member.Creature.Controller.CurrentAction != BehaviorManager.BehaviorActionFighting)
                    gaveUp = ms;
            }

            Assert.IsTrue(past >= 0, "left past its tether");
            Assert.IsTrue(gaveUp >= 0 && gaveUp - past <= 250, $"it gave the fight up at once: {gaveUp - past} ms after it was left past its tether");
            Assert.IsFalse(S.Evades(member.Creature));
        }

        [TestMethod]
        public void OnATetherTheHoldRunsOutThoughAnAttackOfItsComesInReach()
        {
            using var harness = Create();
            var (_, _, member, _) = TetherFight(harness, 12f);

            for (var i = 0; i < 4 * 20 && !S.Evades(member.Creature); i++)
                Step(harness, 250);
            Assert.IsTrue(S.Evades(member.Creature));
            var heldFor = member.Creature.Controller.ActionFighting.TetherHeldMs;

            // Now every attack it has reaches its target, but each is cooling down: it stands in
            // reach, as a Kael with its charge used did in play. It answers - it does not evade -
            // and its hold runs out all the same.
            var ms = 0;
            for (; ms < S.TetherHoldMs + 2000 && member.Creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting; ms += 250)
            {
                foreach (var action in member.Creature.Actions)
                    (action.RangeMin, action.RangeMax, action.CooldownTimer) = (0, 500, 100_000);

                Step(harness, 250);

                if (member.Creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting)
                    Assert.IsFalse(S.Evades(member.Creature), $"in reach, it answers: no evading after {ms} ms");
            }

            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, member.Creature.Controller.CurrentAction, "it gave the fight up");
            Assert.IsTrue(heldFor + ms <= S.TetherHoldMs + 500, $"when the hold ran out: {heldFor + ms} ms");
        }

        [TestMethod]
        public void WithNoTetherAMemberChasesAsBefore()
        {
            using var harness = Create();
            var (squad, leader, member, _) = TetherFight(harness, 0);

            Assert.IsNull(S.TetherOf(member.Creature, out _));

            var furthest = 0f;
            for (var i = 0; i < 4 * 20; i++)
            {
                Step(harness, 250);
                furthest = Math.Max(furthest, Flat(member.Creature.Position - leader.Creature.Position));
                Assert.IsFalse(S.Evades(member.Creature));
            }

            Assert.IsTrue(furthest > 12f + 5f, $"it goes on after its target: {furthest:0.0} m from its leader");
        }

        /// <summary>
        /// A squad of four on the road, with the tether given, its leader rooted to the spot and in
        /// a fight with a Thrax that stands well out of reach of any of them, and the third of them
        /// sent at it too. Returns the squad, its leader, that member and the Thrax.
        /// </summary>
        private (S.LiveSquad, S.Member, S.Member, Creature) TetherFight(WildernessRuntimeTestHarness harness, float tether,
            uint creatureId = AfsSoldier, uint enemyId = ThraxSoldier)
        {
            var road = Road(harness);

            Squad(harness, creatureId, 0, 3, S.Formation.Line, road, S.PathMode.Loop);
            S.Types[1].EngageRange = 0.5f;
            S.Types[1].Tether = tether;
            Step(harness, 250);
            var squad = S.On(harness.Map)[1];

            for (var i = 0; i < 8; i++)
                Step(harness, 250);

            var leader = squad.Leader;
            var member = squad.Members[3];
            Root(leader.Creature);

            // Out of reach of the member's attacks from anywhere on a tether of 12 m.
            var reach = member.Creature.Actions.Select(action => (float)action.RangeMax).DefaultIfEmpty(0).Max();
            var thrax = harness.Creatures.CreateCreature(enemyId, null);
            var at = harness.Map.NavMesh.Nearest(leader.Creature.Position + new Vector3(12f + reach + 8f, 0, 0)) ?? throw new AssertFailedException("no navmesh there");
            Assert.IsTrue(Flat(at - leader.Creature.Position) > 12f + reach + 4f, $"the Thrax out of reach: {Flat(at - leader.Creature.Position):0.0} m off, reach {reach}");
            CreatureManager.Instance.SetLocation(thrax, at, 0, 1220);
            thrax.AggroRange = 0.5f;
            CellManager.Instance.AddToWorld(harness.Map, thrax);
            PinAt(thrax, at);
            harness.MoveTo(Start);

            foreach (var fighter in new[] { leader, member })
            {
                fighter.Creature.Hate.Ensure(thrax.EntityId, Threat.NoticedThreat);
                BehaviorManager.Instance.SetActionFighting(fighter.Creature, thrax.EntityId);
            }

            S.Worker(harness.Map);
            Assert.AreEqual(S.Phase.Fighting, squad.Phase);

            return (squad, leader, member, thrax);
        }

        /// <summary>
        /// The creature cannot move - rooted, stunned - so a fight it cannot press does not wear out
        /// (BehaviorManager.Stalled); nor charge, which carries it to its target whatever its speed
        /// (KaelRushingBlow), so it is given no attacks.
        /// </summary>
        private static void Root(Creature creature)
        {
            creature.WalkSpeed = creature.RunSpeed = 0;
            creature.MovementSpeed = 0;
            creature.Actions.Clear();
        }

        /// <summary>The creature stands where it is put and does nothing.</summary>
        private static void PinAt(Creature creature, Vector3 at)
        {
            creature.Actions.Clear();
            creature.WalkSpeed = creature.RunSpeed = 0;
            creature.Position = at;
        }

        #endregion

        #region The bystander leader

        [TestMethod]
        public void TheBystanderFlagGoesInAtNoneForTheTypesThereAreAndDownTakesItOut()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(BeforeBystander);
            Assert.IsFalse(HasColumn(database, "npc_squad_type", "leader_bystander"));
            var types = Count(database, "SELECT COUNT(*) FROM npc_squad_type");
            Assert.IsTrue(types > 0, "Alia Das's squad is there to be given one");

            migrator.Migrate(Latest);
            Assert.AreEqual(types, Count(database, "SELECT COUNT(*) FROM npc_squad_type WHERE leader_bystander = 0"), "none: as they went before");

            migrator.Migrate(BeforeBystander);
            Assert.IsFalse(HasColumn(database, "npc_squad_type", "leader_bystander"));
            Assert.AreEqual(types, Count(database, "SELECT COUNT(*) FROM npc_squad_type"));

            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var mysql = context.GetService<IMigrator>();

            StringAssert.Contains(mysql.GenerateScript(BeforeBystander, Latest), "ADD `leader_bystander` tinyint unsigned NOT NULL DEFAULT 0");
            StringAssert.Contains(mysql.GenerateScript(Latest, BeforeBystander), "DROP COLUMN `leader_bystander`");
        }

        [TestMethod]
        public void AGmMakesASquadTypesLeaderABystanderAndItIsSavedAndExported()
        {
            using var harness = Create();

            Say(harness, ".squad type new Escort");
            Say(harness, $".squad type 1 add {AfsSoldier}");
            Assert.IsFalse(Say(harness, ".squad type 1").First().Contains("bystander"));

            var said = Say(harness, ".squad type 1 bystander on").Single();
            Assert.IsFalse(said.Contains("NOT SAVED"), said);
            StringAssert.Contains(said, "its leader a bystander");
            Assert.IsTrue(S.Types[1].LeaderBystander);

            var world = harness.World;
            world.ChangeTracker.Clear();
            Assert.AreEqual(1, world.NpcSquadTypeEntries.AsNoTracking().Single().LeaderBystander);

            // The export gives it back.
            var sql = S.ExportSql(0);
            StringAssert.Contains(sql, "engage_range, tether, leader_bystander) VALUES (1, 'Escort', 0, 3, 0, 0, 1);");

            foreach (var table in Tables)
                world.Database.ExecuteSqlRaw($"DELETE FROM {table}");
            foreach (var line in sql.Split('\n').Where(line => line.StartsWith("INSERT")))
                world.Database.ExecuteSqlRaw(line);

            S.Init(harness);
            Assert.IsTrue(S.Types[1].LeaderBystander);

            Say(harness, ".squad type 1 bystander off");
            Assert.IsFalse(S.Types[1].LeaderBystander);

            foreach (var bad in new[] { ".squad type 1 bystander yes", ".squad type 1 bystander" })
            {
                StringAssert.Contains(Say(harness, bad).Single(), "bystander on|off", bad);
                Assert.IsFalse(S.Types[1].LeaderBystander, bad);
            }
        }

        [TestMethod]
        public void ABystanderLeaderTakesUpNoFightAndStandsWhileHisMembersFightByHimAndTheNextLeaderFights()
        {
            using var harness = Create();
            var road = Road(harness);

            Squad(harness, AfsSoldier, 0, 3, S.Formation.Line, road, S.PathMode.Loop);
            Say(harness, ".squad type 1 bystander on");
            S.Types[1].EngageRange = 0.5f;
            Step(harness, 250);
            var squad = S.On(harness.Map)[1];

            for (var i = 0; i < 8; i++)
                Step(harness, 250);

            var leader = squad.Leader;
            Assert.IsTrue(S.IsBystander(leader.Creature), "the leader it was sent with");
            Assert.IsTrue(squad.Members.Where(m => m != leader).All(m => !S.IsBystander(m.Creature)), "his members are none");

            // A Thrax by them, which does nothing; one of the members takes it on.
            var thrax = harness.Creatures.CreateCreature(ThraxSoldier, null);
            var at = harness.Map.NavMesh.Nearest(leader.Creature.Position + new Vector3(6f, 0, 0)) ?? throw new AssertFailedException("no navmesh there");
            CreatureManager.Instance.SetLocation(thrax, at, 0, 1220);
            thrax.AggroRange = 0.5f;
            CellManager.Instance.AddToWorld(harness.Map, thrax);
            PinAt(thrax, at);
            thrax.Attributes[Attributes.Health].Current = thrax.Attributes[Attributes.Health].CurrentMax = thrax.Attributes[Attributes.Health].NormalMax = 1_000_000;
            harness.MoveTo(Start);

            var member = squad.Members[1];
            member.Creature.Hate.Ensure(thrax.EntityId, Threat.NoticedThreat);
            BehaviorManager.Instance.SetActionFighting(member.Creature, thrax.EntityId);
            var stood = leader.Creature.Position;

            Step(harness, 250);
            Step(harness, 250);

            Assert.AreEqual(S.Phase.Fighting, squad.Phase);
            Assert.IsTrue(squad.Members.Where(m => m != leader).All(m => m.Creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting &&
                m.Creature.Controller.ActionFighting.TargetEntityId == thrax.EntityId), "his members on it");
            Assert.AreEqual(BehaviorManager.BehaviorActionSquad, leader.Creature.Controller.CurrentAction, "he is not taken in");
            Assert.IsTrue(Flat(leader.Creature.Position - stood) < 1f, $"he stands: {Flat(leader.Creature.Position - stood):0.0} m on");

            // Nor by a hit, nor by his own scan, nor by anything else.
            Threat.FromDamage(leader.Creature, thrax, 10);
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, leader.Creature.Controller.CurrentAction, "a hit");
            Assert.IsFalse(S.MayTakeUp(leader.Creature, thrax.EntityId), "his scan");
            Assert.IsFalse(BehaviorManager.Instance.TrySetActionFighting(leader.Creature, thrax.EntityId), "anything else");

            // He falls: the next of them leads, and is no bystander.
            Kill(leader.Creature);
            Step(harness, 250);
            Assert.AreNotSame(leader, squad.Leader);
            Assert.IsFalse(S.IsBystander(squad.Leader.Creature));
            Assert.IsTrue(S.MayTakeUp(squad.Leader.Creature, thrax.EntityId));
        }

        [TestMethod]
        public void ABystanderLeaderLeftAloneWalksOnAndHisSquadIsSentAgainInFullOnItsTime()
        {
            using var harness = Create();
            var road = Road(harness);

            Squad(harness, AfsSoldier, 3, 3, S.Formation.Line, road, S.PathMode.Loop);
            Say(harness, ".squad type 1 bystander on");
            Step(harness, 250);
            var squad = S.On(harness.Map)[1];
            var leader = squad.Leader;
            Assert.AreEqual(4, squad.Living.Count());

            // His members fall (to-fix Pools #1: a bot killed Nakrinx's four guards and none came back while he lived).
            foreach (var member in squad.Members.Where(m => m != leader).ToList())
                Kill(member.Creature);

            Step(harness, 250);
            var from = leader.Creature.Position;
            Step(harness, 10_000);
            Assert.IsTrue(Flat(leader.Creature.Position - from) > 5f, $"he walks on alone: {Flat(leader.Creature.Position - from):0.0} m");
            Step(harness, 109_000);

            Assert.AreSame(leader, squad.Leader, "he is not sent again before its time");
            Assert.AreEqual(1, squad.Living.Count());
            Assert.IsTrue(Say(harness, ".squad").Any(line => line.Contains("1 out, sent again in")), "a GM sees when");

            Step(harness, 1_000);

            Assert.AreNotSame(leader, squad.Leader, "the squad, he too, is sent again");
            Assert.IsNull(leader.Creature.SquadMember, "the old leader is taken away");
            Assert.AreEqual(4, squad.Living.Count(), "in full");
            Assert.IsTrue(S.IsBystander(squad.Leader.Creature), "its new leader a bystander");
            Assert.IsTrue(squad.Living.Where(m => m != squad.Leader).All(m => !S.IsBystander(m.Creature)));
            Assert.IsTrue(Flat(squad.Leader.Creature.Position - road[0]) < 2f, $"at the spawn: {Flat(squad.Leader.Creature.Position - road[0]):0.0} m off");

            // While one of his members stands it is not sent again.
            var again = squad.Leader;
            foreach (var member in squad.Members.Where(m => m != again).Skip(1).ToList())
                Kill(member.Creature);
            Step(harness, 130_000);
            Assert.AreSame(again, squad.Leader);
            Assert.AreEqual(2, squad.Living.Count());
        }

        private static bool HasColumn(OrdinaryLootTestDatabase database, string table, string column) =>
            Count(database, $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = '{column}'") == 1;

        #endregion

        #region Helpers

        private WildernessRuntimeTestHarness Create()
        {
            var harness = WildernessRuntimeTestHarness.Create();
            try
            {
                // The squads migrations put in (AliaDasAfsSquad) out of the way: these tests make their own.
                foreach (var table in Tables)
                    harness.World.Database.ExecuteSqlRaw($"DELETE FROM {table}");

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
