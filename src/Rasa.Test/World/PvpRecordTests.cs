using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Context.Char;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Repositories.Char.ClanFeud;
    using Rasa.Repositories.Char.PvpRecord;
    using Rasa.Structures.Char;
    using Rasa.Test.Database;

    /// <summary>The records kept in memory: what a store was given, as it was given it.</summary>
    internal sealed class MemoryPvpStore : PvpRecords.IStore
    {
        private static readonly MethodInfo Clone = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);

        private uint _nextId = 1;

        internal List<PvpMatchEntry> Matches { get; } = new List<PvpMatchEntry>();
        internal Dictionary<uint, Dictionary<uint, PvpMatchPlayerEntry>> Rows { get; } = new Dictionary<uint, Dictionary<uint, PvpMatchPlayerEntry>>();

        /// <summary>Set, every call throws it.</summary>
        internal Exception Fails { get; set; }

        internal int Writes { get; private set; }

        private static T Copy<T>(T entry) => (T)Clone.Invoke(entry, null);

        public uint SaveMatch(PvpMatchEntry match)
        {
            if (Fails != null)
                throw Fails;

            Writes++;

            var copy = Copy(match);

            if (copy.Id == 0)
                copy.Id = _nextId++;

            Matches.RemoveAll(m => m.Id == copy.Id);
            Matches.Add(copy);

            return copy.Id;
        }

        public void SavePlayers(uint matchId, IReadOnlyCollection<PvpMatchPlayerEntry> players)
        {
            if (Fails != null)
                throw Fails;

            Writes++;

            if (!Rows.TryGetValue(matchId, out var rows))
                Rows[matchId] = rows = new Dictionary<uint, PvpMatchPlayerEntry>();

            foreach (var player in players)
            {
                var copy = Copy(player);

                copy.MatchId = matchId;
                rows[copy.CharacterId] = copy;
            }
        }

        public List<PvpMatchEntry> OpenFeuds()
        {
            if (Fails != null)
                throw Fails;

            return Matches.Where(m => m.Kind == (byte)PvpMatchKind.ClanFeud && m.EndedAt == null).Select(Copy).ToList();
        }

        public List<PvpMatchPlayerEntry> Players(uint matchId) =>
            Rows.TryGetValue(matchId, out var rows) ? rows.Values.OrderBy(r => r.Side).ThenBy(r => r.CharacterId).Select(Copy).ToList() : new List<PvpMatchPlayerEntry>();

        /// <summary>The row a character has in a match.</summary>
        internal PvpMatchPlayerEntry Row(uint matchId, uint characterId) => Rows[matchId][characterId];
    }

    /// <summary>
    /// The records of PvP matches (PvpRecords, the pvp_match and pvp_match_player tables): what
    /// is written of a match that has ended, and of a clan feud from its start to its end and
    /// through a restart. The squad wargame's and the battleground's own records are with their
    /// tests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PvpRecordTests
    {
        private const uint RedId = 1, BlueId = 2;

        private static readonly DateTime Noon = new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc);

        private string _directory;
        private string _database;
        private DateTime _utc = Noon;

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

        #region Any match

        [TestMethod]
        public void AMatchThatHasEndedIsWrittenWithItsPlayers()
        {
            var store = new MemoryPvpStore();
            var records = new PvpRecords { UtcNow = () => _utc };

            records.Load(store);

            var match = new PvpMatchEntry
            {
                Kind = (byte)PvpMatchKind.SquadWargame,
                WargameId = 12,
                MapContextId = 1220,
                StartedAt = Noon.AddMinutes(-5),
                Outcome = (byte)PvpMatchOutcome.Won,
                WinnerSide = 2,
                Reason = "kills",
                Side1Name = "One",
                Side1Score = 3,
                Side1Kills = 3,
                Side2Name = "Two",
                Side2Score = 5,
                Side2Kills = 5
            };

            records.Record(match, new[]
            {
                new PvpMatchPlayerEntry { CharacterId = 7, Side = 1, FamilyName = "Seven", Kills = 3, Deaths = 5, PresentAtEnd = true },
                new PvpMatchPlayerEntry { CharacterId = 8, Side = 2, FamilyName = "Eight", Kills = 4, Deaths = 3, PresentAtEnd = true },
                new PvpMatchPlayerEntry { CharacterId = 8, Side = 2, FamilyName = "Eight", Kills = 5, Deaths = 3, PresentAtEnd = true },
                new PvpMatchPlayerEntry { CharacterId = 0, Side = 2, FamilyName = "Nobody" },
                null
            });

            var written = store.Matches.Single();

            Assert.AreEqual(1u, written.Id);
            Assert.AreEqual(1u, match.Id, "the caller's is given its id");
            Assert.AreEqual(Noon, written.EndedAt, "ended now, unless the caller said when");
            Assert.AreEqual(Noon.AddMinutes(-5), written.StartedAt);
            Assert.AreEqual((byte)2, written.WinnerSide);
            Assert.AreEqual("kills", written.Reason);
            Assert.AreEqual(5, written.Side2Score);

            Assert.AreEqual(2, store.Rows[1].Count, "one row a character, and none for no character");
            Assert.AreEqual(5, store.Row(1, 8).Kills, "the last said of them");
            Assert.AreEqual("Seven", store.Row(1, 7).FamilyName);

            // The next is another record; one with no start is dated by its end, and one with no players is a match all the same.
            records.Record(new PvpMatchEntry { Kind = (byte)PvpMatchKind.Battleground, WargameId = 12, EndedAt = Noon.AddHours(1) }, null);

            Assert.AreEqual(2, store.Matches.Count);
            Assert.AreEqual(2u, store.Matches[1].Id);
            Assert.AreEqual(Noon.AddHours(1), store.Matches[1].EndedAt);
            Assert.AreEqual(Noon.AddHours(1), store.Matches[1].StartedAt);

            records.Record(null, null);
            Assert.AreEqual(2, store.Matches.Count);
        }

        [TestMethod]
        public void WithNoStoreNothingIsKeptAndAStoreThatFailsBreaksNothing()
        {
            var records = new PvpRecords { UtcNow = () => _utc };
            var feud = new PvpRecords.FeudInfo(4, RedId, "Red", 0, BlueId, "Blue", 0);

            using var world = new WorldTestContext();
            var one = world.CreateClient();
            var other = world.CreateClient();

            // Nowhere to keep them.
            records.Record(new PvpMatchEntry { Kind = (byte)PvpMatchKind.SquadWargame }, null);
            records.FeudStarted(feud);
            records.FeudKill(feud, one.Player, other.Player);
            records.FeudEnded(feud, PvpMatchOutcome.Tied, 0, "time", null);
            records.FeudsRestored(new[] { feud });
            Assert.IsNull(records.Store);

            // A database that is away: every call fails, and none of it reaches the match.
            var store = new MemoryPvpStore { Fails = new InvalidOperationException("the database is away") };

            records.Load(store);
            records.Record(new PvpMatchEntry { Kind = (byte)PvpMatchKind.SquadWargame }, null);
            records.FeudStarted(feud);
            records.FeudKill(feud, one.Player, other.Player);
            records.FeudEnded(feud, PvpMatchOutcome.Tied, 0, "time", null);
            records.FeudsRestored(new[] { feud });

            Assert.AreEqual(0, store.Matches.Count);

            // Back again: a feud whose record could not be opened has one opened by its next kill.
            store.Fails = null;
            records.FeudKill(new PvpRecords.FeudInfo(4, RedId, "Red", 1, BlueId, "Blue", 0), one.Player, other.Player);

            Assert.AreEqual(1, store.Matches.Count);
            Assert.AreEqual(1, store.Matches[0].Side1Kills);
        }

        [TestMethod]
        public void TheTablesKeepWhatTheyAreGiven()
        {
            var first = Records(r => r.SaveMatch(new PvpMatchEntry
            {
                Kind = (byte)PvpMatchKind.ClanFeud,
                WargameId = 3,
                StartedAt = Noon,
                Side1Name = "Red",
                Side1ClanId = RedId,
                Side2Name = "Blue",
                Side2ClanId = BlueId
            }));
            var second = Records(r => r.SaveMatch(new PvpMatchEntry
            {
                Kind = (byte)PvpMatchKind.Battleground,
                WargameId = 9001,
                MapContextId = 2374,
                InstanceId = 4,
                StartedAt = Noon,
                EndedAt = Noon.AddMinutes(20),
                Outcome = (byte)PvpMatchOutcome.Won,
                WinnerSide = 1,
                Reason = "points",
                Side1Name = "Red Team",
                Side1Score = 3,
                Side1Kills = 14,
                Side2Name = "Blue Team",
                Side2Score = 0,
                Side2Kills = 11
            }));

            Assert.AreEqual(1u, first);
            Assert.AreEqual(2u, second);

            // The feud is the one still open; the match that ended is the recent one.
            Assert.AreEqual(first, Records(r => r.GetOpenMatches(PvpMatchKind.ClanFeud)).Single().Id);
            Assert.AreEqual(0, Records(r => r.GetOpenMatches(PvpMatchKind.Battleground)).Count);
            Assert.AreEqual(second, Records(r => r.GetRecentMatches(10)).Single().Id);

            var read = Records(r => r.GetMatch(second));

            Assert.AreEqual((byte)PvpMatchKind.Battleground, read.Kind);
            Assert.AreEqual(9001u, read.WargameId);
            Assert.AreEqual(2374u, read.MapContextId);
            Assert.AreEqual(4u, read.InstanceId);
            Assert.AreEqual(Noon, read.StartedAt);
            Assert.AreEqual(Noon.AddMinutes(20), read.EndedAt);
            Assert.AreEqual((byte)1, read.WinnerSide);
            Assert.AreEqual("points", read.Reason);
            Assert.AreEqual("Blue Team", read.Side2Name);
            Assert.AreEqual(14, read.Side1Kills);
            Assert.AreEqual(3, read.Side1Score);
            Assert.IsNull(Records(r => r.GetMatch(first)).EndedAt);
            Assert.IsNull(Records(r => r.GetMatch(77)));

            // Written again under its id, it is the same row.
            read.Side2Kills = 12;
            Assert.AreEqual(second, Records(r => r.SaveMatch(read)));
            Assert.AreEqual(12, Records(r => r.GetMatch(second)).Side2Kills);
            Assert.AreEqual(2, Records(r => r.GetRecentMatches(10).Count + r.GetOpenMatches(PvpMatchKind.ClanFeud).Count));

            // Players: a row a match and character, written again in place.
            Records(r =>
            {
                r.SavePlayers(second, new[]
                {
                    new PvpMatchPlayerEntry { CharacterId = 20, Side = 2, Name = "Bea", FamilyName = "Blue", ClanId = 5, Kills = 1, PresentAtEnd = true },
                    new PvpMatchPlayerEntry { CharacterId = 10, Side = 1, Name = "Ray", FamilyName = "Red", Kills = 2, Deaths = 1, Damage = 900, Healing = 40, Captures = 3, Prestige = 350 }
                });
                r.SavePlayers(second, new[] { new PvpMatchPlayerEntry { CharacterId = 20, Side = 2, Name = "Bea", FamilyName = "Blue", ClanId = 5, Kills = 4, PresentAtEnd = true } });
                r.SavePlayers(first, new[] { new PvpMatchPlayerEntry { CharacterId = 10, Side = 1, FamilyName = "Red", Kills = 1 } });
                r.SavePlayers(first, new PvpMatchPlayerEntry[0]);
                r.SavePlayers(first, null);
                return 0;
            });

            var players = Records(r => r.GetPlayers(second));

            CollectionAssert.AreEqual(new uint[] { 10, 20 }, players.Select(p => p.CharacterId).ToArray(), "side 1 first");
            Assert.AreEqual(4, players[1].Kills);
            Assert.AreEqual(5u, players[1].ClanId);
            Assert.IsTrue(players[1].PresentAtEnd);
            Assert.AreEqual("Ray", players[0].Name);
            Assert.AreEqual("Red", players[0].FamilyName);
            Assert.AreEqual(900, players[0].Damage);
            Assert.AreEqual(40, players[0].Healing);
            Assert.AreEqual(3, players[0].Captures);
            Assert.AreEqual(350, players[0].Prestige);
            Assert.IsFalse(players[0].PresentAtEnd);
            Assert.AreEqual(1, Records(r => r.GetPlayers(first)).Single().Kills, "the same character in another match is another row");
        }

        #endregion

        #region Clan feuds

        [TestMethod]
        public void AFeudIsOnRecordFromItsStartToItsEnd()
        {
            using var world = new WorldTestContext();
            var clans = new Clans(world);
            var (feuds, _) = Server(clans);

            var feud = feuds.Start(clans.All[RedId], clans.All[BlueId]);
            var open = Records(r => r.GetOpenMatches(PvpMatchKind.ClanFeud)).Single();

            Assert.AreEqual(feud.Id, open.WargameId);
            Assert.AreEqual(Noon, open.StartedAt);
            Assert.IsNull(open.EndedAt);
            Assert.AreEqual((byte)PvpMatchOutcome.Running, open.Outcome);
            Assert.AreEqual("Red", open.Side1Name);
            Assert.AreEqual(RedId, open.Side1ClanId);
            Assert.AreEqual("Blue", open.Side2Name);
            Assert.AreEqual(BlueId, open.Side2ClanId);
            Assert.AreEqual(0, Records(r => r.GetPlayers(open.Id)).Count, "nobody has fought");

            // Three kills: the record is kept up, and those who fought have their rows.
            _utc = Noon.AddHours(2);
            Assert.IsTrue(feuds.Kill(clans.Leader(RedId), clans.Member(BlueId)));
            Assert.IsTrue(feuds.Kill(clans.Leader(RedId), clans.Member(BlueId)));
            Assert.IsTrue(feuds.Kill(clans.Member(BlueId), clans.Leader(RedId)));

            var running = Records(r => r.GetMatch(open.Id));

            Assert.AreEqual(2, running.Side1Score);
            Assert.AreEqual(2, running.Side1Kills);
            Assert.AreEqual(1, running.Side2Score);
            Assert.IsNull(running.EndedAt);

            var fought = Records(r => r.GetPlayers(open.Id));

            Assert.AreEqual(2, fought.Count);
            Assert.AreEqual(clans.Leader(RedId).Player.Id, fought[0].CharacterId);
            Assert.AreEqual((byte)1, fought[0].Side);
            Assert.AreEqual("RedLead", fought[0].FamilyName);
            Assert.AreEqual(clans.Leader(RedId).Player.Name, fought[0].Name);
            Assert.AreEqual(RedId, fought[0].ClanId);
            Assert.AreEqual((2, 1), (fought[0].Kills, fought[0].Deaths));
            Assert.AreEqual((byte)2, fought[1].Side);
            Assert.AreEqual((1, 2), (fought[1].Kills, fought[1].Deaths));

            // Its time is up: Red, with more kills, has won, and both clans go on the roster.
            _utc = Noon.AddDays(7);
            feuds.Expire(feud);

            Assert.AreEqual(0, Records(r => r.GetOpenMatches(PvpMatchKind.ClanFeud)).Count);

            var closed = Records(r => r.GetRecentMatches(5)).Single();

            Assert.AreEqual(open.Id, closed.Id, "the same record, closed");
            Assert.AreEqual(Noon, closed.StartedAt);
            Assert.AreEqual(Noon.AddDays(7), closed.EndedAt);
            Assert.AreEqual((byte)PvpMatchOutcome.Won, closed.Outcome);
            Assert.AreEqual((byte)1, closed.WinnerSide);
            Assert.AreEqual("time", closed.Reason);
            Assert.AreEqual((2, 1), (closed.Side1Score, closed.Side2Score));

            var roster = Records(r => r.GetPlayers(open.Id));

            Assert.AreEqual(4, roster.Count);
            CollectionAssert.AreEqual(new byte[] { 1, 1, 2, 2 }, roster.Select(p => p.Side).ToArray());
            Assert.IsTrue(roster.All(p => p.PresentAtEnd));

            var redMate = roster.Single(p => p.CharacterId == clans.Member(RedId).Player.Id);

            Assert.AreEqual("RedMate", redMate.FamilyName);
            Assert.AreEqual((0, 0), (redMate.Kills, redMate.Deaths), "there, and never fought");
            Assert.AreEqual((2, 1), (roster[0].Kills, roster[0].Deaths), "what was done is not lost with the roster");
        }

        [TestMethod]
        public void HowAFeudEndedIsOnItsRecord()
        {
            using var world = new WorldTestContext();
            var clans = new Clans(world);
            var (feuds, _) = Server(clans);

            PvpMatchEntry Last() => Records(r => r.GetRecentMatches(1)).Single();

            // Surrendered: the other clan's, whatever the score.
            var feud = feuds.Start(clans.All[RedId], clans.All[BlueId]);
            feuds.Kill(clans.Leader(RedId), clans.Member(BlueId));
            _utc = _utc.AddMinutes(1);
            feuds.SurrenderClanFeud(clans.Leader(RedId), "Blue");

            Assert.AreEqual("surrender", Last().Reason);
            Assert.AreEqual((byte)PvpMatchOutcome.Won, Last().Outcome);
            Assert.AreEqual((byte)2, Last().WinnerSide);
            Assert.AreEqual((1, 0), (Last().Side1Score, Last().Side2Score));
            Assert.AreEqual(feud.Id, Last().WargameId);

            // A clan disbanded: the other's.
            feuds.Start(clans.All[BlueId], clans.All[RedId]);
            _utc = _utc.AddMinutes(1);
            feuds.ClanDisbanded(BlueId);

            Assert.AreEqual("disbanded", Last().Reason);
            Assert.AreEqual("Blue", Last().Side1Name, "the challenger this time");
            Assert.AreEqual((byte)2, Last().WinnerSide);

            // Level on kills when the time is up: nobody's.
            var tied = feuds.Start(clans.All[RedId], clans.All[BlueId]);
            _utc = _utc.AddMinutes(1);
            feuds.Expire(tied);

            Assert.AreEqual("time", Last().Reason);
            Assert.AreEqual((byte)PvpMatchOutcome.Tied, Last().Outcome);
            Assert.AreEqual((byte)0, Last().WinnerSide);

            // A game master's: called off, and ended as it stands.
            var cancelled = feuds.Start(clans.All[RedId], clans.All[BlueId]);
            _utc = _utc.AddMinutes(1);
            feuds.End(cancelled, ClanFeuds.Outcome.Cancelled, reason: "gm");

            Assert.AreEqual("gm", Last().Reason);
            Assert.AreEqual((byte)PvpMatchOutcome.Cancelled, Last().Outcome);
            Assert.AreEqual((byte)0, Last().WinnerSide);

            var ended = feuds.Start(clans.All[RedId], clans.All[BlueId]);
            feuds.Kill(clans.Member(BlueId), clans.Leader(RedId));
            _utc = _utc.AddMinutes(1);
            feuds.Expire(ended, "gm");

            Assert.AreEqual("gm", Last().Reason);
            Assert.AreEqual((byte)2, Last().WinnerSide);

            // Called off with nothing said of why.
            var bare = feuds.Start(clans.All[RedId], clans.All[BlueId]);
            _utc = _utc.AddMinutes(1);
            feuds.End(bare, ClanFeuds.Outcome.Cancelled);

            Assert.AreEqual("cancelled", Last().Reason);

            Assert.AreEqual(6, Records(r => r.GetRecentMatches(50)).Count, "a record each");
            Assert.AreEqual(0, Records(r => r.GetOpenMatches(PvpMatchKind.ClanFeud)).Count);
        }

        [TestMethod]
        public void TheRosterIsBothClansAsTheyAreAtTheEnd()
        {
            using var world = new WorldTestContext();
            var clans = new Clans(world);
            var (feuds, _) = Server(clans);

            var feud = feuds.Start(clans.All[RedId], clans.All[BlueId]);

            feuds.Kill(clans.Member(BlueId), clans.Leader(RedId));
            feuds.Kill(clans.Member(RedId), clans.Member(BlueId));

            // Blue's fighter leaves the clan; Red has a member who is not in the world.
            clans.Leave(BlueId, clans.Member(BlueId));
            clans.Offline[RedId] = new List<(uint, string, string)> { (7001, "Away", "RedAway") };

            _utc = Noon.AddDays(7);
            feuds.Expire(feud);

            var roster = Records(r => r.GetPlayers(Records(x => x.GetRecentMatches(1)).Single().Id));

            Assert.AreEqual(5, roster.Count);

            var left = roster.Single(p => p.FamilyName == "BlueMate");

            Assert.IsFalse(left.PresentAtEnd, "fought, and was gone by the end");
            Assert.AreEqual((1, 1), (left.Kills, left.Deaths));
            Assert.AreEqual((byte)2, left.Side);

            var away = roster.Single(p => p.CharacterId == 7001);

            Assert.IsTrue(away.PresentAtEnd, "in the clan, in the world or not");
            Assert.AreEqual("Away", away.Name);
            Assert.AreEqual("RedAway", away.FamilyName);
            Assert.AreEqual((byte)1, away.Side);
            Assert.AreEqual(RedId, away.ClanId);

            Assert.AreEqual(3, roster.Count(p => p.Side == 1));
            Assert.AreEqual(4, roster.Count(p => p.PresentAtEnd));
        }

        [TestMethod]
        public void AFeudFindsItsRecordAgainAfterARestart()
        {
            using var world = new WorldTestContext();
            var clans = new Clans(world);
            var (first, _) = Server(clans);

            var feud = first.Start(clans.All[RedId], clans.All[BlueId]);

            first.Kill(clans.Leader(RedId), clans.Member(BlueId));

            var recordId = Records(r => r.GetOpenMatches(PvpMatchKind.ClanFeud)).Single().Id;

            // A record left open by a feud that is gone, and a feud begun before records were kept.
            var orphan = Records(r => r.SaveMatch(new PvpMatchEntry
            {
                Kind = (byte)PvpMatchKind.ClanFeud,
                WargameId = 77,
                StartedAt = Noon,
                Side1ClanId = 8,
                Side2ClanId = 9
            }));

            _utc = Noon.AddHours(5);

            var (second, records) = Server(clans);

            Assert.AreEqual(1, second.Feuds.Count);
            Assert.AreEqual(recordId, Records(r => r.GetOpenMatches(PvpMatchKind.ClanFeud)).Single().Id, "the same record, still open");

            var closed = Records(r => r.GetMatch(orphan));

            Assert.AreEqual(Noon.AddHours(5), closed.EndedAt);
            Assert.AreEqual((byte)PvpMatchOutcome.Cancelled, closed.Outcome);
            Assert.AreEqual("lost", closed.Reason);

            // The next kill goes on from what was there.
            Assert.IsTrue(second.Kill(clans.Leader(RedId), clans.Member(BlueId)));

            var killer = Records(r => r.GetPlayers(recordId)).Single(p => p.Side == 1);

            Assert.AreEqual(2, killer.Kills);
            Assert.AreEqual(2, Records(r => r.GetMatch(recordId)).Side1Score);
            Assert.AreEqual(Noon, Records(r => r.GetMatch(recordId)).StartedAt, "begun when it began");

            _utc = Noon.AddHours(6);
            second.Expire(second.Feuds.Single());
            Assert.AreEqual(recordId, Records(r => r.GetRecentMatches(1)).Single().Id);
            Assert.AreEqual(feud.Id, Records(r => r.GetRecentMatches(1)).Single().WargameId);
            Assert.IsNotNull(records.Store);
        }

        [TestMethod]
        public void AFeudFromBeforeRecordsWereKeptGetsOneWhenTheServerStarts()
        {
            using var world = new WorldTestContext();
            var clans = new Clans(world);

            // A server with no records: the feud is kept, its record is not.
            var before = new ClanFeuds(clans) { Now = () => 1_000_000, UtcNow = () => 1_800_000_000_000, Records = null };

            before.Load(new FeudStore(_database));
            before.Start(clans.All[RedId], clans.All[BlueId]);
            before.Kill(clans.Leader(RedId), clans.Member(BlueId));

            Assert.AreEqual(0, Records(r => r.GetOpenMatches(PvpMatchKind.ClanFeud)).Count);

            _utc = Noon.AddDays(1);

            var (feuds, _) = Server(clans);
            var opened = Records(r => r.GetOpenMatches(PvpMatchKind.ClanFeud)).Single();

            Assert.AreEqual(Noon.AddDays(1), opened.StartedAt, "dated from when it was first on record");
            Assert.AreEqual(1, opened.Side1Kills, "with the score the feud has");
            Assert.AreEqual("Red", opened.Side1Name);
            Assert.AreEqual(0, Records(r => r.GetPlayers(opened.Id)).Count, "who made that kill was never written down");

            feuds.Expire(feuds.Feuds.Single());
            Assert.AreEqual((byte)1, Records(r => r.GetMatch(opened.Id)).WinnerSide);
        }

        #endregion

        /// <summary>A server starting on the database: the records first, then the feuds read back, as Server does it.</summary>
        private (ClanFeuds Feuds, PvpRecords Records) Server(Clans clans)
        {
            var records = new PvpRecords { UtcNow = () => _utc };

            records.Load(new RecordStore(_database));

            var feuds = new ClanFeuds(clans) { Now = () => 1_000_000, UtcNow = () => 1_800_000_000_000, Records = records };

            feuds.Load(new FeudStore(_database));

            return (feuds, records);
        }

        private T Records<T>(Func<PvpRecordRepository, T> action)
        {
            using var context = (CharContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteCharContext), _database);
            return action(new PvpRecordRepository(context));
        }

        /// <summary>The store the server has for records, over a database file: a context per call.</summary>
        private sealed class RecordStore : PvpRecords.IStore
        {
            private readonly string _database;

            public RecordStore(string database) => _database = database;

            private T With<T>(Func<PvpRecordRepository, T> action)
            {
                using var context = (CharContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteCharContext), _database);
                return action(new PvpRecordRepository(context));
            }

            public uint SaveMatch(PvpMatchEntry match) => With(r => r.SaveMatch(match));
            public void SavePlayers(uint matchId, IReadOnlyCollection<PvpMatchPlayerEntry> players) => With(r => { r.SavePlayers(matchId, players); return 0; });
            public List<PvpMatchEntry> OpenFeuds() => With(r => r.GetOpenMatches(PvpMatchKind.ClanFeud));
            public List<PvpMatchPlayerEntry> Players(uint matchId) => With(r => r.GetPlayers(matchId));
        }

        /// <summary>The store the server has for feuds.</summary>
        private sealed class FeudStore : ClanFeuds.IStore
        {
            private readonly string _database;

            public FeudStore(string database) => _database = database;

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
        }

        /// <summary>Two PvP clans, each with a leader and a member in the world, and whoever else a test says is in them.</summary>
        private sealed class Clans : ClanFeuds.IClans
        {
            public Dictionary<uint, ClanEntry> All { get; } = new Dictionary<uint, ClanEntry>();
            public Dictionary<uint, List<(uint, string, string)>> Offline { get; } = new Dictionary<uint, List<(uint, string, string)>>();

            private readonly Dictionary<uint, List<Client>> _members = new Dictionary<uint, List<Client>>();

            public Clans(WorldTestContext world)
            {
                foreach (var (id, name) in new[] { (RedId, "Red"), (BlueId, "Blue") })
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
            public Client Member(uint clanId) => _members[clanId].Count > 1 ? _members[clanId][1] : _left;

            private Client _left;

            /// <summary>Out of the clan, and still in the world.</summary>
            public void Leave(uint clanId, Client client)
            {
                _left = client;
                _members[clanId].Remove(client);
                client.Player.ClanId = 0;
            }

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

            public List<(uint CharacterId, string Name, string FamilyName)> Members(uint clanId) =>
                Online(clanId).Select(c => (c.Player.Id, c.Player.Name ?? "", c.Player.FamilyName ?? ""))
                    .Concat(Offline.GetValueOrDefault(clanId) ?? new List<(uint, string, string)>())
                    .ToList();
        }
    }
}
