using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Config;
using Rasa.Context.World;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Database;
using Rasa.Test.Missions;
using Rasa.Test.Missions.Wilderness;
using Content = Rasa.Services.Preloader.AshenDynamicDropshipsContent;

namespace Rasa.Test.World
{
    /// <summary>Ligo Ashen Desert's control points: AFS garrisons by dropship, timed Bane assaults, AFS relief and sorties.</summary>
    [TestClass, DoNotParallelize]
    public class AshenDynamicDropshipsTests
    {
        private const string Migration = "20271114000000_Ashen_dynamic_dropships";
        private const string MapName = "adv_arieki_ligo_ashendesert";

        /// <summary>How far, across the ground, a Bane drop lands from each of its point's turrets at least (ESTIMATE, as the Marshes' and the Crucible's).</summary>
        private const float DropClearOfTurrets = 30f;

        /// <summary>How far a drop lands from a Bane mortar mount at least (ESTIMATE, as the Crucible's).</summary>
        private const float DropClearOfMortars = 20f;

        /// <summary>How far a garrison pool stands from the point's own NPCs (hospital, token banker) at least (ESTIMATE, as the Crucible's).</summary>
        private const float PoolClearOfNpcs = 8f;

        /// <summary>How far a garrison pool, and the relief post, stand from White Oasis Post's quest givers at least (ESTIMATE: an AFS reach of 8 m and a margin).</summary>
        private const float ClearOfQuestNpcs = 12f;

        /// <summary>How far a dropship's landing spot is clear of walls round it (ESTIMATE: a ship's footprint).</summary>
        private const float ShipClear = 8f;

        private WildernessRuntimeTestHarness _world;
        private ControlPointConfig _config;
        private Func<long> _clock;
        private long _now;

        private static ControlPoints.Point Point(uint id) => ControlPoints.Instance.ById(id);
        private static float Flat(Vector3 a, Vector3 b) => new Vector2(a.X - b.X, a.Z - b.Z).Length();

        [TestInitialize]
        public void Start()
        {
            _config = ControlPoints.Instance.Config;
            _clock = LandingZoneAssaults.Now;
            _now = 1000;
            LandingZoneAssaults.Now = () => _now;
            ControlPoints.Instance.Config = new ControlPointConfig();
            _world = WildernessRuntimeTestHarness.Create(mapContextId: Content.MapId);
            using var db = _world.CreateWorld();
            ControlPoints.Instance.Load(db.ControlPoints.GetControlPoints().Where(p => p.MapContextId == Content.MapId),
                db.ControlPoints.GetLinks().Where(l => l.ControlPointId is Content.ConscriptionGarrison or Content.WhiteOasisPost), null);
            ControlPoints.Instance.Place(_world.Map);
        }

        [TestCleanup]
        public void End()
        {
            ControlPoints.Instance.Config.ControlPointAssaultEnabled = false;
            foreach (var id in new[] { Content.ConscriptionGarrison, Content.WhiteOasisPost })
            {
                LandingZoneAssaults.Worker(_world.Map, Point(id));
                AfsSorties.Worker(_world.Map, Point(id));
            }
            ControlPoints.Instance.Load(Array.Empty<ControlPointEntry>(), Array.Empty<ControlPointLinkEntry>(), null);
            _world.Dispose();
            ControlPoints.Instance.Config = _config;
            LandingZoneAssaults.Now = _clock;
        }

        private SpawnPool[] Assaults(uint point) => _world.Map.SpawnPools.Where(p => p.IsControlPointAssault && p.AssaultPointId == point).ToArray();
        private SpawnPool[] Relief(uint point) => _world.Map.SpawnPools.Where(p => p.IsControlPointReinforcement && p.AssaultPointId == point).ToArray();
        private Creature[] Troops(uint point) => EntityManager.Instance.Creatures.Values
            .Where(c => Assaults(point).Contains(c.SpawnPool) && CreatureManager.IsLivingOnMap(_world.Map, c)).ToArray();
        private void Deliver() { for (var i = 0; i < 6; i++) DynamicObjectManager.Instance.DropshipsWorker(_world.Map, 60000); }
        private void Tick() => ControlPoints.Instance.Worker(_world.Map);

        private Vector3 Walk(Vector3 at, string what)
        {
            var ground = NavMeshManager.NearestWalkable(_world.Map, at);
            Assert.IsNotNull(ground, $"{what}: no ground");
            return ground.Value;
        }

        /// <summary>A whole path from <paramref name="from"/> to the point, at most 1.6 times the line (the tester's landing search).</summary>
        private void Reaches(Vector3 from, uint point, string what)
        {
            var start = Walk(from, what);
            var end = Walk(Point(point).Position, $"point {point}");
            var path = NavMeshManager.FindPathBounded(_world.Map, start, end, out var complete);
            Assert.IsTrue(complete && path?.Count > 0, $"{what} must reach point {point}");
            var length = 0f;
            var previous = start;
            foreach (var corner in path) { length += Vector3.Distance(previous, corner); previous = corner; }
            Assert.IsTrue(length <= 1.6f * Vector3.Distance(start, end) + 5f, $"{what}: the way to point {point} is {length:0} m for {Vector3.Distance(start, end):0} m");
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;
            return directory?.FullName ?? AppContext.BaseDirectory;
        }

        private static Rasa.Navigation.CoverMesh CoverFile() =>
            Rasa.Navigation.CoverMesh.Read(Rasa.Navigation.CoverMesh.PathFor(Path.Combine(RepositoryRoot(), "navmesh"), MapName));

        /// <summary>The World DB's turret pools of a point (ControlPointTurrets' range, linked kind 1 or 2): where they stand after FenceTurretBases.</summary>
        private Vector3[] TurretsOf(uint point)
        {
            var ids = _world.World.ControlPointLinkEntries.AsNoTracking().Where(l => l.ControlPointId == point && (l.Kind == 1 || l.Kind == 2) &&
                l.ObjectId >= ControlPointTurrets.PoolIdMin && l.ObjectId <= ControlPointTurrets.PoolIdMax).Select(l => l.ObjectId).ToList();
            return _world.World.SpawnPoolEntries.AsNoTracking().Where(p => ids.Contains(p.Id)).AsEnumerable().Select(p => p.Position).ToArray();
        }

