using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Encounters
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Missions.Definitions;
    using Rasa.Missions.Runtime;
    using Rasa.Missions.Scenes;
    using Rasa.Structures;
    using Rasa.Structures.Char;

    [TestClass]
    [DoNotParallelize]
    public class GroupMissionCreditTests
    {
        [TestMethod]
        [DataRow(MissionCreditMode.Personal, false)]
        [DataRow(MissionCreditMode.Personal, true)]
        [DataRow(MissionCreditMode.NearbyParty, false)]
        [DataRow(MissionCreditMode.NearbyParty, true)]
        [DataRow(MissionCreditMode.EncounterParticipants, false)]
        [DataRow(MissionCreditMode.EncounterParticipants, true)]
        public void RewardingPublicKillKeepsPersonalPartyAndEncounterCreditAssignmentScoped(
            MissionCreditMode independentPolicy, bool retireMembership)
        {
            using var fixture = new SharedSceneKillFixture(independentPolicy, authoredRewards: true);
            var context = fixture.Context;
            if (retireMembership)
            {
                using var unit = context.CreateChar();
                unit.ExecuteTransaction(() =>
                    unit.CharacterMissions.Runtime.Participants(fixture.Run.RunId)
                        .Single(entry => entry.CharacterId == fixture.Member.Player.Id).Active = false);
            }

            fixture.Creatures.HandleCreatureKill(context.Map, fixture.Enemy, context.Client.Player);
            context.Manager.Credit.Tick(context.Map);

            Assert.AreEqual(1U, context.Client.Player.Missions[321].Objectives[1].Counters[0]);
            Assert.AreEqual(retireMembership ? 0U : 1U, fixture.Member.Player.Missions[321].Objectives[1].Counters[0]);
            Assert.AreEqual(independentPolicy == MissionCreditMode.EncounterParticipants ? 0U : 1U,
                context.Client.Player.Missions[322].Objectives[1].Counters[0]);
            Assert.AreEqual(independentPolicy == MissionCreditMode.NearbyParty ? 1U : 0U,
                fixture.Member.Player.Missions[322].Objectives[1].Counters[0]);
            Assert.IsTrue(context.Client.Player.Experience is >= 90 and <= 110);
            Assert.AreEqual(0U, fixture.Member.Player.Experience);
            var lootId = fixture.Enemy.CorpseLootEntityId;
            var loot = context.Map.LootDispensers[lootId];
            Assert.AreEqual(context.Client.Player.EntityId, loot.Owner);
            Assert.AreEqual(28U, loot.LootItems.Single().ItemTemplateId);
            Assert.AreEqual(2U, loot.LootItems.Single().ItemQuantity);
            var experience = context.Client.Player.Experience;
            using (var database = context.Open())
            {
                var deliveries = database.Set<MissionCreditDeliveryEntry>().ToArray();
                Assert.AreEqual((retireMembership ? 1 : 2) +
                    (independentPolicy == MissionCreditMode.NearbyParty ? 2 :
                        independentPolicy == MissionCreditMode.Personal ? 1 : 0), deliveries.Length);
                foreach (var delivery in deliveries)
                {
                    var client = delivery.CharacterId == 1 ? context.Client : fixture.Member;
                    Assert.AreEqual(client.Player.Missions[delivery.MissionId].AssignmentId, delivery.AssignmentId);
                    Assert.AreEqual(client.Player.Missions[delivery.MissionId].Generation, delivery.AssignmentGeneration);
                    Assert.AreEqual("Applied", delivery.Status);
                }
            }

            fixture.Creatures.HandleCreatureKill(context.Map, fixture.Enemy, context.Client.Player);
            context.Manager.Credit.Tick(context.Map);

            Assert.AreEqual(experience, context.Client.Player.Experience);
            Assert.AreEqual(lootId, fixture.Enemy.CorpseLootEntityId);
            Assert.AreEqual(1U, context.Client.Player.Missions[321].Objectives[1].Counters[0]);
        }

        [TestMethod]
        [DataRow(MissionCreditMode.Personal, false, 1U, 0U)]
        [DataRow(MissionCreditMode.Personal, true, 0U, 1U)]
        [DataRow(MissionCreditMode.NearbyParty, false, 1U, 1U)]
        [DataRow(MissionCreditMode.NearbyParty, true, 1U, 1U)]
        [DataRow(MissionCreditMode.EncounterParticipants, false, 0U, 0U)]
        [DataRow(MissionCreditMode.EncounterParticipants, true, 0U, 0U)]
        public void SceneCreatureDeathScopesEncounterMembershipToEachActiveAssignment(
            MissionCreditMode independentPolicy, bool memberKills, uint ownerCredit, uint memberCredit)
        {
            using var fixture = new SharedSceneKillFixture(independentPolicy);
            var context = fixture.Context;
            var killer = memberKills ? fixture.Member : context.Client;
            Assert.AreNotEqual(killer.Player.Missions[321].AssignmentId, killer.Player.Missions[322].AssignmentId);

            fixture.Creatures.HandleCreatureKill(context.Map, fixture.Enemy, killer.Player);
            context.Manager.Credit.Tick(context.Map);
            fixture.Creatures.HandleCreatureKill(context.Map, fixture.Enemy, killer.Player);
            context.Manager.Credit.Tick(context.Map);

            Assert.AreEqual(CharacterState.Dead, fixture.Enemy.State);
            Assert.AreEqual(1U, context.Client.Player.Missions[321].Objectives[1].Counters[0]);
            Assert.AreEqual(1U, fixture.Member.Player.Missions[321].Objectives[1].Counters[0]);
            Assert.AreEqual(ownerCredit, context.Client.Player.Missions[322].Objectives[1].Counters[0],
                "The owner's independent mission must evaluate its own credit policy, not mission 321's membership.");
            Assert.AreEqual(memberCredit, fixture.Member.Player.Missions[322].Objectives[1].Counters[0],
                "A joined assignment must not suppress independent credit or authorize another encounter-only mission.");
            using var database = context.Open();
            var deliveries = database.Set<MissionCreditDeliveryEntry>().ToArray();
            Assert.AreEqual(2 + ownerCredit + memberCredit, (uint)deliveries.Length);
            foreach (var delivery in deliveries)
            {
                var client = delivery.CharacterId == 1 ? context.Client : fixture.Member;
                Assert.AreEqual(client.Player.Missions[delivery.MissionId].AssignmentId, delivery.AssignmentId);
                Assert.AreEqual(client.Player.Missions[delivery.MissionId].Generation, delivery.AssignmentGeneration);
                Assert.AreEqual("Applied", delivery.Status);
            }
            Assert.AreEqual(1, database.Set<MissionActorStateEntry>().Count(entry => entry.RunId == fixture.Run.RunId));
        }

        [TestMethod]
        [DataRow("assignment", MissionCreditMode.Personal)]
        [DataRow("generation", MissionCreditMode.Personal)]
        [DataRow("inactive", MissionCreditMode.Personal)]
        [DataRow("assignment", MissionCreditMode.NearbyParty)]
        [DataRow("generation", MissionCreditMode.NearbyParty)]
        [DataRow("inactive", MissionCreditMode.NearbyParty)]
        public void StaleEncounterMembershipCannotCreditANewAttemptOrSuppressAnIndependentKill(
            string change, MissionCreditMode independentPolicy)
        {
            using var fixture = new SharedSceneKillFixture(independentPolicy);
            var context = fixture.Context;
            var retired = fixture.Member.Player.Missions[321];
            Assert.IsTrue(context.Manager.TryAbandon(fixture.Member, 321));
            Assert.IsTrue(context.Manager.Sharing.TryShare(context.Client, 321));
            Assert.IsTrue(context.Manager.Sharing.TryAccept(fixture.Member, context.Client.Player.EntityId, 321));
            var current = fixture.Member.Player.Missions[321];
            Assert.AreNotEqual(retired.AssignmentId, current.AssignmentId);
            Assert.AreEqual(retired.Generation + 1, current.Generation);
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    var participant = unit.CharacterMissions.Runtime.Participants(fixture.Run.RunId)
                        .Single(entry => entry.CharacterId == fixture.Member.Player.Id);
                    switch (change)
                    {
                        case "assignment": participant.AssignmentId = retired.AssignmentId; break;
                        case "generation": participant.AssignmentGeneration = retired.Generation; break;
                        case "inactive": participant.Active = false; break;
                        default: Assert.Fail($"Unknown membership change {change}."); break;
                    }
                });

            fixture.Creatures.HandleCreatureKill(context.Map, fixture.Enemy, fixture.Member.Player);
            context.Manager.Credit.Tick(context.Map);

            Assert.AreEqual(1U, context.Client.Player.Missions[321].Objectives[1].Counters[0]);
            Assert.AreEqual(0U, fixture.Member.Player.Missions[321].Objectives[1].Counters[0],
                "Encounter-only credit requires the exact active participant assignment and generation.");
            Assert.AreEqual(independentPolicy == MissionCreditMode.NearbyParty ? 1U : 0U,
                context.Client.Player.Missions[322].Objectives[1].Counters[0]);
            Assert.AreEqual(1U, fixture.Member.Player.Missions[322].Objectives[1].Counters[0],
                "Stale membership in mission 321 does not invalidate the killer's independent mission 322.");
            using var database = context.Open();
            var memberDeliveries = database.Set<MissionCreditDeliveryEntry>()
                .Where(entry => entry.CharacterId == fixture.Member.Player.Id).ToArray();
            Assert.HasCount(1, memberDeliveries);
            Assert.AreEqual(fixture.Member.Player.Missions[322].AssignmentId, memberDeliveries[0].AssignmentId);
            Assert.AreEqual("Applied", memberDeliveries[0].Status);
        }

        [TestMethod]
        public void FrozenSceneDeathCreditStaysWithBothCapturedAttemptsAndOnlyNewDeathsCreditReplacements()
        {
            using var fixture = new SharedSceneKillFixture(MissionCreditMode.NearbyParty);
            var context = fixture.Context;
            var retiredEncounter = fixture.Member.Player.Missions[321];
            var retiredIndependent = fixture.Member.Player.Missions[322];

            fixture.Creatures.HandleCreatureKill(context.Map, fixture.Enemy, context.Client.Player);

            using (var unit = context.CreateChar())
                Assert.HasCount(2, unit.CharacterMissions.Runtime.Deliveries(fixture.Member.Player.Id),
                    "Both actual scene-death deliveries must be frozen before either recipient assignment is replaced.");
            Assert.IsTrue(context.Manager.TryAbandon(fixture.Member, 321));
            Assert.IsTrue(context.Manager.TryAbandon(fixture.Member, 322));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(fixture.Member, fixture.Giver.EntityId, 322));
            Assert.IsTrue(context.Manager.Sharing.TryShare(context.Client, 321));
            Assert.IsTrue(context.Manager.Sharing.TryAccept(fixture.Member, context.Client.Player.EntityId, 321));
            Assert.AreEqual(retiredEncounter.Generation + 1, fixture.Member.Player.Missions[321].Generation);
            Assert.AreEqual(retiredIndependent.Generation + 1, fixture.Member.Player.Missions[322].Generation);
            context.Manager.Credit.Tick(context.Map);

            Assert.AreEqual(0U, fixture.Member.Player.Missions[321].Objectives[1].Counters[0]);
            Assert.AreEqual(0U, fixture.Member.Player.Missions[322].Objectives[1].Counters[0]);
            using (var database = context.Open())
            {
                var expired = database.Set<MissionCreditDeliveryEntry>()
                    .Where(entry => entry.CharacterId == fixture.Member.Player.Id).ToArray();
                Assert.HasCount(2, expired);
                Assert.IsTrue(expired.All(entry => entry.Status == "Expired"));
                CollectionAssert.AreEquivalent(new[] { retiredEncounter.AssignmentId, retiredIndependent.AssignmentId },
                    expired.Select(entry => entry.AssignmentId).ToArray());
            }

            fixture.Creatures.HandleCreatureKill(context.Map, fixture.NextEnemy(), context.Client.Player);
            context.Manager.Credit.Tick(context.Map);

            Assert.AreEqual(1U, fixture.Member.Player.Missions[321].Objectives[1].Counters[0]);
            Assert.AreEqual(1U, fixture.Member.Player.Missions[322].Objectives[1].Counters[0]);
            Assert.AreEqual(2U, context.Client.Player.Missions[321].Objectives[1].Counters[0]);
            Assert.AreEqual(2U, context.Client.Player.Missions[322].Objectives[1].Counters[0]);
            Assert.AreEqual(fixture.Run, context.Manager.PublicActors.Handle(context.Map, 77));
        }

        [TestMethod]
        public void SceneDeathFromAnOldRunGenerationCannotFreezeEitherMissionsCredit()
        {
            using var fixture = new SharedSceneKillFixture(MissionCreditMode.NearbyParty);
            var context = fixture.Context;
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    var run = unit.CharacterMissions.Runtime.Scene(fixture.Run.RunId);
                    run.Generation++;
                    run.Version++;
                });

            Assert.ThrowsExactly<GameplayRejectionException>(() =>
                fixture.Creatures.HandleCreatureKill(context.Map, fixture.Enemy, context.Client.Player));

            foreach (var client in new[] { context.Client, fixture.Member })
                foreach (var mission in client.Player.Missions.Values)
                    Assert.AreEqual(0U, mission.Objectives[1].Counters[0]);
            using var database = context.Open();
            Assert.IsEmpty(database.Set<MissionCreditDeliveryEntry>().ToArray());
            Assert.IsEmpty(database.Set<MissionOutcomeEntry>().ToArray());
            Assert.IsEmpty(database.Set<MissionActorStateEntry>().ToArray());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ObjectiveEligibilityIsFrozenAtTheEventRatherThanReevaluatedAtDelivery(bool eligibleAtEvent)
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.IncrementCounterOnExactSubject(MissionProgressEventKind.CreatureKilled, 55, 0, 0, 5),
                counters: new Dictionary<uint, MissionObjectiveCounterDefinition> { [0] = new(0, 0, 5) },
                creditPolicy: new MissionCreditPolicy(MissionCreditMode.NearbyParty, 20),
                objectiveRequirement: new CustomRequirement("example.even-level"));
            var member = context.CreateAdditionalClient(2);
            var giver = context.AddNpc(77);
            foreach (var client in new[] { context.Client, member })
                Assert.IsTrue(context.Manager.AcceptOfferedMission(client, giver.EntityId, 321));
            SetLevel(context, context.Client, 2);
            SetLevel(context, member, eligibleAtEvent ? (byte)2 : (byte)1);
            member.Player.Level = 2;
            using var party = new PartyScope(context.Client, member);
            var eventId = Guid.NewGuid().ToString("N");

            Assert.IsTrue(context.Manager.Credit.Record(context.Client, MissionProgressEvent.Creature(55), Vector3.Zero, eventId));
            SetLevel(context, member, eligibleAtEvent ? (byte)1 : (byte)2);
            context.Manager.Credit.Tick(context.Map);
            context.Manager.Credit.Record(context.Client, MissionProgressEvent.Creature(55), Vector3.Zero, eventId);
            context.Manager.Credit.Deliver(member);

            Assert.AreEqual(1U, context.Client.Player.Missions[321].Objectives[1].Counters[0]);
            Assert.AreEqual(eligibleAtEvent ? 1U : 0U, member.Player.Missions[321].Objectives[1].Counters[0]);
            using var database = context.Open();
            Assert.AreEqual(eligibleAtEvent ? 1 : 0,
                database.Set<MissionCreditDeliveryEntry>().Count(delivery => delivery.CharacterId == 2));
        }

        private static void SetLevel(MissionTestContext context, Client client, byte level)
        {
            using var unit = context.CreateChar();
            unit.Characters.UpdateCharacterProgression(client.Player.Id, client.Player.Experience, level);
            client.Player.Level = level;
        }

        [TestMethod]
        [DataRow("replacement")]
        [DataRow("generation")]
        [DataRow("completed")]
        public void FrozenCreditCannotEscapeItsAssignmentGenerationOrCurrentObjective(string change)
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.IncrementCounterOnExactSubject(MissionProgressEventKind.CreatureKilled, 55, 0, 0, 5),
                counters: new Dictionary<uint, MissionObjectiveCounterDefinition> { [0] = new(0, 0, 5) },
                creditPolicy: new MissionCreditPolicy(MissionCreditMode.NearbyParty, 20));
            var member = context.CreateAdditionalClient(2);
            var giver = context.AddNpc(77);
            foreach (var client in new[] { context.Client, member })
                Assert.IsTrue(context.Manager.AcceptOfferedMission(client, giver.EntityId, 321));
            using var party = new PartyScope(context.Client, member);
            Assert.IsTrue(context.Manager.Credit.Record(context.Client, MissionProgressEvent.Creature(55), Vector3.Zero));
            if (change == "replacement")
            {
                Assert.IsTrue(context.Manager.TryAbandon(member, 321));
                Assert.IsTrue(context.Manager.AcceptOfferedMission(member, giver.EntityId, 321));
            }
            else if (change == "generation")
            {
                using var unit = context.CreateChar();
                unit.ExecuteTransaction(() => unit.CharacterMissions.GetByCharacterAndMission(2, 321).Generation++);
            }
            else
                for (var count = 0; count < 5; count++)
                    Assert.IsTrue(context.Manager.RecordProgress(member, MissionProgressEvent.Creature(55)));

            context.Manager.Credit.Deliver(member);

            Assert.AreEqual(change == "completed" ? 5U : 0U, member.Player.Missions[321].Objectives[1].Counters[0]);
            using var database = context.Open();
            Assert.AreEqual("Expired", database.Set<MissionCreditDeliveryEntry>().Single(entry => entry.CharacterId == 2).Status);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void IdenticalSharedSignalsFromIndependentRunsOrGenerationsProduceIndependentDeliveries(bool newGeneration)
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.IncrementCounterOnExactSubject(MissionProgressEventKind.ScenarioEvent, 55, 0, 0, 5),
                counters: new Dictionary<uint, MissionObjectiveCounterDefinition> { [0] = new(0, 0, 5) },
                creditPolicy: new MissionCreditPolicy(MissionCreditMode.NearbyParty, 20));
            var member = context.CreateAdditionalClient(2);
            var giver = context.AddNpc(77);
            foreach (var client in new[] { context.Client, member })
                Assert.IsTrue(context.Manager.AcceptOfferedMission(client, giver.EntityId, 321));
            using var party = new PartyScope(context.Client, member);
            var bindings = new SceneBindings("test", new Dictionary<string, SceneActorDefinition>(),
                new Dictionary<string, SceneRoute>(), new Dictionary<uint, SceneSequence>
                {
                    [0] = new(signals: new[] { new SceneMissionSignal(321, 9, 55) })
                });
            var scenes = context.Manager.Scenes;
            var first = scenes.Start(context.Client, "data.sequence", bindings, 321);
            context.Manager.Credit.Tick(context.Map);
            string second;
            if (newGeneration)
            {
                using (var unit = context.CreateChar())
                    unit.ExecuteTransaction(() =>
                    {
                        var run = unit.CharacterMissions.Runtime.Scene(first);
                        run.Generation++;
                        run.Version++;
                        run.Checkpoint = "{}";
                    });
                scenes.Attach(context.Client, first, bindings);
                Assert.IsTrue(scenes.Submit(first, new SceneObservation(SceneEventKind.Started, 2)));
                second = first;
            }
            else
                second = scenes.Start(member, "data.sequence", bindings, 321);
            context.Manager.Credit.Tick(context.Map);
            scenes.Submit(second, new SceneObservation(SceneEventKind.Started, newGeneration ? 2U : 1U));
            context.Manager.Credit.Tick(context.Map);

            Assert.AreEqual(2U, context.Client.Player.Missions[321].Objectives[1].Counters[0]);
            Assert.AreEqual(2U, member.Player.Missions[321].Objectives[1].Counters[0]);
            using var database = context.Open();
            var outcomes = database.Set<MissionOutcomeEntry>().ToArray();
            Assert.AreEqual(2, outcomes.Length);
            Assert.AreEqual(2, outcomes.Select(outcome => outcome.EventId).Distinct().Count());
            Assert.AreEqual(newGeneration ? 1 : 2, outcomes.Select(outcome => outcome.RunId).Distinct().Count());
            Assert.AreEqual(newGeneration ? 2 : 1, outcomes.Select(outcome => outcome.Generation).Distinct().Count());
            Assert.AreEqual(4, database.Set<MissionCreditDeliveryEntry>().Count(delivery => delivery.Status == "Applied"));
            Assert.AreEqual(4, database.Set<MissionReceiptEntry>().Count(receipt => receipt.Kind == "Credit"));
        }

        [TestMethod]
        public void OnlyCurrentlyEligibleNearbyPartyAssignmentsReceiveFutureKillCredit()
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled, 55),
                creditPolicy: new MissionCreditPolicy(MissionCreditMode.NearbyParty, 20));
            var eligible = context.CreateAdditionalClient(2);
            var unaccepted = context.CreateAdditionalClient(3);
            var far = context.CreateAdditionalClient(4);
            var stranger = context.CreateAdditionalClient(5);
            var giver = context.AddNpc(77);
            foreach (var client in new[] { context.Client, eligible, far, stranger })
                Assert.IsTrue(context.Manager.AcceptOfferedMission(client, giver.EntityId, 321));
            far.Player.Position = new Vector3(100, 0, 0);
            using var party = new PartyScope(context.Client, eligible, unaccepted, far);

            Assert.IsTrue(context.Manager.Credit.Record(context.Client, MissionProgressEvent.Creature(55), Vector3.Zero));
            context.Manager.Credit.Tick(context.Map);

            Assert.AreEqual(MissionObjectiveState.Completed, eligible.Player.Missions[321].Objectives[1].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, far.Player.Missions[321].Objectives[1].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, stranger.Player.Missions[321].Objectives[1].State);
            Assert.IsFalse(unaccepted.Player.Missions.ContainsKey(321));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(unaccepted, giver.EntityId, 321));
            context.Manager.Credit.Tick(context.Map);
            Assert.AreEqual(MissionObjectiveState.Incomplete, unaccepted.Player.Missions[321].Objectives[1].State);
        }

        [TestMethod]
        public void PartialDeliveryRetriesOnlyFrozenAssignmentsAndDoesNotRepeatSuccessfulCredit()
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.IncrementCounterOnExactSubject(MissionProgressEventKind.CreatureKilled, 55, 0, 0, 5),
                counters: new Dictionary<uint, MissionObjectiveCounterDefinition> { [0] = new(0, 0, 5) },
                creditPolicy: new MissionCreditPolicy(MissionCreditMode.NearbyParty, 20),
                objectiveRequirement: new CustomRequirement("example.even-level"));
            var member = context.CreateAdditionalClient(2);
            var later = context.CreateAdditionalClient(3);
            var giver = context.AddNpc(77);
            foreach (var client in new[] { context.Client, member })
            {
                Assert.IsTrue(context.Manager.AcceptOfferedMission(client, giver.EntityId, 321));
                SetLevel(context, client, 2);
            }
            using var party = new PartyScope(context.Client, member, later);
            context.BeforeSave = database =>
            {
                if (database.ChangeTracker.Entries<CharacterMissionObjectiveCounterEntry>().Any(entry =>
                    entry.State == EntityState.Modified && entry.Entity.CharacterId == 2))
                    throw new DbUpdateException("Injected recipient-only failure.");
            };
            var eventId = Guid.NewGuid().ToString("N");
            context.Manager.Credit.Record(context.Client, MissionProgressEvent.Creature(55), Vector3.Zero, eventId);
            context.Manager.Credit.Tick(context.Map);
            Assert.AreEqual(1U, context.Client.Player.Missions[321].Objectives[1].Counters[0]);
            Assert.AreEqual(0U, member.Player.Missions[321].Objectives[1].Counters[0]);
            context.BeforeSave = null;
            SetLevel(context, member, 1);
            SetLevel(context, later, 2);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(later, giver.EntityId, 321));
            context.Manager.Credit.Deliver(member);
            context.Manager.Credit.Record(context.Client, MissionProgressEvent.Creature(55), Vector3.Zero, eventId);

            Assert.AreEqual(1U, context.Client.Player.Missions[321].Objectives[1].Counters[0]);
            Assert.AreEqual(1U, member.Player.Missions[321].Objectives[1].Counters[0]);
            Assert.AreEqual(0U, later.Player.Missions[321].Objectives[1].Counters[0]);
        }

        private sealed class SharedSceneKillFixture : IDisposable
        {
            private readonly PublicSceneActorPolicyTests.Fixture _world;
            private readonly PartyScope _party;
            internal MissionTestContext Context => _world.Context;
            internal CreatureManager Creatures => _world.Creatures;
            internal Client Member { get; }
            internal ActorHandle Run { get; }
            internal Creature Enemy { get; }
            internal Creature Giver { get; }

            internal SharedSceneKillFixture(MissionCreditMode independentPolicy, bool authoredRewards = false)
            {
                _world = new PublicSceneActorPolicyTests.Fixture(definitions: new Dictionary<uint, Mission>
                {
                    [321] = KillMission(321, 77, MissionCreditMode.EncounterParticipants),
                    [322] = KillMission(322, 78, independentPolicy)
                });
                Member = Context.CreateAdditionalClient(2);
                Giver = Context.AddNpc(78);
                foreach (var client in new[] { Context.Client, Member })
                    Assert.IsTrue(Context.Manager.AcceptOfferedMission(client, Giver.EntityId, 322));
                var guide = _world.PublicActor();
                guide.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
                var policy = authoredRewards ? new ActorGameplayPolicy
                {
                    RewardScenarioKills = true,
                    Loot = new AuthoredLootProfile(new[] { new LootDrop(28, 100, 2, 2) })
                } : null;
                Context.Manager.Scenes.Bind(321, "data.sequence", new SceneBindings("unversioned",
                    new Dictionary<string, SceneActorDefinition>
                    {
                        ["guide"] = new("guide", SceneActorKind.PublicSpawn, 77),
                        ["enemy"] = new("enemy", SceneActorKind.Creature, 510210, new ScenePosition(0, 0, 0),
                            GameplayPolicy: policy),
                        ["next-enemy"] = new("next-enemy", SceneActorKind.Creature, 510210, new ScenePosition(0, 0, 0),
                            GameplayPolicy: policy)
                    }, new Dictionary<string, SceneRoute>(), new Dictionary<uint, SceneSequence>
                    {
                        [0] = new(worldIntents: new WorldIntent[]
                        {
                            new EnsureActorIntent("ensure-guide", "guide"),
                            new EnsureActorIntent("ensure-enemy", "enemy")
                        }),
                        [1] = new(worldIntents: new[] { new EnsureActorIntent("ensure-next-enemy", "next-enemy") })
                    }));
                Context.Manager.PublicActors.Bind(new PublicEncounterBinding(321, 77, "guide", "data.sequence",
                    AllowPartyJoin: true));
                _party = new PartyScope(Context.Client, Member);
                Assert.IsTrue(Context.Manager.AcceptOfferedMission(Context.Client, guide.EntityId, 321));
                Assert.IsTrue(Context.Manager.Sharing.TryShare(Context.Client, 321));
                Assert.IsTrue(Context.Manager.Sharing.TryAccept(Member, Context.Client.Player.EntityId, 321));
                Run = Context.Manager.PublicActors.Handle(Context.Map, 77);
                Enemy = _world.Actor(Run.RunId, "enemy");
            }

            internal Creature NextEnemy()
            {
                Assert.IsTrue(Context.Manager.Scenes.Execute(Context.Client, 321, 1));
                return _world.Actor(Run.RunId, "next-enemy");
            }

            private static Mission KillMission(uint missionId, uint giverId, MissionCreditMode policy) =>
                new(missionId, "Independent kill assignment", missionId, giverId, 88, 1, 1, 2, true, false,
                    new[]
                    {
                        new MissionObjectiveDefinition(1, 1001, 1002, new uint?[] { 1003, null, null }, 0,
                            MissionObjectiveState.Incomplete, true,
                            new Dictionary<uint, MissionObjectiveCounterDefinition> { [0] = new(0, 0, 5) },
                            null, Array.Empty<MissionObjectiveConversation>(), Array.Empty<uint>(), Array.Empty<uint>(),
                            Array.Empty<MissionIndicator>(),
                            MissionProgressRule.IncrementCounterOnExactSubject(MissionProgressEventKind.CreatureKilled, 510210, 0, 0, 5),
                            creditPolicy: new MissionCreditPolicy(policy, policy == MissionCreditMode.Personal ? 0 : 20))
                    }, true);

            public void Dispose()
            {
                _party.Dispose();
                _world.Dispose();
            }
        }

        internal sealed class PartyScope : IDisposable
        {
            private readonly uint _id;
            private readonly Client[] _clients;
            internal PartyScope(params Client[] clients)
            {
                _clients = clients; _id = PartyManager.Instance.GetPartyId;
                PartyManager.Instance.Parties[_id] = new Party(_id, clients[0].AccountEntry.Id,
                    clients.Select(client => new PartyMember(client)).ToList());
                foreach (var client in clients)
                    client.Player.PartyId = _id;
            }
            public void Dispose()
            {
                foreach (var client in _clients)
                    client.Player.PartyId = 0;
                PartyManager.Instance.Parties.Remove(_id);
                PartyManager.Instance.FreePartyId(_id);
            }
        }
    }
}
