using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Missions.Scenes;
using Rasa.Packets.MapChannel.Client;
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
    using static PlateauInstanceMaps;
    using N = MarshesLogosResearchNpcsV1;

    /// <summary>
    /// Marshes set 13, the Logos Research Facility (squad instance 1700) and the Eloh chamber, played through as a player plays
    /// them: Sergeant Fitz's A Taste of Their Own Medicine (546: Hax met, the Security Checkpoint cleared, Hax kept alive in his
    /// hack), Hax's Freedom's Just Another Word (548: the Commander's keypass, three cell blocks) and Slay on Target (718: three
    /// bosses, the lab equipment, the DNA, the teleporter's influx stopped), Captain Sakai's Leggo My Logos (550: two tanks
    /// blown, the field down, the element taken, the escape in time or the self destruct) and Retread Vincent's To a
    /// Subterranean Chamber (1674: the lake's teleporter to Jumna).
    ///
    /// Why these tests: the givers and the scenes are in a squad copy that outlives a test (the run's own actors are looked
    /// for); Hax and Sakai are both public NPCs and their missions' scene actors; 546 and 550 run on timers, 550 on a deadline
    /// with two endings that each open the debriefing, 718 on a first hit and a repeating timer that the teleporter stops; the
    /// Eloh chamber is an island on the navmesh, reached only by the lake's teleporter. A wave that never ends, a hack that
    /// completes after Hax died, a debriefing that does not open on the late ending, a pad that does not take the player, or
    /// progress lost at a reload would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesLogosResearchTests
    {
        private const uint Facility = N.MapContextId;
        private const uint Taste = MarshesTasteOfTheirOwnMedicineV1.MissionId;
        private const uint Freedom = MarshesFreedomsJustAnotherWordV2.MissionId;
        private const uint Leggo = MarshesLeggoMyLogosV1.MissionId;
        private const uint Slay = MarshesSlayOnTargetV2.MissionId;
        private const uint Chamber = MarshesSubterraneanChamberV1.MissionId;

        private static readonly Vector3 RetreadCityWaypoint = new(-250.45f, 216.8f, -830.56f);

        /// <summary>
        /// Hax and Sakai stand by the facility's hospital with the client's names and their levels, walked to from the door;
        /// Hax offers 546's follow-ups only after it; Jumna stands in the Eloh chamber. Fitz offers 546 on the Marshes.
        /// </summary>
        [TestMethod]
        public void TheNpcsStandAtTheirPlaces()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var fitz = Fitz(harness, marshes);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, fitz, Taste), "Fitz offers 546");
            var jumna = Npc(marshes, N.Jumna);
            Assert.AreEqual((N.JumnaName, N.JumnaLevel, N.JumnaPackage), (jumna.NameId, jumna.Level, jumna.Npc?.NpcPackageId));
            AssertOnGround(marshes, jumna.Position, "Jumna");
            Assert.AreNotEqual(TargetCategory.Hostile, jumna.TargetCategory);

            var copy = IntoFacility(harness);
            Assert.IsTrue(copy.IsSquadInstance);
            var door = harness.Client.Player.Position;
            foreach (var (pool, name, level, package) in new[]
            {
                (N.Hax, N.HaxName, N.HaxLevel, N.HaxPackage), (N.Sakai, N.SakaiName, N.SakaiLevel, N.SakaiPackage)
            })
            {
                var npc = Giver(copy, pool);
                Assert.AreEqual((name, level, package), (npc.NameId, npc.Level, npc.Npc?.NpcPackageId), $"{pool}");
                AssertOnGround(copy, npc.Position, $"{pool}");
                AssertPath(copy, door, npc.Position, $"{pool} from the door");
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory);
            }
            var hax = Giver(copy, N.Hax);
            Assert.IsGreaterThan(4f, Vector3.Distance(hax.Position, Giver(copy, N.Sakai).Position), "Sakai clear of Hax (to-fix 32)");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, hax, Freedom), "548 needs 546");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, hax, Slay), "718 needs 546");
            SeedCompletedMarshes(harness, Taste);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, hax, Freedom), "548 after 546");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, hax, Slay), "718 after 546");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Giver(copy, N.Sakai), Leggo), "550 needs 548");
        }

        /// <summary>
        /// 546: accepted at Fitz on the Marshes; Hax heard in the facility opens 1 and brings the six Thrax; their deaths complete
        /// 1 (once each, a reload between) and start the hack: Hax and the medic at the room, three waves, 120 s; the hack done,
        /// the waves and the medic go, 2 completes, and Hax (the scene's) pays.
        /// </summary>
        [TestMethod]
        public void TasteOfTheirOwnMedicineSecuresTheRoomAndKeepsHaxAlive()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            Accept(harness, Fitz(harness, marshes), Taste);
            var copy = IntoFacility(harness);
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Taste, MarshesTasteOfTheirOwnMedicineV1.Secure));
            Assert.IsFalse(Room(harness, copy).Any(), "no Thrax before Hax is met");

            Talk(harness, Giver(copy, N.Hax), Taste, MarshesTasteOfTheirOwnMedicineV1.Rendezvous);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Taste, MarshesTasteOfTheirOwnMedicineV1.Secure));
            var room = Room(harness, copy).ToArray();
            Assert.HasCount(MarshesTasteOfTheirOwnMedicineV1.Room.Length, room, "the six Thrax");
            foreach (var thrax in room)
            {
                Assert.AreEqual(TargetCategory.Hostile, thrax.TargetCategory);
                AssertOnGround(copy, thrax.Position, thrax.SpawnPool.SceneActorRole);
                Assert.IsLessThan((float)MarshesTasteOfTheirOwnMedicineV1.RoomRadius,
                    Vector3.Distance(thrax.Position, ToVector(MarshesTasteOfTheirOwnMedicineV1.RoomAt)), "in the Security Checkpoint");
            }
            for (var index = 0; index < room.Length; index++)
            {
                Kill(harness, copy, room[index]);
                if (index == 2)
                {
                    ReloadAndRecover(harness, copy);
                    Assert.AreEqual(3, Room(harness, copy).Count(), "three left after a reload");
                }
                if (index < room.Length - 1)
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Taste, MarshesTasteOfTheirOwnMedicineV1.Secure));
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Taste, MarshesTasteOfTheirOwnMedicineV1.Secure));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Taste, MarshesTasteOfTheirOwnMedicineV1.Protect));

            var hax = SceneCreature(harness, copy, Taste, MarshesTasteOfTheirOwnMedicineV1.HaxRole);
            var medic = SceneCreature(harness, copy, Taste, MarshesTasteOfTheirOwnMedicineV1.MedicRole);
            Assert.IsNotNull(hax, "Hax comes to the room");
            Assert.IsNotNull(medic, "the medic comes");
            Assert.AreEqual((N.Hax, N.HaxName), (hax.DbId, hax.NameId));
            Assert.AreEqual((N.Medic, N.MedicName), (medic.DbId, medic.NameId));
            Assert.AreEqual(harness.Client.Player.Id, hax.SpawnPool.ScenarioOwnerCharacterId, "the player's own Hax");
            AssertOnGround(copy, hax.Position, "Hax");
            AssertOnGround(copy, medic.Position, "the medic");

            var waves = new HashSet<int>();
            for (var second = 1; second <= MarshesTasteOfTheirOwnMedicineV1.HackMilliseconds / 1000 - 2; second++)
            {
                Tick(harness, copy, 1000, Taste, MarshesTasteOfTheirOwnMedicineV1.HaxRole, MarshesTasteOfTheirOwnMedicineV1.MedicRole);
                for (var wave = 0; wave < MarshesTasteOfTheirOwnMedicineV1.Waves.Length; wave++)
                    if (SceneCreature(harness, copy, Taste, MarshesTasteOfTheirOwnMedicineV1.WaveRole(wave, 0)) != null)
                        waves.Add(wave);
                if (second == 60)
                    Assert.IsFalse(waves.Contains(2), "the third wave not before 90 s");
            }
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, waves.ToArray(), "three waves");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Taste, MarshesTasteOfTheirOwnMedicineV1.Protect), "not before 120 s");
            for (var second = 0; second < 4; second++)
                Tick(harness, copy, 1000, Taste, MarshesTasteOfTheirOwnMedicineV1.HaxRole, MarshesTasteOfTheirOwnMedicineV1.MedicRole);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Taste, MarshesTasteOfTheirOwnMedicineV1.Protect), "the hack done");
            Assert.IsTrue(harness.Client.Player.Missions[Taste].Completeable);
            Assert.IsFalse(Enumerable.Range(0, 3).SelectMany(wave => Enumerable.Range(0, 4)
                .Select(member => SceneCreature(harness, copy, Taste, MarshesTasteOfTheirOwnMedicineV1.WaveRole(wave, member))))
                .Any(creature => creature != null), "the waves go");
            Assert.IsNull(SceneCreature(harness, copy, Taste, MarshesTasteOfTheirOwnMedicineV1.MedicRole), "the medic goes");

            hax = SceneCreature(harness, copy, Taste, MarshesTasteOfTheirOwnMedicineV1.HaxRole);
            Assert.IsNotNull(hax, "Hax stays for the turn-in");
            FailedTurnInPaysNothing(harness, hax, Taste, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, hax, Taste, 1,
                MarshesTasteOfTheirOwnMedicineV1.Experience, MarshesTasteOfTheirOwnMedicineV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesTasteOfTheirOwnMedicineV1.Rewards[1]), "the chosen item");
            Tick(harness, copy, 250);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, Giver(copy, N.Hax), Freedom), "the public Hax offers 548 next");
        }

        /// <summary>546: Hax killed in the hack fails 2 and the mission; the waves go, and the hack's timer does not complete 2 later.</summary>
        [TestMethod]
        public void TasteOfTheirOwnMedicineFailsWhenHaxDies()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            Accept(harness, Fitz(harness, marshes), Taste);
            var copy = IntoFacility(harness);
            Talk(harness, Giver(copy, N.Hax), Taste, MarshesTasteOfTheirOwnMedicineV1.Rendezvous);
            Tick(harness, copy, 250);
            foreach (var thrax in Room(harness, copy).ToArray())
                Kill(harness, copy, thrax);
            for (var second = 0; second < 15; second++)
                Tick(harness, copy, 1000, Taste, MarshesTasteOfTheirOwnMedicineV1.HaxRole, MarshesTasteOfTheirOwnMedicineV1.MedicRole);
            Assert.IsNotNull(SceneCreature(harness, copy, Taste, MarshesTasteOfTheirOwnMedicineV1.WaveRole(0, 0)), "the first wave is in");

            var hax = SceneCreature(harness, copy, Taste, MarshesTasteOfTheirOwnMedicineV1.HaxRole);
            Assert.AreEqual((int)MarshesHaxHoldsV1.HitPoints, (int)hax.Attributes[Attributes.Health].NormalMax, "the hack's Hax has the extra hit points");
            hax.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(copy, hax, null);
            harness.Drain();
            Tick(harness, copy, 250);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, Taste, MarshesTasteOfTheirOwnMedicineV1.Protect));
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[Taste].State);
            Assert.IsNull(SceneCreature(harness, copy, Taste, MarshesTasteOfTheirOwnMedicineV1.WaveRole(0, 0)), "the wave goes");
            Assert.IsNull(SceneCreature(harness, copy, Taste, MarshesTasteOfTheirOwnMedicineV1.MedicRole), "the medic goes");
            for (var second = 0; second < 120; second++)
                Tick(harness, copy, 1000);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Taste, MarshesTasteOfTheirOwnMedicineV1.Hack), "no hack after Hax");
            Assert.IsNull(SceneCreature(harness, copy, Taste, MarshesTasteOfTheirOwnMedicineV1.WaveRole(1, 0)), "no more waves");
            Assert.IsTrue(harness.Manager.TryClear(harness.Client, Taste), "the failed mission cleared");
        }

        /// <summary>
        /// 548: the Commander and his guards in Specimen Control; his death gives the keypass (once) and opens the three blocks
        /// and their terminals; each terminal frees its block's two prisoners and completes its block, once, in any order, a
        /// reload between; Sakai (the scene's) pays and takes the keypass.
        /// </summary>
        [TestMethod]
        public void FreedomFreesTheThreeCellBlocks()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            SeedCompletedMarshes(harness, Taste);
            var copy = IntoFacility(harness);
            Accept(harness, Giver(copy, N.Hax), Freedom);
            Tick(harness, copy, 250);
            var commander = SceneCreature(harness, copy, Freedom, MarshesFreedomsJustAnotherWordV2.CommanderRole);
            Assert.IsNotNull(commander, "the Commander stands");
            Assert.AreEqual((N.Commander, N.CommanderName, N.BossLevel), (commander.DbId, commander.NameId, commander.Level));
            Assert.AreEqual((int)N.CommanderHitPoints, (int)commander.Attributes[Attributes.Health].Current);
            Assert.AreEqual(TargetCategory.Hostile, commander.TargetCategory);
            AssertOnGround(copy, commander.Position, "the Commander");
            AssertPath(copy, Door(harness), commander.Position, "the Commander from the door");
            AssertPath(copy, Door(harness), ToVector(MarshesFreedomsJustAnotherWordV2.SakaiAt), "cell block C (Sakai) from the door");
            AssertPath(copy, Door(harness), ToVector(MarshesFreedomsJustAnotherWordV2.Blocks[2].Terminal), "block C's terminal from the door");
            for (var guard = 0; guard < MarshesFreedomsJustAnotherWordV2.GuardsAt.Length; guard++)
                Assert.IsNotNull(SceneCreature(harness, copy, Freedom, MarshesFreedomsJustAnotherWordV2.GuardRole(guard)), $"guard {guard + 1}");
            var sakai = SceneCreature(harness, copy, Freedom, MarshesFreedomsJustAnotherWordV2.SakaiRole);
            Assert.IsNotNull(sakai, "Sakai is held in block C");
            Assert.AreEqual(N.Sakai, sakai.DbId);
            for (var block = 0; block < 3; block++)
            {
                Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Freedom, MarshesFreedomsJustAnotherWordV2.Blocks[block].Objective));
                for (var index = 0; index < 2; index++)
                {
                    var prisoner = SceneCreature(harness, copy, Freedom, MarshesFreedomsJustAnotherWordV2.PrisonerRole(block, index));
                    Assert.IsNotNull(prisoner, $"prisoner {block}.{index}");
                    Assert.AreEqual((N.Prisoner, N.PrisonerName), (prisoner.DbId, prisoner.NameId));
                    AssertOnGround(copy, prisoner.Position, prisoner.SpawnPool.SceneActorRole);
                }
            }
            Assert.IsFalse(FindSceneObject(harness, copy, Freedom, MarshesFreedomsJustAnotherWordV2.TerminalRole(0)) != null, "no terminal before the keypass");

            Kill(harness, copy, commander);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Freedom, MarshesFreedomsJustAnotherWordV2.Keypass));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesFreedomsJustAnotherWordV2.KeycardTemplate), "the keypass");
            var order = new[] { 2, 0, 1 };
            for (var step = 0; step < order.Length; step++)
            {
                var block = order[step];
                var objective = MarshesFreedomsJustAnotherWordV2.Blocks[block].Objective;
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Freedom, objective));
                var terminal = SceneObject(harness, copy, Freedom, MarshesFreedomsJustAnotherWordV2.TerminalRole(block));
                Assert.AreEqual(MarshesFreedomsJustAnotherWordV2.TerminalClass, (uint)terminal.EntityClassId);
                AssertOnGround(copy, terminal.Position, terminal.SceneActorRole);
                Use(harness, copy, terminal, MarshesFreedomsJustAnotherWordV2.UseArgument);
                Use(harness, copy, terminal, MarshesFreedomsJustAnotherWordV2.UseArgument);
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Freedom, objective), $"block {block}");
                for (var index = 0; index < 2; index++)
                    Assert.IsNull(SceneCreature(harness, copy, Freedom, MarshesFreedomsJustAnotherWordV2.PrisonerRole(block, index)), "freed");
                if (step == 0)
                {
                    ReloadAndRecover(harness, copy);
                    Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Freedom, objective), "kept at a reload");
                }
                if (step < order.Length - 1)
                    Assert.IsFalse(harness.Client.Player.Missions[Freedom].Completeable);
            }
            Assert.IsTrue(harness.Client.Player.Missions[Freedom].Completeable);
            sakai = SceneCreature(harness, copy, Freedom, MarshesFreedomsJustAnotherWordV2.SakaiRole);
            FailedTurnInPaysNothing(harness, sakai, Freedom, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, sakai, Freedom, 2,
                MarshesFreedomsJustAnotherWordV2.Experience, MarshesFreedomsJustAnotherWordV2.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesFreedomsJustAnotherWordV2.Rewards[2]), "the chosen item");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, MarshesFreedomsJustAnotherWordV2.KeycardTemplate), "Sakai takes the keypass");
            Tick(harness, copy, 250);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, Giver(copy, N.Sakai), Leggo), "the public Sakai offers 550 next");
        }

        /// <summary>
        /// 550 in time: the crate's two detonators; each tank's charge takes one, blows its tank and gives a DNA sample; the
        /// console takes the field down; the keyboard takes the element; the entrance hall reached in time completes 4 and 6
        /// and brings Brock, who takes the two samples (10); Fitz pays on the Marshes.
        /// </summary>
        [TestMethod]
        public void LeggoMyLogosEscapesInTimeToBrock()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            SeedCompletedMarshes(harness, Taste, Freedom);
            var copy = IntoFacility(harness);
            TakeTheElement(harness, copy, true);

            Assert.IsNull(SceneCreature(harness, copy, Leggo, MarshesLeggoMyLogosV1.BrockRole), "no Brock before the escape");
            Enter(harness, ToVector(MarshesLeggoMyLogosV1.EscapeAt));
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Leggo, MarshesLeggoMyLogosV1.Escape));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Leggo, MarshesLeggoMyLogosV1.Escaped));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Leggo, MarshesLeggoMyLogosV1.Debrief));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Leggo, MarshesLeggoMyLogosV1.Russell), "no Russell in time");
            var brock = SceneCreature(harness, copy, Leggo, MarshesLeggoMyLogosV1.BrockRole);
            Assert.IsNotNull(brock, "Brock comes");
            Assert.AreEqual((N.Brock, N.BrockName, N.BrockPackage), (brock.DbId, brock.NameId, brock.Npc?.NpcPackageId));
            AssertOnGround(copy, brock.Position, "Brock");
            ReloadAndRecover(harness, copy);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Leggo, MarshesLeggoMyLogosV1.Escaped), "kept at a reload");
            for (var second = 0; second < MarshesLeggoMyLogosV1.DeadlineSeconds + 10; second += 10)
                Tick(harness, copy, 10000);
            harness.Manager.EvaluateDeadlines(harness.Client);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Leggo, MarshesLeggoMyLogosV1.Escape), "the deadline is off once out");

            Talk(harness, SceneCreature(harness, copy, Leggo, MarshesLeggoMyLogosV1.BrockRole), Leggo, MarshesLeggoMyLogosV1.Debrief);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, MarshesLeggoMyLogosV1.DnaTemplate), "Brock takes the samples");
            Assert.IsTrue(harness.Client.Player.Missions[Leggo].Completeable);
            TurnInAtFitz(harness, copy);
        }

        /// <summary>
        /// 550 late: 300 s after the element the escape fails, 6 with it; hidden 8 opens 7 and 10, Russell comes and the player
        /// is set down by him; his talk takes the samples and completes 7, which completes 10; the mission goes on to Fitz.
        /// </summary>
        [TestMethod]
        public void LeggoMyLogosLateEscapeIsSetDownByRussell()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            SeedCompletedMarshes(harness, Taste, Freedom);
            var copy = IntoFacility(harness);
            TakeTheElement(harness, copy, false);

            for (var second = 0; second < MarshesLeggoMyLogosV1.DeadlineSeconds - 10; second += 10)
                Tick(harness, copy, 10000);
            harness.Manager.EvaluateDeadlines(harness.Client);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Leggo, MarshesLeggoMyLogosV1.Escape), "not before 300 s");
            for (var second = 0; second < 20; second++)
                Tick(harness, copy, 1000);
            harness.Manager.EvaluateDeadlines(harness.Client);
            harness.Drain();
            Tick(harness, copy, 250);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, Leggo, MarshesLeggoMyLogosV1.Escape), "too late");
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, Leggo, MarshesLeggoMyLogosV1.Escaped));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Leggo, MarshesLeggoMyLogosV1.FailedEscape));
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[Leggo].State, "the mission goes on");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Leggo, MarshesLeggoMyLogosV1.Russell));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Leggo, MarshesLeggoMyLogosV1.Debrief));
            var transfer = harness.Client.PendingTransfer;
            Assert.IsNotNull(transfer, "set down by Russell");
            Assert.IsLessThan(2f, Vector3.Distance(ToVector(MarshesLeggoMyLogosV1.WakeAt), transfer.DestinationPosition), "in the entrance hall");
            harness.Client.PendingTransfer = null;
            harness.MoveTo(ToVector(MarshesLeggoMyLogosV1.WakeAt));
            harness.Drain();
            var russell = SceneCreature(harness, copy, Leggo, MarshesLeggoMyLogosV1.RussellRole);
            Assert.IsNotNull(russell, "Russell comes");
            Assert.AreEqual((N.Russell, N.RussellName, N.RussellPackage), (russell.DbId, russell.NameId, russell.Npc?.NpcPackageId));
            Assert.IsNull(SceneCreature(harness, copy, Leggo, MarshesLeggoMyLogosV1.BrockRole), "no Brock late");

            Talk(harness, russell, Leggo, MarshesLeggoMyLogosV1.Russell);
            Tick(harness, copy, 250);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, MarshesLeggoMyLogosV1.DnaTemplate), "the samples go");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Leggo, MarshesLeggoMyLogosV1.Debrief), "debriefed by Russell");
            Assert.IsTrue(harness.Client.Player.Missions[Leggo].Completeable);
            TurnInAtFitz(harness, copy);
        }

        /// <summary>
        /// 550: a death after the element is no escape. The player gets up at the facility's door, inside the entrance hall,
        /// but a revive is no step into it (MissionAreaService counts a Move from outside to inside only), nor is a step inside
        /// it; the 5 minutes run out, the late ending comes and Russell's set-down lands on the risen player.
        /// </summary>
        [TestMethod]
        public void LeggoMyLogosDeathAfterTheElementIsNoEscape()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            SeedCompletedMarshes(harness, Taste, Freedom);
            var copy = IntoFacility(harness);
            TakeTheElement(harness, copy, false);

            var player = harness.Client.Player;
            player.Attributes[Attributes.Health].Current = 0;
            PlayerDeath.AtZero(copy, player, null);
            harness.Drain();
            Assert.AreEqual(CharacterState.Dead, player.State, "dead");
            PlayerDeath.ReviveMe(harness.Client, null);
            harness.Drain();
            Assert.AreNotEqual(CharacterState.Dead, player.State, "up again");
            var escape = ToVector(MarshesLeggoMyLogosV1.EscapeAt);
            Assert.IsLessThan((float)MarshesLeggoMyLogosV1.EscapeRadius, Vector3.Distance(player.Position, escape), "up at the door, in the hall");
            var areas = new MissionAreaService(() => harness.Manager);
            var step = player.Position + new Vector3(1, 0, 0);
            areas.RecordAcceptedMovement(harness.Client, player.Position, step);
            harness.MoveTo(step);
            harness.Drain();
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Leggo, MarshesLeggoMyLogosV1.Escape), "no escape by dying");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Leggo, MarshesLeggoMyLogosV1.Escaped));

            for (var second = 0; second < MarshesLeggoMyLogosV1.DeadlineSeconds + 10; second += 10)
                Tick(harness, copy, 10000);
            harness.Manager.EvaluateDeadlines(harness.Client);
            harness.Drain();
            Tick(harness, copy, 250);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, Leggo, MarshesLeggoMyLogosV1.Escape), "too late");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Leggo, MarshesLeggoMyLogosV1.FailedEscape));
            var transfer = harness.Client.PendingTransfer;
            Assert.IsNotNull(transfer, "set down by Russell after the revive");
            Assert.IsLessThan(2f, Vector3.Distance(ToVector(MarshesLeggoMyLogosV1.WakeAt), transfer.DestinationPosition));
            Assert.IsNotNull(SceneCreature(harness, copy, Leggo, MarshesLeggoMyLogosV1.RussellRole), "Russell comes");
        }

        /// <summary>550 abandoned with the detonators and a sample held: both go; taken again, a full crate.</summary>
        [TestMethod]
        public void LeggoMyLogosAbandonedTakesItsItems()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            SeedCompletedMarshes(harness, Taste, Freedom);
            var copy = IntoFacility(harness);
            var sakai = Giver(copy, N.Sakai);
            Accept(harness, sakai, Leggo);
            Tick(harness, copy, 250);
            Use(harness, copy, SceneObject(harness, copy, Leggo, MarshesLeggoMyLogosV1.CrateRole), MarshesLeggoMyLogosV1.CrateUseArgument);
            Tick(harness, copy, 250);
            Use(harness, copy, SceneObject(harness, copy, Leggo, MarshesLeggoMyLogosV1.PrimaryChargeRole), MarshesLeggoMyLogosV1.UseArgument);
            Assert.AreEqual((1U, 1U), (WildernessRanjaGorgeTests.Held(harness, MarshesLeggoMyLogosV1.DetonatorTemplate),
                WildernessRanjaGorgeTests.Held(harness, MarshesLeggoMyLogosV1.DnaTemplate)));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Leggo));
            harness.Manager.Scenes.Tick(copy);
            harness.Drain();
            Assert.AreEqual((0U, 0U), (WildernessRanjaGorgeTests.Held(harness, MarshesLeggoMyLogosV1.DetonatorTemplate),
                WildernessRanjaGorgeTests.Held(harness, MarshesLeggoMyLogosV1.DnaTemplate)), "both go");
            Accept(harness, sakai, Leggo);
            Tick(harness, copy, 250);
            var crate = SceneObject(harness, copy, Leggo, MarshesLeggoMyLogosV1.CrateRole);
            Assert.AreEqual(MarshesLeggoMyLogosV1.CrateState, (uint)crate.StateId, "a full crate");
            Use(harness, copy, crate, MarshesLeggoMyLogosV1.CrateUseArgument);
            Assert.AreEqual(MarshesLeggoMyLogosV1.Detonators, WildernessRanjaGorgeTests.Held(harness, MarshesLeggoMyLogosV1.DetonatorTemplate));
        }

        /// <summary>
        /// 718: the three bosses in their rooms, the six pieces of lab equipment (a piece counted once, at its destruction),
        /// the DNA dispenser (once); Mesgel's first hit raises the alarm (hidden 7, 6 opens): the teleporter and its first four
        /// Thrax at once, four more 30 s later; the teleporter shot down completes 6 and no more come; the bosses' deaths
        /// complete 1-3; HQ pays by radio, once.
        /// </summary>
        [TestMethod]
        public void SlayOnTargetKillsTheBossesAndStopsTheInflux()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            SeedCompletedMarshes(harness, Taste);
            var copy = IntoFacility(harness);
            Accept(harness, Giver(copy, N.Hax), Slay);
            Tick(harness, copy, 250);
            var bosses = new Creature[3];
            var levels = new[] { (N.Larvax, N.LarvaxName, N.LarvaxHitPoints), (N.Xynnr, N.XynnrName, N.XynnrHitPoints), (N.Mesgel, N.MesgelName, N.MesgelHitPoints) };
            for (var index = 0; index < 3; index++)
            {
                bosses[index] = SceneCreature(harness, copy, Slay, MarshesSlayOnTargetV2.BossRole(index));
                Assert.IsNotNull(bosses[index], $"boss {index + 1}");
                Assert.AreEqual((levels[index].Item1, levels[index].Item2, N.BossLevel), (bosses[index].DbId, bosses[index].NameId, bosses[index].Level));
                Assert.AreEqual((int)levels[index].Item3, (int)bosses[index].Attributes[Attributes.Health].Current);
                Assert.AreEqual(TargetCategory.Hostile, bosses[index].TargetCategory);
                AssertOnGround(copy, bosses[index].Position, $"boss {index + 1}");
                AssertPath(copy, ToVector(MarshesTasteOfTheirOwnMedicineV1.RoomAt), bosses[index].Position, $"boss {index + 1} from the checkpoint");
            }
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Slay, MarshesSlayOnTargetV2.Teleporter));
            Assert.IsNull(FindSceneObject(harness, copy, Slay, MarshesSlayOnTargetV2.TeleporterRole), "no teleporter before the alarm");

            for (var index = 0; index < MarshesSlayOnTargetV2.EquipmentAt.Length; index++)
            {
                var piece = SceneObject(harness, copy, Slay, MarshesSlayOnTargetV2.EquipmentRole(index));
                Assert.AreEqual(MarshesSlayOnTargetV2.EquipmentClass, (uint)piece.EntityClassId);
                AssertOnGround(copy, piece.Position, piece.SceneActorRole);
                Damage(harness, copy, piece, 60);
                Assert.AreEqual((uint)index, Counter(harness, Slay, MarshesSlayOnTargetV2.Equipment), "not at 60 of 100");
                Damage(harness, copy, piece, 60);
                Damage(harness, copy, piece, 60);
                Assert.AreEqual((uint)index + 1, Counter(harness, Slay, MarshesSlayOnTargetV2.Equipment), $"piece {index + 1}, once");
                Assert.AreEqual(MarshesSlayOnTargetV2.DestroyedState, (uint)piece.StateId);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Slay, MarshesSlayOnTargetV2.Equipment));

            var dispenser = SceneObject(harness, copy, Slay, MarshesSlayOnTargetV2.DispenserRole);
            AssertOnGround(copy, dispenser.Position, "the dispenser");
            Use(harness, copy, dispenser, MarshesSlayOnTargetV2.DispenserArgument);
            Use(harness, copy, dispenser, MarshesSlayOnTargetV2.DispenserArgument);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesSlayOnTargetV2.DnaTemplate), "one jar");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Slay, MarshesSlayOnTargetV2.Specimen));

            harness.MoveTo(bosses[2].Position + new Vector3(0, 0, 5));
            CreatureManager.RecordCombatDamage(copy, bosses[2], harness.Client.Player, 50);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Slay, MarshesSlayOnTargetV2.Alarm), "the alarm");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Slay, MarshesSlayOnTargetV2.Teleporter));
            var teleporter = SceneObject(harness, copy, Slay, MarshesSlayOnTargetV2.TeleporterRole);
            Assert.AreEqual(MarshesSlayOnTargetV2.TeleporterClass, (uint)teleporter.EntityClassId);
            AssertOnGround(copy, teleporter.Position, "the teleporter");
            Assert.AreEqual((int)MarshesSlayOnTargetV2.WaveSize, Influx(harness, copy, 0).Count(), "the first four at once");
            foreach (var at in MarshesSlayOnTargetV2.WaveAt)
                AssertOnGround(copy, ToVector(at), "a wave's place");
            Assert.IsEmpty(Influx(harness, copy, 1), "the second not yet");
            // Each wave is killed as it comes, as the player would.
            foreach (var thrax in Influx(harness, copy, 0).ToArray())
                Kill(harness, copy, thrax);
            for (var second = 0; second < MarshesSlayOnTargetV2.WaveMilliseconds / 1000 + 1; second++)
                Tick(harness, copy, 1000);
            Assert.AreEqual((int)MarshesSlayOnTargetV2.WaveSize, Influx(harness, copy, 1).Count(), "four more after 30 s");
            foreach (var thrax in Influx(harness, copy, 1).ToArray())
                Kill(harness, copy, thrax);
            Assert.AreNotEqual(CharacterState.Dead, harness.Client.Player.State, "the player stands");

                        Damage(harness, copy, teleporter, 200);
            Damage(harness, copy, teleporter, 200);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Slay, MarshesSlayOnTargetV2.Teleporter), "not at 400 of 600");
            Damage(harness, copy, teleporter, 200);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Slay, MarshesSlayOnTargetV2.Teleporter), "shot down");
            for (var second = 0; second < 2 * MarshesSlayOnTargetV2.WaveMilliseconds / 1000 + 5; second++)
                Tick(harness, copy, 1000);
            Assert.IsEmpty(Influx(harness, copy, 2), "no more come");

            for (var index = 0; index < 3; index++)
            {
                Kill(harness, copy, SceneCreature(harness, copy, Slay, MarshesSlayOnTargetV2.BossRole(index)));
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Slay, MarshesSlayOnTargetV2.Bosses[index].Objective), $"boss {index + 1}");
                if (index == 0)
                    ReloadAndRecover(harness, copy);
            }
            Assert.IsTrue(harness.Client.Player.Missions[Slay].Completeable);
            var before = harness.Context.ReadRewardTotals();
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, Slay, 1, null));
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Slay), "paid once");
            var after = harness.Context.ReadRewardTotals();
            Assert.AreEqual((before.Experience + MarshesSlayOnTargetV2.Experience, before.Credits + (int)MarshesSlayOnTargetV2.Credits),
                (after.Experience, after.Credits));
            Assert.IsFalse(harness.Manager.TryCompleteRadioMission(harness.Client, Slay, 1, null), "and only once");
            Assert.AreEqual(MarshesSlayOnTargetV2.CryoGrenades, WildernessRanjaGorgeTests.Held(harness, MarshesSlayOnTargetV2.CryoGrenade), "the chosen stack");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, MarshesSlayOnTargetV2.DnaTemplate), "the jar goes");
        }

        /// <summary>
        /// To-fix row 9 (bot run): 6 opened only on a player-side hit on Mesgel, so his death with no such hit (a GM kill, a
        /// death that is not credited) left 718 never completable. Why this test: Mesgel dead with no hit, here with no killer
        /// at all, before the alarm: no teleporter stands, hidden 8 completes and 6 with it, and the bosses finish the mission.
        /// </summary>
        [TestMethod]
        public void SlayOnTargetCompletesWhenMesgelDiesBeforeTheAlarm()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            SeedCompletedMarshes(harness, Taste);
            var copy = IntoFacility(harness);
            Accept(harness, Giver(copy, N.Hax), Slay);
            Tick(harness, copy, 250);
            var mesgel = SceneCreature(harness, copy, Slay, MarshesSlayOnTargetV2.BossRole(2));
            harness.MoveTo(mesgel.Position + new Vector3(0, 0, 5));
            mesgel.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(copy, mesgel, null);
            harness.Drain();
            Tick(harness, copy, 250);
            Tick(harness, copy, 250);

            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Slay, MarshesSlayOnTargetV2.Mesgel), "Mesgel's death counts");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Slay, MarshesSlayOnTargetV2.Alarm), "no alarm");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Slay, MarshesSlayOnTargetV2.Quiet));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Slay, MarshesSlayOnTargetV2.Teleporter), "no influx to stop");
            Assert.IsNull(FindSceneObject(harness, copy, Slay, MarshesSlayOnTargetV2.TeleporterRole), "no teleporter");
            Assert.IsEmpty(Influx(harness, copy, 0), "no wave");

            for (var index = 0; index < MarshesSlayOnTargetV2.EquipmentAt.Length; index++)
            {
                var piece = SceneObject(harness, copy, Slay, MarshesSlayOnTargetV2.EquipmentRole(index));
                for (var shot = 0; shot < 2; shot++)
                    Damage(harness, copy, piece, 60);
            }
            Use(harness, copy, SceneObject(harness, copy, Slay, MarshesSlayOnTargetV2.DispenserRole), MarshesSlayOnTargetV2.DispenserArgument);
            for (var index = 0; index < 2; index++)
                Kill(harness, copy, SceneCreature(harness, copy, Slay, MarshesSlayOnTargetV2.BossRole(index)));
            Assert.IsTrue(harness.Client.Player.Missions[Slay].Completeable, "completable without the teleporter");
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, Slay, 1, null));
        }

        /// <summary>
        /// To-fix row 9, the other side: after the alarm Mesgel's death changes nothing, the teleporter must still be shot
        /// down. Why this test: the alarm fails hidden 8 first, so his later death cannot complete 6.
        /// </summary>
        [TestMethod]
        public void SlayOnTargetStillNeedsTheTeleporterWhenMesgelDiesAfterTheAlarm()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            SeedCompletedMarshes(harness, Taste);
            var copy = IntoFacility(harness);
            Accept(harness, Giver(copy, N.Hax), Slay);
            Tick(harness, copy, 250);
            var mesgel = SceneCreature(harness, copy, Slay, MarshesSlayOnTargetV2.BossRole(2));
            harness.MoveTo(mesgel.Position + new Vector3(0, 0, 5));
            CreatureManager.RecordCombatDamage(copy, mesgel, harness.Client.Player, 50);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Slay, MarshesSlayOnTargetV2.Alarm), "the alarm");
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, Slay, MarshesSlayOnTargetV2.Quiet), "the quiet end is gone");

            Kill(harness, copy, mesgel);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Slay, MarshesSlayOnTargetV2.Mesgel));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Slay, MarshesSlayOnTargetV2.Teleporter), "the teleporter still stands");
            var teleporter = SceneObject(harness, copy, Slay, MarshesSlayOnTargetV2.TeleporterRole);
            for (var shot = 0; shot < 3; shot++)
                Damage(harness, copy, teleporter, 200);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Slay, MarshesSlayOnTargetV2.Teleporter), "shot down");
        }

        /// <summary>
        /// 1674: Vincent's after 550; the lake's teleporter, walked to from Retread City, takes the player to the chamber's
        /// plates, from where Jumna is walked to; his talk completes 1 and he pays; the chamber's plates take the player back.
        /// </summary>
        [TestMethod]
        public void SubterraneanChamberTakesTheLakesTeleporterToJumna()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var vincent = Npc(marshes, MarshesSubterraneanChamberV1.Vincent);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, vincent, Chamber), "1674 needs 550");
            SeedCompletedMarshes(harness, Taste, Freedom, Leggo);
            Accept(harness, vincent, Chamber);

            var rows = harness.World.TeleporterEntries.AsNoTracking().Where(row => row.Id == N.LakePad || row.Id == N.ChamberPad).ToDictionary(row => row.Id);
            Assert.HasCount(2, rows);
            foreach (var row in rows.Values)
                Assert.AreEqual((N.Marshes, N.PadClass, N.LocalTeleporter), (row.MapContextId, row.ClassId, (uint)row.Type), $"{row.Id}");
            CollectionAssert.AreEquivalent(new[] { (N.LakePad, N.ChamberPad), (N.ChamberPad, N.LakePad) },
                harness.World.LocalTeleporterLinkEntries.AsNoTracking().ToArray().Where(link => rows.ContainsKey(link.FromTeleporterId))
                    .Select(link => (link.FromTeleporterId, link.ToTeleporterId)).ToArray(), "both ways");
            foreach (var row in rows.Values)
            {
                var loaded = new DynamicObject
                {
                    Position = new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ),
                    Rotation = row.Rotation,
                    MapContextId = row.MapContextId,
                    RuntimeMapChannel = marshes,
                    EntityClassId = (EntityClasses)row.ClassId,
                    Comment = row.Description,
                    ObjectData = new WaypointInfo(row.Id, false, WaypointType.LocalTeleporter),
                    DynamicObjectType = DynamicObjectType.LocalTeleporter
                };
                marshes.Teleporters.Add(row.Id, loaded);
                harness.Objects.Teleporters.TryAdd(row.Id, loaded);
            }
            harness.Objects.InitLocalTeleporters();
            harness.Objects.DynamicObjectWorker(marshes, 0);
            var lake = marshes.Teleporters[N.LakePad];
            var plates = marshes.Teleporters[N.ChamberPad];
            AssertOnGround(marshes, lake.Position, "the lake's pad");
            AssertOnGround(marshes, plates.Position, "the chamber's plates");
            AssertPath(marshes, RetreadCityWaypoint, lake.Position, "the lake's pad from Retread City");
            Assert.IsFalse(marshes.NavMesh.FindPath(RetreadCityWaypoint, plates.Position, out var walked) != null && walked,
                "the chamber is not walked to");
            var jumna = Npc(marshes, N.Jumna);
            AssertPath(marshes, plates.Position, jumna.Position, "Jumna from the plates");

            harness.MoveTo(RetreadCityWaypoint);
            harness.Drain();
            Teleport(harness, marshes, lake);
            Assert.IsLessThan(3f, Vector3.Distance(plates.Position, harness.Client.Player.Position), "on the chamber's plates");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Chamber, MarshesSubterraneanChamberV1.Speak));
            Talk(harness, jumna, Chamber, MarshesSubterraneanChamberV1.Speak);
            Assert.IsTrue(harness.Client.Player.Missions[Chamber].Completeable);
            FailedTurnInPaysNothing(harness, jumna, Chamber, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, jumna, Chamber, 0,
                MarshesSubterraneanChamberV1.Experience, MarshesSubterraneanChamberV1.Credits);
            Assert.AreEqual(MarshesSubterraneanChamberV1.AdvancedMedPacks, WildernessRanjaGorgeTests.Held(harness, MarshesSubterraneanChamberV1.AdvancedMedPack));

            // Set down on the plates, the player is taken back only once he has stepped off them (LocalTeleporterWorker).
            harness.MoveTo(jumna.Position);
            harness.Drain();
            harness.Objects.DynamicObjectProximityWorker(marshes, plates, 0);
            Assert.IsNull(harness.Client.PendingTransfer, "off the plates");
            Teleport(harness, marshes, plates);
            Assert.IsLessThan(3f, Vector3.Distance(lake.Position, harness.Client.Player.Position), "back at the lake");
        }

        /// <summary>Through the lake's pad's sweep onto the far pad (MarshesRetreadArenaTests.APlayerLeftInTheDomeTakesItsPadOut).</summary>
        private static void Teleport(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject pad)
        {
            harness.MoveTo(pad.Position);
            harness.Drain();
            harness.Objects.DynamicObjectProximityWorker(map, pad, 0);
            Assert.IsNotNull(harness.Client.PendingTransfer, "taken by the pad");
            System.Threading.Thread.Sleep(DynamicObjectManager.PassageHoldMs + 50);
            harness.Objects.PassagesWorker(map);
            harness.Objects.TeleportAcknowledge(harness.Client);
            harness.Drain();
            Assert.IsNull(harness.Client.PendingTransfer);
        }

        /// <summary>550 accepted at Sakai and played to the element taken (the escape and its twin open), each step once.</summary>
        private static void TakeTheElement(WildernessRuntimeTestHarness harness, MapChannel copy, bool reload)
        {
            Accept(harness, Giver(copy, N.Sakai), Leggo);
            Tick(harness, copy, 250);
            var crate = SceneObject(harness, copy, Leggo, MarshesLeggoMyLogosV1.CrateRole);
            Assert.AreEqual(MarshesLeggoMyLogosV1.CrateClass, (uint)crate.EntityClassId);
            foreach (var (at, what) in new[]
            {
                (crate.Position, "the crate"), (ToVector(MarshesLeggoMyLogosV1.PrimaryChargeAt), "the primary tank's charge"),
                (ToVector(MarshesLeggoMyLogosV1.SecondaryChargeAt), "the secondary tank's charge"),
                (ToVector(MarshesLeggoMyLogosV1.ConsoleAt), "the console"), (ToVector(MarshesLeggoMyLogosV1.AnalyzerKeyboardAt), "the keyboard")
            })
                AssertPath(copy, Door(harness), at, $"{what} from the door");
            foreach (var role in new[] { MarshesLeggoMyLogosV1.PrimaryRole, MarshesLeggoMyLogosV1.SecondaryRole, MarshesLeggoMyLogosV1.AnalyzerRole })
                AssertOnGround(copy, SceneObject(harness, copy, Leggo, role).Position, role);
            Assert.IsNull(FindSceneObject(harness, copy, Leggo, MarshesLeggoMyLogosV1.PrimaryChargeRole), "no charge before the detonators");
            Use(harness, copy, crate, MarshesLeggoMyLogosV1.CrateUseArgument);
            Use(harness, copy, crate, MarshesLeggoMyLogosV1.CrateUseArgument);
            Assert.AreEqual(MarshesLeggoMyLogosV1.Detonators, WildernessRanjaGorgeTests.Held(harness, MarshesLeggoMyLogosV1.DetonatorTemplate), "two, once");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Leggo, MarshesLeggoMyLogosV1.Bombs));
            Tick(harness, copy, 250);

            foreach (var (objective, charge, device, next) in new[]
            {
                (MarshesLeggoMyLogosV1.Primary, MarshesLeggoMyLogosV1.PrimaryChargeRole, MarshesLeggoMyLogosV1.PrimaryRole, MarshesLeggoMyLogosV1.Secondary),
                (MarshesLeggoMyLogosV1.Secondary, MarshesLeggoMyLogosV1.SecondaryChargeRole, MarshesLeggoMyLogosV1.SecondaryRole, MarshesLeggoMyLogosV1.Field)
            })
            {
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Leggo, objective));
                var held = WildernessRanjaGorgeTests.Held(harness, MarshesLeggoMyLogosV1.DetonatorTemplate);
                var bomb = SceneObject(harness, copy, Leggo, charge);
                Assert.AreEqual(MarshesLeggoMyLogosV1.ChargeClass, (uint)bomb.EntityClassId);
                AssertOnGround(copy, bomb.Position, charge);
                Use(harness, copy, bomb, MarshesLeggoMyLogosV1.UseArgument);
                Use(harness, copy, bomb, MarshesLeggoMyLogosV1.UseArgument);
                Tick(harness, copy, 250);
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Leggo, objective), device);
                Assert.AreEqual(held - 1, WildernessRanjaGorgeTests.Held(harness, MarshesLeggoMyLogosV1.DetonatorTemplate), "one detonator a charge");
                Assert.AreEqual(MarshesLeggoMyLogosV1.DeviceDestroyedState, (uint)SceneObject(harness, copy, Leggo, device).StateId, $"{device} blown");
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Leggo, next));
                if (reload && objective == MarshesLeggoMyLogosV1.Primary)
                {
                    ReloadAndRecover(harness, copy);
                    Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Leggo, objective), "kept at a reload");
                }
            }
            Assert.AreEqual(MarshesLeggoMyLogosV1.DnaSamples, WildernessRanjaGorgeTests.Held(harness, MarshesLeggoMyLogosV1.DnaTemplate), "a sample a tank");

            var console = SceneObject(harness, copy, Leggo, MarshesLeggoMyLogosV1.ConsoleRole);
            AssertOnGround(copy, console.Position, "the console");
            Use(harness, copy, console, MarshesLeggoMyLogosV1.UseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Leggo, MarshesLeggoMyLogosV1.Field));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Leggo, MarshesLeggoMyLogosV1.Logos));
            var analyzer = SceneObject(harness, copy, Leggo, MarshesLeggoMyLogosV1.AnalyzerRole);
            Assert.AreEqual(MarshesLeggoMyLogosV1.AnalyzerReadyState, (uint)analyzer.StateId);
            var keyboard = SceneObject(harness, copy, Leggo, MarshesLeggoMyLogosV1.AnalyzerKeyboardRole);
            AssertOnGround(copy, keyboard.Position, "the keyboard");
            Use(harness, copy, keyboard, MarshesLeggoMyLogosV1.UseArgument);
            Use(harness, copy, keyboard, MarshesLeggoMyLogosV1.UseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Leggo, MarshesLeggoMyLogosV1.Logos));
            Assert.AreEqual(MarshesLeggoMyLogosV1.AnalyzerTakenState, (uint)analyzer.StateId, "the element taken");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Leggo, MarshesLeggoMyLogosV1.Escape));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Leggo, MarshesLeggoMyLogosV1.Escaped));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Leggo, MarshesLeggoMyLogosV1.Debrief));
            AssertPath(copy, keyboard.Position, ToVector(MarshesLeggoMyLogosV1.EscapeAt), "the way out");
        }

        /// <summary>Out of the facility and to Fitz at Paludos: he pays 550 once.</summary>
        private static void TurnInAtFitz(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            harness.Manager.Scenes.Detach(harness.Client, copy);
            var marshes = EnterMarshes(harness);
            var fitz = Fitz(harness, marshes);
            FailedTurnInPaysNothing(harness, fitz, Leggo, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, fitz, Leggo, 3,
                MarshesLeggoMyLogosV1.Experience, MarshesLeggoMyLogosV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesLeggoMyLogosV1.VirulentLeechGun), "the chosen gun");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, MarshesLeggoMyLogosV1.DetonatorTemplate));
        }

        private static Creature Fitz(WildernessRuntimeTestHarness harness, MapChannel marshes)
        {
            var fitz = Npc(marshes, PoolsOf(harness, N.Fitz).First());
            Assert.AreEqual(N.Fitz, fitz.DbId);
            return fitz;
        }

        /// <summary>Into the squad's copy of the facility through its door on the Marshes, the scenes resumed (as MapLoaded).</summary>
        private static MapChannel IntoFacility(WildernessRuntimeTestHarness harness)
        {
            if (!harness.Maps.MapChannelArray.ContainsKey(Facility))
                LoadMap(harness, Facility);
            var copy = ThroughTheDoor(harness, Facility);
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            return copy;
        }

        /// <summary>Where the facility's door sets a player down (its map_link's destination).</summary>
        private static Vector3 Door(WildernessRuntimeTestHarness harness)
        {
            var door = harness.World.MapLinkEntries.AsNoTracking().Where(row => row.DestMapContextId == Facility).OrderBy(row => row.Id).First();
            var at = new Vector3((float)door.DestPosX, (float)door.DestPosY, (float)door.DestPosZ);
            return harness.Maps.MapChannelArray[Facility].NavMesh.GroundHeight(at) is { } ground ? at with { Y = ground } : at;
        }

        private static IEnumerable<Creature> Room(WildernessRuntimeTestHarness harness, MapChannel copy) =>
            Enumerable.Range(0, MarshesTasteOfTheirOwnMedicineV1.Room.Length)
                .Select(index => SceneCreature(harness, copy, Taste, MarshesTasteOfTheirOwnMedicineV1.RoomRole(index))).Where(creature => creature != null);

        private static IEnumerable<Creature> Influx(WildernessRuntimeTestHarness harness, MapChannel copy, int wave) =>
            Enumerable.Range(0, (int)MarshesSlayOnTargetV2.WaveSize)
                .Select(member => SceneCreature(harness, copy, Slay, MarshesSlayOnTargetV2.WaveRole(wave, member))).Where(creature => creature != null);

        /// <summary>Into a mission area at <paramref name="at"/>, as MissionAreaService takes a step from far off.</summary>
        private static void Enter(WildernessRuntimeTestHarness harness, Vector3 at)
        {
            var from = at + new Vector3(60, 0, 60);
            harness.MoveTo(at);
            new MissionAreaService(() => harness.Manager).RecordAcceptedMovement(harness.Client, from, at);
            harness.Drain();
        }

        /// <summary>The mission state as the database holds it again, and the scene out and back in (MarshesEscortsTests).</summary>
        private static void ReloadAndRecover(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            WildernessRanjaGorgeTests.Reload(harness);
            harness.Manager.Scenes.Detach(harness.Client, copy);
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            Tick(harness, copy, 250);
            Tick(harness, copy, 250);
        }

        /// <summary>The living creatures of this copy only (PoolsTurpisRefineryTests.Here).</summary>
        private static IEnumerable<Creature> Here(MapChannel map) =>
            Living(map).Where(creature => ReferenceEquals(creature.RuntimeMapChannel, map));

        private static Creature Giver(MapChannel copy, uint pool)
        {
            if (!Here(copy).Any(creature => creature.SpawnPool?.DbId == pool))
                Spawn(copy, pool);
            var giver = Here(copy).SingleOrDefault(creature => creature.SpawnPool?.DbId == pool && creature.Npc != null);
            Assert.IsNotNull(giver, $"pool {pool} stands in the copy");
            return giver;
        }

        private static HashSet<string> Runs(WildernessRuntimeTestHarness harness, uint mission)
        {
            using var unit = harness.CreateChar();
            return unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, mission).Select(scene => scene.RunId).ToHashSet();
        }

        /// <summary>The object of this test's own run, if it is there: a squad copy can still hold an earlier test's.</summary>
        private static DynamicObject FindSceneObject(WildernessRuntimeTestHarness harness, MapChannel map, uint mission, string role)
        {
            var runs = Runs(harness, mission);
            return map.DynamicObjects.SingleOrDefault(candidate => candidate.SceneMissionId == mission && candidate.SceneActorRole == role &&
                candidate.IsInWorld && runs.Contains(candidate.SceneRunId));
        }

        private static DynamicObject SceneObject(WildernessRuntimeTestHarness harness, MapChannel map, uint mission, string role)
        {
            var obj = FindSceneObject(harness, map, mission, role);
            Assert.IsNotNull(obj, $"{role} is there");
            return obj;
        }

        private static Creature SceneCreature(WildernessRuntimeTestHarness harness, MapChannel map, uint mission, string role)
        {
            var runs = Runs(harness, mission);
            return Here(map).SingleOrDefault(creature => creature.SpawnPool?.SceneActorRole == role && runs.Contains(creature.SpawnPool.SceneRunId) &&
                creature.State != CharacterState.Dead);
        }

        /// <summary>Kills the creature as the player and runs the scene on.</summary>
        private static void Kill(WildernessRuntimeTestHarness harness, MapChannel map, Creature creature)
        {
            Assert.IsNotNull(creature);
            harness.MoveTo(creature.Position);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(map, creature, harness.Client.Player);
            harness.Drain();
            Tick(harness, map, 250);
            Tick(harness, map, 250);
        }

        /// <summary>
        /// The clock on (PoolsTurpisRefineryTests.Tick); the player and the scene's <paramref name="kept"/> actors kept at full
        /// health (only the scene's clock is under test, not the fights).
        /// </summary>
        private static void Tick(WildernessRuntimeTestHarness harness, MapChannel map, long milliseconds, uint mission = 0, params string[] kept)
        {
            foreach (var role in kept)
                if (SceneCreature(harness, map, mission, role) is { } actor)
                    actor.Attributes[Attributes.Health].Current = actor.Attributes[Attributes.Health].NormalMax;
            Steady(harness);
            harness.UtcNow = harness.UtcNow.AddMilliseconds(milliseconds);
            BehaviorManager.Instance.MapChannelThink(map, System.Math.Min(milliseconds, 1000));
            harness.Manager.Scenes.Tick(map);
            harness.Drain();
        }

        /// <summary>A use of the object from beside it, and its recovery if one is due (PoolsTurpisRefineryTests.Use).</summary>
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
            harness.Manager.Scenes.Tick(map);
            harness.Drain();
        }

        /// <summary>
        /// The player kept standing: the facility's Thrax and bosses kill the test's low-level player in seconds, and a dead
        /// player's shots do not land (PracticeTargetManager.CanHit); only the scenes are under test here, not the fights.
        /// </summary>
        private static void Steady(WildernessRuntimeTestHarness harness)
        {
            var health = harness.Client.Player.Attributes[Attributes.Health];
            health.NormalMax = System.Math.Max(health.NormalMax, 1_000_000);
            health.Current = health.NormalMax;
        }

        /// <summary>A weapon hit on the object (PoolsTurpisRefineryTests.Damage).</summary>
        private static void Damage(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj, int damage)
        {
            Steady(harness);
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            var action = new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0) { TargetId = obj.EntityId };
            MissileManager.Instance.MissileLaunch(map, action, damage);
            MissileManager.Instance.DoWork(map, 1000);
            harness.Manager.Scenes.Tick(map);
            harness.Drain();
        }

        private static uint Counter(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].Counters[0];

        /// <summary>A complete path on the navmesh from <paramref name="from"/> to <paramref name="to"/>.</summary>
        private static void AssertPath(MapChannel map, Vector3 from, Vector3 to, string what) =>
            Assert.IsTrue(map.NavMesh.FindPath(from, to, out var complete) != null && complete, $"{what}: a path");

        private static Vector3 ToVector((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);

        private static Vector3 ToVector(ScenePosition at) => new(at.X, at.Y, at.Z);
    }
}