        [TestMethod]
        public void TheAshenDesertHasTwoControlPointsConscriptionGarrisonBaneAndWhiteOasisPostAfs()
        {
            Assert.AreEqual(2, ControlPoints.Instance.Points.Count(p => p.MapContextId == Content.MapId));
            Assert.AreEqual(ControlPoints.Bane, Point(Content.ConscriptionGarrison).Owner);
            Assert.AreEqual(ControlPoints.Afs, Point(Content.WhiteOasisPost).Owner);
            Assert.AreEqual(2, Content.Assaults.Length);
            Assert.IsTrue(Content.Assaults.All(a => a.Sortie.Length > 0), "each point has a sortie plan for when the Bane hold it");
            var garrison = Content.Assaults.Single(a => a.PointId == Content.ConscriptionGarrison);
            var oasis = Content.Assaults.Single(a => a.PointId == Content.WhiteOasisPost);
            Assert.IsTrue(oasis.PeriodSeconds > garrison.PeriodSeconds, "the quest hub is attacked less often than the other point");
            Assert.IsTrue(oasis.FirstDelaySeconds >= 1800, "the quest hub has a quiet first half hour after a start or a retake");
        }

        [TestMethod]
        public void EveryDropPadReliefPadAndGarrisonPostStandsOnGroundThatReachesItsPoint()
        {
            foreach (var assault in Content.Assaults)
            {
                Assert.AreEqual(Content.MapId, assault.MapId, $"point {assault.PointId}");
                Assert.AreEqual(2, assault.Drops.Length, $"point {assault.PointId}: two Bane ships a wave");
                var turrets = TurretsOf(assault.PointId);
                Assert.AreEqual(assault.PointId == Content.WhiteOasisPost ? 4 : 0, turrets.Length, $"point {assault.PointId}: its turret pools (AFS and Bane)");
                for (var i = 0; i < assault.Drops.Length; i++)
                {
                    var drop = assault.Drops[i].Position;
                    Assert.IsTrue(Vector3.Distance(Walk(drop, "drop"), drop) < 2f, $"point {assault.PointId} drop {i + 1} on the ground");
                    Reaches(drop, assault.PointId, $"point {assault.PointId} drop {i + 1}");
                    // White Oasis Post's fence reaches 78 m east of the point (its gate turrets): drops land outside it.
                    var distance = Vector3.Distance(drop, Point(assault.PointId).Position);
                    Assert.IsTrue(distance is > 45 and < 125, $"point {assault.PointId} drop {i + 1} lands outside the walls, {distance:0} m");
                    foreach (var turret in turrets.Concat(ControlPointTurrets.Spots.Where(s => s.Point == assault.PointId).Select(s => new Vector3((float)s.X, (float)s.Y, (float)s.Z))))
                    {
                        var clear = Flat(drop, turret);
                        Assert.IsTrue(clear >= DropClearOfTurrets, $"point {assault.PointId} drop {i + 1} lands {clear:0} m from the turret at ({turret.X:0}, {turret.Z:0}): troops set down by a turret's mount stall against it");
                    }
                    foreach (var mortar in BaneMortars.Mortars.Where(m => m.Map == Content.MapId))
                    {
                        var clear = new Vector2(drop.X - (float)mortar.X, drop.Z - (float)mortar.Z).Length();
                        Assert.IsTrue(clear >= DropClearOfMortars, $"point {assault.PointId} drop {i + 1} lands {clear:0} m from the mortar at ({mortar.X:0}, {mortar.Z:0})");
                    }
                }
                Assert.IsTrue(Vector3.Distance(Walk(assault.ReliefPad.Position, "relief pad"), assault.ReliefPad.Position) < 2f, $"point {assault.PointId} relief pad on the ground");
                var pad = Flat(assault.ReliefPad.Position, Point(assault.PointId).Position);
                Assert.IsTrue(pad < 60, $"point {assault.PointId} relief pad {pad:0} m out: an assault goes for the relief where it lands (LandingZoneBreach.Order)");
                foreach (var mortar in BaneMortars.Mortars.Where(m => m.Map == Content.MapId))
                    Assert.IsTrue(Flat(assault.ReliefPad.Position, new Vector3((float)mortar.X, 0, (float)mortar.Z)) >= DropClearOfMortars, $"point {assault.PointId} relief pad by the mortar at ({mortar.X:0}, {mortar.Z:0})");
                foreach (var drop in assault.Drops)
                    Assert.IsTrue(Flat(assault.ReliefPad.Position, drop.Position) >= 20f, $"point {assault.PointId} relief pad clear of its drops");
                Reaches(assault.ReliefPad.Position, assault.PointId, $"point {assault.PointId} relief pad");
                Reaches(assault.ReliefPost, assault.PointId, $"point {assault.PointId} relief post");
            }
            foreach (var pool in Content.Garrison)
            {
                Assert.IsTrue(Vector3.Distance(Walk(pool.At, $"pool {pool.Id}"), pool.At) < 1.5f, $"pool {pool.Id} stands on the ground");
                Reaches(pool.At, pool.Point, $"pool {pool.Id}");
                Assert.IsTrue(Vector3.Distance(pool.At, Point(pool.Point).Position) < 60, $"pool {pool.Id} inside its point");
            }
        }

        /// <summary>How far a drop or a relief pad lands from a waypoint, hospital or teleporter at least (ESTIMATE, audit 2026-11-14: players arrive there).</summary>
        private const float ClearOfArrivals = 20f;

        /// <summary>
        /// The Ashen Desert's plans: no Bane drop and no relief pad within 20 m of a waypoint, hospital or teleporter of the
        /// map (the World DB teleporter table, TeleporterPreloader), so that an assault does not fight where players arrive.
        /// White Oasis Post's first relief pad was 5 m from its waypoint 317. Ashen only: the Pools' Retread Outpost drop 1
        /// (203, 390) is 12.8 m from its hospital 290 (merged upstream, #202, real-client verified).
        /// </summary>
        [TestMethod]
        public void NoDropOrReliefPadLandsByAWaypointOrTeleporter()
        {
            var arrivals = _world.World.TeleporterEntries.AsNoTracking().Where(t => t.MapContextId != 0).AsEnumerable()
                .Where(t => t.PosX != 0 || t.PosY != 0 || t.PosZ != 0).ToList();
            Assert.IsTrue(arrivals.Any(t => t.Id == 317 && t.MapContextId == Content.MapId), "White Oasis Post's waypoint 317");
            var report = new System.Collections.Generic.List<string>();
            foreach (var assault in Content.Assaults)
                foreach (var (what, at) in assault.Drops.Select((d, i) => ($"drop {i + 1}", d.Position)).Append(("relief pad", assault.ReliefPad.Position)))
                    foreach (var arrival in arrivals.Where(t => t.MapContextId == assault.MapId))
                    {
                        var clear = Flat(at, new Vector3((float)arrival.PosX, 0, (float)arrival.PosZ));
                        if (clear < ClearOfArrivals)
                            report.Add($"map {assault.MapId} point {assault.PointId} {what} ({at.X:0}, {at.Z:0}): {clear:0.0} m from teleporter {arrival.Id} '{arrival.Description}'");
                    }
            Assert.AreEqual(0, report.Count, string.Join("; ", report));
        }

