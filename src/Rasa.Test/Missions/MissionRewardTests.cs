using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Networking;
    using Rasa.Packets.Inventory.Server;
    using Rasa.Packets.Manifestation.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Mission.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.Char;

    [TestClass]
    [DoNotParallelize]
    public class MissionRewardTests
    {
        [TestMethod]
        public void SuccessfulTurnInCommitsTheWholeRewardBeforePublishing()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var before = context.ReadRewardTotals();
            context.BeforeSave = _ =>
            {
                Assert.AreEqual(MissionState.Active, context.Client.Player.Missions[429].State);
                Assert.IsTrue(context.Client.Player.Missions[429].Completeable);
                Assert.AreEqual(before.Experience, context.Client.Player.Experience);
                Assert.AreEqual(before.Credits, context.Client.Player.Credits[CurencyType.Credits]);
                Assert.AreEqual(before.Prestige, context.Client.Player.Credits[CurencyType.Prestige]);
                Assert.AreEqual(0, context.Drain().Count);
            };

            Assert.IsTrue(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0, null));

            var after = context.ReadRewardTotals();
            Assert.AreEqual(before.Experience + context.Reward.Experience, after.Experience);
            Assert.AreEqual(before.Credits + context.Reward.Currencies[CurencyType.Credits], after.Credits);
            Assert.AreEqual(before.Prestige + context.Reward.Currencies[CurencyType.Prestige], after.Prestige);
            Assert.AreEqual(before.ItemCount + 5, after.ItemCount);
            Assert.AreEqual(MissionState.Completed, context.Client.Player.Missions[429].State);
            Assert.IsFalse(context.Client.Player.Missions[429].Completeable);
            var packets = context.Drain();
            Assert.AreEqual(1, packets.OfType<ExperienceChangedPacket>().Count());
            Assert.AreEqual(2, packets.OfType<UpdateCreditsPacket>().Count());
            Assert.AreEqual(2, packets.OfType<InventoryAddItemPacket>().Count());
            Assert.IsFalse(packets.OfType<MissionCompleteablePacket>().Single().IsCompleteable);
            Assert.AreEqual(1, packets.OfType<MissionCompletedPacket>().Count());
            Assert.AreEqual(1, packets.OfType<MissionRewardedPacket>().Count());
            var completionIndex = packets.FindIndex(packet => packet is MissionCompletedPacket);
            Assert.IsTrue(packets.FindIndex(packet => packet is MissionCompleteablePacket) < completionIndex);
            Assert.IsTrue(completionIndex < packets.FindIndex(packet =>
                packet is ExperienceChangedPacket or UpdateCreditsPacket or InventoryAddItemPacket));
            var lastRewardDelta = packets.FindLastIndex(packet =>
                packet is ExperienceChangedPacket or UpdateCreditsPacket or InventoryAddItemPacket);
            var rewardedIndex = packets.FindIndex(packet => packet is MissionRewardedPacket);
            Assert.IsTrue(lastRewardDelta < rewardedIndex);
        }

        [TestMethod]
        public void RetriedTurnInCannotDuplicateRewards()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var before = context.ReadRewardTotals();

            Assert.IsTrue(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0));
            context.Drain();
            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0));

            AssertGrantedOnce(context, before, 5);
            Assert.AreEqual(0, context.Drain().Count);
        }

        [TestMethod]
        public void CompletionPublicationFailureStillPublishesCommittedRewards()
        {
            using var context = MissionTestContext.WithObjectiveMission(
                429,
                selectableReward: false,
                beforeMissionPacketPublication: packet =>
                {
                    if (packet is MissionCompletedPacket)
                        throw new InvalidOperationException("Injected completion publication failure.");
                });
            context.SeedMission(context.Client.Player.Id, 429, (uint)MissionState.Active, true);
            context.ReloadPlayerMissions();
            var receiver = context.AddNpc(88);
            var before = context.ReadRewardTotals();
            context.Drain();

            Assert.IsTrue(context.Manager.CompleteOfferedMission(
                context.Client, receiver.EntityId, 429, null, null));

            Assert.AreEqual(MissionState.Completed, context.Client.Player.Missions[429].State);
            Assert.AreEqual((uint)MissionState.Completed, context.ReadMission(429).MissionState);
            Assert.AreEqual(before.Experience + context.Reward.Experience, context.ReadRewardTotals().Experience);
            var packets = context.Drain();
            Assert.IsFalse(packets.OfType<MissionCompleteablePacket>().Single().IsCompleteable);
            Assert.AreEqual(0, packets.OfType<MissionCompletedPacket>().Count());
            Assert.AreEqual(1, packets.OfType<ExperienceChangedPacket>().Count());
            Assert.AreEqual(1, packets.OfType<MissionRewardedPacket>().Count());

            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, receiver.EntityId, 429, null, null));
            Assert.AreEqual(before.Experience + context.Reward.Experience, context.ReadRewardTotals().Experience);
            Assert.AreEqual(0, context.Drain().Count);
        }

        [TestMethod]
        public void InternalIntegerSelectionChoosesExactlyOneRewardAlternative()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var before = context.ReadRewardTotals();

            Assert.IsTrue(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 1));

            AssertGrantedOnce(context, before, 7);
        }

        [TestMethod]
        public void CompetingClientsGrantOneRewardBatchAndOneDurableCompletion()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var competitor = context.CreateCompetingClient();
            Assert.IsTrue(context.Manager.OpenNpcConversation(context.Client, context.Receiver.EntityId));
            Assert.IsTrue(context.Manager.OpenNpcConversation(competitor, context.Receiver.EntityId));
            context.ResetCharUnitCount();
            var before = context.ReadRewardTotals();
            using var start = new ManualResetEventSlim();
            Assert.AreNotSame(context.Client.SyncRoot, competitor.SyncRoot);

            var results = Task.WhenAll(
                Task.Run(() =>
                {
                    start.Wait();
                    return context.Manager.TryCompleteNpcMission(
                        context.Client, context.Receiver.EntityId, 429, 0);
                }),
                Task.Run(() =>
                {
                    start.Wait();
                    return context.Manager.TryCompleteNpcMission(
                        competitor, context.Receiver.EntityId, 429, 0);
                }));
            start.Set();
            var completed = results.GetAwaiter().GetResult();

            Assert.AreEqual(1, completed.Count(result => result));
            Assert.IsTrue(context.CharUnitsCreated >= 2);
            AssertGrantedOnce(context, before, 5);
            Assert.AreEqual(MissionState.Completed, competitor.Player.Missions[429].State);
            Assert.AreEqual(1, context.Drain().OfType<MissionRewardedPacket>().Count());
            Assert.AreEqual(1, MissionTestContext.Drain(competitor).OfType<MissionRewardedPacket>().Count());
        }

        [TestMethod]
        public void CompetingRewardCallersBothConvergeToDurableCompletion()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            using (var unit = context.CreateChar())
            {
                unit.CharacterMissions.SetState(context.Client.Player.Id, 429, (uint)MissionState.Success);
                unit.CharacterMissions.SetCompletable(context.Client.Player.Id, 429, false);
            }
            context.ReloadPlayerMissions();
            context.Drain();
            var competitor = context.CreateCompetingClient();
            Assert.IsTrue(context.Manager.OpenNpcConversation(context.Client, context.Receiver.EntityId));
            Assert.IsTrue(context.Manager.OpenNpcConversation(competitor, context.Receiver.EntityId));
            using var start = new ManualResetEventSlim();

            var results = Task.WhenAll(
                Task.Run(() =>
                {
                    start.Wait();
                    return context.Manager.TryRewardNpcMission(
                        context.Client, context.Receiver.EntityId, 429, 0, null);
                }),
                Task.Run(() =>
                {
                    start.Wait();
                    return context.Manager.TryRewardNpcMission(
                        competitor, context.Receiver.EntityId, 429, 0, null);
                }));
            start.Set();
            var completed = results.GetAwaiter().GetResult();

            Assert.AreEqual(1, completed.Count(result => result));
            Assert.AreEqual(MissionState.Completed,
                context.Client.Player.Missions[429].State);
            Assert.AreEqual(MissionState.Completed,
                competitor.Player.Missions[429].State);
            Assert.AreEqual((uint)MissionState.Completed,
                context.ReadMission(429).MissionState);
            Assert.AreEqual(1,
                context.Drain().OfType<MissionRewardedPacket>().Count());
            Assert.AreEqual(1,
                MissionTestContext.Drain(competitor)
                    .OfType<MissionRewardedPacket>().Count());
        }

        [TestMethod]
        public void ReconnectRetryUsesCompletedDurableStateAndGrantsNothing()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var before = context.ReadRewardTotals();
            Assert.IsTrue(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0));
            context.Drain();

            context.Client.Player.Missions.Clear();
            context.ReloadPlayerMissions();

            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0));
            AssertGrantedOnce(context, before, 5);
            Assert.AreEqual(0, context.Drain().Count);
        }

        [TestMethod]
        public async Task FailedItemPublicationRehydratesCommittedRewardsWithoutASecondGrant()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var before = context.ReadRewardTotals();
            var assignment = context.Client.Player.Missions[429];

            Assert.IsTrue(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0, null));

            var committed = context.ReadRewardTotals();
            Assert.AreEqual(before.Experience + 100U, committed.Experience);
            Assert.AreEqual(before.Credits + 7, committed.Credits);
            Assert.AreEqual(before.Prestige + 3, committed.Prestige);
            Assert.AreEqual(before.ItemCount + 5, committed.ItemCount);
            var committedItems = ReadItems();
            var committedReceipt = ReadReceipt();
            CollectionAssert.AreEqual(new[] { (28U, 3U), (29U, 2U) },
                committedItems.Select(item => (item.TemplateId, item.Quantity)).ToArray());
            Assert.AreEqual((assignment.AssignmentId, assignment.Generation, "mission-reward", "Grant"),
                (committedReceipt.OwnerId, committedReceipt.Generation, committedReceipt.OperationKey, committedReceipt.Kind));
            var outgoing = World.WorldTestContext.Drain(context.Client).ToArray();
            var missed = outgoing.First(packet => packet.Message is CallMethodMessage { Packet: InventoryAddItemPacket });
            var missedPayload = Encode(missed);
            var publicationFailures = 0;
            BufferManager.Initialize(8192, 8, 8);
            LengthedSocket.InitializeEventArgsPool(64);
            using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);
            using var peer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await peer.ConnectAsync(listener.LocalEndPoint);
            using var accepted = await listener.AcceptAsync();
            var transport = new LengthedSocket(accepted, SizeType.Dword, false) { AutoReceive = false };
            typeof(Client).GetProperty(nameof(Client.Socket))!.SetValue(context.Client, transport);
            transport.OnEncrypt = (BufferData data, ref int length) =>
            {
                if (publicationFailures == 0 &&
                    data.Buffer.AsSpan(data.BaseOffset + data.Offset, length).SequenceEqual(missedPayload))
                {
                    publicationFailures++;
                    throw new IOException("Injected one-shot outbound inventory-add frame failure.");
                }
            };
            using var network = new NetworkStream(peer, false);
            using var receivedBytes = new MemoryStream();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var receiving = network.CopyToAsync(receivedBytes, timeout.Token);
            try
            {
                foreach (var packet in outgoing)
                {
                    var sent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    transport.OnSend = _ => sent.TrySetResult(true);
                    transport.OnError = args => sent.TrySetException(new SocketException((int)args.SocketError));
                    var failuresBeforeSend = publicationFailures;
                    context.Client.SendPacket(packet);
                    if (publicationFailures == failuresBeforeSend)
                        await sent.Task.WaitAsync(TimeSpan.FromSeconds(5));
                    else
                        Assert.AreSame(missed, packet);
                }
            }
            finally { transport.Close(); }
            await receiving.WaitAsync(TimeSpan.FromSeconds(5));
            receivedBytes.Position = 0;
            var received = new List<byte[]>();
            using (var reader = new BinaryReader(receivedBytes, System.Text.Encoding.UTF8, true))
                while (receivedBytes.Position < receivedBytes.Length)
                {
                    var length = reader.ReadInt32();
                    Assert.IsTrue(length > 0 && length <= receivedBytes.Length - receivedBytes.Position);
                    received.Add(reader.ReadBytes(length));
                }
            Assert.IsFalse(received.Any(payload => payload.SequenceEqual(missedPayload)),
                "The selected inventory-add frame must not reach the original connection before recovery.");
            Assert.AreEqual(1, publicationFailures);
            CollectionAssert.AreEqual(outgoing.Where(packet => !ReferenceEquals(packet, missed))
                    .Select(packet => Convert.ToHexString(Encode(packet))).ToArray(),
                received.Select(payload => Convert.ToHexString(payload)).ToArray(),
                "Only the selected item frame is lost; every other queued frame reaches the loopback peer.");
            CollectionAssert.AreEqual(committedItems, ReadItems());
            Assert.AreEqual(committedReceipt, ReadReceipt());
            var reconnected = context.CreateCompetingClient();
            using (var unit = context.CreateChar())
            {
                new CharacterManager(context, context.Manager).HydrateMissions(reconnected.Player, unit);
                var character = unit.Characters.Get(reconnected.Player.Id);
                reconnected.Player.Experience = character.Experience;
                reconnected.Player.Credits[CurencyType.Credits] = character.Credit;
                reconnected.Player.Credits[CurencyType.Prestige] = character.Prestige;
                Assert.IsTrue(unit.CharacterMissions.Runtime.WasRewarded(assignment.AssignmentId));
                Assert.HasCount(1, unit.CharacterMissions.Runtime.History(reconnected.Player.Id));
                Assert.IsTrue(unit.CharacterMissions.Runtime.HasReceipt(
                    assignment.AssignmentId, assignment.Generation, "mission-reward"));
            }
            new InventoryManager(context, context.Manager).InitCharacterInventory(reconnected);
            Assert.AreEqual(MissionState.Completed, reconnected.Player.Missions[429].State);
            Assert.AreEqual(assignment.AssignmentId, reconnected.Player.Missions[429].AssignmentId);
            Assert.AreEqual(assignment.Generation, reconnected.Player.Missions[429].Generation);
            Assert.AreEqual(assignment.ContentRevision, reconnected.Player.Missions[429].ContentRevision);
            Assert.AreEqual(committed.Experience, reconnected.Player.Experience);
            Assert.AreEqual(committed.Credits, reconnected.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(5U, reconnected.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Aggregate(0U, (total, item) => total + item.StackSize));
            CollectionAssert.AreEqual(committedItems,
                reconnected.Player.Inventory.PersonalInventory.Where(id => id != 0)
                    .Select(EntityManager.Instance.GetItem).OrderBy(item => item.Id)
                    .Select(item => (item.Id, item.OwnerSlotId, item.ItemTemplate.ItemTemplateId, item.StackSize)).ToArray());

            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                reconnected, context.Receiver.EntityId, 429, 0, null));
            Assert.AreEqual(committed, context.ReadRewardTotals());
            CollectionAssert.AreEqual(committedItems, ReadItems());
            Assert.AreEqual(committedReceipt, ReadReceipt());

            (uint Id, uint Slot, uint TemplateId, uint Quantity)[] ReadItems()
            {
                using var unit = context.CreateChar();
                return unit.CharacterInventories.GetItems(context.Client.AccountEntry.Id)
                    .Where(row => row.CharacterId == context.Client.Player.Id && row.InventoryType == (uint)InventoryType.Personal)
                    .OrderBy(row => row.ItemId).Select(row =>
                    {
                        var item = unit.Items.GetItem(row.ItemId);
                        return (item.ItemId, row.SlotId, item.ItemTemplateId, item.StackSize);
                    }).ToArray();
            }

            (string OwnerId, uint Generation, string OperationKey, string Kind, DateTime CreatedAtUtc) ReadReceipt()
            {
                using var database = context.Open();
                var receipt = database.Set<MissionReceiptEntry>().AsNoTracking().Single(entry =>
                    entry.OwnerId == assignment.AssignmentId && entry.OperationKey == "mission-reward");
                return (receipt.OwnerId, receipt.Generation, receipt.OperationKey, receipt.Kind, receipt.CreatedAtUtc);
            }

            static byte[] Encode(ProtocolPacket packet)
            {
                using var stream = new MemoryStream();
                using var writer = new BinaryWriter(stream);
                packet.Write(writer);
                return stream.ToArray();
            }
        }

        [TestMethod]
        public void SaveFailurePublishesNothingAndAllowsRetry()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var before = context.ReadRewardTotals();
            context.BeforeSave = _ => throw new DbUpdateException("Injected save failure.");

            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0));

            AssertUnchanged(context, before);
            context.BeforeSave = null;
            Assert.IsTrue(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0));
            AssertGrantedOnce(context, before, 5);
        }

        [TestMethod]
        public void ConnectionLossPublishesNothingAndAllowsRetry()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var before = context.ReadRewardTotals();
            context.AfterSave = database => database.Database.GetDbConnection().Close();

            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0));

            AssertUnchanged(context, before);
            context.AfterSave = null;
            Assert.IsTrue(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0));
            AssertGrantedOnce(context, before, 5);
        }

        [TestMethod]
        [DataRow(true, true)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(false, false)]
        public void UnrelatedPersistenceErrorsPreserveIdentityAndStack(
            bool duringQuery,
            bool invalidOperation)
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var before = context.ReadRewardTotals();
            Exception expected = invalidOperation
                ? new InvalidOperationException("Injected application failure.")
                : new NullReferenceException("Injected application failure.");
            if (duringQuery)
                context.BeforeQuery = _ => ThrowAtPersistenceBoundary(expected);
            else
                context.AfterSave = database =>
                {
                    database.Database.GetDbConnection().Close();
                    ThrowAtPersistenceBoundary(expected);
                };

            var actual = invalidOperation
                ? Assert.ThrowsExactly<InvalidOperationException>(() =>
                    context.Manager.CompleteOfferedMission(
                        context.Client, context.Receiver.EntityId, 429, 0))
                : (Exception)Assert.ThrowsExactly<NullReferenceException>(() =>
                    context.Manager.CompleteOfferedMission(
                        context.Client, context.Receiver.EntityId, 429, 0));

            Assert.AreSame(expected, actual);
            StringAssert.Contains(actual.StackTrace, nameof(ThrowAtPersistenceBoundary));
            AssertUnchanged(context, before);
            context.BeforeQuery = null;
            context.AfterSave = null;
            Assert.IsTrue(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0));
        }

        [TestMethod]
        public void ProgrammingFailureReleasesStagedRewardEntities()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var heldEntityIds = new List<ulong>();
            while (GetFreeEntityIds().Count > 0)
                heldEntityIds.Add(EntityManager.Instance.GetEntityId);
            var expectedEntityId = EntityManager.Instance.GetEntityId;
            EntityManager.Instance.FreeEntity(expectedEntityId);
            var expected = new InvalidOperationException("Injected application failure.");
            context.AfterSave = database =>
            {
                if (database.ItemEntries.Local.Any())
                    ThrowAtPersistenceBoundary(expected);
            };

            var actual = Assert.ThrowsExactly<InvalidOperationException>(() =>
                context.Manager.CompleteOfferedMission(
                    context.Client, context.Receiver.EntityId, 429, 0));

            Assert.AreSame(expected, actual);
            var reusableEntityId = EntityManager.Instance.GetEntityId;
            Assert.AreEqual(expectedEntityId, reusableEntityId);
            EntityManager.Instance.FreeEntity(reusableEntityId);
            foreach (var entityId in heldEntityIds)
                EntityManager.Instance.FreeEntity(entityId);
        }

        [TestMethod]
        public void MidPublicationFailureKeepsEveryCommittedItemInRuntimeInventory()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var expected = new InvalidOperationException("Injected publication failure.");
            var published = new List<Item>();
            using var grant = new InventoryManager.InventoryGrant(item =>
            {
                published.Add(item);
                if (published.Count == 2)
                    ThrowAtPersistenceBoundary(expected);
            });
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() => grant.PlanAndSave(
                    context.Client,
                    new[] { new InventoryManager.InventoryItemGrant(28, 100001) },
                    unit));
            grant.Publish(context.Client);

            grant.Dispose();
            Assert.AreEqual(3, published.Count);
            var freeIds = GetFreeEntityIds();
            foreach (var item in published)
            {
                CollectionAssert.DoesNotContain(freeIds, item.EntityId);
                CollectionAssert.Contains(
                    context.Client.Player.Inventory.PersonalInventory,
                    item.EntityId);
                EntityManager.Instance.ReleaseEntity(item.EntityId, EntityType.Item);
            }
        }

        [TestMethod]
        public void ClosedConnectionLookalikeInvalidOperationPreservesIdentityAndStack()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var before = context.ReadRewardTotals();
            var expected = new InvalidOperationException(
                "The transaction object is not associated with the same connection object as this command.");
            context.AfterSave = database =>
            {
                database.Database.GetDbConnection().Close();
                ThrowAtPersistenceBoundary(expected);
            };

            var actual = Assert.ThrowsExactly<InvalidOperationException>(() =>
                context.Manager.CompleteOfferedMission(
                    context.Client, context.Receiver.EntityId, 429, 0));

            Assert.AreSame(expected, actual);
            StringAssert.Contains(actual.StackTrace, nameof(ThrowAtPersistenceBoundary));
            AssertUnchanged(context, before);
        }

        [TestMethod]
        public void ReceiverRemovedAfterTransactionStartsCannotReceiveReward()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var before = context.ReadRewardTotals();
            var removed = 0;
            context.BeforeQuery = _ =>
            {
                if (Interlocked.Exchange(ref removed, 1) == 0)
                    context.RemoveNpcFromWorld(context.Receiver);
            };

            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0));

            AssertUnchanged(context, before);
        }

        [TestMethod]
        public void IntegerSelectionTurnInApiIsNotPublic()
        {
            var method = typeof(MissionApplication).GetMethod(
                nameof(MissionApplication.TryCompleteNpcMission),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Client), typeof(ulong), typeof(uint), typeof(int) },
                null);

            Assert.IsNotNull(method);
            Assert.IsFalse(method.IsPublic);
            Assert.IsTrue(method.IsAssembly);
        }

        [TestMethod]
        [DataRow("database")]
        public void ExpectedGameplayAndPersistenceFailuresPublishNothingAndAllowRetry(string failure)
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var before = context.ReadRewardTotals();
            Exception expected = failure switch
            {
                "database" => new DbUpdateException("Injected update failure."),
                _ => throw new AssertFailedException($"Unknown failure {failure}.")
            };
            context.AfterSave = _ => throw expected;

            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0));

            AssertUnchanged(context, before);
            context.AfterSave = null;
            Assert.IsTrue(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0));
        }

        [TestMethod]
        public void SyntheticTransientExecutionStrategyLookalikePreservesIdentityAndStack()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var before = context.ReadRewardTotals();
            var expected = new InvalidOperationException(
                "Injected execution strategy wrapper.",
                new DbUpdateException("Injected transient update failure.", new TestDbException(true)));
            context.AfterSave = _ => ThrowAtPersistenceBoundary(expected);

            var actual = Assert.ThrowsExactly<InvalidOperationException>(() =>
                context.Manager.CompleteOfferedMission(
                    context.Client, context.Receiver.EntityId, 429, 0));

            Assert.AreSame(expected, actual);
            StringAssert.Contains(actual.StackTrace, nameof(ThrowAtPersistenceBoundary));
            AssertUnchanged(context, before);
        }

        [TestMethod]
        public void NonTransientExecutionStrategyLookalikePreservesIdentityAndStack()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var before = context.ReadRewardTotals();
            var expected = new InvalidOperationException(
                "Injected execution strategy lookalike.",
                new DbUpdateException("Injected non-transient update failure.", new TestDbException(false)));
            context.AfterSave = _ => ThrowAtPersistenceBoundary(expected);

            var actual = Assert.ThrowsExactly<InvalidOperationException>(() =>
                context.Manager.CompleteOfferedMission(
                    context.Client, context.Receiver.EntityId, 429, 0));

            Assert.AreSame(expected, actual);
            StringAssert.Contains(actual.StackTrace, nameof(ThrowAtPersistenceBoundary));
            AssertUnchanged(context, before);
        }

        [TestMethod]
        public void WrongReceiverNonCompletableMissionAndInvalidSelectionAreRejected()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var wrong = context.AddNpc(77);
            var before = context.ReadRewardTotals();

            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, wrong.EntityId, 429, 0));
            context.Client.Player.Missions[429].Completeable = false;
            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0));
            context.Client.Player.Missions[429].Completeable = true;
            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 2, null));

            AssertUnchanged(context, before);
        }

        [TestMethod]
        public void FullInventoryRejectsTheWholeRewardAndAllowsRetry()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            context.FillRewardCategory();
            var before = context.ReadRewardTotals();

            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0));

            AssertUnchanged(context, before);
            for (var slot = 50; slot <= 51; slot++)
            {
                var item = EntityManager.Instance.GetItem(
                    context.Client.Player.Inventory.PersonalInventory[slot]);
                using (var unit = context.CreateChar())
                {
                    unit.CharacterInventories.DeleteInvItem(
                        context.Client.AccountEntry.Id,
                        context.Client.Player.Id,
                        (uint)InventoryType.Personal,
                        (uint)slot);
                    unit.Items.DeleteItem(item.Id);
                }
                EntityManager.Instance.ReleaseEntity(item.EntityId, EntityType.Item);
                context.Client.Player.Inventory.PersonalInventory[slot] = 0;
            }
            Assert.IsTrue(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0, null));
        }

        [TestMethod]
        public void NullSelectionIsRequiredWhenRewardHasNoSelectableItems()
        {
            using var context = MissionTestContext.WithObjectiveMission(429, selectableReward: false);
            context.SeedMission(1, 429, (uint)MissionState.Active, true);
            context.ReloadPlayerMissions();
            var receiver = context.AddNpc(88);

            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, receiver.EntityId, 429, 0, null));
            Assert.IsTrue(context.Manager.CompleteOfferedMission(
                context.Client, receiver.EntityId, 429, null, null));
            Assert.AreEqual(MissionState.Completed, context.Client.Player.Missions[429].State);
        }

        [TestMethod]
        public void SelectableRewardRequiresInRangeIndexAndRejectsRating()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var before = context.ReadRewardTotals();

            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, null, null));
            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, -1, null));
            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 2, null));
            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0, 5));

            AssertUnchanged(context, before);
            Assert.AreEqual(typeof(int?),
                typeof(CompleteNPCMissionPacket).GetProperty(nameof(CompleteNPCMissionPacket.SelectionIdx))!.PropertyType);
            Assert.AreEqual(typeof(int?),
                typeof(CompleteNPCMissionPacket).GetProperty(nameof(CompleteNPCMissionPacket.Rating))!.PropertyType);
        }

        [TestMethod]
        public void RewardRequestRecoversDurableSuccessWithoutRepeatingMissionCompleted()
        {
            using var context = MissionTestContext.WithObjectiveMission(429, selectableReward: false);
            context.SeedMission(1, 429, (uint)MissionState.Success, false);
            context.ReloadPlayerMissions();
            var receiver = context.AddNpc(88);
            var before = context.ReadRewardTotals();

            Assert.IsTrue(context.Manager.RewardOfferedMission(
                context.Client, receiver.EntityId, 429, null, null));

            Assert.AreEqual((uint)MissionState.Completed, context.ReadMission(429).MissionState);
            Assert.AreEqual(before.Experience + 100U, context.ReadRewardTotals().Experience);
            var packets = context.Drain();
            Assert.AreEqual(0, packets.OfType<MissionCompletedPacket>().Count());
            Assert.AreEqual(1, packets.OfType<MissionRewardedPacket>().Count());
            Assert.IsFalse(context.Manager.RewardOfferedMission(
                context.Client, receiver.EntityId, 429, null, null));
        }

        [TestMethod]
        [DataRow(1449u)]
        [DataRow(1407u)]
        [DataRow(1069u)]
        public void ProductionMissionsRemainInactiveWithoutApprovedRewardDefinitions(uint missionId)
        {
            using var context = MissionTestContext.WithRecoveredDefinitions();
            context.SeedMission(context.Client.Player.Id, missionId, (uint)MissionState.Active, true);
            context.ReloadPlayerMissions();
            var receiver = context.AddNpc(88);
            var before = context.ReadRewardTotals();

            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                context.Client, receiver.EntityId, missionId, 0));

            var after = context.ReadRewardTotals();
            Assert.AreEqual(before.Experience, after.Experience);
            Assert.AreEqual(before.Credits, after.Credits);
            Assert.AreEqual(before.Prestige, after.Prestige);
            Assert.AreEqual(before.ItemCount, after.ItemCount);
            Assert.AreEqual((uint)MissionState.Active, context.ReadMission(missionId).MissionState);
            Assert.IsTrue(context.ReadMission(missionId).Completeable);
            Assert.IsFalse(context.Client.Player.Missions.ContainsKey(missionId));
            Assert.AreEqual(0, context.Drain().Count);
        }

        private static void AssertGrantedOnce(
            MissionTestContext context,
            MissionTestContext.RewardTotals before,
            long itemQuantity)
        {
            var after = context.ReadRewardTotals();
            Assert.AreEqual(before.Experience + context.Reward.Experience, after.Experience);
            Assert.AreEqual(before.Credits + context.Reward.Currencies[CurencyType.Credits], after.Credits);
            Assert.AreEqual(before.Prestige + context.Reward.Currencies[CurencyType.Prestige], after.Prestige);
            Assert.AreEqual(before.ItemCount + itemQuantity, after.ItemCount);
            Assert.AreEqual((uint)MissionState.Completed, context.ReadMission(429).MissionState);
            Assert.IsFalse(context.ReadMission(429).Completeable);
        }

        private static void AssertUnchanged(
            MissionTestContext context,
            MissionTestContext.RewardTotals before)
        {
            var after = context.ReadRewardTotals();
            Assert.AreEqual(before.Experience, after.Experience);
            Assert.AreEqual(before.Credits, after.Credits);
            Assert.AreEqual(before.Prestige, after.Prestige);
            Assert.AreEqual(before.ItemCount, after.ItemCount);
            Assert.AreEqual((uint)MissionState.Active, context.ReadMission(429).MissionState);
            Assert.IsTrue(context.ReadMission(429).Completeable);
            Assert.AreEqual(MissionState.Active, context.Client.Player.Missions[429].State);
            Assert.IsTrue(context.Client.Player.Missions[429].Completeable);
            Assert.AreEqual(0, context.Drain().Count);
        }

        private static void ThrowAtPersistenceBoundary(Exception error) => throw error;

        private static ICollection GetFreeEntityIds() => (ICollection)typeof(EntityManager)
            .GetField("_freeEntityIds", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(EntityManager.Instance)!;

        private sealed class TestDbException : DbException
        {
            private readonly bool _isTransient;

            internal TestDbException(bool isTransient) : base("Injected database failure.")
            {
                _isTransient = isTransient;
            }

            public override bool IsTransient => _isTransient;
        }
    }
}
