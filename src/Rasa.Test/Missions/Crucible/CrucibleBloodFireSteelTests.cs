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
using Rasa.Packets.LootDispenser.Client;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
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
    using B = CrucibleBloodFireSteelV1;
    using C = CrucibleOutpostIntrepidTests;
    using F = MarshesClientFloorTests;
    using I = CrucibleIncurablesWardNpcsV1;
    using W = CrucibleIncurablesWardTests;

    /// <summary>
    /// Crucible set 15, played through as a player plays it: Mechanic Shanul's Blood, Fire, and Steel (1401) in the
    /// Incurables Ward (squad instance 2141): Atali's and Umank's keycards from the wings, the report to Shanul, the two north
    /// doors opened with the cards (each takes its card), the Headsman and his Warden Bots woken, the Headsman's keycard, the
    /// door to Jaikev's Sanctum opened with it, the turn-in at Shanul. With it the places' guards: the bosses, their guards, the
    /// Headsman's group, the doors and the markers on the navmesh and a client floor, walked to with no climb the client fails,
    /// the doors across the only way north, the talk and use targets clear of the hostile pools and the Ward's door.
    ///
    /// The wings are walked on the local N15 rebake only: upstream's navmesh breaks on the centre room's pit stairs (N15,
    /// NOTES-upstream-bugs.md), so on it those two walks end Inconclusive (the rest of the checks run first).
    ///
    /// Why these tests: 1401 has the first keycard doors (UsableLock.KeyItemTemplateId: refused without the card, the card
    /// taken by the mission), a counter and two trackers hidden behind the client's objectives, a group the doors wake, drops
    /// to every squad member on the mission, and a talk on Shanul's package (1664) that 1444 also uses. A door opened without
    /// its card or for another player, a card kept after its door, the Headsman before both doors, a topic of one mission
    /// heard at another's objective, an open door shut again at a reload, or a turn-in that pays twice would each break it.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CrucibleBloodFireSteelTests
    {
        private const uint Ward = I.Ward;
        private const float Walker = 1.8f;
        private const uint Blood = B.MissionId, Asylum = CrucibleLostInTheAsylumV1.MissionId;

        /// <summary>The pools set 15 adds (CrucibleIncurablesWardTests.IntoWard sets them back as after their time).</summary>
        internal static readonly uint[] Pools = { B.AtaliPool, B.UmankPool, B.AtaliGuards, B.UmankGuards };

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// In the squad's copy: Atali and Umank (named public-spawn actors) and their guards (two Brutes and two Slayers a
        /// point pool) come out of their pools hostile, of their classes, levels and hit points, on the navmesh, ReachClearance
        /// and more outside the Ward's door; Shanul and the three doors ReachClearance and more outside every hostile pool's
        /// circle; the 1401 markers 5 m and more outside the door's circle; the walk north (to Paliv's body) crosses each north
        /// door's leaf and the Sanctum door's, so the doors shut the way; the Headsman's group stands on the navmesh between the
        /// north doors and the airlock. Then the walks from the arrival to Atali and Umank (Inconclusive on upstream's mesh).
        /// </summary>
        [TestMethod]
        public void TheBossesTheDoorsAndTheHeadsmanStandWhereTheyAreWalkedTo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.EnterCrucible(harness);
            var copy = W.IntoWard(harness);
            var nav = copy.NavMesh;
            var links = harness.World.Set<MapLinkEntry>().AsNoTracking().Where(link => link.Enabled != 0 && link.MapContextId == Ward).ToList();
            Assert.IsNotEmpty(links, "the Ward's door");
            float LinkGap(Vector3 at, float radius = 0) => links.Min(link =>
                Vector2.Distance(new(at.X, at.Z), new((float)link.PosX, (float)link.PosZ)) - (float)link.Radius - radius);
            var hostileKinds = new[] { I.Brute, I.Slayer, I.Jaikev, I.Parvatee, B.Atali, B.Umank };
            var hostile = harness.World.SpawnPoolEntries.AsNoTracking().Where(pool => pool.MapContextId == Ward).ToList()
                .Where(pool => hostileKinds.Contains(pool.Creature1Id)).ToList();
            Assert.HasCount(I.IncurablePools.Length + 2 + Pools.Length, hostile, "the Ward's hostile pools, set 15's four with them");
            var failures = new List<string>();
            void Clear(string label, Vector3 at)
            {
                foreach (var pool in hostile)
                    if (Vector2.Distance(new(at.X, at.Z), new((float)pool.PosX, (float)pool.PosZ)) - (float)pool.Radius < MarshesNpcsStandStillV1.ReachClearance)
                        failures.Add($"{label} in hostile pool {pool.Id}'s reach");
            }

            // The bosses and their guards.
            foreach (var pool in new[] { B.AtaliPool, B.UmankPool })
                Assert.IsTrue(SpawnPoolManager.Instance.IsNamedPublicSpawn(pool), $"pool {pool} named by 1401's scene");
            Spawn(copy, Pools);
            foreach (var (pool, creature, at) in new[] { (B.AtaliPool, B.Atali, B.AtaliAt), (B.UmankPool, B.Umank, B.UmankAt) })
            {
                var boss = Here(copy).Single(one => one.SpawnPool?.DbId == pool);
                Assert.AreEqual((creature, B.BossLevel, TargetCategory.Hostile, B.BossClass, (int)B.BossHitPoints),
                    (boss.DbId, (uint)boss.Level, boss.TargetCategory, (uint)boss.EntityClass, (int)boss.Attributes[Attributes.Health].NormalMax), $"pool {pool}");
                AssertOnGround(copy, ToVector(at), $"pool {pool}");
            }
            foreach (var (pool, at) in new[] { (B.AtaliGuards, B.AtaliGuardsAt), (B.UmankGuards, B.UmankGuardsAt) })
            {
                var here = Here(copy).Where(one => one.SpawnPool?.DbId == pool).ToArray();
                Assert.AreEqual((2, 2), (here.Count(one => one.DbId == I.Brute), here.Count(one => one.DbId == I.Slayer)), $"pool {pool}");
                AssertOnGround(copy, ToVector(at), $"pool {pool}");
            }
            foreach (var pool in Pools)
            {
                var row = hostile.Single(entry => entry.Id == pool);
                Assert.AreEqual(0.0, row.Radius, $"pool {pool}: a point");
                Assert.IsGreaterThanOrEqualTo(MarshesNpcsStandStillV1.ReachClearance, LinkGap(new((float)row.PosX, (float)row.PosY, (float)row.PosZ)),
                    $"pool {pool} clear of the door");
            }

            // Shanul and the doors clear of every hostile pool; the Headsman's group on the navmesh; the markers clear of the door.
            Clear("Shanul", ToVector(I.ShanulAt));
            foreach (var (label, at) in new[] { ("Atali's door", B.AtaliDoorAt), ("Umank's door", B.UmankDoorAt), ("the Sanctum door", B.SanctumDoorAt) })
            {
                Clear(label, ToVector(at));
                Assert.IsGreaterThanOrEqualTo(5f, LinkGap(ToVector(at)), $"{label} clear of the Ward's door");
            }
            AssertOnGround(copy, ToVector(B.HeadsmanAt), "the Headsman");
            foreach (var (at, index) in B.WardenBotsAt.Select((at, index) => (at, index)))
                AssertOnGround(copy, ToVector(at), $"Warden Bot {index + 1}");
            Assert.IsTrue(B.WardenBotsAt.Append(B.HeadsmanAt).All(at => at.Z > B.UmankDoorAt.Z + 2 && at.Z < I.AirlockSouthAt.Z),
                "the Headsman's group between the north doors and the airlock");
            foreach (var marker in harness.World.MissionIndicatorEntries.AsNoTracking()
                         .Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == Blood).ToList())
                if (LinkGap(new((float)marker.PosX, (float)marker.PosY, (float)marker.PosZ)) < 5)
                    failures.Add($"1401.{marker.ObjectiveId} marker by the Ward's door");

            // The doors shut the way north: the walk to Paliv's body crosses each door's leaf (local x +-3.52, y 0.42-6.12).
            var north = W.WalkFromArrival(nav, ToVector(CrucibleLostInTheAsylumV1.CorpseAt), true, "Paliv's body");
            foreach (var (label, door) in new[] { ("Atali's door", B.AtaliDoorAt), ("Umank's door", B.UmankDoorAt), ("the Sanctum door", B.SanctumDoorAt) })
            {
                var crossed = false;
                for (var i = 0; i + 1 < north.Count && !crossed; i++)
                {
                    var (a, b) = (north[i], north[i + 1]);
                    if ((a.Z - door.Z) * (b.Z - door.Z) > 0 || MathF.Abs(b.Z - a.Z) < 0.001f)
                        continue;
                    var at = Vector3.Lerp(a, b, (door.Z - a.Z) / (b.Z - a.Z));
                    // A walker (feet on the navmesh, 1.8 m tall) against the leaf: the navmesh's height may lie a little under the door's.
                    crossed = MathF.Abs(at.X - door.X) <= 3.52f && at.Y + Walker >= door.Y + 0.42f && at.Y <= door.Y + 6.12f;
                    if (!crossed)
                        failures.Add(FormattableString.Invariant($"the walk north passes {label}'s line at ({at.X:0.0}, {at.Y:0.0}) beside its leaf"));
                }
                if (!crossed)
                    failures.Add($"the walk north does not cross {label}");
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));

            // The wings: whole walks on the local N15 mesh; upstream's breaks on the pit stairs (N15).
            var arrival = nav.Nearest(ToVector(I.ArrivalAt)).Value;
            var broken = new List<string>();
            foreach (var (label, at) in new[] { ("Atali", B.AtaliAt), ("Umank", B.UmankAt) })
            {
                var path = nav.FindPath(arrival, nav.Nearest(ToVector(at)) ?? ToVector(at), out var whole);
                if (path == null || !whole)
                    broken.Add(label);
            }
            if (broken.Count > 0)
                Assert.Inconclusive($"the navmesh does not walk to {string.Join(" and ", broken)}: upstream's mesh (N15, the pit stairs); the local rebake walks them");
        }

        /// <summary>
        /// The client's floors (MarshesClientFloorTests; inconclusive without the client's files): Atali, Umank, their guards,
        /// the Headsman's group, before each door and the 1401 markers on a client floor with none just over them; the walks
        /// from the arrival to Shanul, the north doors, the Headsman and before the Sanctum door, and back, climbed within
        /// CrucibleGuardTests.MaxClientStep and MaxClientGrade; the walks to Atali and Umank too where the navmesh has them
        /// (the local N15 mesh; upstream's: Inconclusive after the other checks).
        /// </summary>
        [TestMethod]
        public void BloodFireSteelsPlacesStandOnAClientFloorAndAreClimbed()
        {
            const float MarkerHeight = 2;
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            C.EnterCrucible(harness);
            var copy = W.IntoWard(harness);
            var nav = copy.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var name = harness.World.MapInfoEntries.AsNoTracking().Single(row => row.Id == Ward).MapName;
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", name), meshes, 1);

            var doorsWay = new Vector3(B.AtaliDoorAt.X, B.AtaliDoorAt.Y + 0.6f, B.AtaliDoorAt.Z - 3);
            var places = new List<(string Label, Vector3 At, bool North)>
            {
                ("Atali", ToVector(B.AtaliAt), false), ("Umank", ToVector(B.UmankAt), false),
                ("Atali's guards", ToVector(B.AtaliGuardsAt), false), ("Umank's guards", ToVector(B.UmankGuardsAt), false),
                ("the Headsman", ToVector(B.HeadsmanAt), false), ("before the north doors", doorsWay, false),
                ("before the Sanctum door", ToVector(B.SanctumMarkerAt), true)
            };
            places.AddRange(B.WardenBotsAt.Select((at, index) => ($"Warden Bot {index + 1}", ToVector(at), false)));
            var (floors, ceilings) = F.Surfaces(geometry, places.Select(place => (place.At, F.Walkable)).ToList());
            var failures = new List<string>();
            for (var i = 0; i < places.Count; i++)
                if (places[i].At.Y - floors[i] > F.MaxOver || !float.IsPositiveInfinity(ceilings[i]))
                    failures.Add($"{places[i].Label} at {places[i].At}: floor {floors[i]}, ceiling {ceilings[i]}");

            var markers = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == Blood).ToList()
                .Select(marker => (Label: $"1401.{marker.ObjectiveId}/{marker.IndicatorId}", At: new Vector3((float)marker.PosX, (float)marker.PosY, (float)marker.PosZ)))
                .ToList();
            Assert.HasCount(6, markers, "1401's markers");
            var (markerFloors, _) = F.Surfaces(geometry, markers.Select(marker => (marker.At + new Vector3(0, MarkerHeight - F.MaxUnder, 0), F.Walkable)).ToList());
            for (var i = 0; i < markers.Count; i++)
            {
                var ground = nav.GroundHeight(markers[i].At + new Vector3(0, MarkerHeight, 0));
                if (ground is not { } y || MathF.Abs(y - markers[i].At.Y) > MarkerHeight)
                    failures.Add($"{markers[i].Label}: navmesh {ground}");
                if (markers[i].At.Y - markerFloors[i] > MarkerHeight)
                    failures.Add($"{markers[i].Label}: client floor {markerFloors[i]}");
            }

            void Climbed(string what, List<Vector3> walk)
            {
                var (step, stepAt, grade, gradeAt) = CrucibleGuardTests.ClientClimb(nav, geometry, walk);
                if (step > CrucibleGuardTests.MaxClientStep)
                    failures.Add(FormattableString.Invariant($"{what}: a {step:0.00} m step at ({stepAt.X:0.0}, {stepAt.Y:0.0}, {stepAt.Z:0.0})"));
                if (grade > CrucibleGuardTests.MaxClientGrade)
                    failures.Add(FormattableString.Invariant($"{what}: {grade:0.00} a metre over 12 m from ({gradeAt.X:0.0}, {gradeAt.Y:0.0}, {gradeAt.Z:0.0})"));
            }
            foreach (var (label, at, north) in places.Where(place => place.Label is "the Headsman" or "before the north doors" or "before the Sanctum door")
                         .Append(("Shanul", ToVector(I.ShanulAt), false)))
            {
                Climbed($"the arrival to {label}", W.WalkFromArrival(nav, at, north, label));
                Climbed($"{label} to the arrival", W.WalkToArrival(nav, at, north, label));
            }
            var arrival = nav.Nearest(ToVector(I.ArrivalAt)).Value;
            var broken = new List<string>();
            foreach (var (label, at) in new[] { ("Atali", ToVector(B.AtaliAt)), ("Umank", ToVector(B.UmankAt)) })
            {
                var path = nav.FindPath(arrival, nav.Nearest(at) ?? at, out var whole);
                if (path == null || !whole)
                {
                    broken.Add(label);
                    continue;
                }
                Climbed($"the arrival to {label}", path.Prepend(arrival).ToList());
                var end = nav.Nearest(at).Value;
                var back = nav.FindPath(end, arrival, out var backWhole);
                Assert.IsTrue(back != null && backWhole, $"{label} back to the arrival");
                Climbed($"{label} to the arrival", back.Prepend(end).ToList());
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
            if (broken.Count > 0)
                Assert.Inconclusive($"the navmesh does not walk to {string.Join(" and ", broken)}: upstream's mesh (N15); the local rebake walks them");
        }

        /// <summary>
        /// Blood, Fire, and Steel: Shanul offers it in the squad's Ward (no prerequisite); at the accept the player's own three
        /// doors stand closed, locked to their cards, not shown to another player; a door used without its card is refused
        /// (PmMissingReqItem); Atali's and Umank's kills drop their cards (a death is not a pickup); a north door used with its card
        /// before 4's talk is refused (5 not open) and keeps the card; the cards complete 1 and 2, hidden 3 and 7 open 4, a
        /// reload keeps them; Shanul hears 4 (1401's, not 1444's); the talk opens 5; Atali's door opens with its card, takes it,
        /// is used no more, stays open at a reload, and wakes nothing alone; Umank's door the same, and the Headsman and his four
        /// Warden Bots wake hostile; the Sanctum door is refused before 6; the Headsman's kill drops his card, the pickup opens 6;
        /// the Sanctum door opens and takes the card, 6 done; the turn-in at Shanul pays once; the doors go with the scene.
        /// </summary>
        [TestMethod]
        public void BloodFireSteelOpensTheWayToJaikevsSanctum()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 99 : minimum);
            C.EnterCrucible(harness);
            var copy = W.IntoWard(harness);
            var shanul = Giver(copy, I.Shanul);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, shanul, Blood), "Shanul offers 1401 (no prerequisite)");
            W.Stand(copy, B.AtaliPool);
            Assert.IsFalse(Holds(Kill(harness, copy, B.Atali), B.AtaliCardTemplate), "no card before the mission");

            Accept(harness, shanul, Blood);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive, MissionObjectiveState.Inactive,
                    MissionObjectiveState.Inactive),
                (Objective(harness, Blood, B.AtaliCard), Objective(harness, Blood, B.UmankCard), Objective(harness, Blood, B.ReportToShanul),
                    Objective(harness, Blood, B.HeadsmanCard), Objective(harness, Blood, B.SanctumDoor)));
            var visible = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Blood].ObjectivesList.Select(objective => objective.ObjectiveId).ToArray();
            foreach (var hidden in new[] { B.Counter, B.CardsOpener, B.AtaliDoorUsed, B.UmankDoorUsed, B.DoorsTracker, B.DoorsOpener })
                Assert.IsFalse(visible.Contains(hidden), $"{hidden} is hidden");
            Assert.AreEqual((MissionObjectiveState.Inactive, MissionObjectiveState.Inactive),
                (Objective(harness, Blood, B.AtaliDoorUsed), Objective(harness, Blood, B.UmankDoorUsed)), "the north doors' gates shut until the talk");

            foreach (var (role, at, key) in Doors)
            {
                var door = SceneObject(harness, copy, Blood, role);
                Assert.AreEqual(((uint)B.DoorClass, UseObjectState.DoorStateClosed, key), ((uint)door.EntityClassId, door.StateId, door.Lock?.KeyItemTemplateId ?? 0), role);
                Assert.AreEqual(UseObjectState.DoorStateClosed, door.Lock.LockStateId, $"{role}: locked while closed");
                Assert.AreEqual(ToVector(at), door.Position, role);
                Assert.AreEqual(harness.Client.Player.Id, door.SceneOwnerCharacterId, $"{role}: the player's own");
            }
            var other = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            try
            {
                Assert.IsFalse(DynamicObjectManager.IsShownTo(other, SceneObject(harness, copy, Blood, B.AtaliDoorRole)), "another player is not shown the door");
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(other);
                other.Player.MapChannel?.ClientList.Remove(other);
            }

            // No card: refused.
            Assert.AreEqual(PlayerMessage.PmMissingReqItem, Refused(harness, copy, SceneObject(harness, copy, Blood, B.AtaliDoorRole)), "no card: refused");

            // 1, 2: the cards (chance 100: the highest roll drops them too).
            W.Stand(copy, B.AtaliPool);
            var atali = Kill(harness, copy, B.Atali);
            Assert.IsTrue(Holds(atali, B.AtaliCardTemplate), "Atali drops her card");
            Assert.AreEqual(0U, Held(harness, B.AtaliCardTemplate), "a death is not a pickup");
            PickUp(harness, atali);
            harness.Drain();
            Assert.AreEqual((1U, MissionObjectiveState.Completed), (Held(harness, B.AtaliCardTemplate), Objective(harness, Blood, B.AtaliCard)));
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, Refused(harness, copy, SceneObject(harness, copy, Blood, B.AtaliDoorRole)),
                "the card, but before the talk at Shanul (8 not open): refused");
            Assert.AreEqual((1U, UseObjectState.DoorStateClosed), (Held(harness, B.AtaliCardTemplate), SceneObject(harness, copy, Blood, B.AtaliDoorRole).StateId),
                "the card kept, the door shut");
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Blood, B.ReportToShanul), "one card does not open 4");
            W.Stand(copy, B.UmankPool);
            PickUp(harness, Kill(harness, copy, B.Umank));
            harness.Drain();
            Tick(harness, copy, 250);
            Assert.AreEqual((1U, MissionObjectiveState.Completed, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Held(harness, B.UmankCardTemplate), Objective(harness, Blood, B.UmankCard), Objective(harness, Blood, B.Counter),
                    Objective(harness, Blood, B.ReportToShanul)), "both cards: 3 done, 4 open");
            W.Stand(copy, B.AtaliPool);
            Assert.IsFalse(Holds(Kill(harness, copy, B.Atali), B.AtaliCardTemplate), "1 done: no second card");
            ReloadAndRecover(harness, copy);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Blood, B.ReportToShanul), "kept at a reload");

            // 4: Shanul.
            shanul = Giver(copy, I.Shanul);
            CollectionAssert.AreEqual(new[] { B.ReportToShanul }, Topics(harness, shanul, Blood), "Shanul hears 4");
            Assert.IsEmpty(Topics(harness, shanul, Asylum), "and nothing of 1444 (not held)");
            Talk(harness, shanul, Blood, B.ReportToShanul);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete),
                (Objective(harness, Blood, B.HeadsmanCard), Objective(harness, Blood, B.AtaliDoorUsed), Objective(harness, Blood, B.UmankDoorUsed)),
                "4 opens 5 and the north doors' 8 and 9");

            // Atali's door: opens, takes the card, used no more, open at a reload; alone it wakes nothing.
            Use(harness, copy, SceneObject(harness, copy, Blood, B.AtaliDoorRole), B.UseArgument);
            Tick(harness, copy, 250);
            var ataliDoor = SceneObject(harness, copy, Blood, B.AtaliDoorRole);
            Assert.AreEqual((UseObjectState.DoorStateOpen, false), (ataliDoor.StateId, ataliDoor.IsEnabled), "Atali's door open, used no more");
            Assert.AreEqual((0U, MissionObjectiveState.Completed), (Held(harness, B.AtaliCardTemplate), Objective(harness, Blood, B.AtaliDoorUsed)), "the card taken");
            Assert.AreEqual(1U, Held(harness, B.UmankCardTemplate), "Umank's card kept");
            Assert.IsNull(SceneCreature(harness, copy, Blood, B.HeadsmanRole), "one door wakes nothing");
            ReloadAndRecover(harness, copy);
            Assert.AreEqual(UseObjectState.DoorStateOpen, SceneObject(harness, copy, Blood, B.AtaliDoorRole).StateId, "open at a reload");
            Assert.AreEqual(UseObjectState.DoorStateClosed, SceneObject(harness, copy, Blood, B.UmankDoorRole).StateId, "Umank's still shut");
            Restart(harness, copy);
            ataliDoor = SceneObject(harness, copy, Blood, B.AtaliDoorRole);
            Assert.AreEqual((UseObjectState.DoorStateOpen, false), (ataliDoor.StateId, ataliDoor.IsEnabled), "open, used no more, at a restart (the card is gone)");
            Assert.AreEqual(UseObjectState.DoorStateClosed, SceneObject(harness, copy, Blood, B.UmankDoorRole).StateId, "Umank's still shut at a restart");

            // Umank's door: the Headsman and his bots wake.
            Use(harness, copy, SceneObject(harness, copy, Blood, B.UmankDoorRole), B.UseArgument);
            Tick(harness, copy, 250);
            Tick(harness, copy, 250);
            Assert.AreEqual(UseObjectState.DoorStateOpen, SceneObject(harness, copy, Blood, B.UmankDoorRole).StateId);
            Assert.AreEqual((0U, MissionObjectiveState.Completed, MissionObjectiveState.Completed),
                (Held(harness, B.UmankCardTemplate), Objective(harness, Blood, B.DoorsTracker), Objective(harness, Blood, B.DoorsOpener)), "both doors: the trackers done");
            var headsman = SceneCreature(harness, copy, Blood, B.HeadsmanRole);
            Assert.IsNotNull(headsman, "the Headsman wakes");
            Assert.AreEqual((B.Headsman, B.HeadsmanClass, B.BossLevel, TargetCategory.Hostile, (int)B.BossHitPoints),
                (headsman.DbId, (uint)headsman.EntityClass, (uint)headsman.Level, headsman.TargetCategory, (int)headsman.Attributes[Attributes.Health].NormalMax));
            for (var index = 0; index < B.WardenBotsAt.Length; index++)
            {
                var bot = SceneCreature(harness, copy, Blood, B.WardenBotRole(index));
                Assert.IsNotNull(bot, $"Warden Bot {index + 1} wakes");
                Assert.AreEqual((B.WardenBot, TargetCategory.Hostile), (bot.DbId, bot.TargetCategory), $"Warden Bot {index + 1}");
            }
            Assert.AreEqual(PlayerMessage.PmMissingReqItem, Refused(harness, copy, SceneObject(harness, copy, Blood, B.SanctumDoorRole)),
                "the Sanctum door before the Headsman's card: refused");
            ReloadAndRecover(harness, copy);
            Assert.IsNotNull(SceneCreature(harness, copy, Blood, B.HeadsmanRole), "the Headsman kept at a reload");
            Restart(harness, copy);
            foreach (var role in new[] { B.AtaliDoorRole, B.UmankDoorRole })
                Assert.AreEqual((UseObjectState.DoorStateOpen, false), (SceneObject(harness, copy, Blood, role).StateId, SceneObject(harness, copy, Blood, role).IsEnabled),
                    $"{role}: open, used no more, at a restart");
            Assert.AreEqual(UseObjectState.DoorStateClosed, SceneObject(harness, copy, Blood, B.SanctumDoorRole).StateId, "the Sanctum door shut at a restart");

            // 5: the Headsman's card.
            for (var index = 0; index < B.WardenBotsAt.Length; index++)
                MarshesPreoDasTests.Kill(harness, copy, SceneCreature(harness, copy, Blood, B.WardenBotRole(index)));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Blood, B.HeadsmanCard), "the bots carry no card");
            var card = KillScene(harness, copy, SceneCreature(harness, copy, Blood, B.HeadsmanRole));
            Assert.IsTrue(Holds(card, B.HeadsmanCardTemplate), "the Headsman drops his card");
            PickUp(harness, card);
            harness.Drain();
            Tick(harness, copy, 250);
            Assert.AreEqual((1U, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Held(harness, B.HeadsmanCardTemplate), Objective(harness, Blood, B.HeadsmanCard), Objective(harness, Blood, B.SanctumDoor)), "5 done, 6 open");

            // 6: the Sanctum door.
            Use(harness, copy, SceneObject(harness, copy, Blood, B.SanctumDoorRole), B.UseArgument);
            var packets = harness.Drain();
            Tick(harness, copy, 250);
            Assert.AreEqual(UseObjectState.DoorStateOpen, SceneObject(harness, copy, Blood, B.SanctumDoorRole).StateId, "the Sanctum door open");
            Assert.AreEqual((0U, MissionObjectiveState.Completed), (Held(harness, B.HeadsmanCardTemplate), Objective(harness, Blood, B.SanctumDoor)), "the card taken, 6 done");
            Assert.IsTrue(harness.Client.Player.Missions[Blood].Completeable, "to be turned in");

            // The turn-in at Shanul.
            shanul = Giver(copy, I.Shanul);
            FailedTurnInPaysNothing(harness, shanul, Blood, null);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, shanul, Blood, null, B.Experience, B.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Blood), "MissionRewarded once");
            for (var tick = 0; tick < 4; tick++)        // RetireRewardedScene retries: up to 1 s
                Tick(harness, copy, 250);
            foreach (var (role, _, _) in Doors)
                Assert.IsNull(FindSceneObject(harness, copy, Blood, role), $"{role} goes with the mission");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Giver(copy, I.Shanul), Blood), "done once");
        }

        /// <summary>
        /// The cards go to every squad member on 1401 (squad instance, MissionLootSharers): with a second member on 1401 beside
        /// the player, Atali's kill by the player leaves each their own card, and the member's own pickup completes the member's 1;
        /// the member cannot use the player's door (not shown, refused) and it stays shut.
        /// </summary>
        [TestMethod]
        public void BloodFireSteelsCardsDropToEverySquadMemberOnIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            C.EnterCrucible(harness);
            var copy = W.IntoWard(harness);
            Assert.IsTrue(copy.IsSquadInstance, "a squad's copy");
            var shanul = Giver(copy, I.Shanul);
            Accept(harness, shanul, Blood);
            Tick(harness, copy, 250);
            W.Stand(copy, B.AtaliPool);
            var atali = Here(copy).Single(one => one.SpawnPool?.DbId == B.AtaliPool && one.State != CharacterState.Dead);

            var member = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            member.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);   // alive: a corpse is looted by the living
            var partyId = PartyManager.Instance.GetPartyId;
            var party = new Party(partyId, harness.Client.AccountEntry.Id, new List<PartyMember> { new(harness.Client), new(member) })
            {
                LootMethod = PartyLootMethod.FreeForAll, LootThreshold = PartyLootThreshold.Prototype
            };
            try
            {
                CellManager.Instance.RemoveFromWorld(member);
                member.Player.MapChannel?.ClientList.Remove(member);
                member.Player.MapChannel = copy;
                member.Player.RuntimeMapChannel = copy;
                member.Player.MapContextId = Ward;
                copy.ClientList.Add(member);
                member.SetWorldPosition(shanul.Position + new Vector3(0, 0, 1), 0);
                CellManager.Instance.AddToWorld(member);
                PartyManager.Instance.Parties.Add(partyId, party);
                harness.Client.Player.PartyId = partyId;
                member.Player.PartyId = partyId;
                lock (Server.Clients)
                    Server.Clients.Add(member);
                Assert.IsTrue(harness.Manager.AcceptOfferedMission(member, shanul.EntityId, Blood), "the member takes 1401");
                harness.Drain();

                var door = SceneObject(harness, copy, Blood, B.AtaliDoorRole);
                Assert.IsFalse(DynamicObjectManager.IsShownTo(member, door), "the player's door is not shown to the member");

                harness.MoveTo(atali.Position);
                member.SetWorldPosition(atali.Position + new Vector3(0, 0, 3), 0);
                harness.Drain();
                atali.Attributes[Attributes.Health].Current = 0;
                harness.Creatures.HandleCreatureKill(copy, atali, harness.Client.Player);
                Assert.IsTrue(copy.LootDispensers.TryGetValue(atali.CorpseLootEntityId, out var loot), "a corpse");
                var cards = loot.LootItems.Where(item => item.ItemTemplateId == B.AtaliCardTemplate).ToArray();
                Assert.AreEqual(1, cards.Count(item => item.ReservedFor == harness.Client.Player.EntityId), "the player's own card");
                Assert.AreEqual(1, cards.Count(item => item.ReservedFor == member.Player.EntityId), "the member's own card");
                LootDispenserManager.Instance.RequestLootAllFromCorpse(member, new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
                harness.Drain();
                Assert.AreEqual((1U, MissionObjectiveState.Completed), (HeldBy(harness, member, B.AtaliCardTemplate),
                    member.Player.Missions[Blood].Objectives[B.AtaliCard].State), "the member's card and 1");
                Assert.AreEqual(0U, Held(harness, B.AtaliCardTemplate), "the player's waits for the player's pickup");

                harness.Objects.RequestUseObjectPacket(member, new RequestUseObjectPacket
                {
                    EntityId = door.EntityId, ActionId = ActionId.UseObject, ActionArgId = B.UseArgument
                });
                harness.Drain();
                Assert.AreEqual(UseObjectState.DoorStateClosed, door.StateId, "the member cannot open the player's door");
                PickUp(harness, loot);
                harness.Drain();
                Assert.AreEqual((1U, MissionObjectiveState.Completed), (Held(harness, B.AtaliCardTemplate), Objective(harness, Blood, B.AtaliCard)), "the player's own card");
            }
            finally
            {
                harness.Manager.Scenes.Detach(member, copy);
                lock (Server.Clients)
                    Server.Clients.Remove(member);
                harness.Client.Player.PartyId = 0;
                member.Player.PartyId = 0;
                PartyManager.Instance.Parties.Remove(partyId);
                PartyManager.Instance.FreePartyId(partyId);
                CellManager.Instance.RemoveFromWorld(member);
                copy.ClientList.Remove(member);
            }
        }

        /// <summary>
        /// An abandon after Atali's door: the doors go with the scene, Umank's card is kept (Retain), and taken again the held
        /// card counts at once (2 Completed) while 1 is open again (the door took Atali's card); the new doors stand shut.
        /// </summary>
        [TestMethod]
        public void BloodFireSteelAbandonedStartsWithTheCardsHeld()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            C.EnterCrucible(harness);
            var copy = W.IntoWard(harness);
            Accept(harness, Giver(copy, I.Shanul), Blood);
            Tick(harness, copy, 250);
            foreach (var (pool, kind) in new[] { (B.AtaliPool, B.Atali), (B.UmankPool, B.Umank) })
            {
                W.Stand(copy, pool);
                PickUp(harness, Kill(harness, copy, kind));
                harness.Drain();
            }
            Tick(harness, copy, 250);
            Talk(harness, Giver(copy, I.Shanul), Blood, B.ReportToShanul);
            Use(harness, copy, SceneObject(harness, copy, Blood, B.AtaliDoorRole), B.UseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual((0U, 1U), (Held(harness, B.AtaliCardTemplate), Held(harness, B.UmankCardTemplate)), "Atali's card taken by her door");

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Blood));
            harness.Drain();
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            foreach (var (role, _, _) in Doors)
                Assert.IsNull(FindSceneObject(harness, copy, Blood, role), $"{role} goes with the abandon");
            Assert.IsFalse(copy.SpawnPools.Any(pool => pool.SceneRunId != null && pool.ScenarioMissionId == Blood), "no 1401 scene pool left");
            Assert.AreEqual(1U, Held(harness, B.UmankCardTemplate), "Umank's card kept at the abandon");

            Accept(harness, Giver(copy, I.Shanul), Blood);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Completed, MissionObjectiveState.Inactive),
                (Objective(harness, Blood, B.AtaliCard), Objective(harness, Blood, B.UmankCard), Objective(harness, Blood, B.ReportToShanul)),
                "the held card counts at the accept; Atali's again");
            foreach (var (role, _, _) in Doors)
                Assert.AreEqual(UseObjectState.DoorStateClosed, SceneObject(harness, copy, Blood, role).StateId, $"{role}: a new one, shut");
        }

        /// <summary>
        /// The doors are gated each on its own objective (8, 9), not on 5 (Fable review 2026-11-08): a Headsman card kept from
        /// an abandoned run completes 5 at the talk (the drop's held-item fact), and the north doors still open. The first run
        /// gets as far as the Headsman's card (both doors open, their cards taken); the abandon keeps the card (Retain); taken
        /// again, Atali's and Umank's cards are won again, the talk completes 4 and 5 at once and opens 6 and the doors' 8 and 9;
        /// both north doors open with their cards, the Sanctum door with the kept card, and the turn-in pays.
        /// </summary>
        [TestMethod]
        public void BloodFireSteelWithAKeptHeadsmansCardStillOpensTheNorthDoors()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            C.EnterCrucible(harness);
            var copy = W.IntoWard(harness);
            Accept(harness, Giver(copy, I.Shanul), Blood);
            Tick(harness, copy, 250);
            WinTheCards(harness, copy);
            Talk(harness, Giver(copy, I.Shanul), Blood, B.ReportToShanul);
            Tick(harness, copy, 250);
            foreach (var role in new[] { B.AtaliDoorRole, B.UmankDoorRole })
            {
                Use(harness, copy, SceneObject(harness, copy, Blood, role), B.UseArgument);
                Tick(harness, copy, 250);
            }
            Tick(harness, copy, 250);
            var card = KillScene(harness, copy, SceneCreature(harness, copy, Blood, B.HeadsmanRole));
            PickUp(harness, card);
            harness.Drain();
            Tick(harness, copy, 250);
            Assert.AreEqual((1U, MissionObjectiveState.Completed), (Held(harness, B.HeadsmanCardTemplate), Objective(harness, Blood, B.HeadsmanCard)), "the first run's 5");

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Blood));
            harness.Drain();
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            Assert.AreEqual((0U, 0U, 1U), (Held(harness, B.AtaliCardTemplate), Held(harness, B.UmankCardTemplate), Held(harness, B.HeadsmanCardTemplate)),
                "the doors took Atali's and Umank's cards; the Headsman's kept at the abandon");

            Accept(harness, Giver(copy, I.Shanul), Blood);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Objective(harness, Blood, B.AtaliCard), Objective(harness, Blood, B.UmankCard), Objective(harness, Blood, B.HeadsmanCard)));
            WinTheCards(harness, copy);
            Talk(harness, Giver(copy, I.Shanul), Blood, B.ReportToShanul);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete),
                (Objective(harness, Blood, B.HeadsmanCard), Objective(harness, Blood, B.SanctumDoor), Objective(harness, Blood, B.AtaliDoorUsed),
                    Objective(harness, Blood, B.UmankDoorUsed)), "the kept card completes 5 at the talk; 6 and the doors' 8 and 9 open");

            foreach (var (role, template, objective) in new[] { (B.AtaliDoorRole, B.AtaliCardTemplate, B.AtaliDoorUsed), (B.UmankDoorRole, B.UmankCardTemplate, B.UmankDoorUsed) })
            {
                Use(harness, copy, SceneObject(harness, copy, Blood, role), B.UseArgument);
                Tick(harness, copy, 250);
                Assert.AreEqual((UseObjectState.DoorStateOpen, 0U, MissionObjectiveState.Completed),
                    (SceneObject(harness, copy, Blood, role).StateId, Held(harness, template), Objective(harness, Blood, objective)), $"{role} opens with 5 done");
            }
            Use(harness, copy, SceneObject(harness, copy, Blood, B.SanctumDoorRole), B.UseArgument);
            harness.Drain();
            Tick(harness, copy, 250);
            Assert.AreEqual((UseObjectState.DoorStateOpen, 0U, MissionObjectiveState.Completed),
                (SceneObject(harness, copy, Blood, B.SanctumDoorRole).StateId, Held(harness, B.HeadsmanCardTemplate), Objective(harness, Blood, B.SanctumDoor)),
                "the Sanctum door opens with the kept card");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, Giver(copy, I.Shanul), Blood, null, B.Experience, B.Credits);
        }

        /// <summary>
        /// A squad mate's 5 completed by the player's Headsman kill (the card drops to every member on 1401 within reach,
        /// MissionLootSharers) before the mate opened a door (Fable review 2026-11-08): the player talks at Shanul and opens the
        /// player's doors while the mate's gates are shut (the mate has not talked: the doors' events do not count for the
        /// mate); the mate talks; the player's Headsman kill gives the mate a card (the mate's 5 done); the mate's own north
        /// doors still open with the mate's cards (gated on the mate's 8 and 9, not 5), and the mate's Sanctum door with the
        /// mate's Headsman card.
        /// </summary>
        [TestMethod]
        public void BloodFireSteelsSquadMateKeepsTheirDoorsAfterThePlayersHeadsmanKill()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            C.EnterCrucible(harness);
            var copy = W.IntoWard(harness);
            var shanul = Giver(copy, I.Shanul);
            Accept(harness, shanul, Blood);
            Tick(harness, copy, 250);
            using var mate = new SquadMate(harness, copy, shanul);
            mate.WinTheCardsWithThePlayer();

            Talk(harness, shanul, Blood, B.ReportToShanul);
            Tick(harness, copy, 250);
            foreach (var role in new[] { B.AtaliDoorRole, B.UmankDoorRole })
            {
                Use(harness, copy, SceneObject(harness, copy, Blood, role), B.UseArgument);
                Tick(harness, copy, 250);
                Assert.AreEqual(UseObjectState.DoorStateOpen, SceneObject(harness, copy, Blood, role).StateId, $"the player's {role} opens");
            }
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Inactive, MissionObjectiveState.Inactive, 1U, 1U, UseObjectState.DoorStateClosed, UseObjectState.DoorStateClosed),
                (mate.Of(B.AtaliDoorUsed), mate.Of(B.UmankDoorUsed), mate.Held(B.AtaliCardTemplate), mate.Held(B.UmankCardTemplate),
                    mate.Door(B.AtaliDoorRole).StateId, mate.Door(B.UmankDoorRole).StateId),
                "the mate's gates shut (no talk yet): the player's doors take nothing of the mate's");

            mate.Talk();
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete),
                (mate.Of(B.HeadsmanCard), mate.Of(B.AtaliDoorUsed), mate.Of(B.UmankDoorUsed)), "the mate's 5, 8 and 9 open");
            var headsman = SceneCreature(harness, copy, Blood, B.HeadsmanRole);
            Assert.IsNotNull(headsman, "the player's Headsman wakes");
            mate.Beside(headsman.Position + new Vector3(0, 0, 3));
            mate.Loot(KillScene(harness, copy, headsman));
            Tick(harness, copy, 250);
            Assert.AreEqual((1U, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (mate.Held(B.HeadsmanCardTemplate), mate.Of(B.HeadsmanCard), mate.Of(B.SanctumDoor)),
                "the player's Headsman kill gives the mate a card: the mate's 5 done before any door of theirs");

            foreach (var (role, template, objective) in new[] { (B.AtaliDoorRole, B.AtaliCardTemplate, B.AtaliDoorUsed), (B.UmankDoorRole, B.UmankCardTemplate, B.UmankDoorUsed) })
            {
                Assert.AreEqual(UseObjectState.DoorStateClosed, mate.Door(role).StateId, $"the mate's {role} shut");
                mate.Use(mate.Door(role));
                Assert.AreEqual((UseObjectState.DoorStateOpen, false, 0U, MissionObjectiveState.Completed),
                    (mate.Door(role).StateId, mate.Door(role).IsEnabled, mate.Held(template), mate.Of(objective)), $"the mate's {role} opens with their 5 done");
            }
            mate.Use(mate.Door(B.SanctumDoorRole));
            Assert.AreEqual((UseObjectState.DoorStateOpen, 0U, MissionObjectiveState.Completed),
                (mate.Door(B.SanctumDoorRole).StateId, mate.Held(B.HeadsmanCardTemplate), mate.Of(B.SanctumDoor)), "the mate's Sanctum door");
        }

        /// <summary>
        /// In the squad's instance a door's use is a scene event every member on 1401 shares (GroupCreditService.
        /// SharedInSquadInstance): with both through the talk, the player's use of Atali's door takes both members' Atali
        /// cards and opens both members' own Atali doors (each one's 8 starts each one's opening), so no member is left with
        /// the card gone and the door shut; the mate's use of Umank's door the same for both; both Headsman groups wake. Both
        /// Headsmen dead and a card each: the player's use of the Sanctum door takes both cards and opens both members' Sanctum
        /// doors (anywhere in the copy, not only near), and the mate's 1401 is completeable.
        /// </summary>
        [TestMethod]
        public void BloodFireSteelsDoorOpensForEverySquadMemberWhoseGateIsOpen()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            C.EnterCrucible(harness);
            var copy = W.IntoWard(harness);
            var shanul = Giver(copy, I.Shanul);
            Accept(harness, shanul, Blood);
            Tick(harness, copy, 250);
            using var mate = new SquadMate(harness, copy, shanul);
            mate.WinTheCardsWithThePlayer();
            Talk(harness, shanul, Blood, B.ReportToShanul);
            mate.Talk();

            Use(harness, copy, SceneObject(harness, copy, Blood, B.AtaliDoorRole), B.UseArgument);
            for (var tick = 0; tick < 2; tick++)
                Tick(harness, copy, 250);
            Assert.AreEqual((UseObjectState.DoorStateOpen, false, 0U, MissionObjectiveState.Completed),
                (SceneObject(harness, copy, Blood, B.AtaliDoorRole).StateId, SceneObject(harness, copy, Blood, B.AtaliDoorRole).IsEnabled,
                    Held(harness, B.AtaliCardTemplate), Objective(harness, Blood, B.AtaliDoorUsed)), "the player's");
            Assert.AreEqual((UseObjectState.DoorStateOpen, false, 0U, MissionObjectiveState.Completed),
                (mate.Door(B.AtaliDoorRole).StateId, mate.Door(B.AtaliDoorRole).IsEnabled, mate.Held(B.AtaliCardTemplate), mate.Of(B.AtaliDoorUsed)),
                "the mate's Atali door opens with the player's use, the mate's card taken");
            Assert.AreEqual(UseObjectState.DoorStateClosed, mate.Door(B.UmankDoorRole).StateId, "the mate's Umank door still shut");

            mate.Use(mate.Door(B.UmankDoorRole));
            Tick(harness, copy, 250);
            Assert.AreEqual((UseObjectState.DoorStateOpen, 0U, MissionObjectiveState.Completed),
                (mate.Door(B.UmankDoorRole).StateId, mate.Held(B.UmankCardTemplate), mate.Of(B.UmankDoorUsed)), "the mate's");
            Assert.AreEqual((UseObjectState.DoorStateOpen, 0U, MissionObjectiveState.Completed),
                (SceneObject(harness, copy, Blood, B.UmankDoorRole).StateId, Held(harness, B.UmankCardTemplate), Objective(harness, Blood, B.UmankDoorUsed)),
                "the player's Umank door opens with the mate's use");
            Assert.IsNotNull(SceneCreature(harness, copy, Blood, B.HeadsmanRole), "the player's Headsman wakes");
            Assert.AreEqual(1, mate.Headsmen(), "and the mate's (each member's own group)");

            var headsman = SceneCreature(harness, copy, Blood, B.HeadsmanRole);
            mate.Beside(headsman.Position + new Vector3(0, 0, 3));
            var card = KillScene(harness, copy, headsman);
            mate.Loot(card);
            PickUp(harness, card);
            harness.Drain();
            mate.Loot(mate.KillHeadsman());
            Tick(harness, copy, 250);
            Assert.AreEqual((0, 1U, 1U, MissionObjectiveState.Completed, MissionObjectiveState.Completed),
                (mate.Headsmen(), Held(harness, B.HeadsmanCardTemplate), mate.Held(B.HeadsmanCardTemplate), Objective(harness, Blood, B.HeadsmanCard),
                    mate.Of(B.HeadsmanCard)), "both Headsmen dead, a card each");

            Use(harness, copy, SceneObject(harness, copy, Blood, B.SanctumDoorRole), B.UseArgument);
            harness.Drain();
            for (var tick = 0; tick < 2; tick++)
                Tick(harness, copy, 250);
            Assert.AreEqual((UseObjectState.DoorStateOpen, 0U, MissionObjectiveState.Completed),
                (SceneObject(harness, copy, Blood, B.SanctumDoorRole).StateId, Held(harness, B.HeadsmanCardTemplate), Objective(harness, Blood, B.SanctumDoor)),
                "the player's Sanctum door");
            Assert.AreEqual((UseObjectState.DoorStateOpen, 0U, MissionObjectiveState.Completed, true),
                (mate.Door(B.SanctumDoorRole).StateId, mate.Held(B.HeadsmanCardTemplate), mate.Of(B.SanctumDoor), mate.Client.Player.Missions[Blood].Completeable),
                "the mate's Sanctum door opens with the player's use, the mate's card taken, the mate's 1401 to be turned in");
        }

        /// <summary>
        /// A second member of the player's squad, in the squad's Ward on 1401 beside the player: alive, with the stats a share of a
        /// kill's experience updates (LoadingScreenDisconnectTests), resumed as one who came in by its door. Disposed: out of
        /// the squad and the copy.
        /// </summary>
        private sealed class SquadMate : IDisposable
        {
            private readonly WildernessRuntimeTestHarness _harness;
            private readonly MapChannel _copy;
            private readonly Creature _shanul;
            private readonly uint _partyId;
            internal Client Client { get; }

            internal SquadMate(WildernessRuntimeTestHarness harness, MapChannel copy, Creature shanul)
            {
                (_harness, _copy, _shanul) = (harness, copy, shanul);
                // The squad's copy outlives a test: an earlier test's client of the player's character would take the
                // player's frozen credit (GroupCreditService.Tick finds the first client of the character on the map).
                copy.ClientList.RemoveAll(client => !ReferenceEquals(client, harness.Client) && client.Player?.Id == harness.Client.Player.Id);
                var member = Client = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
                foreach (var attribute in new[] { Attributes.Body, Attributes.Mind, Attributes.Spirit, Attributes.Chi, Attributes.Aware, Attributes.Speed, Attributes.Regen })
                    member.Player.Attributes[attribute] = new ActorAttributes(attribute, 10, 10, 10, 0, 0);
                member.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
                member.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 100, 100, 100, 0, 0);
                member.Player.Attributes[Attributes.Power] = new ActorAttributes(Attributes.Power, 50, 50, 50, 0, 0);
                _partyId = PartyManager.Instance.GetPartyId;
                var party = new Party(_partyId, harness.Client.AccountEntry.Id, new List<PartyMember> { new(harness.Client), new(member) })
                {
                    LootMethod = PartyLootMethod.FreeForAll, LootThreshold = PartyLootThreshold.Prototype
                };
                CellManager.Instance.RemoveFromWorld(member);
                member.Player.MapChannel?.ClientList.Remove(member);
                member.Player.MapChannel = copy;
                member.Player.RuntimeMapChannel = copy;
                member.Player.MapContextId = Ward;
                copy.ClientList.Add(member);
                member.SetWorldPosition(shanul.Position + new Vector3(0, 0, 1), 0);
                CellManager.Instance.AddToWorld(member);
                PartyManager.Instance.Parties.Add(_partyId, party);
                harness.Client.Player.PartyId = _partyId;
                member.Player.PartyId = _partyId;
                lock (Server.Clients)
                    Server.Clients.Add(member);
                harness.Manager.Scenes.Resume(member);     // in the copy as one who came in by its door
                Assert.IsTrue(harness.Manager.AcceptOfferedMission(member, shanul.EntityId, Blood), "the mate takes 1401");
                harness.Drain();
                Tick(harness, copy, 250);
            }

            internal MissionObjectiveState Of(uint objective) => Client.Player.Missions[Blood].Objectives[objective].State;

            internal uint Held(uint template) => HeldBy(_harness, Client, template);

            /// <summary>The mate's own door (a personal scene object of the mate's run; the copy can still hold an earlier test's).</summary>
            internal DynamicObject Door(string role)
            {
                using var unit = _harness.CreateChar();
                var runs = unit.CharacterMissions.Runtime.Scenes(Client.Player.Id, Blood).Select(scene => scene.RunId).ToHashSet();
                return _copy.DynamicObjects.Single(obj => obj.SceneMissionId == Blood && obj.SceneActorRole == role &&
                    obj.SceneOwnerCharacterId == Client.Player.Id && obj.IsInWorld && runs.Contains(obj.SceneRunId));
            }

            /// <summary>The living Headsmen of the mate's run.</summary>
            internal int Headsmen()
            {
                using var unit = _harness.CreateChar();
                var runs = unit.CharacterMissions.Runtime.Scenes(Client.Player.Id, Blood).Select(scene => scene.RunId).ToHashSet();
                return Here(_copy).Count(one => one.DbId == B.Headsman && one.SpawnPool?.SceneRunId is { } run && runs.Contains(run));
            }

            /// <summary>The mate's kill of the Headsman of the mate's run, from beside it: its corpse.</summary>
            internal LootDispenser KillHeadsman()
            {
                using var unit = _harness.CreateChar();
                var runs = unit.CharacterMissions.Runtime.Scenes(Client.Player.Id, Blood).Select(scene => scene.RunId).ToHashSet();
                var headsman = Here(_copy).Single(one => one.DbId == B.Headsman && one.SpawnPool?.SceneRunId is { } run && runs.Contains(run));
                Beside(headsman.Position + new Vector3(0, 0, 3));
                headsman.Attributes[Attributes.Health].Current = 0;
                _harness.Creatures.HandleCreatureKill(_copy, headsman, Client.Player);
                Assert.IsTrue(_copy.LootDispensers.TryGetValue(headsman.CorpseLootEntityId, out var loot), "the mate's Headsman's corpse");
                _harness.Drain();
                return loot;
            }

            internal void Beside(Vector3 at)
            {
                Client.SetWorldPosition(at, 0);
                _harness.Drain();
            }

            internal void Loot(LootDispenser loot)
            {
                LootDispenserManager.Instance.RequestLootAllFromCorpse(Client, new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
                _harness.Drain();
            }

            /// <summary>Atali's and Umank's cards for both: the player's kills with the mate beside, each their own pickup.</summary>
            internal void WinTheCardsWithThePlayer()
            {
                foreach (var (pool, kind) in new[] { (B.AtaliPool, B.Atali), (B.UmankPool, B.Umank) })
                {
                    W.Stand(_copy, pool);
                    var boss = Here(_copy).First(one => one.SpawnPool?.DbId == pool && one.State != CharacterState.Dead);
                    Beside(boss.Position + new Vector3(0, 0, 3));
                    var loot = Kill(_harness, _copy, kind);
                    Loot(loot);
                    PickUp(_harness, loot);
                    _harness.Drain();
                }
                Tick(_harness, _copy, 250);
                Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete, 1U, 1U),
                    (Of(B.AtaliCard), Of(B.UmankCard), Of(B.ReportToShanul), CrucibleBloodFireSteelTests.Held(_harness, B.AtaliCardTemplate),
                        CrucibleBloodFireSteelTests.Held(_harness, B.UmankCardTemplate)), "both members' cards: the mate's 4 open");
            }

            internal void Talk()
            {
                Beside(_shanul.Position + new Vector3(0, 0, 1));
                Assert.IsTrue(_harness.Manager.CompleteOfferedObjective(Client, _shanul.EntityId, Blood, B.ReportToShanul, 1), "the mate's 4");
                _harness.Drain();
                Tick(_harness, _copy, 250);
            }

            /// <summary>The mate's use of a door from beside it, and its recovery if one is due.</summary>
            internal void Use(DynamicObject door)
            {
                Beside(door.Position + new Vector3(0, 0, -1));
                var count = _copy.PerformRecovery.Count;
                _harness.Objects.RequestUseObjectPacket(Client, new RequestUseObjectPacket
                {
                    EntityId = door.EntityId, ActionId = ActionId.UseObject, ActionArgId = B.UseArgument
                });
                if (_copy.PerformRecovery.Count > count)
                {
                    var recovery = _copy.PerformRecovery.Last();
                    _copy.PerformRecovery.Remove(recovery);
                    ActorActionManager.Instance.PerformRecovery(_copy, recovery);
                }
                _harness.Drain();
                Tick(_harness, _copy, 250);
                Tick(_harness, _copy, 250);
            }

            public void Dispose()
            {
                _harness.Manager.Scenes.Detach(Client, _copy);      // gone from the copy, as a member who leaves
                lock (Server.Clients)
                    Server.Clients.Remove(Client);
                _harness.Client.Player.PartyId = 0;
                Client.Player.PartyId = 0;
                PartyManager.Instance.Parties.Remove(_partyId);
                PartyManager.Instance.FreePartyId(_partyId);
                CellManager.Instance.RemoveFromWorld(Client);
                _copy.ClientList.Remove(Client);
            }
        }

        /// <summary>
        /// An abandon before any door with both cards held: taken again, the held cards count at the accept (1 and 2 by the
        /// drops' held-item facts, 3 by its aggregate, 7 opens 4): 3 Completed and 4 Incomplete at the first tick; the doors'
        /// 8 and 9 wait for the talk, which opens them, and the north doors open with the cards.
        /// </summary>
        [TestMethod]
        public void BloodFireSteelAbandonedWithBothCardsOpensFourAtTheAccept()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            C.EnterCrucible(harness);
            var copy = W.IntoWard(harness);
            Accept(harness, Giver(copy, I.Shanul), Blood);
            Tick(harness, copy, 250);
            WinTheCards(harness, copy);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Blood, B.ReportToShanul));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Blood));
            harness.Drain();
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            Assert.AreEqual((1U, 1U), (Held(harness, B.AtaliCardTemplate), Held(harness, B.UmankCardTemplate)), "both cards kept at the abandon");

            Accept(harness, Giver(copy, I.Shanul), Blood);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Completed, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete,
                    MissionObjectiveState.Inactive, MissionObjectiveState.Inactive),
                (Objective(harness, Blood, B.AtaliCard), Objective(harness, Blood, B.UmankCard), Objective(harness, Blood, B.Counter),
                    Objective(harness, Blood, B.ReportToShanul), Objective(harness, Blood, B.AtaliDoorUsed), Objective(harness, Blood, B.UmankDoorUsed)),
                "the held cards: 1, 2 and 3 done, 4 open; the doors wait for the talk");
            Talk(harness, Giver(copy, I.Shanul), Blood, B.ReportToShanul);
            Tick(harness, copy, 250);
            foreach (var role in new[] { B.AtaliDoorRole, B.UmankDoorRole })
            {
                Use(harness, copy, SceneObject(harness, copy, Blood, role), B.UseArgument);
                Tick(harness, copy, 250);
                Assert.AreEqual(UseObjectState.DoorStateOpen, SceneObject(harness, copy, Blood, role).StateId, $"{role} opens");
            }
            Assert.AreEqual((0U, 0U), (Held(harness, B.AtaliCardTemplate), Held(harness, B.UmankCardTemplate)), "the doors took the cards");
        }

        /// <summary>Atali's and Umank's cards won and picked up (lootRoll 0 or 99: the card drops at chance 100).</summary>
        private static void WinTheCards(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            foreach (var (pool, kind) in new[] { (B.AtaliPool, B.Atali), (B.UmankPool, B.Umank) })
            {
                W.Stand(copy, pool);
                PickUp(harness, Kill(harness, copy, kind));
                harness.Drain();
            }
            Tick(harness, copy, 250);
            Assert.AreEqual((1U, 1U), (Held(harness, B.AtaliCardTemplate), Held(harness, B.UmankCardTemplate)), "both cards");
        }

        private static readonly (string Role, Rasa.Missions.Scenes.ScenePosition At, uint Card)[] Doors =
        {
            (B.AtaliDoorRole, B.AtaliDoorAt, B.AtaliCardTemplate), (B.UmankDoorRole, B.UmankDoorAt, B.UmankCardTemplate),
            (B.SanctumDoorRole, B.SanctumDoorAt, B.HeadsmanCardTemplate)
        };

        private static uint Held(WildernessRuntimeTestHarness harness, uint template) => WildernessRanjaGorgeTests.Held(harness, template);

        private static uint HeldBy(WildernessRuntimeTestHarness harness, Client client, uint template)
        {
            using var unit = harness.CreateChar();
            return unit.CharacterInventories.GetItems(client.AccountEntry.Id)
                .Where(row => row.CharacterId == client.Player.Id)
                .Select(row => unit.Items.GetItem(row.ItemId))
                .Where(item => item.ItemTemplateId == template)
                .Aggregate(0U, (count, item) => count + item.StackSize);
        }

        /// <summary>
        /// A server restart as far as the scene's objects go: the reload and recover (ReloadAndRecover), but with 1401's door
        /// objects dropped from the copy and its cells before the resume, as a restart has none of them in memory; the resume
        /// makes them again from the checkpoint.
        /// </summary>
        internal static void Restart(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            WildernessRanjaGorgeTests.Reload(harness);
            harness.Manager.Scenes.Detach(harness.Client, copy);
            foreach (var obj in copy.DynamicObjects.Where(obj => obj.SceneMissionId == Blood).ToArray())
            {
                CellManager.Instance.RemoveFromWorld(copy, obj);
                copy.DynamicObjects.Remove(obj);
                obj.IsInWorld = false;
            }
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            Tick(harness, copy, 250);
            Tick(harness, copy, 250);
        }

        /// <summary>A use of the door from beside it that the server refuses: no recovery, the refusal's message.</summary>
        private static PlayerMessage? Refused(WildernessRuntimeTestHarness harness, MapChannel copy, DynamicObject door)
        {
            harness.MoveTo(door.Position + new Vector3(0, 0, -1));
            harness.Drain();
            var count = copy.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = door.EntityId, ActionId = ActionId.UseObject, ActionArgId = B.UseArgument
            });
            Assert.AreEqual(count, copy.PerformRecovery.Count, $"{door.SceneActorRole}: no use");
            return harness.Drain().OfType<UserActionFailedPacket>().LastOrDefault()?.MsgId;
        }

        /// <summary>A scene creature killed by the player; its corpse.</summary>
        private static LootDispenser KillScene(WildernessRuntimeTestHarness harness, MapChannel copy, Creature creature)
        {
            Assert.IsNotNull(creature);
            harness.MoveTo(creature.Position);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(copy, creature, harness.Client.Player);
            Assert.IsTrue(copy.LootDispensers.TryGetValue(creature.CorpseLootEntityId, out var loot), "a corpse");
            harness.Drain();
            return loot;
        }
    }
}
