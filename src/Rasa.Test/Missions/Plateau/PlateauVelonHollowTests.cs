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

        private static bool Offers(WildernessRuntimeTestHarness harness, Creature npc) =>
            Conversation(harness, npc).TryGetValue(ConversationType.MissionDispense, out var offered) &&
            ((System.Collections.Generic.Dictionary<uint, MissionInfo>)offered).ContainsKey(OrderOfVelon);
    }
}
