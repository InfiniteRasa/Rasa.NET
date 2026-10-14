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
    using Rasa.Packets.Clan.Client;
    using Rasa.Packets.Clan.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    /// <summary>
    /// Whoever sends a clan invitation hears what came of it. The invitee's client prompts them in
    /// its own words (PM_CLAN_INVITED_TO_JOIN_PVE/_PVP, client/clan.py); the inviter was told
    /// nothing: not that it went, not that it was taken up, not that it was turned down. They are
    /// now told "You have invited %(playername)s to join %(clanname)s", and then "%(playername)s
    /// accepted your clan invitation." or "... declined ...".
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ClanInvitationTests
    {
        private const uint InviteeId = 2;

        private readonly List<Client> _online = new List<Client>();

        [TestCleanup]
        public void Cleanup()
        {
            lock (Server.Clients)
                foreach (var client in _online)
                    Server.Clients.Remove(client);

            _online.Clear();
        }

        [TestMethod]
        public void TheInviterIsToldTheInvitationWent()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var (inviter, invitee, clan) = Fixture(context);

            WithLiveClans(context, clan, clans =>
            {
                clans.InviteToClanById(inviter, new InviteToClanByIdPacket { CharacterEntityId = invitee.Player.EntityId });

                Assert.HasCount(1, Sent<InviteToClanPacket>(invitee), "the invitee is asked");

                var told = Messages(inviter).Single();
                Assert.AreEqual((int)PlayerMessage.PmClanPlayerInvited, told.MsgId);
                Assert.AreEqual(Name(invitee), told.Args["playername"]);
                Assert.AreEqual("Fixture Clan", told.Args["clanname"]);
            });
        }

        [TestMethod]
        public void ADeclineIsToldToTheInviterAndLeavesTheInviteeOut()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var (inviter, invitee, clan) = Fixture(context);

            WithLiveClans(context, clan, clans =>
            {
                clans.InviteToClanById(inviter, new InviteToClanByIdPacket { CharacterEntityId = invitee.Player.EntityId });
                Drain(inviter, invitee);

                clans.ClanInvitationResponse(invitee, Answer(invitee, clan, accepted: false));

                var told = Messages(inviter).Single();
                Assert.AreEqual((int)PlayerMessage.PmClanInvitationRefused, told.MsgId);
                Assert.AreEqual(Name(invitee), told.Args["playername"]);
                Assert.IsNull(clans.GetClanMember(clan.Id, InviteeId), "not in the clan");
                Assert.IsEmpty(Messages(invitee), "nothing for the one who declined");

                // Spent: a second answer is refused.
                clans.ClanInvitationResponse(invitee, Answer(invitee, clan, accepted: true));
                Assert.AreEqual((int)PlayerMessage.PmClanNotInvited, Messages(invitee).Single().MsgId);
                Assert.IsEmpty(Messages(inviter));
            });
        }

        [TestMethod]
        public void AnAcceptIsToldToTheInviter()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var (inviter, invitee, clan) = Fixture(context);

            WithLiveClans(context, clan, clans =>
            {
                clans.InviteToClanById(inviter, new InviteToClanByIdPacket { CharacterEntityId = invitee.Player.EntityId });
                Drain(inviter, invitee);

                clans.ClanInvitationResponse(invitee, Answer(invitee, clan, accepted: true));

                Assert.IsNotNull(clans.GetClanMember(clan.Id, InviteeId), "in the clan");
                Assert.AreEqual(clan.Id, invitee.Player.ClanId);

                var calls = Calls(inviter);
                Assert.HasCount(1, calls.Select(c => c.Packet).OfType<PlayerJoinedClanPacket>().ToList(), "the roster's new line, as every member gets");

                var told = calls.Select(c => c.Packet).OfType<DisplayClanMessagePacket>().Single();
                Assert.AreEqual((int)PlayerMessage.PmClanInvitationAccepted, told.MsgId);
                Assert.AreEqual(Name(invitee), told.Args["playername"]);
            });
        }

        [TestMethod]
        public void AnInviterWhoIsNoLongerInTheWorldIsToldNothingAndTheAnswerStands()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var (inviter, invitee, clan) = Fixture(context);

            WithLiveClans(context, clan, clans =>
            {
                clans.InviteToClanById(inviter, new InviteToClanByIdPacket { CharacterEntityId = invitee.Player.EntityId });
                Drain(inviter, invitee);

                lock (Server.Clients)
                    Server.Clients.Remove(inviter);

                clans.ClanInvitationResponse(invitee, Answer(invitee, clan, accepted: true));

                Assert.IsNotNull(clans.GetClanMember(clan.Id, InviteeId), "joined all the same");
                Assert.IsEmpty(Messages(inviter));
            });
        }

        [TestMethod]
        public void AnInviteByNameReachesSomeoneInTheWorld()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var (inviter, invitee, clan) = Fixture(context);

            WithLiveClans(context, clan, clans =>
            {
                clans.InviteToClanByName(inviter, new InviteToClanByNamePacket { FamilyName = $"Family {InviteeId}" });

                Assert.HasCount(1, Sent<InviteToClanPacket>(invitee));
                Assert.AreEqual((int)PlayerMessage.PmClanPlayerInvited, Messages(inviter).Single().MsgId);
            });
        }

        /// <summary>
        /// Someone not in the world - logged out, or back at character selection - cannot be asked,
        /// and the inviter is told so: "%(name)s is not online." It was silent.
        /// </summary>
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void AnInviteByNameToSomeoneNotInTheWorldIsToldTheyAreNotOnline(bool atCharacterSelection)
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var (inviter, invitee, clan) = Fixture(context);

            if (atCharacterSelection)
                invitee.State = ClientState.CharacterSelection;
            else
                lock (Server.Clients)
                    Server.Clients.Remove(invitee);

            WithLiveClans(context, clan, clans =>
            {
                clans.InviteToClanByName(inviter, new InviteToClanByNamePacket { FamilyName = $"Family {InviteeId}" });

                Assert.IsEmpty(Sent<InviteToClanPacket>(invitee));
                var told = Messages(inviter).Single();
                Assert.AreEqual((int)PlayerMessage.PmUserNotOnline, told.MsgId);
                Assert.AreEqual($"Character {InviteeId} Family {InviteeId}", told.Args["name"]);
            });
        }

        /// <summary>"There is nobody named %(name)s."</summary>
        [TestMethod]
        public void AnInviteByAFamilyNameNobodyHasIsToldSo()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var (inviter, _, clan) = Fixture(context);

            WithLiveClans(context, clan, clans =>
            {
                clans.InviteToClanByName(inviter, new InviteToClanByNamePacket { FamilyName = "Nobody" });

                var told = Messages(inviter).Single();
                Assert.AreEqual((int)PlayerMessage.PmNoSuchUser, told.MsgId);
                Assert.AreEqual("Nobody", told.Args["name"]);
            });
        }

        /// <summary>An entity id that is no player in the world: "Target user is not online or does not exist." It was silent.</summary>
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void AnInviteByAnIdThatIsNoPlayerInTheWorldIsToldSo(bool gone)
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var (inviter, invitee, clan) = Fixture(context);
            var entityId = gone ? invitee.Player.EntityId : 0xDEADBEEFUL;

            if (gone)
                lock (Server.Clients)
                    Server.Clients.Remove(invitee);

            WithLiveClans(context, clan, clans =>
            {
                clans.InviteToClanById(inviter, new InviteToClanByIdPacket { CharacterEntityId = entityId });

                Assert.IsEmpty(Sent<InviteToClanPacket>(invitee));
                var told = Messages(inviter).Single();
                Assert.AreEqual((int)PlayerMessage.PmNoTargetUser, told.MsgId);
                Assert.IsEmpty(told.Args);
            });
        }

        private (Client Inviter, Client Invitee, ClanEntry Clan) Fixture(MissionTestContext context)
        {
            var inviter = context.Client;
            var invitee = context.CreateAdditionalClient(InviteeId);
            var clan = Clan(context, "Fixture Clan", (inviter.Player.Id, ClanRank.Leader));

            inviter.Player.ClanId = clan.Id;
            inviter.Player.Inventory.ResetClanInventory();
            invitee.Player.Inventory.ResetClanInventory();

            foreach (var client in new[] { inviter, invitee })
            {
                client.State = ClientState.Ingame;

                lock (Server.Clients)
                    Server.Clients.Add(client);

                _online.Add(client);
            }

            Drain(inviter, invitee);
            return (inviter, invitee, clan);
        }

        private static ClanInvitationResponsePacket Answer(Client invitee, ClanEntry clan, bool accepted) => new ClanInvitationResponsePacket
        {
            InvitedCharacterEntityId = invitee.Player.EntityId,
            Accepted = accepted,
            ClanId = clan.Id
        };

        private static string Name(Client client) => $"{client.Player.Name} {client.Player.FamilyName}";

        private static ClanEntry Clan(MissionTestContext context, string name, params (uint CharacterId, byte Rank)[] members)
        {
            using var unit = context.CreateChar();
            var clan = unit.Clans.CreateClan(name, false);

            foreach (var (characterId, rank) in members)
                Assert.IsTrue(unit.ClanMembers.InsertClanMemberData(clan.Id, characterId, rank, ""));

            return clan;
        }

        /// <summary>A clan manager over the context's database with the clan cached, as it is on a running server, in place of the instance; and an inventory manager over it.</summary>
        private static void WithLiveClans(MissionTestContext context, ClanEntry clan, Action<ClanManager> body)
        {
            var instance = typeof(ClanManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = instance.GetValue(null);

            // A member who joins is given the clan's lockbox (InventoryManager.SetupLocalClanInventory).
            var inventoryInstance = typeof(InventoryManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previousInventory = inventoryInstance.GetValue(null);

            try
            {
                inventoryInstance.SetValue(null, new InventoryManager(context));

                var clans = (ClanManager)typeof(ClanManager).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(IGameUnitOfWorkFactory) }, null).Invoke(new object[] { context });

                using (var unit = context.CreateChar())
                {
                    var members = unit.ClanMembers.GetAllClanMembersByClanId(clan.Id);

                    clans.Clans[clan.Id] = new Lazy<ClanEntry>(() => clan);
                    clans.ClanMembers[clan.Id] = new Lazy<List<ClanMemberEntry>>(() => members);
                }

                instance.SetValue(null, clans);
                body(clans);
            }
            finally
            {
                instance.SetValue(null, previous);
                inventoryInstance.SetValue(null, previousInventory);
            }
        }

        private static void Drain(params Client[] clients)
        {
            foreach (var client in clients)
                WorldTestContext.Drain(client);
        }

        private static List<CallMethodMessage> Calls(Client client) =>
            WorldTestContext.Drain(client).Select(p => p.Message).OfType<CallMethodMessage>().ToList();

        private static List<DisplayClanMessagePacket> Messages(Client client) =>
            Calls(client).Select(c => c.Packet).OfType<DisplayClanMessagePacket>().ToList();

        private static List<T> Sent<T>(Client client) where T : class =>
            Calls(client).Select(c => c.Packet).OfType<T>().ToList();
    }
}
