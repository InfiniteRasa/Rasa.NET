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
    using T = Rasa.Services.Preloader.TimoraMinesContent;
    using M = Rasa.Services.Preloader.Missions.Timora.TimoraMinesMissionsV1;
    using P = Rasa.Managers.TimoraMines;
    using S = Rasa.Services.Preloader.Missions.Torcastra.TorcastraMissionsV1;

    /// <summary>
    /// Timora Mines as the walkthroughs tell it (Timora_mines: TimoraMinesContent,
    /// TimoraMinesMissionsV1; the Game's TimoraMines): its rows, the drills and pumps, the Main
    /// Fuel Pump's lakes and security, Kearney's escort, the Eloh Shelter's ambush and Stolen
    /// History's pieces.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class TimoraMinesTests
    {
        private const string Migration = "20271012000000_Timora_mines";
        private const string Previous = "20271011000000_Energy_weapon_center";

        private static readonly string[] Tables =
        {
            "creature", "creature_action", "creature_stat", "creature_appearance", "spawnpool",
            "world_usable", "world_destructible", "teleporter", "local_teleporter_link",
            "mission_content_definition", "mission_objective_definition", "mission_trigger", "mission_action",
            "mission_scene_binding", "mission_indicator", "mission_reward_item"
        };

        private long _now = 1_000_000;

        [TestInitialize]
        public void Clock()
        {
            P.Reset();
            P.Now = () => _now;
            PlacedUsables.Now = () => _now;
        }

        [TestCleanup]
        public void Restore()
        {
            P.Reset();
            P.Now = () => Environment.TickCount64;
            PlacedUsables.Now = () => Environment.TickCount64;
            PlacedUsables.Load(null);
            PlacedUsables.Reset();
            PlacedDestructibles.Load(null);
            PlacedDestructibles.Reset();
        }

        #region The rows

        [TestMethod]
        public void TheMinesGoInAndDownTakesThemOutExactly()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Tables.ToDictionary(table => table, table => Dump(database, table));

            migrator.Migrate(Migration);

            Assert.AreEqual(T.Creatures.Length, Count(database, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 569001 AND 569099"));
            Assert.AreEqual(T.Actions.Length, Count(database, "SELECT COUNT(*) FROM creature_action WHERE id BETWEEN 569001 AND 569099"));
            Assert.AreEqual(T.Pools.Length, Count(database, "SELECT COUNT(*) FROM spawnpool WHERE id BETWEEN 569101 AND 569199"));
            Assert.AreEqual(T.Usables.Length, Count(database, "SELECT COUNT(*) FROM world_usable WHERE id BETWEEN 569201 AND 569299 AND map_context_id = 1348"));
            Assert.AreEqual(T.Destructibles.Length, Count(database, "SELECT COUNT(*) FROM world_destructible WHERE id BETWEEN 569301 AND 569399 AND map_context_id = 1348"));
            Assert.AreEqual(5 + 3 * 3 + 1, T.Usables.Length, "five charges, a field, a fire and a console a lake, the cipher's crate");
            Assert.AreEqual(5 + 9 + 1 + 3, T.Destructibles.Length, "five drills, nine pumps, the Main Fuel Pump and its three lakes");
            Assert.AreEqual(1, Count(database, "SELECT COUNT(*) FROM local_teleporter_link WHERE from_teleporter_id = 946 AND to_teleporter_id = 947"));

            // The placeholders asleep, Kearney and Pastre off the hospital.
            Assert.AreEqual(0, Count(database, "SELECT COUNT(*) FROM spawnpool WHERE id BETWEEN 530051 AND 530058 AND mode <> 2"));
            Assert.AreEqual(1, Count(database, "SELECT COUNT(*) FROM spawnpool WHERE id = 510118 AND ABS(pos_x + 136.9) < 0.01 AND ABS(pos_z - 518.2) < 0.01"));
            Assert.AreEqual(1, Count(database, "SELECT COUNT(*) FROM spawnpool WHERE id = 510119 AND ABS(pos_x + 142) < 0.01 AND ABS(pos_z - 517.5) < 0.01"));

            // The six enabled, and Stolen History on with its crates usable.
            Assert.AreEqual(6, Count(database, $"SELECT COUNT(*) FROM mission_content_definition WHERE content_revision = '{M.Revision}' AND enabled = 1"));
            Assert.AreEqual(1, Count(database, "SELECT COUNT(*) FROM mission_content_definition WHERE mission_id = 1860 AND enabled = 1"));
            Assert.AreEqual(1, Count(database, "SELECT COUNT(*) FROM world_usable WHERE id = 568011 AND usable = 1"));
            Assert.AreEqual(3, Count(database, "SELECT COUNT(*) FROM mission_indicator WHERE mission_id = 1860"));

            // The rows a second time: nothing.
            foreach (var sql in T.Up())
                Assert.AreEqual(0, database.Context.Database.ExecuteSqlRaw(sql), sql);
            foreach (var sql in M.StolenHistoryUp())
                Assert.AreEqual(0, database.Context.Database.ExecuteSqlRaw(sql), sql);

            migrator.Migrate(Previous);

            foreach (var table in Tables)
                CollectionAssert.AreEqual(before[table], Dump(database, table), $"Down puts {table} back as it was");
        }

        [TestMethod]
        public void OnMySqlTheMinesGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in T.Up().Concat(M.Up()))
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in M.Down().Concat(T.Down()))
                StringAssert.Contains(down, sql);
        }

        [TestMethod]
        public void TheSixMissionsAndStolenHistoryAreOperationalWithTheClientsMarkers()
        {
            using var harness = Create();

            foreach (var missionId in M.All)
            {
                Assert.IsTrue(harness.Manager.TryGetOperationalMission(missionId, out var mission), $"mission {missionId}");
                Assert.AreEqual(M.Category, mission.CategoryId);
            }

            Assert.IsTrue(harness.Manager.TryGetOperationalMission(S.StolenHistory, out _), "Stolen History has its three pieces");

            var named = new[] { M.MiningDrillsMarker, M.FuelLakeMarker, M.ElohChamberMarker };
            var markers = harness.World.MissionIndicatorEntries.AsNoTracking().Where(row => row.ContentRevision == M.Revision).ToList();
            Assert.IsTrue(markers.Count >= 10);
            Assert.IsTrue(markers.All(row => named.Contains(row.IndicatorId) || row.IndicatorId > MissionIndicator.UnnamedFrom),
                string.Join(", ", markers.Select(row => row.IndicatorId)));
        }

        [TestMethod]
        public void EveryPoolAndObjectOfTheMinesStandsOnItsNavmesh()
        {
            using var harness = Create();
            var nav = harness.Map.NavMesh;

            var wrong = new List<string>();

            foreach (var pool in T.Pools.Where(pool => pool.Map == T.Map))
            {
                var ground = nav.GroundHeight(pool.At + new Vector3(0, 0.5f, 0));
                if (ground == null || Math.Abs(ground.Value - pool.At.Y) > 1.0)
                    wrong.Add($"pool {pool.Id} ({pool.Comment}) at {pool.At}: ground {ground}");
            }

            // The charges, drills, pumps and lakes on the ground; the consoles over the Command Center's floor.
            foreach (var (id, at, over) in T.Usables.Where(u => u.ClassId != T.ConsoleClass).Select(u => (u.Id, u.At, 0.0))
                         .Concat(T.Usables.Where(u => u.ClassId == T.ConsoleClass).Select(u => (u.Id, u.At, 1.0)))
                         .Concat(T.Destructibles.Select(d => (d.Id, d.At, 0.0))))
            {
                var ground = nav.GroundHeight(at + new Vector3(0, 0.5f, 0));
                if (ground == null || Math.Abs(ground.Value - (at.Y - over)) > 1.2)
                    wrong.Add($"row {id} at {at}: ground {ground}");
            }

            Assert.IsEmpty(wrong, string.Join("; ", wrong));

            // The way down is walkable from the camp to Sanchez, and on from the bridge to the Eloh Shelter.
            Assert.AreEqual(true, nav.Connected(T.KearneyAt, T.SanchezAt));
            Assert.AreEqual(true, nav.Connected(new Vector3(129.0f, 201.54f, -25.0f), T.HologramAt));
        }

        #endregion

        #region Disable Production in the Mine

        [TestMethod]
        public void MorrowsDetonatorsBlowTheDrillsAtTheirChargesAndThePumpsAreShot()
        {
            using var harness = Create();
            var state = Enter(harness);
            var morrow = Spawn(harness, T.MorrowPool, T.Morrow);

            WildernessTwinPillarsTests.Accept(harness, morrow, M.DisableProduction);
            Work(harness, 1100);
            Assert.AreEqual((long)M.Detonators, WildernessTwinPillarsTests.Held(harness, T.DetonatorTemplate), "Morrow's detonators");
            Work(harness, 1100);
            Assert.AreEqual((long)M.Detonators, WildernessTwinPillarsTests.Held(harness, T.DetonatorTemplate), "once");

            // The first drill: its charge set, and five seconds later the drill goes with it.
            var charge = PlacedUsables.LiveOf(harness.Map, T.ChargeId(1));
            Use(harness, charge.Object);
            Assert.AreEqual(UseObjectState.BombStateArmed, charge.State);
            Assert.AreEqual((long)M.Detonators - 1, WildernessTwinPillarsTests.Held(harness, T.DetonatorTemplate), "the charge takes one");

            harness.MoveTo(T.KearneyAt);
            Work(harness, 6000);
            Assert.AreEqual(UseObjectState.BombStateDetonated, charge.State);
            Assert.IsTrue(PlacedDestructibles.LiveOf(harness.Map, T.DrillId(1)).IsDown, "the first drill blown");
            Work(harness, 1100);
            Assert.IsTrue(state.DrillsDown.Contains(1));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.DisableProduction, 1), "one of five");

            // It stays down, and its charge is done with.
            Work(harness, 60_000);
            PlacedDestructibles.Worker(harness.Map);
            Assert.IsTrue(PlacedDestructibles.LiveOf(harness.Map, T.DrillId(1)).IsDown);
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, P.RefusalOf(harness.Client, charge));

            foreach (var drill in T.Drills.Skip(1))
                Assert.IsTrue(PlacedDestructibles.Destroy(PlacedDestructibles.LiveOf(harness.Map, T.DrillId(drill.Number)), harness.Client.Player));
            for (uint pump = 1; pump <= T.PumpHouses.Length; pump++)
                Assert.IsTrue(PlacedDestructibles.Destroy(PlacedDestructibles.LiveOf(harness.Map, T.PumpHouseId(pump)), harness.Client.Player));
            Work(harness, 1100);
            Work(harness, 1100);

            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.DisableProduction, 1));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.DisableProduction, 2));
            WildernessTwinPillarsTests.Reward(harness, morrow, M.DisableProduction, 0, 13216, 32000, 3200);
        }

        #endregion

        #region Fuel Lake Sabotage

        [TestMethod]
        public void TheLakesBurnTheMainFuelPumpOnceTheSecurityConsolesAreOff()
        {
            using var harness = Create();
            var state = Enter(harness);
            var simpson = Put(harness, T.Simpson, T.KearneyAt + new Vector3(2, 0, 2));

            WildernessTwinPillarsTests.Accept(harness, simpson, M.FuelLake);

            // The first lake lit engages security: the fields up, the fire out, the consoles lit.
            Shoot(harness, T.FuelId(1));
            Work(harness);
            Assert.AreEqual(P.Security.Engaged, state.Security);
            Assert.IsTrue(state.ConsoleLit.All(lit => lit));
            Assert.IsFalse(PlacedDestructibles.LiveOf(harness.Map, T.FuelId(1)).IsDown, "smothered");
            Assert.IsTrue(T.Lakes.All(lake => PlacedUsables.LiveOf(harness.Map, T.FieldId(lake.Number)).State == (UseObjectState)T.FieldUp));
            Assert.IsTrue(T.Lakes.All(lake => PlacedUsables.LiveOf(harness.Map, T.ConsoleId(lake.Number)).State == UseObjectState.TsState1));

            // Under its field a lake does not burn.
            Shoot(harness, T.FuelId(2));
            Work(harness);
            Assert.IsFalse(PlacedDestructibles.LiveOf(harness.Map, T.FuelId(2)).IsDown);
            Assert.AreEqual(0L, state.Heat);

            // The consoles: each takes its lake's field down; all three, security is off.
            foreach (var lake in T.Lakes)
            {
                Use(harness, PlacedUsables.LiveOf(harness.Map, T.ConsoleId(lake.Number)).Object);
                Assert.AreEqual((UseObjectState)T.FieldDown, PlacedUsables.LiveOf(harness.Map, T.FieldId(lake.Number)).State, $"{lake.Where} field");
            }
            Work(harness);
            Assert.AreEqual(P.Security.Disarmed, state.Security);
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, P.RefusalOf(harness.Client, PlacedUsables.LiveOf(harness.Map, T.ConsoleId(1))), "an unlit console");

            // One lake alone burns out before the heat is enough.
            Shoot(harness, T.FuelId(1));
            Work(harness);
            Assert.AreEqual(UseObjectState.TsState1, PlacedUsables.LiveOf(harness.Map, T.FireId(1)).State, "alight");
            for (var i = 0; i < 50; i++)
                Work(harness, 1000);
            Assert.IsFalse(state.MainPumpDown, "one lake is not enough");
            Assert.IsFalse(PlacedDestructibles.LiveOf(harness.Map, T.FuelId(1)).IsDown, "burnt out, and back");
            Assert.AreEqual(0L, state.Heat);

            // All three together set it off.
            foreach (var lake in T.Lakes)
                Shoot(harness, T.FuelId(lake.Number));
            for (var i = 0; i < 30 && !state.MainPumpDown; i++)
                Work(harness, 1000);
            Assert.IsTrue(state.MainPumpDown);
            Assert.IsTrue(PlacedDestructibles.LiveOf(harness.Map, T.MainPump).IsDown);
            Assert.IsTrue(Pool(harness, T.PumpAmbushPool).Woken, "its ambush comes");
            Work(harness, 1100);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.FuelLake, 1));

            WildernessTwinPillarsTests.Reward(harness, simpson, M.FuelLake, 0, 3121, 32000, 3200);
        }

        #endregion

        #region Central Bound Patrol

        [TestMethod]
        public void KearneysSoldiersFollowThePlayerToSanchez()
        {
            using var harness = Create();
            var state = Enter(harness);
            var kearney = Spawn(harness, T.Kearney, T.Kearney);

            WildernessTwinPillarsTests.Accept(harness, kearney, M.CentralBound);
            Work(harness);
            Assert.AreEqual(0U, state.EscortFor, "not before he is spoken to again");

            WildernessTwinPillarsTests.Talk(harness, kearney, M.CentralBound, 2);
            Work(harness);
            Assert.AreEqual(harness.Client.Player.Id, state.EscortFor);
            Assert.IsTrue(Pool(harness, T.EscortPool).Woken, "the soldiers called out");

            var soldiers = Soldiers(harness);
            Assert.AreEqual(4, soldiers.Count);
            Work(harness);
            Assert.AreEqual(harness.Client.Player.Id, Pool(harness, T.EscortPool).FollowOwnerCharacterId, "they follow the player");

            // One of them brought to Sanchez.
            var sanchez = Spawn(harness, T.SanchezPool, T.Sanchez);
            harness.MoveTo(sanchez.Position + new Vector3(3, 0, 0));
            CreatureManager.Instance.SetLocation(soldiers[0], NavMeshManager.SnapToGround(harness.Map, sanchez.Position + new Vector3(4, 0, 0)), 0, T.Map);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.CentralBound, 1));
            Assert.AreEqual(0U, Pool(harness, T.EscortPool).FollowOwnerCharacterId, "and let go");

            WildernessTwinPillarsTests.Reward(harness, sanchez, M.CentralBound, 0, 12835, 16000, 1600);
        }

        [TestMethod]
        public void AllFourDeadBeforeSanchezFailTheEscort()
        {
            using var harness = Create();
            var state = Enter(harness);
            var kearney = Spawn(harness, T.Kearney, T.Kearney);

            WildernessTwinPillarsTests.Accept(harness, kearney, M.CentralBound);
            WildernessTwinPillarsTests.Talk(harness, kearney, M.CentralBound, 2);
            Work(harness);

            var soldiers = Soldiers(harness);
            Work(harness);
            foreach (var soldier in soldiers)
                CreatureManager.Instance.HandleCreatureKill(harness.Map, soldier, null);
            Work(harness);

            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, M.CentralBound, 1));
            Assert.AreEqual(0U, state.EscortFor);
        }

        #endregion

        #region Mining for Information

        [TestMethod]
        public void TheElohInformationCallsThreeWavesAndTheShelterHeldCountsOnTheClock()
        {
            using var harness = Create();
            var state = Enter(harness);
            var jayjack = Put(harness, T.Jayjack, T.KearneyAt + new Vector3(2, 0, -2));

            WildernessTwinPillarsTests.Accept(harness, jayjack, M.MiningForInformation);

            var hologram = harness.Map.DynamicObjects.SingleOrDefault(obj => obj.SceneMissionId == M.MiningForInformation && obj.SceneActorRole == "eloh-hologram");
            Assert.IsNotNull(hologram, "the Eloh information");
            Assert.AreEqual(T.HologramAt.X, hologram.Position.X, 0.5);
            harness.MoveTo(hologram.Position + new Vector3(2, 0, 0));
            harness.Drain();
            new NpcManager(harness, harness.Manager).RequestNpcConverse(harness.Client,
                new RequestNPCConversePacket { EntityId = hologram.EntityId, ActionId = ActionId.Converse, ActionArgId = 1 });
            new NpcManager(harness, harness.Manager).CompleteNPCObjective(harness.Client,
                new CompleteNPCObjectivePacket { EntityId = hologram.EntityId, MissionId = M.MiningForInformation, ObjectiveId = 1, PlayerFlagId = 1 });
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.MiningForInformation, 1));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.MiningForInformation, 2));

            // The waves, one after another.
            Work(harness);
            for (uint wave = 1; wave <= T.AmbushWaves; wave++)
            {
                Assert.AreEqual(P.AmbushPhase.Running, state.Ambush);
                Assert.AreEqual(wave, state.Wave);
                Assert.IsTrue(Pool(harness, T.AmbushPool(wave)).Woken, $"wave {wave} called");
                Pool(harness, T.AmbushPool(wave)).HasSpawned = true;
                Work(harness);
            }

            Assert.AreEqual(1, state.AmbushesDefended);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.MiningForInformation, 2));
            WildernessTwinPillarsTests.Reward(harness, jayjack, M.MiningForInformation, 0, 12756, 24000, 2400);
        }

        #endregion

        #region Stolen History

        [TestMethod]
        public void TheMapCiphersCrateOpensOnlyForStolenHistory()
        {
            using var harness = Create();
            Enter(harness);
            var crate = PlacedUsables.LiveOf(harness.Map, T.CipherCrate);

            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, P.RefusalOf(harness.Client, crate), "for nobody else");

            var quoa = Put(harness, 510104, T.KearneyAt + new Vector3(-2, 0, 2));
            WildernessTwinPillarsTests.Accept(harness, quoa, S.StolenHistory);
            Assert.IsNull(P.RefusalOf(harness.Client, crate));

            Use(harness, crate.Object);
            Assert.AreEqual(1L, WildernessTwinPillarsTests.Held(harness, T.MapCipherTemplate), "the Map Cipher");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, S.StolenHistory, 2));
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, P.RefusalOf(harness.Client, crate), "once");
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
                    new CharacterStartingExperienceEntry(harness.Client.Player.Id, M.Revision, CharacterStartingExperienceState.Skipped)));
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

        /// <summary>The mines' usables and destructibles down on the copy, as its coming up puts them, and its state.</summary>
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
            PlacedUsables.Worker(harness.Map);
            P.Worker(harness.Map);
            harness.Drain();
        }

        private static void Shoot(WildernessRuntimeTestHarness harness, uint rowId) =>
            PlacedDestructibles.Destroy(PlacedDestructibles.LiveOf(harness.Map, rowId), harness.Client.Player);

        private static List<Creature> Soldiers(WildernessRuntimeTestHarness harness)
        {
            harness.SpawnWorld(T.EscortPool);
            return EntityManager.Instance.Creatures.Values.Where(creature => creature.SpawnPool?.DbId == T.EscortPool &&
                CreatureManager.IsLivingOnMap(harness.Map, creature)).ToList();
        }

        private static SpawnPool Pool(WildernessRuntimeTestHarness harness, uint id) => harness.Map.SpawnPools.Single(pool => pool.DbId == id);

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
            command.CommandText = $"SELECT * FROM {table}";
            database.Context.Database.OpenConnection();
            using var reader = command.ExecuteReader();
            var rows = new List<string>();
            while (reader.Read())
                rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => Convert.ToString(reader.GetValue(i), System.Globalization.CultureInfo.InvariantCulture))));
            rows.Sort(StringComparer.Ordinal);
            return rows;
        }

        #endregion
    }
}
