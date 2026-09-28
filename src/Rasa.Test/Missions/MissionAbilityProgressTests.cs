using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.Mission.Server;
    using Rasa.Repositories.Char;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Repositories.World;
    using Rasa.Structures;
    using Rasa.Structures.World;

    [TestClass]
    [DoNotParallelize]
    public class MissionAbilityProgressTests
    {
        [TestMethod]
        public void OwnedResolvedAbilityHitCompletesMatchingObjective()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            var fixture = CreateAbilityFixture(targetCreatureId: 501);
            var manager = LoadManager(context, fixture);
            var giver = context.AddNpc(101);
            var abilityManager = CreateManager(manager);
            var target = AddTarget(context, 501, new Vector3(4, 0, 0));

            Assert.IsTrue(manager.AcceptOfferedMission(
                context.Client,
                giver.EntityId,
                321));
            PrepareDirectDamageClient(context.Client);
            context.Drain();

            InvokeResolveDirectDamage(
                abilityManager,
                context.Map,
                context.Client,
                LightningAction(LightningInfo(primaryDamage: 10)),
                LightningInfo(primaryDamage: 10),
                new ActionData(
                    context.Client.Player,
                    ActionId.AaRecruitLightning,
                    1,
                    target.EntityId,
                    0));

            Assert.AreEqual(
                MissionObjectiveState.Completed,
                context.Client.Player.Missions[321].Objectives[10].State);
            Assert.AreEqual(
                1,
                context.Drain().OfType<ObjectiveCompletedPacket>().Count());

            CellManager.Instance.RemoveCreatureFromWorld(context.Map, target);
        }

        [TestMethod]
        public void MissesNonDamagingUsesAndWrongTargetsDoNotAdvanceAbilityObjectives()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            var fixture = CreateAbilityFixture(targetCreatureId: 501);
            var manager = LoadManager(context, fixture);
            var giver = context.AddNpc(101);
            var abilityManager = CreateManager(manager);
            var rightTarget = AddTarget(context, 501, new Vector3(4, 0, 0));
            var wrongTarget = AddTarget(context, 502, new Vector3(5, 0, 0));

            Assert.IsTrue(manager.AcceptOfferedMission(
                context.Client,
                giver.EntityId,
                321));
            PrepareDirectDamageClient(context.Client);
            context.Drain();

            InvokeResolveDirectDamage(
                abilityManager,
                context.Map,
                context.Client,
                LightningAction(LightningInfo(primaryDamage: 10)),
                LightningInfo(primaryDamage: 10),
                new ActionData(
                    context.Client.Player,
                    ActionId.AaRecruitLightning,
                    1,
                    wrongTarget.EntityId,
                    0));
            InvokeResolveDirectDamage(
                abilityManager,
                context.Map,
                context.Client,
                LightningAction(LightningInfo(primaryDamage: 0)),
                LightningInfo(primaryDamage: 0),
                new ActionData(
                    context.Client.Player,
                    ActionId.AaRecruitLightning,
                    1,
                    rightTarget.EntityId,
                    0));

            Assert.AreEqual(
                MissionObjectiveState.Incomplete,
                context.Client.Player.Missions[321].Objectives[10].State);
            Assert.AreEqual(0, context.Drain().OfType<ObjectiveCompletedPacket>().Count());

            CellManager.Instance.RemoveCreatureFromWorld(context.Map, rightTarget);
            CellManager.Instance.RemoveCreatureFromWorld(context.Map, wrongTarget);
        }

        private static MissionContentFixture CreateAbilityFixture(uint targetCreatureId)
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
                Comment = "Hit the target"
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
                EventKind = (byte)MissionProgressEventKind.AbilityHit,
                SubjectId = (uint)ActionId.AaRecruitLightning,
                CounterId = targetCreatureId,
                Comment = "Hit the authoritative dummy"
            });
            fixture.CreatureClasses[targetCreatureId] = 4001;
            fixture.EntityClassIds.Add(4001);
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

        private static AbilityManager CreateManager(MissionApplication missionManager) =>
            (AbilityManager)typeof(AbilityManager)
                .GetConstructor(
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[]
                    {
                        typeof(IGameUnitOfWorkFactory),
                        typeof(MissionApplication)
                    },
                    null)!
                .Invoke(new object[] { null, missionManager });

        private static void PrepareDirectDamageClient(Client client)
        {
            client.Player.Attributes[Attributes.Health] =
                new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            client.Player.Attributes[Attributes.Armor] =
                new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            client.Player.Attributes[Attributes.Power] =
                new ActorAttributes(Attributes.Power, 100, 100, 100, 0, 0);
        }

        private static ActionLevelInfo LightningInfo(int primaryDamage)
        {
            var info = new ActionLevelInfo
            {
                ActionId = ActionId.AaRecruitLightning,
                Level = 1,
                MaxRange = 20
            };
            info.Properties[AbilityProperty.DamageAmountMin] = primaryDamage;
            info.Properties[AbilityProperty.DamageAmountMax] = primaryDamage;
            info.Properties[AbilityProperty.RadiusAroundTarget] = 1;
            return info;
        }

        private static ActionInfo LightningAction(ActionLevelInfo info)
        {
            var action = new ActionInfo
            {
                ActionId = ActionId.AaRecruitLightning,
                Module = "abilities.lightning"
            };
            action.Levels[info.Level] = info;
            return action;
        }

        private static void InvokeResolveDirectDamage(
            AbilityManager manager,
            MapChannel map,
            Client client,
            ActionInfo actionInfo,
            ActionLevelInfo info,
            ActionData action)
        {
            var method = typeof(AbilityManager).GetMethod(
                "ResolveDirectDamage",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var arguments = new List<object>
            {
                map,
                client,
                client.Player,
                actionInfo,
                info,
                action
            };
            if (method!.GetParameters().Length == 7)
                arguments.Add(null);
            method.Invoke(manager, arguments.ToArray());
        }

        private static Creature AddTarget(
            MissionTestContext context,
            uint dbId,
            Vector3 position)
        {
            context.AddRewardTemplate(1, 4001);
            var target = new Creature
            {
                DbId = dbId,
                EntityClass = (EntityClasses)4001,
                MapContextId = context.Map.MapInfo.MapContextId,
                RuntimeMapChannel = context.Map,
                Position = position,
                State = CharacterState.Normal,
                Faction = Factions.Bane,
                AppearanceData = new(),
                Attributes = new Dictionary<Attributes, ActorAttributes>
                {
                    [Attributes.Health] = new(Attributes.Health, 100, 100, 100, 0, 0),
                    [Attributes.Armor] = new(Attributes.Armor, 0, 0, 0, 0, 0)
                }
            };
            CellManager.Instance.AddToWorld(context.Map, target);
            return target;
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