        /// <summary>
        /// The map's collision (the cover file's triangles) round a spot: in how many of 16 directions a line from the
        /// spot out to <see cref="ShipClear"/> m meets it at knee, chest or 3 m height (the zone readiness pad-wall probe's
        /// heights, PadWalls, with a ship's reach), and whether anything hangs over the spot up to 30 m.
        /// </summary>
        private static (int Walled, bool Roof) Walls(Rasa.Navigation.CoverMesh cover, Vector3 ground)
        {
            var walled = 0;
            for (var k = 0; k < 16; k++)
            {
                var direction = new Vector3(MathF.Cos(k * MathF.PI / 8), 0, MathF.Sin(k * MathF.PI / 8));
                if (new[] { 0.5f, 1.2f, 3f }.Any(h => cover.GeometryBlocks(ground + new Vector3(0, h, 0), ground + new Vector3(0, h, 0) + direction * ShipClear)))
                    walled++;
            }
            return (walled, cover.GeometryBlocks(ground + new Vector3(0, 1, 0), ground + new Vector3(0, 30, 0)));
        }

        /// <summary>
        /// The ships land on open ground: no wall, fence or sandbag line within 8 m of a drop or a relief pad in any
        /// direction and nothing over it (the Marshes' and the Crucible's drops were found in the real client; here the
        /// map's own collision is probed first, as the zone readiness pad-wall scan does). A garrison ship lands on its
        /// pool: at most one direction of 16 walled there (White Oasis Post's yard has trenches and sandbags).
        /// </summary>
        [TestMethod]
        public void EveryShipLandsOnOpenGround()
        {
            var cover = CoverFile();
            var report = new System.Collections.Generic.List<string>();
            foreach (var assault in Content.Assaults)
                foreach (var (what, at) in assault.Drops.Select((d, i) => ($"point {assault.PointId} drop {i + 1}", d.Position)).Append(($"point {assault.PointId} relief pad", assault.ReliefPad.Position)))
                {
                    var (walled, roof) = Walls(cover, Walk(at, what));
                    if (walled > 0 || roof) report.Add($"{what}: {walled} of 16 walled within {ShipClear} m, roof {roof}");
                }
            foreach (var pool in Content.Garrison)
            {
                var (walled, roof) = Walls(cover, Walk(pool.At, $"pool {pool.Id}"));
                if (walled > 1 || roof) report.Add($"pool {pool.Id}: {walled} of 16 walled within {ShipClear} m, roof {roof}");
            }
            Assert.AreEqual(0, report.Count, string.Join("; ", report));
            // The probe sees walls: the White Oasis fence's gate post, a sandbag ring in the yard.
            Assert.IsTrue(Walls(cover, Walk(new Vector3(-441f, 282f, 150f), "fence")).Walled > 0, "the probe meets the fence");
            Assert.IsTrue(Walls(cover, Walk(new Vector3(420f, 271.7f, -70f), "sandbags")).Walled > 2, "the probe meets the Conscription Garrison's sandbags");
        }

        /// <summary>
        /// The garrison stands off the points' own NPCs (the hospitals and token banker, Linier and Fontecilla; their
        /// World DB pools, linked kind 2), and two pools of a point do not stand on each other.
        /// </summary>
        [TestMethod]
        public void TheGarrisonStandsClearOfThePointsNpcsAndOfEachOther()
        {
            var links = _world.World.ControlPointLinkEntries.AsNoTracking()
                .Where(l => l.Kind == 2 && (l.ControlPointId == Content.ConscriptionGarrison || l.ControlPointId == Content.WhiteOasisPost) &&
                    (l.ObjectId < Content.IdMin || l.ObjectId > Content.IdMax)).ToList();
            var pools = _world.World.SpawnPoolEntries.AsNoTracking().Where(p => p.MapContextId == Content.MapId).ToList();
            var npcs = links.Select(l => (l.ControlPointId, Pool: pools.SingleOrDefault(p => p.Id == l.ObjectId)))
                .Where(n => n.Pool != null && !ControlPointTurrets.Spots.Any(s => s.AfsPool == n.Pool.Id)).ToList();
            Assert.AreEqual(4, npcs.Count(n => n.ControlPointId == Content.WhiteOasisPost), "the post's hospital, token banker, Linier and Fontecilla");
            Assert.AreEqual(2, npcs.Count(n => n.ControlPointId == Content.ConscriptionGarrison), "the garrison's hospital and token banker");
            foreach (var pool in Content.Garrison)
            {
                foreach (var npc in npcs.Where(n => n.ControlPointId == pool.Point))
                {
                    var clear = Flat(pool.At, new Vector3((float)npc.Pool.PosX, 0, (float)npc.Pool.PosZ));
                    Assert.IsTrue(clear >= PoolClearOfNpcs, $"pool {pool.Id} stands {clear:0.0} m from the point's NPC pool {npc.Pool.Id}");
                }
                Assert.IsTrue(Flat(pool.At, Point(pool.Point).Position) >= PoolClearOfNpcs, $"pool {pool.Id} off the point itself");
                foreach (var other in Content.Garrison.Where(g => g.Point == pool.Point && g.Id != pool.Id))
                    Assert.IsTrue(Flat(pool.At, other.At) >= 4.5f, $"pools {pool.Id} and {other.Id} apart");
            }
        }

