extern alias RasaGame;

using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Networking;
    using Rasa.Repositories.Char;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Repositories.World;
    using Rasa.Structures;

    [TestClass]
    [DoNotParallelize]
    public class WorldLifecycleTests
    {
        [TestMethod]
        public void CreatureRemovalUnregistersEvenWithoutObservers()
        {
            using var world = new WorldTestContext();
            var creature = new Creature { Position = Vector3.Zero };
            CellManager.Instance.AddToWorld(world.Map, creature);
            try
            {
                CellManager.Instance.RemoveCreatureFromWorld(world.Map, creature);

                Assert.IsFalse(EntityManager.Instance.RegisteredEntities.ContainsKey(creature.EntityId));
                Assert.IsFalse(EntityManager.Instance.Creatures.ContainsKey(creature.EntityId));
                Assert.IsFalse(world.Map.MapCellInfo.Cells.Values.Any(cell => cell.CreatureList.Contains(creature)));
            }
            finally
            {
                EntityManager.Instance.UnregisterEntity(creature.EntityId);
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
                EntityManager.Instance.FreeEntity(creature.EntityId);
            }
        }

        [TestMethod]
        public void DuplicateCorpseRemovalUpdatesThePoolOnlyOnce()
        {
            using var world = new WorldTestContext();
            var pool = new SpawnPool();
            SpawnPoolManager.Instance.IncreaseDeadCreatureCount(pool);
            var corpse = new Creature { Position = Vector3.Zero, State = CharacterState.Dead, SpawnPool = pool };
            CellManager.Instance.AddToWorld(world.Map, corpse);
            world.Map.MapCellInfo.Cells[corpse.Cells[0, 0]].CreatureList.Add(corpse);
            try
            {
                Assert.IsTrue(CellManager.Instance.RemoveCreatureFromWorld(world.Map, corpse));
                Assert.IsFalse(CellManager.Instance.RemoveCreatureFromWorld(world.Map, corpse));

                Assert.AreEqual(0, pool.DeadCreatures);
                Assert.IsFalse(world.Map.MapCellInfo.Cells.Values.Any(cell => cell.CreatureList.Contains(corpse)));
            }
            finally
            {
                EntityManager.Instance.UnregisterEntity(corpse.EntityId);
                EntityManager.Instance.UnregisterCreature(corpse.EntityId);
                EntityManager.Instance.FreeEntity(corpse.EntityId);
            }
        }

        [TestMethod]
        public void RepeatedRemovalDoesNotNotifyObserversTwice()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var observer = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            CellManager.Instance.AddToWorld(observer);
            WorldTestContext.Drain(observer);

            CellManager.Instance.RemoveFromWorld(client);
            Assert.IsTrue(WorldTestContext.Drain(observer).Count > 0);
            CellManager.Instance.RemoveFromWorld(client);

            Assert.AreEqual(0, WorldTestContext.Drain(observer).Count);
            Assert.IsFalse(CellManager.Instance.IsInWorld(client));
        }

        [TestMethod]
        public void DuplicateMapLoadedDoesNotReinitializeAnActivePlayer()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            WorldTestContext.Drain(client);

            new MapChannelManager(null).MapLoaded(client);

            Assert.AreEqual(ClientState.Ingame, client.State);
            Assert.AreEqual(0, WorldTestContext.Drain(client).Count);
            Assert.AreEqual(1, world.Map.ClientList.Count(player => player == client));
        }

        [TestMethod]
        public void InventoryResendDoesNotGrowExistingSlotLists()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var inventory = client.Player.Inventory;

            InventoryManager.Instance.ResendForMap(client);
            InventoryManager.Instance.ResendForMap(client);

            Assert.AreSame(inventory, client.Player.Inventory);
            Assert.AreEqual(17, inventory.EquippedInventory.Count);
            Assert.AreEqual(0, inventory.PersonalInventory.Count);
        }

        [TestMethod]
        public void DisconnectedPlayerIsRemovedFromCellsQueuesAndRegistries()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var observer = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            CellManager.Instance.AddToWorld(observer);
            WorldTestContext.Drain(observer);
            world.Map.QueuedClients.Enqueue(client);
            world.Map.QueuedClients.Enqueue(client);
            client.State = ClientState.Disconnected;
            var maps = new MapChannelManager(null);
            maps.MapChannelArray.Add(1220, world.Map);

            maps.CleanupDisconnected(client);

            Assert.IsFalse(world.Map.ClientList.Contains(client));
            Assert.IsFalse(world.Map.QueuedClients.Contains(client));
            Assert.IsFalse(world.Map.MapCellInfo.Cells.Values.Any(cell => cell.ClientList.Contains(client)));
            Assert.IsFalse(EntityManager.Instance.Players.ContainsKey(client.Player.EntityId));
            Assert.IsFalse(EntityManager.Instance.Actors.ContainsKey(client.Player.EntityId));
            Assert.IsNull(client.Player.MapChannel);
            Assert.IsTrue(WorldTestContext.Drain(observer).Count > 0);
            maps.CleanupDisconnected(client);
            Assert.AreEqual(0, WorldTestContext.Drain(observer).Count);
        }

        [TestMethod]
        public void QueuedMapLoadsDoNotDuplicateOrActivatePlayersBeforeAcknowledgement()
        {
            using var world = new WorldTestContext();
            var loading = world.CreateClient();
            var disconnected = world.CreateClient();
            world.Map.ClientList.Clear();
            loading.State = ClientState.Loading;
            disconnected.State = ClientState.Disconnected;
            world.Map.QueuedClients.Enqueue(loading);
            world.Map.QueuedClients.Enqueue(loading);
            world.Map.QueuedClients.Enqueue(disconnected);

            MapChannelManager.PruneQueuedClients(world.Map);

            Assert.AreEqual(1, world.Map.QueuedClients.Count);
            Assert.AreSame(loading, world.Map.QueuedClients.Peek());
            Assert.AreEqual(0, world.Map.ClientList.Count);
            MapChannelManager.RemoveQueuedClient(world.Map, loading);
            Assert.AreEqual(0, world.Map.QueuedClients.Count);
        }

        [TestMethod]
        public void CancellingAnUnacknowledgedTransferRestoresTheSavePosition()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var destination = DropshipTravelTests.CreateDestination();
            client.PendingTransfer = new PlayerTransfer
            {
                OriginMap = world.Map,
                OriginPosition = Vector3.Zero,
                OriginRotation = 1,
                DestinationMap = destination,
                DestinationPosition = new Vector3(400, 5, 0),
                IsDropship = true,
                HasDeparted = true
            };
            client.Player.MapChannel = destination;
            client.Player.MapContextId = 1300;
            client.SetWorldPosition(new Vector3(400, 5, 0), 0);

            client.RestoreTransferOrigin();

            Assert.AreSame(world.Map, client.Player.MapChannel);
            Assert.AreEqual(1220U, client.Player.MapContextId);
            Assert.AreEqual(Vector3.Zero, client.Player.Position);
            Assert.AreEqual(Vector3.Zero, client.Movement.Position);
            Assert.AreEqual(1d, client.Player.Rotation);
            Assert.IsNull(client.PendingTransfer);
        }

        [TestMethod]
        public void CharacterSwitchDoesNotReplaceAnActiveWorldPlayer()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var original = client.Player;

            new CharacterManager(null).RequestSwitchToCharacterInSlot(client,
                new Rasa.Packets.Game.Client.RequestSwitchToCharacterInSlotPacket { SlotNum = 1 });

            Assert.AreSame(original, client.Player);
            Assert.AreEqual(ClientState.Ingame, client.State);
        }

        [TestMethod]
        public void LogoutDoesNotStartDuringAnUnacknowledgedTransfer()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            client.PendingTransfer = new PlayerTransfer { OriginMap = world.Map };

            new MapChannelManager(null).RequestLogout(client);

            Assert.IsFalse(client.Player.LogoutActive);
            Assert.AreEqual(0, WorldTestContext.Drain(client).Count);
        }

        [TestMethod]
        public void MapLinkTransferPreservesManifestationUntilMapLoadedCommitsIt()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var destination = DropshipTravelTests.CreateDestination();
            CellManager.Instance.AddToWorld(client);
            var inventory = client.Player.Inventory;
            var maps = new MapChannelManager(null, updateCharacter: (_, _, _) => { },
                refreshStats: (_, _) => { }, assignPlayer: _ => { }, enterMapChannels: _ => { });
            maps.MapChannelArray.Add(world.Map.MapInfo.MapContextId, world.Map);
            maps.MapChannelArray.Add(destination.MapInfo.MapContextId, destination);

            Assert.IsTrue(maps.ChangeMap(client, destination.MapInfo.MapContextId,
                new Vector3(400, 5, 0), 0));

            Assert.IsNotNull(client.PendingTransfer);
            Assert.IsTrue(client.PendingTransfer.HasDeparted);
            Assert.AreEqual(ClientState.Teleporting, client.State);
            Assert.IsTrue(client.AwaitingMapLoaded);
            Assert.IsFalse(world.Map.ClientList.Contains(client));
            Assert.IsFalse(destination.ClientList.Contains(client));
            Assert.IsTrue(EntityManager.Instance.Players.ContainsKey(client.Player.EntityId));
            Assert.AreSame(inventory, client.Player.Inventory);
            Assert.IsFalse(client.HandleMovement(new Rasa.Models.Movement(Vector3.One, Vector2.Zero)));

            maps.MapLoaded(client);

            Assert.IsNull(client.PendingTransfer);
            Assert.AreEqual(ClientState.Ingame, client.State);
            Assert.AreEqual(1, destination.ClientList.Count(member => member == client));
            Assert.IsTrue(CellManager.Instance.IsInWorld(client));
            Assert.AreSame(inventory, client.Player.Inventory);
        }

        [TestMethod]
        public void MapLinkTransferTimeoutRestoresTheOriginAndDisconnects()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var destination = DropshipTravelTests.CreateDestination();
            CellManager.Instance.AddToWorld(client);
            long now = 1000;
            var maps = new MapChannelManager(null, () => now,
                (_, _, _) => { }, current => current.State = ClientState.Disconnected);
            maps.MapChannelArray.Add(world.Map.MapInfo.MapContextId, world.Map);
            maps.MapChannelArray.Add(destination.MapInfo.MapContextId, destination);
            Assert.IsTrue(maps.ChangeMap(client, destination.MapInfo.MapContextId,
                new Vector3(400, 5, 0), 0));

            now = 60999;
            Assert.IsFalse(maps.CheckTransferTimeout(client));
            now = 61000;
            Assert.IsTrue(maps.CheckTransferTimeout(client));

            Assert.AreEqual(ClientState.Disconnected, client.State);
            Assert.AreSame(world.Map, client.Player.MapChannel);
            Assert.AreEqual(Vector3.Zero, client.Player.Position);
            Assert.IsNull(client.PendingTransfer);
            Assert.IsFalse(client.AwaitingMapLoaded);
        }

        [TestMethod]
        public void MapLinkPersistenceFailurePublishesNoDestinationState()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var observer = world.CreateClient();
            var destination = DropshipTravelTests.CreateDestination();
            world.Map.ClientList.Remove(observer);
            observer.Player.MapChannel = destination;
            observer.Player.MapContextId = destination.MapInfo.MapContextId;
            destination.ClientList.Add(observer);
            CellManager.Instance.AddToWorld(client);
            CellManager.Instance.AddToWorld(observer);
            var maps = new MapChannelManager(null,
                updateCharacter: (_, _, _) => throw new DbUpdateException("Fixture persistence failure."),
                disconnect: current => current.State = ClientState.Disconnected,
                refreshStats: (_, _) => { }, assignPlayer: _ => { }, enterMapChannels: _ => { });
            maps.MapChannelArray.Add(world.Map.MapInfo.MapContextId, world.Map);
            maps.MapChannelArray.Add(destination.MapInfo.MapContextId, destination);
            Assert.IsTrue(maps.ChangeMap(client, destination.MapInfo.MapContextId,
                new Vector3(400, 5, 0), 0));
            WorldTestContext.Drain(client);
            WorldTestContext.Drain(observer);

            maps.MapLoaded(client);

            Assert.AreEqual(ClientState.Disconnected, client.State);
            Assert.IsNull(client.PendingTransfer);
            Assert.AreSame(world.Map, client.Player.MapChannel);
            Assert.AreEqual(Vector3.Zero, client.Player.Position);
            Assert.IsFalse(destination.ClientList.Contains(client));
            Assert.IsFalse(destination.MapCellInfo.Cells.Values.Any(cell => cell.ClientList.Contains(client)));
            Assert.AreEqual(0, WorldTestContext.Drain(client).Count);
            Assert.AreEqual(0, WorldTestContext.Drain(observer).Count);
        }

        [TestMethod]
        public async Task MapLoadedWinsTimeoutRaceWithoutStaleCleanup()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var destination = DropshipTravelTests.CreateDestination();
            CellManager.Instance.AddToWorld(client);
            long now = 1000;
            var saves = 0;
            var disconnects = 0;
            var assignments = 0;
            using var saveStarted = new ManualResetEventSlim();
            using var releaseSave = new ManualResetEventSlim();
            var maps = new MapChannelManager(null, () => now,
                (_, _, _) =>
                {
                    Interlocked.Increment(ref saves);
                    saveStarted.Set();
                    Assert.IsTrue(releaseSave.Wait(TimeSpan.FromSeconds(5)));
                },
                _ => Interlocked.Increment(ref disconnects),
                (_, _) => { },
                _ => Interlocked.Increment(ref assignments),
                _ => { });
            maps.MapChannelArray.Add(world.Map.MapInfo.MapContextId, world.Map);
            maps.MapChannelArray.Add(destination.MapInfo.MapContextId, destination);
            Assert.IsTrue(maps.ChangeMap(client, destination.MapInfo.MapContextId,
                new Vector3(400, 5, 0), 0));

            var loaded = Task.Run(() => maps.MapLoaded(client));
            Assert.IsTrue(saveStarted.Wait(TimeSpan.FromSeconds(5)));
            now = 61000;
            var timedOut = Task.Run(() => maps.CheckTransferTimeout(client));
            Assert.IsFalse(timedOut.Wait(TimeSpan.FromMilliseconds(100)));
            releaseSave.Set();
            await Task.WhenAll(loaded, timedOut).WaitAsync(TimeSpan.FromSeconds(5));

            Assert.IsFalse(timedOut.Result);
            Assert.AreEqual(1, saves);
            Assert.AreEqual(0, disconnects);
            Assert.AreEqual(1, assignments);
            Assert.AreEqual(ClientState.Ingame, client.State);
            Assert.IsNull(client.PendingTransfer);
            Assert.AreEqual(1, destination.ClientList.Count(member => member == client));
            Assert.IsTrue(CellManager.Instance.IsInWorld(client));
        }

        [TestMethod]
        public async Task TimeoutSerializesWithCloseAndSavesOnce()
        {
            using var world = new WorldTestContext();
            var factory = new CountingFailureFactory();
            var client = world.CreateClient(factory: factory);
            var socket = new LengthedSocket(SizeType.Dword, false);
            typeof(Rasa.Game.Client).GetProperty(nameof(Rasa.Game.Client.Socket),
                BindingFlags.Instance | BindingFlags.Public).SetValue(client, socket);
            var destination = DropshipTravelTests.CreateDestination();
            CellManager.Instance.AddToWorld(client);
            long now = 1000;
            using var disconnectEntered = new ManualResetEventSlim();
            using var releaseDisconnect = new ManualResetEventSlim();
            var maps = new MapChannelManager(null, () => now, (_, _, _) => { },
                current =>
                {
                    disconnectEntered.Set();
                    Assert.IsTrue(releaseDisconnect.Wait(TimeSpan.FromSeconds(5)));
                    current.Close(false);
                });
            maps.MapChannelArray.Add(world.Map.MapInfo.MapContextId, world.Map);
            maps.MapChannelArray.Add(destination.MapInfo.MapContextId, destination);
            Assert.IsTrue(maps.ChangeMap(client, destination.MapInfo.MapContextId,
                new Vector3(400, 5, 0), 0));
            now = 61000;

            var timeout = Task.Run(() => maps.CheckTransferTimeout(client));
            Assert.IsTrue(disconnectEntered.Wait(TimeSpan.FromSeconds(5)));
            var close = Task.Run(() => client.Close(false));
            Assert.IsFalse(close.Wait(TimeSpan.FromMilliseconds(100)));
            releaseDisconnect.Set();
            await Task.WhenAll(timeout, close).WaitAsync(TimeSpan.FromSeconds(5));

            Assert.IsTrue(timeout.Result);
            Assert.AreEqual(ClientState.Disconnected, client.State);
            Assert.IsNull(client.PendingTransfer);
            Assert.AreSame(world.Map, client.Player.MapChannel);
            Assert.IsFalse(client.AwaitingMapLoaded);
            Assert.AreEqual(1, factory.SaveAttempts);
        }

        private sealed class CountingFailureFactory : IGameUnitOfWorkFactory
        {
            internal int SaveAttempts;

            public ICharUnitOfWork CreateChar()
            {
                Interlocked.Increment(ref SaveAttempts);
                throw new DbUpdateException("Fixture disconnect save failure.");
            }

            public IWorldUnitOfWork CreateWorld() =>
                throw new InvalidOperationException("Unexpected world database access.");
        }
    }
}
