extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Game.Handlers;
    using Rasa.Managers;

    /// <summary>
    /// A world connection's deadlines before the world (Server.ExpirePreLogin): the key exchange
    /// and the login had one; a connection logged in, or at character selection, had none, and
    /// one that went quiet there held its place and a player slot for as long as the server ran.
    /// It is now closed once its player has done nothing for Server.CharacterSelectionTimeout.
    /// </summary>
    [TestClass]
    public class CharacterSelectionTimeoutTests
    {
        private static readonly long Timeout = (long)Server.CharacterSelectionTimeout.TotalMilliseconds;

        [TestMethod]
        public void AConnectionIdleAtLoginOrCharacterSelectionIsClosedAtTheTimeoutAndNotBefore()
        {
            var now = Environment.TickCount64;
            var atSelection = Client(ClientState.CharacterSelection, lastAction: now - Timeout);
            var loggedIn = Client(ClientState.LoggedIn, lastAction: now - Timeout - 1000);
            var notYet = Client(ClientState.CharacterSelection, lastAction: now - Timeout + 1000);

            CollectionAssert.AreEquivalent(new[] { atSelection, loggedIn },
                Server.IdleBeforeTheWorld(new List<Client> { atSelection, loggedIn, notYet, null }, now));

            Assert.AreEqual(15, Server.CharacterSelectionTimeout.TotalMinutes, "as long as one in the world may stand idle");
        }

        [TestMethod]
        public void TheWorldsOwnStatesAreLeftToTheWorld()
        {
            var now = Environment.TickCount64;
            var states = new[] { ClientState.Connected, ClientState.Loading, ClientState.Ingame, ClientState.Teleporting, ClientState.Disconnected };

            Assert.AreEqual(0, Server.IdleBeforeTheWorld(states.Select(state => Client(state, lastAction: now - 10 * Timeout)), now).Count);
        }

        [TestMethod]
        public void ANewScreenStartsANewWait()
        {
            var client = Client(ClientState.Ingame, lastAction: Environment.TickCount64 - 10 * Timeout);

            // Sent back to character selection by in-world inactivity, or by /logout.
            client.State = ClientState.LoggedIn;
            Assert.AreEqual(0, Server.IdleBeforeTheWorld(new[] { client }, Environment.TickCount64).Count);

            // The same state again is no new screen.
            client.LastActionTick -= Timeout;
            client.State = ClientState.LoggedIn;
            Assert.AreEqual(1, Server.IdleBeforeTheWorld(new[] { client }, Environment.TickCount64).Count);
        }

        [TestMethod]
        public void TheClientsKeepAlivesAreNotThePlayerDoingAnything()
        {
            Assert.IsTrue(ManifestationManager.IsAutomatic(GameOpcode.Ping));
            Assert.IsTrue(ManifestationManager.IsAutomatic(GameOpcode.AutoFireKeepAlive));
            Assert.IsFalse(ManifestationManager.IsAutomatic(GameOpcode.RequestCharacterName));
        }

        [TestMethod]
        public void TheLoginDeadlineIsAsItWas()
        {
            var connected = Client(ClientState.Connected, lastAction: Environment.TickCount64);
            typeof(Client).GetProperty("ConnectedTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(connected, DateTime.UtcNow);

            Assert.AreEqual(0, Server.NotLoggedIn(new[] { connected }, DateTime.UtcNow).Count, "just connected");
            Assert.AreEqual(1, Server.NotLoggedIn(new[] { connected }, DateTime.UtcNow.AddSeconds(31)).Count, "30 s on");

            connected.State = ClientState.CharacterSelection;
            Assert.AreEqual(0, Server.NotLoggedIn(new[] { connected }, DateTime.UtcNow.AddSeconds(31)).Count, "logged in");
        }

        private static Client Client(ClientState state, long lastAction)
        {
            var client = new Client(null, new ClientPacketHandler()) { State = state };
            client.LastActionTick = lastAction;
            return client;
        }
    }
}