        /// <summary>
        /// White Oasis Post is the quest hub: Linier and Fontecilla (World DB), and with the Ashen quests Fontecilla's
        /// new spot, Meeks and Hale (Content.QuestNpcs). No garrison pool or relief post stands within 12 m of one, no
        /// relief ship lands within 20 m, no Bane ship within 60 m; each spot is a service NPC's of White Oasis Post
        /// (ControlPoints.ServicePointOf: linked, or the nearest point within ServiceNpcRadius), so an assault never
        /// fights one (BehaviorManager.MayFight, LandingZoneAssaultTargeting.IsServiceTarget) and a Bane capture
        /// only hides them until the post is retaken.
        /// </summary>
        [TestMethod]
        public void TheWhiteOasisQuestNpcsAreNeverInAGarrisonOrAttackArea()
        {
            var plan = Content.Assaults.Single(a => a.PointId == Content.WhiteOasisPost);
            var pools = _world.World.SpawnPoolEntries.AsNoTracking().Where(p => p.Id == 510007 || p.Id == 510012).ToList();
            Assert.AreEqual(2, pools.Count, "Linier and Fontecilla on this World DB");
            foreach (var pool in pools)
                Assert.IsTrue(Content.QuestNpcs.Any(n => Vector3.Distance(n.At, pool.Position) < 0.6f), $"pool {pool.Id}'s spot is in Content.QuestNpcs");
            var radius = ControlPoints.Instance.Config.ServiceNpcRadius;
            foreach (var (name, at) in Content.QuestNpcs)
            {
                foreach (var pool in Content.Garrison)
                    Assert.IsTrue(Flat(pool.At, at) >= ClearOfQuestNpcs, $"pool {pool.Id} {Flat(pool.At, at):0.0} m from {name}");
                Assert.IsTrue(Flat(plan.ReliefPost, at) >= ClearOfQuestNpcs, $"the relief post {Flat(plan.ReliefPost, at):0.0} m from {name}");
                Assert.IsTrue(Flat(plan.ReliefPad.Position, at) >= 20f, $"the relief pad {Flat(plan.ReliefPad.Position, at):0.0} m from {name}");
                foreach (var drop in plan.Drops)
                    Assert.IsTrue(Flat(drop.Position, at) >= 60f, $"a Bane drop {Flat(drop.Position, at):0} m from {name}");
                foreach (var other in Content.Assaults.Where(a => a.PointId != Content.WhiteOasisPost))
                    Assert.IsTrue(other.Drops.All(d => Flat(d.Position, at) > 300f), $"the other point's drops far from {name}");
                // A service NPC (Linier: npc_package 1476) standing there is White Oasis Post's.
                Assert.IsTrue(Vector3.Distance(at, Point(Content.WhiteOasisPost).Position) <= radius, $"{name} within {radius} m of the post");
                Assert.AreSame(Point(Content.WhiteOasisPost), ControlPoints.Instance.ServicePointOf(_world.Map, 510007, at), $"{name}: a service NPC there is the post's");
            }

            // An assault on the post: its troops never fight Linier or Fontecilla, while they do fight the garrison.
            var point = Point(Content.WhiteOasisPost);
            _world.SpawnWorld(point.AfsPools.ToArray());
            Deliver();
            var givers = EntityManager.Instance.Creatures.Values.Where(c => c.SpawnPool != null && (c.SpawnPool.DbId == 510007 || c.SpawnPool.DbId == 510012) &&
                CreatureManager.IsLivingOnMap(_world.Map, c)).ToArray();
            Assert.AreEqual(2, givers.Length, "Linier and Fontecilla stand");
            var rifleman = EntityManager.Instance.Creatures.Values.First(c => c.SpawnPool != null && c.DbId == Content.Rifleman &&
                point.AfsPools.Contains(c.SpawnPool.DbId) && CreatureManager.IsLivingOnMap(_world.Map, c));
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
            Deliver();
            var troops = Troops(point.Id);
            Assert.IsTrue(troops.Length > 0);
            foreach (var troop in troops)
            {
                Assert.IsTrue(BehaviorManager.MayFight(troop, rifleman.EntityId), "a Bane troop fights the AFS garrison");
                foreach (var giver in givers)
                    Assert.IsFalse(BehaviorManager.MayFight(troop, giver.EntityId), $"a Bane troop never fights quest giver pool {giver.SpawnPool.DbId}");
            }

            // Taken by the Bane: the givers hidden, not killed; retaken: back.
            ControlPoints.Instance.SetOwner(point, ControlPoints.Bane, null);
            Tick();
            Assert.IsTrue(_world.Map.SpawnPools.Where(p => p.DbId is 510007 or 510012).All(p => p.ServiceSuspended), "hidden while the Bane hold the post");
            ControlPoints.Instance.SetOwner(point, ControlPoints.Afs, null);
            Tick();
            Assert.IsTrue(_world.Map.SpawnPools.Where(p => p.DbId is 510007 or 510012).All(p => !p.ServiceSuspended && !p.Suspended), "back when the AFS retake it");
        }

        /// <summary>
        /// Every NPC of a point's AFS links that is not a turret or a garrison pool of this plan is one of the
        /// point's service NPCs (ControlPoints.ServicePointOf): an assault never counts it as garrison, kills it
        /// or holds it dead. Fontecilla is so by her service link (kind 7).
        /// </summary>
        [TestMethod]
        public void EveryOtherAfsNpcOfThePointsIsAServiceNpcAndNoDefender()
        {
            foreach (var id in new[] { Content.ConscriptionGarrison, Content.WhiteOasisPost })
            {
                var point = Point(id);
                var others = point.AfsPools.Where(p => !Content.Garrison.Any(g => g.Id == p) && !ControlPointTurrets.Spots.Any(s => s.AfsPool == p)).ToArray();
                Assert.IsTrue(others.Length >= 2, $"point {id}");
                _world.SpawnWorld(others);
                foreach (var pool in others)
                {
                    var spawn = _world.Map.SpawnPools.Single(p => p.DbId == pool);
                    Assert.AreSame(point, ControlPoints.Instance.ServicePointOf(_world.Map, spawn), $"point {id}: pool {pool} is a service NPC of the point");
                }
            }
            var links = _world.World.ControlPointLinkEntries.AsNoTracking().ToList();
            foreach (var pool in Content.ServicePools)
                Assert.IsTrue(links.Any(l => l.ControlPointId == Content.WhiteOasisPost && l.Kind == ControlPointLinkEntry.KindServicePool && l.ObjectId == pool), $"service link {pool}");
        }

