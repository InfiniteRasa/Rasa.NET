extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Api;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Clan.Server;
    using Rasa.Packets.Communicator.Both;
    using Rasa.Packets.Communicator.Client;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.LookingForGroup.Client;
    using Rasa.Packets.LookingForGroup.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Party.Client;
    using Rasa.Packets.Party.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Packets.Social.Server;
    using Rasa.Repositories.Char;
    using Rasa.Repositories.Char.GameAccount;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using StrictProxy = DepartureFailureTests.StrictProxy;

    /// <summary>
    /// .invisible on|off: a GM out of sight of every player who is not a GM (Detection.SetGmInvisible).
    /// Their entity leaves those players' clients and is not given to one who comes into range;
    /// creatures do not notice them and drop them from their tables; to those players they are
    /// offline - friends lists, clan rosters, /who, whispers, invitations and challenges by name,
    /// looking-for-group ads, GET /usersonline. GMs, of any level, see them throughout. It is off
    /// when the character comes into the world, and works alongside a cloak without either
    /// undoing the other.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class GmInvisibleTests
    {
        private static uint _nextAccount = 9700;

        private readonly List<Client> _clients = new List<Client>();
        private readonly List<uint> _clans = new List<uint>();

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var client in _clients)
            {
                LookingForGroupManager.Instance.RemovePlayer(client);

                lock (Server.Clients)
                    Server.Clients.Remove(client);
            }

            foreach (var id in _clans)
            {
                ClanManager.Instance.Clans.TryRemove(id, out _);
                ClanManager.Instance.ClanMembers.TryRemove(id, out _);
            }
        }

        #region In sight

        [TestMethod]
        public void GoingInvisibleTakesTheGmOffEveryPlayerButTheGmsAndBackAgain()
        {
            using var world = new WorldTestContext();
            var gm = Player(world, 0, 0, GmLevel.GameMaster);
            var player = Player(world, 5, 0);
            var squadMate = Player(world, 10, 0);
            var observer = Player(world, 15, 0, GmLevel.Observer);
            using var squad = Squad(gm, squadMate);
            Drain(gm, player, squadMate, observer);

            Assert.IsTrue(Detection.SetGmInvisible(gm, true));

            Assert.AreEqual(gm.Player.EntityId, Sent<DestroyPhysicalEntityPacket>(player).Single().EntityId, "taken off a player");
            Assert.AreEqual(gm.Player.EntityId, Sent<DestroyPhysicalEntityPacket>(squadMate).Single().EntityId, "and off a squad mate: not a squad cloak");
            Assert.IsEmpty(Packets(observer), "a GM goes on seeing them");
            Assert.IsTrue(Detection.IsHiddenFrom(gm.Player, player));
            Assert.IsFalse(Detection.IsHiddenFrom(gm.Player, observer));
            Assert.IsFalse(Detection.IsHiddenFrom(gm.Player, gm), "nor are they hidden from themselves");

            Assert.IsFalse(Detection.SetGmInvisible(gm, true), "already");
            Assert.IsEmpty(Packets(player));

            Assert.IsTrue(Detection.SetGmInvisible(gm, false));

            Assert.AreEqual(gm.Player.EntityId, Sent<CreatePhysicalEntityPacket>(player).Single().EntityId, "given back");
            Assert.AreEqual(gm.Player.EntityId, Sent<CreatePhysicalEntityPacket>(squadMate).Single().EntityId);
            Assert.IsEmpty(Sent<CreatePhysicalEntityPacket>(observer), "who never lost them");
            Assert.IsFalse(Detection.IsHiddenFrom(gm.Player, player));
        }

        [TestMethod]
        public void APlayerComingIntoRangeIsNotGivenAnInvisibleGmAndAGmIs()
        {
            using var world = new WorldTestContext();
            var gm = Player(world, 0, 0, GmLevel.GameMaster);
            var walker = Player(world, 900, 0);
            var gmWalker = Player(world, 900, 10, GmLevel.Observer);

            Detection.SetGmInvisible(gm, true);
            Drain(gm, walker, gmWalker);

            foreach (var client in new[] { walker, gmWalker })
            {
                client.Player.Position = new Vector3(5, 0, client == walker ? 0 : 5);
                CellManager.Instance.UpdateVisibility(client);
            }

            Assert.IsFalse(Sent<CreatePhysicalEntityPacket>(walker).Any(p => p.EntityId == gm.Player.EntityId), "not given to a player");
            Assert.IsTrue(Sent<CreatePhysicalEntityPacket>(gmWalker).Any(p => p.EntityId == gm.Player.EntityId), "given to a GM");
            Assert.IsTrue(Sent<CreatePhysicalEntityPacket>(gm).Any(p => p.EntityId == walker.Player.EntityId), "and the GM sees the player");
        }

        [TestMethod]
        public void CreaturesNeitherNoticeNorKeepFightingAnInvisibleGm()
        {
            using var world = new WorldTestContext();
            var gm = Player(world, 0, 0, GmLevel.GameMaster);
            var creature = Creature(world, 3);

            creature.Hate.Ensure(gm.Player.EntityId, 50);
            Assert.IsTrue(Detection.CanSee(creature, gm.Player));

            Detection.SetGmInvisible(gm, true);

            Assert.IsFalse(creature.Hate.Contains(gm.Player.EntityId), "off its table");
            Assert.IsFalse(Detection.CanSee(creature, gm.Player), "and not noticed");

            Detection.SetGmInvisible(gm, false);

            Assert.IsTrue(Detection.CanSee(creature, gm.Player));
        }

        [TestMethod]
        public void ACloakEndingOnAnInvisibleGmGivesThemBackToTheGmsOnly()
        {
            using var world = new WorldTestContext();
            var gm = Player(world, 0, 0, GmLevel.GameMaster);
            var player = Player(world, 5, 0);
            var observer = Player(world, 10, 0, GmLevel.Observer);

            Detection.SetGmInvisible(gm, true);
            Drain(gm, player, observer);

            Cloak(gm);
            Detection.Hide(world.Map, gm.Player);

            Assert.IsEmpty(Sent<DestroyPhysicalEntityPacket>(player), "already gone from a player's");
            Assert.AreEqual(gm.Player.EntityId, Sent<DestroyPhysicalEntityPacket>(observer).Single().EntityId, "the cloak takes them off a GM outside their squad");

            Uncloak(gm);
            Detection.Reveal(world.Map, gm.Player);

            Assert.IsEmpty(Sent<CreatePhysicalEntityPacket>(player), "still invisible to a player");
            Assert.AreEqual(gm.Player.EntityId, Sent<CreatePhysicalEntityPacket>(observer).Single().EntityId, "back for a GM");
        }

        [TestMethod]
        public void TheCloakEffectWearingOffAnInvisibleGmShowsThemToTheGmsItHadHiddenThemFrom()
        {
            using var world = new WorldTestContext();
            var gm = Player(world, 0, 0, GmLevel.GameMaster);
            var player = Player(world, 5, 0);
            var observer = Player(world, 10, 0, GmLevel.Observer);
            var now = Environment.TickCount64;
            var cloak = new GameEffect
            {
                TypeId = 1,
                EffectId = GameEffectManager.Instance.NextEffectId(world.Map),
                EffectLevel = 1,
                SourceId = gm.Player.EntityId,
                Source = gm.Player,
                SourceLevel = 1,
                Hides = true,
                ExpiresTick = now + 60000
            };

            Detection.SetGmInvisible(gm, true);
            GameEffectManager.Instance.Attach(world.Map, gm.Player, cloak);
            Drain(gm, player, observer);

            // Still invisible, so still hidden: but not cloaked, and that is what the effect's end asks.
            GameEffectManager.Instance.DettachEffect(world.Map, gm.Player, cloak);

            Assert.IsFalse(Sent<CreatePhysicalEntityPacket>(player).Any(p => p.EntityId == gm.Player.EntityId), "not to a player");
            Assert.IsTrue(Sent<CreatePhysicalEntityPacket>(observer).Any(p => p.EntityId == gm.Player.EntityId), "to the GM the cloak had taken them from");
        }

        [TestMethod]
        public void AGmTurningVisibleWhileCloakedIsNotGivenToThoseTheCloakHidesThemFrom()
        {
            using var world = new WorldTestContext();
            var gm = Player(world, 0, 0, GmLevel.GameMaster);
            var player = Player(world, 5, 0);
            var squadMate = Player(world, 10, 0);
            using var squad = Squad(gm, squadMate);

            Cloak(gm);
            Detection.Hide(world.Map, gm.Player);
            Drain(gm, player, squadMate);

            Detection.SetGmInvisible(gm, true);

            Assert.IsEmpty(Packets(player), "the cloak had them gone already");
            Assert.AreEqual(gm.Player.EntityId, Sent<DestroyPhysicalEntityPacket>(squadMate).Single().EntityId, "the squad's cloak does not keep them for an invisible GM");

            Detection.SetGmInvisible(gm, false);

            Assert.IsEmpty(Sent<CreatePhysicalEntityPacket>(player), "still cloaked from a player outside the squad");
            Assert.AreEqual(gm.Player.EntityId, Sent<CreatePhysicalEntityPacket>(squadMate).Single().EntityId, "the squad sees through its own cloak");
        }

        [TestMethod]
        public void TheCommandIsAGameMastersAndSaysWhatItDid()
        {
            using var world = new WorldTestContext();
            var gm = Player(world, 0, 0, GmLevel.GameMaster);
            var observer = Player(world, 5, 0, GmLevel.Observer);
            var commands = new ChatCommandsManager(null) { Audit = null };
            commands.RegisterChatCommands();
            Drain(gm, observer);

            Assert.IsFalse(gm.Player.GmInvisible, "off until it is turned on");

            commands.ProcessCommand(gm, ".invisible");
            StringAssert.StartsWith(Told(gm).Single(), "You are visible.");

            commands.ProcessCommand(gm, ".invisible on");
            Assert.IsTrue(gm.Player.GmInvisible);
            StringAssert.StartsWith(Told(gm).Single(), "You are invisible:");

            commands.ProcessCommand(gm, ".invisible on");
            Assert.AreEqual("You are already invisible.", Told(gm).Single());

            commands.ProcessCommand(gm, ".invisible maybe");
            Assert.AreEqual("usage: .invisible on|off", Told(gm).Single());
            Assert.IsTrue(gm.Player.GmInvisible);

            commands.ProcessCommand(gm, ".invisible off");
            Assert.IsFalse(gm.Player.GmInvisible);
            StringAssert.StartsWith(Told(gm).Single(), "You are visible again:");

            commands.ProcessCommand(observer, ".invisible on");
            Assert.IsFalse(observer.Player.GmInvisible, "an Observer may not");
            StringAssert.Contains(Told(observer).Single(), "needs account level 5");
        }

        #endregion

        #region Offline to players

        [TestMethod]
        public void FriendsWhoAreNotGmsSeeTheGmLogOffAndOnAndHearNothingInBetween()
        {
            using var world = new WorldTestContext();
            var gm = Player(world, 0, 0, GmLevel.GameMaster);
            var friend = Player(world, 5, 0);
            var gmFriend = Player(world, 10, 0, GmLevel.Observer);

            friend.Player.Friends.Add(gm.AccountEntry.Id);
            gmFriend.Player.Friends.Add(gm.AccountEntry.Id);
            gm.FriendsToldOnline = true;

            Detection.SetGmInvisible(gm, true);
            Drain(gm, friend, gmFriend);

            // ... as they were told at the time.
            SocialManager.Instance.GmVisibilityChanged(gm);
            Assert.AreEqual(gm.AccountEntry.Id, Sent<FriendLoggedOutPacket>(friend).Single().UserId, "logged off");
            Assert.IsEmpty(Packets(gmFriend), "a GM saw them online throughout");

            Assert.AreEqual(1, SocialManager.Instance.FriendStatusUpdate(gm), "a level or a map: to the GM only");
            Assert.IsEmpty(Packets(friend));
            Assert.HasCount(1, Sent<FriendStatusUpdatePacket>(gmFriend));

            SocialManager.Instance.FriendLoggedIn(gm);
            Assert.IsEmpty(Packets(friend), "nor is it said they came online");

            SocialManager.Instance.FriendLoggedOut(gm);
            Assert.IsEmpty(Packets(friend), "nor that they went offline: they already had");
            Assert.HasCount(1, Sent<FriendLoggedOutPacket>(gmFriend));

            Detection.SetGmInvisible(gm, false);
            var online = Sent<FriendLoggedInPacket>(friend).Single();
            Assert.AreEqual(gm.AccountEntry.Id, online.Friend.UserId, "online again");
            Assert.IsTrue(online.Friend.IsOnline);
            Assert.IsEmpty(Packets(gmFriend));
        }

        [TestMethod]
        public void AFriendsListRowForAnInvisibleGmIsTheOfflineOne()
        {
            using var world = new WorldTestContext();
            var gm = Player(world, 0, 0, GmLevel.GameMaster);
            var player = Player(world, 5, 0);
            var observer = Player(world, 10, 0, GmLevel.Observer);
            var social = Social(new GameAccountEntry { Id = gm.AccountEntry.Id, FamilyName = gm.AccountEntry.FamilyName });

            Assert.IsTrue(social.GetFriendById(gm.AccountEntry.Id, player).IsOnline);

            Detection.SetGmInvisible(gm, true);

            Assert.IsFalse(social.GetFriendById(gm.AccountEntry.Id, player).IsOnline, "offline, from the database");
            Assert.IsTrue(social.GetFriendById(gm.AccountEntry.Id, observer).IsOnline, "online to a GM");
            Assert.IsFalse(social.GetIgnoredById(gm.AccountEntry.Id, player).IsOnline);
        }

        [TestMethod]
        public void TheClanRosterShowsAnInvisibleGmOfflineToThoseWhoAreNotGms()
        {
            using var world = new WorldTestContext();
            var gm = Player(world, 0, 0, GmLevel.GameMaster);
            var member = Player(world, 5, 0);
            var gmMember = Player(world, 10, 0, GmLevel.Observer);
            Clan(gm, member, gmMember);
            Drain(gm, member, gmMember);

            Detection.SetGmInvisible(gm, true);

            Assert.IsFalse(Line(member, gm).IsOnline, "offline to a member");
            Assert.IsTrue(Line(gmMember, gm).IsOnline, "online to a GM in the clan");

            Detection.SetGmInvisible(gm, false);

            Assert.IsTrue(Line(member, gm).IsOnline, "online again");
            Assert.IsTrue(Line(gmMember, gm).IsOnline);
        }

        [TestMethod]
        public void WhoAndWhispersFindAnInvisibleGmOnlyForAGmOrForWhomeverTheyWhispered()
        {
            using var world = new WorldTestContext();
            var chat = CommunicatorManager.Instance;
            var gm = Player(world, 0, 0, GmLevel.GameMaster);
            var player = Player(world, 5, 0);
            var observer = Player(world, 10, 0, GmLevel.Observer);

            Detection.SetGmInvisible(gm, true);
            Drain(gm, player, observer);

            chat.Who(player, new WhoPacket { SearchText = gm.Player.FamilyName });
            Assert.HasCount(1, Sent<WhoFailAckPacket>(player), "not in /who");

            chat.Who(observer, new WhoPacket { SearchText = gm.Player.FamilyName });
            Assert.AreEqual(gm.Player.FamilyName, Sent<WhoAckPacket>(observer).Single().FamilyName, "but a GM finds them");

            chat.Whisper(player, new WhisperPacket { Reciver = gm.Player.FamilyName, Message = "hello?" });
            Assert.AreEqual(PlayerMessage.PmWhisperTargetNotInGame, Sent<WhisperFailAckPacket>(player).Single().MessageId, "not in game");
            Assert.IsEmpty(Sent<WhisperPacket>(gm));

            chat.Whisper(observer, new WhisperPacket { Reciver = gm.Player.FamilyName, Message = "status?" });
            Assert.AreEqual("status?", Sent<WhisperPacket>(gm).Single().Message, "a GM reaches them");

            // Whispered by the GM: the answer reaches them.
            chat.Whisper(gm, new WhisperPacket { Reciver = player.Player.FamilyName, Message = "a GM here" });
            Drain(player);
            chat.Whisper(player, new WhisperPacket { Reciver = gm.Player.FamilyName, Message = "thanks" });
            Assert.AreEqual("thanks", Sent<WhisperPacket>(gm).Single().Message);

            // Until they go visible and invisible again.
            Detection.SetGmInvisible(gm, false);
            Detection.SetGmInvisible(gm, true);
            Drain(gm, player);
            chat.Whisper(player, new WhisperPacket { Reciver = gm.Player.FamilyName, Message = "still there?" });
            Assert.HasCount(1, Sent<WhisperFailAckPacket>(player));
        }

        [TestMethod]
        public void AnInvitationOrAChallengeByNameFindsNoInvisibleGm()
        {
            using var world = new WorldTestContext();
            var gm = Player(world, 0, 0, GmLevel.GameMaster);
            var player = Player(world, 5, 0);
            var observer = Player(world, 10, 0, GmLevel.Observer);

            Detection.SetGmInvisible(gm, true);
            Drain(gm, player, observer);

            PartyManager.Instance.InviteUserToPartyByName(player, new InviteUserToPartyByNamePacket { FamilyName = gm.Player.FamilyName });
            Duels.Instance.ChallengeUserToWargameByName(player, gm.Player.FamilyName, 5, 5);
            SummonManager.Instance.InviteFriendToJoin(player, new Rasa.Packets.Summon.Client.InviteFriendToJoinPacket { PlayerName = gm.Player.FamilyName });

            Assert.IsEmpty(Packets(gm), "nothing reaches them");
            Assert.IsFalse(PartyManager.Instance.IsInvited(gm));

            // A GM can invite them.
            PartyManager.Instance.InviteUserToPartyByName(observer, new InviteUserToPartyByNamePacket { FamilyName = gm.Player.FamilyName });
            Assert.HasCount(1, Sent<InviteToPartyPacket>(gm));
            PartyManager.Instance.PartyInvitationResponse(gm, new PartyInvitationResponsePacket { Response = false });
        }

        [TestMethod]
        public void AnInvisibleGmsAdIsNotInAPlayersSearch()
        {
            using var world = new WorldTestContext();
            var gm = Player(world, 0, 0, GmLevel.GameMaster);
            var player = Player(world, 5, 0);
            var observer = Player(world, 10, 0, GmLevel.Observer);
            var groups = LookingForGroupManager.Instance;
            var ad = new RequestCreateLookingForGroupAdPacket();
            ad.AdInfo.MinLevel = 1;
            ad.AdInfo.MaxLevel = 50;

            groups.RequestCreateLookingForGroupAd(gm, ad);
            Detection.SetGmInvisible(gm, true);
            Drain(gm, player, observer);

            groups.RequestLookingForGroupSearch(player, new RequestLookingForGroupSearchPacket());
            groups.RequestLookingForGroupSearch(observer, new RequestLookingForGroupSearchPacket());

            Assert.IsFalse(Sent<LookingForGroupSearchResultsPacket>(player).Single().Results.Any(r => r.UserName == gm.Player.FamilyName), "not to a player");
            Assert.IsTrue(Sent<LookingForGroupSearchResultsPacket>(observer).Single().Results.Any(r => r.UserName == gm.Player.FamilyName), "to a GM");
        }

        [TestMethod]
        public void AnInvisibleGmIsNotInTheOnlineList()
        {
            using var world = new WorldTestContext();
            var gm = Player(world, 0, 0, GmLevel.GameMaster);
            var player = Player(world, 5, 0);
            var clients = new List<Client> { gm, player };

            CollectionAssert.AreEquivalent(new[] { gm.AccountEntry.Id, player.AccountEntry.Id }, OnlineUsers.Read(clients).Select(u => u.AccountId).ToList());

            Detection.SetGmInvisible(gm, true);

            CollectionAssert.AreEqual(new[] { player.AccountEntry.Id }, OnlineUsers.Read(clients).Select(u => u.AccountId).ToList());
        }

        #endregion

        #region Fixture

        private Client Player(WorldTestContext world, float x, float z, GmLevel level = GmLevel.Player)
        {
            var client = PlayerDeathTests.Player(world, x, z);
            var id = ++_nextAccount;

            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client,
                new GameAccountEntry { Id = id, FamilyName = $"Hidden{id}", Level = (byte)level, Characters = new List<CharacterEntry>() });
            client.Player.FamilyName = $"Hidden{id}";
            client.Player.Id = id;

            lock (Server.Clients)
                Server.Clients.Add(client);

            _clients.Add(client);
            WorldTestContext.Drain(client);

            return client;
        }

        /// <summary>A squad of these, without TestSquad's own accounts.</summary>
        private static IDisposable Squad(params Client[] members)
        {
            var id = PartyManager.Instance.GetPartyId;
            var party = new Party(id, members[0].AccountEntry.Id, members.Select(c => new PartyMember(c)).ToList());

            PartyManager.Instance.Parties[id] = party;

            foreach (var client in members)
                client.Player.PartyId = id;

            return new Undo(() =>
            {
                if (PartyManager.Instance.Parties.Remove(id))
                    PartyManager.Instance.FreePartyId(id);
            });
        }

        private void Clan(params Client[] members)
        {
            var id = 970000u + members[0].AccountEntry.Id;
            var clan = new ClanEntry { Id = id, Name = $"Hidden Clan {id}" };

            ClanManager.Instance.Clans[id] = new Lazy<ClanEntry>(() => clan);
            ClanManager.Instance.ClanMembers[id] = new Lazy<List<ClanMemberEntry>>(() =>
                members.Select(m => new ClanMemberEntry { ClanId = id, CharacterId = m.Player.Id, Rank = 0 }).ToList());
            _clans.Add(id);

            foreach (var member in members)
                member.Player.ClanId = id;
        }

        /// <summary>The roster line about one member the reader was last sent.</summary>
        private static ClanMemberData Line(Client reader, Client about)
        {
            return Sent<SetClanMemberDataPacket>(reader).Last(p => p.Data.CharacterId == about.Player.Id).Data;
        }

        private static void Cloak(Client client) =>
            client.Player.ActiveEffects[Cloaked] = new GameEffect { EffectId = Cloaked, Hides = true };

        private static void Uncloak(Client client) => client.Player.ActiveEffects.Remove(Cloaked);

        private const int Cloaked = 987654;

        private static Creature Creature(WorldTestContext world, float x)
        {
            var creature = new Creature
            {
                Name = "Watcher",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(x, 0, 0),
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);

            return creature;
        }

        /// <summary>A social manager whose database has this one account in it.</summary>
        private static SocialManager Social(GameAccountEntry account)
        {
            var accounts = StrictProxy.Create<IGameAccountRepository>(new Dictionary<string, object> { ["Find"] = account });
            var unit = StrictProxy.Create<ICharUnitOfWork>(new Dictionary<string, object> { ["get_GameAccounts"] = accounts }, "Dispose");
            var factory = StrictProxy.Create<IGameUnitOfWorkFactory>(new Dictionary<string, object> { ["CreateChar"] = unit });

            return (SocialManager)typeof(SocialManager).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(IGameUnitOfWorkFactory) }, null).Invoke(new object[] { factory });
        }

        private static List<PythonPacket> Packets(Client client) =>
            WorldTestContext.Drain(client).Select(p => p.Message).OfType<CallMethodMessage>().Select(m => m.Packet).ToList();

        private static List<T> Sent<T>(Client client) where T : PythonPacket => Packets(client).OfType<T>().ToList();

        private static List<string> Told(Client client) => Sent<SystemMessagePacket>(client).Select(p => p.TextMessage).ToList();

        private static void Drain(params Client[] clients)
        {
            foreach (var client in clients)
                WorldTestContext.Drain(client);
        }

        private sealed class Undo : IDisposable
        {
            private readonly Action _undo;

            public Undo(Action undo) => _undo = undo;

            public void Dispose() => _undo();
        }

        #endregion
    }
}
