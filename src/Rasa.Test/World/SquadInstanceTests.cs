extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Config;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.ClientMethod.Server;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;
    using Rasa.Test.Missions.Encounters;

    /// <summary>
    /// Squad instances (MapChannelManager.SquadInstances): an Operation is entered as the
    /// instance of the squad's leader, a player who dies in one gets up at its entrance, it
    /// is kept when everybody has left, and the weekly reset closes those nobody is in.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class SquadInstanceTests
    {
        internal const uint Caves = 1506;       // Caves of Donn
        private const uint Hospital = 7001;
        private const long SquadHold = MapChannelManager.SquadHoldMs;
        internal static readonly Vector3 Entrance = new Vector3(400, 5, 0);
        internal static readonly Vector3 Deep = new Vector3(900, 5, 300);
        internal static readonly Vector3 Outside = new Vector3(30, 2, 40);

        private Func<IEnumerable<MapLink>> _links;
        private Func<IEnumerable<(uint, uint, Vector3, string)>> _source;
        private Func<uint, bool> _safe;
        private Action<Client, CharacterTeleporterEntry> _persist;

        [TestInitialize]
        public void KeepTheWorld()
        {
            _links = InstanceEntrances.MapLinks;
            _source = Hospitals.Source;
            _safe = Hospitals.IsSafeZone;
            _persist = Hospitals.Persist;

            Hospitals.Source = () => new[] { (Hospital, Caves, new Vector3(700, 5, 100), "Hospital: Caves of Donn") };
            Hospitals.IsSafeZone = _ => false;
            Hospitals.Persist = (client, entry) => { };
            Hospitals.Reset();
        }

        [TestCleanup]
        public void RestoreTheWorld()
        {
            InstanceEntrances.MapLinks = _links;
            Hospitals.Source = _source;
            Hospitals.IsSafeZone = _safe;
            Hospitals.Persist = _persist;
            Hospitals.Reset();
        }

        #region Whose

        [TestMethod]
        public void APlayerInNoSquadEntersAnInstanceOfTheirOwn()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world);
            var client = fixture.Player();
            var other = fixture.Player();

            Assert.IsTrue(fixture.Maps.EnterMap(client, Caves, Entrance, 1.5f));

            var instance = client.PendingTransfer.DestinationMap;

            Assert.AreNotSame(fixture.Own, instance, "not the map's own channel");
            Assert.IsTrue(instance.IsSquadInstance);
            Assert.IsTrue(instance.IsCopy);
            Assert.IsFalse(instance.IsPrivateInstance, "a public place to those in it: kills, loot and squad credit as on any map");
            Assert.IsFalse(instance.IsSharedInstance);
            Assert.AreEqual(client.Player.Id, instance.SquadOwnerCharacterId);
            Assert.AreEqual(0u, instance.OwnerCharacterId);
            Assert.AreSame(fixture.Own.MapInfo, instance.MapInfo);
            Assert.AreNotEqual(fixture.Own.InstanceId, instance.InstanceId);
            Assert.AreEqual(Entrance, client.PendingTransfer.DestinationPosition);

            Assert.AreSame(instance, fixture.Maps.FindByContextAndInstance(Caves, instance.InstanceId));
            Assert.AreSame(fixture.Own, fixture.Maps.FindByContextId(Caves), "the map's own channel is still the map's");
            Assert.AreEqual(1, fixture.Maps.CopiesOf(Caves).Count, "no copy anyone may pick");
            Assert.IsFalse(Sent<ChooseInstanceListPacket>(client).Any());
            Assert.AreEqual(1, fixture.Maps.PopulationOf(instance), "counted there before the map has loaded");

            fixture.Maps.MapLoaded(client);

            Assert.AreSame(instance, client.Player.MapChannel);
            Assert.IsTrue(instance.ClientList.Contains(client));
            Assert.AreEqual(0, fixture.Own.ClientList.Count, "nobody is on the map's own channel");

            // Somebody else has their own.
            Assert.IsTrue(fixture.Maps.EnterMap(other, Caves, Entrance, 0));

            var theirs = other.PendingTransfer.DestinationMap;

            Assert.AreNotSame(instance, theirs);
            Assert.AreEqual(other.Player.Id, theirs.SquadOwnerCharacterId);
            CollectionAssert.AreEqual(new[] { instance, theirs }, fixture.Maps.SquadInstancesOf(Caves).ToArray());
        }

        [TestMethod]
        public void AnInstanceHasTheMapsSpawnPoolsAndDoorsAsItsOwnAndIsTickedWithTheWorld()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world) { Config = new SquadInstanceConfig { EmptyCloseSeconds = 0 } };
            var client = fixture.Player();
            var exit = new MapLink { Id = Fixture.ExitId, MapContextId = Caves, Position = Entrance, Radius = 4, DestMapContextId = world.Map.MapInfo.MapContextId, DestPosition = Outside, Enabled = true };

            fixture.Own.SpawnPools.Add(new SpawnPool { DbId = 77, MapContextId = Caves, Position = Deep, RespawnTime = 1000, UpdateTimer = 1000, SpawnSlot = new List<SpawnPoolSlot> { new(77, 1, 2) } });
            CellManager.Instance.AddToWorld(fixture.Own, exit);

            var instance = fixture.Into(client);
            var pool = instance.SpawnPools.Single();
            var door = instance.MapCellInfo.Cells.Values.SelectMany(cell => cell.MapLinks).Distinct().Single();

            Assert.AreEqual(77u, pool.DbId);
            Assert.AreNotSame(fixture.Own.SpawnPools.Single(), pool, "its own, to run by itself");
            Assert.AreEqual(Fixture.ExitId, door.Id);
            Assert.AreNotSame(exit, door);
            Assert.AreEqual(world.Map.MapInfo.MapContextId, door.DestMapContextId, "the way out leads out");

            fixture.Maps.MapChannelWorker(100);

            Assert.AreEqual(100L, instance.MapChannelElapsed);

            // Closed, it is taken apart; the map's own channel is as it was.
            fixture.Leave(client);
            fixture.Now += 60000;
            fixture.Maps.SquadInstanceWorker();

            Assert.AreEqual(0, instance.SpawnPools.Count);
            Assert.AreEqual(0, instance.MapCellInfo.Cells.Count);
            Assert.AreEqual(1, fixture.Own.SpawnPools.Count);
            Assert.AreSame(exit, fixture.Own.MapCellInfo.Cells.Values.SelectMany(cell => cell.MapLinks).Distinct().Single());

            fixture.Own.MapCellInfo.Cells.Clear();
        }

        [TestMethod]
        public void ASquadEntersTheInstanceOfItsLeader()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world);
            var leader = fixture.Player();
            var first = fixture.Player();
            var second = fixture.Player();
            var stranger = fixture.Player();

            fixture.Squad(leader, first, second);

            // A member goes in before the leader does: the leader's, all the same.
            Assert.IsTrue(fixture.Maps.EnterMap(first, Caves, Entrance, 0));

            var instance = first.PendingTransfer.DestinationMap;

            Assert.AreEqual(leader.Player.Id, instance.SquadOwnerCharacterId);
            fixture.Maps.MapLoaded(first);

            Assert.IsTrue(fixture.Maps.EnterMap(leader, Caves, Entrance, 0));
            Assert.IsTrue(fixture.Maps.EnterMap(second, Caves, Entrance, 0));

            Assert.AreSame(instance, leader.PendingTransfer.DestinationMap);
            Assert.AreSame(instance, second.PendingTransfer.DestinationMap);
            Assert.AreEqual(1, fixture.Maps.SquadInstancesOf(Caves).Count);
            Assert.AreEqual(3, fixture.Maps.PopulationOf(instance));

            Assert.IsTrue(fixture.Maps.EnterMap(stranger, Caves, Entrance, 0));
            Assert.AreNotSame(instance, stranger.PendingTransfer.DestinationMap, "not of the squad");
        }

        [TestMethod]
        public void TheSquadsInstanceIsWhereTheSquadIsWhenTheLeadChangesHands()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world);
            var leader = fixture.Player();
            var member = fixture.Player();
            var late = fixture.Player();

            fixture.Squad(leader, member, late);
            var instance = fixture.Into(leader);
            fixture.Into(member);

            // The lead passes to the member.
            fixture.Squad(member, leader, late);

            Assert.AreSame(instance, fixture.Into(late), "where the leader is");
            Assert.AreEqual(member.Player.Id, instance.SquadOwnerCharacterId, "and the leader's from now on");
            Assert.AreEqual(1, fixture.Maps.SquadInstancesOf(Caves).Count);

            // A new leader who has kept an instance of their own: still where they stand.
            var keeper = fixture.Player();
            var old = fixture.Player();
            var third = fixture.Player();
            var kept = fixture.Into(keeper);

            fixture.Leave(keeper);
            fixture.Squad(old, keeper, third);

            var squads = fixture.Into(old);

            Assert.AreSame(squads, fixture.Into(keeper), "the old leader's, with the squad");
            Assert.AreNotSame(kept, squads);

            fixture.Squad(keeper, old, third);

            Assert.AreSame(squads, fixture.Into(third), "where the leader stands, not the one they kept");
            Assert.AreEqual(old.Player.Id, squads.SquadOwnerCharacterId, "which stays its owner's: the leader has their own");
            Assert.AreEqual(keeper.Player.Id, kept.SquadOwnerCharacterId);

            // With the leader out of it, the squad goes to the leader's own.
            fixture.Leave(keeper);
            fixture.Leave(third);
            Assert.AreSame(kept, fixture.Into(third));

            // A player in their own when they join a squad: the squad comes to them.
            var solo = fixture.Player();
            var joiner = fixture.Player();
            var head = fixture.Player();
            var own = fixture.Into(solo);

            fixture.Squad(head, solo, joiner);

            Assert.AreSame(own, fixture.Into(joiner));
            Assert.AreEqual(head.Player.Id, own.SquadOwnerCharacterId);

            // One who is no longer of the squad stays where they are, and next time has their own.
            fixture.Alone(solo);
            fixture.Squad(head, joiner);

            Assert.AreSame(own, solo.Player.MapChannel);
            fixture.Leave(solo);
            Assert.AreNotSame(own, fixture.Into(solo));
        }

        [TestMethod]
        public void ThePartyManagersSquadIsTheSquad()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world);
            var leader = fixture.Player();
            var member = fixture.Player();
            var alone = fixture.Player();

            foreach (var client in new[] { leader, member, alone })
                typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, new GameAccountEntry { Id = 500 + client.Player.Id });

            // As the server has it, with no squad of the test's own in the way.
            fixture.Maps.SquadFor = new MapChannelManager(null, () => 0, (_, _, _) => { }, privateInstances: new PrivateMapInstanceService(),
                refreshStats: (_, _) => { }, assignPlayer: _ => { }, enterMapChannels: _ => { }).SquadFor;

            using var party = new GroupMissionCreditTests.PartyScope(leader, member);

            var squad = fixture.Maps.SquadFor(member);

            Assert.AreEqual(leader.Player.Id, squad.OwnerCharacterId);
            CollectionAssert.AreEquivalent(new[] { leader.Player.Id, member.Player.Id }, squad.Members.ToArray());

            var none = fixture.Maps.SquadFor(alone);

            Assert.AreEqual(alone.Player.Id, none.OwnerCharacterId);
            Assert.IsNull(none.Members);

            Assert.AreSame(fixture.Into(member), fixture.Into(leader));
            Assert.AreEqual(leader.Player.Id, leader.Player.MapChannel.SquadOwnerCharacterId);
        }

        #endregion

        #region Which maps

        [TestMethod]
        public void AMapThatIsNotOneOfThemIsEnteredAsBefore()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world);
            var client = fixture.Player();

            fixture.Maps.SquadMapPolicy = _ => false;

            Assert.IsFalse(fixture.Maps.IsSquadInstanceMap(Caves));
            Assert.IsTrue(fixture.Maps.EnterMap(client, Caves, Entrance, 0));
            Assert.AreSame(fixture.Own, client.PendingTransfer.DestinationMap);
            Assert.AreEqual(0, fixture.Maps.SquadInstancesOf().Count);
            Assert.IsNull(fixture.Maps.SquadInstanceFor(client, Caves, create: true));

            // Nor is one that runs in shared copies, or one that is not loaded.
            fixture.Maps.SquadMapPolicy = _ => true;
            Assert.IsTrue(fixture.Maps.IsSquadInstanceMap(Caves));
            Assert.IsFalse(fixture.Maps.IsSquadInstanceMap(4242), "not loaded");

            fixture.Maps.InstancePolicy = id => id == Caves ? new MapInstanceConfig { Capacity = 8, MaxCopies = 3 } : null;
            Assert.IsFalse(fixture.Maps.IsSquadInstanceMap(Caves), "a map of shared copies");
        }

        [TestMethod]
        public void TheOperationsAreTheMapsUnlessTheFileSaysOtherwise()
        {
            try
            {
                SquadInstancePolicies.Apply(null);

                Assert.HasCount(51, SquadInstancePolicies.DefaultMaps);
                Assert.HasCount(51, SquadInstancePolicies.DefaultMaps.Distinct().ToArray());
                Assert.IsTrue(SquadInstancePolicies.IsSquadMap(Caves));
                Assert.IsTrue(SquadInstancePolicies.IsSquadMap(2190), "Dybukkar");
                Assert.IsFalse(SquadInstancePolicies.IsSquadMap(1220), "a battlefield");
                Assert.IsFalse(SquadInstancePolicies.IsSquadMap(1985), "Bootcamp");
                Assert.IsFalse(SquadInstancePolicies.IsSquadMap(2374), "Edmund Range");
                Assert.AreEqual(-1, SquadInstancePolicies.Current.EmptyCloseSeconds, "kept until the weekly reset");
                Assert.AreEqual(45, SquadInstancePolicies.Current.RespawnMinutes, "a cleared spawn pool is back after 45 minutes");
                Assert.AreEqual(300, SquadInstancePolicies.Current.UnloadEmptySeconds, "out of memory after five minutes empty");
                Assert.IsTrue(SquadInstancePolicies.TryWeeklyReset(SquadInstancePolicies.Current, out var day, out var time));
                Assert.AreEqual(DayOfWeek.Tuesday, day);
                Assert.AreEqual(new TimeSpan(3, 0, 0), time);

                // A list in the file replaces the built-in one whole.
                SquadInstancePolicies.Apply(new SquadInstanceConfig { Maps = new List<uint> { 1220, 0 } });
                Assert.IsTrue(SquadInstancePolicies.IsSquadMap(1220));
                Assert.IsFalse(SquadInstancePolicies.IsSquadMap(Caves));
                Assert.IsFalse(SquadInstancePolicies.IsSquadMap(0));

                SquadInstancePolicies.Apply(new SquadInstanceConfig { Enabled = false });
                Assert.IsFalse(SquadInstancePolicies.IsSquadMap(Caves));
            }
            finally
            {
                SquadInstancePolicies.Apply(null);
            }

            Assert.IsNotNull(new RasaGame::Rasa.Config.Config().SquadInstances, "a file without the section has the defaults");
            Assert.IsNull(new RasaGame::Rasa.Config.Config().SquadInstances.Maps);
        }

        [TestMethod]
        public void EveryOperationIsAMapOfTheWorldAndNoBattlefield()
        {
            using var harness = BootcampRuntimeTestHarness.Create();

            var maps = harness.WorldContext.Set<MapInfoEntry>().AsNoTracking().ToDictionary(map => map.Id, map => map.MapName);
            var battlefields = new uint[] { 1220, 1148, 1244, 1454, 1497, 1304, 2047, 2051, 1759, 1764, 1761, 2028, 1734, 1911, 1993 };

            foreach (var id in SquadInstancePolicies.DefaultMaps)
            {
                Assert.IsTrue(maps.ContainsKey(id), $"{id} is a map of the world");
                Assert.IsFalse(battlefields.Contains(id), $"{id} {maps[id]}");
                StringAssert.StartsWith(maps[id], "adv_", $"{id}");
            }
        }

        #endregion

        #region Staying and closing

        [TestMethod]
        public void AnInstanceIsKeptWhenEverybodyLeavesUntilTheWeeklyReset()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world) { Clock = new DateTime(2026, 10, 3, 4, 0, 0) };    // a Saturday
            var leader = fixture.Player();
            var mate = fixture.Player();

            fixture.Squad(leader, mate);
            var instance = fixture.Into(leader);
            fixture.Into(mate);

            fixture.Maps.SquadInstanceWorker();
            Assert.AreEqual(new DateTime(2026, 10, 6, 3, 0, 0), fixture.Maps.NextWeeklyReset, "Tuesday at three in the morning");

            fixture.Leave(leader);
            fixture.Leave(mate);

            // Days go by with nobody in it.
            foreach (var later in new[] { new DateTime(2026, 10, 3, 5, 0, 0), new DateTime(2026, 10, 5, 23, 0, 0), new DateTime(2026, 10, 6, 2, 59, 59) })
            {
                fixture.Clock = later;
                fixture.Now += 24 * 3600 * 1000L;
                fixture.Maps.SquadInstanceWorker();

                Assert.AreSame(instance, fixture.Maps.FindByContextAndInstance(Caves, instance.InstanceId), $"{later}");
            }

            Assert.AreNotEqual(0, instance.EmptySince, "empty, and kept");

            // Its owner and their squad come back to the same one.
            Assert.AreSame(instance, fixture.Into(mate));
            Assert.AreSame(instance, fixture.Into(leader));
            Assert.AreEqual(1, fixture.Maps.SquadInstancesOf().Count);

            // Tuesday, with them in it: left alone. The Tuesday after, empty: closed, and the next is new.
            fixture.Clock = new DateTime(2026, 10, 6, 3, 0, 0);
            fixture.Maps.SquadInstanceWorker();
            Assert.AreEqual(1, fixture.Maps.SquadInstancesOf().Count);
            Assert.AreEqual(new DateTime(2026, 10, 13, 3, 0, 0), fixture.Maps.NextWeeklyReset);

            fixture.Leave(leader);
            fixture.Leave(mate);
            fixture.Clock = new DateTime(2026, 10, 13, 3, 0, 1);
            fixture.Now += SquadHold;
            fixture.Maps.SquadInstanceWorker();

            Assert.AreEqual(0, fixture.Maps.SquadInstancesOf().Count);
            Assert.IsNull(fixture.Maps.FindByContextAndInstance(Caves, instance.InstanceId));

            var next = fixture.Into(leader);

            Assert.AreNotSame(instance, next);
            Assert.AreNotEqual(instance.InstanceId, next.InstanceId);
        }

        [TestMethod]
        public void SetToAnInstanceClosesAsItEmptiesAndTheNextIsNew()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world) { Config = new SquadInstanceConfig { EmptyCloseSeconds = 0 } };
            var client = fixture.Player();
            var mate = fixture.Player();

            fixture.Squad(client, mate);
            var instance = fixture.Into(client);
            fixture.Into(mate);

            fixture.Now += 60000;
            fixture.Maps.SquadInstanceWorker();
            Assert.AreEqual(1, fixture.Maps.SquadInstancesOf(Caves).Count, "two in it");
            Assert.AreEqual(0, instance.EmptySince);

            fixture.Leave(client);
            fixture.Maps.SquadInstanceWorker();
            Assert.AreEqual(1, fixture.Maps.SquadInstancesOf(Caves).Count, "one in it");

            fixture.Leave(mate);
            Assert.IsFalse(instance.ClientList.Any());

            fixture.Maps.SquadInstanceWorker();

            Assert.AreEqual(0, fixture.Maps.SquadInstancesOf().Count, "closed as it emptied");
            Assert.IsNull(fixture.Maps.FindByContextAndInstance(Caves, instance.InstanceId));
            Assert.AreSame(fixture.Own, fixture.Maps.FindByContextId(Caves));
            Assert.IsFalse(fixture.Maps.CloseSquadInstance(instance), "once");

            // The next to go there has a new one.
            var next = fixture.Into(client);

            Assert.AreNotSame(instance, next);
            Assert.AreNotEqual(instance.InstanceId, next.InstanceId);
            Assert.AreEqual(client.Player.Id, next.SquadOwnerCharacterId);
        }

        [TestMethod]
        public void AnInstanceSomebodyIsInOrOnTheWayToIsNotClosed()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world);
            var client = fixture.Player();

            Assert.IsTrue(fixture.Maps.EnterMap(client, Caves, Entrance, 0));

            var instance = client.PendingTransfer.DestinationMap;

            // Past the moment it is held for, and still loading.
            fixture.Now += MapChannelManager.SquadHoldMs + 1000;
            fixture.Maps.SquadInstanceWorker();
            Assert.IsFalse(fixture.Maps.CloseSquadInstance(instance), "on the way");
            Assert.AreEqual(1, fixture.Maps.SquadInstancesOf().Count);

            fixture.Maps.MapLoaded(client);
            fixture.Now += 60000;
            fixture.Maps.SquadInstanceWorker();
            Assert.IsFalse(fixture.Maps.CloseSquadInstance(instance), "in it");
            Assert.AreEqual(0, fixture.Maps.ResetSquadInstances());

            // One just made for somebody is not closed in the moment before they are sent to it,
            // by the reset or by a server set to close the empty ones.
            var other = fixture.Player();
            var held = fixture.Maps.SquadInstanceFor(other, Caves, create: true);

            fixture.Config = new SquadInstanceConfig { EmptyCloseSeconds = 0 };
            Assert.AreEqual(0, fixture.Maps.PopulationOf(held));
            fixture.Maps.SquadInstanceWorker();
            Assert.AreEqual(0, fixture.Maps.ResetSquadInstances());
            Assert.AreEqual(2, fixture.Maps.SquadInstancesOf().Count, "held");

            fixture.Now += MapChannelManager.SquadHoldMs;
            fixture.Maps.SquadInstanceWorker();
            Assert.AreEqual(1, fixture.Maps.SquadInstancesOf().Count, "nobody came");

            // Neither the map's own channel nor a shared copy is a squad instance to close.
            Assert.IsFalse(fixture.Maps.CloseSquadInstance(fixture.Own));
            Assert.IsFalse(fixture.Maps.CloseSquadInstance(null));
        }

        [TestMethod]
        public void WithTimeGivenAnEmptyInstanceStandsThatLong()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world) { Config = new SquadInstanceConfig { EmptyCloseSeconds = 300, WeeklyReset = false } };
            var client = fixture.Player();
            var instance = fixture.Into(client);

            fixture.Now += 60000;
            fixture.Leave(client);

            fixture.Maps.SquadInstanceWorker();
            Assert.AreEqual(fixture.Now, instance.EmptySince);

            fixture.Now += 299000;
            fixture.Maps.SquadInstanceWorker();
            Assert.AreEqual(1, fixture.Maps.SquadInstancesOf().Count);

            // Back before its time is up: the same one, as they left it.
            Assert.AreSame(instance, fixture.Into(client));
            fixture.Now += 600000;
            fixture.Maps.SquadInstanceWorker();
            Assert.AreEqual(0, instance.EmptySince);

            fixture.Leave(client);
            fixture.Maps.SquadInstanceWorker();
            fixture.Now += 300000;
            fixture.Maps.SquadInstanceWorker();
            Assert.AreEqual(0, fixture.Maps.SquadInstancesOf().Count);
        }

        [TestMethod]
        public void TheWeeklyResetClosesEveryInstanceNobodyIsInAndLeavesTheRest()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world)
            {
                Config = new SquadInstanceConfig { EmptyCloseSeconds = 7 * 24 * 3600, WeeklyResetDay = "tuesday", WeeklyResetTime = "09:00" },
                Clock = new DateTime(2026, 10, 5, 12, 0, 0)     // a Monday
            };
            var stays = fixture.Player();
            var leaves = fixture.Player();
            var away = fixture.Player();

            var occupied = fixture.Into(stays);
            var emptied = fixture.Into(leaves);
            var other = fixture.Into(away);

            fixture.Now += 60000;
            fixture.Leave(leaves);
            fixture.Leave(away);

            fixture.Maps.SquadInstanceWorker();

            Assert.AreEqual(new DateTime(2026, 10, 6, 9, 0, 0), fixture.Maps.NextWeeklyReset, "the first pass sets the time");
            Assert.AreEqual(3, fixture.Maps.SquadInstancesOf().Count, "standing empty, with a week to go");

            fixture.Clock = new DateTime(2026, 10, 6, 8, 59, 59);
            fixture.Maps.SquadInstanceWorker();
            Assert.AreEqual(3, fixture.Maps.SquadInstancesOf().Count);

            fixture.Clock = new DateTime(2026, 10, 6, 9, 0, 0);
            fixture.Maps.SquadInstanceWorker();

            CollectionAssert.AreEqual(new[] { occupied }, fixture.Maps.SquadInstancesOf().ToArray(), "the one with a player in it is left");
            Assert.IsNull(fixture.Maps.FindByContextAndInstance(Caves, emptied.InstanceId));
            Assert.IsNull(fixture.Maps.FindByContextAndInstance(Caves, other.InstanceId));
            Assert.AreSame(occupied, stays.Player.MapChannel);
            Assert.AreEqual(new DateTime(2026, 10, 13, 9, 0, 0), fixture.Maps.NextWeeklyReset);

            // Once: the next pass of the same minute does nothing more.
            var again = fixture.Into(leaves);
            fixture.Leave(leaves);
            fixture.Maps.SquadInstanceWorker();
            Assert.IsNotNull(fixture.Maps.FindByContextAndInstance(Caves, again.InstanceId));

            // A day or time changed in the file counts from now; off, there is none.
            fixture.Config = new SquadInstanceConfig { EmptyCloseSeconds = 7 * 24 * 3600, WeeklyResetDay = "Friday", WeeklyResetTime = "18:30" };
            fixture.Maps.SquadInstanceWorker();
            Assert.AreEqual(new DateTime(2026, 10, 9, 18, 30, 0), fixture.Maps.NextWeeklyReset);

            fixture.Config = new SquadInstanceConfig { EmptyCloseSeconds = 7 * 24 * 3600, WeeklyReset = false };
            fixture.Maps.SquadInstanceWorker();
            Assert.IsNull(fixture.Maps.NextWeeklyReset);

            fixture.Config = new SquadInstanceConfig { EmptyCloseSeconds = 7 * 24 * 3600, WeeklyResetDay = "Tuesdy" };
            fixture.Maps.SquadInstanceWorker();
            Assert.IsNull(fixture.Maps.NextWeeklyReset, "a day that does not read");
        }

        [TestMethod]
        public void TheNextResetIsTheFirstSuchDayAndTimeAfterNow()
        {
            var tuesday = DayOfWeek.Tuesday;
            var nine = new TimeSpan(9, 0, 0);

            Assert.AreEqual(new DateTime(2026, 10, 6, 9, 0, 0), SquadInstancePolicies.NextReset(new DateTime(2026, 10, 3, 4, 0, 0), tuesday, nine), "from a Saturday");
            Assert.AreEqual(new DateTime(2026, 10, 6, 9, 0, 0), SquadInstancePolicies.NextReset(new DateTime(2026, 10, 6, 8, 59, 59), tuesday, nine), "that morning");
            Assert.AreEqual(new DateTime(2026, 10, 13, 9, 0, 0), SquadInstancePolicies.NextReset(new DateTime(2026, 10, 6, 9, 0, 0), tuesday, nine), "on the dot: the next");
            Assert.AreEqual(new DateTime(2026, 10, 13, 9, 0, 0), SquadInstancePolicies.NextReset(new DateTime(2026, 10, 6, 23, 0, 0), tuesday, nine));

            foreach (var (day, time, reads) in new[]
                     {
                         ("Tuesday", "09:00", true), ("tuesday", "9:05", true), (" Sunday ", "23:59", true),
                         ("Tuesdy", "09:00", false), ("Tuesday", "9", false), ("Tuesday", "24:00", false), ("7", "09:00", false),
                         (null, "09:00", false), ("Tuesday", null, false)
                     })
                Assert.AreEqual(reads, SquadInstancePolicies.TryWeeklyReset(new SquadInstanceConfig { WeeklyResetDay = day, WeeklyResetTime = time }, out _, out _), $"{day} {time}");
        }

        #endregion

        #region Dying

        [TestMethod]
        public void APlayerWhoDiesInAnInstanceGetsUpAtItsEntrance()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world);
            var client = PlayerDeathTests.Player(world, 0, 0);
            var instance = fixture.Into(client);

            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 200, 200, 0, 0, 0);
            client.Player.PlaceAt(Deep);
            client.Player.InsideMapLinks.Clear();
            WorldTestContext.Drain(client);

            client.Player.Attributes[Attributes.Health].Current = 0;
            PlayerDeath.AtZero(instance, client.Player, null);

            Assert.AreEqual(CharacterState.Dead, client.Player.State);

            // The hospital window: the one way back, the entrance, under the map's hospital's name.
            var offered = Sent<PlayerDeadPacket>(client).Single().Graveyards.Single();

            Assert.AreEqual(Hospitals.OnMap(Caves).Single().GraveyardId, offered.Id);
            Assert.AreEqual(Entrance, offered.Position);

            PlayerDeath.ReviveMe(client, (int)offered.Id);

            Assert.AreNotEqual(CharacterState.Dead, client.Player.State);
            Assert.AreEqual(Entrance, client.Player.Position, "not the hospital, which is at 700");
            Assert.AreSame(instance, client.Player.MapChannel, "still in the instance");
            Assert.AreEqual(1000, client.Player.Attributes[Attributes.Health].Current);
            Assert.AreEqual(200, client.Player.Attributes[Attributes.Armor].Current);
            Assert.IsTrue(Sent<RevivedPacket>(client).Any());

            // They stand in the way out as one who has just arrived does: it does not take them.
            CollectionAssert.Contains(client.Player.InsideMapLinks.ToArray(), Fixture.ExitId);
        }

        [TestMethod]
        public void TheEntranceIsTheDoorTheyCameInByOrTheMapsFirst()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world);
            var byDoor = PlayerDeathTests.Player(world, 0, 0);
            var bySummon = PlayerDeathTests.Player(world, 0, 0);
            var side = new Vector3(-120, 5, 60);

            // By a side door: that one.
            Assert.IsTrue(fixture.Maps.EnterMap(byDoor, Caves, side, 2f));
            fixture.Maps.MapLoaded(byDoor);
            Assert.AreEqual((side, 2f), InstanceEntrances.Of(byDoor.Player.MapChannel, byDoor.Player));

            // By a game master's teleport: the map's first door.
            Assert.IsTrue(fixture.Maps.ChangeMap(bySummon, Caves, Deep, 0));
            fixture.Maps.MapLoaded(bySummon);
            Assert.IsNull(bySummon.Player.InstanceEntrance);
            Assert.AreEqual((Entrance, 1.25f), InstanceEntrances.Of(bySummon.Player.MapChannel, bySummon.Player));

            bySummon.Player.Attributes[Attributes.Health].Current = 0;
            PlayerDeath.AtZero(bySummon.Player.MapChannel, bySummon.Player, null);
            PlayerDeath.ReviveMe(bySummon, null);
            Assert.AreEqual(Entrance, bySummon.Player.Position);

            // Off an instance there is no entrance, and the hospitals are as they were.
            Assert.IsNull(InstanceEntrances.Of(world.Map, byDoor.Player));
            Assert.IsNull(InstanceEntrances.Of(fixture.Own, byDoor.Player), "the map's own channel");
            Assert.IsNull(InstanceEntrances.Of(null, byDoor.Player));

            // A map with no hospital names none, and the entrance is where they go all the same.
            Hospitals.Source = () => new (uint, uint, Vector3, string)[0];
            Hospitals.Reset();

            byDoor.Player.PlaceAt(Deep);
            byDoor.Player.Attributes[Attributes.Health].Current = 0;
            WorldTestContext.Drain(byDoor);
            PlayerDeath.AtZero(byDoor.Player.MapChannel, byDoor.Player, null);

            Assert.AreEqual(0, Sent<PlayerDeadPacket>(byDoor).Single().Graveyards.Count);
            PlayerDeath.ReviveMe(byDoor, null);
            Assert.AreEqual(side, byDoor.Player.Position);

            // A map no door leads into has no entrance: where they fell, as on any map with no hospital.
            InstanceEntrances.MapLinks = () => new MapLink[0];
            bySummon.Player.PlaceAt(Deep);
            bySummon.Player.Attributes[Attributes.Health].Current = 0;
            PlayerDeath.AtZero(bySummon.Player.MapChannel, bySummon.Player, null);
            PlayerDeath.ReviveMe(bySummon, null);
            Assert.AreEqual(Deep, bySummon.Player.Position);
        }

        #endregion

        #region Coming and going

        [TestMethod]
        public void AGameMastersTeleportGoesIntoTheirInstanceAndAboutTheOneTheyAreIn()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world);
            var owner = fixture.Player();
            var master = fixture.Player();
            var instance = fixture.Into(owner);

            // .teleport to the map: their own, not the map's own channel.
            Assert.IsTrue(fixture.Maps.ChangeMap(master, Caves, Deep, 0));

            var theirs = master.PendingTransfer.DestinationMap;

            Assert.IsTrue(theirs.IsSquadInstance);
            Assert.AreNotSame(instance, theirs);
            Assert.AreEqual(master.Player.Id, theirs.SquadOwnerCharacterId);
            fixture.Maps.MapLoaded(master);

            // Summoned into another's, and moving about in it: they stay in that one.
            fixture.Leave(master);
            Assert.IsTrue(fixture.Maps.Send(master, instance, Deep, 0));
            fixture.Maps.MapLoaded(master);
            Assert.AreSame(instance, master.Player.MapChannel);

            Assert.IsTrue(fixture.Maps.ChangeMap(master, Caves, Entrance, 0));
            Assert.AreSame(instance, master.PendingTransfer.DestinationMap);
        }

        [TestMethod]
        public void AWaypointOfTheMapIsTravelledToInsideTheInstance()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world);
            var client = fixture.Player();
            var objects = new DynamicObjectManager(null, fixture.Maps, () => fixture.Now, (_, _, _) => { }, _ => { });

            // The map's waypoints stand on its own channel, as they are loaded.
            foreach (var (id, position) in new[] { (10u, Entrance), (20u, Deep) })
            {
                var waypoint = WaypointTravelTests.AddWaypoint(objects, fixture.Own, id, position);

                CellManager.Instance.AddToWorld(fixture.Own, waypoint);
                Assert.AreSame(fixture.Own, waypoint.RuntimeMapChannel);
            }

            var instance = fixture.Into(client);

            client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, 20, (byte)WaypointType.Waypoint));
            client.Player.PlaceAt(Entrance);
            WorldTestContext.Drain(client);

            // The window lists the instance they are in, and the pick names it.
            Assert.AreEqual(instance.InstanceId, objects.CreateListOfWaypoints(client, WaypointType.Waypoint)[Caves].MapInstanceList.Single().MapInstanceId);

            objects.SelectWaypoint(client, new Rasa.Packets.MapChannel.Client.SelectWaypointPacket { MapInstanceId = instance.InstanceId, WaypointId = 20 });

            Assert.IsFalse(Sent<Rasa.Packets.MapChannel.Client.TeleportFailedPacket>(client).Any(), "not refused as another instance's");
            Assert.IsNotNull(client.PendingTransfer);
            Assert.AreSame(instance, client.PendingTransfer.DestinationMap);
            Assert.AreEqual(Deep + new Vector3(0, 1, 0), client.Player.Position);
            Assert.AreSame(instance, client.Player.MapChannel);

            fixture.Own.MapCellInfo.Cells.Clear();
        }

        [TestMethod]
        public void ASummonBringsThePlayerIntoTheInstanceTheSummonerStandsIn()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world);
            var summoner = fixture.Player();
            var traveller = fixture.Player();
            var instance = fixture.Into(summoner);

            summoner.Player.Position = new Vector3(120, 4, -60);

            using (fixture.AsTheServers())
                typeof(SummonManager).GetMethod("MoveTo", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { traveller, summoner });

            Assert.AreSame(instance, traveller.PendingTransfer.DestinationMap, "not an instance of the traveller's own");
            Assert.AreEqual(new Vector3(120, 4, -60), traveller.PendingTransfer.DestinationPosition);
        }

        [TestMethod]
        public void ALoginGoesBackIntoTheInstanceItLeftOrOutsideItsDoor()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world);
            var client = fixture.Player();
            var mate = fixture.Player();

            fixture.Squad(client, mate);
            var instance = fixture.Into(client);
            fixture.Into(mate);
            client.Player.Position = Deep;

            // Their squad is still in it: back where they were.
            fixture.Relog(client);
            fixture.Now += 60000;
            fixture.Maps.SquadInstanceWorker();
            fixture.Maps.PlaceLogin(client);

            Assert.AreSame(instance, client.Player.MapChannel);
            Assert.AreEqual(Caves, client.Player.MapContextId);
            Assert.AreEqual(Deep, client.Player.Position);
            Assert.IsNull(client.ArrivalNotice);

            client.State = ClientState.CharacterSelection;
            fixture.Maps.PassClientToMapInstance(client);
            Assert.AreEqual(instance.InstanceId, Sent<WonkavatePacket>(client).Single().MapInstanceId);
            Assert.AreEqual(2, fixture.Maps.PopulationOf(instance));

            // Alone in it when they left: it is kept, and they are back in it.
            var solo = fixture.Player();
            var gone = fixture.Into(solo);

            solo.Player.Position = Deep;
            fixture.Relog(solo);
            fixture.Now += 60000;
            fixture.Maps.SquadInstanceWorker();
            fixture.Maps.PlaceLogin(solo);

            Assert.AreSame(gone, solo.Player.MapChannel);
            Assert.AreEqual(Deep, solo.Player.Position);
            Assert.IsNull(solo.ArrivalNotice);

            // The weekly reset came while they were away: they are outside its door.
            fixture.Now += 60000;
            Assert.AreEqual(1, fixture.Maps.ResetSquadInstances());
            Assert.IsNull(fixture.Maps.FindByContextAndInstance(Caves, gone.InstanceId));

            fixture.Maps.PlaceLogin(solo);

            Assert.AreSame(world.Map, solo.Player.MapChannel);
            Assert.AreEqual(world.Map.MapInfo.MapContextId, solo.Player.MapContextId);
            Assert.AreEqual(Outside, solo.Player.Position);
            StringAssert.Contains(solo.ArrivalNotice, "has closed");
            StringAssert.Contains(solo.ArrivalNotice, "outside its door");
            Assert.AreEqual(1, fixture.Maps.SquadInstancesOf().Count, "none made for them");

            // A map with no door in or out: an instance of their own, where they stood.
            var stuck = fixture.Player();
            var before = fixture.Into(stuck);

            stuck.Player.Position = Deep;
            fixture.Relog(stuck);
            fixture.Now += 60000;
            Assert.AreEqual(1, fixture.Maps.ResetSquadInstances());
            fixture.Maps.MapLinks = () => new MapLink[0];
            fixture.Maps.PlaceLogin(stuck);

            Assert.IsTrue(stuck.Player.MapChannel.IsSquadInstance);
            Assert.AreNotSame(before, stuck.Player.MapChannel);
            Assert.AreEqual(stuck.Player.Id, stuck.Player.MapChannel.SquadOwnerCharacterId);
            Assert.AreEqual(Deep, stuck.Player.Position);
            Assert.IsNull(stuck.ArrivalNotice);
        }

        [TestMethod]
        public void LeavingTheMapOrTheWorldNotesTheInstanceLeft()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world);
            var client = fixture.Player();
            var mate = fixture.Player();

            fixture.Squad(client, mate);
            var instance = fixture.Into(client);
            fixture.Into(mate);

            using (fixture.AsTheServers())
                ManifestationManager.Instance.RemovePlayerCharacter(client);

            instance.ClientList.Remove(client);
            client.Player.MapChannel = fixture.Own;
            fixture.Maps.PlaceLogin(client);

            Assert.AreSame(instance, client.Player.MapChannel);
        }

        [TestMethod]
        public void TheInstanceCommandListsTheSquadInstancesAndRunsTheReset()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world) { Config = new SquadInstanceConfig { EmptyCloseSeconds = 3600 }, Clock = new DateTime(2026, 10, 5, 12, 0, 0) };
            var master = fixture.Player();
            var other = fixture.Player();

            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(master, new GameAccountEntry { Level = (byte)GmLevel.Admin });

            var commands = new ChatCommandsManager(null);
            commands.RegisterChatCommands();

            List<string> Say(string command)
            {
                WorldTestContext.Drain(master);
                commands.ProcessCommand(master, command);
                return Packets(master).OfType<SystemMessagePacket>().Select(message => message.TextMessage).ToList();
            }

            var mine = fixture.Into(master);
            var theirs = fixture.Into(other);

            fixture.Leave(other);
            fixture.Now += 60000;
            fixture.Maps.SquadInstanceWorker();

            using (fixture.AsTheServers())
            {
                var here = Say(".instance");

                Assert.AreEqual(3, here.Count);
                StringAssert.Contains(here[0], $"Map {Caves} is entered as a squad's instance: 2 open");
                StringAssert.Contains(here[1], $"instance {mine.InstanceId}: owner character {master.Player.Id}, 1 player(s) (you are here); 0 of 0 spawn pool(s) dead");
                StringAssert.Contains(here[2], $"instance {theirs.InstanceId}: owner character {other.Player.Id}, 0 player(s); 0 of 0 spawn pool(s) dead");

                var all = Say(".instance squads");

                Assert.AreEqual(3, all.Count);
                StringAssert.Contains(all[0], "0 squad instance(s) saved, 2 in memory. The weekly reset is Tuesday 2026-10-06 03:00");
                StringAssert.Contains(all[1], $"map {Caves} instance {mine.InstanceId}");

                StringAssert.Contains(Say(".instance reset").Single(), "Closed 1 squad instance(s); 1 left with players in them");
                CollectionAssert.AreEqual(new[] { mine }, fixture.Maps.SquadInstancesOf().ToArray());
                StringAssert.Contains(Say(".instance reset").Single(), "Closed 0 squad instance(s); 1 left");
                StringAssert.Contains(Say(".instance nonsense").Single(), ".instance squads | .instance reset");
            }
        }

        #endregion

        private static List<PythonPacket> Packets(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet).ToList();

        private static List<T> Sent<T>(Client client) where T : PythonPacket => Packets(client).OfType<T>().ToList();

        /// <summary>
        /// A world of two maps: the fixture's, where the players stand, and the Caves of Donn,
        /// entered as a squad's instance by a door at <see cref="Entrance"/>. Its own clocks and
        /// its own squads. Nothing is saved unless it is given a store (SquadInstanceStateTests).
        /// </summary>
        internal sealed class Fixture
        {
            internal const uint DoorId = 14;
            internal const uint ExitId = 114;

            private readonly WorldTestContext _world;
            private readonly Dictionary<uint, MapChannelManager.SquadMembership> _squads = new Dictionary<uint, MapChannelManager.SquadMembership>();

            internal long Now = 1000;
            internal DateTime Clock = new DateTime(2026, 10, 3, 4, 0, 0);
            internal DateTime Utc = new DateTime(2026, 10, 3, 9, 0, 0, DateTimeKind.Utc);
            internal SquadInstanceConfig Config = new SquadInstanceConfig();
            internal MapChannelManager Maps { get; }
            internal MapChannel Own { get; }

            internal Fixture(WorldTestContext world)
            {
                _world = world;
                Own = new MapChannel
                {
                    MapInfo = new MapInfo(Caves, "adv_foreas_concordia_wilderness_cavesofdonn02", 24, 0),
                    ClientList = new List<Client>(),
                    PlayerLimit = 128
                };
                Maps = new MapChannelManager(null, () => Now, (_, _, _) => { }, privateInstances: new PrivateMapInstanceService(),
                    refreshStats: (_, _) => { }, assignPlayer: _ => { }, enterMapChannels: _ => { })
                {
                    InstancePolicy = _ => null,
                    SquadMapPolicy = id => id == Caves,
                    SquadConfig = () => Config,
                    SquadFor = client => _squads.TryGetValue(client.Player.Id, out var squad) ? squad : new MapChannelManager.SquadMembership(client.Player.Id, null),
                    WallClock = () => Clock,
                    UtcNow = () => Utc
                };
                Maps.MapChannelArray.Add(world.Map.MapInfo.MapContextId, world.Map);
                Maps.MapChannelArray.Add(Caves, Own);

                var links = new[]
                {
                    new MapLink { Id = DoorId, MapContextId = world.Map.MapInfo.MapContextId, Position = Outside, Radius = 3, DestMapContextId = Caves, DestPosition = Entrance, DestRotation = 1.25f, Enabled = true },
                    new MapLink { Id = ExitId, MapContextId = Caves, Position = Entrance, Radius = 4, DestMapContextId = world.Map.MapInfo.MapContextId, DestPosition = Outside, DestRotation = 0.5f, Enabled = true }
                };

                Maps.MapLinks = () => links;
                InstanceEntrances.MapLinks = () => links;
            }

            /// <summary>A player in the world on the fixture's map, with nothing waiting to be read.</summary>
            internal Client Player()
            {
                var client = _world.CreateClient();

                CellManager.Instance.AddToWorld(client);
                WorldTestContext.Drain(client);

                return client;
            }

            /// <summary>Makes these a squad, the first its leader.</summary>
            internal void Squad(Client leader, params Client[] members)
            {
                var ids = members.Append(leader).Select(client => client.Player.Id).ToHashSet();

                foreach (var id in ids)
                    _squads[id] = new MapChannelManager.SquadMembership(leader.Player.Id, ids);
            }

            /// <summary>Takes a player out of whatever squad they were in.</summary>
            internal void Alone(Client client) => _squads.Remove(client.Player.Id);

            /// <summary>Through the door and arrived: the instance they are in.</summary>
            internal MapChannel Into(Client client)
            {
                Assert.IsTrue(Maps.EnterMap(client, Caves, Entrance, 1.25f), "through the door");
                Maps.MapLoaded(client);
                WorldTestContext.Drain(client);

                return client.Player.MapChannel;
            }

            /// <summary>Out by the way out and arrived on the fixture's map.</summary>
            internal void Leave(Client client)
            {
                Assert.IsTrue(Maps.EnterMap(client, _world.Map.MapInfo.MapContextId, Outside, 0), "out of the door");
                Maps.MapLoaded(client);
                WorldTestContext.Drain(client);
            }

            /// <summary>
            /// Takes a player out of the world and brings them to where a login stands before it
            /// is placed: on the own channel of the map they were on, and on no list.
            /// </summary>
            internal void Relog(Client client)
            {
                var map = client.Player.MapChannel;

                Maps.RememberCopy(client);
                map.ClientList.Remove(client);
                client.Player.MapChannel = Maps.MapChannelArray[map.MapInfo.MapContextId];
                WorldTestContext.Drain(client);
            }

            /// <summary>The fixture's manager in the server's place, until disposed.</summary>
            internal IDisposable AsTheServers() => new Singleton(Maps);

            private sealed class Singleton : IDisposable
            {
                private static readonly FieldInfo Field = typeof(MapChannelManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
                private readonly object _previous = Field.GetValue(null);

                internal Singleton(MapChannelManager maps) => Field.SetValue(null, maps);

                public void Dispose() => Field.SetValue(null, _previous);
            }
        }
    }
}
