using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Models;
    using Rasa.Packets.Mission.Server;
    using Rasa.Repositories.Char;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Repositories.World;
    using Rasa.Structures;
    using Rasa.Structures.World;

    [TestClass]
    [DoNotParallelize]
    public class MissionAreaProgressTests
    {
        [TestMethod]
        public void AcceptedOutsideToInsideMovementCompletesAreaObjectiveOnce()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            var fixture = CreateAreaFixture();
            var manager = LoadManager(context, fixture);
            context.Client.MissionAreaService = new MissionAreaService(() => manager);
            var giver = context.AddNpc(101);

            Assert.IsTrue(manager.AcceptOfferedMission(
                context.Client,
                giver.EntityId,
                321));

            context.Client.Player.Position = new Vector3(5.25f, 0, 0);
            context.Client.Movement = new Movement(context.Client.Player.Position, 1f, 0, Vector2.Zero);
            context.Client.Player.MoveBudget = 10;
            context.Drain();

            Assert.IsTrue(context.Client.HandleMovement(
                new Movement(new Vector3(4.75f, 0, 0), 1f, 0, Vector2.Zero)));
            Assert.IsTrue(context.Client.HandleMovement(
                new Movement(new Vector3(4.5f, 0, 0), 1f, 0, Vector2.Zero)));
            Assert.IsTrue(context.Client.HandleMovement(
                new Movement(new Vector3(4.25f, 0, 0), 1f, 0, Vector2.Zero)));

            Assert.AreEqual(
                MissionObjectiveState.Completed,
                context.Client.Player.Missions[321].Objectives[10].State);
            Assert.AreEqual(
                1,
                context.Drain().OfType<ObjectiveCompletedPacket>().Count());
        }

        [TestMethod]
        public void ConcurrentAreaEntriesPublishOneObjectiveCompletion()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            var fixture = CreateAreaFixture();
            var manager = LoadManager(context, fixture);
            context.Client.MissionAreaService = new MissionAreaService(() => manager);
            var giver = context.AddNpc(101);

            Assert.IsTrue(manager.AcceptOfferedMission(
                context.Client,
                giver.EntityId,
                321));
            var competitor = context.CreateCompetingClient(manager);
            competitor.MissionAreaService = new MissionAreaService(() => manager);
            context.Drain();

            context.Client.Player.Position = new Vector3(5.25f, 0, 0);
            context.Client.Movement = new Movement(context.Client.Player.Position, 1f, 0, Vector2.Zero);
            context.Client.Player.MoveBudget = 10;
            competitor.Player.Position = new Vector3(5.25f, 0, 0);
            competitor.Movement = new Movement(competitor.Player.Position, 1f, 0, Vector2.Zero);
            competitor.Player.MoveBudget = 10;

            using var start = new ManualResetEventSlim();
            var results = Task.WhenAll(
                Task.Run(() =>
                {
                    start.Wait();
                    return context.Client.HandleMovement(
                        new Movement(new Vector3(4.75f, 0, 0), 1f, 0, Vector2.Zero));
                }),
                Task.Run(() =>
                {
                    start.Wait();
                    return competitor.HandleMovement(
                        new Movement(new Vector3(4.75f, 0, 0), 1f, 0, Vector2.Zero));
                }));
            start.Set();

            Assert.IsTrue(results.GetAwaiter().GetResult().All(result => result));
            Assert.AreEqual(
                1,
                context.Drain()
                    .Concat(MissionTestContext.Drain(competitor))
                    .OfType<ObjectiveCompletedPacket>()
                    .Count());
        }

        private static MissionContentFixture CreateAreaFixture()
        {
            var fixture = MissionContentFixture.CreateValid();
            fixture.Triggers.Clear();
            fixture.Actions.Clear();
            fixture.Transitions.Clear();
            fixture.Rewards.Clear();
            fixture.RewardItems.Clear();
            fixture.Indicators.Clear();
            fixture.Objectives.RemoveAll(objective => objective.ObjectiveId == 11);
            fixture.Areas[0].PosX = 0;
            fixture.Areas[0].PosY = 0;
            fixture.Areas[0].PosZ = 0;
            fixture.Areas[0].Radius = 5;
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
                Comment = "Enter the area"
            });
            fixture.Triggers.Add(new MissionTriggerEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 20,
                TriggerId = 1,
                Requirement = MissionContentRequirement.Required,
                Kind = MissionTriggerKind.AreaEntered,
                Sequence = 1,
                AreaId = 30,
                Comment = "Enter area 30"
            });
            return fixture;
        }

        private static MissionApplication LoadManager(
            MissionTestContext context,
            MissionContentFixture fixture)
        {
            var manager = new MissionApplication(
                new MissionContentLoadingFactory(context, fixture.CreateWorldUnitOfWork()),
                new Dictionary<uint, Mission>());
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
