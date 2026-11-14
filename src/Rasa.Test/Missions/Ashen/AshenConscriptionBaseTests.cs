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
    using C = AshenConscriptionBaseNpcsV1;
    using Above = AshenDeathFromAboveV1;
    using Lights = AshenTurningOutTheLightsV1;
    using Link = AshenStrongestLinkV1;
    using P = MarshesPreoDasTests;
    using E = MarshesEscortsTests;
    using F = MarshesClientFloorTests;

    /// <summary>
    /// Ashen Desert set 5, the Bane Conscription Base, played through as a player plays it: Captain Akio Yamamoto's Death from
    /// Above (1352: the four trench mortars, then six missile launchers blown with the explosives he gives), Lieutenant Gabriel
    /// Infanger's Turning Out the Lights (1354: three power generators blown with his explosives) and Lieutenant Jan Blood's The
    /// Strongest Link (1355: fifteen Linkers and Overseer Naxveik). With them the set's places: the trench mortars, the Linker
    /// pools and Naxveik, their first spawns and their respawns, and the launchers, generators and their Detonation Devices.
    ///
    /// Why these tests: the three need Fallen Soldiers and Xanxless Job both; 1352 counts four mortars each on its own row
    /// and their count on 11, and six personal launchers; 1354 three personal generators; the explosives of both come at the
    /// accept (TaRapedia's known issue of 2007-08-21: they did not); 1355 counts a World DB row fifteen times and a moved
    /// upstream boss once. A creature no player reaches, a device that takes no explosive or two, a launcher a gun can destroy,
    /// a marker that misses its target, an offer before both prerequisites, a kill or a use that counts twice or not at all,
    /// progress lost at a reload or kept after an abandon, or a turn-in that pays twice or on a full bag would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenConscriptionBaseTests
    {
        private const uint WeaponFiller = 3869;            // inventory category 1 (quest-test-pattern)
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1)
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests
        private const uint ThraxSoldier = 531008;          // Thrax Soldier - Ashen Desert (World DB)
        private const uint OtherMortar = 592195;           // the Ashen Bane Mortar pool by the Conscription Base's west camp (World DB, BaneMortars)
        private const uint HighwayMortar = 790081;         // set 3's highway Mortar 1 (1249)
        private const uint ClientDeviceClass = 7334;       // UsableItemAccHumBombV01 "Detonation Device"
        private const uint ClientDeviceUseArgument = 1;    // usabledata lookup[7334][0]
        private const uint ClientLauncherClass = 21873;    // UsableInertDestBaneMissileLauncherV01
        private const uint ClientGeneratorClass = 20594;   // UsableInertDestBaneGeneratorSmallV01
        private const uint ClientGeneratorName = 245;      // usablenameoverride POWER_GENERATOR "Power Generator"
        private const uint ClientPlasticTemplate = 97491;  // client item class 25490 "High Grade Plastic Explosives"
        private const uint ClientChargeTemplate = 111184;  // client item class 26441 "Electromagnetic Charged Explosive"
        private const uint DeviceReady = 119, DevicePlanted = 121, Intact = 110, Destroyed = 2;
        private const int Respawns = 6;                    // the rounds of respawns sampled after the first spawn
        private const float FloorReach = 1;                // a creature stands within this of the client's floor
        private const float PoolHeight = 2;                // and within this of its pool's height
        private const float ColumnReach = 6;               // the navmesh round a pool's point has one floor this far out
        private const float DeviceClear = 3;               // a Detonation Device stands this far outside every model
        // The client's model bounds (MeshLibrary, entity_meshes.csv, 2026-11-09): the launcher 21873 x -6.39..15.86, z -7.37..7.37
        // (facing 0); the small generator 20594 x -1.28..1.27, z -1.27..0.98, 1.80 m from its point at its farthest corner.
        private const float LauncherBack = 6.39f, LauncherFront = 15.86f, LauncherHalfWidth = 7.47f, GeneratorReach = 1.80f;

        private static readonly Vector3 Ashoka = AshenLeadInsTests.AshokaWaypoint;
        private static readonly uint[] TrenchPools = C.Mortars.Select(mortar => mortar.Pool).ToArray();
        private static readonly uint[] LinkerPools = C.LinkerPools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] AllPools = TrenchPools.Concat(LinkerPools).Append(C.Naxveik).ToArray();

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// Each trench pool puts down its own mortar copy, each Linker pool three living Linkers (531014), Naxveik's pool one
        /// Overseer Naxveik (name 9353), all hostile; the navmesh round each Linker's and Naxveik's pool has one floor (a mortar
        /// never moves: 592202's pad stands over a lower floor, 3.8 m down); at the first spawn and
        /// after each of six rounds of kills and respawns every one of them stands on the navmesh, within 1 m of the client's
        /// floor, within 2 m of its pool's height, walked to from the Ashoka Settlement waypoint and inside a marker of its
        /// mission (1352 for the mortars, 1355 for the Linkers and Naxveik).
        /// </summary>
        [TestMethod]
        public void TheConscriptionBaseCreaturesComeDownAndBackOnTheFloorPlayersWalk()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var nav = desert.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", MapName(harness)), meshes, 1);
            var markers = harness.World.MissionIndicatorEntries.AsEnumerable().Where(row => row.ContentRevision == AshenMissionDataV1.Revision).ToList();
            Spawn(desert, AllPools);
            var failures = new List<string>();
            foreach (var poolId in AllPools)
            {
                var pool = desert.SpawnPools.Single(loaded => loaded.DbId == poolId);
                Assert.IsTrue(SpawnPoolManager.HasWalkableGround(desert, pool), $"{poolId} has walkable ground");
                if (TrenchPools.Contains(poolId))
                    continue;           // a mortar is an emplacement (EntityMovementRates 7482: 0, 0): it stands on its pad, checked each round
                var stacked = Columns(desert, pool.Position).Where(column => column.Count != 1).ToList();
                if (stacked.Count > 0)
                    failures.Add($"pool {poolId}: more than one navmesh floor at " + string.Join("; ", stacked.Take(3).Select(column => string.Join(" / ", column))));
            }

            for (var round = 0; round <= Respawns; round++)
            {
                var here = Alive(desert).Where(candidate => candidate.SpawnPool != null && AllPools.Contains(candidate.SpawnPool.DbId)).Distinct().ToList();
                foreach (var (creature, poolId, _) in C.Mortars)
                    Assert.AreEqual(creature, here.Single(one => one.SpawnPool.DbId == poolId).DbId, $"round {round}: trench pool {poolId}'s own mortar");
                foreach (var poolId in LinkerPools)
                    Assert.AreEqual((int)C.LinkersPerPool, here.Count(one => one.SpawnPool.DbId == poolId && one.DbId == C.Linker), $"round {round}: pool {poolId} has three Linkers");
                var naxveik = here.Single(one => one.SpawnPool.DbId == C.Naxveik);
                Assert.AreEqual((C.Naxveik, 9353U), (naxveik.DbId, naxveik.NameId), $"round {round}: Overseer Naxveik");
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
                    if (MathF.Abs(one.Position.Y - one.SpawnPool.Position.Y) > PoolHeight)
                        failures.Add(FormattableString.Invariant($"{label}: {one.Position.Y - one.SpawnPool.Position.Y:0.00} m from its pool's height"));
                    var mission = TrenchPools.Contains(one.SpawnPool.DbId) ? Above.MissionId : Link.MissionId;
                    if (!markers.Any(marker => marker.MissionId == mission &&
                            Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new(one.Position.X, one.Position.Z)) <= marker.Radius))
                        failures.Add($"{label}: outside the {mission} markers");
                }
                if (round == Respawns)
                    break;
                foreach (var one in here)
                {
                    one.Attributes[Attributes.Health].Current = 0;
                    harness.Creatures.HandleCreatureKill(desert, one, harness.Client.Player);
                }
                Respawn(desert, AllPools);
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// The launchers and generators of a player's own: each launcher on flat navmesh ground over all of its model's ground
        /// (22 by 15 m; heights within 1 m of its point), facing either way along its row (the client's facing convention of a
        /// scene object is not verified), and no Linker pool's point within 6 m of either; each Detonation Device 3 m and more
        /// outside every launcher's model either way and its generator's model turned any way (60, 120, 180 degrees as built),
        /// on the ground, walked to from the Ashoka Settlement waypoint and inside its objective's marker.
        /// </summary>
        [TestMethod]
        public void TheLaunchersAndGeneratorsStandOnTheGroundTheirDevicesAreWalkedTo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompleted(harness, AshenXanxlessJobV1.MissionId, AshenFallenSoldiersV1.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            var nav = desert.NavMesh;
            Accept(harness, Npc(desert, AshenLeadInNpcsV1.Yamamoto), Above.MissionId);
            Accept(harness, Npc(desert, AshenBoneyardNpcsV1.Infanger), Lights.MissionId);
            var markers = harness.World.MissionIndicatorEntries.AsEnumerable().Where(row => row.ContentRevision == AshenMissionDataV1.Revision).ToList();
            bool Walked(Vector3 to) => nav.FindPath(nav.Nearest(Ashoka) ?? Ashoka, to, out var complete) != null && complete;
            (Vector2 Min, Vector2 Max)[] Models(Vector3 at) => new (Vector2, Vector2)[]
            {
                (new(at.X - LauncherBack, at.Z - LauncherHalfWidth), new(at.X + LauncherFront, at.Z + LauncherHalfWidth)),
                (new(at.X - LauncherFront, at.Z - LauncherHalfWidth), new(at.X + LauncherBack, at.Z + LauncherHalfWidth))
            };
            float Outside((Vector2 Min, Vector2 Max) box, Vector3 at) =>
                new Vector2(MathF.Max(0, MathF.Max(box.Min.X - at.X, at.X - box.Max.X)), MathF.Max(0, MathF.Max(box.Min.Y - at.Z, at.Z - box.Max.Y))).Length();

            var (launchers, devices) = Launchers(harness, desert);
            var models = launchers.Select(launcher => Models(launcher.Position)).ToList();
            for (var number = 1; number <= Above.Launchers; number++)
            {
                var launcher = launchers[number - 1];
                foreach (var (min, max) in models[number - 1])
                {
                    for (var x = min.X; x <= max.X; x += 2)
                        for (var z = min.Y; z <= max.Y; z += 2)
                        {
                            var ground = nav.GroundHeight(new Vector3(x, launcher.Position.Y + 1, z));
                            Assert.IsTrue(ground is { } y && MathF.Abs(y - launcher.Position.Y) <= 1, FormattableString.Invariant($"launcher {number}'s model over flat ground at ({x}, {z}): {ground}"));
                        }
                    foreach (var (_, at) in C.LinkerPools)
                        Assert.IsGreaterThanOrEqualTo(6f, Outside((min, max), new Vector3((float)at.X, (float)at.Y, (float)at.Z)), $"launcher {number}: no Linker pool by its model");
                }
                for (var other = 0; other < Above.Launchers; other++)
                    foreach (var model in models[other])
                        Assert.IsGreaterThanOrEqualTo(DeviceClear, Outside(model, devices[number - 1].Position), $"device {number} outside launcher {other + 1}'s model, either facing");
                Assert.IsTrue(Walked(devices[number - 1].Position), $"device {number} walked to");
                var marker = markers.Single(row => row.MissionId == Above.MissionId && row.ObjectiveId == Above.DestroyLauncher(number));
                Assert.IsLessThanOrEqualTo((float)marker.Radius, Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new(devices[number - 1].Position.X, devices[number - 1].Position.Z)),
                    $"device {number} inside its marker");
            }

            var (generators, generatorDevices) = Generators(harness, desert);
            for (var number = 1; number <= Lights.Generators; number++)
            {
                Assert.IsTrue(Walked(generatorDevices[number - 1].Position), $"generator {number}'s device walked to");
                for (var other = 0; other < Lights.Generators; other++)
                    Assert.IsGreaterThanOrEqualTo(GeneratorReach + DeviceClear,
                        Vector2.Distance(new(generators[other].Position.X, generators[other].Position.Z), new(generatorDevices[number - 1].Position.X, generatorDevices[number - 1].Position.Z)),
                        $"generator {number}'s device 3 m outside generator {other + 1}'s model, turned any way");
                var marker = markers.Single(row => row.MissionId == Lights.MissionId && row.ObjectiveId == Lights.Destroy(number));
                foreach (var obj in new[] { generators[number - 1], generatorDevices[number - 1] })
                    Assert.IsLessThanOrEqualTo((float)marker.Radius, Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new(obj.Position.X, obj.Position.Z)),
                        $"{obj.SceneActorRole} inside its marker");
            }
        }

        /// <summary>
        /// Death from Above: not offered before Fallen Soldiers and Xanxless Job are both done; then Yamamoto offers it (not Blood,
        /// not Infanger); a bag with no room refuses the accept (no explosive, no launcher), with one slot free it gives the six
        /// explosives at the accept; a marker on each of the eleven objectives, on Ashen Desert;
        /// Yamamoto hears no objective. Another Ashen mortar and set 3's highway mortar count on nothing; each trench mortar
        /// completes its own objective once and counts on 11 once (the first killed again after its respawn does not count
        /// again), kept at a reload; the launchers cannot be used or hit; each device
        /// takes one explosive, plants it and blows its launcher, completing its own objective once; a used device takes no
        /// second one; a reload keeps the blown launchers; the last makes the mission completeable once, no explosive is left; a
        /// full bag waits, a failed save pays nothing; Yamamoto pays the weapon chosen, once; the launchers go with the mission.
        /// </summary>
        [TestMethod]
        public void DeathFromAboveDestroysTheMortarsAndTheLaunchers()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            var yamamoto = Npc(desert, AshenLeadInNpcsV1.Yamamoto);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, yamamoto, Above.MissionId), "it waits for Fallen Soldiers and Xanxless Job");
            SeedCompleted(harness, AshenXanxlessJobV1.MissionId);
            desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            yamamoto = Npc(desert, AshenLeadInNpcsV1.Yamamoto);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, yamamoto, Above.MissionId), "Xanxless Job alone is not enough");
            SeedCompleted(harness, AshenFallenSoldiersV1.MissionId);
            desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            yamamoto = Npc(desert, AshenLeadInNpcsV1.Yamamoto);
            var blood = Npc(desert, AshenBoneyardNpcsV1.Blood);
            var infanger = Npc(desert, AshenBoneyardNpcsV1.Infanger);
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Above.MissionId && obj.IsInWorld), "no launcher before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, yamamoto, Above.MissionId);
            CollectionAssert.AreEqual(new[] { Above.IncendiaryBlade, Above.PulseCannon, Above.PhotonicShotgun, Above.CryogenicInjector },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, blood, Above.MissionId), "not Blood's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, infanger, Above.MissionId), "not Infanger's to give");
            // No room for the explosives: no mission. One slot freed (six in a stack of ten), they come with it.
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.MoveTo(yamamoto.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, yamamoto.EntityId, Above.MissionId), "no room for the explosives");
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(Above.MissionId));
            harness.Manager.Scenes.Tick(desert);
            Assert.AreEqual(0U, Plastic(harness), "none given");
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Above.MissionId && obj.IsInWorld), "no launcher");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Accept(harness, yamamoto, Above.MissionId);
            Assert.AreEqual(Above.Launchers, Plastic(harness), "six High Grade Plastic Explosives at the accept");
            Assert.IsEmpty(Topics(harness, yamamoto, Above.MissionId), "Yamamoto hears no objective");
            var snapshot = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Above.MissionId].ObjectivesList;
            Assert.HasCount(11, snapshot, "the client's eleven objectives");
            foreach (var objective in snapshot)
                Assert.IsTrue(objective.IndicatorList.Single().MapContextId == AshenMissionDataV1.MapContextId, $"objective {objective.ObjectiveId}: a marker on Ashen Desert");

            Spawn(desert, TrenchPools.Append(OtherMortar).Concat(AshenSheitanHighwayNpcsV1.Mortars.Select(mortar => mortar.Pool)).ToArray());
            KillOne(harness, desert, C.Mortar, spawnFrom: new[] { OtherMortar });
            KillOne(harness, desert, HighwayMortar, spawnFrom: new[] { AshenSheitanHighwayNpcsV1.Mortars[0].Pool });
            harness.Drain();
            Assert.AreEqual((0, 0U), (MortarsDone(harness), MortarCount(harness)), "another Ashen mortar and a highway mortar count on nothing");

            var log = new List<object>();
            var destroyed = 0;
            foreach (var number in new[] { 2, 4, 1, 3 })
            {
                KillOne(harness, desert, C.Mortars[number - 1].Creature, spawnFrom: new[] { C.Mortars[number - 1].Pool });
                var packets = harness.Drain().Cast<object>().ToList();
                log.AddRange(packets);
                destroyed++;
                Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Above.MissionId && packet.ObjectiveId == Above.DestroyMortar(number)),
                    $"mortar {number}: its objective, once");
                Assert.AreEqual((destroyed, (uint)destroyed), (MortarsDone(harness), MortarCount(harness)), $"mortar {number}: only its own objective, and 11 counts it");
                if (destroyed == 1)
                {
                    KillOne(harness, desert, C.Mortars[number - 1].Creature, spawnFrom: new[] { C.Mortars[number - 1].Pool });
                    log.AddRange(harness.Drain());
                    Assert.AreEqual((1, 1U), (MortarsDone(harness), MortarCount(harness)), "the same mortar again (respawned) counts once on 11");
                }
                if (destroyed == 2)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual((2, 2U), (MortarsDone(harness), MortarCount(harness)), "kept at a reload");
                    harness.Drain();
                }
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Above.MissionId, Above.Counter), "11: four mortars destroyed");
            Assert.AreEqual(1, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Above.MissionId && packet.ObjectiveId == Above.Counter), "11 once");
            Assert.IsFalse(harness.Client.Player.Missions[Above.MissionId].Completeable, "the launchers are left");

            var (launchers, devices) = Launchers(harness, desert);
            foreach (var launcher in launchers)
            {
                Assert.IsFalse(launcher.IsEnabled, $"{launcher.SceneActorRole}: no use");
                Assert.IsFalse(PracticeTargetManager.TryGetTarget(desert, launcher.EntityId, out _), $"{launcher.SceneActorRole}: no gun destroys it");
                Assert.IsFalse(WorldDestructibles.TryGetTarget(desert, launcher.EntityId, out _), $"{launcher.SceneActorRole}: no world destructible");
            }
            var blown = 0U;
            foreach (var number in new[] { 5, 2, 6, 1, 4, 3 })
            {
                var device = devices[number - 1];
                Plant(harness, desert, device);
                var packets = harness.Drain().Cast<object>().Concat(E.Tick(harness, desert, 250)).ToList();
                log.AddRange(packets);
                blown++;
                Assert.AreEqual(Above.Launchers - blown, Plastic(harness), $"launcher {number} takes one explosive");
                Assert.AreEqual(((UseObjectState)DevicePlanted, false), (device.StateId, device.IsEnabled), $"device {number} planted and closed");
                Assert.AreEqual((UseObjectState)Destroyed, launchers[number - 1].StateId, $"launcher {number} blown up");
                Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Above.MissionId && packet.ObjectiveId == Above.DestroyLauncher(number)),
                    $"launcher {number}: its objective, once");
                Assert.AreEqual((int)blown, LaunchersDone(harness), $"launcher {number}: only its own objective");
                if (number == 5)
                {
                    TryPlant(harness, desert, device);
                    Assert.AreEqual(Above.Launchers - 1, Plastic(harness), "a used device takes no second explosive");
                }
                if (blown == 3)
                {
                    P.ReloadAndRecover(harness, desert);
                    Assert.AreEqual((3, Above.Launchers - 3), (LaunchersDone(harness), Plastic(harness)), "kept at a reload");
                    (launchers, devices) = Launchers(harness, desert, blown: new[] { 5, 2, 6 });
                }
                if (blown < Above.Launchers)
                    Assert.IsFalse(harness.Client.Player.Missions[Above.MissionId].Completeable, $"{blown} of 6 launchers: not completeable");
            }
            Assert.AreEqual(0U, Plastic(harness), "no explosive left");
            Assert.IsTrue(harness.Client.Player.Missions[Above.MissionId].Completeable, "four mortars and six launchers");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Above.MissionId && packet.IsCompleteable), "MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, blood.EntityId, Above.MissionId, 1), "not Blood's to take");
            FullBagWaits(harness, yamamoto, Above.MissionId, 1, WeaponFiller);
            FailedTurnInPaysNothing(harness, yamamoto, Above.MissionId, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, yamamoto, Above.MissionId, 1, Above.Experience, Above.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Above.MissionId), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Above.PulseCannon), "the rocket launcher chosen");
            for (var tick = 0; tick < 4; tick++)        // RetireRewardedScene retries: up to 1 s
                E.Tick(harness, desert, 250);
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Above.MissionId && obj.IsInWorld), "the launchers go with the mission");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, yamamoto, Above.MissionId), "done once");
        }

        /// <summary>
        /// An abandon of Death from Above after a mortar and a launcher takes the explosives left and the launchers; taken again,
        /// six new explosives (not eleven), six whole launchers and ready devices, nothing done.
        /// </summary>
        [TestMethod]
        public void DeathFromAboveAbandonedTakesTheExplosivesAndTheLaunchers()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompleted(harness, AshenXanxlessJobV1.MissionId, AshenFallenSoldiersV1.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            var yamamoto = Npc(desert, AshenLeadInNpcsV1.Yamamoto);
            Accept(harness, yamamoto, Above.MissionId);
            KillOne(harness, desert, C.Mortars[0].Creature, spawnFrom: new[] { C.Mortars[0].Pool });
            var (_, devices) = Launchers(harness, desert);
            Plant(harness, desert, devices[0]);
            E.Tick(harness, desert, 250);
            harness.Drain();
            Assert.AreEqual((1, 1, Above.Launchers - 1), (MortarsDone(harness), LaunchersDone(harness), Plastic(harness)));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Above.MissionId));
            harness.Manager.Scenes.Tick(desert);
            harness.Drain();
            Assert.AreEqual(0U, Plastic(harness), "the explosives go with the mission");
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Above.MissionId && obj.IsInWorld), "the launchers go with the abandon");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, yamamoto, Above.MissionId), "offered again after an abandon");
            Accept(harness, yamamoto, Above.MissionId);
            Assert.AreEqual(Above.Launchers, Plastic(harness), "six again");
            Launchers(harness, desert);
            Assert.AreEqual((0, 0, 0U), (MortarsDone(harness), LaunchersDone(harness), MortarCount(harness)), "all open again");
        }

        /// <summary>
        /// Turning Out the Lights: not offered before Fallen Soldiers and Xanxless Job; then Infanger offers it (not Yamamoto, not
        /// Blood) and gives the three explosives at the accept; a marker on each generator, on Ashen Desert; Infanger hears no
        /// objective. The generators (the client's class, named "Power Generator") cannot be used or hit; each device takes one
        /// explosive and blows its generator, completing its own objective once (any order: 3, 1, 2); a used device takes no
        /// second one; a reload keeps the blown generator; the third makes the mission completeable once, no explosive is left;
        /// a full bag waits, a failed save pays nothing; Infanger pays the item chosen, once. A bag with no room refuses the
        /// accept (no explosive, no generator); with one slot free it is taken. The explosives cannot be destroyed (mission
        /// items), so no device is left without one. An abandon after one takes the explosives and the generators; taken again, three
        /// explosives and three whole generators.
        /// </summary>
        [TestMethod]
        public void TurningOutTheLightsBlowsTheThreeGenerators()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            var infanger = Npc(desert, AshenBoneyardNpcsV1.Infanger);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, infanger, Lights.MissionId), "it waits for Fallen Soldiers and Xanxless Job");
            SeedCompleted(harness, AshenFallenSoldiersV1.MissionId);
            desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            infanger = Npc(desert, AshenBoneyardNpcsV1.Infanger);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, infanger, Lights.MissionId), "Fallen Soldiers alone is not enough");
            SeedCompleted(harness, AshenXanxlessJobV1.MissionId);
            desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            infanger = Npc(desert, AshenBoneyardNpcsV1.Infanger);
            var yamamoto = Npc(desert, AshenLeadInNpcsV1.Yamamoto);
            var blood = Npc(desert, AshenBoneyardNpcsV1.Blood);

            var offer = WildernessRanjaGorgeTests.Offer(harness, infanger, Lights.MissionId);
            CollectionAssert.AreEqual(new[] { Lights.BioLegs, Lights.StealthHelmet, Lights.HazmatVest, Lights.MotorAssistLegs },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, yamamoto, Lights.MissionId), "not Yamamoto's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, blood, Lights.MissionId), "not Blood's to give");

            // No room for the explosives: no mission. One slot freed (three in a stack of six), they come with it.
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.MoveTo(infanger.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, infanger.EntityId, Lights.MissionId), "no room for the explosives");
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(Lights.MissionId));
            harness.Manager.Scenes.Tick(desert);
            Assert.AreEqual(0U, Charges(harness), "none given");
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Lights.MissionId && obj.IsInWorld), "no generator");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);

            // An abandon after one generator first.
            Accept(harness, infanger, Lights.MissionId);
            Assert.AreEqual(Lights.Generators, Charges(harness), "three explosives at the accept");
            var (_, devices) = Generators(harness, desert);
            Plant(harness, desert, devices[0]);
            E.Tick(harness, desert, 250);
            harness.Drain();
            Assert.AreEqual((1, Lights.Generators - 1), (GeneratorsDone(harness), Charges(harness)));
            // No device without an explosive: the explosives are mission items (MissionItemProtection), a player cannot destroy
            // them, so the two left stay for the two devices left; the devices stand ready.
            var stack = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0).Select(EntityManager.Instance.GetItem)
                .Single(item => item.ItemTemplate.ItemTemplateId == ClientChargeTemplate);
            InventoryManager.Instance.PersonalInventory_DestroyItem(harness.Client,
                new Rasa.Packets.Inventory.Client.PersonalInventory_DestroyItemPacket { EntityId = stack.EntityId, Quantity = Lights.Generators - 1 });
            Assert.AreEqual(Lights.Generators - 1, InventoryManager.Instance.RemoveItemsByClass(harness.Client, stack.ItemTemplate.Class, Lights.Generators - 1),
                "nothing taken by class either");
            harness.Drain();
            Generators(harness, desert, blown: new[] { 1 });
            Assert.AreEqual((1, Lights.Generators - 1), (GeneratorsDone(harness), Charges(harness)), "the explosives cannot be destroyed");
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Lights.MissionId));
            harness.Manager.Scenes.Tick(desert);
            harness.Drain();
            Assert.AreEqual(0U, Charges(harness), "the explosives go with the mission");
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Lights.MissionId && obj.IsInWorld), "the generators go with the abandon");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, infanger, Lights.MissionId), "offered again after an abandon");

            Accept(harness, infanger, Lights.MissionId);
            Assert.AreEqual(Lights.Generators, Charges(harness), "three again");
            Assert.IsEmpty(Topics(harness, infanger, Lights.MissionId), "Infanger hears no objective");
            var snapshot = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Lights.MissionId].ObjectivesList;
            Assert.HasCount((int)Lights.Generators, snapshot);
            foreach (var objective in snapshot)
            {
                var marker = objective.IndicatorList.Single();
                var at = Lights.GeneratorsAt[objective.ObjectiveId - 1];
                Assert.IsLessThan(0.1f, Vector2.Distance(new(marker.Position.X, marker.Position.Z), new(at.X, at.Z)), $"objective {objective.ObjectiveId}'s marker on its generator");
                Assert.AreEqual(AshenMissionDataV1.MapContextId, marker.MapContextId, "on Ashen Desert");
            }

            var (generators, generatorDevices) = Generators(harness, desert);
            Assert.AreEqual(0, GeneratorsDone(harness), "all open again");
            foreach (var generator in generators)
            {
                Assert.IsFalse(generator.IsEnabled, $"{generator.SceneActorRole}: no use");
                Assert.IsFalse(PracticeTargetManager.TryGetTarget(desert, generator.EntityId, out _), $"{generator.SceneActorRole}: no gun destroys it");
                Assert.IsFalse(WorldDestructibles.TryGetTarget(desert, generator.EntityId, out _), $"{generator.SceneActorRole}: no world destructible");
            }
            var log = new List<object>();
            var blown = 0U;
            foreach (var number in new[] { 3, 1, 2 })
            {
                var device = generatorDevices[number - 1];
                Plant(harness, desert, device);
                var packets = harness.Drain().Cast<object>().Concat(E.Tick(harness, desert, 250)).ToList();
                log.AddRange(packets);
                blown++;
                Assert.AreEqual(Lights.Generators - blown, Charges(harness), $"generator {number} takes one explosive");
                Assert.AreEqual(((UseObjectState)DevicePlanted, false), (device.StateId, device.IsEnabled), $"device {number} planted and closed");
                Assert.AreEqual((UseObjectState)Destroyed, generators[number - 1].StateId, $"generator {number} blown up");
                Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Lights.MissionId && packet.ObjectiveId == Lights.Destroy(number)),
                    $"generator {number}: its objective, once");
                Assert.AreEqual((int)blown, GeneratorsDone(harness), $"generator {number}: only its own objective");
                if (number == 3)
                {
                    TryPlant(harness, desert, device);
                    Assert.AreEqual(Lights.Generators - 1, Charges(harness), "a used device takes no second explosive");
                    P.ReloadAndRecover(harness, desert);
                    Assert.AreEqual((1, Lights.Generators - 1), (GeneratorsDone(harness), Charges(harness)), "kept at a reload");
                    (generators, generatorDevices) = Generators(harness, desert, blown: new[] { 3 });
                }
                if (blown < Lights.Generators)
                    Assert.IsFalse(harness.Client.Player.Missions[Lights.MissionId].Completeable, $"{blown} of 3: not completeable");
            }
            Assert.AreEqual(0U, Charges(harness), "no explosive left");
            Assert.IsTrue(harness.Client.Player.Missions[Lights.MissionId].Completeable, "three generators");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Lights.MissionId && packet.IsCompleteable), "MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, yamamoto.EntityId, Lights.MissionId, 2), "not Yamamoto's to take");
            FullBagWaits(harness, infanger, Lights.MissionId, 2, ArmorFiller);
            FailedTurnInPaysNothing(harness, infanger, Lights.MissionId, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, infanger, Lights.MissionId, 2, Lights.Experience, Lights.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Lights.MissionId), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Lights.HazmatVest), "the vest chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, infanger, Lights.MissionId), "done once");
        }

        /// <summary>
        /// The Strongest Link: not offered before Fallen Soldiers and Xanxless Job; then Blood offers it (not Yamamoto, not
        /// Infanger); accepted, Blood hears no objective; a marker on each Linker pool and on Naxveik, on Ashen Desert. A Thrax
        /// Soldier counts on nothing; Naxveik's death completes 2 once; each Linker counts once on 1, kept at a reload; the
        /// fifteenth completes 1 once and makes the mission completeable once, a sixteenth changes nothing; a full bag waits, a
        /// failed save pays nothing; Blood pays the item chosen, once. An abandon drops the counts; taken again, they start from
        /// none.
        /// </summary>
        [TestMethod]
        public void TheStrongestLinkKillsFifteenLinkersAndNaxveik()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            var blood = Npc(desert, AshenBoneyardNpcsV1.Blood);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, blood, Link.MissionId), "it waits for Fallen Soldiers and Xanxless Job");
            SeedCompleted(harness, AshenXanxlessJobV1.MissionId, AshenFallenSoldiersV1.MissionId);
            desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            blood = Npc(desert, AshenBoneyardNpcsV1.Blood);
            var yamamoto = Npc(desert, AshenLeadInNpcsV1.Yamamoto);
            var infanger = Npc(desert, AshenBoneyardNpcsV1.Infanger);
            Spawn(desert, LinkerPools.Append(C.Naxveik).ToArray());

            var offer = WildernessRanjaGorgeTests.Offer(harness, blood, Link.MissionId);
            CollectionAssert.AreEqual(new[] { Link.MechBoots, Link.GravitonVest, Link.BioGloves, Link.ReflectiveHelmet },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, yamamoto, Link.MissionId), "not Yamamoto's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, infanger, Link.MissionId), "not Infanger's to give");

            // An abandon after some kills first.
            Accept(harness, blood, Link.MissionId);
            for (var kill = 0; kill < 4; kill++)
                KillOne(harness, desert, C.Linker);
            harness.Drain();
            Assert.AreEqual(4U, Linkers(harness));
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Link.MissionId));
            harness.Drain();
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, blood, Link.MissionId), "offered again after an abandon");

            Accept(harness, blood, Link.MissionId);
            Assert.AreEqual(0U, Linkers(harness), "the count starts from none");
            Assert.IsEmpty(Topics(harness, blood, Link.MissionId), "Blood hears no objective");
            var objectives = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Link.MissionId].ObjectivesList;
            Assert.HasCount(C.LinkerPools.Length, objectives.Single(objective => objective.ObjectiveId == Link.Linkers).IndicatorList, "a marker on each Linker pool");
            var overseer = objectives.Single(objective => objective.ObjectiveId == Link.Overseer).IndicatorList.Single();
            Assert.IsLessThan(0.1f, Vector2.Distance(new(overseer.Position.X, overseer.Position.Z), new((float)C.NaxveikAt.X, (float)C.NaxveikAt.Z)), "2's marker on Naxveik");
            Assert.IsTrue(objectives.SelectMany(objective => objective.IndicatorList).All(marker => marker.MapContextId == AshenMissionDataV1.MapContextId), "on Ashen Desert");

            KillOne(harness, desert, ThraxSoldier, spawnFrom: PoolsOf(harness, ThraxSoldier));
            harness.Drain();
            Assert.AreEqual((0U, MissionObjectiveState.Incomplete), (Linkers(harness), Objective(harness, Link.MissionId, Link.Overseer)), "a Thrax Soldier counts on nothing");

            var log = new List<object>();
            KillOne(harness, desert, C.Naxveik);
            log.AddRange(harness.Drain());
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Link.MissionId, Link.Overseer), "Naxveik killed");
            Assert.AreEqual(0U, Linkers(harness), "Naxveik is no Linker");
            for (var kill = 1; kill <= Link.LinkerKills + 1; kill++)
            {
                if (kill == 8)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(7U, Linkers(harness), "kept at a reload");
                    harness.Drain();
                }
                KillOne(harness, desert, C.Linker);
                log.AddRange(harness.Drain());
                if (kill < Link.LinkerKills)
                    Assert.IsFalse(harness.Client.Player.Missions[Link.MissionId].Completeable, $"{kill} of 15: not completeable");
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Link.MissionId, Link.Linkers), "fifteen Linkers");
            Assert.AreEqual(1, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Link.MissionId && packet.ObjectiveId == Link.Linkers), "1 once");
            Assert.AreEqual(1, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Link.MissionId && packet.ObjectiveId == Link.Overseer), "2 once");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Link.MissionId && packet.IsCompleteable), "MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, yamamoto.EntityId, Link.MissionId, 0), "not Yamamoto's to take");
            FullBagWaits(harness, blood, Link.MissionId, 0, ArmorFiller);
            FailedTurnInPaysNothing(harness, blood, Link.MissionId, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, blood, Link.MissionId, 0, Link.Experience, Link.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Link.MissionId), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Link.MechBoots), "the boots chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, blood, Link.MissionId), "done once");
        }

        private static uint Plastic(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, ClientPlasticTemplate);

        private static uint Charges(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, ClientChargeTemplate);

        private static int MortarsDone(WildernessRuntimeTestHarness harness) =>
            Enumerable.Range(1, (int)Above.Mortars).Count(number => Objective(harness, Above.MissionId, Above.DestroyMortar(number)) == MissionObjectiveState.Completed);

        private static uint MortarCount(WildernessRuntimeTestHarness harness) =>
            harness.Client.Player.Missions[Above.MissionId].Objectives[Above.Counter].Counters[0];

        private static int LaunchersDone(WildernessRuntimeTestHarness harness) =>
            Enumerable.Range(1, (int)Above.Launchers).Count(number => Objective(harness, Above.MissionId, Above.DestroyLauncher(number)) == MissionObjectiveState.Completed);

        private static int GeneratorsDone(WildernessRuntimeTestHarness harness) =>
            Enumerable.Range(1, (int)Lights.Generators).Count(number => Objective(harness, Lights.MissionId, Lights.Destroy(number)) == MissionObjectiveState.Completed);

        private static uint Linkers(WildernessRuntimeTestHarness harness) =>
            harness.Client.Player.Missions[Link.MissionId].Objectives[Link.Linkers].Counters[0];

        /// <summary>
        /// The six launchers and their devices, in their order: the player's own, of the client's classes, at their places, on
        /// the ground; a launcher whole (or, for <paramref name="blown"/>, blown up) and never interactable; its device ready and
        /// open (or planted and closed), with the client's use argument.
        /// </summary>
        private static (DynamicObject[] Launchers, DynamicObject[] Devices) Launchers(WildernessRuntimeTestHarness harness, MapChannel desert, int[] blown = null)
        {
            harness.Manager.Scenes.Tick(desert);
            var launchers = new DynamicObject[Above.Launchers];
            var devices = new DynamicObject[Above.Launchers];
            for (var number = 1; number <= Above.Launchers; number++)
            {
                var isBlown = blown?.Contains(number) == true;
                var launcher = Actor(desert, Above.MissionId, Above.LauncherRole(number));
                var device = Actor(desert, Above.MissionId, Above.DeviceRole(number));
                Assert.AreEqual((ClientLauncherClass, P.ToVector(Above.LaunchersAt[number - 1])), ((uint)launcher.EntityClassId, launcher.Position), $"launcher {number}");
                Assert.AreEqual((isBlown ? (UseObjectState)Destroyed : (UseObjectState)Intact, false), (launcher.StateId, launcher.IsEnabled), $"launcher {number} state");
                Assert.AreEqual((ClientDeviceClass, P.ToVector(Above.DevicesAt[number - 1]), ClientDeviceUseArgument), ((uint)device.EntityClassId, device.Position, device.MissionUseAction.ActionArgId), $"device {number}");
                Assert.AreEqual((isBlown ? (UseObjectState)DevicePlanted : (UseObjectState)DeviceReady, !isBlown), (device.StateId, device.IsEnabled), $"device {number} state");
                foreach (var obj in new[] { launcher, device })
                {
                    Assert.AreEqual(harness.Client.Player.Id, obj.SceneOwnerCharacterId, "the player's own");
                    AssertOnGround(desert, obj.Position, obj.SceneActorRole);
                }
                launchers[number - 1] = launcher;
                devices[number - 1] = device;
            }
            return (launchers, devices);
        }

        /// <summary>The three generators and their devices, as <see cref="Launchers"/> checks the launchers; each generator named "Power Generator".</summary>
        private static (DynamicObject[] Generators, DynamicObject[] Devices) Generators(WildernessRuntimeTestHarness harness, MapChannel desert, int[] blown = null)
        {
            harness.Manager.Scenes.Tick(desert);
            var generators = new DynamicObject[Lights.Generators];
            var devices = new DynamicObject[Lights.Generators];
            for (var number = 1; number <= Lights.Generators; number++)
            {
                var isBlown = blown?.Contains(number) == true;
                var generator = Actor(desert, Lights.MissionId, Lights.GeneratorRole(number));
                var device = Actor(desert, Lights.MissionId, Lights.DeviceRole(number));
                Assert.AreEqual((ClientGeneratorClass, ClientGeneratorName, P.ToVector(Lights.GeneratorsAt[number - 1])),
                    ((uint)generator.EntityClassId, generator.NameOverrideId, generator.Position), $"generator {number}");
                Assert.AreEqual((isBlown ? (UseObjectState)Destroyed : (UseObjectState)Intact, false), (generator.StateId, generator.IsEnabled), $"generator {number} state");
                Assert.AreEqual((ClientDeviceClass, P.ToVector(Lights.DevicesAt[number - 1]), ClientDeviceUseArgument), ((uint)device.EntityClassId, device.Position, device.MissionUseAction.ActionArgId), $"device {number}");
                Assert.AreEqual((isBlown ? (UseObjectState)DevicePlanted : (UseObjectState)DeviceReady, !isBlown), (device.StateId, device.IsEnabled), $"device {number} state");
                foreach (var obj in new[] { generator, device })
                {
                    Assert.AreEqual(harness.Client.Player.Id, obj.SceneOwnerCharacterId, "the player's own");
                    AssertOnGround(desert, obj.Position, obj.SceneActorRole);
                }
                generators[number - 1] = generator;
                devices[number - 1] = device;
            }
            return (generators, devices);
        }

        private static DynamicObject Actor(MapChannel desert, uint mission, string role) =>
            desert.DynamicObjects.Single(obj => obj.SceneMissionId == mission && obj.SceneActorRole == role && obj.IsInWorld);

        /// <summary>A use of a Detonation Device by a player beside it, with its recovery.</summary>
        private static void Plant(WildernessRuntimeTestHarness harness, MapChannel desert, DynamicObject device)
        {
            harness.MoveTo(device.Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, desert, device, ClientDeviceUseArgument, recovers: true);
        }

        /// <summary>A use the server may refuse at once or after its recovery: the caller checks that nothing came of it.</summary>
        private static void TryPlant(WildernessRuntimeTestHarness harness, MapChannel desert, DynamicObject device)
        {
            harness.MoveTo(device.Position + new Vector3(0, 0, 1));
            harness.Drain();
            var count = desert.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = device.EntityId, ActionId = ActionId.UseObject, ActionArgId = ClientDeviceUseArgument
            });
            if (desert.PerformRecovery.Count > count)
            {
                var recovery = desert.PerformRecovery.Last();
                desert.PerformRecovery.Remove(recovery);
                ActorActionManager.Instance.PerformRecovery(desert, recovery);
            }
            E.Tick(harness, desert, 250);
            harness.Drain();
        }

        /// <summary>A kill of a living creature of this row, the set's pools respawned when none is left (or <paramref name="spawnFrom"/> spawned first).</summary>
        private static void KillOne(WildernessRuntimeTestHarness harness, MapChannel desert, uint creature, uint[] spawnFrom = null)
        {
            if (spawnFrom != null)
                Spawn(desert, spawnFrom);
            if (!Alive(desert).Any(candidate => candidate.DbId == creature && candidate.Npc == null))
                Respawn(desert, AllPools.Concat(spawnFrom ?? Array.Empty<uint>()).ToArray());
            var one = Alive(desert).First(candidate => candidate.DbId == creature && candidate.Npc == null);
            harness.MoveTo(one.Position);
            one.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(desert, one, harness.Client.Player);
        }

        /// <summary>One automatic Ashen Desert pool that holds this creature (World DB), none of the set's.</summary>
        private static uint[] PoolsOf(WildernessRuntimeTestHarness harness, uint creature) =>
            harness.World.SpawnPoolEntries.AsEnumerable().Where(row => row.MapContextId == AshenMissionDataV1.MapContextId &&
                    new[] { row.Creature1Id, row.Creature2Id, row.Creature3Id, row.Creature4Id, row.Creature5Id, row.Creature6Id }.Contains(creature) &&
                    row.Mode == SpawnPoolManager.ModeAutomatic && !AllPools.Contains(row.Id))
                .Select(row => row.Id).Take(1).ToArray();

        /// <summary>
        /// The navmesh floors on a 1 m grid within ColumnReach of <paramref name="centre"/>, from 8 m under to 12 m over its
        /// height: one list per spot, its floors from the lowest up (heights less than 2 m apart are one floor), as
        /// AshenFixATests reads them.
        /// </summary>
        private static List<List<Vector3>> Columns(MapChannel desert, Vector3 centre)
        {
            var columns = new List<List<Vector3>>();
            var reach = (int)MathF.Ceiling(ColumnReach);
            for (var dz = -reach; dz <= reach; dz++)
                for (var dx = -reach; dx <= reach; dx++)
                {
                    if (dx * dx + dz * dz > ColumnReach * ColumnReach)
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

        private static string MapName(WildernessRuntimeTestHarness harness) =>
            harness.World.MapInfoEntries.AsNoTracking().Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName;
    }
}
