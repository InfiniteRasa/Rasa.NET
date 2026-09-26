using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Runtime
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Structures;

    [TestClass]
    [DoNotParallelize]
    public class MissionProductionScalingTests
    {
        [TestMethod]
        public void RealProgressAndNpcLookupIgnoreUnrelatedThousandMissionCatalog()
        {
            var definitions = Enumerable.Range(1, 1000).ToDictionary(id => (uint)id, id =>
            {
                var objective = new MissionObjectiveDefinition(1, 1, 1, new uint?[3], 0,
                    MissionObjectiveState.Incomplete, true, new Dictionary<uint, MissionObjectiveCounterDefinition>(),
                    new Dictionary<uint, MissionObjectiveItemCounterDefinition>(), Array.Empty<MissionObjectiveConversation>(),
                    Array.Empty<uint>(), Array.Empty<uint>(), Array.Empty<MissionIndicator>(),
                    MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled, (uint)id));
                return new Mission((uint)id, $"Fixture {id}", (uint)id, (uint)id, (uint)id,
                    1, 1, 1, false, false, new[] { objective }, true);
            });
            using var context = MissionTestContext.WithCustomDefinitions(definitions);
            var giver = context.AddNpc(43);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 43));
            var before = context.Manager.TotalRuleEvaluations;
            Assert.IsTrue(context.Manager.RecordProgress(context.Client, MissionProgressEvent.Creature(43)));
            Assert.AreEqual(2L, context.Manager.TotalRuleEvaluations - before,
                "Admission and transaction evaluate the one subscribed rule, not all 1000 definitions.");
            Assert.IsFalse(context.Manager.RecordProgress(context.Client, MissionProgressEvent.Creature(2001)));
            Assert.AreEqual(0, context.Manager.LastRuleEvaluations);
            var stranger = context.AddNpc(2001);
            Assert.IsFalse(context.Manager.ClassifyNpcConversation(context.Client.Player, stranger)
                .TryGetStatus(out _, out _));
        }

        [TestMethod]
        public void ActualMapWorkerPerformsNoMissionStoragePollingForIdleAssignments()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.RemoveNpcFromWorld(giver);
            BootcampRuntimeTestHarness.PrepareDirectDamageClient(context.Client);
            var maps = new MapChannelManager(context, scenarioService: context.Manager.ScenarioService);
            maps.MapChannelArray.Add(context.Map.MapInfo.MapContextId, context.Map);
            maps.Timer.Add("MissionDeadlineUpdate", 1000, true, null);
            var units = context.CharUnitsCreated;
            for (var tick = 0; tick < 100; tick++)
                maps.MapChannelWorker(1000);
            Assert.AreEqual(units, context.CharUnitsCreated,
                "The production worker must not invoke the retired per-character deadline/scenario database polling.");
        }
    }
}
