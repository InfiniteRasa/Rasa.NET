extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Packets;
using Rasa.Packets.Game.Server;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;
using Rasa.Test.World;
using Rasa.Test.Gameplay;
using ClientState = RasaGame::Rasa.Data.ClientState;

namespace Rasa.Test.Missions
{
    [TestClass]
    [DoNotParallelize]
    public class CharacterFlagProjectionTests
    {
        [TestMethod]
        public void OwnerIntroductionWritesOneEmptyFlagCollection()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();

            new ManifestationManager(null).CellIntroduceClientToSefl(client);

            var introduction = MissionTestContext.Drain(client).OfType<CreatePhysicalEntityPacket>().Single();
            var flags = introduction.EntityData.OfType<PlayerFlagsPacket>().Single();
            CollectionAssert.AreEqual(new byte[] { 0x81, 0x70 }, MissionTestContext.Encode(flags));
        }

        [TestMethod]
        public void OwnerIntroductionProjectsOnlySortedNonzeroMissionFlagIds()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            client.Player.PlayerFlags = new Dictionary<uint, uint>
            {
                [901] = 7,
                [0x7FFFFFFF] = 1,
                [902] = 0,
                [1] = uint.MaxValue,
                [0] = 1,
                [CharacterFlagIds.ServerFlagStart] = 1,
                [CharacterFlagIds.BootcampComplete] = 1,
                [uint.MaxValue] = 7
            };

            new ManifestationManager(null).CellIntroduceClientToSefl(client);

