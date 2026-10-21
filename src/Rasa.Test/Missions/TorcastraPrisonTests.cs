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
using Rasa.Packets.MapChannel.Client;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;
using Rasa.Test.Database;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions
{
    using T = Rasa.Services.Preloader.TorcastraPrisonContent;
    using M = Rasa.Services.Preloader.Missions.Torcastra.TorcastraMissionsV1;
    using P = Rasa.Managers.TorcastraPrison;

    /// <summary>
    /// Torcastra Prison as the wiki's walkthrough tells it (Torcastra_prison: TorcastraPrisonContent,
    /// TorcastraMissionsV1; the Game's TorcastraPrison): its rows, the trip wire's alarm, the
    /// bunkers and the air strike, the gate, Ranger Ferme, the cell block consoles and Prison Riot,
    /// and Airman Hamilton either way.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class TorcastraPrisonTests
    {
        private const string Migration = "20270307000000_Torcastra_prison";
        private const string Previous = "20270306000000_Wilderness_urn_enigma_regions";

        private static readonly string[] Tables =
        {
            "creature", "creature_action", "creature_stat", "creature_appearance", "npc_package", "spawnpool",
            "world_usable", "world_destructible", "mission_content_definition", "mission_objective_definition",
            "mission_trigger", "mission_action", "mission_scene_binding", "mission_channel_policy"
        };

        private long _now = 1_000_000;

        [TestInitialize]
        public void Clock()
        {
            P.Reset();
            P.Now = () => _now;
        }

        [TestCleanup]
        public void Restore()
        {
            P.Reset();
            P.Now = () => Environment.TickCount64;
            PlacedUsables.Load(null);
            PlacedUsables.Reset();
            PlacedDestructibles.Load(null);
            PlacedDestructibles.Reset();
        }

        #region The rows

        [TestMethod]
        public void ThePrisonGoesInAndDownTakesItOutExactly()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Tables.ToDictionary(table => table, table => Dump(database, table));

            migrator.Migrate(Migration);

            Assert.AreEqual(T.Npcs.Length + T.Combatants.Length, Count(database, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 660001 AND 660023"));
            Assert.AreEqual(T.Combatants.Length, Count(database, "SELECT COUNT(*) FROM creature_stat WHERE id BETWEEN 660001 AND 660023"));
            Assert.AreEqual(T.Pools.Length, Count(database, "SELECT COUNT(*) FROM spawnpool WHERE id BETWEEN 660001 AND 660999"));
            Assert.AreEqual(T.Usables.Length, Count(database, "SELECT COUNT(*) FROM world_usable WHERE id BETWEEN 660001 AND 660999 AND map_context_id = 1349"));
            Assert.AreEqual(T.Destructibles.Length, Count(database, "SELECT COUNT(*) FROM world_destructible WHERE id BETWEEN 660001 AND 660999 AND map_context_id = 1349"));
            Assert.AreEqual(7 + 6 + 5 * 7, T.Usables.Length, "seven usables, six lights and five consoles of seven panels");

            // The Divide seed's placeholder Thrax asleep, the courtyard waiting for the gate, the mortars BaneMortars' still.
            Assert.AreEqual(0, Count(database, "SELECT COUNT(*) FROM spawnpool WHERE id BETWEEN 530059 AND 530079 AND mode <> 2"));
            Assert.AreEqual(4, Count(database, $"SELECT COUNT(*) FROM spawnpool WHERE id IN ({string.Join(", ", T.Bunkers.Select(bunker => bunker.MortarPool))}) AND creature_1_Id = 592002"));
            Assert.AreEqual(2, Count(database, "SELECT COUNT(*) FROM spawnpool WHERE id IN (500169, 500170) AND mode = 2"));
            Assert.AreEqual(1, Count(database, "SELECT COUNT(*) FROM spawnpool WHERE id = 510120 AND ABS(pos_x + 72) < 0.01 AND ABS(pos_z + 648) < 0.01"));
            Assert.AreEqual(1, Count(database, $"SELECT COUNT(*) FROM npc_package WHERE id = 510120 AND package_id = {T.HamiltonPackage}"));

            // The five enabled; Stolen History authored and waiting for its other two pieces.
            Assert.AreEqual(5, Count(database, $"SELECT COUNT(*) FROM mission_content_definition WHERE content_revision = '{M.Revision}' AND enabled = 1"));
            Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) FROM mission_content_definition WHERE mission_id = 1860 AND enabled = 1"));

            // The world's rows a second time: nothing.
            foreach (var sql in T.Up())
                Assert.AreEqual(0, database.Context.Database.ExecuteSqlRaw(sql), sql);

            migrator.Migrate(Previous);

            foreach (var table in Tables)
                CollectionAssert.AreEqual(before[table], Dump(database, table), $"Down puts {table} back as it was");
        }

        [TestMethod]
        public void OnMySqlThePrisonGoesInAndComesOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in T.Up().Concat(M.Up()))
                StringAssert.Contains(up, sql);
            foreach (var sql in M.Down().Concat(T.Down()))
                StringAssert.Contains(down, sql);
        }

        [TestMethod]
        public void TheFiveMissionsAreOperationalWithTheClientsMarkers()
        {
            using var harness = Create();

            foreach (var missionId in M.Enabled)
            {
                Assert.IsTrue(harness.Manager.TryGetOperationalMission(missionId, out var mission), $"mission {missionId}");
                Assert.AreEqual(M.Category, mission.CategoryId);
            }

            Assert.IsFalse(harness.Manager.TryGetOperationalMission(M.StolenHistory, out _), "Stolen History waits for Minos Caverns and Timora Mines");

            // Each marker is the client's own name for it, or none and named by its objective.
            var markers = harness.World.MissionIndicatorEntries.AsNoTracking().Where(row => row.ContentRevision == M.Revision).ToList();
            Assert.IsTrue(markers.Count > 20);
            Assert.IsTrue(markers.All(row => new[] { M.PrisonCellSwitch, M.BaneTerminal, M.BaneDataServers, M.OverseerTorquaName, M.CellBlock }.Contains(row.IndicatorId) ||
                row.IndicatorId > Rasa.Structures.MissionIndicator.UnnamedFrom), string.Join(", ", markers.Select(row => row.IndicatorId)));
        }

        [TestMethod]
        public void EveryPoolOfThePrisonStandsOnItsNavmesh()
        {
            using var harness = Create();

            foreach (var pool in T.Pools.Where(pool => pool.MapContextId == T.Map))
            {
                var ground = harness.Map.NavMesh.GroundHeight(new Vector3((float)pool.X, (float)pool.Y + 0.5f, (float)pool.Z));
                Assert.IsNotNull(ground, $"pool {pool.Id} ({pool.Comment})");
                Assert.AreEqual(pool.Y, ground.Value, 1.0, $"pool {pool.Id} ({pool.Comment})");
            }
        }

        #endregion

        #region The alarm, the bunkers and the strike

        [TestMethod]
        public void CrossingTheTripWireRaisesTheAlarmAndCallsTheStalker()
        {
            using var harness = Create();
            var state = Enter(harness);

            harness.MoveTo(new Vector3(-62f, 46.7f, 3f));
            Work(harness);
            Assert.AreEqual(P.Alarm.Armed, state.Alarm, "north of the wire");

            harness.MoveTo(new Vector3(-62f, 46.7f, -4f));
            Work(harness);

            Assert.AreEqual(P.Alarm.Raised, state.Alarm);
            Assert.AreEqual(_now + M.AlarmSeconds * 1000L, state.StrikeAt);
            Assert.IsTrue(T.AlarmLights.All(id => PlacedUsables.LiveOf(harness.Map, id).State == UseObjectState.TsState1), "the lights are on");
            Assert.IsTrue(Pool(harness, T.StalkerPool).Woken, "the Stalker is called");
        }

        [TestMethod]
        public void ShootingTheBeaconFirstKeepsTheStalkerAway()
        {
            using var harness = Create();
            var state = Enter(harness);

            Assert.IsTrue(PlacedDestructibles.Destroy(PlacedDestructibles.LiveOf(harness.Map, T.SecurityBeacon), harness.Client.Player));
            harness.MoveTo(new Vector3(-62f, 46.7f, 3f));
            Work(harness);
            harness.MoveTo(new Vector3(-62f, 46.7f, -4f));
            Work(harness);

            Assert.IsTrue(state.BeaconDown);
            Assert.AreEqual(P.Alarm.Raised, state.Alarm);
            Assert.IsFalse(Pool(harness, T.StalkerPool).Woken);

            // And it stays down.
            Work(harness, 120_000);
            PlacedDestructibles.Worker(harness.Map);
            Assert.IsTrue(PlacedDestructibles.LiveOf(harness.Map, T.SecurityBeacon).IsDown);
        }

        [TestMethod]
        public void FourBunkersSecuredStandTheAlarmDownAndOpenTheWayToTheGatekeeper()
        {
            using var harness = Create();
            var state = Enter(harness);
            var cisco = Spawn(harness, T.CiscoEntrancePool, T.CiscoAtEntrance);

            WildernessTwinPillarsTests.Accept(harness, cisco, M.Securing);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.Securing, 6));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, M.Securing, 9));

            Trip(harness);
            Work(harness, 1100);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Securing, 6), "the alarm");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.Securing, 9));

            // Three bunkers: three objectives, and the alarm still raised.
            foreach (var bunker in T.Bunkers.Take(3))
                Clear(harness, bunker);
            Work(harness, 1100);
            Work(harness, 1100);
            foreach (var bunker in T.Bunkers.Take(3))
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Securing, bunker.Objective), bunker.Name);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.Securing, 9));
            Assert.AreEqual(P.Alarm.Raised, state.Alarm);

            Clear(harness, T.Bunkers[3]);
            Work(harness, 1100);
            Work(harness, 1100);

            Assert.AreEqual(P.Alarm.StoodDown, state.Alarm);
            Assert.IsTrue(T.AlarmLights.All(id => PlacedUsables.LiveOf(harness.Map, id).State == UseObjectState.TsState0), "the lights are out");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Securing, 9));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.Securing, 3), "the gate key");

            // No strike when the time comes.
            harness.MoveTo(new Vector3(-62f, 46.7f, -30f));
            Work(harness, M.AlarmSeconds * 1000L + 1);
            Assert.AreEqual(0, state.Strikes);
        }

        [TestMethod]
        public void TheStrikeKillsThoseOnThePlateauFailsTheAlarmAndSetsTheBunkersBack()
        {
            using var harness = Create();
            var state = Enter(harness);
            var cisco = Spawn(harness, T.CiscoEntrancePool, T.CiscoAtEntrance);

            WildernessTwinPillarsTests.Accept(harness, cisco, M.Securing);
            Trip(harness);
            Work(harness, 1100);
            Clear(harness, T.Bunkers[0]);
            Work(harness, 1100);
            Assert.IsTrue(state.Secured[0]);

            harness.MoveTo(new Vector3(-80f, 50f, -60f));
            Work(harness, M.AlarmSeconds * 1000L);
            Assert.AreEqual(1, state.Strikes);
            Assert.AreEqual(P.Alarm.Armed, state.Alarm, "the wire is set again");
            Assert.IsFalse(state.Secured[0], "the bunkers come back");
            Assert.AreEqual(UseObjectState.TsState1, PlacedUsables.LiveOf(harness.Map, T.TripWire).State);
            Assert.IsTrue(!harness.Client.Player.Missions.TryGetValue(M.Securing, out var log) || log.State == MissionState.Failed,
                "Securing Torcastra is failed");

            Work(harness, P.StrikeFallMs);
            Assert.AreEqual(0, harness.Client.Player.Attributes[Attributes.Health].Current, "the bombs kill");

            // A bunker set back is secured only once it has come back and been killed again.
            Work(harness, 1100);
            Assert.IsFalse(state.Secured[0]);
        }

        #endregion

        #region The gate

        [TestMethod]
        public void TheGatekeepersKeyTakesTheFieldDownAndBringsCiscoUp()
        {
            using var harness = Create();
            var state = Enter(harness);
            var field = PlacedDestructibles.LiveOf(harness.Map, T.GateField).Field;

            Assert.IsNotNull(field);
            Assert.IsTrue(field.Sealed);
            Assert.IsFalse(ForceFields.MayShoot(field, harness.Client.Player), "the field cannot be shot down");

            var slot = PlacedUsables.LiveOf(harness.Map, T.GateKeySlot);
            Use(harness, slot.Object);
            Assert.IsFalse(state.GateOpen, "not without the key");

            Give(harness, T.GateKeyTemplate);
            Use(harness, slot.Object);

            Assert.IsTrue(state.GateOpen);
            Assert.IsTrue(field.IsDestroyed);
            Assert.AreEqual(0, WildernessTwinPillarsTests.Held(harness, T.GateKeyTemplate), "the key is taken");
            Assert.IsTrue(Pool(harness, T.CiscoGatePool).Woken);
            Assert.IsTrue(T.CourtyardPools.All(id => Pool(harness, id).Woken));
            Assert.IsTrue(Pool(harness, T.JuggernautPool).Woken);

            // It stays down.
            Work(harness, 120_000);
            PlacedDestructibles.Worker(harness.Map);
            Assert.IsTrue(field.IsDestroyed);
        }

        #endregion

        #region Ranger Ferme

        [TestMethod]
        public void FermesCellOpensOnceHerInterrogatorsAreDead()
        {
            using var harness = Create();
            var state = Enter(harness);
            var otoole = Put(harness, T.OToole, new Vector3(-262f, 13.7f, 306f));

            WildernessTwinPillarsTests.Accept(harness, otoole, M.InTerror);

            var console = PlacedUsables.LiveOf(harness.Map, T.FermeCellConsole);
            Use(harness, console.Object);
            Assert.IsFalse(state.CellOpen, "not while they stand");

            Pool(harness, T.InterrogatorPool).HasSpawned = true;
            Work(harness, 1100);
            Work(harness, 1100);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.InTerror, 1));

            Use(harness, console.Object);
            Assert.IsTrue(state.CellOpen);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.InTerror, 2));

            var ferme = Spawn(harness, T.FermePool, T.Ferme);
            WildernessTwinPillarsTests.Reward(harness, ferme, M.InTerror, 0, 13197, 17000, 2550);
        }

        #endregion

        #region The cell blocks and Prison Riot

        [TestMethod]
        public void AConsoleShowsItsSequenceAndAWrongPanelCallsSecurity()
        {
            using var harness = Create();
            var state = Enter(harness);
            var block = state.Blocks[0];

            harness.MoveTo(block.Console + new Vector3(2, 0, 0));
            Work(harness);
            Assert.AreEqual(P.PuzzlePhase.Showing, block.Phase);

            // The two first, together.
            Work(harness);
            Assert.AreEqual(UseObjectState.TsState1, Panel(harness, 1, 2).State);
            Assert.AreEqual(UseObjectState.TsState1, Panel(harness, 1, 7).State);
            Assert.AreEqual(UseObjectState.TsState0, Panel(harness, 1, 1).State);

            // Not to be pressed while it shows.
            Use(harness, Panel(harness, 1, 1).Object);
            Assert.AreEqual(0, block.Pressed.Count);

            Work(harness, 20_000);
            Assert.AreEqual(P.PuzzlePhase.Input, block.Phase);

            Use(harness, Panel(harness, 1, 1).Object);
            Assert.IsTrue(block.Pressed.Contains(1));
            Use(harness, Panel(harness, 1, 2).Object);

            Assert.AreEqual(P.PuzzlePhase.Idle, block.Phase);
            Assert.IsTrue(Pool(harness, block.Cells.SecurityPool).Woken, "Bane security");
            Assert.IsTrue(Enumerable.Range(1, 7).All(panel => Panel(harness, 1, (uint)panel).State == UseObjectState.TsState0));
        }

        [TestMethod]
        public void TheSequenceUnlocksTheBlockFreesItsPrisonersAndCountsForPrisonRiot()
        {
            using var harness = Create();
            var state = Enter(harness);
            Completed(harness, M.Sabotage);

            // Offered by radio here to whoever has done Information Sabotage.
            Work(harness, 1100);
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, M.Riot));
            harness.Drain();

            foreach (var number in new uint[] { 1, 4 })
            {
                var block = state.Blocks[number - 1];
                var sequence = P.SequenceOf(number);

                harness.MoveTo(block.Console + new Vector3(-block.Cells.Side * 2f, 0, 0));
                Work(harness);
                Work(harness, 20_000);
                Assert.AreEqual(P.PuzzlePhase.Input, block.Phase, $"block {number}");

                // The last too soon starts it over.
                Use(harness, Panel(harness, number, sequence.Last).Object);
                Assert.AreEqual(P.PuzzlePhase.Idle, block.Phase);

                Work(harness, P.ConsoleRestartMs + 1);
                Work(harness, 20_000);
                foreach (var panel in sequence.Press.Reverse())
                    Use(harness, Panel(harness, number, panel).Object);
                Use(harness, Panel(harness, number, sequence.Last).Object);

                Assert.AreEqual(P.PuzzlePhase.Unlocked, block.Phase, $"block {number}");
                Assert.IsTrue(block.Cells.PrisonerPools.All(id => Pool(harness, id).Quieted), "its prisoners go free");
                Assert.IsTrue(Enumerable.Range(1, 7).All(panel => Panel(harness, number, (uint)panel).State == UseObjectState.TsState1));

                Work(harness, 1100);
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Riot, number), $"block {number}");
            }

            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.Riot, 2));
        }

        #endregion

        #region Airman Hamilton

        [TestMethod]
        public void HamiltonTakenInFollowsThePlayerToCiscoAndMatlinHearsOfIt()
        {
            using var harness = Create();
            var (cisco, hamilton) = ToHamilton(harness);
            var state = P.StateOf(harness.Map);

            WildernessTwinPillarsTests.OpenNativeNpcConversation(harness, hamilton);
            Assert.IsTrue(harness.Manager.TryPerformNpcChoice(harness.Client, hamilton.EntityId, M.Keymaster, 11, 1, 1));
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.Keymaster, 8));

            Work(harness);
            Assert.AreEqual(harness.Client.Player.Id, state.HamiltonWith);
            Assert.AreEqual(harness.Client.Player.Id, hamilton.SpawnPool.FollowOwnerCharacterId);

            CreatureManager.Instance.SetLocation(hamilton, cisco.Position + new Vector3(3, 0, 0), 0, T.Map);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Keymaster, 8));
            Assert.AreEqual(0u, state.HamiltonWith);

            WildernessTwinPillarsTests.Talk(harness, cisco, M.Keymaster, 10);
            var matlin = Put(harness, T.Matlin, cisco.Position + new Vector3(0, 0, 6));
            WildernessTwinPillarsTests.Talk(harness, matlin, M.Keymaster, 2);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Keymaster, 6), "either report");
            WildernessTwinPillarsTests.Reward(harness, matlin, M.Keymaster, 0, 13077, 34000, 2250);
        }

        [TestMethod]
        public void HamiltonNotTakenInIsKilledAndCiscoHearsOfIt()
        {
            using var harness = Create();
            var (cisco, hamilton) = ToHamilton(harness);

            WildernessTwinPillarsTests.OpenNativeNpcConversation(harness, hamilton);
            Assert.IsTrue(harness.Manager.TryPerformNpcChoice(harness.Client, hamilton.EntityId, M.Keymaster, 11, 1, 2));
            harness.Drain();

            Work(harness);
            Assert.AreEqual(CharacterState.Dead, hamilton.State);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Keymaster, 7));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, M.Keymaster, 8));

            WildernessTwinPillarsTests.Talk(harness, cisco, M.Keymaster, 9);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.Keymaster, 13));
        }

        /// <summary>Securing Torcastra done, Keymaster taken from Cisco at the gate, the computer banks read and Hamilton released.</summary>
        private (Creature Cisco, Creature Hamilton) ToHamilton(WildernessRuntimeTestHarness harness)
        {
            Enter(harness);
            Completed(harness, M.Securing);
            Pool(harness, T.CiscoGatePool).Woken = true;
            var cisco = Spawn(harness, T.CiscoGatePool, T.CiscoAtGate);

            WildernessTwinPillarsTests.Accept(harness, cisco, M.Keymaster);

            var banks = harness.Map.DynamicObjects.SingleOrDefault(obj => obj.SceneMissionId == M.Keymaster && obj.SceneActorRole == "computer-banks");
            Assert.IsNotNull(banks, "the computer banks");
            harness.MoveTo(banks.Position + new Vector3(2, 0, 0));
            harness.Drain();
            new NpcManager(harness, harness.Manager).RequestNpcConverse(harness.Client,
                new RequestNPCConversePacket { EntityId = banks.EntityId, ActionId = ActionId.Converse, ActionArgId = 1 });
            new NpcManager(harness, harness.Manager).CompleteNPCObjective(harness.Client,
                new CompleteNPCObjectivePacket { EntityId = banks.EntityId, MissionId = M.Keymaster, ObjectiveId = 4, PlayerFlagId = 1 });
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Keymaster, 4));

            var restraints = PlacedUsables.LiveOf(harness.Map, T.ResearchWardConsole);
            Use(harness, restraints.Object);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.Keymaster, 5), "not without the Warden's pass key");

            // The Warden dead, his pass key; once.
            Pool(harness, T.WardenPool).HasSpawned = true;
            Work(harness, 1100);
            Work(harness, 1100);
            Assert.AreEqual(1, WildernessTwinPillarsTests.Held(harness, T.AccessKeyTemplate));

            Use(harness, restraints.Object);
            Assert.AreEqual(0, WildernessTwinPillarsTests.Held(harness, T.AccessKeyTemplate), "the restraints take it");
            Work(harness, 1100);
            Assert.AreEqual(0, WildernessTwinPillarsTests.Held(harness, T.AccessKeyTemplate), "and no second");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Keymaster, 5));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.Keymaster, 11));

            var hamilton = Spawn(harness, T.Hamilton, T.Hamilton);
            Assert.IsTrue(Vector3.Distance(hamilton.Position, new Vector3((float)T.HamiltonX, (float)T.HamiltonY, (float)T.HamiltonZ)) < 3, "in the research ward");
            return (cisco, hamilton);
        }

        #endregion

        #region Helpers

        private static WildernessRuntimeTestHarness Create()
        {
            var harness = WildernessRuntimeTestHarness.Create(mapContextId: T.Map);
            try
            {
                using var unit = harness.CreateChar();
                unit.ExecuteTransaction(() => unit.CharacterStartingExperience.Add(
                    new CharacterStartingExperienceEntry(harness.Client.Player.Id, M.Revision,
                        CharacterStartingExperienceState.Skipped)));
                harness.Client.Player.StartingExperienceCompleted = true;
                harness.Drain();
                return harness;
            }
            catch
            {
                harness.Dispose();
                throw;
            }
        }

        /// <summary>The prison's usables and destructibles down on the copy, as its coming up puts them, and its state.</summary>
        private P.State Enter(WildernessRuntimeTestHarness harness)
        {
            PlacedUsables.Load(harness.World.Set<WorldUsableEntry>().AsNoTracking().Where(row => row.MapContextId == T.Map).ToList());
            PlacedUsables.Place(harness.Map);
            PlacedDestructibles.Load(harness.World.Set<WorldDestructibleEntry>().AsNoTracking().Where(row => row.MapContextId == T.Map).ToList());
            PlacedDestructibles.Place(harness.Map);

            Work(harness);
            return P.StateOf(harness.Map);
        }

        private void Work(WildernessRuntimeTestHarness harness, long ms = 100)
        {
            _now += ms;
            P.Worker(harness.Map);
            harness.Drain();
        }

        private void Trip(WildernessRuntimeTestHarness harness)
        {
            harness.MoveTo(new Vector3(-62f, 46.7f, 3f));
            Work(harness);
            harness.MoveTo(new Vector3(-62f, 46.7f, -4f));
            Work(harness);
            Assert.AreEqual(P.Alarm.Raised, P.StateOf(harness.Map).Alarm);
        }

        /// <summary>A bunker's crew and mortar spawned and killed.</summary>
        private static void Clear(WildernessRuntimeTestHarness harness, T.Bunker bunker)
        {
            foreach (var id in bunker.CrewPools.Append(bunker.MortarPool))
                Pool(harness, id).HasSpawned = true;
        }

        private static SpawnPool Pool(WildernessRuntimeTestHarness harness, uint id) => harness.Map.SpawnPools.Single(pool => pool.DbId == id);

        private static PlacedUsables.Live Panel(WildernessRuntimeTestHarness harness, uint block, uint panel) =>
            PlacedUsables.LiveOf(harness.Map, T.PanelId(block, panel));

        private static Creature Spawn(WildernessRuntimeTestHarness harness, uint poolId, uint creatureId)
        {
            harness.SpawnWorld(poolId);
            return WildernessTwinPillarsTests.RequireNpc(harness, poolId, creatureId);
        }

        private static Creature Put(WildernessRuntimeTestHarness harness, uint dbId, Vector3 position)
        {
            var creature = CreatureManager.Instance.CreateCreature(dbId, null);
            CreatureManager.Instance.SetLocation(creature, NavMeshManager.SnapToGround(harness.Map, position), 0, T.Map);
            CellManager.Instance.AddToWorld(harness.Map, creature);
            return creature;
        }

        private static void Give(WildernessRuntimeTestHarness harness, uint template)
        {
            var item = ItemManager.Instance.CreateFromTemplateId(template, 1);
            Assert.IsNotNull(InventoryManager.Instance.GrantItemToInventory(harness.Client, item), $"template {template}");
            harness.Drain();
        }

        private static void Use(WildernessRuntimeTestHarness harness, DynamicObject obj)
        {
            harness.MoveTo(obj.Position + new Vector3(1, 0, 0));
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId,
                ActionId = ActionId.UseObject,
                ActionArgId = DynamicObjectManager.FootlockerUseArgId
            });
            ActorActionManager.Instance.DoWork(harness.Map, 10000);
            harness.Drain();
        }

        private static MissionObjectiveState Objective(WildernessRuntimeTestHarness harness, uint missionId, uint objectiveId) =>
            harness.Client.Player.Missions[missionId].Objectives[objectiveId].State;

        /// <summary>The mission done before, as a history row the server keeps.</summary>
        private static void Completed(WildernessRuntimeTestHarness harness, uint missionId)
        {
            using var unit = harness.CreateChar();
            unit.ExecuteTransaction(() => unit.CharacterMissions.Runtime.Archive(
                new CharacterMissionEntry(harness.Client.Player.Id, missionId, (uint)MissionState.Completed)
                {
                    AssignmentId = Guid.NewGuid().ToString("N"),
                    Generation = 1,
                    ContentRevision = M.Revision
                }, harness.UtcNow));
            harness.Client.Player.MissionHistory[missionId] = MissionState.Completed;
            harness.Client.Player.MissionSuccessHistory.Add(missionId);
        }

        private static long Count(OrdinaryLootTestDatabase database, string sql)
        {
            using var command = database.Context.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            database.Context.Database.OpenConnection();
            return Convert.ToInt64(command.ExecuteScalar());
        }

        private static List<string> Dump(OrdinaryLootTestDatabase database, string table)
        {
            using var command = database.Context.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"SELECT * FROM {table} ORDER BY 1, 2";
            database.Context.Database.OpenConnection();
            using var reader = command.ExecuteReader();
            var rows = new List<string>();

            while (reader.Read())
                rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => Convert.ToString(reader.GetValue(i), System.Globalization.CultureInfo.InvariantCulture))));

            return rows;
        }

        #endregion
    }
}
