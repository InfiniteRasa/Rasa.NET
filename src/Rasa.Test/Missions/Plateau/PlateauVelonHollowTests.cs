extern alias RasaGame;

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Plateau
{
    using static PlateauCampResistanceTests;
    using static PlateauTrinityBridgeLogosTests;

    /// <summary>
    /// Luminary Sampei's The Order Of Velon (1982), played through as a player plays it: offered at New
    /// Velon Village after Village Of The Damned, Elder Hathan found in the squad's own copy of Velon
    /// Hollow (2136), and the mission handed in to him there.
    ///
    /// Why these tests: Hathan is the server's own NPC of Velon Hollow (pool 510152) and stands only in
    /// the instance, so the talk and the hand-in are done in the squad's copy of the map, which
    /// MapChannelManager makes as the player goes through the door. The reward is the reconstructed one
    /// (one Class V Standard Med Pack, 45,000 XP, no credits).
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauVelonHollowTests
    {
        private const uint OrderOfVelon = PlateauOrderOfVelonV1.MissionId;

        [TestMethod]
        public void SampeiSendsThePlayerToElderHathanInTheSquadsVelonHollow()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, PlateauVillageOfTheDamnedV1.Sampei);
            var sampei = RequireNpc(plateau, PlateauVillageOfTheDamnedV1.Sampei);

            Assert.IsFalse(Offers(harness, sampei), "it waits for Village Of The Damned");
            SeedCompleted(harness, PlateauVillageOfTheDamnedV1.MissionId);

            var offer = WildernessRanjaGorgeTests.Offer(harness, sampei, OrderOfVelon);
            Assert.AreEqual(0, offer.SelectableReward.Count);
            CollectionAssert.AreEquivalent(new[] { (PlateauOrderOfVelonV1.MedPack, 1U) },
                offer.FixedReward.FixedItems.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            harness.MoveTo(sampei.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, sampei.EntityId, OrderOfVelon));
            harness.Drain();

            LoadMap(harness, PlateauOrderOfVelonV1.VelonHollow);
            var copy = ThroughTheDoor(harness, PlateauOrderOfVelonV1.VelonHollow);
            Assert.IsTrue(copy.IsSquadInstance);
            Spawn(copy, PlateauOrderOfVelonV1.Hathen);
            var hathen = RequireNpc(copy, PlateauOrderOfVelonV1.Hathen);
            Assert.AreEqual(PlateauOrderOfVelonV1.HathenPackage, hathen.Npc.NpcPackageId);

            AssertHandInTopic(Conversation(harness, hathen), OrderOfVelon, 1);
            harness.MoveTo(hathen.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, hathen.EntityId, OrderOfVelon, 1, 1));
            harness.Drain();
            Assert.IsTrue(harness.Client.Player.Missions[OrderOfVelon].Completeable);

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, hathen, OrderOfVelon, null,
                PlateauOrderOfVelonV1.Experience, PlateauOrderOfVelonV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauOrderOfVelonV1.MedPack));
        }

        /// <summary>
        /// Deadly Silence (1974): Hathen is found by a talk, the three corrupted leaders by killing them at
        /// their sites in the squad's copy; the mission waits for all four and goes back to Sampei.
        /// </summary>
        [TestMethod]
        public void DeadlySilenceFindsHathenAndKillsTheThreeCorruptedLeaders()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, PlateauVillageOfTheDamnedV1.Sampei);
            var sampei = RequireNpc(plateau, PlateauVillageOfTheDamnedV1.Sampei);
            Assert.IsFalse(Offers(harness, sampei, DeadlySilence), "it waits for Village Of The Damned");
            SeedCompleted(harness, PlateauVillageOfTheDamnedV1.MissionId);

            var offer = WildernessRanjaGorgeTests.Offer(harness, sampei, DeadlySilence);
            CollectionAssert.AreEqual(new[]
                {
                    PlateauDeadlySilenceV1.BioBoots, PlateauDeadlySilenceV1.GravitonBoots,
                    PlateauDeadlySilenceV1.MechBoots, PlateauDeadlySilenceV1.StealthBoots
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            harness.MoveTo(sampei.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, sampei.EntityId, DeadlySilence));
            harness.Drain();

            LoadMap(harness, PlateauOrderOfVelonV1.VelonHollow);
            var copy = ThroughTheDoor(harness, PlateauOrderOfVelonV1.VelonHollow);
            Spawn(copy, PlateauOrderOfVelonV1.Hathen);
            Spawn(copy, PlateauVelonElderBossesV1.All.Select(boss => boss.Pool).ToArray());
            var hathen = RequireNpc(copy, PlateauOrderOfVelonV1.Hathen);

            AssertHandInTopic(Conversation(harness, hathen), DeadlySilence, 1);
            harness.MoveTo(hathen.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, hathen.EntityId, DeadlySilence, 1, 1));
            harness.Drain();
            Assert.IsFalse(harness.Client.Player.Missions[DeadlySilence].Completeable, "three are still to be found");

            foreach (var (objective, boss, _, _) in PlateauDeadlySilenceV1.Leaders)
            {
                var creature = Living(copy).Single(candidate => candidate.DbId == boss.Id);
                Assert.IsNull(creature.Npc, $"{boss.Name} is no NPC");
                Assert.AreEqual(PlateauVelonElderBossesV1.Level, creature.Level);
                Assert.AreEqual(boss.NameId, creature.NameId);
                Assert.AreEqual((int)PlateauVelonElderBossesV1.HitPoints, (int)creature.Attributes[Attributes.Health].Current, $"{boss.Name} hit points");
                var ground = copy.NavMesh.GroundHeight(creature.Position);
                Assert.IsNotNull(ground, $"{boss.Name} has no navmesh under it");
                Assert.AreEqual(ground.Value, creature.Position.Y, 1f, $"{boss.Name} on the ground");

                Assert.AreNotEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[DeadlySilence].Objectives[objective].State);
                harness.MoveTo(creature.Position);
                creature.Attributes[Attributes.Health].Current = 0;
                harness.Creatures.HandleCreatureKill(copy, creature, harness.Client.Player);
                harness.Drain();
                Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[DeadlySilence].Objectives[objective].State, boss.Name);
            }
            Assert.IsTrue(harness.Client.Player.Missions[DeadlySilence].Completeable);

            Return(harness, plateau);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, sampei, DeadlySilence, 3,
                PlateauDeadlySilenceV1.Experience, PlateauDeadlySilenceV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauDeadlySilenceV1.StealthBoots), "the fourth choice");
        }

        private const uint DeadlySilence = PlateauDeadlySilenceV1.MissionId;

        private static bool Offers(WildernessRuntimeTestHarness harness, Creature npc, uint mission = OrderOfVelon) =>
            Conversation(harness, npc).TryGetValue(ConversationType.MissionDispense, out var offered) &&
            ((System.Collections.Generic.Dictionary<uint, MissionInfo>)offered).ContainsKey(mission);
    }
}
