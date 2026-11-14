using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.ClientData;
using Rasa.Config;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Services.Preloader.Missions.Crucible;
using Rasa.Missions.Scenes;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using static MarshesFalconHoldTests;
    using static MarshesPaludosTests;
    using static PlateauCampResistanceTests;
    using N = AshenAshokaNpcsV1;
    using Echoes = AshenEchoesInTheSandV1;
    using Supply = AshenSupplyLogisticsV1;
    using Tools = AshenPreciousToolsV1;
    using E = MarshesEscortsTests;
    using P = MarshesPreoDasTests;
    using WO = AshenWhiteOasisTests;
    using F = MarshesClientFloorTests;

    /// <summary>
    /// Ashen Desert set 14, round Ashoka Settlement, played through as a player plays it: Captain Botha's Echoes in the Sand
    /// (1736: Researchers Yorish and Jackson escorted to the Eloh obelisk at Daedalus' Landing and back, either's death fails it,
    /// two Bane ambushes), Lieutenant Chase's Supply Logistics (1358: five deliveries, Ashoka to White Oasis Post) and Mechanic
    /// Raji's Precious Tools (1286: his wrench off Overseer Jirex by the pillbox northeast of the Ashoka waypoint). With them
    /// the set's places and walks on the client's floor.
    ///
    /// Why these tests: 1736 is the zone's only escort of two at once (triage M6), so a researcher who does not follow, a death
    /// that does not fail the mission (or fails it twice), an obelisk there before the team is, an ambush that does not come,
    /// or a run that does not end cleanly at the turn-in would each break it; 1358 hands out five items and takes each at its
    /// own captain only; 1286 hangs on a new boss's drop and his place.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenAshokaTests
    {
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1)
        private const uint WeaponFiller = 3869;            // inventory category 1 (quest-test-pattern)
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests (inventory category 4)
        private const uint ClientObeliskClass = 30138;     // NPCElohObeliskV01
        private const uint ClientObeliskPackage = 2066;    // objectiveconversation 1736,1
        private const uint ClientBothaPackage = 1410;      // objectiveconversation 1736,2 and 1358,1
        private const uint ClientWrenchTemplate = 97482;   // client item class 25481 "Mechanic Raji's Lucky Wrench"
        private const uint Botha = 770040;                 // Captain Amandla Botha (PR #224's row)
        private const uint ItCameFromErdas = 1218;

        private static readonly Vector3 Ashoka = AshenLeadInsTests.AshokaWaypoint;
        private static readonly Vector3 ShadowsEdge = AshenLeadInsTests.ShadowsEdgeWaypoint;

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// The offers: Botha offers Echoes in the Sand only after It Came From Erdas (the Crucible's, PR #224), never Harris or
        /// Raji; Chase offers Supply Logistics and Raji Precious Tools at once (no requirement), each only by its own NPC; after a
        /// reload the same.
        /// </summary>
        [TestMethod]
        public void TheOffersFollowTheirChains()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            var botha = Npc(desert, Botha);
            var harris = Npc(desert, AshenLeadInNpcsV1.Harris);
            var raji = Npc(desert, Tools.Raji);
            var chase = Npc(desert, N.Chase);
            bool Offers(Creature npc, uint mission) => PlateauRetreadCampTests.Offers(harness, npc, mission);
            Assert.IsFalse(Offers(botha, Echoes.MissionId), "not before It Came From Erdas");
            Assert.IsTrue(Offers(chase, Supply.MissionId), "Chase offers Supply Logistics");
            Assert.IsTrue(Offers(raji, Tools.MissionId), "Raji offers Precious Tools");
            foreach (var (npc, mission) in new[] { (botha, Supply.MissionId), (botha, Tools.MissionId), (harris, Supply.MissionId), (harris, Echoes.MissionId),
                         (raji, Echoes.MissionId), (raji, Supply.MissionId), (chase, Tools.MissionId), (chase, Echoes.MissionId) })
                Assert.IsFalse(Offers(npc, mission), $"{npc.DbId} does not offer {mission}");

            SeedErdas(harness);
            Assert.IsTrue(Offers(botha, Echoes.MissionId), "It Came From Erdas done: Botha offers Echoes in the Sand");
            Assert.IsFalse(Offers(harris, Echoes.MissionId) || Offers(raji, Echoes.MissionId) || Offers(chase, Echoes.MissionId), "only Botha's");
            AshenRuntime.Reload(harness);
            Assert.IsTrue(Offers(botha, Echoes.MissionId) && Offers(chase, Supply.MissionId) && Offers(raji, Tools.MissionId), "after a reload too");
        }

        /// <summary>
        /// Echoes in the Sand's rows (on the Crucible's revision with 1218: MissionContentValidator's one revision per required
        /// chain): Botha gives and takes it, level 42, the Ashen Desert category 10000016, the client's texts (missionconversation
        /// 16665; missionobjective 1736 1-5: 16970/16971, 16972/16973, 16974/16975, 16976/16977, 17013/17014, no counters; the server-only hidden 6 carries 5's texts), the
        /// talks at the client's packages (objectiveconversation 1736,1 2066 the obelisk, 1736,2 1410 Botha, variant 1), the one
        /// prerequisite It Came From Erdas; two markers, both on Ashen Desert (MissionMarkerProjection), unnamed; operational.
        /// </summary>
        [TestMethod]
        public void EchoesInTheSandsRowsAreTheClientsOnTheCruciblesRevision()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            const string revision = CrucibleMissionDataV1.Revision;
            Assert.AreEqual(revision, Echoes.Revision);
            Assert.IsFalse(world.MissionContentDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == Echoes.MissionId && entry.ContentRevision != revision), "one revision");
            var definition = world.MissionContentDefinitionEntries.AsNoTracking().Single(entry => entry.MissionId == Echoes.MissionId && entry.ContentRevision == revision);
            Assert.AreEqual(((uint?)Botha, (uint?)Botha, (uint?)42, (uint?)AshenMissionDataV1.CategoryId, 16665U, true, false),
                (definition.GiverId, definition.ReceiverId, definition.Level, definition.CategoryId, definition.ClientNameTextId, definition.Enabled, definition.Shareable));
            var objectives = world.MissionObjectiveDefinitionEntries.AsNoTracking().Where(entry => entry.MissionId == Echoes.MissionId && entry.ContentRevision == revision)
                .AsEnumerable().Select(entry => (entry.ObjectiveId, entry.ClientNameTextId, entry.ClientBodyTextId, entry.ClientCounter0TextId)).OrderBy(entry => entry.ObjectiveId).ToArray();
            CollectionAssert.AreEqual(new (uint, uint, uint, uint?)[] { (1, 16970, 16971, null), (2, 16972, 16973, null), (3, 16974, 16975, null), (4, 16976, 16977, null), (5, 17013, 17014, null), (6, 17013, 17014, null) }, objectives);
            var talks = world.Set<MissionTriggerEntry>().AsNoTracking()
                .Where(entry => entry.MissionId == Echoes.MissionId && entry.ContentRevision == revision && entry.Kind == MissionTriggerKind.Conversation)
                .AsEnumerable().Select(entry => (entry.ObjectiveId, entry.NpcPackageId, entry.PlayerFlagId)).OrderBy(entry => entry.ObjectiveId).ToArray();
            CollectionAssert.AreEqual(new (uint, uint?, uint?)[] { (1, ClientObeliskPackage, 1), (2, ClientBothaPackage, 1) }, talks);
            var prerequisites = world.MissionPrerequisiteEntries.AsNoTracking().Where(entry => entry.MissionId == Echoes.MissionId && entry.ContentRevision == revision).ToList();
            Assert.AreEqual((uint?)ItCameFromErdas, prerequisites.Single().RequiredMissionId);
            var markers = world.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.MissionId == Echoes.MissionId).ToList();
            Assert.HasCount(2, markers);
            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Mission>(), null);
            catalog.Load();
            foreach (var row in markers)
            {
                Assert.AreEqual(revision, row.ContentRevision);
                Assert.AreEqual(AshenMissionDataV1.MapContextId, MissionMarkerProjection.ResolveMap(catalog, row.MissionId, row.ObjectiveId,
                    new MissionIndicator { IndicatorId = row.IndicatorId }), $"1736.{row.ObjectiveId}: on Ashen Desert");
                Assert.IsTrue(row.IndicatorId > MissionIndicator.UnnamedFrom, $"1736.{row.ObjectiveId}: unnamed");
            }
            Assert.IsTrue(harness.Manager.LoadedMissions[Echoes.MissionId].IsOperational, harness.Manager.LoadedMissions[Echoes.MissionId].OperationalDiagnostic);
        }

        /// <summary>
        /// Echoes in the Sand played through: Botha offers the four laser weapons; accepted, she hears no objective; 1 is open
        /// with a marker on the obelisk (on Ashen Desert), 2 waits, the helpers 3-6 reach the client in no form. Both
        /// researchers come by Botha (the client's names, friendly, the player's own) and follow the player at once. No obelisk
        /// and no Bane before the landing. Walking the way to the landing (the navmesh's: the followers keep up), the landing
        /// brings the first ambush (two Thrax Soldiers, hostile, the player's own) and no obelisk; a reload there keeps the
        /// followers following; both Thrax dead bring the obelisk (the client's NPCElohObeliskV01, its talk the client's package
        /// 2066). The obelisk's talk
        /// completes 1 once, opens 2 with a marker at Botha and brings the second ambush. The four Bane dead, the way back with
        /// the team; Botha hears 2 (her package 1410, variant 1): 2 done, the team goes in, 3 and 4 done (silently), the mission
        /// completeable once. A full bag waits, a failed save pays nothing, Botha pays the weapon chosen once; done once.
        /// </summary>
        [TestMethod]
        public void EchoesInTheSandTakesBothResearchersThereAndBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedErdas(harness);
            var desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            var botha = Npc(desert, Botha);
            var offer = WildernessRanjaGorgeTests.Offer(harness, botha, Echoes.MissionId);
            CollectionAssert.AreEqual(new[] { Echoes.LaserPistol, Echoes.LaserRifle, Echoes.PhotonicShotgun, Echoes.LaserChaingun },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, botha, Echoes.MissionId);
            harness.Manager.Scenes.Tick(desert);
            harness.Drain();
            Assert.IsEmpty(Topics(harness, botha, Echoes.MissionId), "Botha hears nothing before the landing");
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Objective(harness, Echoes.MissionId, Echoes.ToTheLanding), Objective(harness, Echoes.MissionId, Echoes.BackToAshoka)), "1 open, 2 waits");
            var visible = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Echoes.MissionId].ObjectivesList;
            CollectionAssert.AreEquivalent(new[] { Echoes.ToTheLanding }, visible.Select(entry => (uint)entry.ObjectiveId).ToArray(), "2 waits unseen; 3-6 are hidden");
            var marker = visible.Single(entry => entry.ObjectiveId == Echoes.ToTheLanding).IndicatorList.Single();
            Assert.AreEqual(AshenMissionDataV1.MapContextId, marker.MapContextId, "on Ashen Desert");
            Assert.IsLessThan(0.1f, Vector2.Distance(new(marker.Position.X, marker.Position.Z), new(Echoes.ObeliskAt.X, Echoes.ObeliskAt.Z)), "1's marker on the obelisk");

            var team = Team(harness, desert);
            Assert.AreEqual((N.YorishName, N.JacksonName), (team[0].NameId, team[1].NameId), "Researcher Yorish and Researcher Jackson");
            Assert.IsTrue(team.All(member => member.TargetCategory != TargetCategory.Hostile), "friendly");
            Assert.IsNull(Obelisk(desert), "no obelisk before the landing");
            Assert.IsTrue(Ambushers(harness, desert).Count == 0, "no Bane before the landing");

            var log = new List<PythonPacket>();
            foreach (var corner in WayThere)
                E.WalkTo(harness, desert, corner, log, team);
            E.WalkTo(harness, desert, E.ToVector(Echoes.ObeliskAt), log, team, Echoes.ArrivalRadius);
            log.AddRange(E.Tick(harness, desert, 250));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Echoes.MissionId, Echoes.Ambushes), "5 at the landing");
            Assert.AreEqual(0, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Echoes.MissionId && packet.ObjectiveId == Echoes.Ambushes), "nothing of 5 reaches the client");
            var first = Ambushers(harness, desert);
            Assert.HasCount(2, first, "the first ambush: two");
            Assert.IsTrue(first.All(bane => bane.DbId == Echoes.Soldier && bane.TargetCategory == TargetCategory.Hostile), "Ashen Thrax Soldiers, hostile");
            Assert.IsNull(Obelisk(desert), "no obelisk while the first ambush lives");

            P.ReloadAndRecover(harness, desert);
            team = Team(harness, desert);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Echoes.MissionId, Echoes.ToTheLanding), "1 open after a reload");
            KillFirstAmbush(harness, desert);
            var obelisk = Obelisk(desert);
            Assert.IsNotNull(obelisk, "the obelisk once both are dead");
            Assert.AreEqual((ClientObeliskClass, E.ToVector(Echoes.ObeliskAt)), ((uint)obelisk.EntityClassId, obelisk.Position), "the client's obelisk at its place");
            Assert.AreEqual(ClientObeliskPackage, obelisk.MissionConversation.NpcPackageId, "its talk the client's package 2066");
            Assert.AreEqual(harness.Client.Player.Id, obelisk.SceneOwnerCharacterId, "the player's own");

            log.Clear();
            log.AddRange(TalkToObelisk(harness, desert, Obelisk(desert)));
            Assert.AreEqual(1, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Echoes.MissionId && packet.ObjectiveId == Echoes.ToTheLanding), "1: ObjectiveCompleted once");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Echoes.MissionId, Echoes.BackToAshoka), "2 open");
            var home = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Echoes.MissionId].ObjectivesList.Single(entry => entry.ObjectiveId == Echoes.BackToAshoka).IndicatorList.Single();
            Assert.IsLessThan(0.1f, Vector2.Distance(new(home.Position.X, home.Position.Z), new(Echoes.BothaAt.X, Echoes.BothaAt.Z)), "2's marker at Botha");
            E.Tick(harness, desert, 250);
            var second = Ambushers(harness, desert);
            Assert.HasCount(2, second, "the second ambush: two");
            foreach (var bane in second)
                E.Die(harness, desert, bane);

            team = Team(harness, desert);
            log.Clear();
            foreach (var corner in Enumerable.Reverse(WayThere))
                E.WalkTo(harness, desert, corner, log, team);
            E.WalkTo(harness, desert, E.ToVector(Echoes.BothaAt), log, team, 6);
            Assert.IsFalse(harness.Client.Player.Missions[Echoes.MissionId].Completeable, "not before Botha's talk");
            CollectionAssert.AreEqual(new[] { (Echoes.BackToAshoka, 1U) }, AshenFormalComplaintsTests.TopicVariants(harness, botha, Echoes.MissionId), "Botha hears 2, variant 1");
            log = AshenFormalComplaintsTests.Say(harness, botha, Echoes.MissionId, Echoes.BackToAshoka, 1).Cast<PythonPacket>().ToList();
            log.AddRange(E.Tick(harness, desert, 250));
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Completed, MissionObjectiveState.Completed),
                (Objective(harness, Echoes.MissionId, Echoes.BackToAshoka), Objective(harness, Echoes.MissionId, Echoes.Syndicate), Objective(harness, Echoes.MissionId, Echoes.Afs)), "2, then 3 and 4");
            Assert.AreEqual(0, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Echoes.MissionId && packet.ObjectiveId is Echoes.Syndicate or Echoes.Afs), "nothing of 3 and 4 reaches the client");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Echoes.MissionId && packet.IsCompleteable), "MissionCompleteable once");
            Assert.IsNull(E.SceneCreature(harness, desert, Echoes.MissionId, Echoes.YorishRole), "Yorish goes in");
            Assert.IsNull(E.SceneCreature(harness, desert, Echoes.MissionId, Echoes.JacksonRole), "Jackson goes in");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, Npc(desert, AshenLeadInNpcsV1.Harris).EntityId, Echoes.MissionId, 0), "not Harris' to take");
            FullBagWaits(harness, botha, Echoes.MissionId, 0, WeaponFiller);
            FailedTurnInPaysNothing(harness, botha, Echoes.MissionId, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, botha, Echoes.MissionId, 0, Echoes.Experience, Echoes.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Echoes.MissionId), "MissionRewarded once");
            Assert.IsGreaterThanOrEqualTo(1U, WildernessRanjaGorgeTests.Held(harness, Echoes.LaserPistol), "the pistol chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, botha, Echoes.MissionId), "done once");
        }

        /// <summary>
        /// The ambush gate (to-fix row 26: four Thrax at once are not won solo): at the landing the first pair comes and no obelisk,
        /// so no talk and no second ambush; one of them dead, still none, and a reload keeps the count (one of two); the second
        /// dead brings the obelisk, once; until its talk no second ambush. 6 never reaches the client.
        /// </summary>
        [TestMethod]
        public void TheObeliskWaitsUntilTheFirstAmbushIsDead()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedErdas(harness);
            var desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            Accept(harness, Npc(desert, Botha), Echoes.MissionId);
            var team = Team(harness, desert);
            var log = new List<PythonPacket>();
            foreach (var corner in WayThere)
                E.WalkTo(harness, desert, corner, log, team);
            E.WalkTo(harness, desert, E.ToVector(Echoes.ObeliskAt), log, team, Echoes.ArrivalRadius);
            log.AddRange(E.Tick(harness, desert, 250));
            Assert.HasCount(2, Ambushers(harness, desert), "the first pair");
            Assert.IsNull(Obelisk(desert), "no obelisk while both live");
            log.AddRange(E.Die(harness, desert, E.SceneCreature(harness, desert, Echoes.MissionId, Echoes.AmbushRole(1, 1))));
            log.AddRange(E.Tick(harness, desert, 250));
            Assert.IsNull(Obelisk(desert), "one alive: no obelisk");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Echoes.MissionId, Echoes.FirstPairDown), "6: one of two");
            P.ReloadAndRecover(harness, desert);
            Assert.IsNull(Obelisk(desert), "after a reload: still none");
            Assert.IsNull(E.SceneCreature(harness, desert, Echoes.MissionId, Echoes.AmbushRole(1, 1)), "the dead one stays dead");
            var last = E.SceneCreature(harness, desert, Echoes.MissionId, Echoes.AmbushRole(1, 2));
            Assert.IsNotNull(last, "the other is there");
            log.AddRange(E.Die(harness, desert, last));
            log.AddRange(E.Tick(harness, desert, 250));
            Assert.IsNotNull(Obelisk(desert), "both dead: the obelisk");
            Assert.AreEqual(1, desert.DynamicObjects.Count(obj => obj.SceneMissionId == Echoes.MissionId && obj.SceneActorRole == Echoes.ObeliskRole && obj.IsInWorld), "once");
            log.AddRange(E.Tick(harness, desert, 250));
            Assert.IsEmpty(Ambushers(harness, desert), "no second ambush before the talk");
            Assert.AreEqual(0, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Echoes.MissionId && packet.ObjectiveId == Echoes.FirstPairDown), "nothing of 6 reaches the client");
        }

        /// <summary>
        /// Either death fails it (the client's "If either researcher is killed, then you will fail your mission"): Yorish killed
        /// on the way out fails 3 and the mission once; Jackson killed on the way back (after the obelisk) fails 4 and the
        /// mission. Each time the other researcher, the obelisk and the Bane go. The
        /// failed mission cleared, Botha offers it again and a new team follows.
        /// </summary>
        [TestMethod]
        public void EitherResearchersDeathFailsTheMission()
        {
            foreach (var (dies, helper, afterObelisk) in new[] { (Echoes.YorishRole, Echoes.Syndicate, false), (Echoes.JacksonRole, Echoes.Afs, true) })
            {
                using var harness = WildernessRuntimeTestHarness.Create();
                SeedErdas(harness);
                var desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
                var botha = Npc(desert, Botha);
                Accept(harness, botha, Echoes.MissionId);
                var team = Team(harness, desert);
                if (afterObelisk)
                {
                    var log = new List<PythonPacket>();
                    foreach (var corner in WayThere)
                        E.WalkTo(harness, desert, corner, log, team);
                    E.WalkTo(harness, desert, E.ToVector(Echoes.ObeliskAt), log, team, Echoes.ArrivalRadius);
                    E.Tick(harness, desert, 250);
                    KillFirstAmbush(harness, desert);
                    TalkToObelisk(harness, desert, Obelisk(desert));
                    E.Tick(harness, desert, 250);
                    Assert.HasCount(2, Ambushers(harness, desert), "the second ambush there");
                }
                var victim = E.SceneCreature(harness, desert, Echoes.MissionId, dies);
                var packets = E.Die(harness, desert, victim);
                Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, Echoes.MissionId, helper), $"{dies}: its helper failed");
                Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[Echoes.MissionId].State, $"{dies}: the mission failed");
                Assert.AreEqual(1, packets.OfType<MissionFailedPacket>().Count(packet => packet.MissionId == Echoes.MissionId), $"{dies}: MissionFailed once");
                Assert.IsNull(E.SceneCreature(harness, desert, Echoes.MissionId, dies == Echoes.YorishRole ? Echoes.JacksonRole : Echoes.YorishRole), "the other goes");
                Assert.IsNull(Obelisk(desert), "the obelisk goes");
                Assert.IsEmpty(Ambushers(harness, desert), "the Bane go");

                Assert.IsTrue(harness.Manager.TryClear(harness.Client, Echoes.MissionId), "the failed mission cleared");
                E.Tick(harness, desert, 250);
                Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, botha, Echoes.MissionId), "offered again");
                Accept(harness, botha, Echoes.MissionId);
                Assert.HasCount(2, Team(harness, desert), "a new team follows");
                Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete),
                    (Objective(harness, Echoes.MissionId, Echoes.ToTheLanding), Objective(harness, Echoes.MissionId, Echoes.Syndicate), Objective(harness, Echoes.MissionId, Echoes.Afs)), "from the start");
            }
        }

        /// <summary>An abandon takes the team away; taken again, a new team by Botha, 1 open from the start.</summary>
        [TestMethod]
        public void EchoesInTheSandAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedErdas(harness);
            var desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            var botha = Npc(desert, Botha);
            Accept(harness, botha, Echoes.MissionId);
            Team(harness, desert);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Echoes.MissionId));
            E.Tick(harness, desert, 250);
            Assert.IsNull(E.SceneCreature(harness, desert, Echoes.MissionId, Echoes.YorishRole), "Yorish goes");
            Assert.IsNull(E.SceneCreature(harness, desert, Echoes.MissionId, Echoes.JacksonRole), "Jackson goes");
            Accept(harness, botha, Echoes.MissionId);
            var team = Team(harness, desert);
            Assert.IsTrue(team.All(member => Vector3.Distance(member.Position, E.ToVector(Echoes.BothaAt)) < 15), "a new team by Botha");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Echoes.MissionId, Echoes.ToTheLanding));
        }

        /// <summary>
        /// Supply Logistics: Chase offers the four armour pieces; a bag without room for the five supplies refuses the accept;
        /// accepted, the five supplies (the client's templates); five markers, each at its captain, on Ashen Desert; Chase hears
        /// no objective. In the order Yamamoto, Botha, Linier, Harris, Emmons each captain hears only his delivery (the client's
        /// package, variant 1), takes only his supplies and completes only his objective, once; a reload after two keeps them;
        /// the fifth makes the mission completeable once. A full bag waits, a failed save pays nothing; Chase pays the piece
        /// chosen, once; done once. An abandon (a second run) takes the supplies.
        /// </summary>
        [TestMethod]
        public void SupplyLogisticsReachesTheFiveCaptains()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, ShadowsEdge);
            var chase = Npc(desert, N.Chase);
            var offer = WildernessRanjaGorgeTests.Offer(harness, chase, Supply.MissionId);
            CollectionAssert.AreEqual(new[] { Supply.MechVest, Supply.GravitonLegs, Supply.HazmatBoots, Supply.ReflectiveBoots },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());

            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.FillInbox();
            harness.MoveTo(chase.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, chase.EntityId, Supply.MissionId), "no room for the supplies");
            for (var slot = 0; slot < 5; slot++)
                WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            harness.Drain();
            Accept(harness, chase, Supply.MissionId);
            foreach (var delivery in Supply.Deliveries)
                Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, delivery.Template), $"the supplies for {delivery.Key}");
            Assert.IsEmpty(Topics(harness, chase, Supply.MissionId), "Chase hears no objective");
            var snapshot = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Supply.MissionId].ObjectivesList;
            Assert.HasCount(5, snapshot, "five deliveries open");
            foreach (var objective in snapshot)
            {
                var at = Supply.CaptainsAt.Single(captain => captain.Captain == Supply.Deliveries.Single(d => d.Objective == objective.ObjectiveId).Captain).At;
                var mark = objective.IndicatorList.Single();
                Assert.IsLessThan(0.1f, Vector2.Distance(new(mark.Position.X, mark.Position.Z), new((float)at.X, (float)at.Z)), $"{objective.ObjectiveId}: at its captain");
                Assert.AreEqual(AshenMissionDataV1.MapContextId, mark.MapContextId, "on Ashen Desert");
            }

            var captains = Supply.Deliveries.ToDictionary(delivery => delivery.Objective, delivery => Npc(desert, delivery.Captain));
            var log = new List<object>();
            var done = new List<uint>();
            foreach (var objective in new uint[] { 4, 1, 5, 2, 3 })
            {
                var delivery = Supply.Deliveries.Single(d => d.Objective == objective);
                var captain = captains[objective];
                foreach (var other in captains.Where(entry => entry.Key != objective))
                    Assert.IsFalse(Topics(harness, other.Value, Supply.MissionId).Contains(objective), $"{other.Value.DbId} does not hear {objective}");
                CollectionAssert.AreEqual(new[] { (objective, Supply.Variant) }, AshenFormalComplaintsTests.TopicVariants(harness, captain, Supply.MissionId), $"{captain.DbId} hears {objective}, variant 1");
                var packets = AshenFormalComplaintsTests.Say(harness, captain, Supply.MissionId, objective, Supply.Variant);
                log.AddRange(packets);
                done.Add(objective);
                Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Supply.MissionId && packet.ObjectiveId == objective), $"{objective} once");
                foreach (var d in Supply.Deliveries)
                    Assert.AreEqual(done.Contains(d.Objective) ? 0U : 1U, WildernessRanjaGorgeTests.Held(harness, d.Template), $"after {objective}: {d.Key}");
                if (done.Count == 2)
                {
                    AshenRuntime.Reload(harness);
                    harness.Drain();
                    Assert.AreEqual(2, Supply.Deliveries.Count(d => Objective(harness, Supply.MissionId, d.Objective) == MissionObjectiveState.Completed), "kept at a reload");
                }
                if (done.Count < 5)
                    Assert.IsFalse(harness.Client.Player.Missions[Supply.MissionId].Completeable, $"{done.Count} of 5");
            }
            Assert.IsTrue(harness.Client.Player.Missions[Supply.MissionId].Completeable, "all five");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Supply.MissionId && packet.IsCompleteable), "MissionCompleteable once");
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, captains[1].EntityId, Supply.MissionId, 0), "not Botha's to take");
            FullBagWaits(harness, chase, Supply.MissionId, 0, ArmorFiller);
            FailedTurnInPaysNothing(harness, chase, Supply.MissionId, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, chase, Supply.MissionId, 0, Supply.Experience, Supply.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Supply.MissionId), "MissionRewarded once");
            Assert.IsGreaterThanOrEqualTo(1U, WildernessRanjaGorgeTests.Held(harness, Supply.MechVest), "the vest chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, chase, Supply.MissionId), "done once");

            using var second = WildernessRuntimeTestHarness.Create();
            var desert2 = AshenLeadInsTests.EnterAshen(second, ShadowsEdge);
            Accept(second, Npc(desert2, N.Chase), Supply.MissionId);
            Assert.IsTrue(second.Manager.TryAbandon(second.Client, Supply.MissionId));
            second.Drain();
            Assert.IsTrue(Supply.Deliveries.All(d => WildernessRanjaGorgeTests.Held(second, d.Template) == 0), "an abandon takes the supplies");
        }

        /// <summary>
        /// Supply Logistics' fifth delivery and the White Oasis control point (ControlPointServices, the existing rule; triage
        /// "Control points"): with point 17 held by the Bane, Daris Linier is gone; 5 stays open, the mission neither fails nor
        /// completes, the supplies kept. The AFS take it back: Linier stands again and takes his delivery.
        /// </summary>
        [TestMethod]
        public void SupplyLogisticsWaitsForWhiteOasisWhileTheBaneHoldIt()
        {
            const uint whiteOasisPoint = 17;
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, ShadowsEdge);
            Accept(harness, Npc(desert, N.Chase), Supply.MissionId);
            var linier = Supply.Deliveries.Single(d => d.Objective == 5);
            var points = ControlPoints.Instance;
            var config = points.Config;
            try
            {
                points.Config = new ControlPointConfig();
                points.Load(harness.World.ControlPointEntries.AsNoTracking().Where(row => row.Id == whiteOasisPoint).ToList(),
                    harness.World.ControlPointLinkEntries.AsNoTracking().Where(row => row.ControlPointId == whiteOasisPoint).ToList(), null);
                points.Place(desert);
                var white = points.ById(whiteOasisPoint);
                Assert.IsNotNull(white, "control point 17 loaded");
                points.SetOwner(white, ControlPoints.Bane, null);
                Respawn(desert, linier.Captain);
                Assert.IsFalse(Alive(desert).Any(creature => creature.SpawnPool?.DbId == linier.Captain), "Bane: Linier gone");
                harness.Drain();
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Supply.MissionId, 5), "5 waits");
                Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[Supply.MissionId].State, "no fail");
                Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, linier.Template), "his supplies kept");

                points.SetOwner(white, ControlPoints.Afs, null);
                Respawn(desert, linier.Captain);
                var daris = RequireNpc(desert, linier.Captain);
                AshenFormalComplaintsTests.Say(harness, daris, Supply.MissionId, 5, Supply.Variant);
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Supply.MissionId, 5), "AFS again: Linier takes them");
                Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, linier.Template));
            }
            finally
            {
                points.Load(new List<ControlPointEntry>(), new List<ControlPointLinkEntry>(), null);
                points.Config = config;
            }
        }

        /// <summary>
        /// Precious Tools: Raji offers the four armour pieces; no wrench off Jirex before the mission. Accepted, Raji hears no
        /// objective; a marker on Jirex (on Ashen Desert). Jirex drops the wrench only, at 100% (99), once; a death is not a pickup;
        /// a full bag leaves it on the corpse, one slot free and it is picked up; 1 done once, completeable once; a second kill
        /// drops none; a reload keeps it. An abandon keeps the wrench, taken again it counts at once. A full bag waits, a failed
        /// save pays nothing; Raji pays the piece chosen, once, and takes the wrench; done once.
        /// </summary>
        [TestMethod]
        public void PreciousToolsComesOffJirex()
        {
            var percentile = 99;
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            var desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            var raji = Npc(desert, Tools.Raji);
            var pools = new[] { N.JirexPool };
            Spawn(desert, pools);
            var offer = WildernessRanjaGorgeTests.Offer(harness, raji, Tools.MissionId);
            CollectionAssert.AreEqual(new[] { Tools.BioGloves, Tools.GravitonHelmet, Tools.MechGloves, Tools.StealthGloves },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(Holds(AshenScavengerTests.Kill(harness, desert, N.Jirex, pools), ClientWrenchTemplate), "no wrench before the mission");

            Accept(harness, raji, Tools.MissionId);
            Assert.IsEmpty(Topics(harness, raji, Tools.MissionId), "Raji hears no objective");
            var mark = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Tools.MissionId].ObjectivesList.Single().IndicatorList.Single();
            Assert.IsLessThan(0.1f, Vector2.Distance(new(mark.Position.X, mark.Position.Z), new((float)N.JirexAt.X, (float)N.JirexAt.Z)), "the marker on Jirex");
            Assert.AreEqual(AshenMissionDataV1.MapContextId, mark.MapContextId, "on Ashen Desert");

            var loot = AshenScavengerTests.Kill(harness, desert, N.Jirex, pools);
            Assert.IsTrue(Holds(loot, ClientWrenchTemplate), "99: the wrench drops (100%)");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, ClientWrenchTemplate), "a death is not a pickup");
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            PickUp(harness, loot);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, ClientWrenchTemplate), "no room");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, loot);
            var packets = harness.Drain();
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, ClientWrenchTemplate), "picked up once there is room");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Tools.MissionId, Tools.Recover), "1 done");
            Assert.IsTrue(harness.Client.Player.Missions[Tools.MissionId].Completeable, "completeable");
            Assert.IsFalse(Holds(AshenScavengerTests.Kill(harness, desert, N.Jirex, pools), ClientWrenchTemplate), "one: a second kill drops none");
            AshenRuntime.Reload(harness);
            harness.Drain();
            Assert.AreEqual((1U, MissionObjectiveState.Completed), (WildernessRanjaGorgeTests.Held(harness, ClientWrenchTemplate), Objective(harness, Tools.MissionId, Tools.Recover)), "kept at a reload");

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Tools.MissionId));
            harness.Drain();
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, ClientWrenchTemplate), "kept at an abandon");
            Accept(harness, raji, Tools.MissionId);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Tools.MissionId, Tools.Recover), "the wrench held counts again");

            FullBagWaits(harness, raji, Tools.MissionId, 0, ArmorFiller);
            FailedTurnInPaysNothing(harness, raji, Tools.MissionId, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, raji, Tools.MissionId, 0, Tools.Experience, Tools.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Tools.MissionId), "MissionRewarded once");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, ClientWrenchTemplate), "Raji takes the wrench");
            Assert.IsGreaterThanOrEqualTo(1U, WildernessRanjaGorgeTests.Held(harness, Tools.BioGloves), "the gloves chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, raji, Tools.MissionId), "done once");
        }

        /// <summary>
        /// The set's creatures (to-fix rows 20 and 26): in the id block 790520-790559 Chase and the two researchers friendly, Jirex
        /// hostile with the client's name 9793 (the boss list's "Jerix": BossTitles counts him on Ashen Desert), the Overseers'
        /// class 10504, level 44, alone in his pool (one, radius 0, respawn 3000 tenths), the only pools Chase's and his. Neither
        /// his class nor the ambushers' (531008's) nor the researchers' carries a client IMMUNE_* damage flag (creature_class_flag
        /// 125-135). Jirex stands 60 m or more (the hostile-pool rule, stricter than fix F's 35 m boss rule) outside every other hostile pool's circle and 100 m
        /// or more from every pool of a creature with the warcry (action 477).
        /// </summary>
        [TestMethod]
        public void TheSetsCreaturesCanBeBeatenAndJirexStandsAlone()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var rows = world.CreatureEntries.AsNoTracking().Where(row => row.Id >= 790520 && row.Id <= 790559).ToList();
            CollectionAssert.AreEquivalent(new[] { N.Chase, N.Yorish, N.Jackson, N.Jirex }, rows.Select(row => row.Id).ToArray(), "the four");
            Assert.IsTrue(rows.Where(row => row.Id != N.Jirex).All(row => row.Faction == 1), "Chase and the researchers friendly");
            var jirex = rows.Single(row => row.Id == N.Jirex);
            Assert.AreEqual((0U, 10504U, 44U, 9793U), ((uint)jirex.Faction, jirex.ClassId, jirex.Level, jirex.NameId), "Jirex: hostile, the Overseers' class, 44, the client's name");
            Assert.IsNotNull(BossTitles.ZoneOf(AshenMissionDataV1.MapContextId, N.JirexName), "the boss list counts him (1580's Jerix)");
            var immune = new uint[] { 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135 };
            var soldierClass = world.CreatureEntries.AsNoTracking().Single(row => row.Id == Echoes.Soldier).ClassId;
            foreach (var cls in rows.Select(row => row.ClassId).Append(soldierClass).Distinct())
                CollectionAssert.AreEqual(Array.Empty<uint>(), world.CreatureClassFlagEntries.AsNoTracking().Where(flag => flag.ClassId == cls).Select(flag => flag.FlagId).AsEnumerable()
                    .Where(immune.Contains).ToArray(), $"class {cls}: no damage immunity");
            var pools = world.SpawnPoolEntries.AsNoTracking().Where(pool => pool.Id >= 790520 && pool.Id <= 790559).ToList();
            CollectionAssert.AreEquivalent(new[] { N.Chase, N.JirexPool }, pools.Select(pool => pool.Id).ToArray(), "Chase's and Jirex's pools");
            var own = pools.Single(pool => pool.Id == N.JirexPool);
            Assert.AreEqual((N.Jirex, (byte)1, (byte)1, 3000U, 0.0), (own.Creature1Id, own.Creature1MinCount, own.Creature1MaxCount, own.RespawnTime, own.Radius), "one alone, 300 s");
            var hostile = WO.HostilePools(harness).Where(pool => pool.Id != N.JirexPool).ToList();
            var nearest = hostile.Min(pool => Vector2.Distance(new((float)own.PosX, (float)own.PosZ), new((float)pool.PosX, (float)pool.PosZ)) - (float)pool.Radius);
            Assert.IsGreaterThanOrEqualTo(60f, nearest, "Jirex 60 m+ outside every other hostile pool's circle (60 m between hostile pools)");
            // Every Ashen Desert pool of a creature with the harvester warcry (action 477, AshenFixFTests.WarcryCreatures), not only set 2's Workers.
            var criers = AshenFixFTests.WarcryCreatures(harness);
            var crying = world.SpawnPoolEntries.AsNoTracking().Where(pool => pool.MapContextId == AshenMissionDataV1.MapContextId).AsEnumerable()
                .Where(pool => new[] { pool.Creature1Id, pool.Creature2Id, pool.Creature3Id, pool.Creature4Id, pool.Creature5Id, pool.Creature6Id }.Any(criers.Contains)).ToList();
            Assert.IsGreaterThanOrEqualTo(2, crying.Count, "the warcry pools are read");
            foreach (var pool in crying)
                Assert.IsGreaterThan(100f, Vector2.Distance(new((float)own.PosX, (float)own.PosZ), new((float)pool.PosX, (float)pool.PosZ)), $"warcry pool {pool.Id}");
        }

        /// <summary>
        /// The set's places on the client's floor: Chase, Jirex, the researchers' start, the obelisk and the four ambush spots
        /// each on the navmesh's ground and a client floor (within 1 m), with one navmesh floor round it, nothing of the client's
        /// surfaces over it within 40 m (open sky); Chase, Jirex, the researchers and the obelisk seen from all 16 places 2 m round
        /// it (line of sight: to-fix row 26); every one 34 m or more outside every hostile pool's circle (Jirex's own left out).
        /// </summary>
        [TestMethod]
        public void ThePlacesStandOnTheClientsFloorUnderOpenSky()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var nav = desert.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", harness.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
            var hostile = WO.HostilePools(harness).Where(pool => pool.Id != N.JirexPool).ToList();
            Assert.IsGreaterThan(50, hostile.Count, "the Ashen hostile pools are read");
            var places = Spots();
            var near = F.Near(geometry, places.Select(place => place.At).ToList(), 45);
            var failures = new List<string>();
            for (var i = 0; i < places.Count; i++)
            {
                var (label, at, ring, middle) = places[i];
                if (nav.GroundHeight(at) is not { } ground || MathF.Abs(ground - at.Y) > 1f)
                    failures.Add(FormattableString.Invariant($"{label}: the navmesh ground {nav.GroundHeight(at)} at {at}"));
                if (F.FloorAt(near[i], at, at.Y - 2, at.Y + 1.5f) is not { } floor || MathF.Abs(floor - at.Y) > 1)
                    failures.Add(FormattableString.Invariant($"{label}: the client's floor at {at}"));
                if (near[i].Any(t => F.Hit(at + new Vector3(0, 1, 0), Vector3.UnitY, t.A, t.B, t.C) is { } up && up < 40))
                    failures.Add($"{label}: a client surface over it");
                for (var h = 3; h <= 12; h++)
                    foreach (var y in new[] { at.Y - h, at.Y + h })
                        if (nav.Nearest(at with { Y = y }) is { } other && Vector2.Distance(new(other.X, other.Z), new(at.X, at.Z)) < 0.3f && MathF.Abs(other.Y - at.Y) > 2.5f)
                            failures.Add(FormattableString.Invariant($"{label}: a second navmesh floor at {other.Y:0.00}"));
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
                var gap = hostile.Min(pool => Vector2.Distance(new(at.X, at.Z), new((float)pool.PosX, (float)pool.PosZ)) - (float)pool.Radius);
                if (gap < PlaceClearance)
                    failures.Add(FormattableString.Invariant($"{label}: {gap:0.0} m outside a hostile pool's circle"));
            }
            Assert.HasCount(1 + 1 + 2 + 1 + 4, places);
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>
        /// The walks on the client's floor (AshenFixBTests.ClientFloorLine with a player's half width, fix G's 55-degree climb
        /// rule), there and back by the corners Walks names: Chase from the Shadow's Edge waypoint; the escort from Botha and from
        /// the Ashoka waypoint to the obelisk (the navmesh's way, by (258, -299) and (220, -240)); Jirex from the Ashoka waypoint
        /// (set 13's road north, fix G's corners, then east); each ambush spot to the obelisk (their run in). Every walk keeps
        /// HostileClearance (30 m, 2D) outside the circle of every hostile Ashen pool but its own target's. The researchers'
        /// own way (the navmesh, as they follow) is complete from Botha to the obelisk and back.
        /// </summary>
        [TestMethod]
        public void ThePlacesAreWalkedToOnTheClientsFloorAwayFromEveryCamp()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", harness.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
            var hostile = WO.HostilePools(harness);
            Assert.IsGreaterThan(50, hostile.Count, "the Ashen hostile pools are read");
            var failures = new List<string>();
            var walked = 0;
            foreach (var (label, from, target, corners, own) in Walks())
            {
                var there = corners.Prepend(from).Append(target).ToList();
                var back = Enumerable.Reverse(there).ToList();
                foreach (var (line, direction) in new[] { (there, "there"), (back, "back") })
                {
                    walked++;
                    if (AshenFixBTests.ClientFloorLine(geometry, line, line[^1], null, AshenFormalComplaintsTests.PlayerHalfWidth) is { } why)
                        failures.Add($"{label} {direction}: {why}");
                    if (WO.Clearance(line, hostile.Where(pool => pool.Id != own).ToList()) is { } close)
                        failures.Add(FormattableString.Invariant($"{label} {direction}: {close.Gap:0.0} m outside hostile pool {close.Pool}'s circle at ({close.At.X:0.0}, {close.At.Y:0.0})"));
                }
            }
            foreach (var (a, b) in new[] { (E.ToVector(Echoes.YorishAt), E.ToVector(Echoes.ObeliskAt)), (E.ToVector(Echoes.ObeliskAt), E.ToVector(Echoes.JacksonAt)) })
            {
                var path = desert.NavMesh.FindPath(desert.NavMesh.Nearest(a) ?? a, b, out var complete);
                if (path == null || !complete)
                    failures.Add($"the researchers' navmesh way {a} to {b} is not complete");
            }
            Assert.AreEqual(2 * Walks().Count, walked, "every walk both ways");
            Assert.HasCount(1 + 2 + 1 + 4, Walks());
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>How far outside every hostile pool's circle the set's places stand (2D; 30 m walk clearance and a margin).</summary>
        private const float PlaceClearance = 34;

        /// <summary>The way to the landing (the navmesh's corners, the step by (210, -231) left out for (220, -240): ClientFloorLine), before the obelisk.</summary>
        internal static readonly Vector3[] WayThere =
        {
            new(360.0f, 284.65f, -426.0f), new(358.4f, 285.25f, -424.0f), new(327.6f, 297.25f, -390.8f), new(298.4f, 317.45f, -353.2f),
            new(258.0f, 319.45f, -299.2f), new(220.0f, 309.02f, -240.0f)
        };

        /// <summary>Off the waypoint pad to the west, as set 13's Maiek walk (fix G).</summary>
        private static readonly Vector3[] OffThePad = { new(389.60f, 280.45f, -457.60f), new(387.60f, 280.65f, -456.00f) };

        /// <summary>The set's places: label, place, whether it is seen from the ring, the height looked at.</summary>
        internal static List<(string Label, Vector3 At, bool Ring, float Middle)> Spots()
        {
            var spots = new List<(string, Vector3, bool, float)>
            {
                ("Lieutenant Daniel Chase", V(N.ChaseAt), true, F.NpcMiddle), ("Overseer Jirex", V(N.JirexAt), true, F.NpcMiddle),
                ("Researcher Yorish's start", E.ToVector(Echoes.YorishAt), true, F.NpcMiddle), ("Researcher Jackson's start", E.ToVector(Echoes.JacksonAt), true, F.NpcMiddle),
                ("the Daedalus obelisk", E.ToVector(Echoes.ObeliskAt), true, F.ObjectMiddle)
            };
            foreach (var at in Echoes.FirstAmbushAt.Concat(Echoes.SecondAmbushAt))
                spots.Add(($"an ambush spot {at.X:0}, {at.Z:0}", E.ToVector(at), false, 0));
            return spots;
        }

        /// <summary>The walks: label, start, place, the corners between (ESTIMATE of a player's line), the target's own pool (0: none).</summary>
        internal static List<(string Label, Vector3 From, Vector3 Target, Vector3[] Corners, uint Own)> Walks()
        {
            var obelisk = E.ToVector(Echoes.ObeliskAt);
            var walks = new List<(string, Vector3, Vector3, Vector3[], uint)>
            {
                ("Chase from the Shadow's Edge Post waypoint", ShadowsEdge, V(N.ChaseAt), Array.Empty<Vector3>(), 0),
                ("the escort from Botha to the obelisk", E.ToVector(Echoes.BothaAt), obelisk, WayThere, 0),
                ("the escort from the Ashoka waypoint to the obelisk", Ashoka, obelisk, OffThePad.Concat(WayThere.Skip(1)).ToArray(), 0),
                ("Jirex from the Ashoka waypoint", Ashoka, V(N.JirexAt), OffThePad.Concat(new Vector3[]
                {
                    new(387.60f, 281.25f, -454.80f), new(388.40f, 283.45f, -430.80f), new(402f, 290.95f, -401f), new(405f, 290.95f, -352f), new(395f, 298.15f, -290f)
                }).ToArray(), N.JirexPool)
            };
            foreach (var at in Echoes.FirstAmbushAt.Concat(Echoes.SecondAmbushAt))
                walks.Add(($"the ambush from {at.X:0}, {at.Z:0} to the obelisk", E.ToVector(at), obelisk, Array.Empty<Vector3>(), 0));
            return walks;
        }

        private static Vector3 V((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);

        /// <summary>It Came From Erdas (the Crucible's, PR #224) done in the character's history, then a reload.</summary>
        private static void SeedErdas(WildernessRuntimeTestHarness harness)
        {
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() => unit.CharacterMissions.Runtime.Add(new CharacterMissionHistoryEntry
                {
                    CharacterId = harness.Client.Player.Id,
                    MissionId = ItCameFromErdas,
                    AssignmentId = Guid.NewGuid().ToString("N"),
                    AssignmentGeneration = 1,
                    ContentRevision = CrucibleMissionDataV1.Revision,
                    CompletedAtUtc = harness.UtcNow,
                    RewardedAtUtc = harness.UtcNow,
                    Outcome = (uint)MissionState.Completed,
                    Rewarded = true
                }));
            AshenRuntime.Reload(harness);
        }

        /// <summary>The two researchers: there, the player's own, following the player (Yorish, Jackson).</summary>
        private static Creature[] Team(WildernessRuntimeTestHarness harness, MapChannel desert)
        {
            harness.Manager.Scenes.Tick(desert);
            harness.Drain();
            return new[] { E.Follower(harness, desert, Echoes.MissionId, Echoes.YorishRole), E.Follower(harness, desert, Echoes.MissionId, Echoes.JacksonRole) };
        }

        /// <summary>The first ambush's two killed, one after the other: the obelisk only after the second.</summary>
        private static void KillFirstAmbush(WildernessRuntimeTestHarness harness, MapChannel desert)
        {
            for (var member = 1; member <= 2; member++)
            {
                Assert.IsNull(Obelisk(desert), $"no obelisk with {3 - member} of the first ambush alive");
                var bane = E.SceneCreature(harness, desert, Echoes.MissionId, Echoes.AmbushRole(1, member));
                Assert.IsNotNull(bane, $"the first ambush's {member}");
                E.Die(harness, desert, bane);
                E.Tick(harness, desert, 250);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Echoes.MissionId, Echoes.FirstPairDown), "6: both dead");
        }

        private static DynamicObject Obelisk(MapChannel desert) =>
            desert.DynamicObjects.SingleOrDefault(obj => obj.SceneMissionId == Echoes.MissionId && obj.SceneActorRole == Echoes.ObeliskRole && obj.IsInWorld);

        /// <summary>The living Bane of this run's ambushes.</summary>
        private static List<Creature> Ambushers(WildernessRuntimeTestHarness harness, MapChannel desert) =>
            new[] { 1, 2 }.SelectMany(ambush => new[] { 1, 2 }.Select(member => E.SceneCreature(harness, desert, Echoes.MissionId, Echoes.AmbushRole(ambush, member))))
                .Where(creature => creature != null).ToList();

        /// <summary>The obelisk's talk (its conversation, then the objective's completion, as Timora's Eloh hologram); the packets.</summary>
        private static List<PythonPacket> TalkToObelisk(WildernessRuntimeTestHarness harness, MapChannel desert, DynamicObject obelisk)
        {
            Assert.IsNotNull(obelisk, "the obelisk");
            harness.MoveTo(obelisk.Position + new Vector3(2, 0, 0));
            harness.Drain();
            var npcs = new NpcManager(harness, harness.Manager);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = obelisk.EntityId, ActionId = ActionId.Converse, ActionArgId = 1 });
            npcs.CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = obelisk.EntityId, MissionId = Echoes.MissionId, ObjectiveId = Echoes.ToTheLanding, PlayerFlagId = Echoes.Variant
            });
            var packets = harness.Drain().ToList();
            packets.AddRange(E.Tick(harness, desert, 250));
            return packets;
        }
    }
}
