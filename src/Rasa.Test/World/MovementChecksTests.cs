extern alias RasaGame;

using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Config;
    using Rasa.Managers;
    using Rasa.Models;
    using Rasa.Navigation;
    using Rasa.Packets.Protocol;

    [TestClass]
    [DoNotParallelize]
    public class MovementChecksTests
    {
        private MovementChecksConfig _previous;

        [TestInitialize]
        public void Initialize()
        {
            _previous = MovementChecks.Config;
            MovementChecks.Config = new MovementChecksConfig();
        }

        [TestCleanup]
        public void Cleanup()
        {
            MovementChecks.Config = _previous;
        }

        /// <summary>Metres in hand for the speed check, which PlaceAt empties.</summary>
        private static void Settle(Rasa.Game.Client client) => client.Player.MoveBudget = 60;

        /// <summary>
        /// A world of flat terrain at y = 0 over 200 m square, a wall across x = 5 from z = -5 to 5
        /// and 3 m high, and a crate top at y = 2 over x 20..22, z -1..1.
        /// </summary>
        private static CoverMesh Fixture()
        {
            var vertices = new float[]
            {
                // the wall, two triangles in the plane x = 5
                5, 0, -5,   5, 3, -5,   5, 3, 5,   5, 0, 5,
                // the crate top, two triangles in the plane y = 2
                20, 2, -1,  22, 2, -1,  22, 2, 1,  20, 2, 1
            };
            var indices = new[] { 0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7 };
            var cover = CoverMesh.Build(vertices, indices);

            var columns = 21;
            cover.SetTerrain(-100, -100, 10, columns, columns, Enumerable.Repeat(0f, columns * columns).ToArray());
            return cover;
        }

        [TestMethod]
        public void ALevelStepThroughAWallIsSeenAndOneAlongItIsNot()
        {
            var cover = Fixture();

            Assert.IsTrue(MovementChecks.ThroughGeometry(cover, new Vector3(0, 0, 0), new Vector3(10, 0, 0)));
            Assert.IsFalse(MovementChecks.ThroughGeometry(cover, new Vector3(0, 0, -3), new Vector3(0, 0, 3)), "beside the wall");
            Assert.IsFalse(MovementChecks.ThroughGeometry(cover, new Vector3(0, 0, 8), new Vector3(10, 0, 8)), "past its end");
            Assert.IsFalse(MovementChecks.ThroughGeometry(cover, new Vector3(0, 0, 0), new Vector3(0.01f, 0, 0)), "standing still");
            Assert.IsFalse(MovementChecks.ThroughGeometry(null, new Vector3(0, 0, 0), new Vector3(10, 0, 0)), "no cover mesh");
        }

        [TestMethod]
        public void AJumpAndAFallAreNotLookedAtForWalls()
        {
            var cover = Fixture();

            // Over the wall in a jump: the rise is more than a level step.
            Assert.IsFalse(MovementChecks.ThroughGeometry(cover, new Vector3(4, 0, 0), new Vector3(6, 1, 0)));
            Assert.IsFalse(MovementChecks.ThroughGeometry(cover, new Vector3(4, 4, 0), new Vector3(6, 3, 0)));
        }

        [TestMethod]
        public void TheRayIsAboveAKerbAndBelowAWall()
        {
            // A kerb 0.8 m high across x = 5 is stepped over; the wall's 3 m is not.
            var vertices = new float[] { 5, 0, -5, 5, 0.8f, -5, 5, 0.8f, 5, 5, 0, 5 };
            var kerb = CoverMesh.Build(vertices, new[] { 0, 1, 2, 0, 2, 3 });

            Assert.IsFalse(MovementChecks.ThroughGeometry(kerb, new Vector3(0, 0, 0), new Vector3(10, 0, 0)));
            Assert.IsTrue(MovementChecks.ChestHeight > 0.8f);
        }

        [TestMethod]
        public void SupportIsTerrainGeometryOrNothing()
        {
            var map = new WorldTestContext();
            map.Map.Cover = Fixture();

            Assert.IsTrue(MovementChecks.IsSupported(map.Map, new Vector3(0, 1, 0)), "on the ground");
            Assert.IsTrue(MovementChecks.IsSupported(map.Map, new Vector3(0, 5, 0)), "a jump's height over it");
            Assert.IsFalse(MovementChecks.IsSupported(map.Map, new Vector3(0, 30, 0)), "thirty metres up");
            Assert.IsTrue(MovementChecks.IsSupported(map.Map, new Vector3(21, 2.5f, 0)), "on the crate");
            Assert.IsFalse(MovementChecks.IsSupported(map.Map, new Vector3(21, 12, 0)), "ten metres over the crate");
            Assert.IsTrue(MovementChecks.IsSupported(map.Map, new Vector3(500, 30, 500)), "off the terrain grid: nothing known, nothing said");

            map.Map.Cover = null;
            Assert.IsTrue(MovementChecks.IsSupported(map.Map, new Vector3(0, 30, 0)), "no cover mesh: every map without one supports everyone");
            map.Dispose();
        }

        [TestMethod]
        public void HoveringTakesLongerThanAJumpAndEndsOnTheWayDown()
        {
            using var world = new WorldTestContext();
            world.Map.Cover = Fixture();
            var player = world.CreateClient().Player;
            var up = new Vector3(0, 30, 0);
            var now = 1_000_000L;

            // The first Move in the air starts the clock; nothing is said for the grace.
            Assert.IsNull(MovementChecks.Judge(player, up, up + new Vector3(0.5f, 0, 0), now));
            Assert.AreEqual(now, player.UnsupportedSinceTick);
            Assert.IsNull(MovementChecks.Judge(player, up, up + new Vector3(1, 0, 0), now + MovementChecks.HoverGraceMs - 1));

            var finding = MovementChecks.Judge(player, up, up + new Vector3(1.5f, 0.2f, 0), now + MovementChecks.HoverGraceMs + 500);
            Assert.IsNotNull(finding);
            Assert.AreEqual(MovementChecks.Check.Hover, finding.Value.Check);
            Assert.IsFalse(finding.Value.Refuse, "log mode by default");

            // Coming down is never refused, and ends the hover.
            Assert.IsNull(MovementChecks.Judge(player, up, up - new Vector3(0, 2, 0), now + MovementChecks.HoverGraceMs + 600));
            Assert.AreEqual(0, player.UnsupportedSinceTick);

            // On the ground the clock never starts.
            Assert.IsNull(MovementChecks.Judge(player, new Vector3(0, 0, 0), new Vector3(1, 0, 0), now + 10_000));
            Assert.AreEqual(0, player.UnsupportedSinceTick);
        }

        [TestMethod]
        public void AJumpIsARiseAndADescentAndNeverAHover()
        {
            using var world = new WorldTestContext();
            world.Map.Cover = Fixture();
            var player = world.CreateClient().Player;
            var now = 1_000_000L;
            var position = new Vector3(0, 0, 0);

            // Up for half a second, down for half a second, ten Moves a second.
            for (var i = 0; i < 10; i++)
            {
                var next = position + new Vector3(0.6f, i < 5 ? 0.6f : -0.6f, 0);
                Assert.IsNull(MovementChecks.Judge(player, position, next, now + i * 100));
                position = next;
            }
        }

        [TestMethod]
        public void InRefuseModeAHoverIsRefusedThroughTheMovePath()
        {
            using var world = new WorldTestContext();
            world.Map.Cover = Fixture();
            MovementChecks.Config = new MovementChecksConfig { Hover = "refuse" };
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            client.Player.PlaceAt(new Vector3(0, 30, 0));
            Settle(client);
            WorldTestContext.Drain(client);

            // Long in the air already.
            client.Player.UnsupportedSinceTick = Environment.TickCount64 - MovementChecks.HoverGraceMs - 1000;

            Assert.IsFalse(client.HandleMovement(new Movement(new Vector3(1, 30, 0), Vector2.Zero)));
            Assert.AreEqual(new Vector3(0, 30, 0), client.Player.Position);
            Assert.IsTrue(WorldTestContext.Drain(client).Any(p => p.Message is MoveObjectMessage), "put back");

            // Down is allowed.
            Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(1, 28, 0), Vector2.Zero)));
            Assert.AreEqual(new Vector3(1, 28, 0), client.Player.Position);
        }

        [TestMethod]
        public void AStepThroughABaneForceFieldIsRefusedAndAnAfsOneIsNot()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            var gate = ForceFields.ClassOf("humgate");
            var field = ForceFields.Place(world.Map, gate, ForceFields.Side.B, new Vector3(10, 0, 0), 0f, 100);

            try
            {
                client.Player.PlaceAt(new Vector3(10, 0, -2));
                Settle(client);
                WorldTestContext.Drain(client);

                Assert.IsFalse(client.HandleMovement(new Movement(new Vector3(10, 0, 2), Vector2.Zero)));
                Assert.AreEqual(new Vector3(10, 0, -2), client.Player.Position);
                Assert.IsTrue(WorldTestContext.Drain(client).Any(p => p.Message is MoveObjectMessage), "put back");

                // Beside the gate's 13 m: through.
                Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(20, 0, -2), Vector2.Zero)));
                Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(20, 0, 2), Vector2.Zero)));

                // An AFS field lets players through.
                ForceFields.SetSide(field, ForceFields.Side.A);
                client.Player.PlaceAt(new Vector3(10, 0, -2));
                Settle(client);
                Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(10, 0, 2), Vector2.Zero)));

                // And a Bane one that is down.
                ForceFields.SetSide(field, ForceFields.Side.B);
                field.Health = 0;
                client.Player.PlaceAt(new Vector3(10, 0, -2));
                Settle(client);
                Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(10, 0, 2), Vector2.Zero)));
            }
            finally
            {
                ForceFields.Remove(field);
            }
        }

        [TestMethod]
        public void OffAndLogDoWhatTheySay()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            var field = ForceFields.Place(world.Map, ForceFields.ClassOf("humgate"), ForceFields.Side.B, new Vector3(10, 0, 0), 0f, 100);

            try
            {
                MovementChecks.Config = new MovementChecksConfig { ForceFields = "Log" };
                client.Player.PlaceAt(new Vector3(10, 0, -2));
                Settle(client);
                WorldTestContext.Drain(client);
                Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(10, 0, 2), Vector2.Zero)));
                Assert.AreEqual(new Vector3(10, 0, 2), client.Player.Position);
                Assert.IsFalse(WorldTestContext.Drain(client).Any(p => p.Message is MoveObjectMessage), "not put back");

                MovementChecks.Config = new MovementChecksConfig { ForceFields = "off" };
                client.Player.PlaceAt(new Vector3(10, 0, -2));
                Assert.IsNull(MovementChecks.Judge(client.Player, new Vector3(10, 0, -2), new Vector3(10, 0, 2), 0));

                // Anything else is log.
                MovementChecks.Config = new MovementChecksConfig { ForceFields = "whatever" };
                var finding = MovementChecks.Judge(client.Player, new Vector3(10, 0, -2), new Vector3(10, 0, 2), 0);
                Assert.IsNotNull(finding);
                Assert.IsFalse(finding.Value.Refuse);
            }
            finally
            {
                ForceFields.Remove(field);
            }
        }

        [TestMethod]
        public void LinesAreRateLimitedPerPlayerAndCountTheRest()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var finding = new MovementChecks.Finding(MovementChecks.Check.Geometry, "through a wall", false);

            MovementChecks.Report(client, finding, Vector3.Zero, Vector3.One, 1000);
            Assert.AreEqual(1000, client.Player.MovementCheckLogTick);
            Assert.AreEqual(0, client.Player.MovementCheckHits);

            MovementChecks.Report(client, finding, Vector3.Zero, Vector3.One, 2000);
            MovementChecks.Report(client, finding, Vector3.Zero, Vector3.One, 3000);
            Assert.AreEqual(1000, client.Player.MovementCheckLogTick, "within the quiet time: counted, not written");
            Assert.AreEqual(2, client.Player.MovementCheckHits);

            MovementChecks.Report(client, finding, Vector3.Zero, Vector3.One, 1000 + MovementChecks.QuietMs);
            Assert.AreEqual(1000 + MovementChecks.QuietMs, client.Player.MovementCheckLogTick);
            Assert.AreEqual(0, client.Player.MovementCheckHits);
        }
    }
}
