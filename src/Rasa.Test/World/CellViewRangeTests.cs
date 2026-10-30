extern alias RasaGame;

using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;

    /// <summary>
    /// How far the server shows the world (GameConfig.CellViewRange, CellManager.ViewRange): the
    /// cells a player sees, ViewRange out on each side of their own.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CellViewRangeTests
    {
        [TestCleanup]
        public void End() => CellManager.SetViewRange(CellManager.DefaultViewRange);

        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        public void TheMatrixIsViewRangeOutOnEachSideWithTheActorsCellInTheMiddle(int range)
        {
            CellManager.SetViewRange(range);
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);

            var cells = client.Player.Cells;
            Assert.AreEqual(2 * range + 1, cells.GetLength(0));
            Assert.AreEqual(2 * range + 1, cells.GetLength(1));
            Assert.AreEqual(CellManager.Instance.GetCellSeed(client.Player.Position), CellManager.Centre(cells));
            Assert.AreEqual(cells.Length, cells.Cast<uint>().Distinct().Count(), "each cell once");
            Assert.AreEqual(64f * range, MissileManager.MaxTargetDistance, "aim as far as the client can see, twice over");
        }

        /// <summary>Another player 80 m off, three cells over: out of sight at range 2, in sight at range 3.</summary>
        [TestMethod]
        [DataRow(2, false)]
        [DataRow(3, true)]
        public void APlayerEightyMetresOffIsInSightFromRangeThree(int range, bool seen)
        {
            CellManager.SetViewRange(range);
            using var world = new WorldTestContext();
            var here = world.CreateClient(1, 1);
            var there = world.CreateClient(1 + 80, 1);
            CellManager.Instance.AddToWorld(here);
            CellManager.Instance.AddToWorld(there);

            Assert.AreEqual(seen, here.Player.Cells.Cast<uint>().Contains(CellManager.Centre(there.Player.Cells)));
        }

        /// <summary>
        /// A change while the server runs (a config reload): the map's next visibility update gives
        /// every matrix the new size, and two players who come into or go out of each other's
        /// sight are each sent the other once.
        /// </summary>
        [TestMethod]
        public void ARangeChangedWhileTheServerRunsTakesEffectOnTheMapsNextUpdate()
        {
            CellManager.SetViewRange(2);
            using var world = new WorldTestContext();
            var here = world.CreateClient(1, 1);
            var there = world.CreateClient(1 + 80, 1);
            CellManager.Instance.AddToWorld(here);
            CellManager.Instance.AddToWorld(there);
            WorldTestContext.Drain(here);
            WorldTestContext.Drain(there);
            int Created(Client to, Client of) => WorldTestContext.Drain(to).Select(p => p.Message).OfType<CallMethodMessage>()
                .Count(p => p.Packet is CreatePhysicalEntityPacket c && c.EntityId == of.Player.EntityId);

            CellManager.SetViewRange(3);
            Assert.AreEqual(5, here.Player.Cells.GetLength(0), "nothing changes before the map's update");
            CellManager.Instance.UpdateVisibility(world.Map);
            Assert.AreEqual(7, here.Player.Cells.GetLength(0));
            Assert.AreEqual(7, there.Player.Cells.GetLength(0));
            Assert.AreEqual(1, Created(here, there), "the other comes into sight once");
            Assert.AreEqual(1, Created(there, here));

            CellManager.Instance.UpdateVisibility(world.Map);
            Assert.AreEqual(0, Created(here, there), "and not again");

            CellManager.SetViewRange(2);
            CellManager.Instance.UpdateVisibility(world.Map);
            Assert.AreEqual(5, here.Player.Cells.GetLength(0));
            int Destroyed(Client to, Client of) => WorldTestContext.Drain(to).Select(p => p.Message).OfType<CallMethodMessage>()
                .Count(p => p.Packet is DestroyPhysicalEntityPacket d && d.EntityId == of.Player.EntityId);
            Assert.AreEqual(1, Destroyed(here, there), "and goes out of sight once");
            Assert.AreEqual(1, Destroyed(there, here));
        }

        [TestMethod]
        public void TheRangeStaysOneToFour()
        {
            CellManager.SetViewRange(9);
            Assert.AreEqual(CellManager.MaxViewRange, CellManager.ViewRange);
            CellManager.SetViewRange(0);
            Assert.AreEqual(1, CellManager.ViewRange);
        }
    }
}
