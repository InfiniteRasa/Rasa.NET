extern alias RasaGame;

using System;
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
    using static PlateauInstanceMaps;

    /// <summary>
    /// Vogren's hologram's five instanced Trinity Bridge Logos (1047 Future, 1048 Disperse, 1049 Logos,
    /// 1051 Far, 1057 Distance), played through as a player plays them: offered at Vogren's Tomb after
    /// Trinity Bridge Logos: Past, the Logos taken in the squad's own copy of its map, turned in back
    /// at the tomb.
    ///
    /// Why these tests: the Logos are the server's shrines (table logos) of maps other than the
    /// Plateau, and the player gets them in a squad instance, a copy of the map that MapChannelManager
    /// makes as they go through its door (EnterMap). A copy that left the shrines out would leave the
    /// missions unfinishable; one that shared them would take one squad's use for another's. So the
    /// door is walked through as MapLinkManager walks it, and the shrine used is the copy's.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauTrinityBridgeLogosTests
    {
        private const uint LogosPast = PlateauTrinityLogosPastV1.MissionId;

        public static IEnumerable<object[]> Missions =>
            PlateauTrinityBridgeLogosV1.All.Select(logos => new object[] { logos.MissionId });

        private static PlateauTrinityBridgeLogosV1.Logos Of(uint mission) =>
            PlateauTrinityBridgeLogosV1.All.Single(logos => logos.MissionId == mission);

        [TestMethod]
        [DynamicData(nameof(Missions))]
        public void TheLogosIsTakenInTheSquadsOwnCopyOfItsMapAndTurnedInAtTheTomb(uint missionId)
        {
            var logos = Of(missionId);
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            SeedCompleted(harness, PlateauVoiceFromThePastV1.MissionId, LogosPast);
            var hologram = Hologram(plateau);

            var offer = WildernessRanjaGorgeTests.Offer(harness, hologram, missionId);
            Assert.AreEqual(0, offer.SelectableReward.Count, "no item: the Logos is the reward");
            Assert.AreEqual(0, offer.FixedReward.FixedItems.Count);
            harness.MoveTo(hologram.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, hologram.EntityId, missionId));
            harness.Drain();
            Assert.IsFalse(harness.Client.Player.Missions[missionId].Completeable);

            var template = LoadMap(harness, logos.MapContextId);
            var copy = ThroughTheDoor(harness, logos.MapContextId);
            Assert.AreNotSame(template, copy, "the squad's own copy, not the map's channel");
            Assert.IsTrue(copy.IsSquadInstance);

            var shrine = copy.DynamicObjects.OfType<Logos>().Single(entry => entry.Id == logos.LogosId);
            Assert.AreNotSame(template.DynamicObjects.OfType<Logos>().Single(entry => entry.Id == logos.LogosId), shrine,
                "the copy has a shrine of its own");
            // 1's marker at the shrine, in the instance too.
            var marker = harness.World.MissionIndicatorEntries.AsNoTracking().Where(row => row.MissionId == missionId).ToList();
            Assert.HasCount(1, marker);
            Assert.AreEqual(1U, marker[0].ObjectiveId);
            Assert.AreEqual(0f, Vector2.Distance(new((float)marker[0].PosX, (float)marker[0].PosZ),
                new(shrine.Position.X, shrine.Position.Z)), 1f, "1's marker at the shrine");
            UseShrine(harness, copy, shrine);
            Assert.IsTrue(harness.Client.Player.Logos.Contains(logos.LogosId));
            Assert.IsTrue(harness.Client.Player.Missions[missionId].Completeable, "the Logos completes it, in the instance");

            Return(harness, plateau);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, hologram, missionId, null,
                PlateauTrinityBridgeLogosV1.Experience, logos.Credits);
        }

        [TestMethod]
        public void TheFiveWaitForTrinityBridgeLogosPast()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            SeedCompleted(harness, PlateauVoiceFromThePastV1.MissionId);
            var hologram = Hologram(plateau);

            var offered = Offered(harness, hologram);
            Assert.IsTrue(offered.Contains(LogosPast));
            foreach (var logos in PlateauTrinityBridgeLogosV1.All)
                Assert.IsFalse(offered.Contains(logos.MissionId), $"{logos.MissionId} waits for Past");

            SeedCompleted(harness, LogosPast);
            offered = Offered(harness, hologram);
            foreach (var logos in PlateauTrinityBridgeLogosV1.All)
                Assert.IsTrue(offered.Contains(logos.MissionId), $"{logos.MissionId} after Past");
        }

        [TestMethod]
        public void ALogosAlreadyLearnedCounts()
        {
            var logos = PlateauTrinityBridgeLogosV1.Distance;
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            SeedCompleted(harness, PlateauVoiceFromThePastV1.MissionId, LogosPast);
            var hologram = Hologram(plateau);

            LoadMap(harness, logos.MapContextId);
            var copy = ThroughTheDoor(harness, logos.MapContextId);
            UseShrine(harness, copy, copy.DynamicObjects.OfType<Logos>().Single(entry => entry.Id == logos.LogosId));
            Assert.IsTrue(harness.Client.Player.Logos.Contains(logos.LogosId));
            Return(harness, plateau);

            harness.MoveTo(hologram.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, hologram.EntityId, logos.MissionId));
            harness.Drain();
            Assert.IsTrue(harness.Client.Player.Missions[logos.MissionId].Completeable, "the Logos the player has counts");
        }

        /// <summary>The hologram in Vogren's Tomb, spawned from its pool.</summary>
        internal static Creature Hologram(MapChannel plateau)
        {
            Spawn(plateau, PlateauVogrenNpcsV1.HologramPool);
            var hologram = Living(plateau).Single(candidate => candidate.SpawnPool?.DbId == PlateauVogrenNpcsV1.HologramPool && candidate.Npc != null);
            Assert.AreEqual(PlateauVogrenNpcsV1.Hologram, hologram.DbId);
            return hologram;
        }

        private static HashSet<uint> Offered(WildernessRuntimeTestHarness harness, Creature npc) =>
            Conversation(harness, npc).TryGetValue(ConversationType.MissionDispense, out var offered)
                ? ((Dictionary<uint, MissionInfo>)offered).Keys.ToHashSet()
                : new HashSet<uint>();

        /// <summary>The shrine used, from where it stands, and the use's ten seconds run out (LogosShrineUseArgTests).</summary>
        internal static void UseShrine(WildernessRuntimeTestHarness harness, MapChannel map, Logos shrine)
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
    }
}
