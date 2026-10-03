extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Config;
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets;
    using Rasa.Packets.ClientMethod.Server;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Packets.Team.Server;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;
    using Rasa.Test.Database;

    /// <summary>
    /// The Edmund Range match (Battlegrounds): joining and leaving a team, the phases of a match,
    /// who may be where, who is whose enemy, the control points, how a match is won, and what
    /// the clients are told of it.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class BattlegroundTests
    {
        private const uint MapId = 1220;    // WorldTestContext's map
        private const uint Whiskey = 101;
        private const uint Charlie = 102;
        private const uint Echo = 103;
        private const uint BlueHospital = 404;
        private const uint RedHospital = 405;
        private const uint WhiskeyHospital = 533;
        private const uint EchoHospital = 526;

        private static readonly Vector3 RedBase = new Vector3(400, 100, 0);
        private static readonly Vector3 BlueBase = new Vector3(-400, 100, 0);
        private static readonly Vector3 Staging = new Vector3(0, 0, -30);
        private static readonly Vector3 Field = new Vector3(0, 100, 0);

        #region Teams

        [TestMethod]
        public void WalkingIntoATeamsTeleporterJoinsItAndGoesToItsBase()
        {
            using var f = new Fixture();
            var red = f.Player();
            var onlooker = f.Player();

            Assert.IsTrue(f.Grounds.TakeLink(red, f.RedDoor));

            Assert.AreEqual(Battlegrounds.Red, f.Grounds.TeamOf(red.Player));
            Assert.AreEqual(RedBase, red.Player.Position);
            Assert.AreEqual(1, f.Match.Count(Battlegrounds.Red));

            var packets = Packets(red);
            var joined = packets.OfType<JoinedTeamPacket>().Single();

            Assert.AreEqual(Battlegrounds.Red, joined.TeamId);
            CollectionAssert.AreEqual(new[] { red.Player.EntityId }, joined.Members);
            Assert.AreEqual(PlayerMessage.PmPvpRedTeam, Messages(packets).Single());
            Assert.AreEqual(PlayerMessage.PmPvpRedTeam, Messages(Packets(onlooker)).Single(), "everyone on the map is told");

            // A second for Red, once Blue has somebody: the first is told, and the second has both.
            var blue = f.Join(Battlegrounds.Blue);
            Drain(red);

            var second = f.Player();
            Assert.IsTrue(f.Grounds.TakeLink(second, f.RedDoor));

            Assert.AreEqual(second.Player.EntityId, Packets(red).OfType<AddTeamMemberPacket>().Single().EntityId);
            CollectionAssert.AreEquivalent(new[] { red.Player.EntityId, second.Player.EntityId },
                Packets(second).OfType<JoinedTeamPacket>().Single().Members);
            Assert.IsFalse(Packets(blue).OfType<AddTeamMemberPacket>().Any(), "the other team is not");
        }

        [TestMethod]
        public void ATeamWithMorePlayersThanTheOtherCannotBeJoined()
        {
            using var f = new Fixture();

            f.Join(Battlegrounds.Red);

            var second = f.Player();
            Assert.IsTrue(f.Grounds.TakeLink(second, f.RedDoor), "the link is the battleground's whatever comes of it");

            Assert.AreEqual(0u, f.Grounds.TeamOf(second.Player));
            Assert.AreEqual(1, f.Teleports.Count, "and went nowhere");

            var packets = Packets(second);
            Assert.AreEqual(PlayerMessage.EdmundrangeCanNotJoinRedTeamBigtext, packets.OfType<DisplaySystemMessagePacket>().Single().MsgId);
            StringAssert.Contains(packets.OfType<SystemMessagePacket>().Single().TextMessage, "join Blue Team");

            Assert.IsTrue(f.Grounds.TakeLink(second, f.BlueDoor));
            Assert.AreEqual(Battlegrounds.Blue, f.Grounds.TeamOf(second.Player));

            // Level: either may be joined.
            var third = f.Player();
            f.Grounds.TakeLink(third, f.RedDoor);
            Assert.AreEqual(Battlegrounds.Red, f.Grounds.TeamOf(third.Player));

            // With a player's leeway a team may be two up, and no more.
            f.Grounds.Config.MaxImbalance = 1;

            var fourth = f.Player();
            f.Grounds.TakeLink(fourth, f.RedDoor);
            Assert.AreEqual(Battlegrounds.Red, f.Grounds.TeamOf(fourth.Player), "3 against 1");

            var fifth = f.Player();
            f.Grounds.TakeLink(fifth, f.RedDoor);
            Assert.AreEqual(0u, f.Grounds.TeamOf(fifth.Player));
        }

        [TestMethod]
        public void ALowLevelPlayerDoesNotEnterTheMap()
        {
            using var f = new Fixture();
            var player = f.Player();

            player.Player.Level = 44;
            Assert.IsFalse(f.Grounds.MayEnter(player, MapId));
            CollectionAssert.AreEqual(
                new[] { PlayerMessage.EdmundrangeNotHighEnoughLevelBigtext, PlayerMessage.EdmundrangeNotHighEnoughLevelInfo },
                Packets(player).OfType<DisplaySystemMessagePacket>().Select(p => p.MsgId).ToArray());

            Assert.IsTrue(f.Grounds.MayEnter(player, 999), "a map with no match");

            player.Player.Level = 45;
            Assert.IsTrue(f.Grounds.MayEnter(player, MapId));

            player.Player.Level = 1;
            f.Exempt.Add(player);
            Assert.IsTrue(f.Grounds.MayEnter(player, MapId), "a game master");
        }

        [TestMethod]
        public void StandingInTheWayOutOfABaseLeavesTheTeam()
        {
            using var f = new Fixture();
            var red = f.Join(Battlegrounds.Red);
            var mate = f.Join(Battlegrounds.Blue);
            var other = f.Join(Battlegrounds.Red);

            // Walked into it and out again.
            red.Player.Position = f.RedExit.Position;
            Assert.IsTrue(f.Grounds.TakeLink(red, f.RedExit));
            StringAssert.Contains(Packets(red).OfType<SystemMessagePacket>().Single().TextMessage, "5 seconds");

            red.Player.Position = RedBase;
            f.Tick(1000);
            Assert.AreEqual(Battlegrounds.Red, f.Grounds.TeamOf(red.Player));
            Assert.IsTrue(Packets(red).OfType<SystemMessagePacket>().Any(m => m.TextMessage.Contains("You stay")));

            // Walked into it and stayed.
            red.Player.Position = f.RedExit.Position;
            f.Grounds.TakeLink(red, f.RedExit);
            f.Tick(4000);
            Assert.AreEqual(Battlegrounds.Red, f.Grounds.TeamOf(red.Player), "not yet");
            Drain(red);
            Drain(other);

            f.Tick(1000);

            Assert.AreEqual(0u, f.Grounds.TeamOf(red.Player));
            Assert.AreEqual(f.RedExit.DestPosition, red.Player.Position, "in the staging area");
            Assert.AreEqual(0u, f.Grounds.DesertedTeamOf(red.Player.Id), "no match was being played");

            var packets = Packets(red);
            Assert.AreEqual(1, packets.OfType<LeftTeamPacket>().Count());
            CollectionAssert.Contains(Messages(packets), PlayerMessage.PmPvpRedTeamLeave);
            Assert.AreEqual(red.Player.EntityId, Packets(other).OfType<RemoveTeamMemberPacket>().Single().EntityId);
            Assert.IsNotNull(mate);
        }

        [TestMethod]
        public void LeavingAMatchThatIsBeingPlayedIsDesertion()
        {
            using var f = new Fixture();
            var red = f.Join(Battlegrounds.Red);
            var blue = f.Join(Battlegrounds.Blue);
            var stays = f.Join(Battlegrounds.Red);

            f.Begin();

            // Another map, a logout, a lost connection: all come through here.
            f.Grounds.PlayerLeft(red);

            Assert.AreEqual(0u, f.Grounds.TeamOf(red.Player));
            Assert.AreEqual(Battlegrounds.Red, f.Grounds.DesertedTeamOf(red.Player.Id));
            Assert.AreEqual(0, f.Grounds.WargameDataOf(red.Player).Count, "nobody's enemy now");
            Assert.AreEqual(0, Packets(red).OfType<SetNumberOfTeamsPacket>().Single().Teams);
            Assert.IsFalse(f.Match.Scores[red.Player.Id].Active, "their row is hidden");

            // Back on the map: Blue is closed to them, in the client's words, and Red is not.
            red.Player.Position = Staging;
            f.Grounds.TakeLink(red, f.BlueDoor);

            Assert.AreEqual(0u, f.Grounds.TeamOf(red.Player));
            CollectionAssert.AreEqual(
                new[] { PlayerMessage.EdmundrangeCanNotJoinBlueTeamBigtext, PlayerMessage.EdmundrangeCanNotJoinBlueTeamInfo },
                Packets(red).OfType<DisplaySystemMessagePacket>().Select(p => p.MsgId).ToArray());

            f.Grounds.TakeLink(red, f.RedDoor);
            Assert.AreEqual(Battlegrounds.Red, f.Grounds.TeamOf(red.Player));
            Assert.IsTrue(f.Match.Scores[red.Player.Id].Active, "and their row is back, with what was on it");

            // Seen through to its end, the desertion is forgiven.
            f.Grounds.End(f.Match, Battlegrounds.Blue);
            Assert.AreEqual(0u, f.Grounds.DesertedTeamOf(red.Player.Id));
            Assert.IsNotNull(blue);
            Assert.IsNotNull(stays);
        }

        [TestMethod]
        public void APlayerWhoHasGoneWithoutAWordIsTakenOffTheirTeam()
        {
            using var f = new Fixture();
            var red = f.Join(Battlegrounds.Red);
            var blue = f.Join(Battlegrounds.Blue);

            Drain(red);
            red.State = ClientState.Disconnected;
            f.Tick(1000);

            Assert.AreEqual(0, f.Match.Count(Battlegrounds.Red));
            Assert.AreEqual(1, f.Match.Count(Battlegrounds.Blue));
            Assert.AreEqual(0, Packets(red).Count, "nothing is said to a connection that is gone");
            Assert.IsNotNull(blue);
        }

        #endregion

        #region The leaver's lockout

        [TestMethod]
        public void LeavingAMatchThatIsBeingPlayedKeepsThePlayerToItsInstance()
        {
            using var f = new Fixture();
            var red = f.Join(Battlegrounds.Red);
            var blue = f.Join(Battlegrounds.Blue);
            var stays = f.Join(Battlegrounds.Red);
            var copy = new MapChannel { MapInfo = f.World.Map.MapInfo, InstanceId = 5, ClientList = new List<Client>(), IsSharedInstance = true };

            f.Begin();
            Assert.IsNull(f.Grounds.LockoutOf(red.Player.Id));

            f.Grounds.PlayerLeft(red);

            var lockout = f.Grounds.LockoutOf(red.Player.Id);
            Assert.AreEqual(MapId, lockout.MapContextId);
            Assert.AreEqual(f.World.Map.InstanceId, lockout.InstanceId);
            Assert.AreEqual(f.Now + 15 * 60000L, lockout.Until);

            Assert.AreSame(lockout, f.Grounds.LockoutFor(red, MapId));
            Assert.IsNull(f.Grounds.LockoutFor(red, 999), "another map is none of its business");
            Assert.IsNull(f.Grounds.LockoutFor(blue, MapId));

            Assert.IsFalse(f.Grounds.BarredFrom(red, f.World.Map, out _), "the one they left");
            Assert.IsTrue(f.Grounds.BarredFrom(red, copy, out var barring), "any other");
            Assert.AreSame(lockout, barring);
            Assert.IsFalse(f.Grounds.BarredFrom(blue, copy, out _));
            Assert.IsFalse(f.Grounds.BarredFrom(red, null, out _));

            // Back in the one they left: their team takes them as before.
            Drain(red);
            red.Player.Position = Staging;
            f.Grounds.TakeLink(red, f.RedDoor);
            Assert.AreEqual(Battlegrounds.Red, f.Grounds.TeamOf(red.Player));
            Assert.IsFalse(Packets(red).OfType<SystemMessagePacket>().Any(m => m.TextMessage.Contains("instance you left")));

            // Seen through to its end, the desertion is forgiven. The fifteen minutes are not.
            f.Grounds.End(f.Match, Battlegrounds.Blue);
            Assert.AreEqual(0u, f.Grounds.DesertedTeamOf(red.Player.Id));
            Assert.IsTrue(f.Grounds.BarredFrom(red, copy, out _));

            f.Now += 15 * 60000L - 1;
            Assert.IsTrue(f.Grounds.BarredFrom(red, copy, out _));

            f.Now += 1;
            Assert.IsFalse(f.Grounds.BarredFrom(red, copy, out _));
            Assert.IsNull(f.Grounds.LockoutOf(red.Player.Id));
            Assert.IsNotNull(stays);
        }

        [TestMethod]
        public void EveryWayOutOfARunningMatchLocksAndNoneBeforeItBegins()
        {
            using var f = new Fixture();
            var early = f.Join(Battlegrounds.Red);
            var walker = f.Join(Battlegrounds.Red);
            var dropped = f.Join(Battlegrounds.Red);
            var red = f.Join(Battlegrounds.Red);
            var blue = f.Join(Battlegrounds.Blue);

            // The staging area and the preparation: nothing is being played.
            f.Tick(1000);
            Assert.AreEqual(Battlegrounds.Phase.Preparing, f.Match.Phase);
            f.Grounds.PlayerLeft(early);
            Assert.IsNull(f.Grounds.LockoutOf(early.Player.Id));

            f.Begin();

            // Under the sign in the base, for its five seconds.
            walker.Player.Position = f.RedExit.Position;
            Assert.IsTrue(f.Grounds.TakeLink(walker, f.RedExit));
            f.Tick(Battlegrounds.LeaveDwellMs);
            Assert.AreEqual(0u, f.Grounds.TeamOf(walker.Player));
            Assert.AreEqual(f.World.Map.InstanceId, f.Grounds.LockoutOf(walker.Player.Id).InstanceId);

            // A connection that is gone.
            dropped.State = ClientState.Disconnected;
            f.Tick(1000);
            Assert.IsNotNull(f.Grounds.LockoutOf(dropped.Player.Id));

            // A game master taking somebody off a team is nobody's desertion.
            Assert.IsTrue(f.Grounds.LeaveTeam(red));
            Assert.IsNull(f.Grounds.LockoutOf(red.Player.Id));
            Assert.IsNull(f.Grounds.LockoutOf(blue.Player.Id));
        }

        [TestMethod]
        public void NoTeamOfAnotherInstanceTakesALockedOutPlayer()
        {
            using var f = new Fixture();
            var player = f.Player();
            var blue = f.Join(Battlegrounds.Blue);

            // They left a match on another copy of the map; this is not it.
            f.Grounds.Lock(player.Player.Id, MapId, f.World.Map.InstanceId + 5);

            // Arriving here some way that is not the door, they hear of it.
            f.Grounds.PlayerEntered(player);
            var told = Packets(player).OfType<SystemMessagePacket>().Single().TextMessage;
            StringAssert.Contains(told, "for 15 more minutes you can only play in the instance you left");
            StringAssert.Contains(told, "Leave by the door");

            f.Grounds.TakeLink(player, f.RedDoor);

            Assert.AreEqual(0u, f.Grounds.TeamOf(player.Player));
            Assert.AreEqual(0, f.Teleports.Count(t => t.Client == player));

            var packets = Packets(player);
            Assert.AreEqual(PlayerMessage.EdmundrangeCanNotJoinRedTeamBigtext, packets.OfType<DisplaySystemMessagePacket>().Single().MsgId);
            StringAssert.Contains(packets.OfType<SystemMessagePacket>().Single().TextMessage, "15 more minutes");

            // By the minute begun.
            f.Now += 14 * 60000L + 1000;
            f.Grounds.TakeLink(player, f.RedDoor);
            StringAssert.Contains(Packets(player).OfType<SystemMessagePacket>().Single().TextMessage, "for 1 more minute you");
            Assert.AreEqual(0u, f.Grounds.TeamOf(player.Player));

            // And when it has run out, as anybody.
            f.Now += 59000;
            f.Grounds.PlayerEntered(player);
            Assert.IsFalse(Packets(player).OfType<SystemMessagePacket>().Any());

            f.Grounds.TakeLink(player, f.RedDoor);
            Assert.AreEqual(Battlegrounds.Red, f.Grounds.TeamOf(player.Player));
            Assert.IsNotNull(blue);
        }

        [TestMethod]
        public void TheLockoutIsNotForAGameMasterNorForACommand()
        {
            using var f = new Fixture();
            var master = f.Player();
            var moved = f.Player();
            var other = f.World.Map.InstanceId + 5;
            var copy = new MapChannel { MapInfo = f.World.Map.MapInfo, InstanceId = 9, ClientList = new List<Client>(), IsSharedInstance = true };

            f.Grounds.Lock(master.Player.Id, MapId, other);
            f.Grounds.Lock(moved.Player.Id, MapId, other);
            f.Exempt.Add(master);

            Assert.IsNull(f.Grounds.LockoutFor(master, MapId));
            Assert.IsFalse(f.Grounds.BarredFrom(master, copy, out _));
            Assert.IsNotNull(f.Grounds.LockoutOf(master.Player.Id), "it is kept, and counts once they are no game master");

            f.Grounds.TakeLink(master, f.RedDoor);
            Assert.AreEqual(Battlegrounds.Red, f.Grounds.TeamOf(master.Player));

            // .bg team, which refuses nothing.
            Assert.IsTrue(f.Grounds.BarredFrom(moved, f.World.Map, out _));
            Assert.IsTrue(f.Grounds.Join(moved, Battlegrounds.Blue, force: true));
        }

        [TestMethod]
        public void ALaterDesertionReplacesTheLockoutAndForgivingClearsIt()
        {
            using var f = new Fixture();
            var red = f.Join(Battlegrounds.Red);
            var blue = f.Join(Battlegrounds.Blue);

            f.Grounds.Lock(red.Player.Id, MapId, f.World.Map.InstanceId + 5);
            f.Begin();
            f.Now += 10 * 60000L;
            f.Grounds.PlayerLeft(red);

            var lockout = f.Grounds.LockoutOf(red.Player.Id);
            Assert.AreEqual(f.World.Map.InstanceId, lockout.InstanceId, "the one left last");
            Assert.AreEqual(f.Now + 15 * 60000L, lockout.Until, "and its time from then");

            Assert.IsTrue(f.Grounds.Forgive(red.Player.Id));
            Assert.IsNull(f.Grounds.LockoutOf(red.Player.Id));
            Assert.AreEqual(0u, f.Grounds.DesertedTeamOf(red.Player.Id));
            Assert.IsFalse(f.Grounds.Forgive(red.Player.Id), "nothing left to forgive");

            f.Grounds.Lock(blue.Player.Id, MapId, 5);
            f.Grounds.Reset();
            Assert.IsNull(f.Grounds.LockoutOf(blue.Player.Id));
        }

        [TestMethod]
        public void ALockoutOfNoMinutesIsNoLockout()
        {
            using var f = new Fixture();
            var red = f.Join(Battlegrounds.Red);
            var blue = f.Join(Battlegrounds.Blue);

            f.Grounds.Config.LeaverLockoutMinutes = 0;
            f.Begin();
            f.Grounds.PlayerLeft(red);

            Assert.IsNull(f.Grounds.LockoutOf(red.Player.Id));
            Assert.AreEqual(Battlegrounds.Red, f.Grounds.DesertedTeamOf(red.Player.Id), "a deserter all the same");
            Assert.AreEqual(15, new BattlegroundConfig().LeaverLockoutMinutes);
            Assert.IsNotNull(blue);
        }

        #endregion

        #region The match

        [TestMethod]
        public void AMatchWaitsForBothTeamsPreparesAndBegins()
        {
            using var f = new Fixture();
            var red = f.Join(Battlegrounds.Red);

            f.Tick(1000);
            Assert.AreEqual(Battlegrounds.Phase.Waiting, f.Match.Phase);
            Assert.AreEqual(0, f.Grounds.WargameDataOf(red.Player).Count);

            var blue = f.Join(Battlegrounds.Blue);
            Drain(red);

            f.Tick(1000);
            Assert.AreEqual(Battlegrounds.Phase.Preparing, f.Match.Phase);

            var packets = Packets(red);
            StringAssert.Contains(packets.OfType<SystemMessagePacket>().Single().TextMessage, "60 seconds");
            Assert.AreEqual(60, packets.OfType<ScoreBoardGameScorePacket>().Single().RemainingSeconds);

            f.Tick(59000);
            Assert.AreEqual(Battlegrounds.Phase.Preparing, f.Match.Phase);
            Assert.IsFalse(Pvp.AreEnemies(red.Player, blue.Player), "not until it begins");

            Drain(red);
            f.Tick(1000);

            Assert.AreEqual(Battlegrounds.Phase.Running, f.Match.Phase);
            Assert.AreEqual(7001u, f.Match.WargameId);
            Assert.IsTrue(Pvp.AreEnemies(red.Player, blue.Player));
            Assert.IsTrue(f.Grounds.WargameDataOf(red.Player)[7001], "Red is the side that is true");
            Assert.IsFalse(f.Grounds.WargameDataOf(blue.Player)[7001]);

            packets = Packets(red);
            Assert.IsTrue(packets.OfType<ScoreBoardActivePacket>().Single().Active);
            Assert.AreEqual(2, packets.OfType<ScoreBoardIndividualUpdatePacket>().Count(), "a row each");
            Assert.AreEqual(20 * 60, packets.OfType<ScoreBoardGameScorePacket>().Single().RemainingSeconds);
            CollectionAssert.IsSubsetOf(new[] { PlayerMessage.PmEdmundrangeBattlegroundStart, PlayerMessage.PmBattlegroundBegins }, Messages(packets));
        }

        [TestMethod]
        public void APreparationWithATeamGoneShortGoesBackToWaiting()
        {
            using var f = new Fixture();

            f.Grounds.Config.MinPlayersPerTeam = 2;

            var reds = new[] { f.Join(Battlegrounds.Red), null };
            f.Join(Battlegrounds.Blue);
            reds[1] = f.Join(Battlegrounds.Red);

            f.Tick(1000);
            Assert.AreEqual(Battlegrounds.Phase.Waiting, f.Match.Phase, "Blue has one");

            var blue = f.Join(Battlegrounds.Blue);
            f.Tick(1000);
            Assert.AreEqual(Battlegrounds.Phase.Preparing, f.Match.Phase);

            f.Grounds.PlayerLeft(blue);
            Assert.AreEqual(0u, f.Grounds.DesertedTeamOf(blue.Player.Id), "nothing was being played");

            f.Tick(1000);
            Assert.AreEqual(Battlegrounds.Phase.Waiting, f.Match.Phase);

            f.Tick(120000);
            Assert.AreEqual(Battlegrounds.Phase.Waiting, f.Match.Phase);
        }

        [TestMethod]
        public void TheDoorsAreShutUntilTheMatchBeginsAndTheOtherBaseAlways()
        {
            using var f = new Fixture();
            var red = f.Join(Battlegrounds.Red);
            var blue = f.Join(Battlegrounds.Blue);

            f.Teleports.Clear();

            // Out on the field before the match: back behind the doors.
            red.Player.Position = Field;
            f.Tick(1000);
            Assert.AreEqual(RedBase, red.Player.Position);
            Assert.IsTrue(Packets(red).OfType<SystemMessagePacket>().Any(m => m.TextMessage.Contains("doors open")));

            // Anywhere in their own base they are left alone.
            red.Player.Position = RedBase + new Vector3(-40, 0, 0);
            var before = f.Teleports.Count;
            f.Tick(1000);
            Assert.AreEqual(before, f.Teleports.Count);

            f.Begin();
            f.Teleports.Clear();

            // The field is theirs now.
            red.Player.Position = Field;
            blue.Player.Position = Field + new Vector3(10, 0, 0);
            f.Tick(1000);
            Assert.AreEqual(0, f.Teleports.Count);

            // The other team's base never is.
            red.Player.Position = BlueBase + new Vector3(30, 0, 0);
            f.Tick(1000);
            Assert.AreEqual(RedBase, red.Player.Position);
            Assert.IsTrue(Packets(red).OfType<SystemMessagePacket>().Any(m => m.TextMessage.Contains("Blue Team's base")));

            // The dead lie where they fell.
            blue.Player.Position = RedBase;
            blue.Player.State = CharacterState.Dead;
            f.Tick(1000);
            Assert.AreEqual(RedBase, blue.Player.Position);
        }

        [TestMethod]
        public void APlayerWithNoTeamIsKeptToTheStagingArea()
        {
            using var f = new Fixture();
            var stray = f.Player();
            var master = f.Player();

            f.Exempt.Add(master);
            stray.Player.Position = Field;
            master.Player.Position = Field;

            f.Tick(1000);

            Assert.AreEqual(Staging, stray.Player.Position);
            Assert.AreEqual(Field, master.Player.Position, "a game master goes where they like");

            f.Teleports.Clear();
            f.Tick(1000);
            Assert.AreEqual(0, f.Teleports.Count, "in the staging area they are left alone");
        }

        [TestMethod]
        public void NobodyIsHurtOrHurtsFromInsideTheirOwnBase()
        {
            using var f = new Fixture();
            var red = f.Join(Battlegrounds.Red);
            var blue = f.Join(Battlegrounds.Blue);

            f.Begin();

            red.Player.Position = Field;
            blue.Player.Position = Field;
            Assert.IsFalse(f.Grounds.Sheltered(red.Player, blue.Player), "on the field");

            blue.Player.Position = BlueBase + new Vector3(44, 0, 0);
            Assert.IsTrue(f.Grounds.Sheltered(red.Player, blue.Player), "hit in their base");
            Assert.IsTrue(f.Grounds.Sheltered(blue.Player, red.Player), "hitting from it");
            Assert.IsTrue(Pvp.Shielded(red.Player, blue.Player));

            blue.Player.Position = BlueBase + new Vector3(46, 0, 0);
            Assert.IsFalse(f.Grounds.Sheltered(red.Player, blue.Player), "past its edge");

            var stray = f.Player();
            Assert.IsFalse(f.Grounds.Sheltered(red.Player, stray.Player), "nobody's business but the match's");
        }

        [TestMethod]
        public void HoldingEveryPointWinsOnceTheMinimumTimeHasPassed()
        {
            using var f = new Fixture();
            var red = f.Join(Battlegrounds.Red);
            var blue = f.Join(Battlegrounds.Blue);

            f.Begin();
            f.TakeAll(Battlegrounds.Red);

            f.Tick(9 * 60000);
            Assert.AreEqual(Battlegrounds.Phase.Running, f.Match.Phase, "all three, and not ten minutes");

            Drain(red);
            Drain(blue);
            f.Tick(60000);

            Assert.AreEqual(Battlegrounds.Phase.Waiting, f.Match.Phase);
            Assert.AreEqual(1, f.Ended.Count);

            var redPackets = Packets(red);
            var bluePackets = Packets(blue);

            Assert.AreEqual(1, redPackets.OfType<WonBattlegroundPacket>().Count());
            Assert.AreEqual(1, bluePackets.OfType<LostBattlegroundPacket>().Count());
            Assert.IsFalse(redPackets.OfType<ScoreBoardActivePacket>().Single().Active);
            CollectionAssert.IsSubsetOf(
                new[] { PlayerMessage.PmBattlegroundMinimumTimeExpired, PlayerMessage.PmCocpTestMatchOverRedWin, PlayerMessage.PmBattlegroundEnds, PlayerMessage.PmEdmundrangeBattlegroundEnd },
                Messages(redPackets));

            Assert.AreEqual(200, f.Prestige[red], "the win's");
            Assert.AreEqual(50, f.Prestige[blue], "the loss's");
            Assert.IsFalse(Pvp.AreEnemies(red.Player, blue.Player), "the wargame was the match");

            // The field is as a match finds it, and everyone is behind their doors.
            Assert.IsTrue(f.Match.Points.All(p => p.Owner == 0 && p.Object.StateId == UseObjectState.OcpStateUncontrolled));
            Assert.AreEqual(RedBase, red.Player.Position);
            Assert.AreEqual(BlueBase, blue.Player.Position);
            Assert.AreEqual(Battlegrounds.Red, f.Grounds.TeamOf(red.Player), "the teams stand for the next");

            // And the next prepares at once.
            f.Tick(1000);
            Assert.AreEqual(Battlegrounds.Phase.Preparing, f.Match.Phase);
        }

        [TestMethod]
        public void AtTheMaximumTimeTheMostPointsWinThenTheMostKills()
        {
            using (var f = new Fixture())
            {
                f.Join(Battlegrounds.Red);
                f.Join(Battlegrounds.Blue);
                f.Begin();

                f.Grounds.SetOwner(f.Match, f.Point(Whiskey), Battlegrounds.Blue);
                f.Grounds.SetOwner(f.Match, f.Point(Charlie), Battlegrounds.Blue);
                f.Grounds.SetOwner(f.Match, f.Point(Echo), Battlegrounds.Red);

                f.Tick(20 * 60000 - 1000);
                Assert.AreEqual(Battlegrounds.Phase.Running, f.Match.Phase);

                f.Tick(1000);
                CollectionAssert.AreEqual(new[] { Battlegrounds.Blue }, f.Ended);
            }

            using (var f = new Fixture())
            {
                var red = f.Join(Battlegrounds.Red);
                var blue = f.Join(Battlegrounds.Blue);
                f.Begin();

                f.Grounds.SetOwner(f.Match, f.Point(Whiskey), Battlegrounds.Blue);
                f.Grounds.SetOwner(f.Match, f.Point(Echo), Battlegrounds.Red);
                f.Grounds.Kill(red, blue);

                Assert.AreEqual(Battlegrounds.Red, Battlegrounds.Leader(f.Match));

                f.Tick(20 * 60000);
                CollectionAssert.AreEqual(new[] { Battlegrounds.Red }, f.Ended);
            }

            using (var f = new Fixture())
            {
                var red = f.Join(Battlegrounds.Red);
                var blue = f.Join(Battlegrounds.Blue);
                f.Begin();
                Drain(red);

                f.Tick(20 * 60000 - 30000);
                f.Tick(30000);

                CollectionAssert.AreEqual(new[] { 0u }, f.Ended, "level on both: nobody's");
                Assert.AreEqual(50, f.Prestige[red]);
                Assert.AreEqual(50, f.Prestige[blue]);

                var packets = Packets(red);
                Assert.IsFalse(packets.OfType<WonBattlegroundPacket>().Any() || packets.OfType<LostBattlegroundPacket>().Any());
                CollectionAssert.IsSubsetOf(new[] { PlayerMessage.PmBattleground5MinuteWarning, PlayerMessage.PmBattleground1MinuteWarning }, Messages(packets));
            }
        }

        [TestMethod]
        public void ATeamWithNobodyLeftHasLostUnlessAGameMasterStartedIt()
        {
            using (var f = new Fixture())
            {
                var red = f.Join(Battlegrounds.Red);
                var blue = f.Join(Battlegrounds.Blue);
                f.Begin();

                f.Grounds.PlayerLeft(blue);
                f.Tick(1000);

                CollectionAssert.AreEqual(new[] { Battlegrounds.Red }, f.Ended);
                Assert.IsFalse(f.Prestige.ContainsKey(red), "a match cut short is worth nothing");
                Assert.AreEqual(1, Packets(red).OfType<WonBattlegroundPacket>().Count(), "won all the same");
            }

            using (var f = new Fixture())
            {
                var red = f.Join(Battlegrounds.Red);
                var blue = f.Join(Battlegrounds.Blue);
                f.Begin();
                f.Tick(10 * 60000);

                f.Grounds.PlayerLeft(blue);
                f.Tick(1000);

                Assert.AreEqual(200, f.Prestige[red], "past its minimum time it is a match played");
            }

            using (var f = new Fixture())
            {
                f.Join(Battlegrounds.Red);

                f.Grounds.Start(f.Match, forced: true);
                f.Tick(60000);

                Assert.AreEqual(Battlegrounds.Phase.Running, f.Match.Phase);
                Assert.AreEqual(0, f.Ended.Count);
            }
        }

        #endregion

        #region Control points

        [TestMethod]
        public void APointIsTakenByUsingItOnceItsBaneAreDeadAndTheMatchIsRunning()
        {
            using var f = new Fixture();
            var red = f.Join(Battlegrounds.Red);
            var blue = f.Join(Battlegrounds.Blue);
            var stray = f.Player();
            var point = f.Point(Whiskey);

            f.Garrison[Whiskey] = ControlPoints.Garrison.Down;
            f.Tick(1000);
            Assert.IsFalse(point.Object.IsEnabled, "out of service before the match");
            Assert.IsFalse(f.Grounds.MayCapture(red, point));

            f.Begin();
            f.Garrison[Whiskey] = ControlPoints.Garrison.Standing;
            f.Tick(1000);
            Assert.IsFalse(point.Object.IsEnabled, "its Bane stand");
            Assert.IsFalse(f.Grounds.MayCapture(red, point));

            f.Garrison[Whiskey] = ControlPoints.Garrison.Down;
            f.Tick(1000);
            Assert.IsTrue(point.Object.IsEnabled);
            Assert.IsTrue(f.Grounds.MayCapture(red, point));
            Assert.IsFalse(f.Grounds.MayCapture(stray, point), "no team");
            Assert.AreSame(point, f.Grounds.PointOf(point.Object));
            Assert.AreEqual(10000u, f.Grounds.CaptureMsOf(point));

            Drain(red);
            f.Grounds.Claiming(red, point);
            var claiming = Packets(red).OfType<DisplayClientMessagePacket>().Single();
            Assert.AreEqual(PlayerMessage.PmControlpointClaiming, claiming.MsgId);
            Assert.AreEqual("Red Team", claiming.Args["faction"]);
            Assert.AreEqual("Whiskey", claiming.Args["cpName"]);

            Assert.IsTrue(f.Grounds.Captured(red, point));

            Assert.AreEqual(Battlegrounds.Red, point.Owner);
            Assert.AreEqual(UseObjectState.OcpStateTeamControlled, point.Object.StateId);
            Assert.AreEqual(1, f.Match.Scores[red.Player.Id].Captures);
            Assert.AreEqual(50, f.Prestige[red]);
            Assert.AreEqual(50, f.Match.Scores[red.Player.Id].Prestige);

            var packets = Packets(blue);
            var score = packets.OfType<ScoreBoardGameScorePacket>().Last();
            Assert.AreEqual(Battlegrounds.Red, score.ControlPoints[12], "by the client's id for Whiskey");
            Assert.AreEqual(0u, score.ControlPoints[10]);
            Assert.AreEqual(0u, score.ControlPoints[11]);
            CollectionAssert.IsSubsetOf(new[] { PlayerMessage.PmBattlegroundCpTaken, PlayerMessage.PmControlpointOwned }, Messages(packets));

            var marker = packets.OfType<UpdateMapMarkerPacket>().First(p => p.MarkerEntityId == 9101);
            Assert.IsTrue(marker.State.TeamOwned);
            Assert.AreEqual(Battlegrounds.Red, marker.State.OwnerTeamId);

            // Theirs: not for them to take again, and Blue's to take from them.
            Assert.IsFalse(f.Grounds.MayCapture(red, point));
            Assert.IsFalse(f.Grounds.Captured(red, point));
            Assert.IsTrue(f.Grounds.Captured(blue, point));
            Assert.AreEqual(Battlegrounds.Blue, point.Owner);
            Assert.AreEqual(UseObjectState.OcpStateTeamControlled, point.Object.StateId);

            // Taken back: counted, and worth nothing the second time.
            Assert.IsTrue(f.Grounds.Captured(red, point));
            Assert.AreEqual(2, f.Match.Scores[red.Player.Id].Captures);
            Assert.AreEqual(50, f.Prestige[red]);
        }

        [TestMethod]
        public void AMatchIsClosedToEveryoneButAPlayersOwnTeam()
        {
            using var f = new Fixture();
            var red = f.Join(Battlegrounds.Red);
            var mate = f.Join(Battlegrounds.Red);
            var blue = f.Join(Battlegrounds.Blue);
            var onlooker = f.Player();

            Assert.IsTrue(Pvp.MayHelp(onlooker.Player, red.Player), "until the match runs anyone may");
            Assert.IsTrue(Pvp.MayHelp(blue.Player, red.Player));

            f.Begin();

            Assert.IsTrue(Pvp.MayHelp(mate.Player, red.Player), "their team");
            Assert.IsFalse(Pvp.MayHelp(blue.Player, red.Player), "the other team");
            Assert.IsFalse(Pvp.MayHelp(onlooker.Player, red.Player), "on no team");
            Assert.IsTrue(Pvp.MayTrade(red.Player, mate.Player));
            Assert.IsFalse(Pvp.MayTrade(red.Player, onlooker.Player));

            red.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 400, 0, 0);

            Assert.AreEqual(0, ActorManager.Instance.Heal(red.Player, 200, onlooker.Player.EntityId));
            Assert.AreEqual(200, ActorManager.Instance.Heal(red.Player, 200, mate.Player.EntityId));

            // Dead in the match: revived by their team, and by nobody else.
            red.Player.State = CharacterState.Dead;
            Assert.IsFalse(PlayerDeath.OfferRevive(f.World.Map, onlooker.Player, red.Player, 100));
            Assert.IsTrue(PlayerDeath.OfferRevive(f.World.Map, mate.Player, red.Player, 100));
            red.Player.State = CharacterState.Idle;
        }

        [TestMethod]
        public void AUseOfAPointsObjectThatRunsItsTimeTakesThePoint()
        {
            using var f = new Fixture();
            var red = f.Join(Battlegrounds.Red);
            var blue = f.Join(Battlegrounds.Blue);
            var point = f.Point(Charlie);

            f.Begin();
            red.Player.Position = point.Object.Position;
            blue.Player.Position = point.Object.Position;

            // Interrupted: nothing.
            point.Object.TriggeredByPlayers.Add(red);
            DynamicObjectManager.Instance.CaptureControlPointRecovery(f.World.Map,
                new ActionData(red.Player, ActionId.UseObject, DynamicObjectManager.ControlPointUseArgId, 10000) { IsInrerrupted = true });
            Assert.AreEqual(0u, point.Owner);
            Assert.AreEqual(0, point.Object.TriggeredByPlayers.Count);

            // Run to its end.
            point.Object.TriggeredByPlayers.Add(red);
            DynamicObjectManager.Instance.CaptureControlPointRecovery(f.World.Map,
                new ActionData(red.Player, ActionId.UseObject, DynamicObjectManager.ControlPointUseArgId, 10000));
            Assert.AreEqual(Battlegrounds.Red, point.Owner);
            Assert.AreEqual(UseObjectState.OcpStateTeamControlled, point.Object.StateId, "not flipped as a scene's object is");

            // Their own already: a second use changes nothing.
            point.Object.TriggeredByPlayers.Add(red);
            DynamicObjectManager.Instance.CaptureControlPointRecovery(f.World.Map,
                new ActionData(red.Player, ActionId.UseObject, DynamicObjectManager.ControlPointUseArgId, 10000));
            Assert.AreEqual(Battlegrounds.Red, point.Owner);
            Assert.AreEqual(1, f.Match.Scores[red.Player.Id].Captures);

            // Walked off before it was up.
            blue.Player.Position = point.Object.Position + new Vector3(50, 0, 0);
            point.Object.TriggeredByPlayers.Add(blue);
            DynamicObjectManager.Instance.CaptureControlPointRecovery(f.World.Map,
                new ActionData(blue.Player, ActionId.UseObject, DynamicObjectManager.ControlPointUseArgId, 10000));
            Assert.AreEqual(Battlegrounds.Red, point.Owner);
        }

        [TestMethod]
        public void TheSimulatedBaneStandOnceAMatchAndComeBackForTheNext()
        {
            using var f = new Fixture();
            var pool = new SpawnPool { DbId = 9201, Mode = SpawnPoolManager.ModeControlPoint, RespawnTime = 60000, SpawnSlot = new List<SpawnPoolSlot>() };

            f.World.Map.SpawnPools.Add(pool);
            f.Grounds.Reset();

            var match = f.Match;

            Assert.IsTrue(pool.IsGarrison && !pool.Suspended && !pool.HasSpawned, "a match finds them due");
            Assert.AreEqual(pool.RespawnTime, pool.UpdateTimer, "at once");

            f.Join(Battlegrounds.Red);
            f.Join(Battlegrounds.Blue);

            // The spawn worker has set them down.
            pool.HasSpawned = true;
            pool.AliveCreatures = 3;
            f.Tick(1000);
            Assert.IsTrue(pool.Suspended, "and sets down no more");

            f.Begin();
            pool.AliveCreatures = 0;
            f.Tick(60000);
            Assert.IsTrue(pool.Suspended, "dead for the match");

            f.Grounds.End(match, Battlegrounds.Red);
            Assert.IsTrue(!pool.Suspended && !pool.HasSpawned && pool.UpdateTimer == pool.RespawnTime, "and back for the next");
        }

        [TestMethod]
        public void APointsObjectIsToldWhoseItIs()
        {
            using var f = new Fixture();
            var red = f.Join(Battlegrounds.Red);
            var point = f.Point(Echo);

            Assert.AreEqual(UseObjectState.OcpStateUncontrolled, point.Object.StateId);
            Assert.AreEqual(-1, Battlegrounds.OwnerIdOf(0), "nobody's has no effect on it");

            Drain(red);
            f.Grounds.ShowTo(red, point.Object);
            Assert.AreEqual(-1, Packets(red).OfType<SetOwnerIdPacket>().Single().OwnerId);

            f.Grounds.SetOwner(f.Match, point, Battlegrounds.Blue);
            f.Grounds.ShowTo(red, point.Object);
            Assert.AreEqual(2, Packets(red).OfType<SetOwnerIdPacket>().Last().OwnerId);

            f.Grounds.ShowTo(red, new DynamicObject());
            Assert.AreEqual(0, Packets(red).Count, "no point of a match's");

            Assert.IsTrue(f.Grounds.MarkerStateOf(f.World.Map, point.Source).TeamOwned);
            Assert.AreEqual(Battlegrounds.Blue, f.Grounds.MarkerStateOf(f.World.Map, point.Source).OwnerTeamId);
        }

        [TestMethod]
        public void TheWorldsControlPointRulesLeaveABattlegroundsPointsAlone()
        {
            using var f = new Fixture();
            var points = new ControlPoints();

            points.Load(
                new[]
                {
                    new ControlPointEntry { Id = 7, MapContextId = MapId, Name = "Whiskey", ClassId = 10000071, PosX = 10, PosZ = 10, DefaultOwner = ControlPointEntry.OwnerBane },
                    new ControlPointEntry { Id = 8, MapContextId = 999, Name = "Elsewhere", ClassId = 3814, DefaultOwner = ControlPointEntry.OwnerBane }
                },
                new[]
                {
                    new ControlPointLinkEntry { ControlPointId = 7, Kind = ControlPointLinkEntry.KindBanePool, ObjectId = 9201 },
                    new ControlPointLinkEntry { ControlPointId = 7, Kind = ControlPointLinkEntry.KindHospital, ObjectId = WhiskeyHospital },
                    new ControlPointLinkEntry { ControlPointId = 8, Kind = ControlPointLinkEntry.KindHospital, ObjectId = 9302 }
                },
                null);

            var ours = points.ById(7);

            Assert.IsTrue(ours.IsBattleground);
            Assert.IsFalse(points.ById(8).IsBattleground);
            CollectionAssert.Contains(ours.BanePools.ToArray(), 9201u);
            CollectionAssert.Contains(ours.Hospitals.ToArray(), WhiskeyHospital);

            Assert.IsTrue(points.IsOpen(WhiskeyHospital), "its hospital is not the Bane's to shut");
            Assert.IsFalse(points.IsOpen(9302), "as another point's is");
            Assert.IsFalse(points.HoldsBack(new SpawnPool { DbId = 9201, HasSpawned = true }));

            points.Place(f.World.Map);
            Assert.IsNull(ours.Object, "the match sets its own down");
            Assert.IsFalse(points.SetOwner(ours, ControlPoints.Afs, null));
        }

        #endregion

        #region Fighting

        [TestMethod]
        public void AKillBetweenTheTeamsIsCountedAndGeneratesPrestige()
        {
            using var f = new Fixture();
            var red = f.Join(Battlegrounds.Red);
            var blue = f.Join(Battlegrounds.Blue);
            var mate = f.Join(Battlegrounds.Red);

            red.Player.Level = 50;
            blue.Player.Level = 50;

            f.Grounds.Kill(red, blue);
            Assert.AreEqual(0, f.Match.Scores.Count, "nothing is being played");

            f.Begin();
            Drain(blue);

            f.Grounds.Kill(red, blue);
            f.Grounds.Kill(red, mate);

            var killer = f.Match.Scores[red.Player.Id];
            var victim = f.Match.Scores[blue.Player.Id];

            Assert.AreEqual(1, killer.Kills);
            Assert.AreEqual(1, victim.Deaths);
            Assert.AreEqual(0, f.Match.Scores[mate.Player.Id].Deaths, "their own team's is not");
            Assert.AreEqual(PvpPrestige.BaseGenerated, killer.Prestige);
            Assert.AreEqual(PvpPrestige.BaseGenerated, f.Prestige[red]);
            Assert.IsFalse(f.Prestige.ContainsKey(blue), "nothing is stolen");
            Assert.AreEqual(1, f.Match.Kills(Battlegrounds.Red));

            var rows = Packets(blue).OfType<ScoreBoardIndividualUpdatePacket>().ToList();
            Assert.AreEqual(1, rows.Single(r => r.EntityId == red.Player.EntityId).Kills);
            Assert.AreEqual(1, rows.Single(r => r.EntityId == blue.Player.EntityId).Deaths);

            // Once from one victim in the interval.
            f.Grounds.Kill(red, blue);
            Assert.AreEqual(2, killer.Kills);
            Assert.AreEqual(PvpPrestige.BaseGenerated, f.Prestige[red]);
        }

        [TestMethod]
        public void DamageToTheOtherTeamAndHealingOfTheirOwnGoOnAPlayersRow()
        {
            using var f = new Fixture();
            var red = f.Join(Battlegrounds.Red);
            var blue = f.Join(Battlegrounds.Blue);
            var mate = f.Join(Battlegrounds.Red);

            f.Begin();

            f.Grounds.Damaged(red.Player, blue.Player, 120);
            f.Grounds.Damaged(red.Player, mate.Player, 500);
            f.Grounds.Damaged(red.Player, blue.Player, -5);
            f.Grounds.Healed(red.Player.EntityId, mate.Player, 80);
            f.Grounds.Healed(red.Player.EntityId, red.Player, 20);
            f.Grounds.Healed(red.Player.EntityId, blue.Player, 999);
            f.Grounds.Healed(0, mate.Player, 999);

            Assert.AreEqual(120, f.Match.Scores[red.Player.Id].Damage);
            Assert.AreEqual(100, f.Match.Scores[red.Player.Id].Healing);
        }

        [TestMethod]
        public void TheDeadGoBackToTheirTeamsHospitalsAtNoCost()
        {
            using var f = new Fixture();
            var red = f.Join(Battlegrounds.Red);
            var blue = f.Join(Battlegrounds.Blue);
            var stray = f.Player();

            CollectionAssert.AreEqual(new[] { RedHospital }, f.Grounds.HospitalsFor(red.Player).Select(h => h.TeleporterId).ToArray());
            CollectionAssert.AreEqual(new[] { BlueHospital }, Hospitals.AvailableTo(blue.Player, MapId).Select(h => h.TeleporterId).ToArray());
            Assert.IsNull(f.Grounds.HospitalsFor(stray.Player), "anybody else has the map's");

            f.Grounds.SetOwner(f.Match, f.Point(Whiskey), Battlegrounds.Red);
            f.Grounds.SetOwner(f.Match, f.Point(Echo), Battlegrounds.Blue);

            CollectionAssert.AreEquivalent(new[] { RedHospital, WhiskeyHospital }, f.Grounds.HospitalsFor(red.Player).Select(h => h.TeleporterId).ToArray());
            CollectionAssert.AreEquivalent(new[] { BlueHospital, EchoHospital }, f.Grounds.HospitalsFor(blue.Player).Select(h => h.TeleporterId).ToArray());

            Assert.IsTrue(f.Grounds.NoDeathPenalty(red.Player));
            Assert.IsFalse(f.Grounds.NoDeathPenalty(stray.Player));

            Assert.IsTrue(f.Grounds.OwnsTeleporter(MapId, RedHospital));
            Assert.IsTrue(f.Grounds.OwnsTeleporter(MapId, WhiskeyHospital));
            Assert.IsFalse(f.Grounds.OwnsTeleporter(MapId, 77));
            Assert.IsFalse(f.Grounds.OwnsTeleporter(999, RedHospital));
        }

        [TestMethod]
        public void NoSquadHoldsBothTeams()
        {
            using var f = new Fixture();
            var red = f.Join(Battlegrounds.Red);
            var blue = f.Join(Battlegrounds.Blue);
            var mate = f.Join(Battlegrounds.Red);
            var stray = f.Player();

            Assert.IsTrue(f.Grounds.OnDifferentTeams(new[] { red }, new[] { blue }));
            Assert.IsTrue(f.Grounds.OnDifferentTeams(new[] { red, stray }, new[] { stray, blue }));
            Assert.IsFalse(f.Grounds.OnDifferentTeams(new[] { red }, new[] { mate }));
            Assert.IsFalse(f.Grounds.OnDifferentTeams(new[] { red }, new[] { stray }));
            Assert.IsFalse(f.Grounds.OnDifferentTeams(new[] { stray }, null));
        }

        #endregion

        #region What the clients are told

        [TestMethod]
        public void APlayerArrivingIsShownTheMatchAsItStands()
        {
            using var f = new Fixture();
            var red = f.Join(Battlegrounds.Red);
            var blue = f.Join(Battlegrounds.Blue);
            var arrival = f.Player();

            f.Grounds.PlayerEntered(arrival);

            var packets = Packets(arrival);
            Assert.AreEqual(2, packets.OfType<SetNumberOfTeamsPacket>().Single().Teams);
            Assert.AreEqual(0, packets.OfType<ScoreBoardGameScorePacket>().Single().RemainingSeconds, "waiting");
            Assert.AreEqual(3, packets.OfType<ScoreBoardGameScorePacket>().Single().ControlPoints.Count);
            Assert.IsFalse(packets.OfType<ScoreBoardActivePacket>().Any());

            f.Begin();
            f.Grounds.SetOwner(f.Match, f.Point(Charlie), Battlegrounds.Blue);
            f.Now += 30000;
            Drain(arrival);

            f.Grounds.PlayerEntered(arrival);

            packets = Packets(arrival);
            Assert.IsTrue(packets.OfType<ScoreBoardActivePacket>().Single().Active);
            Assert.AreEqual(2, packets.OfType<ScoreBoardIndividualUpdatePacket>().Count());
            Assert.AreEqual(20 * 60 - 30, packets.OfType<ScoreBoardGameScorePacket>().Single().RemainingSeconds);
            Assert.AreEqual(Battlegrounds.Blue, packets.OfType<ScoreBoardGameScorePacket>().Single().ControlPoints[10]);

            var row = packets.OfType<ScoreBoardIndividualUpdatePacket>().Single(r => r.EntityId == red.Player.EntityId);
            Assert.AreEqual("Fixture", row.Name);
            Assert.AreEqual(Battlegrounds.Red, row.TeamId);
            Assert.IsTrue(row.Active);

            // A map with no match says nothing.
            var elsewhere = f.Player();
            elsewhere.Player.MapChannel = new MapChannel { MapInfo = new MapInfo(999, "elsewhere", 1, 0), ClientList = new List<Client>() };
            Drain(elsewhere);
            f.Grounds.PlayerEntered(elsewhere);
            f.Grounds.PlayerLeft(elsewhere);
            Assert.AreEqual(0, Packets(elsewhere).Count);
            Assert.IsNotNull(blue);
        }

        [TestMethod]
        public void ThePacketsAreWhatTheClientReads()
        {
            Read(new JoinedTeamPacket(2, new List<ulong> { 11, 12 }), r =>
            {
                Assert.AreEqual(2, r.ReadTuple());
                Assert.AreEqual(2, r.ReadInt());
                Assert.AreEqual(2, r.ReadList());
                Assert.AreEqual(11UL, r.ReadULong());
                Assert.AreEqual(12UL, r.ReadULong());
            });

            Read(new LeftTeamPacket(), r => Assert.AreEqual(0, r.ReadTuple()));
            Read(new AddTeamMemberPacket(31), r => { Assert.AreEqual(1, r.ReadTuple()); Assert.AreEqual(31UL, r.ReadULong()); });
            Read(new RemoveTeamMemberPacket(32), r => { Assert.AreEqual(1, r.ReadTuple()); Assert.AreEqual(32UL, r.ReadULong()); });
            Read(new SetNumberOfTeamsPacket(2), r => { Assert.AreEqual(1, r.ReadTuple()); Assert.AreEqual(2, r.ReadInt()); });
            Read(new ScoreBoardActivePacket(true), r => { Assert.AreEqual(1, r.ReadTuple()); Assert.IsTrue(r.ReadBool()); });
            Read(new SetOwnerIdPacket(-1), r => { Assert.AreEqual(1, r.ReadTuple()); Assert.AreEqual(-1, r.ReadInt()); });
            Read(new WonBattlegroundPacket(), r => Assert.AreEqual(0, r.ReadTuple()));
            Read(new LostBattlegroundPacket(), r => Assert.AreEqual(0, r.ReadTuple()));

            Read(new ScoreBoardGameScorePacket(90, new Dictionary<uint, uint> { [12] = 1, [10] = 0 }), r =>
            {
                Assert.AreEqual(2, r.ReadTuple());
                Assert.AreEqual(90, r.ReadInt());
                Assert.AreEqual(2, r.ReadDictionary());
                Assert.AreEqual(12, r.ReadInt());
                Assert.AreEqual(1, r.ReadInt());
                Assert.AreEqual(10, r.ReadInt());
                r.ReadNoneStruct();
            });

            Read(new ScoreBoardIndividualUpdatePacket(77) { Name = "Ward", ClassId = 4, TeamId = 1, Active = true, Kills = 3, Deaths = 2, Damage = 900, Healing = 40, Captures = 1, Prestige = 80 }, r =>
            {
                Assert.AreEqual(2, r.ReadTuple());
                Assert.AreEqual(77UL, r.ReadULong());
                Assert.AreEqual(10, r.ReadList(), "a list: the client's tracker writes into it");
                Assert.AreEqual("Ward", r.ReadUnicodeString());
                Assert.AreEqual(4, r.ReadInt());
                Assert.AreEqual(1, r.ReadInt());
                Assert.IsTrue(r.ReadBool());
                CollectionAssert.AreEqual(new[] { 3, 2, 900, 40, 1, 80 }, Enumerable.Range(0, 6).Select(_ => r.ReadInt()).ToArray());
            });

            Assert.AreEqual(GameOpcode.JoinedTeam, new JoinedTeamPacket(1, null).Opcode);
            Assert.AreEqual(GameOpcode.ScoreBoardGameScore, new ScoreBoardGameScorePacket(0, null).Opcode);
            Assert.AreEqual(GameOpcode.SetOwnerId, new SetOwnerIdPacket(1).Opcode);
        }

        [TestMethod]
        public void ATeamsControlPointMarkerIsTeamOwned()
        {
            byte[] Bytes(MapMarkerState state)
            {
                using var stream = new MemoryStream();
                using (var binary = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                using (var writer = new PythonWriter(binary))
                    state.Write(writer);

                return stream.ToArray();
            }

            using (var reader = new PythonReader(new BinaryReader(new MemoryStream(Bytes(MapMarkerState.TeamControlPoint(Battlegrounds.Blue))))))
            {
                Assert.AreEqual(2, reader.ReadTuple());
                Assert.AreEqual(3, reader.ReadInt(), "TEAM_OWNED");
                Assert.AreEqual(2, reader.ReadInt());
            }

            using (var reader = new PythonReader(new BinaryReader(new MemoryStream(Bytes(MapMarkerState.TeamControlPoint(0))))))
            {
                Assert.AreEqual(2, reader.ReadTuple());
                Assert.AreEqual(3, reader.ReadInt());
                reader.ReadNoneStruct();
            }

            using (var reader = new PythonReader(new BinaryReader(new MemoryStream(Bytes(MapMarkerState.ControlPoint(true))))))
            {
                Assert.AreEqual(2, reader.ReadTuple());
                Assert.AreEqual(6, reader.ReadInt(), "FACTION_OWNED, as before");
                Assert.IsTrue(reader.ReadBool());
            }
        }

        #endregion

        #region The record

        [TestMethod]
        public void AMatchThatEndsIsPutOnRecord()
        {
            using var f = new Fixture();
            var noon = new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc);
            var utc = noon;
            var store = new MemoryPvpStore();
            var wagered = new Dictionary<uint, uint>();
            var records = new PvpRecords
            {
                UtcNow = () => utc,
                WageredItems = ids => ids.Where(wagered.ContainsKey).Select(id => new PvpMatchWagerEntry { CharacterId = id, ItemId = wagered[id], StackSize = 1 }).ToList()
            };

            records.Load(store);
            f.Grounds.Records = records;

            var red = f.Join(Battlegrounds.Red);
            var mate = f.Join(Battlegrounds.Red);
            var blue = f.Join(Battlegrounds.Blue);
            var deserter = f.Join(Battlegrounds.Blue);

            // One on each team has an item wagered.
            wagered[mate.Player.Id] = 901;
            wagered[blue.Player.Id] = 902;

            red.Player.ClanId = 31;
            f.Garrison[Whiskey] = ControlPoints.Garrison.Down;
            f.Begin();
            f.Tick(1000);

            var wargameId = f.Match.WargameId;

            f.Grounds.Kill(red, blue);
            f.Grounds.Kill(red, blue);
            f.Grounds.Kill(blue, mate);
            Assert.IsTrue(f.Grounds.Captured(red, f.Point(Whiskey)));
            f.Grounds.SetOwner(f.Match, f.Point(Echo), Battlegrounds.Blue);
            f.Grounds.PlayerLeft(deserter);

            Assert.AreEqual(0, store.Matches.Count, "nothing is written until it is over");

            utc = noon.AddMinutes(12);
            f.Grounds.End(f.Match, Battlegrounds.Red, "points");

            var match = store.Matches.Single();

            Assert.AreEqual((byte)PvpMatchKind.Battleground, match.Kind);
            Assert.AreEqual(wargameId, match.WargameId);
            Assert.AreEqual(MapId, match.MapContextId);
            Assert.AreEqual(f.World.Map.InstanceId, match.InstanceId);
            Assert.AreEqual(noon, match.StartedAt);
            Assert.AreEqual(noon.AddMinutes(12), match.EndedAt);
            Assert.AreEqual((byte)PvpMatchOutcome.Won, match.Outcome);
            Assert.AreEqual((byte)1, match.WinnerSide, "Red Team is side 1");
            Assert.AreEqual("points", match.Reason);
            Assert.AreEqual("Red Team", match.Side1Name);
            Assert.AreEqual("Blue Team", match.Side2Name);
            Assert.AreEqual((1, 1), (match.Side1Score, match.Side2Score), "the points each held when it ended, not after the field was reset");
            Assert.AreEqual((2, 1), (match.Side1Kills, match.Side2Kills));

            Assert.AreEqual(4, store.Rows[match.Id].Count);

            var scorer = store.Row(match.Id, red.Player.Id);

            Assert.AreEqual((byte)1, scorer.Side);
            Assert.AreEqual(red.Player.Name, scorer.Name);
            Assert.AreEqual("Fixture", scorer.FamilyName);
            Assert.AreEqual(31u, scorer.ClanId);
            Assert.AreEqual((2, 0), (scorer.Kills, scorer.Deaths));
            Assert.AreEqual(1, scorer.Captures);
            Assert.IsTrue(scorer.Prestige >= 50, "the capture's at least");
            Assert.AreEqual(f.Prestige[red], scorer.Prestige, "what the match gave them: its kills and its capture, and nothing for a win before the minimum time");
            Assert.IsTrue(scorer.PresentAtEnd);

            Assert.AreEqual((0, 1), (store.Row(match.Id, mate.Player.Id).Kills, store.Row(match.Id, mate.Player.Id).Deaths));

            var loser = store.Row(match.Id, blue.Player.Id);

            Assert.AreEqual((byte)2, loser.Side);
            Assert.AreEqual((1, 2), (loser.Kills, loser.Deaths));

            var gone = store.Row(match.Id, deserter.Player.Id);

            Assert.AreEqual((byte)2, gone.Side);
            Assert.IsFalse(gone.PresentAtEnd, "a deserter is on the record, as one who left");

            // What was wagered: on record for both teams, and kept - a match takes nobody's item.
            Assert.AreEqual(2, store.Wagers[match.Id].Count);
            Assert.AreEqual((901u, (byte)1), (store.Wager(match.Id, mate.Player.Id).ItemId, store.Wager(match.Id, mate.Player.Id).Side));
            Assert.AreEqual((902u, (byte)2), (store.Wager(match.Id, blue.Player.Id).ItemId, store.Wager(match.Id, blue.Player.Id).Side));
            Assert.IsTrue(store.Wagers[match.Id].Values.All(w => w.Result == (byte)PvpWagerResult.Kept));

            // The next match on the same field is another record; with nobody ahead it is nobody's.
            f.Begin();
            utc = noon.AddMinutes(40);
            f.Grounds.End(f.Match, 0, "time");

            Assert.AreEqual(2, store.Matches.Count);
            Assert.AreNotEqual(match.Id, store.Matches[1].Id);
            Assert.AreEqual((byte)PvpMatchOutcome.Tied, store.Matches[1].Outcome);
            Assert.AreEqual((byte)0, store.Matches[1].WinnerSide);
            Assert.AreEqual("time", store.Matches[1].Reason);
            Assert.AreEqual(noon.AddMinutes(12), store.Matches[1].StartedAt);
            Assert.AreEqual((0, 0), (store.Matches[1].Side1Score, store.Matches[1].Side2Score));
            Assert.AreEqual(3, store.Rows[store.Matches[1].Id].Count, "those on a team in it");

            // A match with nobody in it - a game master's, on an empty field - leaves no record.
            using var empty = new Fixture();

            empty.Grounds.Records = records;
            empty.Grounds.Start(empty.Match, forced: true);
            empty.Grounds.End(empty.Match, 0, "gm");

            Assert.AreEqual(2, store.Matches.Count);
        }

        #endregion

        #region Copies, commands and data

        [TestMethod]
        public void EveryChannelOfTheMapHasItsOwnMatch()
        {
            using var f = new Fixture();
            var copy = new MapChannel { MapInfo = f.World.Map.MapInfo, InstanceId = 5, ClientList = new List<Client>(), IsSharedInstance = true };
            var other = f.Grounds.MatchOf(copy);

            Assert.AreNotSame(f.Match, other);
            Assert.AreSame(other, f.Grounds.MatchOf(copy));
            Assert.AreEqual(3, other.Points.Count);
            Assert.AreNotSame(f.Point(Whiskey).Object, other.Points[0].Object, "its own objects");
            Assert.AreSame(copy, other.Points[0].Object.RuntimeMapChannel);
            Assert.AreSame(other.Points[0].Object, copy.ControlPoints[Whiskey]);

            f.Join(Battlegrounds.Red);
            Assert.AreEqual(0, other.Count(Battlegrounds.Red));

            f.Grounds.SetOwner(other, other.Points[0], Battlegrounds.Blue);
            Assert.AreEqual(0u, f.Point(Whiskey).Owner);

            Assert.IsNull(f.Grounds.MatchOf(new MapChannel { MapInfo = new MapInfo(999, "elsewhere", 1, 0), ClientList = new List<Client>() }));

            f.Grounds.Forget(copy);
            Assert.AreEqual(1, f.Grounds.Matches.Count);
        }

        [TestMethod]
        public void TheBattlegroundCommandRunsAMatchForOne()
        {
            using var f = new Fixture();
            var master = f.Player();

            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(master, new GameAccountEntry { Level = (byte)GmLevel.Admin });

            var commands = new ChatCommandsManager(null);
            commands.RegisterChatCommands();

            List<string> Say(string command)
            {
                Drain(master);
                commands.ProcessCommand(master, command);
                return Packets(master).OfType<SystemMessagePacket>().Select(message => message.TextMessage).ToList();
            }

            var status = Say(".bg");
            StringAssert.Contains(status[0], "Waiting");
            StringAssert.Contains(status[0], "Red 0, Blue 0");
            Assert.AreEqual(6, status.Count, "the match, a line a team, a line a point");

            StringAssert.Contains(Say(".bg team red").Last(), "You are on Red Team.");
            Assert.AreEqual(Battlegrounds.Red, f.Grounds.TeamOf(master.Player));
            StringAssert.Contains(Say(".bg team blue").Last(), "You are on Blue Team.");
            Assert.AreEqual(Battlegrounds.Blue, f.Grounds.TeamOf(master.Player), "whatever the rules say");
            Assert.AreEqual(1, f.Match.Members.Count);

            StringAssert.Contains(Say(".bg end").Single(), "No match is running");
            StringAssert.Contains(Say(".bg start").Last(), "The match has begun");
            Assert.AreEqual(Battlegrounds.Phase.Running, f.Match.Phase);
            Assert.IsTrue(f.Match.Forced);
            StringAssert.Contains(Say(".bg start").Single(), "running");

            StringAssert.Contains(Say(".bg capture whiskey blue").Last(), "Whiskey is Blue Team's.");
            StringAssert.Contains(Say(".bg capture 103 red").Last(), "Echo is Red Team's.");
            StringAssert.Contains(Say(".bg capture nowhere red").Single(), "no control point");
            StringAssert.Contains(Say(".bg capture whiskey green").Single(), "usage:");
            StringAssert.Contains(Say(".bg")[3], "Whiskey: Blue Team");

            StringAssert.Contains(Say(".bg end blue").Last(), "Blue Team won");
            CollectionAssert.AreEqual(new[] { Battlegrounds.Blue }, f.Ended);
            Assert.IsFalse(f.Prestige.ContainsKey(master), "ended before its minimum time");

            StringAssert.Contains(Say(".bg team none").Last(), "You are on no team.");
            StringAssert.Contains(Say(".bg team none").Single(), "You were on no team.");

            master.Player.MapChannel = new MapChannel { MapInfo = new MapInfo(999, "elsewhere", 1, 0), ClientList = new List<Client>() };
            master.Player.MapContextId = 999;
            StringAssert.Contains(Say(".bg").Single(), "no battleground");
        }

        [TestMethod]
        public void TheBattlegroundCommandShowsAndForgivesALockout()
        {
            using var f = new Fixture();
            var master = f.Player();
            var leaver = f.Player();

            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(master, new GameAccountEntry { Level = (byte)GmLevel.Admin });
            f.Exempt.Add(master);
            leaver.Player.FamilyName = "Leaver";

            var commands = new ChatCommandsManager(null);
            commands.RegisterChatCommands();

            List<string> Say(string command)
            {
                Drain(master);
                commands.ProcessCommand(master, command);
                return Packets(master).OfType<SystemMessagePacket>().Select(message => message.TextMessage).ToList();
            }

            Assert.AreEqual(6, Say(".bg").Count, "nothing of a lockout while there is none");

            f.Grounds.Lock(master.Player.Id, MapId, 5);
            f.Grounds.Lock(leaver.Player.Id, MapId, 5);

            var line = Say(".bg").Last();
            StringAssert.Contains(line, "instance 5 of map " + MapId);
            StringAssert.Contains(line, "15 more minutes");
            StringAssert.Contains(line, "game master is not held to it");

            StringAssert.Contains(Say(".bg forgive").Single(), "Fixture's desertion is forgotten");
            Assert.IsNull(f.Grounds.LockoutOf(master.Player.Id));
            StringAssert.Contains(Say(".bg forgive").Single(), "Fixture has deserted nothing");

            StringAssert.Contains(Say(".bg forgive leaver").Single(), "leaver is not in the world");
            Assert.IsNotNull(f.Grounds.LockoutOf(leaver.Player.Id));

            lock (Server.Clients)
                Server.Clients.Add(leaver);

            try
            {
                // From anywhere, of anybody in the world, by their name in any case.
                master.Player.MapChannel = new MapChannel { MapInfo = new MapInfo(999, "elsewhere", 1, 0), ClientList = new List<Client>() };
                master.Player.MapContextId = 999;

                StringAssert.Contains(Say(".bg forgive leaver").Single(), "Leaver's desertion is forgotten");
                Assert.IsNull(f.Grounds.LockoutOf(leaver.Player.Id));
            }
            finally
            {
                lock (Server.Clients)
                    Server.Clients.Remove(leaver);
            }

            StringAssert.Contains(Say(".bg forgive a b").Single(), "no battleground");
        }

        [TestMethod]
        public void EdmundRangeIsTheMapWithAMatch()
        {
            var grounds = new Battlegrounds();
            var range = grounds.Definitions[EdmundRangeSeed.MapContextId];

            Assert.IsTrue(grounds.IsBattleground(2374));
            Assert.IsFalse(grounds.IsBattleground(1220));
            Assert.AreEqual(405u, range.RedHospitalId);
            Assert.AreEqual(404u, range.BlueHospitalId);

            // controlpointdata: Whiskey 12, Charlie 10, Echo 11.
            Assert.AreEqual(12u, range.ClientPointIds[EdmundRangeSeed.WhiskeyId]);
            Assert.AreEqual(10u, range.ClientPointIds[EdmundRangeSeed.CharlieId]);
            Assert.AreEqual(11u, range.ClientPointIds[EdmundRangeSeed.EchoId]);

            Assert.AreEqual((byte)MapLinkKind.TeamRed, EdmundRangeSeed.KindTeamRed);
            Assert.AreEqual((byte)MapLinkKind.TeamBlue, EdmundRangeSeed.KindTeamBlue);
            Assert.AreEqual((byte)MapLinkKind.TeamLeave, EdmundRangeSeed.KindTeamLeave);

            var config = new RasaGame::Rasa.Config.Config().Battleground;
            Assert.AreEqual(60, config.PrepSeconds);
            Assert.AreEqual(10, config.MinMinutes);
            Assert.AreEqual(20, config.MaxMinutes);
            Assert.AreEqual(45, config.MinLevel);
        }

        [TestMethod]
        public void TheMigrationPutsTheRangesRowsInTheWorldDatabaseAndTakesThemOut()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                using var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), Path.Combine(directory, "database"));
                var migrator = context.GetService<IMigrator>();

                List<string> Rows(string sql)
                {
                    var rows = new List<string>();
                    var connection = context.Database.GetDbConnection();

                    if (connection.State != System.Data.ConnectionState.Open)
                        connection.Open();

                    using var command = connection.CreateCommand();
                    command.CommandText = sql;

                    using var reader = command.ExecuteReader();

                    while (reader.Read())
                        rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetValue(i).ToString())));

                    return rows;
                }

                migrator.Migrate();

                // Each Simulated Bane is its Howling Maw row under another name.
                CollectionAssert.AreEqual(new[]
                {
                    "595001|Simulated Thrax Rifleman|10731",
                    "595002|Simulated Thrax Grunt|10730",
                    "595003|Simulated Thrax Technician|10732",
                    "595004|Simulated Caretaker|10727"
                }, Rows("select id, comment, name_id from creature where id between 595001 and 595004 order by id"));

                Assert.AreEqual(Rows("select class_id, faction, level, max_hp, action1, action2 from creature where id = 531029").Single(),
                    Rows("select class_id, faction, level, max_hp, action1, action2 from creature where id = 595001").Single());
                Assert.AreEqual(Rows("select count(*) from creature_stat where id between 531029 and 531032").Single(),
                    Rows("select count(*) from creature_stat where id between 595001 and 595004").Single());
                Assert.AreEqual(Rows("select count(*) from creature_appearance where id between 531029 and 531032").Single(),
                    Rows("select count(*) from creature_appearance where id between 595001 and 595004").Single());

                Assert.AreEqual("6|1|2374", Rows("select count(*), min(mode), min(map_context_id) from spawnpool where id between 595101 and 595106").Single());

                CollectionAssert.AreEqual(new[] { "101|Whiskey|10000071|134419591466262", "102|Charlie|10000071|134419591466263", "103|Echo|10000071|134419591466264" },
                    Rows("select id, name, class_id, marker_entity_id from control_point where map_context_id = 2374 order by id"));
                Assert.AreEqual("11", Rows("select count(*) from control_point_link where control_point_id between 101 and 103").Single());

                CollectionAssert.AreEqual(new[] { "9003|2|2374", "9004|3|2374", "9005|4|2374", "9006|4|2374" },
                    Rows("select id, kind, dest_map_context_id from map_link where id between 9003 and 9006 order by id"));

                migrator.Migrate("20261106000000_Add_edmund_range_door");

                Assert.AreEqual("0", Rows("select count(*) from creature where id between 595001 and 595004").Single());
                Assert.AreEqual("0", Rows("select count(*) from creature_stat where id between 595001 and 595004").Single());
                Assert.AreEqual("0", Rows("select count(*) from creature_appearance where id between 595001 and 595004").Single());
                Assert.AreEqual("0", Rows("select count(*) from spawnpool where id between 595101 and 595106").Single());
                Assert.AreEqual("0", Rows("select count(*) from control_point where map_context_id = 2374").Single());
                Assert.AreEqual("0", Rows("select count(*) from control_point_link where control_point_id between 101 and 103").Single());
                Assert.AreEqual("2", Rows("select count(*) from map_link where id > 9000").Single(), "the door of the migration before stays");
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void TheMigrationIsTheSameStatementsOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var script = context.GetService<IMigrator>()
                .GenerateScript("20261106000000_Add_edmund_range_door", "20261107000000_Add_edmund_range_match");

            foreach (var insert in EdmundRangeSeed.InsertStatements)
                StringAssert.Contains(script, insert);

            StringAssert.Contains(script, "'20261107000000_Add_edmund_range_match'");
            Assert.AreEqual(16, EdmundRangeSeed.InsertStatements.Count(), "three a creature, and one a table");
        }

        [TestMethod]
        public void ATeamKillGeneratesPrestigeByTheFeudsNumbersAndStealsNone()
        {
            using var f = new Fixture();
            var killer = f.Player();
            var victim = f.Player();

            killer.Player.Level = 46;
            victim.Player.Level = 50;
            victim.Player.Credits[CurencyType.Prestige] = 10000;

            Assert.AreEqual(38, PvpPrestige.TeamKill(killer, victim), "30 and 2 a level of the victim's four");
            Assert.AreEqual(38, f.Prestige[killer]);
            Assert.AreEqual(10000, victim.Player.Credits[CurencyType.Prestige]);

            var said = Packets(killer).OfType<DisplayClientMessagePacket>().Single();
            Assert.AreEqual(PlayerMessage.PmPrestigePointsReceivedPvpkill, said.MsgId);
            Assert.AreEqual("0", said.Args["amountStolen"]);

            Assert.AreEqual(0, PvpPrestige.TeamKill(killer, victim), "once in the interval");

            // Ten levels up is past a kill's credit, and so is ten down.
            killer.Player.Level = 50;
            victim.Player.Level = 40;
            Assert.AreEqual(0, PvpPrestige.TeamKill(victim, killer));

            var third = f.Player();
            third.Player.Level = 40;
            Assert.AreEqual(0, PvpPrestige.TeamKill(killer, third));
        }

        #endregion

        #region Helpers

        private static List<PythonPacket> Packets(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet).ToList();

        private static void Drain(Client client) => WorldTestContext.Drain(client);

        private static List<PlayerMessage> Messages(IEnumerable<PythonPacket> packets) =>
            packets.OfType<DisplayClientMessagePacket>().Select(p => p.MsgId).ToList();

        private static void Read(ServerPythonPacket packet, Action<PythonReader> read)
        {
            using var stream = new MemoryStream();
            using (var binary = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            using (var writer = new PythonWriter(binary))
                packet.Write(writer);

            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            read(reader);
        }

        /// <summary>
        /// The fixture map as a battleground: the staging area on the ground, the field a
        /// hundred metres up, a base at either end, three points and the four links. Its own
        /// clock, and the singleton swapped for the manager under test.
        /// </summary>
        private sealed class Fixture : IDisposable
        {
            private static readonly FieldInfo InstanceField = typeof(Battlegrounds).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);

            private readonly object _previous;
            private readonly Func<Client, int, bool> _change;
            private readonly Func<IEnumerable<(uint, uint, Vector3, string)>> _hospitalSource;
            private readonly Func<uint, bool> _hospitalSafe;
            private readonly Func<uint, bool> _hospitalOpen;
            private readonly Func<long> _prestigeNow;
            private readonly List<ControlPoints.Point> _sources = new List<ControlPoints.Point>();
            private uint _nextWargame = 7001;

            internal long Now = 1_000_000;
            internal WorldTestContext World { get; } = new WorldTestContext();
            internal Battlegrounds Grounds { get; } = new Battlegrounds();
            internal List<(Client Client, Vector3 Position)> Teleports { get; } = new List<(Client, Vector3)>();
            internal Dictionary<Client, int> Prestige { get; } = new Dictionary<Client, int>();
            internal Dictionary<uint, ControlPoints.Garrison> Garrison { get; } = new Dictionary<uint, ControlPoints.Garrison>();
            internal HashSet<Client> Exempt { get; } = new HashSet<Client>();
            internal List<uint> Ended { get; } = new List<uint>();

            internal MapLink RedDoor { get; }
            internal MapLink BlueDoor { get; }
            internal MapLink RedExit { get; }
            internal MapLink BlueExit { get; }

            internal Battlegrounds.Match Match => Grounds.MatchOf(World.Map);

            internal Fixture()
            {
                _previous = InstanceField.GetValue(null);
                _change = PvpPrestige.Change;
                _hospitalSource = Hospitals.Source;
                _hospitalSafe = Hospitals.IsSafeZone;
                _hospitalOpen = Hospitals.IsOpen;
                _prestigeNow = PvpPrestige.Now;

                InstanceField.SetValue(null, Grounds);

                var definition = new Battlegrounds.Definition
                {
                    MapContextId = MapId,
                    RedHospitalId = RedHospital,
                    BlueHospitalId = BlueHospital,
                    StagingMaxY = 50f,
                    StagingArrival = Staging
                };

                definition.ClientPointIds[Whiskey] = 12;
                definition.ClientPointIds[Charlie] = 10;
                definition.ClientPointIds[Echo] = 11;

                Grounds.Definitions.Clear();
                Grounds.Definitions[MapId] = definition;
                Grounds.Config = new BattlegroundConfig();
                Grounds.Now = () => Now;
                Grounds.NextWargameId = () => _nextWargame++;
                Grounds.IsExempt = client => Exempt.Contains(client);
                Grounds.MatchEnded = (match, winner) => Ended.Add(winner);
                Grounds.Teleport = (client, position, rotation) =>
                {
                    client.Player.Position = position;
                    Teleports.Add((client, position));
                };

                _sources.Add(Source(Whiskey, "Whiskey", new Vector3(-100, 100, 50), 9101, WhiskeyHospital));
                _sources.Add(Source(Charlie, "Charlie", new Vector3(0, 100, 50), 9102, 0));
                _sources.Add(Source(Echo, "Echo", new Vector3(100, 100, 50), 9103, EchoHospital));

                Grounds.PointsOn = id => id == MapId ? _sources : new List<ControlPoints.Point>();
                Grounds.GarrisonOf = (map, point) => Garrison.TryGetValue(point.Id, out var garrison) ? garrison : ControlPoints.Garrison.None;

                Hospitals.Source = () => new[]
                {
                    (BlueHospital, MapId, BlueBase, "Hospital for Team Blue"),
                    (RedHospital, MapId, RedBase, "Hospital for Team Red"),
                    (WhiskeyHospital, MapId, new Vector3(-100, 100, 60), "Hospital: Control Point Whiskey"),
                    (EchoHospital, MapId, new Vector3(100, 100, 60), "Hospital: Control Point Echo")
                };
                Hospitals.IsSafeZone = _ => false;
                Hospitals.IsOpen = _ => true;
                Hospitals.Reset();

                PvpPrestige.Reset();
                PvpPrestige.Now = () => Now;
                PvpPrestige.Change = (client, amount) =>
                {
                    Prestige[client] = (Prestige.TryGetValue(client, out var had) ? had : 0) + amount;
                    return true;
                };

                RedDoor = Link(1, MapLinkKind.TeamRed, new Vector3(10, 0, 0), 3f, RedBase);
                BlueDoor = Link(2, MapLinkKind.TeamBlue, new Vector3(-10, 0, 0), 3f, BlueBase);
                RedExit = Link(3, MapLinkKind.TeamLeave, RedBase + new Vector3(-8, 0, 5), 1.5f, new Vector3(20, 0, 20));
                BlueExit = Link(4, MapLinkKind.TeamLeave, BlueBase + new Vector3(8, 0, 5), 1.5f, new Vector3(-20, 0, 20));
            }

            private static ControlPoints.Point Source(uint id, string name, Vector3 position, ulong marker, uint hospital)
            {
                var point = new ControlPoints.Point
                {
                    Id = id,
                    MapContextId = MapId,
                    Name = name,
                    ClassId = (EntityClasses)10000071,
                    Position = position,
                    MarkerEntityId = marker,
                    IsBattleground = true
                };

                if (hospital != 0)
                    point.Hospitals.Add(hospital);

                if (id == Whiskey)
                    point.BanePools.Add(9201);

                return point;
            }

            private MapLink Link(uint id, MapLinkKind kind, Vector3 position, float radius, Vector3 destination)
            {
                var link = new MapLink
                {
                    Id = id,
                    MapContextId = MapId,
                    Position = position,
                    Radius = radius,
                    DestMapContextId = MapId,
                    DestPosition = destination,
                    Kind = kind,
                    Enabled = true,
                    Comment = kind.ToString()
                };

                CellManager.Instance.AddToWorld(World.Map, link);

                return link;
            }

            internal Battlegrounds.Point Point(uint id) => Match.Points.Single(p => p.Id == id);

            /// <summary>A player in the staging area, level 50, with nothing waiting to be read.</summary>
            internal Client Player()
            {
                var client = World.CreateClient(Staging.X, Staging.Z);

                client.Player.Level = 50;
                CellManager.Instance.AddToWorld(client);
                WorldTestContext.Drain(client);

                return client;
            }

            /// <summary>A player put on a team as a game master would be - no balance, no desertion - and in its base.</summary>
            internal Client Join(uint team)
            {
                var client = Player();

                Assert.IsTrue(Grounds.Join(client, team, force: true));
                WorldTestContext.Drain(client);

                return client;
            }

            /// <summary>The match begun as the worker begins it, with everyone on the field of their own base.</summary>
            internal void Begin()
            {
                var before = Match.WargameId;

                Grounds.Start(Match, forced: false);
                Assert.AreNotEqual(before, Match.WargameId);

                foreach (var client in World.Map.ClientList)
                    WorldTestContext.Drain(client);
            }

            internal void TakeAll(uint team)
            {
                foreach (var point in Match.Points)
                    Grounds.SetOwner(Match, point, team);
            }

            /// <summary>Moves the clock on and runs the worker once.</summary>
            internal void Tick(long ms)
            {
                Now += ms;
                Grounds.Worker(World.Map);
            }

            public void Dispose()
            {
                InstanceField.SetValue(null, _previous);
                PvpPrestige.Change = _change;
                PvpPrestige.Now = _prestigeNow;
                PvpPrestige.Reset();
                Hospitals.Source = _hospitalSource;
                Hospitals.IsSafeZone = _hospitalSafe;
                Hospitals.IsOpen = _hospitalOpen;
                Hospitals.Reset();
                World.Dispose();
            }
        }

        #endregion
    }
}
