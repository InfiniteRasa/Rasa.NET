extern alias RasaGame;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Missions.Content.Plateau;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Plateau
{
    using static PlateauCampResistanceTests;
    using static PlateauTrinityBridgeLogosTests;

    /// <summary>
    /// The Temporal Chamber's missions, played through as a player plays them: Array Of Hope
    /// (935: the six Logos Conduits on Trinity Bridge, then Veryin beyond the portal), Future Sacrifice
    /// (1171: through the chamber's teleporter to Colonel Hudson in Alia Das), The Final Assault (1167:
    /// three stations, then the detonator on the power core, turned in to Veryin), Foreas Base Past (1178:
    /// through the pad on Veryin's left, Villaran escorted to the drop-ship, Xykryl's strike force) and Send a
    /// Message (1181: the Communications Array).
    ///
    /// Why these tests: the conduits are personal objects on the public Plateau, but Veryin and Hudson are
    /// the server's NPCs of the squad instance 2029, which MapChannelManager copies as the player goes
    /// through the portal (map_link 49). A talk to Veryin that did not wait for the conduits, a copy without
    /// the teleporter pair, or a core that took the detonator before the three stations would each break the
    /// chain; so the door is walked as MapLinkManager walks it, the teleporter as LocalTeleporterWorker takes
    /// a player, and the stations are shot as Toxic Detox's pumps (PlateauMartyrsCanyonTests).
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauTemporalChamberTests
    {
        private const uint ArrayOfHope = PlateauArrayOfHopeV1.MissionId;
        private const uint FutureSacrifice = PlateauFutureSacrificeV1.MissionId;
        private const uint FinalAssault = PlateauFinalAssaultV1.MissionId;
        private const uint Chamber = PlateauTemporalChamberNpcsV1.MapContextId;
        private const uint ForeasBasePast = PlateauForeasBasePastV1.MissionId;
        private const uint SendAMessage = PlateauSendAMessageV1.MissionId;

        [TestMethod]
        public void ArrayOfHopeWaitsForTheSixLogosAndVeryinWaitsForTheSixConduits()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            SeedCompleted(harness, PlateauVoiceFromThePastV1.MissionId, PlateauTrinityLogosPastV1.MissionId, 1047, 1048, 1049, 1051);
            var hologram = Hologram(plateau);
            Assert.IsFalse(Offered(harness, hologram).Contains(ArrayOfHope), "waits for the sixth Logos mission");
            SeedCompleted(harness, 1057);

            var offer = WildernessRanjaGorgeTests.Offer(harness, hologram, ArrayOfHope);
            CollectionAssert.AreEqual(new[]
                {
                    (PlateauArrayOfHopeV1.StandardMedPack, 3U), (PlateauArrayOfHopeV1.IncendiaryGrenade, 3U),
                    (PlateauArrayOfHopeV1.AdvancedMedPack, 2U), (PlateauArrayOfHopeV1.EmpGrenade, 3U)
                },
                offer.SelectableReward.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            Assert.IsFalse(plateau.DynamicObjects.Any(obj => obj.SceneMissionId == ArrayOfHope), "no conduit before the mission");
            harness.MoveTo(hologram.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, hologram.EntityId, ArrayOfHope));
            harness.Drain();

            var conduits = Conduits(plateau);
            for (var index = 0; index < conduits.Length; index++)
            {
                Assert.AreEqual(PlateauArrayOfHopeV1.ConduitClass, (uint)conduits[index].EntityClassId);
                Assert.AreEqual(PlateauArrayOfHopeV1.ReadyState, (uint)conduits[index].StateId);
                var ground = plateau.NavMesh.GroundHeight(conduits[index].Position);
                Assert.IsNotNull(ground, $"{conduits[index].SceneActorRole} has no navmesh under it");
                Assert.AreEqual(ground.Value, conduits[index].Position.Y, 0.5f, $"{conduits[index].SceneActorRole} on the bridge");
            }

            // Through the portal before the conduits: Veryin has nothing to say yet.
            var copy = IntoTheChamber(harness);
            var veryin = Npc(copy, PlateauTemporalChamberNpcsV1.Veryin, PlateauTemporalChamberNpcsV1.VeryinPackage);
            harness.MoveTo(veryin.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, veryin.EntityId, ArrayOfHope, 1, 1),
                "Veryin waits for the conduits");
            Return(harness, plateau);

            for (var index = 0; index < conduits.Length; index++)
            {
                Assert.AreNotEqual(MissionObjectiveState.Completed,
                    harness.Client.Player.Missions[ArrayOfHope].Objectives[PlateauArrayOfHopeV1.Tally].State, $"before conduit {index + 1}");
                Use(harness, plateau, conduits[index]);
                Assert.AreEqual(PlateauArrayOfHopeV1.UsedState, (uint)conduits[index].StateId, $"conduit {index + 1} on");
            }
            Use(harness, plateau, conduits[0]);
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[ArrayOfHope].Objectives[PlateauArrayOfHopeV1.Tally].State, "the six conduits");
            Assert.IsFalse(harness.Client.Player.Missions[ArrayOfHope].Completeable);

            copy = IntoTheChamber(harness);
            veryin = Npc(copy, PlateauTemporalChamberNpcsV1.Veryin, PlateauTemporalChamberNpcsV1.VeryinPackage);
            AssertHandInTopic(Conversation(harness, veryin), ArrayOfHope, 1);
            harness.MoveTo(veryin.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, veryin.EntityId, ArrayOfHope, 1, 1), "Who comes here?");
            harness.Drain();
            Assert.IsTrue(harness.Client.Player.Missions[ArrayOfHope].Completeable);

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, veryin, ArrayOfHope, 1,
                PlateauArrayOfHopeV1.Experience, PlateauArrayOfHopeV1.Credits);
            Assert.AreEqual(3U, WildernessRanjaGorgeTests.Held(harness, PlateauArrayOfHopeV1.IncendiaryGrenade), "the second choice");
        }

        [TestMethod]
        public void VeryinSendsThePlayerThroughTheTeleporterToHudsonWhoGivesTheFinalAssault()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            SeedCompleted(harness, ArrayOfHope);
            var copy = IntoTheChamber(harness);
            var veryin = Npc(copy, PlateauTemporalChamberNpcsV1.Veryin, PlateauTemporalChamberNpcsV1.VeryinPackage);
            var hudson = Npc(copy, PlateauTemporalChamberNpcsV1.Hudson, PlateauTemporalChamberNpcsV1.HudsonPackage);

            var offer = WildernessRanjaGorgeTests.Offer(harness, veryin, FutureSacrifice);
            CollectionAssert.AreEqual(new[]
                {
                    (PlateauFutureSacrificeV1.ResKit, 1U), (PlateauFutureSacrificeV1.ConcussionGrenade, 3U),
                    (PlateauFutureSacrificeV1.AdvancedMedPack, 6U), (PlateauFutureSacrificeV1.FragGrenade, 3U)
                },
                offer.SelectableReward.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            harness.MoveTo(veryin.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, veryin.EntityId, FutureSacrifice));
            harness.Drain();

            // The pad on Veryin's right takes the player to Hudson's.
            var pad = copy.Teleporters[PlateauTemporalChamberNpcsV1.ChamberPad];
            var far = copy.Teleporters[PlateauTemporalChamberNpcsV1.AliaDasPad];
            Assert.AreNotSame(harness.Objects.Teleporters[PlateauTemporalChamberNpcsV1.ChamberPad], pad, "the copy's own pad");
            Teleport(harness, copy, pad);
            Assert.IsTrue(Vector3.Distance(harness.Client.Player.Position, far.Position) < 2f, "at the Alia Das pad");
            Assert.IsTrue(Vector3.Distance(harness.Client.Player.Position, hudson.Position) < 15f, "by Hudson");

            AssertHandInTopic(Conversation(harness, hudson), FutureSacrifice, 1);
            harness.MoveTo(hudson.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, hudson.EntityId, FutureSacrifice, 1, 1));
            harness.Drain();
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, hudson, FutureSacrifice, 2,
                PlateauFutureSacrificeV1.Experience, PlateauFutureSacrificeV1.Credits);
            Assert.AreEqual(6U, WildernessRanjaGorgeTests.Held(harness, PlateauFutureSacrificeV1.AdvancedMedPack), "the third choice");

            WildernessRanjaGorgeTests.Offer(harness, hudson, FinalAssault);
            harness.Objects.DynamicObjectProximityWorker(copy, far, 0);   // off the pad they arrived on, by Hudson
            Teleport(harness, copy, far);
            Assert.IsTrue(Vector3.Distance(harness.Client.Player.Position, pad.Position) < 2f, "and back to the chamber");
        }

        [TestMethod]
        public void TheFinalAssaultTakesThreeStationsBeforeTheCoreTakesTheDetonator()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            SeedCompleted(harness, ArrayOfHope, FutureSacrifice);
            var copy = IntoTheChamber(harness);
            var veryin = Npc(copy, PlateauTemporalChamberNpcsV1.Veryin, PlateauTemporalChamberNpcsV1.VeryinPackage);
            var hudson = Npc(copy, PlateauTemporalChamberNpcsV1.Hudson, PlateauTemporalChamberNpcsV1.HudsonPackage);
            Assert.IsFalse(copy.DynamicObjects.Any(obj => obj.SceneMissionId == FinalAssault), "nothing before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, hudson, FinalAssault);
            CollectionAssert.AreEqual(new[]
                {
                    PlateauFinalAssaultV1.RocketLauncher, PlateauFinalAssaultV1.Rifle,
                    PlateauFinalAssaultV1.IncendiaryNetGun, PlateauFinalAssaultV1.PulsePistol
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            harness.MoveTo(hudson.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, hudson.EntityId, FinalAssault));
            harness.Drain();
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauFinalAssaultV1.Detonator), "Hudson's detonator");

            var stations = PlateauFinalAssaultV1.Stations.Select(station => copy.DynamicObjects.Single(obj =>
                obj.SceneMissionId == FinalAssault && obj.SceneActorRole == PlateauFinalAssaultV1.StationRole(station.Objective))).ToArray();
            var core = copy.DynamicObjects.Single(obj => obj.SceneMissionId == FinalAssault && obj.SceneActorRole == PlateauFinalAssaultV1.CoreRole);
            Assert.AreEqual(PlateauFinalAssaultV1.CoreClass, (uint)core.EntityClassId);
            foreach (var station in stations)
            {
                Assert.AreEqual(PlateauFinalAssaultV1.IntactState, (uint)station.StateId);
                var ground = copy.NavMesh.GroundHeight(station.Position);
                Assert.IsNotNull(ground, $"{station.SceneActorRole} has no navmesh under it");
                Assert.AreEqual(ground.Value, station.Position.Y, 0.5f, $"{station.SceneActorRole} on the ground");
            }

            var mission = harness.Client.Player.Missions[FinalAssault];
            Assert.AreEqual(MissionObjectiveState.Inactive, mission.Objectives[PlateauFinalAssaultV1.Detonate].State);
            Use(harness, copy, core);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauFinalAssaultV1.Detonator), "the core is locked");
            Assert.AreEqual(PlateauFinalAssaultV1.CoreReadyState, (uint)core.StateId);

            var objectives = new[] { PlateauFinalAssaultV1.Alpha, PlateauFinalAssaultV1.Bravo, PlateauFinalAssaultV1.Charlie };
            for (var index = 0; index < stations.Length; index++)
            {
                Damage(harness, copy, stations[index], (int)PlateauFinalAssaultV1.StationHitPoints);
                Assert.AreEqual(PlateauFinalAssaultV1.DestroyedState, (uint)stations[index].StateId, $"station {index + 1} down");
                mission = harness.Client.Player.Missions[FinalAssault];
                Assert.AreEqual(MissionObjectiveState.Completed, mission.Objectives[objectives[index]].State, $"objective {objectives[index]}");
                Assert.AreEqual(index == 2 ? MissionObjectiveState.Incomplete : MissionObjectiveState.Inactive,
                    mission.Objectives[PlateauFinalAssaultV1.Detonate].State, $"the core after station {index + 1}");
            }

            Use(harness, copy, core);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauFinalAssaultV1.Detonator), "the detonator on the core");
            Assert.AreEqual(PlateauFinalAssaultV1.CoreSetState, (uint)core.StateId);
            mission = harness.Client.Player.Missions[FinalAssault];
            Assert.AreEqual(MissionObjectiveState.Completed, mission.Objectives[PlateauFinalAssaultV1.Detonate].State);
            Assert.IsTrue(mission.Completeable);

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, veryin, FinalAssault, 0,
                PlateauFinalAssaultV1.Experience, PlateauFinalAssaultV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauFinalAssaultV1.RocketLauncher), "the first choice");
        }

        [TestMethod]
        public void ForeasBasePastEscortsVillaranToTheDropshipAndBeatsXykrylsStrikeForce()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            SeedCompleted(harness, ArrayOfHope, FutureSacrifice);
            var copy = IntoTheChamber(harness);
            var veryin = Npc(copy, PlateauTemporalChamberNpcsV1.Veryin, PlateauTemporalChamberNpcsV1.VeryinPackage);
            Assert.IsFalse(Offered(harness, veryin).Contains(ForeasBasePast), "waits for The Final Assault");
            SeedCompleted(harness, FinalAssault);

            var offer = WildernessRanjaGorgeTests.Offer(harness, veryin, ForeasBasePast);
            Assert.IsEmpty(offer.SelectableReward, "no items");
            harness.MoveTo(veryin.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, veryin.EntityId, ForeasBasePast));
            harness.Drain();
            Tick(harness, copy, 250);

            // From the start: the defenders, Villaran beside Fonseca, the drop-ship on its pad; no Bane yet.
            foreach (var role in PlateauForeasBasePastScene.Statics.Take(4).Append(PlateauForeasBasePastScene.Villaran))
            {
                var friend = SceneCreature(harness, copy, ForeasBasePast, role);
                Assert.IsNotNull(friend, $"{role} is there");
                Assert.AreEqual(TargetCategory.Friendly, friend.TargetCategory, role);
                AssertStandsOnNavMesh(copy, friend.Position);
            }
            foreach (var role in new[] { "landing-pad", "dropship" })
                AssertStandsOnNavMesh(copy, SceneObject(harness, copy, ForeasBasePast, role).Position);
            Assert.IsTrue(PlateauForeasBasePastScene.FirstWave.Concat(PlateauForeasBasePastScene.SecondWave())
                .All(role => SceneCreature(harness, copy, ForeasBasePast, role) == null), "no Bane before Fonseca's talk");
            var villaran = SceneCreature(harness, copy, ForeasBasePast, PlateauForeasBasePastScene.Villaran);
            Assert.AreEqual(PlateauForeasBasePastNpcsV1.Villaran, villaran.DbId);
            for (var index = 0; index < PlateauForeasBasePastV1.Route.Length; index++)
                AssertStandsOnNavMesh(copy, ToVector(PlateauForeasBasePastV1.Route[index]));

            // The pad on Veryin's left takes the player to Foreas Base, north of HQ; Fonseca is inside.
            var pad = copy.Teleporters[PlateauForeasBasePastNpcsV1.ChamberPad];
            var far = copy.Teleporters[PlateauForeasBasePastNpcsV1.PastPad];
            Assert.IsTrue(pad.Position.X < veryin.Position.X, "on Veryin's left (Hudson's is on the right)");
            Teleport(harness, copy, pad);
            Assert.IsTrue(Vector3.Distance(harness.Client.Player.Position, far.Position) < 2f, "at the pad north of HQ");
            Spawn(copy, PlateauForeasBasePastNpcsV1.FonsecaPool);
            var fonseca = Living(copy).Single(npc => npc.DbId == PlateauForeasBasePastNpcsV1.Fonseca && npc.Npc != null &&
                ReferenceEquals(npc.RuntimeMapChannel, copy));
            Assert.AreEqual(PlateauForeasBasePastNpcsV1.FonsecaPackage, fonseca.Npc.NpcPackageId);
            AssertStandsOnNavMesh(copy, fonseca.Position);
            Assert.IsTrue(Vector3.Distance(fonseca.Position, villaran.Position) < 5f, "Villaran beside him");
            Assert.IsTrue(copy.NavMesh.FindPath(far.Position, fonseca.Position, out var toFonseca) != null && toFonseca, "the pad to Fonseca");

            // 1: the first wave comes for Villaran as he sets off.
            Talk(harness, fonseca, ForeasBasePast, PlateauForeasBasePastV1.FirstTalk);
            Tick(harness, copy, 250);
            var mission = harness.Client.Player.Missions[ForeasBasePast];
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[PlateauForeasBasePastV1.Escort].State);
            var wave = PlateauForeasBasePastScene.FirstWave.Select(role => SceneCreature(harness, copy, ForeasBasePast, role)).ToArray();
            for (var index = 0; index < wave.Length; index++)
            {
                Assert.IsNotNull(wave[index], PlateauForeasBasePastScene.FirstWave[index]);
                Assert.AreEqual(TargetCategory.Hostile, wave[index].TargetCategory);
                Assert.AreEqual(index < 4 ? PlateauForeasBasePastNpcsV1.Recon : PlateauForeasBasePastNpcsV1.Cavalryman, wave[index].DbId);
                Assert.IsTrue(copy.NavMesh.FindPath(copy.NavMesh.Nearest(wave[index].Position).Value, villaran.Position, out var reaches) != null &&
                    reaches, $"{PlateauForeasBasePastScene.FirstWave[index]} can reach Villaran");
            }
            foreach (var bane in wave)
                Kill(harness, copy, bane);
            harness.MoveTo(fonseca.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, fonseca.EntityId, ForeasBasePast,
                PlateauForeasBasePastV1.SecondTalk, 1), "Fonseca's second talk waits for the escort");

            // 3: Villaran walks out through the gate to the drop-ship and is gone.
            var start = villaran.Position;
            for (var tick = 0; tick < 300 && mission.Objectives[PlateauForeasBasePastV1.Escort].State != MissionObjectiveState.Completed; tick++)
            {
                if (SceneCreature(harness, copy, ForeasBasePast, PlateauForeasBasePastScene.Villaran) != null)
                    harness.MoveTo(villaran.Position + Vector3.UnitZ);
                Tick(harness, copy, 1000);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, mission.Objectives[PlateauForeasBasePastV1.Escort].State,
                $"Villaran stalled at {villaran.Position}");
            Assert.IsTrue(Vector3.Distance(start, villaran.Position) > 90, "he walked the whole way");
            Assert.IsTrue(Vector3.Distance(ToVector(PlateauForeasBasePastV1.DropshipAt), villaran.Position) < 15, "by the drop-ship");
            Assert.IsNull(SceneCreature(harness, copy, ForeasBasePast, PlateauForeasBasePastScene.Villaran), "he has boarded");
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[PlateauForeasBasePastV1.SecondTalk].State);

            // A reload halfway keeps the escort done and the drop-ship there; no Villaran comes back.
            WildernessRanjaGorgeTests.Reload(harness);
            Tick(harness, copy, 250);
            mission = harness.Client.Player.Missions[ForeasBasePast];
            Assert.AreEqual(MissionObjectiveState.Completed, mission.Objectives[PlateauForeasBasePastV1.Escort].State, "after the reload");
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[PlateauForeasBasePastV1.SecondTalk].State, "after the reload");
            Assert.IsNull(SceneCreature(harness, copy, ForeasBasePast, PlateauForeasBasePastScene.Villaran), "still boarded");
            SceneObject(harness, copy, ForeasBasePast, "dropship");

            // 4, 5: Xykryl and his strike force; one count of five, whoever kills them.
            Talk(harness, fonseca, ForeasBasePast, PlateauForeasBasePastV1.SecondTalk);
            Tick(harness, copy, 250);
            var strike = PlateauForeasBasePastScene.SecondWave().Select(role => SceneCreature(harness, copy, ForeasBasePast, role)).ToArray();
            Assert.AreEqual(PlateauForeasBasePastNpcsV1.Xykryl, strike[0].DbId);
            Assert.IsTrue(strike.Skip(1).All(bane => bane.DbId == PlateauForeasBasePastNpcsV1.SpecialForces));
            for (var index = 0; index < strike.Length; index++)
            {
                Assert.AreEqual(TargetCategory.Hostile, strike[index].TargetCategory);
                AssertStandsOnNavMesh(copy, strike[index].Position);
                Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[PlateauForeasBasePastV1.Kill].State, $"before kill {index + 1}");
                if (index == 1)
                {
                    // A defender's kill counts as the player's.
                    strike[index].Attributes[Attributes.Health].Current = 0;
                    harness.Creatures.HandleCreatureKill(copy, strike[index], null);
                    Tick(harness, copy, 250);
                }
                else
                    Kill(harness, copy, strike[index]);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, mission.Objectives[PlateauForeasBasePastV1.Kill].State, "the five killed");
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[PlateauForeasBasePastV1.ThirdTalk].State);

            // 6, 7: Fonseca, then back through the pad north of HQ to Veryin.
            Talk(harness, fonseca, ForeasBasePast, PlateauForeasBasePastV1.ThirdTalk);
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[PlateauForeasBasePastV1.Return].State);
            Assert.IsFalse(mission.Completeable);
            harness.MoveTo(far.Position + new Vector3(0, 0, 4));
            harness.Objects.DynamicObjectProximityWorker(copy, far, 0);
            Teleport(harness, copy, far);
            Assert.IsTrue(Vector3.Distance(harness.Client.Player.Position, pad.Position) < 2f, "back in the chamber");
            Talk(harness, veryin, ForeasBasePast, PlateauForeasBasePastV1.Return);
            Assert.IsTrue(harness.Client.Player.Missions[ForeasBasePast].Completeable);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, veryin, ForeasBasePast, null,
                PlateauForeasBasePastV1.Experience, PlateauForeasBasePastV1.Credits);
            Assert.IsTrue(Offered(harness, veryin).Contains(SendAMessage), "Send a Message opens");
        }

        [TestMethod]
        public void ForeasBasePastFailsWhenVillaranDiesOnTheWay()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            SeedCompleted(harness, ArrayOfHope, FutureSacrifice, FinalAssault);
            var copy = IntoTheChamber(harness);
            var veryin = Npc(copy, PlateauTemporalChamberNpcsV1.Veryin, PlateauTemporalChamberNpcsV1.VeryinPackage);
            harness.MoveTo(veryin.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, veryin.EntityId, ForeasBasePast));
            harness.Drain();
            Tick(harness, copy, 250);
            Teleport(harness, copy, copy.Teleporters[PlateauForeasBasePastNpcsV1.ChamberPad]);
            Spawn(copy, PlateauForeasBasePastNpcsV1.FonsecaPool);
            var fonseca = Living(copy).Single(npc => npc.DbId == PlateauForeasBasePastNpcsV1.Fonseca && npc.Npc != null &&
                ReferenceEquals(npc.RuntimeMapChannel, copy));
            Talk(harness, fonseca, ForeasBasePast, PlateauForeasBasePastV1.FirstTalk);
            var villaran = SceneCreature(harness, copy, ForeasBasePast, PlateauForeasBasePastScene.Villaran);
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 1000);
            Assert.IsTrue(Vector3.Distance(ToVector(PlateauForeasBasePastV1.VillaranAt), villaran.Position) > 2, "he is on his way");

            villaran.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(copy, villaran, null);
            Tick(harness, copy, 250);
            var mission = harness.Client.Player.Missions[ForeasBasePast];
            Assert.AreEqual(MissionObjectiveState.Failed, mission.Objectives[PlateauForeasBasePastV1.Escort].State);
            Assert.AreEqual(MissionState.Failed, mission.State);
            Assert.AreEqual(MissionObjectiveState.Inactive, mission.Objectives[PlateauForeasBasePastV1.SecondTalk].State);
        }

        [TestMethod]
        public void ForeasBasePastAbandonedTakesItsSceneAndIsOfferedAgain()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            SeedCompleted(harness, ArrayOfHope, FutureSacrifice, FinalAssault);
            var copy = IntoTheChamber(harness);
            var veryin = Npc(copy, PlateauTemporalChamberNpcsV1.Veryin, PlateauTemporalChamberNpcsV1.VeryinPackage);
            harness.MoveTo(veryin.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, veryin.EntityId, ForeasBasePast));
            harness.Drain();
            Tick(harness, copy, 250);
            Teleport(harness, copy, copy.Teleporters[PlateauForeasBasePastNpcsV1.ChamberPad]);
            Spawn(copy, PlateauForeasBasePastNpcsV1.FonsecaPool);
            var fonseca = Living(copy).Single(npc => npc.DbId == PlateauForeasBasePastNpcsV1.Fonseca && npc.Npc != null &&
                ReferenceEquals(npc.RuntimeMapChannel, copy));
            Talk(harness, fonseca, ForeasBasePast, PlateauForeasBasePastV1.FirstTalk);
            Tick(harness, copy, 1000);
            var roles = PlateauForeasBasePastScene.FirstWave.Concat(PlateauForeasBasePastScene.Statics.Take(4))
                .Append(PlateauForeasBasePastScene.Villaran).ToArray();
            Assert.IsTrue(roles.All(role => SceneCreature(harness, copy, ForeasBasePast, role) != null), "the escort under way");

            var creatures = Living(copy).Where(creature => creature.SpawnPool?.SceneRunId != null &&
                ReferenceEquals(creature.RuntimeMapChannel, copy) && roles.Contains(creature.SpawnPool.SceneActorRole)).ToArray();
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, ForeasBasePast));
            Tick(harness, copy, 250);
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(ForeasBasePast), "gone from the log");
            Assert.IsFalse(creatures.Any(creature => Living(copy).Contains(creature)), "Villaran, the defenders and the wave are gone");
            Assert.IsFalse(copy.DynamicObjects.Any(obj => obj.SceneMissionId == ForeasBasePast && obj.IsInWorld), "the drop-ship is gone");
            Assert.IsTrue(Offered(harness, veryin).Contains(ForeasBasePast), "Veryin offers it again");
        }

        [TestMethod]
        public void SendAMessageUsesTheCommunicationsArray()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            SeedCompleted(harness, ArrayOfHope, FutureSacrifice, FinalAssault);
            var copy = IntoTheChamber(harness);
            var veryin = Npc(copy, PlateauTemporalChamberNpcsV1.Veryin, PlateauTemporalChamberNpcsV1.VeryinPackage);
            Assert.IsFalse(Offered(harness, veryin).Contains(SendAMessage), "waits for Foreas Base Past");
            SeedCompleted(harness, ForeasBasePast);
            Assert.IsEmpty(WildernessRanjaGorgeTests.Offer(harness, veryin, SendAMessage).SelectableReward, "no items");
            harness.MoveTo(veryin.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, veryin.EntityId, SendAMessage));
            harness.Drain();

            var array = SceneObject(harness, copy, SendAMessage, PlateauSendAMessageV1.ArrayRole);
            Assert.AreEqual(PlateauSendAMessageV1.ArrayClass, (uint)array.EntityClassId);
            Assert.AreEqual(PlateauSendAMessageV1.ReadyState, (uint)array.StateId);
            AssertStandsOnNavMesh(copy, array.Position, 0.5f);
            Assert.IsTrue(copy.NavMesh.FindPath(veryin.Position, array.Position, out var walked) != null && walked, "walked to from Veryin");
            Assert.IsFalse(harness.Client.Player.Missions[SendAMessage].Completeable);

            Use(harness, copy, array);
            Assert.AreEqual(PlateauSendAMessageV1.UsedState, (uint)array.StateId);
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[SendAMessage].Objectives[PlateauSendAMessageV1.Use].State);
            Assert.IsTrue(harness.Client.Player.Missions[SendAMessage].Completeable);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, veryin, SendAMessage, null,
                PlateauSendAMessageV1.Experience, PlateauSendAMessageV1.Credits);
        }

        /// <summary>
        /// The chamber as the server loads it (LoadMap), with its teleporters as InitTeleporters reads them and
        /// the links as InitLocalTeleporters does, then the player through the portal into the squad's copy.
        /// </summary>
        private static MapChannel IntoTheChamber(WildernessRuntimeTestHarness harness)
        {
            if (!harness.Maps.MapChannelArray.ContainsKey(Chamber))
            {
                var template = LoadMap(harness, Chamber);
                foreach (var row in harness.World.TeleporterEntries.AsNoTracking().Where(row => row.MapContextId == Chamber && row.Type == 1).ToArray())
                {
                    var pad = new DynamicObject
                    {
                        Position = new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ),
                        Rotation = row.Rotation,
                        MapContextId = Chamber,
                        RuntimeMapChannel = template,
                        EntityClassId = (EntityClasses)row.ClassId,
                        Comment = row.Description,
                        ObjectData = new WaypointInfo(row.Id, false, WaypointType.LocalTeleporter),
                        DynamicObjectType = DynamicObjectType.LocalTeleporter
                    };
                    template.Teleporters.Add(row.Id, pad);
                    harness.Objects.Teleporters.Add(row.Id, pad);
                }
                harness.Objects.InitLocalTeleporters();
                harness.Objects.DynamicObjectWorker(template, 0);
            }
            var copy = ThroughTheDoor(harness, Chamber);
            Assert.IsTrue(copy.IsSquadInstance);
            return copy;
        }

        private static Creature Npc(MapChannel map, uint creature, uint package)
        {
            if (!Living(map).Any(candidate => candidate.DbId == creature))
                Spawn(map, creature == PlateauTemporalChamberNpcsV1.Veryin ? PlateauTemporalChamberNpcsV1.VeryinPool : PlateauTemporalChamberNpcsV1.HudsonPool);
            var npc = Living(map).Single(candidate => candidate.DbId == creature && candidate.Npc != null);
            Assert.AreEqual(package, npc.Npc.NpcPackageId);
            return npc;
        }

        /// <summary>The scene creature of this test's own run (as PlateauMaligoBaseTests.SceneCreature).</summary>
        private static Creature SceneCreature(WildernessRuntimeTestHarness harness, MapChannel map, uint mission, string role)
        {
            var runs = Runs(harness, mission);
            return Living(map).SingleOrDefault(creature => ReferenceEquals(creature.RuntimeMapChannel, map) &&
                creature.SpawnPool?.SceneActorRole == role && runs.Contains(creature.SpawnPool.SceneRunId));
        }

        private static DynamicObject SceneObject(WildernessRuntimeTestHarness harness, MapChannel map, uint mission, string role)
        {
            var runs = Runs(harness, mission);
            var obj = map.DynamicObjects.SingleOrDefault(candidate => candidate.SceneMissionId == mission && candidate.SceneActorRole == role &&
                candidate.IsInWorld && runs.Contains(candidate.SceneRunId));
            Assert.IsNotNull(obj, $"{role} is there");
            return obj;
        }

        private static HashSet<string> Runs(WildernessRuntimeTestHarness harness, uint mission)
        {
            using var unit = harness.CreateChar();
            return unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, mission).Select(scene => scene.RunId).ToHashSet();
        }

        /// <summary>Kills the creature as the player and runs the map on.</summary>
        private static void Kill(WildernessRuntimeTestHarness harness, MapChannel map, Creature creature)
        {
            harness.MoveTo(creature.Position);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(map, creature, harness.Client.Player);
            Tick(harness, map, 250);
        }

        /// <summary>The NPC's hand-in topic of this objective, and the objective completes there.</summary>
        private static void Talk(WildernessRuntimeTestHarness harness, Creature npc, uint mission, uint objective)
        {
            AssertHandInTopic(Conversation(harness, npc), mission, objective);
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, mission, objective, 1),
                $"{mission}.{objective}");
            harness.Drain();
        }

        /// <summary>As PlateauMaligoBaseTests.Tick.</summary>
        private static void Tick(WildernessRuntimeTestHarness harness, MapChannel map, long milliseconds)
        {
            harness.UtcNow = harness.UtcNow.AddMilliseconds(milliseconds);
            BehaviorManager.Instance.MapChannelThink(map, System.Math.Min(milliseconds, 1000));
            harness.Manager.Scenes.Tick(map);
            harness.Drain();
        }

        private static void AssertStandsOnNavMesh(MapChannel map, Vector3 position, float tolerance = 1f)
        {
            var ground = map.NavMesh.GroundHeight(position);
            Assert.IsNotNull(ground, $"navmesh under {position}");
            Assert.AreEqual(ground.Value, position.Y, tolerance, $"{position} on the ground");
        }

        private static Vector3 ToVector(Rasa.Missions.Scenes.ScenePosition at) => new(at.X, at.Y, at.Z);

        private static HashSet<uint> Offered(WildernessRuntimeTestHarness harness, Creature npc) =>
            Conversation(harness, npc).TryGetValue(ConversationType.MissionDispense, out var offered)
                ? ((Dictionary<uint, MissionInfo>)offered).Keys.ToHashSet()
                : new HashSet<uint>();

        private static DynamicObject[] Conduits(MapChannel plateau) => Enumerable.Range(0, PlateauArrayOfHopeV1.Conduits.Length)
            .Select(index => plateau.DynamicObjects.Single(obj => obj.SceneMissionId == ArrayOfHope &&
                obj.SceneActorRole == PlateauArrayOfHopeV1.ConduitRole(index)))
            .ToArray();

        /// <summary>A use of the object from where it stands, and its recovery if one is due (PlateauChasmTests.Use).</summary>
        private static void Use(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            harness.Drain();
            var count = map.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = 1
            });
            if (map.PerformRecovery.Count > count)
            {
                var recovery = map.PerformRecovery.Last();
                map.PerformRecovery.Remove(recovery);
                ActorActionManager.Instance.PerformRecovery(map, recovery);
            }
            harness.Drain();
        }

        /// <summary>A weapon hit on the object, as PlateauMartyrsCanyonTests.DamagePump.</summary>
        private static void Damage(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj, int damage)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            var action = new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0) { TargetId = obj.EntityId };
            MissileManager.Instance.MissileLaunch(map, action, damage);
            MissileManager.Instance.DoWork(map, 1000);
            harness.Drain();
        }

        /// <summary>Onto the pad, as LocalTeleporterWorker takes a player, and the passage through.</summary>
        private static void Teleport(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject pad)
        {
            harness.MoveTo(pad.Position);
            harness.Drain();
            harness.Objects.DynamicObjectProximityWorker(map, pad, 0);
            Assert.IsNotNull(harness.Client.PendingTransfer, "taken by the pad");
            System.Threading.Thread.Sleep((int)DynamicObjectManager.PassageHoldMs + 50);
            harness.Objects.PassagesWorker(map);
            harness.Objects.TeleportAcknowledge(harness.Client);
            harness.Drain();
            Assert.IsNull(harness.Client.PendingTransfer);
        }
    }
}
