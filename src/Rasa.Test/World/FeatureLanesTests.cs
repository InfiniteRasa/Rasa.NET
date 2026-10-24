using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Config;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// Where the feature lanes meet, on the migrated world: the title bosses (lane small) come back at a boss's
    /// pace (lane pace); a cleared camp's pace says when it comes back and the reinforcements (lane raids) how -
    /// the ship is sent when the pace is up and is not a second wait; the arrival kinds the raids added are
    /// no data's; and a Bane raid besieges the control point defences' garrison posts (lane cpdef) with the
    /// rest of the point's AFS garrison, which stands down when the point is lost and back when it is retaken.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class FeatureLanesTests
    {
        private const uint LandingZone = 4;

        private RespawnPaceConfig _pace;
        private ReinforcementConfig _reinforcements;
        private RaidConfig _raids;
        private Func<uint, bool> _staged;

        [TestInitialize]
        public void Start()
        {
            _pace = RespawnPace.Config;
            _reinforcements = Reinforcements.Config;
            _raids = BaneRaids.Instance.Config;
            _staged = SpawnPoolManager.Instance.IsStaged;

            RespawnPace.Config = new RespawnPaceConfig { Enabled = true };
            Reinforcements.Config = new ReinforcementConfig();
            BaneRaids.Instance.Config = new RaidConfig();
            BaneRaids.Instance.Reset();
            RespawnPace.Forget();
        }

        [TestCleanup]
        public void Restore()
        {
            foreach (var point in ControlPoints.Instance.Points.ToList())
                BaneRaids.Instance.Stop(point.Id);

            BaneRaids.Instance.Reset();
            ControlPoints.Instance.Load(new List<ControlPointEntry>(), new List<ControlPointLinkEntry>(), null);
            RespawnPace.Config = _pace;
            Reinforcements.Config = _reinforcements;
            BaneRaids.Instance.Config = _raids;
            SpawnPoolManager.Instance.IsStaged = _staged;
            RespawnPace.Forget();
        }

        /// <summary>
        /// The 32 title bosses' pools (9300001..32) say 50 s, upstream's boss pools' time. To the pace, 31 are named
        /// bosses (300-600 s): 25 by their classes' names, the other six (a Filcher, a Hunter, a Lightbender, a Thrax
        /// trooper, a Lasher, the Juggernaut prototype) because a boss title names them on their map. The Warden
        /// Abomination is of a class a loaded mission asks to be killed, and keeps a mission's pace (60-120 s), as
        /// every mission target does. On a battlefield each stays down its kind's time, never its row's 50 s.
        /// </summary>
        [TestMethod]
        public void TheTitleBossesComeBackAtABosssPace()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SpawnPoolManager.Instance.IsStaged = _ => false;
            RespawnPace.Forget();

            var kinds = new Dictionary<uint, RespawnPace.Kind>();

            for (var i = 0u; i < TitleBossesSeed.BossCount; i++)
            {
                var id = TitleBossesSeed.FirstCreatureId + i;
                var pool = SpawnPoolManager.Instance.LoadedSpawnPools[id];

                Assert.AreEqual(50_000L, pool.RespawnTime, $"pool {id}: its row");
                kinds[id] = RespawnPace.KindOf(pool);
            }

            const uint warden = 9300032;
            var notBoss = kinds.Where(k => k.Key != warden && k.Value != RespawnPace.Kind.Boss).Select(k => $"{k.Key} {k.Value}").ToList();

            Assert.IsEmpty(notBoss, string.Join(", ", notBoss));
            Assert.AreEqual(RespawnPace.Kind.Mission, kinds[warden], "a mission's target keeps a mission's pace");

            // Down on its own battlefield: a boss's five to ten minutes, the Warden a mission's one to two.
            foreach (var (id, kind) in kinds)
            {
                var template = SpawnPoolManager.Instance.LoadedSpawnPools[id];

                if (!RespawnPace.Battlefields.Contains(template.MapContextId))
                    continue;

                var pool = new SpawnPool
                {
                    DbId = id,
                    Mode = template.Mode,
                    MapContextId = template.MapContextId,
                    RespawnTime = template.RespawnTime,
                    SpawnSlot = template.SpawnSlot,
                    RuntimeMapChannel = new MapChannel { MapInfo = new MapInfo(template.MapContextId, "battlefield", 0, 0) }
                };

                RespawnPace.Down(pool, 0.0);
                Assert.AreEqual(kind == RespawnPace.Kind.Boss ? 300_000L : 60_000L, pool.PaceDownMs, $"pool {id} ({kind}) at its least");
                RespawnPace.Down(pool, 1.0);
                Assert.AreEqual(kind == RespawnPace.Kind.Boss ? 600_000L : 120_000L, pool.PaceDownMs, $"pool {id} ({kind}) at its most");
            }
        }

        /// <summary>
        /// spawnpool_arrival.kind 3 (an AFS dropship) and 4 (a beam-in) are the raids' own: the world's rows are all
        /// upstream's 1 (a pad or bay) and 2 (a teleporter), and the four constants differ.
        /// </summary>
        [TestMethod]
        public void TheArrivalKindsTheRaidsAddedAreNoDatas()
        {
            using var harness = WildernessRuntimeTestHarness.Create();

            var kinds = harness.World.SpawnPoolArrivalEntries.Select(arrival => arrival.Kind).Distinct().OrderBy(kind => kind).ToList();

            CollectionAssert.AreEqual(new List<byte> { SpawnPoolArrivalEntry.KindDropship, SpawnPoolArrivalEntry.KindTeleporter }, kinds);
            CollectionAssert.AllItemsAreUnique(new[]
            {
                SpawnPoolArrivalEntry.KindDropship, SpawnPoolArrivalEntry.KindTeleporter, SpawnPoolArrivalEntry.KindAfsDropship, SpawnPoolArrivalEntry.KindBeamIn
            });
            Assert.AreEqual((byte)3, SpawnPoolArrivalEntry.KindAfsDropship);
            Assert.AreEqual((byte)4, SpawnPoolArrivalEntry.KindBeamIn);
        }

        /// <summary>
        /// The pace says when, the reinforcements how: a Wilderness Thrax camp cleared stays down its pace (60-90 s)
        /// and no ship comes before; when the pace is up one Bane ship is sent, and the few seconds of its flight are
        /// its way in, not a second wait - the pool is not cleared again by the ship, and its time is not started
        /// over. A troop of the landed camp killed comes back no sooner than its pace either.
        /// </summary>
        [TestMethod]
        public void AClearedCampComesBackAtItsPaceAndByOneShip()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SpawnPoolManager.Instance.IsStaged = _ => false;
            RespawnPace.Forget();

            var map = harness.Map;
            var camp = map.SpawnPools.FirstOrDefault(pool => RespawnPace.Applies(pool) && pool.AnimType == 0 && pool.Arrivals.Count == 0
                && pool.SpawnSlot.Count(slot => slot.CountMax > 0) > 0
                && pool.SpawnSlot.Where(slot => slot.CountMax > 0).All(slot => CreatureManager.Instance.LoadedCreatures.TryGetValue(slot.CreatureId, out var c) && Reinforcements.IsBaneTroop(c) && c.WalkSpeed > 0.01f)
                && RespawnPace.KindOf(pool) == RespawnPace.Kind.Trash && pool.RespawnTime < 60_000);

            Assert.IsNotNull(camp, "a Wilderness Thrax camp whose row is shorter than its pace");

            var pools = map.SpawnPools.ToList();
            map.SpawnPools.Clear();
            map.SpawnPools.Add(camp);

            try
            {
                // The world's first stocking: set down where it stands, no ship.
                camp.UpdateTimer = camp.RespawnTime;
                SpawnPoolManager.Instance.SpawnPoolWorker(map, 1000);
                Assert.IsFalse(Ships(camp).Any(), "the world starts stocked");
                var first = Living(harness, camp);
                Assert.IsNotEmpty(first);

                // Cleared.
                foreach (var creature in first)
                    Kill(harness, creature);

                Assert.AreEqual(0, camp.AliveCreatures + camp.QueuedCreatures + camp.DropshipQueue);
                Assert.AreEqual(RespawnPace.Kind.Trash, camp.PaceKind);
                Assert.IsTrue(camp.PaceDownMs >= 60_000 && camp.PaceDownMs <= 90_000, $"down {camp.PaceDownMs} ms: the rank and file's pace, not its row's {camp.RespawnTime} ms");
                var down = camp.PaceDownMs;

                var waited = 0L;

                while (!Ships(camp).Any() && waited < 200_000)
                {
                    SpawnPoolManager.Instance.SpawnPoolWorker(map, 1000);
                    waited += 1000;
                }

                Assert.IsTrue(waited >= down && waited < down + 1000, $"the ship is sent when the pace is up: {waited} ms after, down {down} ms");
                Assert.AreEqual(1, Ships(camp).Count(), "one ship");
                Assert.AreEqual(EntityClasses.UsableCrSpawnerBaneDropshipV01, Ships(camp).Single().EntityClassId);

                // In flight: the worker does not send another nor count the camp down again.
                SpawnPoolManager.Instance.SpawnPoolWorker(map, 1000);
                Assert.AreEqual(1, Ships(camp).Count());

                var flown = 0L;

                while (Ships(camp).Any() && flown < 60_000)
                {
                    DynamicObjectManager.Instance.DropshipsWorker(map, 250);
                    flown += 250;
                }

                Assert.IsLessThan(30_000L, flown, $"the flight, {flown} ms, is the way in");
                var landed = Living(harness, camp);
                Assert.IsNotEmpty(landed, "its troops are down");
                Assert.AreEqual(0, camp.DropshipQueue + camp.QueuedCreatures);
                Assert.AreEqual(down, camp.PaceDownMs, "not cleared again by its own ship");
                Assert.IsTrue(camp.UpdateTimer >= 0, "nor its time started over");

                // One of them killed among the rest: back no sooner than the rank and file's pace.
                if (landed.Count > 1)
                {
                    Kill(harness, landed[0]);
                    Assert.IsTrue(camp.UpdateTimer <= camp.RespawnTime - 60_000, $"timer {camp.UpdateTimer}: the top-up waits its pace");
                }
            }
            finally
            {
                foreach (var ship in Ships(camp).ToList())
                {
                    CellManager.Instance.RemoveFromWorld(map, ship);
                    DynamicObjectManager.Instance.Dropships.Remove(ship.EntityId);
                }

                foreach (var creature in Living(harness, camp))
                    CellManager.Instance.RemoveCreatureFromWorld(map, creature);

                map.SpawnPools.Clear();
                map.SpawnPools.AddRange(pools);
            }
        }

        /// <summary>
        /// The Landing Zone's AFS garrison is its posts of the control point defences (pools 9705001.., kind-2 links)
        /// besides upstream's: a Bane raid besieges every one of them, which are not restocked until it is over; the
        /// point lost, they stand down with the rest of the AFS garrison, and the Bane garrison a raid leaves a point
        /// is adopted beside them and given back without touching them.
        /// </summary>
        [TestMethod]
        public void ABaneRaidBesiegesTheDefencesGarrisonPosts()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var map = harness.Map;
            var points = harness.World.ControlPointEntries.Where(p => p.MapContextId == map.MapInfo.MapContextId).ToList();
            var ids = points.Select(p => p.Id).ToHashSet();
            var links = harness.World.ControlPointLinkEntries.Where(l => ids.Contains(l.ControlPointId)).ToList();

            ControlPoints.Instance.Load(points, links, null);
            ControlPoints.Instance.Place(map);

            var point = ControlPoints.Instance.ById(LandingZone);
            Assert.IsNotNull(point, "the Landing Zone");
            point.Object.IsInWorld = true;

            var posts = point.AfsPools.Where(id => id >= 9705001 && id <= 9705299).ToHashSet();
            Assert.IsNotEmpty(posts, "the defences' garrison posts are the point's AFS garrison");
            var onMap = map.SpawnPools.Where(pool => posts.Contains(pool.DbId)).ToList();
            Assert.HasCount(posts.Count, onMap, "and on its map");
            Assert.IsTrue(onMap.All(pool => pool.IsGarrison && !pool.Suspended), "running while the AFS hold it");

            var raid = BaneRaids.Instance.Begin(map, point);
            Assert.IsNotNull(raid, "a raid on it");
            Assert.IsTrue(onMap.All(pool => pool.Besieged), "every post besieged");
            Assert.IsTrue(onMap.All(pool => raid.Besieged.Contains(pool)));
            Assert.IsNotNull(ControlPoints.Instance.GarrisonOf(map, point, ControlPoints.Afs).ToString());

            BaneRaids.Instance.Stop(point.Id);
            Assert.IsFalse(onMap.Any(pool => pool.Besieged), "relieved when it is over");

            // Lost: the posts stand down; a Bane garrison of no table's adopted, then given back.
            Assert.IsTrue(ControlPoints.Instance.SetOwner(point, ControlPoints.Bane, null));
            Assert.IsTrue(onMap.All(pool => pool.Suspended), "the AFS posts stand down");

            var given = Reinforcements.TransientPool(map, point.Object.Position, point.Object.Rotation, 10f,
                new List<SpawnPoolSlot> { new SpawnPoolSlot(point.AfsPools.Select(id => map.SpawnPools.First(p => p.DbId == id)).SelectMany(p => p.SpawnSlot).First().CreatureId, 1, 1) });

            ControlPoints.Instance.AdoptBanePool(map, point, given);
            Assert.IsTrue(point.BanePools.Contains(given.DbId));
            Assert.IsTrue(posts.IsSubsetOf(point.AfsPools), "the posts are the AFS's still");

            ControlPoints.Instance.ReleaseBanePool(map, point, given);
            Assert.IsFalse(point.BanePools.Contains(given.DbId));
            Assert.IsFalse(map.SpawnPools.Contains(given));

            // Retaken: the posts run again, flown in.
            Assert.IsTrue(ControlPoints.Instance.SetOwner(point, ControlPoints.Afs, null));
            Assert.IsTrue(onMap.All(pool => !pool.Suspended && pool.ShipNextArrival), "the posts back, by ship");
        }

        #region Fixtures

        private static IEnumerable<Dropship> Ships(SpawnPool pool) =>
            DynamicObjectManager.Instance.Dropships.Values.Where(ship => ReferenceEquals(ship.SpawnPool, pool));

        private static List<Creature> Living(WildernessRuntimeTestHarness harness, SpawnPool pool) =>
            harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct()
                .Where(creature => ReferenceEquals(creature.SpawnPool, pool) && CreatureManager.IsLivingOnMap(harness.Map, creature)).ToList();

        /// <summary>As a kill tells its pool (CreatureManager.HandleCreatureKill): one fewer alive, and which.</summary>
        private static void Kill(WildernessRuntimeTestHarness harness, Creature creature)
        {
            CellManager.Instance.RemoveCreatureFromWorld(harness.Map, creature);
            SpawnPoolManager.Instance.DecreaseAliveCreatureCount(harness.Map, creature.SpawnPool, creature);
        }

        #endregion
    }
}
