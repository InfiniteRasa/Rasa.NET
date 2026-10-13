extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;

    // A requested logout used to stand until it was cancelled, and CharacterLogout asked only that
    // the countdown had once run: a client that requested one and hid the window without cancelling
    // played on, and could leave at once whenever it liked, in the middle of a fight. A fight in the
    // countdown now starts it again, and the client is told (MapChannelManager.FoughtDuringLogout).
    [TestClass]
    [DoNotParallelize]
    public class LogoutCountdownTests
    {
        [TestMethod]
        public void AFightInTheCountdownStartsItAgainAndTellsTheClient()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);

            new MapChannelManager(null).RequestLogout(client);
            Drain(client);

            // Four seconds into the five, the player is hit.
            client.Player.LogoutRequestedTick = Environment.TickCount64 - 4000;
            ManifestationManager.Instance.EnterCombat(client);

            Assert.AreEqual(MapChannelManager.LogoutDelayMs, Sent<LogoutTimeRemainingPacket>(client).Single().TimeRemainingMs,
                "the window counts five seconds again, its Logout button disabled meanwhile");

            new MapChannelManager(null).CharacterLogout(client);

            Assert.IsFalse(client.Player.RemoveFromMap, "not out a second later, as the old countdown would have had it");
            Assert.AreEqual(ClientState.Ingame, client.State);
        }

        [TestMethod]
        public void ALogoutRequestedLongAgoDoesNotLetAPlayerLeaveMidFight()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);

            new MapChannelManager(null).RequestLogout(client);

            // Requested a minute ago, the window hidden without Cancel, and now in a fight.
            client.Player.LogoutRequestedTick = Environment.TickCount64 - 60000;
            ManifestationManager.Instance.EnterCombat(client);
            new MapChannelManager(null).CharacterLogout(client);

            Assert.IsFalse(client.Player.RemoveFromMap, "refused: the countdown has to run out of the fight");

            // Five seconds with no fight in them: the logout goes.
            client.Player.LogoutRequestedTick -= MapChannelManager.LogoutDelayMs;
            new MapChannelManager(null).CharacterLogout(client);

            Assert.IsTrue(client.Player.RemoveFromMap);
            Assert.AreEqual(ClientState.LoggedIn, client.State);
        }

        [TestMethod]
        public void AFightRestartsItAtMostOnceASecond()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);

            new MapChannelManager(null).RequestLogout(client);
            Drain(client);

            client.Player.LogoutRequestedTick = Environment.TickCount64 - 1500;
            ManifestationManager.Instance.EnterCombat(client);
            var restarted = client.Player.LogoutRequestedTick;

            Assert.HasCount(1, Sent<LogoutTimeRemainingPacket>(client));

            // A held trigger: hit after hit inside the second.
            ManifestationManager.Instance.EnterCombat(client);
            ManifestationManager.Instance.EnterCombat(client);

            Assert.IsEmpty(Sent<LogoutTimeRemainingPacket>(client), "one LogoutTimeRemaining, not one a hit");
            Assert.AreEqual(restarted, client.Player.LogoutRequestedTick, "and the server's clock left with the client's");
        }

        [TestMethod]
        public void AFightWithNoLogoutRequestedOrOneUnderWaySendsNothing()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);
            Drain(client);

            ManifestationManager.Instance.EnterCombat(client);

            Assert.IsEmpty(Sent<LogoutTimeRemainingPacket>(client), "no logout requested");

            // Requested, then cancelled.
            ManifestationManager.Instance.ExitCombat(client);
            new MapChannelManager(null).RequestLogout(client);
            new MapChannelManager(null).CancelLogoutRequest(client);
            Drain(client);
            client.Player.LogoutRequestedTick = Environment.TickCount64 - 4000;
            ManifestationManager.Instance.EnterCombat(client);

            Assert.IsEmpty(Sent<LogoutTimeRemainingPacket>(client), "cancelled");

            // Already under way: CharacterLogout honoured, the player on the way out.
            new MapChannelManager(null).RequestLogout(client);
            client.Player.LogoutRequestedTick = Environment.TickCount64 - MapChannelManager.LogoutDelayMs;
            new MapChannelManager(null).CharacterLogout(client);
            Assert.IsTrue(client.Player.RemoveFromMap);
            Drain(client);

            MapChannelManager.FoughtDuringLogout(client, Environment.TickCount64);

            Assert.IsEmpty(Sent<LogoutTimeRemainingPacket>(client), "under way");
        }

        private static void Drain(Client client) => WorldTestContext.Drain(client);

        private static List<T> Sent<T>(Client client) where T : PythonPacket =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Select(message => message.Packet).OfType<T>().ToList();
    }
}