            var introduction = MissionTestContext.Drain(client).OfType<CreatePhysicalEntityPacket>().Single();
            CollectionAssert.AreEqual(new uint[] { 1, 901, 0x7FFFFFFF },
                ReadFlags(introduction.EntityData.OfType<PlayerFlagsPacket>().Single()));
            Assert.AreEqual(0U, client.Player.PlayerFlags[902]);
            Assert.AreEqual(1U, client.Player.PlayerFlags[CharacterFlagIds.BootcampComplete]);
        }

        [TestMethod]
        [DataRow(new uint[] { }, new byte[] { 0x81, 0x70 })]
        [DataRow(new uint[] { 1 }, new byte[] { 0x81, 0x71, 0x11 })]
        [DataRow(new uint[] { 1, 901, 0x7FFFFFFF },
            new byte[] { 0x81, 0x73, 0x11, 0x1E, 0x85, 0x03, 0x1F, 0xFF, 0xFF, 0xFF, 0x7F })]
        [DataRow(new uint[] { uint.MaxValue }, new byte[] { 0x81, 0x71, 0x1F, 0xFF, 0xFF, 0xFF, 0xFF })]
        public void PacketWritesExactCollectionAndUnsignedIds(uint[] ids, byte[] expected)
        {
            var packet = new PlayerFlagsPacket(ids);

            Assert.AreEqual(GameOpcode.PlayerFlags, packet.Opcode);
            Assert.AreEqual(710, (int)packet.Opcode);
            CollectionAssert.AreEqual(expected, MissionTestContext.Encode(packet));
            CollectionAssert.AreEqual(ids, ReadFlags(packet));
        }

        [TestMethod]
        public void PacketOwnsItsSnapshotAndRejectsAnUnspecifiedCollection()
        {
            var ids = new List<uint> { 901 };
            var packet = new PlayerFlagsPacket(ids);
            ids[0] = 902;
            ids.Add(903);

            CollectionAssert.AreEqual(new uint[] { 901 }, ReadFlags(packet));
            Assert.ThrowsExactly<ArgumentNullException>(() => new PlayerFlagsPacket(null));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void OtherPlayerIntroductionsNeverExposePrivateFlagMembership(bool introduceNewArrival)
        {
            using var world = new WorldTestContext();
            var owner = world.CreateClient();
            var viewer = world.CreateClient();
            owner.Player.PlayerFlags[901] = 7;
            viewer.Player.PlayerFlags[902] = 1;
            var manifestations = new ManifestationManager(null);

            if (introduceNewArrival)
                manifestations.CellIntroduceClientToPlayers(owner, new List<Rasa.Game.Client> { owner, viewer });
            else
                manifestations.CellIntroducePlayersToClient(viewer, new List<Rasa.Game.Client> { viewer, owner });

            var introduction = MissionTestContext.Drain(viewer).OfType<CreatePhysicalEntityPacket>().Single();
            Assert.AreEqual(owner.Player.EntityId, introduction.EntityId);
            CollectionAssert.AreEqual(Array.Empty<uint>(),
                ReadFlags(introduction.EntityData.OfType<PlayerFlagsPacket>().Single()));
            manifestations.CellIntroduceClientToSefl(owner);
            var self = MissionTestContext.Drain(owner).OfType<CreatePhysicalEntityPacket>().Single();
            CollectionAssert.AreEqual(new uint[] { 901 }, ReadFlags(self.EntityData.OfType<PlayerFlagsPacket>().Single()));
        }

        [TestMethod]
        public void SceneFlagsPublishFullOwnerSnapshotsOnlyWhenMembershipChanges()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var observer = context.CreateAdditionalClient(2);
            context.Drain();
            var bindings = FlagSequences(
                (901, 7), (901, 99), (902, 1), (901, 0), (901, null),
                (CharacterFlagIds.BootcampComplete, 1), (902, 0));

            var runId = context.Manager.Scenes.Start(context.Client, "data.sequence", bindings);

            CollectionAssert.AreEqual(new uint[] { 901 }, ReadOnlyFlagUpdate(context.Drain()));
            Assert.AreEqual(0, MissionTestContext.Drain(observer).OfType<PlayerFlagsPacket>().Count());

            Assert.IsTrue(context.Manager.Scenes.Submit(runId,
                new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 1)));
            Assert.AreEqual(99U, context.Client.Player.PlayerFlags[901]);
            Assert.AreEqual(0, context.Drain().OfType<PlayerFlagsPacket>().Count());

            Assert.IsTrue(context.Manager.Scenes.Submit(runId,
                new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 2)));
            CollectionAssert.AreEqual(new uint[] { 901, 902 }, ReadOnlyFlagUpdate(context.Drain()));

            Assert.IsTrue(context.Manager.Scenes.Submit(runId,
                new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 3)));
            CollectionAssert.AreEqual(new uint[] { 902 }, ReadOnlyFlagUpdate(context.Drain()));
            Assert.AreEqual(0U, context.Client.Player.PlayerFlags[901]);

            Assert.IsTrue(context.Manager.Scenes.Submit(runId,
                new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 4)));
            Assert.IsFalse(context.Client.Player.PlayerFlags.ContainsKey(901));
            Assert.AreEqual(0, context.Drain().OfType<PlayerFlagsPacket>().Count());

            Assert.IsTrue(context.Manager.Scenes.Submit(runId,
                new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 5)));
            Assert.IsTrue(context.Client.Player.StartingExperienceCompleted);
            Assert.AreEqual(0, context.Drain().OfType<PlayerFlagsPacket>().Count());

            Assert.IsTrue(context.Manager.Scenes.Submit(runId,
                new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 6)));
            CollectionAssert.AreEqual(Array.Empty<uint>(), ReadOnlyFlagUpdate(context.Drain()));
            Assert.AreEqual(0, MissionTestContext.Drain(observer).OfType<PlayerFlagsPacket>().Count());
        }

        [TestMethod]
        public void WorldProgressPublishesOnlyAfterCommitAndRestoresTheSnapshotOnRelog()
        {
            using var harness = BootcampRuntimeTestHarness.CreateFromPendingSelection();
            AddFlagAction(harness, 901, 7);
            var giver = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1990));
            harness.Drain();
            harness.Context.AfterSave = _ => throw new DbUpdateException("Injected flag rollback.");

            Assert.IsFalse(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Area(1990, 430)));

            Assert.IsFalse(harness.Client.Player.PlayerFlags.ContainsKey(901));
            Assert.AreEqual(0, harness.Drain().OfType<PlayerFlagsPacket>().Count());
            harness.Context.AfterSave = null;
            Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Area(1990, 430)));
            CollectionAssert.AreEqual(new uint[] { 901 }, ReadOnlyFlagUpdate(harness.Drain()));
            Assert.IsFalse(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Area(1990, 430)));
            Assert.AreEqual(0, harness.Drain().OfType<PlayerFlagsPacket>().Count());

            harness.ReconnectFromSelection();

            Assert.AreEqual(7U, harness.Client.Player.PlayerFlags[901]);
            var introduction = harness.Drain().OfType<CreatePhysicalEntityPacket>()
                .Single(packet => packet.EntityId == harness.Client.Player.EntityId);
            CollectionAssert.AreEqual(new uint[] { 901 },
                ReadFlags(introduction.EntityData.OfType<PlayerFlagsPacket>().Single()));
        }

        [TestMethod]
        public void NpcObjectivePublishesItsCommittedFlagsAndRollbackPublishesNothing()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            AddFlagAction(harness, 902, 7, 1992, 4);
            harness.SeedMission(harness.Client.Player.Id, 1990, (uint)MissionState.Completed, false);
            var giver = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            var delessio = harness.AddNpc(BootcampRuntimeTestHarness.CaptainDelessioCreatureId, 2560);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1992));
            harness.Drain();
            harness.Context.AfterSave = _ => throw new DbUpdateException("Injected NPC flag rollback.");

            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, delessio.EntityId, 1992, 4, 1));

            Assert.IsFalse(harness.Client.Player.PlayerFlags.ContainsKey(902));
            Assert.AreEqual(0, harness.Drain().OfType<PlayerFlagsPacket>().Count());
            harness.Context.AfterSave = null;
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, delessio.EntityId, 1992, 4, 1));
            CollectionAssert.AreEqual(new uint[] { 902 }, ReadOnlyFlagUpdate(harness.Drain()));
            Assert.AreEqual(7U, harness.Client.Player.PlayerFlags[902]);
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, delessio.EntityId, 1992, 4, 1));
            Assert.AreEqual(0, harness.Drain().OfType<PlayerFlagsPacket>().Count());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void DeadlineAndAbandonmentPublishTheFullCommittedFlagSet(bool abandon)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            AddFlagAction(harness, 902, 7, 1995, 1, 2);
            ConradCorpseDialogueTests.FindMissingSoldiers(harness);
            harness.UseObjectAndRecover(ConradCorpseDialogueTests.Corpse(harness));
            harness.Manager.Scenes.Start(harness.Client, "data.sequence", FlagSequences((901, 1)));
            harness.Drain();

            if (abandon)
                Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, 1995));
            else
            {
                harness.UtcNow += TimeSpan.FromSeconds(601);
                Assert.IsTrue(harness.Manager.EvaluateDeadlines(harness.Client));
            }

            CollectionAssert.AreEqual(new uint[] { 901, 902 }, ReadOnlyFlagUpdate(harness.Drain()));
            Assert.AreEqual(7U, harness.Client.Player.PlayerFlags[902]);
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[1995].State);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CompositePublishesOneFinalMembershipSnapshotInTransactionOrder(bool typedFlagLast)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            AddFlagAction(harness, 901, 7);
            var giver = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1990));
            harness.Drain();
            using var publication = new MissionScenarioPlan();
            var manifestations = new ManifestationManager(harness.Context);
            var adapter = new Rasa.Game.Missions.Persistence.SceneCharacterAdapter(harness.Manager, manifestations);
            var run = new SceneRun("flags", "test", "data.sequence", 1, 1, 0, "{}", SceneStatus.Running,
                harness.Client.Player.Id);
            using (var unit = harness.Context.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    if (!typedFlagLast)
                        adapter.Apply(harness.Client, run, new SetCharacterFlagIntent("typed", 901, 0), unit, publication);
                    publication.AddProgressPlan(harness.Manager.PlanProgress(harness.Client,
                        new[] { MissionProgressEvent.Area(1990, 430) }, unit));
                    if (typedFlagLast)
                        adapter.Apply(harness.Client, run, new SetCharacterFlagIntent("typed", 901, 0), unit, publication);
                    adapter.Apply(harness.Client, run, new SetCharacterFlagIntent("other", 902, 1), unit, publication);
                });
            Assert.AreEqual(0, harness.Drain().OfType<PlayerFlagsPacket>().Count());

            publication.ApplyRuntime(harness.Client, manifestations, harness.Manager);

            CollectionAssert.AreEqual(typedFlagLast ? new uint[] { 902 } : new uint[] { 901, 902 },
                ReadOnlyFlagUpdate(harness.Drain()));
            Assert.AreEqual(typedFlagLast ? 0U : 7U, harness.Client.Player.PlayerFlags[901]);
        }

        [TestMethod]
        public void FailedSceneFlagWriteKeepsTheOwnerSnapshotAndRetryPublishesOnlyTheCommit()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var runId = context.Manager.Scenes.Start(context.Client, "data.sequence", FlagSequences((901, 7), (901, 0)));
            context.Drain();
            context.AfterSave = _ => throw new DbUpdateException("Injected scene flag rollback.");

            Assert.IsFalse(context.Manager.Scenes.Submit(runId,
                new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 1)));

            Assert.AreEqual(7U, context.Client.Player.PlayerFlags[901]);
            Assert.AreEqual(0, context.Drain().OfType<PlayerFlagsPacket>().Count());
            new ManifestationManager(context).CellIntroduceClientToSefl(context.Client);
            var introduction = context.Drain().OfType<CreatePhysicalEntityPacket>().Single();
            CollectionAssert.AreEqual(new uint[] { 901 },
                ReadFlags(introduction.EntityData.OfType<PlayerFlagsPacket>().Single()));

            context.AfterSave = null;
            Assert.IsTrue(context.Manager.Scenes.Submit(runId,
                new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 1)));
            CollectionAssert.AreEqual(Array.Empty<uint>(), ReadOnlyFlagUpdate(context.Drain()));
        }

        [TestMethod]
        public void FailedPublicationRetriesLatestSnapshotWithoutReadingDatabaseOrRepeatingGrants()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            context.AddRewardTemplate(28, 3147);
            var failPublication = true;
            var attempts = 0;
            var manager = PublicationManager(context, packet =>
            {
                if (packet is PlayerFlagsPacket)
                {
                    attempts++;
                    if (failPublication)
                        throw new IOException("Injected flag publication failure.");
                }
            });
            var manifestations = new ManifestationManager(context);
            var adapter = new Rasa.Game.Missions.Persistence.SceneCharacterAdapter(manager, manifestations);
            var run = new SceneRun("flag-reward", "test", "data.sequence", 1, 1, 0, "{}", SceneStatus.Running, 1);
            var before = context.ReadRewardTotals();
            using (var publication = new MissionScenarioPlan())
            {
                var grant = new MissionRewardDefinition(100,
                    new Dictionary<CurencyType, int> { [CurencyType.Credits] = 7 },
                    new[] { new MissionRewardItem(28, 3) }, null).CreateScenarioGrant();
                publication.AddRewardGrant(grant);
                using (var unit = context.CreateChar())
                    unit.ExecuteTransaction(() =>
                    {
                        grant.PlanAndSave(context.Client, unit.Characters.Get(1), unit, manifestations);
                        adapter.Apply(context.Client, run, new SetCharacterFlagIntent("first", 901, 7), unit, publication);
                    });
                publication.ApplyRuntime(context.Client, manifestations, manager);
            }
            Assert.AreEqual(1, attempts);
            Assert.AreEqual(0, context.Drain().OfType<PlayerFlagsPacket>().Count());
            var granted = context.ReadRewardTotals();
            Assert.AreEqual(before.Experience + 100U, granted.Experience);
            Assert.AreEqual(before.Credits + 7, granted.Credits);
            Assert.AreEqual(before.ItemCount + 3, granted.ItemCount);

            var replacement = new SceneBindings("test", new Dictionary<string, SceneActorDefinition>(),
                new Dictionary<string, SceneRoute>(), new Dictionary<uint, SceneSequence>
                {
                    [0] = new(characterIntents: new CharacterIntent[]
                    {
                        new SetCharacterFlagIntent("clear-first", 901, 0),
                        new SetCharacterFlagIntent("second", 902, 1)
                    })
                });
            manager.Scenes.Start(context.Client, "data.sequence", replacement);
            Assert.AreEqual(2, attempts);
            Assert.AreEqual(0, context.Drain().OfType<PlayerFlagsPacket>().Count());
            using (var newer = context.CreateChar())
                newer.CharacterFlags.Set(1, 999, 1);
            var reads = context.CharUnitsCreated;
            var writes = context.SaveAttempts;
            var inventory = context.Client.Player.Inventory.PersonalInventory.ToArray();
            failPublication = false;

            manager.ScenarioService.TickMap(context.Map);

            CollectionAssert.AreEqual(new uint[] { 902 }, ReadOnlyFlagUpdate(context.Drain()));
            Assert.IsFalse(context.Client.Player.PlayerFlags.ContainsKey(999),
                "Publication retry must not load another writer's newer database state.");
            manager.ScenarioService.TickMap(context.Map);
            Assert.AreEqual(0, context.Drain().OfType<PlayerFlagsPacket>().Count());
            Assert.AreEqual(reads, context.CharUnitsCreated);
            Assert.AreEqual(writes, context.SaveAttempts);
            Assert.AreEqual(granted, context.ReadRewardTotals());
            CollectionAssert.AreEqual(inventory, context.Client.Player.Inventory.PersonalInventory.ToArray());
        }

        [TestMethod]
        public void OwnerReintroductionClearsPendingPublicationUsingOnlyItsCommittedCache()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var failPublication = true;
            var manager = PublicationManager(context, packet =>
            {
                if (failPublication && packet is PlayerFlagsPacket)
                    throw new IOException("Injected flag publication failure.");
            });
            manager.Scenes.Start(context.Client, "data.sequence", FlagSequences((901, 7)));
            Assert.AreEqual(0, context.Drain().OfType<PlayerFlagsPacket>().Count());
            using (var newer = context.CreateChar())
                newer.CharacterFlags.Set(1, 999, 1);
            var reads = context.CharUnitsCreated;
            var writes = context.SaveAttempts;

            new ManifestationManager(context).CellIntroduceClientToSefl(context.Client);

            var introduction = context.Drain().OfType<CreatePhysicalEntityPacket>().Single();
            CollectionAssert.AreEqual(new uint[] { 901 },
                ReadFlags(introduction.EntityData.OfType<PlayerFlagsPacket>().Single()));
            failPublication = false;
            Assert.IsFalse(manager.PublishCharacterFlags(context.Client),
                "The successful owner introduction already carried the pending replacement snapshot.");
            Assert.AreEqual(0, context.Drain().OfType<PlayerFlagsPacket>().Count());
            Assert.AreEqual(reads, context.CharUnitsCreated);
            Assert.AreEqual(writes, context.SaveAttempts);
        }

        [TestMethod]
        public void StartingExperienceCommitRetriesPendingNativeFlagsWithoutExposingTheServerFlag()
        {
            using var context = new BootcampSelectionTestContext();
            context.SeedAccount(71);
            var characterId = context.SeedCharacter(71, 1, "FlagDeparture",
                mapContextId: BootcampSelectionTestContext.BootcampMapContextId, x: -225, y: 101.12099, z: -71);
            context.SeedStartingExperience(characterId, CharacterStartingExperienceState.Bootcamp);
            context.SeedMission(characterId, BootcampSelectionTestContext.MissionFinale, MissionState.Active, completeable: true);
            context.SeedWaypoint(characterId, BootcampSelectionTestContext.ExitPadWaypointId, WaypointType.Dropship);
            var client = context.CreateSelectionClient(71);
            context.Characters.RequestSwitchToCharacterInSlot(client,
                new Rasa.Packets.Game.Client.RequestSwitchToCharacterInSlotPacket { SlotNum = 1 });
            context.MaterializeLoadedClient(client);
            MissionTestContext.Drain(client);
            var failedPublisher = new MissionApplication(context, context.Missions.LoadedMissions,
                new Dictionary<uint, MissionRewardDefinition>(), new ManifestationManager(context),
                beforeMissionPacketPublication: packet =>
                {
                    if (packet is PlayerFlagsPacket)
                        throw new IOException("Injected pre-departure flag publication failure.");
                });
            failedPublisher.Scenes.Start(client, "data.sequence", FlagSequences((901, 7)));
            Assert.AreEqual(7U, client.Player.PlayerFlags[901]);
            Assert.AreEqual(0, MissionTestContext.Drain(client).OfType<PlayerFlagsPacket>().Count());

            context.Objects.SelectWaypoint(client, new Rasa.Packets.MapChannel.Client.SelectWaypointPacket
            {
                WaypointId = BootcampSelectionTestContext.ExitPadWaypointId,
                MapInstanceId = client.Player.MapChannel.InstanceId
            });

            Assert.IsNotNull(client.PendingTransfer);
            Assert.AreEqual(1U, client.Player.PlayerFlags[CharacterFlagIds.BootcampComplete]);
            Assert.IsTrue(client.Player.StartingExperienceCompleted);
            CollectionAssert.AreEqual(new uint[] { 901 }, ReadOnlyFlagUpdate(MissionTestContext.Drain(client)));
            context.CompletePendingDeparture(client);
            var introduction = MissionTestContext.Drain(client).OfType<CreatePhysicalEntityPacket>()
                .Single(packet => packet.EntityId == client.Player.EntityId);
            CollectionAssert.AreEqual(new uint[] { 901 },
                ReadFlags(introduction.EntityData.OfType<PlayerFlagsPacket>().Single()));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void PendingSnapshotCannotFollowAReplacementManifestation(bool sameCharacter)
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var replacementClient = context.CreateAdditionalClient(2);
            var failPublication = true;
            var attempts = 0;
            var manager = PublicationManager(context, packet =>
            {
                if (packet is PlayerFlagsPacket)
                {
                    attempts++;
                    if (failPublication)
                        throw new IOException("Injected flag publication failure.");
                }
            });
            manager.Scenes.Start(context.Client, "data.sequence", FlagSequences((901, 7)));
            context.Drain();
            var original = context.Client.Player;
            var replacement = replacementClient.Player;
            replacement.PlayerFlags[903] = 1;
            if (sameCharacter)
                replacement.Id = original.Id;
            try
            {
                context.Client.Player = replacement;
                failPublication = false;

                Assert.IsFalse(manager.PublishCharacterFlags(context.Client));
                Assert.AreEqual(1, attempts);
                Assert.AreEqual(0, context.Drain().OfType<PlayerFlagsPacket>().Count());
                new ManifestationManager(context).CellIntroduceClientToSefl(context.Client);
                var introduction = context.Drain().OfType<CreatePhysicalEntityPacket>().Single();
                CollectionAssert.AreEqual(new uint[] { 903 },
                    ReadFlags(introduction.EntityData.OfType<PlayerFlagsPacket>().Single()));
            }
            finally
            {
                context.Client.Player = original;
                replacement.Id = 2;
            }
        }

        [TestMethod]
        public void PendingSnapshotIsInvalidatedWhenTheCharacterIdChangesInPlace()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var manager = PublicationManager(context, packet =>
            {
                if (packet is PlayerFlagsPacket)
                    throw new IOException("Injected flag publication failure.");
            });
            manager.Scenes.Start(context.Client, "data.sequence", FlagSequences((901, 7)));
            context.Drain();
            context.Client.Player.Id = 2;
            context.Client.Player.PlayerFlags = new Dictionary<uint, uint> { [903] = 1 };
            try
            {
                Assert.IsFalse(context.Manager.PublishCharacterFlags(context.Client));
                Assert.AreEqual(0, context.Drain().OfType<PlayerFlagsPacket>().Count());
            }
            finally
            {
                context.Client.Player.Id = 1;
            }
        }

        [TestMethod]
        public void DirtySnapshotsWaitDuringTransferAndObserverIntroductionsDoNotAcknowledgeThem()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var observer = context.CreateAdditionalClient(2);
            context.Drain();
            var failedPublisher = PublicationManager(context, packet =>
            {
                if (packet is PlayerFlagsPacket)
                    throw new IOException("Injected flag publication failure.");
            });
            failedPublisher.Scenes.Start(context.Client, "data.sequence", FlagSequences((901, 7)));
            context.Drain();
            context.Client.PendingTransfer = new PlayerTransfer();
            Assert.IsFalse(context.Manager.PublishCharacterFlags(context.Client));
            context.Client.PendingTransfer = null;
            context.Client.State = ClientState.Disconnected;
            Assert.IsFalse(context.Manager.PublishCharacterFlags(context.Client));
            context.Client.State = ClientState.Ingame;

            new ManifestationManager(context).CellIntroduceClientToPlayers(context.Client,
                new List<Rasa.Game.Client> { observer });

            var observed = MissionTestContext.Drain(observer).OfType<CreatePhysicalEntityPacket>().Single();
            CollectionAssert.AreEqual(Array.Empty<uint>(),
                ReadFlags(observed.EntityData.OfType<PlayerFlagsPacket>().Single()));
            Assert.IsTrue(context.Manager.PublishCharacterFlags(context.Client));
            CollectionAssert.AreEqual(new uint[] { 901 }, ReadOnlyFlagUpdate(context.Drain()));
        }

        private static MissionApplication PublicationManager(MissionTestContext context, Action<PythonPacket> beforePublish) =>
            new(context, context.Manager.LoadedMissions, new Dictionary<uint, MissionRewardDefinition>(),
                new ManifestationManager(context), beforeMissionPacketPublication: beforePublish);

        private static void AddFlagAction(BootcampRuntimeTestHarness.Harness harness, uint id, uint value,
            uint mission = 1990, uint objective = 1, uint transition = 1)
        {
            harness.WorldContext.MissionActionEntries.Add(new MissionActionEntry
            {
                MissionId = mission, ContentRevision = "deployment_11", ObjectiveId = objective, TransitionId = transition,
                ActionId = 99, Sequence = 99, Kind = MissionActionKind.SetPlayerFlag,
                PlayerFlagId = id, PlayerFlagValue = value, Comment = "Native flag snapshot regression"
            });
            harness.WorldContext.SaveChanges();
            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
        }

        private static SceneBindings FlagSequences(params (uint Id, uint? Value)[] flags) =>
            new("test", new Dictionary<string, SceneActorDefinition>(), new Dictionary<string, SceneRoute>(),
                flags.Select((flag, index) => new
                {
                    Id = (uint)index,
                    Sequence = new SceneSequence(characterIntents: new CharacterIntent[]
                    {
                        new SetCharacterFlagIntent($"flag-{index}", flag.Id, flag.Value)
                    })
                }).ToDictionary(entry => entry.Id, entry => entry.Sequence));

        private static uint[] ReadOnlyFlagUpdate(IEnumerable<Rasa.Packets.PythonPacket> packets) =>
            ReadFlags(packets.OfType<PlayerFlagsPacket>().Single());

        private static uint[] ReadFlags(PlayerFlagsPacket packet)
        {
            using var stream = new MemoryStream(MissionTestContext.Encode(packet));
            using var binary = new BinaryReader(stream);
            using var reader = new PythonReader(binary);
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(PythonType.List, reader.PeekType());
            var ids = Enumerable.Range(0, reader.ReadList()).Select(_ => reader.ReadUInt()).ToArray();
            Assert.AreEqual(stream.Length, stream.Position, "PlayerFlags must contain exactly one ID collection.");
            return ids;
        }
    }
}
