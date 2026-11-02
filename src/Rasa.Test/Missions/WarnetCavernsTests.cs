using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Missions.Definitions;
using Rasa.Packets.LootDispenser.Client;
using Rasa.Packets.MapChannel.Client;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;
using Rasa.Test.Database;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions
{
    using W = WarnetCavernsContent;
    using M = WarnetCavernsMissions;
    using P = WarnetCaverns;

    /// <summary>
    /// The eight Warnet Caverns missions (WarnetCavernsMissions) and what the caverns do for them
    /// (WarnetCaverns): the specialists' walk to the camp, the stations, Brumley's escort, the
    /// metal container, the lure, the Queen and her attacks, the hives and Tozid, the Krimm
    /// missions' kills, drops, radio and bio-bomb.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class WarnetCavernsTests
    {
        private const string Migration = "20271014000000_Warnet_caverns";

        private long _now = 1_000_000;

        [TestInitialize]
        public void Clock()
        {
            P.Reset();
            P.Now = () => _now;
            PlacedDestructibles.Now = () => _now;
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

        [TestMethod]
        public void TheMigrationWritesTheEightMissionsAndTakesThemOutAgain()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var all = database.Context.Database.GetMigrations().ToList();
            var previous = all[all.IndexOf(Migration) - 1];

            migrator.Migrate(Migration);
            Assert.AreEqual(8L, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{M.Revision}' AND enabled = 1 AND category_id = {M.Category}"));
            Assert.AreEqual(1608L, Count(database, $"SELECT client_counter_0_text_id AS Value FROM mission_objective_definition WHERE mission_id = {M.ResetStations} AND objective_id = 1"));
            Assert.AreEqual(1L, Count(database, $"SELECT radio_completeable AS Value FROM mission_content_definition WHERE mission_id = {M.KillTheLight}"));

            migrator.Migrate(previous);
            foreach (var table in new[] { "mission_content_definition", "mission_objective_definition", "mission_trigger", "mission_action", "mission_reward_item", "mission_scene_binding", "mission_evidence", "mission_indicator", "mission_scenario", "mission_channel_policy" })
                Assert.AreEqual(0L, Count(database, $"SELECT COUNT(*) AS Value FROM {table} WHERE content_revision = '{M.Revision}'"), table);
        }

        [TestMethod]
        public void TheEightAreOperationalWithTheirGiversDropsAndChannels()
        {
            using var harness = Create();

            foreach (var missionId in M.Missions)
            {
                Assert.IsTrue(harness.Manager.TryGetOperationalMission(missionId, out var mission), $"mission {missionId}");
                Assert.AreEqual(M.Category, mission.CategoryId);
            }

            var definitions = harness.World.Set<MissionContentDefinitionEntry>().Where(row => row.ContentRevision == M.Revision).ToDictionary(row => row.MissionId);
            Assert.AreEqual((W.Sides, W.Sides), (definitions[M.ResetStations].GiverId, definitions[M.ResetStations].ReceiverId));
            Assert.AreEqual((W.Brumley, W.Brumley), (definitions[M.EscortToStation].GiverId, definitions[M.EscortToStation].ReceiverId));
            Assert.AreEqual((W.Brumley, W.Gooding), (definitions[M.QueenLure].GiverId, definitions[M.QueenLure].ReceiverId));
            Assert.AreEqual((W.Gooding, W.Brumley), (definitions[M.KillTheQueen].GiverId, definitions[M.KillTheQueen].ReceiverId));
            Assert.AreEqual((W.LaFontaine, (uint?)null), (definitions[M.KillTheLight].GiverId, definitions[M.KillTheLight].ReceiverId));
            Assert.AreEqual(((uint?)null, W.LaFontaine), (definitions[M.KillTheLightII].GiverId, definitions[M.KillTheLightII].ReceiverId));
            Assert.AreEqual((M.Barrasso, M.Barrasso), (definitions[M.Scarecrow].GiverId, definitions[M.Scarecrow].ReceiverId));
            Assert.AreEqual((M.Mullen, M.Mullen), (definitions[M.TrophyCase].GiverId, definitions[M.TrophyCase].ReceiverId));

            // Lake Elinor's trophies are the Palisades' warnets', the caverns' the caverns'; Joahg alone carries the meat.
            var trophies = Bindings(harness, M.TrophyCase);
            Assert.AreEqual(W.Palisades, trophies["lake-elinor-trophies"].Drop.MapContextId);
            CollectionAssert.AreEqual(new[] { W.PalisadesWarnet }, trophies["lake-elinor-trophies"].Drop.CreatureIds.ToArray());
            Assert.AreEqual(W.Map, trophies["caverns-trophies"].Drop.MapContextId);
            CollectionAssert.AreEquivalent(W.CavernsWarnets, trophies["caverns-trophies"].Drop.CreatureIds.ToArray());
            CollectionAssert.AreEqual(new[] { W.Joahg }, Bindings(harness, M.Scarecrow)["miasma-meat"].Drop.CreatureIds.ToArray());
            Assert.AreEqual(100, Bindings(harness, M.KillTheQueen)["goo-covered-object"].Drop.ChancePercent);

            Assert.IsTrue(harness.Creatures.LoadedCreatures[W.Brumley].RunSpeed > 0, "Brumley walks");
        }

        [TestMethod]
        public void TheAttacksScaleWithTheSquadInTheNest()
        {
            Assert.AreEqual(5, P.WaveSize(1));
            Assert.AreEqual(5, P.WaveSize(0));
            Assert.AreEqual(7, P.WaveSize(2));
            Assert.AreEqual(11, P.WaveSize(4));
            Assert.AreEqual(12, P.WaveSize(5));
            Assert.AreEqual(12, P.WaveSize(10));
            Assert.IsTrue(P.InNest(new Vector3((float)W.LureStand.X, 0, (float)W.LureStand.Z)));
            Assert.IsFalse(P.InNest(new Vector3((float)W.RemoteStation.X, 0, (float)W.RemoteStation.Z)));
        }

        /// <summary>Sides's mission: the way cleared, the specialists to the mouth; the camp cleared, to their posts and the turrets on; the stations rebooted.</summary>
        [TestMethod]
        public void TheWayTheCampAndTheStationsResetTheResearchStations()
        {
            using var harness = Create();
            var map = harness.Map;
            var state = Enter(harness);
            harness.SpawnWorld(W.SidesPool, W.BrumleyPool, W.GoodingPool);
            var sides = WildernessTwinPillarsTests.RequireNpc(harness, W.SidesPool, W.Sides);

            WildernessTwinPillarsTests.Accept(harness, sides, M.ResetStations);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.ResetStations, 6));
            Work(harness);
            Assert.IsFalse(state.WayCleared, "the way's warnets not yet down");
            Assert.AreEqual(P.Place.Entrance, state.Specialists);

            foreach (var id in W.WayPools)
                Pool(harness, id).HasSpawned = true;
            Work(harness);
            Assert.IsTrue(state.WayCleared);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.ResetStations, 6));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.ResetStations, 2));
            Assert.AreEqual(P.Place.Mouth, state.Specialists);
            Assert.AreEqual(V(W.SidesAtMouth), sides.Controller.ActionFollow.Anchor, "Sides walks to the camp's mouth");
            Assert.IsTrue(sides.Controller.ActionFollow.HasAnchor);
            Assert.IsFalse(Pool(harness, W.CampTurretPools[0]).Woken);

            // He walks it, through the tunnel and the first chamber, and does not jump.
            harness.MoveTo(V(W.SidesAtMouth) + new Vector3(0, 0, 30));
            for (var tick = 0; tick < 2400 && Vector3.Distance(sides.Position, V(W.SidesAtMouth)) > 4; tick++)
            {
                var was = sides.Position;
                harness.Tick();
                Assert.IsTrue(Vector3.Distance(was, sides.Position) < 5, "walked, not teleported");
            }
            Assert.IsTrue(Vector3.Distance(sides.Position, V(W.SidesAtMouth)) <= 4, $"Sides at the mouth: {sides.Position}");

            foreach (var id in W.CampPools)
                Pool(harness, id).HasSpawned = true;
            Work(harness);
            Assert.IsTrue(state.SiteCleared);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.ResetStations, 2));
            Assert.AreEqual(V(W.SidesAtPost), sides.Controller.ActionFollow.Anchor, "and to his post in the camp");
            Assert.IsTrue(W.CampTurretPools.All(id => Pool(harness, id).Woken), "the camp's turrets come on");

            for (uint i = 0; i < 3; i++)
            {
                var objective = 3 + i;
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.ResetStations, objective));
                var station = map.DynamicObjects.Single(obj => obj.SceneMissionId == M.ResetStations && obj.SceneActorRole == $"station-{i + 1}");
                Assert.IsTrue(Vector3.Distance(station.Position, V(W.Stations[i])) < 0.5f);
                var npcs = new NpcManager(harness, harness.Manager);
                harness.MoveTo(station.Position + new Vector3(1.5f, 0, 0));
                harness.Drain();
                npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = station.EntityId, ActionId = ActionId.Converse, ActionArgId = 1 });
                harness.Drain();
                npcs.PerformNPCChoice(harness.Client, new PerformNPCChoicePacket
                {
                    EntityId = station.EntityId, MissionId = M.ResetStations, ObjectiveId = objective, PlayerFlagId = 1, ChoiceIdx = 1
                });
                harness.Drain();
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.ResetStations, objective));
            }

            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.ResetStations, 1), "Research Stations Reset: three");
            Assert.IsTrue(harness.Client.Player.Missions[M.ResetStations].Completeable);
            WildernessTwinPillarsTests.Reward(harness, sides, M.ResetStations, 0, M.InjectionGun, 18000, 2700);
        }

        /// <summary>A copy someone enters having done 393: the specialists wait at the camp's mouth, and the camp is to be cleared again.</summary>
        [TestMethod]
        public void ACopyEnteredAfterTheStationsHasThemWaitingAtTheMouth()
        {
            using var harness = Create();
            Completed(harness, M.ResetStations);
            var state = Enter(harness);

            Assert.IsTrue(state.WayCleared);
            Assert.IsFalse(state.SiteCleared);
            Assert.AreEqual(P.Place.Mouth, state.Specialists);
        }

        /// <summary>Brumley ready, following, lost and found again, and at Remote Station 3: event 1, and he stays there.</summary>
        [TestMethod]
        public void BrumleyFollowsWhoeverIsReadyToRemoteStation3()
        {
            using var harness = Create();
            Completed(harness, M.ResetStations);
            var state = Enter(harness);
            harness.SpawnWorld(W.BrumleyPool);
            var brumley = WildernessTwinPillarsTests.RequireNpc(harness, W.BrumleyPool, W.Brumley);

            WildernessTwinPillarsTests.Accept(harness, brumley, M.EscortToStation);
            WildernessTwinPillarsTests.OpenNativeNpcConversation(harness, brumley);
            Assert.IsTrue(harness.Manager.TryPerformNpcChoice(harness.Client, brumley.EntityId, M.EscortToStation, 2, 1, 1), "Hold on a moment.");
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.EscortToStation, 2), "not yet");
            Work(harness);
            Assert.AreEqual(0u, state.BrumleyFollows);

            WildernessTwinPillarsTests.OpenNativeNpcConversation(harness, brumley);
            Assert.IsTrue(harness.Manager.TryPerformNpcChoice(harness.Client, brumley.EntityId, M.EscortToStation, 2, 1, 2), "We're ready when you are.");
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.EscortToStation, 2));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.EscortToStation, 1));

            harness.MoveTo(brumley.Position + new Vector3(3, 0, 0));
            Work(harness);
            Assert.AreEqual(harness.Client.Player.Id, state.BrumleyFollows, "he goes with the player");
            Assert.AreEqual(harness.Client.Player.Id, brumley.SpawnPool.FollowOwnerCharacterId);
            Assert.IsTrue(BehaviorManager.IsMissionEscort(brumley));

            // Left behind, he goes back; come back for him and he comes again.
            harness.MoveTo(brumley.Position + new Vector3(P.LostRadius + 5, 0, 0));
            Work(harness);
            Assert.AreEqual(0u, state.BrumleyFollows);
            Assert.AreEqual(0u, brumley.SpawnPool.FollowOwnerCharacterId);
            harness.MoveTo(brumley.Position + new Vector3(2, 0, 0));
            Work(harness);
            Assert.AreEqual(harness.Client.Player.Id, state.BrumleyFollows);

            // At Remote Station 3.
            brumley.Position = V(W.RemoteStation) + new Vector3(2, 0, 0);
            harness.MoveTo(V(W.RemoteStation) + new Vector3(4, 0, 0));
            Work(harness);
            Assert.IsTrue(state.BrumleyAtStation);
            Assert.AreEqual(0u, state.BrumleyFollows, "his walk is over");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.EscortToStation, 1));
            Assert.AreEqual(V(W.RemoteStation), brumley.Controller.ActionFollow.Anchor, "and he stays at the station");

            WildernessTwinPillarsTests.Reward(harness, brumley, M.EscortToStation, 2, M.EmpShotgun, 18000, 2700);
        }

        /// <summary>The metal container gives one only to whoever wants one; glands and fluids drop off the caverns' warnets; to Gooding.</summary>
        [TestMethod]
        public void TheMetalContainerIsOnlyForCreateTheQueenLure()
        {
            using var harness = Create();
            Completed(harness, M.ResetStations);
            Completed(harness, M.EscortToStation);
            Enter(harness);
            var dispenser = PlacedUsables.LiveOf(harness.Map, W.MetalContainerDispenser);

            Use(harness, dispenser.Object);
            Assert.AreEqual(0, WildernessTwinPillarsTests.Held(harness, W.MetalContainerTemplate), "nobody's container without the mission");

            harness.SpawnWorld(W.BrumleyPool);
            var brumley = WildernessTwinPillarsTests.RequireNpc(harness, W.BrumleyPool, W.Brumley);
            WildernessTwinPillarsTests.Accept(harness, brumley, M.QueenLure);
            Use(harness, dispenser.Object);
            Assert.AreEqual(1, WildernessTwinPillarsTests.Held(harness, W.MetalContainerTemplate));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.QueenLure, 1));

            _now += 20_000;
            PlacedUsables.Worker(harness.Map);
            Use(harness, dispenser.Object);
            Assert.AreEqual(1, WildernessTwinPillarsTests.Held(harness, W.MetalContainerTemplate), "and one only");
        }

        /// <summary>The lure placed: event 1, the Queen and an attack; her death: event 2, another attack; the goo covered object off her; to Brumley.</summary>
        [TestMethod]
        public void TheLureCallsTheQueenAndHerAttacksAndBrumleyTakesHerObject()
        {
            using var harness = Create();
            var map = harness.Map;
            Completed(harness, M.ResetStations);
            Completed(harness, M.EscortToStation);
            Completed(harness, M.QueenLure);
            var state = Enter(harness);
            var stand = PlacedUsables.LiveOf(map, W.LureAcceptor);

            Use(harness, stand.Object);
            Assert.IsFalse(Pool(harness, W.QueenPool).Woken, "no lure, no Queen");

            harness.SpawnWorld(W.GoodingPool);
            var gooding = WildernessTwinPillarsTests.RequireNpc(harness, W.GoodingPool, W.Gooding);
            WildernessTwinPillarsTests.Accept(harness, gooding, M.KillTheQueen);
            Assert.AreEqual(1, WildernessTwinPillarsTests.Held(harness, W.QueenLureTemplate), "Gooding's lure");

            Use(harness, stand.Object);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.KillTheQueen, 2), "the lure placed");
            Assert.AreEqual(1, WildernessTwinPillarsTests.Held(harness, W.QueenLureTemplate), "it stays with the mission");
            Assert.IsTrue(Pool(harness, W.QueenPool).Woken, "the Queen is called");
            var lureWave = Enumerable.Range(0, (int)W.WavePools).Select(i => W.FirstLureWavePool + (uint)i).Where(id => Pool(harness, id).Woken).ToArray();
            Assert.AreEqual(P.WaveSize(1), lureWave.Length, "an attack for one");

            harness.SpawnWorld(lureWave.Append(W.QueenPool).ToArray());
            Work(harness);
            var living = Living(harness);
            var queen = living.Single(c => c.DbId == W.Queen);
            Assert.AreEqual(2685, queen.Attributes[Attributes.Health].CurrentMax);
            Assert.IsFalse(Pool(harness, W.QueenPool).Woken, "out, she is not back on her pool's timer");
            var warnets = living.Where(c => lureWave.Contains(c.SpawnPool?.DbId ?? 0)).ToList();
            Assert.AreEqual(P.WaveSize(1), warnets.Count);
            Assert.IsTrue(warnets.All(w => w.Controller.ActionFighting.TargetEntityId == harness.Client.Player.EntityId), "sent at the player");
            Assert.IsTrue(lureWave.All(id => !Pool(harness, id).Woken));

            // Her death: the goo covered object off her, event 2, and the second attack.
            WildernessDaghdasUrnTests.Kill(harness, queen);
            var corpse = map.LootDispensers[queen.CorpseLootEntityId];
            Assert.AreEqual(1, corpse.LootItems.Count(item => item.ItemTemplateId == W.GooObjectTemplate));
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client, new RequestLootAllFromCorpsePacket { EntityId = corpse.EntityId });
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.KillTheQueen, 4));

            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.KillTheQueen, 1));
            Assert.AreEqual(P.WaveSize(1), Enumerable.Range(0, (int)W.WavePools).Count(i => Pool(harness, W.FirstDeathWavePool + (uint)i).Woken), "her death's attack");
            Assert.IsTrue(harness.Client.Player.Missions[M.KillTheQueen].Completeable);

            harness.SpawnWorld(W.BrumleyPool);
            var brumley = WildernessTwinPillarsTests.RequireNpc(harness, W.BrumleyPool, W.Brumley);
            WildernessTwinPillarsTests.Reward(harness, brumley, M.KillTheQueen, 0, M.NetGun, 25000, 3800);
            Assert.AreEqual(0, WildernessTwinPillarsTests.Held(harness, W.QueenLureTemplate));
            Assert.AreEqual(0, WildernessTwinPillarsTests.Held(harness, W.GooObjectTemplate));
        }

        /// <summary>All six hives down, Tozid comes out; he counts for Trophy Case, as Chelliak does.</summary>
        [TestMethod]
        public void TheHivesDownBringOutTozidWhoCountsForTrophyCaseWithChelliak()
        {
            using var harness = Create();
            var map = harness.Map;
            var state = Enter(harness);
            var mullen = Put(harness, M.Mullen, V(W.GoodingAtEntrance) + new Vector3(2, 0, 0));
            WildernessTwinPillarsTests.Accept(harness, mullen, M.TrophyCase);

            var hives = PlacedDestructibles.On(map).Where(live => W.Hives.Any(h => h.Id == live.Placement.Id)).ToList();
            Assert.AreEqual(6, hives.Count);
            foreach (var hive in hives.Take(5))
                Assert.IsTrue(PlacedDestructibles.Destroy(hive, harness.Client.Player));
            Work(harness);
            Assert.IsFalse(Pool(harness, W.TozidPool).Woken, "five is not all");

            Assert.IsTrue(PlacedDestructibles.Destroy(hives[5], harness.Client.Player));
            Work(harness);
            Assert.IsTrue(Pool(harness, W.TozidPool).Woken, "Tozid comes out");
            Assert.AreEqual(1, state.TozidOuts);
            Assert.AreEqual(0, state.HivesDown.Count);

            harness.SpawnWorld(W.TozidPool);
            Work(harness);
            var tozid = Living(harness).Single(c => c.DbId == W.Tozid);
            Assert.IsFalse(Pool(harness, W.TozidPool).Woken, "out once");

            WildernessDaghdasUrnTests.Kill(harness, tozid);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.TrophyCase, 4));

            harness.SpawnWorld(W.ChelliakPool);
            WildernessDaghdasUrnTests.Kill(harness, Living(harness).Single(c => c.DbId == W.Chelliak));
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.TrophyCase, 3));
        }

        /// <summary>Brazix's orders, radioed in; part II by radio at that reward; the canister used; back to LaFontaine.</summary>
        [TestMethod]
        public void KillTheLightIsBrazixsOrdersByRadioThenTheBioBomb()
        {
            using var harness = Create();
            var map = harness.Map;
            Enter(harness);
            var lafontaine = Put(harness, W.LaFontaine, V(W.GoodingAtEntrance) + new Vector3(2, 0, 0));
            WildernessTwinPillarsTests.Accept(harness, lafontaine, M.KillTheLight);

            harness.SpawnWorld(W.BrazixPool);
            var brazix = Living(harness).Single(c => c.DbId == W.Brazix);
            WildernessDaghdasUrnTests.Kill(harness, brazix);
            var corpse = map.LootDispensers[brazix.CorpseLootEntityId];
            Assert.AreEqual(1, corpse.LootItems.Count(item => item.ItemTemplateId == W.OrdersTemplate));
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client, new RequestLootAllFromCorpsePacket { EntityId = corpse.EntityId });
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.KillTheLight, 1));
            Assert.IsTrue(harness.Client.Player.Missions[M.KillTheLight].Completeable);

            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, M.KillTheLight, null, null), "radioed in");
            harness.Drain();
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, M.KillTheLightII), "part II offered by radio");
            harness.Drain();

            var bomb = map.DynamicObjects.Single(obj => obj.SceneMissionId == M.KillTheLightII && obj.SceneActorRole == "bio-bomb");
            Assert.IsTrue(Vector3.Distance(bomb.Position, V(W.BioBomb)) < 0.5f);
            Use(harness, bomb);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.KillTheLightII, 1), "disarmed");

            WildernessTwinPillarsTests.Reward(harness, lafontaine, M.KillTheLightII, 1, M.HealingDisc, 19000, 2850);
        }

        #region Helpers

        private static WildernessRuntimeTestHarness Create()
        {
            var harness = WildernessRuntimeTestHarness.Create(mapContextId: W.Map);
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

        /// <summary>The caverns' usables and hives down on the copy, as its coming up puts them, and its state.</summary>
        private P.State Enter(WildernessRuntimeTestHarness harness)
        {
            PlacedUsables.Load(harness.World.Set<WorldUsableEntry>().AsNoTracking().Where(row => row.MapContextId == W.Map).ToList());
            PlacedUsables.Place(harness.Map);
            PlacedDestructibles.Load(harness.World.Set<WorldDestructibleEntry>().AsNoTracking().Where(row => row.MapContextId == W.Map).ToList());
            PlacedDestructibles.Place(harness.Map);
            Work(harness);
            return P.StateOf(harness.Map);
        }

        private void Work(WildernessRuntimeTestHarness harness, long ms = 2100)
        {
            _now += ms;
            P.Worker(harness.Map);
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

        private static Creature Put(WildernessRuntimeTestHarness harness, uint dbId, Vector3 position)
        {
            var creature = CreatureManager.Instance.CreateCreature(dbId, null);
            CreatureManager.Instance.SetLocation(creature, NavMeshManager.SnapToGround(harness.Map, position), 0, W.Map);
            CellManager.Instance.AddToWorld(harness.Map, creature);
            return creature;
        }

        private static List<Creature> Living(WildernessRuntimeTestHarness harness) =>
            harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct()
                .Where(creature => CreatureManager.IsLivingOnMap(harness.Map, creature)).ToList();

        private static SpawnPool Pool(WildernessRuntimeTestHarness harness, uint id) => harness.Map.SpawnPools.Single(pool => pool.DbId == id);

        private static Dictionary<string, MissionItemBinding> Bindings(WildernessRuntimeTestHarness harness, uint missionId)
        {
            var row = harness.World.Set<MissionSceneBindingEntry>().AsNoTracking().Single(binding => binding.MissionId == missionId && binding.ContentRevision == M.Revision);
            var scene = System.Text.Json.JsonSerializer.Deserialize<Rasa.Missions.Content.MissionSceneDefinition>(row.Bindings, Rasa.Missions.Content.MissionContentCodec.Options);
            return scene.Items.ToDictionary(item => item.ItemKey);
        }

        private static Vector3 V(W.Spot spot) => new((float)spot.X, (float)spot.Y, (float)spot.Z);

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

        private static long Count(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).AsEnumerable().Single();

        #endregion
    }
}
