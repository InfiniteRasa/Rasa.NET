extern alias RasaGame;

using System;
using System.Linq;
using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Networking;
    using Rasa.Packets.Game.Client;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Gameplay;
    using ClientState = RasaGame::Rasa.Data.ClientState;

    // A connection that ends on the login loading screen.
    //
    // Choosing a character makes its manifestation, points it at its map and sends the Wonkavate;
    // its health, armour and power are worked out when the client answers with MapLoaded, and
    // until then what the character left the world with is held aside (Manifestation.LeftWith,
    // RelogVitals). The map's worker puts the client on the map's list on its next tick, long
    // before the client has loaded anything - so a connection that ends on that screen is taken
    // out by the worker like any other player's, with everything a logout saves.
    [TestClass]
    [DoNotParallelize]
    public class LoginScreenDisconnectTests
    {
        private const uint Account = 61;
        private const long Hour = 3_600_000;
        private const ActionId Wave = (ActionId)194;
        private const ActionId Boost = (ActionId)195;

        private IGameUnitOfWorkFactory _factoryBefore;

        [TestInitialize]
        public void Initialize() => _factoryBefore = Server.GameUnitOfWorkFactory;

        [TestCleanup]
        public void Cleanup() => Server.GameUnitOfWorkFactory = _factoryBefore;

        #region Health, armour, power and the death penalties

        [TestMethod]
        public void ACharacterDroppedOnTheLoginLoadingScreenKeepsWhatItLeftWith()
        {
            using var context = new BootcampSelectionTestContext();
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var characterId = Wounded(context, now);

            // Chosen, and on its way in.
            var client = Choose(context);
            var player = client.Player;
            var map = player.MapChannel;

            Assert.AreEqual(ClientState.Loading, client.State);
            Assert.IsNotNull(player.LeftWith, "held aside for the arrival");
            Assert.AreEqual(0, player.Attributes[Attributes.Armor].Current, "nothing worked out yet");

            // The map's next tick: on its list, still loading.
            context.Maps.MapChannelWorker(0);
            Assert.IsTrue(map.ClientList.Contains(client), "the worker's from now on");

            // The client crashes, or is closed, before the world appears.
            client.Close(false);
            context.Maps.MapChannelWorker(0);

            Assert.IsFalse(map.ClientList.Contains(client), "taken out");

            Assert.AreEqual(Left(now), Saved(context, characterId));
        }

        [TestMethod]
        public void TheNextLoginIsGivenWhatTheCharacterLeftWith()
        {
            using var context = new BootcampSelectionTestContext();
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            Wounded(context, now);

            var dropped = Choose(context);

            context.Maps.MapChannelWorker(0);
            dropped.Close(false);
            context.Maps.MapChannelWorker(0);

            // In again: what the arrival puts back (RelogVitals.ApplyVitals, RestorePenalties).
            var held = Choose(context).Player.LeftWith;

            Assert.AreEqual(250, held.Health);
            Assert.AreEqual(40, held.Armor);
            Assert.AreEqual(15, held.Power);
            Assert.AreEqual(2, held.RezTraumaStacks);
            Assert.AreEqual(now + Hour, held.RezTraumaEndsAt);
            Assert.AreEqual(now + Hour / 2, held.NoHealEndsAt);
        }

        [TestMethod]
        public void ACharacterThatArrivedIsSavedAsItStands()
        {
            using var context = new BootcampSelectionTestContext();
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var characterId = Wounded(context, now);
            var client = Choose(context);
            var player = client.Player;

            context.Maps.MapChannelWorker(0);

            // The arrival's own two steps for what was held (MapChannelManager.InitializeLoadedMap),
            // with the bars it works out standing in for the stats.
            context.MaterializeLoadedClient(client);
            player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 100, 100, 100, 0, 0);
            player.Attributes[Attributes.Power] = new ActorAttributes(Attributes.Power, 50, 50, 50, 0, 0);
            RelogVitals.ApplyVitals(player);
            Assert.AreEqual(250, player.Attributes[Attributes.Health].Current);
            Assert.AreEqual(40, player.Attributes[Attributes.Armor].Current);
            Assert.AreEqual(15, player.Attributes[Attributes.Power].Current);
            RelogVitals.RestorePenalties(client, now);
            Assert.IsNull(player.LeftWith, "all put back, and held no longer");

            // Hurt some more, and then the connection goes.
            player.Attributes[Attributes.Health].Current = 100;
            player.Attributes[Attributes.Armor].Current = 5;
            player.Attributes[Attributes.Power].Current = 0;
            client.Close(false);
            context.Maps.MapChannelWorker(0);

            StringAssert.StartsWith(Saved(context, characterId), "health 100, armor 5, power 0, Rez Trauma x2 until ");
        }

        #endregion

        #region Cooldowns

        [TestMethod]
        public void LoadingACharacterLeavesItsSavedCooldownsInTheDatabase()
        {
            using var context = new BootcampSelectionTestContext();
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var characterId = Wounded(context, now);

            Cooling(context, characterId, now + Hour);

            // Somebody else's, which is theirs.
            var other = context.SeedCharacter(Account, 2, "Other");

            Cooling(context, other, now + 2 * Hour, Boost);

            var player = Choose(context).Player;

            // On the server's clock for the session...
            Assert.AreEqual(Hour, player.ActionReuseUntil[Wave] - Environment.TickCount64, 5000);
            Assert.AreEqual(1, player.ActionReuseUntil.Count, "its own and no other's");

            // ...and still in the row, for a server that stops before it has saved anybody.
            Assert.AreEqual($"{(uint)Wave} until {now + Hour}", Cooldowns(context, characterId));
            Assert.AreEqual($"{(uint)Boost} until {now + 2 * Hour}", Cooldowns(context, other));
        }

        [TestMethod]
        public void ACharacterDroppedBeforeTheMapHasListedItKeepsItsCooldowns()
        {
            using var context = new BootcampSelectionTestContext();
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var characterId = Wounded(context, now);

            Cooling(context, characterId, now + Hour);

            // Chosen, and gone in the same tick: the main loop clears up after the connection
            // before the map's worker has put it on the map's list, and nothing is saved.
            var client = Choose(context);

            client.Close(false);
            context.Maps.CleanupDisconnected(client);

            Assert.IsFalse(client.Player.MapChannel?.QueuedClients.Contains(client) ?? false);
            Assert.AreEqual($"{(uint)Wave} until {now + Hour}", Cooldowns(context, characterId));
            Assert.AreEqual(Left(now), Saved(context, characterId));
        }

        [TestMethod]
        public void ACharacterDroppedOnTheLoginLoadingScreenKeepsItsCooldowns()
        {
            using var context = new BootcampSelectionTestContext();
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var characterId = Wounded(context, now);

            Cooling(context, characterId, now + Hour);

            var client = Choose(context);

            context.Maps.MapChannelWorker(0);
            client.Close(false);
            context.Maps.MapChannelWorker(0);

            var saved = Cooldowns(context, characterId).Split(" until ");

            Assert.AreEqual($"{(uint)Wave}", saved[0]);
            Assert.AreEqual(now + Hour, long.Parse(saved[1]), 5000, "saved again from the server's clock");
        }

        #endregion

        #region Fixture

        /// <summary>A character that left the world hurt, with Rez Trauma and the no-healing still to run.</summary>
        private static uint Wounded(BootcampSelectionTestContext context, long now)
        {
            context.SeedAccount(Account);

            var characterId = context.SeedCharacter(Account, 1, "Hurt", level: 10, experience: 200_000);

            context.SeedStartingExperience(characterId, CharacterStartingExperienceState.Legacy, revision: "legacy");

            using (var unit = context.CreateChar())
                unit.Characters.UpdateCharacterVitals(characterId, 250, 40, 15, 2, now + Hour, now + Hour / 2);

            // RelogVitals and ActionReuse save through the server's factory.
            Server.GameUnitOfWorkFactory = context;

            return characterId;
        }

        /// <summary>What <see cref="Wounded"/> left the world with, as <see cref="Saved"/> reads a row.</summary>
        private static string Left(long now) => $"health 250, armor 40, power 15, Rez Trauma x2 until {now + Hour}, no healing until {now + Hour / 2}";

        /// <summary>The character's row: what its next login will be given.</summary>
        private static string Saved(BootcampSelectionTestContext context, uint characterId)
        {
            using var database = context.OpenChar();
            var row = database.CharacterEntries.AsNoTracking().Single(entry => entry.Id == characterId);

            return $"health {row.CurrentHealth}, armor {row.CurrentArmor}, power {row.CurrentPower}, " +
                   $"Rez Trauma x{row.RezTraumaStacks} until {row.RezTraumaEndsAt}, no healing until {row.NoHealEndsAt}";
        }

        /// <summary>A cooldown the character left the world with, to end at that wall-clock time.</summary>
        private static void Cooling(BootcampSelectionTestContext context, uint characterId, long readyAt, ActionId action = Wave)
        {
            using var unit = context.CreateChar();

            unit.CharacterActionReuses.Replace(characterId, new[] { new CharacterActionReuseEntry(characterId, (uint)action, readyAt) });
        }

        /// <summary>The character's saved cooldowns, as its next login will read them.</summary>
        private static string Cooldowns(BootcampSelectionTestContext context, uint characterId)
        {
            using var database = context.OpenChar();

            return string.Join(", ", database.CharacterActionReuseEntries.AsNoTracking()
                .Where(entry => entry.CharacterId == characterId)
                .OrderBy(entry => entry.ActionId)
                .AsEnumerable()
                .Select(entry => $"{entry.ActionId} until {entry.ReadyAt}"));
        }

        /// <summary>A connection that picks the character on the selection screen, and can be closed.</summary>
        private static Client Choose(BootcampSelectionTestContext context)
        {
            var client = context.CreateSelectionClient(Account);

            typeof(Client).GetProperty(nameof(Client.Socket), BindingFlags.Instance | BindingFlags.Public)
                .SetValue(client, new LengthedSocket(SizeType.Dword, false));

            context.Characters.RequestSwitchToCharacterInSlot(client, new RequestSwitchToCharacterInSlotPacket { SlotNum = 1 });

            return client;
        }

        #endregion
    }
}
