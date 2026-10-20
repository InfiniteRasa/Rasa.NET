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
    using Rasa.Packets.Clan.Server;
    using Rasa.Packets.Communicator.Client;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    /// <summary>
    /// The client's guild slash commands (GuildCommands), which reach the server as
    /// PrivilegedCommand: each is its clan command, for the player's own clan.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class GuildCommandTests
    {
        private const uint MemberId = 2;

        private readonly List<Client> _online = new List<Client>();
        private long _now;

        [TestInitialize]
        public void Initialize()
        {
            GuildCommands.Reset();
            _now = 1_000_000;
            GuildCommands.Now = () => _now;
        }

        [TestCleanup]
        public void Cleanup()
        {
            GuildCommands.Reset();

            lock (Server.Clients)
                foreach (var client in _online)
                    Server.Clients.Remove(client);

            _online.Clear();
        }

        [TestMethod]
        public void EveryGuildCommandTheClientPassesOnIsAClanCommand()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var (leader, _, clan) = Fixture(context);

            WithLiveClans(context, clan, _ =>
            {
                // Invite and kick by family name, through the clan's own by-name paths.
                foreach (var command in new[] { "guildinvite", "addguild", "guinvite" })
                {
                    Slash(leader, command, "Nobody");
                    Assert.AreEqual((int)PlayerMessage.PmNoSuchUser, ClanMessages(leader).Single().MsgId, command);
                }

                foreach (var command in new[] { "guildkick", "gukick" })
                {
                    Slash(leader, command, "Nobody");
                    StringAssert.Contains(SystemMessages(leader).Single(), "Nobody", command);
                }

                // Nothing after it: how to use it, and nothing done.
                foreach (var command in new[] { "guildinvite", "guildkick" })
                {
                    Slash(leader, command, "  ");
                    Assert.AreEqual($"usage: /{command} <family name>", SystemMessages(leader).Single());
                }

                // None of them is unknown any more.
                foreach (var command in new[] { "guildleave", "guleave", "guilddisband", "gudisband" })
                {
                    Slash(leader, command, "");
                    Assert.IsFalse(SystemMessages(leader).Any(text => text.StartsWith("Unknown command")), command);
                }
            });
        }

        [TestMethod]
        public void LeavingTakesTheCommandTwiceWithinFifteenSeconds()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var (_, member, clan) = Fixture(context);

            WithLiveClans(context, clan, clans =>
            {
                Slash(member, "guildleave", "");

                Assert.AreEqual("Type /guildleave again within 15 seconds to leave Fixture Clan.", SystemMessages(member).Single());
                Assert.IsNotNull(clans.GetClanMember(clan.Id, MemberId), "still in it");

                // Too late: asked again.
                _now += 15_001;
                Slash(member, "guleave", "");
                StringAssert.StartsWith(SystemMessages(member).Single(), "Type /guleave again");
                Assert.IsNotNull(clans.GetClanMember(clan.Id, MemberId));

                // In time: out, as /clanleave leaves.
                _now += 15_000;
                Slash(member, "guildleave", "");

                Assert.IsNull(clans.GetClanMember(clan.Id, MemberId), "out of the clan");
                Assert.AreEqual(0u, member.Player.ClanId);
            });
        }

        [TestMethod]
        public void ALeaveOrDisbandTheClanWouldRefuseIsRefusedAtOnce()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var (leader, member, clan) = Fixture(context);

            WithLiveClans(context, clan, clans =>
            {
                // The leader cannot walk out; a member cannot disband.
                Slash(leader, "guildleave", "");
                Assert.AreEqual((int)PlayerMessage.PmClanInsufficientPermissions, ClanMessages(leader).Single().MsgId);

                Slash(member, "guilddisband", "");
                Assert.AreEqual((int)PlayerMessage.PmClanInsufficientPermissions, ClanMessages(member).Single().MsgId);

                Assert.IsNotNull(clans.GetClanMember(clan.Id, leader.Player.Id));
                Assert.IsNotNull(clans.GetClanMember(clan.Id, MemberId));
            });
        }

        [TestMethod]
        public void TheLeaderDisbandsWithTheCommandTwice()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var (leader, _, clan) = Fixture(context);

            WithLiveClans(context, clan, clans =>
            {
                Slash(leader, "gudisband", "");
                Assert.AreEqual("Type /gudisband again within 15 seconds to disband Fixture Clan.", SystemMessages(leader).Single());
                Assert.IsNotNull(clans.GetClanMember(clan.Id, leader.Player.Id));

                // In time, under the other name: disbanded, as /clandisband disbands.
                _now += 1_000;
                Slash(leader, "guilddisband", "");

                Assert.IsNull(clans.GetClanMember(clan.Id, leader.Player.Id), "disbanded");

                using var unit = context.CreateChar();
                Assert.IsNull(unit.Clans.GetClanByCharacterId(leader.Player.Id));
            });
        }

        [TestMethod]
        public void NotInAClanIsTheClansOwnAnswer()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var (_, member, clan) = Fixture(context);

            WithLiveClans(context, clan, _ =>
            {
                member.Player.ClanId = 0;

                Slash(member, "guildleave", "");
                Assert.AreEqual((int)PlayerMessage.PmClanNotInAClan, ClanMessages(member).Single().MsgId);
                Assert.IsEmpty(SystemMessages(member), "nothing to confirm");
            });
        }

        [TestMethod]
        public void OtherUnknownCommandsAreStillUnknown()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var (leader, _, _) = Fixture(context);

            Slash(leader, "honor", "Somebody");
            Assert.AreEqual("Unknown command: /honor", SystemMessages(leader).Single());
        }

        private static void Slash(Client client, string command, string args) =>
            ChatCommandsManager.Instance.PrivilegedCommand(client, new PrivilegedCommandPacket { Command = command, Args = args });

        private (Client Leader, Client Member, ClanEntry Clan) Fixture(MissionTestContext context)
        {
            var leader = context.Client;
            var member = context.CreateAdditionalClient(MemberId);
            var clan = Clan(context, "Fixture Clan", (leader.Player.Id, ClanRank.Leader), (MemberId, (byte)0));

            leader.Player.ClanId = clan.Id;
            member.Player.ClanId = clan.Id;

            foreach (var client in new[] { leader, member })
            {
                client.State = ClientState.Ingame;
                client.Player.Inventory.ResetClanInventory();

                lock (Server.Clients)
                    Server.Clients.Add(client);

                _online.Add(client);
                WorldTestContext.Drain(client);
            }

            return (leader, member, clan);
        }

        private static ClanEntry Clan(MissionTestContext context, string name, params (uint CharacterId, byte Rank)[] members)
        {
            using var unit = context.CreateChar();
            var clan = unit.Clans.CreateClan(name, false);

            foreach (var (characterId, rank) in members)
                Assert.IsTrue(unit.ClanMembers.InsertClanMemberData(clan.Id, characterId, rank, ""));

            return clan;
        }

        /// <summary>A clan manager over the context's database with the clan cached, as it is on a running server, in place of the instance.</summary>
        private static void WithLiveClans(MissionTestContext context, ClanEntry clan, Action<ClanManager> body)
        {
            var instance = typeof(ClanManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = instance.GetValue(null);
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

        private static List<CallMethodMessage> Calls(Client client) =>
            WorldTestContext.Drain(client).Select(p => p.Message).OfType<CallMethodMessage>().ToList();

        private static List<DisplayClanMessagePacket> ClanMessages(Client client) =>
            Calls(client).Select(c => c.Packet).OfType<DisplayClanMessagePacket>().ToList();

        private static List<string> SystemMessages(Client client) =>
            Calls(client).Select(c => c.Packet).OfType<SystemMessagePacket>().Select(p => p.TextMessage).ToList();
    }
}
