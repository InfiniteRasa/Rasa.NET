using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.ClientData;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Missions.Scenes;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Services.Preloader.Missions.Crucible;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using static MarshesFalconHoldTests;
    using static MarshesPaludosTests;
    using static MarshesPreoDasTests;
    using static PlateauCampResistanceTests;
    using N = AshenConscriptionQueenNpcsV1;
    using Entry = AshenConscriptionEntryTests;
    using Lockdown = AshenLockdownTheConscriptionFacilityV1;
    using Raid = AshenRaidTheConscriptionFacilityV1;
    using Blast = AshenBlastTheBiolabV1;
    using Tick551 = AshenWhatMakesThemTickV1;
    using V2 = AshenFixV2V1;
    using Den = AshenIntoTheLionsDenV1;
    using Queen = AshenSaveTheQueenV1;
    using Facility = AshenIndraFacilityTests;
    using F = MarshesClientFloorTests;
    using WO = AshenWhiteOasisTests;

    /// <summary>
    /// Ashen Desert set 20, the Bane Conscription Facility (squad instance 1988), the Atta queen part, played through as a
    /// player plays it: What Makes Them Tick (1551, by radio after 1498: ten Enraged Atta in the east wing, the Atta Hive
    /// Queen's kill, the DNA sample's talk by her body, the turn-in at Yamamoto on Ashen Desert), Into the Lion's Den (1499, by
    /// radio after 1498: the arena within ten minutes, then either Humphries' pit fight and his talk or the ledge's fight, the
    /// Cell Door Control and Lukaz' talk; the first side done shuts the other out; the turn-in by radio) and Save the Queen
    /// (1501, by radio after 1499: Information Extraction, Enslaver Xahn's kill, the queen's talk, the Life Support System, the
    /// egg's talk, Yamamoto, then Jawan on Thunderhead and his turn-in). With them the set's places and walks on the client's
    /// floor, the two pad pairs from the HQ hall.
    ///
    /// Why these tests: a deadline that does not fail 1, or fails it after the arena; a side that does not shut the other out,
    /// or a mission that completes without a talk; a shared package (1937) that lets the DNA sample's talk complete 1501's
    /// queen or the queen's 1551's; a radio offer one member never gets; a pad that does not take the player both ways; a talk
    /// on another map (Yamamoto, Jawan) that the facility-bound scene stops would each break the chain.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenConscriptionQueenTests
    {
        private const uint Map = N.Facility;

        /// <summary>The set's missions.</summary>
        internal static readonly uint[] Missions = { Tick551.MissionId, Den.MissionId, Queen.MissionId };

        /// <summary>The pads set 20 adds.</summary>
        internal static readonly uint[] Pads = N.Pads.Select(pad => pad.Id).ToArray();

        private static readonly uint[] Pools = { N.EnragedAttaPool, N.QueenPool, N.XahnPool };

        private static readonly uint[] Before1551 = { AshenTheBigSecretV1.MissionId, Lockdown.MissionId, Raid.MissionId, Blast.MissionId };

        private const float PlayerHalfWidth = AshenAvernusOutpostTests.PlayerHalfWidth;

        private Func<MissionApplication> _missions;

        [TestInitialize]
        public void Clock()
        {
            PlateauGiantKillerSquadTests.UseClock();
            _missions = SpawnPoolClearedProgress.Missions;
        }

        [TestCleanup]
        public void Restore()
        {
            PlateauGiantKillerSquadTests.RestoreClock();
            SpawnPoolClearedProgress.Missions = _missions;
        }

        /// <summary>The set's markers: mission, objective, x, z, radius (AshenMissionTests.TheMarkersStandAtTheirObjectivesTargets).</summary>
        internal static IEnumerable<(uint Mission, uint Objective, double X, double Z, double Radius)> Markers()
        {
            yield return (Tick551.MissionId, Tick551.KillTheAtta, N.EnragedAttaAt.X, N.EnragedAttaAt.Z, Tick551.PoolMarkerRadius);
            yield return (Tick551.MissionId, Tick551.KillTheQueen, N.QueenAt.X, N.QueenAt.Z, Tick551.QueenMarkerRadius);
            yield return (Den.MissionId, Den.ToTheArena, Den.ArenaMarkerAt.X, Den.ArenaMarkerAt.Z, Den.ArenaRadius);
            yield return (Den.MissionId, Den.HelpHumphries, Den.HumphriesAt.X, Den.HumphriesAt.Z, Den.FightMarkerRadius);
            yield return (Den.MissionId, Den.HelpThePrisoners, Den.LukazAt.X, Den.LukazAt.Z, Den.FightMarkerRadius);
            yield return (Den.MissionId, Den.SpeakHumphries, Den.HumphriesAt.X, Den.HumphriesAt.Z, Den.NpcMarkerRadius);
            yield return (Den.MissionId, Den.SpeakLukaz, Den.LukazAt.X, Den.LukazAt.Z, Den.NpcMarkerRadius);
            yield return (Queen.MissionId, Queen.FindTheQueen, Queen.ExtractionMarkerAt.X, Queen.ExtractionMarkerAt.Z, Queen.ExtractionRadius);
            yield return (Queen.MissionId, Queen.KillTheEnslaver, N.XahnAt.X, N.XahnAt.Z, Queen.BossMarkerRadius);
            yield return (Queen.MissionId, Queen.TalkToTheQueen, Queen.QueenAt.X, Queen.QueenAt.Z, Queen.ObjectMarkerRadius);
            yield return (Queen.MissionId, Queen.EndTheSuffering, V2.ConsoleAt.X, V2.ConsoleAt.Z, Queen.ObjectMarkerRadius);   // fix V2: on the console
            yield return (Queen.MissionId, Queen.SecureTheEgg, Queen.EggAt.X, Queen.EggAt.Z, Queen.ObjectMarkerRadius);
            yield return (Queen.MissionId, Queen.ToYamamoto, AshenLeadInNpcsV1.YamamotoAt.X, AshenLeadInNpcsV1.YamamotoAt.Z, Queen.NpcMarkerRadius);
            yield return (Queen.MissionId, Queen.ToJawan, N.JawanAt.X, N.JawanAt.Z, Queen.NpcMarkerRadius);
        }

        /// <summary>The map a set 20 marker is on: 1501's sixth on Ashen Desert (Yamamoto), its seventh on Thunderhead (Jawan), all others in the facility.</summary>
        internal static uint MarkerMap(uint mission, uint indicator) =>
            mission != Queen.MissionId ? Map
            : indicator == MissionIndicator.UnnamedFrom + Queen.ToYamamoto ? AshenMissionDataV1.MapContextId
            : indicator == MissionIndicator.UnnamedFrom + Queen.ToJawan ? N.Thunderhead : Map;

        /// <summary>Of the set's markers, on Ashen Desert: 1501's at Yamamoto.</summary>
        internal const int DesertMarkers = 1;

        /// <summary>
        /// The rows of the three missions: no giver (all three by radio), the receivers (1551 Yamamoto, 1499 by radio, 1501 Jawan),
        /// levels, the Ashen category, the client's texts and talks (1551,2 the sample 1937; 1499,5 Humphries 1936, 1499,6 Lukaz
        /// 1934; 1501,3 and 1501,5 the queen 1937, 1501,6 Yamamoto 1414, 1501,7 Jawan 1648), the requirements (1498 → 1551, 1499;
        /// 1499 → 1501); the markers on their maps, unnamed; each mission operational.
        /// </summary>
        [TestMethod]
        public void TheSetsRowsAreTheClients()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            const string revision = AshenMissionDataV1.Revision;
            var expected = new (uint Mission, uint? Receiver, uint Level, uint Name, uint Requires, bool Radio, (uint, uint?)[] Talks)[]
            {
                (Tick551.MissionId, AshenLeadInNpcsV1.Yamamoto, 43, 14075, Blast.MissionId, false, new (uint, uint?)[] { (2, 1937) }),
                (Den.MissionId, null, 43, 13389, Blast.MissionId, true, new (uint, uint?)[] { (5, 1936), (6, 1934) }),
                (Queen.MissionId, N.Jawan, 44, 13399, Den.MissionId, false, new (uint, uint?)[] { (3, 1937), (5, 1937), (6, 1414), (7, 1648) })
            };
            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Mission>(), null);
            catalog.Load();
            foreach (var (mission, receiver, level, name, requires, radio, talks) in expected)
            {
                var definition = world.MissionContentDefinitionEntries.AsNoTracking().Single(entry => entry.MissionId == mission && entry.ContentRevision == revision);
                Assert.AreEqual(((uint?)null, receiver, (uint?)level, (uint?)AshenMissionDataV1.CategoryId, name, true, radio),
                    (definition.GiverId, definition.ReceiverId, definition.Level, definition.CategoryId, definition.ClientNameTextId, definition.Enabled,
                        definition.RadioCompleteable), $"{mission}");
                var talkRows = world.Set<MissionTriggerEntry>().AsNoTracking()
                    .Where(entry => entry.MissionId == mission && entry.ContentRevision == revision && entry.Kind == MissionTriggerKind.Conversation)
                    .AsEnumerable().Select(entry => (entry.ObjectiveId, entry.NpcPackageId)).OrderBy(entry => entry.ObjectiveId).ToArray();
                CollectionAssert.AreEqual(talks, talkRows, $"{mission}'s talks");
                var prerequisites = world.MissionPrerequisiteEntries.AsNoTracking().Where(entry => entry.MissionId == mission && entry.ContentRevision == revision).ToList();
                Assert.AreEqual(requires, prerequisites.Single().RequiredMissionId, $"{mission} requires {requires}");
                Assert.IsTrue(harness.Manager.LoadedMissions[mission].IsOperational, harness.Manager.LoadedMissions[mission].OperationalDiagnostic);
            }

            var markers = world.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == revision && Missions.Contains(entry.MissionId)).AsEnumerable()
                .Select(entry => (entry.MissionId, entry.ObjectiveId, entry.IndicatorId,
                    Map: MissionMarkerProjection.ResolveMap(catalog, entry.MissionId, entry.ObjectiveId, new MissionIndicator { IndicatorId = entry.IndicatorId })))
                .ToArray();
            Assert.HasCount(2 + 5 + 7, markers, "the set's markers");
            foreach (var (mission, objective, indicator, map) in markers)
            {
                Assert.AreEqual(MarkerMap(mission, indicator), map, $"{mission}.{objective}/{indicator}: its map");
                Assert.IsNull(AshenMissionTests.NamedMarker(mission, objective), $"{mission}.{objective}: the client names none");
                Assert.IsGreaterThan(MissionIndicator.UnnamedFrom, indicator, $"{mission}.{objective}: unnamed");
            }
        }

        /// <summary>
        /// The set's rows in the id block 790662-790671: Humphries, Lukaz, the Brann Prisoner and Jawan friendly with the client's
        /// names, levels and packages (the prisoner none), the class of the row each copies (Harris, a Brann); the Enraged Atta,
        /// the queen and Xahn hostile with the client's names and the classes the plan names. The pools: the Enraged Atta four in
        /// one, the queen and Xahn alone, on the facility; Jawan alone on Thunderhead; the Enraged Atta's and the queen's 60 m+
        /// outside every other hostile pool's circle (100 m+ a warcry pool's); Xahn's among the Enslaver cells (the break, 19
        /// m+); Jawan's 100 m+ outside every hostile pool's on Thunderhead. The pads: four local teleporters (type 1, class 8044)
        /// on the facility linked in two pairs and to nothing else. The places' clearances as Places gives them.
        /// </summary>
        [TestMethod]
        public void TheSetsCreaturesAndPadsStandApart()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var rows = world.CreatureEntries.AsNoTracking().Where(row => row.Id >= 790662 && row.Id <= 790699).ToList();
            CollectionAssert.AreEquivalent(N.Creatures, rows.Select(row => row.Id).ToArray(), "the seven");
            foreach (var (id, _, name, level, package, copies) in N.Friends)
            {
                var row = rows.Single(entry => entry.Id == id);
                var copied = world.CreatureEntries.AsNoTracking().Single(entry => entry.Id == copies);
                Assert.AreEqual((1U, name, level, copied.ClassId), ((uint)row.Faction, row.NameId, row.Level, row.ClassId), $"{id}: friendly, the client's name, its copy's class");
                Assert.AreEqual(package == 0 ? (uint?)null : package, world.NpcPackageEntries.AsNoTracking().SingleOrDefault(entry => entry.Id == id)?.PackageId, $"{id}'s package");
            }
            foreach (var (id, _, copies, classId, name, level, _, _, _) in N.Foes)
            {
                var row = rows.Single(entry => entry.Id == id);
                var copied = world.CreatureEntries.AsNoTracking().Single(entry => entry.Id == copies);
                Assert.AreEqual((copied.Faction, name, level, classId == 0 ? copied.ClassId : classId), (row.Faction, row.NameId, row.Level, row.ClassId), $"{id}");
                Assert.IsTrue(CreatureManager.Instance.LoadedCreatures[id].TargetCategory == TargetCategory.Hostile, $"{id} hostile");
            }

            var pools = world.SpawnPoolEntries.AsNoTracking().Where(pool => N.PoolIds.Contains(pool.Id)).ToList();
            Assert.HasCount(4, pools, "the set's four pools");
            Assert.IsFalse(world.SpawnPoolEntries.AsNoTracking().Any(pool => pool.Id >= 790662 && pool.Id <= 790699 && !N.PoolIds.Contains(pool.Id)), "no other");
            foreach (var (id, map, creature, count, radius, at) in new[]
                {
                    (N.EnragedAttaPool, Map, N.EnragedAtta, (byte)4, 5.0, N.EnragedAttaAt), (N.QueenPool, Map, N.Queen, (byte)1, 0.0, N.QueenAt),
                    (N.XahnPool, Map, N.Xahn, (byte)1, 0.0, N.XahnAt), (N.Jawan, N.Thunderhead, N.Jawan, (byte)1, 0.0, N.JawanAt)
                })
            {
                var pool = pools.Single(entry => entry.Id == id);
                Assert.AreEqual((map, creature, count, count, radius), (pool.MapContextId, pool.Creature1Id, pool.Creature1MinCount, pool.Creature1MaxCount, pool.Radius), $"pool {id}");
                Assert.IsLessThan(0.01f, Vector3.Distance(V(at), new((float)pool.PosX, (float)pool.PosY, (float)pool.PosZ)), $"pool {id}'s place");
            }
            var criers = AshenFixFTests.WarcryCreatures(harness);
            foreach (var (id, rule) in new[] { (N.EnragedAttaPool, 60f), (N.QueenPool, 60f), (N.XahnPool, 19f) })
            {
                var pool = pools.Single(entry => entry.Id == id);
                foreach (var other in HostilePools(harness).Where(other => other.Id != id))
                {
                    var crier = new[] { other.Creature1Id, other.Creature2Id, other.Creature3Id, other.Creature4Id, other.Creature5Id, other.Creature6Id }.Any(criers.Contains);
                    var gap = Facility.Gap(new Vector3((float)pool.PosX, (float)pool.PosY, (float)pool.PosZ), other) - (float)pool.Radius;
                    Assert.IsGreaterThanOrEqualTo(crier ? Math.Max(rule, 100f) : rule, gap, $"pool {id} {(crier ? 100 : rule)} m+ outside hostile pool {other.Id}'s circle");
                }
            }
            foreach (var other in HostilePools(harness, N.Thunderhead))
                Assert.IsGreaterThanOrEqualTo(100f, Facility.Gap(V(N.JawanAt), other), $"Jawan 100 m+ outside Thunderhead's hostile pool {other.Id}");

            var pads = world.TeleporterEntries.AsNoTracking().Where(row => Pads.Contains(row.Id)).OrderBy(row => row.Id).ToList();
            Assert.HasCount(4, pads);
            foreach (var (row, (id, _, place, _)) in pads.Zip(N.Pads))
            {
                Assert.AreEqual((id, Map, N.PadClass, N.LocalTeleporter, 0.0), (row.Id, row.MapContextId, row.ClassId, (uint)row.Type, row.Rotation), $"{row.Id}");
                Assert.IsLessThan(0.01f, Vector3.Distance(V(AshenFixS2V1.Moved(id, place)), new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ)), $"{row.Id} place");
            }
            int Links(string where) => world.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE {where}").AsEnumerable().Single();
            foreach (var (id, _, _, to) in N.Pads)
                Assert.AreEqual(1, Links($"from_teleporter_id = {id} AND to_teleporter_id = {to}"), $"{id} to {to}");
            var ids = string.Join(", ", Pads);
            Assert.AreEqual(4, Links($"from_teleporter_id IN ({ids}) OR to_teleporter_id IN ({ids})"), "no other link");
            Assert.IsFalse(world.TeleporterEntries.AsNoTracking().Where(row => row.MapContextId == Map && !Pads.Contains(row.Id)).AsEnumerable().Any(row =>
                (uint)row.Type == 1 && N.Pads.Any(pad => Vector2.Distance(new((float)row.PosX, (float)row.PosZ), new((float)AshenFixS2V1.Moved(pad.Id, pad.At).X, (float)AshenFixS2V1.Moved(pad.Id, pad.At).Z)) < 5)),
                "no other pad by them (5 m: 2 m triggers; S2's 921 and 925 stand 7 m apart in Central Operations)");

            var failures = new List<string>();
            foreach (var (label, place, _, _, clearance) in Places())
                foreach (var pool in HostilePools(harness).Where(pool => !N.PoolIds.Contains(pool.Id)))
                    if (Facility.Gap(place, pool) < clearance)
                        failures.Add(FormattableString.Invariant($"{label}: {Facility.Gap(place, pool):0.0} m outside hostile pool {pool.Id}'s circle (its rule {clearance})"));
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// In the squad's copy (SquadInstanceState: a cleared pool back 45 min after its last death, SquadInstanceConfig) the
        /// pools the set's missions name as public-spawn actors (1551: the Enraged Atta's 790663 and the queen's 790665; 1501:
        /// Xahn's 790667) come back on their own respawn time (SpawnPoolManager.ReturnsOnOwnTime): each cleared, not back just
        /// before its own time (90 s, 300 s, 900 s), back just after it. Without the names 1551's ten kills from a pool of four
        /// would wait about 90 min, and an early kill of the queen or Xahn (on the way, by a mate, before an abandon) 45 min.
        /// </summary>
        [TestMethod]
        public void TheNamedPoolsComeBackOnTheirOwnTimeInASquadsCopy()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoFacility(harness);
            Assert.IsNotNull(copy.SquadState, "the squad's copy");
            Assert.IsGreaterThan(900_000L, copy.SquadState.RespawnMs, "the copy's own respawn is longer than every pool's");
            foreach (var (pool, kind, respawnMs) in new[] { (N.EnragedAttaPool, N.EnragedAtta, 90_000L), (N.QueenPool, N.Queen, 300_000L), (N.XahnPool, N.Xahn, 900_000L) })
            {
                List<Creature> Standing() => Here(copy).Where(one => one.SpawnPool?.DbId == pool && one.DbId == kind && one.State != CharacterState.Dead).ToList();
                Spawn(copy, pool);
                var standing = Standing();
                Assert.IsNotEmpty(standing, $"pool {pool} up");
                foreach (var creature in standing)
                {
                    creature.Attributes[Attributes.Health].Current = 0;
                    harness.Creatures.HandleCreatureKill(copy, creature, harness.Client.Player);
                }
                harness.Drain();
                var entry = copy.SpawnPools.Single(candidate => candidate.DbId == pool && candidate.SceneRunId == null);
                Assert.AreNotEqual(0L, entry.ClearedAtUtcMs, $"pool {pool} cleared in the copy");
                Assert.AreEqual((pool, respawnMs), (pool, entry.RespawnTime), $"pool {pool}'s own respawn");
                SpawnPoolManager.Instance.SpawnPoolWorker(copy, respawnMs - 10_000);
                Assert.IsEmpty(Standing(), $"pool {pool}: not back before its own respawn");
                SpawnPoolManager.Instance.SpawnPoolWorker(copy, 20_000);
                SpawnPoolManager.Instance.SpawnPoolWorker(copy, 0);
                Assert.IsNotEmpty(Standing(), $"pool {pool}: back after its own respawn, long before the copy's");
            }
        }

        /// <summary>
        /// In the squad's copy: the Enraged Atta, the queen and Xahn come out hostile on the navmesh's ground; there is no public
        /// Humphries, Lukaz or prisoner. On Thunderhead Jawan comes out friendly, talks, on the navmesh's ground.
        /// </summary>
        [TestMethod]
        public void TheSetsCreaturesStandInTheirMaps()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoFacility(harness);
            foreach (var (pool, kind) in new[] { (N.EnragedAttaPool, N.EnragedAtta), (N.QueenPool, N.Queen), (N.XahnPool, N.Xahn) })
            {
                Spawn(copy, pool);
                var one = Here(copy).FirstOrDefault(creature => creature.SpawnPool?.DbId == pool && creature.State != CharacterState.Dead);
                Assert.IsNotNull(one, $"pool {pool}");
                Assert.AreEqual((kind, TargetCategory.Hostile), (one.DbId, one.TargetCategory), $"pool {pool}");
                AssertOnGround(copy, one.Position, $"pool {pool}'s");
            }
            Assert.IsFalse(Here(copy).Any(one => one.DbId is N.Humphries or N.Lukaz or N.Prisoner), "no public Humphries, Lukaz or prisoner");
            var thunderhead = EnterThunderhead(harness, V(N.JawanAt) + new Vector3(0, 0, 2));
            var jawan = Npc(thunderhead, N.Jawan);
            Assert.AreEqual((N.Jawan, TargetCategory.Friendly), (jawan.DbId, jawan.TargetCategory), "Jawan");
            Assert.IsNotNull(jawan.Npc, "Jawan talks");
            AssertOnGround(thunderhead, jawan.Position, "Jawan");
        }

        /// <summary>
        /// The two pad pairs: a player who steps onto the HQ hall's pad is set down over the far one (the east wing's, the
        /// fortress') and not sent straight back; off it and on again, he is taken back. Each arrival off the other pad's trigger.
        /// </summary>
        [TestMethod]
        public void TheBaneTeleportersTakeThePlayerThereAndBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoFacility(harness);
            LoadPads(harness, copy);
            foreach (var (hall, far, off) in new[] { (N.HallWingPad, N.WingPad, new Vector3(288, 96.02f, -140)), (N.HallFortressPad, N.FortressPad, new Vector3(15, 71.62f, 25)) })
            {
                var there = copy.Teleporters[hall];
                var back = copy.Teleporters[far];
                Teleport(harness, copy, there);
                Assert.IsLessThan(3f, Vector3.Distance(back.Position, harness.Client.Player.Position), $"at {far}");
                harness.Objects.DynamicObjectProximityWorker(copy, back, 0);
                Assert.IsNull(harness.Client.PendingTransfer, "not sent straight back");
                harness.MoveTo(off);
                harness.Drain();
                harness.Objects.DynamicObjectProximityWorker(copy, back, 0);
                Assert.IsNull(harness.Client.PendingTransfer, "off the pad");
                Teleport(harness, copy, back);
                Assert.IsLessThan(3f, Vector3.Distance(there.Position, harness.Client.Player.Position), $"back at {hall}");
                Assert.IsGreaterThan(2f, Vector3.Distance(back.Position + Vector3.UnitY, there.Position), "the arrivals off the other trigger");
                harness.MoveTo(ToVector(AshenFixS2V1.RampFoot));
                harness.Drain();
            }
        }

        /// <summary>
        /// The set's places on the client's floor: each on the navmesh's ground (within 1 m) and a client floor (within 1 m), with no
        /// client surface over its head within 2.2 m; the NPCs and the objects one talks to or uses seen from all 16 places 2 m
        /// round them; each outside every hostile pool's circle by its clearance (Places). The facility's markers on the navmesh
        /// and a client floor within 2 m. On Thunderhead the same for Jawan and his marker.
        /// </summary>
        [TestMethod]
        public void ThePlacesStandOnTheClientsFloor()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoFacility(harness);
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var failures = new List<string>();
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", Facility.MapName(harness, Map)), meshes, 1);
            var places = Places();
            CheckPlaces(copy.NavMesh, geometry, places, HostilePools(harness).Where(pool => !N.PoolIds.Contains(pool.Id)).ToList(), failures);
            CheckMarkers(harness, copy.NavMesh, geometry, Map, 12, failures);

            var thunderhead = EnterThunderhead(harness, V(N.JawanAt) + new Vector3(0, 0, 2));
            var thunderGeometry = MapGeometry.Load(Path.Combine(dataDir, "maps", Facility.MapName(harness, N.Thunderhead)), meshes, 1);
            CheckPlaces(thunderhead.NavMesh, thunderGeometry, new() { ("Jawan", V(N.JawanAt), true, F.NpcMiddle, 100) },
                HostilePools(harness, N.Thunderhead), failures);
            CheckMarkers(harness, thunderhead.NavMesh, thunderGeometry, N.Thunderhead, 1, failures);
            Assert.HasCount(17, places);
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>
        /// The walks on the client's floor (AshenFixBTests.ClientFloorLine with a player's half width, 0.5 m), with the collision of
        /// the set's objects (the pads, the egg, the talk objects, set 19's hall objects), every leg there and back by the corners
        /// Legs names (a client-floor flood's way, 2026-11-12), also with its start moved 0.13, 0.27 and 0.4 m along its first
        /// stretch (StartShifts). Every walk keeps 30 m (2D) outside the circle of every hostile pool but those it names
        /// (docs/ashen-playtest.md). Thunderhead's walk from the base's waypoint (85, its arrival) to Jawan the same on its map.
        /// </summary>
        [TestMethod]
        public void ThePlacesAreWalkedToOnTheClientsFloor()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", Facility.MapName(harness, Map)), meshes, 1);
            foreach (var (classId, at, yaw) in Objects())
                AddObject(geometry, meshes, classId, at, yaw);
            var thunderGeometry = MapGeometry.Load(Path.Combine(dataDir, "maps", Facility.MapName(harness, N.Thunderhead)), meshes, 1);
            var hostile = HostilePools(harness);
            var failures = new List<string>();
            var walked = 0;
            foreach (var (leg, corners) in Legs)
                foreach (var (given, direction) in new[] { (corners.ToList(), "there"), (Enumerable.Reverse(corners).ToList(), "back") })
                {
                    var first = Vector2.Distance(new(given[0].X, given[0].Z), new(given[1].X, given[1].Z));
                    foreach (var shift in StartShifts.Where(shift => shift == 0 || first > shift + 0.05f))
                    {
                        walked++;
                        var line = given.ToList();
                        line[0] = Vector3.Lerp(given[0], given[1], shift / first);
                        if (AshenFixBTests.ClientFloorLine(leg == ThunderheadLeg ? thunderGeometry : geometry, line, line[^1], leg == ThunderheadLeg ? null : AshenFixS2Tests.OverTheRamp, PlayerHalfWidth, rampFoot: true) is { } why)
                            failures.Add(FormattableString.Invariant($"{leg} {direction} (start +{shift} m): {why}"));
                    }
                }
            foreach (var (label, legs, passes, target) in Walks())
            {
                var line = Line(legs);
                var pools = legs.Contains(ThunderheadLeg) ? HostilePools(harness, N.Thunderhead) : hostile;
                if (WO.Clearance(line, pools.Where(pool => !passes.Contains(pool.Id)).ToList()) is { } close)
                    failures.Add(FormattableString.Invariant($"{label}: {close.Gap:0.0} m outside hostile pool {close.Pool}'s circle at ({close.At.X:0.0}, {close.At.Y:0.0})"));
                // A pool a walk names it passes (a rule break, docs/ashen-playtest.md) is still kept PassedMinimum off, but for
                // the walk's own target (set 20's pool within 15 m of its end) and a pool WalkThrough names for it (531065).
                var through = N.PoolIds.Where(id => pools.Any(pool => pool.Id == id &&
                        Vector2.Distance(new((float)pool.PosX, (float)pool.PosZ), new(target.X, target.Z)) <= 15))
                    .Concat(WalkThrough.GetValueOrDefault(label, Array.Empty<uint>())).ToHashSet();
                if (WO.Clearance(line, pools.Where(pool => passes.Contains(pool.Id) && !through.Contains(pool.Id)).ToList()) is { } near &&
                    near.Gap < PassedMinimum)
                    failures.Add(FormattableString.Invariant($"{label}: {near.Gap:0.0} m outside passed pool {near.Pool}'s circle at ({near.At.X:0.0}, {near.At.Y:0.0}), under {PassedMinimum} m"));
                if (Vector3.Distance(line[^1], target) > 0.01f)
                    failures.Add($"{label} ends at {line[^1]}, not {target}");
            }
            Assert.IsGreaterThanOrEqualTo(2 * Legs.Count * 3, walked, "most legs at three shifts or more");
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>
        /// The set's places and walks on the facility keep out of every hostile pool's reach (fix K, AshenFixKTests: a creature
        /// notices a player within its AggroRange, 18 m 3D, no sight needed; its spawn is NavMeshManager.RandomPointAround the
        /// pool's place with its radius, which takes whole navmesh polygons touching the circle, so a spawn can stand well outside
        /// the circle). Each hostile pool's ground is fix M's AshenSpawnReach.Ground (its point and its spawn draws, seeded per
        /// pool; a pool without a circle its point and a 10 m stroll, or its patrol); the least 3D distance of each place (its
        /// floor) and of each walk's points (every half metre) to that ground: OK at AshenSpawnReach.Clearance (20 m) or more,
        /// RISK from AggroRange (18 m), FAIL inside it (AshenSpawnReach.Verdict). Exempt: the pools a place or a
        /// walk is about (its mission's own target: the queen's at the DNA sample, Xahn's in Information Extraction, the pools a
        /// walk ends at) and the rule breaks ReachBreaks names (docs/ashen-playtest.md). The report lists every FAIL and RISK.
        /// </summary>
        [TestMethod]
        public void ThePlacesAndWalksKeepOutOfThePoolsReach()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoFacility(harness);
            Assert.AreEqual(AshenSpawnReach.AggroRange, new Creature().AggroRange, "Creature.AggroRange");
            var hostile = HostilePools(harness).Select(entry => entry.Id).ToHashSet();
            var ground = AshenSpawnReach.Ground(copy, copy.SpawnPools.Where(pool => hostile.Contains(pool.DbId) && pool.SceneRunId == null).Select(pool => pool.DbId));
            Assert.IsTrue(ground.ContainsKey(N.XahnPool) && ground.ContainsKey(531065), "the facility's pools in the copy");
            var targets = new List<(string Label, List<Vector3> Points, uint[] Own)>();
            foreach (var (label, at, _, _, _) in Places().Where(place => !place.Label.EndsWith("pool", StringComparison.Ordinal)))
                targets.Add((label, new List<Vector3> { at }, ReachOwn.GetValueOrDefault(label, Array.Empty<uint>())));
            foreach (var (label, legs, passes, _) in Walks().Where(walk => !walk.Legs.Contains(ThunderheadLeg)))
                targets.Add((label, AshenSpawnReach.Samples(Line(legs)), passes.Where(N.PoolIds.Contains).ToArray()));
            var fails = new List<string>();
            var report = new List<string>();
            foreach (var (label, points, own) in targets)
                foreach (var (pool, near, at) in AshenSpawnReach.Reach(points, ground).Where(row => !own.Contains(row.Pool)))
                {
                    var verdict = AshenSpawnReach.Verdict(near);
                    if (verdict == "OK")
                        continue;
                    var line = FormattableString.Invariant($"{label} / {pool}: {near:0.0} m at ({at.X:0.0}, {at.Y:0.0}, {at.Z:0.0})");
                    report.Add($"{verdict} {line}");
                    if (verdict == "FAIL" && !(ReachBreaks.TryGetValue(label, out var allowed) && allowed.Contains(pool)))
                        fails.Add(line);
                }
            Console.WriteLine(string.Join("\n", report));
            Assert.IsEmpty(fails, $"{fails.Count}: " + string.Join("; ", fails));
            var stale = ReachBreaks.SelectMany(entry => entry.Value.Select(pool => (Label: entry.Key, Pool: pool)))
                .Where(pair => !report.Any(line => line.StartsWith($"FAIL {pair.Label} / {pair.Pool}:", StringComparison.Ordinal)))
                .Select(pair => $"{pair.Label} / {pair.Pool}").ToList();
            Assert.IsEmpty(stale, "rule breaks that no longer fail (drop them from ReachBreaks and the doc): " + string.Join("; ", stale));
        }


        /// <summary>The pools a place is about (its mission's own target): exempt from ThePlacesAndWalksKeepOutOfThePoolsReach.</summary>
        private static readonly Dictionary<string, uint[]> ReachOwn = new()
        {
            ["the DNA sample"] = new[] { N.QueenPool },
            ["the queen's talk"] = new[] { N.XahnPool }, ["the Life Support System"] = new[] { N.XahnPool }, ["the Life Support System's console"] = new[] { N.XahnPool }, ["the egg"] = new[] { N.XahnPool },
            ["Information Extraction's marker"] = new[] { N.XahnPool }
        };

        /// <summary>
        /// The rule breaks of ThePlacesAndWalksKeepOutOfThePoolsReach (under AggroRange of a pool's spawn draws; docs/ashen-playtest.md
        /// set 20, rule breaks; user review).
        /// </summary>
        private static readonly Dictionary<string, uint[]> ReachBreaks = new()
        {
            // Through the ramp's own Atta pool (r30), the only way down into the pit: Humphries' side needs it cleared.
            ["the ledge down to Humphries (1499)"] = new[] { 531065u },
            // Into Information Extraction between its Enslaver cells (15-17 m off their draws at z 126-128, round the raised step
            // at x 6.5, z 129-136): TaRapedia puts Xahn, the queen and the egg among the four cells.
            ["the pad to the queen's life support and the egg (1501)"] = new[] { 531073u, 531076u },
            ["the pad to the Life Support System's console (1501, fix V2)"] = new[] { 531073u, 531076u }
        };

        /// <summary>
        /// The pads' body (class 8044: its rim faces 42 degrees only on local -z and +z within local x -0.56..0.49, set 19's probe):
        /// each of 925-928 stands at rotation 0, and every walk to or from one leaves its centre toward a neighbour corner whose
        /// line crosses the rim (2.2 m out) within local x -0.45..0.45; a walk that ends inside a pad's 2 m trigger short of its
        /// centre (the hall pads') comes up the rim there: its last stretch within local x -0.45..0.45.
        /// </summary>
        [TestMethod]
        public void ThePadsAreWalkedOverTheirShallowRim()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var rows = harness.World.TeleporterEntries.AsNoTracking().Where(row => Pads.Contains(row.Id)).ToList();
            Assert.HasCount(4, rows);
            foreach (var row in rows)
                Assert.AreEqual((row.Id, 0f), (row.Id, (float)row.Rotation), $"{row.Id}: rotation 0");
            var failures = new List<string>();
            var reached = new HashSet<uint>();
            foreach (var (label, legs, _, _) in Walks())
            {
                var line = Line(legs);
                foreach (var (end, next) in new[] { (line[0], line[1]), (line[^1], line[^2]) })
                    foreach (var (id, _, at, _) in N.Pads.Select(pad => (pad.Id, pad.Description, AshenFixS2V1.Moved(pad.Id, pad.At), pad.To)).Where(pad => Vector3.Distance(V(pad.Item3), end) < 2f))
                    {
                        reached.Add(id);
                        if (Vector2.Distance(new((float)at.X, (float)at.Z), new(end.X, end.Z)) < 0.01f)
                        {
                            var way = Vector2.Normalize(new Vector2(next.X - end.X, next.Z - end.Z));
                            if (MathF.Abs(way.X * 2.2f) > 0.45f)
                                failures.Add(FormattableString.Invariant($"{label}: pad {id}'s rim crossed at local x {way.X * 2.2f:0.00}"));
                        }
                        else
                            foreach (var corner in new[] { end, next })
                                if (MathF.Abs(corner.X - (float)at.X) > 0.45f)
                                    failures.Add(FormattableString.Invariant($"{label}: pad {id}'s rim come up at local x {corner.X - (float)at.X:0.00}"));
                    }
            }
            CollectionAssert.AreEquivalent(Pads, reached.ToArray(), "every pad walked to or from");
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// What Makes Them Tick: no radio offer before 1498; then on arrival (with 1499's). Taken: 1, 2 open, 3 hidden, no sample.
        /// Nine Enraged Atta leave 1 open, the tenth completes it; the queen's kill completes the hidden 3 and puts the player's
        /// own sample by her body (also after a reload); it hears 2 and its talk completes it. Not radioed in: the turn-in at
        /// Yamamoto on Ashen Desert waits with a full bag and pays the piece chosen once.
        /// </summary>
        [TestMethod]
        public void WhatMakesThemTickKillsTheAttaAndSamplesTheQueen()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, AshenTheBigSecretV1.MissionId, Lockdown.MissionId, Raid.MissionId);
            var copy = IntoFacility(harness);
            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.AreEqual(0, harness.Drain().OfType<DispenseRadioMissionPacket>().Count(packet => packet.MissionId == Tick551.MissionId), "not before 1498");
            AshenRuntime.SeedCompleted(harness, Blast.MissionId);
            harness.Manager.OfferArrivalMissions(harness.Client);
            var offered = harness.Drain().OfType<DispenseRadioMissionPacket>().Select(packet => packet.MissionId).ToArray();
            Assert.AreEqual((1, 1, 0), (offered.Count(id => id == Tick551.MissionId), offered.Count(id => id == Den.MissionId), offered.Count(id => id == Queen.MissionId)),
                "1551 and 1499 on arrival, not 1501");
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, Tick551.MissionId), "taken by radio");
            harness.Drain();
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete),
                (Objective(harness, Tick551.MissionId, Tick551.KillTheAtta), Objective(harness, Tick551.MissionId, Tick551.KillTheQueen)), "1, 2 open");
            var visible = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Tick551.MissionId].ObjectivesList.Select(objective => objective.ObjectiveId).ToArray();
            Assert.IsFalse(visible.Contains(Tick551.QueenDown), "3 is hidden");
            Assert.IsNull(FindSceneObject(harness, copy, Tick551.MissionId, Tick551.SampleRole), "no sample yet");

            for (var kill = 1; kill <= Tick551.AttaKills; kill++)
            {
                KillOne(harness, copy, N.EnragedAttaPool, N.EnragedAtta);
                Tick(harness, copy, 250);
                Assert.AreEqual(kill < Tick551.AttaKills ? MissionObjectiveState.Incomplete : MissionObjectiveState.Completed,
                    Objective(harness, Tick551.MissionId, Tick551.KillTheAtta), $"{kill} Enraged Atta");
            }
            KillOne(harness, copy, N.QueenPool, N.Queen);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Tick551.MissionId, Tick551.QueenDown), Objective(harness, Tick551.MissionId, Tick551.KillTheQueen)), "the queen dead: 3 done, 2 open");
            ReloadAndRecover(harness, copy);
            var sample = SceneObject(harness, copy, Tick551.MissionId, Tick551.SampleRole);
            Assert.AreEqual((Tick551.SampleClass, ToVector(Tick551.SampleAt), harness.Client.Player.Id, Tick551.SamplePackage),
                ((uint)sample.EntityClassId, sample.Position, sample.SceneOwnerCharacterId, sample.MissionConversation.NpcPackageId), "the player's own sample, also after a reload");
            CollectionAssert.AreEqual(new[] { (Tick551.MissionId, Tick551.KillTheQueen) }, ObjectTopics(harness, harness.Client, sample), "the sample hears 2");
            TalkToObject(harness, harness.Client, sample, Tick551.MissionId, Tick551.KillTheQueen);
            Tick(harness, copy, 250);
            Assert.IsTrue(harness.Client.Player.Missions[Tick551.MissionId].Completeable, "to be turned in");
            Assert.IsFalse(harness.Manager.TryCompleteRadioMission(harness.Client, Tick551.MissionId, 0, null), "not by radio");
            harness.Drain();

            var desert = AshenLeadInsTests.EnterAshen(harness, V(AshenLeadInNpcsV1.YamamotoAt) + new Vector3(0, 0, 2));
            var yamamoto = Npc(desert, AshenLeadInNpcsV1.Yamamoto);
            FullBagWaits(harness, yamamoto, Tick551.MissionId, 1, ArmorFiller);
            FailedTurnInPaysNothing(harness, yamamoto, Tick551.MissionId, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, yamamoto, Tick551.MissionId, 1, Tick551.Experience, Tick551.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Tick551.MissionId), "MissionRewarded once");
            Assert.AreEqual(1U, Facility.Held(harness, Tick551.StealthLegs), "the legs chosen");
        }

        /// <summary>
        /// What Makes Them Tick in a squad of two on it: the queen killed with the mate beside her completes each one's hidden 3, and
        /// each gets his own sample; the mate's talk completes only his own 2.
        /// </summary>
        [TestMethod]
        public void WhatMakesThemTicksQueenGivesEachMateHisOwnSample()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Before1551);
            var copy = IntoFacility(harness);
            TakeByRadio(harness, harness.Client, Tick551.MissionId);
            Tick(harness, copy, 250);
            using var mate = new Facility.SquadMate(harness, copy, Giver(copy, AshenConscriptionEntryNpcsV1.Peacock), 0, Before1551);
            TakeByRadio(harness, mate.Client, Tick551.MissionId);
            Tick(harness, copy, 250);
            KillOne(harness, copy, N.QueenPool, N.Queen, mate);
            Tick(harness, copy, 250);
            MissionObjectiveState MateOf(uint objective) => mate.Client.Player.Missions[Tick551.MissionId].Objectives[objective].State;
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Completed),
                (Objective(harness, Tick551.MissionId, Tick551.QueenDown), MateOf(Tick551.QueenDown)), "each one's 3 done");
            var mine = SceneObject(harness, copy, Tick551.MissionId, Tick551.SampleRole);
            var his = MateObject(harness, copy, mate.Client, Tick551.MissionId, Tick551.SampleRole);
            Assert.IsNotNull(his, "the mate's own sample");
            Assert.AreNotSame(mine, his);
            Assert.IsEmpty(ObjectTopics(harness, mate.Client, mine), "the player's sample talks nothing to the mate");
            TalkToObject(harness, mate.Client, his, Tick551.MissionId, Tick551.KillTheQueen);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (MateOf(Tick551.KillTheQueen), Objective(harness, Tick551.MissionId, Tick551.KillTheQueen)), "the mate's 2 done, the player's still open");
        }

        /// <summary>
        /// 1551 and 1501 open at once (both talk package 1937): with 1501 at 3 and the queen of 1551 dead, the sample hears only
        /// 1551's 2 and the queen's talk object only 1501's 3; the egg's nothing yet. The sample's talk completes 1551's 2 and leaves
        /// 1501's 3 open; the queen's completes 1501's 3 and changes nothing of 1551.
        /// </summary>
        [TestMethod]
        public void TheSampleAndTheQueenTalkApartWithBothMissionsOpen()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Before1551.Append(Den.MissionId).ToArray());
            var copy = IntoFacility(harness);
            TakeByRadio(harness, harness.Client, Tick551.MissionId, Queen.MissionId);
            Tick(harness, copy, 250);
            KillOne(harness, copy, N.QueenPool, N.Queen);
            Tick(harness, copy, 250);
            ToTheQueen(harness, copy);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete),
                (Objective(harness, Tick551.MissionId, Tick551.KillTheQueen), Objective(harness, Queen.MissionId, Queen.TalkToTheQueen)), "1551's 2 and 1501's 3 open");
            var sample = SceneObject(harness, copy, Tick551.MissionId, Tick551.SampleRole);
            var queen = SceneObject(harness, copy, Queen.MissionId, Queen.QueenRole);
            var egg = SceneObject(harness, copy, Queen.MissionId, Queen.EggTalkRole);
            CollectionAssert.AreEqual(new[] { (Tick551.MissionId, Tick551.KillTheQueen) }, ObjectTopics(harness, harness.Client, sample), "the sample: 1551's only");
            CollectionAssert.AreEqual(new[] { (Queen.MissionId, Queen.TalkToTheQueen) }, ObjectTopics(harness, harness.Client, queen), "the queen: 1501's only");
            Assert.IsEmpty(ObjectTopics(harness, harness.Client, egg), "the egg: nothing before 5");
            TalkToObject(harness, harness.Client, sample, Tick551.MissionId, Tick551.KillTheQueen);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Tick551.MissionId, Tick551.KillTheQueen), Objective(harness, Queen.MissionId, Queen.TalkToTheQueen)), "the sample's talk: 1551 only");
            TalkToObject(harness, harness.Client, queen, Queen.MissionId, Queen.TalkToTheQueen);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Queen.MissionId, Queen.TalkToTheQueen), Objective(harness, Queen.MissionId, Queen.EndTheSuffering),
                    Objective(harness, Tick551.MissionId, Tick551.KillTheQueen), Objective(harness, Tick551.MissionId, Tick551.KillTheAtta)),
                "the queen's talk: 1501's 3 done, 4 open; 1551 as it was (2 done, 1 open)");
        }

        /// <summary>
        /// Into the Lion's Den, Humphries' side: taken by radio after 1498. 1 open, 3-6 waiting, 2, 7, 8 hidden; the player's own
        /// Humphries, Lukaz, six prisoners and the Cell Door Control (dark, not usable) stand from the arrival; no fighters. At the
        /// arena 1 completes; 3, 4 open; the pit's seven and the ledge's six come, hostile. Six of the pit's dead leave 3 open
        /// (also after a reload); the seventh completes 3 and 2, fails 4 and the hidden 7 stays open; Lukaz, the prisoners, the
        /// ledge's fighters and the switch go. Humphries hears 5; his talk completes 5 and 8; radioed in it pays once and the radio
        /// offers Save the Queen.
        /// </summary>
        [TestMethod]
        public void IntoTheLionsDenSavesHumphriesInThePit()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Before1551);
            var copy = IntoFacility(harness);
            TakeByRadio(harness, harness.Client, Den.MissionId);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Den.MissionId, Den.ToTheArena), "1 open");
            foreach (var objective in new[] { Den.HelpHumphries, Den.HelpThePrisoners, Den.SpeakHumphries, Den.SpeakLukaz, Den.LedgeCleared })
                Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Den.MissionId, objective), $"{objective} waits");
            var visible = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Den.MissionId].ObjectivesList.Select(objective => objective.ObjectiveId).ToArray();
            foreach (var hidden in new[] { Den.Master, Den.LedgeCleared, Den.Spoken })
                Assert.IsFalse(visible.Contains(hidden), $"{hidden} is hidden");
            var humphries = SceneCreature(harness, copy, Den.MissionId, Den.HumphriesRole);
            Assert.AreEqual((N.Humphries, N.HumphriesPackage, TargetCategory.Friendly), (humphries.DbId, humphries.Npc?.NpcPackageId, humphries.TargetCategory), "Humphries");
            Assert.IsTrue(Rasa.Game.Missions.World.CreatureGameplayRules.IsInvulnerable(humphries), "unhurt by anything");
            Assert.IsNotNull(SceneCreature(harness, copy, Den.MissionId, Den.LukazRole), "Lukaz");
            Assert.AreEqual(Den.PrisonersAt.Length, Enumerable.Range(1, Den.PrisonersAt.Length).Count(index => SceneCreature(harness, copy, Den.MissionId, Den.PrisonerRole(index)) != null), "six prisoners");
            var control = SceneObject(harness, copy, Den.MissionId, Den.SwitchRole);
            Assert.AreEqual((Den.SwitchClass, (UseObjectState)Den.SwitchOff, false, Den.SwitchName), ((uint)control.EntityClassId, control.StateId, control.IsEnabled, control.NameOverrideId),
                "the Cell Door Control, dark, not usable");
            Assert.IsTrue(Den.PitFighters.Concat(Den.LedgeFighters).All(fighter => SceneCreature(harness, copy, Den.MissionId, fighter.Role) == null), "no fighters yet");

            ToTheArena(harness, copy);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete),
                (Objective(harness, Den.MissionId, Den.ToTheArena), Objective(harness, Den.MissionId, Den.HelpHumphries), Objective(harness, Den.MissionId, Den.HelpThePrisoners),
                    Objective(harness, Den.MissionId, Den.LedgeCleared)), "at the arena: 1 done, 3, 4 and 7 open");
            foreach (var (role, creature, _) in Den.PitFighters.Concat(Den.LedgeFighters))
            {
                var fighter = SceneCreature(harness, copy, Den.MissionId, role);
                Assert.IsNotNull(fighter, role);
                Assert.AreEqual((creature, TargetCategory.Hostile), (fighter.DbId, fighter.TargetCategory), role);
            }

            // One of the ledge's six killed first: its loot stays when the pit decides the side (killed, not removed).
            var ledgeOne = SceneCreature(harness, copy, Den.MissionId, Den.LedgeFighters.First(fighter => fighter.Item2 == 531015).Item1);
            Kill(harness, copy, ledgeOne);
            Assert.IsTrue(copy.LootDispensers.ContainsKey(ledgeOne.CorpseLootEntityId), "the ledge's first, killed: its loot");
            for (var index = 0; index < Den.PitFighters.Length; index++)
            {
                Kill(harness, copy, SceneCreature(harness, copy, Den.MissionId, Den.PitFighters[index].Role));
                Tick(harness, copy, 250);
                if (index == Den.PitFighters.Length - 2)
                {
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Den.MissionId, Den.HelpHumphries), "six of seven: 3 open");
                    ReloadAndRecover(harness, copy);
                    Assert.IsNotNull(SceneCreature(harness, copy, Den.MissionId, Den.PitFighters[^1].Role), "the seventh still there after a reload");
                }
            }
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Completed, MissionObjectiveState.Failed, MissionObjectiveState.Incomplete,
                    MissionObjectiveState.Inactive),
                (Objective(harness, Den.MissionId, Den.HelpHumphries), Objective(harness, Den.MissionId, Den.Master), Objective(harness, Den.MissionId, Den.HelpThePrisoners),
                    Objective(harness, Den.MissionId, Den.SpeakHumphries), Objective(harness, Den.MissionId, Den.SpeakLukaz)),
                "the pit's seven dead: 3 and 2 done, 4 failed, 5 open, 6 waits");
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[Den.MissionId].State, "the mission goes on");
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            var ledgeBodies = Bodies(harness, copy, harness.Client, Den.LedgeFighters.Select(fighter => fighter.Role));
            Assert.IsTrue(ledgeBodies.All(body => body.State == CharacterState.Dead), "the ledge's fighters fall (none stands)");
            Assert.IsTrue(copy.LootDispensers.ContainsKey(ledgeOne.CorpseLootEntityId), "the ledge's first: its loot still there (not taken by a remove)");
            Assert.IsTrue(ledgeBodies.Where(body => body != ledgeOne).All(body => body.HarvestOwnerEntityId == 0 && !copy.LootDispensers.ContainsKey(body.CorpseLootEntityId)),
                "the fallen: nobody's kill, no loot");
            Assert.IsEmpty(Bodies(harness, copy, harness.Client, new[] { Den.LukazRole, Den.PrisonerRole(1) }), "Lukaz and the prisoners go");
            Assert.IsNull(FindSceneObject(harness, copy, Den.MissionId, Den.SwitchRole), "the switch goes");

            humphries = SceneCreature(harness, copy, Den.MissionId, Den.HumphriesRole);
            CollectionAssert.AreEqual(new[] { Den.SpeakHumphries }, Topics(harness, humphries, Den.MissionId), "Humphries hears 5");
            Talk(harness, humphries, Den.MissionId, Den.SpeakHumphries);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Completed, true),
                (Objective(harness, Den.MissionId, Den.SpeakHumphries), Objective(harness, Den.MissionId, Den.Spoken), harness.Client.Player.Missions[Den.MissionId].Completeable),
                "5 and 8 done: to be radioed in");
            var before = harness.Context.ReadRewardTotals();
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, Den.MissionId, 0, null), "radioed in");
            var rewarded = harness.Drain();
            Assert.AreEqual(1, rewarded.OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Den.MissionId), "paid once");
            var after = harness.Context.ReadRewardTotals();
            Assert.AreEqual((before.Experience + Den.Experience, before.Credits + (int)Den.Credits), (after.Experience, after.Credits));
            Assert.AreEqual(1U, Facility.Held(harness, Den.SonicDisperser), "the disperser chosen");
            Assert.AreEqual(1, rewarded.OfType<DispenseRadioMissionPacket>().Count(packet => packet.MissionId == Queen.MissionId), "1501 by radio");
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            Assert.IsNull(SceneCreature(harness, copy, Den.MissionId, Den.HumphriesRole), "Humphries goes with the mission");
        }

        /// <summary>
        /// Into the Lion's Den, the prisoners' side: at the arena, five of the ledge's six dead leave the hidden 7 open and the Cell
        /// Door Control not usable; the sixth completes 7 and makes it usable. Its use completes 4 and 2, fails 3; the switch on and
        /// used no more; Humphries and the pit's fighters go. Lukaz hears 6; his talk completes 6 and 8: to be radioed in.
        /// </summary>
        [TestMethod]
        public void IntoTheLionsDenFreesTheBrannPrisoners()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Before1551);
            var copy = IntoFacility(harness);
            TakeByRadio(harness, harness.Client, Den.MissionId);
            Tick(harness, copy, 250);
            ToTheArena(harness, copy);
            for (var index = 0; index < Den.LedgeFighters.Length; index++)
            {
                Kill(harness, copy, SceneCreature(harness, copy, Den.MissionId, Den.LedgeFighters[index].Role));
                Tick(harness, copy, 250);
                if (index == Den.LedgeFighters.Length - 2)
                {
                    Assert.AreEqual((MissionObjectiveState.Incomplete, false),
                        (Objective(harness, Den.MissionId, Den.LedgeCleared), SceneObject(harness, copy, Den.MissionId, Den.SwitchRole).IsEnabled), "five of six: 7 open, the switch not usable");
                    Use(harness, copy, SceneObject(harness, copy, Den.MissionId, Den.SwitchRole), Den.UseArgument);
                    Tick(harness, copy, 250);
                    Assert.AreEqual((MissionObjectiveState.Incomplete, (UseObjectState)Den.SwitchOff),
                        (Objective(harness, Den.MissionId, Den.HelpThePrisoners), SceneObject(harness, copy, Den.MissionId, Den.SwitchRole).StateId), "a use before it is usable: refused, 4 open, the switch dark");
                }
            }
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete, true),
                (Objective(harness, Den.MissionId, Den.LedgeCleared), Objective(harness, Den.MissionId, Den.HelpThePrisoners), SceneObject(harness, copy, Den.MissionId, Den.SwitchRole).IsEnabled),
                "the ledge's six dead: 7 done, the switch usable, 4 open");
            Use(harness, copy, SceneObject(harness, copy, Den.MissionId, Den.SwitchRole), Den.UseArgument);
            Tick(harness, copy, 250);
            var control = SceneObject(harness, copy, Den.MissionId, Den.SwitchRole);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Completed, MissionObjectiveState.Failed, MissionObjectiveState.Incomplete,
                    (UseObjectState)Den.SwitchOn, false),
                (Objective(harness, Den.MissionId, Den.HelpThePrisoners), Objective(harness, Den.MissionId, Den.Master), Objective(harness, Den.MissionId, Den.HelpHumphries),
                    Objective(harness, Den.MissionId, Den.SpeakLukaz), control.StateId, control.IsEnabled), "the cells open: 4 and 2 done, 3 failed, 6 open; the switch on");
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            var pitBodies = Bodies(harness, copy, harness.Client, Den.PitFighters.Select(fighter => fighter.Role));
            Assert.IsTrue(pitBodies.All(body => body.State == CharacterState.Dead && body.HarvestOwnerEntityId == 0 && !copy.LootDispensers.ContainsKey(body.CorpseLootEntityId)),
                "the pit's fighters fall: none stands, nobody's kill, no loot");
            Assert.IsEmpty(Bodies(harness, copy, harness.Client, new[] { Den.HumphriesRole }), "Humphries goes");
            var lukaz = SceneCreature(harness, copy, Den.MissionId, Den.LukazRole);
            CollectionAssert.AreEqual(new[] { Den.SpeakLukaz }, Topics(harness, lukaz, Den.MissionId), "Lukaz hears 6");
            Talk(harness, lukaz, Den.MissionId, Den.SpeakLukaz);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, true),
                (Objective(harness, Den.MissionId, Den.Spoken), harness.Client.Player.Missions[Den.MissionId].Completeable), "6 and 8 done: to be radioed in");
        }

        /// <summary>
        /// Into the Lion's Den's race: 590 s after the accept 1 is still open; past 600 s it fails and the mission with it; no
        /// fighters came. Cleared, the next arrival offers it again by radio; taken again, at the arena before the deadline 1
        /// completes, and the deadline passing after that fails nothing.
        /// </summary>
        [TestMethod]
        public void IntoTheLionsDenFailsAtItsDeadlineAndIsOfferedAgain()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Before1551);
            var copy = IntoFacility(harness);
            TakeByRadio(harness, harness.Client, Den.MissionId);
            Tick(harness, copy, 250);
            harness.UtcNow = harness.UtcNow.AddSeconds(Den.ArenaSeconds - 10);
            harness.Manager.EvaluateDeadlines(harness.Client);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Den.MissionId, Den.ToTheArena), "590 s: still open");
            harness.UtcNow = harness.UtcNow.AddSeconds(15);
            harness.Manager.EvaluateDeadlines(harness.Client);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Failed, MissionState.Failed),
                (Objective(harness, Den.MissionId, Den.ToTheArena), harness.Client.Player.Missions[Den.MissionId].State), "past 600 s: 1 failed, the mission failed");
            Assert.IsTrue(Den.PitFighters.All(fighter => SceneCreature(harness, copy, Den.MissionId, fighter.Role) == null), "no fighters came");
            Assert.IsTrue(harness.Manager.TryClear(harness.Client, Den.MissionId), "the failed mission cleared");
            harness.Drain();

            TakeByRadio(harness, harness.Client, Den.MissionId);
            Tick(harness, copy, 250);
            ToTheArena(harness, copy);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Den.MissionId, Den.ToTheArena), "in time: 1 done");
            harness.UtcNow = harness.UtcNow.AddSeconds(Den.ArenaSeconds + 60);
            harness.Manager.EvaluateDeadlines(harness.Client);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionState.Active),
                (Objective(harness, Den.MissionId, Den.ToTheArena), harness.Client.Player.Missions[Den.MissionId].State), "the deadline after the arena fails nothing");
        }

        /// <summary>
        /// Into the Lion's Den's arena is entered on the real way (MissionAreaService takes an entry only from a move that ends
        /// inside it): the walk from the east wing's pad 926 to the ledge in moves of 1 m or less, and the same with every other
        /// corner cut. On the pad and the first stretch 1 is still open; at the ledge's east end, 14 m short of the walk's end, it is
        /// done.
        /// </summary>
        [TestMethod]
        public void IntoTheLionsDensArenaIsEnteredOnTheWayIn()
        {
            var leg = Legs["the east wing's pad to the ledge"];
            var cut = leg.Where((_, index) => index % 2 == 0 || index == leg.Length - 1).ToArray();
            foreach (var (label, corners) in new[] { ("the walk", leg), ("the corners cut", cut) })
            {
                using var harness = WildernessRuntimeTestHarness.Create();
                AshenRuntime.SeedCompleted(harness, Before1551);
                var copy = IntoFacility(harness);
                TakeByRadio(harness, harness.Client, Den.MissionId);
                Tick(harness, copy, 250);
                Walk(harness, corners.Take(2).ToArray());
                Tick(harness, copy, 250);
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Den.MissionId, Den.ToTheArena), $"{label}: off the pad, 1 open");
                // Up to the ledge's east end (254, -99), 14 m short of the walk's end: entered on the way, not only at the end.
                var east = Array.FindIndex(corners, corner => corner.X == 254f && corner.Z == -99f);
                Assert.IsGreaterThan(0, east, "the ledge's east end is a corner of both");
                Walk(harness, corners.Skip(1).Take(east).ToArray());
                Tick(harness, copy, 250);
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Den.MissionId, Den.ToTheArena), $"{label}: on the ledge, 1 done");
            }
        }

        /// <summary>
        /// Into the Lion's Den in a squad of two, both at the arena: the player's side done completes the mate's matching objective
        /// by the shared credit (GroupCreditService), and the fighters still standing, the mate's own of both sides and the
        /// player's of the side lost, fall: killed by nobody, not removed. The player's own kills keep their loot; the fallen are nobody's kill (no harvest owner: CreatureManager pays XP and makes loot only with a player behind
        /// the kill, as it sets that owner), with no loot; the switch's use, and the fallen with it, give no XP.
        /// </summary>
        [TestMethod]
        public void IntoTheLionsDenClearsTheMatesOwnFightersWithTheSharedCredit()
        {
            foreach (var pit in new[] { true, false })
            {
                using var harness = WildernessRuntimeTestHarness.Create();
                AshenRuntime.SeedCompleted(harness, Before1551);
                var copy = IntoFacility(harness);
                TakeByRadio(harness, harness.Client, Den.MissionId);
                Tick(harness, copy, 250);
                using var mate = new Facility.SquadMate(harness, copy, Giver(copy, AshenConscriptionEntryNpcsV1.Peacock), 0, Before1551);
                TakeByRadio(harness, mate.Client, Den.MissionId);
                Tick(harness, copy, 250);
                MissionObjectiveState MateOf(uint objective) => mate.Client.Player.Missions[Den.MissionId].Objectives[objective].State;
                ToTheArena(harness, copy);
                MateIntoTheArena(harness, copy, mate);
                Assert.AreEqual(MissionObjectiveState.Completed, MateOf(Den.ToTheArena), "the mate at the arena");
                var pitRoles = Den.PitFighters.Select(fighter => fighter.Role).ToArray();
                var ledgeRoles = Den.LedgeFighters.Select(fighter => fighter.Role).ToArray();
                var all = pitRoles.Concat(ledgeRoles).ToArray();
                Assert.AreEqual((all.Length, all.Length), (Bodies(harness, copy, harness.Client, all).Count(alive => alive.State != CharacterState.Dead),
                    Bodies(harness, copy, mate.Client, all).Count(alive => alive.State != CharacterState.Dead)), "both members' own fighters");
                var side = pit ? "the pit" : "the ledge";
                var killed = new List<Creature>();
                (long Player, long Mate) Xp() => (harness.Client.Player.Experience, mate.Client.Player.Experience);
                (long Player, long Mate) Gain((long Player, long Mate) before) { var now = Xp(); return (now.Player - before.Player, now.Mate - before.Mate); }
                foreach (var role in pit ? pitRoles : ledgeRoles)
                {
                    var creature = SceneCreature(harness, copy, Den.MissionId, role);
                    killed.Add(creature);
                    Kill(harness, copy, creature);
                }
                for (var tick = 0; tick < 4; tick++)
                    Tick(harness, copy, 250);
                if (!pit)
                {
                    var before = Xp();
                    Use(harness, copy, SceneObject(harness, copy, Den.MissionId, Den.SwitchRole), Den.UseArgument);
                    for (var tick = 0; tick < 4; tick++)
                        Tick(harness, copy, 250);
                    Assert.AreEqual((0L, 0L), Gain(before), "the ledge: the use, and the fallen with it, give no XP");
                }
                Assert.AreEqual(MissionObjectiveState.Completed, MateOf(pit ? Den.HelpHumphries : Den.HelpThePrisoners), $"{side}: the mate's objective by the shared credit");
                var mine = Bodies(harness, copy, harness.Client, all);
                var mates = Bodies(harness, copy, mate.Client, all);
                Assert.IsTrue(mine.Concat(mates).All(body => body.State == CharacterState.Dead), $"{side}: none stands");
                foreach (var body in killed)
                    Assert.IsTrue(copy.LootDispensers.ContainsKey(body.CorpseLootEntityId),
                        $"{side}: the player's kill {body.SpawnPool.SceneActorRole}: its loot stays (on the body or, the corpse's time up, its gibblet)");
                foreach (var body in mine.Except(killed).Concat(mates))
                    Assert.IsTrue(body.HarvestOwnerEntityId == 0 && !copy.LootDispensers.ContainsKey(body.CorpseLootEntityId),
                        $"{side}: the fallen {body.SpawnPool.SceneActorRole}: nobody's kill (no XP), no loot");
            }
        }

        /// <summary>
        /// Into the Lion's Den's switch step comes no more once a side is decided (re-audit): the hidden 7 fails when 3 or 4 is
        /// done first, also by a mate's shared credit, so the ledge's tally finishing late (the fighters falling) lights no switch.
        /// (a) The pit decided (the player's kills; the mate's 3 by the credit): both members' 7 failed, and their switches gone.
        /// (b) The player at the arena first, one of his ledge's six dead before the mate comes; the rest dead (the mate's 7 at
        /// five by the credit) and the player's switch used: the mate's 4 by the credit, his 7 failed, his switch on and not
        /// usable, also some seconds on.
        /// </summary>
        [TestMethod]
        public void IntoTheLionsDenLightsNoSwitchOnceASideIsDecided()
        {
            foreach (var pit in new[] { true, false })
            {
                using var harness = WildernessRuntimeTestHarness.Create();
                AshenRuntime.SeedCompleted(harness, Before1551);
                var copy = IntoFacility(harness);
                TakeByRadio(harness, harness.Client, Den.MissionId);
                Tick(harness, copy, 250);
                using var mate = new Facility.SquadMate(harness, copy, Giver(copy, AshenConscriptionEntryNpcsV1.Peacock), 0, Before1551);
                TakeByRadio(harness, mate.Client, Den.MissionId);
                Tick(harness, copy, 250);
                MissionObjectiveState MateOf(uint objective) => mate.Client.Player.Missions[Den.MissionId].Objectives[objective].State;
                ToTheArena(harness, copy);
                if (pit)
                {
                    MateIntoTheArena(harness, copy, mate);
                    foreach (var (role, _, _) in Den.PitFighters)
                        Kill(harness, copy, SceneCreature(harness, copy, Den.MissionId, role));
                    for (var tick = 0; tick < 12; tick++)
                        Tick(harness, copy, 250);
                    Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Completed, MissionObjectiveState.Failed, MissionObjectiveState.Failed),
                        (Objective(harness, Den.MissionId, Den.HelpHumphries), MateOf(Den.HelpHumphries), Objective(harness, Den.MissionId, Den.LedgeCleared), MateOf(Den.LedgeCleared)),
                        "(a) the pit decided: both 3 done, both 7 failed");
                    Assert.IsNull(FindSceneObject(harness, copy, Den.MissionId, Den.SwitchRole), "(a) the player's switch gone");
                    Assert.IsNull(MateObject(harness, copy, mate.Client, Den.MissionId, Den.SwitchRole), "(a) the mate's switch gone");
                }
                else
                {
                    Kill(harness, copy, SceneCreature(harness, copy, Den.MissionId, Den.LedgeFighters[0].Role));
                    MateIntoTheArena(harness, copy, mate);
                    foreach (var (role, _, _) in Den.LedgeFighters.Skip(1))
                        Kill(harness, copy, SceneCreature(harness, copy, Den.MissionId, role));
                    Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                        (Objective(harness, Den.MissionId, Den.LedgeCleared), MateOf(Den.LedgeCleared)), "(b) the player's 7 done, the mate's at five");
                    Use(harness, copy, SceneObject(harness, copy, Den.MissionId, Den.SwitchRole), Den.UseArgument);
                    for (var tick = 0; tick < 12; tick++)
                        Tick(harness, copy, 250);
                    var control = MateObject(harness, copy, mate.Client, Den.MissionId, Den.SwitchRole);
                    Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Failed, (UseObjectState)Den.SwitchOn, false),
                        (MateOf(Den.HelpThePrisoners), MateOf(Den.LedgeCleared), control.StateId, control.IsEnabled),
                        "(b) the cells open: the mate's 4 by the credit, his 7 failed, his switch on and not usable");
                }
            }
        }

        /// <summary>The mate walks onto the ledge: his arena entered (an accepted move into it), his fights come.</summary>
        private static void MateIntoTheArena(WildernessRuntimeTestHarness harness, MapChannel copy, Facility.SquadMate mate)
        {
            mate.Beside(ToVector(Den.LedgeFighters[0].At) + new Vector3(0, 0, -5));
            mate.Client.MissionAreaService.RecordAcceptedMovement(mate.Client, new Vector3(288, 96.02f, -146), ToVector(Den.LedgeFighters[0].At) + new Vector3(0, 0, -5));
            Tick(harness, copy, 250);
        }

        /// <summary>The bodies (living or dead) of a member's own 1499 scene creatures of these roles in the copy.</summary>
        private static List<Creature> Bodies(WildernessRuntimeTestHarness harness, MapChannel copy, Client member, IEnumerable<string> roles)
        {
            using var unit = harness.CreateChar();
            var runs = unit.CharacterMissions.Runtime.Scenes(member.Player.Id, Den.MissionId).Select(scene => scene.RunId).ToHashSet();
            var set = roles.ToHashSet();
            return Here(copy).Where(creature => creature.SpawnPool?.SceneActorRole is { } role && set.Contains(role) && runs.Contains(creature.SpawnPool.SceneRunId)).ToList();
        }

        /// <summary>
        /// Into the Lion's Den in a squad of two on it, the mate not yet at the arena: the player's arena and his pit fights leave
        /// the mate's 1 open and his 3 waiting (not credited by the player's kills: his 3 is not open), and the player's fighters
        /// stay the player's; the mate's own arrival brings his own fights.
        /// </summary>
        [TestMethod]
        public void IntoTheLionsDenWaitsForAMateBehind()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Before1551);
            var copy = IntoFacility(harness);
            TakeByRadio(harness, harness.Client, Den.MissionId);
            Tick(harness, copy, 250);
            using var mate = new Facility.SquadMate(harness, copy, Giver(copy, AshenConscriptionEntryNpcsV1.Peacock), 0, Before1551);
            TakeByRadio(harness, mate.Client, Den.MissionId);
            Tick(harness, copy, 250);
            MissionObjectiveState MateOf(uint objective) => mate.Client.Player.Missions[Den.MissionId].Objectives[objective].State;
            ToTheArena(harness, copy);
            foreach (var (role, _, _) in Den.PitFighters)
            {
                Kill(harness, copy, SceneCreature(harness, copy, Den.MissionId, role));
                Tick(harness, copy, 250);
            }
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Objective(harness, Den.MissionId, Den.HelpHumphries), MateOf(Den.ToTheArena), MateOf(Den.HelpHumphries)), "the player's 3 done; the mate's 1 open, 3 waits");
            mate.Beside(ToVector(Den.LedgeFighters[0].At) + new Vector3(0, 0, -5));
            mate.Client.MissionAreaService.RecordAcceptedMovement(mate.Client, new Vector3(288, 96.02f, -146), ToVector(Den.LedgeFighters[0].At) + new Vector3(0, 0, -5));
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete), (MateOf(Den.ToTheArena), MateOf(Den.HelpHumphries)), "the mate at the arena: his 1 done, 3 open");
            using var unit = harness.CreateChar();
            var runs = unit.CharacterMissions.Runtime.Scenes(mate.Client.Player.Id, Den.MissionId).Select(scene => scene.RunId).ToHashSet();
            Assert.AreEqual(Den.PitFighters.Length, Here(copy).Count(creature => creature.SpawnPool?.SceneActorRole?.StartsWith("pit-") == true &&
                runs.Contains(creature.SpawnPool.SceneRunId) && creature.State != CharacterState.Dead), "the mate's own pit fighters");
        }

        /// <summary>
        /// Save the Queen through: by radio on the 1499 reward (not before). Taken: 1 open, the rest waits; the player's own queen
        /// talk object, Life Support System's console (fix V2: 7468, ready, not usable, named 153), the hanging device (a visual,
        /// 10000002), egg and egg talk stand. In Information Extraction 1 completes; Xahn's kill completes 2; a use of the console
        /// before 3 is refused; the queen hears 3, her talk makes the console usable (also after a reload, and after a reload of a
        /// run whose journal has no visual device, as a run from before fix V2); its use completes 4: the console set, the device
        /// at 10000003, the queen's talk object gone (also after a reload). The egg hears 5: the egg goes. Yamamoto on Ashen Desert hears 6;
        /// Jawan on Thunderhead hears 7, and takes the mission in (a full bag waits; the piece chosen once).
        /// </summary>
        [TestMethod]
        public void SaveTheQueenEndsHerSufferingAndTakesTheEggToJawan()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Before1551);
            var copy = IntoFacility(harness);
            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.AreEqual(0, harness.Drain().OfType<DispenseRadioMissionPacket>().Count(packet => packet.MissionId == Queen.MissionId), "not before 1499");
            AshenRuntime.SeedCompleted(harness, Den.MissionId);
            TakeByRadio(harness, harness.Client, Queen.MissionId);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Queen.MissionId, Queen.FindTheQueen), "1 open");
            for (var objective = Queen.KillTheEnslaver; objective <= Queen.ToJawan; objective++)
                Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Queen.MissionId, objective), $"{objective} waits");
            var console = SceneObject(harness, copy, Queen.MissionId, Queen.DeviceRole);
            Assert.AreEqual((V2.ConsoleClass, (UseObjectState)V2.ConsoleReady, false, Queen.DeviceName, ToVector(V2.ConsoleAt)),
                ((uint)console.EntityClassId, console.StateId, console.IsEnabled, console.NameOverrideId, console.Position), "the Life Support System's console (fix V2), ready, not usable, named 153");
            AssertDevice(harness, copy, AshenFixVV1.DeviceActive, "spawned: the hanging device lowered (10000002), a visual only");
            Assert.AreEqual(Queen.EggClass, (uint)SceneObject(harness, copy, Queen.MissionId, Queen.EggRole).EntityClassId, "the egg");
            var queen = SceneObject(harness, copy, Queen.MissionId, Queen.QueenRole);
            Assert.IsEmpty(ObjectTopics(harness, harness.Client, queen), "the queen talks nothing before 3");

            ToTheQueen(harness, copy);
            Use(harness, copy, SceneObject(harness, copy, Queen.MissionId, Queen.DeviceRole), Queen.UseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Inactive, (UseObjectState)V2.ConsoleReady),
                (Objective(harness, Queen.MissionId, Queen.EndTheSuffering), SceneObject(harness, copy, Queen.MissionId, Queen.DeviceRole).StateId), "a use before 3: refused, 4 waits, the console ready");
            queen = SceneObject(harness, copy, Queen.MissionId, Queen.QueenRole);
            CollectionAssert.AreEqual(new[] { (Queen.MissionId, Queen.TalkToTheQueen) }, ObjectTopics(harness, harness.Client, queen), "the queen hears 3");
            TalkToObject(harness, harness.Client, queen, Queen.MissionId, Queen.TalkToTheQueen);
            Tick(harness, copy, 250);
            AssertConsoleUsable(harness, copy, "3 done: 4 open, the console usable");
            AssertDevice(harness, copy, AshenFixVV1.DeviceActive, "3 done: the device as it was");
            ReloadAndRecover(harness, copy);
            AssertConsoleUsable(harness, copy, "3 done, after a reload: the console usable");
            AssertDevice(harness, copy, AshenFixVV1.DeviceActive, "3 done, after a reload: the device");

            // A run that ran sequence 0 and 3's step before fix V2 (char 565 on the live server): its journal holds the device role's
            // Ensure and SetInteraction, not the visual's Ensure. A rebuild replays the journal: the console stands usable at once.
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    foreach (var run in Runs(harness, Queen.MissionId))
                        foreach (var effect in unit.CharacterMissions.Runtime.Effects(run).Where(effect => effect.OperationKey == V2.VisualCreateKey))
                        {
                            effect.Status = "Cancelled";
                            effect.Version++;
                        }
                });
            // and its copy made again (a restart): no hanging device in it.
            var hanging = SceneObject(harness, copy, Queen.MissionId, V2.VisualRole);
            CellManager.Instance.RemoveFromWorld(copy, hanging);
            copy.DynamicObjects.Remove(hanging);
            ReloadAndRecover(harness, copy);
            AssertConsoleUsable(harness, copy, "a pre-fix run with 3 done, after a reload: the console usable at once");
            Assert.IsNull(FindSceneObject(harness, copy, Queen.MissionId, V2.VisualRole), "a pre-fix run: no hanging device before 4");

            Use(harness, copy, SceneObject(harness, copy, Queen.MissionId, Queen.DeviceRole), Queen.UseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Queen.MissionId, Queen.EndTheSuffering), "the console's use: 4 done");
            AssertDevice(harness, copy, AshenFixVV1.DeviceEnd, "4 done: the device made again and at its chain's end");
            ReloadAndRecover(harness, copy);
            console = SceneObject(harness, copy, Queen.MissionId, Queen.DeviceRole);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete, (UseObjectState)V2.ConsoleSet, false),
                (Objective(harness, Queen.MissionId, Queen.EndTheSuffering), Objective(harness, Queen.MissionId, Queen.SecureTheEgg), console.StateId, console.IsEnabled),
                "4 done, 5 open, the console set (56), not usable; also after a reload");
            AssertDevice(harness, copy, AshenFixVV1.DeviceEnd, "4 done, after a reload: the device at its chain's end");
            Assert.IsNull(FindSceneObject(harness, copy, Queen.MissionId, Queen.QueenRole), "the queen at rest: her talk object gone");
            var egg = SceneObject(harness, copy, Queen.MissionId, Queen.EggTalkRole);
            CollectionAssert.AreEqual(new[] { (Queen.MissionId, Queen.SecureTheEgg) }, ObjectTopics(harness, harness.Client, egg), "the egg hears 5");
            TalkToObject(harness, harness.Client, egg, Queen.MissionId, Queen.SecureTheEgg);
            Tick(harness, copy, 250);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Queen.MissionId, Queen.ToYamamoto), "6 open");
            foreach (var role in new[] { Queen.EggRole, Queen.EggTalkRole })
                Assert.IsNull(FindSceneObject(harness, copy, Queen.MissionId, role), $"{role} taken");

            var desert = AshenLeadInsTests.EnterAshen(harness, V(AshenLeadInNpcsV1.YamamotoAt) + new Vector3(0, 0, 2));
            var yamamoto = Npc(desert, AshenLeadInNpcsV1.Yamamoto);
            CollectionAssert.AreEqual(new[] { Queen.ToYamamoto }, Topics(harness, yamamoto, Queen.MissionId), "Yamamoto hears 6");
            Talk(harness, yamamoto, Queen.MissionId, Queen.ToYamamoto);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Queen.MissionId, Queen.ToJawan), "7 open");
            Assert.IsFalse(harness.Client.Player.Missions[Queen.MissionId].Completeable);

            var thunderhead = EnterThunderhead(harness, V(N.JawanAt) + new Vector3(0, 0, 2));
            var jawan = Npc(thunderhead, N.Jawan);
            CollectionAssert.AreEqual(new[] { Queen.ToJawan }, Topics(harness, jawan, Queen.MissionId), "Jawan hears 7");
            Talk(harness, jawan, Queen.MissionId, Queen.ToJawan);
            Assert.IsTrue(harness.Client.Player.Missions[Queen.MissionId].Completeable, "to be turned in at Jawan");
            jawan = Npc(thunderhead, N.Jawan);
            FullBagWaits(harness, jawan, Queen.MissionId, 1, ModuleFiller);
            FailedTurnInPaysNothing(harness, jawan, Queen.MissionId, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, jawan, Queen.MissionId, 1, Queen.Experience, Queen.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Queen.MissionId), "MissionRewarded once");
            Assert.AreEqual(1U, Facility.Held(harness, Queen.StealPower), "the module chosen");
        }

        /// <summary>1501's console (fix V2, the device role): 4 open, usable, ready (55).</summary>
        private static void AssertConsoleUsable(WildernessRuntimeTestHarness harness, MapChannel copy, string what)
        {
            var console = SceneObject(harness, copy, Queen.MissionId, Queen.DeviceRole);
            Assert.AreEqual((MissionObjectiveState.Incomplete, V2.ConsoleClass, true, (UseObjectState)V2.ConsoleReady),
                (Objective(harness, Queen.MissionId, Queen.EndTheSuffering), (uint)console.EntityClassId, console.IsEnabled, console.StateId), what);
        }

        /// <summary>1501's hanging device (fix V2's visual role): its class and place, never usable, in <paramref name="state"/>.</summary>
        private static void AssertDevice(WildernessRuntimeTestHarness harness, MapChannel copy, uint state, string what)
        {
            var device = SceneObject(harness, copy, Queen.MissionId, V2.VisualRole);
            Assert.AreEqual((Queen.DeviceClass, (UseObjectState)state, false, ToVector(Queen.DeviceAt)),
                ((uint)device.EntityClassId, device.StateId, device.IsEnabled, device.Position), what);
        }

        /// <summary>
        /// An abandon of Save the Queen after the queen's talk: the device, the queen's talk object, the egg and its talk go. Taken
        /// again, they stand again, the device not usable, 1 open; Xahn killed again opens 3 again.
        /// </summary>
        [TestMethod]
        public void SaveTheQueenAbandonedStartsAgain()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Before1551.Append(Den.MissionId).ToArray());
            var copy = IntoFacility(harness);
            TakeByRadio(harness, harness.Client, Queen.MissionId);
            Tick(harness, copy, 250);
            ToTheQueen(harness, copy);
            TalkToObject(harness, harness.Client, SceneObject(harness, copy, Queen.MissionId, Queen.QueenRole), Queen.MissionId, Queen.TalkToTheQueen);
            Tick(harness, copy, 250);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Queen.MissionId));
            harness.Drain();
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            foreach (var role in new[] { Queen.DeviceRole, V2.VisualRole, Queen.QueenRole, Queen.EggRole, Queen.EggTalkRole })
                Assert.IsNull(FindSceneObject(harness, copy, Queen.MissionId, role), $"{role} goes with the abandon");
            TakeByRadio(harness, harness.Client, Queen.MissionId);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, false),
                (Objective(harness, Queen.MissionId, Queen.FindTheQueen), SceneObject(harness, copy, Queen.MissionId, Queen.DeviceRole).IsEnabled), "taken again: 1 open, the device not usable");
            ToTheQueen(harness, copy);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Queen.MissionId, Queen.TalkToTheQueen), "3 open again");
        }

        /// <summary>
        /// Ashen_conscription_queen takes out 1551, 1499 and 1501, the seven creatures (creature, stats, looks, packages) with their
        /// four pools and the pads 925-928 with their links; the rows they copy stay; it updates nothing. Every table the migration
        /// writes is counted (set 20's ids and missions only). Nothing else changes. Up again, all of it is back; the NPC SQL run
        /// again changes nothing.
        /// </summary>
        [TestMethod]
        public void ConscriptionQueenRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: AshenMissionTests.ConscriptionQueen);
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            const string ids = "(mission_id IN (1551, 1499, 1501) AND content_revision = 'ashen_1_16_5')";
            var npcs = string.Join(", ", N.Creatures);
            var pools = string.Join(", ", N.PoolIds);
            var pads = string.Join(", ", Pads);
            var tables = new[]
            {
                "mission_content_definition", "mission_objective_definition", "mission_objective_transition", "mission_trigger", "mission_action",
                "mission_prerequisite", "mission_reward_definition", "mission_reward_item", "mission_indicator", "mission_scene_binding", "mission_evidence",
                "mission_scenario", "mission_scenario_step", "mission_channel_policy", "mission_repeat_policy", "mission_spawn", "mission_spawn_group", "mission_area"
            };
            string All() => string.Join(", ", tables.Select(table => $"{table} {Count($"SELECT COUNT(*) AS Value FROM {table} WHERE {ids}")}")
                .Concat(new[] { "creature", "creature_stat", "creature_appearance", "npc_package" }
                    .Select(table => $"{table} {Count($"SELECT COUNT(*) AS Value FROM {table} WHERE id IN ({npcs})")}"))
                .Append($"spawnpool {Count($"SELECT COUNT(*) AS Value FROM spawnpool WHERE id IN ({pools})")}")
                .Append($"teleporter {Count($"SELECT COUNT(*) AS Value FROM teleporter WHERE id IN ({pads})")}")
                .Append($"local_teleporter_link {Count($"SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE from_teleporter_id IN ({pads}) OR to_teleporter_id IN ({pads})")}")
                .Append($"kept {Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({N.Harris}, {N.Brann}, {N.AttaSoldier}, {N.ImperialGuard}, {N.Mitecht})")}"));
            static string Others(DbContext db) => string.Join("; ", db.Set<SpawnPoolEntry>().AsNoTracking()
                    .Where(entry => (entry.MapContextId == AshenMissionDataV1.MapContextId || entry.MapContextId == N.Facility || entry.MapContextId == N.Thunderhead) &&
                        !N.PoolIds.Contains(entry.Id))
                    .OrderBy(entry => entry.Id).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.Id}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{entry.Radius:R},{entry.Creature1Id},{entry.Creature1MaxCount}")))
                + " | " + string.Join("; ", db.Set<TeleporterEntry>().AsNoTracking().Where(entry => !Pads.Contains(entry.Id))
                    .OrderBy(entry => entry.Id).AsEnumerable().Select(entry => FormattableString.Invariant($"{entry.Id}:{entry.MapContextId},{entry.PosX:R},{entry.PosZ:R}")))
                + " | " + string.Join("; ", db.Set<MissionContentDefinitionEntry>().AsNoTracking()
                    .Where(entry => entry.MissionId != 1551 && entry.MissionId != 1499 && entry.MissionId != 1501)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision)
                    .AsEnumerable().Select(entry => $"{entry.MissionId}/{entry.ContentRevision}"))
                + " | " + string.Join("; ", db.Set<MissionIndicatorEntry>().AsNoTracking()
                    .Where(entry => (entry.ContentRevision == AshenMissionDataV1.Revision || entry.ContentRevision == CrucibleMissionDataV1.Revision)
                        && entry.MissionId != 1551 && entry.MissionId != 1499 && entry.MissionId != 1501)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.MissionId}.{entry.ObjectiveId}/{entry.IndicatorId}:{entry.PosX:R},{entry.PosZ:R}")));
            string others;
            using (var atQueen = WildernessRuntimeTestHarness.Create(targetWorldMigration: AshenMissionTests.ConscriptionQueen))
                others = Others(atQueen.World);
            // 1551: 3 objectives, 3 transitions; triggers: 1 the kills, 3 the queen, 2 the talk; actions 3's scenario, 2's reward;
            // one scenario, no steps; two markers. 1499: 9 objectives (2 and 8 the scene's aggregations), 12 transitions (1 two, 3
            // two, 4 two, 5, 6, 7 three, 9); triggers: 1 the area and the deadline, 9 seven, 3 the state of 9 and its fail, 4 the switch
            // and its fail, 5, 6 the talks, 7 six and its two fails; actions 1: four opens and the scenario (9), 3: open 5 and the scenario (3), 4:
            // the same (3), 7: the scenario, 5 and 6 the reward; scenarios 1-7 (2 seven steps, 3 six, 4 one); the radio's channel;
            // five markers; one area. 1501: 7 objectives and transitions; triggers 1 the area, 2 the kill, 3, 5, 6, 7 the talks, 4 the device; actions
            // two opens each of 1-6, the scenarios of 3, 4, 5, the reward; scenarios 1-4 (1 one step); seven markers; one area.
            // Each one prerequisite, one reward (four items), three evidence rows, a scene, a channel. The seven creatures with stats
            // (the friends' looks: Harris' and the Brann's), Humphries', Lukaz' and Jawan's packages; four pools; four pads and four links.
            const string up = "mission_content_definition 3, mission_objective_definition 19, mission_objective_transition 22, mission_trigger 33, " +
                "mission_action 36, mission_prerequisite 3, mission_reward_definition 3, mission_reward_item 12, mission_indicator 14, " +
                "mission_scene_binding 3, mission_evidence 9, mission_scenario 12, mission_scenario_step 15, mission_channel_policy 3, " +
                "mission_repeat_policy 0, mission_spawn 0, mission_spawn_group 0, mission_area 2, creature 7, creature_stat 7, " +
                "creature_appearance LOOK, npc_package 3, spawnpool 4, teleporter 4, local_teleporter_link 4, kept 5";
            const string down = "mission_content_definition 0, mission_objective_definition 0, mission_objective_transition 0, mission_trigger 0, " +
                "mission_action 0, mission_prerequisite 0, mission_reward_definition 0, mission_reward_item 0, mission_indicator 0, " +
                "mission_scene_binding 0, mission_evidence 0, mission_scenario 0, mission_scenario_step 0, mission_channel_policy 0, " +
                "mission_repeat_policy 0, mission_spawn 0, mission_spawn_group 0, mission_area 0, creature 0, creature_stat 0, " +
                "creature_appearance 0, npc_package 0, spawnpool 0, teleporter 0, local_teleporter_link 0, kept 5";
            var look = Count($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = {N.Harris}") * 2 +
                Count($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = {N.Brann}") * 2;
            var upNow = up.Replace("LOOK", look.ToString(System.Globalization.CultureInfo.InvariantCulture));
            void AllUp(string when)
            {
                Assert.IsTrue(Missions.All(Has), $"{when}: the three missions");
                var all = All();
                Assert.AreEqual(upNow, all, $"{when}: {all}");
                Assert.AreEqual(others, Others(world), $"{when}: nothing else changes");
            }

            AllUp("at its migration");
            world.GetService<IMigrator>().Migrate(AshenMissionTests.Previous(world, AshenMissionTests.ConscriptionQueen));
            world.ChangeTracker.Clear();
            Assert.IsFalse(Missions.Any(Has));
            Assert.AreEqual(down, All());
            Assert.AreEqual(others, Others(world), "Down: nothing else changes");
            Assert.IsTrue(AshenMissionTests.MissionIds.Where(id => !Missions.Contains(id)).All(Has), "sets 1-19 stay");
            foreach (var sql in N.PadsDown())
                world.Database.ExecuteSqlRaw(sql);
            foreach (var sql in N.Down())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            Assert.AreEqual(down, All(), "a second Down changes nothing");

            world.GetService<IMigrator>().Migrate(AshenMissionTests.ConscriptionQueen);
            world.ChangeTracker.Clear();
            AllUp("Up again");
            foreach (var sql in N.Up())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            foreach (var sql in N.PadsUp())
                world.Database.ExecuteSqlRaw(sql);
            AllUp("the NPC and pad rows run again");
        }

        /// <summary>
        /// The squad's copy of the facility (Entry.IntoFacility), and stale scene copies gone. The pools' cleared marks stay: the
        /// named pools come back on their own respawn time (TheNamedPoolsComeBackOnTheirOwnTimeInASquadsCopy).
        /// </summary>
        internal static MapChannel IntoFacility(WildernessRuntimeTestHarness harness)
        {
            var copy = Entry.IntoFacility(harness);
            var runs = Missions.SelectMany(mission => Runs(harness, mission)).ToHashSet();
            copy.SpawnPools.RemoveAll(pool => pool.SceneRunId != null && Missions.Contains(pool.ScenarioMissionId) && !runs.Contains(pool.SceneRunId));
            foreach (var creature in Here(copy).Where(one => Pools.Contains(one.SpawnPool?.DbId ?? 0)).ToArray())
                if (creature.State == CharacterState.Dead)
                    RunTheClock(harness, copy, creature.SpawnPool.DbId);
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            return copy;
        }

        /// <summary>Thunderhead loaded and the player on it at <paramref name="at"/> (as AshenLeadInsTests.EnterAshen).</summary>
        private static MapChannel EnterThunderhead(WildernessRuntimeTestHarness harness, Vector3 at)
        {
            var map = harness.Maps.MapChannelArray.TryGetValue(N.Thunderhead, out var loaded) ? loaded : PlateauInstanceMaps.LoadMap(harness, N.Thunderhead);
            var client = harness.Client;
            CellManager.Instance.RemoveFromWorld(client);
            client.Player.MapChannel.ClientList.Remove(client);
            client.Player.MapChannel = map;
            client.Player.RuntimeMapChannel = map;
            client.Player.MapContextId = N.Thunderhead;
            map.ClientList.Add(client);
            harness.MoveTo(at);
            CellManager.Instance.AddToWorld(client);
            harness.Drain();
            return map;
        }

        /// <summary>The hostile spawn pools of a map (the facility's by default).</summary>
        internal static List<SpawnPoolEntry> HostilePools(WildernessRuntimeTestHarness harness, uint map = Map) =>
            harness.World.SpawnPoolEntries.AsNoTracking().Where(row => row.MapContextId == map && row.Mode == 0).AsEnumerable()
                .Where(row => new[] { row.Creature1Id, row.Creature2Id, row.Creature3Id, row.Creature4Id, row.Creature5Id, row.Creature6Id }
                    .Any(id => id != 0 && CreatureManager.Instance.LoadedCreatures.TryGetValue(id, out var creature) && creature.TargetCategory == TargetCategory.Hostile))
                .ToList();

        /// <summary>
        /// The set's places: label, place, whether it is seen from the ring, the height looked at, its clearance (34 m, 2D, outside
        /// every hostile pool's circle but the set's own; 35 m for the bosses' pools; the breaks: Information Extraction's places
        /// 19 m from its Enslaver cell pools 531073-531077, TaRapedia's room; docs/ashen-playtest.md).
        /// </summary>
        internal static List<(string Label, Vector3 At, bool Ring, float Middle, float Clearance)> Places() => new()
        {
            ("the hall's east-wing pad", V(AshenFixS2V1.PadAt(N.HallWingPad)), false, 0, Clearance), ("the east wing's pad", V(N.WingPadAt), false, 0, Clearance),
            ("the hall's fortress pad", V(AshenFixS2V1.PadAt(N.HallFortressPad)), false, 0, Clearance), ("the fortress' pad", V(N.FortressPadAt), false, 0, ExtractionClearance),
            ("the Enraged Atta's pool", V(N.EnragedAttaAt), false, 0, 60), ("the queen's pool", V(N.QueenAt), false, 0, 60),
            ("the DNA sample", ToVector(Tick551.SampleAt), true, F.ObjectMiddle, Clearance),
            ("Humphries", ToVector(Den.HumphriesAt), true, F.NpcMiddle, Clearance), ("Lukaz", ToVector(Den.LukazAt), true, F.NpcMiddle, Clearance),
            ("the Cell Door Control", ToVector(Den.SwitchAt), true, F.ObjectMiddle, Clearance),
            ("the arena's marker", ToVector(Den.ArenaMarkerAt), false, 0, Clearance),
            ("Xahn's pool", V(N.XahnAt), false, 0, ExtractionClearance), ("the queen's talk", ToVector(Queen.QueenAt), true, F.NpcMiddle, ExtractionClearance),
            ("the Life Support System", ToVector(Queen.DeviceAt), true, F.ObjectMiddle, ExtractionClearance),
            ("the Life Support System's console", ToVector(V2.ConsoleAt), true, F.ObjectMiddle, ExtractionClearance),
            ("the egg", ToVector(Queen.EggAt), true, F.ObjectMiddle, ExtractionClearance),
            ("Information Extraction's marker", ToVector(Queen.ExtractionMarkerAt), false, 0, ExtractionClearance)
        };

        private const float Clearance = 34, ExtractionClearance = 19, PassedMinimum = 15;

        /// <summary>The pools a walk goes through, not its target (rule breaks, docs/ashen-playtest.md): Humphries' way down.</summary>
        private static readonly Dictionary<string, uint[]> WalkThrough = new()
        {
            ["the ledge down to Humphries (1499)"] = new[] { 531065u }
        };

        /// <summary>How far each leg's start is moved along its first stretch for the walk's recheck (as AshenAvernusHiveTests).</summary>
        private static readonly float[] StartShifts = { 0, 0.13f, 0.27f, 0.4f };

        /// <summary>The upstream pools a walk passes (rule breaks, docs/ashen-playtest.md): the ramp's to the pit, Information Extraction's and the fortress'.</summary>
        private static readonly uint[] RampPools = { 531065 };
        private static readonly uint[] ExtractionPools = { 531073, 531074, 531075, 531076, 531077 };

        private const string ThunderheadLeg = "Thunderhead: the base's waypoint to Jawan";

        /// <summary>
        /// Where the hall pads' walks end (fix S2, Central Operations): on the pad's disc 1.5 m south of its centre, over its shallow
        /// south rim (the pad's frame stands on its east side), inside the pad's 2 m trigger
        /// (DynamicObjectManager.LocalTeleporterWorker, IsNear2m). The walks from the far pads start on the pad's disc (0.6 m up).
        /// </summary>
        private static readonly Vector3 HallWingPadFoot = new(-7f, 104.2f, -319.5f), HallFortressPadFoot = new(21.5f, 104.18f, -320.5f);

        /// <summary>
        /// The legs of the set's walks on the client's floor, each walked there and back on its own (corners: a client-floor
        /// flood's way, 2026-11-12, ESTIMATE of a player's line). A walk is a run of legs.
        /// </summary>
        internal static readonly Dictionary<string, Vector3[]> Legs = new()
        {
            // Fix S2: the hall pads in Central Operations, reached down the spiral ramp from the tunnel's end deck (set 19's leg).
            ["the deck down to Central Operations"] = AshenConscriptionEntryTests.Legs["the terminal to Central Operations"],
            ["Central Operations to the east-wing pad"] = new Vector3[]
            {
                new(7f, 103.81f, -340f), new(-4.5f, 103.62f, -321f), new(-7f, 103.62f, -321f), new(-7f, 104.2f, -319.5f)
            },
            ["Central Operations to the fortress pad"] = new Vector3[]
            {
                new(7f, 103.81f, -340f), new(18.5f, 103.62f, -322f), new(21.5f, 103.62f, -322.5f), new(21.5f, 104.18f, -320.5f)
            },
            ["the east wing's pad to the ledge"] = new Vector3[]
            {
                new(288f, 96.62f, -156f), new(288f, 96.02f, -146f), new(286f, 96.02f, -129f), new(281f, 96.02f, -121f), new(277f, 96.01f, -116f),
                new(272f, 96.02f, -109f), new(265f, 96.01f, -104f), new(259f, 96.02f, -102f), new(254f, 96.02f, -99f), new(245f, 96.02f, -98f), new(240f, 96.02f, -98.5f)
            },
            ["the ledge to Lukaz"] = new Vector3[]
            {
                new(240f, 96.02f, -98.5f), new(240f, 96.84f, -86f), new(240f, 97.61f, -70f), new(239f, 97.61f, -56f), new(238.5f, 97.61f, -52.5f)
            },
            ["the ledge to the ramp"] = new Vector3[]
            {
                new(240f, 96.02f, -98.5f), new(229f, 96.01f, -98f), new(218f, 96.02f, -101f), new(208f, 96.01f, -108f), new(200f, 96.01f, -117f),
                new(196f, 96.01f, -123f), new(186.4f, 96.01f, -123f), new(186.4f, 105.11f, -139f)
            },
            ["the ramp down to Humphries"] = new Vector3[]
            {
                new(186.4f, 105.11f, -139f), new(182f, 105.6f, -144f), new(163f, 105.6f, -144f), new(164f, 105.6f, -121f), new(168f, 105.6f, -115f),
                new(168f, 105.58f, -100f), new(166f, 105.59f, -91f), new(167f, 105.2f, -77f), new(174f, 105f, -73f), new(194f, 97.14f, -73f),
                new(198f, 97f, -66f), new(199f, 89.39f, -51f), new(201f, 89.27f, -41f), new(208f, 88.92f, -41f), new(222f, 81.03f, -41f),
                new(235f, 81.22f, -42f), new(238f, 81.2f, -46f), new(239f, 81f, -62f), new(240f, 81.25f, -75f), new(239f, 80.97f, -100f), new(238f, 80.97f, -147f)
            },
            ["the east wing's pad to the Enraged Atta"] = new Vector3[]
            {
                new(288f, 96.62f, -156f), new(288f, 96.02f, -146f), new(298f, 96.5f, -145f), new(312f, 97.61f, -145f), new(340f, 97.61f, -144f),
                new(349f, 97.61f, -134f), new(350f, 97.61f, -108f)
            },
            ["the east wing's pad to the queen"] = new Vector3[]
            {
                new(288f, 96.62f, -156f), new(288f, 96.02f, -146f), new(298f, 96.5f, -145f), new(343f, 97.61f, -146f), new(350f, 97.61f, -157f),
                new(350f, 97.61f, -212f), new(345f, 97.61f, -227f), new(329f, 97.61f, -228f), new(323f, 97.61f, -233f), new(318f, 97.61f, -236f),
                new(318f, 97.61f, -253f), new(320f, 97.61f, -258f), new(319f, 97.61f, -273f), new(309f, 97.61f, -286f), new(298f, 97.61f, -289f),
                new(276f, 97.61f, -291f), new(272f, 97.61f, -300f), new(270f, 97.61f, -324f), new(270f, 97.61f, -332f)
            },
            ["the fortress' pad to Information Extraction"] = new Vector3[]
            {
                new(6.5f, 78.21f, 110f), new(6.5f, 77.61f, 115f), new(6.5f, 76.61f, 119f), new(6.5f, 76.61f, 127f), new(9f, 76.61f, 128f),
                new(9f, 76.97f, 135f), new(7f, 77.5f, 137f), new(6.5f, 78.11f, 145f)
            },
            ["round the platform's hole to the queen"] = new Vector3[]
            {
                new(6.5f, 78.11f, 145f), new(1.5f, 78.11f, 147f), new(1.5f, 78.11f, 155f), new(6.5f, 78.11f, 157f)
            },
            ["the queen to the egg"] = new Vector3[]
            {
                new(6.5f, 78.11f, 157f), new(8.5f, 78.11f, 155.5f)
            },
            // Fix V2: from the queen's talk to the stand 2 m south of the Life Support System's console.
            ["the queen to the console"] = new Vector3[]
            {
                new(6.5f, 78.11f, 157f), new(8.5f, 78.11f, 158.5f)
            },
            [ThunderheadLeg] = new Vector3[]
            {
                new(676.5f, 339f, -523.5f), new(672f, 339f, -527f)
            }
        };

        /// <summary>The walks: label, its legs, the pools it passes (rule breaks), its target.</summary>
        internal static List<(string Label, string[] Legs, uint[] Passes, Vector3 Target)> Walks()
        {
            var none = Array.Empty<uint>();
            return new List<(string, string[], uint[], Vector3)>
            {
                ("the deck to the east-wing pad", new[] { "the deck down to Central Operations", "Central Operations to the east-wing pad" }, none, HallWingPadFoot),
                ("the deck to the fortress pad", new[] { "the deck down to Central Operations", "Central Operations to the fortress pad" }, none, HallFortressPadFoot),
                ("the pad to the arena's ledge (1499)", new[] { "the east wing's pad to the ledge" }, none, new Vector3(240, 96.02f, -98.5f)),
                ("the ledge to Lukaz and the Cell Door Control (1499)", new[] { "the ledge to Lukaz" }, none, new Vector3(238.5f, 97.61f, -52.5f)),
                ("the ledge down to Humphries (1499)", new[] { "the ledge to the ramp", "the ramp down to Humphries" }, RampPools, new Vector3(238, 80.97f, -147)),
                ("the pad to the Enraged Atta (1551)", new[] { "the east wing's pad to the Enraged Atta" }, new[] { N.EnragedAttaPool }, new Vector3(350, 97.61f, -108)),
                ("the pad to the queen (1551)", new[] { "the east wing's pad to the queen" }, new[] { N.QueenPool }, new Vector3(270, 97.61f, -332)),
                ("the pad to the queen's life support and the egg (1501)",
                    new[] { "the fortress' pad to Information Extraction", "round the platform's hole to the queen", "the queen to the egg" }, ExtractionPools.Append(N.XahnPool).ToArray(),
                    new Vector3(8.5f, 78.11f, 155.5f)),
                ("the pad to the Life Support System's console (1501, fix V2)",
                    new[] { "the fortress' pad to Information Extraction", "round the platform's hole to the queen", "the queen to the console" }, ExtractionPools.Append(N.XahnPool).ToArray(),
                    ToVector(V2.ConsoleStand)),
                ("Thunderhead: the base's waypoint to Jawan (1501)", new[] { ThunderheadLeg }, none, V(N.JawanAt))
            };
        }

        private static List<Vector3> Line(IEnumerable<string> legs) =>
            legs.SelectMany((leg, index) => index == 0 ? Legs[leg] : Legs[leg].Skip(1)).ToList();

        /// <summary>The objects with collision in the walks: class, place, yaw (the set's, and set 19's in the HQ hall).</summary>
        private static IEnumerable<(int Class, Vector3 At, float Yaw)> Objects()
        {
            foreach (var pad in N.Pads)
                yield return ((int)N.PadClass, V(AshenFixS2V1.Moved(pad.Id, pad.At)), 0);
            foreach (var pad in AshenConscriptionEntryNpcsV1.Pads)
                yield return ((int)AshenConscriptionEntryNpcsV1.PadClass, V(AshenFixS2V1.Moved(pad.Id, pad.At)), 0);
            yield return ((int)Raid.TerminalClass, ToVector(AshenFixSV1.TerminalAt), (float)Raid.TerminalOrientation);
            yield return ((int)Blast.TeleporterClass, ToVector(AshenFixS2V1.TeleporterAt), 0);
            yield return ((int)Tick551.SampleClass, ToVector(Tick551.SampleAt), 0);
            yield return ((int)Den.SwitchClass, ToVector(Den.SwitchAt), 0);
            yield return ((int)Queen.QueenClass, ToVector(Queen.QueenAt), 0);
            yield return ((int)Queen.DeviceClass, ToVector(Queen.DeviceAt), 0);
            yield return ((int)V2.ConsoleClass, ToVector(V2.ConsoleAt), (float)V2.ConsoleOrientation);
            yield return ((int)Queen.EggClass, ToVector(Queen.EggAt), 0);
            yield return ((int)Queen.QueenClass, ToVector(Queen.EggAt), 0);
        }

        /// <summary>An object's collision mesh put into the map's geometry at its place (as MapGeometry.Load places the map's own).</summary>
        private static void AddObject(MapGeometry geometry, MeshLibrary meshes, int classId, Vector3 at, float yaw)
        {
            var mesh = meshes.GetForClass(classId);
            if (mesh == null || !mesh.HasCollision)
                return;
            var turn = Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw);
            var first = geometry.Vertices.Count / 3;
            foreach (var point in mesh.Vertices.Select(point => Vector3.Transform(point, turn) + at))
            {
                geometry.Vertices.Add(point.X);
                geometry.Vertices.Add(point.Y);
                geometry.Vertices.Add(point.Z);
            }
            for (var k = 0; k + 2 < mesh.Triangles.Count; k += 3)
                for (var corner = 0; corner < 3; corner++)
                    geometry.Triangles.Add(first + mesh.Triangles[k + corner]);
        }

        /// <summary>Each place on the navmesh and a client floor, nothing over its head, seen from its ring, clear of the pools (set 19's checks).</summary>
        private static void CheckPlaces(Rasa.Navigation.NavMeshQuery nav, MapGeometry geometry, List<(string Label, Vector3 At, bool Ring, float Middle, float Clearance)> places,
            List<SpawnPoolEntry> hostile, List<string> failures)
        {
            var near = F.Near(geometry, places.Select(place => place.At).ToList(), 12);
            for (var i = 0; i < places.Count; i++)
            {
                var (label, at, ring, middle, clearance) = places[i];
                if (nav.GroundHeight(at + new Vector3(0, 1, 0)) is not { } ground || MathF.Abs(ground - at.Y) > 1f)
                    failures.Add(FormattableString.Invariant($"{label}: the navmesh ground {nav.GroundHeight(at + new Vector3(0, 1, 0))} at {at}"));
                if (F.FloorAt(near[i], at, at.Y - 2, at.Y + 1.5f) is not { } floor || MathF.Abs(floor - at.Y) > 1)
                {
                    failures.Add(FormattableString.Invariant($"{label}: the client's floor at {at}"));
                    continue;
                }
                if (near[i].Any(t => F.Hit(new Vector3(at.X, floor + 0.3f, at.Z), Vector3.UnitY, t.A, t.B, t.C) is { } up && up < 2.2f))
                    failures.Add($"{label}: a client surface over its head");
                if (ring)
                {
                    var seen = 0;
                    for (var k = 0; k < F.RingPlaces; k++)
                    {
                        var angle = k * 2 * MathF.PI / F.RingPlaces;
                        var place = at + new Vector3(MathF.Sin(angle) * F.Ring, 0, MathF.Cos(angle) * F.Ring);
                        if (F.FloorAt(near[i], place, at.Y - F.MaxOver, at.Y + F.MaxUnder) is { } y && F.Clear(near[i], place with { Y = y + F.Eye }, at + new Vector3(0, middle, 0)))
                            seen++;
                    }
                    if (seen < F.RingPlaces)
                        failures.Add($"{label}: seen from {seen} of {F.RingPlaces} ring places");
                }
                foreach (var pool in hostile)
                    if (Facility.Gap(at, pool) < clearance)
                        failures.Add(FormattableString.Invariant($"{label}: {Facility.Gap(at, pool):0.0} m outside hostile pool {pool.Id}'s circle (its rule {clearance})"));
            }
        }

        /// <summary>The set's markers on <paramref name="map"/> on its navmesh and a client floor within 2 m.</summary>
        private static void CheckMarkers(WildernessRuntimeTestHarness harness, Rasa.Navigation.NavMeshQuery nav, MapGeometry geometry, uint map, int count, List<string> failures)
        {
            var markers = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && Missions.Contains(entry.MissionId)).AsEnumerable()
                .Where(marker => MarkerMap(marker.MissionId, marker.IndicatorId) == map)
                .Select(marker => (Label: $"{marker.MissionId}.{marker.ObjectiveId}/{marker.IndicatorId}", At: new Vector3((float)marker.PosX, (float)marker.PosY, (float)marker.PosZ)))
                .ToList();
            if (markers.Count != count)
                failures.Add($"{markers.Count} markers on {map}, not {count}");
            var (floors, _) = F.Surfaces(geometry, markers.Select(marker => (marker.At + new Vector3(0, 2 - F.MaxUnder, 0), F.Walkable)).ToList());
            for (var i = 0; i < markers.Count; i++)
            {
                if (nav.GroundHeight(markers[i].At + new Vector3(0, 2, 0)) is not { } y || MathF.Abs(y - markers[i].At.Y) > 2f)
                    failures.Add($"{markers[i].Label}: navmesh {nav.GroundHeight(markers[i].At + new Vector3(0, 2, 0))}");
                if (markers[i].At.Y - floors[i] > 2f)
                    failures.Add($"{markers[i].Label}: client floor {floors[i]}");
            }
        }

        /// <summary>The missions offered on the client's arrival and taken by radio.</summary>
        private static void TakeByRadio(WildernessRuntimeTestHarness harness, Client client, params uint[] missions)
        {
            harness.Manager.OfferArrivalMissions(client);
            var offered = MissionTestContext.Drain(client).OfType<DispenseRadioMissionPacket>().Select(packet => packet.MissionId).ToArray();
            foreach (var mission in missions)
            {
                Assert.AreEqual(1, offered.Count(id => id == mission), $"{mission} on arrival");
                Assert.IsTrue(harness.Manager.TryAcceptRadioMission(client, mission), $"{mission} taken by radio");
                MissionTestContext.Drain(client);
            }
        }

        /// <summary>The player walks in to the arena's ledge from the east wing's pad (1499's 1).</summary>
        private static void ToTheArena(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            Walk(harness, Legs["the east wing's pad to the ledge"]);
            Tick(harness, copy, 250);
            Tick(harness, copy, 250);
        }

        /// <summary>
        /// The player walks the corners in moves of 1 m or less (a client's moves), each told to MissionAreaService as an
        /// accepted move: an area is entered only where a move ends inside it.
        /// </summary>
        private static void Walk(WildernessRuntimeTestHarness harness, IReadOnlyList<Vector3> corners)
        {
            var at = corners[0];
            harness.MoveTo(at);
            harness.Drain();
            for (var k = 1; k < corners.Count; k++)
            {
                var steps = Math.Max(1, (int)MathF.Ceiling(Vector3.Distance(corners[k - 1], corners[k])));
                for (var step = 1; step <= steps; step++)
                {
                    var next = Vector3.Lerp(corners[k - 1], corners[k], step / (float)steps);
                    harness.MoveTo(next);
                    harness.Client.MissionAreaService.RecordAcceptedMovement(harness.Client, at, next);
                    at = next;
                }
            }
            harness.Drain();
        }

        /// <summary>1501's 1 and 2: the player walks into Information Extraction and kills Xahn.</summary>
        private static void ToTheQueen(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            Walk(harness, Legs["the fortress' pad to Information Extraction"]);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Queen.MissionId, Queen.FindTheQueen), Objective(harness, Queen.MissionId, Queen.KillTheEnslaver)), "in Information Extraction: 1 done, 2 open");
            KillOne(harness, copy, N.XahnPool, N.Xahn);
            Tick(harness, copy, 250);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Queen.MissionId, Queen.KillTheEnslaver), Objective(harness, Queen.MissionId, Queen.TalkToTheQueen)), "Xahn dead: 2 done, 3 open");
        }

        /// <summary>
        /// None of the pool standing (cleared in the copy): the clock runs, a second at a time, until the pool comes back on its
        /// own (as fix L's KillOne: no reset of its cleared mark, no forced respawn; a pool left to the squad copy's 45 min fails
        /// here), at most its own respawn time and 30 s.
        /// </summary>
        private static void RunTheClock(WildernessRuntimeTestHarness harness, MapChannel copy, uint pool)
        {
            bool Up() => Here(copy).Any(one => one.SpawnPool?.DbId == pool && one.State != CharacterState.Dead);
            var limit = copy.SpawnPools.Where(entry => entry.DbId == pool).Max(entry => entry.RespawnTime) / 1000 + 30;
            var all = copy.SpawnPools.ToArray();
            try
            {
                copy.SpawnPools.Clear();
                copy.SpawnPools.AddRange(all.Where(entry => entry.DbId == pool));
                for (var second = 0; second <= limit && !Up(); second++)
                {
                    harness.UtcNow = harness.UtcNow.AddSeconds(1);
                    SpawnPoolManager.Instance.SpawnPoolWorker(copy, 1000);
                }
            }
            finally
            {
                copy.SpawnPools.Clear();
                copy.SpawnPools.AddRange(all);
            }
            harness.Drain();
            Assert.IsTrue(Up(), $"pool {pool} back on its own within {limit} s");
        }

        /// <summary>One of the pool's kind killed by the player (the mates beside the body); its body (as set 19's KillOne).</summary>
        private static LootDispenser KillOne(WildernessRuntimeTestHarness harness, MapChannel copy, uint pool, uint kind, params Facility.SquadMate[] beside)
        {
            Creature Standing() => Here(copy).FirstOrDefault(one => one.SpawnPool?.DbId == pool && one.DbId == kind && one.Npc == null && one.State != CharacterState.Dead);
            Spawn(copy, pool);
            if (Standing() == null)
                RunTheClock(harness, copy, pool);
            var creature = Standing();
            Assert.IsNotNull(creature, $"a {kind} of pool {pool}");
            harness.MoveTo(creature.Position);
            harness.Drain();
            foreach (var mate in beside)
                mate.Beside(creature.Position + new Vector3(0, 0, 2));
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(copy, creature, harness.Client.Player);
            Assert.IsTrue(copy.LootDispensers.TryGetValue(creature.CorpseLootEntityId, out var loot), "a corpse");
            harness.Drain();
            return loot;
        }

        /// <summary>The mate's own scene object of <paramref name="role"/> (of the mate's run).</summary>
        private static DynamicObject MateObject(WildernessRuntimeTestHarness harness, MapChannel copy, Client mate, uint mission, string role)
        {
            using var unit = harness.CreateChar();
            var runs = unit.CharacterMissions.Runtime.Scenes(mate.Player.Id, mission).Select(scene => scene.RunId).ToHashSet();
            return copy.DynamicObjects.SingleOrDefault(obj => obj.SceneMissionId == mission && obj.SceneActorRole == role &&
                obj.SceneOwnerCharacterId == mate.Player.Id && obj.IsInWorld && runs.Contains(obj.SceneRunId));
        }

        /// <summary>An object's topics for this client: mission and objective (its conversation as the client opens it, set 19's TerminalTopics).</summary>
        private static (uint, uint)[] ObjectTopics(WildernessRuntimeTestHarness harness, Client client, DynamicObject obj)
        {
            client.SetWorldPosition(obj.Position + new Vector3(0, 0, 1), 0);
            MissionTestContext.Drain(client);
            new NpcManager(harness, harness.Manager).RequestNpcConverse(client,
                new RequestNPCConversePacket { EntityId = obj.EntityId, ActionId = ActionId.Converse, ActionArgId = 1 });
            var talk = MissionTestContext.Drain(client).OfType<ConversePacket>().LastOrDefault();
            return talk != null && talk.ConvoDataDict.TryGetValue(ConversationType.ObjectiveComplete, out var topics)
                ? ((List<CompleteableObjectives>)topics).Select(topic => ((uint)topic.MissionId, (uint)topic.ObjectiveId)).ToArray()
                : Array.Empty<(uint, uint)>();
        }

        /// <summary>The object's objective done for this client (set 19's TalkToTerminal).</summary>
        private static void TalkToObject(WildernessRuntimeTestHarness harness, Client client, DynamicObject obj, uint mission, uint objective)
        {
            // The client opens the object's conversation first (the session the completion is checked against).
            ObjectTopics(harness, client, obj);
            new NpcManager(harness, harness.Manager).CompleteNPCObjective(client, new CompleteNPCObjectivePacket
            {
                EntityId = obj.EntityId, MissionId = mission, ObjectiveId = objective, PlayerFlagId = 1
            });
            Assert.AreEqual(1, MissionTestContext.Drain(client).OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == mission && packet.ObjectiveId == objective),
                $"{mission}.{objective} ObjectiveCompleted once");
        }

        /// <summary>The set's pad rows on the copy, as InitTeleporters puts them on a loaded map (set 19's LoadPads), and the links.</summary>
        private static void LoadPads(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            foreach (var row in harness.World.TeleporterEntries.AsNoTracking().Where(row => Pads.Contains(row.Id)).ToList())
            {
                if (copy.Teleporters.TryGetValue(row.Id, out var standing))
                {
                    standing.TriggeredByPlayers.Clear();
                    harness.Objects.Teleporters.TryAdd(row.Id, standing);
                    continue;
                }
                var loaded = new DynamicObject
                {
                    Position = new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ),
                    Rotation = row.Rotation,
                    MapContextId = row.MapContextId,
                    RuntimeMapChannel = copy,
                    EntityClassId = (EntityClasses)row.ClassId,
                    Comment = row.Description,
                    ObjectData = new WaypointInfo(row.Id, false, WaypointType.LocalTeleporter),
                    DynamicObjectType = DynamicObjectType.LocalTeleporter
                };
                copy.Teleporters.TryAdd(row.Id, loaded);
                harness.Objects.Teleporters.TryAdd(row.Id, loaded);
            }
            harness.Objects.InitLocalTeleporters();
            harness.Objects.DynamicObjectWorker(copy, 0);
        }

        /// <summary>Through the pad onto the far one (set 19's Teleport).</summary>
        private static void Teleport(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject pad)
        {
            harness.MoveTo(pad.Position + new Vector3(0, 0, 4));
            harness.Drain();
            harness.Objects.DynamicObjectProximityWorker(map, pad, 0);
            harness.MoveTo(pad.Position);
            harness.Drain();
            harness.Objects.DynamicObjectProximityWorker(map, pad, 0);
            Assert.IsNotNull(harness.Client.PendingTransfer, "taken by the pad");
            System.Threading.Thread.Sleep(DynamicObjectManager.PassageHoldMs + 50);
            harness.Objects.PassagesWorker(map);
            harness.Objects.TeleportAcknowledge(harness.Client);
            harness.Drain();
            Assert.IsNull(harness.Client.PendingTransfer);
        }

        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1, as AshenAvernusHiveTests)
        private const uint ModuleFiller = 122983;          // Armor Module: Regen Bonus [1] (inventory category 3, as AshenLogisticianTests)

        private static Vector3 V((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);
    }
}
