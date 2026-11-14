using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    using H = AshenSheitanHighwayNpcsV1;
    using Hell = AshenHighwayToHellV1;
    using Air = AshenAirSuperiorityV1;
    using Art = AshenArtilleryAnnihilationV1;
    using E = MarshesEscortsTests;

    /// <summary>
    /// Ashen Desert set 3, the Sheitan Highway, played through as a player plays it: Captain Sera Karter's Highway to Hell (1247:
    /// ten Thrax Ravagers and ten Thrax Soldiers), Lt. Colonel Trent Ivan's Air Superiority (1248: one Desert Predator) and Lt.
    /// Colonel Dania Caldwell's Artillery Annihilation (1249: the four Bane mortars by the highway). With them the set's places:
    /// the Thrax pools by the highway's Bane camp, the Desert Predators on their beats, the four mortars.
    ///
    /// Why these tests: the Ravagers are an upstream row renamed, the Predator is new and walks a beat, each mortar is its own
    /// row in an upstream pool; 1247 counts two kinds on two objectives (1 and 3: the client has no 2), 1248 one kill of a
    /// creature that moves, 1249 four kills each on its own objective. A creature no player reaches or that walks through a hill,
    /// a marker that misses its creature, an offer at the wrong NPC, a kill that counts on the wrong objective or at all when
    /// it should not, progress lost at a reload or kept after an abandon, or a turn-in that pays twice or on a full bag would
    /// each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenSheitanHighwayTests
    {
        private const uint WeaponFiller = 3869;            // inventory category 1 (quest-test-pattern)
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1)
        private const uint Technician = 531010;            // Thrax Technician - Ashen Desert (World DB, the camp's pool 531034)
        private const uint AttaSoldier = AshenMaunaBarrensNpcsV1.AttaSoldier;

        private static readonly Vector3 Shadow = AshenLeadInsTests.ShadowsEdgeWaypoint;
        private static readonly uint[] ThraxPools = H.ThraxPools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] PredatorPools = H.PredatorPools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] MortarPools = H.Mortars.Select(mortar => mortar.Pool).ToArray();

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// Each Thrax pool has walkable ground and puts down three living Thrax of its kind (the Ravagers named "Thrax Ravager"),
        /// each on the navmesh, walked to from the Shadow's Edge Post waypoint and inside its marker. Each Desert Predator comes
        /// down at the start of its beat, hostile, with the client's name; every step of the beat is on the navmesh ground,
        /// walked to from the waypoint and inside the beat's marker, and the straight walk between two steps keeps to the
        /// ground (the navmesh under it within 1.5 m of the line, every metre). Each highway mortar is its own copy, on the
        /// navmesh at its pad, walked to from the waypoint, inside its marker. Left alone for 40 s, each Predator walks its
        /// beat: it is more than 15 m on from its start, on the ground and inside its marker all the way.
        /// </summary>
        [TestMethod]
        public void TheHighwayPlacesStandWhereTheyAreWalkedTo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var nav = desert.NavMesh;
            bool Walked(Vector3 from, Vector3 to) => nav.FindPath(nav.Nearest(from) ?? from, to, out var complete) != null && complete;
            var markers = harness.World.MissionIndicatorEntries.AsEnumerable().Where(row => row.ContentRevision == AshenMissionDataV1.Revision).ToList();
            void Inside(uint mission, Vector3 at, string what) => Assert.IsTrue(markers.Any(marker => marker.MissionId == mission &&
                    Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new(at.X, at.Z)) <= marker.Radius),
                FormattableString.Invariant($"{what} at {at} inside a {mission} marker"));

            Spawn(desert, ThraxPools.Concat(PredatorPools).Concat(MortarPools).ToArray());
            foreach (var (poolId, creature, _) in H.ThraxPools)
            {
                var pool = desert.SpawnPools.Single(loaded => loaded.DbId == poolId);
                Assert.IsTrue(SpawnPoolManager.HasWalkableGround(desert, pool), $"{poolId} has walkable ground");
                var here = Alive(desert).Where(candidate => candidate.SpawnPool?.DbId == poolId).Distinct().ToArray();
                // Fix E: the Ravager pools hold two (AshenFixEV1.Ravagers), the Soldier pools three (set 3).
                var count = creature == H.Ravager ? (int)AshenFixEV1.Ravagers : (int)H.PoolCreatures;
                Assert.HasCount(count, here, $"pool {poolId}: {count}");
                foreach (var one in here)
                {
                    Assert.AreEqual((creature, TargetCategory.Hostile), (one.DbId, one.TargetCategory), $"pool {poolId}");
                    if (creature == H.Ravager)
                        Assert.AreEqual(H.RavagerName, one.NameId, "a Thrax Ravager");
                    Assert.IsTrue(nav.IsOnMesh(one.Position), $"pool {poolId}'s Thrax on the navmesh at {one.Position}");
                    Assert.IsTrue(Walked(Shadow, one.Position), $"pool {poolId}'s Thrax at {one.Position} walked to from the Shadow's Edge Post waypoint");
                    Inside(Hell.MissionId, one.Position, $"pool {poolId}'s Thrax");
                }
            }

            foreach (var (poolId, _, way) in H.PredatorPools)
            {
                var predator = Alive(desert).Single(candidate => candidate.SpawnPool?.DbId == poolId);
                Assert.AreEqual((H.Predator, H.PredatorName, TargetCategory.Hostile), (predator.DbId, predator.NameId, predator.TargetCategory), $"pool {poolId}");
                var home = H.Home(poolId);
                Assert.IsLessThan(0.5f, Vector2.Distance(new((float)home.X, (float)home.Z), new(predator.Position.X, predator.Position.Z)), $"pool {poolId}: at its home step");
                Assert.IsLessThan(0.5f, Vector2.Distance(new((float)home.X, (float)home.Z), new(predator.HomePos.Position.X, predator.HomePos.Position.Z)), $"pool {poolId}: its home is the pool's point");
                Assert.IsTrue(Patrols.Has(predator), $"pool {poolId}: it has its beat");
                Assert.HasCount(2 * way.Length - 2, predator.Patrol, $"pool {poolId}: every step both ways");
                var beat = H.Beat(poolId).Select(step => new Vector3((float)step.X, (float)step.Y, (float)step.Z)).ToArray();
                for (var i = 0; i < beat.Length; i++)
                {
                    var step = beat[i];
                    AssertOnGround(desert, step, $"pool {poolId} step {i}");
                    Assert.IsTrue(Walked(Shadow, step), $"pool {poolId} step {i} walked to from the Shadow's Edge Post waypoint");
                    Inside(Air.MissionId, step, $"pool {poolId} step {i}");
                    Assert.IsLessThanOrEqualTo(BehaviorManager.MaxChaseDistance - 15f, Vector2.Distance(new((float)home.X, (float)home.Z), new(step.X, step.Z)),
                        $"pool {poolId} step {i}: 15 m or more inside the leash from home");
                    var next = beat[(i + 1) % beat.Length];
                    var length = Vector3.Distance(step, next);
                    for (var metre = 1; metre < length; metre++)
                    {
                        var on = Vector3.Lerp(step, next, metre / length);
                        var ground = nav.GroundHeight(on);
                        Assert.IsNotNull(ground, FormattableString.Invariant($"pool {poolId}: navmesh under {on}"));
                        Assert.IsLessThanOrEqualTo(1.5f, MathF.Abs(ground.Value - on.Y), FormattableString.Invariant($"pool {poolId}: the walk at {on} keeps to the ground {ground}"));
                    }
                }
            }

            for (var index = 0; index < H.Mortars.Length; index++)
            {
                var (creature, poolId, at) = H.Mortars[index];
                var mortar = Alive(desert).Single(candidate => candidate.SpawnPool?.DbId == poolId);
                Assert.AreEqual((creature, TargetCategory.Hostile, 7482U), (mortar.DbId, mortar.TargetCategory, (uint)mortar.EntityClass), $"mortar pool {poolId}");
                Assert.IsLessThan(0.5f, Vector2.Distance(new((float)at.X, (float)at.Z), new(mortar.Position.X, mortar.Position.Z)), $"mortar {index + 1} on its pad");
                Assert.IsTrue(nav.IsOnMesh(mortar.Position), $"mortar {index + 1} on the navmesh at {mortar.Position}");
                Assert.IsTrue(Walked(Shadow, mortar.Position), $"mortar {index + 1} walked to from the Shadow's Edge Post waypoint");
                var marker = markers.Single(row => row.MissionId == Art.MissionId && row.ObjectiveId == Art.Destroy(index + 1));
                Assert.IsLessThanOrEqualTo((float)marker.Radius, Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new(mortar.Position.X, mortar.Position.Z)),
                    $"mortar {index + 1} inside its marker");
            }

            var walkers = PredatorPools.Select(poolId => Alive(desert).Single(candidate => candidate.SpawnPool?.DbId == poolId)).ToArray();
            var starts = walkers.Select(walker => walker.Position).ToArray();
            var furthest = new float[walkers.Length];
            harness.MoveTo(Shadow);
            harness.Drain();
            for (var tick = 0; tick < 160; tick++)
            {
                E.Tick(harness, desert, 250);
                for (var i = 0; i < walkers.Length; i++)
                {
                    var at = walkers[i].Position;
                    furthest[i] = MathF.Max(furthest[i], Vector2.Distance(new(at.X, at.Z), new(starts[i].X, starts[i].Z)));
                    var ground = nav.GroundHeight(at);
                    Assert.IsTrue(ground.HasValue && MathF.Abs(ground.Value - at.Y) <= 1.5f, FormattableString.Invariant($"pool {PredatorPools[i]}'s Predator at {at} on the ground {ground}"));
                    Inside(Air.MissionId, at, $"pool {PredatorPools[i]}'s Predator");
                }
            }
            for (var i = 0; i < walkers.Length; i++)
                Assert.IsGreaterThan(15f, furthest[i], $"pool {PredatorPools[i]}'s Predator walks its beat ({furthest[i]:0.0} m)");
        }

        /// <summary>
        /// Auditor F1 (2026-11-09): a beat creature's home is its pool's point, and a fight is left past MaxChaseDistance from
        /// home (BehaviorManager.ChasedTooFar: it runs home untouchable). Each Desert Predator, at the step of its beat furthest
        /// from home and hit there by a player beside it, fights: after four thinks it is not running home, it fights the player
        /// and can be hurt (DamageImmunity).
        /// </summary>
        [TestMethod]
        public void APredatorHitAtTheFarEndOfItsBeatFightsThere()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            Spawn(desert, PredatorPools);
            foreach (var poolId in PredatorPools)
            {
                var predator = Alive(desert).Single(candidate => candidate.SpawnPool?.DbId == poolId);
                var home = predator.HomePos.Position;
                var far = H.Beat(poolId).Select(step => new Vector3((float)step.X, (float)step.Y, (float)step.Z))
                    .OrderByDescending(step => Vector2.Distance(new(step.X, step.Z), new(home.X, home.Z))).First();
                predator.Position = far;
                harness.MoveTo(far + new Vector3(4, 0, 0));
                harness.Drain();
                Threat.FromDamage(predator, harness.Client.Player, 10);
                for (var tick = 0; tick < 4; tick++)
                    E.Tick(harness, desert, 250);
                Assert.IsFalse(BehaviorManager.IsReturning(predator), FormattableString.Invariant($"pool {poolId}: not leashed home from {far}"));
                Assert.AreEqual(BehaviorManager.BehaviorActionFighting, predator.Controller.CurrentAction, $"pool {poolId}: it fights");
                Assert.AreEqual(harness.Client.Player.EntityId, predator.Controller.ActionFighting.TargetEntityId, $"pool {poolId}: the player");
                Assert.IsFalse(DamageImmunity.IsImmune(predator, DamageType.Physical), $"pool {poolId}: it can be hurt");
                Assert.IsGreaterThan(15f, BehaviorManager.MaxChaseDistance - Vector2.Distance(new(predator.Position.X, predator.Position.Z), new(home.X, home.Z)),
                    $"pool {poolId}: 15 m and more before the leash");
                BehaviorManager.Instance.StopFighting(predator);
            }
        }

        /// <summary>
        /// Highway to Hell: Karter offers it (not Kuang, not Ivan); accepted, Karter hears no objective; two markers on each
        /// objective, on Ashen Desert; a Technician and an Atta count on neither, a Ravager only on 1, a Soldier only on 3; the
        /// counts kept at a reload; the tenth of each completes its objective once, an eleventh changes nothing; the last makes it
        /// completeable once; a full bag waits, a failed save pays nothing; Karter pays the weapon chosen, once.
        /// </summary>
        [TestMethod]
        public void HighwayToHellKillsTenRavagersAndTenSoldiers()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var karter = Npc(desert, AshenMaunaBarrensNpcsV1.Karter);
            var kuang = Npc(desert, AshenMaunaBarrensNpcsV1.Kuang);
            var ivan = Npc(desert, AshenLeadInNpcsV1.Ivan);
            Spawn(desert, ThraxPools);

            var offer = WildernessRanjaGorgeTests.Offer(harness, karter, Hell.MissionId);
            CollectionAssert.AreEqual(new[] { Hell.ElectricTorqueshell, Hell.FireInjector, Hell.IceLauncher, Hell.LaserChaingun },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kuang, Hell.MissionId), "not Kuang's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ivan, Hell.MissionId), "not Ivan's to give");
            Accept(harness, karter, Hell.MissionId);
            Assert.IsEmpty(Topics(harness, karter, Hell.MissionId), "Karter hears no objective");
            var snapshot = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Hell.MissionId].ObjectivesList;
            CollectionAssert.AreEquivalent(new[] { Hell.Ravagers, Hell.Soldiers }, snapshot.Select(o => o.ObjectiveId).ToArray(), "objectives 1 and 3");
            foreach (var objective in snapshot)
            {
                Assert.HasCount(2, objective.IndicatorList, $"objective {objective.ObjectiveId}'s markers");
                Assert.IsTrue(objective.IndicatorList.All(marker => marker.MapContextId == AshenMissionDataV1.MapContextId), "on Ashen Desert");
            }

            KillOne(harness, desert, Technician, spawnFrom: PoolsOf(harness, Technician));
            KillOne(harness, desert, AttaSoldier, spawnFrom: new[] { AshenMaunaBarrensNpcsV1.AttaPools[0].Pool });
            harness.Drain();
            Assert.AreEqual((0U, 0U), Counts(harness), "a Technician and an Atta count on neither");
            KillOne(harness, desert, H.Soldier);
            harness.Drain();
            Assert.AreEqual((0U, 1U), Counts(harness), "a Soldier counts on objective 3 only");

            var completed = new List<uint>();
            var completeable = 0;
            void Collect(IEnumerable<object> drained)
            {
                var list = drained.ToList();
                completed.AddRange(list.OfType<ObjectiveCompletedPacket>().Where(packet => packet.MissionId == Hell.MissionId).Select(packet => packet.ObjectiveId));
                completeable += list.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Hell.MissionId && packet.IsCompleteable);
            }
            for (var kill = 1; kill <= Hell.Kills + 1; kill++)
            {
                if (kill == 6)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual((5U, 1U), Counts(harness), "kept at a reload");
                    harness.Drain();
                }
                KillOne(harness, desert, H.Ravager);
                Collect(harness.Drain());
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Hell.MissionId, Hell.Ravagers), "ten Ravagers");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Hell.MissionId, Hell.Soldiers), "one Soldier");
            Assert.IsFalse(harness.Client.Player.Missions[Hell.MissionId].Completeable);
            for (var kill = 2; kill <= Hell.Kills; kill++)
            {
                KillOne(harness, desert, H.Soldier);
                Collect(harness.Drain());
            }
            CollectionAssert.AreEquivalent(new[] { Hell.Ravagers, Hell.Soldiers }, completed, "ObjectiveCompleted once each");
            Assert.AreEqual(1, completeable, "MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, ivan.EntityId, Hell.MissionId, 1), "not Ivan's to take");
            FullBagWaits(harness, karter, Hell.MissionId, 2, WeaponFiller);
            FailedTurnInPaysNothing(harness, karter, Hell.MissionId, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, karter, Hell.MissionId, 2, Hell.Experience, Hell.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Hell.MissionId), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Hell.IceLauncher), "the launcher chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, karter, Hell.MissionId), "done once");
        }

        /// <summary>An abandon of Highway to Hell drops the counts; taken again, the kills start from none.</summary>
        [TestMethod]
        public void HighwayToHellAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var karter = Npc(desert, AshenMaunaBarrensNpcsV1.Karter);
            Spawn(desert, ThraxPools);
            Accept(harness, karter, Hell.MissionId);
            for (var kill = 0; kill < 3; kill++)
                KillOne(harness, desert, H.Ravager);
            KillOne(harness, desert, H.Soldier);
            harness.Drain();
            Assert.AreEqual((3U, 1U), Counts(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Hell.MissionId));
            harness.Drain();
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, karter, Hell.MissionId), "offered again after an abandon");
            Accept(harness, karter, Hell.MissionId);
            KillOne(harness, desert, H.Ravager);
            harness.Drain();
            Assert.AreEqual((1U, 0U), Counts(harness), "one kill, not four");
        }

        /// <summary>
        /// Air Superiority: Ivan offers it (not Karter, not Caldwell); abandoned before the kill it is offered again; accepted,
        /// Ivan hears no objective; a marker on each Predator's beat, on Ashen Desert; a Ravager and a mortar do not count; a
        /// Desert Predator's death completes it once (ObjectiveCompleted and MissionCompleteable once), kept at a reload; a second
        /// Predator changes nothing; a full bag waits, a failed save pays nothing; Ivan pays the weapon chosen, once.
        /// </summary>
        [TestMethod]
        public void AirSuperiorityDestroysADesertPredator()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var ivan = Npc(desert, AshenLeadInNpcsV1.Ivan);
            var karter = Npc(desert, AshenMaunaBarrensNpcsV1.Karter);
            var caldwell = Npc(desert, AshenLeadInNpcsV1.Caldwell);
            Spawn(desert, PredatorPools.Concat(ThraxPools).Concat(MortarPools).ToArray());

            var offer = WildernessRanjaGorgeTests.Offer(harness, ivan, Air.MissionId);
            CollectionAssert.AreEqual(new[] { Air.FlameThrower, Air.ElectricRifle, Air.IceLeechGun, Air.ElectricChaingun },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, karter, Air.MissionId), "not Karter's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, caldwell, Air.MissionId), "not Caldwell's to give");
            Accept(harness, ivan, Air.MissionId);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Air.MissionId));
            harness.Drain();
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, ivan, Air.MissionId), "offered again after an abandon");
            Accept(harness, ivan, Air.MissionId);
            Assert.IsEmpty(Topics(harness, ivan, Air.MissionId), "Ivan hears no objective");
            var markers = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Air.MissionId].ObjectivesList.Single().IndicatorList;
            Assert.HasCount(2, markers, "a marker on each beat");
            Assert.IsTrue(markers.All(marker => marker.MapContextId == AshenMissionDataV1.MapContextId), "on Ashen Desert");

            KillOne(harness, desert, H.Ravager);
            KillOne(harness, desert, H.Mortars[0].Creature);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Air.MissionId, Air.Destroy), "a Ravager and a mortar are no Predator");
            KillOne(harness, desert, H.Predator);
            var packets = harness.Drain();
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Air.MissionId), "ObjectiveCompleted once");
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Air.MissionId && packet.IsCompleteable), "MissionCompleteable once");
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Air.MissionId, Air.Destroy), "kept at a reload");
            harness.Drain();
            KillOne(harness, desert, H.Predator);
            var second = harness.Drain();
            Assert.AreEqual(0, second.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Air.MissionId), "the second Predator changes nothing");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, karter.EntityId, Air.MissionId, 0), "not Karter's to take");
            FullBagWaits(harness, ivan, Air.MissionId, 0, WeaponFiller);
            FailedTurnInPaysNothing(harness, ivan, Air.MissionId, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, ivan, Air.MissionId, 0, Air.Experience, Air.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Air.MissionId), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Air.FlameThrower), "the flame thrower chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ivan, Air.MissionId), "done once");
        }

        /// <summary>
        /// Artillery Annihilation: Caldwell offers it (not Ivan); accepted, Caldwell hears no objective; one marker on each mortar,
        /// keyed by the client's names 160-163, on Ashen Desert; another Ashen mortar (592011) counts on none; each highway mortar
        /// completes its own objective once, in any order (3, 1, 4, 2), and a second kill of one changes nothing; a reload after
        /// two keeps them; the fourth makes it completeable once; a full bag waits, a failed save pays nothing; Caldwell pays the
        /// item chosen, once. An abandon after one drops it; taken again, nothing is done.
        /// </summary>
        [TestMethod]
        public void ArtilleryAnnihilationDestroysTheFourMortars()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var caldwell = Npc(desert, AshenLeadInNpcsV1.Caldwell);
            var ivan = Npc(desert, AshenLeadInNpcsV1.Ivan);
            var otherMortar = PoolsOf(harness, H.Mortar);
            Spawn(desert, MortarPools.Concat(otherMortar).ToArray());

            var offer = WildernessRanjaGorgeTests.Offer(harness, caldwell, Art.MissionId);
            CollectionAssert.AreEqual(new[] { Art.GravitonVest, Art.HazmatGloves, Art.ReflectiveVest, Art.BioVest },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ivan, Art.MissionId), "not Ivan's to give");
            Accept(harness, caldwell, Art.MissionId);
            Assert.IsEmpty(Topics(harness, caldwell, Art.MissionId), "Caldwell hears no objective");
            var snapshot = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Art.MissionId].ObjectivesList;
            Assert.HasCount((int)Art.Mortars, snapshot);
            foreach (var objective in snapshot)
            {
                var marker = objective.IndicatorList.Single();
                var at = H.Mortars[objective.ObjectiveId - 1].At;
                Assert.IsLessThan(0.1f, Vector2.Distance(new(marker.Position.X, marker.Position.Z), new((float)at.X, (float)at.Z)), $"objective {objective.ObjectiveId}'s marker on its mortar");
                Assert.AreEqual(159 + objective.ObjectiveId, marker.IndicatorId, $"objective {objective.ObjectiveId}: the client's 'Bane Heavy Mortar {objective.ObjectiveId}'");
                Assert.AreEqual(AshenMissionDataV1.MapContextId, marker.MapContextId, "on Ashen Desert");
            }

            KillOne(harness, desert, H.Mortar, spawnFrom: otherMortar);
            harness.Drain();
            Assert.AreEqual(0, Done(harness), "another Ashen mortar counts on none");

            var log = new List<object>();
            var destroyed = 0;
            foreach (var number in new[] { 3, 1, 4, 2 })
            {
                KillOne(harness, desert, H.Mortars[number - 1].Creature, spawnFrom: new[] { H.Mortars[number - 1].Pool });
                var packets = harness.Drain().Cast<object>().ToList();
                log.AddRange(packets);
                destroyed++;
                Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Art.MissionId && packet.ObjectiveId == Art.Destroy(number)),
                    $"mortar {number}: its objective, once");
                Assert.AreEqual(destroyed, Done(harness), $"mortar {number}: only its own objective");
                if (number == 3)
                {
                    KillOne(harness, desert, H.Mortars[2].Creature, spawnFrom: new[] { H.Mortars[2].Pool });
                    log.AddRange(harness.Drain());
                    Assert.AreEqual(1, Done(harness), "mortar 3 again changes nothing");
                }
                if (destroyed == 2)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(2, Done(harness), "kept at a reload");
                    harness.Drain();
                }
                if (destroyed < Art.Mortars)
                    Assert.IsFalse(harness.Client.Player.Missions[Art.MissionId].Completeable, $"{destroyed} of 4: not completeable");
            }
            Assert.IsTrue(harness.Client.Player.Missions[Art.MissionId].Completeable, "four mortars");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Art.MissionId && packet.IsCompleteable), "MissionCompleteable once");
            Assert.AreEqual(4, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Art.MissionId), "ObjectiveCompleted once each");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, ivan.EntityId, Art.MissionId, 3), "not Ivan's to take");
            FullBagWaits(harness, caldwell, Art.MissionId, 3, ArmorFiller);
            FailedTurnInPaysNothing(harness, caldwell, Art.MissionId, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, caldwell, Art.MissionId, 3, Art.Experience, Art.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Art.MissionId), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Art.BioVest), "the vest chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, caldwell, Art.MissionId), "done once");
        }

        /// <summary>An abandon of Artillery Annihilation after one mortar drops it; taken again, all four are open.</summary>
        [TestMethod]
        public void ArtilleryAnnihilationAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var caldwell = Npc(desert, AshenLeadInNpcsV1.Caldwell);
            Spawn(desert, MortarPools);
            Accept(harness, caldwell, Art.MissionId);
            KillOne(harness, desert, H.Mortars[0].Creature);
            harness.Drain();
            Assert.AreEqual(1, Done(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Art.MissionId));
            harness.Drain();
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, caldwell, Art.MissionId), "offered again after an abandon");
            Accept(harness, caldwell, Art.MissionId);
            Assert.AreEqual(0, Done(harness), "all four open again");
        }

        /// <summary>1247's counts: Ravagers (objective 1), Soldiers (objective 3).</summary>
        private static (uint Ravagers, uint Soldiers) Counts(WildernessRuntimeTestHarness harness) =>
            (harness.Client.Player.Missions[Hell.MissionId].Objectives[Hell.Ravagers].Counters[0],
             harness.Client.Player.Missions[Hell.MissionId].Objectives[Hell.Soldiers].Counters[0]);

        /// <summary>How many of 1249's four objectives are done.</summary>
        private static int Done(WildernessRuntimeTestHarness harness) =>
            Enumerable.Range(1, (int)Art.Mortars).Count(number => Objective(harness, Art.MissionId, Art.Destroy(number)) == MissionObjectiveState.Completed);

        /// <summary>A kill of a living creature of this row, the set's pools respawned when none is left (or <paramref name="spawnFrom"/> spawned first).</summary>
        private static void KillOne(WildernessRuntimeTestHarness harness, MapChannel desert, uint creature, uint[] spawnFrom = null)
        {
            if (spawnFrom != null)
                Spawn(desert, spawnFrom);
            if (!Alive(desert).Any(candidate => candidate.DbId == creature && candidate.Npc == null))
                Respawn(desert, ThraxPools.Concat(PredatorPools).Concat(MortarPools).Concat(spawnFrom ?? Array.Empty<uint>()).ToArray());
            var one = Alive(desert).First(candidate => candidate.DbId == creature && candidate.Npc == null);
            harness.MoveTo(one.Position);
            one.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(desert, one, harness.Client.Player);
        }

        /// <summary>One automatic Ashen Desert pool that holds this creature (World DB), none of the set's.</summary>
        private static uint[] PoolsOf(WildernessRuntimeTestHarness harness, uint creature) =>
            harness.World.SpawnPoolEntries.AsEnumerable().Where(row => row.MapContextId == AshenMissionDataV1.MapContextId &&
                    new[] { row.Creature1Id, row.Creature2Id, row.Creature3Id, row.Creature4Id, row.Creature5Id, row.Creature6Id }.Contains(creature) &&
                    row.Mode == SpawnPoolManager.ModeAutomatic && !ThraxPools.Contains(row.Id))
                .Select(row => row.Id).Take(1).ToArray();
    }
}
