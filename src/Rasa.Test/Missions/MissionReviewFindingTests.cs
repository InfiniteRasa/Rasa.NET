using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Inventory.Client;
    using Rasa.Packets.Inventory.Server;
    using Rasa.Packets.LootDispenser.Client;
    using Rasa.Packets.LootDispenser.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Mission.Server;
    using Rasa.Repositories.Char;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Repositories.World;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;

    [TestClass]
    [DoNotParallelize]
    public class MissionReviewFindingTests
    {
        [TestMethod]
        public void GenericMissionRuntimeFilesDoNotContainBootcampMissionIdSwitches()
        {
            var repositoryRoot = FindRepositoryRoot();
            foreach (var relativePath in new[]
                     {
                         @"src\Rasa.Game\Missions\MissionApplication.cs",
                         @"src\Rasa.Game\Missions\MissionSceneHost.cs"
                     })
            {
                var path = Path.Combine(repositoryRoot, relativePath);
                var contents = File.ReadAllText(path);
                Assert.IsFalse(
                    Regex.IsMatch(contents, @"\b(1990|1995|2005)\b"),
                    $"{relativePath} still contains a Bootcamp mission id switch.");
                Assert.IsFalse(
                    Regex.IsMatch(
                        contents,
                        @"ShouldDeferBootcampDepartureScenario|ResolveBootcampDepartureMissionId|BootcampPrivateMapContextId"),
                    $"{relativePath} still contains Bootcamp-specific branching.");
            }
        }

        [TestMethod]
        public void DatabaseMissionLoadsInactiveDuringLegacyTransition()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            var factory = new MissionLoadingFactory(
                context,
                new[]
                {
                    new NpcMissionEntry
                    {
                        Id = 321,
                        GiverId = 101,
                        ReciverId = 100,
                        Level = 5,
                        GroupType = 1,
                        CategoryId = 1,
                        Shareable = false,
                        RadioCompleteable = false,
                        Comment = "Assemble With Lieutenant Perkins"
                    },
                    new NpcMissionEntry
                    {
                        Id = 429,
                        GiverId = 101,
                        ReciverId = 100,
                        Level = 3,
                        GroupType = 2,
                        CategoryId = 2,
                        Shareable = true,
                        RadioCompleteable = true,
                        Comment = "River Recon"
                    }
                });
            var manager = new MissionApplication(factory, new Dictionary<uint, Mission>());
            manager.LoadMissions();

            Assert.IsFalse(manager.LoadedMissions[321].IsOperational);
            Assert.IsFalse(manager.LoadedMissions[429].IsOperational);
            StringAssert.Contains(
                manager.LoadedMissions[321].OperationalDiagnostic,
                "legacy npc_mission rows stay inactive");
            StringAssert.Contains(
                manager.LoadedMissions[429].OperationalDiagnostic,
                "legacy npc_mission rows stay inactive");
            Assert.IsFalse(manager.TryGetRewardInfo(321, out _));
            Assert.AreEqual(0, manager.LoadedMissions[321].Objectives.Count);

            var giver = context.AddNpc(101);
            Assert.IsFalse(manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.IsFalse(context.Client.Player.Missions.ContainsKey(321));
        }

        [TestMethod]
        public void RewardlessLegacyNpcMissionRowsAreInactiveAndCannotBeAdvertisedOrAccepted()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            var factory = new MissionLoadingFactory(
                context,
                new[]
                {
                    new NpcMissionEntry
                    {
                        Id = 321,
                        GiverId = 101,
                        ReciverId = 100,
                        Level = 5,
                        GroupType = 1,
                        CategoryId = 1,
                        Shareable = false,
                        RadioCompleteable = false,
                        Comment = "Legacy rewardless mission"
                    }
                });
            var manager = new MissionApplication(factory, new Dictionary<uint, Mission>());
            manager.LoadMissions();

            Assert.IsFalse(manager.LoadedMissions[321].IsOperational);
            StringAssert.Contains(
                manager.LoadedMissions[321].OperationalDiagnostic,
                "legacy npc_mission rows stay inactive");
            Assert.IsFalse(manager.TryGetRewardInfo(321, out _));

            var giver = context.AddNpc(101);
            var classification = manager.ClassifyNpcConversation(context.Client.Player, giver);
            Assert.IsFalse(classification.TryGetStatus(out var status, out var missionIds));
            Assert.AreEqual(ConversationStatus.None, status);
            CollectionAssert.AreEqual(Array.Empty<uint>(), missionIds);
            Assert.IsFalse(manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
        }

        [TestMethod]
        public void IncompleteRecoveredAndUnsupportedRewardDefinitionsHaveExplicitDiagnostics()
        {
            var recovered = MissionDefinitionCatalog.CreateRecoveredInactiveDefinitions()[1069];
            StringAssert.Contains(recovered.OperationalDiagnostic, "objective");

            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            var factory = new MissionLoadingFactory(
                context,
                new[]
                {
                    new NpcMissionEntry
                    {
                        Id = 321,
                        GiverId = 101,
                        ReciverId = 100,
                        Level = 5,
                        GroupType = 1,
                        CategoryId = 1,
                        Comment = "Unsupported reward"
                    }
                },
                new[]
                {
                    new NpcMissionRewardEntry
                    {
                        Id = 321,
                        Type = 99,
                        Credits = 1,
                        ItemTemplateId = 2,
                        Quantity = 3
                    }
                });
            var manager = new MissionApplication(factory, new Dictionary<uint, Mission>());
            manager.LoadMissions();

            Assert.IsFalse(manager.LoadedMissions[321].IsOperational);
            StringAssert.Contains(manager.LoadedMissions[321].OperationalDiagnostic,
                "legacy npc_mission rows stay inactive until an enabled, complete definition is installed by migrations");
            Assert.IsFalse(manager.TryGetRewardInfo(321, out _));
        }

        [TestMethod]
        public void TaskFourRepositoryContractKeepsAccountSlotGetAndNamedAggregateLookup()
        {
            using var context = new MissionTestContext();
            context.SeedCharacter(10, 3, 100);
            context.SeedMission(100, 321, (uint)MissionState.Active, false);
            using var unit = context.CreateChar();

            CollectionAssert.AreEqual(
                new uint[] { 321 },
                unit.CharacterMissions.Get(10, 3).Select(entry => entry.MissionId).ToArray());
            Assert.AreEqual(
                321U,
                unit.CharacterMissions.GetByCharacterAndMission(100, 321).MissionId);
        }

        [TestMethod]
        public void LoginHydrationClearsOnlyUnhydratableRowsAndFreesCapacityForSameId()
        {
            var retainedIds = Enumerable.Range(322, 29)
                .Select(value => (uint)value)
                .ToArray();
            using var context = MissionTestContext.WithDefinitions(
                retainedIds.Prepend(321U).ToArray());
            foreach (var missionId in retainedIds)
                context.SeedMission(
                    context.Client.Player.Id,
                    missionId,
                    (uint)MissionState.Active,
                    false);
            context.SeedLegacyMissionWithoutObjectives(
                context.Client.Player.Id,
                321,
                (uint)MissionState.Active,
                false);

            using (var unit = context.CreateChar())
                new CharacterManager(context, context.Manager)
                    .HydrateMissions(context.Client.Player, unit);

            Assert.AreEqual(29, context.MissionCount(context.Client.Player.Id));
            Assert.IsFalse(context.Client.Player.Missions.ContainsKey(321));
            CollectionAssert.AreEquivalent(
                retainedIds,
                context.Client.Player.Missions.Keys.ToArray());

            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(
                context.Client,
                giver.EntityId,
                321));
            Assert.AreEqual(30, context.MissionCount(context.Client.Player.Id));
            Assert.IsTrue(context.Client.Player.Missions[321].Objectives.ContainsKey(1));

            Assert.IsTrue(context.Manager.TryAbandon(context.Client, 321));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(
                context.Client,
                giver.EntityId,
                321));
            Assert.AreEqual(30, context.MissionCount(context.Client.Player.Id));
        }

        [TestMethod]
        public void LegacyCleanupAndTerminalClearDoNotRemoveMappedMissions()
        {
            using var context = MissionTestContext.WithDefinitions(321, 429);
            context.SeedMission(1, 321, (uint)MissionState.Active, false);
            context.SeedLegacyMissionWithoutObjectives(
                1,
                429,
                (uint)MissionState.Active,
                false);

            using (var unit = context.CreateChar())
                new CharacterManager(context, context.Manager)
                    .HydrateMissions(context.Client.Player, unit);

            Assert.IsTrue(context.Client.Player.Missions.ContainsKey(321));
            Assert.IsFalse(context.Client.Player.Missions.ContainsKey(429));
            Assert.AreEqual(1, context.MissionCount(1));

            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(
                context.Client,
                giver.EntityId,
                429));
            Assert.IsTrue(context.Manager.TryFailMission(context.Client, 429));
            Assert.IsTrue(context.Manager.TryClear(context.Client, 429));
            Assert.IsTrue(context.Client.Player.Missions.ContainsKey(321));
            Assert.AreEqual(1, context.MissionCount(1));
        }

        [TestMethod]
        public void LegacyCleanupRollsBackWithoutPublishingPartialHydration()
        {
            using var context = MissionTestContext.WithDefinitions(321, 429);
            context.SeedMission(1, 321, (uint)MissionState.Active, false);
            context.SeedLegacyMissionWithoutObjectives(
                1,
                429,
                (uint)MissionState.Active,
                false);
            context.Client.Player.Missions = new Dictionary<uint, MissionLog>
            {
                [999] = new MissionLog(
                    999,
                    MissionState.Active,
                    false,
                    new Dictionary<uint, MissionObjectiveLog>())
            };
            context.BeforeSave = database =>
            {
                if (database.ChangeTracker.Entries<CharacterMissionEntry>()
                    .Any(entry => entry.State == EntityState.Deleted))
                    throw new DbUpdateException("Injected legacy cleanup failure.");
            };

            Assert.ThrowsExactly<DbUpdateException>(() =>
            {
                using var unit = context.CreateChar();
                new CharacterManager(context, context.Manager)
                    .HydrateMissions(context.Client.Player, unit);
            });

            CollectionAssert.AreEqual(
                new uint[] { 999 },
                context.Client.Player.Missions.Keys.ToArray());
            Assert.AreEqual(2, context.MissionCount(1));
        }

        [TestMethod]
        public void ObjectiveFailureFailsRequiredMissionAndClearRemovesTerminalState()
        {
            using var context = MissionTestContext.WithObjectiveMission();
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();

            Assert.IsTrue(context.Manager.TryFailObjective(context.Client, 321, 5));
            Assert.AreEqual(MissionObjectiveState.Failed,
                context.Client.Player.Missions[321].Objectives[5].State);
            Assert.AreEqual(MissionState.Failed, context.Client.Player.Missions[321].State);
            var packets = context.Drain();
            Assert.AreEqual(1, packets.OfType<ObjectiveFailedPacket>().Count());
            Assert.AreEqual(1, packets.OfType<MissionFailedPacket>().Count());

            Assert.IsTrue(context.Manager.TryClear(context.Client, 321));
            Assert.IsFalse(context.Client.Player.Missions.ContainsKey(321));
            using var unit = context.CreateChar();
            Assert.IsNull(unit.CharacterMissions.GetByCharacterAndMission(1, 321));
            Assert.AreEqual(1, context.Drain().OfType<MissionClearedPacket>().Count());
            Assert.IsFalse(context.Manager.TryClear(context.Client, 321));
        }

        [TestMethod]
        public void RequiredObjectivePublicationFailureConvergesAndRetryDoesNotRepeatTransition()
        {
            var failedPublications = 0;
            var observedObjectiveState = MissionObjectiveState.Incomplete;
            var observedMissionState = MissionState.Active;
            var observedCompleteable = true;
            MissionTestContext context = null;
            context = MissionTestContext.WithObjectiveMission(
                beforeMissionPacketPublication: packet =>
                {
                    if (packet is ObjectiveFailedPacket && failedPublications++ == 0)
                    {
                        observedObjectiveState =
                            context.Client.Player.Missions[321].Objectives[5].State;
                        observedMissionState =
                            context.Client.Player.Missions[321].State;
                        observedCompleteable =
                            context.Client.Player.Missions[321].Completeable;
                        throw new InvalidOperationException("Injected objective publication failure.");
                    }
                });
            using var scope = context;
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(
                context.Client, giver.EntityId, 321));
            context.Drain();

            Assert.IsTrue(context.Manager.TryFailObjective(
                context.Client, 321, 5));

            Assert.AreEqual(MissionObjectiveState.Failed, observedObjectiveState);
            Assert.AreEqual(MissionState.Failed, observedMissionState);
            Assert.IsFalse(observedCompleteable);
            Assert.AreEqual(MissionObjectiveState.Failed,
                context.Client.Player.Missions[321].Objectives[5].State);
            Assert.AreEqual(MissionState.Failed,
                context.Client.Player.Missions[321].State);
            Assert.IsFalse(context.Client.Player.Missions[321].Completeable);
            Assert.AreEqual((byte)MissionObjectiveState.Failed,
                context.ReadProgress(321).Missions[321].Objectives[5].State);
            Assert.AreEqual((uint)MissionState.Failed,
                context.ReadMission(321).MissionState);
            Assert.IsFalse(context.ReadMission(321).Completeable);
            var saveAttempts = context.SaveAttempts;
            var failurePackets = context.Drain();
            Assert.AreEqual(0, failurePackets.OfType<ObjectiveFailedPacket>().Count());
            Assert.AreEqual(1, failurePackets.OfType<MissionFailedPacket>().Count());

            Assert.IsFalse(context.Manager.TryFailObjective(
                context.Client, 321, 5));
            Assert.AreEqual(saveAttempts, context.SaveAttempts);
            Assert.AreEqual(0, context.Drain().Count);

            context.Manager.PublishInitialState(context.Client);

            var snapshot = context.Drain().OfType<MissionStatusInfoPacket>().Single();
            Assert.AreEqual(MissionState.Failed,
                snapshot.MissionStatusDict[321].MissionState);
            Assert.IsFalse(snapshot.MissionStatusDict[321].Completeable);
            Assert.AreEqual(MissionObjectiveState.Failed,
                snapshot.MissionStatusDict[321].ObjectivesList
                    .Single(objective => objective.ObjectiveId == 5).State);
        }

        [TestMethod]
        public void AuthoritativeNpcLifecycleEventsFailAndClearMissionInProduction()
        {
            using var context = MissionTestContext.WithObjectiveMission();
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(
                context.Client, giver.EntityId, 321));
            context.Drain();
            var npcManager = new NpcManager(context, context.Manager);

            npcManager.ObjectiveFailed(context.Client, 321, 5);
            Assert.AreEqual(MissionObjectiveState.Failed,
                context.Client.Player.Missions[321].Objectives[5].State);
            Assert.AreEqual(MissionState.Failed,
                context.Client.Player.Missions[321].State);
            var failedPackets = context.Drain();
            Assert.AreEqual(1, failedPackets.OfType<ObjectiveFailedPacket>().Count());
            Assert.AreEqual(1, failedPackets.OfType<MissionFailedPacket>().Count());

            npcManager.AbandonMission(
                context.Client,
                new AbandonMissionPacket { MissionId = 321 });

            Assert.IsFalse(context.Client.Player.Missions.ContainsKey(321));
            Assert.AreEqual(1,
                context.Drain().OfType<MissionClearedPacket>().Count());
        }

        [TestMethod]
        public void AdminFailureCommandsUseTheProductionMissionFailurePath()
        {
            using var context = MissionTestContext.WithObjectiveMission();
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(
                context.Client, giver.EntityId, 321));
            context.Drain();
            var commands = new ChatCommandsManager(
                new NpcManager(context, context.Manager));
            commands.RegisterChatCommands();

            commands.ProcessCommand(context.Client, ".failobjective 321 5");
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                context.Client.Player.Missions[321].Objectives[5].State);
            Assert.AreEqual(0,
                context.Drain().OfType<ObjectiveFailedPacket>().Count());

            context.Client.AccountEntry.Level = (byte)GmLevel.Admin;
            commands.ProcessCommand(context.Client, ".failobjective 321 5");

            Assert.AreEqual(MissionObjectiveState.Failed,
                context.Client.Player.Missions[321].Objectives[5].State);
            Assert.AreEqual(MissionState.Failed,
                context.Client.Player.Missions[321].State);
            var packets = context.Drain();
            Assert.AreEqual(1, packets.OfType<ObjectiveFailedPacket>().Count());
            Assert.AreEqual(1, packets.OfType<MissionFailedPacket>().Count());
        }

        [TestMethod]
        public void AdminMissionFailureCommandFailsAnActiveMission()
        {
            using var context = MissionTestContext.WithObjectiveMission();
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(
                context.Client, giver.EntityId, 321));
            context.Drain();
            context.Client.AccountEntry.Level = (byte)GmLevel.Admin;
            var commands = new ChatCommandsManager(
                new NpcManager(context, context.Manager));
            commands.RegisterChatCommands();

            commands.ProcessCommand(context.Client, ".failmission 321");

            Assert.AreEqual(MissionState.Failed,
                context.Client.Player.Missions[321].State);
            Assert.AreEqual((uint)MissionState.Failed,
                context.ReadMission(321).MissionState);
            Assert.AreEqual(1,
                context.Drain().OfType<MissionFailedPacket>().Count());
        }

        [TestMethod]
        public void OptionalObjectiveFailureKeepsMissionCompletableEverywhere()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>
                {
                    [321] = CreateMission(
                        321,
                        CreateObjective(1, true),
                        CreateObjective(2, false))
                });
            context.SeedMission(
                context.Client.Player.Id,
                321,
                (uint)MissionState.Active,
                true);
            context.ReloadPlayerMissions();
            context.Drain();

            Assert.IsTrue(context.Manager.TryFailObjective(
                context.Client, 321, 2));

            Assert.AreEqual(MissionObjectiveState.Failed,
                context.Client.Player.Missions[321].Objectives[2].State);
            Assert.IsTrue(context.Client.Player.Missions[321].Completeable);
            Assert.IsTrue(context.ReadMission(321).Completeable);
            var packets = context.Drain();
            Assert.AreEqual(1, packets.OfType<ObjectiveFailedPacket>().Count());
            Assert.AreEqual(0, packets.OfType<MissionFailedPacket>().Count());
            Assert.AreEqual(0, packets.OfType<MissionCompleteablePacket>().Count());
        }

        [TestMethod]
        public void ItemAcquisitionAndConsumptionAdvanceItemCountersThroughInventoryHooks()
        {
            AssertItemHook(MissionProgressEventKind.ItemAcquired, consume: false);
            AssertItemHook(MissionProgressEventKind.ItemConsumed, consume: true);
        }

        [TestMethod]
        public void ClanDepositAndWithdrawalCannotFarmItemAcquisition()
        {
            const uint itemClassId = 3147;
            using var context = MissionTestContext.WithItemProgressMission(
                MissionProgressEventKind.ItemAcquired,
                itemClassId,
                10);
            context.SeedMission(1, 321, (uint)MissionState.Active, false);
            context.ReloadPlayerMissions();
            var inventory = new InventoryManager(context, context.Manager);
            var item = context.CreateInventoryItem(28, itemClassId, 3);
            Assert.IsNotNull(inventory.GrantItemToInventory(context.Client, item));
            Assert.AreEqual(3U,
                context.Client.Player.Missions[321].Objectives[1].ItemCounters[itemClassId]);

            var clanManager = ClanManager.Instance;
            var originalClans = clanManager.Clans;
            var originalMembers = clanManager.ClanMembers;
            try
            {
                var clan = context.CreateClanForPlayer();
                context.Client.Player.Inventory.ResetClanInventory();
                inventory.ClanLockbox_DepositItemInSlot(
                    context.Client,
                    new ClanLockbox_DepositItemInSlotPacket
                    {
                        SrcSlot = item.OwnerSlotId,
                        DestSlot = 0,
                        Quantity = item.StackSize
                    });
                Assert.AreEqual(3U,
                    context.Client.Player.Missions[321].Objectives[1].ItemCounters[itemClassId]);
                inventory.ClanLockbox_WithdrawItem(
                    context.Client,
                    new ClanLockbox_WithdrawItemPacket
                    {
                        SrcSlot = 0,
                        Quantity = item.StackSize,
                        ManagePersonalSlot = true
                    });

                Assert.AreEqual(clan.Id, context.Client.Player.ClanId);
                Assert.AreEqual(3U,
                    context.Client.Player.Missions[321].Objectives[1].ItemCounters[itemClassId]);
                Assert.AreEqual(1,
                    context.Drain().OfType<UpdateObjectiveItemCounterPacket>().Count());
            }
            finally
            {
                clanManager.Clans = originalClans;
                clanManager.ClanMembers = originalMembers;
                context.Client.Player.ClanId = 0;
            }
        }

        [TestMethod]
        public void SuccessfulObjectUseAdvancesMatchingInteractionObjective()
        {
            const uint objectClassId = 3147;
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnExactSubject(
                    MissionProgressEventKind.InteractionUsed,
                    objectClassId));
            context.SeedMission(1, 321, (uint)MissionState.Active, false);
            context.ReloadPlayerMissions();
            var controlPoint = new DynamicObject
            {
                EntityClassId = (EntityClasses)objectClassId,
                DynamicObjectType = DynamicObjectType.ControlPoint,
                MapContextId = context.Map.MapInfo.MapContextId,
                Position = context.Client.Player.Position,
                IsInWorld = true
            };
            controlPoint.TriggeredByPlayers.Add(context.Client);
            context.Map.ControlPoints.Add(500, controlPoint);
            var manager = new DynamicObjectManager(context, missionManager: context.Manager);

            manager.CaptureControlPointRecovery(
                context.Map,
                new ActionData(
                    context.Client.Player,
                    ActionId.UseObject,
                    DynamicObjectManager.ControlPointUseArgId,
                    0));

            Assert.AreEqual(MissionObjectiveState.Completed,
                context.Client.Player.Missions[321].Objectives[1].State);
            Assert.AreEqual(1, context.Drain().OfType<ObjectiveCompletedPacket>().Count());
        }

        [TestMethod]
        public void RewardPublicationFailureLeavesRuntimeTerminalAndCannotDuplicateCommit()
        {
            var published = 0;
            using var context = MissionTestContext.WithCompletableMission(
                429,
                _ =>
                {
                    if (published++ == 0)
                        throw new InvalidOperationException("Injected publication failure.");
                });
            var before = context.ReadRewardTotals();

            Assert.IsTrue(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0, null));

            Assert.AreEqual(MissionState.Completed, context.Client.Player.Missions[429].State);
            Assert.AreEqual((uint)MissionState.Completed, context.ReadMission(429).MissionState);
            var after = context.ReadRewardTotals();
            Assert.AreEqual(before.ItemCount + 5, after.ItemCount);
            Assert.AreEqual(after.Experience, context.Client.Player.Experience);
            Assert.AreEqual(after.Credits,
                context.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(after.Prestige,
                context.Client.Player.Credits[CurencyType.Prestige]);
            Assert.AreEqual(after.ItemCount, RuntimeItemCount(context.Client));

            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0, null));
            Assert.AreEqual(after, context.ReadRewardTotals());
        }

        [TestMethod]
        public void CommittedLootRecordsAcquisitionWhenItemPublicationFails()
        {
            const uint itemClassId = 3147;
            using var context = MissionTestContext.WithItemProgressMission(
                MissionProgressEventKind.ItemAcquired,
                itemClassId,
                3);
            context.SeedMission(1, 321, (uint)MissionState.Active, false);
            context.ReloadPlayerMissions();
            context.Client.Player.Position = Vector3.Zero;
            context.Client.Player.Attributes[Attributes.Health] =
                new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            var item = context.CreateInventoryItem(28, itemClassId, 3);
            var corpse = new Creature
            {
                EntityClass = EntityClasses.HumanBaseMale,
                MapContextId = context.Map.MapInfo.MapContextId,
                Position = Vector3.Zero,
                State = CharacterState.Dead,
                Faction = Factions.Bane,
                AppearanceData = new(),
                Attributes = new Dictionary<Attributes, ActorAttributes>
                {
                    [Attributes.Health] = new(
                        Attributes.Health, 100, 100, 0, 0, 0),
                    [Attributes.Armor] = new(
                        Attributes.Armor, 0, 0, 0, 0, 0)
                }
            };
            CellManager.Instance.AddToWorld(context.Map, corpse);
            var loot = new LootDispenser
            {
                Owner = context.Client.Player.EntityId,
                AttachedTo = corpse.EntityId,
                IsLootable = true,
                Credits = 7,
                UnitOfWorkFactory = context
            };
            loot.LootItems.Add(new LootItem(
                item,
                context.Client.Player.EntityId,
                0));
            corpse.CorpseLootEntityId = loot.EntityId;
            context.Map.LootDispensers.Add(loot.EntityId, loot);
            var manager = new LootDispenserManager(
                context,
                _ => 2,
                context.Manager,
                _ => throw new InvalidOperationException(
                    "Injected loot publication failure."));
            try
            {
                manager.RequestLootAllFromCorpse(
                    context.Client,
                    new RequestLootAllFromCorpsePacket
                    {
                        EntityId = loot.EntityId
                    });

                var packets = context.Drain();
                AssertAcquisitionPrecedesMissionProgress(
                    packets,
                    typeof(UpdateCreditsPacket),
                    typeof(ActorGotLootPacket),
                    typeof(TakenInfoPacket));
                Assert.AreEqual(3U,
                    context.Client.Player.Missions[321]
                        .Objectives[1].ItemCounters[itemClassId]);
                Assert.AreEqual(3U,
                    context.ReadProgress(321)
                        .Missions[321].Objectives[1].ItemCounters[itemClassId]);

                manager.RequestLootAllFromCorpse(
                    context.Client,
                    new RequestLootAllFromCorpsePacket
                    {
                        EntityId = loot.EntityId
                    });
                Assert.AreEqual(3U,
                    context.Client.Player.Missions[321]
                        .Objectives[1].ItemCounters[itemClassId]);
                Assert.AreEqual(0, context.Drain()
                    .OfType<UpdateObjectiveItemCounterPacket>().Count());
            }
            finally
            {
                manager.RemoveForOwner(context.Map, context.Client);
                CellManager.Instance.RemoveCreatureFromWorld(context.Map, corpse);
            }
        }

        [TestMethod]
        public void AuctionBuyoutPublishesAcquisitionBeforeMissionProgress()
        {
            const uint itemClassId = 3147;
            using var context = MissionTestContext.WithItemProgressMission(
                MissionProgressEventKind.ItemAcquired,
                itemClassId,
                3);
            context.SeedMission(1, 321, (uint)MissionState.Active, false);
            context.ReloadPlayerMissions();
            context.Drain();
            var item = context.CreateAuctionItem(28, itemClassId, 3, 99, 50);
            var manager = new AuctionHouseManager(context, context.Manager);

            manager.RequestAuctionBuyout(
                context.Client,
                new RequestAuctionBuyoutPacket
                {
                    ItemId = checked((uint)item.EntityId),
                    Price = 50
                });

            var packets = context.Drain();
            AssertAcquisitionPrecedesMissionProgress(
                packets,
                typeof(UpdateCreditsPacket),
                typeof(AddInboxItemPacket),
                typeof(AuctionBuyoutSuccessPacket));
            Assert.AreEqual(3U,
                context.Client.Player.Missions[321]
                    .Objectives[1].ItemCounters[itemClassId]);
            Assert.AreEqual(3U,
                context.ReadProgress(321)
                    .Missions[321].Objectives[1].ItemCounters[itemClassId]);
            Assert.IsTrue(context.Client.Player.Inventory.InboxItems.Contains(
                item.EntityId));
        }

        [TestMethod]
        public void AuctionPublicationFailureStillConvergesAndDoesNotDuplicateProgress()
        {
            const uint itemClassId = 3147;
            using var context = MissionTestContext.WithItemProgressMission(
                MissionProgressEventKind.ItemAcquired,
                itemClassId,
                3);
            context.SeedMission(1, 321, (uint)MissionState.Active, false);
            context.ReloadPlayerMissions();
            context.Drain();
            var item = context.CreateAuctionItem(28, itemClassId, 3, 99, 50);
            var failed = false;
            var manager = new AuctionHouseManager(
                context,
                context.Manager,
                packet =>
                {
                    if (!failed && packet is AddInboxItemPacket)
                    {
                        failed = true;
                        throw new InvalidOperationException(
                            "Injected inbox publication failure.");
                    }
                });
            var request = new RequestAuctionBuyoutPacket
            {
                ItemId = checked((uint)item.EntityId),
                Price = 50
            };

            manager.RequestAuctionBuyout(context.Client, request);

            var packets = context.Drain();
            AssertAcquisitionPrecedesMissionProgress(
                packets,
                typeof(UpdateCreditsPacket),
                typeof(AuctionBuyoutSuccessPacket));
            Assert.AreEqual(0, packets.OfType<AddInboxItemPacket>().Count());
            Assert.IsTrue(context.Client.Player.Inventory.InboxItems.Contains(
                item.EntityId));
            Assert.AreEqual(50,
                context.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(3U,
                context.Client.Player.Missions[321]
                    .Objectives[1].ItemCounters[itemClassId]);

            manager.RequestAuctionBuyout(context.Client, request);
            Assert.AreEqual(3U,
                context.ReadProgress(321)
                    .Missions[321].Objectives[1].ItemCounters[itemClassId]);
            Assert.AreEqual(0, context.Drain()
                .OfType<UpdateObjectiveItemCounterPacket>().Count());
        }

        [TestMethod]
        public void AuctionProgressRejectionRollsBackBuyoutAndRetryCommitsOnce()
        {
            const uint itemClassId = 3147;
            using var context = MissionTestContext.WithItemProgressMission(
                MissionProgressEventKind.ItemAcquired,
                itemClassId,
                3);
            context.SeedMission(1, 321, (uint)MissionState.Active, false);
            context.ReloadPlayerMissions();
            context.Drain();
            var item = context.CreateAuctionItem(28, itemClassId, 3, 99, 50);
            var manager = new AuctionHouseManager(context, context.Manager);
            var request = new RequestAuctionBuyoutPacket
            {
                ItemId = checked((uint)item.EntityId),
                Price = 50
            };
            context.Client.Player.Missions[321]
                .Objectives[1].SetItemCounter(itemClassId, 1);

            manager.RequestAuctionBuyout(context.Client, request);

            using (var unit = context.CreateChar())
            {
                Assert.IsNotNull(unit.Auctions.GetAuctionByItemId(item.Id));
                Assert.AreEqual(100, unit.Characters.Get(1).Credit);
                Assert.AreEqual(99U,
                    unit.CharacterInventories.FindByItemId(item.Id).CharacterId);
            }
            Assert.AreEqual(0U,
                context.ReadProgress(321)
                    .Missions[321].Objectives[1].ItemCounters[itemClassId]);
            Assert.AreEqual(0, context.Client.Player.Inventory.InboxItems.Count);

            context.Client.Player.Missions[321]
                .Objectives[1].SetItemCounter(itemClassId, 0);
            manager.RequestAuctionBuyout(context.Client, request);

            Assert.AreEqual(3U,
                context.ReadProgress(321)
                    .Missions[321].Objectives[1].ItemCounters[itemClassId]);
            Assert.AreEqual(1, context.Client.Player.Inventory.InboxItems
                .Count(entityId => entityId == item.EntityId));
            manager.RequestAuctionBuyout(context.Client, request);
            Assert.AreEqual(3U,
                context.ReadProgress(321)
                    .Missions[321].Objectives[1].ItemCounters[itemClassId]);
        }

        [TestMethod]
        public void LootProgressFailureRollsBackClaimAndRetryCommitsOnce()
        {
            const uint itemClassId = 3147;
            using var context = MissionTestContext.WithItemProgressMission(
                MissionProgressEventKind.ItemAcquired,
                itemClassId,
                3);
            context.SeedMission(1, 321, (uint)MissionState.Active, false);
            context.ReloadPlayerMissions();
            context.Client.Player.Position = Vector3.Zero;
            context.Client.Player.Attributes[Attributes.Health] =
                new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            var item = context.CreateInventoryItem(28, itemClassId, 3);
            var corpse = CreateLootCorpse(context, item);
            var manager = new LootDispenserManager(
                context, _ => 2, context.Manager);
            var before = context.ReadRewardTotals();
            context.BeforeSave = db =>
            {
                if (db.ChangeTracker
                    .Entries<CharacterMissionObjectiveItemCounterEntry>()
                    .Any(entry => entry.State == EntityState.Modified))
                    throw new DbUpdateException("Injected transient progress failure.");
            };

            try
            {
                manager.RequestLootAllFromCorpse(
                    context.Client,
                    new RequestLootAllFromCorpsePacket
                    {
                        EntityId = corpse.Loot.EntityId
                    });

                Assert.AreEqual(before, context.ReadRewardTotals());
                Assert.AreEqual(0U,
                    context.ReadProgress(321)
                        .Missions[321].Objectives[1].ItemCounters[itemClassId]);
                Assert.IsFalse(corpse.Loot.LootItems[0].Taken);

                context.BeforeSave = null;
                manager.RequestLootAllFromCorpse(
                    context.Client,
                    new RequestLootAllFromCorpsePacket
                    {
                        EntityId = corpse.Loot.EntityId
                    });
                var after = context.ReadRewardTotals();
                Assert.AreEqual(before.ItemCount + 3, after.ItemCount);
                Assert.AreEqual(3U,
                    context.ReadProgress(321)
                        .Missions[321].Objectives[1].ItemCounters[itemClassId]);

                manager.RequestLootAllFromCorpse(
                    context.Client,
                    new RequestLootAllFromCorpsePacket
                    {
                        EntityId = corpse.Loot.EntityId
                    });
                Assert.AreEqual(after, context.ReadRewardTotals());
                Assert.AreEqual(3U,
                    context.ReadProgress(321)
                        .Missions[321].Objectives[1].ItemCounters[itemClassId]);
            }
            finally
            {
                manager.RemoveForOwner(context.Map, context.Client);
                CellManager.Instance.RemoveCreatureFromWorld(
                    context.Map, corpse.Corpse);
            }
        }

        [TestMethod]
        public void MissionCompletionDependencyFailureRollsBackAndRetriesOnce()
        {
            var source = CreateMission(429);
            var target = CreateMission(
                430,
                CreateObjective(
                    1,
                    true,
                    MissionProgressRule.CompleteOnExactSubject(
                        MissionProgressEventKind.MissionCompleted,
                        429)));
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>
                {
                    [429] = source,
                    [430] = target
                },
                new Dictionary<uint, MissionRewardDefinition>
                {
                    [429] = new(0, null, null, null)
                });
            context.SeedMission(1, 429, (uint)MissionState.Active, true);
            context.SeedMission(1, 430, (uint)MissionState.Active, false);
            context.ReloadPlayerMissions();
            var receiver = context.AddNpc(88);
            context.Drain();
            context.BeforeSave = db =>
            {
                if (db.ChangeTracker
                    .Entries<CharacterMissionObjectiveEntry>()
                    .Any(entry =>
                        entry.Entity.MissionId == 430 &&
                        entry.State == EntityState.Modified))
                    throw new DbUpdateException("Injected transient dependency failure.");
            };

            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, receiver.EntityId, 429, null, null));
            Assert.AreEqual((uint)MissionState.Active,
                context.ReadMission(429).MissionState);
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                context.Client.Player.Missions[430].Objectives[1].State);

            context.BeforeSave = null;
            Assert.IsTrue(context.Manager.CompleteOfferedMission(
                context.Client, receiver.EntityId, 429, null, null));
            Assert.AreEqual((uint)MissionState.Completed,
                context.ReadMission(429).MissionState);
            Assert.AreEqual(MissionObjectiveState.Completed,
                context.Client.Player.Missions[430].Objectives[1].State);
            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, receiver.EntityId, 429, null, null));
            Assert.AreEqual(1,
                context.Drain().OfType<ObjectiveCompletedPacket>().Count());
        }

        [TestMethod]
        public void RewardAcquisitionProgressFailureRollsBackAndRetriesOnce()
        {
            const uint itemClassId = 3147;
            var source = CreateMission(429);
            var itemCounter = new Dictionary<uint, MissionObjectiveItemCounterDefinition>
            {
                [itemClassId] = new(itemClassId, 0, 3)
            };
            var target = CreateMission(
                430,
                new MissionObjectiveDefinition(
                    1, 1001, 1002, new uint?[] { null, null, null }, 0,
                    MissionObjectiveState.Incomplete, true,
                    new Dictionary<uint, MissionObjectiveCounterDefinition>(),
                    itemCounter,
                    Array.Empty<MissionObjectiveConversation>(),
                    Array.Empty<uint>(),
                    Array.Empty<uint>(),
                    Array.Empty<MissionIndicator>(),
                    MissionProgressRule.IncrementItemCounterOnExactSubject(
                        MissionProgressEventKind.ItemAcquired,
                        itemClassId,
                        0,
                        3)));
            var reward = new MissionRewardDefinition(
                0,
                new Dictionary<CurencyType, int>(),
                new[] { new MissionRewardItem(28, 3) },
                Array.Empty<MissionRewardItem>());
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>
                {
                    [429] = source,
                    [430] = target
                },
                new Dictionary<uint, MissionRewardDefinition>
                {
                    [429] = reward
                });
            context.AddRewardTemplate(28, itemClassId);
            context.SeedMission(1, 429, (uint)MissionState.Success, false);
            context.SeedMission(1, 430, (uint)MissionState.Active, false);
            context.ReloadPlayerMissions();
            var receiver = context.AddNpc(88);
            context.Drain();
            var before = context.ReadRewardTotals();
            context.BeforeSave = db =>
            {
                if (db.ChangeTracker
                    .Entries<CharacterMissionObjectiveItemCounterEntry>()
                    .Any(entry =>
                        entry.Entity.MissionId == 430 &&
                        entry.State == EntityState.Modified))
                    throw new DbUpdateException("Injected transient reward progress failure.");
            };

            Assert.IsFalse(context.Manager.RewardOfferedMission(
                context.Client, receiver.EntityId, 429, null, null));
            Assert.AreEqual(before, context.ReadRewardTotals());
            Assert.AreEqual((uint)MissionState.Success,
                context.ReadMission(429).MissionState);
            Assert.AreEqual(0U,
                context.ReadProgress(430)
                    .Missions[430].Objectives[1].ItemCounters[itemClassId]);

            context.BeforeSave = null;
            Assert.IsTrue(context.Manager.RewardOfferedMission(
                context.Client, receiver.EntityId, 429, null, null));
            var after = context.ReadRewardTotals();
            Assert.AreEqual(before.ItemCount + 3, after.ItemCount);
            Assert.AreEqual(3U,
                context.ReadProgress(430)
                    .Missions[430].Objectives[1].ItemCounters[itemClassId]);
            Assert.IsFalse(context.Manager.RewardOfferedMission(
                context.Client, receiver.EntityId, 429, null, null));
            Assert.AreEqual(after, context.ReadRewardTotals());
        }

        [TestMethod]
        [DataRow("overflow")]
        [DataRow("capability")]
        public void UnrelatedProviderExceptionsRetainIdentityAndEscape(string kind)
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var before = context.ReadRewardTotals();
            var expected = kind == "overflow"
                ? (Exception)new OverflowException("Injected provider overflow.")
                : new NotSupportedException("Injected provider capability failure.");
            context.AfterSave = _ => throw expected;
            Exception actual = null;
            try
            {
                context.Manager.CompleteOfferedMission(
                    context.Client, context.Receiver.EntityId, 429, 0, null);
            }
            catch (Exception error)
            {
                actual = error;
            }

            Assert.AreSame(expected, actual);
            context.AfterSave = null;
            Assert.AreEqual(before, context.ReadRewardTotals());
            Assert.AreEqual(MissionState.Active, context.Client.Player.Missions[429].State);
            Assert.IsTrue(context.Client.Player.Missions[429].Completeable);
        }

        [TestMethod]
        [DataRow("experience")]
        [DataRow("clone-credits")]
        public void KnownRewardProgressionOverflowIsRejectedBeforeAnyProviderWrite(string kind)
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var player = context.Client.Player;
            player.Level = 4;
            player.Experience = kind == "experience" ? uint.MaxValue : 42900U;
            player.CloneCredits = kind == "clone-credits" ? uint.MaxValue : 0U;
            using (var unit = context.CreateChar())
            {
                unit.Characters.UpdateCharacterProgression(player.Id, player.Experience, player.Level);
                unit.Characters.UpdateCharacterCloneCredits(player.Id, player.CloneCredits);
            }
            var before = context.ReadRewardTotals();
            var saveAttempts = context.SaveAttempts;

            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0, null));

            Assert.AreEqual(saveAttempts, context.SaveAttempts);
            Assert.AreEqual(before, context.ReadRewardTotals());
            Assert.AreEqual((byte)4, player.Level);
            Assert.AreEqual(kind == "clone-credits" ? uint.MaxValue : 0U, player.CloneCredits);
            Assert.AreEqual(MissionState.Active, player.Missions[429].State);
            Assert.IsTrue(player.Missions[429].Completeable);
        }

        [TestMethod]
        public void KnownRewardArithmeticOverflowIsRejectedAtItsGameplaySource()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            using (var unit = context.CreateChar())
                unit.Characters.UpdateCharacterCurrencies(
                    context.Client.Player.Id,
                    int.MaxValue,
                    context.Client.Player.Credits[CurencyType.Prestige]);
            context.Client.Player.Credits[CurencyType.Credits] = int.MaxValue;
            var before = context.ReadRewardTotals();

            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0, null));

            Assert.AreEqual(before, context.ReadRewardTotals());
            Assert.AreEqual(MissionState.Active,
                context.Client.Player.Missions[429].State);
            Assert.IsTrue(context.Client.Player.Missions[429].Completeable);
        }

        private static void AssertItemHook(MissionProgressEventKind kind, bool consume)
        {
            const uint itemClassId = 3147;
            using var context = MissionTestContext.WithItemProgressMission(kind, itemClassId, 3);
            context.SeedMission(1, 321, (uint)MissionState.Active, false);
            context.ReloadPlayerMissions();
            var item = context.CreateInventoryItem(28, itemClassId, 3);
            var inventory = new InventoryManager(context, context.Manager);

            var placed = consume
                ? inventory.AddItemToInventory(context.Client, item)
                : inventory.GrantItemToInventory(context.Client, item);
            Assert.IsNotNull(placed);
            if (consume)
            {
                context.Drain();
                inventory.ReduceStackCount(
                    context.Client, InventoryType.Personal, placed, 3);
            }

            Assert.AreEqual(3U,
                context.Client.Player.Missions[321].Objectives[1].ItemCounters[itemClassId]);
            Assert.AreEqual(1,
                context.Drain().OfType<UpdateObjectiveItemCounterPacket>().Count());
        }

        private static Mission CreateMission(
            uint missionId,
            params MissionObjectiveDefinition[] objectives) =>
            new(
                missionId,
                $"Mission {missionId}",
                missionId,
                77,
                88,
                5,
                1,
                2,
                true,
                false,
                objectives,
                true);

        private static MissionObjectiveDefinition CreateObjective(
            uint objectiveId,
            bool required,
            MissionProgressRule progressRule = null) =>
            new(
                objectiveId,
                1000 + objectiveId,
                2000 + objectiveId,
                new uint?[] { null, null, null },
                objectiveId,
                MissionObjectiveState.Incomplete,
                required,
                new Dictionary<uint, MissionObjectiveCounterDefinition>(),
                new Dictionary<uint, MissionObjectiveItemCounterDefinition>(),
                Array.Empty<MissionObjectiveConversation>(),
                Array.Empty<uint>(),
                Array.Empty<uint>(),
                Array.Empty<MissionIndicator>(),
                progressRule);

        private static (Creature Corpse, LootDispenser Loot) CreateLootCorpse(
            MissionTestContext context,
            Item item)
        {
            var corpse = new Creature
            {
                EntityClass = EntityClasses.HumanBaseMale,
                MapContextId = context.Map.MapInfo.MapContextId,
                Position = Vector3.Zero,
                State = CharacterState.Dead,
                Faction = Factions.Bane,
                AppearanceData = new(),
                Attributes = new Dictionary<Attributes, ActorAttributes>
                {
                    [Attributes.Health] = new(
                        Attributes.Health, 100, 100, 0, 0, 0),
                    [Attributes.Armor] = new(
                        Attributes.Armor, 0, 0, 0, 0, 0)
                }
            };
            CellManager.Instance.AddToWorld(context.Map, corpse);
            var loot = new LootDispenser
            {
                Owner = context.Client.Player.EntityId,
                AttachedTo = corpse.EntityId,
                IsLootable = true,
                UnitOfWorkFactory = context
            };
            loot.LootItems.Add(new LootItem(
                item,
                context.Client.Player.EntityId,
                0));
            corpse.CorpseLootEntityId = loot.EntityId;
            context.Map.LootDispensers.Add(loot.EntityId, loot);
            return (corpse, loot);
        }

        private static long RuntimeItemCount(Client client) =>
            client.Player.Inventory.PersonalInventory
                .Where(entityId => entityId != 0)
                .Select(EntityManager.Instance.GetItem)
                .Where(item => item != null)
                .Sum(item => (long)item.StackSize);

        private static void AssertAcquisitionPrecedesMissionProgress(
            IReadOnlyList<PythonPacket> packets,
            params Type[] acquisitionPacketTypes)
        {
            var firstMissionPacket = FindIndex(packets, packet =>
                packet is UpdateObjectiveItemCounterPacket or
                    ObjectiveCompletedPacket or
                    MissionCompleteablePacket);
            Assert.IsTrue(firstMissionPacket >= 0, "No mission progress packet was published.");
            foreach (var packetType in acquisitionPacketTypes)
            {
                var acquisitionIndex = FindIndex(packets, packet =>
                    packetType.IsInstanceOfType(packet));
                Assert.IsTrue(
                    acquisitionIndex >= 0,
                    $"No {packetType.Name} was published.");
                Assert.IsTrue(
                    acquisitionIndex < firstMissionPacket,
                    $"{packetType.Name} was published after mission progress.");
            }
        }

        private static int FindIndex(
            IReadOnlyList<PythonPacket> packets,
            Func<PythonPacket, bool> predicate)
        {
            for (var index = 0; index < packets.Count; index++)
                if (predicate(packets[index]))
                    return index;
            return -1;
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null &&
                   !directory.GetFiles("Rasa.NET.sln").Any())
            {
                directory = directory.Parent;
            }

            if (directory == null)
                throw new DirectoryNotFoundException("Could not find repository root from test output.");

            return directory.FullName;
        }

        private sealed class MissionLoadingFactory : IGameUnitOfWorkFactory
        {
            private readonly MissionTestContext _charFactory;
            private readonly IReadOnlyList<NpcMissionEntry> _missions;
            private readonly IReadOnlyList<NpcMissionRewardEntry> _rewards;

            internal MissionLoadingFactory(
                MissionTestContext charFactory,
                IReadOnlyList<NpcMissionEntry> missions,
                IReadOnlyList<NpcMissionRewardEntry> rewards = null)
            {
                _charFactory = charFactory;
                _missions = missions;
                _rewards = rewards ?? Array.Empty<NpcMissionRewardEntry>();
            }

            public ICharUnitOfWork CreateChar() => _charFactory.CreateChar();
            public IWorldUnitOfWork CreateWorld() =>
                new MissionWorldUnitOfWork(_missions, _rewards);
        }

        private sealed class MissionWorldUnitOfWork : IWorldUnitOfWork
        {
            internal MissionWorldUnitOfWork(
                IReadOnlyList<NpcMissionEntry> missions,
                IReadOnlyList<NpcMissionRewardEntry> rewards)
            {
                NpcMissions = new MissionRepository(missions);
                NpcMissionRewards = new RewardRepository(rewards);
            }

            public IActionRepository Actions => null;
            public IEquipmentRepository Equipment => null;
            public ICreatureRepository Creatures => null;
            public IEntityClassRepository EntityClasses => null;
            public IFootlockerRepository Footlockers => null;
            public ILogosRepository Logoses => null;
            public IMapInfoRepository MapInfos => null;
            public IMapLinkRepository MapLinks => null;
            public IKraftwerksRepository Kraftwerks => null;
            public IMapRegionRepository MapRegions => null;
            public IMapMarkerRepository MapMarkers => null;
            public IRecipeRepository Recipes => null;
            public INpcMissionRepository NpcMissions { get; }
            public INpcMissionRewardRepository NpcMissionRewards { get; }
            public IMissionContentRepository MissionContent => null;
            public INpcPackageRepository NpcPackages => null;
            public IPlayerRandomNameRepository RandomNames => null;
            public ISpawnpoolRepository Spawnpools => null;
            public ITeleporterRepository Teleporters => null;
            public void Complete() { }
            public void Reject() { }
            public IDbContextTransaction BeginTransaction() =>
                throw new NotSupportedException();
            public void Dispose() { }
        }

        private sealed class MissionRepository : INpcMissionRepository
        {
            private readonly IReadOnlyList<NpcMissionEntry> _missions;
            internal MissionRepository(IReadOnlyList<NpcMissionEntry> missions) =>
                _missions = missions;
            public List<NpcMissionEntry> Get() => _missions.ToList();
        }

        private sealed class RewardRepository : INpcMissionRewardRepository
        {
            private readonly IReadOnlyList<NpcMissionRewardEntry> _rewards;
            internal RewardRepository(IReadOnlyList<NpcMissionRewardEntry> rewards) =>
                _rewards = rewards;
            public List<NpcMissionRewardEntry> Get(uint missionId) =>
                _rewards.Where(entry => entry.Id == missionId).ToList();
        }
    }
}
