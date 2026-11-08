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
using Rasa.Missions.Runtime;
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
    using I = CrucibleIncurablesWardNpcsV1;
    using P = CrucibleLeakyPipesV1;
    using W = CrucibleIncurablesWardTests;

    /// <summary>
    /// Crucible set 11b, played through as a player plays it: Engineer Dhimand's Leaky Pipes (1398: his tool chest from Jaikev,
    /// his walk with the player into the Maintenance Corridors to the water processing system, three waves of Incurables while
    /// he seals the pipes, back to Supervisor Sheira), in the Incurables Ward (squad instance 2141). With it the places'
    /// guards: the water system, Dhimand's repair place and the wave places on the navmesh and a client floor, walked to with no
    /// climb the client fails, clear of the hostile pools and the door; and the talks that share a package with set 11a's
    /// missions (Dhimand 1700 with 1444, Sheira 1640 with 1445) kept apart by mission.
    ///
    /// Blood, Fire, and Steel (1401, set 15) is CrucibleBloodFireSteelTests'.
    ///
    /// Why these tests: 1398 has a boss drop, an escort copy of an NPC whose row set 11a wrote, a defence counted by scene
    /// defeats on wall-clock timers, a talk gated by a player flag, and two talks on packages 1444 and 1445 also use. A drop
    /// before the objective, a talk heard before the waves are dead, a follow lost at a reload, a flag kept after an abandon,
    /// a topic of one mission heard at another's objective, or a turn-in that pays twice would each break it.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CrucibleIncurablesWardWingsTests
    {
        private const uint Ward = I.Ward;
        private const uint Leaky = P.MissionId, Sabotage = CrucibleSadisticSabotageV1.MissionId, Asylum = CrucibleLostInTheAsylumV1.MissionId;
        private const uint GameWardens = CrucibleGameWardensV1.MissionId;
        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack (as CrucibleIncurablesWardTests)
        private const uint MissionItemFiller = 11519;      // as CrucibleStaalJunkyardTests (inventory category 4, the chest's)

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// The places of 1398 in the squad's copy: the water system (the area of 3), Dhimand's repair place and the nine wave
        /// places on the navmesh and a client floor with none just over them; the walk from Dhimand to the water system and back
        /// complete and one the client climbs (the corridors are open: tester, 2026-11-08); each wave place a whole walk from the
        /// repair place, 20 m and more from it; the repair place walked back to the arrival through the north airlock (the
        /// navmesh gap, upstream N13, pinned in CrucibleIncurablesWardTests); every place ReachClearance and more outside every
        /// hostile pool's circle and 5 m and more outside the door's; the repair place seen from 16 of 16 places 2 m round it.
        /// </summary>
        [TestMethod]
        public void TheWaterSystemAndTheWavesStandWhereTheyAreWalkedTo()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            C.EnterCrucible(harness);
            var copy = W.IntoWard(harness);
            var nav = copy.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var name = harness.World.MapInfoEntries.AsNoTracking().Single(row => row.Id == Ward).MapName;
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", name), meshes, 1);
            var links = harness.World.Set<MapLinkEntry>().AsNoTracking().Where(link => link.Enabled != 0 && link.MapContextId == Ward).ToList();
            var hostile = harness.World.SpawnPoolEntries.AsNoTracking().Where(pool => pool.MapContextId == Ward && pool.Id != I.Sheira &&
                pool.Id != I.Shanul && pool.Id != I.Dhimand).ToList();
            Assert.IsNotEmpty(hostile);

            var water = ToVector(P.WaterAt);
            var repair = ToVector(P.RepairAt);
            var places = new List<(string Label, Vector3 At)> { ("the water system", water), ("the repair place", repair) };
            places.AddRange(P.WaveAt.Select((at, index) => ($"wave Incurable {index + 1}", ToVector(at))));
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
            }
            Assert.IsLessThanOrEqualTo((float)P.AreaRadius - 1, Vector2.Distance(new(repair.X, repair.Z), new(water.X, water.Z)), "the repair place inside 3's area");

            void Climb(string what, Vector3 from, Vector3 to)
            {
                var start = nav.Nearest(from) ?? from;
                var path = nav.FindPath(start, nav.Nearest(to) ?? to, out var complete);
                Assert.IsTrue(path != null && complete, $"{what}: a walk");
                var (step, stepAt, grade, gradeAt) = CrucibleGuardTests.ClientClimb(nav, geometry, path.Prepend(start).ToList());
                if (step > CrucibleGuardTests.MaxClientStep)
                    failures.Add(FormattableString.Invariant($"{what}: a {step:0.00} m step at ({stepAt.X:0.0}, {stepAt.Y:0.0}, {stepAt.Z:0.0})"));
                if (grade > CrucibleGuardTests.MaxClientGrade)
                    failures.Add(FormattableString.Invariant($"{what}: {grade:0.00} a metre over 12 m from ({gradeAt.X:0.0}, {gradeAt.Y:0.0}, {gradeAt.Z:0.0})"));
            }
            var dhimand = ToVector(I.DhimandAt);
            Climb("Dhimand to the water system", dhimand, water);
            Climb("the water system to Dhimand", water, dhimand);
            Climb("Dhimand to his repair place", dhimand, repair);
            foreach (var (at, index) in P.WaveAt.Select((at, index) => (ToVector(at), index)))
            {
                Climb($"wave Incurable {index + 1} to the repair place", at, repair);
                Assert.IsGreaterThanOrEqualTo(20f, Vector3.Distance(at, repair), $"wave Incurable {index + 1} 20 m or more from Dhimand");
            }
            var back = W.WalkToArrival(nav, repair, true, "the repair place");
            var (backStep, _, backGrade, _) = CrucibleGuardTests.ClientClimb(nav, geometry, back);
            if (backStep > CrucibleGuardTests.MaxClientStep || backGrade > CrucibleGuardTests.MaxClientGrade)
                failures.Add(FormattableString.Invariant($"the repair place to the arrival: step {backStep:0.00}, grade {backGrade:0.00}"));
            W.WalkFromArrival(nav, water, true, "the water system");

            var near = F.Near(geometry, new List<Vector3> { repair }, 8)[0];
            var aim = repair + new Vector3(0, F.NpcMiddle, 0);
            for (var k = 0; k < F.RingPlaces; k++)
            {
                var angle = k * 2 * MathF.PI / F.RingPlaces;
                var stand = repair + new Vector3(MathF.Sin(angle) * F.Ring, 0, MathF.Cos(angle) * F.Ring);
                if (F.FloorAt(near, stand, repair.Y - F.MaxOver, repair.Y + F.MaxUnder) is not { } fy || !F.Clear(near, stand with { Y = fy + F.Eye }, aim))
                    failures.Add($"Dhimand at his repair place not seen from ({stand.X:F1}, {stand.Z:F1})");
            }

            // The walks to Atali's and Umank's wings (1401) are CrucibleBloodFireSteelTests' (the pit stairs, upstream N15).
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// Leaky Pipes: Dhimand offers it in the squad's Ward (no prerequisite), not Sheira; Jaikev killed before the accept drops
        /// no chest; at the accept Dhimand's copy (the player's own, the public Dhimand hidden) stands at his place; a full bag
        /// refuses the chest, a slot freed takes it; a death is not a pickup; the chest opens 2, a second Jaikev kill drops no second chest and a reload keeps it; his talk
        /// takes the chest, opens 3 and he follows; the water system's area completes 3 (he stops following and walks to his
        /// repair place); no wave before 15 s; each wave's three come 20 m and more off, hostile, at him; 4's talk is not heard
        /// while one lives, a reload in the middle keeps the count and the flag unset; the ninth sets it and his talk is heard,
        /// opens 5 and he follows; with Sadistic Sabotage 1445 held, Sheira (package 1640 for both) offers 1398's 5 and 1445's Report each for its own mission, and 1398's talk leaves 1445 open; the mission is completeable once; a failed save pays
        /// nothing; a full bag does not stop it (no items); Sheira pays once; the copy goes and the public Dhimand is back.
        /// </summary>
        [TestMethod]
        public void LeakyPipesSealsTheWaterSystem()
        {
            var percentile = 99;
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            C.SeedCompleted(harness, GameWardens);
            var crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            Accept(harness, Npc(crucible, CrucibleSadisticSabotageV1.Nema), Sabotage);    // held to the end: Sheira's package 1640 is 1445's too
            var copy = W.IntoWard(harness);
            var dhimand = Giver(copy, I.Dhimand);
            var sheira = Giver(copy, I.Sheira);
            var offer = WildernessRanjaGorgeTests.Offer(harness, dhimand, Leaky);
            Assert.IsEmpty(offer.SelectableReward, "no items to choose");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, sheira, Leaky), "not Sheira's to give");
            W.Stand(copy, I.JaikevPool);
            Assert.IsFalse(Holds(Kill(harness, copy, I.Jaikev), P.ChestTemplate), "no chest before the mission");

            Accept(harness, dhimand, Leaky);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive, MissionObjectiveState.Inactive, MissionObjectiveState.Inactive,
                    MissionObjectiveState.Inactive),
                (Objective(harness, Leaky, P.RecoverChest), Objective(harness, Leaky, P.ReturnTools), Objective(harness, Leaky, P.Escort),
                    Objective(harness, Leaky, P.Protect), Objective(harness, Leaky, P.EscortBack)));
            var visible = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Leaky].ObjectivesList.Select(objective => objective.ObjectiveId).ToArray();
            Assert.IsFalse(visible.Contains(P.WavesTally), "the wave tally is hidden");
            var follower = SceneCreature(harness, copy, Leaky, P.DhimandRole);
            Assert.IsNotNull(follower, "Dhimand's copy");
            Assert.AreEqual((I.Dhimand, I.DhimandPackage), (follower.DbId, follower.Npc?.NpcPackageId), "a copy of Dhimand's row");
            Assert.AreEqual(ToVector(I.DhimandAt).X, follower.Position.X, 0.5f, "at Dhimand's place");
            Assert.AreEqual(ToVector(I.DhimandAt).Z, follower.Position.Z, 0.5f, "at Dhimand's place");
            Assert.IsTrue(MissionNpcVisibility.IsShownTo(harness.Client, follower), "the copy is shown to the player");
            Assert.IsFalse(MissionNpcVisibility.IsShownTo(harness.Client, dhimand), "the public Dhimand is hidden while the copy lives");
            Assert.IsEmpty(Topics(harness, follower, Leaky), "Dhimand waits for his chest");

            // 1: the chest from Jaikev (chance 100: no roll, so the highest roll drops it too).
            W.Stand(copy, I.JaikevPool);
            var chest = Kill(harness, copy, I.Jaikev);
            Assert.IsTrue(Holds(chest, P.ChestTemplate), "Jaikev drops the chest");
            Assert.AreEqual(0U, Chests(harness), "a death is not a pickup");
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.Drain();
            PickUp(harness, chest);
            harness.Drain();
            Assert.AreEqual((0U, MissionObjectiveState.Incomplete), (Chests(harness), Objective(harness, Leaky, P.RecoverChest)), "a full bag takes no chest");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            harness.Drain();
            PickUp(harness, chest);
            var packets = harness.Drain();
            Assert.AreEqual(1U, Chests(harness));
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Leaky && packet.ObjectiveId == P.RecoverChest),
                "ObjectiveCompleted once");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Leaky, P.ReturnTools), "the chest opens 2");
            W.Stand(copy, I.JaikevPool);
            Assert.IsFalse(Holds(Kill(harness, copy, I.Jaikev), P.ChestTemplate), "1 done: Jaikev drops no second chest");
            Assert.AreEqual(1U, Chests(harness));
            ReloadAndRecover(harness, copy);
            Assert.AreEqual((1U, MissionObjectiveState.Incomplete), (Chests(harness), Objective(harness, Leaky, P.ReturnTools)), "kept at a reload");

            // 2: the chest to Dhimand; he follows.
            follower = SceneCreature(harness, copy, Leaky, P.DhimandRole);
            CollectionAssert.AreEqual(new[] { P.ReturnTools }, Topics(harness, follower, Leaky), "Dhimand hears 2 only");
            Assert.IsEmpty(Topics(harness, sheira, Leaky), "Sheira hears nothing of 1398 yet");
            Talk(harness, follower, Leaky, P.ReturnTools);
            Tick(harness, copy, 250);
            Assert.AreEqual((0U, MissionObjectiveState.Incomplete), (Chests(harness), Objective(harness, Leaky, P.Escort)), "he takes the chest; 3 opens");
            Assert.AreEqual(0U, Flag(harness), "the defence flag unset");
            follower = SceneCreature(harness, copy, Leaky, P.DhimandRole);
            Assert.AreEqual(harness.Client.Player.Id, follower.SpawnPool.FollowOwnerCharacterId, "Dhimand follows");

            // A first stretch of the walk to the water system, Dhimand with the player.
            var nav = copy.NavMesh;
            var start = nav.Nearest(ToVector(I.DhimandAt)).Value;
            var walk = nav.FindPath(start, nav.Nearest(ToVector(P.WaterAt)).Value, out var whole).Prepend(start).ToList();
            Assert.IsTrue(whole, "a walk to the water system");
            var walked = 0f;
            for (var i = 1; i < walk.Count && walked < 40; i++)
            {
                var steps = Math.Max(1, (int)MathF.Ceiling(Vector3.Distance(walk[i - 1], walk[i]) / 4));
                for (var s = 1; s <= steps && walked < 40; s++)
                {
                    harness.MoveTo(Vector3.Lerp(walk[i - 1], walk[i], s / (float)steps));
                    walked += Vector3.Distance(walk[i - 1], walk[i]) / steps;
                    Tick(harness, copy, 1000);
                }
            }
            for (var second = 0; second < 5; second++)
                Tick(harness, copy, 1000);
            follower = SceneCreature(harness, copy, Leaky, P.DhimandRole);
            Assert.IsLessThan(12f, Vector3.Distance(follower.Position, harness.Client.Player.Position), "Dhimand came along");

            // 3: at the water system; he walks to his repair place.
            Enter(harness, ToVector(P.WaterAt));
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete), (Objective(harness, Leaky, P.Escort), Objective(harness, Leaky, P.Protect)));
            Assert.AreEqual(0U, SceneCreature(harness, copy, Leaky, P.DhimandRole).SpawnPool.FollowOwnerCharacterId, "he follows no longer");
            Assert.IsFalse(Topics(harness, SceneCreature(harness, copy, Leaky, P.DhimandRole), Leaky).Contains(P.Protect), "no talk before the waves are dead");

            // 4: three waves at 15 s, 60 s and 120 s.
            var elapsed = 250L;
            void Until(long ms)
            {
                for (; elapsed < ms; elapsed += 1000)
                    Tick(harness, copy, 1000);
            }
            Creature[] Wave(int wave) => Enumerable.Range(wave * P.PerWave, P.PerWave).Select(index => SceneCreature(harness, copy, Leaky, P.WaveRole(index))).ToArray();
            Until(P.WaveMilliseconds[0] - 2000);
            Assert.IsTrue(Wave(0).All(one => one == null), "no wave before 15 s");
            var killed = 0;
            for (var wave = 0; wave < P.WaveMilliseconds.Length; wave++)
            {
                Until(P.WaveMilliseconds[wave] + 1000);
                var come = Wave(wave);
                Assert.IsTrue(come.All(one => one != null), $"wave {wave + 1} came");
                for (var slot = 0; slot < come.Length; slot++)
                {
                    var one = come[slot];
                    Assert.AreEqual((P.WaveKind(wave * P.PerWave + slot), TargetCategory.Hostile), (one.DbId, one.TargetCategory), $"wave {wave + 1}");
                    Assert.IsGreaterThanOrEqualTo(20f, Vector3.Distance(one.Position, ToVector(P.RepairAt)), "20 m or more from Dhimand");
                }
                if (wave + 1 < P.WaveMilliseconds.Length)
                    Assert.IsTrue(Wave(wave + 1).All(one => one == null), $"wave {wave + 2} not yet");
                foreach (var one in come.Take(wave == P.WaveMilliseconds.Length - 1 ? come.Length - 1 : come.Length))
                {
                    Kill(harness, copy, one);
                    killed++;
                }
                if (wave == 0)
                {
                    ReloadAndRecover(harness, copy);
                    Assert.AreEqual((3U, MissionObjectiveState.Incomplete), (Counter(harness, Leaky, P.WavesTally), Objective(harness, Leaky, P.Protect)),
                        "kept at a reload in the middle of the defence");
                }
            }
            Assert.AreEqual(8, killed);
            Assert.AreEqual((8U, 0U), (Counter(harness, Leaky, P.WavesTally), Flag(harness)), "8 of 9: the flag unset");
            follower = SceneCreature(harness, copy, Leaky, P.DhimandRole);
            Assert.IsFalse(Topics(harness, follower, Leaky).Contains(P.Protect), "no talk with one left");
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, follower.EntityId, Leaky, P.Protect, 1), "not before the waves are dead");
            Kill(harness, copy, Wave(2).Last());
            Assert.AreEqual((MissionObjectiveState.Completed, 1U), (Objective(harness, Leaky, P.WavesTally), Flag(harness)), "the ninth: the flag set");
            for (var second = 0; second < 150 && Vector3.Distance(SceneCreature(harness, copy, Leaky, P.DhimandRole).Position, ToVector(P.RepairAt)) > 1.5f; second++)
                Tick(harness, copy, 1000);
            follower = SceneCreature(harness, copy, Leaky, P.DhimandRole);
            Assert.IsLessThanOrEqualTo(1.5f, Vector3.Distance(follower.Position, ToVector(P.RepairAt)), "Dhimand at his repair place");
            CollectionAssert.AreEqual(new[] { P.Protect }, Topics(harness, follower, Leaky), "now his talk");
            Talk(harness, follower, Leaky, P.Protect);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Leaky, P.EscortBack));
            Assert.AreEqual(harness.Client.Player.Id, SceneCreature(harness, copy, Leaky, P.DhimandRole).SpawnPool.FollowOwnerCharacterId, "he follows out");

            // 5: back to Sheira.
            CollectionAssert.AreEqual(new[] { P.EscortBack }, Topics(harness, sheira, Leaky), "Sheira hears 5 for 1398");
            CollectionAssert.AreEqual(new[] { CrucibleSadisticSabotageV1.Report }, Topics(harness, sheira, Sabotage), "and 1445's Report for 1445");
            harness.MoveTo(sheira.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, sheira.EntityId, Leaky, P.EscortBack, 1));
            packets = harness.Drain();
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Leaky && packet.IsCompleteable), "MissionCompleteable once");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Sabotage, CrucibleSadisticSabotageV1.Report), "1398's talk does nothing to 1445");
            CollectionAssert.AreEqual(new[] { CrucibleSadisticSabotageV1.Report }, Topics(harness, sheira, Sabotage), "1445's Report still heard");

            FailedTurnInPaysNothing(harness, sheira, Leaky, null);
            WildernessAliaBranchesTests.FillCategory(harness, ConsumableFiller, 50);
            harness.FillInbox();
            harness.Drain();
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, sheira, Leaky, null, P.Experience, P.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Leaky), "MissionRewarded once, a full bag or not");
            for (var tick = 0; tick < 4; tick++)        // RetireRewardedScene retries: up to 1 s
                Tick(harness, copy, 250);
            Assert.IsNull(SceneCreature(harness, copy, Leaky, P.DhimandRole), "the copy goes with the mission");
            Assert.IsTrue(MissionNpcVisibility.IsShownTo(harness.Client, Giver(copy, I.Dhimand)), "the public Dhimand is shown again");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Giver(copy, I.Dhimand), Leaky), "done once");
        }

        /// <summary>
        /// Abandons: one between the chest's pickup and 2's talk: taken again, the held chest counts at once
        /// (ExistingFactObjectiveIds: 1 Completed, 2 Incomplete). One after the defence (the flag set): the flag outlives the
        /// abandon, taken again (a new chest: his talk took the first) 2's talk sets the flag back to 0, so 4 is not heard
        /// again before nine more wave deaths.
        /// </summary>
        [TestMethod]
        public void LeakyPipesAbandonedMustDefendDhimandAgain()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            C.EnterCrucible(harness);
            var copy = W.IntoWard(harness);
            Accept(harness, Giver(copy, I.Dhimand), Leaky);
            Tick(harness, copy, 250);
            W.Stand(copy, I.JaikevPool);
            PickUp(harness, Kill(harness, copy, I.Jaikev));
            harness.Drain();
            Assert.AreEqual(1U, Chests(harness));

            // Abandoned with the chest held, taken again: 1 counts at once.
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Leaky));
            harness.Drain();
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            Assert.AreEqual(1U, Chests(harness), "the chest kept at the abandon");
            Accept(harness, Giver(copy, I.Dhimand), Leaky);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Leaky, P.RecoverChest), Objective(harness, Leaky, P.ReturnTools)), "the held chest counts at the accept");
            Talk(harness, SceneCreature(harness, copy, Leaky, P.DhimandRole), Leaky, P.ReturnTools);
            Enter(harness, ToVector(P.WaterAt));
            for (var second = 0; second < 125; second++)
                Tick(harness, copy, 1000);
            for (var index = 0; index < P.WaveAt.Length; index++)
                Kill(harness, copy, SceneCreature(harness, copy, Leaky, P.WaveRole(index)));
            Assert.AreEqual(1U, Flag(harness), "defended");

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Leaky));
            harness.Drain();
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            Assert.IsNull(SceneCreature(harness, copy, Leaky, P.DhimandRole), "the copy goes");
            Assert.AreEqual(1U, Flag(harness), "a player flag outlives the abandon");
            var dhimand = Giver(copy, I.Dhimand);
            Assert.IsTrue(MissionNpcVisibility.IsShownTo(harness.Client, dhimand), "the public Dhimand is shown again");

            // Taken again after the defence: another kill first (the first chest was taken by his talk).
            W.Stand(copy, I.JaikevPool);
            Accept(harness, dhimand, Leaky);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Leaky, P.RecoverChest), "all over again");
            PickUp(harness, Kill(harness, copy, I.Jaikev));
            harness.Drain();
            Talk(harness, SceneCreature(harness, copy, Leaky, P.DhimandRole), Leaky, P.ReturnTools);
            Assert.AreEqual(0U, Flag(harness), "his talk sets the flag back");
            Enter(harness, ToVector(P.WaterAt));
            Tick(harness, copy, 250);
            Assert.IsFalse(Topics(harness, SceneCreature(harness, copy, Leaky, P.DhimandRole), Leaky).Contains(P.Protect), "4 must be earned again");
        }

        /// <summary>
        /// An abandon in the middle of the defence (the first wave out): the wave Incurables and Dhimand's copy go with the
        /// scene, no scene pool of 1398 is left in the copy, and the mission is taken again at Dhimand (1 open: his talk took
        /// the chest).
        /// </summary>
        [TestMethod]
        public void LeakyPipesAbandonedMidWaveClearsTheWaves()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            C.EnterCrucible(harness);
            var copy = W.IntoWard(harness);
            Accept(harness, Giver(copy, I.Dhimand), Leaky);
            Tick(harness, copy, 250);
            W.Stand(copy, I.JaikevPool);
            PickUp(harness, Kill(harness, copy, I.Jaikev));
            harness.Drain();
            Talk(harness, SceneCreature(harness, copy, Leaky, P.DhimandRole), Leaky, P.ReturnTools);
            Enter(harness, ToVector(P.WaterAt));
            for (var second = 0; second < P.WaveMilliseconds[0] / 1000 + 2; second++)
                Tick(harness, copy, 1000);
            var wave = Enumerable.Range(0, P.PerWave).Select(index => SceneCreature(harness, copy, Leaky, P.WaveRole(index))).ToArray();
            Assert.IsTrue(wave.All(one => one != null), "the first wave is out");

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Leaky));
            harness.Drain();
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            for (var index = 0; index < P.WaveAt.Length; index++)
                Assert.IsNull(SceneCreature(harness, copy, Leaky, P.WaveRole(index)), $"wave Incurable {index + 1} gone");
            Assert.IsNull(SceneCreature(harness, copy, Leaky, P.DhimandRole), "the copy gone");
            Assert.IsFalse(copy.SpawnPools.Any(pool => pool.SceneRunId != null && pool.ScenarioMissionId == Leaky), "no 1398 scene pool left");
            Assert.IsFalse(Alive(copy).Any(one => wave.Contains(one)), "no wave Incurable left on the map");
            for (var second = 0; second < 130; second++)
                Tick(harness, copy, 1000);
            Assert.IsNull(SceneCreature(harness, copy, Leaky, P.WaveRole(P.PerWave)), "no later wave");

            var dhimand = Giver(copy, I.Dhimand);
            Assert.IsTrue(MissionNpcVisibility.IsShownTo(harness.Client, dhimand), "the public Dhimand is shown again");
            Accept(harness, dhimand, Leaky);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Objective(harness, Leaky, P.RecoverChest), Objective(harness, Leaky, P.Protect)), "taken again from the start");
            Assert.IsNotNull(SceneCreature(harness, copy, Leaky, P.DhimandRole), "a new copy");
        }

        /// <summary>
        /// The packages 1398 shares with set 11a: with Lost in the Asylum (3 at Dhimand, package 1700) and Leaky Pipes (2 at
        /// Dhimand) both open, Dhimand's copy offers each mission's own objective, and a talk for one completes only that one;
        /// with Sadistic Sabotage held (1 at Sheira, package 1640) and 1398's 5 not yet open, Sheira offers 1445's Report only.
        /// </summary>
        [TestMethod]
        public void TheSharedPackagesKeepEachMissionsTalkApart()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            C.SeedCompleted(harness, GameWardens);
            var crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            Accept(harness, Npc(crucible, CrucibleSadisticSabotageV1.Nema), Sabotage);
            var copy = W.IntoWard(harness);
            var sheira = Giver(copy, I.Sheira);
            Accept(harness, Giver(copy, I.Dhimand), Leaky);
            Tick(harness, copy, 250);
            CollectionAssert.AreEqual(new[] { CrucibleSadisticSabotageV1.Report }, Topics(harness, sheira, Sabotage), "Sheira: 1445's Report");
            Assert.IsEmpty(Topics(harness, sheira, Leaky), "and not 1398's 5 (not open)");
            Talk(harness, sheira, Sabotage, CrucibleSadisticSabotageV1.Report);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, sheira, Sabotage, 1, CrucibleSadisticSabotageV1.Experience, CrucibleSadisticSabotageV1.Credits);
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Leaky, P.EscortBack), "1445's talk does nothing to 1398");

            Accept(harness, sheira, Asylum);
            Tick(harness, copy, 250);
            W.Stand(copy, I.JaikevPool);
            PickUp(harness, Kill(harness, copy, I.Jaikev));
            harness.Drain();
            var follower = SceneCreature(harness, copy, Leaky, P.DhimandRole);
            CollectionAssert.AreEqual(new[] { CrucibleLostInTheAsylumV1.FindDhimand }, Topics(harness, follower, Asylum), "1444's find at the copy");
            CollectionAssert.AreEqual(new[] { P.ReturnTools }, Topics(harness, follower, Leaky), "1398's 2 at the copy");
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, follower.EntityId, Asylum, CrucibleLostInTheAsylumV1.FindDhimand, 1),
                "1444's talk (two topics at the copy: no single hand-in)");
            harness.Drain();
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Asylum, CrucibleLostInTheAsylumV1.FindDhimand), Objective(harness, Leaky, P.ReturnTools)), "1444's talk is 1444's only");
            Assert.AreEqual(1U, Chests(harness), "the chest kept");
            Talk(harness, SceneCreature(harness, copy, Leaky, P.DhimandRole), Leaky, P.ReturnTools);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Leaky, P.ReturnTools), "1398's talk");
        }

        /// <summary>
        /// Fix job L (to-fix row 81): Dhimand's copy holds his post while the player fights the Sanctum hub's Incurables
        /// (pools 770051, 770052) 18 m off it, for 5 minutes: posed Standing (CrucibleFixLV1), within 1 m of (-40, 94.61, 386),
        /// standing idle, not fighting. Before the fix the copy (no pose, walk 2) joined that fight and stood at pool 770051's
        /// Incurables, (-24.5, 94.6, 407.4) in the probe, (-25.2, 94.5, 410.1) in the play.
        /// </summary>
        [TestMethod]
        public void DhimandsCopyHoldsHisPostThroughTheHubFight()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            C.EnterCrucible(harness);
            var copy = W.IntoWard(harness);
            Accept(harness, Giver(copy, I.Dhimand), Leaky);
            Tick(harness, copy, 250);
            var post = ToVector(I.DhimandAt);
            var follower = SceneCreature(harness, copy, Leaky, P.DhimandRole);
            Assert.AreEqual(NpcPose.Standing, follower.Pose, "the copy posed Standing as it comes");

            W.Stand(copy, 770051);
            W.Stand(copy, 770052);
            var hub = new Vector3(-30f, 94.69f, 400f);
            harness.MoveTo(hub);
            var fought = false;
            var farthest = 0f;
            for (var second = 0; second < 300; second++)
            {
                Tick(harness, copy, 1000);
                follower = SceneCreature(harness, copy, Leaky, P.DhimandRole);
                farthest = Math.Max(farthest, Vector2.Distance(new(follower.Position.X, follower.Position.Z), new(post.X, post.Z)));
                fought |= Alive(copy).Any(one => one.TargetCategory == TargetCategory.Hostile && one.SpawnPool?.DbId is 770051 or 770052 &&
                    one.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting);
            }
            Assert.IsTrue(fought, "the hub's Incurables fight the player");
            Assert.IsLessThanOrEqualTo(1f, farthest, "Dhimand's copy within 1 m of his post the whole fight");
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, follower.Controller.CurrentAction, "not fighting");
        }

        private static uint Chests(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, P.ChestTemplate);

        private static uint Counter(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].Counters[0];

        /// <summary>The player's defence flag (1398001), 0 when unset.</summary>
        private static uint Flag(WildernessRuntimeTestHarness harness) =>
            harness.Client.Player.PlayerFlags.TryGetValue(P.DefendedFlag, out var value) ? value : 0;
    }
}
