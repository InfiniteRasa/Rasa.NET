using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;

    /// <summary>
    /// A point a player aims an ability at is put on the walkable ground of its column
    /// (AbilityManager.OnTheGround). Its height was taken as sent, with only its range and
    /// finite values checked: an area ability centred in the air hit nothing, and one lifted to a
    /// ledge or dropped to a floor below hit whoever was there. The Bootcamp map and its navmesh,
    /// with a firework (CONSUMABLE_FIREWORK 482, range 80) to aim.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AimedPointTests
    {
        private const uint BlueFireworkTemplate = 117207;
        private const int Firework = 482;

        [TestMethod]
        [DataRow(30f, DisplayName = "in the air")]
        [DataRow(-20f, DisplayName = "under the ground")]
        public void APointOffTheGroundIsPutOnIt(float offset)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var item = ToyTests.Grant(harness, BlueFireworkTemplate);
            var manager = ToyTests.CreateManager(harness, Firework, 1, BlueFireworkTemplate);
            var player = harness.Client.Player;
            var ground = GroundAhead(harness, 15f);
            harness.Drain();

            manager.RequestPerformAbility(harness.Client, ToyTests.Request(Firework, 1, item.EntityId, ground + new Vector3(0, offset, 0)));

            Assert.IsFalse(harness.Drain().OfType<UserActionFailedPacket>().Any(), "not refused");
            var pending = harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == ActionId.ConsumableFirework);
            Assert.AreEqual(ground.Y, pending.TargetLocation.Value.Y, 0.5f, "on the ground, not where it was sent");
            Assert.AreEqual(0f, Vector2.Distance(new(ground.X, ground.Z), new(pending.TargetLocation.Value.X, pending.TargetLocation.Value.Z)), 0.5f);

            ToyTests.Land(harness, manager, pending);
            var proxy = harness.BootcampMap.MapCellInfo.Cells.Values.SelectMany(cell => cell.DynamicObjectList)
                .Where(o => o.EntityClassId == AbilityManager.LocationProxyClass).Distinct().Single();
            Assert.AreEqual(ground.Y, proxy.Position.Y, 0.5f, "goes off there");
        }

        [TestMethod]
        public void APointOnTheGroundStaysWhereItWasAimed()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var item = ToyTests.Grant(harness, BlueFireworkTemplate);
            var manager = ToyTests.CreateManager(harness, Firework, 1, BlueFireworkTemplate);
            var ground = GroundAhead(harness, 15f);

            manager.RequestPerformAbility(harness.Client, ToyTests.Request(Firework, 1, item.EntityId, ground));

            var pending = harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == ActionId.ConsumableFirework);
            Assert.AreEqual(0f, Vector3.Distance(ground, pending.TargetLocation.Value), 0.1f);
        }

        [TestMethod]
        public void APointWhoseGroundIsOutOfRangeIsRefused()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var item = ToyTests.Grant(harness, BlueFireworkTemplate);
            var manager = ToyTests.CreateManager(harness, Firework, 1, BlueFireworkTemplate);
            var player = harness.Client.Player;
            var ground = GroundAhead(harness, -30f);
            // Eighty metres up, aimed level thirty metres off: 30 m away as sent, 85 m once it is
            // on the ground under it, past the firework's 80 and the 2.5 m of slack. South of the
            // start, where nothing walkable stands above the ground (to the north and east the
            // cliff tops are 70-85 m up, and a point up there is on them).
            player.Position = new Vector3(player.Position.X, ground.Y + 80f, player.Position.Z);
            var aimed = new Vector3(ground.X, ground.Y + 80f, ground.Z);
            Assert.IsLessThan(31f, Vector3.Distance(player.Position, aimed));
            harness.Drain();

            manager.RequestPerformAbility(harness.Client, ToyTests.Request(Firework, 1, item.EntityId, aimed));

            Assert.IsFalse(harness.BootcampMap.PerformRecovery.Any(action => action.ActionId == ActionId.ConsumableFirework),
                "not performed");
            Assert.AreEqual(PlayerMessage.PmTargetOutOfRange, harness.Drain().OfType<UserActionFailedPacket>().Single().MsgId, "and the client told");
        }

        [TestMethod]
        public void WithNoNavmeshThePointIsLeftAsItWas()
        {
            var map = new MapChannel();
            var info = new ActionLevelInfo { MaxRange = 80 };
            var point = new Vector3(10, 40, 10);

            Assert.AreEqual(point, AbilityManager.OnTheGround(map, point, info));
            Assert.AreEqual(point, AbilityManager.OnTheGround(null, point, info));
        }

        /// <summary>Where the player is stood: open ground by the Bootcamp base (SceneNavigationFixture's start).</summary>
        private static readonly Vector3 Start = new(391.5f, 120.059f, 164.8f);

        /// <summary>
        /// The walkable ground the given distance north (+Z; south for a negative distance) of
        /// the player, who is put on the ground at Start first.
        /// </summary>
        private static Vector3 GroundAhead(BootcampRuntimeTestHarness.Harness harness, float distance)
        {
            var player = harness.Client.Player;
            var mesh = harness.BootcampMap.NavMesh;

            harness.MovePlayerTo(mesh.Nearest(Start) ?? throw new AssertFailedException("the start is not on the mesh"));

            var ahead = mesh.Nearest(player.Position + new Vector3(0, 0, distance));

            return ahead ?? throw new AssertFailedException($"no ground {distance} m from the player at {player.Position}");
        }
    }
}
