using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Marshes;
using Rasa.Structures;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Marshes
{
    using static MarshesFalconHoldTests;
    using static MarshesPaludosTests;
    using static PlateauCampResistanceTests;

    /// <summary>
    /// Marshes set 8, Gun Control (High Point Retreat), played through as a player plays it: Retread Lou's Gun Control (1865:
    /// Thrax Warrant Officer armor fragments and Lightbender rifle power cores), Part II (1866: ten Mox with his discharge
    /// container) and Part III (1867: the CGH 9000 to Shaman Masai, or sold to Retread Duvall on the way), and its two ends,
    /// Part IV back to Lou (1868, the gun given) or to Duvall (1904, the gun sold).
    ///
    /// Why these tests: Lou, Masai and the Lightbenders are new on the server; 1865 drops two items from two kinds of
    /// creature, 1866 and 1867 give their items at the accept, 1867 branches on a choice at another NPC than its giver and
    /// receiver, and its ending flag opens only one of two follow-ups, one of them offered by radio. An NPC off the navmesh,
    /// an offer at the wrong NPC or before its prerequisite, a drop on the wrong side of its chance or from the wrong creature,
    /// a kill taken for a pickup, progress lost at a reload, an item given with no room or kept after an abandon, a sale that
    /// leaves the delivery open, both follow-ups offered, or a turn-in that pays twice would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesGunControlTests
    {
        private const uint One = MarshesGunControlV1.MissionId;
        private const uint Two = MarshesGunControlPartTwoV1.MissionId;
        private const uint Three = MarshesGunControlPartThreeV1.MissionId;
        private const uint ToLou = MarshesGunControlPartFourV1.LouMissionId;
        private const uint ToDuvall = MarshesGunControlPartFourV1.DuvallMissionId;
        private const uint Lou = MarshesGunControlNpcsV1.Lou, Masai = MarshesGunControlNpcsV1.Masai;
        private const uint Duvall = MarshesStalkerWoodsNpcsV1.Duvall, Jeska = MarshesStalkerWoodsNpcsV1.Jeska;
        private const uint Technician = MarshesGunControlV1.ThraxTechnician, Lightbender = MarshesGunControlV1.Lightbender;
        private const uint Mox = MarshesPaludosNpcsV1.Mox;
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests
        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack: fills the consumables bag

        /// <summary>The Stalker Woods (High Point Retreat) and Plateau Point waypoints (World DB teleporters).</summary>
        private static readonly Vector3 HighPointRetreatWaypoint = new(-784.272f, 241.4954f, -547.8776f);
        private static readonly Vector3 PlateauPointWaypoint = new(-726.85f, 218.05f, 227.62f);

        private static readonly uint[] TechnicianPools = MarshesGunControlV1.TechnicianPools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] MoxPools = MarshesGunControlPartTwoV1.MoxPools.Select(pool => pool.Pool).ToArray();

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        [TestMethod]
        public void LouMasaiAndTheLightbendersStandAtTheirPlaces()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            foreach (var (id, name, package, at, from) in new[]
            {
                (Lou, MarshesGunControlNpcsV1.LouName, 2356U, MarshesGunControlNpcsV1.LouAt, HighPointRetreatWaypoint),
                (Masai, MarshesGunControlNpcsV1.MasaiName, 991U, MarshesGunControlNpcsV1.MasaiAt, PlateauPointWaypoint)
            })
            {
                var npc = Npc(marshes, id);
                Assert.AreEqual(name, npc.NameId);
                Assert.AreEqual(package, npc.Npc.NpcPackageId, $"{id}: the client's package");
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory, $"{id} is no foe");
                Assert.IsLessThan(0.1f, Vector2.Distance(new((float)at.X, (float)at.Z), new(npc.Position.X, npc.Position.Z)), $"{id} at the place");
                AssertOnGround(marshes, npc.Position, $"{id}");
                Assert.IsTrue(marshes.NavMesh.FindPath(from, npc.Position, out var reached) != null && reached, $"{id} is walked to");
                Assert.IsTrue(marshes.NavMesh.FindPath(HighPointRetreatWaypoint, npc.Position, out var fromCamp) != null && fromCamp,
                    $"{id} is walked to from the camp");
            }
            var masai = Npc(marshes, Masai);
            Assert.AreEqual(7035U, (uint)masai.EntityClass, "a Forean shaman");

            Spawn(marshes, MarshesGunControlNpcsV1.LightbenderPool);
            var lightbenders = Alive(marshes).Where(creature => creature.SpawnPool?.DbId == MarshesGunControlNpcsV1.LightbenderPool).Distinct().ToArray();
            Assert.HasCount(3, lightbenders, "three Lightbenders");
            foreach (var lightbender in lightbenders)
            {
                Assert.AreEqual((Lightbender, 36U, TargetCategory.Hostile), (lightbender.DbId, lightbender.Level, lightbender.TargetCategory));
                Assert.AreEqual(7120U, (uint)lightbender.EntityClass);
                Assert.AreEqual(10406, (int)lightbender.Attributes[Attributes.Health].NormalMax, "the level-36 Hunter Lieutenant's hit points");
            }
            var place = MarshesGunControlNpcsV1.LightbenderAt;
            var pool = new Vector3((float)place.X, (float)place.Y, (float)place.Z);
            AssertOnGround(marshes, pool, "the Lightbender pool");
            Assert.IsTrue(marshes.NavMesh.FindPath(HighPointRetreatWaypoint, pool, out var walked) != null && walked, "the Lightbenders are walked to");
        }

        [TestMethod]
        public void GunControlTakesFragmentsAndCoresToLou()
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(
                lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            var marshes = EnterMarshes(harness);
            var lou = Npc(marshes, Lou);
            var jeska = Npc(marshes, Jeska);
            Spawn(marshes, TechnicianPools.Append(MarshesGunControlNpcsV1.LightbenderPool).Concat(MoxPools).ToArray());

            var offer = WildernessRanjaGorgeTests.Offer(harness, lou, One);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesGunControlV1.HazmatGloves, MarshesGunControlV1.MotorAssistGloves, MarshesGunControlV1.BioHelmet,
                    MarshesGunControlV1.ReflectiveGloves
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, lou, Two), "Part II waits for it");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, jeska, One), "not Jeska's to give");
            percentile = 0;
            Assert.IsFalse(Holds(Kill(harness, marshes, Technician), MarshesGunControlV1.FragmentTemplate), "no fragment before the mission");
            Assert.IsFalse(Holds(Kill(harness, marshes, Lightbender), MarshesGunControlV1.CoreTemplate), "no core before the mission");
            Accept(harness, lou, One);
            Assert.IsEmpty(Topics(harness, lou, One), "Lou hears no objective: the parts count as they are picked up");

            // A technician gives a fragment only under 60, a Lightbender a core only under 50; a Mox neither.
            percentile = 0;
            var mox = Kill(harness, marshes, Mox);
            Assert.IsFalse(Holds(mox, MarshesGunControlV1.FragmentTemplate) || Holds(mox, MarshesGunControlV1.CoreTemplate), "a Mox has no part");
            percentile = MarshesGunControlV1.FragmentChance;
            Assert.IsFalse(Holds(Kill(harness, marshes, Technician), MarshesGunControlV1.FragmentTemplate), "60 misses");
            percentile = MarshesGunControlV1.CoreChance;
            Assert.IsFalse(Holds(Kill(harness, marshes, Lightbender), MarshesGunControlV1.CoreTemplate), "50 misses");
            percentile = MarshesGunControlV1.CoreChance - 1;
            var core = Kill(harness, marshes, Lightbender);
            Assert.IsTrue(Holds(core, MarshesGunControlV1.CoreTemplate), "49 drops a core");
            Assert.IsFalse(Holds(core, MarshesGunControlV1.FragmentTemplate), "a Lightbender has no fragment");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, MarshesGunControlV1.CoreTemplate), "a death is not a pickup");
            PickUp(harness, core);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesGunControlV1.CoreTemplate));
            harness.Drain();

            var completed = 0;
            var completeable = 0;
            void Gather(uint creature, uint[] pools, uint template, uint count, int chance)
            {
                percentile = chance - 1;
                while (WildernessRanjaGorgeTests.Held(harness, template) < count)
                {
                    if (!Alive(marshes).Any(candidate => candidate.DbId == creature && candidate.Npc == null))
                        Respawn(marshes, pools);
                    var held = WildernessRanjaGorgeTests.Held(harness, template);
                    var loot = Kill(harness, marshes, creature);
                    Assert.IsTrue(Holds(loot, template), $"{chance - 1} drops");
                    PickUp(harness, loot);
                    Assert.AreEqual(held + 1, WildernessRanjaGorgeTests.Held(harness, template));
                    var drained = harness.Drain();
                    completed += drained.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == One);
                    completeable += drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == One && packet.IsCompleteable);
                }
            }
            Gather(Lightbender, new[] { MarshesGunControlNpcsV1.LightbenderPool }, MarshesGunControlV1.CoreTemplate, MarshesGunControlV1.CoreCount,
                MarshesGunControlV1.CoreChance);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, One, MarshesGunControlV1.Cores));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, One, MarshesGunControlV1.Fragments));
            Assert.AreEqual((1, 0), (completed, completeable), "the cores alone do not finish it");
            Gather(Technician, TechnicianPools, MarshesGunControlV1.FragmentTemplate, 5, MarshesGunControlV1.FragmentChance);
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual((5U, MarshesGunControlV1.CoreCount), (WildernessRanjaGorgeTests.Held(harness, MarshesGunControlV1.FragmentTemplate),
                WildernessRanjaGorgeTests.Held(harness, MarshesGunControlV1.CoreTemplate)), "kept at a reload");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, One, MarshesGunControlV1.Fragments));
            harness.Drain();
            Gather(Technician, TechnicianPools, MarshesGunControlV1.FragmentTemplate, MarshesGunControlV1.FragmentCount,
                MarshesGunControlV1.FragmentChance);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, One, MarshesGunControlV1.Fragments));
            Assert.AreEqual((2, 1), (completed, completeable), "ObjectiveCompleted once each, MissionCompleteable once");

            harness.MoveTo(jeska.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, jeska.EntityId, One, 1), "not Jeska's to take");
            FullBagWaits(harness, lou, One, 1, MarshesGunControlV1.HazmatGloves);
            FailedTurnInPaysNothing(harness, lou, One, 1);
            Assert.AreEqual(MarshesGunControlV1.FragmentCount, WildernessRanjaGorgeTests.Held(harness, MarshesGunControlV1.FragmentTemplate),
                "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, lou, One, 1, MarshesGunControlV1.Experience, MarshesGunControlV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == One), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesGunControlV1.MotorAssistGloves), "the selected gloves");
            Assert.AreEqual((0U, 0U), (WildernessRanjaGorgeTests.Held(harness, MarshesGunControlV1.FragmentTemplate),
                WildernessRanjaGorgeTests.Held(harness, MarshesGunControlV1.CoreTemplate)), "Lou keeps them");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, lou, One), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, lou, Two), "Part II follows");
        }

        /// <summary>A core stays on the corpse while the bag is full; an abandon keeps the parts and the mission is offered again.</summary>
        [TestMethod]
        public void ThePartsWaitForRoomAndGunControlCanBeAbandoned()
        {
            using var harness = WildernessRuntimeTestHarness.Create(
                lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            var marshes = EnterMarshes(harness);
            var lou = Npc(marshes, Lou);
            Spawn(marshes, MarshesGunControlNpcsV1.LightbenderPool);
            Accept(harness, lou, One);
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);

            var loot = Kill(harness, marshes, Lightbender);
            PickUp(harness, loot);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, MarshesGunControlV1.CoreTemplate), "no room");
            Assert.IsTrue(Holds(loot, MarshesGunControlV1.CoreTemplate), "still on the corpse");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, loot);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesGunControlV1.CoreTemplate), "picked up once there is room");
            harness.Drain();

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, One));
            harness.Drain();
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesGunControlV1.CoreTemplate), "the core is kept");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, lou, One), "offered again after an abandon");
            Accept(harness, lou, One);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, One, MarshesGunControlV1.Cores), "one of five");
        }

        [TestMethod]
        public void PartTwoKillsTenMoxWithLousContainer()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var lou = Npc(marshes, Lou);
            var jeska = Npc(marshes, Jeska);
            Spawn(marshes, MoxPools.Append(MarshesGunControlNpcsV1.LightbenderPool).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, lou, Two), "not before Gun Control");
            SeedCompletedMarshes(harness, One);
            harness.Drain();

            var offer = WildernessRanjaGorgeTests.Offer(harness, lou, Two);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesGunControlPartTwoV1.MechHelmet, MarshesGunControlPartTwoV1.GravitonLegs,
                    MarshesGunControlPartTwoV1.StealthHelmet, MarshesGunControlPartTwoV1.BioLegs
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, jeska, Two), "not Jeska's to give");

            // No room for the container: no mission. One slot freed, it comes with it.
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.MoveTo(lou.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, lou.EntityId, Two), "no room for the container");
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(Two));
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Accept(harness, lou, Two);
            Assert.AreEqual(1U, Container(harness), "the discharge container");
            Assert.IsEmpty(Topics(harness, lou, Two), "Lou hears no objective");

            Kill(harness, marshes, Lightbender);
            harness.Drain();
            Assert.AreEqual(0U, Counter(harness), "a Lightbender is no Mox");
            var completed = 0;
            var completeable = 0;
            for (var kill = 1U; kill <= MarshesGunControlPartTwoV1.MoxCount; kill++)
            {
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Two, MarshesGunControlPartTwoV1.Gather));
                if (!Alive(marshes).Any(candidate => candidate.DbId == Mox && candidate.Npc == null))
                    Respawn(marshes, MoxPools);
                Kill(harness, marshes, Mox);
                var drained = harness.Drain();
                completed += drained.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Two);
                completeable += drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Two && packet.IsCompleteable);
                if (kill < MarshesGunControlPartTwoV1.MoxCount)
                    Assert.AreEqual(kill, Counter(harness), $"kill {kill} counted");
                if (kill == 5)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(5U, Counter(harness), "kept at a reload");
                    harness.Drain();
                }
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Two, MarshesGunControlPartTwoV1.Gather));
            Assert.AreEqual((1, 1), (completed, completeable), "ObjectiveCompleted and MissionCompleteable once");

            harness.MoveTo(jeska.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, jeska.EntityId, Two, 0), "not Jeska's to take");
            FullBagWaits(harness, lou, Two, 0, MarshesGunControlPartTwoV1.MechHelmet);
            FailedTurnInPaysNothing(harness, lou, Two, 0);
            Assert.AreEqual(1U, Container(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, lou, Two, 0,
                MarshesGunControlPartTwoV1.Experience, MarshesGunControlPartTwoV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Two), "MissionRewarded once");
            Assert.AreEqual(0U, Container(harness), "Lou takes the container");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, lou, Two), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, lou, Three), "Part III follows");
        }

        /// <summary>An abandon takes the container and the count away; taken again, a new container and a count from 0.</summary>
        [TestMethod]
        public void PartTwoAbandonedTakesTheContainerBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var lou = Npc(marshes, Lou);
            Spawn(marshes, MoxPools);
            SeedCompletedMarshes(harness, One);
            harness.Drain();
            Accept(harness, lou, Two);
            Kill(harness, marshes, Mox);
            harness.Drain();
            Assert.AreEqual(1U, Counter(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Two));
            harness.Drain();
            Assert.AreEqual(0U, Container(harness), "the container goes with the mission");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, lou, Two), "offered again after an abandon");
            Accept(harness, lou, Two);
            Assert.AreEqual(1U, Container(harness), "a new container");
            Assert.AreEqual(0U, Counter(harness), "from 0");
        }

        /// <summary>Duvall's offer turned down: the gun goes to Masai, the flag says given, Part IV for Lou follows at Masai.</summary>
        [TestMethod]
        public void PartThreeGivesTheGunToMasaiAndLouHearsOfIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var lou = Npc(marshes, Lou);
            var masai = Npc(marshes, Masai);
            var duvall = Npc(marshes, Duvall);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, lou, Three), "not before Part II");
            SeedCompletedMarshes(harness, One, Two);
            harness.Drain();

            var offer = WildernessRanjaGorgeTests.Offer(harness, lou, Three);
            Assert.IsEmpty(offer.SelectableReward, "XP and credits only");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, masai, Three), "not Masai's to give");
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.MoveTo(lou.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, lou.EntityId, Three), "no room for the gun");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Accept(harness, lou, Three);
            Assert.AreEqual(1U, Gun(harness), "the CGH 9000");
            Assert.IsEmpty(Topics(harness, lou, Three), "Lou hears no objective");
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Three, MarshesGunControlPartThreeV1.Return), "3 hidden");
            CollectionAssert.AreEqual(new[] { MarshesGunControlPartThreeV1.Deliver }, Topics(harness, masai, Three), "Masai hears the delivery");
            CollectionAssert.AreEqual(new[] { (MarshesGunControlPartThreeV1.Sidetracked, 1U) }, Choices(harness, duvall, Three), "Duvall's offer");

            Choose(harness, duvall, MarshesGunControlPartThreeV1.Refuse);
            var refused = harness.Drain();
            Assert.AreEqual(1, refused.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Three &&
                packet.ObjectiveId == MarshesGunControlPartThreeV1.Sidetracked), "ObjectiveCompleted once");
            Assert.IsFalse(refused.OfType<MissionCompleteablePacket>().Any(packet => packet.MissionId == Three && packet.IsCompleteable),
                "not done by Duvall");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Three, MarshesGunControlPartThreeV1.Sidetracked), "turned down");
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Three, MarshesGunControlPartThreeV1.Return), "3 stays hidden");
            Assert.AreEqual(1U, Gun(harness), "the gun kept");
            Assert.IsEmpty(Choices(harness, duvall, Three), "asked once");
            Assert.IsFalse(harness.Client.Player.Missions[Three].Completeable);
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Three, MarshesGunControlPartThreeV1.Sidetracked), "kept at a reload");
            harness.Drain();

            Hear(harness, masai, Three, MarshesGunControlPartThreeV1.Deliver);
            Assert.AreEqual(0U, Gun(harness), "Masai takes the gun");
            Assert.AreEqual(MarshesGunControlPartThreeV1.Gave, Flag(harness), "the gun given");
            Assert.IsTrue(harness.Client.Player.Missions[Three].Completeable);
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, lou.EntityId, Three, null), "not Lou's to take");
            FailedTurnInPaysNothing(harness, masai, Three, null);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, masai, Three, null,
                MarshesGunControlPartThreeV1.Experience, MarshesGunControlPartThreeV1.Credits);
            var rewarded = harness.Drain();
            Assert.AreEqual(1, rewarded.OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Three), "MissionRewarded once");
            Assert.IsFalse(rewarded.OfType<DispenseRadioMissionPacket>().Any(packet => packet.MissionId == ToDuvall), "Duvall does not call");
            Assert.IsFalse(harness.Manager.TryAcceptRadioMission(harness.Client, ToDuvall), "Duvall's end is closed");

            // Part IV for Lou: Masai's to give, Lou's to hear.
            var four = WildernessRanjaGorgeTests.Offer(harness, masai, ToLou);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesGunControlPartFourV1.EmpMachineGun, MarshesGunControlPartFourV1.Shotgun,
                    MarshesGunControlPartFourV1.VirulentInjectionGun, MarshesGunControlPartFourV1.SonicRocketLauncher
                },
                four.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, lou, ToLou), "not Lou's to give");
            Accept(harness, masai, ToLou);
            Assert.IsEmpty(Topics(harness, masai, ToLou), "Masai hears no objective");
            Assert.IsEmpty(Topics(harness, duvall, ToLou), "Duvall hears no objective");
            Hear(harness, lou, ToLou, MarshesGunControlPartFourV1.Return);
            Assert.IsTrue(harness.Client.Player.Missions[ToLou].Completeable);
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, masai.EntityId, ToLou, 1), "not Masai's to take");
            FullBagWaits(harness, lou, ToLou, 1, MarshesGunControlPartFourV1.EmpMachineGun);
            FailedTurnInPaysNothing(harness, lou, ToLou, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, lou, ToLou, 1,
                MarshesGunControlPartFourV1.Experience, MarshesGunControlPartFourV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == ToLou), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesGunControlPartFourV1.Shotgun), "the selected shotgun");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, masai, ToLou), "done once");
        }

        /// <summary>The gun sold: the delivery fails, Masai hears the empty-handed return, Duvall calls with Part IV and pays it.</summary>
        [TestMethod]
        public void PartThreeSellsTheGunToDuvallWhoCallsBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var lou = Npc(marshes, Lou);
            var masai = Npc(marshes, Masai);
            var duvall = Npc(marshes, Duvall);
            SeedCompletedMarshes(harness, One, Two);
            harness.Drain();
            Accept(harness, lou, Three);

            Choose(harness, duvall, MarshesGunControlPartThreeV1.Sell);
            var sold = harness.Drain();
            Assert.AreEqual(1, sold.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Three &&
                packet.ObjectiveId == MarshesGunControlPartThreeV1.Sidetracked), "ObjectiveCompleted once");
            Assert.IsFalse(sold.OfType<MissionCompleteablePacket>().Any(packet => packet.MissionId == Three && packet.IsCompleteable),
                "not done by the sale");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Three, MarshesGunControlPartThreeV1.Sidetracked), "taken");
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, Three, MarshesGunControlPartThreeV1.Deliver), "no delivery");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Three, MarshesGunControlPartThreeV1.Return), "3 open");
            Assert.AreEqual(1, sold.OfType<ObjectiveFailedPacket>().Count(packet => packet.MissionId == Three &&
                packet.ObjectiveId == MarshesGunControlPartThreeV1.Deliver), "ObjectiveFailed once");
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[Three].State, "the mission goes on");
            Assert.AreEqual(0U, Gun(harness), "Duvall has the gun");
            Assert.AreEqual(0U, Flag(harness), "nothing decided yet");
            CollectionAssert.AreEqual(new[] { MarshesGunControlPartThreeV1.Return }, Topics(harness, masai, Three), "Masai hears only the return");
            Assert.IsEmpty(Choices(harness, duvall, Three), "asked once");
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual((MissionObjectiveState.Failed, MissionObjectiveState.Incomplete),
                (Objective(harness, Three, MarshesGunControlPartThreeV1.Deliver), Objective(harness, Three, MarshesGunControlPartThreeV1.Return)),
                "kept at a reload");
            harness.Drain();

            Hear(harness, masai, Three, MarshesGunControlPartThreeV1.Return);
            Assert.AreEqual(MarshesGunControlPartThreeV1.Sold, Flag(harness), "the gun sold");
            Assert.IsTrue(harness.Client.Player.Missions[Three].Completeable);
            // No item in 1867's reward: a full bag does not hold it back (PartThreeGivesTheGunToMasai... checks the same turn-in).
            WildernessAliaBranchesTests.FillCategory(harness, ConsumableFiller, 50);
            FailedTurnInPaysNothing(harness, masai, Three, null);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, masai, Three, null,
                MarshesGunControlPartThreeV1.Experience, MarshesGunControlPartThreeV1.Credits);
            var rewarded = harness.Drain();
            Assert.AreEqual(1, rewarded.OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Three), "MissionRewarded once");
            Assert.AreEqual(1, rewarded.OfType<DispenseRadioMissionPacket>().Count(packet => packet.MissionId == ToDuvall), "Duvall calls once");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, masai, ToLou), "Lou's end is closed");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, masai, ToDuvall), "a radio mission, not Masai's offer");

            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, ToDuvall));
            Assert.AreEqual(1, harness.Drain().OfType<MissionGainedPacket>().Count(packet => packet.MissionId == ToDuvall), "MissionGained once");
            // Abandoned, Duvall calls again on arrival on the Marshes (Retread Planning's way).
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, ToDuvall));
            harness.Drain();
            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.IsTrue(harness.Drain().OfType<DispenseRadioMissionPacket>().Any(packet => packet.MissionId == ToDuvall), "offered again on arrival");
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, ToDuvall));
            harness.Drain();
            Assert.IsEmpty(Topics(harness, lou, ToDuvall), "Lou hears no objective");
            Hear(harness, duvall, ToDuvall, MarshesGunControlPartFourV1.Return);
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, lou.EntityId, ToDuvall, 3), "not Lou's to take");
            FullBagWaits(harness, duvall, ToDuvall, 3, MarshesGunControlPartFourV1.EmpMachineGun);
            FailedTurnInPaysNothing(harness, duvall, ToDuvall, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, duvall, ToDuvall, 3,
                MarshesGunControlPartFourV1.Experience, MarshesGunControlPartFourV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == ToDuvall), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesGunControlPartFourV1.SonicRocketLauncher), "the selected launcher");
            Assert.IsFalse(harness.Manager.TryAcceptRadioMission(harness.Client, ToDuvall), "done once");
        }

        /// <summary>Straight to Masai: Duvall's offer closes with the gun gone (2 stays open, optional), the mission is done.</summary>
        [TestMethod]
        public void PartThreeDeliveredStraightToMasaiClosesDuvallsOffer()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var lou = Npc(marshes, Lou);
            var masai = Npc(marshes, Masai);
            var duvall = Npc(marshes, Duvall);
            SeedCompletedMarshes(harness, One, Two);
            harness.Drain();
            Accept(harness, lou, Three);

            Hear(harness, masai, Three, MarshesGunControlPartThreeV1.Deliver);
            Assert.IsTrue(harness.Client.Player.Missions[Three].Completeable, "done without Duvall");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Three, MarshesGunControlPartThreeV1.Sidetracked));
            Assert.IsEmpty(Choices(harness, duvall, Three), "no gun, no offer");
            Choose(harness, duvall, MarshesGunControlPartThreeV1.Sell);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Three, MarshesGunControlPartThreeV1.Sidetracked), "no sale");
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Three, MarshesGunControlPartThreeV1.Return));
            Assert.AreEqual(MarshesGunControlPartThreeV1.Gave, Flag(harness));
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, masai, Three, null,
                MarshesGunControlPartThreeV1.Experience, MarshesGunControlPartThreeV1.Credits);
            harness.Drain();
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, masai, ToLou), "Part IV for Lou");
        }

        /// <summary>
        /// To-fix 28, the playtest's path: Part III given by a GM (.givemission) with Parts I and II never done, a reload
        /// between the sale and Masai. Duvall still calls with 1904 at the turn-in.
        /// </summary>
        [TestMethod]
        public void PartThreeGivenByAGmStillGetsDuvallsCall()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var masai = Npc(marshes, Masai);
            var duvall = Npc(marshes, Duvall);
            harness.Drain();
            Assert.IsTrue(harness.Manager.TryGiveMission(harness.Client, Three, "test"));
            harness.Drain();

            Choose(harness, duvall, MarshesGunControlPartThreeV1.Sell);
            harness.Drain();
            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();
            Hear(harness, masai, Three, MarshesGunControlPartThreeV1.Return);
            harness.Drain();
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, masai, Three, null,
                MarshesGunControlPartThreeV1.Experience, MarshesGunControlPartThreeV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<DispenseRadioMissionPacket>().Count(packet => packet.MissionId == ToDuvall),
                "Duvall calls once");
        }

        /// <summary>An abandon after the sale: no gun, no flag, neither end; taken again, a new gun and a new choice.</summary>
        [TestMethod]
        public void PartThreeAbandonedAfterTheSaleDecidesNothing()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var lou = Npc(marshes, Lou);
            var masai = Npc(marshes, Masai);
            var duvall = Npc(marshes, Duvall);
            SeedCompletedMarshes(harness, One, Two);
            harness.Drain();
            Accept(harness, lou, Three);
            Choose(harness, duvall, MarshesGunControlPartThreeV1.Sell);
            Assert.AreEqual(0U, Gun(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Three));
            harness.Drain();
            Assert.AreEqual(0U, Flag(harness), "no ending");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, masai, ToLou), "no Part IV for Lou");
            Assert.IsFalse(harness.Manager.TryAcceptRadioMission(harness.Client, ToDuvall), "no Part IV for Duvall");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, lou, Three), "offered again after an abandon");
            Accept(harness, lou, Three);
            Assert.AreEqual(1U, Gun(harness), "a new gun");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Three, MarshesGunControlPartThreeV1.Deliver));
            CollectionAssert.AreEqual(new[] { (MarshesGunControlPartThreeV1.Sidetracked, 1U) }, Choices(harness, duvall, Three), "a new choice");
        }

        /// <summary>Part IV for Lou abandoned: the talk is undone; Masai offers it again and Lou hears it once more.</summary>
        [TestMethod]
        public void PartFourForLouCanBeAbandoned()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var lou = Npc(marshes, Lou);
            var masai = Npc(marshes, Masai);
            SeedCompletedMarshes(harness, One, Two);
            harness.Drain();
            Accept(harness, lou, Three);
            Hear(harness, masai, Three, MarshesGunControlPartThreeV1.Deliver);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, masai, Three, null,
                MarshesGunControlPartThreeV1.Experience, MarshesGunControlPartThreeV1.Credits);
            harness.Drain();
            Accept(harness, masai, ToLou);
            Hear(harness, lou, ToLou, MarshesGunControlPartFourV1.Return);

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, ToLou));
            harness.Drain();
            Assert.IsFalse(harness.Client.Player.Missions.TryGetValue(ToLou, out var gone) && gone.State == MissionState.Active, "gone");
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, lou.EntityId, ToLou, 1), "nothing to turn in");
            Assert.AreEqual(MarshesGunControlPartThreeV1.Gave, Flag(harness), "the ending stays");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, masai, ToLou), "offered again after an abandon");
            Accept(harness, masai, ToLou);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, ToLou, MarshesGunControlPartFourV1.Return), "from the start");
            Hear(harness, lou, ToLou, MarshesGunControlPartFourV1.Return);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, lou, ToLou, 0,
                MarshesGunControlPartFourV1.Experience, MarshesGunControlPartFourV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == ToLou), "MissionRewarded once");
        }

        /// <summary>The NPC hears the objective (only that one): ObjectiveCompleted and MissionCompleteable once each.</summary>
        private static void Hear(WildernessRuntimeTestHarness harness, Creature npc, uint mission, uint objective)
        {
            AssertHandInTopic(Conversation(harness, npc), mission, objective);
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, mission, objective, 1), $"{mission}.{objective}");
            var drained = harness.Drain();
            Assert.AreEqual(1, drained.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == mission &&
                packet.ObjectiveId == objective), $"{mission}.{objective} ObjectiveCompleted once");
            Assert.AreEqual(1, drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == mission && packet.IsCompleteable),
                $"{mission} MissionCompleteable once");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, mission, objective));
        }

        private static uint Container(WildernessRuntimeTestHarness harness) =>
            WildernessRanjaGorgeTests.Held(harness, MarshesGunControlPartTwoV1.ContainerTemplate);

        private static uint Gun(WildernessRuntimeTestHarness harness) =>
            WildernessRanjaGorgeTests.Held(harness, MarshesGunControlPartThreeV1.GunTemplate);

        private static uint Counter(WildernessRuntimeTestHarness harness) =>
            harness.Client.Player.Missions[Two].Objectives[MarshesGunControlPartTwoV1.Gather].Counters[0];

        /// <summary>The ending flag 1867001 as the character database holds it (0: none).</summary>
        private static uint Flag(WildernessRuntimeTestHarness harness)
        {
            using var unit = harness.CreateChar();
            return unit.CharacterFlags.Get(harness.Client.Player.Id).TryGetValue(MarshesGunControlPartThreeV1.GunFlag, out var value) ? value : 0;
        }

        /// <summary>The choice topics of <paramref name="mission"/> that <paramref name="npc"/> offers now (as PoolsSpyChainTests).</summary>
        private static (uint, uint)[] Choices(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.ObjectiveChoice, out var value)
                ? ((List<ChoiceObjectives>)value).Where(topic => topic.MissionId == mission)
                    .Select(topic => ((uint)topic.ObjectiveId, (uint)topic.PlayerFlagId)).ToArray()
                : System.Array.Empty<(uint, uint)>();

        /// <summary>Duvall's choice on 1867's objective 2 (as PoolsSnakePitContactsTests.Choose picks Timlin's).</summary>
        private static void Choose(WildernessRuntimeTestHarness harness, Creature npc, uint choice)
        {
            var npcs = new NpcManager(harness, harness.Manager);
            harness.MoveTo(npc.Position);
            harness.Drain();
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = npc.EntityId });
            npcs.PerformNPCChoice(harness.Client, new PerformNPCChoicePacket
            {
                EntityId = npc.EntityId, MissionId = Three, ObjectiveId = MarshesGunControlPartThreeV1.Sidetracked, PlayerFlagId = 1,
                ChoiceIdx = (int)choice
            });
        }
    }
}
