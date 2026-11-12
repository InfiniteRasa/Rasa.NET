using System;
using System.Linq;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Managers;
    using Rasa.Structures;

    // A player put back by the safety floor (SafetyFloor.Rescue) onto a spot inside a map link's
    // circle stands in it as one who has just arrived through it: the link does not fire until
    // they step out and back in. The Bane supply depot (1451): a player who fell from the arrival
    // behind its door was put back inside link 39, the depot's way out, and sent to the Marshes.
    [TestClass]
    [DoNotParallelize]
    public class SafetyFloorMapLinkTests
    {
        private const uint MapId = 1220;    // WorldTestContext's map

        private static readonly Vector3 InLink = new(104, 0, 1);

        private long _now = 1_000_000;

        [TestInitialize]
        public void Clock() => SafetyFloor.Now = () => _now;

        [TestCleanup]
        public void Restore() => SafetyFloor.Now = () => Environment.TickCount64;

        private static MapLink Link(WorldTestContext world)
        {
            var link = new MapLink
            {
                Id = 939,
                MapContextId = MapId,
                Position = new Vector3(100, 0, 0),
                Radius = 6,                             // link 39's
                DestMapContextId = 99939,               // not loaded: going through it is recorded against the map
                Enabled = true,
                Comment = "test -> marshes"
            };
            CellManager.Instance.AddToWorld(world.Map, link);
            MapErrorManager.Instance.Clear();
            return link;
        }

        private static bool WentThrough(MapLink link) =>
            MapErrorManager.Instance.Errors(MapId).Any(e => e.Contains($"map_link {link.Id}"));

        [TestMethod]
        public void ARescueInsideAMapLinkDoesNotSendThePlayerThroughItUntilTheyStepOutAndBackIn()
        {
            using var world = new WorldTestContext();
            world.Map.SafetyFloorY = -100f;
            var link = Link(world);

            try
            {
                // Arrived through the link, as at the depot's door, then fell out of the world.
                var client = PlayerDeathTests.Player(world, InLink.X, InLink.Z);
                MapLinkManager.Instance.PlayerEnteredMap(client);
                client.Player.LastSafePosition = InLink;
                client.Player.LastSafeMapContextId = MapId;

                var below = new Vector3(InLink.X, -150f, InLink.Z);
                client.Player.Position = below;
                MapLinkManager.Instance.Worker(world.Map);      // falling: out of the link, which is forgotten
                Assert.IsFalse(client.Player.InsideMapLinks.Contains(link.Id));

                Assert.IsTrue(SafetyFloor.OnMove(client, below, _now), "rescued");
                Assert.AreEqual(InLink, client.Player.Position, "put back where they last stood, inside the link");
                Assert.IsTrue(client.Player.InsideMapLinks.Contains(link.Id), "standing in it, as one who arrived through it");

                MapLinkManager.Instance.Worker(world.Map);
                Assert.IsFalse(WentThrough(link), "not sent through it");

                // Out of it, and back in: now it fires.
                client.Player.Position = new Vector3(100, 0, 20);
                CellManager.Instance.UpdateVisibility(client);
                MapLinkManager.Instance.Worker(world.Map);
                Assert.IsFalse(WentThrough(link));

                client.Player.Position = InLink;
                CellManager.Instance.UpdateVisibility(client);
                MapLinkManager.Instance.Worker(world.Map);
                Assert.IsTrue(WentThrough(link), "walked back in: sent through it");
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(world.Map, link);
                MapErrorManager.Instance.Clear();
            }
        }

        [TestMethod]
        public void ARescueFarFromTheFallPutsThePlayerInTheCellsWhereTheyStandAndInTheLinkThere()
        {
            using var world = new WorldTestContext();
            world.Map.SafetyFloorY = -100f;
            var link = Link(world);

            try
            {
                // Fell 400 m off, in other cells than the link's; last stood inside it.
                var client = PlayerDeathTests.Player(world, 100, 400);
                client.Player.LastSafePosition = InLink;
                client.Player.LastSafeMapContextId = MapId;

                var below = new Vector3(100, -150f, 400);
                client.Player.Position = below;
                Assert.IsTrue(SafetyFloor.OnMove(client, below, _now), "rescued");

                var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
                Assert.AreEqual(CellManager.Centre(CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16)),
                    CellManager.Centre(client.Player.Cells), "in the cells where they were put back");
                Assert.IsTrue(client.Player.InsideMapLinks.Contains(link.Id), "standing in the link there");

                MapLinkManager.Instance.Worker(world.Map);
                Assert.IsFalse(WentThrough(link), "not sent through it");
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(world.Map, link);
                MapErrorManager.Instance.Clear();
            }
        }
    }
}
