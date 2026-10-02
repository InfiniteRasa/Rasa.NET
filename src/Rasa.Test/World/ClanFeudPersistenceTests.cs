using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Context.Char;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Wargame.Server;
    using Rasa.Repositories.Char.ClanFeud;
    using Rasa.Structures.Char;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;

    // Clan feuds and feud challenges kept through a restart (ClanFeuds.Load, the clan_feud and
    // clan_feud_challenge tables): each restart is a new ClanFeuds reading the same database.
    [TestClass]
    [DoNotParallelize]
    public class ClanFeudPersistenceTests
    {
        private const uint RedId = 1, BlueId = 2, GreenId = 3;
        private const long Hour = 60 * 60 * 1000L;

        private string _directory;
        private string _database;
        private long _utc = 1_800_000_000_000;

        [TestInitialize]
        public void MigrateACharacterDatabase()
        {
            _directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _database = Path.Combine(_directory, "database");

            using var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteCharContext), _database);
            context.Database.Migrate();
        }

        [TestCleanup]
        public void DeleteTheDatabase()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Directory.Delete(_directory, true);
        }

        [TestMethod]
        public void AFeudAndAChallengeComeBackAfterARestart()
        {
            using var world = new WorldTestContext();
            var clans = new Clans(world);
            var first = Restart(clans, tick: 1_000_000);

            var feud = first.Start(clans.All[RedId], clans.All[BlueId]);
            Assert.IsTrue(first.Kill(clans.Leader(RedId), clans.Member(BlueId)));
            first.ChallengeClanToFeud(clans.Leader(GreenId), "Blue", true);
            var challenge = first.Challenges.Single();

            // An hour down, and the new process's clock starts somewhere else.
            _utc += Hour;
            var second = Restart(clans, tick: 50);

            var back = second.Feuds.Single();
            Assert.AreEqual(feud.Id, back.Id);
            Assert.AreEqual(RedId, back.ChallengerClanId);
            Assert.AreEqual(BlueId, back.TargetClanId);
            Assert.AreEqual(1, back.ChallengerKills);
            Assert.AreEqual(0, back.TargetKills);
            Assert.AreEqual((int)((ClanFeuds.DefaultDuration.TotalMilliseconds - Hour) / 1000), second.SecondsLeft(back), 1, "the clock ran on while it was down");
            Assert.IsTrue(second.AreFeuding(RedId, BlueId));

            var backChallenge = second.Challenges.Single();
            Assert.AreEqual(challenge.WargameId, backChallenge.WargameId);
            Assert.AreEqual(GreenId, backChallenge.ChallengerClanId);
            Assert.AreEqual(BlueId, backChallenge.TargetClanId);

            // New ids follow on from the kept ones.
            Assert.IsTrue(second.Start(clans.All[RedId], clans.All[GreenId]).Id > challenge.WargameId);
        }

        [TestMethod]
        public void WhoMadeAndAcceptedTheChallengeIsKeptAndAWonFeudForfeitsToTheWinners()
        {
            using var world = new WorldTestContext();
            var clans = new Clans(world);
            var greenLead = clans.Leader(GreenId).Player.Id;
            var blueLead = clans.Leader(BlueId).Player.Id;

            Restart(clans, tick: 1000).ChallengeClanToFeud(clans.Leader(GreenId), "Blue", true);
            Assert.AreEqual(greenLead, Rows().Challenges.Single().ChallengerCharacterId);

            Restart(clans, tick: 1000).FeudChallengeResponse(clans.Leader(BlueId), "Green", true);

            var row = Rows().Feuds.Single();
            Assert.AreEqual(greenLead, row.ChallengerCharacterId);
            Assert.AreEqual(blueLead, row.TargetCharacterId);

            var feuds = Restart(clans, tick: 1000);
            var forfeits = new List<(uint Loser, uint Winner, uint Character, string LoserName, string WinnerName)>();
            feuds.Forfeit = (loser, winner, character, loserName, winnerName, departed) => forfeits.Add((loser, winner, character, loserName, winnerName));

            // The challenged clan wins: the one who accepted stands for it.
            feuds.End(feuds.Feuds.Single(), ClanFeuds.Outcome.Won, BlueId);
            CollectionAssert.AreEqual(new[] { (GreenId, BlueId, blueLead, "Green", "Blue") }, forfeits);

            // The challenger wins: the one who made the challenge.
            feuds.End(feuds.Start(clans.All[RedId], clans.All[BlueId], 0, 41, 42), ClanFeuds.Outcome.Won, RedId);
            Assert.AreEqual((BlueId, RedId, 41u, "Blue", "Red"), forfeits[1]);

            // A feud nobody challenged for names nobody; a tie and a cancelled feud forfeit nothing.
            feuds.End(feuds.Start(clans.All[RedId], clans.All[GreenId]), ClanFeuds.Outcome.Won, GreenId);
            Assert.AreEqual((RedId, GreenId, 0u, "Red", "Green"), forfeits[2]);

            feuds.End(feuds.Start(clans.All[RedId], clans.All[GreenId]), ClanFeuds.Outcome.Tied);
            feuds.End(feuds.Start(clans.All[RedId], clans.All[GreenId]), ClanFeuds.Outcome.Cancelled);
            Assert.AreEqual(3, forfeits.Count);

            // A forfeit that throws does not stop the feud from ending.
            feuds.Forfeit = (loser, winner, character, loserName, winnerName, departed) => throw new InvalidOperationException("no lockbox");
            feuds.End(feuds.Start(clans.All[RedId], clans.All[GreenId]), ClanFeuds.Outcome.Won, RedId);
            Assert.AreEqual(0, feuds.Feuds.Count);
        }

        [TestMethod]
        public void WhoLeavesAClanAtFeudWithAWagerStaysAtStakeThroughARestart()
        {
            using var world = new WorldTestContext();
            var clans = new Clans(world);
            var first = Restart(clans, tick: 1000);
            var redBlue = first.Start(clans.All[RedId], clans.All[BlueId]);
            var redGreen = first.Start(clans.All[RedId], clans.All[GreenId]);
            var leaver = clans.Member(RedId);
            var leaverId = leaver.Player.Id;

            // Nothing wagered: nothing at stake.
            first.HoldsWager = id => false;
            Assert.AreEqual(0, first.MemberRemoved(leaverId, RedId, leaver).Count);
            Assert.AreEqual(0, Stakes().Count);
            Assert.AreEqual(0, redBlue.Departed.Count);

            // A clan that is in no feud: nothing either.
            first.HoldsWager = id => true;
            Assert.AreEqual(0, first.MemberRemoved(leaverId, 99, leaver).Count);

            // With an item wagered: at stake in each of the clan's feuds, and told so.
            first.HoldsWager = id => id == leaverId;
            Drain(leaver);
            Assert.AreEqual(2, first.MemberRemoved(leaverId, RedId, leaver).Count);
            Assert.IsTrue(Drain(leaver).OfType<Rasa.Packets.Communicator.Server.SystemMessagePacket>().Any(p => p.TextMessage.Contains("at stake")));

            // Kicked while not in the world, and nobody to ask what they hold: taken to hold one.
            first.HoldsWager = null;
            Assert.AreEqual(1, first.MemberRemoved(500, BlueId).Count);

            CollectionAssert.AreEquivalent(
                new[] { (redBlue.Id, leaverId, RedId), (redGreen.Id, leaverId, RedId), (redBlue.Id, 500u, BlueId) },
                Stakes().Select(s => (s.FeudId, s.CharacterId, s.ClanId)).ToArray());

            // A restart, and Blue wins: who left Red is forfeit with Red; who left Blue is not.
            var second = Restart(clans, tick: 1000);
            var forfeits = new List<(uint Loser, uint Winner, uint[] Departed)>();
            second.Forfeit = (loser, winner, character, loserName, winnerName, departed) => forfeits.Add((loser, winner, departed.ToArray()));

            var back = second.Feuds.Single(f => f.Id == redBlue.Id);
            Assert.AreEqual(RedId, back.Departed[leaverId]);
            Assert.AreEqual(BlueId, back.Departed[500]);

            second.End(back, ClanFeuds.Outcome.Won, BlueId);

            Assert.AreEqual(1, forfeits.Count);
            Assert.AreEqual((RedId, BlueId), (forfeits[0].Loser, forfeits[0].Winner));
            CollectionAssert.AreEqual(new[] { leaverId }, forfeits[0].Departed);

            // The stakes go with their feud; the other feud's stand until it ends.
            CollectionAssert.AreEqual(new[] { (redGreen.Id, leaverId, RedId) }, Stakes().Select(s => (s.FeudId, s.CharacterId, s.ClanId)).ToArray());

            // Red wins that one: nobody left Green, and nothing of Red's is forfeit.
            second.End(second.Feuds.Single(), ClanFeuds.Outcome.Won, RedId);
            Assert.AreEqual((GreenId, RedId), (forfeits[1].Loser, forfeits[1].Winner));
            Assert.AreEqual(0, forfeits[1].Departed.Length);
            Assert.AreEqual(0, Stakes().Count);
        }

        [TestMethod]
        public void TheChallengedLeaderIsRemindedOnceAndCanStillAnswer()
        {
            using var world = new WorldTestContext();
            var clans = new Clans(world);
            Restart(clans, tick: 1000).ChallengeClanToFeud(clans.Leader(GreenId), "Blue", true);

            var second = Restart(clans, tick: 1000);
            var blueLead = clans.Leader(BlueId);
            Drain(blueLead);

            second.PlayerEnteredWorld(clans.Member(BlueId));
            Assert.AreEqual(0, Drain(clans.Member(BlueId)).OfType<ClanWargameInviteReceivedPacket>().Count(), "only the leader answers");

            second.PlayerEnteredWorld(blueLead);
            var invite = Drain(blueLead).OfType<ClanWargameInviteReceivedPacket>().Single();
            Assert.AreEqual("Green", invite.ClanName);

            second.PlayerEnteredWorld(blueLead);
            Assert.AreEqual(0, Drain(blueLead).OfType<ClanWargameInviteReceivedPacket>().Count(), "once: a map link is not a new client");

            second.FeudChallengeResponse(blueLead, "Green", true);
            Assert.IsTrue(second.AreFeuding(GreenId, BlueId));
            Assert.AreEqual(invite.WargameId, second.Feuds.Single().Id, "the feud takes the challenge's id");

            Assert.AreEqual(0, Rows().Challenges.Count);
            Assert.AreEqual(invite.WargameId, Rows().Feuds.Single().Id);
        }

        [TestMethod]
        public void AnEndedFeudAndAnAnsweredOrRevokedChallengeAreNotKept()
        {
            using var world = new WorldTestContext();
            var clans = new Clans(world);
            var first = Restart(clans, tick: 1000);

            first.End(first.Start(clans.All[RedId], clans.All[BlueId]), ClanFeuds.Outcome.Tied);
            first.ChallengeClanToFeud(clans.Leader(GreenId), "Blue", true);
            first.FeudChallengeResponse(clans.Leader(BlueId), "Green", false);
            first.ChallengeClanToFeud(clans.Leader(RedId), "Green", true);
            first.RevokeClanFeud(clans.Leader(RedId), "Green");

            Assert.AreEqual(0, Rows().Feuds.Count);
            Assert.AreEqual(0, Rows().Challenges.Count);

            var second = Restart(clans, tick: 1000);
            Assert.AreEqual(0, second.Feuds.Count);
            Assert.AreEqual(0, second.Challenges.Count);
        }

        [TestMethod]
        public void AFeudThatRanOutWhileTheServerWasDownEndsOnTheFirstTick()
        {
            using var world = new WorldTestContext();
            var clans = new Clans(world);
            var first = Restart(clans, tick: 1000);
            first.Start(clans.All[RedId], clans.All[BlueId]);
            first.Kill(clans.Member(BlueId), clans.Leader(RedId));

            _utc += 8 * 24 * Hour;
            var second = Restart(clans, tick: 1000);
            var blueMate = clans.Member(BlueId);
            Drain(blueMate);

            Assert.AreEqual(0, second.SecondsLeft(second.Feuds.Single()));
            second.Worker();

            Assert.AreEqual(0, second.Feuds.Count);
            Assert.AreEqual(GameOpcode.WargameVictory, Drain(blueMate).OfType<WargameResultPacket>().Single().Opcode, "the score it was left on decides it");
            Assert.AreEqual(0, Rows().Feuds.Count);
        }

        [TestMethod]
        public void RowsNamingAClanThatIsGoneAreDropped()
        {
            using var world = new WorldTestContext();
            var clans = new Clans(world);
            var first = Restart(clans, tick: 1000);
            first.Start(clans.All[RedId], clans.All[BlueId]);
            first.ChallengeClanToFeud(clans.Leader(GreenId), "Red", true);

            // Red is gone by the time the server comes back.
            clans.All.Remove(RedId);
            var second = Restart(clans, tick: 1000);

            Assert.AreEqual(0, second.Feuds.Count);
            Assert.AreEqual(0, second.Challenges.Count);
            Assert.AreEqual(0, Rows().Feuds.Count);
            Assert.AreEqual(0, Rows().Challenges.Count);
        }

        [TestMethod]
        public void WithoutAStoreNothingIsKept()
        {
            using var world = new WorldTestContext();
            var clans = new Clans(world);
            var feuds = new ClanFeuds(clans);
            feuds.Load(null);

            feuds.Start(clans.All[RedId], clans.All[BlueId]);

            Assert.AreEqual(0, Rows().Feuds.Count);
        }

        /// <summary>A server starting: a new ClanFeuds with its own clock, reading the database back.</summary>
        private ClanFeuds Restart(Clans clans, long tick)
        {
            var feuds = new ClanFeuds(clans) { Now = () => tick, UtcNow = () => _utc };
            feuds.Load(new SqliteStore(_database));
            return feuds;
        }

        private (List<ClanFeudEntry> Feuds, List<ClanFeudChallengeEntry> Challenges) Rows() => new SqliteStore(_database).Load();

        private List<ClanFeudStakeEntry> Stakes() => new SqliteStore(_database).LoadStakes();

        private static List<PythonPacket> Drain(Client client) => MissionTestContext.Drain(client).ToList();

        /// <summary>The store the server has, over a database file: a context per call.</summary>
        private sealed class SqliteStore : ClanFeuds.IStore
        {
            private readonly string _database;

            public SqliteStore(string database)
            {
                _database = database;
            }

            private T With<T>(Func<ClanFeudRepository, T> action)
            {
                using var context = (CharContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteCharContext), _database);
                return action(new ClanFeudRepository(context));
            }

            public (List<ClanFeudEntry> Feuds, List<ClanFeudChallengeEntry> Challenges) Load() => With(r => (r.GetFeuds(), r.GetChallenges()));
            public void SaveFeud(ClanFeudEntry feud) => With(r => { r.SaveFeud(feud); return 0; });
            public void DeleteFeud(uint id) => With(r => { r.DeleteFeud(id); return 0; });
            public void SaveChallenge(ClanFeudChallengeEntry challenge) => With(r => { r.SaveChallenge(challenge); return 0; });
            public void DeleteChallenge(uint wargameId) => With(r => { r.DeleteChallenge(wargameId); return 0; });
            public List<ClanFeudStakeEntry> LoadStakes() => With(r => r.GetStakes());
            public void SaveStake(ClanFeudStakeEntry stake) => With(r => { r.SaveStake(stake); return 0; });
        }

        /// <summary>Three PvP clans, each with a leader and a member online.</summary>
        private sealed class Clans : ClanFeuds.IClans
        {
            public Dictionary<uint, ClanEntry> All { get; } = new Dictionary<uint, ClanEntry>();
            private readonly Dictionary<uint, List<Client>> _members = new Dictionary<uint, List<Client>>();

            public Clans(WorldTestContext world)
            {
                foreach (var (id, name) in new[] { (RedId, "Red"), (BlueId, "Blue"), (GreenId, "Green") })
                {
                    All[id] = new ClanEntry { Id = id, Name = name, IsPvP = true };
                    _members[id] = new List<Client>();

                    for (var i = 0; i < 2; i++)
                    {
                        var client = world.CreateClient(id * 10 + i, 0);
                        client.Player.ClanId = id;
                        client.Player.FamilyName = name + (i == 0 ? "Lead" : "Mate");
                        _members[id].Add(client);
                    }
                }
            }

            public Client Leader(uint clanId) => _members[clanId][0];
            public Client Member(uint clanId) => _members[clanId][1];

            public ClanEntry Find(uint clanId) => All.GetValueOrDefault(clanId);

            public ClanEntry FindByName(string name) => All.Values.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

            public byte? RankOf(uint clanId, uint characterId)
            {
                if (!_members.TryGetValue(clanId, out var members))
                    return null;

                var index = members.FindIndex(c => c.Player.Id == characterId);
                return index < 0 ? null : index == 0 ? ClanRank.MinRankToChallenge : (byte)0;
            }

            public List<Client> Online(uint clanId) => _members.TryGetValue(clanId, out var members) ? members.ToList() : new List<Client>();
        }
    }
}
