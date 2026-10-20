extern alias RasaGame;

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
    using Rasa.Packets;
    using Rasa.Packets.Clan.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Gameplay;

    /// <summary>
    /// A clan member's roster line as they are now: AFK when they are (ClanMemberData.IsAfk, which
    /// nothing set), and sent again to the clan online - themselves too - when they go AFK, come
    /// back or go up a level (ClanManager.MemberChanged).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ClanRosterLiveTests
    {
        private static uint _nextAccount = 9800;

        private readonly List<Client> _clients = new List<Client>();
        private readonly List<uint> _clans = new List<uint>();

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var client in _clients)
                lock (Server.Clients)
                    Server.Clients.Remove(client);

            foreach (var id in _clans)
            {
                ClanManager.Instance.Clans.TryRemove(id, out _);
                ClanManager.Instance.ClanMembers.TryRemove(id, out _);
            }
        }

        [TestMethod]
        public void GoingAfkAndComingBackIsOnTheRosterOfEveryMemberOnline()
        {
            using var world = new WorldTestContext();
            var away = Online(world, PlayerDeathTests.Player(world, 0, 0));
            var member = Online(world, PlayerDeathTests.Player(world, 200, 0));
            var stranger = Online(world, PlayerDeathTests.Player(world, 400, 0));
            Clan(away, member);
            Drain(away, member, stranger);

            ManifestationManager.Instance.SetAfk(away, true);

            Assert.IsTrue(Line(member, away).IsAfk, "AFK on a member's roster, though they are nowhere near");
            var own = Line(away, away);
            Assert.IsTrue(own.IsAfk, "and on their own");
            Assert.IsTrue(own.IsOnline);
            Assert.IsEmpty(Packets(stranger).OfType<SetClanMemberDataPacket>().ToArray(), "nobody outside the clan");

            ManifestationManager.Instance.SetAfk(away, false);

            Assert.IsFalse(Line(member, away).IsAfk, "back");
            Assert.IsFalse(Line(away, away).IsAfk);

            ManifestationManager.Instance.SetAfk(away, false);
            Assert.IsEmpty(Packets(member).OfType<SetClanMemberDataPacket>().ToArray(), "no change, nothing sent");
        }

        [TestMethod]
        public void ALineMadeForAnAfkMemberSaysSo()
        {
            using var world = new WorldTestContext();
            var away = Online(world, PlayerDeathTests.Player(world, 0, 0));
            var member = Online(world, PlayerDeathTests.Player(world, 200, 0));
            Clan(away, member);
            away.Player.IsAFK = true;
            Drain(away, member);

            // What GM visibility sends is the line made from the live character, as at login.
            ClanManager.Instance.GmVisibilityChanged(away);

            Assert.IsTrue(Line(member, away).IsAfk);
        }

        [TestMethod]
        public void ALevelGainedIsOnTheRosterOfEveryMemberOnline()
        {
            using var context = new WeaponAmmoContext();
            using var world = new WorldTestContext();
            // The context's own character, as its database has it: online, account and id as they are.
            var mover = context.Client;
            lock (Server.Clients)
                Server.Clients.Add(mover);
            _clients.Add(mover);
            var member = Online(world, world.CreateClient());
            var manager = new ManifestationManager(context);
            mover.Player.Attributes = Enum.GetValues<Attributes>().ToDictionary(attribute => attribute, attribute => new ActorAttributes(attribute, 0, 0, 0, 0, 0));
            Clan(mover, member);
            Drain(mover, member);

            manager.SetLevel(mover, 3);

            Assert.AreEqual(3, (int)mover.Player.Level);
            Assert.AreEqual(3u, Line(member, mover).Level);
            Assert.AreEqual(3u, Line(mover, mover).Level, "their own line too");
        }

        private Client Online(WorldTestContext world, Client client)
        {
            var id = ++_nextAccount;

            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client,
                new GameAccountEntry { Id = id, FamilyName = $"Roster{id}", Characters = new List<CharacterEntry>() });
            client.Player.FamilyName = $"Roster{id}";
            client.Player.Id = id;

            lock (Server.Clients)
                Server.Clients.Add(client);

            _clients.Add(client);
            WorldTestContext.Drain(client);
            return client;
        }

        private void Clan(params Client[] members)
        {
            var id = 980000u + members[0].AccountEntry.Id;
            var clan = new ClanEntry { Id = id, Name = $"Roster Clan {id}" };

            ClanManager.Instance.Clans[id] = new Lazy<ClanEntry>(() => clan);
            ClanManager.Instance.ClanMembers[id] = new Lazy<List<ClanMemberEntry>>(() =>
                members.Select(m => new ClanMemberEntry { ClanId = id, CharacterId = m.Player.Id, Rank = 0 }).ToList());
            _clans.Add(id);

            foreach (var member in members)
                member.Player.ClanId = id;
        }

        /// <summary>The roster line about one member the reader was last sent.</summary>
        private static ClanMemberData Line(Client reader, Client about) =>
            Packets(reader).OfType<SetClanMemberDataPacket>().Last(p => p.Data.CharacterId == about.Player.Id).Data;

        private static List<PythonPacket> Packets(Client client) =>
            WorldTestContext.Drain(client).Select(p => p.Message).OfType<CallMethodMessage>().Select(m => m.Packet).ToList();

        private static void Drain(params Client[] clients)
        {
            foreach (var client in clients)
                WorldTestContext.Drain(client);
        }
    }
}
