using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Mission.Server;
    using Rasa.Repositories.Char;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Repositories.World;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;

    [TestClass]
    [DoNotParallelize]
    public class MissionDeadlineTests
    {
        [TestMethod]
        public void AcceptCreatesActiveDeadlineFromInjectedUtcClock()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            var fixture = CreateDeadlineFixture();
            var now = new DateTime(2026, 9, 19, 6, 30, 0, DateTimeKind.Utc);
            MissionApplication manager = null;
            var deadlineService = new MissionDeadlineService(
                () => context,
                () => manager,
                () => now);
            manager = LoadManager(context, fixture, deadlineService);
            var giver = context.AddNpc(101);

            Assert.IsTrue(manager.AcceptOfferedMission(
                context.Client,
                giver.EntityId,
                321));

            using var unit = context.CreateChar();
            var deadline = unit.CharacterMissionDeadlines.Get(1, 321);
            Assert.IsNotNull(deadline);
            Assert.AreEqual(now.AddSeconds(5), deadline.DueAtUtc);
            Assert.AreEqual(CharacterMissionDeadlineState.Active, deadline.State);
        }

        [TestMethod]
        public void DeadlineExpiryCompletesAcrossReconnectWithoutReplay()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            var fixture = CreateDeadlineFixture();
            var now = new DateTime(2026, 9, 19, 6, 30, 0, DateTimeKind.Utc);
            MissionApplication manager = null;
            var deadlineService = new MissionDeadlineService(
                () => context,
                () => manager,
                () => now);
            manager = LoadManager(context, fixture, deadlineService);
            var giver = context.AddNpc(101);

            Assert.IsTrue(manager.AcceptOfferedMission(
                context.Client,
                giver.EntityId,
                321));
            using (var reloaded = context.CreateChar())
                manager.Hydrate(
                    context.Client.Player,
                    reloaded.CharacterMissions.Get(context.Client.Player.Id),
                    reloaded.CharacterMissionProgress.Get(context.Client.Player.Id));
            context.Drain();

            now = now.AddSeconds(6);

            Assert.IsTrue(manager.EvaluateDeadlines(context.Client));
            Assert.IsFalse(manager.EvaluateDeadlines(context.Client));

            Assert.AreEqual(
                MissionObjectiveState.Completed,
                context.Client.Player.Missions[321].Objectives[10].State);
            Assert.AreEqual(
                1,
                context.Drain().OfType<ObjectiveCompletedPacket>().Count());
            using var unit = context.CreateChar();
            Assert.AreEqual(
                CharacterMissionDeadlineState.Expired,
                unit.CharacterMissionDeadlines.Get(1, 321).State);
        }

        [TestMethod]
        public void ConcurrentDeadlineEvaluationCommitsOneCompletion()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            var fixture = CreateDeadlineFixture();
            var now = new DateTime(2026, 9, 19, 6, 30, 0, DateTimeKind.Utc);
            MissionApplication manager = null;
            var deadlineService = new MissionDeadlineService(
                () => context,
                () => manager,
                () => now);
            manager = LoadManager(context, fixture, deadlineService);
            var giver = context.AddNpc(101);

            Assert.IsTrue(manager.AcceptOfferedMission(
                context.Client,
                giver.EntityId,
                321));
            var competitor = context.CreateCompetingClient(manager);
            context.Drain();
            now = now.AddSeconds(6);

            using var start = new ManualResetEventSlim();
            var results = Task.WhenAll(
                Task.Run(() =>
                {
                    start.Wait();
                    return manager.EvaluateDeadlines(context.Client);
                }),
                Task.Run(() =>
                {
                    start.Wait();
                    return manager.EvaluateDeadlines(competitor);
                }));
            start.Set();

            Assert.AreEqual(1, results.GetAwaiter().GetResult().Count(result => result));
            Assert.AreEqual(
                1,
                context.Drain()
                    .Concat(MissionTestContext.Drain(competitor))
                    .OfType<ObjectiveCompletedPacket>()
                    .Count());
        }

        private static MissionContentFixture CreateDeadlineFixture()
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
                Comment = "Wait five seconds"
            });
            fixture.Triggers.Add(new MissionTriggerEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 20,
                TriggerId = 1,
                Requirement = MissionContentRequirement.Required,
                Kind = MissionTriggerKind.TimerElapsed,
                Sequence = 1,
                DurationSeconds = 5,
                Comment = "Timer"
            });
            return fixture;
        }

        private static MissionApplication LoadManager(
            MissionTestContext context,
            MissionContentFixture fixture,
            MissionDeadlineService deadlineService)
        {
            var manager = new MissionApplication(
                new MissionContentLoadingFactory(context, fixture.CreateWorldUnitOfWork()),
                new Dictionary<uint, Mission>(),
                deadlineService);
            var report = manager.LoadMissions();
            Assert.IsFalse(
                report.BlocksReadiness,
                string.Join(" | ", report.Diagnostics.Select(diagnostic => diagnostic.Code)));
            return manager;
        }

        private sealed class MissionContentLoadingFactory : IGameUnitOfWorkFactory
        {
            private readonly MissionTestContext _charFactory;
            private readonly IWorldUnitOfWork _worldUnit;

            internal MissionContentLoadingFactory(
                MissionTestContext charFactory,
                IWorldUnitOfWork worldUnit)
            {
                _charFactory = charFactory;
                _worldUnit = worldUnit;
            }

            public ICharUnitOfWork CreateChar() => _charFactory.CreateChar();
            public IWorldUnitOfWork CreateWorld() => _worldUnit;
        }
    }
}
