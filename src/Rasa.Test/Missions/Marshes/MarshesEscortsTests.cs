using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Missions.Content.Marshes;
using Rasa.Missions.Scenes;
using Rasa.Packets;
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
    /// Marshes set 9, the escorts, played through as a player plays them: Shaman Masai's Forean Flu (497: a wounded Forean
    /// Warrior to Field Medic DuBois at Falcon Hold), Retread Duvall's Tread Lightly (888: Retread Otto through the woods to
    /// Retread Vincent), MP Price's AWOL Andy (889: Lieutenant Bruncke found at one of four places and brought back to
    /// Price) and Sergeant Elway's Rest and Recreation (1023: four groups of three soldiers round the lookouts east of Falcon
    /// Hold, 10 minutes each).
    ///
    /// Why these tests: the escorted are the player's own scene creatures that follow them hundreds of metres over the
    /// navmesh (MarshesEscortScene); a follower left behind, an arrival that does not take him away, a death that does not
    /// fail the leg (or one of three that does), a deadline that does not fail it, a group that does not come after the
    /// officer's talk, a run that loses its followers at a reload, Bruncke found at Price (who had his package 386), or a
    /// turn-in that pays twice would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesEscortsTests
    {
        private const uint Flu = MarshesForeanFluV1.MissionId;
        private const uint Tread = MarshesTreadLightlyV1.MissionId;
        private const uint Awol = MarshesAwolAndyV1.MissionId;
        private const uint Rest = MarshesRestAndRecreationV1.MissionId;
        private const uint Masai = MarshesGunControlNpcsV1.Masai, DuBois = MarshesForeanFluV1.DuBois;
        private const uint Duvall = MarshesStalkerWoodsNpcsV1.Duvall, Vincent = MarshesStalkerWoodsNpcsV1.Vincent;
        private const uint Price = MarshesEscortsNpcsV1.Price, Toida = 510160, Elway = MarshesRestAndRecreationV1.Elway;
        private const uint Belfast = MarshesEscortsNpcsV1.Belfast, Delaio = MarshesEscortsNpcsV1.Delaio, Urqhart = MarshesEscortsNpcsV1.Urqhart;
        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack: fills the consumables bag

        /// <summary>The road north of Paludos the search places are walked to from (probe of 2026-11-02).</summary>
        private static readonly Vector3 Road = new(-420f, 216.9f, 472.4f);
        /// <summary>The Falcon Hold waypoint (World DB teleporter).</summary>
        private static readonly Vector3 FalconHoldWaypoint = new(-143.27f, 219.05f, 581.54f);

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        [TestMethod]
        public void TheLieutenantsStandAtTheLookoutsAndPriceKeepsNoBrunckeTalk()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            foreach (var (id, name, package) in new[]
            {
                (Belfast, MarshesEscortsNpcsV1.BelfastName, MarshesEscortsNpcsV1.BelfastPackage),
                (Delaio, MarshesEscortsNpcsV1.DelaioName, MarshesEscortsNpcsV1.DelaioPackage),
                (Urqhart, MarshesEscortsNpcsV1.UrqhartName, MarshesEscortsNpcsV1.UrqhartPackage)
            })
            {
                var npc = Npc(marshes, id);
                Assert.AreEqual((name, package), (npc.NameId, npc.Npc.NpcPackageId), $"{id}: the client's name and package");
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory, $"{id} is no foe");
                AssertOnGround(marshes, npc.Position, $"{id}");
                Assert.IsTrue(marshes.NavMesh.FindPath(FalconHoldWaypoint, npc.Position, out var walked) != null && walked, $"{id} is walked to");
                Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, npc, Rest), $"{id} gives nothing");
            }
            var price = Npc(marshes, Price);
            Assert.AreEqual(MarshesEscortsNpcsV1.PricePackage, price.Npc.NpcPackageId, "Price's own package (U1)");
            foreach (var (_, _, _, at) in MarshesAwolAndyV1.Searches)
                AssertOnGround(marshes, ToVector(at), $"search place {at}");
            foreach (var at in new[] { MarshesForeanFluV1.WarriorAt, MarshesTreadLightlyV1.OttoAt })
                AssertOnGround(marshes, ToVector(at), $"{at}");
        }

        /// <summary>497: the warrior follows from Masai to DuBois, is taken in there; DuBois pays.</summary>
        [TestMethod]
        public void ForeanFluWalksTheWarriorFromMasaiToDuBois()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            PlayOneLeg(harness, marshes, Flu, Npc(marshes, Masai), Npc(marshes, DuBois), Npc(marshes, Toida),
                MarshesEscortsNpcsV1.Warrior, MarshesForeanFluV1.WarriorAt, MarshesForeanFluV1.DuBoisAt, MarshesForeanFluV1.ArrivalRadius,
                new[]
                {
                    MarshesForeanFluV1.BioBoots, MarshesForeanFluV1.MechBoots, MarshesForeanFluV1.GravitonHelmet,
                    MarshesForeanFluV1.ReflectiveLegs
                },
                MarshesForeanFluV1.Experience, MarshesForeanFluV1.Credits, MarshesForeanFluV1.BioBoots);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesForeanFluV1.MechBoots), "the selected boots");
        }

        /// <summary>888: Otto follows from Duvall to Vincent, is taken in there; Vincent pays a stack.</summary>
        [TestMethod]
        public void TreadLightlyWalksOttoFromDuvallToVincent()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            PlayOneLeg(harness, marshes, Tread, Npc(marshes, Duvall), Npc(marshes, Vincent), Npc(marshes, Masai),
                MarshesEscortsNpcsV1.Otto, MarshesTreadLightlyV1.OttoAt, MarshesTreadLightlyV1.VincentAt, MarshesTreadLightlyV1.ArrivalRadius,
                new[]
                {
                    MarshesTreadLightlyV1.FragGrenade, MarshesTreadLightlyV1.EmpBomb, MarshesTreadLightlyV1.CryoGrenade,
                    MarshesTreadLightlyV1.StandardMedPack
                },
                MarshesTreadLightlyV1.Experience, MarshesTreadLightlyV1.Credits, ConsumableFiller);
            Assert.AreEqual(MarshesTreadLightlyV1.EmpBombs, WildernessRanjaGorgeTests.Held(harness, MarshesTreadLightlyV1.EmpBomb), "the selected stack");
            var otto = harness.World.CreatureEntries.Single(row => row.Id == MarshesEscortsNpcsV1.Otto);
            Assert.AreEqual(MarshesEscortsNpcsV1.OttoName, otto.NameId, "Retread Otto");
        }

        /// <summary>The warrior killed on the way: 1 failed, the mission failed; abandoned, Masai offers it again with a new warrior.</summary>
        [TestMethod]
        public void ForeanFluFailsWhenTheWarriorDies()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var masai = Npc(marshes, Masai);
            Begin(harness, marshes, masai, Flu);
            var warrior = Follower(harness, marshes, Flu, MarshesEscortScene.Role(1, 1));
            var failed = Die(harness, marshes, warrior);
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, Flu, MarshesForeanFluV1.Escort));
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[Flu].State);
            Assert.AreEqual(1, failed.OfType<ObjectiveFailedPacket>().Count(packet => packet.MissionId == Flu), "ObjectiveFailed once");

            Assert.IsTrue(harness.Manager.TryClear(harness.Client, Flu), "the failed mission cleared");
            Tick(harness, marshes, 250);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, masai, Flu), "offered again");
            Begin(harness, marshes, masai, Flu);
            Assert.IsNotNull(Follower(harness, marshes, Flu, MarshesEscortScene.Role(1, 1)), "a new warrior");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Flu, MarshesForeanFluV1.Escort));
        }

        /// <summary>Otto abandoned on the way: he is gone, Duvall offers it again.</summary>
        [TestMethod]
        public void TreadLightlyAbandonedTakesOttoAway()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var duvall = Npc(marshes, Duvall);
            Begin(harness, marshes, duvall, Tread);
            var otto = Follower(harness, marshes, Tread, MarshesEscortScene.Role(1, 1));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Tread));
            Tick(harness, marshes, 250);
            Tick(harness, marshes, 250);
            Assert.IsFalse(Living(marshes).Contains(otto), "Otto is gone");
            Assert.IsFalse(harness.Client.Player.Missions.TryGetValue(Tread, out var gone) && gone.State == MissionState.Active);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, duvall, Tread), "offered again");
        }

        /// <summary>
        /// 889: Price cannot hear "Locate Bruncke" (his package went to Bruncke); a search place walked into completes its
        /// optional objective only; Bruncke, standing at the east place, is talked to, follows, and is taken in at Price.
        /// </summary>
        [TestMethod]
        public void AwolAndyFindsBrunckeAndBringsHimBackToPrice()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var price = Npc(marshes, Price);
            var toida = Npc(marshes, Toida);
            var offer = WildernessRanjaGorgeTests.Offer(harness, price, Awol);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesAwolAndyV1.BioLegs, MarshesAwolAndyV1.GravitonLegs, MarshesAwolAndyV1.MechHelmet, MarshesAwolAndyV1.StealthHelmet
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, toida, Awol), "not Toida's to give");
            Begin(harness, marshes, price, Awol);
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Awol, MarshesAwolAndyV1.Escort), "2 hidden");
            Assert.IsEmpty(Topics(harness, price, Awol), "Price hears nothing");
            harness.MoveTo(price.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, price.EntityId, Awol, MarshesAwolAndyV1.Locate, 1),
                "Bruncke is not found at Price");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Awol, MarshesAwolAndyV1.Locate));

            var bruncke = SceneCreature(harness, marshes, Awol, MarshesEscortScene.Role(1, 1));
            Assert.IsNotNull(bruncke, "Bruncke waits from the accept");
            Assert.AreEqual((MarshesEscortsNpcsV1.Bruncke, MarshesEscortsNpcsV1.BrunckeName), (bruncke.DbId, bruncke.NameId));
            Assert.IsLessThan(0.5f, Vector3.Distance(bruncke.Position, ToVector(MarshesAwolAndyV1.BrunckeAt)), "at the east place");
            Assert.AreNotEqual(harness.Client.Player.Id, bruncke.SpawnPool.FollowOwnerCharacterId, "he stands");
            Assert.AreEqual(MarshesEscortsNpcsV1.BrunckePackage, bruncke.Npc?.NpcPackageId, "the client's package 386");

            // A search place: its objective (optional) only.
            var log = new List<PythonPacket>();
            harness.MoveTo(Road);
            harness.Drain();
            var west = MarshesAwolAndyV1.Searches[0];
            WalkTo(harness, marshes, ToVector(west.At), log);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Awol, west.Objective), "the west place searched");
            Assert.AreEqual(1, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Awol && packet.ObjectiveId == west.Objective));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Awol, MarshesAwolAndyV1.Locate), "Bruncke is not there");
            Assert.IsFalse(harness.Client.Player.Missions[Awol].Completeable);

            // Bruncke's talk: 1 done, 2 open, he follows.
            WalkTo(harness, marshes, bruncke.Position + new Vector3(2, 0, 0), log, stopShort: 0);
            AssertHandInTopic(Conversation(harness, bruncke), Awol, MarshesAwolAndyV1.Locate);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, bruncke.EntityId, Awol, MarshesAwolAndyV1.Locate, 1));
            var found = harness.Drain();
            Assert.AreEqual(1, found.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Awol &&
                packet.ObjectiveId == MarshesAwolAndyV1.Locate), "ObjectiveCompleted once");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Awol, MarshesAwolAndyV1.Escort));
            Tick(harness, marshes, 250);
            Assert.AreSame(bruncke, Follower(harness, marshes, Awol, MarshesEscortScene.Role(1, 1)), "the same Bruncke follows");
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, bruncke.EntityId, Awol, MarshesAwolAndyV1.Locate, 1), "asked once");

            ReloadAndRecover(harness, marshes);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Awol, MarshesAwolAndyV1.Escort), "kept at a reload");
            bruncke = Follower(harness, marshes, Awol, MarshesEscortScene.Role(1, 1));

            // Back to Price: the path ends short of him (Paludos's navmesh is cut off), inside the 25 m.
            log.Clear();
            WalkTo(harness, marshes, ToVector(MarshesAwolAndyV1.PriceAt), log, followers: new[] { bruncke },
                arrival: MarshesAwolAndyV1.PriceRadius);
            Assert.AreEqual(1, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Awol &&
                packet.ObjectiveId == MarshesAwolAndyV1.Escort), "ObjectiveCompleted once");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Awol && packet.IsCompleteable),
                "MissionCompleteable once, the other places left open");
            Tick(harness, marshes, 250);
            Assert.IsNull(SceneCreature(harness, marshes, Awol, MarshesEscortScene.Role(1, 1)), "Bruncke is taken to the brig");

            harness.MoveTo(toida.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, toida.EntityId, Awol, 0), "not Toida's to take");
            FullBagWaits(harness, price, Awol, 0, MarshesAwolAndyV1.BioLegs);
            FailedTurnInPaysNothing(harness, price, Awol, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, price, Awol, 3, MarshesAwolAndyV1.Experience, MarshesAwolAndyV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Awol), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesAwolAndyV1.StealthHelmet), "the selected helmet");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, price, Awol), "done once");
        }

        /// <summary>Bruncke killed where he waits fails 1; killed on the way back, 2; abandoned, he is gone and Price offers it again.</summary>
        [TestMethod]
        public void AwolAndyFailsWithBrunckeDeadAndCanBeAbandoned()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var price = Npc(marshes, Price);
            Begin(harness, marshes, price, Awol);
            Die(harness, marshes, SceneCreature(harness, marshes, Awol, MarshesEscortScene.Role(1, 1)));
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, Awol, MarshesAwolAndyV1.Locate), "lost before he is found");
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[Awol].State);

            Assert.IsTrue(harness.Manager.TryClear(harness.Client, Awol), "the failed mission cleared");
            Tick(harness, marshes, 250);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, price, Awol), "offered again");
            Begin(harness, marshes, price, Awol);
            var bruncke = SceneCreature(harness, marshes, Awol, MarshesEscortScene.Role(1, 1));
            harness.MoveTo(bruncke.Position + new Vector3(2, 0, 0));
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, bruncke.EntityId, Awol, MarshesAwolAndyV1.Locate, 1));
            Tick(harness, marshes, 250);
            Die(harness, marshes, Follower(harness, marshes, Awol, MarshesEscortScene.Role(1, 1)));
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, Awol, MarshesAwolAndyV1.Escort), "lost on the way back");
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[Awol].State);

            Assert.IsTrue(harness.Manager.TryClear(harness.Client, Awol), "the failed mission cleared");
            Tick(harness, marshes, 250);
            Begin(harness, marshes, price, Awol);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Awol), "abandoned with Bruncke waiting");
            Tick(harness, marshes, 250);
            Tick(harness, marshes, 250);
            Assert.IsNull(SceneCreature(harness, marshes, Awol, MarshesEscortScene.Role(1, 1)), "Bruncke is gone");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, price, Awol), "offered again");
        }

        /// <summary>
        /// 1023: four legs. Two of the first three soldiers killed, the one left brings the leg in; each officer's talk sends
        /// the next group; a reload keeps the leg and its followers; the last group back at Elway, who pays.
        /// </summary>
        [TestMethod]
        public void RestAndRecreationRotatesTheFourGroups()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var elway = Npc(marshes, Elway);
            var officers = new[] { Npc(marshes, Belfast), Npc(marshes, Delaio), Npc(marshes, Urqhart) };
            var offer = WildernessRanjaGorgeTests.Offer(harness, elway, Rest);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesRestAndRecreationV1.CritHitBonus, MarshesRestAndRecreationV1.BodyBonus, MarshesRestAndRecreationV1.MindBonus,
                    MarshesRestAndRecreationV1.SpiritBonus
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, officers[0], Rest), "not Belfast's to give");
            Begin(harness, marshes, elway, Rest);
            foreach (var leg in MarshesRestAndRecreationV1.Legs.Skip(1))
                Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Rest, leg.Escort), $"{leg.Escort} hidden");
            foreach (var officer in officers)
                Assert.IsEmpty(Topics(harness, officer, Rest), "no report before the leg is in");

            var log = new List<PythonPacket>();
            for (var index = 0; index < MarshesRestAndRecreationV1.Legs.Length; index++)
            {
                var leg = MarshesRestAndRecreationV1.Legs[index];
                var number = (int)leg.Escort;
                var group = MarshesEscortsNpcsV1.Roles(number, 3).Select(role => Follower(harness, marshes, Rest, role)).ToArray();
                foreach (var (soldier, at) in group.Zip(MarshesRestAndRecreationV1.Group(leg.From)))
                {
                    Assert.AreEqual(MarshesEscortsNpcsV1.Soldier, soldier.DbId);
                    Assert.AreNotEqual(TargetCategory.Hostile, soldier.TargetCategory);
                    Assert.AreEqual(4000, (int)soldier.Attributes[Attributes.Health].NormalMax, "the escort's hit points");
                    Assert.IsLessThan(12f, Vector3.Distance(soldier.Position, ToVector(at)), $"leg {number} sets out from its place");
                }
                if (index == 0)
                {
                    Die(harness, marshes, group[0]);
                    Die(harness, marshes, group[1]);
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Rest, leg.Escort), "one of three left");
                    group = new[] { group[2] };
                }
                if (index == 1)
                {
                    ReloadAndRecover(harness, marshes);
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Rest, leg.Escort), "kept at a reload");
                    group = MarshesEscortsNpcsV1.Roles(number, 3).Select(role => Follower(harness, marshes, Rest, role)).ToArray();
                }
                log.Clear();
                WalkTo(harness, marshes, ToVector(leg.To), log, followers: group, arrival: leg.Radius);
                Assert.AreEqual(1, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Rest && packet.ObjectiveId == leg.Escort),
                    $"leg {number} ObjectiveCompleted once");
                Tick(harness, marshes, 250);
                Assert.IsTrue(MarshesEscortsNpcsV1.Roles(number, 3).All(role => SceneCreature(harness, marshes, Rest, role) == null), $"leg {number}'s group goes in");
                if (leg.Report == 0)
                {
                    Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Rest && packet.IsCompleteable),
                        "MissionCompleteable once");
                    break;
                }
                Assert.AreEqual(0, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Rest && packet.IsCompleteable));
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Rest, leg.Report), $"report {leg.Report} open");
                var officer = officers[index];
                foreach (var other in officers.Where(other => other != officer))
                    Assert.IsEmpty(Topics(harness, other, Rest), "only the leg's officer hears the report");
                harness.MoveTo(officer.Position);
                harness.Drain();
                AssertHandInTopic(Conversation(harness, officer), Rest, leg.Report);
                Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, officer.EntityId, Rest, leg.Report, 1));
                Assert.AreEqual(1, harness.Drain().OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Rest &&
                    packet.ObjectiveId == leg.Report), $"report {leg.Report} ObjectiveCompleted once");
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Rest, MarshesRestAndRecreationV1.Legs[index + 1].Escort));
                Tick(harness, marshes, 250);
            }
            Assert.IsTrue(harness.Client.Player.Missions[Rest].Completeable);

            harness.MoveTo(officers[0].Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, officers[0].EntityId, Rest, 0), "not Belfast's to take");
            FullBagWaits(harness, elway, Rest, 0, MarshesRestAndRecreationV1.CritHitBonus);
            FailedTurnInPaysNothing(harness, elway, Rest, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, elway, Rest, 2,
                MarshesRestAndRecreationV1.Experience, MarshesRestAndRecreationV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Rest), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesRestAndRecreationV1.MindBonus), "the selected module");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, elway, Rest), "done once");
        }

        /// <summary>The first group still out after 10 minutes: the leg and the mission fail, the group goes.</summary>
        [TestMethod]
        public void RestAndRecreationFailsAtTheLegsTenMinutes()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var elway = Npc(marshes, Elway);
            Begin(harness, marshes, elway, Rest);
            var group = MarshesEscortsNpcsV1.Roles(1, 3).Select(role => Follower(harness, marshes, Rest, role)).ToArray();

            for (var second = 0; second < MarshesRestAndRecreationV1.LegSeconds - 5; second++)
                Tick(harness, marshes, 1000);
            harness.Manager.EvaluateDeadlines(harness.Client);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Rest, 1), "not yet at 9:55");
            for (var second = 0; second < 10; second++)
                Tick(harness, marshes, 1000);
            harness.Manager.EvaluateDeadlines(harness.Client);
            harness.Drain();
            Tick(harness, marshes, 250);
            Tick(harness, marshes, 250);
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, Rest, 1), "past 10 minutes");
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[Rest].State);
            Assert.IsFalse(group.Any(soldier => Living(marshes).Contains(soldier)), "the group goes");
        }

        /// <summary>
        /// Only the first group is timed: the server keeps one deadline row per character and mission (MissionDeadlineService,
        /// upstream), so leg 2's FailOnDeadline does not arm. Past 600 s on leg 2 the mission goes on (pins the current
        /// behaviour; TaRapedia has 10 minutes per group, a deviation in NOTES-playtest.md).
        /// </summary>
        [TestMethod]
        public void RestAndRecreationTimesOnlyTheFirstGroup()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var elway = Npc(marshes, Elway);
            var belfast = Npc(marshes, Belfast);
            Begin(harness, marshes, elway, Rest);
            var log = new List<PythonPacket>();
            var group = MarshesEscortsNpcsV1.Roles(1, 3).Select(role => Follower(harness, marshes, Rest, role)).ToArray();
            WalkTo(harness, marshes, ToVector(MarshesRestAndRecreationV1.Legs[0].To), log, followers: group, arrival: MarshesRestAndRecreationV1.ArrivalRadius);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Rest, 1));
            harness.MoveTo(belfast.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, belfast.EntityId, Rest, 5, 1));
            Tick(harness, marshes, 250);
            var second = MarshesEscortsNpcsV1.Roles(2, 3).Select(role => Follower(harness, marshes, Rest, role)).ToArray();

            for (var elapsed = 0; elapsed < MarshesRestAndRecreationV1.LegSeconds + 30; elapsed++)
                Tick(harness, marshes, 1000);
            harness.Manager.EvaluateDeadlines(harness.Client);
            harness.Drain();
            Tick(harness, marshes, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Rest, 2), "leg 2 is untimed");
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[Rest].State, "the mission goes on");
            Assert.IsTrue(second.All(soldier => Living(marshes).Contains(soldier)), "the second group still follows");
        }

        /// <summary>All three of a group dead fails the leg; the second leg abandoned fails it, cleared its group goes; offered again.</summary>
        [TestMethod]
        public void RestAndRecreationFailsWithTheWholeGroupDeadAndCanBeAbandoned()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var elway = Npc(marshes, Elway);
            var belfast = Npc(marshes, Belfast);
            Begin(harness, marshes, elway, Rest);
            var group = MarshesEscortsNpcsV1.Roles(1, 3).Select(role => Follower(harness, marshes, Rest, role)).ToArray();
            Die(harness, marshes, group[0]);
            Die(harness, marshes, group[1]);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Rest, 1));
            Die(harness, marshes, group[2]);
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, Rest, 1), "none left");
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[Rest].State);

            Assert.IsTrue(harness.Manager.TryClear(harness.Client, Rest), "the failed mission cleared");
            Tick(harness, marshes, 250);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, elway, Rest), "offered again");
            Begin(harness, marshes, elway, Rest);
            var log = new List<PythonPacket>();
            group = MarshesEscortsNpcsV1.Roles(1, 3).Select(role => Follower(harness, marshes, Rest, role)).ToArray();
            WalkTo(harness, marshes, ToVector(MarshesRestAndRecreationV1.Legs[0].To), log, followers: group, arrival: MarshesRestAndRecreationV1.ArrivalRadius);
            harness.MoveTo(belfast.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, belfast.EntityId, Rest, 5, 1));
            Tick(harness, marshes, 250);
            var second = MarshesEscortsNpcsV1.Roles(2, 3).Select(role => Follower(harness, marshes, Rest, role)).ToArray();

            // A timed mission abandoned fails (MissionApplication.TryPlanAuthoredAbandonment, as Daghda's Urn): the group
            // waits with the failed mission and goes when it is cleared.
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Rest));
            harness.Drain();
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[Rest].State, "abandoned: failed");
            Assert.IsTrue(harness.Manager.TryClear(harness.Client, Rest), "the failed mission cleared");
            Tick(harness, marshes, 250);
            Tick(harness, marshes, 250);
            Assert.IsFalse(second.Any(soldier => Living(marshes).Contains(soldier)), "the second group goes");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, elway, Rest), "offered again");
            Begin(harness, marshes, elway, Rest);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Rest, 1), "from the first leg");
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Rest, 2));
        }

        /// <summary>
        /// The escort script runs an authored sequence as data.sequence does, on its signal and on its timer's elapse
        /// (1033's kiosks ride on an escort scene); a timer or signal of no sequence changes nothing.
        /// </summary>
        [TestMethod]
        public void TheEscortRunsAuthoredSequencesOnSignalsAndTimers()
        {
            const uint authored = 50;
            var scene = MarshesEscortsNpcsV1.Scene(MarshesProductionDestructionV1.HollowayMissionId,
                new MarshesEscortsNpcsV1.Leg(0, 0, 0, null, MarshesRetreadCityNpcsV1.Mech, new[] { MarshesProductionDestructionV1.MechAt }));
            scene.Sequences[authored] = new Rasa.Missions.Content.SceneSequenceDefinition
            {
                World = { new EnsureActorIntent("authored-ensure", MarshesEscortScene.Role(1, 1)) }
            };
            var bindings = scene.Bindings(MarshesMissionDataV1.Revision);
            var run = new SceneRun("run", MarshesMissionDataV1.Revision, "marshes.escort", 1, 1, 0,
                System.Text.Json.JsonSerializer.Serialize(new MarshesEscortScene.Checkpoint("escorting", 1, System.Array.Empty<string>())),
                SceneStatus.Running, 7, MarshesProductionDestructionV1.HollowayMissionId);
            SceneDecision Handle(SceneEventKind kind, uint sequence) => new MarshesEscortScene().Handle(
                new SceneContext(run, bindings, new System.DateTime(2026, 11, 2, 0, 0, 0, System.DateTimeKind.Utc)),
                new SceneObservation(kind, 1, SequenceId: sequence));

            foreach (var kind in new[] { SceneEventKind.Signal, SceneEventKind.TimerElapsed })
            {
                var decision = Handle(kind, authored);
                CollectionAssert.AreEqual(new[] { "authored-ensure" }, decision.WorldIntents.Select(intent => intent.OperationKey).ToArray(), $"{kind}");
                Assert.AreEqual(SceneStatus.Running, decision.Status, $"{kind}: the escort goes on");
                Assert.IsEmpty(Handle(kind, 77).WorldIntents, $"{kind} of no sequence");
            }
            Assert.IsEmpty(Handle(SceneEventKind.TimerElapsed, 0).WorldIntents, "a timer of sequence 0 does not rerun the start");
        }

        /// <summary>
        /// A one-leg escort from <paramref name="giver"/> to <paramref name="receiver"/>: offer, accept, the follower at its
        /// place, a reload on the way, the walk, the arrival (packets once), the follower taken in, the turn-in checks.
        /// </summary>
        private static void PlayOneLeg(WildernessRuntimeTestHarness harness, MapChannel marshes, uint mission, Creature giver,
            Creature receiver, Creature other, uint creature, ScenePosition start, ScenePosition to, double radius, uint[] items,
            uint experience, uint credits, uint filler)
        {
            var offer = WildernessRanjaGorgeTests.Offer(harness, giver, mission);
            CollectionAssert.AreEqual(items, offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, receiver, mission), "not the receiver's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, other, mission), "nor another's");
            Begin(harness, marshes, giver, mission);
            Assert.IsEmpty(Topics(harness, receiver, mission), "nothing to hear: walking in completes it");

            var role = MarshesEscortScene.Role(1, 1);
            var follower = Follower(harness, marshes, mission, role);
            Assert.AreEqual(creature, follower.DbId);
            Assert.AreNotEqual(TargetCategory.Hostile, follower.TargetCategory);
            Assert.AreEqual(4000, (int)follower.Attributes[Attributes.Health].NormalMax, "the escort's hit points");
            Assert.IsLessThan(3f, Vector3.Distance(follower.Position, ToVector(start)), "beside the giver");
            Assert.IsTrue(marshes.NavMesh.FindPath(follower.Position, ToVector(to), out var whole) != null && whole, "the way is walked");

            var log = new List<PythonPacket>();
            harness.MoveTo(giver.Position);
            harness.Drain();
            var path = marshes.NavMesh.FindPath(harness.Client.Player.Position, ToVector(to), out _);
            WalkTo(harness, marshes, path[path.Count / 2], log, followers: new[] { follower });
            ReloadAndRecover(harness, marshes);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, mission, 1), "kept at a reload");
            follower = Follower(harness, marshes, mission, role);
            WalkTo(harness, marshes, ToVector(to), log, followers: new[] { follower }, arrival: radius);
            Assert.AreEqual(1, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == mission), "ObjectiveCompleted once");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == mission && packet.IsCompleteable),
                "MissionCompleteable once");
            Tick(harness, marshes, 250);
            Assert.IsNull(SceneCreature(harness, marshes, mission, role), "taken in");

            harness.MoveTo(other.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, other.EntityId, mission, 1), "not another's to take");
            harness.MoveTo(giver.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, giver.EntityId, mission, 1), "not the giver's to take");
            FullBagWaits(harness, receiver, mission, 1, filler);
            FailedTurnInPaysNothing(harness, receiver, mission, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, receiver, mission, 1, experience, credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == mission), "MissionRewarded once");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, giver, mission), "done once");
        }

        /// <summary>Accepted at the NPC, its scene started.</summary>
        internal static void Begin(WildernessRuntimeTestHarness harness, MapChannel marshes, Creature giver, uint mission)
        {
            Accept(harness, giver, mission);
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            Tick(harness, marshes, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, mission, 1));
        }

        /// <summary>The mission state as the database holds it again, and the scene out and back in (a reconnect): Recovered.</summary>
        internal static void ReloadAndRecover(WildernessRuntimeTestHarness harness, MapChannel marshes)
        {
            WildernessRanjaGorgeTests.Reload(harness);
            harness.Manager.Scenes.Detach(harness.Client, marshes);
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            Tick(harness, marshes, 250);
            Tick(harness, marshes, 250);
        }

        /// <summary>
        /// The player walks the navmesh to <paramref name="to"/> at about 5 m/s (each step a recorded movement, so areas are
        /// entered); the followers keep up. With <paramref name="arrival"/>, they are checked beside the player just before
        /// the area of that radius, and the walk may end short of <paramref name="to"/> but inside it.
        /// </summary>
        internal static void WalkTo(WildernessRuntimeTestHarness harness, MapChannel marshes, Vector3 to, List<PythonPacket> log,
            IReadOnlyList<Creature> followers = null, double? arrival = null, float stopShort = 0)
        {
            var path = marshes.NavMesh.FindPath(harness.Client.Player.Position, to, out var complete);
            Assert.IsNotNull(path, $"a path to {to}");
            Assert.IsTrue(complete || arrival != null && Vector3.Distance(path[^1], to) < arrival.Value - 2,
                $"the path to {to} ends at {path[^1]}");
            var areas = new MissionAreaService(() => harness.Manager);
            var at = harness.Client.Player.Position;
            var checkedBeside = arrival == null;
            foreach (var point in path)
            {
                var steps = System.Math.Max(1, (int)(Vector3.Distance(at, point) / 5));
                var from = at;
                for (var step = 1; step <= steps; step++)
                {
                    var next = Vector3.Lerp(from, point, step / (float)steps);
                    if (!checkedBeside && Vector3.Distance(next, to) < arrival.Value + 3)
                    {
                        CatchUp(harness, marshes, followers);
                        foreach (var follower in followers)
                            Assert.IsLessThan(10f, Vector3.Distance(follower.Position, harness.Client.Player.Position),
                                $"{follower.SpawnPool?.SceneActorRole} beside the player at {harness.Client.Player.Position}, at {follower.Position}" +
                                FollowState(marshes, follower));
                        checkedBeside = true;
                    }
                    harness.MoveTo(next);
                    areas.RecordAcceptedMovement(harness.Client, at, next);
                    log.AddRange(harness.Drain());
                    at = next;
                    log.AddRange(Tick(harness, marshes, 1000));
                }
            }
            if (followers != null && arrival == null)
            {
                CatchUp(harness, marshes, followers);
                foreach (var follower in followers)
                    Assert.IsLessThan(10f, Vector3.Distance(follower.Position, harness.Client.Player.Position),
                        $"{follower.SpawnPool?.SceneActorRole} beside the player at {harness.Client.Player.Position}, at {follower.Position}" +
                        FollowState(marshes, follower));
            }
        }

        /// <summary>Up to 30 s for the followers to come up.</summary>
        internal static void CatchUp(WildernessRuntimeTestHarness harness, MapChannel marshes, IReadOnlyList<Creature> followers)
        {
            for (var tick = 0; tick < 30 && followers.Any(follower => Vector3.Distance(follower.Position, harness.Client.Player.Position) > 8); tick++)
                Tick(harness, marshes, 1000);
        }

        /// <summary>The follower of this run in <paramref name="role"/>: there, following the player.</summary>
        internal static Creature Follower(WildernessRuntimeTestHarness harness, MapChannel marshes, uint mission, string role)
        {
            var creature = SceneCreature(harness, marshes, mission, role);
            Assert.IsNotNull(creature, $"{role} is there");
            Assert.AreEqual(harness.Client.Player.Id, creature.SpawnPool.FollowOwnerCharacterId, $"{role} follows the player");
            Assert.AreEqual(harness.Client.Player.Id, creature.SpawnPool.ScenarioOwnerCharacterId, $"{role} is the player's own");
            Assert.AreEqual(BehaviorManager.BehaviorActionFollow, creature.Controller.CurrentAction, $"{role} is on its follow action");
            return creature;
        }

        /// <summary>What the follower's behaviour holds, for a failed "beside the player" (the 889 flake in long runs).</summary>
        private static string FollowState(MapChannel marshes, Creature follower) =>
            $"; action {follower.Controller.CurrentAction}, path {follower.Controller.Path.Count} nodes (at {follower.Controller.PathIndex}), " +
            $"escort owner {CreatureManager.FindEscortOwner(marshes, follower)?.Player.Id.ToString() ?? "none"}";

        /// <summary>The creature dies (no killer) and the scene runs on; the packets of it.</summary>
        internal static List<PythonPacket> Die(WildernessRuntimeTestHarness harness, MapChannel marshes, Creature creature)
        {
            Assert.IsNotNull(creature);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(marshes, creature, null);
            var packets = harness.Drain().ToList();
            packets.AddRange(Tick(harness, marshes, 250));
            packets.AddRange(Tick(harness, marshes, 250));
            return packets;
        }

        internal static Creature SceneCreature(WildernessRuntimeTestHarness harness, MapChannel map, uint mission, string role)
        {
            using var unit = harness.CreateChar();
            var runs = unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, mission).Select(scene => scene.RunId).ToHashSet();
            return Living(map).SingleOrDefault(creature => creature.SpawnPool?.SceneActorRole == role && runs.Contains(creature.SpawnPool.SceneRunId) &&
                creature.State != CharacterState.Dead);
        }

        /// <summary>As PoolsRetreadCavesTests.Tick; the packets of it.</summary>
        internal static IReadOnlyList<PythonPacket> Tick(WildernessRuntimeTestHarness harness, MapChannel map, long milliseconds)
        {
            harness.UtcNow = harness.UtcNow.AddMilliseconds(milliseconds);
            BehaviorManager.Instance.MapChannelThink(map, System.Math.Min(milliseconds, 1000));
            harness.Manager.Scenes.Tick(map);
            return harness.Drain();
        }

        internal static Vector3 ToVector(ScenePosition at) => new(at.X, at.Y, at.Z);
    }
}