        // The standing player's six sample points (Cover.StandingPoints): share of height, sideways offset in half-widths.
        private static readonly (float Up, float Side)[] StandingPoints = { (0.95f, 0f), (0.75f, 0f), (0.55f, -1f), (0.55f, 1f), (0.4f, 0f), (0.2f, 0f) };

        /// <summary>
        /// A turret standing at <paramref name="at"/>, as the engine casts its sight (Cover: the eye at 0.9 of a
        /// creature's 1.8 m): how many of eight level rays go more than 1 m out, and how many ground points of
        /// rings 10-60 m round it (24 a ring) it sees any of a standing player's points at (as the Crucible's test).
        /// </summary>
        private static (int Open, int Seen, int Rings) Sight(Rasa.Navigation.CoverMesh cover, Vector3 at)
        {
            var eye = at + new Vector3(0, Cover.CreatureHeight * Cover.EyeFactor, 0);
            var open = Enumerable.Range(0, 8).Count(a =>
                !cover.Blocked(eye, eye + new Vector3(MathF.Cos(a * MathF.PI / 4), 0, MathF.Sin(a * MathF.PI / 4)) * 1.05f));
            int seen = 0, rings = 0;
            for (var r = 10f; r <= 60f; r += 10f)
                for (var a = 0; a < 24; a++)
                {
                    var angle = a * MathF.PI * 2 / 24;
                    float x = eye.X + r * MathF.Cos(angle), z = eye.Z + r * MathF.Sin(angle);
                    var h = cover.TerrainHeight(x, z);
                    if (h == null) continue;
                    rings++;
                    var ground = new Vector3(x, h.Value, z);
                    var to = new Vector3(ground.X - eye.X, 0, ground.Z - eye.Z);
                    var side = Vector3.Normalize(new Vector3(-to.Z, 0, to.X));
                    if (StandingPoints.Any(p => !cover.Blocked(eye, ground + new Vector3(0, Cover.PlayerHeight * p.Up, 0) + side * (Cover.HalfWidth * p.Side))))
                        seen++;
                }
            return (open, seen, rings);
        }

        /// <summary>
        /// Every turret and mortar of the two points sees at least 5% of the ground 10-60 m round it with the map's real
        /// cover mesh, and its eye is not boxed in (at least four of eight rays out past 1 m): the Crucible's Prometheus
        /// Outpost turret that saw nothing could not be killed after a Bane capture (playtest 2026-11-09 row 84).
        /// </summary>
        [TestMethod]
        public void EveryTurretAndMortarOfThePointsSeesTheGroundRoundIt()
        {
            var cover = CoverFile();
            var links = _world.World.ControlPointLinkEntries.AsNoTracking()
                .Where(l => (l.ControlPointId == Content.ConscriptionGarrison || l.ControlPointId == Content.WhiteOasisPost) && (l.Kind == 1 || l.Kind == 2)).ToList();
            var ids = links.Select(l => l.ObjectId).Where(id => id is >= ControlPointTurrets.PoolIdMin and <= ControlPointTurrets.PoolIdMax ||
                BaneMortars.Mortars.Any(m => m.Pool == id)).ToHashSet();
            var pools = _world.World.SpawnPoolEntries.AsNoTracking().Where(p => ids.Contains(p.Id)).ToList();
            Assert.AreEqual(9, pools.Count, "CP 17's two AFS and two Bane turrets, CP 16's five Bane mortars");
            var report = new System.Collections.Generic.List<string>();
            var failed = false;
            foreach (var pool in pools.OrderBy(p => p.Id))
            {
                var (open, seen, rings) = Sight(cover, pool.Position);
                report.Add($"pool {pool.Id} at ({pool.PosX}, {pool.PosY}, {pool.PosZ}): {open} of 8 rays out past 1 m, sees {seen} of {rings} ground points 10-60 m round it");
                failed |= !(open >= 4 && seen >= 0.05 * rings);
            }
            Console.WriteLine(string.Join(Environment.NewLine, report));
            Assert.IsFalse(failed, string.Join("; ", report));
        }

        [TestMethod]
        public void EveryIdIsInTheAshenBlockAndEveryPlanUsesTheAshenDesertsOwnAfsTroops()
        {
            var afs = new[] { Content.Rifleman, Content.MachineGunner, Content.Mech };
            Assert.IsTrue(afs.Concat(Content.Garrison.Select(g => g.Id)).All(id => id is >= Content.IdMin and <= Content.IdMax));
            Assert.AreEqual(Content.Garrison.Length, Content.Garrison.Select(g => g.Id).Distinct().Count());
            Assert.IsFalse(Content.Garrison.Any(g => CrucibleDynamicDropshipsContent.Garrison.Any(p => p.Id == g.Id) || MarshesDynamicDropshipsContent.Garrison.Any(p => p.Id == g.Id) ||
                PoolsDynamicDropshipsContent.Garrison.Any(p => p.Id == g.Id) || PlateauDynamicDropshipsContent.Garrison.Any(p => p.Id == g.Id)));
            foreach (var assault in Content.Assaults)
            {
                Assert.IsTrue(assault.Relief.Concat(assault.Sortie).All(t => afs.Contains(t.Creature)), $"point {assault.PointId}");
                Assert.AreSame(assault, DynamicDropshipsContent.Assaults.Single(a => a.MapId == Content.MapId && a.PointId == assault.PointId));
            }
            foreach (var id in afs.Concat(new[] { Content.ThraxSoldier, Content.ThraxGrenadier, Content.ThraxTechnician, Content.Caretaker, Content.ThraxCommander }))
                Assert.IsTrue(_world.World.CreatureEntries.Any(c => c.Id == id), $"creature {id}");
        }

