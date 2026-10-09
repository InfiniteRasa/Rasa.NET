extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using RasaGameClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Missions;
    using Rasa.Packets;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Manifestation.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Test.Missions;
    using Rasa.Test.World;

    /// <summary>
    /// Kill streaks (KillStreaks): three kills in a row, each within eight seconds of the last,
    /// light the skull and double the experience of the kills after them; further threes raise
    /// it as far as the character's level allows; eight seconds without a kill end it.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class KillStreakTests
    {
        private Func<Client, Vector3, List<Client>> _sharers;
        private Func<long> _now;
        private Func<Client, int, bool> _change;
        private long _clock;
        private List<(Client Client, int Amount)> _prestige;

        [TestInitialize]
        public void Begin()
        {
            _sharers = KillShares.SharersOf;
            _now = KillStreaks.Now;
            _change = PvpPrestige.Change;
            _clock = 1_000_000;
            _prestige = new List<(Client, int)>();

            KillStreaks.Reset();
            KillStreaks.Now = () => _clock;
            PvpPrestige.Change = (client, amount) =>
            {
                _prestige.Add((client, amount));
                return true;
            };
        }

        [TestCleanup]
        public void End()
        {
            KillStreaks.Reset();
            KillStreaks.Now = _now;
            KillShares.SharersOf = _sharers;
            PvpPrestige.Change = _change;
        }

        private static MissionTestContext Start() =>
            MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled, 82));

        private static CreatureManager Creatures(MissionTestContext context) =>
            new CreatureManager(context, new ManifestationManager(context), context.Manager);

        /// <summary>Everything the client was sent since last asked.</summary>
        private static List<PythonPacket> Sent(Client client) => MissionTestContext.Drain(client);

        private static List<XPInfo> Gains(IEnumerable<PythonPacket> sent) =>
            sent.OfType<ExperienceChangedPacket>().Select(packet => packet.XPInfo).ToList();

        private static List<int> Streaks(IEnumerable<PythonPacket> sent) =>
            sent.OfType<SetKillStreakPacket>().Select(packet => packet.Count).ToList();

        private static List<DisplayClientMessagePacket> PrestigeLines(IEnumerable<PythonPacket> sent) =>
            sent.OfType<DisplayClientMessagePacket>().Where(packet => packet.MsgId == PlayerMessage.PmPrestigePointsReceivedKillstreakmax).ToList();

        /// <summary>A level 1 creature where the killer stands, killed by them, and what the killer's client was sent for it.</summary>
        private static List<PythonPacket> Kill(MissionTestContext context, CritKill critKill = CritKill.None)
        {
            var creature = new Creature
            {
                DbId = 9001,
                EntityClass = EntityClasses.HumanBaseMale,
                MapContextId = context.Map.MapInfo.MapContextId,
                Position = context.Client.Player.Position,
                Level = 1,
                State = CharacterState.Idle,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                Attributes = Enum.GetValues<Attributes>().ToDictionary(
                    id => id, id => new ActorAttributes(id, 100, 100, 0, 0, 0)),
                SpawnPool = new SpawnPool
                {
                    MapContextId = context.Map.MapInfo.MapContextId,
                    AliveCreatures = 1,
                    RespawnTime = 1000,
                    UpdateTimer = 1000
                }
            };

            CellManager.Instance.AddToWorld(context.Map, creature);
            Sent(context.Client);

            try
            {
                Creatures(context).HandleCreatureKill(context.Map, creature, context.Client.Player, critKill);

                return Sent(context.Client);
            }
            finally
            {
                CellManager.Instance.RemoveCreatureFromWorld(context.Map, creature);
            }
        }

        private static uint Saved(MissionTestContext context, Client client)
        {
            using var unit = context.CreateChar();

            return unit.Characters.Find(client.Player.Id).Experience;
        }

        [TestMethod]
        [DataRow(0, 1)]
        [DataRow(1, 1)]
        [DataRow(9, 1)]
        [DataRow(10, 2)]
        [DataRow(19, 2)]
        [DataRow(20, 3)]
        [DataRow(29, 3)]
        [DataRow(30, 4)]
        [DataRow(39, 4)]
        [DataRow(40, 5)]
        [DataRow(50, 5)]
        public void TheHighestStreakIsByTheCharactersLevel(int characterLevel, int highest)
        {
            // The client's help: x2 up to level 9, x3 for 10 to 19, x4, x5, and x6 from 40.
            Assert.AreEqual(highest, KillStreaks.MaxLevelFor(characterLevel));
        }

        [TestMethod]
        public void TheNumbersAreTheClientsAndTheFootages()
        {
            Assert.AreEqual(3, KillStreaks.KillsPerLevelPerMember, "STREAK_BASE_PER_PARTY_MEMBER");
            Assert.AreEqual(10, KillStreaks.LevelBasis, "STREAK_LEVEL_BASIS");
            Assert.AreEqual(5, KillStreaks.MaxLevel, "STREAK_MAX_VALUE");
            Assert.AreEqual(1, KillStreaks.MaxStreakPrestige, "MAX_KILLING_STREAK_PRESTIGE_POINT_BONUS");
            Assert.AreEqual(8000L, KillStreaks.LapseMs, "eight seconds after the last kill");

            // A kill divides evenly between any squad there can be.
            for (var size = 1; size <= PartyManager.MaxPartySize; size++)
                Assert.AreEqual(0, KillStreaks.CreditPerKill % size, $"a squad of {size}");
        }

        [TestMethod]
        public void TheThirdKillInARowLightsTheSkullAndTheKillsAfterItAreDoubled()
        {
            using var context = Start();
            var client = context.Client;

            // The first two: nothing to see, and nothing added.
            for (var kill = 1; kill <= 2; kill++)
            {
                var sent = Kill(context);
                var gained = Gains(sent).Single();

                Assert.AreEqual(1, gained.StreakMod, $"kill {kill}");
                Assert.AreEqual(gained.Gained, gained.BaseGained);
                Assert.IsEmpty(Streaks(sent), $"kill {kill}");
                Assert.AreEqual(0, KillStreaks.LevelOf(client));
                Assert.AreEqual(kill * KillStreaks.CreditPerKill, KillStreaks.CreditOf(client));

                _clock += 3000;
            }

            // The third is paid as the two before it, and then the skull is lit: x2.
            var third = Kill(context);
            var thirds = Gains(third).Single();

            Assert.AreEqual(1, thirds.StreakMod, "the kill that starts the streak is not multiplied by it");
            Assert.AreEqual(thirds.Gained, thirds.BaseGained);
            CollectionAssert.AreEqual(new[] { 1 }, Streaks(third));
            Assert.IsTrue(third.FindIndex(packet => packet is ExperienceChangedPacket) < third.FindIndex(packet => packet is SetKillStreakPacket),
                "its experience first, then the streak");
            Assert.AreEqual(1, KillStreaks.LevelOf(client));
            Assert.AreEqual(2, KillStreaks.MultiplierOf(client));

            // A character under level 10 can reach no higher: that is its highest, worth 1 prestige.
            CollectionAssert.AreEqual(new[] { (client, 1) }, _prestige);
            Assert.AreEqual("1", PrestigeLines(third).Single().Args["amount"]);
            Assert.AreEqual(MsgFilterId.PrestigeGainLose, PrestigeLines(third).Single().Filterid);

            // The kills after it: twice the experience, and the client told why.
            var before = client.Player.Experience;

            for (var kill = 4; kill <= 7; kill++)
            {
                _clock += 7999;

                var sent = Kill(context);
                var gained = Gains(sent).Single();

                Assert.AreEqual(2, gained.StreakMod, $"kill {kill}: +100% Kill Streak Bonus");
                Assert.IsTrue(gained.BaseGained >= 90 && gained.BaseGained <= 110, $"a level 1 kill is 100 give or take 10, not {gained.BaseGained}");
                Assert.AreEqual(gained.BaseGained * 2, gained.Gained);
                Assert.AreEqual(1, gained.GroupMod);
                Assert.AreEqual(1, gained.BoosterMod);
                Assert.AreEqual(before + gained.Gained, gained.Total);
                Assert.AreEqual(gained.Total, client.Player.Experience);
                Assert.AreEqual(gained.Total, Saved(context, client), "and saved");
                Assert.IsEmpty(Streaks(sent), "the level has not changed: nothing more is said of it");
                Assert.AreEqual(1, KillStreaks.LevelOf(client), "x2 is as high as a character under level 10 goes");
                Assert.AreEqual(KillStreaks.CreditPerLevel, KillStreaks.CreditOf(client), "and nothing is banked past it");

                before = gained.Total;
            }

            Assert.HasCount(1, _prestige, "paid for reaching it, not for holding it");
        }

        [TestMethod]
        public void EightSecondsWithoutAKillEndTheStreak()
        {
            using var context = Start();
            var client = context.Client;

            Kill(context);
            Kill(context);
            Kill(context);
            Assert.AreEqual(1, KillStreaks.LevelOf(client));
            Sent(client);

            // A moment short of eight seconds: it stands.
            _clock += 7999;
            KillStreaks.Worker();
            Assert.IsEmpty(Sent(client));
            Assert.AreEqual(1, KillStreaks.LevelOf(client));

            // Eight seconds after the last kill: gone, and the client told so.
            _clock += 1;
            KillStreaks.Worker();
            CollectionAssert.AreEqual(new[] { 0 }, Streaks(Sent(client)));
            Assert.AreEqual(0, KillStreaks.LevelOf(client));
            Assert.AreEqual(0, KillStreaks.CreditOf(client));
            Assert.AreEqual(1, KillStreaks.MultiplierOf(client));

            // Said once.
            _clock += 60000;
            KillStreaks.Worker();
            Assert.IsEmpty(Sent(client));

            // The next kill is the first of three again.
            var sent = Kill(context);
            Assert.AreEqual(1, Gains(sent).Single().StreakMod);
            Assert.IsEmpty(Streaks(sent));
            Assert.AreEqual(KillStreaks.CreditPerKill, KillStreaks.CreditOf(client));
        }

        [TestMethod]
        public void EachKillStartsTheEightSecondsAgainAndKillsTooFarApartNeverMakeAStreak()
        {
            using var context = Start();
            var client = context.Client;

            // Two kills, then nine seconds: the two are forgotten, with nothing to tell.
            Kill(context);
            _clock += 4000;
            Kill(context);
            _clock += 9000;
            Sent(client);
            KillStreaks.Worker();
            Assert.IsEmpty(Sent(client), "there was no skull to put out");
            Assert.AreEqual(0, KillStreaks.CreditOf(client));

            // One every nine seconds, for as long as you like: never a streak.
            for (var kill = 0; kill < 6; kill++)
            {
                var sent = Kill(context);

                Assert.IsEmpty(Streaks(sent));
                Assert.AreEqual(1, Gains(sent).Single().StreakMod);
                _clock += 9000;
            }

            // One every seven: the third makes it, and it holds as long as they keep coming -
            // far longer than eight seconds from its start.
            for (var kill = 0; kill < 3; kill++)
            {
                _clock += 7000;
                Kill(context);
            }

            Assert.AreEqual(1, KillStreaks.LevelOf(client));

            for (var kill = 0; kill < 5; kill++)
            {
                _clock += 7000;
                KillStreaks.Worker();
                Assert.AreEqual(2, Gains(Kill(context)).Single().StreakMod);
            }

            // A kill that comes after the time is up finds no streak, whether the worker has run or not.
            _clock += 8000;
            var late = Kill(context);

            Assert.AreEqual(1, Gains(late).Single().StreakMod);
            CollectionAssert.AreEqual(new[] { 0 }, Streaks(late), "the client is told it is over as the late kill is paid");
            Assert.AreEqual(KillStreaks.CreditPerKill, KillStreaks.CreditOf(client));
        }

        [TestMethod]
        public void EveryThreeKillsRaiseItALevelAsFarAsTheCharactersLevelAllows()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var alone = new[] { client };

            client.Player.Level = 25;   // x4 at the highest

            var told = new List<int>();
            var multipliers = new List<int>();

            for (var kill = 1; kill <= 14; kill++)
            {
                multipliers.Add(KillStreaks.MultiplierOf(client));
                KillStreaks.KillCounted(alone);
                told.AddRange(Streaks(Sent(client)));
                _clock += 2000;
            }

            // What each kill is paid at: the level before it.
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 4, 4 }, multipliers);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, told, "told as it rises, and not again");
            Assert.AreEqual(3, KillStreaks.LevelOf(client));
            Assert.AreEqual(3 * KillStreaks.CreditPerLevel, KillStreaks.CreditOf(client));

            // Prestige for the highest only: the ninth kill's, not the third's or the sixth's.
            CollectionAssert.AreEqual(new[] { (client, 1) }, _prestige);

            // It ends whole: from x4 to nothing.
            _clock += 8000;
            KillStreaks.Worker();
            CollectionAssert.AreEqual(new[] { 0 }, Streaks(Sent(client)));
            Assert.AreEqual(0, KillStreaks.LevelOf(client));

            // Reached again, paid again.
            for (var kill = 1; kill <= 9; kill++)
                KillStreaks.KillCounted(alone);

            CollectionAssert.AreEqual(new[] { (client, 1), (client, 1) }, _prestige);

            // At level 40 and over, fifteen kills are x6.
            client.Player.Level = 50;
            KillStreaks.End(client);
            Sent(client);

            for (var kill = 1; kill <= 20; kill++)
                KillStreaks.KillCounted(alone);

            Assert.AreEqual(5, KillStreaks.LevelOf(client));
            Assert.AreEqual(6, KillStreaks.MultiplierOf(client));
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, Streaks(Sent(client)));
            Assert.HasCount(3, _prestige);
        }

        [TestMethod]
        public void ALevelGainedInAStreakRaisesWhatItCanReach()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var alone = new[] { client };

            client.Player.Level = 9;

            for (var kill = 1; kill <= 5; kill++)
                KillStreaks.KillCounted(alone);

            Assert.AreEqual(1, KillStreaks.LevelOf(client));
            Assert.HasCount(1, _prestige);

            // Level 10: x3 is in reach, three kills on from where the credit stood.
            client.Player.Level = 10;
            Sent(client);

            KillStreaks.KillCounted(alone);
            KillStreaks.KillCounted(alone);
            Assert.AreEqual(1, KillStreaks.LevelOf(client));
            KillStreaks.KillCounted(alone);

            Assert.AreEqual(2, KillStreaks.LevelOf(client));
            CollectionAssert.AreEqual(new[] { 2 }, Streaks(Sent(client)));
            Assert.HasCount(2, _prestige, "the new highest, reached");
        }

        [TestMethod]
        public void ASquadSharingTheKillsNeedsThreeForEachOfThem()
        {
            using var context = Start();
            var mate = context.CreateAdditionalClient(2);
            var away = context.CreateAdditionalClient(3);

            // Two share the kills; a third of the squad is elsewhere and has no part in them.
            KillShares.SharersOf = (killer, corpse) => new List<Client> { killer, mate };

            for (var kill = 1; kill <= 5; kill++)
            {
                var sent = Kill(context);

                Assert.IsEmpty(Streaks(sent), $"kill {kill}");
                Assert.AreEqual(kill * KillStreaks.CreditPerKill / 2, KillStreaks.CreditOf(context.Client));
                Assert.AreEqual(kill * KillStreaks.CreditPerKill / 2, KillStreaks.CreditOf(mate));
            }

            Assert.IsEmpty(Streaks(Sent(mate)));

            // The sixth: both, though one of them made every kill.
            CollectionAssert.AreEqual(new[] { 1 }, Streaks(Kill(context)));
            CollectionAssert.AreEqual(new[] { 1 }, Streaks(Sent(mate)));
            Assert.AreEqual(1, KillStreaks.LevelOf(context.Client));
            Assert.AreEqual(1, KillStreaks.LevelOf(mate));
            CollectionAssert.AreEquivalent(new[] { (context.Client, 1), (mate, 1) }, _prestige);

            // And the next is doubled for both: each their half, twice.
            var killers = Gains(Kill(context)).Single();
            var mates = Gains(Sent(mate)).Single();

            Assert.AreEqual(2, killers.StreakMod);
            Assert.AreEqual(2, mates.StreakMod);
            Assert.IsTrue(killers.BaseGained >= 45 && killers.BaseGained <= 55, $"half of 90 to 110, not {killers.BaseGained}");
            Assert.AreEqual(killers.BaseGained * 2, killers.Gained);
            Assert.AreEqual(killers.Gained, mates.Gained);

            Assert.AreEqual(0, KillStreaks.CreditOf(away));

            var aways = Sent(away);

            Assert.IsEmpty(Gains(aways));
            Assert.IsEmpty(Streaks(aways));

            // Each has their own: one who joins the kills late is paid at their own level.
            KillShares.SharersOf = (killer, corpse) => new List<Client> { killer, mate, away };

            var late = Kill(context);

            Assert.AreEqual(2, Gains(late).Single().StreakMod);
            Assert.AreEqual(1, Gains(Sent(away)).Single().StreakMod);
            Assert.AreEqual(KillStreaks.CreditPerKill / 3, KillStreaks.CreditOf(away));
        }

        [TestMethod]
        public void AFinishingMoveIsPaidTwiceAtTheStreakBeforeItAndCountsAsOneKill()
        {
            using var context = Start();
            var client = context.Client;

            // The third kill, finished: both its awards at x1, and one kill counted.
            Kill(context);
            Kill(context);

            var third = Kill(context, CritKill.Own);
            var thirds = Gains(third);

            Assert.HasCount(2, thirds);
            Assert.IsTrue(thirds.All(gain => gain.StreakMod == 1 && gain.Gained == gain.BaseGained));
            CollectionAssert.AreEqual(new[] { 1 }, Streaks(third));
            Assert.AreEqual(KillStreaks.CreditPerLevel, KillStreaks.CreditOf(client));

            // The fourth, finished: both at x2, the second "by Crit Killing".
            var fourth = Gains(Kill(context, CritKill.Own));

            Assert.HasCount(2, fourth);
            Assert.IsTrue(fourth.All(gain => gain.StreakMod == 2 && gain.Gained == gain.BaseGained * 2));
            Assert.AreEqual(fourth[0].Gained, fourth[1].Gained);
            Assert.IsFalse(fourth[0].WasCritKill);
            Assert.IsTrue(fourth[1].WasCritKill);
        }

        [TestMethod]
        public void LeavingEndsItAndAnotherCharacterStartsWithNone()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var alone = new[] { client };

            // Nothing to end, nothing said.
            KillStreaks.End(client);
            KillStreaks.End(null);
            Assert.IsEmpty(Sent(client));

            // Credit and no level: ended, nothing said.
            KillStreaks.KillCounted(alone);
            KillStreaks.End(client);
            Assert.IsEmpty(Sent(client));
            Assert.AreEqual(0, KillStreaks.CreditOf(client));

            // A level: the client is told it is over, as the player leaves the map.
            KillStreaks.KillCounted(alone);
            KillStreaks.KillCounted(alone);
            KillStreaks.KillCounted(alone);
            Assert.AreEqual(1, KillStreaks.LevelOf(client));
            Sent(client);

            KillStreaks.End(client);
            CollectionAssert.AreEqual(new[] { 0 }, Streaks(Sent(client)));
            Assert.AreEqual(0, KillStreaks.LevelOf(client));

            // Dying does not end it: only time does.
            KillStreaks.KillCounted(alone);
            KillStreaks.KillCounted(alone);
            KillStreaks.KillCounted(alone);
            client.Player.State = CharacterState.Dead;
            KillStreaks.Worker();
            Assert.AreEqual(1, KillStreaks.LevelOf(client));
            client.Player.State = CharacterState.Idle;
            Sent(client);

            // A streak whose connection has gone is dropped by the worker with nothing sent.
            client.State = RasaGameClientState.Disconnected;
            KillStreaks.Worker();
            client.State = RasaGameClientState.Ingame;
            Assert.IsEmpty(Sent(client));
            Assert.AreEqual(0, KillStreaks.LevelOf(client));

            // Someone not in the game is counted no kill.
            client.State = RasaGameClientState.Loading;
            KillStreaks.KillCounted(alone);
            KillStreaks.Set(client, 3);
            client.State = RasaGameClientState.Ingame;
            Assert.AreEqual(0, KillStreaks.CreditOf(client));
            Assert.IsEmpty(Sent(client));

            KillStreaks.KillCounted(null);
            KillStreaks.KillCounted(Array.Empty<Client>());
            Assert.AreEqual(0, KillStreaks.LevelOf(null));
            Assert.AreEqual(1, KillStreaks.MultiplierOf(null));
        }

        [TestMethod]
        public void AGameMasterSetsARealStreakThatMultipliesAndLapses()
        {
            using var context = Start();
            var client = context.Client;
            client.AccountEntry.Level = (byte)GmLevel.Admin;

            var commands = new ChatCommandsManager(null);
            commands.RegisterChatCommands();

            List<int> Say(string command)
            {
                Sent(client);
                commands.ProcessCommand(client, command);
                return Streaks(Sent(client));
            }

            // x4, on a level 1 character: more than its level allows, which is a game master's to give.
            CollectionAssert.AreEqual(new[] { 3 }, Say(".setkillstreak 3"));
            Assert.AreEqual(3, KillStreaks.LevelOf(client));
            Assert.IsEmpty(_prestige, "no prestige for a streak that was given");

            var gained = Gains(Kill(context)).Single();

            Assert.AreEqual(4, gained.StreakMod);
            Assert.AreEqual(gained.BaseGained * 4, gained.Gained);
            Assert.AreEqual(3, KillStreaks.LevelOf(client), "a kill does not take it down to what the level allows");

            Assert.IsEmpty(Say(".setkillstreak 3"), "the same again says nothing");
            CollectionAssert.AreEqual(new[] { 5 }, Say(".setkillstreak 9"));
            CollectionAssert.AreEqual(new[] { 0 }, Say(".setkillstreak 0"));
            Assert.AreEqual(0, KillStreaks.LevelOf(client));
            Assert.IsEmpty(Say(".setkillstreak 0"));
            Assert.IsEmpty(Say(".setkillstreak -4"), "under nothing is nothing");

            // It lapses as any other.
            CollectionAssert.AreEqual(new[] { 2 }, Say(".setkillstreak 2"));
            _clock += 8000;
            KillStreaks.Worker();
            CollectionAssert.AreEqual(new[] { 0 }, Streaks(Sent(client)));
            Assert.IsEmpty(_prestige);
        }

        [TestMethod]
        public void PrestigeThatCouldNotBeKeptIsNotAnnouncedAndTheStreakStands()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var alone = new[] { client };

            PvpPrestige.Change = (who, amount) => false;

            KillStreaks.KillCounted(alone);
            KillStreaks.KillCounted(alone);
            KillStreaks.KillCounted(alone);

            var sent = Sent(client);

            CollectionAssert.AreEqual(new[] { 1 }, Streaks(sent));
            Assert.IsEmpty(PrestigeLines(sent));

            PvpPrestige.Change = (who, amount) => throw new InvalidOperationException("no database");
            KillStreaks.End(client);
            Sent(client);

            KillStreaks.KillCounted(alone);
            KillStreaks.KillCounted(alone);
            KillStreaks.KillCounted(alone);

            sent = Sent(client);
            CollectionAssert.AreEqual(new[] { 1 }, Streaks(sent));
            Assert.IsEmpty(PrestigeLines(sent));
            Assert.AreEqual(1, KillStreaks.LevelOf(client));
        }

        [TestMethod]
        public void ACharacterAtTheLevelCapKeepsAStreakForItsPrestige()
        {
            using var context = Start();
            var client = context.Client;

            client.Player.Level = ManifestationManager.MaxPlayerLevel;

            for (var kill = 1; kill <= 15; kill++)
                Assert.IsEmpty(Gains(Kill(context)), "no experience at the cap");

            Assert.AreEqual(5, KillStreaks.LevelOf(client));
            CollectionAssert.AreEqual(new[] { (client, 1) }, _prestige);
        }

        [TestMethod]
        public void WhatTheClientIsSentIsTheLevelAsOneNumber()
        {
            // clientmethod.Recv_SetKillStreak(count)
            var bytes = MissionTestContext.Encode(new SetKillStreakPacket(3));
            var none = MissionTestContext.Encode(new SetKillStreakPacket(0));

            Assert.AreEqual(GameOpcode.SetKillStreak, new SetKillStreakPacket(3).Opcode);
            Assert.AreEqual(367u, (uint)GameOpcode.SetKillStreak);
            Assert.IsTrue(bytes.Length > 0 && !bytes.SequenceEqual(none));

            // ExperienceChanged's tuple: total, gained, base, groupMod, streakMod, boosterMod, wasCritKill, wasTeamCritKill.
            var plain = MissionTestContext.Encode(new ExperienceChangedPacket(new XPInfo(500, 200, 100)));
            var streak = MissionTestContext.Encode(new ExperienceChangedPacket(new XPInfo(500, 200, 100) { StreakMod = 2 }));

            Assert.AreEqual(plain.Length, streak.Length);
            Assert.AreEqual(1, plain.Zip(streak, (a, b) => a != b).Count(differs => differs), "one number differs: the streak's");
        }
    }
}
