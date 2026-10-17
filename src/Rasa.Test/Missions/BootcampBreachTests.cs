using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Game.Missions;
    using Rasa.Managers;
    using Rasa.Missions.Scenes;
    using Rasa.Services.Preloader;
    using Rasa.Services.Preloader.Missions;
    using Rasa.Structures;

    /// <summary>
    /// The Proving Grounds' cave-in seen from across the bridge (BootcampCaveInLandmarkV10).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class BootcampBreachTests
    {
        #region The rocks seen from across the bridge

        [TestMethod]
        public void BothScenesGiveTheRocksAViewRadiusAndChangeNothingElse()
        {
            var capture = BootcampCaveInLandmarkV10.CaptureTheFlag();
            var experience = BootcampCaveInLandmarkV10.Experience();

            foreach (var scene in new[] { capture, experience.Scene })
            {
                var rocks = scene.Actors[BootcampCaveInBreachV9.CaveIn];
                Assert.AreEqual(150f, rocks.ViewRadius);
                Assert.AreEqual(BootcampCaveInBreachV9.CaveInActor(), rocks with { ViewRadius = 0 }, "the same pile, as far as anything else goes");
            }

            Assert.HasCount(BootcampCaveInBreachV9.CaptureTheFlag().Actors.Count, capture.Actors);
            Assert.HasCount(BootcampCaveInBreachV9.Experience().Scene.Actors.Count, experience.Scene.Actors);
        }

        [TestMethod]
        public void FromTheFarEndOfTheBridgeThePlayerHasTheRocks()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            var map = harness.BootcampMap;
            var rocks = BootcampRuntimeTestHarness.FindScenarioObject(map, BootcampCaveInBreachV9.CaveIn);

            Assert.AreEqual(150f, rocks.ViewRadius, "stood up by the experience with its radius");

            // The far end of the bridge: 76 m off, beyond the rocks' cells.
            harness.MovePlayerTo(new Vector3(351f, 121.7f, 64.5f));
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.IsTrue(CellManager.TryGetCellCoordinates(rocks.CellPosition, out var x, out var z));
            Assert.IsFalse(CellManager.Instance.GetClientsInCells(map, CellManager.Instance.CreateCellMatrix(map, x, z)).Contains(harness.Client),
                "not through its cells");

            Landmarks.Worker(map);

            Assert.IsTrue(Landmarks.HeldFromAfar(harness.Client, rocks), "in view all the same");
        }

        /// <summary>
        /// A pile already standing when a scene takes it up - Capture the Flag's breach binding
        /// the one the experience put down - is seen as that scene says, so one put down before
        /// its definition had a radius is given it.
        /// </summary>
        [TestMethod]
        public void APileAlreadyStandingIsGivenTheRadiusWhenASceneTakesItUp()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);
            harness.Client.Player.GmFlagAlwaysFriendly = true;
            var rocks = BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, BootcampCaveInBreachV9.CaveIn);
            rocks.ViewRadius = 0;

            Breach(harness);

            Assert.AreSame(rocks, BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, BootcampCaveInBreachV9.CaveIn), "the same pile");
            Assert.AreEqual(UseObjectState.DoorStateOpen, rocks.StateId, "blown by Capture the Flag's scene");
            Assert.AreEqual(150f, rocks.ViewRadius);
        }

        #endregion

        private static void Breach(BootcampRuntimeTestHarness.Harness harness)
        {
            harness.SeedMission(harness.Client.Player.Id, 1992, (uint)MissionState.Completed, true);
            var deSimone = BootcampCaveInBreachTests.Creatures(harness).Single(actor => actor.DbId == BootcampRuntimeTestHarness.CorporalDeSimoneCreatureId);
            harness.MovePlayerTo(deSimone);
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Drain();
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, deSimone.EntityId, 1994));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, deSimone.EntityId, 1994, 4, 1));

            BootcampCaveInBreachTests.Cross(harness, new Vector3(340f, 121.7f, 64.5f));
            BootcampCaveInBreachTests.Cross(harness, new Vector3(316f, 120.61f, 64.5f));
            harness.UtcNow += TimeSpan.FromSeconds(2.5);
            harness.Manager.TickScenarios(harness.Client);
        }
    }
}