        /// <summary>The waves are 2 levels over the Ashen Desert's own Bane (531008-531011, 39-40), the commander 2 over its commander 531012 (42).</summary>
        [TestMethod]
        public void TheWavesAreTwoLevelsOverTheAshenDesertsBaneAndTheAfsTroopsTheMarshesTimesTheAbyssOverTheMires()
        {
            var creatures = _world.World.CreatureEntries.AsNoTracking().ToDictionary(c => c.Id);
            var actions = _world.World.CreatureActionEntries.AsNoTracking().ToDictionary(a => a.Id);
            Assert.AreEqual(2, (int)creatures[Content.ThraxSoldier].Level - (int)creatures[531008].Level, "Thrax Soldier over the Ashen Desert's");
            Assert.AreEqual(2, (int)creatures[Content.ThraxCommander].Level - (int)creatures[531012].Level, "Thrax Commander over the Ashen Desert's");
            var ratio = (double)creatures[Content.ThraxSoldier].MaxHitPoints / creatures[MarshesDynamicDropshipsContent.ThraxSoldier].MaxHitPoints;
            Assert.AreEqual(1.54, ratio, 0.01, "the Abyss' Thrax over the Mires'");
            var levels = (int)creatures[Content.ThraxSoldier].Level - (int)creatures[MarshesDynamicDropshipsContent.ThraxSoldier].Level;
            foreach (var (ashen, marshes) in new[] { (Content.Rifleman, MarshesDynamicDropshipsContent.Rifleman),
                (Content.MachineGunner, MarshesDynamicDropshipsContent.MachineGunner), (Content.Mech, MarshesDynamicDropshipsContent.Mech) })
            {
                Assert.AreEqual((uint)Math.Round(creatures[marshes].MaxHitPoints * 1.54), creatures[ashen].MaxHitPoints, $"creature {ashen} health");
                Assert.AreEqual((int)creatures[marshes].Level + levels, (int)creatures[ashen].Level, $"creature {ashen} level");
                Assert.AreEqual(1u, creatures[ashen].Faction, "AFS");
                Assert.AreEqual((uint)Math.Round(actions[marshes].MinDamage * 1.54), actions[ashen].MinDamage, $"action {ashen}");
                Assert.AreEqual((uint)Math.Round(actions[marshes].MaxDamage * 1.54), actions[ashen].MaxDamage, $"action {ashen}");
            }
        }

        [TestMethod]
        public void WhiteOasisHasAGarrisonThatComesBackByAfsDropshipAndTheBaneGarrisonNoneYet()
        {
            void Has(uint id, int pools, int riflemen, int gunners, int mechs, bool suspended)
            {
                var spawned = _world.Map.SpawnPools.Where(p => Point(id).AfsPools.Contains(p.DbId) && Content.Garrison.Any(g => g.Id == p.DbId)).ToArray();
                Assert.AreEqual(pools, spawned.Length, $"point {id}");
                Assert.AreEqual(riflemen, Content.Garrison.Where(g => g.Point == id && g.Troop.Creature == Content.Rifleman).Sum(g => g.Troop.Max), $"point {id}");
                Assert.AreEqual(gunners, Content.Garrison.Where(g => g.Point == id && g.Troop.Creature == Content.MachineGunner).Sum(g => g.Troop.Max), $"point {id}");
                Assert.AreEqual(mechs, Content.Garrison.Where(g => g.Point == id && g.Troop.Creature == Content.Mech).Sum(g => g.Troop.Max), $"point {id}");
                Assert.IsTrue(spawned.All(p => p.AnimType == 2 && p.IsGarrison && p.Suspended == suspended), $"point {id}");
            }
            Has(Content.WhiteOasisPost, 7, 8, 2, 2, false);         // two AFS turrets
            Has(Content.ConscriptionGarrison, 10, 10, 2, 4, true);  // no AFS turrets; Bane at the start
        }

        [TestMethod]
        public void WhiteOasisIsAttackedInThreeWavesOfTwoShipsWithTheCommanderInTheLast()
        {
            var oasis = Content.Assaults.Single(a => a.PointId == Content.WhiteOasisPost);
            Assert.AreEqual(2700, oasis.PeriodSeconds, "the quest hub: Prometheus Outpost's period");
            _now += oasis.FirstDelaySeconds * 1000L - 1;
            Tick();
            Assert.AreEqual(0, Assaults(Content.WhiteOasisPost).Length);
            _now++;
            Tick();
            Assert.AreEqual(2, Assaults(Content.WhiteOasisPost).Length);
            Assert.AreEqual(0, Assaults(Content.ConscriptionGarrison).Length, "no assault on a point the Bane hold");
            Deliver();
            _now += oasis.WaveSeconds * 1000L;
            Tick();
            _now += oasis.WaveSeconds * 1000L;
            Tick();
            Deliver();
            Assert.AreEqual(6, Assaults(Content.WhiteOasisPost).Length);
            var troops = Troops(Content.WhiteOasisPost);
            Assert.AreEqual(1, troops.Count(c => c.DbId == Content.ThraxCommander));
            Assert.IsTrue(troops.Length is >= 28 and <= 33, $"{troops.Length} Bane in the first assault");
            Assert.IsTrue(troops.All(c => c.Level is >= 41 and <= 44), "the Abyss' Thrax (41-42) and its commander (44)");
            _now += oasis.ReliefSeconds * 1000L - 2L * oasis.WaveSeconds * 1000;
            Tick();
            Assert.AreEqual(1, Relief(Content.WhiteOasisPost).Length, "the AFS relief ship after the last wave");
            Deliver();
            var relief = EntityManager.Instance.Creatures.Values.Where(c => Relief(Content.WhiteOasisPost).Contains(c.SpawnPool)).ToArray();
            Assert.AreEqual(4, relief.Length);
            Assert.IsTrue(relief.All(c => c.DbId is Content.Rifleman or Content.MachineGunner));
        }

        [TestMethod]
        public void TheCommanderTakesWhiteOasisWhenItsDefendersAreDeadAndThenAfsSquadsCome()
        {
            var point = Point(Content.WhiteOasisPost);
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
            _now += 360000;
            Tick();
            Deliver();
            var defenders = _world.Map.SpawnPools.Where(p => point.AfsPools.Contains(p.DbId) || p.IsControlPointReinforcement).ToList();
            ControlPoints.TakeOffLiving(_world.Map, defenders);
            foreach (var pool in defenders)
            {
                pool.Suspended |= pool.IsControlPointReinforcement;
                pool.HasSpawned = true; pool.AliveCreatures = 0; pool.QueuedCreatures = 0; pool.DropshipQueue = 0;
            }
            var commander = Troops(point.Id).Single(c => c.DbId == Content.ThraxCommander);
            commander.Position = Walk(point.Position, "point");
            Tick();
            Assert.AreSame(commander, point.Object.UsedBy);
            _now += 10000;
            Tick();
            Assert.AreEqual(ControlPoints.Bane, point.Owner);
            Assert.AreEqual(0, Assaults(point.Id).Length);

            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            _now += plan.SortieFirstDelaySeconds * 1000L;
            Tick();
            Assert.AreEqual(1, DynamicObjectManager.Instance.Dropships.Values.Count(s => s.SpawnPool?.AssaultPointId == point.Id && s.SpawnPool.AnimType == 2),
                "an AFS sortie ship on the fallen White Oasis Post");
        }

