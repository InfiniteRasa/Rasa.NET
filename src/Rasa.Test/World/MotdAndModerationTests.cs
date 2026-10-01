extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Config;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Networking;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;
    using ClientState = RasaGame::Rasa.Data.ClientState;

    // The message of the day (MessageOfTheDay) and the GM tools that act on other players
    // (Moderation: announce, kick, mute, unmute).
    [TestClass]
    [DoNotParallelize]
    public class MotdAndModerationTests
    {
        private const long Now = 1_800_000_000_000;

        private Func<long> _unixNow;
        private Func<string, GameAccountEntry> _findStored;
        private Action<uint, long> _persist;
        private MessageOfTheDayConfig _motd;
        private readonly List<Client> _registered = new();
        private readonly Dictionary<uint, long> _persisted = new();
        private readonly Dictionary<string, GameAccountEntry> _stored = new(StringComparer.OrdinalIgnoreCase);

        [TestInitialize]
        public void UseFixtures()
        {
            _unixNow = Moderation.UnixNow;
            _findStored = Moderation.FindStoredAccount;
            _persist = Moderation.PersistMutedUntil;
            _motd = MessageOfTheDay.Current;

            Moderation.UnixNow = () => Now;
            Moderation.FindStoredAccount = name => _stored.TryGetValue(name, out var account) ? account : null;
            Moderation.PersistMutedUntil = (id, until) => _persisted[id] = until;
        }

        [TestCleanup]
        public void RestoreFixtures()
        {
            Moderation.UnixNow = _unixNow;
            Moderation.FindStoredAccount = _findStored;
            Moderation.PersistMutedUntil = _persist;
            MessageOfTheDay.Apply(_motd);

            lock (Server.Clients)
                foreach (var client in _registered)
                    Server.Clients.Remove(client);
        }

        private Client Online(WorldTestContext world, uint accountId, string familyName, GmLevel level = GmLevel.Player)
        {
            var client = world.CreateClient();
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, new GameAccountEntry
            {
                Id = accountId,
                FamilyName = familyName,
                Level = (byte)level,
                Characters = new List<CharacterEntry>()
            });

            lock (Server.Clients)
                Server.Clients.Add(client);

            _registered.Add(client);
            return client;
        }

        private static List<PlayerMessage> Messages(Client client) =>
            MissionTestContext.Drain(client).OfType<DisplayClientMessagePacket>().Select(p => p.MsgId).ToList();

        [TestMethod]
        public void TheMessageOfTheDayIsSentOncePerChangeByDefaultAndEveryLoginIfAsked()
        {
            var config = new MessageOfTheDayConfig { Text = "Patch 2 is live.", Translations = new() { [6] = "Patch 2 ist da.", [1] = "ignored" } };

            Assert.IsTrue(MessageOfTheDay.Apply(config));
            Assert.IsFalse(MessageOfTheDay.Apply(new MessageOfTheDayConfig { Text = "Patch 2 is live.", Translations = new() { [6] = "Patch 2 ist da." } }), "the same message again");

            var packet = (SendMOTDPacket)MessageOfTheDay.PacketFor(MessageOfTheDay.Current);
            Assert.AreEqual("Patch 2 is live.", packet.Messages[SendMOTDPacket.English]);
            Assert.AreEqual("Patch 2 ist da.", packet.Messages[6]);

            Assert.IsInstanceOfType(MessageOfTheDay.PacketFor(new MessageOfTheDayConfig { Text = "x", ShowEveryLogin = true }), typeof(PreviewMOTDPacket));
            Assert.IsNull(MessageOfTheDay.PacketFor(new MessageOfTheDayConfig { Text = "  " }), "an empty message sends nothing");
            Assert.AreEqual(MessageOfTheDayConfig.DefaultText, new MessageOfTheDayConfig().Text);
        }

        [TestMethod]
        public void TheMessageOfTheDayGoesOncePerConnection()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            MessageOfTheDay.Apply(new MessageOfTheDayConfig { Text = "Hello" });

            MessageOfTheDay.SendOnLogin(client);
            MessageOfTheDay.SendOnLogin(client);

            Assert.AreEqual(1, MissionTestContext.Drain(client).OfType<SendMOTDPacket>().Count());
        }

        [TestMethod]
        public void AChangedMessageGoesToEveryoneInTheWorld()
        {
            using var world = new WorldTestContext();
            var inWorld = world.CreateClient();
            var loading = world.CreateClient();
            loading.State = ClientState.Loading;
            MessageOfTheDay.Apply(new MessageOfTheDayConfig { Text = "Changed" });

            Assert.AreEqual(1, MessageOfTheDay.SendToAll(new[] { inWorld, loading }));
            Assert.AreEqual("Changed", MissionTestContext.Drain(inWorld).OfType<SendMOTDPacket>().Single().Messages[1]);
        }

        [TestMethod]
        public void AGameMasterSilencesAPlayerWhoIsThenRefusedChatUntilItRunsOut()
        {
            using var world = new WorldTestContext();
            var gm = Online(world, 1, "Warden", GmLevel.GameMaster);
            var player = Online(world, 2, "Loud");

            var result = Moderation.Mute("loud", 15, "spam", gm);

            Assert.IsTrue(result.Done);
            Assert.IsTrue(result.Told);
            Assert.AreEqual(Now + 15 * 60_000L, _persisted[2]);
            Assert.AreEqual(Now + 15 * 60_000L, player.AccountEntry.MutedUntil);
            Assert.AreEqual(PlayerMessage.PmGmYouAreSilenced, Messages(player).Single());
            Assert.AreEqual(PlayerMessage.PmGmUserSilenced, Messages(gm).Single());

            Assert.IsTrue(Moderation.RefuseIfMuted(player));
            Assert.AreEqual(PlayerMessage.PmGmYouAreStillSilenced, Messages(player).Single());
            Assert.IsFalse(Moderation.RefuseIfMuted(gm));

            Moderation.UnixNow = () => Now + 15 * 60_000L;
            Assert.IsFalse(Moderation.RefuseIfMuted(player), "the time is up");
            Assert.AreEqual(1, Moderation.Worker(world.Map));
            Assert.AreEqual(PlayerMessage.PmGmYouAreUnsilenced, Messages(player).Single());
            Assert.AreEqual(0, Moderation.Worker(world.Map), "told once");
        }

        [TestMethod]
        public void AGameMasterCannotSilenceOrKickTheirEqualButTheConsoleCan()
        {
            using var world = new WorldTestContext();
            var gm = Online(world, 1, "Warden", GmLevel.GameMaster);
            Online(world, 2, "Other", GmLevel.GameMaster);

            Assert.IsFalse(Moderation.Mute("Other", 5, null, gm).Done);
            Assert.IsFalse(Moderation.Kick("Other", null, gm).Done);
            Assert.IsFalse(_persisted.ContainsKey(2));

            Assert.IsTrue(Moderation.Mute("Other", 5, null, null).Done);
            Assert.AreEqual(Now + 5 * 60_000L, _persisted[2]);
        }

        [TestMethod]
        public void AnAccountNotOnlineCanBeSilencedAndUnsilenced()
        {
            _stored["Away"] = new GameAccountEntry { Id = 9, FamilyName = "Away" };

            Assert.IsTrue(Moderation.Mute("away", 60, null, null).Done);
            Assert.AreEqual(Now + 3_600_000L, _persisted[9]);

            _stored["Away"].MutedUntil = _persisted[9];
            Assert.IsTrue(Moderation.Unmute("Away", null).Done);
            Assert.AreEqual(0L, _persisted[9]);

            Assert.IsFalse(Moderation.Mute("Nobody", 60, null, null).Done);
            Assert.IsFalse(Moderation.Mute("Away", 0, null, null).Done, "minutes from 1");
            Assert.IsFalse(Moderation.Mute("Away", Moderation.MaxMuteMinutes + 1, null, null).Done);
        }

        [TestMethod]
        public void UnsilencingSomeoneWhoIsNotSilencedSaysSo()
        {
            using var world = new WorldTestContext();
            var gm = Online(world, 1, "Warden", GmLevel.GameMaster);
            var player = Online(world, 2, "Quiet");

            var result = Moderation.Unmute("Quiet", gm);

            Assert.IsFalse(result.Done);
            Assert.IsTrue(result.Told);
            Assert.AreEqual(PlayerMessage.PmGmUserNotInSilencedList, Messages(gm).Single());

            Moderation.Mute("Quiet", 5, null, gm);
            Messages(gm);
            Messages(player);

            Assert.IsTrue(Moderation.Unmute("Quiet", gm).Done);
            Assert.AreEqual(0L, player.AccountEntry.MutedUntil);
            Assert.AreEqual(PlayerMessage.PmGmYouAreUnsilenced, Messages(player).Single());
            Assert.AreEqual(PlayerMessage.PmGmUserUnsilenced, Messages(gm).Single());
        }

        [TestMethod]
        public void AKickTellsThePlayerClosesTheConnectionAndSkipsTheCombatLinger()
        {
            using var world = new WorldTestContext();
            var gm = Online(world, 1, "Warden", GmLevel.GameMaster);
            var player = Online(world, 2, "Trouble");
            typeof(Client).GetProperty(nameof(Client.Socket), BindingFlags.Instance | BindingFlags.Public)
                .SetValue(player, new LengthedSocket(SizeType.Dword, false));
            player.Player.InCombat = true;
            player.Player.CombatExpiresAt = Environment.TickCount64 + 10_000;

            var result = Moderation.Kick("trouble", "griefing", gm);

            Assert.IsTrue(result.Done);
            Assert.IsTrue(player.SkipCombatLinger);
            Assert.AreEqual(ClientState.Disconnected, player.State, "no server timer here: closed at once");
            Assert.AreEqual(0L, player.Player.LingerUntil, "no lingering in the fight");
            Assert.IsFalse(Moderation.Kick("trouble", null, gm).Done, "gone");
        }

        [TestMethod]
        public void AnAnnouncementGoesToEveryoneInTheWorld()
        {
            using var world = new WorldTestContext();
            var one = Online(world, 1, "One");
            var two = Online(world, 2, "Two");
            var selecting = Online(world, 3, "Three");
            selecting.State = ClientState.CharacterSelection;

            Assert.AreEqual(2, Moderation.Announce("  Event at Twin Pillars in 10 minutes "));
            Assert.AreEqual(0, Moderation.Announce("   "));
            Assert.AreEqual(1, MissionTestContext.Drain(one).OfType<SystemMessagePacket>().Count());
            Assert.AreEqual(0, MissionTestContext.Drain(selecting).OfType<SystemMessagePacket>().Count());
        }
    }
}
