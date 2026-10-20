using System;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Services.Preloader.Missions;
    using Rasa.Structures;
    using Rasa.Test.Database;

    /// <summary>
    /// The Proving Grounds' cave-in blows whichever way the player comes to it
    /// (BootcampCaveInAnyRouteV13): round the rock wall to the doorwell as well as over the bridge,
    /// and for a player whose way out was marked found with the pile still shut.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class BootcampCaveInAnyRouteTests
    {
        private const string Migration = "20270120000000_Bootcamp_cave_in_any_route";
        private const string Before = "20270119000000_Index_creature_appearance";

        /// <summary>In the doorwell's area (10 m round 279.05, 66.07) and well out of the bridge's (7 m round 317, 65).</summary>
        private static readonly Vector3 Doorwell = new(284f, 120.6f, 66f);

        /// <summary>Before the pile (275.27, 64.37), within the 8 m of its area and in the doorwell's.</summary>
        private static readonly Vector3 AtThePile = new(281f, 120.9f, 65f);

        [TestMethod]
        public void RoundTheRockWallToTheDoorwellTheRocksBlowAndTheWaveComes()
        {
            using var harness = Promoted();
            var rocks = BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, BootcampCaveInBreachV9.CaveIn);
            Assert.AreEqual(UseObjectState.DoorStateClosed, rocks.StateId);

            BootcampCaveInBreachTests.Cross(harness, Doorwell);

            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1994].Objectives[2].State, "the way out found");
            Assert.AreEqual(UseObjectState.DoorStateOpen, rocks.StateId, "and the rocks blown, though the bridge was never crossed");

            harness.UtcNow += TimeSpan.FromSeconds(2.5);
            harness.Manager.TickScenarios(harness.Client);
            Assert.HasCount(6, BootcampCaveInBreachTests.Thrax(harness), "the one wave");
        }

        [TestMethod]
        public void OverTheBridgeAndThenToTheDoorwellIsStillOneWave()
        {
            using var harness = Promoted();
            var rocks = BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, BootcampCaveInBreachV9.CaveIn);

            BootcampCaveInBreachTests.Cross(harness, new Vector3(340f, 121.7f, 64.5f));
            BootcampCaveInBreachTests.Cross(harness, new Vector3(316f, 120.61f, 64.5f));
            Assert.AreEqual(UseObjectState.DoorStateOpen, rocks.StateId, "blown off the bridge, as before");
            harness.UtcNow += TimeSpan.FromSeconds(2.5);
            harness.Manager.TickScenarios(harness.Client);
            Assert.HasCount(6, BootcampCaveInBreachTests.Thrax(harness));

            BootcampCaveInBreachTests.Cross(harness, Doorwell);
            harness.UtcNow += TimeSpan.FromSeconds(5);
            harness.Manager.TickScenarios(harness.Client);

            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1994].Objectives[2].State);
            Assert.AreSame(rocks, BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, BootcampCaveInBreachV9.CaveIn), "the same pile");
            Assert.AreEqual(UseObjectState.DoorStateOpen, rocks.StateId);
            Assert.HasCount(6, BootcampCaveInBreachTests.Thrax(harness), "no second wave");
        }

        /// <summary>
        /// A player caught before: "Find a way out of the cave" done, Tizzik's objective open, the
        /// pile shut. Coming up to the pile blows it.
        /// </summary>
        [TestMethod]
        public void APlayerCaughtWithThePileShutBlowsItByComingUpToThePile()
        {
            using var harness = Promoted();
            var rocks = BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, BootcampCaveInBreachV9.CaveIn);
            var id = harness.Client.Player.Id;

            using (var unit = harness.Context.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    var objectives = unit.CharacterMissionProgress.GetTracked(id, 1994);
                    objectives[2].ObjectiveState = (byte)MissionObjectiveState.Completed;
                    objectives[1].ObjectiveState = (byte)MissionObjectiveState.Incomplete;
                });
            using (var reload = harness.Context.CreateChar())
                harness.Manager.Hydrate(harness.Client.Player, reload.CharacterMissions.Get(id), reload.CharacterMissionProgress.Get(id));

            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1994].Objectives[1].State);
            Assert.AreEqual(UseObjectState.DoorStateClosed, rocks.StateId, "caught: the pile shut");

            BootcampCaveInBreachTests.Cross(harness, AtThePile);

            Assert.AreEqual(UseObjectState.DoorStateOpen, rocks.StateId, "blown on coming up to the pile");
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1994].Objectives[1].State, "Tizzik still to fight");
            harness.UtcNow += TimeSpan.FromSeconds(2.5);
            harness.Manager.TickScenarios(harness.Client);
            Assert.HasCount(6, BootcampCaveInBreachTests.Thrax(harness));
        }

        [TestMethod]
        public void TheRowsGoInOnceAndDownTakesOutOnlyThem()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Before);
            var before = Counts(database);

            migrator.Migrate(Migration);
            Assert.AreEqual(1, Count(database, "mission_action WHERE mission_id = 1994 AND objective_id = 2 AND transition_id = 1 AND action_id = 5 AND kind = 5 AND scenario_id = 5"));
            Assert.AreEqual(1, Count(database, "mission_objective_transition WHERE mission_id = 1994 AND objective_id = 1 AND transition_id = 2 AND from_state = 1 AND to_state = 1"));
            Assert.AreEqual(1, Count(database, "mission_area WHERE mission_id = 1994 AND area_id = 441 AND shape = 1 AND map_context_id = 1985 AND radius = 8"));
            Assert.AreEqual(1, Count(database, "mission_trigger WHERE mission_id = 1994 AND objective_id = 1 AND transition_id = 2 AND kind = 4 AND area_id = 441"));
            Assert.AreEqual(1, Count(database, "mission_action WHERE mission_id = 1994 AND objective_id = 1 AND transition_id = 2 AND action_id = 1 AND kind = 5 AND scenario_id = 5"));
            var after = Counts(database);
            Assert.AreEqual((before.Actions + 2, before.Transitions + 1, before.Triggers + 1, before.Areas + 1), after);

            foreach (var sql in BootcampCaveInAnyRouteV13.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(after, Counts(database), "a second run adds nothing");

            migrator.Migrate(Before);
            Assert.AreEqual(before, Counts(database));
        }

        [TestMethod]
        public void OnMySqlTheRowsGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "SELECT 1994, 'deployment_11', 2, 1, 5, 'Blow the cave-in, whichever way the player came', 5, 1, 5, 5 FROM mission_objective_transition t");
            StringAssert.Contains(up, "SELECT 1994, 'deployment_11', 441, 1, 1985, 1, 275.2656, 121.2305, 64.3711, 8, '1994 the cave-in pile'");
            StringAssert.Contains(up, "SELECT 1994, 'deployment_11', 1, 2, 1, 2, 1, 1, 'Cave-in pile reached, still shut'");
            StringAssert.Contains(up, "SELECT 1994, 'deployment_11', 1, 2, 1, 1, 4, 1, 441, 'Come up to the cave-in pile'");
            StringAssert.Contains(down, "DELETE FROM mission_area WHERE mission_id = 1994 AND content_revision = 'deployment_11' AND area_id = 441;");
            StringAssert.Contains(up, $"'{Migration}'");
            StringAssert.Contains(down, "DELETE FROM mission_objective_transition WHERE mission_id = 1994 AND content_revision = 'deployment_11' AND objective_id = 1 AND transition_id = 2;");
            StringAssert.Contains(down, "DELETE FROM mission_action WHERE mission_id = 1994 AND content_revision = 'deployment_11' AND objective_id = 2 AND transition_id = 1 AND action_id = 5;");
        }

        /// <summary>Capture the Flag taken from Corporal DeSimone and the promotion done: "Find a way out of the cave" open.</summary>
        private static BootcampRuntimeTestHarness.Harness Promoted()
        {
            var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);
            harness.Client.Player.GmFlagAlwaysFriendly = true;
            harness.SeedMission(harness.Client.Player.Id, 1992, (uint)MissionState.Completed, true);
            var deSimone = BootcampCaveInBreachTests.Creatures(harness).Single(actor => actor.DbId == BootcampRuntimeTestHarness.CorporalDeSimoneCreatureId);
            harness.MovePlayerTo(deSimone);
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Drain();
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, deSimone.EntityId, 1994));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, deSimone.EntityId, 1994, 4, 1));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1994].Objectives[2].State);
            harness.Drain();
            return harness;
        }

        private static (int Actions, int Transitions, int Triggers, int Areas) Counts(OrdinaryLootTestDatabase database) =>
            (Count(database, "mission_action"), Count(database, "mission_objective_transition"), Count(database, "mission_trigger"), Count(database, "mission_area"));

        private static int Count(OrdinaryLootTestDatabase database, string from) =>
            database.Context.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM {from}").AsEnumerable().Single();
    }
}
