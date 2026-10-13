using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.Clan.Client;
    using Rasa.Packets.Clan.Server;
    using Rasa.Packets.Communicator.Client;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    /// <summary>
    /// The tag over a player's name is their clan's name. The client shows it from the ClanId on
    /// the manifestation and the clan's data it has been sent (client/clan.py GetClanByClanId),
    /// and both went only to the member's own client: nobody else ever saw a clan tag.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ClanTagTests
    {
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
        public void SomeoneWhoMeetsAMemberIsGivenTheClanAndTheTag()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var manager = new ManifestationManager(context);
            var member = context.Client;
            var stranger = context.CreateAdditionalClient(2);
            var clan = Clan(context, "Tagged", (1, ClanRank.Leader));

            Join(member, clan);
            Online(stranger);

            WithLiveClans(context, clan, () =>
            {
                Drain(member);
                Drain(stranger);

                // The member, to a client that arrives where they are, and to one already there.
                manager.CellIntroducePlayersToClient(stranger, new List<Client> { member });
                AssertTagged(Drain(stranger), member, clan);

                manager.CellIntroduceClientToPlayers(member, new List<Client> { stranger });
                AssertTagged(Drain(stranger), member, clan);

                // Their own client, on every map and not only at login.
                Assert.AreEqual(clan.Id, manager.CreatePlayerEntityData(member, member).OfType<ClanIdPacket>().Single().ClanId);

                // Someone in no clan: no tag, and no clan's data.
                manager.CellIntroducePlayersToClient(member, new List<Client> { stranger });
                var calls = Drain(member);

                Assert.IsFalse(calls.Any(c => c.Packet is SetClanDataPacket));
                Assert.IsFalse(calls.Select(c => c.Packet).OfType<CreatePhysicalEntityPacket>().Single().EntityData.OfType<ClanIdPacket>().Any());
            });
        }

        [TestMethod]
        public void LeavingTakesTheTagOffForThoseAroundAndLoggingOutTellsNobody()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var leaver = context.Client;
            var leader = context.CreateAdditionalClient(5);
            var stranger = context.CreateAdditionalClient(2);
            var clan = Clan(context, "Tagged", (5, ClanRank.Leader), (1, ClanRank.Member));

            Join(leaver, clan);
            Join(leader, clan);
            Online(stranger);

            WithLiveClans(context, clan, () =>
            {
                Drain(leaver);
                Drain(leader);
                Drain(stranger);

                ClanManager.Instance.LeaveClan(leaver, new LeaveClanPacket { CharacterId = leaver.Player.EntityId, ClanId = clan.Id });

                Assert.AreEqual(0u, leaver.Player.ClanId);

                foreach (var onlooker in new[] { stranger, leader })
                {
                    var tag = Drain(onlooker).Where(c => c.Packet is ClanIdPacket).ToList();

                    Assert.AreEqual(1, tag.Count, "a ClanId of None on the leaver");
                    Assert.AreEqual(leaver.Player.EntityId, tag[0].EntityId);
                    Assert.AreEqual(0u, ((ClanIdPacket)tag[0].Packet).ClanId);
                }

                // Out of the world rather than out of the clan: nothing to the others.
                ClanManager.Instance.RemovePlayer(leader);

                Assert.AreEqual(0u, leader.Player.ClanId);
                Assert.IsFalse(Drain(stranger).Any(c => c.Packet is ClanIdPacket));
            });
        }

        [TestMethod]
        public void ANewClanNameReachesThoseAroundAMember()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var leader = context.Client;
            var stranger = context.CreateAdditionalClient(2);
            var clan = Clan(context, "Tagged", (1, ClanRank.Leader));

            Join(leader, clan);
            Online(stranger);

            WithLiveClans(context, clan, () =>
            {
                Drain(leader);
                Drain(stranger);

                ClanManager.Instance.ChangeClanName(leader, new ChangeClanNamePacket { ClanName = "Retagged" });

                var data = Drain(stranger).Select(c => c.Packet).OfType<SetClanDataPacket>().Single();

                Assert.AreEqual(clan.Id, data.Data.Id);
                Assert.AreEqual("Retagged", data.Data.Name);

                // The member has it once, with the clan window's.
                Assert.AreEqual(1, Drain(leader).Count(c => c.Packet is SetClanDataPacket));
            });
        }

        private static void AssertTagged(List<CallMethodMessage> calls, Client member, ClanEntry clan)
        {
            var data = calls.FindIndex(c => c.Packet is SetClanDataPacket);
            var made = calls.FindIndex(c => c.Packet is CreatePhysicalEntityPacket p && p.EntityId == member.Player.EntityId);

            Assert.IsTrue(data >= 0, "the clan's data, for its name");
            Assert.AreEqual(clan.Id, ((SetClanDataPacket)calls[data].Packet).Data.Id);
            Assert.AreEqual(clan.Name, ((SetClanDataPacket)calls[data].Packet).Data.Name);
            Assert.IsTrue(made > data, "before the manifestation");
            Assert.AreEqual(clan.Id, ((CreatePhysicalEntityPacket)calls[made].Packet).EntityData.OfType<ClanIdPacket>().Single().ClanId);
        }

        private static List<CallMethodMessage> Drain(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>().ToList();

        private static ClanEntry Clan(MissionTestContext context, string name, params (uint CharacterId, byte Rank)[] members)
        {
            using var unit = context.CreateChar();
            var clan = unit.Clans.CreateClan(name, false);

            foreach (var (characterId, rank) in members)
                Assert.IsTrue(unit.ClanMembers.InsertClanMemberData(clan.Id, characterId, rank, ""));

            return clan;
        }

        private void Join(Client client, ClanEntry clan)
        {
            client.Player.ClanId = clan.Id;
            client.Player.Inventory.ResetClanInventory();
            Online(client);
        }

        private void Online(Client client)
        {
            lock (Server.Clients)
                Server.Clients.Add(client);

            _online.Add(client);
        }

        /// <summary>A clan manager over the context's database with the clan cached, as it is on a running server, in place of the instance.</summary>
        private static void WithLiveClans(MissionTestContext context, ClanEntry clan, Action body)
        {
            var instance = typeof(ClanManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = instance.GetValue(null);

            try
            {
                var clans = (ClanManager)typeof(ClanManager).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(IGameUnitOfWorkFactory) }, null).Invoke(new object[] { context });

                using (var unit = context.CreateChar())
                {
                    var members = unit.ClanMembers.GetAllClanMembersByClanId(clan.Id);

                    clans.Clans[clan.Id] = new Lazy<ClanEntry>(() => clan);
                    clans.ClanMembers[clan.Id] = new Lazy<List<ClanMemberEntry>>(() => members);
                }

                instance.SetValue(null, clans);
                body();
            }
            finally
            {
                instance.SetValue(null, previous);
            }
        }
    }
}
