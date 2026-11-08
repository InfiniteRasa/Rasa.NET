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
using Rasa.Services.Preloader.Missions.Crucible;
using Rasa.Services.Preloader.Missions.Marshes;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Crucible
{
    using static MarshesFalconHoldTests;
    using static MarshesPaludosTests;
    using static MarshesPreoDasTests;
    using static PlateauCampResistanceTests;
    using C = CrucibleOutpostIntrepidTests;
    using F = MarshesClientFloorTests;
    using N = CrucibleChaukasNpcsV1;
    using H = CrucibleChaukasTests;
    using D = CrucibleFactoryFortificationV1;
    using K = CrucibleInformationIsKeyV1;

    /// <summary>
    /// Crucible set 12b, played through as a player plays it: Cipher Specialist Brian's Factory Fortification (1523: into the
    /// east round room, the factory, three waves of Thrax Soldiers held off, two Refitted Warden Bots after) and Information is
    /// Key (1525: no objective in the client, handed in at Information Analyst Ruben on the Crucible), in the Chaukas Robotics
    /// Facility (squad instance 2146). With them the factory room's guards: the marker and the Refitted Warden Bots on the
    /// room's floor, the navmesh and a client floor, clear of the hostile pools and the door; the room's area holding the
    /// marker, the bots and the entrance strips, not the corridors; and, where the local N14 fix is in place, the Thrax' walk
    /// onto the room's floor (inconclusive on upstream's mesh, where the floor is an island: CrucibleChaukasTests pins it).
    ///
    /// Why these tests: 1523 has a defence started by an area and counted by scene defeats on wall-clock timers, with scene
    /// creatures shown after it; 1525 is the Crucible's first mission with no client objective, completeable at its accept
    /// and handed in on another map than its giver's. A wave before the room, a count lost at a reload or kept after an
    /// abandon, a hidden objective sent to the client, a 1525 not completeable at once or handed in at Brian, or a turn-in
    /// that pays twice would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CrucibleChaukasFactoryTests
    {
        private const uint Fortification = D.MissionId, Information = K.MissionId;
        private const uint GetTo = CrucibleGetToChaukasV1.MissionId, Brain = CrucibleControlTheBrainV1.MissionId, Factory = CrucibleFixTheFactoryV1.MissionId;
        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack (as CrucibleIncurablesWardTests): fills the consumables bag

        private static readonly Vector3 Arrival = ToVector(N.ArrivalAt);
        private static readonly uint[] ClavigerPools = Enumerable.Range(531119, 15).Select(id => (uint)id).ToArray();

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// The factory room's places in the squad's copy: 1523's marker and the two Refitted Warden Bots on the navmesh (the
        /// room's floor: an island on upstream's mesh) and a client floor with none just over them; ReachClearance and more
        /// outside every Claviger pool's circle, 5 m and more outside the door's; inside the room's area, as the two entrance
        /// strips and the bridge by the factory are; the arrival, Brian and the nine Thrax (in the south corridor) outside it,
        /// each Thrax 20 m and more from the marker; no lava 20 m round any of them (vacuous: no Chaukas lava data, as
        /// CrucibleChaukasTests).
        /// </summary>
        [TestMethod]
        public void TheFactoryRoomsPlacesStandOnItsFloorInsideItsArea()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            C.EnterCrucible(harness);
            var copy = H.IntoChaukas(harness);
            var nav = copy.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var name = harness.World.MapInfoEntries.AsNoTracking().Single(row => row.Id == N.Chaukas).MapName;
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", name), meshes, 1);
            var links = harness.World.Set<MapLinkEntry>().AsNoTracking().Where(link => link.Enabled != 0 && link.MapContextId == N.Chaukas).ToList();
            var hostile = harness.World.SpawnPoolEntries.AsNoTracking().Where(pool => pool.MapContextId == N.Chaukas && ClavigerPools.Contains(pool.Id)).ToList();
            Assert.HasCount(ClavigerPools.Length, hostile, "the Claviger pools");

            var places = new List<(string Label, Vector3 At)> { ("1523's marker", ToVector(D.FactoryAt)) };
            places.AddRange(D.BotsAt.Select((at, index) => ($"Refitted Warden Bot {index + 1}", ToVector(at))));
            var (floors, ceilings) = F.Surfaces(geometry, places.Select(place => (place.At, F.Walkable)).ToList());
            var failures = new List<string>();
            for (var i = 0; i < places.Count; i++)
            {
                var (label, at) = places[i];
                if (at.Y - floors[i] > F.MaxOver || !float.IsPositiveInfinity(ceilings[i]))
                    failures.Add($"{label} at {at}: floor {floors[i]}, ceiling {ceilings[i]}");
                AssertOnGround(copy, at, label);
                foreach (var link in links)
                    if (Vector2.Distance(new(at.X, at.Z), new((float)link.PosX, (float)link.PosZ)) - (float)link.Radius < 5)
                        failures.Add($"{label} by link {link.Id}");
                foreach (var pool in hostile)
                    if (Vector2.Distance(new(at.X, at.Z), new((float)pool.PosX, (float)pool.PosZ)) - (float)pool.Radius < MarshesNpcsStandStillV1.ReachClearance)
                        failures.Add($"{label} in hostile pool {pool.Id}'s reach");
                if (!InTheRoom(at))
                    failures.Add($"{label} outside the room's area");
                for (var dx = -20f; dx <= 20; dx += 2)
                    for (var dz = -20f; dz <= 20; dz += 2)
                        if (dx * dx + dz * dz <= 400 && nav.Nearest(at + new Vector3(dx, 0, dz)) is { } ground && LavaDamage.InLava(name, ground))
                            failures.Add(FormattableString.Invariant($"lava at ({ground.X:0.0}, {ground.Y:0.0}, {ground.Z:0.0}) by {label}"));
            }
            foreach (var (label, at) in new[] { ("the north strip", new Vector3(88f, 17.28f, -62f)), ("the south strip", new Vector3(88f, 17.36f, -178f)),
                         ("the bridge by the factory", new Vector3(89f, 24.04f, -100f)) })
                if (!InTheRoom(at))
                    failures.Add($"{label} outside the room's area");
            foreach (var (label, at) in new[] { ("the arrival", Arrival), ("Brian", ToVector(N.BrianAt)), ("the north corridor", new Vector3(88f, 17.3f, -50f)) }
                         .Concat(D.WaveAt.Select((at, index) => ($"Thrax {index + 1}", ToVector(at)))))
                if (InTheRoom(at))
                    failures.Add($"{label} inside the room's area");
            foreach (var (at, index) in D.WaveAt.Select((at, index) => (ToVector(at), index)))
                Assert.IsGreaterThanOrEqualTo(20f, Vector3.Distance(at, ToVector(D.FactoryAt)), $"Thrax {index + 1} 20 m or more from the marker");
            Assert.IsTrue(D.BotsAt.All(at => Vector2.Distance(new(at.X, at.Z), new((float)D.FactoryAt.X, (float)D.FactoryAt.Z)) <= D.FactoryRadius + 2),
                "the bots by the marker");
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// With the local N14 fix v2 (data/navfix; inconclusive on upstream's mesh, where the room's floor is an island): the
        /// marker, the Refitted Warden Bots and the east half's floor are walked to from the arrival and from each Thrax' place,
        /// every walk one the client climbs (MaxClientStep, MaxClientGrade). Walks in only: v2's one link is one-way, from the
        /// south strip (92.8, 17.18, -176) down to the east floor (95.8, 16, -176), as the real client steps it (it does not
        /// climb back up the 1.2 m side); the way out is the room's south rim slope, which the client walks and the mesh does not
        /// have (NOTES-upstream-bugs.md "N14 v2", to-fix row 68). The west half stays an island on v2: no point there.
        /// </summary>
        [TestMethod]
        public void TheThraxWalkOntoTheFactoryFloorOnTheLocalMesh()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            C.EnterCrucible(harness);
            var copy = H.IntoChaukas(harness);
            var nav = copy.NavMesh;
            var arrival = nav.Nearest(Arrival).Value;
            var marker = nav.Nearest(ToVector(D.FactoryAt)).Value;
            if (nav.FindPath(arrival, marker, out var joined) == null || !joined)
                Assert.Inconclusive("the factory room's floor is an island (upstream's Chaukas navmesh, NOTES-upstream-bugs.md N14): the local fix is not in place");
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var name = harness.World.MapInfoEntries.AsNoTracking().Single(row => row.Id == N.Chaukas).MapName;
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", name), meshes, 1);
            var failures = new List<string>();
            void Walk(string what, Vector3 from, Vector3 to)
            {
                var start = nav.Nearest(from) ?? from;
                var path = nav.FindPath(start, nav.Nearest(to) ?? to, out var complete);
                if (path == null || !complete)
                {
                    failures.Add($"{what}: no whole walk");
                    return;
                }
                var (step, stepAt, grade, gradeAt) = CrucibleGuardTests.ClientClimb(nav, geometry, path.Prepend(start).ToList());
                if (step > CrucibleGuardTests.MaxClientStep)
                    failures.Add(FormattableString.Invariant($"{what}: a {step:0.00} m step at ({stepAt.X:0.0}, {stepAt.Y:0.0}, {stepAt.Z:0.0})"));
                if (grade > CrucibleGuardTests.MaxClientGrade)
                    failures.Add(FormattableString.Invariant($"{what}: {grade:0.00} a metre over 12 m from ({gradeAt.X:0.0}, {gradeAt.Y:0.0}, {gradeAt.Z:0.0})"));
            }
            var targets = new List<(string Label, Vector3 At)> { ("1523's marker", ToVector(D.FactoryAt)) };
            targets.AddRange(D.BotsAt.Select((at, index) => ($"Refitted Warden Bot {index + 1}", ToVector(at))));
            targets.Add(($"the room's east floor at {H.EastRoomFloor[0]}", H.EastRoomFloor[0]));
            foreach (var (label, at) in targets)
                Walk($"the arrival to {label}", Arrival, at);
            foreach (var (at, index) in D.WaveAt.Select((at, index) => (ToVector(at), index)))
                Walk($"Thrax {index + 1} to the marker", at, ToVector(D.FactoryAt));
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// Factory Fortification after Fix the Factory: Brian offers it in the copy (not before), with its four stacks; at the
        /// accept 1 is open and the room and tally objectives are hidden; no Thrax before the room; the room's area starts the
        /// defence; no wave before 15 s; each wave's three come at 15 s, 60 s and 120 s, the Crucible's Thrax Soldier, hostile,
        /// at their places, at the player; a reload in the middle keeps the count; eight dead leave 1 open; the ninth completes
        /// it (ObjectiveCompleted once, MissionCompleteable once) and the two Refitted Warden Bots stand on the room's floor,
        /// friendly, still, the player's own; a full bag waits, a failed save pays nothing; Brian pays once; then the bots go
        /// with the scene, and Information is Key follows.
        /// </summary>
        [TestMethod]
        public void FactoryFortificationHoldsTheFactoryAgainstThreeWaves()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, GetTo, Brain);
            C.EnterCrucible(harness);
            var copy = H.IntoChaukas(harness);
            var brian = Giver(copy, N.Brian);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, brian, Fortification), "it waits for Fix the Factory");
            C.SeedCompleted(harness, Factory);
            copy = H.IntoChaukas(harness);
            brian = Giver(copy, N.Brian);
            var offer = WildernessRanjaGorgeTests.Offer(harness, brian, Fortification);
            CollectionAssert.AreEqual(new[] { (D.EmpBomb, D.EmpBombs), (D.BasicMedPack, D.BasicMedPacks), (D.IncendiaryGrenade, D.IncendiaryGrenades),
                    (D.ResTraumaKit, D.ResTraumaKits) },
                offer.SelectableReward.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, brian, Information), "Information is Key waits for it");
            Accept(harness, brian, Fortification);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete),
                (Objective(harness, Fortification, D.Defend), Objective(harness, Fortification, D.InTheRoom), Objective(harness, Fortification, D.WavesTally)));
            var visible = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Fortification].ObjectivesList.Select(objective => objective.ObjectiveId).ToArray();
            CollectionAssert.AreEqual(new[] { D.Defend }, visible, "only 1 is sent: the room and the tally are hidden");
            Assert.IsEmpty(Topics(harness, brian, Fortification), "Brian hears no objective");
            Assert.IsNull(SceneCreature(harness, copy, Fortification, D.WaveRole(0)), "no Thrax before the room");

            // 2: into the room.
            var elapsed = 0L;
            void Until(long ms)
            {
                for (; elapsed < ms; elapsed += 1000)
                    Tick(harness, copy, 1000);
            }
            Creature[] Wave(int wave) => Enumerable.Range(wave * D.PerWave, D.PerWave).Select(index => SceneCreature(harness, copy, Fortification, D.WaveRole(index))).ToArray();
            for (var second = 0; second < 20; second++)
                Tick(harness, copy, 1000);
            Assert.IsTrue(Wave(0).All(one => one == null), "no wave before the room, however long");
            Enter(harness, new Vector3(88f, 17.28f, -64f));
            Tick(harness, copy, 250);
            elapsed = 250;
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Fortification, D.InTheRoom), Objective(harness, Fortification, D.Defend)), "in the room by the north door");
            Until(D.WaveMilliseconds[0] - 2000);
            Assert.IsTrue(Wave(0).All(one => one == null), "no wave before 15 s");

            var killed = 0;
            for (var wave = 0; wave < D.WaveMilliseconds.Length; wave++)
            {
                Until(D.WaveMilliseconds[wave] + 1000);
                var come = Wave(wave);
                Assert.IsTrue(come.All(one => one != null), $"wave {wave + 1} came");
                for (var slot = 0; slot < come.Length; slot++)
                {
                    var one = come[slot];
                    Assert.AreEqual((D.ThraxSoldier, TargetCategory.Hostile), (one.DbId, one.TargetCategory), $"wave {wave + 1}");
                    Assert.IsGreaterThanOrEqualTo(20f, Vector3.Distance(one.Position, ToVector(D.FactoryAt)), "20 m or more from the marker");
                }
                if (wave + 1 < D.WaveMilliseconds.Length)
                    Assert.IsTrue(Wave(wave + 1).All(one => one == null), $"wave {wave + 2} not yet");
                foreach (var one in come.Take(wave == D.WaveMilliseconds.Length - 1 ? come.Length - 1 : come.Length))
                {
                    Kill(harness, copy, one);
                    killed++;
                    elapsed += 500;
                }
                if (wave == 0)
                {
                    ReloadAndRecover(harness, copy);
                    elapsed += 500;
                    Assert.AreEqual((3U, MissionObjectiveState.Incomplete), (H.Counter(harness, Fortification, D.WavesTally), Objective(harness, Fortification, D.Defend)),
                        "kept at a reload in the middle of the defence");
                }
            }
            Assert.AreEqual(8, killed);
            Assert.AreEqual((8U, MissionObjectiveState.Incomplete), (H.Counter(harness, Fortification, D.WavesTally), Objective(harness, Fortification, D.Defend)), "8 of 9");
            Assert.IsFalse(harness.Client.Player.Missions[Fortification].Completeable);
            Assert.IsNull(SceneCreature(harness, copy, Fortification, D.BotRole(0)), "no bot while a Thrax lives");

            var last = Wave(2).Last();
            Assert.IsNotNull(last, "the ninth is still there");
            harness.MoveTo(last.Position);
            last.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(copy, last, harness.Client.Player);
            var packets = harness.Drain().ToList();
            for (var tick = 0; tick < 2; tick++)    // Tick's scene pass, its packets kept
            {
                harness.UtcNow = harness.UtcNow.AddMilliseconds(250);
                harness.Manager.Scenes.Tick(copy);
                packets.AddRange(harness.Drain());
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Fortification, D.Defend), "the ninth: the factory held");
            Assert.IsTrue(harness.Client.Player.Missions[Fortification].Completeable);
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Fortification && packet.ObjectiveId == D.Defend),
                "ObjectiveCompleted once");
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Fortification && packet.IsCompleteable),
                "MissionCompleteable once");
            Tick(harness, copy, 250);

            for (var index = 0; index < D.BotsAt.Length; index++)
            {
                var bot = SceneCreature(harness, copy, Fortification, D.BotRole(index));
                Assert.IsNotNull(bot, $"Refitted Warden Bot {index + 1}");
                Assert.AreEqual((D.RefittedBot, D.RefittedBotName), (bot.DbId, bot.NameId), "the Refitted Warden Bot's row");
                Assert.AreNotEqual(TargetCategory.Hostile, bot.TargetCategory, "friendly");
                Assert.IsTrue(BehaviorManager.NeverMoves(bot), "it stands still");
                Assert.AreEqual(ToVector(D.BotsAt[index]).X, bot.Position.X, 0.5f, "at its place");
                Assert.AreEqual(ToVector(D.BotsAt[index]).Z, bot.Position.Z, 0.5f, "at its place");
                Assert.IsTrue(MissionNpcVisibility.IsShownTo(harness.Client, bot), "shown to the player");
            }

            FullBagWaits(harness, brian, Fortification, 2, ConsumableFiller);
            FailedTurnInPaysNothing(harness, brian, Fortification, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, brian, Fortification, 2, D.Experience, D.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Fortification), "MissionRewarded once");
            Assert.AreEqual(D.IncendiaryGrenades, WildernessRanjaGorgeTests.Held(harness, D.IncendiaryGrenade), "the three Incendiary Grenades");
            for (var tick = 0; tick < 4; tick++)        // RetireRewardedScene retries: up to 1 s
                Tick(harness, copy, 250);
            Assert.IsNull(SceneCreature(harness, copy, Fortification, D.BotRole(0)), "the bots go with the mission");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, brian, Fortification), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, brian, Information), "Information is Key follows");
        }

        /// <summary>
        /// An abandon in the middle of the defence (the first wave out): the Thrax go with the scene, no later wave comes and no
        /// 1523 scene pool is left in the copy; taken again, 1 and the room are open again and no Thrax stands before the room.
        /// </summary>
        [TestMethod]
        public void FactoryFortificationAbandonedMidWaveClearsTheWaves()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, GetTo, Brain, Factory);
            C.EnterCrucible(harness);
            var copy = H.IntoChaukas(harness);
            var brian = Giver(copy, N.Brian);
            Accept(harness, brian, Fortification);
            Tick(harness, copy, 250);
            Enter(harness, new Vector3(88f, 17.36f, -178f));
            for (var second = 0; second < D.WaveMilliseconds[0] / 1000 + 2; second++)
                Tick(harness, copy, 1000);
            var wave = Enumerable.Range(0, D.PerWave).Select(index => SceneCreature(harness, copy, Fortification, D.WaveRole(index))).ToArray();
            Assert.IsTrue(wave.All(one => one != null), "the first wave is out (the room entered by the south door counts too)");

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Fortification));
            harness.Drain();
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            for (var index = 0; index < D.WaveAt.Length; index++)
                Assert.IsNull(SceneCreature(harness, copy, Fortification, D.WaveRole(index)), $"Thrax {index + 1} gone");
            Assert.IsFalse(copy.SpawnPools.Any(pool => pool.SceneRunId != null && pool.ScenarioMissionId == Fortification), "no 1523 scene pool left");
            Assert.IsFalse(Alive(copy).Any(one => wave.Contains(one)), "no Thrax left on the map");
            for (var second = 0; second < 130; second++)
                Tick(harness, copy, 1000);
            Assert.IsNull(SceneCreature(harness, copy, Fortification, D.WaveRole(D.PerWave)), "no later wave");

            Accept(harness, brian, Fortification);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete, 0U),
                (Objective(harness, Fortification, D.Defend), Objective(harness, Fortification, D.InTheRoom), H.Counter(harness, Fortification, D.WavesTally)),
                "taken again from the start");
            for (var second = 0; second < D.WaveMilliseconds[0] / 1000 + 2; second++)
                Tick(harness, copy, 1000);
            Assert.IsNull(SceneCreature(harness, copy, Fortification, D.WaveRole(0)), "no Thrax before the room again");
        }

        /// <summary>
        /// Information is Key after Factory Fortification: only Brian offers it, in the copy (Ruben does not), with its four
        /// stacks; at the accept it is completeable at once (MissionCompleteable), and the client is sent no objective (its one
        /// objective hidden); Brian takes no turn-in (not its receiver); on the Crucible Ruben takes it: a full bag waits, a
        /// failed save pays nothing, Ruben pays once; done once.
        /// </summary>
        [TestMethod]
        public void InformationIsKeyIsHandedInAtRuben()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, GetTo, Brain, Factory);
            var crucible = C.EnterCrucible(harness, ToVector(N.RubenAt));
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Npc(crucible, N.Ruben), Information), "not Ruben's to give");
            var copy = H.IntoChaukas(harness);
            var brian = Giver(copy, N.Brian);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, brian, Information), "it waits for Factory Fortification");
            C.SeedCompleted(harness, Fortification);
            copy = H.IntoChaukas(harness);
            brian = Giver(copy, N.Brian);
            var offer = WildernessRanjaGorgeTests.Offer(harness, brian, Information);
            CollectionAssert.AreEqual(new[] { (K.StandardMedPack, K.StandardMedPacks), (K.IncendiaryGrenade, K.IncendiaryGrenades),
                    (K.AdvancedMedPack, K.AdvancedMedPacks), (K.EmpGrenade, K.EmpGrenades) },
                offer.SelectableReward.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());

            harness.MoveTo(brian.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, brian.EntityId, Information));
            var packets = harness.Drain();
            Assert.AreEqual(1, packets.OfType<MissionGainedPacket>().Count(packet => packet.MissionId == Information), "MissionGained once");
            Assert.IsTrue(packets.OfType<MissionGainedPacket>().Single(packet => packet.MissionId == Information).MissionInfo.Completeable,
                "bCompleteable on the wire: Ruben can show the turn-in at once");
            Assert.IsTrue(harness.Client.Player.Missions[Information].Completeable, "completeable at the accept: the client has no objective");
            Assert.IsEmpty(harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Information].ObjectivesList, "no objective sent");
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, brian.EntityId, Information, 1), "Brian is not its receiver");
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[Information].State);

            harness.Manager.Scenes.Detach(harness.Client, copy);
            crucible = C.EnterCrucible(harness, ToVector(N.RubenAt));
            Assert.IsTrue(harness.Client.MissionAreaService.RecordArrival(harness.Client), "arrived, as ManifestationManager.MissionArrival tells it");
            var arrival = harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[Information].Objectives[K.BackOnTheCrucible].State,
                "the hidden objective completes on the Crucible");
            Assert.IsFalse(arrival.OfType<ObjectiveCompletedPacket>().Any(packet => packet.MissionId == Information && packet.ObjectiveId == K.BackOnTheCrucible),
                "hidden: the client hears nothing of it");
            Assert.IsTrue(harness.Client.Player.Missions[Information].Completeable);
            var ruben = Npc(crucible, N.Ruben);
            Assert.IsEmpty(Topics(harness, ruben, Information), "Ruben hears no objective");
            FullBagWaits(harness, ruben, Information, 2, ConsumableFiller);
            FailedTurnInPaysNothing(harness, ruben, Information, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, ruben, Information, 2, K.Experience, K.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Information), "MissionRewarded once");
            Assert.AreEqual(K.AdvancedMedPacks, WildernessRanjaGorgeTests.Held(harness, K.AdvancedMedPack), "the two Advanced Med Packs");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Npc(crucible, N.Ruben), Information), "done once");
        }

        /// <summary>Inside 1523's room area (MissionAreaService's cylinder: radius and half-height round its middle).</summary>
        private static bool InTheRoom(Vector3 at) =>
            Vector2.Distance(new(at.X, at.Z), new((float)D.RoomAt.X, (float)D.RoomAt.Z)) <= D.RoomRadius &&
            MathF.Abs(at.Y - (float)D.RoomAt.Y) <= D.RoomHalfHeight;
    }
}
