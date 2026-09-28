extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.ClientMethod.Server;
    using Rasa.Packets.Manifestation.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Mission.Server;
    using Rasa.Structures;
    using Rasa.Structures.Char;

    [TestClass]
    [DoNotParallelize]
    public class MissionProgressTests
    {
        [TestMethod]
        public void ExactSubjectCompletesOnceAfterCommitAndReconnectsCompleted()
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnExactSubject(
                    MissionProgressEventKind.CreatureKilled, 82));
            SeedActive(context);
            context.BeforeSave = _ =>
            {
                Assert.AreEqual(MissionObjectiveState.Incomplete,
                    context.Client.Player.Missions[321].Objectives[1].State);
                Assert.AreEqual(0, context.Drain().Count);
            };

            Assert.IsFalse(context.Manager.RecordProgress(
                context.Client, MissionProgressEvent.Creature(81)));
            Assert.IsTrue(context.Manager.RecordProgress(
                context.Client, MissionProgressEvent.Creature(82)));
            Assert.IsFalse(context.Manager.RecordProgress(
                context.Client, MissionProgressEvent.Creature(82)));

            Assert.AreEqual(MissionObjectiveState.Completed,
                context.Client.Player.Missions[321].Objectives[1].State);
            CollectionAssert.AreEqual(
                new[] { typeof(ObjectiveCompletedPacket), typeof(MissionCompleteablePacket) },
                context.Drain().Select(packet => packet.GetType()).ToArray());
            context.ReloadPlayerMissions();
            Assert.AreEqual(MissionObjectiveState.Completed,
                context.Client.Player.Missions[321].Objectives[1].State);
            Assert.IsTrue(context.Client.Player.Missions[321].Completeable);
        }

        [TestMethod]
        public void DistinctSubjectsUsePersistedAcquisitionCollectionsWithoutCounters()
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteWhenAllDistinctSubjectsObserved(
                    MissionProgressEventKind.WaypointAcquired,
                    new HashSet<uint> { 49, 50, 51 }));
            SeedActive(context);
            context.Client.Player.GainedWaypoints.AddRange(new[]
            {
                new CharacterTeleporterEntry(1, 49, (byte)WaypointType.Waypoint),
                new CharacterTeleporterEntry(1, 50, (byte)WaypointType.Waypoint)
            });
            using (var unit = context.CreateChar())
            {
                unit.CharacterTeleporters.Add(
                    new CharacterTeleporterEntry(1, 49, (byte)WaypointType.Waypoint));
                unit.CharacterTeleporters.Add(
                    new CharacterTeleporterEntry(1, 50, (byte)WaypointType.Waypoint));
            }

            Assert.IsFalse(context.Manager.RecordProgress(
                context.Client, MissionProgressEvent.Waypoint(50)));
            var finalWaypoint = new CharacterTeleporterEntry(
                1, 51, (byte)WaypointType.Waypoint);
            using (var unit = context.CreateChar())
                unit.CharacterTeleporters.Add(finalWaypoint);
            context.Client.Player.GainedWaypoints.Add(finalWaypoint);
            Assert.IsTrue(context.Manager.RecordProgress(
                context.Client, MissionProgressEvent.Waypoint(51)));

            Assert.AreEqual(0, context.Client.Player.Missions[321].Objectives[1].Counters.Count);
            Assert.AreEqual(0, context.Drain().OfType<UpdateObjectiveCounterPacket>().Count());
        }

        [TestMethod]
        public void ExactCounterPublishesCounterBeforeCompletionAndUsesCheckedMonotonicValues()
        {
            var counters = new Dictionary<uint, MissionObjectiveCounterDefinition>
            {
                [0] = new MissionObjectiveCounterDefinition(0, 2, 4)
            };
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.IncrementCounterOnExactSubject(
                    MissionProgressEventKind.CreatureKilled, 75, 0, 2, 4),
                counters: counters);
            SeedActive(context);

            Assert.IsTrue(context.Manager.RecordProgress(
                context.Client, MissionProgressEvent.Creature(75)));
            Assert.AreEqual(3U, context.Client.Player.Missions[321].Objectives[1].Counters[0]);
            CollectionAssert.AreEqual(
                new[] { typeof(UpdateObjectiveCounterPacket) },
                context.Drain().Select(packet => packet.GetType()).ToArray());

            Assert.IsTrue(context.Manager.RecordProgress(
                context.Client, MissionProgressEvent.Creature(75)));
            CollectionAssert.AreEqual(
                new[]
                {
                    typeof(UpdateObjectiveCounterPacket),
                    typeof(ObjectiveCompletedPacket),
                    typeof(MissionCompleteablePacket)
                },
                context.Drain().Select(packet => packet.GetType()).ToArray());
        }

        [TestMethod]
        public void MultiMatchEventPublishesAllCountersThenObjectivesThenCompletableMissions()
        {
            var mission321 = CreateMissionWithObjectives(
                321,
                CreateProgressObjective(
                    9,
                    MissionProgressRule.IncrementCounterOnExactSubject(
                        MissionProgressEventKind.CreatureKilled, 75, 0, 3, 4),
                    new Dictionary<uint, MissionObjectiveCounterDefinition>
                    {
                        [0] = new MissionObjectiveCounterDefinition(0, 3, 4)
                    }),
                CreateProgressObjective(
                    2,
                    MissionProgressRule.CompleteOnExactSubject(
                        MissionProgressEventKind.CreatureKilled, 75)));
            var mission320 = CreateMissionWithObjectives(
                320,
                CreateProgressObjective(
                    5,
                    MissionProgressRule.IncrementCounterOnExactSubject(
                        MissionProgressEventKind.CreatureKilled, 75, 0, 6, 7),
                    new Dictionary<uint, MissionObjectiveCounterDefinition>
                    {
                        [0] = new MissionObjectiveCounterDefinition(0, 6, 7)
                    }),
                CreateProgressObjective(
                    1,
                    MissionProgressRule.CompleteOnExactSubject(
                        MissionProgressEventKind.CreatureKilled, 75)));
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>
                {
                    [321] = mission321,
                    [320] = mission320
                });
            SeedActive(context, 321);
            context.SeedMission(1, 320, (uint)MissionState.Active, false);
            context.ReloadPlayerMissions();
            context.Drain();

            Assert.IsTrue(context.Manager.RecordProgress(
                context.Client, MissionProgressEvent.Creature(75)));

            CollectionAssert.AreEqual(
                new[]
                {
                    "counter:320:5:0:7",
                    "counter:321:9:0:4",
                    "objective:320:1",
                    "objective:320:5",
                    "objective:321:2",
                    "objective:321:9",
                    "completable:320",
                    "completable:321"
                },
                context.Drain().Select(DescribeProgressPacket).ToArray());
            Assert.IsTrue(context.Client.Player.Missions[320].Completeable);
            Assert.IsTrue(context.Client.Player.Missions[321].Completeable);
        }

        [TestMethod]
        public void StaleAndOverflowingDurableProgressAreNoOps()
        {
            var counters = new Dictionary<uint, MissionObjectiveCounterDefinition>
            {
                [0] = new MissionObjectiveCounterDefinition(0, uint.MaxValue, uint.MaxValue)
            };
            using var overflow = MissionTestContext.WithProgressMission(
                MissionProgressRule.IncrementCounterOnExactSubject(
                    MissionProgressEventKind.CreatureKilled,
                    75, 0, uint.MaxValue, uint.MaxValue),
                counters: counters);
            SeedActive(overflow);
            Assert.IsFalse(overflow.Manager.RecordProgress(
                overflow.Client, MissionProgressEvent.Creature(75)));
            Assert.AreEqual(0, overflow.Drain().Count);

            using var stale = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnExactSubject(
                    MissionProgressEventKind.CreatureKilled, 82));
            SeedActive(stale);
            using (var unit = stale.CreateChar())
                unit.CharacterMissionProgress.SetObjectiveState(
                    1, 321, 1,
                    (byte)MissionObjectiveState.Incomplete,
                    (byte)MissionObjectiveState.Completed);
            Assert.IsFalse(stale.Manager.RecordProgress(
                stale.Client, MissionProgressEvent.Creature(82)));
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                stale.Client.Player.Missions[321].Objectives[1].State);
            Assert.AreEqual(0, stale.Drain().Count);
        }

        [TestMethod]
        public void PersistenceFailuresRollbackProgressButProgrammingErrorsRetainIdentity()
        {
            using var expectedFailure = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnExactSubject(
                    MissionProgressEventKind.CreatureKilled, 82));
            SeedActive(expectedFailure);
            expectedFailure.AfterSave = _ => throw new DbUpdateException("Injected progress failure.");
            Assert.IsFalse(expectedFailure.Manager.RecordProgress(
                expectedFailure.Client, MissionProgressEvent.Creature(82)));
            Assert.AreEqual((byte)MissionObjectiveState.Incomplete,
                expectedFailure.ReadProgress(321).Missions[321].Objectives[1].State);
            Assert.AreEqual(0, expectedFailure.Drain().Count);

            using var programming = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnExactSubject(
                    MissionProgressEventKind.CreatureKilled, 82));
            SeedActive(programming);
            var expected = new InvalidOperationException("Injected programming failure.");
            programming.AfterSave = _ => ThrowAtPersistenceBoundary(expected);
            var actual = Assert.ThrowsExactly<InvalidOperationException>(() =>
                programming.Manager.RecordProgress(
                    programming.Client, MissionProgressEvent.Creature(82)));
            Assert.AreSame(expected, actual);
            StringAssert.Contains(actual.StackTrace, nameof(ThrowAtPersistenceBoundary));
            Assert.AreEqual(0, programming.Drain().Count);
        }

        [TestMethod]
        public void CompetingClientsPersistAndPublishOneProgressDelta()
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnExactSubject(
                    MissionProgressEventKind.CreatureKilled, 82));
            SeedActive(context);
            var competitor = context.CreateCompetingClient();
            using var start = new ManualResetEventSlim();
            var results = Task.WhenAll(
                Task.Run(() =>
                {
                    start.Wait();
                    return context.Manager.RecordProgress(
                        context.Client, MissionProgressEvent.Creature(82));
                }),
                Task.Run(() =>
                {
                    start.Wait();
                    return context.Manager.RecordProgress(
                        competitor, MissionProgressEvent.Creature(82));
                }));
            start.Set();

            Assert.AreEqual(1, results.GetAwaiter().GetResult().Count(result => result));
            Assert.AreEqual(1, context.Drain()
                .Concat(MissionTestContext.Drain(competitor))
                .OfType<ObjectiveCompletedPacket>().Count());
        }

        [TestMethod]
        public void WaypointAndLogosAdaptersRunOnlyAfterNewDurableGrantPackets()
        {
            using var waypoint = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteWhenAllDistinctSubjectsObserved(
                    MissionProgressEventKind.WaypointAcquired,
                    new HashSet<uint> { 49 }));
            SeedActive(waypoint);
            var character = new CharacterManager(waypoint, waypoint.Manager);
            var objects = new DynamicObjectManager(
                waypoint,
                updateCharacter: (client, update, value) =>
                    character.UpdateCharacter(client, update, value),
                missionManager: waypoint.Manager);

            objects.CheckPlayerWaypoint(
                waypoint.Client, new WaypointInfo(49, false, WaypointType.Waypoint));
            objects.CheckPlayerWaypoint(
                waypoint.Client, new WaypointInfo(49, false, WaypointType.Waypoint));

            CollectionAssert.AreEqual(
                new[] { typeof(WaypointGainedPacket), typeof(ObjectiveCompletedPacket), typeof(MissionCompleteablePacket) },
                waypoint.Drain().Select(packet => packet.GetType()).ToArray());

            using var logos = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnExactSubject(
                    MissionProgressEventKind.LogosAcquired, 10));
            SeedActive(logos);
            var logosCharacter = new CharacterManager(logos, logos.Manager);
            Assert.IsTrue(logosCharacter.TryAddLogos(logos.Client, 10));
            Assert.IsFalse(logosCharacter.TryAddLogos(logos.Client, 10));
            CollectionAssert.AreEqual(
                new[] { typeof(LogosStoneAddedPacket), typeof(ObjectiveCompletedPacket), typeof(MissionCompleteablePacket) },
                logos.Drain().Select(packet => packet.GetType()).ToArray());
        }

        [TestMethod]
        public void ProgressFailurePreservesPersistedWaypointAndLogosGrants()
        {
            using var waypoint = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteWhenAllDistinctSubjectsObserved(
                    MissionProgressEventKind.WaypointAcquired,
                    new HashSet<uint> { 49 }));
            SeedActive(waypoint);
            waypoint.BeforeSave = database =>
            {
                if (database.ChangeTracker.Entries<CharacterMissionObjectiveEntry>()
                    .Any(entry => entry.State == EntityState.Modified))
                    throw new DbUpdateException("Injected waypoint progress failure.");
            };
            var character = new CharacterManager(waypoint, waypoint.Manager);
            var objects = new DynamicObjectManager(
                waypoint,
                updateCharacter: (client, update, value) =>
                    character.UpdateCharacter(client, update, value),
                missionManager: waypoint.Manager);

            objects.CheckPlayerWaypoint(
                waypoint.Client, new WaypointInfo(49, false, WaypointType.Waypoint));

            using (var unit = waypoint.CreateChar())
                Assert.AreEqual(1, unit.CharacterTeleporters.Get(1).Count);
            Assert.AreEqual(1, waypoint.Client.Player.GainedWaypoints.Count);
            CollectionAssert.AreEqual(
                new[] { typeof(WaypointGainedPacket) },
                waypoint.Drain().Select(packet => packet.GetType()).ToArray());

            using var logos = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnExactSubject(
                    MissionProgressEventKind.LogosAcquired, 10));
            SeedActive(logos);
            logos.BeforeSave = database =>
            {
                if (database.ChangeTracker.Entries<CharacterMissionObjectiveEntry>()
                    .Any(entry => entry.State == EntityState.Modified))
                    throw new DbUpdateException("Injected Logos progress failure.");
            };
            var logosCharacter = new CharacterManager(logos, logos.Manager);

            Assert.IsTrue(logosCharacter.TryAddLogos(logos.Client, 10));

            using (var unit = logos.CreateChar())
                CollectionAssert.AreEqual(
                    new uint[] { 10 },
                    unit.CharacterLogoses.GetLogos(1));
            CollectionAssert.AreEqual(new uint[] { 10 }, logos.Client.Player.Logos);
            CollectionAssert.AreEqual(
                new[] { typeof(LogosStoneAddedPacket) },
                logos.Drain().Select(packet => packet.GetType()).ToArray());
        }

        [TestMethod]
        public void CreatureAdapterRunsOnceAfterExperienceAndLootForAuthoritativeKiller()
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnExactSubject(
                    MissionProgressEventKind.CreatureKilled, 82));
            SeedActive(context);
            var creature = AddCreature(context, 82);
            var manager = new CreatureManager(
                context,
                new ManifestationManager(context),
                context.Manager);
            try
            {
                manager.HandleCreatureKill(context.Map, creature, context.Client.Player);
                manager.HandleCreatureKill(context.Map, creature, context.Client.Player);

                Assert.AreEqual(1, context.Map.LootDispensers.Count);
                var packets = context.Drain();
                Assert.AreEqual(1, packets.OfType<ExperienceChangedPacket>().Count());
                Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count());
                Assert.IsTrue(
                    packets.FindIndex(packet => packet is ExperienceChangedPacket) <
                    packets.FindIndex(packet => packet is ObjectiveCompletedPacket));
            }
            finally
            {
                CellManager.Instance.RemoveCreatureFromWorld(context.Map, creature);
            }
        }

        [TestMethod]
        public void CreatureProgressFailurePreservesExperienceAndLoot()
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnExactSubject(
                    MissionProgressEventKind.CreatureKilled, 82));
            SeedActive(context);
            var before = context.ReadRewardTotals();
            context.BeforeSave = database =>
            {
                if (database.ChangeTracker.Entries<CharacterMissionObjectiveEntry>()
                    .Any(entry => entry.State == EntityState.Modified))
                    throw new DbUpdateException("Injected creature progress failure.");
            };
            var creature = AddCreature(context, 82);
            var manager = new CreatureManager(
                context,
                new ManifestationManager(context),
                context.Manager);
            try
            {
                manager.HandleCreatureKill(context.Map, creature, context.Client.Player);

                var after = context.ReadRewardTotals();
                Assert.IsTrue(after.Experience > before.Experience);
                Assert.AreEqual(1, context.Map.LootDispensers.Count);
                Assert.AreEqual(MissionObjectiveState.Incomplete,
                    context.Client.Player.Missions[321].Objectives[1].State);
                var packets = context.Drain();
                Assert.AreEqual(1, packets.OfType<ExperienceChangedPacket>().Count());
                Assert.AreEqual(0, packets.OfType<ObjectiveCompletedPacket>().Count());
            }
            finally
            {
                CellManager.Instance.RemoveCreatureFromWorld(context.Map, creature);
            }
        }

        [TestMethod]
        public void MissionCompletionAdapterFailureRollsBackBeforeRewardAndCanRetry()
        {
            using var context = CreateCompletionAdapterContext();
            var receiver = context.AddNpc(88);
            var before = context.ReadRewardTotals();
            var baselineSaves = context.SaveAttempts;
            context.BeforeSave = database =>
            {
                if (database.ChangeTracker.Entries<CharacterMissionObjectiveEntry>()
                    .Any(entry => entry.Entity.MissionId == 430 &&
                        entry.State == EntityState.Modified))
                    throw new DbUpdateException("Injected post-reward progress failure.");
            };

            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, receiver.EntityId, 429, null, null));
            Assert.AreEqual(before, context.ReadRewardTotals());
            Assert.AreEqual(MissionState.Active,
                context.Client.Player.Missions[429].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                context.Client.Player.Missions[430].Objectives[1].State);
            Assert.IsTrue(context.SaveAttempts > baselineSaves);
            Assert.AreEqual(0, context.Drain().Count);

            context.BeforeSave = null;
            Assert.IsTrue(context.Manager.CompleteOfferedMission(
                context.Client, receiver.EntityId, 429, null, null));

            var after = context.ReadRewardTotals();
            Assert.AreEqual(before.Experience + 100, after.Experience);
            Assert.AreEqual(MissionState.Completed,
                context.Client.Player.Missions[429].State);
            Assert.AreEqual(MissionObjectiveState.Completed,
                context.Client.Player.Missions[430].Objectives[1].State);
            var packets = context.Drain();
            Assert.AreEqual(1, packets.OfType<MissionRewardedPacket>().Count());
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count());
        }

        [TestMethod]
        public void MissionCompletionAdapterPublishesTargetAfterRewardWithoutSelfRecursion()
        {
            using var context = CreateCompletionAdapterContext();
            var receiver = context.AddNpc(88);

            Assert.IsTrue(context.Manager.CompleteOfferedMission(
                context.Client, receiver.EntityId, 429, null, null));

            var packets = context.Drain();
            Assert.IsTrue(
                packets.FindIndex(packet => packet is ObjectiveCompletedPacket) <
                packets.FindIndex(packet => packet is MissionRewardedPacket));
            Assert.AreEqual(MissionState.Completed, context.Client.Player.Missions[429].State);
            Assert.AreEqual(MissionObjectiveState.Completed,
                context.Client.Player.Missions[430].Objectives[1].State);
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count());
        }

        [TestMethod]
        public void RecoveredInactiveSourceIdentitySweepNeverReadsOrWritesCharacterProgress()
        {
            using var context = MissionTestContext.WithRecoveredDefinitions();
            Assert.IsFalse(context.Manager.LoadedMissions[1069].IsOperational);
            Assert.IsFalse(context.Manager.LoadedMissions[1407].IsOperational);
            Assert.IsFalse(context.Manager.LoadedMissions[1449].IsOperational);

            foreach (var definition in context.Manager.LoadedMissions.Values)
            {
                context.SeedMission(
                    context.Client.Player.Id,
                    definition.MissionId,
                    (uint)MissionState.Active,
                    false);
                var runtimeObjectives = new Dictionary<uint, MissionObjectiveLog>();
                foreach (var objective in definition.Objectives.Values)
                {
                    context.SeedObjective(
                        context.Client.Player.Id,
                        definition.MissionId,
                        objective.ObjectiveId,
                        MissionObjectiveState.Incomplete);
                    runtimeObjectives.Add(
                        objective.ObjectiveId,
                        new MissionObjectiveLog(
                            objective.ObjectiveId,
                            MissionObjectiveState.Incomplete,
                            new Dictionary<uint, uint>(),
                            new Dictionary<uint, uint>()));
                }
                context.Client.Player.Missions[definition.MissionId] =
                    new MissionLog(
                        definition.MissionId,
                        MissionState.Active,
                        false,
                        runtimeObjectives);
            }
            var recoveredWaypoints = context.Manager.LoadedMissions[1449].Objectives[1]
                .ProgressRule.Subjects.OrderBy(id => id).ToArray();
            var recoveredLogos = context.Manager.LoadedMissions.Values
                .SelectMany(definition => definition.Objectives.Values)
                .Where(objective =>
                    objective.ProgressRule?.Kind == MissionProgressEventKind.LogosAcquired)
                .SelectMany(objective => objective.ProgressRule.Subjects)
                .Distinct()
                .OrderBy(id => id)
                .ToArray();
            using (var unit = context.CreateChar())
            {
                foreach (var waypointId in recoveredWaypoints)
                    unit.CharacterTeleporters.Add(new CharacterTeleporterEntry(
                        context.Client.Player.Id,
                        waypointId,
                        (byte)WaypointType.Waypoint));
                foreach (var logosId in recoveredLogos)
                    unit.CharacterLogoses.SetLogos(context.Client.Player.Id, logosId);
            }
            context.Client.Player.GainedWaypoints.AddRange(
                recoveredWaypoints.Select(waypointId => new CharacterTeleporterEntry(
                    context.Client.Player.Id,
                    waypointId,
                    (byte)WaypointType.Waypoint)));
            context.Client.Player.Logos.AddRange(recoveredLogos);

            var identities = new HashSet<uint>
            {
                38, 42, 43, 75, 79, 80, 81, 82, 83, 84, 91,
                112, 113, 156, 168, 184, 219, 1220, 7364
            };
            foreach (var definition in context.Manager.LoadedMissions.Values)
            {
                identities.Add(definition.MissionId);
                identities.Add(definition.ClientNameTextId.Value);
                foreach (var objective in definition.Objectives.Values)
                {
                    identities.Add(objective.ObjectiveId);
                    if (objective.ClientNameTextId.HasValue)
                        identities.Add(objective.ClientNameTextId.Value);
                    if (objective.ClientBodyTextId.HasValue)
                        identities.Add(objective.ClientBodyTextId.Value);
                    foreach (var textId in objective.ClientCounterTextIds.Where(id => id.HasValue))
                        identities.Add(textId.Value);
                    foreach (var subject in objective.ProgressRule?.Subjects ?? Array.Empty<uint>())
                        identities.Add(subject);
                    foreach (var conversation in objective.Conversations)
                    {
                        identities.Add(conversation.NpcPackageId);
                        identities.Add(conversation.PlayerFlagId);
                        identities.Add((uint)conversation.Type);
                    }
                }
            }

            var runtimeBefore = DescribeRuntimeAttempts(context);
            var durableBefore = DescribeDurableAttempts(context);
            var runtimeWaypointsBefore = context.Client.Player.GainedWaypoints
                .Select(entry => entry.WaypointId).OrderBy(id => id).ToArray();
            var runtimeLogosBefore = context.Client.Player.Logos.OrderBy(id => id).ToArray();
            uint[] durableWaypointsBefore;
            uint[] durableLogosBefore;
            using (var unit = context.CreateChar())
            {
                durableWaypointsBefore = unit.CharacterTeleporters.Get(context.Client.Player.Id)
                    .Select(entry => entry.WaypointId).OrderBy(id => id).ToArray();
                durableLogosBefore = unit.CharacterLogoses.GetLogos(context.Client.Player.Id)
                    .OrderBy(id => id).ToArray();
            }
            context.Drain();
            context.ResetCharUnitCount();
            var baselineSaves = context.SaveAttempts;
            foreach (var subject in identities.OrderBy(id => id))
            foreach (MissionProgressEventKind kind in Enum.GetValues(typeof(MissionProgressEventKind)))
            {
                var progress = kind switch
                {
                    MissionProgressEventKind.WaypointAcquired => MissionProgressEvent.Waypoint(subject),
                    MissionProgressEventKind.LogosAcquired => MissionProgressEvent.Logos(subject),
                    MissionProgressEventKind.CreatureKilled => MissionProgressEvent.Creature(subject),
                    MissionProgressEventKind.MissionCompleted => MissionProgressEvent.Mission(subject),
                    MissionProgressEventKind.ItemAcquired =>
                        MissionProgressEvent.ItemAcquired(subject, 1),
                    MissionProgressEventKind.ItemConsumed =>
                        MissionProgressEvent.ItemConsumed(subject, 1),
                    MissionProgressEventKind.InteractionUsed =>
                        MissionProgressEvent.Interaction(subject),
                    MissionProgressEventKind.AreaEntered =>
                        MissionProgressEvent.Area(subject, subject),
                    MissionProgressEventKind.ItemEquipped =>
                        MissionProgressEvent.ItemEquipped(subject, subject),
                    MissionProgressEventKind.AbilityHit =>
                        MissionProgressEvent.AbilityHit(subject, subject),
                    MissionProgressEventKind.ScenarioEvent =>
                        MissionProgressEvent.Scenario(subject, subject, subject),
                    MissionProgressEventKind.DeadlineElapsed =>
                        MissionProgressEvent.Deadline(subject, subject),
                    MissionProgressEventKind.ObjectiveStateReached =>
                        MissionProgressEvent.ObjectiveState(subject, subject, (byte)MissionObjectiveState.Completed),
                    MissionProgressEventKind.ObjectHit =>
                        MissionProgressEvent.ObjectHit(subject, subject),
                    _ => throw new AssertFailedException()
                };
                Assert.IsFalse(context.Manager.RecordProgress(context.Client, progress),
                    $"Inactive definitions advanced for systematic identity {subject} as {kind}.");
            }

            Assert.AreEqual(0, context.CharUnitsCreated);
            Assert.AreEqual(baselineSaves, context.SaveAttempts);
            Assert.AreEqual(0, context.Drain().Count);
            CollectionAssert.AreEqual(runtimeBefore, DescribeRuntimeAttempts(context));
            CollectionAssert.AreEqual(durableBefore, DescribeDurableAttempts(context));
            CollectionAssert.AreEqual(
                runtimeWaypointsBefore,
                context.Client.Player.GainedWaypoints
                    .Select(entry => entry.WaypointId).OrderBy(id => id).ToArray());
            CollectionAssert.AreEqual(
                runtimeLogosBefore,
                context.Client.Player.Logos.OrderBy(id => id).ToArray());
            using (var unit = context.CreateChar())
            {
                CollectionAssert.AreEqual(
                    durableWaypointsBefore,
                    unit.CharacterTeleporters.Get(context.Client.Player.Id)
                        .Select(entry => entry.WaypointId).OrderBy(id => id).ToArray());
                CollectionAssert.AreEqual(
                    durableLogosBefore,
                    unit.CharacterLogoses.GetLogos(context.Client.Player.Id)
                        .OrderBy(id => id).ToArray());
            }
        }

        [TestMethod]
        public void InvalidClientAndDurableOwnerMismatchAreNoOps()
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnExactSubject(
                    MissionProgressEventKind.CreatureKilled, 82));
            SeedActive(context);
            context.ResetCharUnitCount();
            context.Client.State = ClientState.Disconnected;
            Assert.IsFalse(context.Manager.RecordProgress(
                context.Client, MissionProgressEvent.Creature(82)));
            Assert.AreEqual(0, context.CharUnitsCreated);

            context.Client.State = ClientState.Ingame;
            context.Client.AccountEntry.Id = 2;
            Assert.IsFalse(context.Manager.RecordProgress(
                context.Client, MissionProgressEvent.Creature(82)));
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                context.Client.Player.Missions[321].Objectives[1].State);
            Assert.AreEqual(0, context.Drain().Count);
        }

        private static void SeedActive(MissionTestContext context, uint missionId = 321)
        {
            context.SeedMission(1, missionId, (uint)MissionState.Active, false);
            context.ReloadPlayerMissions();
            context.Drain();
        }

        private static MissionTestContext CreateCompletionAdapterContext()
        {
            var source = CreateMission(429, null);
            var target = CreateMission(
                430,
                MissionProgressRule.CompleteOnExactSubject(
                    MissionProgressEventKind.MissionCompleted, 429));
            var reward = new MissionRewardDefinition(
                100,
                new Dictionary<CurencyType, int>(),
                Array.Empty<MissionRewardItem>(),
                Array.Empty<MissionRewardItem>());
            var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission> { [429] = source, [430] = target },
                new Dictionary<uint, MissionRewardDefinition> { [429] = reward });
            context.SeedMission(1, 429, (uint)MissionState.Active, true);
            context.SeedMission(1, 430, (uint)MissionState.Active, false);
            context.ReloadPlayerMissions();
            context.Drain();
            return context;
        }

        private static Mission CreateMission(uint missionId, MissionProgressRule rule) =>
            CreateMissionWithObjectives(missionId, CreateProgressObjective(1, rule));

        private static Mission CreateMissionWithObjectives(
            uint missionId,
            params MissionObjectiveDefinition[] objectives) =>
            new(
                missionId, $"Mission {missionId}", missionId, 77, 88, 5, 1, 2,
                true, false, objectives, true);

        private static MissionObjectiveDefinition CreateProgressObjective(
            uint objectiveId,
            MissionProgressRule rule,
            IReadOnlyDictionary<uint, MissionObjectiveCounterDefinition> counters = null)
        {
            counters ??= new Dictionary<uint, MissionObjectiveCounterDefinition>();
            var counterTextIds = new uint?[3];
            foreach (var counterId in counters.Keys)
                counterTextIds[counterId] = 9000 + counterId;
            return new MissionObjectiveDefinition(
                objectiveId, 1001, 1002, counterTextIds, objectiveId,
                MissionObjectiveState.Incomplete, true,
                counters,
                new Dictionary<uint, MissionObjectiveItemCounterDefinition>(),
                Array.Empty<MissionObjectiveConversation>(),
                Array.Empty<uint>(), Array.Empty<uint>(), Array.Empty<MissionIndicator>(), rule);
        }

        private static string DescribeProgressPacket(PythonPacket packet) =>
            packet switch
            {
                UpdateObjectiveCounterPacket counter =>
                    $"counter:{counter.MissionId}:{counter.ObjectiveId}:{counter.CounterId}:{counter.CounterValue}",
                ObjectiveCompletedPacket completed =>
                    $"objective:{completed.MissionId}:{completed.ObjectiveId}",
                MissionCompleteablePacket completeable =>
                    $"completable:{completeable.MissionId}",
                _ => $"unexpected:{packet.GetType().Name}"
            };

        private static string[] DescribeRuntimeAttempts(MissionTestContext context) =>
            context.Client.Player.Missions.Values
                .OrderBy(mission => mission.MissionId)
                .SelectMany(mission => new[]
                    {
                        $"mission:{mission.MissionId}:{mission.State}:{mission.Completeable}"
                    }
                    .Concat(mission.Objectives.Values
                        .OrderBy(objective => objective.ObjectiveId)
                        .Select(objective =>
                            $"objective:{mission.MissionId}:{objective.ObjectiveId}:{objective.State}:" +
                            $"{string.Join(",", objective.Counters.OrderBy(counter => counter.Key))}:" +
                            $"{string.Join(",", objective.ItemCounters.OrderBy(counter => counter.Key))}")))
                .ToArray();

        private static string[] DescribeDurableAttempts(MissionTestContext context) =>
            context.Manager.LoadedMissions.Keys
                .OrderBy(missionId => missionId)
                .SelectMany(missionId =>
                {
                    var mission = context.ReadMission(missionId);
                    var progress = context.ReadProgress(missionId).Missions[missionId];
                    return new[]
                        {
                            $"mission:{mission.MissionId}:{mission.MissionState}:{mission.Completeable}"
                        }
                        .Concat(progress.Objectives.Values
                            .OrderBy(objective => objective.ObjectiveId)
                            .Select(objective =>
                                $"objective:{missionId}:{objective.ObjectiveId}:{objective.State}:" +
                                $"{string.Join(",", objective.Counters.OrderBy(counter => counter.Key))}:" +
                                $"{string.Join(",", objective.ItemCounters.OrderBy(counter => counter.Key))}"));
                })
                .ToArray();

        private static Creature AddCreature(MissionTestContext context, uint dbId)
        {
            var creature = new Creature
            {
                DbId = dbId,
                EntityClass = EntityClasses.HumanBaseMale,
                MapContextId = context.Map.MapInfo.MapContextId,
                Level = 1,
                State = CharacterState.Idle,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                Attributes = Enum.GetValues<Attributes>().ToDictionary(
                    id => id, id => new ActorAttributes(id, 100, 100, 0, 0, 0)),
                SpawnPool = new SpawnPool
                {
                    MapContextId = context.Map.MapInfo.MapContextId,
                    AliveCreatures = 1,
                    RespawnTime = 1000,
                    UpdateTimer = 1000
                }
            };
            CellManager.Instance.AddToWorld(context.Map, creature);
            return creature;
        }

        private static void ThrowAtPersistenceBoundary(Exception error) => throw error;
    }
}
