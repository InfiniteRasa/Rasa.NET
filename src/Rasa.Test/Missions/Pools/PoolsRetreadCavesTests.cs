extern alias RasaGame;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Services.Preloader.Missions.Pools;
using Rasa.Structures;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Pools
{
    using static PlateauCampResistanceTests;
    using static PlateauInstanceMaps;

    /// <summary>
    /// The Retread Caves' nine missions (squad instance 1694), played through as a player plays them: Stick Your Neck Out
    /// (1329: Amee in the Pools sends the player to Kaethi), then Ellen's, Donius's and Miles's chain: Mox Clearing (1132:
    /// twelve of the Grotto's Mox), Mox Shouldn't Smoke (1135: Donius, then the crate's carton), A Mox On Both Our Houses
    /// (1133: the carton in the burner, the Alpha Mox), Deadly Moxins (1134: three flares, three Mox, Amee pays), Miasma
    /// Mutations (1131: ten samples from the Mox Lair), Miasma Testing (1136: three docile miasmas led to Miles), Total
    /// Slaughter (1138: the pump, three waves) and The Mad Bomber (856).
    ///
    /// Why these tests: the four Retreads are new NPCs inside a squad copy, and the caves' navmesh is in four parts with
    /// no path between them, so each mission's creatures must stand in the part where they act (a docile miasma must have
    /// a path to Miles, a wave to the pump). The objects and creatures are the player's own in a copy that outlives a
    /// test; 1133, 1134 and 1138 set their creatures down only at a step; 1136 chains eleven objectives over three rounds.
    /// A creature off the navmesh or in the wrong part, an Alpha or a wave there too soon, a Pools Mox that counts, a
    /// carton, flare or sample kept after the turn-in, or progress lost at a reload would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PoolsRetreadCavesTests
    {
        private const uint Caves = PoolsRetreadCavesNpcsV1.MapContextId;
        private const uint Neck = PoolsStickYourNeckOutV1.MissionId;
        private const uint Clearing = PoolsMoxClearingV1.MissionId;
        private const uint Smoke = PoolsMoxShouldntSmokeV1.MissionId;
        private const uint Houses = PoolsMoxOnBothOurHousesV1.MissionId;
        private const uint Moxins = PoolsDeadlyMoxinsV1.MissionId;
        private const uint Mutations = PoolsMiasmaMutationsV1.MissionId;
        private const uint Testing = PoolsMiasmaTestingV1.MissionId;
        private const uint Slaughter = PoolsTotalSlaughterV1.MissionId;
        private const uint Bomber = PoolsMadBomberV1.MissionId;
        private const uint OldMoxPool = 540062;         // the Grotto's pool of Pools Mox (540011)
        private const uint MissionItemFiller = 11519;   // as PlateauCampResistanceTests

        private static readonly uint[] Chain = { Clearing, Smoke, Houses, Moxins, Mutations, Testing, Slaughter, Bomber };
        private static readonly Vector3 Door = ToVector(PoolsRetreadCavesNpcsV1.DoorArrival);
        private static readonly Vector3 Camp = ToVector(PoolsRetreadCavesNpcsV1.EllenAt);
        private static readonly Vector3 Grotto = new(195.6f, 9.01f, -34.6f);   // the Grotto's Mox pool
        private static readonly Vector3 Lair = new(121f, 60.79f, -292f);       // the Lair's Miasma pool

        /// <summary>
        /// The Retreads stand on the camp's floors, each with its name and level; Ellen offers Mox Clearing with no
        /// requirement. The Mox pools stand in the Grotto, the mutated miasma pools in the Lair, each on the ground.
        /// </summary>
        [TestMethod]
        public void TheRetreadsStandInTheCampAndThePoolsInTheirCaves()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoCaves(harness);
            foreach (var (pool, name, at) in new[]
                {
                    (PoolsRetreadCavesNpcsV1.Ellen, PoolsRetreadCavesNpcsV1.EllenName, PoolsRetreadCavesNpcsV1.EllenAt),
                    (PoolsRetreadCavesNpcsV1.Kaethi, PoolsRetreadCavesNpcsV1.KaethiName, PoolsRetreadCavesNpcsV1.KaethiAt),
                    (PoolsRetreadCavesNpcsV1.Donius, PoolsRetreadCavesNpcsV1.DoniusName, PoolsRetreadCavesNpcsV1.DoniusAt),
                    (PoolsRetreadCavesNpcsV1.Miles, PoolsRetreadCavesNpcsV1.MilesName, PoolsRetreadCavesNpcsV1.MilesAt),
                })
            {
                var npc = Giver(copy, pool);
                Assert.AreEqual(name, npc.NameId);
                Assert.AreEqual(PoolsRetreadCavesNpcsV1.RetreadLevel, npc.Level);
                AssertOnTheGround(copy, ToVector(at), $"Retread {pool}");
                AssertInPart(copy, Camp, ToVector(at), $"Retread {pool}");
            }
            Assert.IsTrue(Vector3.Distance(ToVector(PoolsRetreadCavesNpcsV1.KaethiAt), Camp) < 10, "Kaethi next to Ellen");
            Assert.IsTrue(ToVector(PoolsRetreadCavesNpcsV1.MilesAt).Y < ToVector(PoolsRetreadCavesNpcsV1.EllenAt).Y - 10,
                "Miles on the camp's lowest floor");
            Assert.IsTrue(Offers(harness, Giver(copy, PoolsRetreadCavesNpcsV1.Ellen), Clearing), "Ellen offers Mox Clearing");
            Assert.IsFalse(Offers(harness, Giver(copy, PoolsRetreadCavesNpcsV1.Ellen), Bomber), "not The Mad Bomber yet");

            foreach (var (id, x, y, z) in PoolsRetreadCavesNpcsV1.MoxPools)
            {
                AssertOnTheGround(copy, new Vector3((float)x, (float)y, (float)z), $"Mox pool {id}");
                AssertInPart(copy, Grotto, new Vector3((float)x, (float)y, (float)z), $"Mox pool {id}");
                var mox = Pooled(harness, copy, id);
                Assert.AreEqual((PoolsRetreadCavesNpcsV1.MoxName, PoolsRetreadCavesNpcsV1.MoxLevel), (mox.NameId, mox.Level));
                Assert.AreEqual(TargetCategory.Hostile, mox.TargetCategory);
            }
            foreach (var (id, x, y, z) in PoolsRetreadCavesNpcsV1.MutatedPools)
            {
                AssertOnTheGround(copy, new Vector3((float)x, (float)y, (float)z), $"mutated pool {id}");
                AssertInPart(copy, Lair, new Vector3((float)x, (float)y, (float)z), $"mutated pool {id}");
                var miasma = Pooled(harness, copy, id);
                Assert.AreEqual((PoolsRetreadCavesNpcsV1.MutatedMiasmaName, PoolsRetreadCavesNpcsV1.MutatedMiasmaHitPoints),
                    (miasma.NameId, (uint)miasma.Attributes[Attributes.Health].Current));
            }
        }

        /// <summary>1329: Amee in the Pools offers it after Retread Planning; Kaethi in the caves hears it and pays the items.</summary>
        [TestMethod]
        public void StickYourNeckOutTakesThePlayerToKaethi()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var amee = Npc(pools, PoolsRetreadCampNpcsV1.Amee);
            Assert.IsFalse(Offers(harness, amee, Neck), "not before Retread Planning");
            SeedCompleted(harness, PoolsRetreadPlanningV1.MissionId);
            pools = PlateauValverdePoolsTests.OntoPools(harness);
            amee = Npc(pools, PoolsRetreadCampNpcsV1.Amee);
            Accept(harness, amee, Neck);

            var copy = IntoCaves(harness);
            var kaethi = Giver(copy, PoolsRetreadCavesNpcsV1.Kaethi);
            Talk(harness, kaethi, Neck, PoolsStickYourNeckOutV1.Speak);
            Assert.IsTrue(harness.Client.Player.Missions[Neck].Completeable);
            FailedTurnInPaysNothing(harness, kaethi, Neck, null);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, kaethi, Neck, null,
                PoolsStickYourNeckOutV1.Experience, PoolsStickYourNeckOutV1.Credits);
            foreach (var (template, count) in new[]
                {
                    (PoolsStickYourNeckOutV1.MedPack, PoolsStickYourNeckOutV1.MedPacks),
                    (PoolsStickYourNeckOutV1.AdrenalineBooster, PoolsStickYourNeckOutV1.AdrenalineBoosters),
                    (PoolsStickYourNeckOutV1.EmpGrenade, PoolsStickYourNeckOutV1.EmpGrenades),
                    (PoolsStickYourNeckOutV1.ConcussionGrenade, PoolsStickYourNeckOutV1.ConcussionGrenades),
                })
                Assert.AreEqual(count, WildernessRanjaGorgeTests.Held(harness, template), $"item {template}");
        }

        /// <summary>
        /// 1132: the Grotto's older Pools Mox do not count; twelve of the caves' Mox do, each once; a reload keeps the count;
        /// Ellen pays.
        /// </summary>
        [TestMethod]
        public void MoxClearingCountsTwelveOfTheCavesMox()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoCaves(harness);
            var ellen = Giver(copy, PoolsRetreadCavesNpcsV1.Ellen);
            var offer = WildernessRanjaGorgeTests.Offer(harness, ellen, Clearing);
            CollectionAssert.AreEqual(new[]
                {
                    PoolsMoxClearingV1.LaserChaingun, PoolsMoxClearingV1.ElectricNetGun,
                    PoolsMoxClearingV1.LaserPistol, PoolsMoxClearingV1.EndothermicPolarityGun
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, ellen, Clearing);

            Kill(harness, copy, Pooled(harness, copy, OldMoxPool));
            Assert.AreEqual(0U, Counter(harness, Clearing, PoolsMoxClearingV1.Kill), "a Pools Mox does not count");
            var kills = 0U;
            foreach (var (pool, _, _, _) in PoolsRetreadCavesNpcsV1.MoxPools.Take(4))
                for (var index = 0; index < PoolsRetreadCavesNpcsV1.PerPool; index++)
                {
                    var mox = Pooled(harness, copy, pool);
                    Kill(harness, copy, mox);
                    kills++;
                    Assert.AreEqual(kills, Counter(harness, Clearing, PoolsMoxClearingV1.Kill), $"Mox {kills}");
                    Kill(harness, copy, mox);
                    Assert.AreEqual(kills, Counter(harness, Clearing, PoolsMoxClearingV1.Kill), "counted once");
                    if (kills == 6)
                    {
                        WildernessRanjaGorgeTests.Reload(harness);
                        harness.Drain();
                        Assert.AreEqual(6U, Counter(harness, Clearing, PoolsMoxClearingV1.Kill), "kept at a reload");
                    }
                    if (kills < PoolsMoxClearingV1.Kills)
                        Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Clearing, PoolsMoxClearingV1.Kill));
                }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Clearing, PoolsMoxClearingV1.Kill));
            FailedTurnInPaysNothing(harness, ellen, Clearing, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, ellen, Clearing, 0,
                PoolsMoxClearingV1.Experience, PoolsMoxClearingV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsMoxClearingV1.LaserChaingun));
            Assert.IsTrue(Offers(harness, ellen, Smoke), "then Mox Shouldn't Smoke");
        }

        /// <summary>1135: the crate waits for Donius's talk; it gives the carton once; Ellen takes it and pays.</summary>
        [TestMethod]
        public void MoxShouldntSmokeTalksToDoniusThenTakesTheCarton()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompleted(harness, Clearing);
            var copy = IntoCaves(harness);
            var ellen = Giver(copy, PoolsRetreadCavesNpcsV1.Ellen);
            var donius = Giver(copy, PoolsRetreadCavesNpcsV1.Donius);
            Accept(harness, ellen, Smoke);
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Smoke, PoolsMoxShouldntSmokeV1.Carton));

            var crate = SceneObject(harness, copy, Smoke, PoolsMoxShouldntSmokeV1.CrateRole);
            Assert.AreEqual(PoolsMoxShouldntSmokeV1.CrateClass, (uint)crate.EntityClassId);
            AssertOnTheGround(copy, crate.Position, "the crate");
            AssertInPart(copy, Door, crate.Position, "the crate");
            Assert.IsTrue(Vector3.Distance(Door, crate.Position) < 40, "near the cave entrance");
            Use(harness, copy, crate, PoolsMoxShouldntSmokeV1.UseArgument);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsMoxShouldntSmokeV1.CartonTemplate), "not before Donius");

            Talk(harness, donius, Smoke, PoolsMoxShouldntSmokeV1.Talk);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Smoke, PoolsMoxShouldntSmokeV1.Carton));
            Use(harness, copy, crate, PoolsMoxShouldntSmokeV1.UseArgument);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsMoxShouldntSmokeV1.CartonTemplate), "the carton");
            Assert.AreEqual(PoolsMoxShouldntSmokeV1.TakenState, (uint)crate.StateId);
            Use(harness, copy, crate, PoolsMoxShouldntSmokeV1.UseArgument);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsMoxShouldntSmokeV1.CartonTemplate), "once");
            Assert.IsTrue(harness.Client.Player.Missions[Smoke].Completeable);
            FailedTurnInPaysNothing(harness, ellen, Smoke, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, ellen, Smoke, 1,
                PoolsMoxShouldntSmokeV1.Experience, PoolsMoxShouldntSmokeV1.Credits);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsMoxShouldntSmokeV1.CartonTemplate), "Ellen has it");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsMoxShouldntSmokeV1.MechVest));
        }

        /// <summary>
        /// 1133: the laced carton at the accept; no Alpha before the burner; the burner takes the carton and the Alpha comes
        /// at the player; a reload keeps 2 open; his death completes it; Ellen pays.
        /// </summary>
        [TestMethod]
        public void AMoxOnBothOurHousesBurnsTheCartonAndTheAlphaComes()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompleted(harness, Clearing, Smoke);
            var copy = IntoCaves(harness);
            var donius = Giver(copy, PoolsRetreadCavesNpcsV1.Donius);
            var ellen = Giver(copy, PoolsRetreadCavesNpcsV1.Ellen);
            Accept(harness, donius, Houses);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsMoxOnBothOurHousesV1.CartonTemplate), "the laced carton");

            var burner = SceneObject(harness, copy, Houses, PoolsMoxOnBothOurHousesV1.BurnerRole);
            Assert.AreEqual(PoolsMoxOnBothOurHousesV1.BurnerClass, (uint)burner.EntityClassId);
            AssertOnTheGround(copy, burner.Position, "the burner");
            AssertInPart(copy, Grotto, burner.Position, "the burner");
            Assert.IsNull(SceneCreature(harness, copy, Houses, PoolsMoxOnBothOurHousesV1.AlphaRole), "no Alpha before the burner");

            Use(harness, copy, burner, PoolsMoxOnBothOurHousesV1.UseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsMoxOnBothOurHousesV1.CartonTemplate), "in the burner");
            Assert.AreEqual(PoolsMoxOnBothOurHousesV1.BurnerLitState, (uint)burner.StateId);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Houses, PoolsMoxOnBothOurHousesV1.Place));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Houses, PoolsMoxOnBothOurHousesV1.Kill));
            var alpha = SceneCreature(harness, copy, Houses, PoolsMoxOnBothOurHousesV1.AlphaRole);
            Assert.IsNotNull(alpha, "the Alpha comes");
            Assert.AreEqual((PoolsRetreadCavesNpcsV1.AlphaMoxName, PoolsRetreadCavesNpcsV1.AlphaMoxLevel), (alpha.NameId, alpha.Level));
            Assert.AreEqual((int)PoolsRetreadCavesNpcsV1.AlphaMoxHitPoints, (int)alpha.Attributes[Attributes.Health].Current);
            Assert.AreEqual(TargetCategory.Hostile, alpha.TargetCategory);
            AssertOnTheGround(copy, ToVector(PoolsMoxOnBothOurHousesV1.AlphaAt), "the Alpha");
            AssertPath(copy, ToVector(PoolsMoxOnBothOurHousesV1.AlphaAt), burner.Position, "the Alpha to the burner");

            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Houses, PoolsMoxOnBothOurHousesV1.Kill), "after a reload");
            Kill(harness, copy, alpha);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Houses, PoolsMoxOnBothOurHousesV1.Kill));
            Assert.AreEqual(1U, Counter(harness, Houses, PoolsMoxOnBothOurHousesV1.Kill), "Alpha Mox Killed");
            FailedTurnInPaysNothing(harness, ellen, Houses, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, ellen, Houses, 2,
                PoolsMoxOnBothOurHousesV1.Experience, PoolsMoxOnBothOurHousesV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsMoxOnBothOurHousesV1.Rifle));
        }

        /// <summary>1133 abandoned after the burner: the Alpha and the lit burner go; taken again, a new carton and a whole burner.</summary>
        [TestMethod]
        public void AMoxOnBothOurHousesAbandonedTakesTheAlpha()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompleted(harness, Clearing, Smoke);
            var copy = IntoCaves(harness);
            var donius = Giver(copy, PoolsRetreadCavesNpcsV1.Donius);
            Accept(harness, donius, Houses);
            Use(harness, copy, SceneObject(harness, copy, Houses, PoolsMoxOnBothOurHousesV1.BurnerRole), PoolsMoxOnBothOurHousesV1.UseArgument);
            Tick(harness, copy, 250);
            var alpha = SceneCreature(harness, copy, Houses, PoolsMoxOnBothOurHousesV1.AlphaRole);
            Assert.IsNotNull(alpha);

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Houses));
            harness.Manager.Scenes.Tick(copy);
            harness.Drain();
            Assert.IsFalse(alpha.State != CharacterState.Dead && ReferenceEquals(alpha.RuntimeMapChannel, copy) &&
                Here(copy).Contains(alpha), "the Alpha goes");
            Accept(harness, donius, Houses);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsMoxOnBothOurHousesV1.CartonTemplate), "a new carton");
            var burner = SceneObject(harness, copy, Houses, PoolsMoxOnBothOurHousesV1.BurnerRole);
            Assert.AreEqual(PoolsMoxOnBothOurHousesV1.BurnerReadyState, (uint)burner.StateId, "a whole burner");
            Assert.IsNull(SceneCreature(harness, copy, Houses, PoolsMoxOnBothOurHousesV1.AlphaRole), "no Alpha yet");
        }

        /// <summary>
        /// 1134: three flares at the accept; three spots in any order, each once; with the third the last Mox come up from
        /// the tunnel; their three deaths complete it; Amee in the Pools pays and no flare is left.
        /// </summary>
        [TestMethod]
        public void DeadlyMoxinsPlacesThreeFlaresThenTheLastMoxComeAndAmeePays()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompleted(harness, Clearing, Smoke, Houses);
            var copy = IntoCaves(harness);
            var donius = Giver(copy, PoolsRetreadCavesNpcsV1.Donius);
            Accept(harness, donius, Moxins);
            Assert.AreEqual(PoolsDeadlyMoxinsV1.Flares, WildernessRanjaGorgeTests.Held(harness, PoolsDeadlyMoxinsV1.FlareTemplate));

            var spots = Enumerable.Range(0, PoolsDeadlyMoxinsV1.Spots.Length)
                .Select(index => SceneObject(harness, copy, Moxins, PoolsDeadlyMoxinsV1.SpotRole(index))).ToArray();
            foreach (var spot in spots)
            {
                AssertOnTheGround(copy, spot.Position, spot.SceneActorRole);
                AssertInPart(copy, Grotto, spot.Position, spot.SceneActorRole);
            }
            foreach (var at in PoolsDeadlyMoxinsV1.MoxAt)
            {
                AssertOnTheGround(copy, ToVector(at), "a last Mox");
                AssertPath(copy, ToVector(at), spots[1].Position, "a last Mox to the room");
            }
            var order = new[] { 1, 2, 0 };
            for (var step = 0; step < order.Length; step++)
            {
                Use(harness, copy, spots[order[step]], PoolsDeadlyMoxinsV1.UseArgument);
                Assert.AreEqual((uint)step + 1, Counter(harness, Moxins, PoolsDeadlyMoxinsV1.PlaceFlares), $"flare {step + 1}");
                Use(harness, copy, spots[order[step]], PoolsDeadlyMoxinsV1.UseArgument);
                Assert.AreEqual((uint)step + 1, Counter(harness, Moxins, PoolsDeadlyMoxinsV1.PlaceFlares), "once a spot");
                Assert.AreEqual(PoolsDeadlyMoxinsV1.Flares - (uint)step - 1, WildernessRanjaGorgeTests.Held(harness, PoolsDeadlyMoxinsV1.FlareTemplate));
                if (step < order.Length - 1)
                    Assert.IsNull(SceneCreature(harness, copy, Moxins, PoolsDeadlyMoxinsV1.MoxRole(0)), "no Mox before the third flare");
            }
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Moxins, PoolsDeadlyMoxinsV1.KillMox));
            var mox = Enumerable.Range(0, PoolsDeadlyMoxinsV1.MoxAt.Length)
                .Select(index => SceneCreature(harness, copy, Moxins, PoolsDeadlyMoxinsV1.MoxRole(index))).ToArray();
            Assert.IsTrue(mox.All(creature => creature != null && creature.TargetCategory == TargetCategory.Hostile), "the last Mox come");
            for (var index = 0; index < mox.Length; index++)
            {
                Kill(harness, copy, mox[index]);
                if (index < mox.Length - 1)
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Moxins, PoolsDeadlyMoxinsV1.KillMox));
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Moxins, PoolsDeadlyMoxinsV1.KillMox));
            Assert.IsTrue(harness.Client.Player.Missions[Moxins].Completeable);

            OutOfCaves(harness, copy);
            var amee = Npc(harness.Client.Player.MapChannel, PoolsRetreadCampNpcsV1.Amee);
            FailedTurnInPaysNothing(harness, amee, Moxins, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, amee, Moxins, 3,
                PoolsDeadlyMoxinsV1.Experience, PoolsDeadlyMoxinsV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsDeadlyMoxinsV1.StealthBoots));
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsDeadlyMoxinsV1.FlareTemplate));
        }

        /// <summary>
        /// 1131: two mutated miasmas spring up by Donius at the accept; a sample from each of them and from each public one
        /// killed in the Lair, once a corpse; Donius takes the ten and pays.
        /// </summary>
        [TestMethod]
        public void MiasmaMutationsTakesTenSamplesFromTheLair()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompleted(harness, Clearing, Smoke, Houses, Moxins);
            var copy = IntoCaves(harness);
            var donius = Giver(copy, PoolsRetreadCavesNpcsV1.Donius);
            Accept(harness, donius, Mutations);
            Tick(harness, copy, 250);
            for (var index = 0; index < PoolsMiasmaMutationsV1.SpringAt.Length; index++)
            {
                var spring = SceneCreature(harness, copy, Mutations, PoolsMiasmaMutationsV1.SpringRole(index));
                Assert.IsNotNull(spring, $"miasma {index + 1} springs up");
                Assert.AreEqual(TargetCategory.Hostile, spring.TargetCategory);
                AssertOnTheGround(copy, ToVector(PoolsMiasmaMutationsV1.SpringAt[index]), "a spring");
                AssertPath(copy, ToVector(PoolsMiasmaMutationsV1.SpringAt[index]), ToVector(PoolsRetreadCavesNpcsV1.DoniusAt), "to Donius");
                Assert.IsTrue(Vector3.Distance(ToVector(PoolsMiasmaMutationsV1.SpringAt[index]), donius.Position) < 25, "by Donius");
            }
            var samples = 0U;
            for (var index = 0; index < PoolsMiasmaMutationsV1.SpringAt.Length; index++)
            {
                var sprung = Kill(harness, copy, SceneCreature(harness, copy, Mutations, PoolsMiasmaMutationsV1.SpringRole(index)));
                Assert.IsNotNull(sprung, "a sprung miasma's corpse");
                PickUp(harness, sprung);
                harness.Drain();
                samples++;
                Assert.AreEqual(samples, WildernessRanjaGorgeTests.Held(harness, PoolsMiasmaMutationsV1.SampleTemplate), "a sprung miasma's sample");
            }

            foreach (var (pool, _, _, _) in PoolsRetreadCavesNpcsV1.MutatedPools)
                for (var index = 0; index < PoolsRetreadCavesNpcsV1.PerPool && samples < PoolsMiasmaMutationsV1.Samples; index++)
                {
                    var corpse = Kill(harness, copy, Pooled(harness, copy, pool));
                    Assert.IsNotNull(corpse, "a corpse");
                    PickUp(harness, corpse);
                    harness.Drain();
                    samples++;
                    Assert.AreEqual(samples, WildernessRanjaGorgeTests.Held(harness, PoolsMiasmaMutationsV1.SampleTemplate), $"sample {samples}");
                    PickUp(harness, corpse);
                    harness.Drain();
                    Assert.AreEqual(samples, WildernessRanjaGorgeTests.Held(harness, PoolsMiasmaMutationsV1.SampleTemplate), "once a corpse");
                    if (samples < PoolsMiasmaMutationsV1.Samples)
                        Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Mutations, PoolsMiasmaMutationsV1.Collect));
                }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Mutations, PoolsMiasmaMutationsV1.Collect));
            FailedTurnInPaysNothing(harness, donius, Mutations, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, donius, Mutations, 0,
                PoolsMiasmaMutationsV1.Experience, PoolsMiasmaMutationsV1.Credits);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsMiasmaMutationsV1.SampleTemplate), "Donius has them");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsMiasmaMutationsV1.PulseRpgLauncher));
        }

        /// <summary>1131: a mutated miasma killed before the accept drops no sample; after it, the next one does.</summary>
        [TestMethod]
        public void MiasmaMutationsDropsNoSampleBeforeTheAccept()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompleted(harness, Clearing, Smoke, Houses, Moxins);
            var copy = IntoCaves(harness);
            var pool = PoolsRetreadCavesNpcsV1.MutatedPools[0].Id;
            var corpse = Kill(harness, copy, Pooled(harness, copy, pool));
            Assert.IsFalse(corpse != null && corpse.LootItems.Any(item => item.ItemTemplateId == PoolsMiasmaMutationsV1.SampleTemplate),
                "no sample before the accept");
            Accept(harness, Giver(copy, PoolsRetreadCavesNpcsV1.Donius), Mutations);
            PickUp(harness, Kill(harness, copy, Pooled(harness, copy, pool)));
            harness.Drain();
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsMiasmaMutationsV1.SampleTemplate));
        }

        /// <summary>
        /// 1136: three rounds. Each round's docile miasma stands in the camp's part with a path to Miles; coming near it
        /// completes the find and it follows; coming to Miles completes the return; his test takes the miasma; his word
        /// sends the player for the next. A reload keeps the round. Miles pays after the third test.
        /// </summary>
        [TestMethod]
        public void MiasmaTestingLeadsThreeDocileMiasmasToMiles()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompleted(harness, Clearing, Smoke, Houses, Moxins, Mutations);
            var copy = IntoCaves(harness);
            var donius = Giver(copy, PoolsRetreadCavesNpcsV1.Donius);
            var miles = Giver(copy, PoolsRetreadCavesNpcsV1.Miles);
            Accept(harness, donius, Testing);
            Tick(harness, copy, 250);
            var milesAt = ToVector(PoolsRetreadCavesNpcsV1.MilesAt);

            for (var index = 0; index < PoolsMiasmaTestingV1.Rounds.Length; index++)
            {
                var round = PoolsMiasmaTestingV1.Rounds[index];
                var docile = SceneCreature(harness, copy, Testing, PoolsMiasmaTestingV1.DocileRole(index));
                Assert.IsNotNull(docile, $"docile miasma {index + 1}");
                Assert.AreEqual(PoolsRetreadCavesNpcsV1.DocileMiasmaName, docile.NameId);
                Assert.AreNotEqual(TargetCategory.Hostile, docile.TargetCategory, "docile");
                AssertOnTheGround(copy, ToVector(round.At), $"docile miasma {index + 1}");
                AssertPath(copy, ToVector(round.At), milesAt, $"docile miasma {index + 1} to Miles");
                Assert.IsTrue(Vector3.Distance(ToVector(round.At), milesAt) > 3 * PoolsMiasmaTestingV1.MilesRadius, "away from Miles");
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Testing, round.Find), $"round {index + 1} find");
                Assert.AreEqual(0U, docile.SpawnPool.FollowOwnerCharacterId, "it waits");

                Enter(harness, ToVector(round.At));
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Testing, round.Find));
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Testing, round.Return));
                Tick(harness, copy, 250);
                Assert.AreEqual(harness.Client.Player.Id, docile.SpawnPool.FollowOwnerCharacterId, $"docile miasma {index + 1} follows");
                if (index == 0)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    harness.Drain();
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Testing, round.Return), "after a reload");
                }
                Enter(harness, milesAt + new Vector3(2, 0, 2));
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Testing, round.Return));
                Talk(harness, miles, Testing, round.Test);
                Tick(harness, copy, 250);
                Assert.IsNull(SceneCreature(harness, copy, Testing, PoolsMiasmaTestingV1.DocileRole(index)), "the test takes it");
                if (round.Next == 0)
                    break;
                Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Testing, PoolsMiasmaTestingV1.Rounds[index + 1].Find),
                    "Miles has more to say first");
                Talk(harness, miles, Testing, round.Next);
            }
            Assert.IsTrue(harness.Client.Player.Missions[Testing].Completeable);
            FailedTurnInPaysNothing(harness, miles, Testing, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, miles, Testing, 1,
                PoolsMiasmaTestingV1.Experience, PoolsMiasmaTestingV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsMiasmaTestingV1.GravitonVest));
        }

        /// <summary>1136: Miles's return waits for the find: coming to Miles first completes nothing.</summary>
        [TestMethod]
        public void MiasmaTestingReturnWaitsForTheFind()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompleted(harness, Clearing, Smoke, Houses, Moxins, Mutations);
            var copy = IntoCaves(harness);
            Accept(harness, Giver(copy, PoolsRetreadCavesNpcsV1.Donius), Testing);
            var first = PoolsMiasmaTestingV1.Rounds[0];
            Enter(harness, ToVector(PoolsRetreadCavesNpcsV1.MilesAt) + new Vector3(2, 0, 2));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Testing, first.Return), "no return before the find");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Testing, first.Find));
            Enter(harness, ToVector(PoolsMiasmaTestingV1.Rounds[1].At));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Testing, first.Find), "the second miasma is not the first");
        }

        /// <summary>
        /// 1138: no miasma before the pump; the pump completes 1 and the first wave comes; each wave follows the one before
        /// it, the last after its pause; the last wave's deaths complete 2; Ellen pays.
        /// </summary>
        [TestMethod]
        public void TotalSlaughterTurnsOnThePumpAndThreeWavesCome()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompleted(harness, Clearing, Smoke, Houses, Moxins, Mutations, Testing);
            var copy = IntoCaves(harness);
            var miles = Giver(copy, PoolsRetreadCavesNpcsV1.Miles);
            var ellen = Giver(copy, PoolsRetreadCavesNpcsV1.Ellen);
            Accept(harness, miles, Slaughter);
            var computer = SceneObject(harness, copy, Slaughter, PoolsTotalSlaughterV1.ComputerRole);
            Assert.AreEqual(PoolsTotalSlaughterV1.ComputerClass, (uint)computer.EntityClassId);
            AssertOnTheGround(copy, computer.Position, "the pump's computer");
            AssertInPart(copy, Camp, computer.Position, "the pump's computer");
            Assert.IsTrue(Vector3.Distance(computer.Position, ToVector(PoolsRetreadCavesNpcsV1.MilesAt)) < 50, "down by Miles");
            Assert.IsNull(SceneCreature(harness, copy, Slaughter, PoolsTotalSlaughterV1.MiasmaRole(0, 0)), "no miasma before the pump");

            Use(harness, copy, computer, PoolsTotalSlaughterV1.UseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Slaughter, PoolsTotalSlaughterV1.Pump));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Slaughter, PoolsTotalSlaughterV1.Slaughter));
            Assert.AreEqual(PoolsTotalSlaughterV1.ComputerOnState, (uint)computer.StateId);
            for (var wave = 0; wave < PoolsTotalSlaughterV1.Waves.Length; wave++)
            {
                var at = PoolsTotalSlaughterV1.Waves[wave].At;
                if (wave == PoolsTotalSlaughterV1.Waves.Length - 1)
                {
                    Tick(harness, copy, PoolsTotalSlaughterV1.LastPauseMs - 2000);
                    Assert.IsNull(SceneCreature(harness, copy, Slaughter, PoolsTotalSlaughterV1.MiasmaRole(wave, 0)), "the pause");
                    Tick(harness, copy, 2500);
                }
                var miasmas = Enumerable.Range(0, at.Length)
                    .Select(index => SceneCreature(harness, copy, Slaughter, PoolsTotalSlaughterV1.MiasmaRole(wave, index))).ToArray();
                Assert.IsTrue(miasmas.All(miasma => miasma != null), $"wave {wave + 1} comes");
                if (wave + 1 < PoolsTotalSlaughterV1.Waves.Length)
                    Assert.IsNull(SceneCreature(harness, copy, Slaughter, PoolsTotalSlaughterV1.MiasmaRole(wave + 1, 0)), "one wave at a time");
                for (var index = 0; index < miasmas.Length; index++)
                {
                    Assert.AreEqual((PoolsRetreadCavesNpcsV1.PumpMiasmaName, PoolsRetreadCavesNpcsV1.PumpMiasmaLevel), (miasmas[index].NameId, miasmas[index].Level));
                    Assert.AreEqual(TargetCategory.Hostile, miasmas[index].TargetCategory);
                    AssertOnTheGround(copy, ToVector(at[index]), $"wave {wave + 1} miasma {index + 1}");
                    AssertPath(copy, ToVector(at[index]), computer.Position, $"wave {wave + 1} miasma {index + 1} to the pump");
                }
                if (wave == 1)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    harness.Drain();
                }
                foreach (var miasma in miasmas)
                {
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Slaughter, PoolsTotalSlaughterV1.Slaughter));
                    Kill(harness, copy, miasma);
                }
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Slaughter, PoolsTotalSlaughterV1.Slaughter));
            FailedTurnInPaysNothing(harness, ellen, Slaughter, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, ellen, Slaughter, 2,
                PoolsTotalSlaughterV1.Experience, PoolsTotalSlaughterV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsTotalSlaughterV1.SonicCannon));
        }

        /// <summary>856: the Mad Bomber stands in the Eastern Fringe from the accept; his death completes it; Ellen pays.</summary>
        [TestMethod]
        public void TheMadBomberDiesInTheEasternFringe()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompleted(harness, Clearing, Smoke, Houses, Moxins, Mutations, Testing, Slaughter);
            var copy = IntoCaves(harness);
            var ellen = Giver(copy, PoolsRetreadCavesNpcsV1.Ellen);
            Assert.IsTrue(Offers(harness, ellen, Bomber), "after Total Slaughter");
            Accept(harness, ellen, Bomber);
            Tick(harness, copy, 250);
            var bomber = SceneCreature(harness, copy, Bomber, PoolsMadBomberV1.BomberRole);
            Assert.IsNotNull(bomber, "the Mad Bomber");
            Assert.AreEqual((PoolsRetreadCavesNpcsV1.MadBomberName, PoolsRetreadCavesNpcsV1.MadBomberLevel), (bomber.NameId, bomber.Level));
            Assert.AreEqual((int)PoolsRetreadCavesNpcsV1.MadBomberHitPoints, (int)bomber.Attributes[Attributes.Health].Current);
            Assert.AreEqual(TargetCategory.Hostile, bomber.TargetCategory);
            AssertOnTheGround(copy, ToVector(PoolsMadBomberV1.BomberAt), "the Mad Bomber");
            AssertInPart(copy, Door, ToVector(PoolsMadBomberV1.BomberAt), "the Mad Bomber");

            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();
            Kill(harness, copy, bomber);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Bomber, PoolsMadBomberV1.Kill));
            FailedTurnInPaysNothing(harness, ellen, Bomber, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, ellen, Bomber, 3,
                PoolsMadBomberV1.Experience, PoolsMadBomberV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsMadBomberV1.StealthLegs));
        }

        /// <summary>1135: a full bag: the crate gives nothing and stays usable; with a slot free it gives once.</summary>
        [TestMethod]
        public void MoxShouldntSmokeCrateWaitsForAFreeSlot()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompleted(harness, Clearing);
            var copy = IntoCaves(harness);
            Accept(harness, Giver(copy, PoolsRetreadCavesNpcsV1.Ellen), Smoke);
            Talk(harness, Giver(copy, PoolsRetreadCavesNpcsV1.Donius), Smoke, PoolsMoxShouldntSmokeV1.Talk);
            var crate = SceneObject(harness, copy, Smoke, PoolsMoxShouldntSmokeV1.CrateRole);
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            Use(harness, copy, crate, PoolsMoxShouldntSmokeV1.UseArgument);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsMoxShouldntSmokeV1.CartonTemplate), "no room");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Smoke, PoolsMoxShouldntSmokeV1.Carton));
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Use(harness, copy, crate, PoolsMoxShouldntSmokeV1.UseArgument);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsMoxShouldntSmokeV1.CartonTemplate), "with room it gives");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Smoke, PoolsMoxShouldntSmokeV1.Carton));
        }

        /// <summary>
        /// 1134: two flares placed, a reload keeps the count; then an abandon: the flares and spots go; taken again, three
        /// flares and three empty spots.
        /// </summary>
        [TestMethod]
        public void DeadlyMoxinsKeepsItsFlaresAtAReloadAndAbandonedTakesThem()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompleted(harness, Clearing, Smoke, Houses);
            var copy = IntoCaves(harness);
            var donius = Giver(copy, PoolsRetreadCavesNpcsV1.Donius);
            Accept(harness, donius, Moxins);
            for (var index = 0; index < 2; index++)
                Use(harness, copy, SceneObject(harness, copy, Moxins, PoolsDeadlyMoxinsV1.SpotRole(index)), PoolsDeadlyMoxinsV1.UseArgument);
            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();
            Assert.AreEqual(2U, Counter(harness, Moxins, PoolsDeadlyMoxinsV1.PlaceFlares), "kept at a reload");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsDeadlyMoxinsV1.FlareTemplate));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Moxins));
            harness.Manager.Scenes.Tick(copy);
            harness.Drain();
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsDeadlyMoxinsV1.FlareTemplate), "the flares go");
            Accept(harness, donius, Moxins);
            Assert.AreEqual(PoolsDeadlyMoxinsV1.Flares, WildernessRanjaGorgeTests.Held(harness, PoolsDeadlyMoxinsV1.FlareTemplate), "three again");
            Assert.AreEqual(0U, Counter(harness, Moxins, PoolsDeadlyMoxinsV1.PlaceFlares));
            for (var index = 0; index < PoolsDeadlyMoxinsV1.Spots.Length; index++)
                Assert.AreEqual(PoolsDeadlyMoxinsV1.SpotReadyState,
                    (uint)SceneObject(harness, copy, Moxins, PoolsDeadlyMoxinsV1.SpotRole(index)).StateId, $"spot {index + 1} empty");
        }

        /// <summary>1136 abandoned while a miasma follows: it goes; taken again, the first round starts over with a waiting miasma.</summary>
        [TestMethod]
        public void MiasmaTestingAbandonedTakesTheFollower()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompleted(harness, Clearing, Smoke, Houses, Moxins, Mutations);
            var copy = IntoCaves(harness);
            var donius = Giver(copy, PoolsRetreadCavesNpcsV1.Donius);
            Accept(harness, donius, Testing);
            Tick(harness, copy, 250);
            var docile = SceneCreature(harness, copy, Testing, PoolsMiasmaTestingV1.DocileRole(0));
            Enter(harness, ToVector(PoolsMiasmaTestingV1.Rounds[0].At));
            Tick(harness, copy, 250);
            Assert.AreEqual(harness.Client.Player.Id, docile.SpawnPool.FollowOwnerCharacterId, "it follows");

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Testing));
            harness.Manager.Scenes.Tick(copy);
            harness.Drain();
            Assert.IsFalse(Here(copy).Contains(docile), "the follower goes");
            Accept(harness, donius, Testing);
            Tick(harness, copy, 250);
            var again = SceneCreature(harness, copy, Testing, PoolsMiasmaTestingV1.DocileRole(0));
            Assert.IsNotNull(again, "a new first miasma");
            Assert.AreEqual(0U, again.SpawnPool.FollowOwnerCharacterId, "it waits");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Testing, PoolsMiasmaTestingV1.Rounds[0].Find));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Testing, PoolsMiasmaTestingV1.Rounds[0].Return));
        }

        /// <summary>
        /// 1138: a reload in the pause before the last wave: the last wave still comes at its time. Then an abandon: the
        /// wave and the switched-on computer go; taken again, no miasma and a computer to turn on.
        /// </summary>
        [TestMethod]
        public void TotalSlaughterLastWaveComesAfterAReloadAndAbandonedTakesTheWave()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompleted(harness, Clearing, Smoke, Houses, Moxins, Mutations, Testing);
            var copy = IntoCaves(harness);
            var miles = Giver(copy, PoolsRetreadCavesNpcsV1.Miles);
            Accept(harness, miles, Slaughter);
            Use(harness, copy, SceneObject(harness, copy, Slaughter, PoolsTotalSlaughterV1.ComputerRole), PoolsTotalSlaughterV1.UseArgument);
            Tick(harness, copy, 250);
            for (var wave = 0; wave < 2; wave++)
                for (var index = 0; index < PoolsTotalSlaughterV1.Waves[wave].At.Length; index++)
                    Kill(harness, copy, SceneCreature(harness, copy, Slaughter, PoolsTotalSlaughterV1.MiasmaRole(wave, index)));
            Tick(harness, copy, 5000);
            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();
            Assert.IsNull(SceneCreature(harness, copy, Slaughter, PoolsTotalSlaughterV1.MiasmaRole(2, 0)), "still the pause");
            Tick(harness, copy, PoolsTotalSlaughterV1.LastPauseMs);
            var last = SceneCreature(harness, copy, Slaughter, PoolsTotalSlaughterV1.MiasmaRole(2, 0));
            Assert.IsNotNull(last, "the last wave comes after the reload");

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Slaughter));
            harness.Manager.Scenes.Tick(copy);
            harness.Drain();
            Assert.IsFalse(Here(copy).Contains(last), "the wave goes");
            Accept(harness, miles, Slaughter);
            Tick(harness, copy, 250);
            Assert.IsNull(SceneCreature(harness, copy, Slaughter, PoolsTotalSlaughterV1.MiasmaRole(0, 0)), "no miasma before the pump");
            Assert.AreEqual(PoolsTotalSlaughterV1.ComputerReadyState,
                (uint)SceneObject(harness, copy, Slaughter, PoolsTotalSlaughterV1.ComputerRole).StateId, "the pump is off again");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Slaughter, PoolsTotalSlaughterV1.Pump));
        }

        /// <summary>856 abandoned: the Mad Bomber goes; taken again, he stands there whole.</summary>
        [TestMethod]
        public void TheMadBomberAbandonedGoesAndComesBackWhole()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompleted(harness, Clearing, Smoke, Houses, Moxins, Mutations, Testing, Slaughter);
            var copy = IntoCaves(harness);
            var ellen = Giver(copy, PoolsRetreadCavesNpcsV1.Ellen);
            Accept(harness, ellen, Bomber);
            Tick(harness, copy, 250);
            var bomber = SceneCreature(harness, copy, Bomber, PoolsMadBomberV1.BomberRole);
            bomber.Attributes[Attributes.Health].Current = 1000;
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Bomber));
            harness.Manager.Scenes.Tick(copy);
            harness.Drain();
            Assert.IsFalse(Here(copy).Contains(bomber), "he goes");
            Accept(harness, ellen, Bomber);
            Tick(harness, copy, 250);
            var again = SceneCreature(harness, copy, Bomber, PoolsMadBomberV1.BomberRole);
            Assert.IsNotNull(again);
            Assert.AreEqual((int)PoolsRetreadCavesNpcsV1.MadBomberHitPoints, (int)again.Attributes[Attributes.Health].Current, "whole");
        }

        /// <summary>Each mission of the chain is offered only after the one before it.</summary>
        [TestMethod]
        public void TheChainOpensOneMissionAtATime()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoCaves(harness);
            var givers = new Dictionary<uint, uint>
            {
                [Clearing] = PoolsRetreadCavesNpcsV1.Ellen, [Smoke] = PoolsRetreadCavesNpcsV1.Ellen, [Houses] = PoolsRetreadCavesNpcsV1.Donius,
                [Moxins] = PoolsRetreadCavesNpcsV1.Donius, [Mutations] = PoolsRetreadCavesNpcsV1.Donius, [Testing] = PoolsRetreadCavesNpcsV1.Donius,
                [Slaughter] = PoolsRetreadCavesNpcsV1.Miles, [Bomber] = PoolsRetreadCavesNpcsV1.Ellen
            };
            for (var index = 1; index < Chain.Length; index++)
            {
                Assert.IsFalse(Offers(harness, Giver(copy, givers[Chain[index]]), Chain[index]), $"{Chain[index]} before {Chain[index - 1]}");
                SeedCompleted(harness, Chain[index - 1]);
                copy = IntoCaves(harness);
                Assert.IsTrue(Offers(harness, Giver(copy, givers[Chain[index]]), Chain[index]), $"{Chain[index]} after {Chain[index - 1]}");
            }
        }

        private static MapChannel IntoCaves(WildernessRuntimeTestHarness harness)
        {
            PlateauValverdePoolsTests.OntoPools(harness);
            if (!harness.Maps.MapChannelArray.ContainsKey(Caves))
                LoadMap(harness, Caves);
            var copy = ThroughTheDoor(harness, Caves);
            Assert.IsTrue(copy.IsSquadInstance);
            harness.Manager.Scenes.Resume(harness.Client);   // as MapChannelManager.MapLoaded does (ResumeMissionScenes)
            harness.Drain();
            return copy;
        }

        /// <summary>Back to the Pools, the scene detached from the copy as a map change does.</summary>
        private static void OutOfCaves(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            harness.Manager.Scenes.Detach(harness.Client, copy);
            PlateauValverdePoolsTests.OntoPools(harness);
        }

        /// <summary>Into a mission area at <paramref name="at"/>, as MissionAreaService takes a step from far off.</summary>
        private static void Enter(WildernessRuntimeTestHarness harness, Vector3 at)
        {
            var from = at + new Vector3(60, 0, 60);
            harness.MoveTo(at);
            new MissionAreaService(() => harness.Manager).RecordAcceptedMovement(harness.Client, from, at);
            harness.Drain();
        }

        /// <summary>The living creatures of this copy only (PlateauUstorYardTests.Here).</summary>
        private static IEnumerable<Creature> Here(MapChannel map) =>
            Living(map).Where(creature => ReferenceEquals(creature.RuntimeMapChannel, map));

        private static Creature Giver(MapChannel copy, uint pool)
        {
            if (!Here(copy).Any(creature => creature.SpawnPool?.DbId == pool))
                Spawn(copy, pool);
            var giver = Here(copy).SingleOrDefault(creature => creature.SpawnPool?.DbId == pool && creature.Npc != null);
            Assert.IsNotNull(giver, $"Retread {pool} stands in the copy");
            return giver;
        }

        /// <summary>The NPC of the pool on a public map.</summary>
        private static Creature Npc(MapChannel map, uint pool)
        {
            Spawn(map, pool);
            var npc = Living(map).SingleOrDefault(creature => creature.SpawnPool?.DbId == pool && creature.Npc != null);
            Assert.IsNotNull(npc, $"pool {pool} stands an NPC");
            return npc;
        }

        /// <summary>
        /// A living creature of <paramref name="pool"/>. A squad copy outlives a test, and a cleared pool in it comes back
        /// only after the squad's respawn time (SquadInstanceState): one cleared by an earlier test is set back as the
        /// weekly reset does it, then spawned.
        /// </summary>
        private static Creature Pooled(WildernessRuntimeTestHarness harness, MapChannel copy, uint pool)
        {
            if (!Here(copy).Any(creature => creature.SpawnPool?.DbId == pool && creature.State != CharacterState.Dead))
            {
                var spawnPool = copy.SpawnPools.Single(candidate => candidate.DbId == pool);
                spawnPool.ClearedAtUtcMs = 0;
                spawnPool.UpdateTimer = spawnPool.RespawnTime;
                Spawn(copy, pool);
            }
            var creature = Here(copy).FirstOrDefault(candidate => candidate.SpawnPool?.DbId == pool && candidate.State != CharacterState.Dead);
            Assert.IsNotNull(creature, $"pool {pool} stands a creature in the copy");
            return creature;
        }

        private static HashSet<string> Runs(WildernessRuntimeTestHarness harness, uint mission)
        {
            using var unit = harness.CreateChar();
            return unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, mission).Select(scene => scene.RunId).ToHashSet();
        }

        /// <summary>The object of this test's own run: a squad copy can still hold an earlier test's.</summary>
        private static DynamicObject SceneObject(WildernessRuntimeTestHarness harness, MapChannel map, uint mission, string role)
        {
            var runs = Runs(harness, mission);
            var obj = map.DynamicObjects.SingleOrDefault(candidate => candidate.SceneMissionId == mission && candidate.SceneActorRole == role &&
                candidate.IsInWorld && runs.Contains(candidate.SceneRunId));
            Assert.IsNotNull(obj, $"{role} is there");
            return obj;
        }

        private static Creature SceneCreature(WildernessRuntimeTestHarness harness, MapChannel map, uint mission, string role)
        {
            var runs = Runs(harness, mission);
            return Here(map).SingleOrDefault(creature => creature.SpawnPool?.SceneActorRole == role && runs.Contains(creature.SpawnPool.SceneRunId) &&
                creature.State != CharacterState.Dead);
        }

        /// <summary>Kills the creature as the player and runs the scene on; returns its corpse, if it has one (PlateauUstorYardTests.Kill).</summary>
        private static LootDispenser Kill(WildernessRuntimeTestHarness harness, MapChannel map, Creature creature)
        {
            harness.MoveTo(creature.Position);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(map, creature, harness.Client.Player);
            Tick(harness, map, 250);
            return map.LootDispensers.TryGetValue(creature.CorpseLootEntityId, out var loot) ? loot : null;
        }

        /// <summary>As PlateauUstorYardTests.Tick.</summary>
        private static void Tick(WildernessRuntimeTestHarness harness, MapChannel map, long milliseconds)
        {
            harness.UtcNow = harness.UtcNow.AddMilliseconds(milliseconds);
            BehaviorManager.Instance.MapChannelThink(map, System.Math.Min(milliseconds, 1000));
            harness.Manager.Scenes.Tick(map);
            harness.Drain();
        }

        /// <summary>A use of the object from where it stands, and its recovery if one is due (PlateauUstorYardTests.Use).</summary>
        private static void Use(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj, uint argument)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            harness.Drain();
            var count = map.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = argument
            });
            if (map.PerformRecovery.Count > count)
            {
                var recovery = map.PerformRecovery.Last();
                map.PerformRecovery.Remove(recovery);
                ActorActionManager.Instance.PerformRecovery(map, recovery);
            }
            harness.Drain();
        }

        private static void FailedTurnInPaysNothing(WildernessRuntimeTestHarness harness, Creature receiver, uint mission, int? choice)
        {
            harness.MoveTo(receiver.Position);
            harness.Drain();
            var before = harness.Context.ReadRewardTotals();
            harness.Context.BeforeSave = _ => throw new DbUpdateException($"Injected {mission} turn-in failure.");
            try
            {
                Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, receiver.EntityId, mission, choice));
            }
            finally
            {
                harness.Context.BeforeSave = null;
            }
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.IsTrue(harness.Client.Player.Missions[mission].Completeable, "still to be turned in");
            harness.Drain();
        }

        private static bool Offers(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.MissionDispense, out var offered) &&
            ((Dictionary<uint, MissionInfo>)offered).ContainsKey(mission);

        private static void Accept(WildernessRuntimeTestHarness harness, Creature npc, uint mission)
        {
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, npc.EntityId, mission), $"{mission} accepted");
            harness.Drain();
        }

        /// <summary>The objectives of <paramref name="mission"/> that <paramref name="npc"/> offers to hear now.</summary>
        private static uint[] Topics(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.ObjectiveComplete, out var value)
                ? ((List<CompleteableObjectives>)value).Where(topic => topic.MissionId == mission)
                    .Select(topic => (uint)topic.ObjectiveId).ToArray()
                : System.Array.Empty<uint>();

        /// <summary>The NPC hears the objective, and it is done.</summary>
        private static void Talk(WildernessRuntimeTestHarness harness, Creature npc, uint mission, uint objective)
        {
            harness.MoveTo(npc.Position);
            harness.Drain();
            CollectionAssert.AreEqual(new[] { objective }, Topics(harness, npc, mission), $"{mission}.{objective} topic");
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, mission, objective, 1), $"{mission}.{objective}");
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, mission, objective));
        }

        private static MissionObjectiveState Objective(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].State;

        private static uint Counter(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].Counters[0];

        private static void AssertOnTheGround(MapChannel map, Vector3 position, string what)
        {
            var ground = map.NavMesh.GroundHeight(position);
            Assert.IsNotNull(ground, $"navmesh under {what}");
            Assert.AreEqual(ground.Value, position.Y, 0.5f, $"{what} on the ground");
        }

        /// <summary>A complete path on the navmesh from <paramref name="from"/> to <paramref name="to"/>.</summary>
        private static void AssertPath(MapChannel map, Vector3 from, Vector3 to, string what) =>
            Assert.IsTrue(map.NavMesh.FindPath(from, to, out var complete) != null && complete, $"{what}: a path");

        /// <summary>In the navmesh part of <paramref name="anchor"/>: the caves' parts have no path between them.</summary>
        private static void AssertInPart(MapChannel map, Vector3 anchor, Vector3 position, string what) =>
            AssertPath(map, anchor, position, what);

        private static Vector3 ToVector((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);

        private static Vector3 ToVector(Rasa.Missions.Scenes.ScenePosition at) => new(at.X, at.Y, at.Z);
    }
}
