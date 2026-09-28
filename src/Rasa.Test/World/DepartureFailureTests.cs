extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Game.Handlers;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Repositories.Char;
    using Rasa.Repositories.Char.Character;
    using Rasa.Repositories.Char.ClanMember;
    using Rasa.Repositories.Char.UserOption;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Repositories.World;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Packets.Protocol;

    [TestClass]
    [DoNotParallelize]
    public class DepartureFailureTests
    {
        [TestMethod]
        [DataRow(0)]
        [DataRow(17)]
        [DataRow(500)]
        public void ClanCleanupNormalizesTheInventoryToTheAuthoritativeLockboxSize(int count)
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            client.Player.ClanId = 7;
            client.Player.Inventory.ClanInventory = Enumerable.Repeat(42UL, count).ToList();

            ClanManager.Instance.CleanupClan(client);

            Assert.AreEqual(0U, client.Player.ClanId);
            Assert.AreEqual((int)ClanLockboxTab.TotalSlots, client.Player.Inventory.ClanInventory.Count);
            Assert.IsTrue(client.Player.Inventory.ClanInventory.All(id => id == 0));
        }

        [TestMethod]
        public void DisconnectRemovesEveryOwnedAutoFireTimerBeforeInventoryTeardown()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var other = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            var timers = AutoFireTimers();
            var first = Timer(client);
            var second = Timer(client);
            var retained = Timer(other);
            timers.AddRange(new[] { first, second, retained });
            try
            {
                client.State = ClientState.Disconnected;
                var maps = new MapChannelManager(null);
                maps.MapChannelArray.Add(1220, world.Map);

                maps.CleanupDisconnected(client);

                Assert.IsFalse(timers.Any(timer => timer.Client == client));
                Assert.IsTrue(timers.Contains(retained));
            }
            finally
            {
                timers.RemoveAll(timer => timer == first || timer == second || timer == retained);
            }
        }

        [TestMethod]
        public void LoadingClanMemberCanDisconnectBeforeItsClanInventoryIsInitialized()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            client.Player.ClanId = 700;
            client.State = ClientState.Disconnected;
            var maps = new MapChannelManager(null);
            maps.MapChannelArray.Add(1220, world.Map);

            maps.CleanupDisconnected(client);

            Assert.AreEqual(0U, client.Player.ClanId);
            Assert.IsNull(client.Player.MapChannel);
            Assert.AreEqual(0, client.Player.Inventory.ClanInventory.Count);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CachedClanDepartureCleansTheOldInventoryBeforeReset(bool logout)
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            client.Player.ClanId = 42;
            var oldInventory = client.Player.Inventory;
            oldInventory.ClanInventory = Enumerable.Repeat(42UL, 500).ToList();
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, new GameAccountEntry
            {
                Id = 123,
                FamilyName = "Fixture",
                Characters = new List<CharacterEntry>()
            });
            var characters = StrictProxy.Create<ICharacterRepository>(
                new Dictionary<string, object> { ["GetByAccountId"] = new Dictionary<byte, CharacterEntry>() },
                "UpdateCharacterPosition", "UpdateCharacterLogin");
            var options = StrictProxy.Create<IUserOptionRepository>(
                new Dictionary<string, object> { ["Get"] = new List<UserOptionEntry>() });
            var unit = StrictProxy.Create<ICharUnitOfWork>(new Dictionary<string, object>
            {
                ["get_Characters"] = characters,
                ["get_UserOptions"] = options
            }, "Dispose", "Complete", "Reject");
            var factory = StrictProxy.Create<IGameUnitOfWorkFactory>(
                new Dictionary<string, object> { ["CreateChar"] = unit });
            var clans = (ClanManager)Activator.CreateInstance(typeof(ClanManager),
                BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { factory }, null);
            var members = new List<ClanMemberEntry> { new ClanMemberEntry { ClanId = 42, CharacterId = client.Player.Id } };
            clans.ClanMembers[42] = new Lazy<List<ClanMemberEntry>>(members);
            clans.Clans[42] = new Lazy<ClanEntry>(() => new ClanEntry
            {
                Id = 42, Name = "Fixture", RankTitle0 = "0", RankTitle1 = "1", RankTitle2 = "2", RankTitle3 = "3"
            });
            var clanField = typeof(ClanManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var characterField = typeof(CharacterManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previousClans = clanField.GetValue(null);
            var previousCharacters = characterField.GetValue(null);
            var timer = Timer(client);
            var timers = AutoFireTimers();
            clanField.SetValue(null, clans);
            characterField.SetValue(null, new CharacterManager(factory));
            Server.Clients.Add(client);
            timers.Add(timer);
            try
            {
                var maps = new MapChannelManager(null);
                maps.MapChannelArray.Add(1220, world.Map);
                if (logout)
                {
                    client.State = ClientState.LoggedIn;
                    maps.RemovePlayer(client, true);
                    Assert.AreEqual(ClientState.CharacterSelection, client.State);
                }
                else
                {
                    client.State = ClientState.Disconnected;
                    maps.CleanupDisconnected(client);
                }

                Assert.IsTrue(oldInventory.ClanInventory.All(id => id == 0));
                Assert.AreNotSame(oldInventory, client.Player.Inventory);
                Assert.AreEqual(0U, client.Player.ClanId);
                Assert.AreEqual(0, members.Count);
                Assert.IsFalse(timers.Contains(timer));
                Assert.IsFalse(world.Map.ClientList.Contains(client));
            }
            finally
            {
                timers.Remove(timer);
                Server.Clients.Remove(client);
                clanField.SetValue(null, previousClans);
                characterField.SetValue(null, previousCharacters);
            }
        }

        [TestMethod]
        public void FailedRosterRefreshDoesNotPreventMandatoryDisconnectCleanup()
        {
            using var world = new WorldTestContext();
            var departing = world.CreateClient();
            var remaining = world.CreateClient();
            CellManager.Instance.AddToWorld(departing);
            CellManager.Instance.AddToWorld(remaining);
            WorldTestContext.Drain(remaining);
            departing.Player.ClanId = remaining.Player.ClanId = 42;
            departing.Player.Inventory.ClanInventory = Enumerable.Repeat(42UL, 500).ToList();
            var repository = StrictProxy.Create<IClanMemberRepository>(new Dictionary<string, object>
            {
                ["GetRosterEntry"] = new SqliteException("Fixture roster query failure.", 1)
            });
            var unit = StrictProxy.Create<ICharUnitOfWork>(
                new Dictionary<string, object> { ["get_ClanMembers"] = repository }, "Dispose");
            var factory = StrictProxy.Create<IGameUnitOfWorkFactory>(
                new Dictionary<string, object> { ["CreateChar"] = unit });
            var clans = (ClanManager)Activator.CreateInstance(typeof(ClanManager),
                BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { factory }, null);
            var members = new List<ClanMemberEntry>
            {
                new ClanMemberEntry { ClanId = 42, CharacterId = departing.Player.Id },
                new ClanMemberEntry { ClanId = 42, CharacterId = remaining.Player.Id }
            };
            clans.ClanMembers[42] = new Lazy<List<ClanMemberEntry>>(members);
            clans.Clans[42] = new Lazy<ClanEntry>(new ClanEntry { Id = 42, Name = "Fixture" });
            var field = typeof(ClanManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = field.GetValue(null);
            field.SetValue(null, clans);
            Server.Clients.Add(departing);
            Server.Clients.Add(remaining);
            try
            {
                departing.State = ClientState.Disconnected;
                world.Map.QueuedClients.Enqueue(departing);
                var maps = new MapChannelManager(null);
                maps.MapChannelArray.Add(1220, world.Map);

                maps.CleanupDisconnected(departing);

                Assert.IsTrue(((StrictProxy)(object)repository).Calls.Contains("GetRosterEntry"));
                Assert.IsFalse(world.Map.ClientList.Contains(departing));
                Assert.IsFalse(world.Map.QueuedClients.Contains(departing));
                Assert.IsFalse(world.Map.MapCellInfo.Cells.Values.Any(cell => cell.ClientList.Contains(departing)));
                Assert.IsFalse(EntityManager.Instance.Players.ContainsKey(departing.Player.EntityId));
                Assert.IsNull(departing.Player.MapChannel);
                Assert.AreEqual(0U, departing.Player.ClanId);
                Assert.AreEqual(1, members.Count);
                Assert.AreEqual(remaining.Player.Id, members[0].CharacterId);
                Assert.AreEqual(42U, remaining.Player.ClanId);
                Assert.IsTrue(world.Map.ClientList.Contains(remaining));
                Assert.IsFalse(WorldTestContext.Drain(remaining).Any(packet =>
                    packet.Message is CallMethodMessage call && call.MethodId == GameOpcode.ClanMembersRosterBegin));
            }
            finally
            {
                Server.Clients.Remove(departing);
                Server.Clients.Remove(remaining);
                field.SetValue(null, previous);
            }
        }

        [TestMethod]
        public void AutoFireWorkerRejectsDisconnectedClientsBeforeLookingUpTheirWeapon()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            client.State = ClientState.Disconnected;
            client.Player.WeaponReady = true;
            client.Player.Inventory.EquippedInventory.Clear();
            var timers = AutoFireTimers();
            var timer = Timer(client);
            timers.Add(timer);
            try
            {
                ManifestationManager.Instance.AutoFireTimerDoWork(100);

                Assert.IsFalse(timers.Contains(timer));
            }
            finally
            {
                timers.Remove(timer);
            }
        }

        [TestMethod]
        public void ProviderQueryFailureAbortsTheTransferAndRestoresOrigin()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            var manager = WaypointTravelTests.CreateManager(world, (_, update, _) =>
            {
                if (update == CharacterUpdate.Position)
                    throw new SqliteException("Fixture query failure.", 1);
            });
            WaypointTravelTests.AddWaypoint(manager, world.Map, 10, Vector3.Zero);
            WaypointTravelTests.AddWaypoint(manager, world.Map, 20, new Vector3(200, 0, 0));
            client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, 20, (byte)WaypointType.Waypoint));
            manager.SelectWaypoint(client, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });

            manager.TeleportAcknowledge(client);

            Assert.AreEqual(ClientState.Disconnected, client.State);
            Assert.AreEqual(Vector3.Zero, client.Player.Position);
            Assert.IsNull(client.PendingTransfer);
        }

        [TestMethod]
        public void DisconnectSaveReportsProviderFailureWithoutThrowing()
        {
            using var world = new WorldTestContext();
            var client = new Client(new FailingFactory(), new ClientPacketHandler());
            client.Player.Id = 1;
            try
            {
                Assert.IsFalse(client.SaveCharacterOnDisconnect());
            }
            finally
            {
                EntityManager.Instance.FreeEntity(client.Player.EntityId);
            }
        }

        [TestMethod]
        public void RealRepositoryQueryFailureAndFailingDisconnectSaveDoNotEscapeRollback()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                using var context = WaypointPersistenceTests.Open(Path.Combine(directory, "missing-schema"));
                var repository = new CharacterRepository(context);
                using var world = new WorldTestContext();
                var client = world.CreateClient(factory: new FailingFactory());
                CellManager.Instance.AddToWorld(client);
                var maps = new MapChannelManager(null);
                maps.MapChannelArray.Add(1220, world.Map);
                var saveWasAttempted = false;
                var manager = new DynamicObjectManager(null, maps, () => 1000,
                    (current, _, _) => repository.UpdateCharacterPosition(current.Player.Id,
                        current.Player.Position.X, current.Player.Position.Y, current.Player.Position.Z,
                        current.Player.Rotation, current.Player.MapContextId),
                    current =>
                    {
                        current.State = ClientState.Disconnected;
                        saveWasAttempted = true;
                        Assert.IsFalse(current.SaveCharacterOnDisconnect());
                    });
                WaypointTravelTests.AddWaypoint(manager, world.Map, 10, Vector3.Zero);
                WaypointTravelTests.AddWaypoint(manager, world.Map, 20, new Vector3(200, 0, 0));
                client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, 20, (byte)WaypointType.Waypoint));
                manager.SelectWaypoint(client, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });

                manager.TeleportAcknowledge(client);

                Assert.IsTrue(saveWasAttempted);
                Assert.AreEqual(ClientState.Disconnected, client.State);
                Assert.AreEqual(Vector3.Zero, client.Player.Position);
                Assert.IsNull(client.PendingTransfer);
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void UnexpectedDisconnectSaveErrorsRemainVisible()
        {
            using var world = new WorldTestContext();
            var client = new Client(new FailingFactory(new InvalidOperationException("Fixture application failure.")),
                new ClientPacketHandler());
            client.Player.Id = 1;
            try
            {
                Assert.ThrowsExactly<InvalidOperationException>(() => client.SaveCharacterOnDisconnect());
            }
            finally
            {
                EntityManager.Instance.FreeEntity(client.Player.EntityId);
            }
        }

        private static AutoFireTimer Timer(Client client) =>
            new AutoFireTimer(client, 100, 0) { MaxAliveTime = 10000 };

        private static List<AutoFireTimer> AutoFireTimers() =>
            (List<AutoFireTimer>)typeof(ManifestationManager)
                .GetField("AutoFire", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

        private sealed class FailingFactory : IGameUnitOfWorkFactory
        {
            private readonly Exception _failure;
            internal FailingFactory(Exception failure = null) =>
                _failure = failure ?? new SqliteException("Fixture disconnect save failure.", 1);
            public ICharUnitOfWork CreateChar() => throw _failure;
            public IWorldUnitOfWork CreateWorld() => throw new InvalidOperationException("Unexpected world database access.");
        }

        public class StrictProxy : DispatchProxy
        {
            private Dictionary<string, object> _values;
            private HashSet<string> _allowed;
            internal List<string> Calls { get; } = new();

            protected override object Invoke(MethodInfo method, object[] args)
            {
                Calls.Add(method.Name);
                if (_values.TryGetValue(method.Name, out var value))
                {
                    if (value is Exception error)
                        throw error;
                    return value;
                }
                if (_allowed.Contains(method.Name))
                    return null;
                throw new AssertFailedException($"Unexpected fixture call: {method.Name}.");
            }

            internal static T Create<T>(Dictionary<string, object> values, params string[] allowed) where T : class
            {
                var proxy = DispatchProxy.Create<T, StrictProxy>();
                var control = (StrictProxy)(object)proxy;
                control._values = values;
                control._allowed = new HashSet<string>(allowed);
                return proxy;
            }
        }
    }
}
