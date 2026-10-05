extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Config;
    using Rasa.Context.Char;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Repositories.Char.SquadInstance;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;
    using Fixture = SquadInstanceTests.Fixture;

    /// <summary>
    /// What a squad instance keeps (MapChannelManager.SquadInstances, SquadInstanceState): a
    /// spawn pool whose creatures are all dead comes back 45 minutes after the last of them
    /// died, by the clock on the wall; and the instance, its dead pools and who last went into
    /// it are saved to the character database, so that it lasts through a restart, is taken out
    /// of memory when it stands empty, and is closed by the weekly reset wherever it is.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class SquadInstanceStateTests
    {
        private const uint Caves = SquadInstanceTests.Caves;
        private const uint ThraxSoldier = 3;        // Bane_Thrax_Soldier
        private const uint PoolA = 9101, PoolB = 9102;
        private const long OwnRespawnMs = 60_000;   // the pools' own respawn time: a minute
        private const long Minute = 60_000;

        private readonly List<Fixture> _fixtures = new List<Fixture>();
        private Func<IEnumerable<MapLink>> _links;
        private string _directory;
        private string _database;

        [TestInitialize]
        public void MigrateACharacterDatabase()
        {
            _links = InstanceEntrances.MapLinks;
            _directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _database = Path.Combine(_directory, "database");

            using var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteCharContext), _database);
            context.Database.Migrate();
        }

        [TestCleanup]
        public void CloseTheInstancesAndDeleteTheDatabase()
        {
            // The creatures of the instances these tests made go with them.
            foreach (var fixture in _fixtures)
            {
                fixture.Maps.SquadStore = null;
                fixture.Now += 3600_000;

                foreach (var map in fixture.Maps.SquadInstancesOf())
                {
                    map.ClientList.Clear();
                    map.QueuedClients.Clear();
                    map.Arriving.Clear();
                    fixture.Maps.CloseSquadInstance(map);
                }
            }

            InstanceEntrances.MapLinks = _links;
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Directory.Delete(_directory, true);
        }

        #region The respawn

        [TestMethod]
        public void AClearedPoolComesBackFortyFiveMinutesAfterItsLastCreatureDiedAndNotBefore()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            using var world = new WorldTestContext();
            var fixture = World(harness, world, saved: false);
            var instance = fixture.Into(fixture.Player());
            var (a, b) = (Pool(instance, PoolA), Pool(instance, PoolB));

            Assert.IsNotNull(instance.SquadState);
            Spawns(instance, 0);
            Assert.AreEqual(2, a.AliveCreatures);
            Assert.AreEqual(1, b.AliveCreatures);
            Assert.AreEqual(0, a.ClearedAtUtcMs);

            // One of two dead: the pool is not cleared, and is not topped up.
            Kill(instance, a, 1);
            Assert.AreEqual(0, a.ClearedAtUtcMs);
            fixture.Utc = fixture.Utc.AddHours(2);
            Spawns(instance, OwnRespawnMs);
            Assert.AreEqual(1, a.AliveCreatures);

            // The last of it dead: it is dead from this moment.
            Kill(instance, a);
            Assert.AreEqual(0, a.AliveCreatures);
            Assert.AreEqual(Ms(fixture.Utc), a.ClearedAtUtcMs);
            Assert.AreEqual(0, b.ClearedAtUtcMs, "the other pool is not");

            // Its own respawn time, a minute, is not what brings it back.
            Spawns(instance, OwnRespawnMs);
            fixture.Utc = fixture.Utc.AddMinutes(44).AddSeconds(59);
            Spawns(instance, 44 * Minute + 59_000);
            Assert.AreEqual(0, a.AliveCreatures, "not before the 45 minutes are up");
            Assert.AreEqual(0, a.UpdateTimer);
            Assert.AreEqual(1, b.AliveCreatures);

            fixture.Utc = fixture.Utc.AddSeconds(1);
            Spawns(instance, 1000);
            Assert.AreEqual(2, a.AliveCreatures, "45 minutes after the last of it died");
            Assert.AreEqual(0, a.ClearedAtUtcMs);
            Assert.IsTrue(a.HasSpawned);

            // The file's time, not 45: and none, a pool that stays dead.
            fixture.Config.RespawnMinutes = 10;
            Kill(instance, a);
            fixture.Utc = fixture.Utc.AddMinutes(10);
            Spawns(instance, 0);
            Assert.AreEqual(2, a.AliveCreatures, "worker passes or none, the clock on the wall tells it");

            fixture.Config.RespawnMinutes = 0;
            Kill(instance, a);
            fixture.Utc = fixture.Utc.AddDays(6);
            Spawns(instance, 6 * 24 * 60 * Minute);
            Assert.AreEqual(0, a.AliveCreatures, "no respawn time: dead until the instance is closed");
        }

        [TestMethod]
        public void APoolOfAChannelThatIsNoSquadInstanceRespawnsOnItsOwnTime()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            using var world = new WorldTestContext();
            var fixture = World(harness, world, saved: false);
            var a = Pool(fixture.Own, PoolA);

            Assert.IsNull(fixture.Own.SquadState);
            Spawns(fixture.Own, 0);
            Kill(fixture.Own, a);
            Assert.AreEqual(0, a.ClearedAtUtcMs, "nothing is noted of it");

            Spawns(fixture.Own, OwnRespawnMs - 1);
            Assert.AreEqual(0, a.AliveCreatures);
            Spawns(fixture.Own, 1);
            Assert.AreEqual(2, a.AliveCreatures);

            foreach (var creature in Creatures(fixture.Own).ToList())
                CellManager.Instance.RemoveCreatureFromWorld(fixture.Own, creature);
        }

        [TestMethod]
        public void AMapResetBringsEveryPoolBackAtOnceAndForgetsThatTheyWereDead()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            using var world = new WorldTestContext();
            var fixture = World(harness, world, saved: true);
            var instance = fixture.Into(fixture.Player());
            var a = Pool(instance, PoolA);

            Spawns(instance, 0);
            Kill(instance, a);
            Assert.HasCount(1, Rows(r => r.GetPools(instance.SquadState.DbId)));

            MapReset.Request(instance, null);
            MapReset.Worker(instance);

            Assert.AreEqual(0, a.ClearedAtUtcMs);
            Assert.HasCount(0, Rows(r => r.GetPools(instance.SquadState.DbId)));

            Spawns(instance, 0);
            Assert.AreEqual(2, a.AliveCreatures);
        }

        #endregion

        #region Saved

        [TestMethod]
        public void TheInstanceItsDeadPoolsAndWhoWentIntoItAreSavedAsTheyChange()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            using var world = new WorldTestContext();
            var fixture = World(harness, world, saved: true);
            var leader = fixture.Player();
            var mate = fixture.Player();
            var stranger = fixture.Player();

            fixture.Squad(leader, mate);

            // The member goes in first: the instance is the leader's all the same.
            var instance = fixture.Into(mate);
            var row = Rows(r => r.GetAll()).Single();

            Assert.AreEqual(row.Id, instance.SquadState.DbId);
            Assert.AreEqual(Caves, row.MapContextId);
            Assert.AreEqual(leader.Player.Id, row.OwnerCharacterId);
            Assert.AreEqual(Ms(fixture.Utc), row.CreatedAt);
            Assert.AreEqual(row.Id, Rows(r => r.GetVisitorInstance(mate.Player.Id)));
            Assert.AreEqual(0u, Rows(r => r.GetVisitorInstance(leader.Player.Id)), "not in it yet");

            Assert.AreSame(instance, fixture.Into(leader));
            Assert.HasCount(1, Rows(r => r.GetAll()), "one row an instance");
            Assert.AreEqual(row.Id, Rows(r => r.GetVisitorInstance(leader.Player.Id)));

            var theirs = fixture.Into(stranger);

            Assert.HasCount(2, Rows(r => r.GetAll()));
            Assert.AreEqual(theirs.SquadState.DbId, Rows(r => r.GetVisitorInstance(stranger.Player.Id)));
            Assert.AreNotEqual(row.Id, theirs.SquadState.DbId);

            // A pool is saved when the last of it has died, with when; and no longer once it is back.
            var a = Pool(instance, PoolA);

            Spawns(instance, 0);
            Kill(instance, a, 1);
            Assert.HasCount(0, Rows(r => r.GetPools(row.Id)), "some of it dead is not saved");

            fixture.Utc = fixture.Utc.AddMinutes(7);
            Kill(instance, a);

            var dead = Rows(r => r.GetPools(row.Id));

            Assert.HasCount(1, dead);
            Assert.AreEqual(Ms(fixture.Utc), dead[PoolA]);
            Assert.HasCount(0, Rows(r => r.GetPools(theirs.SquadState.DbId)), "the pool of that instance only");

            fixture.Utc = fixture.Utc.AddMinutes(45);
            Spawns(instance, 0);
            Assert.AreEqual(2, a.AliveCreatures);
            Assert.HasCount(0, Rows(r => r.GetPools(row.Id)));

            // Nor when its last creature is put back on its feet (a Technician's Jumpstart).
            var b = Pool(instance, PoolB);
            var soldier = Creatures(instance).Single(creature => ReferenceEquals(creature.SpawnPool, b));

            Kill(instance, b);
            Assert.AreEqual(Ms(fixture.Utc), Rows(r => r.GetPools(row.Id))[PoolB]);

            CreatureSupport.Revive(instance, soldier, 500, null);
            Assert.AreEqual(1, b.AliveCreatures);
            Assert.AreEqual(0, b.ClearedAtUtcMs);
            Assert.HasCount(0, Rows(r => r.GetPools(row.Id)));
        }

        [TestMethod]
        public void AnInstanceLastsThroughARestart()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            using var world = new WorldTestContext();
            var before = World(harness, world, saved: true);
            var leader = before.Player();
            var mate = before.Player();

            before.Squad(leader, mate);

            var instance = before.Into(leader);
            var id = instance.SquadState.DbId;
            var made = Ms(before.Utc);

            before.Into(mate);
            Spawns(instance, 0);
            before.Utc = before.Utc.AddMinutes(5);
            Kill(instance, Pool(instance, PoolA));
            Kill(instance, Pool(instance, PoolB), 0);

            var died = Ms(before.Utc);

            // The leader walks out; the member is in it when the server stops, twenty minutes on.
            before.Leave(leader);
            before.Utc = before.Utc.AddMinutes(20);

            var after = World(harness, world, saved: true, from: before);

            after.Squad(leader, mate);
            Assert.HasCount(0, after.Maps.SquadInstancesOf(), "nothing is in memory until somebody goes to it");
            Assert.HasCount(1, after.Maps.SavedSquadInstances());

            // The member enters the world on the map: back into the instance, loaded from its rows.
            mate.Player.MapChannel = after.Own;
            after.Maps.PlaceLogin(mate);

            var loaded = mate.Player.MapChannel;

            Assert.IsTrue(loaded.IsSquadInstance);
            Assert.AreNotSame(instance, loaded);
            Assert.AreEqual(id, loaded.SquadState.DbId);
            Assert.AreEqual(made, loaded.SquadState.CreatedAtUtcMs);
            Assert.AreEqual(leader.Player.Id, loaded.SquadOwnerCharacterId);
            Assert.IsNull(mate.ArrivalNotice);
            Assert.AreEqual(Caves, mate.Player.MapContextId);

            // And the leader comes in by the door to the same one.
            Assert.AreSame(loaded, after.Into(leader));
            Assert.HasCount(1, after.Maps.SavedSquadInstances());

            // What was dead is dead, since when it died; what was alive is there whole.
            var (a, b) = (Pool(loaded, PoolA), Pool(loaded, PoolB));

            Assert.AreEqual(died, a.ClearedAtUtcMs);
            Assert.AreEqual(0, b.ClearedAtUtcMs);

            Spawns(loaded, OwnRespawnMs);
            Assert.AreEqual(0, a.AliveCreatures);
            Assert.AreEqual(1, b.AliveCreatures);

            // The 45 minutes ran while the server was down: 20 gone, 25 to go.
            after.Utc = after.Utc.AddMinutes(24).AddSeconds(59);
            Spawns(loaded, OwnRespawnMs);
            Assert.AreEqual(0, a.AliveCreatures);

            after.Utc = after.Utc.AddSeconds(1);
            Spawns(loaded, 0);
            Assert.AreEqual(2, a.AliveCreatures);
            Assert.HasCount(0, Rows(r => r.GetPools(id)));
        }

        [TestMethod]
        public void ALoginWhoseInstanceIsGoneIsPutOutsideAndOneInNoInstanceGoesIntoTheirOwn()
        {
            using var world = new WorldTestContext();
            var before = World(null, world, saved: true);
            var leader = before.Player();
            var mate = before.Player();

            before.Squad(leader, mate);
            var instance = before.Into(leader);
            before.Into(mate);

            // The server stops with both in it; the weekly reset comes round while it is down.
            var after = World(null, world, saved: true, from: before);

            after.Clock = after.Clock.AddDays(7);
            after.Utc = after.Utc.AddDays(7);
            after.Maps.SquadInstanceWorker();
            Assert.HasCount(0, after.Maps.SavedSquadInstances());
            Assert.AreEqual(0u, Rows(r => r.GetVisitorInstance(mate.Player.Id)), "who was in it went with it");

            mate.Player.MapChannel = after.Own;
            after.Maps.PlaceLogin(mate);

            Assert.AreSame(world.Map, mate.Player.MapChannel);
            Assert.AreEqual(SquadInstanceTests.Outside, mate.Player.Position);
            StringAssert.Contains(mate.ArrivalNotice, "has closed");
            Assert.HasCount(0, after.Maps.SavedSquadInstances(), "none made for them");

            // A character whose own instance is saved, and who was last in none: into their own.
            var third = World(null, world, saved: true, from: after);
            var solo = third.Player();
            var own = third.Into(solo);
            var id = own.SquadState.DbId;
            var fourth = World(null, world, saved: true, from: third);

            Rows(r => { r.SetVisitorInstance(solo.Player.Id, 0); return 0; });
            solo.Player.MapChannel = fourth.Own;
            fourth.Maps.PlaceLogin(solo);

            Assert.IsTrue(solo.Player.MapChannel.IsSquadInstance);
            Assert.AreEqual(id, solo.Player.MapChannel.SquadState.DbId);
            Assert.AreEqual(id, Rows(r => r.GetVisitorInstance(solo.Player.Id)));
            Assert.AreNotSame(instance, solo.Player.MapChannel);
        }

        [TestMethod]
        public void TheLeadPassingInAnInstanceIsSaved()
        {
            using var world = new WorldTestContext();
            var fixture = World(null, world, saved: true);
            var first = fixture.Player();
            var second = fixture.Player();
            var third = fixture.Player();

            fixture.Squad(first, second, third);

            var instance = fixture.Into(first);

            fixture.Into(second);

            // The lead passes to the second, who has no instance of the map: it is theirs.
            fixture.Squad(second, first, third);
            Assert.AreSame(instance, fixture.Into(third));
            Assert.AreEqual(second.Player.Id, instance.SquadOwnerCharacterId);

            var row = Rows(r => r.GetAll()).Single();

            Assert.AreEqual(instance.SquadState.DbId, row.Id);
            Assert.AreEqual(second.Player.Id, row.OwnerCharacterId);

            // It passes to the third, who has one saved and out of memory: the squad still goes
            // where its leader stands, and each instance stays whose it was.
            var theirs = Rows(r => r.Create(Caves, third.Player.Id, Ms(fixture.Utc)));
            var fourth = fixture.Player();

            fixture.Squad(third, first, second, fourth);
            Assert.AreSame(instance, fixture.Into(fourth));
            Assert.AreEqual(second.Player.Id, instance.SquadOwnerCharacterId);
            Assert.AreEqual(second.Player.Id, Rows(r => r.Get(row.Id)).OwnerCharacterId);
            Assert.AreEqual(third.Player.Id, Rows(r => r.Get(theirs.Id)).OwnerCharacterId);
            Assert.HasCount(2, Rows(r => r.GetAll()));
        }

        [TestMethod]
        public void ADatabaseThatFailsLeavesTheInstancesGoingInMemory()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            using var world = new WorldTestContext();
            var fixture = World(harness, world, saved: true);
            var store = (FileStore)fixture.Maps.SquadStore;
            var client = fixture.Player();

            store.Fails = new InvalidOperationException("the database is away");

            var instance = fixture.Into(client);

            Assert.IsTrue(instance.IsSquadInstance);
            Assert.AreEqual(0u, instance.SquadState.DbId, "not saved");
            Assert.IsTrue(store.Calls > 0);

            var a = Pool(instance, PoolA);

            Spawns(instance, 0);
            Kill(instance, a);
            Assert.AreEqual(Ms(fixture.Utc), a.ClearedAtUtcMs);
            fixture.Utc = fixture.Utc.AddMinutes(45);
            Spawns(instance, 0);
            Assert.AreEqual(2, a.AliveCreatures, "the respawn is told in memory");

            // Empty for an hour: it is not taken out of memory, where it would be lost.
            fixture.Leave(client);
            fixture.Maps.SquadInstanceWorker();
            fixture.Now += 3600_000;
            fixture.Maps.SquadInstanceWorker();
            Assert.AreSame(instance, fixture.Maps.SquadInstancesOf().Single());
            Assert.HasCount(0, fixture.Maps.SavedSquadInstances());
            Assert.AreSame(instance, fixture.Into(client));

            store.Fails = null;
            Assert.HasCount(0, Rows(r => r.GetAll()));
        }

        [TestMethod]
        public void AManagerWithADatabaseSavesThroughItsCharacterUnitOfWork()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var maps = new MapChannelManager(harness.Context, () => 0, (_, _, _) => { }, privateInstances: new PrivateMapInstanceService(),
                refreshStats: (_, _) => { }, assignPlayer: _ => { }, enterMapChannels: _ => { });

            Assert.IsInstanceOfType<DatabaseSquadInstanceStore>(maps.SquadStore);

            var row = maps.SquadStore.With(r => r.Create(Caves, 9, 123));

            Assert.AreEqual(row.Id, maps.SavedSquadInstances().Single().Id);

            using (var unit = harness.Context.CreateChar())
            {
                Assert.AreEqual(9u, unit.SquadInstances.Get(row.Id).OwnerCharacterId);
                unit.SquadInstances.SetPoolCleared(row.Id, PoolA, 77);
            }

            Assert.AreEqual(77, maps.SquadStore.With(r => r.GetPools(row.Id))[PoolA]);

            // Taken away, it stays away.
            maps.SquadStore = null;
            Assert.IsNull(maps.SquadStore);
            Assert.HasCount(0, maps.SavedSquadInstances());
        }

        [TestMethod]
        public void AManagerWithNoDatabaseSavesNothing()
        {
            using var world = new WorldTestContext();
            var fixture = World(null, world, saved: false);

            Assert.IsNull(fixture.Maps.SquadStore);

            var instance = fixture.Into(fixture.Player());

            Assert.AreEqual(0u, instance.SquadState.DbId);
            Assert.HasCount(0, fixture.Maps.SavedSquadInstances());
            Assert.IsFalse(fixture.Maps.UnloadSquadInstance(instance));
        }

        #endregion

        #region In and out of memory

        [TestMethod]
        public void AnEmptyInstanceIsTakenOutOfMemoryAndLoadedWhenSomebodyGoesToIt()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            using var world = new WorldTestContext();
            var fixture = World(harness, world, saved: true);
            var client = fixture.Player();
            var instance = fixture.Into(client);
            var id = instance.SquadState.DbId;

            Spawns(instance, 0);
            Kill(instance, Pool(instance, PoolA));

            var died = Ms(fixture.Utc);

            // With somebody in it, never.
            fixture.Now += 3600_000;
            fixture.Maps.SquadInstanceWorker();
            Assert.AreSame(instance, fixture.Maps.SquadInstancesOf().Single());

            fixture.Leave(client);
            fixture.Maps.SquadInstanceWorker();
            fixture.Now += 299_000;
            fixture.Maps.SquadInstanceWorker();
            Assert.AreSame(instance, fixture.Maps.SquadInstancesOf().Single(), "not before it has stood empty five minutes");

            fixture.Now += 1000;
            fixture.Maps.SquadInstanceWorker();
            Assert.HasCount(0, fixture.Maps.SquadInstancesOf());
            Assert.IsNull(fixture.Maps.FindByContextAndInstance(Caves, instance.InstanceId));
            Assert.IsNull(instance.SquadState, "taken apart with nothing listening");
            Assert.AreEqual(id, fixture.Maps.SavedSquadInstances().Single().Id, "its rows are kept");
            Assert.AreEqual(died, Rows(r => r.GetPools(id))[PoolA], "its creatures going was not their dying");
            Assert.HasCount(1, Rows(r => r.GetPools(id)));

            // Back in: the same instance as it was left, made again from its rows.
            fixture.Utc = fixture.Utc.AddMinutes(30);

            var again = fixture.Into(client);

            Assert.AreNotSame(instance, again);
            Assert.AreEqual(id, again.SquadState.DbId);
            Assert.AreEqual(died, Pool(again, PoolA).ClearedAtUtcMs);
            Assert.HasCount(1, fixture.Maps.SquadInstancesOf());
            Assert.HasCount(1, fixture.Maps.SavedSquadInstances());

            Spawns(again, OwnRespawnMs);
            Assert.AreEqual(0, Pool(again, PoolA).AliveCreatures);
            Assert.AreEqual(1, Pool(again, PoolB).AliveCreatures);

            // A server that keeps them in memory.
            fixture.Config.UnloadEmptySeconds = -1;
            fixture.Leave(client);
            fixture.Maps.SquadInstanceWorker();
            fixture.Now += 30L * 24 * 3600_000;
            fixture.Maps.SquadInstanceWorker();
            Assert.AreSame(again, fixture.Maps.SquadInstancesOf().Single());

            // And one that closes the empty ones: out of memory, rows and all.
            fixture.Config.EmptyCloseSeconds = 60;
            fixture.Maps.SquadInstanceWorker();
            Assert.HasCount(0, fixture.Maps.SquadInstancesOf());
            Assert.HasCount(0, Rows(r => r.GetAll()));
            Assert.HasCount(0, Rows(r => r.GetPools(id)));
        }

        #endregion

        #region The weekly reset

        [TestMethod]
        public void TheWeeklyResetClosesTheSavedInstancesNobodyIsInWhereverTheyAre()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            using var world = new WorldTestContext();
            var fixture = World(harness, world, saved: true);
            var stays = fixture.Player();
            var unloaded = fixture.Player();
            var empty = fixture.Player();

            // Saturday 04:00. One with somebody in it; one saved and out of memory, with a dead
            // pool; one in memory with nobody in it.
            var occupied = fixture.Into(stays);
            var gone = fixture.Into(unloaded);
            var goneId = gone.SquadState.DbId;

            Spawns(gone, 0);
            Kill(gone, Pool(gone, PoolA));
            fixture.Leave(unloaded);
            fixture.Maps.SquadInstanceWorker();
            fixture.Now += 300_000;
            fixture.Maps.SquadInstanceWorker();
            Assert.HasCount(1, fixture.Maps.SquadInstancesOf());

            var idle = fixture.Into(empty);

            fixture.Leave(empty);
            fixture.Now += MapChannelManager.SquadHoldMs;
            fixture.Maps.SquadInstanceWorker();
            Assert.HasCount(2, fixture.Maps.SquadInstancesOf());
            Assert.HasCount(3, Rows(r => r.GetAll()));

            var made = occupied.SquadState.CreatedAtUtcMs;

            // Tuesday 03:00.
            fixture.Clock = new DateTime(2026, 10, 6, 3, 0, 0);
            fixture.Utc = fixture.Utc.AddDays(3).AddHours(-1);
            fixture.Now += 1000;
            fixture.Maps.SquadInstanceWorker();

            Assert.AreSame(occupied, fixture.Maps.SquadInstancesOf().Single());

            var left = Rows(r => r.GetAll()).Single();

            Assert.AreEqual(occupied.SquadState.DbId, left.Id);
            Assert.AreEqual(Ms(fixture.Utc), left.CreatedAt, "passed over: it counts from this reset");
            Assert.AreEqual(left.CreatedAt, occupied.SquadState.CreatedAtUtcMs);
            Assert.AreNotEqual(made, left.CreatedAt);
            Assert.HasCount(0, Rows(r => r.GetPools(goneId)), "its dead pools with it");
            Assert.AreEqual(0u, Rows(r => r.GetVisitorInstance(unloaded.Player.Id)));
            Assert.AreEqual(0u, Rows(r => r.GetVisitorInstance(empty.Player.Id)));
            Assert.AreEqual(left.Id, Rows(r => r.GetVisitorInstance(stays.Player.Id)));
            Assert.IsNull(idle.SquadState);

            // The next to go to the map has a new one.
            var fresh = fixture.Into(unloaded);

            Assert.AreNotEqual(goneId, fresh.SquadState.DbId);
            Assert.AreEqual(0, Pool(fresh, PoolA).ClearedAtUtcMs);
        }

        [TestMethod]
        public void AServerThatWasDownAtTheResetClosesWhatItMissedOnItsFirstPass()
        {
            using var world = new WorldTestContext();
            var probe = World(null, world, saved: true);
            var now = Ms(probe.Utc);
            const long Day = 24L * 3600_000;

            // Saturday 04:00 by the server's clock: the last reset was Tuesday 03:00, 4 days and an hour ago.
            var lastReset = now - 4 * Day - 3600_000;
            var old = Rows(r => r.Create(Caves, 71, lastReset - 1));
            var week = Rows(r => r.Create(Caves, 72, now - 20 * Day));
            var since = Rows(r => r.Create(Caves, 73, lastReset));
            var recent = Rows(r => r.Create(Caves, 74, now - Day));

            Rows(r => { r.SetPoolCleared(old.Id, PoolA, now - 5 * Day); r.SetVisitorInstance(71, old.Id); r.SetVisitorInstance(74, recent.Id); return 0; });

            // An instance of this run, made before the first pass, is not one it missed.
            var mine = probe.Into(probe.Player());

            probe.Maps.SquadInstanceWorker();

            CollectionAssert.AreEquivalent(new[] { since.Id, recent.Id, mine.SquadState.DbId }, Rows(r => r.GetAll()).Select(row => row.Id).ToArray());
            Assert.HasCount(0, Rows(r => r.GetPools(old.Id)));
            Assert.AreEqual(0u, Rows(r => r.GetVisitorInstance(71)));
            Assert.AreEqual(recent.Id, Rows(r => r.GetVisitorInstance(74)));
            Assert.AreEqual(now - Day, Rows(r => r.Get(recent.Id)).CreatedAt, "those left are left as they were");
            Assert.AreEqual(week.Id, week.Id);

            // Only on the first pass: one dated before it afterwards waits for the reset itself.
            var later = Rows(r => r.Create(Caves, 75, lastReset - Day));

            probe.Now += 1000;
            probe.Maps.SquadInstanceWorker();
            Assert.IsNotNull(Rows(r => r.Get(later.Id)));

            // With no weekly reset, nothing is looked for.
            var none = World(null, world, saved: true, from: probe);

            none.Config.WeeklyReset = false;
            none.Maps.SquadInstanceWorker();
            Assert.IsNotNull(Rows(r => r.Get(later.Id)));
        }

        #endregion

        #region The rows

        [TestMethod]
        public void TheRepositoryKeepsOneInstanceAnOwnerAMapAndDeletesWhatHangsOnOne()
        {
            var first = Rows(r => r.Create(Caves, 5, 1000));

            Assert.AreNotEqual(0u, first.Id);
            Assert.AreEqual(first.Id, Rows(r => r.Create(Caves, 5, 2000)).Id, "the one there already");
            Assert.AreEqual(1000, Rows(r => r.Find(Caves, 5)).CreatedAt);
            Assert.IsNull(Rows(r => r.Find(Caves, 6)));
            Assert.IsNull(Rows(r => r.Find(1507, 5)));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Rows(r => r.Create(Caves, 0, 1)));

            var second = Rows(r => r.Create(Caves, 6, 3000));
            var elsewhere = Rows(r => r.Create(1507, 5, 4000));

            Assert.IsFalse(Rows(r => r.SetOwner(second.Id, 5)), "5 has one of that map");
            Assert.IsTrue(Rows(r => r.SetOwner(second.Id, 7)));
            Assert.IsTrue(Rows(r => r.SetOwner(second.Id, 7)));
            Assert.IsFalse(Rows(r => r.SetOwner(9999, 8)));
            Assert.AreEqual(second.Id, Rows(r => r.Find(Caves, 7)).Id);

            Rows(r =>
            {
                r.SetPoolCleared(first.Id, PoolA, 10);
                r.SetPoolCleared(first.Id, PoolA, 20);
                r.SetPoolCleared(first.Id, PoolB, 30);
                r.SetPoolCleared(second.Id, PoolA, 40);
                r.SetVisitorInstance(5, first.Id);
                r.SetVisitorInstance(5, second.Id);
                r.SetVisitorInstance(6, first.Id);
                r.SetVisitorInstance(8, elsewhere.Id);
                return 0;
            });

            var pools = Rows(r => r.GetPools(first.Id));

            Assert.HasCount(2, pools);
            Assert.AreEqual(20, pools[PoolA]);
            Assert.AreEqual(second.Id, Rows(r => r.GetVisitorInstance(5)), "one row a character");
            Assert.AreEqual(0u, Rows(r => r.GetVisitorInstance(99)));

            Rows(r => { r.RemovePool(first.Id, PoolA); return 0; });
            Assert.HasCount(1, Rows(r => r.GetPools(first.Id)));
            Rows(r => { r.SetPoolCleared(first.Id, PoolA, 50); r.RemovePools(first.Id); return 0; });
            Assert.HasCount(0, Rows(r => r.GetPools(first.Id)));
            Assert.HasCount(1, Rows(r => r.GetPools(second.Id)));

            Rows(r => { r.SetCreatedAt(first.Id, 5000); r.SetPoolCleared(first.Id, PoolB, 60); return 0; });
            Assert.AreEqual(5000, Rows(r => r.Get(first.Id)).CreatedAt);

            // Before a time, and but for those named.
            Assert.AreEqual(0, Rows(r => r.DeleteAllExcept(new[] { second.Id }, 3001)), "the one from before it is kept by name");
            Assert.AreEqual(1, Rows(r => r.DeleteAllExcept(new uint[0], 3001)));
            Assert.IsNull(Rows(r => r.Get(second.Id)));
            Assert.HasCount(0, Rows(r => r.GetPools(second.Id)));
            Assert.AreEqual(0u, Rows(r => r.GetVisitorInstance(5)));
            Assert.AreEqual(first.Id, Rows(r => r.GetVisitorInstance(6)));

            Assert.IsTrue(Rows(r => r.Delete(first.Id)));
            Assert.IsFalse(Rows(r => r.Delete(first.Id)));
            Assert.HasCount(0, Rows(r => r.GetPools(first.Id)));
            Assert.AreEqual(0u, Rows(r => r.GetVisitorInstance(6)));

            Assert.AreEqual(1, Rows(r => r.DeleteAllExcept(null, null)));
            Assert.HasCount(0, Rows(r => r.GetAll()));
            Assert.AreEqual(0u, Rows(r => r.GetVisitorInstance(8)));
        }

        #endregion

        private static long Ms(DateTime utc) => (long)(utc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

        private T Rows<T>(Func<ISquadInstanceRepository, T> action)
        {
            using var context = (CharContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteCharContext), _database);
            return action(new SquadInstanceRepository(context));
        }

        /// <summary>
        /// The fixture's world with two spawn pools on the map: two soldiers and one. Saved to
        /// this test's database or not; and, from another, the same world after a restart: its
        /// clocks carried on, and nothing in memory.
        /// </summary>
        private Fixture World(BootcampRuntimeTestHarness.Harness harness, WorldTestContext world, bool saved, Fixture from = null)
        {
            var fixture = new Fixture(world);

            if (from != null)
            {
                fixture.Now = from.Now;
                fixture.Clock = from.Clock;
                fixture.Utc = from.Utc;
                fixture.Config = from.Config;
            }

            if (saved)
                fixture.Maps.SquadStore = new FileStore(_database);

            if (harness != null)
            {
                Define(harness, ThraxSoldier);
                fixture.Own.SpawnPools.Add(Template(PoolA, 2, new Vector3(900, 5, 300)));
                fixture.Own.SpawnPools.Add(Template(PoolB, 1, new Vector3(940, 5, 300)));

                foreach (var pool in fixture.Own.SpawnPools)
                    pool.RuntimeMapChannel = fixture.Own;
            }

            _fixtures.Add(fixture);

            return fixture;
        }

        private static SpawnPool Template(uint id, short count, Vector3 position) => new SpawnPool
        {
            DbId = id,
            Position = position,
            MapContextId = Caves,
            Mode = SpawnPoolManager.ModeAutomatic,
            AnimType = 0,
            RespawnTime = OwnRespawnMs,
            UpdateTimer = OwnRespawnMs,
            SpawnSlot = new List<SpawnPoolSlot> { new SpawnPoolSlot(ThraxSoldier, count, count) }
        };

        /// <summary>A creature of the world database, known to the creature manager as the server has it.</summary>
        private static void Define(BootcampRuntimeTestHarness.Harness harness, uint creatureId)
        {
            var entry = harness.WorldContext.Set<CreatureEntry>().AsNoTracking().Single(row => row.Id == creatureId);
            var classEntry = harness.WorldContext.Set<EntityClassEntry>().AsNoTracking().Single(row => row.Id == entry.ClassId);
            var entityClass = new EntityClass(classEntry.Id, classEntry.ClassName, classEntry.MeshId, classEntry.ClassCollisionRole,
                classEntry.AugList.Split(',').Select(value => (AugmentationType)uint.Parse(value)).ToList(), classEntry.TargetFlag != 0);

            entityClass.CreatureFlags.AddRange(harness.WorldContext.Set<CreatureClassFlagEntry>().AsNoTracking()
                .Where(row => row.ClassId == entry.ClassId).Select(row => (CreatureFlag)row.FlagId));
            EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)entry.ClassId] = entityClass;
            CreatureManager.Instance.LoadedCreatures[entry.Id] = new Creature(entry) { AppearanceData = new Dictionary<EquipmentData, AppearanceData>() };
        }

        private static SpawnPool Pool(MapChannel map, uint id) => map.SpawnPools.Single(pool => pool.DbId == id);

        private static void Spawns(MapChannel map, long passed) => SpawnPoolManager.Instance.SpawnPoolWorker(map, passed);

        private static IEnumerable<Creature> Creatures(MapChannel map) =>
            map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct();

        /// <summary>Kills creatures of a pool that are alive: all of them, or so many.</summary>
        private static void Kill(MapChannel map, SpawnPool pool, int count = int.MaxValue)
        {
            foreach (var creature in Creatures(map).Where(creature => ReferenceEquals(creature.SpawnPool, pool) && creature.State != CharacterState.Dead).Take(count).ToList())
            {
                creature.Attributes[Attributes.Health].Current = 0;
                CreatureManager.Instance.HandleCreatureKill(map, creature, null);
            }
        }

        /// <summary>The store the server has, over a database file: a context for each thing done.</summary>
        private sealed class FileStore : ISquadInstanceStore
        {
            private readonly string _database;

            internal Exception Fails;
            internal int Calls;

            internal FileStore(string database) => _database = database;

            public T With<T>(Func<ISquadInstanceRepository, T> action)
            {
                Calls++;

                if (Fails != null)
                    throw Fails;

                using var context = (CharContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteCharContext), _database);
                return action(new SquadInstanceRepository(context));
            }
        }
    }
}
