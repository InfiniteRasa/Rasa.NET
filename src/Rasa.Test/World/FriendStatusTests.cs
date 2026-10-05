extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Protocol;
    using Rasa.Packets.Social.Server;
    using Rasa.Repositories.Char;
    using Rasa.Repositories.Char.Character;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Gameplay;
    using Rasa.Test.Missions;
    using StrictProxy = DepartureFailureTests.StrictProxy;

    /// <summary>
    /// What an account's friends are sent (SocialManager, CommunicatorManager.PlayerEnterMap):
    /// that it is online once, on entering the world; and its row in their lists as it now
    /// reads, with nothing said, when it arrives on another map, changes level or is renamed.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class FriendStatusTests
    {
        private const uint MoverAccount = 9501;

        private readonly List<Client> _clients = new List<Client>();

        [TestCleanup]
        public void OutOfTheWorld()
        {
            foreach (var client in _clients)
            {
                CommunicatorManager.Instance.LeaveMapChannels(client);

                lock (Server.Clients)
                    Server.Clients.Remove(client);
            }
        }

        [TestMethod]
        public void FriendsAreToldSomebodyIsOnlineOnceAndOfEveryMapAfterWithNothingSaid()
        {
            using var world = new WorldTestContext();
            var chat = CommunicatorManager.Instance;
            var mover = Online(world.CreateClient(), MoverAccount);
            var friend = Online(world.CreateClient(), 9502, friends: MoverAccount);
            var stranger = Online(world.CreateClient(), 9503);
            var selecting = Online(world.CreateClient(), 9504, friends: MoverAccount);

            selecting.State = ClientState.CharacterSelection;
            mover.Player.Level = 12;

            // Into the world: online, once.
            chat.PlayerEnterMap(mover);

            var packets = Packets(friend);
            var online = (FriendLoggedInPacket)packets.Single();

            Assert.AreEqual(MoverAccount, online.Friend.UserId);
            Assert.AreEqual(mover.Player.Name, online.Friend.CharacterName);
            Assert.AreEqual(12u, online.Friend.Level);
            Assert.AreEqual(world.Map.MapInfo.MapContextId, online.Friend.ContextId);
            Assert.IsTrue(online.Friend.IsOnline);
            Assert.IsEmpty(Packets(stranger), "not on their list");
            Assert.IsEmpty(Packets(selecting), "not in the world");
            Assert.IsEmpty(Packets(mover).Where(packet => packet is FriendLoggedInPacket || packet is FriendStatusUpdatePacket).ToList(), "nobody is told of themselves");

            // To another map: the row with its new map, and no "is online" a second time.
            var elsewhere = new MapChannel { MapInfo = new MapInfo(1148, "adv_foreas_concordia_divide", 1, 0), ClientList = new List<Client>() };

            chat.LeaveMapChannels(mover);
            mover.Player.MapChannel = elsewhere;
            mover.Player.MapContextId = 1148;
            chat.PlayerEnterMap(mover);

            var moved = (FriendStatusUpdatePacket)Packets(friend).Single();

            Assert.AreEqual(MoverAccount, moved.Friend.UserId);
            Assert.AreEqual(1148u, moved.Friend.ContextId);
            Assert.AreEqual(12u, moved.Friend.Level);
            Assert.IsTrue(moved.Friend.IsOnline);
            Assert.IsEmpty(Packets(stranger));
            Assert.IsEmpty(Packets(selecting));

            // And another.
            chat.LeaveMapChannels(mover);
            mover.Player.MapChannel = world.Map;
            mover.Player.MapContextId = world.Map.MapInfo.MapContextId;
            chat.PlayerEnterMap(mover);
            Assert.AreEqual(world.Map.MapInfo.MapContextId, ((FriendStatusUpdatePacket)Packets(friend).Single()).Friend.ContextId);

            // Out of the world: offline. And in again: online again.
            using (CharacterSaves())
                chat.PlayerExitMap(mover);

            Assert.AreEqual(MoverAccount, ((FriendLoggedOutPacket)Packets(friend).Single()).UserId);
            Assert.IsEmpty(Packets(stranger));

            chat.PlayerEnterMap(mover);
            Assert.IsInstanceOfType<FriendLoggedInPacket>(Packets(friend).Single());
        }

        [TestMethod]
        public void AStatusUpdateGoesToThoseWhoHaveTheAccountAsAFriendAndAreInTheWorld()
        {
            using var world = new WorldTestContext();
            var social = SocialManager.Instance;
            var mover = Online(world.CreateClient(), MoverAccount);
            var friend = Online(world.CreateClient(), 9502, friends: MoverAccount);
            var loading = Online(world.CreateClient(), 9503, friends: MoverAccount);
            var gone = Online(world.CreateClient(), 9504, friends: MoverAccount);
            var nobody = world.CreateClient();

            loading.State = ClientState.Loading;
            gone.State = ClientState.Disconnected;

            Assert.AreEqual(2, social.FriendStatusUpdate(mover));
            Assert.IsInstanceOfType<FriendStatusUpdatePacket>(Packets(friend).Single());
            Assert.IsInstanceOfType<FriendStatusUpdatePacket>(Packets(loading).Single(), "on their way in");
            Assert.IsEmpty(Packets(gone));
            Assert.IsEmpty(Packets(mover));

            // Friendship is one way: the mover has nobody on their own list.
            Assert.AreEqual(0, social.FriendStatusUpdate(friend));
            Assert.IsEmpty(Packets(mover));

            // A client with no account, and none at all.
            Assert.AreEqual(0, social.FriendStatusUpdate(nobody));
            Assert.AreEqual(0, social.FriendStatusUpdate(null));
        }

        [TestMethod]
        public void ALevelGainedOrLostReachesTheFriendsLists()
        {
            using var context = new WeaponAmmoContext();
            using var world = new WorldTestContext();
            var mover = context.Client;
            var friend = Online(world.CreateClient(), 9502, friends: mover.AccountEntry.Id);
            var manager = new ManifestationManager(context);

            // Lowering a level saves through the server's character manager: this database's.
            using var characters = new Swap(CharacterManagerInstance, new CharacterManager(context));

            lock (Server.Clients)
                Server.Clients.Add(mover);

            _clients.Add(mover);
            mover.Player.Attributes = Enum.GetValues<Attributes>().ToDictionary(attribute => attribute, attribute => new ActorAttributes(attribute, 0, 0, 0, 0, 0));
            Assert.AreEqual(1, (int)mover.Player.Level);

            // Up, as experience raises a level.
            manager.SetLevel(mover, 3);
            Assert.AreEqual(3, (int)mover.Player.Level);

            var raised = Sent<FriendStatusUpdatePacket>(friend);

            Assert.IsGreaterThan(0, raised.Count);
            Assert.AreEqual(3u, raised.Last().Friend.Level);
            Assert.AreEqual(mover.AccountEntry.Id, raised.Last().Friend.UserId);

            // Down, as a game master's .setlevel lowers one.
            manager.SetLevel(mover, 2);
            Assert.AreEqual(2u, Sent<FriendStatusUpdatePacket>(friend).Single().Friend.Level);

            // A level that is not a change says nothing.
            manager.SetLevel(mover, 2);
            Assert.IsEmpty(Packets(friend));
        }

        [TestMethod]
        public void ANewNameReachesTheFriendsLists()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            using var world = new WorldTestContext();
            var mover = harness.Client;
            var friend = Online(world.CreateClient(), 9502, friends: mover.AccountEntry.Id);
            var family = mover.Player.FamilyName;
            var characters = new CharacterManager(harness.Context);

            lock (Server.Clients)
                if (!Server.Clients.Contains(mover))
                {
                    Server.Clients.Add(mover);
                    _clients.Add(mover);
                }

            // The character's.
            Assert.IsTrue(characters.Rename(mover, mover, "Renamed", false));

            var renamed = Sent<FriendStatusUpdatePacket>(friend).Single().Friend;

            Assert.AreEqual("Renamed", renamed.CharacterName);
            Assert.AreEqual(family, renamed.FamilyName);
            Assert.AreEqual(mover.AccountEntry.Id, renamed.UserId);

            // And the family's.
            Assert.IsTrue(characters.Rename(mover, mover, "Newfamily", true));
            Assert.AreEqual("Newfamily", Sent<FriendStatusUpdatePacket>(friend).Single().Friend.FamilyName);

            // A name that is refused changes no row.
            Assert.IsFalse(characters.Rename(mover, mover, "x", false));
            Assert.IsEmpty(Sent<FriendStatusUpdatePacket>(friend));
        }

        /// <summary>A client with this account, friends with those accounts, among the server's clients.</summary>
        private Client Online(Client client, uint accountId, params uint[] friends)
        {
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, new GameAccountEntry { Id = accountId, FamilyName = $"Family{accountId}" });
            client.Player.Friends.AddRange(friends);

            lock (Server.Clients)
                Server.Clients.Add(client);

            _clients.Add(client);
            WorldTestContext.Drain(client);

            return client;
        }

        /// <summary>A character manager whose saves of position and time played go nowhere, in the server's place until disposed.</summary>
        private static IDisposable CharacterSaves()
        {
            var characters = StrictProxy.Create<ICharacterRepository>(new Dictionary<string, object>(), "UpdateCharacterPosition", "UpdateCharacterLogin");
            var unit = StrictProxy.Create<ICharUnitOfWork>(new Dictionary<string, object> { ["get_Characters"] = characters }, "Dispose", "Complete", "Reject");
            var factory = StrictProxy.Create<IGameUnitOfWorkFactory>(new Dictionary<string, object> { ["CreateChar"] = unit });

            return new Swap(CharacterManagerInstance, new CharacterManager(factory));
        }

        private static readonly FieldInfo CharacterManagerInstance = typeof(CharacterManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);

        private sealed class Swap : IDisposable
        {
            private readonly FieldInfo _field;
            private readonly object _previous;

            internal Swap(FieldInfo field, object value)
            {
                _field = field;
                _previous = field.GetValue(null);
                field.SetValue(null, value);
            }

            public void Dispose() => _field.SetValue(null, _previous);
        }

        private static List<PythonPacket> Packets(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet).ToList();

        private static List<T> Sent<T>(Client client) where T : PythonPacket => Packets(client).OfType<T>().ToList();
    }
}
