using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Runtime
{
    using Rasa.Data;
    using Rasa.Structures;

    [TestClass]
    [DoNotParallelize]
    public class MissionRunVersionTests
    {
        [TestMethod]
        public void CounterOnlyProgressAdvancesTheAssignmentVersion()
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.IncrementCounterOnExactSubject(MissionProgressEventKind.CreatureKilled, 55, 0, 0, 5),
                counters: new Dictionary<uint, MissionObjectiveCounterDefinition> { [0] = new(0, 0, 5) });
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            var before = context.ReadMission(321);
            Assert.IsTrue(context.Manager.RecordProgress(context.Client, MissionProgressEvent.Creature(55)));
            var after = context.ReadMission(321);
            Assert.AreEqual(before.AssignmentId, after.AssignmentId);
            Assert.AreEqual(before.Generation, after.Generation);
            Assert.IsGreaterThan(before.Version, after.Version);
        }
    }
}
