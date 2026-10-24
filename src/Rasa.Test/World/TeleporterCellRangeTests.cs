using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Managers;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// A teleporter whose place the cells cannot hold - teleporter 86 on Thunderhead, whose z lost
    /// its decimal point (-14538672 for -145.38672, MarkedTeleporters) - made AddToWorld throw in
    /// the object worker on every pass, so the teleporters after it in the map's list were never
    /// put in the world. It is now passed over, and the ones after it are put in as before.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class TeleporterCellRangeTests
    {
        [TestMethod]
        public void ThePlacesTheCellsCanHold()
        {
            Assert.IsTrue(CellManager.InCellRange(Vector3.Zero));
            Assert.IsTrue(CellManager.InCellRange(new Vector3(755.6289f, 385f, -145.38672f)));
            Assert.IsTrue(CellManager.InCellRange(new Vector3(-838000f, 0f, 838000f)));

            Assert.IsFalse(CellManager.InCellRange(new Vector3(755.6289f, 385f, -14538672f)));
            Assert.IsFalse(CellManager.InCellRange(new Vector3(840000f, 0f, 0f)));
            Assert.IsFalse(CellManager.InCellRange(new Vector3(0f, 0f, -840000f)));
            Assert.IsFalse(CellManager.InCellRange(new Vector3(float.NaN, 0f, 0f)));
        }

        [TestMethod]
        public void ATeleporterOutsideTheCellsDoesNotKeepTheOnesAfterItOutOfTheWorld()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pads = harness.Map.Teleporters.Values.Where(t => t.IsInWorld).Take(2).ToArray();
            Assert.AreEqual(2, pads.Length);
            var (lost, next) = (pads[0], pads[1]);
            var place = lost.Position;

            try
            {
                foreach (var pad in pads)
                {
                    CellManager.Instance.RemoveFromWorld(harness.Map, pad);
                    pad.IsInWorld = false;
                    pad.RespawnTime = 0;
                }

                lost.Position = new Vector3(place.X, place.Y, -14538672f);

                harness.Objects.DynamicObjectWorker(harness.Map, 0);
                harness.Objects.DynamicObjectWorker(harness.Map, 100);

                Assert.IsFalse(lost.IsInWorld);
                Assert.IsTrue(next.IsInWorld);
                Assert.IsFalse(harness.Map.MapCellInfo.Cells.Values.Any(cell => cell.DynamicObjectList.Contains(lost)));
            }
            finally
            {
                lost.Position = place;
            }
        }
    }
}
