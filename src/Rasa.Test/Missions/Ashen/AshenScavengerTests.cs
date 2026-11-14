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
    using S = AshenScavengerNpcsV1;
    using K = AshenFormalComplaintsNpcsV1;
    using Complaints = AshenFormalComplaintsV1;
    using Human = AshenHumanPerspectiveV1;
    using Robotic = AshenRoboticSalvageV1;
    using Weapon = AshenWeaponSalvageV1;
    using Heavy = AshenHeavySalvageV1;
    using FC = AshenFormalComplaintsTests;
    using F = MarshesClientFloorTests;

    /// <summary>
    /// Ashen Desert set 7, Scavenger Vakiel Latan's side of Formal Complaints, played through as a player plays it: Robotic
    /// Salvage Recovery (1264: 15 salvage off the Boneyard's Nitroglazers and 15 off its Hominis Machina), Weapon Salvage
    /// Recovery (1266: 15 rifles off Thrax Soldiers and 15 grenade launchers off Thrax Ravagers) and Heavy Salvage Recovery
    /// (1265: one salvage off a Desert Stalker and one off a Desert Predator). With them the set's places: the Nitroglazer and
    /// Hominis Machina pools, their spawns and respawns, and the walks to them on the client's floor.
    ///
    /// Why these tests: the side is for good (TaRapedia 1580: Scavenger or Logistician, only one), and the flags stay at an
    /// abandon, so a gate on the flag alone, or on Formal Complaints alone, would give a player both sides; a salvage dropped by
    /// the wrong creature, before the mission, past its count or at the wrong chance, progress lost at a reload, a turn-in that
    /// pays twice or on a full bag, or a pool the client's floor does not reach would each break the chain.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenScavengerTests
    {
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1)
        private const uint WeaponFiller = 3869;            // inventory category 1 (quest-test-pattern)
        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack: fills the consumables bag
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests (inventory category 4)
        private const int Respawns = 6;                    // the rounds of respawns sampled after the first spawn
        private const float FloorReach = 1;                // a creature stands within this of the client's floor
        private const float PoolHeight = 2;                // and within this of its pool's height

        private static readonly Vector3 Ashoka = AshenLeadInsTests.AshokaWaypoint;
        private static readonly uint[] NitroPools = S.NitroglazerPools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] MachinaPools = S.MachinaPools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] RobotPools = NitroPools.Concat(MachinaPools).ToArray();
        private static readonly uint[] BoneyardThrax = AshenDearlyDepartedV1.Pools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] StalkerPools = AshenBoneyardNpcsV1.StalkerPools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] PredatorPools = AshenSheitanHighwayNpcsV1.PredatorPools.Select(pool => pool.Pool).ToArray();

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// The triage's D1 gate. Robotic Salvage Recovery is not offered before Formal Complaints; nor after choice 1 while Formal
        /// Complaints is still open; nor after choice 1 and an abandon of Formal Complaints (flag 250 stays at 1: a gate on the
        /// flag alone fails here), not after a reload either; nor after Formal Complaints is done by choice 2 (flag 250 back to
        /// 0, 251 at 1: a gate on the mission alone fails here), when Ito offers The Human Perspective instead.
        /// </summary>
        [TestMethod]
        public void TheSalvageJobsAreNotOfferedOffLatansSide()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, FC.AshokaHospital);
            var latan = Npc(desert, K.Latan);
            var ito = Npc(desert, K.Ito);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, latan, Robotic.MissionId), "not before Formal Complaints");

            Accept(harness, latan, Complaints.MissionId);
            FC.Choose(harness, latan, Complaints.MissionId, Complaints.Discuss, Complaints.SideWithLatan);
            Assert.AreEqual(1U, FC.Flag(harness, Complaints.LatanFlag), "choice 1: flag 250");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, latan, Robotic.MissionId), "not while Formal Complaints is open");
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Complaints.MissionId));
            harness.Drain();
            Assert.AreEqual(1U, FC.Flag(harness, Complaints.LatanFlag), "the flag stays at an abandon");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, latan, Robotic.MissionId), "choice 1, abandon 1254: 1264 not offered");
            AshenRuntime.Reload(harness);
            harness.Drain();
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, latan, Robotic.MissionId), "nor after a reload");

            Accept(harness, latan, Complaints.MissionId);
            FC.Choose(harness, latan, Complaints.MissionId, Complaints.Discuss, Complaints.HearYamamoto);
            FC.Say(harness, latan, Complaints.MissionId, Complaints.YamamotoSide, Complaints.YamamotoFlag);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, latan, Complaints.MissionId, 3, Complaints.Experience, Complaints.Credits);
            harness.Drain();
            Assert.AreEqual((0U, 1U), (FC.Flag(harness, Complaints.LatanFlag), FC.Flag(harness, Complaints.YamamotoFlag)), "choice 2: 251, 250 back to 0");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, latan, Robotic.MissionId), "choice 2 completed: 1264 not offered");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, ito, Human.MissionId), "Ito's side instead");
            AshenRuntime.Reload(harness);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, latan, Robotic.MissionId), "nor after a reload");
        }

        /// <summary>
        /// Formal Complaints done by choice 1: Latan offers Robotic Salvage Recovery (not Ito; The Human Perspective is not
        /// offered); Weapon and Heavy Salvage Recovery wait for their turn.
        /// </summary>
        [TestMethod]
        public void SidingWithLatanOpensTheSalvageJobs()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, FC.AshokaHospital);
            var latan = Npc(desert, K.Latan);
            var ito = Npc(desert, K.Ito);
            Accept(harness, latan, Complaints.MissionId);
            FC.Choose(harness, latan, Complaints.MissionId, Complaints.Discuss, Complaints.SideWithLatan);
            FC.Say(harness, latan, Complaints.MissionId, Complaints.LatanSide, Complaints.LatanFlag);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, latan, Complaints.MissionId, 0, Complaints.Experience, Complaints.Credits);
            harness.Drain();
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, latan, Robotic.MissionId), "Latan offers Robotic Salvage Recovery");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ito, Robotic.MissionId), "not Ito's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ito, Human.MissionId), "no AFS side");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, latan, Weapon.MissionId), "Weapon Salvage Recovery waits for Robotic");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, latan, Heavy.MissionId), "Heavy Salvage Recovery waits for Weapon");
            AshenRuntime.Reload(harness);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, latan, Robotic.MissionId), "after a reload too");
        }

        /// <summary>
        /// The set's four robot pools: each puts down three of its kind (Nitroglazer 790240 or Hominis Machina 790241), hostile, at
        /// level 40 with the row's hit points, under the client's names. Round each pool's point every navmesh floor is one floor,
        /// joined to the Ashoka Settlement waypoint's ground, within 1 m of a client floor and under open sky; the point is walked
        /// to on the client's floor from the Ashoka Settlement waypoint and back (AshenFixBTests.ClientFloorWalk). At the first
        /// spawn and after each of six rounds of kills and respawns every robot stands on the navmesh, on the point or its rings,
        /// within 1 m of the client's floor and 2 m of its pool's height, walked to from the waypoint and inside its 1264 marker.
        /// </summary>
        [TestMethod]
        public void TheBoneyardRobotsComeDownAndBackOnTheFloorPlayersWalk()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            harness.LoadAbilities(); // the Machina's revive gets it up with its level's data (as WildernessTwinPillarsTests)
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var nav = desert.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", harness.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
            var markers = harness.World.MissionIndicatorEntries.AsEnumerable()
                .Where(row => row.ContentRevision == AshenMissionDataV1.Revision && row.MissionId == Robotic.MissionId).ToList();
            Assert.HasCount(RobotPools.Length, markers, "a marker on each pool");
            Spawn(desert, RobotPools);
            var failures = new List<string>();
            var region = nav.RegionArea(nav.Nearest(Ashoka).Value);
            foreach (var poolId in RobotPools)
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
                    if (F.FloorAt(round[i], at, at.Y - 2, at.Y + 1.5f) is not { } y || y - at.Y > 1 || at.Y - y > 1.5f)
                        failures.Add(FormattableString.Invariant($"pool {poolId}: no client floor under the navmesh at ({at.X:0.0}, {at.Y:0.00}, {at.Z:0.0})"));
                    if (round[i].Any(t => F.Hit(at + new Vector3(0, 1, 0), Vector3.UnitY, t.A, t.B, t.C) is { } up && up < 40))
                        failures.Add(FormattableString.Invariant($"pool {poolId}: a client surface over ({at.X:0.0}, {at.Y:0.00}, {at.Z:0.0})"));
                }
                var point = nav.Nearest(pool.Position) ?? pool.Position;
                if (AshenFixBTests.ClientFloorWalk(nav, geometry, Ashoka, point) is { } there)
                    failures.Add($"pool {poolId} from the Ashoka Settlement waypoint on the client's floor: {there}");
                if (AshenFixBTests.ClientFloorWalk(nav, geometry, point, Ashoka) is { } back)
                    failures.Add($"pool {poolId} back to the Ashoka Settlement waypoint on the client's floor: {back}");
            }

            var levels = new Dictionary<uint, (uint Name, int HitPoints)> { [S.Nitroglazer] = (434, (int)S.HitPoints), [S.Machina] = (7716, (int)S.HitPoints) };
            for (var round = 0; round <= Respawns; round++)
            {
                var here = Alive(desert).Where(candidate => candidate.SpawnPool != null && RobotPools.Contains(candidate.SpawnPool.DbId)).Distinct().ToList();
                foreach (var poolId in RobotPools)
                {
                    var kind = NitroPools.Contains(poolId) ? S.Nitroglazer : S.Machina;
                    Assert.AreEqual((int)S.PerPool, here.Count(one => one.SpawnPool.DbId == poolId && one.DbId == kind), $"round {round}: pool {poolId} has three of {kind}");
                    Assert.AreEqual((int)S.PerPool, here.Count(one => one.SpawnPool.DbId == poolId), $"round {round}: pool {poolId} has nothing else");
                }
                var near = F.Near(geometry, here.Select(one => one.Position).ToList(), 1);
                for (var i = 0; i < here.Count; i++)
                {
                    var one = here[i];
                    var label = FormattableString.Invariant($"round {round}: pool {one.SpawnPool.DbId}'s {one.DbId} at {one.Position}");
                    Assert.AreEqual((TargetCategory.Hostile, S.Level, levels[one.DbId].Name, levels[one.DbId].HitPoints),
                        (one.TargetCategory, one.Level, one.NameId, (int)one.Attributes[Attributes.Health].NormalMax), label);
                    if (!nav.IsOnMesh(one.Position))
                        failures.Add($"{label}: off the navmesh");
                    var path = nav.FindPath(nav.Nearest(Ashoka) ?? Ashoka, one.Position, out var complete);
                    if (path == null || !complete)
                        failures.Add($"{label}: no walk from the Ashoka Settlement waypoint");
                    var floor = F.FloorAt(near[i], one.Position, one.Position.Y - 3, one.Position.Y + 8);
                    if (floor is not { } y || MathF.Abs(y - one.Position.Y) > FloorReach)
                        failures.Add(FormattableString.Invariant($"{label}: the client's floor at {floor}"));
                    if (Vector2.Distance(new(one.Position.X, one.Position.Z), new(one.SpawnPool.Position.X, one.SpawnPool.Position.Z)) > AshenFixAV1.PointSpread + 0.5f)
                        failures.Add($"{label}: off the point and its rings");
                    if (MathF.Abs(one.Position.Y - one.SpawnPool.Position.Y) > PoolHeight)
                        failures.Add(FormattableString.Invariant($"{label}: {one.Position.Y - one.SpawnPool.Position.Y:0.00} m from its pool's height"));
                    if (!markers.Any(marker => Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new(one.SpawnPool.Position.X, one.SpawnPool.Position.Z)) < 0.1f &&
                                               Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new(one.Position.X, one.Position.Z)) <= marker.Radius))
                        failures.Add($"{label}: outside the 1264 markers");
                }
                if (round == Respawns)
                    break;
                foreach (var one in here)
                    KillForGood(harness, desert, one);
                Respawn(desert, RobotPools);
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// The set's four robot pools (the only new pools of sets 7 and 8: set 8 adds none) stand apart (across, 2D) 60 m or more
        /// from every other Ashen Desert pool of a hostile creature, so a fight at one does not pull in another camp, and 85 m or
        /// more from every friendly NPC's pool (Latan and Ito on Ashoka's lower level included). The probe of 2026-11-09 said
        /// "90 m+"; the nearest NPC is the Ashoka market's 500013 at 89.96 m across (21 m below, on the lower level), so 85 m
        /// (ESTIMATE margin) is held here.
        /// </summary>
        [TestMethod]
        public void TheRobotPoolsStandApartFromOtherHostilesAndTheNpcs()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = harness.World.SpawnPoolEntries.AsNoTracking().Where(row => row.MapContextId == AshenMissionDataV1.MapContextId).ToList();
            bool Holds(Rasa.Structures.World.SpawnPoolEntry row, Func<Creature, bool> what) =>
                new[] { row.Creature1Id, row.Creature2Id, row.Creature3Id, row.Creature4Id, row.Creature5Id, row.Creature6Id }
                    .Any(id => id != 0 && CreatureManager.Instance.LoadedCreatures.TryGetValue(id, out var creature) && what(creature));
            var hostile = pools.Where(row => !RobotPools.Contains(row.Id) && Holds(row, creature => creature.TargetCategory == TargetCategory.Hostile)).ToList();
            var npcs = pools.Where(row => Holds(row, creature => creature.TargetCategory != TargetCategory.Hostile && creature.Npc != null)).ToList();
            Assert.IsGreaterThan(50, hostile.Count, "the Ashen hostile pools are read");
            Assert.IsTrue(npcs.Any(row => row.Id == K.Latan) && npcs.Any(row => row.Id == K.Ito), "Latan and Ito among the NPCs");
            var failures = new List<string>();
            foreach (var poolId in RobotPools)
            {
                var pool = pools.Single(row => row.Id == poolId);
                float Across(Rasa.Structures.World.SpawnPoolEntry other) => Vector2.Distance(new((float)pool.PosX, (float)pool.PosZ), new((float)other.PosX, (float)other.PosZ));
                var nearestHostile = hostile.MinBy(Across);
                if (Across(nearestHostile) < 60)
                    failures.Add(FormattableString.Invariant($"pool {poolId}: hostile pool {nearestHostile.Id} {Across(nearestHostile):0.0} m across"));
                var nearestNpc = npcs.MinBy(Across);
                if (Across(nearestNpc) < 85)
                    failures.Add(FormattableString.Invariant($"pool {poolId}: NPC pool {nearestNpc.Id} {Across(nearestNpc):0.0} m across"));
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// Robotic Salvage Recovery: Latan offers it with the four armour pieces; no salvage drops before it. Accepted, Latan hears
        /// no objective; two markers on each objective, on Ashen Desert. A Nitroglazer drops Nitroglazer salvage only and a Hominis
        /// Machina Machina salvage only, under 75 (75 misses, 74 drops); a death is not a pickup; a full bag leaves it on the
        /// corpse, one slot free and it is picked up. Fifteen of each: the fifteenth completes its objective once, a sixteenth
        /// kill drops none; a reload halfway keeps the count; the last makes the mission completeable once. A full bag waits, a
        /// failed save pays nothing and keeps the salvage; Latan takes all thirty and pays the gloves chosen, once; then she
        /// offers Weapon Salvage Recovery.
        /// </summary>
        [TestMethod]
        public void RoboticSalvageComesOffTheBoneyardRobots()
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            harness.LoadAbilities(); // the Machina's revive (as WildernessTwinPillarsTests)
            var desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            var latan = Npc(desert, K.Latan);
            var ito = Npc(desert, K.Ito);
            SideWithLatan(harness, latan);
            Spawn(desert, RobotPools);

            var offer = WildernessRanjaGorgeTests.Offer(harness, latan, Robotic.MissionId);
            CollectionAssert.AreEqual(new[] { Robotic.GravitonGloves, 29113U, Robotic.HazmatLegs, Robotic.ReflectiveLegs }, // fix D: the Motor Assist Legs at level 43
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ito, Robotic.MissionId), "not Ito's to give");
            Assert.IsFalse(Holds(Kill(harness, desert, S.Nitroglazer, NitroPools), Robotic.NitroTemplate), "no salvage before the mission");

            Accept(harness, latan, Robotic.MissionId);
            Assert.IsEmpty(Topics(harness, latan, Robotic.MissionId), "Latan hears no objective: the salvage counts as it is picked up");
            var objectives = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Robotic.MissionId].ObjectivesList;
            foreach (var objective in new[] { Robotic.NitroSalvage, Robotic.MachinaSalvage })
            {
                var markers = objectives.Single(entry => entry.ObjectiveId == objective).IndicatorList;
                Assert.HasCount(2, markers, $"{objective}: a marker on each of its two pools");
                Assert.IsTrue(markers.All(marker => marker.MapContextId == AshenMissionDataV1.MapContextId), "on Ashen Desert");
            }

            percentile = 75;                                  // the rate written out (ESTIMATE 75 %)
            Assert.IsFalse(Holds(Kill(harness, desert, S.Nitroglazer, NitroPools), Robotic.NitroTemplate), "75 misses");
            percentile = 74;
            var nitro = Kill(harness, desert, S.Nitroglazer, NitroPools);
            Assert.IsTrue(Holds(nitro, Robotic.NitroTemplate) && !Holds(nitro, Robotic.MachinaTemplate), "74: a Nitroglazer drops Nitroglazer salvage only");
            Assert.AreEqual(0U, Held(harness, Robotic.NitroTemplate), "a death is not a pickup");
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            PickUp(harness, nitro);
            Assert.AreEqual(0U, Held(harness, Robotic.NitroTemplate), "no room");
            Assert.IsTrue(Holds(nitro, Robotic.NitroTemplate), "still on the corpse");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, nitro);
            var machina = Kill(harness, desert, S.Machina, MachinaPools);
            Assert.IsTrue(Holds(machina, Robotic.MachinaTemplate) && !Holds(machina, Robotic.NitroTemplate), "a Hominis Machina drops Machina salvage only");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, machina);
            var packets = harness.Drain().Cast<object>().ToList();
            Assert.AreEqual((1U, 1U), (Held(harness, Robotic.NitroTemplate), Held(harness, Robotic.MachinaTemplate)), "picked up once there is room");

            for (var count = 2; count <= Robotic.Count; count++)
            {
                PickUp(harness, Kill(harness, desert, S.Nitroglazer, NitroPools));
                PickUp(harness, Kill(harness, desert, S.Machina, MachinaPools));
                packets.AddRange(harness.Drain());
                if (count == 7)
                {
                    AshenRuntime.Reload(harness);
                    harness.Drain();
                    Assert.AreEqual((7U, 7U), (Held(harness, Robotic.NitroTemplate), Held(harness, Robotic.MachinaTemplate)), "kept at a reload");
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Robotic.MissionId, Robotic.NitroSalvage), "seven of fifteen");
                }
            }
            Assert.AreEqual((15U, 15U), (Held(harness, Robotic.NitroTemplate), Held(harness, Robotic.MachinaTemplate)), "fifteen of each");
            foreach (var objective in new[] { Robotic.NitroSalvage, Robotic.MachinaSalvage })
            {
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Robotic.MissionId, objective), $"{objective} done");
                Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Robotic.MissionId && packet.ObjectiveId == objective), $"{objective}: ObjectiveCompleted once");
            }
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Robotic.MissionId && packet.IsCompleteable), "MissionCompleteable once");
            Assert.IsFalse(Holds(Kill(harness, desert, S.Nitroglazer, NitroPools), Robotic.NitroTemplate), "a sixteenth Nitroglazer drops none");
            Assert.IsFalse(Holds(Kill(harness, desert, S.Machina, MachinaPools), Robotic.MachinaTemplate), "a sixteenth Machina drops none");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, ito.EntityId, Robotic.MissionId, 0), "not Ito's to take");
            FullBagWaits(harness, latan, Robotic.MissionId, 0, ArmorFiller);
            FailedTurnInPaysNothing(harness, latan, Robotic.MissionId, 0);
            Assert.AreEqual((15U, 15U), (Held(harness, Robotic.NitroTemplate), Held(harness, Robotic.MachinaTemplate)), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, latan, Robotic.MissionId, 0, Robotic.Experience, Robotic.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Robotic.MissionId), "MissionRewarded once");
            Assert.AreEqual(1U, Held(harness, Robotic.GravitonGloves), "the gloves chosen");
            Assert.AreEqual((0U, 0U), (Held(harness, Robotic.NitroTemplate), Held(harness, Robotic.MachinaTemplate)), "Latan takes all thirty");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, latan, Robotic.MissionId), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, latan, Weapon.MissionId), "Latan offers Weapon Salvage Recovery");
            Assert.IsFalse(Holds(Kill(harness, desert, S.Nitroglazer, NitroPools), 97465), "after the turn-in a Nitroglazer drops none");
            Assert.IsFalse(Holds(Kill(harness, desert, S.Machina, MachinaPools), 97466), "nor a Machina");
        }

        /// <summary>
        /// Weapon Salvage Recovery: a Thrax Soldier drops a rifle only and a Thrax Ravager a grenade launcher only, under 75 (75
        /// misses, 74 drops), a Boneyard Thrax and a Sheitan Highway one alike; markers on the Boneyard's Thrax pools (rifles) and
        /// the highway's Ravager pools (launchers). Fifteen of each make it completeable once; a reload keeps them; a full bag
        /// waits, a failed save pays nothing; Latan takes them and pays the injector gun, once; then she offers Heavy Salvage
        /// Recovery.
        /// </summary>
        [TestMethod]
        public void WeaponSalvageComesOffThraxSoldiersAndRavagers()
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            AshenRuntime.SeedCompleted(harness, Complaints.MissionId, Robotic.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            var latan = Npc(desert, K.Latan);
            var thrax = BoneyardThrax.Concat(Weapon.RavagerPools).ToArray();
            Spawn(desert, thrax);

            var offer = WildernessRanjaGorgeTests.Offer(harness, latan, Weapon.MissionId);
            CollectionAssert.AreEqual(new[] { Weapon.IncendiaryInjector }, offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(Holds(Kill(harness, desert, Weapon.ThraxSoldier, BoneyardThrax), Weapon.RifleTemplate), "no rifle before the mission");
            Accept(harness, latan, Weapon.MissionId);
            Assert.IsEmpty(Topics(harness, latan, Weapon.MissionId), "Latan hears no objective");
            var objectives = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Weapon.MissionId].ObjectivesList;
            Assert.HasCount(3, objectives.Single(entry => entry.ObjectiveId == Weapon.Rifles).IndicatorList, "rifles: the Boneyard's three Thrax pools");
            Assert.HasCount(2, objectives.Single(entry => entry.ObjectiveId == Weapon.Launchers).IndicatorList, "launchers: the highway's two Ravager pools");

            percentile = 75;
            Assert.IsFalse(Holds(Kill(harness, desert, Weapon.ThraxSoldier, BoneyardThrax), Weapon.RifleTemplate), "75 misses");
            percentile = 74;
            var soldier = Kill(harness, desert, Weapon.ThraxSoldier, BoneyardThrax);
            Assert.IsTrue(Holds(soldier, Weapon.RifleTemplate) && !Holds(soldier, Weapon.LauncherTemplate), "74: a Soldier drops a rifle only");
            Assert.AreEqual(0U, Held(harness, Weapon.RifleTemplate), "a death is not a pickup");
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            PickUp(harness, soldier);
            Assert.AreEqual(0U, Held(harness, Weapon.RifleTemplate), "a full bag: no room");
            Assert.IsTrue(Holds(soldier, Weapon.RifleTemplate), "the rifle stays on the corpse");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, soldier);
            Assert.AreEqual(1U, Held(harness, Weapon.RifleTemplate), "picked up once there is room");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            var ravager = Kill(harness, desert, Weapon.ThraxRavager, Weapon.RavagerPools);
            Assert.IsTrue(Holds(ravager, Weapon.LauncherTemplate) && !Holds(ravager, Weapon.RifleTemplate), "a Ravager drops a launcher only");
            PickUp(harness, ravager);
            var packets = harness.Drain().Cast<object>().ToList();
            for (var count = 2; count <= Weapon.Count; count++)
            {
                WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
                WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
                var rifle = Kill(harness, desert, Weapon.ThraxSoldier, BoneyardThrax);
                Assert.IsTrue(Holds(rifle, Weapon.RifleTemplate), $"rifle {count} dropped");
                PickUp(harness, rifle);
                PickUp(harness, Kill(harness, desert, Weapon.ThraxRavager, Weapon.RavagerPools));
                packets.AddRange(harness.Drain());
                Assert.AreEqual(((uint)count, (uint)count), (Held(harness, Weapon.RifleTemplate), Held(harness, Weapon.LauncherTemplate)), $"after {count}");
            }
            Assert.AreEqual((15U, 15U), (Held(harness, Weapon.RifleTemplate), Held(harness, Weapon.LauncherTemplate)), "fifteen of each");
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Weapon.MissionId && packet.IsCompleteable), "MissionCompleteable once");
            Assert.IsFalse(Holds(Kill(harness, desert, Weapon.ThraxSoldier, BoneyardThrax), Weapon.RifleTemplate), "a sixteenth Soldier drops no rifle");
            Assert.IsFalse(Holds(Kill(harness, desert, Weapon.ThraxRavager, Weapon.RavagerPools), Weapon.LauncherTemplate), "a sixteenth Ravager drops no launcher");
            AshenRuntime.Reload(harness);
            harness.Drain();
            Assert.IsTrue(harness.Client.Player.Missions[Weapon.MissionId].Completeable, "kept at a reload");

            FullBagWaits(harness, latan, Weapon.MissionId, 0, WeaponFiller);
            FailedTurnInPaysNothing(harness, latan, Weapon.MissionId, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, latan, Weapon.MissionId, 0, Weapon.Experience, Weapon.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Weapon.MissionId), "MissionRewarded once");
            Assert.AreEqual(1U, Held(harness, Weapon.IncendiaryInjector), "the injector gun");
            Assert.AreEqual((0U, 0U), (Held(harness, Weapon.RifleTemplate), Held(harness, Weapon.LauncherTemplate)), "Latan takes them");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, latan, Heavy.MissionId), "Latan offers Heavy Salvage Recovery");
        }

        /// <summary>
        /// Heavy Salvage Recovery: a Desert Stalker drops the Stalker salvage and a Desert Predator the Predator salvage, every
        /// kill (100%), one each, only while the mission is open; the markers on the Stalker pools and the Predator beats. An
        /// abandon keeps the salvage; taken again, it counts at once. A full bag waits, a failed save pays nothing; Latan takes
        /// both and pays the stack chosen, once; the chain ends (nothing more offered).
        /// </summary>
        [TestMethod]
        public void HeavySalvageComesOffAStalkerAndAPredator()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 99 : minimum);
            AshenRuntime.SeedCompleted(harness, Complaints.MissionId, Robotic.MissionId, Weapon.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            var latan = Npc(desert, K.Latan);
            Spawn(desert, StalkerPools.Concat(PredatorPools).ToArray());

            Assert.IsFalse(Holds(Kill(harness, desert, Heavy.Stalker, StalkerPools), Heavy.StalkerTemplate), "none before the mission");
            Accept(harness, latan, Heavy.MissionId);
            var objectives = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Heavy.MissionId].ObjectivesList;
            Assert.HasCount(2, objectives.Single(entry => entry.ObjectiveId == Heavy.StalkerSalvage).IndicatorList, "the two Stalker pools");
            Assert.HasCount(2, objectives.Single(entry => entry.ObjectiveId == Heavy.PredatorSalvage).IndicatorList, "the two Predator beats");

            var stalker = Kill(harness, desert, Heavy.Stalker, StalkerPools);
            Assert.IsTrue(Holds(stalker, Heavy.StalkerTemplate) && !Holds(stalker, Heavy.PredatorTemplate), "99: a Stalker drops its salvage (100%)");
            PickUp(harness, stalker);
            harness.Drain();
            Assert.AreEqual(1U, Held(harness, Heavy.StalkerTemplate));
            Assert.IsFalse(Holds(Kill(harness, desert, Heavy.Stalker, StalkerPools), Heavy.StalkerTemplate), "one: the next Stalker has none");

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Heavy.MissionId));
            harness.Drain();
            Assert.AreEqual(1U, Held(harness, Heavy.StalkerTemplate), "kept at an abandon");
            Accept(harness, latan, Heavy.MissionId);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Heavy.MissionId, Heavy.StalkerSalvage), "the salvage held counts");

            var predator = Kill(harness, desert, Heavy.Predator, PredatorPools);
            Assert.IsTrue(Holds(predator, Heavy.PredatorTemplate) && !Holds(predator, Heavy.StalkerTemplate), "a Predator drops its salvage");
            PickUp(harness, predator);
            var packets = harness.Drain();
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Heavy.MissionId && packet.IsCompleteable), "MissionCompleteable once");

            FullBagWaits(harness, latan, Heavy.MissionId, 3, ConsumableFiller);
            FailedTurnInPaysNothing(harness, latan, Heavy.MissionId, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, latan, Heavy.MissionId, 3, Heavy.Experience, Heavy.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Heavy.MissionId), "MissionRewarded once");
            Assert.AreEqual(6U, Held(harness, 45063), "the six Basic Med Packs chosen, no more");
            Assert.AreEqual((0U, 0U), (Held(harness, Heavy.StalkerTemplate), Held(harness, Heavy.PredatorTemplate)), "Latan takes both");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, latan, Heavy.MissionId), "done once");
        }

        private static uint Held(WildernessRuntimeTestHarness harness, uint template) => WildernessRanjaGorgeTests.Held(harness, template);

        /// <summary>
        /// Formal Complaints played to its end on Latan's side (choice 1: flag 250). Played, not seeded: the flag is the choice's
        /// own write (AshenRuntime.Reload reads it back from the character database).
        /// </summary>
        private static void SideWithLatan(WildernessRuntimeTestHarness harness, Creature latan)
        {
            Accept(harness, latan, Complaints.MissionId);
            FC.Choose(harness, latan, Complaints.MissionId, Complaints.Discuss, Complaints.SideWithLatan);
            FC.Say(harness, latan, Complaints.MissionId, Complaints.LatanSide, Complaints.LatanFlag);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, latan, Complaints.MissionId, 0, Complaints.Experience, Complaints.Credits);
            harness.Drain();
            Assert.AreEqual(1U, FC.Flag(harness, Complaints.LatanFlag), "flag 250");
        }

        /// <summary>A kill of a living <paramref name="kind"/> of these pools (respawned when none is left); its corpse.</summary>
        internal static LootDispenser Kill(WildernessRuntimeTestHarness harness, MapChannel desert, uint kind, uint[] pools)
        {
            bool Living(Creature candidate) => candidate.DbId == kind && candidate.Npc == null && candidate.SpawnPool != null && pools.Contains(candidate.SpawnPool.DbId);
            if (!Alive(desert).Any(Living))
                Respawn(desert, pools);
            var one = Alive(desert).First(Living);
            harness.MoveTo(one.Position);
            KillForGood(harness, desert, one);
            Assert.IsTrue(desert.LootDispensers.TryGetValue(one.CorpseLootEntityId, out var loot), "a corpse");
            return loot;
        }

        /// <summary>
        /// A kill that is a death. A Hominis Machina's first death in a life is not (its self revive, upstream's machina_revive
        /// 62059: CreatureSupport.DefersDeath): it goes down with no corpse, gets up at the recovery's end, and the second kill
        /// is its death.
        /// </summary>
        internal static void KillForGood(WildernessRuntimeTestHarness harness, MapChannel desert, Creature one)
        {
            one.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(desert, one, harness.Client.Player);
            if (one.DbId != S.Machina)
                return;
            Assert.IsFalse(desert.LootDispensers.ContainsKey(one.CorpseLootEntityId), "a Machina's first death: down, no corpse");
            CreatureSupport.Worker(desert, Environment.TickCount64 + 60_000);
            Assert.AreNotEqual(CharacterState.Dead, one.State, "the Machina gets up again");
            one.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(desert, one, harness.Client.Player);
        }

        /// <summary>The navmesh floors on a 1 m grid within AshenFixAV1.ClearRadius of <paramref name="centre"/> (as AshenFormalComplaintsTests).</summary>
        private static List<List<Vector3>> Columns(MapChannel desert, Vector3 centre)
        {
            var columns = new List<List<Vector3>>();
            var reach = (int)Math.Ceiling(AshenFixAV1.ClearRadius);
            for (var dz = -reach; dz <= reach; dz++)
                for (var dx = -reach; dx <= reach; dx++)
                {
                    if (dx * dx + dz * dz > AshenFixAV1.ClearRadius * AshenFixAV1.ClearRadius)
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
