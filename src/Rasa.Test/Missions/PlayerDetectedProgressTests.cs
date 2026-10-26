extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Missions.Runtime;
    using Rasa.Structures;
    using Rasa.Structures.Missions;
    using Rasa.Structures.World;
    using PartyScope = Encounters.GroupMissionCreditTests.PartyScope;

    /// <summary>
    /// "Avoid Detection While Uploading Virus" (1187 Silent But Deadly): the player is detected
    /// when a creature of a spawn pool starts to fight them (MissionProgressEventKind.PlayerDetected,
    /// PlayerDetection) - by noticing them, or by being hit first. The objective here counts the
    /// detections (counter 0), so a test can tell one from two.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlayerDetectedProgressTests
    {
        private const uint MissionId = 321;
        private const uint ObjectiveId = 1;
        private const uint Guards = 531225, Others = 531226;
        private const int Cloaked = 987655;

        private Func<MissionApplication> _missions;

        [TestInitialize]
        public void KeepTheServersMissions() => _missions = PlayerDetection.Missions;

        [TestCleanup]
        public void PutBackTheServersMissions() => PlayerDetection.Missions = _missions;

        #region Authoring

        [TestMethod]
        public void ADetectionTriggerIsAnObjectiveOverThatPool()
        {
            var authored = MissionProgressRuleAuthoring.TryBuild(
                new[] { new MissionTriggerDefinition(TriggerRow(Guards)) }, out var rule, out _, out _, out var diagnostic);

            Assert.IsTrue(authored, diagnostic);
            Assert.AreEqual(MissionProgressEventKind.PlayerDetected, rule.Kind);
            CollectionAssert.AreEqual(new[] { Guards }, rule.Subjects.ToArray());
            Assert.AreEqual(MissionProgressEventKind.PlayerDetected, (MissionProgressEventKind)18, "stored as a byte: never reordered");
            Assert.IsTrue(MissionProgressRule.IsShared(MissionProgressEventKind.PlayerDetected));
        }

        [TestMethod]
        public void TheValidatorNamesAPoolTheWorldHasNot()
        {
            CollectionAssert.DoesNotContain(Validate(withPool: true), "missing-spawn-pool");
            CollectionAssert.Contains(Validate(withPool: false), "missing-spawn-pool");
        }

        #endregion

        #region Detection

        [TestMethod]
        public void APoolCreatureNoticingThePlayerDetectsThem()
        {
            using var context = Accepted();
            var guard = Guard(context, Guards, 10);

            Assert.IsTrue(Scan(context, guard));
            Assert.AreEqual(1U, Detections(context.Client));
        }

        [TestMethod]
        public void HittingAGuardFirstGivesThePlayerAway()
        {
            using var context = Accepted();
            var guard = Guard(context, Guards, 40); // out of its aggro range
            guard.Controller.CurrentAction = BehaviorManager.BehaviorActionWander;

            Assert.IsFalse(Scan(context, guard), "not noticed");
            Threat.FromDamage(guard, context.Client.Player, 10);

            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, guard.Controller.CurrentAction);
            Assert.AreEqual(1U, Detections(context.Client));
        }

        [TestMethod]
        public void ACreatureOfNoPoolOrAnotherPoolDoesNotDetect()
        {
            using var context = Accepted();
            var stray = Guard(context, null, 10);
            var other = Guard(context, Others, 12);

            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(stray, context.Client.Player.EntityId));
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(other, context.Client.Player.EntityId));
            Assert.AreEqual(0U, Detections(context.Client));
        }

        [TestMethod]
        public void ACloakedPlayerIsNotDetected()
        {
            using var context = Accepted();
            var guard = Guard(context, Guards, 5);
            context.Client.Player.ActiveEffects[Cloaked] = new GameEffect { EffectId = Cloaked, Hides = true };

            Assert.IsFalse(Scan(context, guard));
            Assert.AreEqual(0U, Detections(context.Client));

            // Turned on them by a hit that does not break the cloak (a damage over time tick): still unseen.
            guard.Controller.CurrentAction = BehaviorManager.BehaviorActionWander;
            Threat.FromDamage(guard, context.Client.Player, 10);
            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, guard.Controller.CurrentAction);
            Assert.AreEqual(0U, Detections(context.Client));
            BehaviorManager.Instance.StopFighting(guard);

            context.Client.Player.ActiveEffects.Remove(Cloaked);

            Assert.IsTrue(Scan(context, guard));
            Assert.AreEqual(1U, Detections(context.Client));
        }

        [TestMethod]
        public void StealthArmorLetsThePlayerCloserUndetected()
        {
            using var context = Accepted();
            var guard = Guard(context, Guards, 12); // within 18 m, not within half of it
            context.Client.Player.DetectionRangePercent = 50;

            Assert.IsFalse(Scan(context, guard));
            Assert.AreEqual(0U, Detections(context.Client));

            guard.Position = new Vector3(8, 0, 0);

            Assert.IsTrue(Scan(context, guard));
            Assert.AreEqual(1U, Detections(context.Client));
        }

        [TestMethod]
        public void OneFightIsOneDetection()
        {
            using var context = Accepted();
            var guard = Guard(context, Guards, 10);

            Assert.IsTrue(Scan(context, guard));
            Assert.IsTrue(Scan(context, guard));
            Threat.FromDamage(guard, context.Client.Player, 10);
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(guard, context.Client.Player.EntityId));
            Assert.AreEqual(1U, Detections(context.Client), "the same fight, the same target");

            // The fight over, a new one is another detection.
            guard.Controller.CurrentAction = BehaviorManager.BehaviorActionIdle;

            Assert.IsTrue(Scan(context, guard));
            Assert.AreEqual(2U, Detections(context.Client));
        }

        [TestMethod]
        public void OneOfASquadSeenIsTheSquadSeen()
        {
            using var context = Accepted(new MissionCreditPolicy(MissionCreditMode.NearbyParty, 100));
            var mate = context.CreateAdditionalClient(2);
            mate.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(mate, context.AddNpc(77).EntityId, MissionId));
            mate.Player.Position = new Vector3(0, 0, 60); // far from the guard, near enough for the squad
            using var party = new PartyScope(context.Client, mate);
            var guard = Guard(context, Guards, 10);

            Assert.IsTrue(Scan(context, guard));
            context.Manager.Credit.Tick(context.Map);

            Assert.AreEqual(1U, Detections(context.Client));
            Assert.AreEqual(1U, Detections(mate));
        }

        #endregion

        private static MissionTestContext Accepted(MissionCreditPolicy creditPolicy = null)
        {
            var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.IncrementCounterOnExactSubject(MissionProgressEventKind.PlayerDetected, Guards, 0, 0, 5),
                counters: new Dictionary<uint, MissionObjectiveCounterDefinition> { [0] = new(0, 0, 5) },
                creditPolicy: creditPolicy);
            PlayerDetection.Missions = () => context.Manager;
            context.Client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            var giver = context.AddNpc(77);

            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, MissionId));
            context.Drain();

            return context;
        }

        private static uint Detections(Client client) =>
            client.Player.Missions[MissionId].Objectives[ObjectiveId].Counters[0];

        /// <summary>A hostile creature of the pool (none for null), <paramref name="x"/> m from the player.</summary>
        private static Creature Guard(MissionTestContext context, uint? pool, float x)
        {
            var guard = context.AddNpc(9000 + (uint)x, position: new Vector3(x, 0, 0));
            guard.Npc = null;
            guard.TargetCategory = TargetCategory.Hostile;
            guard.Level = 1;

            if (pool is uint id)
                guard.SpawnPool = new SpawnPool
                {
                    DbId = id,
                    MapContextId = context.Map.MapInfo.MapContextId,
                    Mode = SpawnPoolManager.ModeAutomatic,
                    RuntimeMapChannel = context.Map,
                    SpawnSlot = new List<SpawnPoolSlot>()
                };

            return guard;
        }

        /// <summary>The aggro scan, as an idle creature's think runs it (CheckForAttackableEntityInRange).</summary>
        private static bool Scan(MissionTestContext context, Creature creature)
        {
            var found = BehaviorManager.FindAttackableInRange(context.Map, creature, creature.AggroRange);

            if (found == 0)
                return false;

            BehaviorManager.Instance.SetActionFighting(creature, found);
            return true;
        }

        private static string[] Validate(bool withPool)
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
                ToState = (byte)MissionObjectiveState.Failed,
                Comment = "Detected"
            });
            var trigger = TriggerRow(Guards);
            trigger.MissionId = 321;
            trigger.ContentRevision = "deployment_11";
            trigger.ObjectiveId = 10;
            trigger.TransitionId = 20;
            fixture.Triggers.Add(trigger);
            if (withPool)
                fixture.SpawnPools.Add(new SpawnPoolEntry { Id = Guards, MapContextId = 1 });

            var snapshot = new MissionContentLoader().Load(fixture.CreateRepository());
            var report = new MissionContentValidator().Validate(snapshot, fixture.CreateWorldUnitOfWork());

            return report.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray();
        }

        private static MissionTriggerEntry TriggerRow(uint pool) =>
            new()
            {
                MissionId = MissionId,
                ContentRevision = "player-detected-test",
                ObjectiveId = ObjectiveId,
                TransitionId = 1,
                TriggerId = 1,
                Requirement = MissionContentRequirement.Required,
                Kind = MissionTriggerKind.ProgressEvent,
                Sequence = 1,
                EventKind = (byte)MissionProgressEventKind.PlayerDetected,
                SubjectId = pool,
                Comment = "Detected by the guards"
            };
    }
}
