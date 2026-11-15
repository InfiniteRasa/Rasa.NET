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
using Content = Rasa.Services.Preloader.CrucibleDynamicDropshipsContent;

namespace Rasa.Test.World
{
    /// <summary>Ligo Crucible's control points: AFS garrisons by dropship, timed Bane assaults, AFS relief and sorties.</summary>
    [TestClass, DoNotParallelize]
    public class CrucibleDynamicDropshipsTests
    {
        private const string Migration = "20271109000000_Crucible_dynamic_dropships";
        private const string MapName = "adv_arieki_ligo_burningsteps";

        /// <summary>How far, across the ground, a Bane drop lands from each of its point's turrets at least (ESTIMATE, as the Marshes').</summary>
        private const float DropClearOfTurrets = 30f;

        /// <summary>How far a drop lands from a Bane mortar mount at least (ESTIMATE): the mounts stand off the navmesh, as the turrets'.</summary>
        private const float DropClearOfMortars = 20f;

        /// <summary>How far a garrison pool stands from the point's own NPCs (quest givers, vendors) at least (ESTIMATE).</summary>
        private const float PoolClearOfNpcs = 8f;

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
                db.ControlPoints.GetLinks().Where(l => l.ControlPointId is Content.FortIntrepid or Content.PrometheusOutpost), null);
            ControlPoints.Instance.Place(_world.Map);
        }

        [TestCleanup]
        public void End()
        {
            ControlPoints.Instance.Config.ControlPointAssaultEnabled = false;
            foreach (var id in new[] { Content.FortIntrepid, Content.PrometheusOutpost })
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

        /// <summary>A whole path from <paramref name="from"/> to the point, none of it in the Crucible's lava.</summary>
        private void Reaches(Vector3 from, uint point, string what)
        {
            var start = Walk(from, what);
            var path = NavMeshManager.FindPathBounded(_world.Map, start, Walk(Point(point).Position, $"point {point}"), out var complete);
            Assert.IsTrue(complete && path?.Count > 0, $"{what} must reach point {point}");
            Assert.IsFalse(LavaDamage.InLava(MapName, start), $"{what} stands in the lava");
            var previous = start;
            foreach (var corner in path)
            {
                for (var t = 0f; t <= 1f; t += 0.05f)
                    Assert.IsFalse(LavaDamage.InLava(MapName, Vector3.Lerp(previous, corner, t)), $"{what}: the way to point {point} goes through the lava");
                previous = corner;
            }
        }

        [TestMethod]
        public void TheCrucibleHasTwoControlPointsFortIntrepidAndPrometheusOutpostBothAfs()
        {
            Assert.AreEqual(2, ControlPoints.Instance.Points.Count(p => p.MapContextId == Content.MapId));
            Assert.AreEqual(ControlPoints.Afs, Point(Content.FortIntrepid).Owner);
            Assert.AreEqual(ControlPoints.Afs, Point(Content.PrometheusOutpost).Owner);
            Assert.AreEqual(2, Content.Assaults.Length);
            Assert.IsTrue(Content.Assaults.All(a => a.Sortie.Length > 0), "each point has a sortie plan for when the Bane take it");
            var fort = Content.Assaults.Single(a => a.PointId == Content.FortIntrepid);
            var outpost = Content.Assaults.Single(a => a.PointId == Content.PrometheusOutpost);
            Assert.IsTrue(outpost.PeriodSeconds > fort.PeriodSeconds, "the quest hub is attacked less often than the contested fort");
            Assert.IsTrue(Math.Abs(outpost.FirstDelaySeconds - fort.FirstDelaySeconds) >= 600, "the two points are not attacked at once");
        }

        [TestMethod]
        public void EveryDropPadReliefPadAndGarrisonPostStandsOnGroundThatReachesItsPoint()
        {
            foreach (var assault in Content.Assaults)
            {
                Assert.AreEqual(Content.MapId, assault.MapId, $"point {assault.PointId}");
                Assert.AreEqual(2, assault.Drops.Length, $"point {assault.PointId}: two Bane ships a wave");
                for (var i = 0; i < assault.Drops.Length; i++)
                {
                    var drop = assault.Drops[i].Position;
                    Assert.IsTrue(Vector3.Distance(Walk(drop, "drop"), drop) < 2f, $"point {assault.PointId} drop {i + 1} on the ground");
                    Reaches(drop, assault.PointId, $"point {assault.PointId} drop {i + 1}");
                    var distance = Vector3.Distance(drop, Point(assault.PointId).Position);
                    Assert.IsTrue(distance is > 45 and < 100, $"point {assault.PointId} drop {i + 1} lands outside the walls, {distance:0} m");
                    foreach (var turret in ControlPointTurrets.Spots.Where(s => s.Point == assault.PointId))
                    {
                        var clear = new Vector2(drop.X - (float)turret.X, drop.Z - (float)turret.Z).Length();
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
                Assert.IsTrue(Vector3.Distance(pool.At, Point(pool.Point).Position) < 60, $"pool {pool.Id} inside its point (the garrison posts are at most about 52 m out)");
            }
        }

        /// <summary>
        /// The garrison stands off the point's own NPCs (Prometheus Outpost's quest givers and vendors, Fort
        /// Intrepid's medic and token banker; their World DB pools, linked kind 2), and two pools of a point
        /// do not stand on each other.
        /// </summary>
        [TestMethod]
        public void TheGarrisonStandsClearOfThePointsNpcsAndOfEachOther()
        {
            var links = _world.World.ControlPointLinkEntries.AsNoTracking()
                .Where(l => l.Kind == 2 && (l.ControlPointId == Content.FortIntrepid || l.ControlPointId == Content.PrometheusOutpost) &&
                    (l.ObjectId < Content.IdMin || l.ObjectId > Content.IdMax)).ToList();
            var pools = _world.World.SpawnPoolEntries.AsNoTracking().Where(p => p.MapContextId == Content.MapId).ToList();
            var npcs = links.Select(l => (l.ControlPointId, Pool: pools.SingleOrDefault(p => p.Id == l.ObjectId)))
                .Where(n => n.Pool != null && !ControlPointTurrets.Spots.Any(s => s.AfsPool == n.Pool.Id)).ToList();
            Assert.IsTrue(npcs.Count(n => n.ControlPointId == Content.PrometheusOutpost) >= 7, "the outpost's quest givers and vendors");
            Assert.IsTrue(npcs.Count(n => n.ControlPointId == Content.FortIntrepid) >= 2, "the fort's medic and token banker");
            // The fort's Thrax Commander (on the point) and Executioner Derge (moved to the rampart).
            npcs.Add((Content.FortIntrepid, pools.Single(p => p.Id == Content.CommanderPool)));
            var derge = pools.Single(p => p.Id == Content.DergePool);
            Assert.IsTrue(Vector3.Distance(Content.DergeAt, derge.Position) < 0.01f, "Derge on the rampart after Up");
            npcs.Add((Content.FortIntrepid, derge));
            var fort = Content.Assaults.Single(a => a.PointId == Content.FortIntrepid);
            foreach (var place in Content.Garrison.Where(g => g.Point == Content.FortIntrepid).Select(g => g.At)
                .Concat(fort.Drops.Select(d => d.Position)).Append(fort.ReliefPad.Position).Append(fort.ReliefPost))
                Assert.IsTrue(Vector3.Distance(place, Content.DergeAt) >= 30f, $"({place.X:0}, {place.Z:0}) 30 m and more from Derge (an AFS reach of 18 m and a margin)");
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
        /// Every NPC of a point's AFS links that is not a turret or a garrison pool of this plan is one of the
        /// point's service NPCs (ControlPoints.ServicePointOf): an assault never counts it as garrison, kills it
        /// or holds it dead. Tiersky and Madias are so by their service links (kind 7).
        /// </summary>
        [TestMethod]
        public void EveryOtherAfsNpcOfThePointsIsAServiceNpcAndNoDefender()
        {
            foreach (var id in new[] { Content.FortIntrepid, Content.PrometheusOutpost })
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
                Assert.IsTrue(links.Any(l => l.ControlPointId == Content.PrometheusOutpost && l.Kind == ControlPointLinkEntry.KindServicePool && l.ObjectId == pool), $"service link {pool}");
            Assert.IsTrue(links.Any(l => l.ControlPointId == Content.FortIntrepid && l.Kind == ControlPointLinkEntry.KindBanePool && l.ObjectId == Content.CommanderPool),
                "the fort's Thrax Commander is its Bane garrison");
            Assert.IsTrue(Point(Content.FortIntrepid).BanePools.Contains(Content.CommanderPool));
            Assert.IsTrue(_world.Map.SpawnPools.Single(p => p.DbId == Content.CommanderPool).Suspended, "no Thrax Commander while the AFS hold the fort");
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;
            return directory?.FullName ?? AppContext.BaseDirectory;
        }

        // The standing player's six sample points (Cover.StandingPoints): share of height, sideways offset in half-widths.
        private static readonly (float Up, float Side)[] StandingPoints = { (0.95f, 0f), (0.75f, 0f), (0.55f, -1f), (0.55f, 1f), (0.4f, 0f), (0.2f, 0f) };

        /// <summary>The probe passes: at least four of the eight rays out, at least 5% of the ground seen.</summary>
        private static bool Clear(int open, int seen, int rings) => open >= 4 && seen >= 0.05 * rings;

        /// <summary>
        /// A turret standing at <paramref name="at"/>, as the engine casts its sight (Cover: the eye at 0.9 of a
        /// creature's 1.8 m): how many of eight level rays go more than 1 m out, and how many ground points of
        /// rings 10-60 m round it (24 a ring) it sees any of a standing player's points at.
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
        /// Playtest 2026-11-09 row 84: the Bane turret of Prometheus Outpost's spot (416.27, 177.40, 339.56)
        /// saw nothing (its eye in a closed box of the cover mesh), so it gave up every fight and healed 10%
        /// a second out of one: it could not be killed. Every turret and mortar of the two points sees at
        /// least 5% of the ground 10-60 m round it with the map's real cover mesh, as the engine casts it
        /// (Cover: the eye at 0.9 of the creature's height, the standing player's six points), and its eye
        /// is not boxed in: rays go more than 1 m out in at least four of eight directions (the old spot's
        /// stopped after 0.2-0.5 m in every direction; a turret against a wall loses a few).
        /// </summary>
        [TestMethod]
        public void EveryTurretAndMortarOfThePointsSeesTheGroundRoundIt()
        {
            var cover = Rasa.Navigation.CoverMesh.Read(Rasa.Navigation.CoverMesh.PathFor(Path.Combine(RepositoryRoot(), "navmesh"), MapName));
            var links = _world.World.ControlPointLinkEntries.AsNoTracking()
                .Where(l => (l.ControlPointId == Content.FortIntrepid || l.ControlPointId == Content.PrometheusOutpost) && (l.Kind == 1 || l.Kind == 2)).ToList();
            var ids = links.Select(l => l.ObjectId).Where(id => id is >= ControlPointTurrets.PoolIdMin and <= ControlPointTurrets.PoolIdMax ||
                BaneMortars.Mortars.Any(m => m.Pool == id)).ToHashSet();
            var pools = _world.World.SpawnPoolEntries.AsNoTracking().Where(p => ids.Contains(p.Id)).ToList();
            Assert.AreEqual(12, pools.Count, "CP 31's four AFS and four Bane turrets, CP 30's four Bane mortars");
            var report = new System.Collections.Generic.List<string>();
            var failed = false;
            foreach (var pool in pools.OrderBy(p => p.Id))
            {
                var (open, seen, rings) = Sight(cover, pool.Position);
                report.Add($"pool {pool.Id} at ({pool.PosX}, {pool.PosY}, {pool.PosZ}): {open} of 8 rays out past 1 m, sees {seen} of {rings} ground points 10-60 m round it");
                failed |= !Clear(open, seen, rings);
            }
            // The same probe at the spot the turret left: boxed in and blind, so the move is what passes it.
            var (wasOpen, wasSeen, wasRings) = Sight(cover, Content.BoxedTurretWas);
            Assert.IsFalse(Clear(wasOpen, wasSeen, wasRings), $"the old spot: {wasOpen} of 8 rays out, sees {wasSeen} of {wasRings}");
            Assert.IsTrue(wasOpen < 4, $"the old spot is boxed in: {wasOpen} of 8 rays out past 1 m");
            Assert.IsTrue(wasSeen < 0.05 * wasRings, $"the old spot is blind: sees {wasSeen} of {wasRings}");
            Console.WriteLine(string.Join(Environment.NewLine, report));
            Assert.IsFalse(failed, string.Join("; ", report));
        }

        [TestMethod]
        public void EveryIdIsInTheCrucibleBlockAndEveryPlanUsesTheCruciblesOwnAfsTroops()
        {
            var afs = new[] { Content.Rifleman, Content.MachineGunner, Content.Mech };
            Assert.IsTrue(afs.Concat(Content.Garrison.Select(g => g.Id)).All(id => id is >= Content.IdMin and <= Content.IdMax));
            Assert.AreEqual(Content.Garrison.Length, Content.Garrison.Select(g => g.Id).Distinct().Count());
            Assert.IsFalse(Content.Garrison.Any(g => MarshesDynamicDropshipsContent.Garrison.Any(p => p.Id == g.Id) ||
                PoolsDynamicDropshipsContent.Garrison.Any(p => p.Id == g.Id) || PlateauDynamicDropshipsContent.Garrison.Any(p => p.Id == g.Id)));
            foreach (var assault in Content.Assaults)
            {
                Assert.IsTrue(assault.Relief.Concat(assault.Sortie).All(t => afs.Contains(t.Creature)), $"point {assault.PointId}");
                Assert.AreSame(assault, DynamicDropshipsContent.Assaults.Single(a => a.MapId == Content.MapId && a.PointId == assault.PointId));
            }
            foreach (var id in afs.Concat(new[] { Content.ThraxSoldier, Content.ThraxGrenadier, Content.ThraxTechnician, Content.Caretaker, Content.ThraxCommander }))
                Assert.IsTrue(_world.World.CreatureEntries.Any(c => c.Id == id), $"creature {id}");
        }

        [TestMethod]
        public void TheCrucibleAfsTroopsAreTheMarshesTimesTheHowlingMawOverTheMiresThrax()
        {
            var creatures = _world.World.CreatureEntries.AsNoTracking().ToDictionary(c => c.Id);
            var actions = _world.World.CreatureActionEntries.AsNoTracking().ToDictionary(a => a.Id);
            // The waves' Thrax Soldier over the Marshes' waves' (the Mires'): 8179 / 4460, about 1.83.
            var ratio = (double)creatures[Content.ThraxSoldier].MaxHitPoints / creatures[MarshesDynamicDropshipsContent.ThraxSoldier].MaxHitPoints;
            Assert.AreEqual(1.83, ratio, 0.01, "the Howling Maw's Thrax over the Mires'");
            var levels = (int)creatures[Content.ThraxSoldier].Level - (int)creatures[MarshesDynamicDropshipsContent.ThraxSoldier].Level;
            foreach (var (crucible, marshes) in new[] { (Content.Rifleman, MarshesDynamicDropshipsContent.Rifleman),
                (Content.MachineGunner, MarshesDynamicDropshipsContent.MachineGunner), (Content.Mech, MarshesDynamicDropshipsContent.Mech) })
            {
                Assert.AreEqual((uint)Math.Round(creatures[marshes].MaxHitPoints * 1.83), creatures[crucible].MaxHitPoints, $"creature {crucible} health");
                Assert.AreEqual((int)creatures[marshes].Level + levels, (int)creatures[crucible].Level, $"creature {crucible} level");
                Assert.AreEqual(1u, creatures[crucible].Faction, "AFS");
                Assert.AreEqual((uint)Math.Round(actions[marshes].MinDamage * 1.83), actions[crucible].MinDamage, $"action {crucible}");
                Assert.AreEqual((uint)Math.Round(actions[marshes].MaxDamage * 1.83), actions[crucible].MaxDamage, $"action {crucible}");
            }
        }

        [TestMethod]
        public void BothPointsHaveAGarrisonThatComesBackByAfsDropship()
        {
            void Has(uint id, int pools, int riflemen, int gunners, int mechs)
            {
                var spawned = _world.Map.SpawnPools.Where(p => Point(id).AfsPools.Contains(p.DbId) && Content.Garrison.Any(g => g.Id == p.DbId)).ToArray();
                Assert.AreEqual(pools, spawned.Length, $"point {id}");
                Assert.AreEqual(riflemen, Content.Garrison.Where(g => g.Point == id && g.Troop.Creature == Content.Rifleman).Sum(g => g.Troop.Max), $"point {id}");
                Assert.AreEqual(gunners, Content.Garrison.Where(g => g.Point == id && g.Troop.Creature == Content.MachineGunner).Sum(g => g.Troop.Max), $"point {id}");
                Assert.AreEqual(mechs, Content.Garrison.Where(g => g.Point == id && g.Troop.Creature == Content.Mech).Sum(g => g.Troop.Max), $"point {id}");
                Assert.IsTrue(spawned.All(p => p.AnimType == 2 && p.IsGarrison && !p.Suspended), $"point {id}");
            }
            Has(Content.FortIntrepid, 10, 10, 2, 4);        // no AFS turrets: the bigger garrison
            Has(Content.PrometheusOutpost, 7, 8, 2, 2);     // four AFS turrets
        }

        [TestMethod]
        public void FortIntrepidIsAttackedInThreeWavesOfTwoShipsWithTheCommanderInTheLast()
        {
            var fort = Content.Assaults.Single(a => a.PointId == Content.FortIntrepid);
            Assert.AreEqual(1800, fort.PeriodSeconds, "the contested point: the Pools' period");
            _now += fort.FirstDelaySeconds * 1000L - 1;
            Tick();
            Assert.AreEqual(0, Assaults(Content.FortIntrepid).Length);
            Assert.AreEqual(0, Assaults(Content.PrometheusOutpost).Length);
            _now++;
            Tick();
            Assert.AreEqual(2, Assaults(Content.FortIntrepid).Length);
            Assert.AreEqual(0, Assaults(Content.PrometheusOutpost).Length, "the outpost has its own timer");
            Deliver();
            _now += fort.WaveSeconds * 1000L;
            Tick();
            _now += fort.WaveSeconds * 1000L;
            Tick();
            Deliver();
            Assert.AreEqual(6, Assaults(Content.FortIntrepid).Length);
            var troops = Troops(Content.FortIntrepid);
            Assert.AreEqual(1, troops.Count(c => c.DbId == Content.ThraxCommander));
            Assert.IsTrue(troops.Length is >= 28 and <= 33, $"{troops.Length} Bane in the first assault");
            Assert.IsTrue(troops.All(c => c.Level is >= 43 and <= 46), "the Howling Maw's Thrax (43-44) and its commander (46)");
            _now += fort.ReliefSeconds * 1000L - 2L * fort.WaveSeconds * 1000;
            Tick();
            Assert.AreEqual(1, Relief(Content.FortIntrepid).Length, "the AFS relief ship after the last wave");
            Deliver();
            var relief = EntityManager.Instance.Creatures.Values.Where(c => Relief(Content.FortIntrepid).Contains(c.SpawnPool)).ToArray();
            Assert.AreEqual(4, relief.Length);
            Assert.IsTrue(relief.All(c => c.DbId is Content.Rifleman or Content.MachineGunner));
        }

        [TestMethod]
        public void PrometheusOutpostIsAttackedOnItsOwnLongerTimer()
        {
            var outpost = Content.Assaults.Single(a => a.PointId == Content.PrometheusOutpost);
            Assert.AreEqual(2700, outpost.PeriodSeconds, "the quest hub: the Plateau's period");
            _now += outpost.FirstDelaySeconds * 1000L - 1;
            Tick();
            Assert.AreEqual(0, Assaults(Content.PrometheusOutpost).Length);
            _now++;
            Tick();
            Assert.AreEqual(2, Assaults(Content.PrometheusOutpost).Length, "two Bane ships");
        }

        [TestMethod]
        public void TheCommanderTakesFortIntrepidWhenItsDefendersAreDeadAndThenAfsSquadsCome()
        {
            var point = Point(Content.FortIntrepid);
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
                "an AFS sortie ship on the fallen Fort Intrepid");
        }

        [TestMethod]
        public void AWaveTakesOnTheGarrisonBeforeTheTurretsAndKeepsThePointAsItsHome()
        {
            // Prometheus Outpost's fence turrets are in its AFS garrison (ControlPointTurrets, link kind 2), as
            // the Marshes test has it for Research Point.
            var point = Point(Content.PrometheusOutpost);
            _world.SpawnWorld(point.AfsPools.ToArray());
            Deliver();
            var turrets = EntityManager.Instance.Creatures.Values.Where(c => c.SpawnPool != null && point.AfsPools.Contains(c.SpawnPool.DbId) &&
                Emplacements.Is(c) && CreatureManager.IsLivingOnMap(_world.Map, c)).ToArray();
            Assert.IsTrue(turrets.Length > 0, "the point's AFS turrets stand");
            Assert.IsTrue(EntityManager.Instance.Creatures.Values.Any(c => c.SpawnPool != null && point.AfsPools.Contains(c.SpawnPool.DbId) &&
                !Emplacements.Is(c) && CreatureManager.IsLivingOnMap(_world.Map, c)), "the point's AFS troops stand");
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

        /// <summary>While an assault runs the garrison stays down and no turret is repaired; after it, it comes back (the Marshes' live run of 2026-11-05).</summary>
        [TestMethod]
        public void NoAfsGarrisonComesBackAndNoTurretIsRepairedWhileAnAssaultRuns()
        {
            var point = Point(Content.PrometheusOutpost);
            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            _world.SpawnWorld(point.AfsPools.ToArray());
            Deliver();
            var garrison = Garrison(point);
            Assert.IsTrue(garrison.Any(Emplacements.Is) && garrison.Any(c => !Emplacements.Is(c)), "turrets and troops stand");
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
            Deliver();
            foreach (var creature in garrison) Kill(creature);
            Assert.IsTrue(garrison.Where(Emplacements.Is).All(AlternateMesh.KeepsWreck), "the turrets are wrecks on their mounts");
            Assert.AreEqual(ControlPoints.Garrison.Down, ControlPoints.Instance.GarrisonOf(_world.Map, point, ControlPoints.Afs));

            // Every respawn time of the garrison (5 and 10 min) passes while the assault runs.
            for (var i = 0; i < 4; i++) { SpawnPoolManager.Instance.SpawnPoolWorker(_world.Map, 300000); Deliver(); }
            Assert.AreEqual(0, Garrison(point).Length, "no AFS soldier set down again, no turret repaired");
            Assert.AreEqual(ControlPoints.Garrison.Down, ControlPoints.Instance.GarrisonOf(_world.Map, point, ControlPoints.Afs), "no AFS ship on its way");

            // Over (its Bane gone): the garrison comes back, the turrets repaired where they stand.
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

        /// <summary>The plan's relief is the one help in an assault, and only for a garrison that still holds.</summary>
        [TestMethod]
        public void TheReliefShipComesOnlyWhileTheGarrisonHolds()
        {
            var point = Point(Content.FortIntrepid);
            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            _world.SpawnWorld(point.AfsPools.ToArray());
            Deliver();
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
            foreach (var creature in Garrison(point)) Kill(creature);
            _now += plan.ReliefSeconds * 1000L;
            Tick();
            Assert.AreEqual(0, Relief(point.Id).Length, "no relief ship for a garrison that has fallen");
            _now += 60000;
            Tick();
            Assert.AreEqual(0, Relief(point.Id).Length, "nor later in the same assault");
        }

        /// <summary>
        /// The garrison down, the Bane make for the point, the claimant first; if it dies the next one claims;
        /// the assault does not end as held off while the garrison is down and a claimant stands; the claim
        /// done, the point is the Bane's (the Marshes' live run of 2026-11-05).
        /// </summary>
        [TestMethod]
        public void WithTheGarrisonDownTheBaneGoToThePointAndTheNextClaimantTakesIt()
        {
            var point = Point(Content.FortIntrepid);
            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            _world.SpawnWorld(point.AfsPools.ToArray());
            Deliver();
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
            _now += 2L * plan.WaveSeconds * 1000;
            Tick();
            Deliver();
            var vendor = EntityManager.Instance.Creatures.Values.FirstOrDefault(c => c.SpawnPool != null && point.AfsPools.Contains(c.SpawnPool.DbId) &&
                ControlPoints.Instance.ServicePointOf(_world.Map, c.SpawnPool) != null && CreatureManager.IsLivingOnMap(_world.Map, c));
            Assert.IsNotNull(vendor, "a vendor of the point stands (a service pool, no garrison)");
            foreach (var creature in Garrison(point)) Kill(creature);
            var troops = Troops(point.Id);
            var commander = troops.Single(c => c.DbId == Content.ThraxCommander);
            var soldier = troops.First(c => c.DbId == Content.ThraxSoldier && Flat(c.Position, point.Position) > LandingZoneAssaults.RallyReach);
            soldier.Controller.CurrentAction = BehaviorManager.BehaviorActionFighting;
            soldier.Controller.ActionFighting.TargetEntityId = vendor.EntityId;
            Tick();
            Assert.IsTrue(commander.OnClaimDuty, "the commander goes for the point");
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, soldier.Controller.CurrentAction, "the soldier gives up the vendor and makes for the point");
            Assert.IsTrue(Flat(soldier.Controller.ActionWander.WanderDestination, point.Position) < LandingZoneAssaults.RallyReach, "to the point");

            _now = 1000 + plan.LifetimeSeconds * 1000L + 1000;
            Tick();
            Assert.IsTrue(Assaults(point.Id).Length > 0, "the assault goes on while the garrison is down and a claimant stands");
            Assert.AreEqual(0, LandingZoneAssaults.Repelled(_world.Map, point));

            Kill(commander);
            Tick();
            var next = Troops(point.Id).SingleOrDefault(c => c.OnClaimDuty);
            Assert.IsNotNull(next, "the next claimant is chosen");
            next.Position = Walk(point.Position, "point");
            Tick();
            Assert.AreSame(next, point.Object.UsedBy, "the claim begins");
            _now += Math.Max(1, ControlPoints.Instance.Config.LandingZoneAssaultCaptureSeconds) * 1000L;
            Tick();
            Assert.AreEqual(ControlPoints.Bane, point.Owner, "the claim done, the point is the Bane's");
        }

        [TestMethod]
        public void AnAfsSquadAttacksAFallenPrometheusOutpostButCannotTakeIt()
        {
            var point = Point(Content.PrometheusOutpost);
            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            ControlPoints.Instance.SetOwner(point, ControlPoints.Bane, null);
            Tick();
            Assert.IsTrue(_world.Map.SpawnPools.Where(p => Content.Garrison.Any(g => g.Id == p.DbId && g.Point == point.Id)).All(p => p.Suspended),
                "no AFS garrison while the Bane hold the outpost");
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
        public void ARetakenPrometheusOutpostGetsItsAfsGarrisonBackAndACounterAttack()
        {
            var point = Point(Content.PrometheusOutpost);
            ControlPoints.Instance.SetOwner(point, ControlPoints.Bane, null);
            Tick();
            ControlPoints.Instance.SetOwner(point, ControlPoints.Afs, null);
            Tick();
            var garrison = _world.Map.SpawnPools.Where(p => Content.Garrison.Any(g => g.Id == p.DbId && g.Point == point.Id)).ToArray();
            Assert.AreEqual(7, garrison.Length);
            Assert.IsTrue(garrison.All(p => !p.Suspended && p.AnimType == 2));
            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            _now += plan.FirstDelaySeconds * 1000L - 1;
            Tick();
            Assert.AreEqual(0, Assaults(point.Id).Length);
            _now++;
            Tick();
            Assert.AreEqual(2, Assaults(point.Id).Length, "the Bane attack again after the first delay");
        }

        [TestMethod]
        public void EachAssaultHeldOffMakesTheNextBigger()
        {
            var point = Point(Content.FortIntrepid);
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
                void Rows(int creatures, int pools, string when)
                {
                    Assert.AreEqual(creatures, world.CreatureEntries.AsNoTracking().Count(r => r.Id >= Content.IdMin && r.Id <= Content.IdMax), $"creatures {when}");
                    Assert.AreEqual(creatures, world.CreatureActionEntries.AsNoTracking().Count(r => r.Id >= Content.IdMin && r.Id <= Content.IdMax), $"actions {when}");
                    Assert.AreEqual(pools, world.SpawnPoolEntries.AsNoTracking().Count(r => r.Id >= Content.IdMin && r.Id <= Content.IdMax), $"pools {when}");
                    Assert.AreEqual(pools, world.ControlPointLinkEntries.AsNoTracking().Count(r => r.ObjectId >= Content.IdMin && r.ObjectId <= Content.IdMax), $"links {when}");
                }
                Vector3[] Turrets() => world.SpawnPoolEntries.AsNoTracking().Where(p => Content.BoxedTurretPools.Contains(p.Id)).AsEnumerable().Select(p => p.Position).ToArray();
                int Respawn() => (int)world.SpawnPoolEntries.AsNoTracking().Single(p => p.Id == Content.CommanderPool).RespawnTime;
                Rows(3, 17, "after");
                Assert.AreEqual(1200, Respawn(), "the commander's respawn as the fort's Bane pool 531098's");
                Assert.AreEqual(2, Turrets().Length);
                Assert.IsTrue(Turrets().All(p => Vector3.Distance(p, Content.BoxedTurretAt) < 0.01f), "the boxed-in turret moved");
                Assert.AreEqual((int)world.SpawnPoolEntries.AsNoTracking().Single(p => p.Id == 531098).RespawnTime, Respawn());
                Assert.AreEqual(10, world.ControlPointLinkEntries.AsNoTracking().Count(r => r.ControlPointId == Content.FortIntrepid && r.Kind == 2 && r.ObjectId >= Content.IdMin && r.ObjectId <= Content.IdMax));
                Assert.AreEqual(7, world.ControlPointLinkEntries.AsNoTracking().Count(r => r.ControlPointId == Content.PrometheusOutpost && r.Kind == 2 && r.ObjectId >= Content.IdMin && r.ObjectId <= Content.IdMax));
                world.GetService<IMigrator>().Migrate(DynamicDropshipsMigrations.Previous(world, Migration));
                Rows(0, 0, "down");
                Assert.IsFalse(world.ControlPointLinkEntries.AsNoTracking().Any(l => (l.ObjectId == Content.CommanderPool && l.Kind == 1) || (Content.ServicePools.Contains(l.ObjectId) && l.Kind == 7)),
                    "the commander's and the service links go");
                Assert.IsTrue(Vector3.Distance(Content.DergeAt, world.SpawnPoolEntries.AsNoTracking().Single(p => p.Id == Content.DergePool).Position) < 0.01f,
                    "Derge stays on the rampart (the Crucible quests need him there)");
                Assert.AreEqual(Content.CommanderRespawnBefore, Respawn(), "the commander's upstream respawn back");
                Assert.IsTrue(Turrets().All(p => Vector3.Distance(p, Content.BoxedTurretWas) < 0.01f), "the turret back on its upstream spot");
                Assert.AreEqual(17, world.SpawnPoolEntries.AsNoTracking().Count(r => r.Id >= MarshesDynamicDropshipsContent.IdMin && r.Id <= MarshesDynamicDropshipsContent.IdMax), "the Marshes' rows stay");
                world.GetService<IMigrator>().Migrate(Migration);
                Rows(3, 17, "up again");
                Assert.AreEqual(1, world.ControlPointLinkEntries.AsNoTracking().Count(l => l.ControlPointId == Content.FortIntrepid && l.Kind == 1 && l.ObjectId == Content.CommanderPool));
                Assert.AreEqual(2, world.ControlPointLinkEntries.AsNoTracking().Count(l => l.ControlPointId == Content.PrometheusOutpost && l.Kind == 7));
                Assert.AreEqual(1200, Respawn(), "up again");
                Assert.IsTrue(Turrets().All(p => Vector3.Distance(p, Content.BoxedTurretAt) < 0.01f), "moved again");
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }
    }
}
