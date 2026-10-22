extern alias RasaGame;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Missions.Content.Plateau;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Plateau
{
    using static PlateauCampResistanceTests;

    /// <summary>
    /// Outpost Cmdr. Russ's missions into the quarry and the chasm, Plateau, played through as a player plays
    /// them: Bomb Voyage (931), Lookout Down Below (970), and Field Sgt. Garde's The Stainless Steel Bloodhound
    /// (867, turned in by radio), after which Headquarters calls with Behind Enemy Lines (555, to Colonel Bosley).
    ///
    /// Why these tests: as PlateauPerduTests. The crates are used as Soyuz Salvage's debris
    /// (PlateauWedgeRockTests); the sentinel walks as Clean Slate's soldier (PlateauPerduTests); the radio
    /// turn-in is MissionRadioLifecycleTests' TryCompleteRadioMission.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauChasmTests
    {
        private const uint BombVoyage = PlateauBombVoyageV1.MissionId;
        private const uint Lookout = PlateauLookoutDownBelowV1.MissionId;
        private const uint Bloodhound = PlateauBloodhoundV1.MissionId;
        private const uint Garde = PlateauBloodhoundV1.Garde;      // creature and pool
        private const uint Bomb = PlateauBombVoyageV1.Bomb;
        private const uint BehindEnemyLines = PlateauBehindEnemyLinesV1.MissionId;
        private const uint Bosley = PlateauBehindEnemyLinesV1.Bosley;   // creature and pool

        [TestMethod]
        public void HeadquartersCallsAfterTheBloodhoundAndBosleyTakesTheReport()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Bosley);
            var bosley = RequireNpc(plateau, Bosley);
            Assert.IsFalse(Offers(harness, bosley, BehindEnemyLines), "no NPC offers it");
            SeedCompleteable(harness, Bloodhound);
            Assert.IsTrue(harness.Client.Player.Missions[Bloodhound].Completeable);
            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.IsFalse(Called(harness), "not before the Bloodhound is turned in");

            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, Bloodhound, 0, null));
            var packets = harness.Drain().ToList();
            Assert.AreEqual(BehindEnemyLines, packets.OfType<DispenseRadioMissionPacket>().Single().MissionId);
            Assert.IsLessThan(packets.FindIndex(packet => packet is DispenseRadioMissionPacket),
                packets.FindIndex(packet => packet is MissionRewardedPacket), "the call comes after the reward");

            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, BehindEnemyLines));
            Assert.AreEqual(BehindEnemyLines, harness.Drain().OfType<MissionGainedPacket>().Single().MissionId);
            var mission = harness.Client.Player.Missions[BehindEnemyLines];
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[PlateauBehindEnemyLinesV1.Report].State);
            Assert.IsFalse(mission.Completeable);

            WildernessRanjaGorgeTests.Reload(harness);
            Talk(harness, bosley, BehindEnemyLines, PlateauBehindEnemyLinesV1.Report);
            Assert.IsTrue(harness.Client.Player.Missions[BehindEnemyLines].Completeable);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, bosley, BehindEnemyLines, 2,
                PlateauBehindEnemyLinesV1.Experience, PlateauBehindEnemyLinesV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauBehindEnemyLinesV1.MechVest), "the selected vest");
        }

        [TestMethod]
        public void ACallNotTakenComesAgainOnArrivalOnThePlateau()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.IsFalse(Called(harness), "nothing before the Bloodhound");

            SeedCompleted(harness, Bloodhound);
            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.IsTrue(Called(harness), "called on arrival");
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, BehindEnemyLines));
            harness.Drain();

            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.IsFalse(Called(harness), "not while the player holds it");
        }

        /// <summary>Headquarters offered Behind Enemy Lines by radio (another Plateau mission, 1586, is offered on arrival too).</summary>
        private static bool Called(WildernessRuntimeTestHarness harness) =>
            harness.Drain().OfType<DispenseRadioMissionPacket>().Any(offer => offer.MissionId == BehindEnemyLines);

        /// <summary>An active mission with its required objectives done, ready to turn in (BootcampRuntimeTestHarness.SeedMission).</summary>
        private static void SeedCompleteable(WildernessRuntimeTestHarness harness, uint missionId)
        {
            var characterId = harness.Client.Player.Id;
            var definition = harness.Manager.LoadedMissions[missionId];
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    unit.CharacterMissions.Add(new CharacterMissionEntry(characterId, missionId, (uint)MissionState.Active)
                    {
                        Completeable = true
                    });
                    unit.CharacterMissionProgress.AddObjectives(definition.Objectives.Values.Select(objective =>
                        new CharacterMissionObjectiveEntry(characterId, missionId, objective.ObjectiveId,
                            (byte)(objective.IsRequired.Value ? MissionObjectiveState.Completed : objective.InitialState.Value))));
                });
            WildernessRanjaGorgeTests.Reload(harness);
        }

        [TestMethod]
        public void BombVoyagePlantsOneOfRusssSixBombsInEachOfSixCrates()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, PlateauPerduNpcsV1.RussPool);
            var russ = Npc(plateau, PlateauPerduNpcsV1.RussPool, PlateauPerduNpcsV1.Russ);
            Assert.IsFalse(plateau.DynamicObjects.Any(obj => obj.SceneMissionId == BombVoyage), "no crates before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, russ, BombVoyage);
            CollectionAssert.AreEquivalent(new[] { PlateauBombVoyageV1.BioVest, PlateauBombVoyageV1.GravitonVest,
                PlateauBombVoyageV1.MechVest, PlateauBombVoyageV1.StealthVest },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray(), "the four vests");
            Assert.AreEqual(PlateauBombVoyageV1.Credits, offer.FixedReward.Credits[CurencyType.Credits]);
            harness.MoveTo(russ.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, russ.EntityId, BombVoyage));
            Assert.AreEqual(BombVoyage, harness.Drain().OfType<MissionGainedPacket>().Single().MissionId);
            Assert.AreEqual(PlateauBombVoyageV1.Bombs, WildernessRanjaGorgeTests.Held(harness, Bomb), "Russ's six bombs");

            var crates = Enumerable.Range(0, PlateauBombVoyageV1.Crates.Length).Select(index => plateau.DynamicObjects.Single(obj =>
                obj.SceneMissionId == BombVoyage && obj.SceneActorRole == PlateauBombVoyageV1.CrateRole(index))).ToArray();
            for (var index = 0; index < crates.Length; index++)
            {
                var at = PlateauBombVoyageV1.Crates[index];
                Assert.AreEqual(PlateauBombVoyageV1.CrateClass, (uint)crates[index].EntityClassId);
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), crates[index].Position);
                Assert.IsTrue(Vector2.Distance(new Vector2(at.X, at.Z), new Vector2(348.79f, -230.05f)) < 20, "by the landing pad");
                var ground = plateau.NavMesh.GroundHeight(crates[index].Position);
                Assert.IsNotNull(ground, $"crate {index + 1} has navmesh under it");
                // On the client's terrain; the quarry floor's navmesh lies up to 0.8 m over it here.
                Assert.IsTrue(System.Math.Abs(ground.Value - at.Y) < 1f, $"crate {index + 1} at {at.Y}, ground {ground}");
            }

            var mission = harness.Client.Player.Missions[BombVoyage];
            harness.MoveTo(crates[0].Position + new Vector3(0, 0, 1));
            Use(harness, plateau, crates[0], 1, recovers: false);
            Assert.AreEqual(PlateauBombVoyageV1.Bombs, WildernessRanjaGorgeTests.Held(harness, Bomb), "the wrong use plants nothing");
            Use(harness, plateau, crates[0], PlateauBombVoyageV1.UseArgument, recovers: true);
            Assert.AreEqual(5U, WildernessRanjaGorgeTests.Held(harness, Bomb), "one bomb in the first crate");
            Assert.IsFalse(crates[0].IsEnabled, "a crate takes one bomb");
            Use(harness, plateau, crates[0], PlateauBombVoyageV1.UseArgument, recovers: false);
            Assert.AreEqual(5U, WildernessRanjaGorgeTests.Held(harness, Bomb), "the same crate counts once");

            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();
            Assert.AreEqual(5U, WildernessRanjaGorgeTests.Held(harness, Bomb));

            for (var index = 1; index < crates.Length; index++)
            {
                Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[BombVoyage]
                    .Objectives[PlateauBombVoyageV1.Plant].State, $"before crate {index + 1}");
                harness.MoveTo(crates[index].Position + new Vector3(0, 0, 1));
                Use(harness, plateau, crates[index], PlateauBombVoyageV1.UseArgument, recovers: true);
            }
            mission = harness.Client.Player.Missions[BombVoyage];
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, Bomb), "all six planted");
            Assert.AreEqual(MissionObjectiveState.Completed, mission.Objectives[PlateauBombVoyageV1.Plant].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[PlateauBombVoyageV1.Report].State);
            Assert.IsFalse(mission.Completeable, "Russ wants to hear it first");

            Talk(harness, russ, BombVoyage, PlateauBombVoyageV1.Report);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, russ, BombVoyage, 1,
                PlateauBombVoyageV1.Experience, PlateauBombVoyageV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauBombVoyageV1.GravitonVest), "the second choice");
        }

        [TestMethod]
        public void BombVoyageAbandonedTakesTheBombsAndTheCrates()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, PlateauPerduNpcsV1.RussPool);
            var russ = Npc(plateau, PlateauPerduNpcsV1.RussPool, PlateauPerduNpcsV1.Russ);
            harness.MoveTo(russ.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, russ.EntityId, BombVoyage));
            harness.Drain();
            var crate = plateau.DynamicObjects.Single(obj => obj.SceneMissionId == BombVoyage &&
                obj.SceneActorRole == PlateauBombVoyageV1.CrateRole(0));
            harness.MoveTo(crate.Position + new Vector3(0, 0, 1));
            Use(harness, plateau, crate, PlateauBombVoyageV1.UseArgument, recovers: true);
            Assert.AreEqual(5U, WildernessRanjaGorgeTests.Held(harness, Bomb));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, BombVoyage));
            harness.Drain();
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, Bomb), "the bombs go with the mission");
            Assert.IsFalse(plateau.DynamicObjects.Any(obj => obj.SceneMissionId == BombVoyage && obj.IsEnabled), "no crate to use");
        }

        [TestMethod]
        public void LookoutDownBelowSendsThePlayerToGardeWhoseBloodhoundFollows()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, PlateauPerduNpcsV1.RussPool, Garde);
            var russ = Npc(plateau, PlateauPerduNpcsV1.RussPool, PlateauPerduNpcsV1.Russ);
            var garde = RequireNpc(plateau, Garde);
            Assert.IsFalse(Offers(harness, garde, Bloodhound), "the Bloodhound waits for Lookout Down Below");

            var offer = WildernessRanjaGorgeTests.Offer(harness, russ, Lookout);
            Assert.AreEqual(0, offer.SelectableReward.Count, "no choice");
            harness.MoveTo(russ.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, russ.EntityId, Lookout));
            harness.Drain();
            Assert.IsFalse(HandsIn(harness, russ, Lookout, 1), "Russ is not Garde");

            Talk(harness, garde, Lookout, 1);
            Assert.IsTrue(harness.Client.Player.Missions[Lookout].Completeable);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, garde, Lookout, null,
                PlateauLookoutDownBelowV1.Experience, PlateauLookoutDownBelowV1.Credits);
            foreach (var (template, quantity) in PlateauLookoutDownBelowV1.Items)
                Assert.AreEqual(quantity, WildernessRanjaGorgeTests.Held(harness, template), $"{template}");
            Assert.IsTrue(Offers(harness, garde, Bloodhound), "Garde's Bloodhound opens");
        }

        [TestMethod]
        public void TheBloodhoundsSentinelWaitsForGardeThenLeadsToTheCaveAndRussCallsIn()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Garde);
            var garde = RequireNpc(plateau, Garde);
            SeedCompleted(harness, Lookout);

            var offer = WildernessRanjaGorgeTests.Offer(harness, garde, Bloodhound);
            CollectionAssert.AreEquivalent(new[] { PlateauBloodhoundV1.InjectorGun, PlateauBloodhoundV1.PulseChaingun,
                PlateauBloodhoundV1.PulsePistol, PlateauBloodhoundV1.LaserCannon },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray(), "the four green weapons");
            harness.MoveTo(garde.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, garde.EntityId, Bloodhound));
            harness.Drain();
            Assert.IsTrue(harness.Manager.LoadedMissions[Bloodhound].RadioCompletable == true);
            Tick(harness, plateau, 250);

            Assert.IsNull(SceneCreature(plateau, "sentinel"), "no sentinel before Garde's word");
            var mission = harness.Client.Player.Missions[Bloodhound];
            Assert.AreEqual(MissionObjectiveState.Inactive, mission.Objectives[PlateauBloodhoundScene.Escort].State);

            Talk(harness, garde, Bloodhound, PlateauBloodhoundScene.Ready);
            Tick(harness, plateau, 250);
            mission = harness.Client.Player.Missions[Bloodhound];
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[PlateauBloodhoundScene.Escort].State);
            var sentinel = SceneCreature(plateau, "sentinel");
            Assert.IsNotNull(sentinel, "Garde activates the sentinel");
            Assert.AreEqual(PlateauChasmNpcsV1.Sentinel, sentinel.DbId);
            Assert.AreEqual(TargetCategory.Friendly, sentinel.TargetCategory);
            Assert.IsTrue(Vector3.Distance(garde.Position, sentinel.Position) < 20, $"beside Garde: {sentinel.Position}");

            for (var tick = 0; tick < 600 &&
                 harness.Client.Player.Missions[Bloodhound].Objectives[PlateauBloodhoundScene.Escort].State != MissionObjectiveState.Completed; tick++)
            {
                if (SceneCreature(plateau, "sentinel") != null)
                    harness.MoveTo(sentinel.Position + Vector3.UnitZ);
                Tick(harness, plateau, 1000);
            }
            mission = harness.Client.Player.Missions[Bloodhound];
            Assert.AreEqual(MissionObjectiveState.Completed, mission.Objectives[PlateauBloodhoundScene.Escort].State,
                $"the sentinel stalled at {sentinel.Position}");
            var cave = PlateauBloodhoundV1.Route[^1];
            Assert.IsTrue(Vector3.Distance(new Vector3(cave.X, cave.Y, cave.Z), sentinel.Position) < 3, $"at the cave: {sentinel.Position}");
            Assert.IsNull(SceneCreature(plateau, "sentinel"), "it is gone into the cave");
            Assert.IsTrue(mission.Completeable);
            Assert.IsFalse(Conversation(harness, garde).ContainsKey(ConversationType.MissionComplete), "no NPC turn-in");

            var before = harness.Context.ReadRewardTotals();
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, Bloodhound, 1, null));
            Assert.AreEqual(BehindEnemyLines, harness.Drain().OfType<DispenseRadioMissionPacket>().Single().MissionId,
                "Headquarters calls with Behind Enemy Lines");
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[Bloodhound].State);
            Assert.AreEqual(before.Credits + PlateauBloodhoundV1.Credits, harness.Context.ReadRewardTotals().Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauBloodhoundV1.PulseChaingun), "the second choice");
        }

        [TestMethod]
        public void TheBloodhoundFailsWhenTheSentinelDies()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Garde);
            var garde = RequireNpc(plateau, Garde);
            SeedCompleted(harness, Lookout);
            harness.MoveTo(garde.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, garde.EntityId, Bloodhound));
            harness.Drain();
            Talk(harness, garde, Bloodhound, PlateauBloodhoundScene.Ready);
            Tick(harness, plateau, 250);
            var sentinel = SceneCreature(plateau, "sentinel");
            for (var tick = 0; tick < 10; tick++)
                Tick(harness, plateau, 1000);
            Assert.IsTrue(Vector3.Distance(PlateauBloodhoundV1.Route[0].ToVector(), sentinel.Position) > 5, "it is on its way");

            sentinel.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(plateau, sentinel, null);
            Tick(harness, plateau, 250);
            var mission = harness.Client.Player.Missions[Bloodhound];
            Assert.AreEqual(MissionObjectiveState.Failed, mission.Objectives[PlateauBloodhoundScene.Escort].State);
            Assert.AreEqual(MissionState.Failed, mission.State);
            Assert.IsFalse(mission.Completeable);
        }

        /// <summary>As WildernessRuntimeTestHarness.Tick, on the Plateau.</summary>
        private static void Tick(WildernessRuntimeTestHarness harness, MapChannel plateau, long milliseconds)
        {
            harness.UtcNow = harness.UtcNow.AddMilliseconds(milliseconds);
            BehaviorManager.Instance.MapChannelThink(plateau, milliseconds);
            harness.Manager.Scenes.Tick(plateau);
            harness.Drain();
        }

        private static Creature SceneCreature(MapChannel plateau, string role) => Living(plateau)
            .SingleOrDefault(creature => creature.SpawnPool?.SceneActorRole == role);

        /// <summary>A public NPC of its own pool (the pool's id is not the creature's).</summary>
        private static Creature Npc(MapChannel plateau, uint pool, uint creature)
        {
            var npc = Living(plateau).Single(candidate => candidate.SpawnPool?.DbId == pool && candidate.Npc != null);
            Assert.AreEqual(creature, npc.DbId);
            return npc;
        }

        private static bool Offers(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.MissionDispense, out var offered) &&
            ((Dictionary<uint, MissionInfo>)offered).ContainsKey(mission);

        private static bool HandsIn(WildernessRuntimeTestHarness harness, Creature npc, uint mission, uint objective)
        {
            harness.MoveTo(npc.Position);
            harness.Drain();
            return harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, mission, objective, 1);
        }

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

        /// <summary>A use of the object: the request, then its recovery if one is due.</summary>
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
            harness.Drain();
        }
    }
}