        [TestMethod]
        public void AWaveTakesOnTheGarrisonBeforeTheTurretsAndKeepsThePointAsItsHome()
        {
            var point = Point(Content.WhiteOasisPost);
            _world.SpawnWorld(point.AfsPools.ToArray());
            Deliver();
            var turrets = EntityManager.Instance.Creatures.Values.Where(c => c.SpawnPool != null && point.AfsPools.Contains(c.SpawnPool.DbId) &&
                Emplacements.Is(c) && CreatureManager.IsLivingOnMap(_world.Map, c)).ToArray();
            Assert.IsTrue(turrets.Length > 0, "the point's AFS turrets stand");
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
            Deliver();
            Tick();
            var troops = Troops(point.Id);
            Assert.IsTrue(troops.Length > 0);
            var onTurrets = troops.Where(c => c.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting &&
                turrets.Any(t => t.EntityId == c.Controller.ActionFighting.TargetEntityId)).ToArray();
            Assert.AreEqual(0, onTurrets.Length, $"{onTurrets.Length} of {troops.Length} troops on a turret while the AFS troops stand");
            Assert.IsTrue(troops.All(c => Flat(c.HomePos.Position, point.Position) < 15f), "an assault troop's home is its point (or its spot about it), not its drop");
        }

        private Creature[] Garrison(ControlPoints.Point point) => EntityManager.Instance.Creatures.Values
            .Where(c => c.SpawnPool != null && point.AfsPools.Contains(c.SpawnPool.DbId) && ControlPoints.Instance.ServicePointOf(_world.Map, c.SpawnPool) == null &&
                CreatureManager.IsLivingOnMap(_world.Map, c)).ToArray();

        private void Kill(Creature creature)
        {
            creature.Attributes[Attributes.Health].Current = 0;
            CreatureManager.Instance.HandleCreatureKill(_world.Map, creature, null);
        }

        [TestMethod]
        public void NoAfsGarrisonComesBackAndNoTurretIsRepairedWhileAnAssaultRuns()
        {
            var point = Point(Content.WhiteOasisPost);
            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            _world.SpawnWorld(point.AfsPools.ToArray());
            Deliver();
            var garrison = Garrison(point);
            Assert.IsTrue(garrison.Any(Emplacements.Is) && garrison.Any(c => !Emplacements.Is(c)), "turrets and troops stand");
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
            Deliver();
            foreach (var creature in garrison) Kill(creature);
            Assert.AreEqual(ControlPoints.Garrison.Down, ControlPoints.Instance.GarrisonOf(_world.Map, point, ControlPoints.Afs));
            for (var i = 0; i < 4; i++) { SpawnPoolManager.Instance.SpawnPoolWorker(_world.Map, 300000); Deliver(); }
            Assert.AreEqual(0, Garrison(point).Length, "no AFS soldier set down again, no turret repaired");
            ControlPoints.TakeOffLiving(_world.Map, Assaults(point.Id).ToList());
            _now += plan.LifetimeSeconds * 1000L;
            Tick();
            Assert.AreEqual(0, Assaults(point.Id).Length, "the assault is over");
            Assert.AreEqual(ControlPoints.Afs, point.Owner);
            for (var i = 0; i < 4; i++) { SpawnPoolManager.Instance.SpawnPoolWorker(_world.Map, 300000); Deliver(); }
            var back = Garrison(point);
            Assert.IsTrue(back.Any(c => !Emplacements.Is(c)), "the AFS troops are back after the assault");
            Assert.IsTrue(garrison.Where(Emplacements.Is).All(t => back.Contains(t)), "the turrets are repaired after the assault");
        }

        [TestMethod]
        public void TheReliefShipComesOnlyWhileTheGarrisonHolds()
        {
            var point = Point(Content.WhiteOasisPost);
            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            _world.SpawnWorld(point.AfsPools.ToArray());
            Deliver();
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
            foreach (var creature in Garrison(point)) Kill(creature);
            _now += plan.ReliefSeconds * 1000L;
            Tick();
            Assert.AreEqual(0, Relief(point.Id).Length, "no relief ship for a garrison that has fallen");
        }

        /// <summary>The garrison down, the Bane give up a service NPC and make for the point; the claim done, the post is the Bane's.</summary>
        [TestMethod]
        public void WithTheGarrisonDownTheBaneGoToThePointAndTheNextClaimantTakesIt()
        {
            var point = Point(Content.WhiteOasisPost);
            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            _world.SpawnWorld(point.AfsPools.ToArray());
            Deliver();
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
            _now += 2L * plan.WaveSeconds * 1000;
            Tick();
            Deliver();
            var giver = EntityManager.Instance.Creatures.Values.Single(c => c.SpawnPool?.DbId == 510007 && CreatureManager.IsLivingOnMap(_world.Map, c));
            foreach (var creature in Garrison(point)) Kill(creature);
            var troops = Troops(point.Id);
            var commander = troops.Single(c => c.DbId == Content.ThraxCommander);
            var soldier = troops.First(c => c.DbId == Content.ThraxSoldier && Flat(c.Position, point.Position) > LandingZoneAssaults.RallyReach);
            soldier.Controller.CurrentAction = BehaviorManager.BehaviorActionFighting;
            soldier.Controller.ActionFighting.TargetEntityId = giver.EntityId;
            Tick();
            Assert.IsTrue(commander.OnClaimDuty, "the commander goes for the point");
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, soldier.Controller.CurrentAction, "the soldier gives up Linier and makes for the point");
            Kill(commander);
            Tick();
            var next = Troops(point.Id).SingleOrDefault(c => c.OnClaimDuty);
            Assert.IsNotNull(next, "the next claimant is chosen");
            next.Position = Walk(point.Position, "point");
            Tick();
            Assert.AreSame(next, point.Object.UsedBy, "the claim begins");
            _now += Math.Max(1, ControlPoints.Instance.Config.LandingZoneAssaultCaptureSeconds) * 1000L;
            Tick();
            Assert.AreEqual(ControlPoints.Bane, point.Owner, "the claim done, the post is the Bane's");
            Assert.IsTrue(CreatureManager.IsLivingOnMap(_world.Map, giver) || _world.Map.SpawnPools.Single(p => p.DbId == 510007).ServiceSuspended,
                "Linier is not killed: he stands, or is hidden while the Bane hold the post");
        }

