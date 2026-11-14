using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.ClientData;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Navigation;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Structures;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using static MarshesFalconHoldTests;
    using static MarshesPaludosTests;
    using static PlateauCampResistanceTests;
    using D = AshenDesignerHidesNpcsV1;
    using Jewelry = AshenDesignerJewelryV1;
    using Rage = AshenLatestRageV1;
    using Wear = AshenWouldntWearItV1;
    using Fad = AshenNewFadV1;
    using F = MarshesClientFloorTests;

    /// <summary>
    /// Ashen Desert set 10, Ida Hayner's orders, played through as a player plays them: Designer Jewelry Gone Wrong (1287: 10
    /// Shiny Atta Carapace off the Mauna Barrens' Ashen Atta Soldiers and Workers, the Pristine one off Sally), All the Latest
    /// Rage! (1293: Granitour carapace and Wendy's, at Coheri Pools), I Wouldn't Wear It, But... (1289: Lasher hides and Molly's,
    /// west of Ashoka) and A New Fad (1291: Beam Manta hides and Trudy's). With them the set's places: Ida Hayner off the weapon
    /// vendor, the new pools, their spawns and respawns, and the walks to them on the client's floor.
    ///
    /// Why these tests: a part dropped by the wrong creature, before the mission, past its count or at the wrong chance, a
    /// pristine part that does not drop at once, progress lost at a reload, a turn-in that pays twice or on a full bag, a
    /// follow-up offered too early, or a pool or boss the client's floor does not reach would each break an order.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenDesignerHidesTests
    {
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1)
        private const uint WeaponFiller = 3869;            // inventory category 1 (quest-test-pattern)
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests (inventory category 4)
        private const int Respawns = 4;                    // the rounds of respawns sampled after the first spawn
        private const float FloorReach = 1;                // a creature stands within this of the client's floor
        private const float PoolHeight = 2;                // and within this of its pool's height
        private const float BossClear = 6;                 // the ground round a boss's point (a pool of one: no rings)

        private static readonly Vector3 ShadowsEdge = AshenLeadInsTests.ShadowsEdgeWaypoint;
        private static readonly uint[] AttaPools = AshenMaunaBarrensNpcsV1.AttaPools.Where(pool => Jewelry.Droppers.Contains(pool.Creature)).Select(pool => pool.Pool).ToArray();
        private static readonly uint[] LasherPools = D.LasherPools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] MantaPools = D.MantaPools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] GranitourPools = D.GranitourPools.Select(pool => pool.Pool).ToArray();

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// Ida Hayner offers Designer Jewelry Gone Wrong and I Wouldn't Wear It, But... at once (TaRapedia: no requirement), not
        /// their follow-ups; another Shadow's Edge NPC (Kuang) offers none of them. 1287 done, she offers All the Latest Rage!
        /// (not A New Fad); 1289 done too, A New Fad; after a reload as well.
        /// </summary>
        [TestMethod]
        public void IdaOffersTheOrdersInTurn()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, ShadowsEdge);
            var ida = Npc(desert, D.IdaHayner);
            var kuang = Npc(desert, AshenMaunaBarrensNpcsV1.Kuang);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, ida, Jewelry.MissionId), "1287 at once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, ida, Wear.MissionId), "1289 at once");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ida, Rage.MissionId), "1293 not before 1287");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ida, Fad.MissionId), "1291 not before 1289");
            foreach (var mission in new[] { Jewelry.MissionId, Wear.MissionId })
                Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kuang, mission), $"{mission} is not Kuang's to give");

            AshenRuntime.SeedCompleted(harness, Jewelry.MissionId);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, ida, Rage.MissionId), "1287 done: 1293");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ida, Fad.MissionId), "1291 still waits for 1289");
            AshenRuntime.SeedCompleted(harness, Jewelry.MissionId, Wear.MissionId);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, ida, Fad.MissionId), "1289 done: 1291");
            AshenRuntime.Reload(harness);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, ida, Rage.MissionId) && PlateauRetreadCampTests.Offers(harness, ida, Fad.MissionId), "after a reload too");
        }

        [TestMethod]
        public void DesignerJewelryComesOffTheAttaAndSally() => Order(new(Jewelry.MissionId, Array.Empty<uint>(), Jewelry.Droppers, AttaPools, D.Sally, D.SallyPool,
            Jewelry.ShinyTemplate, Jewelry.PristineTemplate, new[] { Jewelry.MotorAssistLegs, Jewelry.MechLegs, Jewelry.BioLegs, Jewelry.GravitonLegs },
            Jewelry.Experience, Jewelry.Credits, ArmorFiller, 4 + 1, Rage.MissionId, Abandon: false)); // fix F: a marker on its own pool too

        [TestMethod]
        public void TheLatestRageComesOffTheGranitourAndWendy() => Order(new(Rage.MissionId, new[] { Jewelry.MissionId }, new[] { D.Granitour }, GranitourPools, D.Wendy, D.WendyPool,
            Rage.ShinyTemplate, Rage.PristineTemplate, new[] { Rage.SonicTorqueshell, Rage.VirulentDisperser, Rage.ElectricPolarityGun, Rage.LaserPistol },
            Rage.Experience, Rage.Credits, WeaponFiller, 2, null, Abandon: true));

        [TestMethod]
        public void TheLasherHidesComeOffTheLashersAndMolly() => Order(new(Wear.MissionId, Array.Empty<uint>(), new[] { D.Lasher }, LasherPools, D.Molly, D.MollyPool,
            Wear.LeatherTemplate, Wear.PristineTemplate, new[] { Wear.CryogenicInjector, Wear.LaserCannon, Wear.PulseChaingun, Wear.Pistol },
            Wear.Experience, Wear.Credits, WeaponFiller, 2, Fad.MissionId, Abandon: false));

        [TestMethod]
        public void TheNewFadComesOffTheBeamMantaAndTrudy() => Order(new(Fad.MissionId, new[] { Wear.MissionId }, new[] { D.Manta }, MantaPools, D.Trudy, D.TrudyPool,
            Fad.LeatherTemplate, Fad.PristineTemplate, new[] { Fad.ElectricPolarityGun, Fad.ElectricNetGun, Fad.SonicCannon, Fad.ElectricChaingun },
            Fad.Experience, Fad.Credits, WeaponFiller, 3, null, Abandon: true));

        /// <summary>One of Ida's orders: the mission, its prerequisites, the creatures of objective 1 and their pools, the boss and its pool, the two parts, the items, the pay, a filler of the items' bag, the markers on 1, the follow-up; with an abandon or not.</summary>
        internal sealed record Hides(uint Mission, uint[] Before, uint[] Normals, uint[] Pools, uint Boss, uint BossPool, uint Part, uint Pristine,
            uint[] Items, uint Experience, uint Credits, uint Filler, int Markers, uint? FollowUp, bool Abandon);

        /// <summary>
        /// An order played through: Ida offers it with the four items; no part drops before it. Accepted, Ida hears no
        /// objective; the markers on 1 (the pools) and 2 (the boss), on Ashen Desert. A normal drops the part only, under 75 (75
        /// misses, 74 drops); a death is not a pickup; a full bag leaves it on the corpse, one slot free and it is picked up. The
        /// boss drops the pristine part only, at once (99: 100%), once (a second boss kill drops none). Ten parts: the tenth
        /// completes 1 once, an eleventh kill drops none; a reload halfway keeps the count; the mission is completeable once.
        /// With an abandon: the parts are kept and count again at once. A full bag waits, a failed save pays nothing and keeps
        /// the parts; Ida takes all eleven and pays the item chosen, once; then the follow-up (if any) is offered.
        /// </summary>
        private static void Order(Hides order)
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            if (order.Before.Length > 0)
                AshenRuntime.SeedCompleted(harness, order.Before);
            var desert = AshenLeadInsTests.EnterAshen(harness, ShadowsEdge);
            var ida = Npc(desert, D.IdaHayner);
            var bossPools = new[] { order.BossPool };
            Spawn(desert, order.Pools.Append(order.BossPool).ToArray());

            var offer = WildernessRanjaGorgeTests.Offer(harness, ida, order.Mission);
            CollectionAssert.AreEqual(order.Items, offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray(), "the four items to choose");
            Assert.IsFalse(Holds(AshenScavengerTests.Kill(harness, desert, order.Normals[0], order.Pools), order.Part), "no part before the mission");
            percentile = 99;
            Assert.IsFalse(Holds(AshenScavengerTests.Kill(harness, desert, order.Boss, bossPools), order.Pristine), "no pristine part before the mission");

            Accept(harness, ida, order.Mission);
            Assert.IsEmpty(Topics(harness, ida, order.Mission), "Ida hears no objective: the parts count as they are picked up");
            var objectives = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[order.Mission].ObjectivesList;
            Assert.HasCount(order.Markers, objectives.Single(entry => entry.ObjectiveId == 1).IndicatorList, "1: a marker on each pool");
            Assert.HasCount(1, objectives.Single(entry => entry.ObjectiveId == 2).IndicatorList, "2: a marker on the boss");
            Assert.IsTrue(objectives.SelectMany(entry => entry.IndicatorList).All(marker => marker.MapContextId == AshenMissionDataV1.MapContextId), "on Ashen Desert");

            percentile = 75;                                  // the rate written out (ESTIMATE 75 %)
            Assert.IsFalse(Holds(AshenScavengerTests.Kill(harness, desert, order.Normals[0], order.Pools), order.Part), "75 misses");
            percentile = 74;
            var first = AshenScavengerTests.Kill(harness, desert, order.Normals[0], order.Pools);
            Assert.IsTrue(Holds(first, order.Part) && !Holds(first, order.Pristine), "74: a normal drops the part only");
            Assert.AreEqual(0U, Held(harness, order.Part), "a death is not a pickup");
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            PickUp(harness, first);
            Assert.AreEqual(0U, Held(harness, order.Part), "no room");
            Assert.IsTrue(Holds(first, order.Part), "still on the corpse");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, first);
            Assert.AreEqual(1U, Held(harness, order.Part), "picked up once there is room");

            percentile = 99;                                  // the boss's part at 100 %
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            var boss = AshenScavengerTests.Kill(harness, desert, order.Boss, bossPools);
            Assert.IsTrue(Holds(boss, order.Pristine) && !Holds(boss, order.Part), "the boss drops the pristine part only, at once");
            PickUp(harness, boss);
            var packets = harness.Drain().Cast<object>().ToList();
            Assert.AreEqual(1U, Held(harness, order.Pristine), "the pristine part");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, order.Mission, 2), "2 done");
            Assert.IsFalse(Holds(AshenScavengerTests.Kill(harness, desert, order.Boss, bossPools), order.Pristine), "one: the next boss kill drops none");

            percentile = 0;
            for (var count = 2; count <= 10; count++)
            {
                WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
                var loot = AshenScavengerTests.Kill(harness, desert, order.Normals[count % order.Normals.Length], order.Pools);
                Assert.IsTrue(Holds(loot, order.Part), $"part {count} dropped (kind {order.Normals[count % order.Normals.Length]})");
                PickUp(harness, loot);
                packets.AddRange(harness.Drain());
                if (count == 5)
                {
                    AshenRuntime.Reload(harness);
                    harness.Drain();
                    Assert.AreEqual(5U, Held(harness, order.Part), "kept at a reload");
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, order.Mission, 1), "five of ten");
                }
                if (order.Abandon && count == 7)
                {
                    Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, order.Mission));
                    harness.Drain();
                    Assert.AreEqual((7U, 1U), (Held(harness, order.Part), Held(harness, order.Pristine)), "kept at an abandon");
                    Assert.IsFalse(Holds(AshenScavengerTests.Kill(harness, desert, order.Normals[0], order.Pools), order.Part), "no part while it is dropped");
                    Accept(harness, ida, order.Mission);
                    Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, order.Mission, 2), "the pristine part held counts again");
                    packets = harness.Drain().Cast<object>().ToList();
                }
            }
            Assert.AreEqual((10U, 1U), (Held(harness, order.Part), Held(harness, order.Pristine)), "ten parts and the pristine one");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, order.Mission, 1), "1 done");
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == order.Mission && packet.ObjectiveId == 1), "1: ObjectiveCompleted once");
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == order.Mission && packet.IsCompleteable), "MissionCompleteable once");
            Assert.IsFalse(Holds(AshenScavengerTests.Kill(harness, desert, order.Normals[0], order.Pools), order.Part), "an eleventh kill drops none");

            FullBagWaits(harness, ida, order.Mission, 0, order.Filler);
            FailedTurnInPaysNothing(harness, ida, order.Mission, 0);
            Assert.AreEqual((10U, 1U), (Held(harness, order.Part), Held(harness, order.Pristine)), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, ida, order.Mission, 0, order.Experience, order.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == order.Mission), "MissionRewarded once");
            Assert.AreEqual(1U, Held(harness, order.Items[0]), "the item chosen");
            Assert.AreEqual((0U, 0U), (Held(harness, order.Part), Held(harness, order.Pristine)), "Ida takes all eleven");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ida, order.Mission), "done once");
            if (order.FollowUp is { } next)
                Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, ida, next), $"Ida offers {next}");
            Assert.IsFalse(Holds(AshenScavengerTests.Kill(harness, desert, order.Normals[0], order.Pools), order.Part), "after the turn-in no part drops");
        }

        /// <summary>
        /// Ida Hayner's pool (510011): off the weapon vendor's spot (500018, where upstream had her too), 3 m or more from it, in
        /// the same shop, her row as upstream's (Ida Hayner 9288, friendly, no package); she comes down at her pool's point.
        /// </summary>
        [TestMethod]
        public void IdaStandsOffTheWeaponVendor()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var ida = world.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == D.IdaHayner);
            var vendor = world.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == 500018);
            Assert.IsLessThan(0.01, Math.Abs(ida.PosX - D.IdaAt.X) + Math.Abs(ida.PosY - D.IdaAt.Y) + Math.Abs(ida.PosZ - D.IdaAt.Z), $"Ida at {D.IdaAt}");
            Assert.IsGreaterThanOrEqualTo(3f, Vector2.Distance(new((float)ida.PosX, (float)ida.PosZ), new((float)vendor.PosX, (float)vendor.PosZ)), "off the weapon vendor");
            Assert.IsLessThan(0.01, Math.Abs(vendor.PosX - D.IdaBefore.X) + Math.Abs(vendor.PosZ - D.IdaBefore.Z), "where upstream had her: the vendor's spot");
            var row = world.CreatureEntries.AsNoTracking().Single(entry => entry.Id == D.IdaHayner);
            Assert.AreEqual((9288U, 1U), (row.NameId, row.Faction), "Ida Hayner, friendly");
            Assert.IsFalse(world.NpcPackageEntries.AsNoTracking().Any(entry => entry.Id == D.IdaHayner), "no package (the client gives her no talk)");
            var desert = AshenLeadInsTests.EnterAshen(harness, ShadowsEdge);
            var npc = Npc(desert, D.IdaHayner);
            Assert.IsLessThan(1f, Vector2.Distance(new(npc.Position.X, npc.Position.Z), new((float)D.IdaAt.X, (float)D.IdaAt.Z)), "she stands at her point");
        }

        /// <summary>
        /// The set's pools: each puts down its kind (three normals, or the one boss), hostile, with the row's level, name and hit
        /// points. Round each point every navmesh floor is one floor (ClearRadius for the normals' rings, BossClear for a boss),
        /// joined to its waypoint's ground, within 1 m of a client floor and under open sky; at the first spawn and after each of
        /// four rounds of kills and respawns every creature stands on the navmesh, on the point or its rings, within 1 m of the
        /// client's floor and 2 m of its pool's height, and inside its marker.
        /// </summary>
        [TestMethod]
        public void TheHidesComeDownAndBackOnTheFloorPlayersWalk()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var nav = desert.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", harness.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
            var markers = harness.World.MissionIndicatorEntries.AsEnumerable()
                .Where(row => row.ContentRevision == AshenMissionDataV1.Revision && Missions.Contains(row.MissionId)).ToList();
            var pools = Places.Select(place => place.Pool).Where(pool => pool != D.IdaHayner).ToArray();
            Spawn(desert, pools);
            var failures = new List<string>();
            foreach (var (poolId, _, from, _) in Places.Where(place => place.Pool != D.IdaHayner))
            {
                var pool = desert.SpawnPools.Single(loaded => loaded.DbId == poolId);
                Assert.AreEqual(0f, pool.Radius, $"{poolId} a point pool");
                Assert.IsTrue(SpawnPoolManager.HasWalkableGround(desert, pool), $"{poolId} has walkable ground");
                var boss = D.Bosses.Any(row => row.Pool == poolId);
                var region = nav.RegionArea(nav.Nearest(from).Value);
                var ground = Ground(desert, pool.Position, boss ? BossClear : (float)AshenFixAV1.ClearRadius, out var stacked);
                Assert.IsGreaterThan(boss ? 60 : 500, ground.Count, $"{poolId}: the floors round the point are read");
                if (stacked.Count > 0)
                    failures.Add($"pool {poolId}: more than one navmesh floor at " + string.Join("; ", stacked.Take(3)));
                var off = ground.Where(at => nav.RegionArea(at) != region).ToList();
                if (off.Count > 0)
                    failures.Add($"pool {poolId}: {off.Count} floors not joined to its waypoint, " + string.Join("; ", off.Take(3)));
                var round = F.Near(geometry, ground, 0.5f);
                for (var i = 0; i < ground.Count; i++)
                {
                    var at = ground[i];
                    if (F.FloorAt(round[i], at, at.Y - 2, at.Y + 1.5f) is not { } y || y - at.Y > 1 || at.Y - y > 1.5f)
                        failures.Add(FormattableString.Invariant($"pool {poolId}: no client floor under the navmesh at ({at.X:0.0}, {at.Y:0.00}, {at.Z:0.0})"));
                    if (round[i].Any(t => F.Hit(at + new Vector3(0, 1, 0), Vector3.UnitY, t.A, t.B, t.C) is { } up && up < 40))
                        failures.Add(FormattableString.Invariant($"pool {poolId}: a client surface over ({at.X:0.0}, {at.Y:0.00}, {at.Z:0.0})"));
                }
            }

            var kinds = D.Normals.Select(row => (row.Id, row.Pools.Select(pool => pool.Pool).ToArray(), (int)D.PerPool))
                .Concat(D.Bosses.Select(row => (row.Id, new[] { row.Pool }, 1))).ToList();
            var creatures = harness.World.CreatureEntries.AsNoTracking().ToDictionary(row => row.Id);
            var stats = harness.World.CreatureStatEntries.AsNoTracking().ToDictionary(row => row.Id);
            for (var round = 0; round <= Respawns; round++)
            {
                var here = Alive(desert).Where(candidate => candidate.SpawnPool != null && pools.Contains(candidate.SpawnPool.DbId)).Distinct().ToList();
                foreach (var (kind, kindPools, count) in kinds)
                    foreach (var poolId in kindPools)
                    {
                        Assert.AreEqual(count, here.Count(one => one.SpawnPool.DbId == poolId && one.DbId == kind), $"round {round}: pool {poolId} has {count} of {kind}");
                        Assert.AreEqual(count, here.Count(one => one.SpawnPool.DbId == poolId), $"round {round}: pool {poolId} has nothing else");
                    }
                var near = F.Near(geometry, here.Select(one => one.Position).ToList(), 1);
                for (var i = 0; i < here.Count; i++)
                {
                    var one = here[i];
                    var label = FormattableString.Invariant($"round {round}: pool {one.SpawnPool.DbId}'s {one.DbId} at {one.Position}");
                    var row = creatures[one.DbId];
                    Assert.AreEqual((TargetCategory.Hostile, row.Level, row.NameId, stats[one.DbId].Health),
                        (one.TargetCategory, one.Level, one.NameId, (int)one.Attributes[Attributes.Health].NormalMax), label);
                    if (!nav.IsOnMesh(one.Position))
                        failures.Add($"{label}: off the navmesh");
                    var floor = F.FloorAt(near[i], one.Position, one.Position.Y - 3, one.Position.Y + 8);
                    if (floor is not { } y || MathF.Abs(y - one.Position.Y) > FloorReach)
                        failures.Add(FormattableString.Invariant($"{label}: the client's floor at {floor}"));
                    if (Vector2.Distance(new(one.Position.X, one.Position.Z), new(one.SpawnPool.Position.X, one.SpawnPool.Position.Z)) > AshenFixAV1.PointSpread + 0.5f)
                        failures.Add($"{label}: off the point and its rings");
                    if (MathF.Abs(one.Position.Y - one.SpawnPool.Position.Y) > PoolHeight)
                        failures.Add(FormattableString.Invariant($"{label}: {one.Position.Y - one.SpawnPool.Position.Y:0.00} m from its pool's height"));
                    if (!markers.Any(marker => Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new(one.SpawnPool.Position.X, one.SpawnPool.Position.Z)) < 0.1f &&
                                               Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new(one.Position.X, one.Position.Z)) <= marker.Radius))
                        failures.Add($"{label}: outside its markers");
                }
                if (round == Respawns)
                    break;
                foreach (var one in here)
                    AshenScavengerTests.KillForGood(harness, desert, one);
                Respawn(desert, pools);
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// The walks on the client's floor (AshenFixBTests.ClientFloorLine: every half metre, never more than 1.1 m up over a
        /// metre, nothing of the placed meshes in the way at knee, chest or head height on the line and a player's half width to
        /// each side), there and back, from each place's waypoint (Places): Ida Hayner, the new pools and the bosses. Where the
        /// navmesh's straight walk meets a step the client's floor does not take, the walk goes by the corners Places names (the
        /// probe of 2026-11-10; ESTIMATE of a player's line). Set 12: every walk, all of it, keeps AshenWhiteOasisTests.
        /// HostileClearance (30 m, 2D) outside the circle of every automatic hostile Ashen Desert pool not of its order.
        /// </summary>
        [TestMethod]
        public void TheOrdersPlacesAreWalkedToOnTheClientsFloor()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var nav = desert.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", harness.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
            var hostile = AshenWhiteOasisTests.HostilePools(harness);
            Assert.IsGreaterThan(50, hostile.Count, "the Ashen hostile pools are read");
            var failures = new List<string>();
            var walked = 0;
            foreach (var (poolId, label, from, way) in Places)
            {
                var row = harness.World.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == poolId);
                var at = nav.Nearest(new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ)).Value;
                // Set 12: the whole walk (not only its last 100 m, AshenFixETests) keeps HostileClearance outside every hostile
                // pool's circle that is not of its order (Ida's: none of hers).
                var own = AshenFixETests.OrderPools.SingleOrDefault(order => order.Pools.Contains(poolId)).Pools ?? Array.Empty<uint>();
                foreach (var (legs, direction) in new[] { (way.There.Prepend(from).Append(at).ToList(), "there"), (way.Back.Prepend(at).Append(from).ToList(), "back") })
                {
                    var line = new List<Vector3>();
                    for (var leg = 0; leg + 1 < legs.Count; leg++)
                    {
                        walked++;
                        if (Walk(nav, geometry, legs[leg], legs[leg + 1]) is { } why)
                            failures.Add(FormattableString.Invariant($"{label} (pool {poolId}) {direction}, leg {leg + 1} ({legs[leg].X:0.0}, {legs[leg].Z:0.0}) to ({legs[leg + 1].X:0.0}, {legs[leg + 1].Z:0.0}): {why}"));
                        var start = nav.Nearest(legs[leg]) ?? legs[leg];
                        var path = nav.FindPath(start, nav.Nearest(legs[leg + 1]) ?? legs[leg + 1], out _);
                        if (path != null)
                            line.AddRange(path.Prepend(start));
                    }
                    if (AshenWhiteOasisTests.Clearance(line, hostile.Where(pool => !own.Contains(pool.Id)).ToList()) is { } close)
                        failures.Add(FormattableString.Invariant($"{label} (pool {poolId}) {direction}: {close.Gap:0.0} m outside hostile pool {close.Pool}'s circle at ({close.At.X:0.0}, {close.At.Y:0.0})"));
                }
            }
            Assert.IsGreaterThanOrEqualTo(2 * Places.Length, walked, "the legs are walked");
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>
        /// The set's normal pools stand apart (across, 2D) 60 m or more from every other Ashen Desert pool of a hostile creature
        /// (their own kind's other pool and their boss aside), so a fight at one does not pull in another camp; every set 10 pool
        /// 85 m or more from every friendly NPC's pool. A boss stands among its own kind (Sally among set 2's Atta: TaRapedia's
        /// spot is 21 m from pool 790047), 30 m or more from any other kind's pool.
        /// </summary>
        [TestMethod]
        public void TheHidePoolsStandApartFromOtherHostilesAndTheNpcs()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = harness.World.SpawnPoolEntries.AsNoTracking().Where(row => row.MapContextId == AshenMissionDataV1.MapContextId).ToList();
            bool Holds(Rasa.Structures.World.SpawnPoolEntry row, Func<Creature, bool> what) =>
                new[] { row.Creature1Id, row.Creature2Id, row.Creature3Id, row.Creature4Id, row.Creature5Id, row.Creature6Id }
                    .Any(id => id != 0 && CreatureManager.Instance.LoadedCreatures.TryGetValue(id, out var creature) && what(creature));
            var hostile = pools.Where(row => Holds(row, creature => creature.TargetCategory == TargetCategory.Hostile)).ToList();
            var npcs = pools.Where(row => Holds(row, creature => creature.TargetCategory != TargetCategory.Hostile && creature.Npc != null)).ToList();
            Assert.IsGreaterThan(50, hostile.Count, "the Ashen hostile pools are read");
            Assert.IsTrue(npcs.Any(row => row.Id == D.IdaHayner), "Ida among the NPCs");
            var families = new List<(uint[] Pools, uint[] Family, float Gap)>
            {
                (LasherPools, LasherPools.Append(D.MollyPool).ToArray(), 60),
                (MantaPools, MantaPools.Append(D.TrudyPool).ToArray(), 60),
                (GranitourPools, GranitourPools.Append(D.WendyPool).ToArray(), 60),
                (new[] { D.SallyPool }, AshenMaunaBarrensNpcsV1.AttaPools.Select(pool => pool.Pool).ToArray(), 30),
                (new[] { D.MollyPool }, LasherPools, 30),
                (new[] { D.TrudyPool }, MantaPools, 30),
                (new[] { D.WendyPool }, GranitourPools, 30)
            };
            var failures = new List<string>();
            foreach (var (ids, family, gap) in families)
                foreach (var poolId in ids)
                {
                    var pool = pools.Single(row => row.Id == poolId);
                    float Across(Rasa.Structures.World.SpawnPoolEntry other) => Vector2.Distance(new((float)pool.PosX, (float)pool.PosZ), new((float)other.PosX, (float)other.PosZ));
                    var nearestHostile = hostile.Where(row => row.Id != poolId && !family.Contains(row.Id)).MinBy(Across);
                    if (Across(nearestHostile) < gap)
                        failures.Add(FormattableString.Invariant($"pool {poolId}: hostile pool {nearestHostile.Id} {Across(nearestHostile):0.0} m across"));
                    var nearestNpc = npcs.MinBy(Across);
                    if (Across(nearestNpc) < 85)
                        failures.Add(FormattableString.Invariant($"pool {poolId}: NPC pool {nearestNpc.Id} {Across(nearestNpc):0.0} m across"));
                }
            Assert.HasCount(3 + 1 + 3, families, "three normal families, Sally, the other three bosses");
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        private static readonly uint[] Missions = { Jewelry.MissionId, Rage.MissionId, Wear.MissionId, Fad.MissionId };

        /// <summary>The walk on the client's floor with a player's width (AshenFixBTests.ClientFloorLine on the navmesh's corners); null when it gets there.</summary>
        internal static string Walk(NavMeshQuery nav, MapGeometry geometry, Vector3 from, Vector3 to)
        {
            var start = nav.Nearest(from) ?? from;
            var end = nav.Nearest(to) ?? to;
            var path = nav.FindPath(start, end, out var complete);
            if (path == null || !complete)
                return "no complete navmesh walk";
            return AshenFixBTests.ClientFloorLine(geometry, path.Prepend(start).ToList(), end, null, AshenFormalComplaintsTests.PlayerHalfWidth);
        }

        /// <summary>The navmesh floors on a 1 m grid within <paramref name="reach"/> of <paramref name="centre"/>, and the spots with more than one.</summary>
        private static List<Vector3> Ground(MapChannel desert, Vector3 centre, float reach, out List<string> stacked)
        {
            var ground = new List<Vector3>();
            stacked = new List<string>();
            var steps = (int)Math.Ceiling(reach);
            for (var dz = -steps; dz <= steps; dz++)
                for (var dx = -steps; dx <= steps; dx++)
                {
                    if (dx * dx + dz * dz > reach * reach)
                        continue;
                    var heights = new SortedSet<float>();
                    for (var h = -8; h <= 12; h++)
                    {
                        var probe = new Vector3(centre.X + dx, centre.Y + h, centre.Z + dz);
                        if (desert.NavMesh.Nearest(probe) is { } nearest && MathF.Abs(nearest.X - probe.X) < 0.3f && MathF.Abs(nearest.Z - probe.Z) < 0.3f &&
                            desert.NavMesh.GroundHeight(probe) is { } y)
                            heights.Add(MathF.Round(y, 1));
                    }
                    var column = new List<Vector3>();
                    foreach (var y in heights)
                        if (column.Count == 0 || y - column[^1].Y >= 2)
                            column.Add(new Vector3(centre.X + dx, y, centre.Z + dz));
                    if (column.Count > 1)
                        stacked.Add(string.Join(" / ", column));
                    ground.AddRange(column);
                }
            return ground;
        }

        private static uint Held(WildernessRuntimeTestHarness harness, uint template) => WildernessRanjaGorgeTests.Held(harness, template);

        private static readonly Vector3 Ashoka = AshenLeadInsTests.AshokaWaypoint;
        private static readonly Vector3 WhiteOasis = AshenLeadInsTests.WhiteOasisWaypoint;

        private static (Vector3[] There, Vector3[] Back) Straight => (Array.Empty<Vector3>(), Array.Empty<Vector3>());

        /// <summary>Fix E: the corner on the ridge west of Coheri Pools (probe 2026-11-10, ESTIMATE of a player's line).</summary>
        internal static Vector3[] WestRidge => new[] { new Vector3(-300.00f, 308.19f, 620.00f) };
        /// <summary>
        /// Set 12: the way from the White Oasis Post waypoint round the west side of Maji Field (set 11's way to Harvester Control
        /// Panel 4, AshenWhiteOasisTests.Places). Fix E's straight leg from (-439.1, 204.8) to the ridge passed 2.2 m outside the
        /// Xanx pool 540076's circle and 2.4 m outside the Tree Mite pool 540078's (the whole-walk clearance, set 12's probe).
        /// </summary>
        internal static Vector3[] WestOfMaji => new[] { new Vector3(-439.10f, 284.34f, 204.80f), new Vector3(-430f, 285f, 300f), new Vector3(-425f, 290f, 470f) };
        /// <summary>The shared way to the ridge west of Coheri Pools: round the west of Maji Field, then the ridge corner; and back.</summary>
        internal static Vector3[] ByTheWestRidge => WestOfMaji.Concat(WestRidge).ToArray();
        internal static Vector3[] BackByTheWestRidge => WestRidge.Concat(Enumerable.Reverse(WestOfMaji)).ToArray();
        /// <summary>Set 12: west of Coheri Pools, the way between the ridge corner and the first Granitour pool, off Wendy's pool 790377 and Trudy's 790372 (ESTIMATE of a player's line, probe 2026-11-10).</summary>
        internal static readonly Vector3 CoheriWest = new(-250f, 295f, 540f);
        /// <summary>Set 12: the way east from the White Oasis Post waypoint south of Maji Field's camps (set 11's to the Sheitan Embankment, without its last corner by the garrison 531032), off Overseer Ulovik 520006.</summary>
        internal static Vector3[] SouthOfMaji => new[] { new Vector3(-260f, 283f, 200f), new Vector3(-160f, 283f, 200f) };
        private static Vector3[] ToIndraPass => new[] { new Vector3(152.82f, 312.80f, 459.30f), new Vector3(195.75f, 320.57f, 477.30f), new Vector3(208.74f, 321.33f, 488.89f) };

        /// <summary>
        /// The places a player walks to, each from its waypoint, with the corners of the walk there and back where the navmesh's
        /// straight walk takes a step the client's floor does not (the probe of 2026-11-10).
        /// </summary>
        internal static readonly (uint Pool, string Label, Vector3 From, (Vector3[] There, Vector3[] Back) Way)[] Places =
        {
            (D.IdaHayner, "Ida Hayner from the Shadow's Edge Post waypoint", ShadowsEdge,
                (new[] { new Vector3(-139.60f, 296.13f, -770.86f), new Vector3(-143.20f, 297.05f, -759.80f) }, new[] { new Vector3(-145.00f, 296.13f, -754.80f) })),
            (D.SallyPool, "Sally from the Shadow's Edge Post waypoint", ShadowsEdge, Straight),
            // Fix F: 1287's own Ashen Atta Soldier pool on the slope east of the camp, out of its Workers' warcry (AshenFixFTests).
            (AshenFixFV1.AttaPool, "1287's Ashen Atta Soldier pool from the Shadow's Edge Post waypoint", ShadowsEdge, Straight),
            (LasherPools[0], "the first Lasher pool from the Ashoka Settlement waypoint", Ashoka, Straight),
            (LasherPools[1], "the second Lasher pool from the Ashoka Settlement waypoint", Ashoka, Straight),
            (D.MollyPool, "Molly from the Ashoka Settlement waypoint", Ashoka, (Array.Empty<Vector3>(), new[] { new Vector3(380.61f, 281.49f, -493.00f) })),
            (MantaPools[0], "the eastern Boneyard's Beam Manta from the Ashoka Settlement waypoint", Ashoka, Straight),
            (MantaPools[1], "the Indra Pass Beam Manta from the White Oasis Post waypoint (the Indra Pass waypoint's floor is joined to nothing)", WhiteOasis,
                // Set 12: by the south of Maji Field first (the straight way passed 5.3 m outside Overseer Ulovik's pool 520006).
                (SouthOfMaji.Concat(ToIndraPass).ToArray(), Enumerable.Reverse(SouthOfMaji.Concat(ToIndraPass)).ToArray())),
            // Fix E: the Coheri Pools and Maji Field walks go by the west ridge (-300, 620), off the Fithik pools 540075 and 540077
            // (the last 100 m of each walk 30 m and more from every hostile pool not of its mission: AshenFixETests).
            // Set 12: and round the west of Maji Field first (WestOfMaji), off the Xanx and Tree Mite pools 540076 and 540078.
            (MantaPools[2], "the north-east Maji Field Beam Manta from the White Oasis Post waypoint", WhiteOasis, (ByTheWestRidge, BackByTheWestRidge)),
            (D.TrudyPool, "Trudy from the White Oasis Post waypoint", WhiteOasis, (ByTheWestRidge, BackByTheWestRidge)),
            (GranitourPools[0], "the first Coheri Pools Granitour from the White Oasis Post waypoint", WhiteOasis,
                // Set 12: by CoheriWest both ways (fix E's back corner (-220, 580) passed 25.2 m outside Trudy's pool 790372).
                (ByTheWestRidge.Append(CoheriWest).ToArray(), BackByTheWestRidge.Prepend(CoheriWest).ToArray())),
            (GranitourPools[1], "the second Coheri Pools Granitour from the White Oasis Post waypoint", WhiteOasis, (ByTheWestRidge, BackByTheWestRidge)),
            (D.WendyPool, "Wendy from the White Oasis Post waypoint", WhiteOasis,
                (ByTheWestRidge.Append(new Vector3(-300.00f, 295.45f, 550.00f)).ToArray(), new[] { new Vector3(-300.00f, 295.45f, 550.00f) }.Concat(BackByTheWestRidge).ToArray()))
        };
    }
}
