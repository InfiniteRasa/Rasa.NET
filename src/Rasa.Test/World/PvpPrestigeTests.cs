using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Wargame.Server;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    // Prestige from a clan feud (PvpPrestige): what a kill generates and steals by the levels of
    // the two, the levels a kill has credit across, the interval between generating from the same
    // victim, and what winning is worth.
    [TestClass]
    [DoNotParallelize]
    public class PvpPrestigeTests
    {
        private const uint RedClanId = 900041;
        private const uint BlueClanId = 900042;

        private Func<Client, int, bool> _change;
        private Func<long> _now;
        private int _victory;
        private long _tick;

        [TestInitialize]
        public void KeepPrestigeInMemory()
        {
            _change = PvpPrestige.Change;
            _now = PvpPrestige.Now;
            _victory = PvpPrestige.FeudVictoryPrestige;
            _tick = 1_000_000;

            PvpPrestige.Reset();
            PvpPrestige.Now = () => _tick;
            PvpPrestige.Change = (client, amount) =>
            {
                client.Player.Credits[CurencyType.Prestige] = Prestige(client) + amount;
                return true;
            };
        }

        [TestCleanup]
        public void Restore()
        {
            PvpPrestige.Change = _change;
            PvpPrestige.Now = _now;
            PvpPrestige.FeudVictoryPrestige = _victory;
            PvpPrestige.Reset();
        }

        [TestMethod]
        public void AKillGeneratesByTheLevelsBetweenTenAndFifty()
        {
            Assert.AreEqual(30, PvpPrestige.Generated(10, 10));
            Assert.AreEqual(22, PvpPrestige.Generated(14, 10), "the killer four above");
            Assert.AreEqual(46, PvpPrestige.Generated(2, 10), "the killer eight below");
            Assert.AreEqual(50, PvpPrestige.Generated(1, 30));
            Assert.AreEqual(10, PvpPrestige.Generated(40, 1));
        }

        [TestMethod]
        public void AKillStealsAPercentageByTheLevelsBetweenNoneAndTwo()
        {
            Assert.AreEqual(1.0, PvpPrestige.StolenPercentage(10, 10), 1e-9);
            Assert.AreEqual(0.6, PvpPrestige.StolenPercentage(14, 10), 1e-9);
            Assert.AreEqual(1.8, PvpPrestige.StolenPercentage(2, 10), 1e-9);
            Assert.AreEqual(2.0, PvpPrestige.StolenPercentage(1, 30), 1e-9);
            Assert.AreEqual(0.0, PvpPrestige.StolenPercentage(40, 1), 1e-9);

            Assert.AreEqual(10, PvpPrestige.Stolen(10, 10, 1050), "rounded down");
            Assert.AreEqual(0, PvpPrestige.Stolen(10, 10, 99));
            Assert.AreEqual(0, PvpPrestige.Stolen(10, 10, 0));
        }

        [TestMethod]
        public void AKillHasCreditFourLevelsDownAndEightUp()
        {
            Assert.IsTrue(PvpPrestige.HasKillCredit(10, 10));
            Assert.IsTrue(PvpPrestige.HasKillCredit(14, 10));
            Assert.IsFalse(PvpPrestige.HasKillCredit(15, 10), "five above the victim");
            Assert.IsTrue(PvpPrestige.HasKillCredit(2, 10));
            Assert.IsFalse(PvpPrestige.HasKillCredit(1, 10), "nine below the victim");
        }

        [TestMethod]
        public void AFeudKillGivesTheKillerWhatItGeneratesAndWhatItSteals()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId, level: 10, prestige: 5);
            var blue = Fighter(world, BlueClanId, level: 10, prestige: 1000, x: 10);

            WithFeud(world, feud =>
            {
                Assert.IsTrue(ClanFeuds.Instance.Kill(red, blue));

                Assert.AreEqual(45, Prestige(red), "30 generated and 1% of 1000 stolen");
                Assert.AreEqual(990, Prestige(blue));
                Assert.AreEqual(1, feud.ChallengerKills);

                var gained = Messages(red).Single(m => m.MsgId == PlayerMessage.PmPrestigePointsReceivedPvpkill);
                Assert.AreEqual("40", gained.Args["amount"]);
                Assert.AreEqual("30", gained.Args["amountGenerated"]);
                Assert.AreEqual("10", gained.Args["amountStolen"]);
                Assert.AreEqual(blue.Player.FamilyName, gained.Args["playerName"]);
                Assert.AreEqual(MsgFilterId.PrestigeGainLose, gained.Filterid);

                var lost = Messages(blue).Single(m => m.MsgId == PlayerMessage.PmPrestigePointsRemovedPvpdeath);
                Assert.AreEqual("10", lost.Args["amount"]);
                Assert.AreEqual(red.Player.FamilyName, lost.Args["playerName"]);
            });
        }

        [TestMethod]
        public void TheSameVictimGeneratesAgainOnlyAfterTenMinutes()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId, level: 10, prestige: 0);
            var blue = Fighter(world, BlueClanId, level: 10, prestige: 1000, x: 10);
            var blueMate = Fighter(world, BlueClanId, level: 10, prestige: 0, x: 12);

            WithFeud(world, feud =>
            {
                ClanFeuds.Instance.Kill(red, blue);
                Assert.AreEqual(40, Prestige(red));

                _tick += 9 * 60_000;
                ClanFeuds.Instance.Kill(red, blue);
                Assert.AreEqual(49, Prestige(red), "nothing generated, 1% of 990 stolen");
                Assert.AreEqual(981, Prestige(blue));
                Assert.AreEqual(2, feud.ChallengerKills, "the kill still counts for the feud");

                ClanFeuds.Instance.Kill(red, blueMate);
                Assert.AreEqual(79, Prestige(red), "another victim generates");

                _tick += 60_000;
                ClanFeuds.Instance.Kill(red, blue);
                Assert.AreEqual(79 + 30 + 9, Prestige(red), "ten minutes on");
            });
        }

        [TestMethod]
        public void AKillAcrossTooManyLevelsHasNoCreditAndGivesNothing()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId, level: 20, prestige: 0);
            var blue = Fighter(world, BlueClanId, level: 10, prestige: 1000, x: 10);

            WithFeud(world, feud =>
            {
                Drain(red);

                Assert.IsFalse(ClanFeuds.Instance.Kill(red, blue));

                Assert.AreEqual(0, feud.ChallengerKills);
                Assert.AreEqual(0, Prestige(red));
                Assert.AreEqual(1000, Prestige(blue));

                var packets = Drain(red);
                Assert.IsTrue(packets.OfType<DisplayWargameMessagePacket>().Any(m => m.Message == PlayerMessage.PmWargameFeudNoKillCreditLevels));
                Assert.IsFalse(packets.OfType<WargameScoreboardPacket>().Any());

                // The other way it has credit: ten below is too many, eight is not.
                red.Player.Level = 18;
                Assert.IsTrue(ClanFeuds.Instance.Kill(blue, red));
                Assert.AreEqual(1000 + 46, Prestige(blue));
            });
        }

        [TestMethod]
        public void AKillInAFeudThroughDamageGivesItsPrestige()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId, level: 10, prestige: 0);
            var blue = Fighter(world, BlueClanId, level: 10, prestige: 200, x: 10);
            CellManager.Instance.AddToWorld(red);
            CellManager.Instance.AddToWorld(blue);

            WithFeud(world, feud =>
            {
                blue.Player.Attributes[Attributes.Health].Current = 40;
                ActorManager.Instance.Damage(world.Map, blue.Player, 200, red.Player);

                Assert.AreEqual(1, feud.ChallengerKills);
                Assert.AreEqual(32, Prestige(red));
                Assert.AreEqual(198, Prestige(blue));
            });
        }

        [TestMethod]
        public void WinningAFeudIsWorthNothingUntilAValueIsSet()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId, level: 10, prestige: 7);
            var blue = Fighter(world, BlueClanId, level: 10, prestige: 7, x: 10);

            Assert.AreEqual(0, PvpPrestige.FeudVictoryPrestige);

            WithFeud(world, feud => ClanFeuds.Instance.End(feud, ClanFeuds.Outcome.Won, RedClanId));

            Assert.AreEqual(7, Prestige(red));
            Assert.AreEqual(7, Prestige(blue));
            Assert.IsFalse(Messages(red).Any(m => m.MsgId == PlayerMessage.PmPrestigePointsReceived));

            PvpPrestige.FeudVictoryPrestige = 25;

            WithFeud(world, feud => ClanFeuds.Instance.End(feud, ClanFeuds.Outcome.Won, RedClanId));

            Assert.AreEqual(32, Prestige(red), "to the winners");
            Assert.AreEqual(7, Prestige(blue));
            Assert.AreEqual("25", Messages(red).Single(m => m.MsgId == PlayerMessage.PmPrestigePointsReceived).Args["amount"]);

            WithFeud(world, feud => ClanFeuds.Instance.End(feud, ClanFeuds.Outcome.Tied));
            Assert.AreEqual(32, Prestige(red), "a tie is no win");
        }

        private static int Prestige(Client client) =>
            client.Player.Credits.TryGetValue(CurencyType.Prestige, out var prestige) ? prestige : 0;

        private static List<PythonPacket> Drain(Client client) => MissionTestContext.Drain(client).ToList();

        private static List<DisplayClientMessagePacket> Messages(Client client) => Drain(client).OfType<DisplayClientMessagePacket>().ToList();

        private static Client Fighter(WorldTestContext world, uint clanId, byte level, int prestige, float x = 0)
        {
            var client = world.CreateClient(x, 0);
            client.Player.ClanId = clanId;
            client.Player.Level = level;
            client.Player.FamilyName = $"Fighter{client.Player.Id}";
            client.Player.Credits[CurencyType.Prestige] = prestige;
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            return client;
        }

        /// <summary>Red challenges Blue and Blue accepts; the fighters are online for the feud's messages.</summary>
        private static void WithFeud(WorldTestContext world, Action<ClanFeuds.Feud> body)
        {
            var online = world.Map.ClientList.ToList();

            lock (Server.Clients)
                Server.Clients.AddRange(online);

            ClanFeuds.Feud feud = null;

            try
            {
                feud = ClanFeuds.Instance.Start(
                    new ClanEntry { Id = RedClanId, Name = "Red", IsPvP = true },
                    new ClanEntry { Id = BlueClanId, Name = "Blue", IsPvP = true });
                Assert.IsNotNull(feud);

                body(feud);
            }
            finally
            {
                if (feud != null)
                    ClanFeuds.Instance.End(feud, ClanFeuds.Outcome.Cancelled);

                lock (Server.Clients)
                    foreach (var client in online)
                        Server.Clients.Remove(client);
            }
        }
    }
}
