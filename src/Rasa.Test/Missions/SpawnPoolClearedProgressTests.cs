extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Mission.Server;
    using Rasa.Structures;
    using Rasa.Structures.Missions;
    using Rasa.Structures.World;

    /// <summary>
    /// "Secure the outer Bane Outpost" (1977 Holy Vengeance, client missionobjective): an
    /// objective complete when every spawn pool it names is dead at once in the squad's instance
    /// the player is in (MissionProgressEventKind.SpawnPoolCleared, SpawnPoolClearedProgress).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class SpawnPoolClearedProgressTests
    {
        private const uint MissionId = 321;
        private const uint ObjectiveId = 1;

        // The outer Bane Outpost of Velon Hollow (NOTES-missing-mechanics.md, 1977).
        private const uint OuterA = 531225, OuterB = 531226, Inner = 531224;
        private const uint VelonHollow = 2136;

        private Func<MissionApplication> _missions;

        [TestInitialize]
        public void KeepTheServersMissions() => _missions = SpawnPoolClearedProgress.Missions;

        [TestCleanup]
        public void PutBackTheServersMissions() => SpawnPoolClearedProgress.Missions = _missions;

        #region Authoring

        [TestMethod]
        public void SeveralPoolTriggersAreOneObjectiveOverAllOfThePools()
        {
            var rule = Rule(OuterA, OuterB);

            Assert.AreEqual(MissionProgressEventKind.SpawnPoolCleared, rule.Kind);
            CollectionAssert.AreEqual(new[] { OuterA, OuterB }, rule.Subjects.ToArray());
            Assert.IsNull(rule.CounterId);
        }

        [TestMethod]
        public void OnePoolTriggerIsAnObjectiveOverThatPool()
        {
            var rule = Rule(Inner);

            Assert.AreEqual(MissionProgressEventKind.SpawnPoolCleared, rule.Kind);
            CollectionAssert.AreEqual(new[] { Inner }, rule.Subjects.ToArray());
        }

        [TestMethod]
        public void APoolTriggerDoesNotShareATransitionWithAWaypoint()
        {
            var triggers = new[]
            {
                Trigger(1, MissionProgressEventKind.SpawnPoolCleared, OuterA),
                Trigger(2, MissionProgressEventKind.WaypointAcquired, 49)
            };

            Assert.IsFalse(MissionProgressRuleAuthoring.TryBuild(triggers, out _, out _, out _, out var diagnostic));
            StringAssert.Contains(diagnostic, "one event_kind");
        }

        [TestMethod]
        public void APoolRuleHasNoCounter()
        {
            var triggers = new[] { OuterA, OuterB }.Select((pool, index) =>
            {
                var entry = TriggerRow((uint)index + 1, MissionProgressEventKind.SpawnPoolCleared, pool);
                entry.CounterId = 0;
                entry.InitialValue = 0;
                entry.TargetValue = 2;
                return new MissionTriggerDefinition(entry);
            }).ToArray();

            Assert.IsFalse(MissionProgressRuleAuthoring.TryBuild(triggers, out _, out _, out _, out var diagnostic));
            StringAssert.Contains(diagnostic, "no counter_id");
            Assert.ThrowsExactly<ArgumentException>(() => MissionProgressRule.CompleteWhenAllDistinctSubjectsObserved(
                MissionProgressEventKind.SpawnPoolCleared, new HashSet<uint> { OuterA }, counterId: 0));
        }

        [TestMethod]
        public void AClearedPoolObjectiveMayCountExistingFacts()
        {
            var objective = Objective(Rule(OuterA, OuterB), recognizeExistingFacts: true);

            Assert.IsTrue(objective.HasCompleteServerContract);
        }

        [TestMethod]
        public void TheValidatorAcceptsAPoolTheWorldHas()
        {
            var fixture = MissionContentFixture.CreateValid();
            fixture.Triggers.Clear();
            fixture.Actions.Clear();
            fixture.Transitions.Clear();
            fixture.Rewards.Clear();
            fixture.RewardItems.Clear();
            fixture.Indicators.Clear();
            fixture.Objectives.RemoveAll(objective => objective.ObjectiveId == 11);
            fixture.Transitions.Add(new MissionObjectiveTransitionEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 20,
                Requirement = MissionContentRequirement.Required,
                Sequence = 1,
                FromState = (byte)MissionObjectiveState.Incomplete,
                ToState = (byte)MissionObjectiveState.Completed,
                Comment = "Pools cleared"
            });
            fixture.Triggers.Add(new MissionTriggerEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 20,
                TriggerId = 1,
                Requirement = MissionContentRequirement.Required,
                Kind = MissionTriggerKind.ProgressEvent,
                Sequence = 1,
                EventKind = (byte)MissionProgressEventKind.SpawnPoolCleared,
                SubjectId = OuterA,
                Comment = "Outer pool cleared"
            });
            fixture.SpawnPools.Add(new SpawnPoolEntry { Id = OuterA, MapContextId = VelonHollow });

            var snapshot = new MissionContentLoader().Load(fixture.CreateRepository());
            var report = new MissionContentValidator().Validate(snapshot, fixture.CreateWorldUnitOfWork());

            CollectionAssert.DoesNotContain(report.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray(), "missing-spawn-pool");
        }

        #endregion

        #region Progress

        [TestMethod]
        public void TheObjectiveIsCompleteOnlyWhenEveryPoolIsDeadAtOnce()
        {
            using var context = Accepted(Rule(OuterA, OuterB));
            var squad = Squad(context.Map);
            var (a, b) = (Pool(context.Map, OuterA), Pool(context.Map, OuterB));

            squad.PoolCleared(a);

            Assert.IsFalse(context.Manager.RecordProgress(context.Client, MissionProgressEvent.SpawnPoolCleared(OuterA)));
            Assert.AreEqual(MissionObjectiveState.Incomplete, State(context));
            Assert.AreEqual(0, context.Drain().OfType<ObjectiveCompletedPacket>().Count());

            squad.PoolCleared(b);

            Assert.IsTrue(context.Manager.RecordProgress(context.Client, MissionProgressEvent.SpawnPoolCleared(OuterB)));
            Assert.AreEqual(MissionObjectiveState.Completed, State(context));
            Assert.AreEqual(1, context.Drain().OfType<ObjectiveCompletedPacket>().Count());
        }

        [TestMethod]
        public void APoolThatCameBackIsToBeClearedAgain()
        {
            using var context = Accepted(Rule(OuterA, OuterB));
            var squad = Squad(context.Map);
            var (a, b) = (Pool(context.Map, OuterA), Pool(context.Map, OuterB));

            squad.PoolCleared(a);
            squad.PoolRespawned(a);
            squad.PoolCleared(b);

            Assert.IsFalse(context.Manager.RecordProgress(context.Client, MissionProgressEvent.SpawnPoolCleared(OuterB)));
            Assert.AreEqual(MissionObjectiveState.Incomplete, State(context));

            squad.PoolCleared(a);

            Assert.IsTrue(context.Manager.RecordProgress(context.Client, MissionProgressEvent.SpawnPoolCleared(OuterA)));
            Assert.AreEqual(MissionObjectiveState.Completed, State(context));
        }

        [TestMethod]
        public void APoolTheObjectiveDoesNotNameIsNoMatter()
        {
            using var context = Accepted(Rule(OuterA));
            var squad = Squad(context.Map);

            squad.PoolCleared(Pool(context.Map, Inner));

            Assert.IsFalse(context.Manager.RecordProgress(context.Client, MissionProgressEvent.SpawnPoolCleared(Inner)));
            Assert.AreEqual(MissionObjectiveState.Incomplete, State(context));
        }

        [TestMethod]
        public void OffASquadsInstanceNoPoolIsCleared()
        {
            using var context = Accepted(Rule(OuterA));
            var a = Pool(context.Map, OuterA);
            a.ClearedAtUtcMs = 1;

            Assert.IsNull(context.Map.SquadState);
            Assert.AreEqual(0, MissionApplication.ClearedSpawnPools(context.Client).Count);
            Assert.IsFalse(context.Manager.RecordProgress(context.Client, MissionProgressEvent.SpawnPoolCleared(OuterA)));
            Assert.AreEqual(MissionObjectiveState.Incomplete, State(context));
        }

        [TestMethod]
        public void ThePlayersInTheInstanceHearOfItOnTheSpawnPoolsNextPass()
        {
            using var context = Accepted(Rule(OuterA, OuterB));
            SpawnPoolClearedProgress.Missions = () => context.Manager;
            var squad = Squad(context.Map);

            squad.PoolCleared(Pool(context.Map, OuterA));
            squad.PoolCleared(Pool(context.Map, OuterB));

            // A pool cleared is only put by to be told: nothing is recorded where it died.
            Assert.AreEqual(MissionObjectiveState.Incomplete, State(context));

            SpawnPoolManager.Instance.SpawnPoolWorker(context.Map, 0);

            Assert.AreEqual(MissionObjectiveState.Completed, State(context));
            Assert.AreEqual(1, context.Drain().OfType<ObjectiveCompletedPacket>().Count());
            Assert.AreEqual(0, squad.TakeNewlyCleared().Count);

            // Told once.
            SpawnPoolClearedProgress.Announce(context.Map);
            Assert.AreEqual(0, context.Drain().OfType<ObjectiveCompletedPacket>().Count());
        }

        [TestMethod]
        public void EveryPlayerInTheInstanceIsTold()
        {
            using var context = Accepted(Rule(OuterA));
            SpawnPoolClearedProgress.Missions = () => context.Manager;
            var mate = context.CreateAdditionalClient(2);
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(mate, giver.EntityId, MissionId));
            var squad = Squad(context.Map);

            squad.PoolCleared(Pool(context.Map, OuterA));
            SpawnPoolClearedProgress.Announce(context.Map);

            Assert.AreEqual(MissionObjectiveState.Completed, State(context));
            Assert.AreEqual(MissionObjectiveState.Completed, mate.Player.Missions[MissionId].Objectives[ObjectiveId].State);
        }

        [TestMethod]
        public void OneNotInTheWorldAtThePassIsToldOnceTheyAre()
        {
            using var context = Accepted(Rule(OuterA, OuterB));
            SpawnPoolClearedProgress.Missions = () => context.Manager;
            var squad = Squad(context.Map);
            context.Client.State = ClientState.Teleporting;

            squad.PoolCleared(Pool(context.Map, OuterA));
            squad.PoolCleared(Pool(context.Map, OuterB));
            SpawnPoolClearedProgress.Announce(context.Map);

            Assert.AreEqual(MissionObjectiveState.Incomplete, State(context));
            CollectionAssert.Contains(squad.Untold.ToArray(), context.Client.Player.Id);

            // Still not in: kept for later.
            SpawnPoolClearedProgress.Announce(context.Map);
            CollectionAssert.Contains(squad.Untold.ToArray(), context.Client.Player.Id);

            context.Client.State = ClientState.Ingame;
            SpawnPoolClearedProgress.Announce(context.Map);

            Assert.AreEqual(MissionObjectiveState.Completed, State(context));
            Assert.AreEqual(0, squad.Untold.Count);
        }

        [TestMethod]
        public void OneWhoLeftTheInstanceUntoldIsForgotten()
        {
            using var context = Accepted(Rule(OuterA));
            var squad = Squad(context.Map);
            squad.Untold.Add(999);

            SpawnPoolClearedProgress.Announce(context.Map);

            Assert.AreEqual(0, squad.Untold.Count);
        }

        [TestMethod]
        public void APoolThatCameBackBeforeThePassIsNotTold()
        {
            using var context = Accepted(Rule(OuterA, OuterB));
            SpawnPoolClearedProgress.Missions = () => context.Manager;
            var squad = Squad(context.Map);
            var a = Pool(context.Map, OuterA);

            squad.PoolCleared(a);
            squad.PoolRespawned(a);
            squad.PoolCleared(Pool(context.Map, OuterB));

            SpawnPoolClearedProgress.Announce(context.Map);

            Assert.AreEqual(MissionObjectiveState.Incomplete, State(context));
        }

        [TestMethod]
        public void AKillMapResetForgetsThePoolsNotYetTold()
        {
            using var context = Accepted(Rule(OuterA));
            var squad = Squad(context.Map);
            var a = Pool(context.Map, OuterA);

            squad.PoolCleared(a);
            squad.PoolsReset(context.Map.SpawnPools);

            Assert.AreEqual(0, squad.TakeNewlyCleared().Count);
            Assert.AreEqual(0, a.ClearedAtUtcMs);
        }

        [TestMethod]
        public void TakingTheMissionInAClearedInstanceCountsThePoolsDeadThere()
        {
            using var context = MissionTestContext.WithProgressMission(Rule(OuterA, OuterB), recognizeExistingFacts: true);
            var squad = Squad(context.Map);
            squad.PoolCleared(Pool(context.Map, OuterA));
            squad.PoolCleared(Pool(context.Map, OuterB));
            var giver = context.AddNpc(77);

            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, MissionId));

            Assert.AreEqual(MissionObjectiveState.Completed, State(context));
        }

        [TestMethod]
        public void TakingTheMissionWithoutExistingFactsWaitsForTheNextPool()
        {
            using var context = MissionTestContext.WithProgressMission(Rule(OuterA, OuterB));
            var squad = Squad(context.Map);
            squad.PoolCleared(Pool(context.Map, OuterA));
            squad.PoolCleared(Pool(context.Map, OuterB));
            var giver = context.AddNpc(77);

            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, MissionId));

            Assert.AreEqual(MissionObjectiveState.Incomplete, State(context));
        }

        [TestMethod]
        public void OneWhoComesIntoTheInstanceAfterThePoolsDiedHearsOfThem()
        {
            using var context = Accepted(Rule(OuterA, OuterB));
            SpawnPoolClearedProgress.Missions = () => context.Manager;
            var squad = Squad(context.Map);
            squad.PoolCleared(Pool(context.Map, OuterA));
            squad.PoolCleared(Pool(context.Map, OuterB));
            squad.TakeNewlyCleared();   // told to those who were there

            // The map change is over (ManifestationManager.FinishArrival), with the server's
            // missions being the test's for the radio offers on arrival.
            var singleton = typeof(MissionApplication).GetField("_instance",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
            var previous = singleton.GetValue(null);
            singleton.SetValue(null, context.Manager);

            try
            {
                context.Client.MissionArrivalPending = true;
                ManifestationManager.Instance.FinishArrival(context.Client);
            }
            finally
            {
                singleton.SetValue(null, previous);
            }

            Assert.IsFalse(context.Client.MissionArrivalPending);
            Assert.AreEqual(MissionObjectiveState.Completed, State(context));
        }

        #endregion

        private static MissionTestContext Accepted(MissionProgressRule rule)
        {
            var context = MissionTestContext.WithProgressMission(rule);
            var giver = context.AddNpc(77);

            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, MissionId));
            context.Drain();

            return context;
        }

        private static MissionObjectiveState State(MissionTestContext context) =>
            context.Client.Player.Missions[MissionId].Objectives[ObjectiveId].State;

        /// <summary>The map is a squad's instance, not saved: which of its pools are dead is kept in memory.</summary>
        private static SquadInstanceState Squad(MapChannel map)
        {
            var now = 1_000L;

            map.SquadState = new SquadInstanceState(0, now, () => now++, () => 45 * 60_000L,
                (_, _, _) => { }, (_, _) => { }, _ => { });

            foreach (var id in new[] { OuterA, OuterB, Inner })
                Pool(map, id);

            return map.SquadState;
        }

        private static SpawnPool Pool(MapChannel map, uint id)
        {
            var pool = map.SpawnPools.SingleOrDefault(entry => entry.DbId == id);

            if (pool != null)
                return pool;

            pool = new SpawnPool
            {
                DbId = id,
                MapContextId = map.MapInfo.MapContextId,
                Mode = SpawnPoolManager.ModeAutomatic,
                RuntimeMapChannel = map,
                SpawnSlot = new List<SpawnPoolSlot>()
            };
            map.SpawnPools.Add(pool);

            return pool;
        }

        private static MissionProgressRule Rule(params uint[] pools)
        {
            var authored = MissionProgressRuleAuthoring.TryBuild(
                pools.Select((pool, index) => Trigger((uint)index + 1, MissionProgressEventKind.SpawnPoolCleared, pool)).ToArray(),
                out var rule, out _, out _, out var diagnostic);

            Assert.IsTrue(authored, diagnostic);

            return rule;
        }

        private static MissionTriggerDefinition Trigger(uint id, MissionProgressEventKind kind, uint subject) =>
            new(TriggerRow(id, kind, subject));

        private static MissionTriggerEntry TriggerRow(uint id, MissionProgressEventKind kind, uint subject) =>
            new()
            {
                MissionId = MissionId,
                ContentRevision = "spawn-pool-cleared-test",
                ObjectiveId = ObjectiveId,
                TransitionId = 1,
                TriggerId = id,
                Requirement = MissionContentRequirement.Required,
                Kind = MissionTriggerKind.ProgressEvent,
                Sequence = id,
                EventKind = (byte)kind,
                SubjectId = subject,
                Comment = "Pool cleared"
            };

        private static MissionObjectiveDefinition Objective(MissionProgressRule rule, bool recognizeExistingFacts) =>
            new(ObjectiveId, 1001, 1002, new uint?[3], 0, MissionObjectiveState.Incomplete, true,
                new Dictionary<uint, MissionObjectiveCounterDefinition>(),
                new Dictionary<uint, MissionObjectiveItemCounterDefinition>(),
                Array.Empty<MissionObjectiveConversation>(), Array.Empty<uint>(), Array.Empty<uint>(),
                Array.Empty<MissionIndicator>(), rule, recognizeExistingFacts: recognizeExistingFacts);
    }
}
