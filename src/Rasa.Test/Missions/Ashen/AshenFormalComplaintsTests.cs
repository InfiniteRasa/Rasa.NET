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
using Rasa.Packets.MapChannel.Client;
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
    using K = AshenFormalComplaintsNpcsV1;
    using N = AshenLeadInNpcsV1;
    using Complaints = AshenFormalComplaintsV1;
    using Human = AshenHumanPerspectiveV1;
    using Bribe = AshenBribeForYourThoughtsV1;
    using Court = AshenCourtMartialArtsV1;
    using Departed = AshenDearlyDepartedV1;
    using F = MarshesClientFloorTests;

    /// <summary>
    /// Ashen Desert set 6, Formal Complaints and its AFS side, played through as a player plays it: Scavenger Vakiel Latan's
    /// Formal Complaints (1254: her side, player flag 250, or Captain Yamamoto's, flag 251), Shinichiro Ito's The Human
    /// Perspective (1258, only after choice 2), Latan's A Bribe for Your Thoughts (1259: her package to Yamamoto), Yamamoto's
    /// Court Martial Arts (1261: the package to Colonel Mason Linier) and Megumi Yamamoto's Dearly Departed (1285: her father's
    /// ring off a Thrax). With them the set's places: Latan, Ito and Megumi, the Boneyard's Thrax pools 1285 marks, their
    /// spawns and respawns.
    ///
    /// Why these tests: the choice is for good and splits the zone (TaRapedia 1580); the client reads each side's texts by the
    /// player flag of its conversation variant, so a choice that sets the wrong flag, both flags, or none, a follow-up offered
    /// to the wrong side, a talk heard in the wrong variant, a package not given or not taken, a ring that drops before the
    /// mission or twice, a marker that misses the Thrax, progress lost at a reload, or a turn-in that pays twice or on a full
    /// bag would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenFormalComplaintsTests
    {
        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack (as AshenLeadInsTests): fills the consumables bag
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1)
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests
        private const uint ClientPackageTemplate = 97462;  // client item class 25439 "Latan's Package"
        private const uint ClientRingTemplate = 97481;     // client item class 25480 "Yamamoto's Wedding Ring"
        private const uint ThraxSoldier = 531008, ThraxRavager = 531009;
        private const uint HighwayThraxPool = 790087;      // set 3's Thrax Soldier pool on the Sheitan Highway
        private const int Respawns = 6;                    // the rounds of respawns sampled after the first spawn
        private const float FloorReach = 1;                // a creature stands within this of the client's floor
        private const float PoolHeight = 2;                // and within this of its pool's height

        /// <summary>The Ashoka Settlement hospital (World DB teleporter 207), on Ashoka's lower level, where Latan and Ito stand.</summary>
        internal static readonly Vector3 AshokaHospital = new(594.41f, 248.5f, -307.91f);
        /// <summary>The top of the market's ramp, the navmesh's end next to the Ashoka Settlement waypoint (probe of 2026-11-09).</summary>
        internal static readonly Vector3 AshokaRampTop = new(428f, 277.05f, -467.2f);

        private static readonly Vector3 Ashoka = AshenLeadInsTests.AshokaWaypoint;
        private static readonly uint[] ThraxPools = Departed.Pools.Select(pool => pool.Pool).ToArray();

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// Latan, Ito and Megumi stand at their places on the navmesh with the client's names and packages, friendly. Latan and
        /// Ito stand on Ashoka's lower level: the navmesh walks to them from the hospital on that floor, from the Ashoka Upper
        /// teleporter (613) and from the top of the market's ramp. From the Ashoka Settlement waypoint, where the server navmesh
        /// does not join them (upstream N18), the client's floor reaches each of them and back through the Brann corridor under
        /// the dome (AshokaWalk, fix C). Megumi is walked to from the Shadow's Edge Post waypoint.
        /// </summary>
        [TestMethod]
        public void LatanItoAndMegumiStandWherePlayersWalkToThem()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, AshokaHospital);
            var nav = desert.NavMesh;
            bool Walked(Vector3 from, Vector3 to) => nav.FindPath(nav.Nearest(from) ?? from, to, out var complete) != null && complete;
            foreach (var (pool, name, package, from) in new (uint, uint, uint, Vector3[])[]
            {
                (K.Latan, 9277, 1449, new[] { AshokaHospital, AshokaRampTop, new Vector3(495.27f, 264.5f, -443.25f) }),
                (K.Ito, 9272, K.ItoPackage, new[] { AshokaHospital, AshokaRampTop, new Vector3(495.27f, 264.5f, -443.25f) }),
                (K.Megumi, 9295, K.MegumiPackage, new[] { AshenLeadInsTests.ShadowsEdgeWaypoint })
            })
            {
                var npc = Npc(desert, pool);
                Assert.AreEqual((name, package), (npc.NameId, npc.Npc.NpcPackageId), $"{pool}");
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory, $"{pool} friendly");
                var at = K.At(pool);
                Assert.IsLessThan(0.1f, Vector2.Distance(new((float)at.X, (float)at.Z), new(npc.Position.X, npc.Position.Z)), $"{pool} at its place");
                AssertOnGround(desert, npc.Position, $"{pool}");
                foreach (var start in from)
                    Assert.IsTrue(Walked(start, npc.Position), $"{pool} walked to from {start}");
            }

            var dataDir = F.ClientData();
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", harness.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
            foreach (var pool in new[] { K.Latan, K.Ito })
            {
                var at = Npc(desert, pool).Position;
                var down = AshokaWalk(nav, geometry, at, true);
                Assert.IsNull(down, $"{pool} from the waypoint: {down}");
                var up = AshokaWalk(nav, geometry, at, false);
                Assert.IsNull(up, $"{pool} back to the waypoint: {up}");
            }
        }

        /// <summary>
        /// Fix C (to-fix row 7), the negative control for AshokaWalk: the lower level's way up is the Brann corridor under the
        /// dome east of the waypoint (client map: ArchBrannCorridorEntrance01V01 at (416, 280, -472), its mouth at x 404 between
        /// walls at z -470.5 and -473.5, then two ArchBrannCorridorRamp01V01 down to the market, 264.5). The real client walked
        /// from the waypoint straight at the ramp's navmesh end (428, 277.05, -467.2) and stopped at (407.4-408, 281,
        /// -466.4..-467.8): the dome's wall (ArchBrannDome01V01, its face at x 408.1 there). That line fails the same check
        /// both ways, and from the real client's stop it fails on the dome's wall; the real client's slant from there into the
        /// mouth fails with a player's width (it caught on the north jamb). The client-floor flood of 2026-11-09 (0.5 m
        /// grid, terrain and the placed meshes, knee / chest / head) found no other way between the waypoint's terrace and the
        /// lower level.
        /// </summary>
        [TestMethod]
        public void TheRealClientsStraightLineIsWalledOff()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", harness.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
            Assert.IsNotNull(AshenFixBTests.ClientFloorLine(geometry, new[] { Ashoka, AshokaRampTop }, AshokaRampTop, InAshokaCorridor), "the real client's line down");
            Assert.IsNotNull(AshenFixBTests.ClientFloorLine(geometry, new[] { AshokaRampTop, Ashoka }, Ashoka, InAshokaCorridor), "the real client's line up");
            var stop = new Vector3(405f, 280.6f, -467f);
            var wall = AshenFixBTests.ClientFloorLine(geometry, new[] { stop, AshokaRampTop }, AshokaRampTop, InAshokaCorridor) ?? "";
            var at = System.Text.RegularExpressions.Regex.Match(wall, @"^something in the way at \w+ height from \(([0-9.]+),");
            Assert.IsTrue(at.Success && float.Parse(at.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) is >= 407f and <= 408.5f,
                $"the dome's wall stops the line from the real client's stop at x 407-408.5: {wall}");
            // The real client's "south corner route": from its stop at a slant to the mouth (404, -471.6), then in along
            // z -471.6; it caught at (403.7, 280.1, -470.2). A player's width must not pass that slant.
            var slant = new[] { new Vector3(407.4f, 280.8f, -467.8f), new Vector3(404f, 280.4f, -471.6f), new Vector3(414.8f, 280.5f, -471.6f) };
            Assert.IsNotNull(AshenFixBTests.ClientFloorLine(geometry, slant, slant[^1], InAshokaCorridor, PlayerHalfWidth), "the real client's slant into the mouth");
        }

        /// <summary>Half a player's width (HumanMale capsule radius, MarshesClientFloorTests).</summary>
        internal const float PlayerHalfWidth = 0.33f;

        /// <summary>
        /// The way from the waypoint down to the lower level, along the middles (fix C; client-floor flood of 2026-11-09): round
        /// the crates west of the corridor's mouth, then straight in along its middle from (398.5, -472) (the real client, coming
        /// in at a slant, caught on the mouth's north jamb at (403.7, 280.1, -470.2)), down both corridor ramps to the market
        /// (264.5), across it to the east corridor (z -440), down its ramp, then north along the cross corridor (x 597.5, clear
        /// of the tables and the bin at x 590.8-595) and down its ramp to the lower level's floor (248.5), where the navmesh
        /// goes on.
        /// </summary>
        internal static readonly Vector3[] AshokaCorridor =
        {
            AshenLeadInsTests.AshokaWaypoint, new(397f, 280.4f, -466f), new(398.5f, 280.2f, -472f), new(459f, 266.5f, -472f),
            new(525f, 264.5f, -470f), new(548f, 264.5f, -440f), new(597f, 260.5f, -440f), new(597.5f, 248.5f, -345f)
        };

        /// <summary>
        /// The client-floor walk (AshenFixBTests.ClientFloorLine: 0.5 m samples, at most 1.1 m up over a metre, knee / chest /
        /// head lines clear of the placed meshes on the line and PlayerHalfWidth to each side; the terrain is floor, except
        /// InAshokaCorridor) between the Ashoka Settlement waypoint and
        /// <paramref name="lower"/> on the lower level: AshokaCorridor, then the navmesh's walk. Null when it gets there.
        /// </summary>
        internal static string AshokaWalk(Rasa.Navigation.NavMeshQuery nav, MapGeometry geometry, Vector3 lower, bool down)
        {
            var foot = nav.Nearest(AshokaCorridor[^1]) ?? AshokaCorridor[^1];
            if (Vector2.Distance(new(foot.X, foot.Z), new(AshokaCorridor[^1].X, AshokaCorridor[^1].Z)) > 0.5f)
                return "the corridor's foot is off the navmesh";
            var from = down ? foot : nav.Nearest(lower) ?? lower;
            var path = nav.FindPath(from, down ? lower : foot, out var complete);
            if (path == null || !complete)
                return "no complete navmesh walk on the lower level";
            var corridor = AshokaCorridor.Take(AshokaCorridor.Length - 1).Append(foot);
            var corners = down
                ? corridor.Concat(path).ToList()
                : path.Prepend(from).Concat(corridor.Reverse().Skip(1)).ToList();
            return AshenFixBTests.ClientFloorLine(geometry, corners, down ? lower : AshenLeadInsTests.AshokaWaypoint, InAshokaCorridor, PlayerHalfWidth);
        }

        /// <summary>
        /// The corridor inside the dome (client map: the entrance at (416, 280, -472) and its two ramps reach x 404-464, z -480 to
        /// -464; the dome's west wall stands at x 408.1, so from x 412 on). Inside it the floor is the corridor's, not the heightmap: the terrain's flat 279.55 sheet runs through the first ramp
        /// (x 418-435, 0.3-5 m over its floor) and the mound over the second one. A Brann corridor ramp with the terrain through
        /// it is common in the client's maps (2026-11-09 survey: also Crucible (-144, 252, 288), (-28, 188, 296), Torden Abyss,
        /// Torden Incline); were the sheet floor or solid to the client, the lower level, its hospital (207) and vendors would
        /// have had no way up. ESTIMATE until the real client walks AshokaCorridor (NOTES-playtest).
        /// </summary>
        internal static bool InAshokaCorridor(Vector3 at) => at.X >= 412f && at.X <= 464f && at.Z >= -480f && at.Z <= -464f;

        /// <summary>
        /// The Boneyard's three Thrax pools (World DB, made point pools by set 6, 1285's markers): each puts down two or three
        /// Thrax Soldiers (531008) and at most one Thrax Ravager (531009), hostile. Round each pool's point (ClearRadius: the
        /// rings and a stroll) every navmesh floor is one floor only, joined to the Ashoka Settlement waypoint's ground, within
        /// 1 m of a client floor and under open sky (upstream's radius pools drew points up to 47 m out, onto rocks and 2-5 m
        /// off their rows' heights). At the first spawn and after each of six rounds of kills and respawns every Thrax stands
        /// on the navmesh, on the point or its rings, within 1 m of the client's floor, within 2 m of its pool's height, walked
        /// to from the Ashoka Settlement waypoint and inside its 1285 marker. Their classes have no IMMUNE_ flag (World DB
        /// creature_class_flag of 20757 and 9354).
        /// </summary>
        [TestMethod]
        public void TheBoneyardThraxComeDownAndBackOnTheFloorPlayersWalk()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var nav = desert.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", harness.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
            var markers = harness.World.MissionIndicatorEntries.AsEnumerable()
                .Where(row => row.ContentRevision == AshenMissionDataV1.Revision && row.MissionId == Departed.MissionId).ToList();
            Assert.HasCount(ThraxPools.Length, markers, "a marker on each pool");
            var immune = Enumerable.Range((int)CreatureFlag.ImmunePhysical, (int)CreatureFlag.ImmuneBlind - (int)CreatureFlag.ImmunePhysical + 1).Select(flag => (uint)flag).ToHashSet();
            foreach (var cls in new uint[] { 20757, 9354 })
                Assert.IsFalse(harness.World.CreatureClassFlagEntries.AsNoTracking().Where(flag => flag.ClassId == cls).AsEnumerable().Any(flag => immune.Contains(flag.FlagId)),
                    $"class {cls}: no immunity");
            Spawn(desert, ThraxPools);
            var failures = new List<string>();
            var region = nav.RegionArea(nav.Nearest(Ashoka).Value);
            foreach (var poolId in ThraxPools)
            {
                var pool = desert.SpawnPools.Single(loaded => loaded.DbId == poolId);
                Assert.AreEqual(0f, pool.Radius, $"{poolId} a point pool");
                Assert.IsTrue(SpawnPoolManager.HasWalkableGround(desert, pool), $"{poolId} has walkable ground");
                var columns = Columns(desert, pool.Position);
                Assert.IsGreaterThan(500, columns.Count, $"{poolId}: the floors round the point are read");
                var stacked = columns.Where(column => column.Count != 1).ToList();
                if (stacked.Count > 0)
                    failures.Add($"pool {poolId}: more than one navmesh floor at " + string.Join("; ", stacked.Take(3).Select(column => string.Join(" / ", column))));
                var ground = columns.SelectMany(column => column).ToList();
                var off = ground.Where(at => nav.RegionArea(at) != region).ToList();
                if (off.Count > 0)
                    failures.Add($"pool {poolId}: {off.Count} floors not joined to the waypoint, " + string.Join("; ", off.Take(3)));
                var round = F.Near(geometry, ground, 0.5f);
                for (var i = 0; i < ground.Count; i++)
                {
                    var at = ground[i];
                    // A navmesh floor up to 1.5 m over the client's (the mesh rides over stones) and not more than 1 m under it (as AshenFixATests).
                    if (F.FloorAt(round[i], at, at.Y - 2, at.Y + 1.5f) is not { } y || y - at.Y > 1 || at.Y - y > 1.5f)
                        failures.Add(FormattableString.Invariant($"pool {poolId}: no client floor under the navmesh at ({at.X:0.0}, {at.Y:0.00}, {at.Z:0.0})"));
                    if (round[i].Any(t => F.Hit(at + new Vector3(0, 1, 0), Vector3.UnitY, t.A, t.B, t.C) is { } up && up < 40))
                        failures.Add(FormattableString.Invariant($"pool {poolId}: a client surface over ({at.X:0.0}, {at.Y:0.00}, {at.Z:0.0})"));
                }
            }

            for (var round = 0; round <= Respawns; round++)
            {
                var here = Alive(desert).Where(candidate => candidate.SpawnPool != null && ThraxPools.Contains(candidate.SpawnPool.DbId)).Distinct().ToList();
                foreach (var poolId in ThraxPools)
                {
                    var soldiers = here.Count(one => one.SpawnPool.DbId == poolId && one.DbId == ThraxSoldier);
                    var ravagers = here.Count(one => one.SpawnPool.DbId == poolId && one.DbId == ThraxRavager);
                    Assert.IsTrue(soldiers is >= 2 and <= 3 && ravagers <= 1 && soldiers + ravagers == here.Count(one => one.SpawnPool.DbId == poolId),
                        $"round {round}: pool {poolId} has {soldiers} Soldiers and {ravagers} Ravagers");
                }
                var near = F.Near(geometry, here.Select(one => one.Position).ToList(), 1);
                for (var i = 0; i < here.Count; i++)
                {
                    var one = here[i];
                    var label = FormattableString.Invariant($"round {round}: pool {one.SpawnPool.DbId}'s {one.DbId} at {one.Position}");
                    Assert.AreEqual(TargetCategory.Hostile, one.TargetCategory, label);
                    if (!nav.IsOnMesh(one.Position))
                        failures.Add($"{label}: off the navmesh");
                    var path = nav.FindPath(nav.Nearest(Ashoka) ?? Ashoka, one.Position, out var complete);
                    if (path == null || !complete)
                        failures.Add($"{label}: no walk from the Ashoka Settlement waypoint");
                    var floor = F.FloorAt(near[i], one.Position, one.Position.Y - 3, one.Position.Y + 8);
                    if (floor is not { } y || MathF.Abs(y - one.Position.Y) > FloorReach)
                        failures.Add(FormattableString.Invariant($"{label}: the client's floor at {floor}"));
                    if (Vector2.Distance(new(one.Position.X, one.Position.Z), new(one.SpawnPool.Position.X, one.SpawnPool.Position.Z)) > Departed.PointSpread + 0.5f)
                        failures.Add($"{label}: off the point and its rings");
                    if (MathF.Abs(one.Position.Y - one.SpawnPool.Position.Y) > PoolHeight)
                        failures.Add(FormattableString.Invariant($"{label}: {one.Position.Y - one.SpawnPool.Position.Y:0.00} m from its pool's height"));
                    if (!markers.Any(marker => Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new(one.SpawnPool.Position.X, one.SpawnPool.Position.Z)) < 0.1f && Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new(one.Position.X, one.Position.Z)) <= marker.Radius))
                        failures.Add($"{label}: outside the 1285 markers");
                }
                if (round == Respawns)
                    break;
                foreach (var one in here)
                {
                    one.Attributes[Attributes.Health].Current = 0;
                    harness.Creatures.HandleCreatureKill(desert, one, harness.Client.Player);
                }
                Respawn(desert, ThraxPools);
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// Formal Complaints, choice 2, and the AFS side after it. Latan offers it (not Ito, not Yamamoto); Ito offers nothing
        /// before it is done. Accepted, Latan asks the client's choice (objective 1, variant 1) and hears nothing else; a marker
        /// at Latan on Ashen Desert. Choice 2 completes 1 once, sets flag 251 to 1 and 250 to 0, opens 3 (2 stays shut); a reload
        /// keeps it; Latan then hears 3 in the client's variant 251, which makes the mission completeable once. Ito cannot take
        /// it; a full bag waits, a failed save pays nothing; Latan pays the stack chosen, once. Then: Ito offers The Human
        /// Perspective (Yamamoto hears it in variant 251, Latan takes it); Latan offers A Bribe for Your Thoughts (her package
        /// at the accept, Yamamoto takes it in variant 251 and the mission); Yamamoto offers Court Martial Arts (the package
        /// again, Mason Linier takes it in variant 251 and the mission, once); Megumi offers Dearly Departed, Yamamoto does not.
        /// </summary>
        [TestMethod]
        public void HearingYamamotoFirstOpensTheAfsSide()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, AshokaHospital);
            var latan = Npc(desert, K.Latan);
            var ito = Npc(desert, K.Ito);
            var yamamoto = Npc(desert, N.Yamamoto);
            var linier = Npc(desert, N.MasonLinier);
            var megumi = Npc(desert, K.Megumi);

            var offer = WildernessRanjaGorgeTests.Offer(harness, latan, Complaints.MissionId);
            CollectionAssert.AreEqual(new[] { Complaints.EmpGrenade, Complaints.ResTraumaKit, Complaints.ConcussionGrenade, Complaints.BasicMedPack },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ito, Complaints.MissionId), "not Ito's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, yamamoto, Complaints.MissionId), "not Yamamoto's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ito, Human.MissionId), "The Human Perspective waits for Formal Complaints");

            Accept(harness, latan, Complaints.MissionId);
            CollectionAssert.AreEqual(new[] { (Complaints.Discuss, 1U) }, Choices(harness, latan, Complaints.MissionId), "Latan asks the choice");
            Assert.IsEmpty(Topics(harness, latan, Complaints.MissionId), "and hears nothing yet");
            var snapshot = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Complaints.MissionId].ObjectivesList;
            Assert.AreEqual(Complaints.Discuss, snapshot.Single().ObjectiveId, "only 1 shows (4 is hidden, 2 and 3 shut)");
            Assert.AreEqual(AshenMissionDataV1.MapContextId, snapshot.Single().IndicatorList.Single().MapContextId, "a marker on Ashen Desert");

            var packets = Choose(harness, latan, Complaints.MissionId, Complaints.Discuss, Complaints.HearYamamoto);
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Complaints.MissionId && packet.ObjectiveId == Complaints.Discuss), "1 once");
            Assert.IsFalse(packets.OfType<MissionCompleteablePacket>().Any(packet => packet.MissionId == Complaints.MissionId && packet.IsCompleteable), "not yet completeable");
            Assert.AreEqual((1U, 0U), (Flag(harness, Complaints.YamamotoFlag), Flag(harness, Complaints.LatanFlag)), "flag 251 set, 250 not");
            Assert.AreEqual((MissionObjectiveState.Inactive, MissionObjectiveState.Incomplete),
                (Objective(harness, Complaints.MissionId, Complaints.LatanSide), Objective(harness, Complaints.MissionId, Complaints.YamamotoSide)), "3 opens, 2 stays shut");
            AshenRuntime.Reload(harness);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Complaints.MissionId, Complaints.YamamotoSide), "kept at a reload");
            Assert.IsEmpty(Choices(harness, latan, Complaints.MissionId), "no second choice");
            CollectionAssert.AreEqual(new[] { (Complaints.YamamotoSide, Complaints.YamamotoFlag) }, TopicVariants(harness, latan, Complaints.MissionId), "3 in variant 251");
            packets = Say(harness, latan, Complaints.MissionId, Complaints.YamamotoSide, Complaints.YamamotoFlag);
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Complaints.MissionId && packet.IsCompleteable), "MissionCompleteable once");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ito, Human.MissionId), "not before Formal Complaints is turned in");
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, ito.EntityId, Complaints.MissionId, 3), "not Ito's to take");
            FullBagWaits(harness, latan, Complaints.MissionId, 3, ConsumableFiller);
            FailedTurnInPaysNothing(harness, latan, Complaints.MissionId, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, latan, Complaints.MissionId, 3, Complaints.Experience, Complaints.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Complaints.MissionId), "MissionRewarded once");
            Assert.IsGreaterThanOrEqualTo(Complaints.BasicMedPacks, WildernessRanjaGorgeTests.Held(harness, Complaints.BasicMedPack), "the stack chosen");
            Unfill(harness, ConsumableFiller);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, latan, Complaints.MissionId), "done once");

            // The Human Perspective.
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, latan, Bribe.MissionId), "A Bribe for Your Thoughts waits for The Human Perspective");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, ito, Human.MissionId), "Ito offers The Human Perspective");
            Accept(harness, ito, Human.MissionId);
            Assert.IsEmpty(Topics(harness, ito, Human.MissionId), "Ito hears nothing");
            Assert.IsEmpty(Topics(harness, latan, Human.MissionId), "Latan hears nothing");
            CollectionAssert.AreEqual(new[] { (Human.Speak, Complaints.YamamotoFlag) }, TopicVariants(harness, yamamoto, Human.MissionId), "Yamamoto in variant 251");
            AshenRuntime.Reload(harness);
            harness.Drain();
            packets = Say(harness, yamamoto, Human.MissionId, Human.Speak, Complaints.YamamotoFlag);
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Human.MissionId && packet.IsCompleteable), "MissionCompleteable once");
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, yamamoto.EntityId, Human.MissionId, 0), "not Yamamoto's to take");
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, ito.EntityId, Human.MissionId, 0), "not Ito's to take");
            BagFullAgainWaits(harness, latan, Human.MissionId, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, latan, Human.MissionId, 0, Human.Experience, Human.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Human.MissionId), "MissionRewarded once");

            // A Bribe for Your Thoughts.
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, yamamoto, Court.MissionId), "Court Martial Arts waits for the bribe");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, latan, Bribe.MissionId), "Latan offers A Bribe for Your Thoughts");
            Accept(harness, latan, Bribe.MissionId);
            Assert.AreEqual(1U, Package(harness), "Latan's Package at the accept");
            CollectionAssert.AreEqual(new[] { (Bribe.Deliver, Complaints.YamamotoFlag) }, TopicVariants(harness, yamamoto, Bribe.MissionId), "Yamamoto in variant 251");
            packets = Say(harness, yamamoto, Bribe.MissionId, Bribe.Deliver, Complaints.YamamotoFlag);
            Assert.AreEqual(0U, Package(harness), "Yamamoto takes it");
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Bribe.MissionId && packet.IsCompleteable), "MissionCompleteable once");
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, latan.EntityId, Bribe.MissionId, 0), "not Latan's to take");
            BagFullAgainWaits(harness, yamamoto, Bribe.MissionId, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, yamamoto, Bribe.MissionId, 1, Bribe.Experience, Bribe.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Bribe.MissionId), "MissionRewarded once");

            // Court Martial Arts.
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, megumi, Departed.MissionId), "Dearly Departed waits for Court Martial Arts");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, linier, Court.MissionId), "not Linier's to give");
            Accept(harness, yamamoto, Court.MissionId);
            Assert.AreEqual(1U, Package(harness), "the package again, from Yamamoto");
            Assert.IsEmpty(Topics(harness, yamamoto, Court.MissionId), "Yamamoto hears nothing");
            CollectionAssert.AreEqual(new[] { (Court.Deliver, Complaints.YamamotoFlag) }, TopicVariants(harness, linier, Court.MissionId), "Linier in variant 251");
            packets = Say(harness, linier, Court.MissionId, Court.Deliver, Complaints.YamamotoFlag);
            Assert.AreEqual(0U, Package(harness), "Linier takes it");
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Court.MissionId && packet.IsCompleteable), "MissionCompleteable once");
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, yamamoto.EntityId, Court.MissionId, 2), "not Yamamoto's to take");
            BagFullAgainWaits(harness, linier, Court.MissionId, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, linier, Court.MissionId, 3, Court.Experience, Court.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Court.MissionId), "MissionRewarded once");
            Assert.IsGreaterThanOrEqualTo(Court.StandardMedPacks, WildernessRanjaGorgeTests.Held(harness, Court.StandardMedPack), "the med packs chosen");

            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, megumi, Departed.MissionId), "Megumi offers Dearly Departed");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, yamamoto, Departed.MissionId), "not Yamamoto's to give");
        }

        /// <summary>
        /// Formal Complaints, choice 1: it sets flag 250 to 1 and 251 to 0 and opens 2 (3 stays shut); Latan hears 2 in the
        /// client's variant 250; turned in, Ito never offers The Human Perspective. Before that an abandon after choice 2 keeps
        /// its flag; taken again, the choice is asked again, and choice 1 turns the flags round: only one side is ever set.
        /// </summary>
        [TestMethod]
        public void SidingWithLatanKeepsTheAfsSideShut()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, AshokaHospital);
            var latan = Npc(desert, K.Latan);
            var ito = Npc(desert, K.Ito);

            Accept(harness, latan, Complaints.MissionId);
            Choose(harness, latan, Complaints.MissionId, Complaints.Discuss, Complaints.HearYamamoto);
            Assert.AreEqual((1U, 0U), (Flag(harness, Complaints.YamamotoFlag), Flag(harness, Complaints.LatanFlag)));
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Complaints.MissionId));
            harness.Drain();
            Assert.AreEqual(1U, Flag(harness, Complaints.YamamotoFlag), "the flag stays at an abandon");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ito, Human.MissionId), "but Formal Complaints is not done");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, latan, Complaints.MissionId), "offered again after an abandon");

            Accept(harness, latan, Complaints.MissionId);
            CollectionAssert.AreEqual(new[] { (Complaints.Discuss, 1U) }, Choices(harness, latan, Complaints.MissionId), "the choice again");
            var packets = Choose(harness, latan, Complaints.MissionId, Complaints.Discuss, Complaints.SideWithLatan);
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Complaints.MissionId && packet.ObjectiveId == Complaints.Discuss), "1 once");
            Assert.AreEqual((1U, 0U), (Flag(harness, Complaints.LatanFlag), Flag(harness, Complaints.YamamotoFlag)), "flag 250 set, 251 back to 0");
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Objective(harness, Complaints.MissionId, Complaints.LatanSide), Objective(harness, Complaints.MissionId, Complaints.YamamotoSide)), "2 opens, 3 stays shut");
            CollectionAssert.AreEqual(new[] { (Complaints.LatanSide, Complaints.LatanFlag) }, TopicVariants(harness, latan, Complaints.MissionId), "2 in variant 250");
            packets = Say(harness, latan, Complaints.MissionId, Complaints.LatanSide, Complaints.LatanFlag);
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Complaints.MissionId && packet.IsCompleteable), "MissionCompleteable once");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, latan, Complaints.MissionId, 0, Complaints.Experience, Complaints.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Complaints.MissionId), "MissionRewarded once");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ito, Human.MissionId), "Latan's side: no The Human Perspective");
            AshenRuntime.Reload(harness);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ito, Human.MissionId), "not after a reload either");
            Assert.AreEqual((1U, 0U), (Flag(harness, Complaints.LatanFlag), Flag(harness, Complaints.YamamotoFlag)), "the flags kept");
        }

        /// <summary>
        /// A Bribe for Your Thoughts and Court Martial Arts with a bag that has no room: the accept is refused (no package, no
        /// mission); one slot free, the package comes with it. An abandon takes the package; taken again, a new one (not two).
        /// </summary>
        [TestMethod]
        public void ThePackageNeedsRoomAndGoesWithAnAbandon()
        {
            foreach (var (mission, giver, done) in new (uint, uint, uint[])[]
            {
                (Bribe.MissionId, K.Latan, new[] { Complaints.MissionId, Human.MissionId }),
                (Court.MissionId, N.Yamamoto, new[] { Complaints.MissionId, Human.MissionId, Bribe.MissionId })
            })
            {
                using var harness = WildernessRuntimeTestHarness.Create();
                AshenRuntime.SeedCompleted(harness, done);
                SeedFlag(harness, Complaints.YamamotoFlag, 1);
                var desert = AshenLeadInsTests.EnterAshen(harness, AshokaHospital);
                var npc = Npc(desert, giver);
                WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
                harness.MoveTo(npc.Position);
                harness.Drain();
                Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, npc.EntityId, mission), $"{mission}: no room for the package");
                Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(mission));
                Assert.AreEqual(0U, Package(harness), $"{mission}: none given");
                WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
                Accept(harness, npc, mission);
                Assert.AreEqual(1U, Package(harness), $"{mission}: the package at the accept");
                Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, mission));
                harness.Drain();
                Assert.AreEqual(0U, Package(harness), $"{mission}: the package goes with the mission");
                Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, npc, mission), $"{mission}: offered again after an abandon");
                Accept(harness, npc, mission);
                Assert.AreEqual(1U, Package(harness), $"{mission}: one again");
            }
        }

        /// <summary>
        /// Dearly Departed: Megumi offers it (not Yamamoto); no ring drops before it. Accepted, Megumi and Yamamoto hear no
        /// objective; a marker on each Boneyard Thrax pool. A Thrax Soldier drops the ring under 15 only (15 misses, 14 drops);
        /// a death is not a pickup; a full bag leaves it on the corpse, one slot free and it is picked up; that completes 1 once
        /// and the mission is completeable once; the next Thrax drops none; a reload keeps it. Megumi cannot take it; a full bag
        /// waits, a failed save pays nothing and keeps the ring; Yamamoto takes it and pays the item chosen, once. A Thrax of the
        /// Sheitan Highway drops it too (the drop is bound to the rows, not the Boneyard: deviation).
        /// </summary>
        [TestMethod]
        public void DearlyDepartedFindsTheRingOnAThrax()
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            AshenRuntime.SeedCompleted(harness, Complaints.MissionId, Human.MissionId, Bribe.MissionId, Court.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            var megumi = Npc(desert, K.Megumi);
            var yamamoto = Npc(desert, N.Yamamoto);
            Spawn(desert, ThraxPools.Append(HighwayThraxPool).ToArray());

            var offer = WildernessRanjaGorgeTests.Offer(harness, megumi, Departed.MissionId);
            CollectionAssert.AreEqual(new[] { Departed.StealthBoots, Departed.GravitonHelmet, Departed.HazmatLegs, Departed.MotorAssistLegs },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, yamamoto, Departed.MissionId), "not Yamamoto's to give");
            Assert.IsFalse(Holds(KillThrax(harness, desert, ThraxPools), ClientRingTemplate), "no ring before the mission");
            Accept(harness, megumi, Departed.MissionId);
            Assert.IsEmpty(Topics(harness, megumi, Departed.MissionId), "Megumi hears no objective");
            Assert.IsEmpty(Topics(harness, yamamoto, Departed.MissionId), "Yamamoto hears no objective: the ring counts as it is picked up");
            var markers = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Departed.MissionId].ObjectivesList.Single().IndicatorList;
            Assert.HasCount(ThraxPools.Length, markers, "a marker on each Boneyard Thrax pool");
            Assert.IsTrue(markers.All(marker => marker.MapContextId == AshenMissionDataV1.MapContextId), "on Ashen Desert");

            percentile = 15;
            Assert.IsFalse(Holds(KillThrax(harness, desert, ThraxPools), ClientRingTemplate), "15 misses");
            percentile = 14;
            var highway = KillThrax(harness, desert, new[] { HighwayThraxPool });
            Assert.IsTrue(Holds(highway, ClientRingTemplate), "a Sheitan Highway Thrax drops it too (deviation)");
            var loot = KillThrax(harness, desert, ThraxPools);
            Assert.IsTrue(Holds(loot, ClientRingTemplate), "14 drops the ring");
            Assert.AreEqual(0U, Ring(harness), "a death is not a pickup");
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            PickUp(harness, loot);
            Assert.AreEqual(0U, Ring(harness), "no room");
            Assert.IsTrue(Holds(loot, ClientRingTemplate), "still on the corpse");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, loot);
            var packets = harness.Drain();
            Assert.AreEqual(1U, Ring(harness), "picked up once there is room");
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Departed.MissionId), "ObjectiveCompleted once");
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Departed.MissionId && packet.IsCompleteable), "MissionCompleteable once");
            Assert.IsFalse(Holds(KillThrax(harness, desert, ThraxPools), ClientRingTemplate), "one ring: the next Thrax has none");
            PickUp(harness, highway);
            Assert.AreEqual(1U, Ring(harness), "never two");
            AshenRuntime.Reload(harness);
            Assert.AreEqual((1U, MissionObjectiveState.Completed), (Ring(harness), Objective(harness, Departed.MissionId, Departed.Recover)), "kept at a reload");
            harness.Drain();

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, megumi.EntityId, Departed.MissionId, 1), "not Megumi's to take");
            FullBagWaits(harness, yamamoto, Departed.MissionId, 1, ArmorFiller);
            FailedTurnInPaysNothing(harness, yamamoto, Departed.MissionId, 1);
            Assert.AreEqual(1U, Ring(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, yamamoto, Departed.MissionId, 1, Departed.Experience, Departed.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Departed.MissionId), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Departed.GravitonHelmet), "the helmet chosen");
            Assert.AreEqual(0U, Ring(harness), "Yamamoto takes the ring");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, megumi, Departed.MissionId), "done once");
        }

        /// <summary>An abandon of Dearly Departed keeps the ring (as Harmful Mutations' samples); taken again, it counts at once.</summary>
        [TestMethod]
        public void DearlyDepartedAbandonedKeepsTheRing()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            AshenRuntime.SeedCompleted(harness, Complaints.MissionId, Human.MissionId, Bribe.MissionId, Court.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            var megumi = Npc(desert, K.Megumi);
            Spawn(desert, ThraxPools);
            Accept(harness, megumi, Departed.MissionId);
            PickUp(harness, KillThrax(harness, desert, ThraxPools));
            harness.Drain();
            Assert.AreEqual(1U, Ring(harness));
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Departed.MissionId));
            harness.Drain();
            Assert.AreEqual(1U, Ring(harness), "the ring is kept");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, megumi, Departed.MissionId), "offered again after an abandon");
            Accept(harness, megumi, Departed.MissionId);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Departed.MissionId, Departed.Recover), "the ring held counts");
            Assert.IsTrue(harness.Client.Player.Missions[Departed.MissionId].Completeable);
        }

        /// <summary>
        /// A later full bag in the same run (the inbox still full from FullBagWaits): filler stacks until no more fit, the
        /// turn-in pays nothing and the mission stays to be turned in; then the filler goes again.
        /// </summary>
        private static void BagFullAgainWaits(WildernessRuntimeTestHarness harness, Creature receiver, uint mission, int? choice)
        {
            var template = ItemManager.Instance.GetItemTemplateById(ConsumableFiller);
            var stack = EntityClassManager.Instance.GetClassInfo(template.Class).ItemClassInfo.StackSize;
            for (var stacks = 0; ; stacks++)
            {
                Assert.IsLessThan(200, stacks, "the bag fills up");
                try
                {
                    WildernessAliaBranchesTests.GrantItem(harness, ConsumableFiller, stack);
                }
                catch (GameplayRejectionException)
                {
                    break;
                }
            }
            harness.MoveTo(receiver.Position);
            harness.Drain();
            var before = harness.Context.ReadRewardTotals();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, receiver.EntityId, mission, choice), $"{mission}: no room");
            Assert.AreEqual(before, harness.Context.ReadRewardTotals(), $"{mission}: nothing paid");
            Assert.IsTrue(harness.Client.Player.Missions[mission].Completeable, $"{mission}: still to be turned in");
            Unfill(harness, ConsumableFiller);
        }

        /// <summary>The filler's stacks out of the backpack again (the inbox stays full).</summary>
        private static void Unfill(WildernessRuntimeTestHarness harness, uint filler)
        {
            while (harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0).Select(EntityManager.Instance.GetItem)
                .Any(item => item.ItemTemplate.ItemTemplateId == filler))
                WildernessAliaBranchesTests.FreeOneSlot(harness, filler);
            harness.Drain();
        }

        private static uint Package(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, ClientPackageTemplate);

        private static uint Ring(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, ClientRingTemplate);

        /// <summary>A kill of a living Thrax Soldier of these pools (respawned when none is left); its corpse.</summary>
        private static LootDispenser KillThrax(WildernessRuntimeTestHarness harness, MapChannel desert, uint[] pools)
        {
            if (!Alive(desert).Any(candidate => candidate.DbId == ThraxSoldier && candidate.Npc == null && candidate.SpawnPool != null && pools.Contains(candidate.SpawnPool.DbId)))
                Respawn(desert, pools);
            var one = Alive(desert).First(candidate => candidate.DbId == ThraxSoldier && candidate.Npc == null && candidate.SpawnPool != null && pools.Contains(candidate.SpawnPool.DbId));
            harness.MoveTo(one.Position);
            one.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(desert, one, harness.Client.Player);
            Assert.IsTrue(desert.LootDispensers.TryGetValue(one.CorpseLootEntityId, out var loot), "a corpse");
            return loot;
        }

        /// <summary>The choice <paramref name="choice"/> of <paramref name="mission"/>'s objective at <paramref name="npc"/> (as CrucibleAwolCampTests.Choose); its packets.</summary>
        internal static List<object> Choose(WildernessRuntimeTestHarness harness, Creature npc, uint mission, uint objective, uint choice)
        {
            var npcs = new NpcManager(harness, harness.Manager);
            harness.MoveTo(npc.Position);
            harness.Drain();
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = npc.EntityId });
            npcs.PerformNPCChoice(harness.Client, new PerformNPCChoicePacket
            {
                EntityId = npc.EntityId, MissionId = mission, ObjectiveId = objective, PlayerFlagId = 1, ChoiceIdx = (int)choice
            });
            return harness.Drain().Cast<object>().ToList();
        }

        /// <summary>A talk the NPC hears in this variant, done; the packets it sent.</summary>
        internal static List<object> Say(WildernessRuntimeTestHarness harness, Creature npc, uint mission, uint objective, uint variant)
        {
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, mission, objective, variant),
                $"{npc.DbId} hears {mission}.{objective} (variant {variant})");
            var packets = harness.Drain().Cast<object>().ToList();
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == mission && packet.ObjectiveId == objective),
                $"{mission}.{objective} ObjectiveCompleted once");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, mission, objective));
            return packets;
        }

        /// <summary>The choice topics of <paramref name="mission"/> that <paramref name="npc"/> offers now: objective and variant.</summary>
        private static (uint, uint)[] Choices(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.ObjectiveChoice, out var value)
                ? ((List<ChoiceObjectives>)value).Where(topic => topic.MissionId == mission)
                    .Select(topic => ((uint)topic.ObjectiveId, (uint)topic.PlayerFlagId)).ToArray()
                : Array.Empty<(uint, uint)>();

        /// <summary>The hand-in topics of <paramref name="mission"/> that <paramref name="npc"/> offers now: objective and variant.</summary>
        internal static (uint, uint)[] TopicVariants(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.ObjectiveComplete, out var value)
                ? ((List<CompleteableObjectives>)value).Where(topic => topic.MissionId == mission)
                    .Select(topic => ((uint)topic.ObjectiveId, (uint)topic.PlayerFlagId)).ToArray()
                : Array.Empty<(uint, uint)>();

        /// <summary>A player flag as the character database holds it (0: none).</summary>
        internal static uint Flag(WildernessRuntimeTestHarness harness, uint flag)
        {
            using var unit = harness.CreateChar();
            return unit.CharacterFlags.Get(harness.Client.Player.Id).TryGetValue(flag, out var value) ? value : 0;
        }

        /// <summary>A player flag set in the character database, then a reload.</summary>
        internal static void SeedFlag(WildernessRuntimeTestHarness harness, uint flag, uint value)
        {
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() => unit.CharacterFlags.Set(harness.Client.Player.Id, flag, value));
            AshenRuntime.Reload(harness);
        }

        /// <summary>The navmesh floors on a 1 m grid within Departed.ClearRadius of <paramref name="centre"/> (as AshenFixATests.Columns).</summary>
        private static List<List<Vector3>> Columns(MapChannel desert, Vector3 centre)
        {
            var columns = new List<List<Vector3>>();
            var reach = (int)Math.Ceiling(Departed.ClearRadius);
            for (var dz = -reach; dz <= reach; dz++)
                for (var dx = -reach; dx <= reach; dx++)
                {
                    if (dx * dx + dz * dz > Departed.ClearRadius * Departed.ClearRadius)
                        continue;
                    var heights = new SortedSet<float>();
                    for (var h = -8; h <= 12; h++)
                    {
                        var probe = new Vector3(centre.X + dx, centre.Y + h, centre.Z + dz);
                        if (desert.NavMesh.Nearest(probe) is { } nearest && MathF.Abs(nearest.X - probe.X) < 0.3f && MathF.Abs(nearest.Z - probe.Z) < 0.3f &&
                            desert.NavMesh.GroundHeight(probe) is { } y)
                            heights.Add(MathF.Round(y, 1));
                    }
                    if (heights.Count == 0)
                        continue;
                    var column = new List<Vector3>();
                    foreach (var y in heights)
                        if (column.Count == 0 || y - column[^1].Y >= 2)
                            column.Add(new Vector3(centre.X + dx, y, centre.Z + dz));
                    columns.Add(column);
                }
            return columns;
        }
    }
}
