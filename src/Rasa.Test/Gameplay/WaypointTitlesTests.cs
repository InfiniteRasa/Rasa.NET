using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;

    /// <summary>
    /// "Wilderness Pathfinder: Gained every AFS Waypoint on Wilderness." A character who has
    /// gained every waypoint of a battlefield has its title; no mission is asked.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class WaypointTitlesTests
    {
        private const uint WildernessPathfinder = 362;
        private const uint CruciblePathfinder = 414;
        private const uint AbyssPathfinder = 448;
        private const uint DividePathfinder = 447;
        private const uint DivideWanderer = 462;
        private const uint MarshesPathfinder = 491;
        private const uint MarshesWanderer = 493;

        // The seven of the recovered Wilderness objective (MissionDefinitionCatalog, 1449/1);
        // 156, Imperial Valley, is a control point's.
        private static readonly uint[] Wilderness = { 49, 50, 51, 57, 61, 73, 156 };
        private static readonly uint[] Crucible = { 191, 192, 193, 204, 269, 313, 315 };
        private static readonly uint[] Abyss = { 84, 276, 278, 372, 373, 376 };

        [TestCleanup]
        public void ForgetTheWaypoints() => WaypointTitles.Load(null);

        [TestMethod]
        public void ATitleAsksForTheWaypointsOfItsBattlefieldsOwnMap()
        {
            using var harness = Start();

            CollectionAssert.AreEquivalent(Wilderness, WaypointTitles.WaypointsOf(WildernessPathfinder).ToArray());
            CollectionAssert.AreEquivalent(Crucible, WaypointTitles.WaypointsOf(CruciblePathfinder).ToArray());
            CollectionAssert.AreEquivalent(Abyss, WaypointTitles.WaypointsOf(AbyssPathfinder).ToArray());

            var counts = WaypointTitles.Zones.ToDictionary(zone => zone.Name, zone => WaypointTitles.WaypointsOf(zone.TitleId).Count);
            CollectionAssert.AreEquivalent(
                new Dictionary<string, int>
                {
                    ["Wilderness"] = 7, ["Divide"] = 11, ["Palisades"] = 8, ["Plateau"] = 11, ["Pools"] = 9, ["Marshes"] = 6,
                    ["Descent"] = 11, ["Ashen Desert"] = 7, ["Thunderhead"] = 11, ["Abyss"] = 6, ["Crucible"] = 7
                }.ToArray(),
                counts.ToArray());

            // Waypoints only: no dropship pad, no hospital, no instance's, none the client has no name for.
            var world = harness.WorldContext.Set<TeleporterEntry>().AsNoTracking().ToDictionary(row => row.Id);
            var all = WaypointTitles.Zones.SelectMany(zone => WaypointTitles.WaypointsOf(zone.TitleId)).ToArray();

            Assert.HasCount(all.Length, all.Distinct().ToArray(), "a waypoint is one battlefield's");

            foreach (var zone in WaypointTitles.Zones)
                foreach (var id in WaypointTitles.WaypointsOf(zone.TitleId))
                {
                    Assert.AreEqual((byte)WaypointType.Waypoint, world[id].Type, $"{zone.Name}: {id}");
                    Assert.AreEqual(zone.MapContextId, world[id].MapContextId, $"{zone.Name}: {id}");
                }

            Assert.IsFalse(all.Contains(267u), "Dropship Transport: Twin Pillars");
            Assert.IsFalse(all.Contains(78u), "Purgas Station's, an instance of Divide");

            foreach (var unnamed in new uint[] { 534, 624, 583, 541, 576, 575, 622 })
            {
                Assert.IsTrue(world.ContainsKey(unnamed), $"{unnamed} is a row of the world");
                Assert.IsFalse(all.Contains(unnamed), $"{unnamed}: the client has no name for it");
            }

            // One title a battlefield: the Pathfinder for Divide and Marshes.
            Assert.HasCount(11, WaypointTitles.Zones.Select(zone => zone.TitleId).Distinct().ToArray());
            Assert.HasCount(11, WaypointTitles.WaypointsOf(DividePathfinder));
            Assert.HasCount(6, WaypointTitles.WaypointsOf(MarshesPathfinder));
            Assert.IsEmpty(WaypointTitles.WaypointsOf(DivideWanderer).ToArray());
            Assert.IsEmpty(WaypointTitles.WaypointsOf(MarshesWanderer).ToArray());
        }

        [TestMethod]
        public void TheLastWaypointOfABattlefieldGivesItsTitle()
        {
            using var harness = Start();

            foreach (var waypointId in Crucible.Take(6))
                Gain(harness, waypointId);

            Assert.IsEmpty(harness.Drain().OfType<TitleAddedPacket>().ToArray(), "six of seven");
            Assert.IsFalse(harness.Client.Player.Titles.Contains(CruciblePathfinder));

            Gain(harness, Crucible[6]);

            var packets = harness.Drain();
            Assert.AreEqual(CruciblePathfinder, packets.OfType<TitleAddedPacket>().Single().TitleId);
            Assert.IsTrue(IndexOf<WaypointGainedPacket>(packets) < IndexOf<TitleAddedPacket>(packets), "the waypoint, then what it completed");
            Assert.IsTrue(harness.Client.Player.Titles.Contains(CruciblePathfinder));

            using (var unit = harness.Context.CreateChar())
                CollectionAssert.Contains(unit.CharacterTitles.Get(harness.Client.Player.Id), CruciblePathfinder);

            // Another battlefield's waypoint gives nothing more, and walking over one again nothing at all.
            Gain(harness, Abyss[0]);
            Gain(harness, Crucible[6]);
            Assert.IsEmpty(harness.Drain().OfType<TitleAddedPacket>().ToArray());
        }

        [TestMethod]
        public void TheRowsTheClientHasNoNameForAreNotAsked()
        {
            using var harness = Start();

            // Neither Tampeii Settlement row (575, 622), nor the dropship pad (389).
            foreach (var waypointId in Abyss)
                Gain(harness, waypointId);

            Assert.AreEqual(AbyssPathfinder, harness.Drain().OfType<TitleAddedPacket>().Single().TitleId);
        }

        [TestMethod]
        public void ADropshipPadOrAHospitalIsNoWaypointOfTheTitle()
        {
            using var harness = Start();

            foreach (var waypointId in Wilderness.Take(6))
                Gain(harness, waypointId);

            // The pad at Twin Pillars instead of Imperial Valley.
            DynamicObjectManager.Instance.CheckPlayerWaypoint(harness.Client, new WaypointInfo(267, false, WaypointType.Dropship));

            Assert.IsEmpty(harness.Drain().OfType<TitleAddedPacket>().ToArray());

            Gain(harness, 156);

            Assert.AreEqual(WildernessPathfinder, harness.Drain().OfType<TitleAddedPacket>().Single().TitleId);
        }

        [TestMethod]
        public void ACharacterWhoHasThemAlreadyIsGivenTheTitleOnComingOntoAMap()
        {
            using var harness = Start();

            // Held from before - as a clone has its source's - with no title for them.
            var entries = Crucible.Select(id => new CharacterTeleporterEntry(harness.Client.Player.Id, id, (byte)WaypointType.Waypoint)).ToArray();

            using (var unit = harness.Context.CreateChar())
                foreach (var entry in entries)
                    unit.CharacterTeleporters.Add(entry);

            harness.Client.Player.GainedWaypoints.AddRange(entries);
            harness.Drain();

            ManifestationManager.Instance.AssignPlayer(harness.Client);

            var packets = harness.Drain();
            Assert.AreEqual(CruciblePathfinder, packets.OfType<TitleAddedPacket>().Single().TitleId);
            Assert.IsTrue(IndexOf<TitlesPacket>(packets) < IndexOf<TitleAddedPacket>(packets), "added to the list the client has been given");

            // The next map: it is in the list, and not gained again.
            ManifestationManager.Instance.AssignPlayer(harness.Client);

            Assert.IsEmpty(harness.Drain().OfType<TitleAddedPacket>().ToArray());
        }

        [TestMethod]
        public void WithNoWaypointsKnownNothingIsGiven()
        {
            using var harness = Start();
            WaypointTitles.Load(null);

            foreach (var waypointId in Crucible)
                Gain(harness, waypointId);

            WaypointTitles.CatchUp(harness.Client);

            Assert.IsEmpty(harness.Drain().OfType<TitleAddedPacket>().ToArray(), "a battlefield with no waypoint asks for nothing and gives nothing");
            Assert.IsEmpty(WaypointTitles.WaypointsOf(CruciblePathfinder).ToArray());
        }

        /// <summary>The harness with the titles' waypoints worked out from its world's teleporters, as DynamicObjectManager.InitTeleporters does.</summary>
        private static BootcampRuntimeTestHarness.Harness Start()
        {
            var harness = BootcampRuntimeTestHarness.Create();

            WaypointTitles.Load(harness.WorldContext.Set<TeleporterEntry>().AsNoTracking().ToList());
            harness.Drain();
            return harness;
        }

        /// <summary>A waypoint gained, as walking over it gains it.</summary>
        private static void Gain(BootcampRuntimeTestHarness.Harness harness, uint waypointId) =>
            DynamicObjectManager.Instance.CheckPlayerWaypoint(harness.Client, new WaypointInfo(waypointId, false, WaypointType.Waypoint));

        private static int IndexOf<T>(IReadOnlyList<PythonPacket> packets) where T : PythonPacket
        {
            for (var i = 0; i < packets.Count; i++)
                if (packets[i] is T)
                    return i;

            return -1;
        }
    }
}
