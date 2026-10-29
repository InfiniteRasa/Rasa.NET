extern alias RasaGame;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Navigation;
using Rasa.Packets.MapChannel.Client;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Plateau
{
    using static PlateauCampResistanceTests;

    /// <summary>
    /// General Bailey's and Liaison Ridout's missions, played through as a player plays them: A Mystery Unearthed
    /// (2016) from Topper on the Wilderness to Bailey on the Plateau, Symbolism (2017) across Pools, the Marshes
    /// and the Wilderness, Logos: Vortex (1659) on the Plateau, and Mislaid Plans (965) at Pinnacle Watchtower.
    ///
    /// Why these tests: as PlateauPerduTests. Symbolism's talks are on three other maps, so the player goes from
    /// map to map as EnterPlateau puts them on the Plateau; the Logos are used at the server's own shrines
    /// (table logos), as a player uses them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauBaileyTests
    {
        private const uint Mystery = PlateauMysteryUnearthedV1.MissionId;
        private const uint Symbolism = PlateauSymbolismV1.MissionId;
        private const uint Vortex = PlateauLogosVortexV1.MissionId;
        private const uint Topper = PlateauMysteryUnearthedV1.Topper;
        private const uint Plans = PlateauMislaidPlansV1.MissionId;

        [TestMethod]
        public void TopperSendsThePlayerToBaileyWhoSendsThemAfterTheEarthLogos()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var wilderness = harness.Map;
            harness.SpawnWorld(Topper);
            var topper = harness.Npc(Topper);
            Assert.IsTrue(Offers(harness, topper, Mystery), "Topper offers A Mystery Unearthed");
            harness.MoveTo(topper.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, topper.EntityId, Mystery));
            harness.Drain();
            Assert.IsFalse(Offers(harness, topper, Symbolism), "Symbolism comes from Bailey");

            var plateau = EnterPlateau(harness);
            Spawn(plateau, PlateauBaileyNpcsV1.BaileyPool);
            var bailey = Npc(plateau, PlateauBaileyNpcsV1.BaileyPool, PlateauBaileyNpcsV1.Bailey);
            AssertAt(plateau, bailey, PlateauBaileyNpcsV1.BaileyAt, "Bailey");
            AssertMarker(harness, Mystery, PlateauMysteryUnearthedV1.Report, 1, bailey.Position, "2016's marker on Bailey");
            Assert.IsFalse(Offers(harness, bailey, Symbolism), "Symbolism waits for A Mystery Unearthed");
            Talk(harness, bailey, Mystery, PlateauMysteryUnearthedV1.Report);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, bailey, Mystery, null,
                PlateauMysteryUnearthedV1.Experience, PlateauMysteryUnearthedV1.Credits);

            Assert.IsTrue(Offers(harness, bailey, Symbolism), "Bailey offers Symbolism");
            harness.MoveTo(bailey.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, bailey.EntityId, Symbolism));
            harness.Drain();

            var pools = Enter(harness, PlateauBaileyNpcsV1.PoolsMap, "adv_foreas_valverde_pools", 520);
            Spawn(pools, PlateauBaileyNpcsV1.HermitPool);
            var hermit = Npc(pools, PlateauBaileyNpcsV1.HermitPool, PlateauBaileyNpcsV1.Hermit);
            AssertAt(pools, hermit, PlateauBaileyNpcsV1.HermitAt, "the Hermit");
            AssertMarker(harness, Symbolism, PlateauSymbolismV1.Hermit, 1, hermit.Position, "1's marker on the Hermit");
            Talk(harness, hermit, Symbolism, PlateauSymbolismV1.Hermit);

            var marshes = Enter(harness, PlateauBaileyNpcsV1.MarshesMap, "adv_foreas_valverde_marshes", 663);
            Spawn(marshes, PlateauBaileyNpcsV1.McCormick);
            var mcCormick = RequireNpc(marshes, PlateauBaileyNpcsV1.McCormick);
            AssertAt(marshes, mcCormick, PlateauBaileyNpcsV1.McCormickAt, "McCormick");
            AssertMarker(harness, Symbolism, PlateauSymbolismV1.McCormick, 2, mcCormick.Position, "2's marker on McCormick");
            Talk(harness, mcCormick, Symbolism, PlateauSymbolismV1.McCormick);
            var mission = harness.Client.Player.Missions[Symbolism];
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[PlateauSymbolismV1.Symbol].State);

            MoveTo(harness, wilderness);
            var earth = Shrine(harness, wilderness, PlateauSymbolismV1.Earth);
            AssertMarker(harness, Symbolism, PlateauSymbolismV1.Symbol, 3, earth.Position, "4's marker on the Earth shrine");
            AssertMarker(harness, Symbolism, PlateauSymbolismV1.Report, 4, topper.Position, "6's marker on Topper");
            Assert.AreEqual(4, harness.World.MissionIndicatorEntries.AsNoTracking().Count(row => row.MissionId == Symbolism));
            UseShrine(harness, wilderness, earth);
            mission = harness.Client.Player.Missions[Symbolism];
            Assert.AreEqual(MissionObjectiveState.Completed, mission.Objectives[PlateauSymbolismV1.Symbol].State, "the Earth Logos");
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[PlateauSymbolismV1.Report].State);
            Assert.IsFalse(mission.Completeable, "Topper wants to hear it");

            Talk(harness, topper, Symbolism, PlateauSymbolismV1.Report);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, topper, Symbolism, null,
                PlateauSymbolismV1.Experience, PlateauSymbolismV1.Credits);
        }

        [TestMethod]
        public void SymbolismCountsAnEarthLogosThePlayerAlreadyHas()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompleted(harness, Mystery);
            using (var unit = harness.CreateChar())
                unit.CharacterLogoses.SetLogos(harness.Client.Player.Id, PlateauSymbolismV1.Earth);
            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();

            var plateau = EnterPlateau(harness);
            Spawn(plateau, PlateauBaileyNpcsV1.BaileyPool);
            var bailey = Npc(plateau, PlateauBaileyNpcsV1.BaileyPool, PlateauBaileyNpcsV1.Bailey);
            harness.MoveTo(bailey.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, bailey.EntityId, Symbolism));
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Inactive,
                harness.Client.Player.Missions[Symbolism].Objectives[PlateauSymbolismV1.Symbol].State, "not before McCormick");

            var pools = Enter(harness, PlateauBaileyNpcsV1.PoolsMap, "adv_foreas_valverde_pools", 520);
            Spawn(pools, PlateauBaileyNpcsV1.HermitPool);
            Talk(harness, Npc(pools, PlateauBaileyNpcsV1.HermitPool, PlateauBaileyNpcsV1.Hermit), Symbolism, PlateauSymbolismV1.Hermit);
            var marshes = Enter(harness, PlateauBaileyNpcsV1.MarshesMap, "adv_foreas_valverde_marshes", 663);
            Spawn(marshes, PlateauBaileyNpcsV1.McCormick);
            Talk(harness, RequireNpc(marshes, PlateauBaileyNpcsV1.McCormick), Symbolism, PlateauSymbolismV1.McCormick);

            var mission = harness.Client.Player.Missions[Symbolism];
            Assert.AreEqual(MissionObjectiveState.Completed, mission.Objectives[PlateauSymbolismV1.Symbol].State, "the Logos counts");
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[PlateauSymbolismV1.Report].State);
        }

        [TestMethod]
        public void RidoutsVortexIsTheLogosInTheRiverCave()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, PlateauBaileyNpcsV1.RidoutPool);
            var ridout = Npc(plateau, PlateauBaileyNpcsV1.RidoutPool, PlateauBaileyNpcsV1.Ridout);
            AssertAt(plateau, ridout, PlateauBaileyNpcsV1.RidoutAt, "Ridout");

            var offer = WildernessRanjaGorgeTests.Offer(harness, ridout, Vortex);
            Assert.AreEqual(0, offer.SelectableReward.Count, "no choice");
            harness.MoveTo(ridout.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, ridout.EntityId, Vortex));
            harness.Drain();
            Assert.IsFalse(harness.Client.Player.Missions[Vortex].Completeable);

            UseShrine(harness, plateau, Shrine(harness, plateau, PlateauLogosVortexV1.Vortex));
            var mission = harness.Client.Player.Missions[Vortex];
            Assert.AreEqual(MissionObjectiveState.Completed, mission.Objectives[PlateauLogosVortexV1.Acquire].State);
            Assert.IsTrue(mission.Completeable);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, ridout, Vortex, null,
                PlateauLogosVortexV1.Experience, PlateauLogosVortexV1.Credits);
        }

        [TestMethod]
        public void TheBattlePlansLieInTheCourierBagBesideTheWreckedWatchtower()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, PlateauBaileyNpcsV1.BaileyPool);
            var bailey = Npc(plateau, PlateauBaileyNpcsV1.BaileyPool, PlateauBaileyNpcsV1.Bailey);
            Assert.IsFalse(plateau.DynamicObjects.Any(obj => obj.SceneMissionId == Plans), "no bag before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, bailey, Plans);
            Assert.AreEqual(0, offer.SelectableReward.Count, "no choice");
            Assert.AreEqual(PlateauMislaidPlansV1.Credits, offer.FixedReward.Credits[CurencyType.Credits]);
            harness.MoveTo(bailey.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, bailey.EntityId, Plans));
            harness.Drain();
            CollectionAssert.AreEquivalent(new[] { PlateauMislaidPlansV1.Retrieve },
                harness.Client.Player.Missions[Plans].Objectives.Keys.ToArray(), "no radiation objective (missing mechanic)");

            var bag = plateau.DynamicObjects.Single(obj => obj.SceneMissionId == Plans && obj.SceneActorRole == "bag");
            Assert.AreEqual(PlateauMislaidPlansV1.BagClass, (uint)bag.EntityClassId);
            Assert.AreEqual((UseObjectState)PlateauMislaidPlansV1.ReadyState, bag.StateId);
            Assert.AreEqual(new Vector3(-552, 395.3f, 174), bag.Position, "south-west of the tower");
            Assert.AreEqual(harness.Client.Player.Id, bag.SceneOwnerCharacterId, "the player's own");
            var ground = plateau.NavMesh.GroundHeight(bag.Position);
            Assert.IsNotNull(ground, "the bag has navmesh under it");
            Assert.IsTrue(System.Math.Abs(ground.Value - bag.Position.Y) < 0.5f, $"the bag at {bag.Position.Y}, ground {ground}");

            harness.MoveTo(bag.Position + new Vector3(0, 0, 1));
            Use(harness, plateau, bag, 1, recovers: false);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauMislaidPlansV1.PlansTemplate), "not with another argument");
            Use(harness, plateau, bag, PlateauMislaidPlansV1.UseArgument, recovers: true);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauMislaidPlansV1.PlansTemplate));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[Plans].Objectives[PlateauMislaidPlansV1.Retrieve].State);
            Assert.AreEqual((UseObjectState)PlateauMislaidPlansV1.TakenState, bag.StateId);
            Assert.IsFalse(bag.IsEnabled);
            Use(harness, plateau, bag, PlateauMislaidPlansV1.UseArgument, recovers: false);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauMislaidPlansV1.PlansTemplate), "one set of plans");

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauMislaidPlansV1.PlansTemplate));
            Assert.IsTrue(harness.Client.Player.Missions[Plans].Completeable);
            harness.Drain();

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, bailey, Plans, null,
                PlateauMislaidPlansV1.Experience, PlateauMislaidPlansV1.Credits);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauMislaidPlansV1.PlansTemplate), "Bailey keeps the plans");
        }

        [TestMethod]
        public void AbandonedPlansGoBackWithTheBag()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, PlateauBaileyNpcsV1.BaileyPool);
            var bailey = Npc(plateau, PlateauBaileyNpcsV1.BaileyPool, PlateauBaileyNpcsV1.Bailey);
            harness.MoveTo(bailey.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, bailey.EntityId, Plans));
            harness.Drain();
            var bag = plateau.DynamicObjects.Single(obj => obj.SceneMissionId == Plans && obj.SceneActorRole == "bag");
            harness.MoveTo(bag.Position + new Vector3(0, 0, 1));
            Use(harness, plateau, bag, PlateauMislaidPlansV1.UseArgument, recovers: true);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauMislaidPlansV1.PlansTemplate));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Plans));
            harness.Drain();
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauMislaidPlansV1.PlansTemplate), "the plans go");
            Assert.IsFalse(plateau.DynamicObjects.Any(obj => obj.SceneMissionId == Plans && obj.IsEnabled), "no bag left to use");
        }

        private static void Use(WildernessRuntimeTestHarness harness, MapChannel plateau, DynamicObject obj, uint argument, bool recovers)
        {
            var count = plateau.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = argument
            });
            Assert.AreEqual(count + (recovers ? 1 : 0), plateau.PerformRecovery.Count, $"argument {argument}");
            if (!recovers)
                return;
            var recovery = plateau.PerformRecovery.Last();
            plateau.PerformRecovery.Remove(recovery);
            ActorActionManager.Instance.PerformRecovery(plateau, recovery);
        }

        /// <summary>The player on another map of the server, as EnterPlateau puts them on the Plateau.</summary>
        private static MapChannel Enter(WildernessRuntimeTestHarness harness, uint context, string name, uint version)
        {
            if (!harness.Maps.MapChannelArray.TryGetValue(context, out var map))
            {
                map = new MapChannel
                {
                    MapInfo = new MapInfo(context, name, version, 0),
                    ClientList = new List<Client>(),
                    PlayerLimit = 128,
                    NavMesh = TestNavMeshes.Query(NavMeshFile.PathFor(Path.Combine(RepositoryRoot(), "navmesh"), name))
                };
                harness.Maps.MapChannelArray.Add(context, map);
                SpawnPoolManager.Instance.InitializeMapChannel(map);
            }
            MoveTo(harness, map);
            return map;
        }

        private static void MoveTo(WildernessRuntimeTestHarness harness, MapChannel map)
        {
            var client = harness.Client;
            CellManager.Instance.RemoveFromWorld(client);
            client.Player.MapChannel.ClientList.Remove(client);
            client.Player.MapChannel = map;
            client.Player.RuntimeMapChannel = map;
            client.Player.MapContextId = map.MapInfo.MapContextId;
            map.ClientList.Add(client);
            CellManager.Instance.AddToWorld(client);
            harness.Drain();
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(System.AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;
            Assert.IsNotNull(directory, "the repository's navmesh directory");
            return directory.FullName;
        }

        /// <summary>The objective's one marker, <paramref name="indicator"/>, within 2 m of <paramref name="at"/> (x and z).</summary>
        private static void AssertMarker(WildernessRuntimeTestHarness harness, uint mission, uint objective, uint indicator,
            Vector3 at, string what)
        {
            var row = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Single(entry => entry.MissionId == mission && entry.ObjectiveId == objective);
            Assert.AreEqual(MissionIndicator.UnnamedFrom + indicator, row.IndicatorId, what);
            Assert.AreEqual(0f, Vector2.Distance(new Vector2((float)row.PosX, (float)row.PosZ), new Vector2(at.X, at.Z)), 2f, what);
        }

        /// <summary>The NPC stands where its pool says, on the navmesh.</summary>
        private static void AssertAt(MapChannel map, Creature npc, (double X, double Y, double Z) at, string what)
        {
            Assert.IsTrue(Vector2.Distance(new Vector2(npc.Position.X, npc.Position.Z), new Vector2((float)at.X, (float)at.Z)) < 0.5f,
                $"{what}: {npc.Position}");
            var ground = map.NavMesh.GroundHeight(new Vector3((float)at.X, (float)at.Y, (float)at.Z));
            Assert.IsNotNull(ground, $"{what} has navmesh under it");
            Assert.IsTrue(System.Math.Abs(ground.Value - at.Y) < 0.5f, $"{what} at {at.Y}, ground {ground}");
        }

        /// <summary>A public NPC of its own pool (the pool's id is not the creature's).</summary>
        private static Creature Npc(MapChannel map, uint pool, uint creature)
        {
            var npc = Living(map).Single(candidate => candidate.SpawnPool?.DbId == pool && candidate.Npc != null);
            Assert.AreEqual(creature, npc.DbId);
            return npc;
        }

        /// <summary>The server's shrine, from the World's logos table, on its map.</summary>
        private static Logos Shrine(WildernessRuntimeTestHarness harness, MapChannel map, uint id)
        {
            var existing = map.DynamicObjects.OfType<Logos>().SingleOrDefault(shrine => shrine.Id == id);
            if (existing != null)
                return existing;
            var entry = harness.World.LogosEntries.AsNoTracking().Single(row => row.Id == id);
            Assert.AreEqual(map.MapInfo.MapContextId, entry.MapContextId);
            var created = new Logos(entry);
            map.DynamicObjects.Add(created);
            harness.Objects.DynamicObjectWorker(map, 0);
            harness.Drain();
            return created;
        }

        /// <summary>The shrine used, from where it stands, and the use's ten seconds run out (LogosShrineUseArgTests).</summary>
        private static void UseShrine(WildernessRuntimeTestHarness harness, MapChannel map, Logos shrine)
        {
            harness.MoveTo(shrine.Position);
            harness.Drain();
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = shrine.EntityId, ActionId = ActionId.UseObject, ActionArgId = DynamicObjectManager.LogosUseArgId
            });
            ActorActionManager.Instance.DoWork(map, 10001);
            harness.Drain();
        }

        private static bool Offers(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.MissionDispense, out var offered) &&
            ((Dictionary<uint, MissionInfo>)offered).ContainsKey(mission);

        /// <summary>The NPC's hand-in topic of this objective, and the objective completes there.</summary>
        private static void Talk(WildernessRuntimeTestHarness harness, Creature npc, uint mission, uint objective)
        {
            AssertHandInTopic(Conversation(harness, npc), mission, objective);
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, mission, objective, 1),
                $"{mission}.{objective}");
            harness.Drain();
        }
    }
}