        [TestMethod]
        public void AnAfsSquadAttacksConscriptionGarrisonButCannotTakeIt()
        {
            var point = Point(Content.ConscriptionGarrison);
            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            Tick();
            _now += plan.SortieFirstDelaySeconds * 1000L;
            Tick();
            var ship = DynamicObjectManager.Instance.Dropships.Values.Single(s => s.SpawnPool?.AssaultPointId == point.Id);
            Assert.AreEqual(2, ship.SpawnPool.AnimType, "an AFS ship");
            Deliver();
            var squad = AfsSorties.Squad(_world.Map, point).ToArray();
            Assert.AreEqual(1, squad.Count(c => c.DbId == Content.Mech));
            Assert.AreEqual(4, squad.Count(c => c.DbId == Content.Rifleman), "4 riflemen");
            Assert.AreEqual(1, squad.Count(c => c.DbId == Content.MachineGunner), "and a machine gunner (ESTIMATE, one over retail's 3-4)");
            ControlPoints.TakeOffLiving(_world.Map, _world.Map.SpawnPools.Where(p => point.BanePools.Contains(p.DbId)).ToList());
            foreach (var trooper in squad) trooper.Position = Walk(point.Position, "point");
            _now += 30000;
            Tick();
            Assert.AreEqual(ControlPoints.Bane, point.Owner, "no claim by the squad");
        }

        [TestMethod]
        public void ACapturedConscriptionGarrisonGetsItsAfsGarrisonAndACounterAttack()
        {
            var point = Point(Content.ConscriptionGarrison);
            ControlPoints.Instance.SetOwner(point, ControlPoints.Afs, null);
            Tick();
            var garrison = _world.Map.SpawnPools.Where(p => Content.Garrison.Any(g => g.Id == p.DbId && g.Point == point.Id)).ToArray();
            Assert.AreEqual(10, garrison.Length);
            Assert.IsTrue(garrison.All(p => !p.Suspended && p.AnimType == 2));
            Assert.IsTrue(_world.Map.SpawnPools.Where(p => point.BanePools.Contains(p.DbId)).All(p => p.Suspended), "no Bane garrison or mortars while the AFS hold it");
            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            _now += plan.FirstDelaySeconds * 1000L - 1;
            Tick();
            Assert.AreEqual(0, Assaults(point.Id).Length);
            _now++;
            Tick();
            Assert.AreEqual(2, Assaults(point.Id).Length, "the Bane counter-attack after a delay");
            Assert.AreEqual(0, Assaults(Content.WhiteOasisPost).Length, "White Oasis Post has its own timer");
        }

        [TestMethod]
        public void EachAssaultHeldOffMakesTheNextBigger()
        {
            var point = Point(Content.WhiteOasisPost);
            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            int Soldiers(int repelled)
            {
                Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
                Deliver();
                Assert.AreEqual(repelled, LandingZoneAssaults.Repelled(_world.Map, point));
                var soldiers = Troops(point.Id).Count(c => c.DbId == Content.ThraxSoldier);
                _now += plan.LifetimeSeconds * 1000L;
                Tick();
                Assert.AreEqual(ControlPoints.Afs, point.Owner, "held off");
                return soldiers;
            }
            Assert.IsTrue(Soldiers(0) is >= 6 and <= 7);
            Assert.IsTrue(Soldiers(1) is >= 12 and <= 14, "twice after one held off (MaxEscalation 1)");
            Assert.IsTrue(Soldiers(2) is >= 12 and <= 14, "and no more after two");
        }

        [TestMethod]
        public void TheMigrationAddsTheRowsAndItsDownTakesThemOut()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var database = Path.Combine(directory, "world");
                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());
                using var world = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database);
                void Rows(int creatures, int pools, int service, string when)
                {
                    Assert.AreEqual(creatures, world.CreatureEntries.AsNoTracking().Count(r => r.Id >= Content.IdMin && r.Id <= Content.IdMax), $"creatures {when}");
                    Assert.AreEqual(creatures, world.CreatureActionEntries.AsNoTracking().Count(r => r.Id >= Content.IdMin && r.Id <= Content.IdMax), $"actions {when}");
                    Assert.AreEqual(pools, world.SpawnPoolEntries.AsNoTracking().Count(r => r.Id >= Content.IdMin && r.Id <= Content.IdMax), $"pools {when}");
                    Assert.AreEqual(pools, world.ControlPointLinkEntries.AsNoTracking().Count(r => r.ObjectId >= Content.IdMin && r.ObjectId <= Content.IdMax), $"links {when}");
                    Assert.AreEqual(service, world.ControlPointLinkEntries.AsNoTracking().Count(l => l.ControlPointId == Content.WhiteOasisPost && l.Kind == 7), $"service links {when}");
                }
                Rows(3, 17, 1, "after");
                Assert.AreEqual(7, world.ControlPointLinkEntries.AsNoTracking().Count(r => r.ControlPointId == Content.WhiteOasisPost && r.Kind == 2 && r.ObjectId >= Content.IdMin && r.ObjectId <= Content.IdMax));
                Assert.AreEqual(10, world.ControlPointLinkEntries.AsNoTracking().Count(r => r.ControlPointId == Content.ConscriptionGarrison && r.Kind == 2 && r.ObjectId >= Content.IdMin && r.ObjectId <= Content.IdMax));
                var before = DynamicDropshipsMigrations.Previous(world, Migration);
                world.GetService<IMigrator>().Migrate(before);
                Rows(0, 0, 0, "down");
                Assert.IsTrue(world.SpawnPoolEntries.AsNoTracking().Any(p => p.Id == 510012), "Fontecilla's own pool stays");
                Assert.AreEqual(17, world.SpawnPoolEntries.AsNoTracking().Count(r => r.Id >= CrucibleDynamicDropshipsContent.IdMin && r.Id <= CrucibleDynamicDropshipsContent.IdMax), "the Crucible's rows stay");
                world.GetService<IMigrator>().Migrate(Migration);
                Rows(3, 17, 1, "up again");
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }
    }
}
