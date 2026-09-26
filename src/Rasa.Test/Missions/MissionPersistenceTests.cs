using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Structures.Char;

    [TestClass]
    [DoNotParallelize]
    public class MissionPersistenceTests
    {
        [TestMethod]
        public void ProgressSnapshotGroupsObjectivesAndCountersWithoutCharacterLeakage()
        {
            using var context = new MissionTestContext();
            context.SeedCharacter(10, 0, 100);
            context.SeedCharacter(20, 0, 200);
            context.SeedMission(100, 321, (uint)MissionState.Active, false);
            context.SeedMission(100, 429, (uint)MissionState.Active, false);
            context.SeedMission(200, 321, (uint)MissionState.Active, false);
            context.SeedObjective(100, 321, 1, MissionObjectiveState.Incomplete,
                new Dictionary<uint, uint> { [4] = 5 },
                new Dictionary<uint, uint> { [200] = 6 });
            context.SeedObjective(100, 429, 2, MissionObjectiveState.Completed);
            context.SeedObjective(200, 321, 9, MissionObjectiveState.Failed);

            using var unit = context.CreateChar();
            var snapshot = unit.CharacterMissionProgress.Get(100);

            CollectionAssert.AreEquivalent(new uint[] { 321, 429 }, snapshot.Missions.Keys.ToArray());
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                (MissionObjectiveState)snapshot.Missions[321].Objectives[1].State);
            Assert.AreEqual(5U, snapshot.Missions[321].Objectives[1].Counters[4]);
            Assert.AreEqual(6U, snapshot.Missions[321].Objectives[1].ItemCounters[200]);
            Assert.IsFalse(snapshot.Missions[321].Objectives.ContainsKey(9));
        }

        [TestMethod]
        public void TrackedMutationsPersistAndRejectMissingRows()
        {
            using var context = new MissionTestContext();
            context.SeedCharacter(10, 0, 100);
            context.SeedMission(100, 321, (uint)MissionState.Active, false);
            context.SeedObjective(100, 321, 1, MissionObjectiveState.Incomplete,
                new Dictionary<uint, uint> { [4] = 5 },
                new Dictionary<uint, uint> { [200] = 6 });

            using (var unit = context.CreateChar())
            {
                var tracked = unit.CharacterMissionProgress.Get(100, 321, 1);
                Assert.AreSame(tracked, unit.CharacterMissionProgress.Get(100, 321, 1));
                unit.CharacterMissionProgress.SetObjectiveState(
                    100, 321, 1,
                    (byte)MissionObjectiveState.Incomplete,
                    (byte)MissionObjectiveState.Completed);
                unit.CharacterMissionProgress.SetCounter(100, 321, 1, 4, 5, 7);
                unit.CharacterMissionProgress.SetItemCounter(100, 321, 1, 200, 6, 8);
                Assert.ThrowsExactly<System.InvalidOperationException>(() =>
                    unit.CharacterMissionProgress.SetCounter(100, 321, 1, 99, 0, 1));
            }

            using var reopened = context.CreateChar();
            var objective = reopened.CharacterMissionProgress.Get(100, 321).Missions[321].Objectives[1];
            Assert.AreEqual((byte)MissionObjectiveState.Completed, objective.State);
            Assert.AreEqual(7U, objective.Counters[4]);
            Assert.AreEqual(8U, objective.ItemCounters[200]);
        }

        [TestMethod]
        public void StaleObjectiveAndCounterUpdatesRejectAcrossDistinctContexts()
        {
            using var context = new MissionTestContext();
            context.SeedCharacter(10, 0, 100);
            context.SeedMission(100, 321, (uint)MissionState.Active, false);
            context.SeedObjective(100, 321, 1, MissionObjectiveState.Incomplete,
                new Dictionary<uint, uint> { [4] = 5 },
                new Dictionary<uint, uint> { [200] = 6 });

            using (var current = context.CreateChar())
            using (var stale = context.CreateChar())
            {
                current.CharacterMissionProgress.Get(100, 321, 1);
                stale.CharacterMissionProgress.Get(100, 321, 1);
                current.CharacterMissionProgress.SetObjectiveState(
                    100, 321, 1,
                    (byte)MissionObjectiveState.Incomplete,
                    (byte)MissionObjectiveState.Completed);
                Assert.ThrowsExactly<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>(() =>
                    stale.CharacterMissionProgress.SetObjectiveState(
                        100, 321, 1,
                        (byte)MissionObjectiveState.Incomplete,
                        (byte)MissionObjectiveState.Failed));
            }

            using (var current = context.CreateChar())
            using (var stale = context.CreateChar())
            {
                current.CharacterMissionProgress.Get(100, 321, 1);
                stale.CharacterMissionProgress.Get(100, 321, 1);
                current.CharacterMissionProgress.SetCounter(100, 321, 1, 4, 5, 7);
                Assert.ThrowsExactly<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>(() =>
                    stale.CharacterMissionProgress.SetCounter(100, 321, 1, 4, 5, 9));
            }

            using (var current = context.CreateChar())
            using (var stale = context.CreateChar())
            {
                current.CharacterMissionProgress.Get(100, 321, 1);
                stale.CharacterMissionProgress.Get(100, 321, 1);
                current.CharacterMissionProgress.SetItemCounter(100, 321, 1, 200, 6, 8);
                Assert.ThrowsExactly<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>(() =>
                    stale.CharacterMissionProgress.SetItemCounter(100, 321, 1, 200, 6, 10));
            }

            using var mismatch = context.CreateChar();
            Assert.ThrowsExactly<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>(() =>
                mismatch.CharacterMissionProgress.SetCounter(100, 321, 1, 4, 5, 11));
        }

        [TestMethod]
        public void RemovingMissionCascadesAllObjectiveProgress()
        {
            using var context = new MissionTestContext();
            context.SeedCharacter(10, 0, 100);
            context.SeedMission(100, 321, (uint)MissionState.Active, false);
            context.SeedObjective(100, 321, 1, MissionObjectiveState.Incomplete,
                new Dictionary<uint, uint> { [4] = 5 },
                new Dictionary<uint, uint> { [200] = 6 });

            using (var unit = context.CreateChar())
                unit.CharacterMissions.Remove(100, 321);

            using var reopened = context.CreateChar();
            Assert.AreEqual(0, reopened.CharacterMissionProgress.Get(100).Missions.Count);
        }

        [TestMethod]
        public void RemovingProgressLeavesTheMissionAttemptIntact()
        {
            using var context = new MissionTestContext();
            context.SeedCharacter(10, 0, 100);
            context.SeedMission(100, 321, (uint)MissionState.Active, false);
            context.SeedObjective(100, 321, 1, MissionObjectiveState.Incomplete);

            using (var unit = context.CreateChar())
                unit.CharacterMissionProgress.Remove(100, 321);

            using var reopened = context.CreateChar();
            Assert.IsNotNull(
                reopened.CharacterMissions.GetByCharacterAndMission(100, 321));
            Assert.AreEqual(0, reopened.CharacterMissionProgress.Get(100).Missions.Count);
        }
    }
}
