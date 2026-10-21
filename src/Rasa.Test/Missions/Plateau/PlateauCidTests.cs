extern alias RasaGame;

using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Plateau
{
    using static PlateauCampResistanceTests;

    /// <summary>
    /// Fort Defiance's murder case, Plateau, played through as a player plays it: Blue Flu (1014), Murder Most
    /// Foul (895) and both ends of Looking For Clues (928).
    ///
    /// Why these tests: as PlateauPerduTests. The suspect, the Mainframe, Pendrik and the Craps Players are the
    /// player's own scene actors, so the tests talk to them as the client does; the Mainframe is talked to as
    /// the Corman arc's blood analyzer (WildernessCormanArcTests), Pendrik's choice as Alia's
    /// (WildernessAliaBranchesTests), and his hostile copy is killed as Clean Slate's berserk soldier.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauCidTests
    {
        private const uint BlueFlu = PlateauBlueFluV1.MissionId;
        private const uint Murder = PlateauMurderMostFoulV1.MissionId;
        private const uint Clues = PlateauLookingForCluesV1.MissionId;
        private const uint Parkman = PlateauCidNpcsV1.ParkmanPool;
        private const uint Provost = PlateauCidNpcsV1.Provost, Crais = PlateauCidNpcsV1.Crais, Norton = PlateauCidNpcsV1.Norton;
        private const uint Remick = PlateauWedgeRockNpcsV1.Remick, Sampei = PlateauLookingForCluesV1.SampeiNpc;

        [TestMethod]
        public void BlueFluSendsThePlayerFromParkmanToProvostAndNortonLyingInTheWarehouse()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Parkman, Provost, Norton);
            var parkman = Living(plateau).Single(creature => creature.SpawnPool?.DbId == Parkman);
            Assert.AreEqual(PlateauCidNpcsV1.Parkman, parkman.DbId);
            var provost = RequireNpc(plateau, Provost);
            var norton = RequireNpc(plateau, Norton);
            Assert.AreEqual(NpcPose.LyingDown, norton.Pose, "Norton lies in the warehouse");
            var at = PlateauCidNpcsV1.Moves[0].At;
            Assert.IsTrue(Vector3.Distance(new Vector3((float)at.X, (float)at.Y, (float)at.Z), norton.Position) < 1, $"{norton.Position}");
            Assert.IsFalse(Offers(harness, parkman, BlueFlu), "Blue Flu waits for The Defiant Ones");

            SeedCompleted(harness, PlateauDefiantOnesV1.MissionId);
            var offer = WildernessRanjaGorgeTests.Offer(harness, parkman, BlueFlu);
            CollectionAssert.AreEquivalent(new[] { PlateauBlueFluV1.MedPack, PlateauBlueFluV1.EmpGrenade, PlateauBlueFluV1.ResKit,
                PlateauBlueFluV1.ConcussionGrenade }, offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            harness.MoveTo(parkman.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, parkman.EntityId, BlueFlu));
            harness.Drain();
            Assert.IsFalse(HandsIn(harness, norton, BlueFlu, PlateauBlueFluV1.Warehouse), "Provost first");

            Talk(harness, provost, BlueFlu, PlateauBlueFluV1.Headquarters);
            Talk(harness, norton, BlueFlu, PlateauBlueFluV1.Warehouse);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, norton, BlueFlu, 1,
                PlateauBlueFluV1.Experience, PlateauBlueFluV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauBlueFluV1.EmpGrenade), "the second choice");
            Assert.IsTrue(Offers(harness, norton, Murder), "Murder Most Foul opens at Norton");
        }

        [TestMethod]
        public void MurderMostFoulScansTheBodyMeetsTheSuspectAndUploadsToTheMainframe()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Provost, Crais, Norton);
            var provost = RequireNpc(plateau, Provost);
            var crais = RequireNpc(plateau, Crais);
            var norton = RequireNpc(plateau, Norton);
            SeedCompleted(harness, BlueFlu);

            var offer = WildernessRanjaGorgeTests.Offer(harness, norton, Murder);
            CollectionAssert.AreEquivalent(new[] { PlateauMurderMostFoulV1.StealthHelmet, PlateauMurderMostFoulV1.GravitonGloves,
                PlateauMurderMostFoulV1.HazmatGloves, PlateauMurderMostFoulV1.MotorAssistGloves },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            harness.MoveTo(norton.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, norton.EntityId, Murder));
            harness.Drain();
            var mainframe = plateau.DynamicObjects.Single(obj => obj.SceneMissionId == Murder && obj.SceneActorRole == "mainframe");
            Assert.AreEqual(PlateauMurderMostFoulV1.MainframeClass, (uint)mainframe.EntityClassId);
            Assert.IsNull(SceneCreature(plateau, "suspect"), "no suspect before the scan");

            Talk(harness, provost, Murder, PlateauMurderMostFoulV1.Provost);
            Talk(harness, crais, Murder, PlateauMurderMostFoulV1.Crais);
            Talk(harness, norton, Murder, PlateauMurderMostFoulV1.Scan);
            Tick(harness, plateau, 250);
            var suspect = SceneCreature(plateau, "suspect");
            Assert.IsNotNull(suspect, "the suspect comes to the crime scene");
            Assert.AreEqual(PlateauCidNpcsV1.Suspect, suspect.DbId);
            Assert.AreEqual(TargetCategory.Friendly, suspect.TargetCategory);
            AssertStandsOnNavMesh(plateau, suspect.Position);

            Talk(harness, suspect, Murder, PlateauMurderMostFoulV1.Suspect);
            Tick(harness, plateau, 250);
            Assert.IsNull(SceneCreature(plateau, "suspect"), "he goes once spoken to");

            var npcs = new NpcManager(harness, harness.Manager);
            harness.MoveTo(mainframe.Position + Vector3.UnitX);
            harness.Drain();
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = mainframe.EntityId });
            npcs.CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = mainframe.EntityId, MissionId = Murder, ObjectiveId = PlateauMurderMostFoulV1.Upload, PlayerFlagId = 1
            });
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[Murder].Objectives[PlateauMurderMostFoulV1.Upload].State, "the scan uploaded");

            Talk(harness, crais, Murder, PlateauMurderMostFoulV1.Report);
            Talk(harness, crais, Murder, PlateauMurderMostFoulV1.Tell);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, crais, Murder, 3,
                PlateauMurderMostFoulV1.Experience, PlateauMurderMostFoulV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauMurderMostFoulV1.MotorAssistGloves), "the fourth choice");
            Assert.IsTrue(Offers(harness, crais, Clues), "Looking For Clues opens at Crais");
        }

        [TestMethod]
        public void LookingForCluesLetsPendrikOffWithAWarning()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var (plateau, crais) = FindPendrik(harness);
            var pendrik = SceneCreature(plateau, "pendrik");

            Choose(harness, pendrik, PlateauLookingForCluesV1.Warning);
            Tick(harness, plateau, 250);
            var mission = harness.Client.Player.Missions[Clues];
            Assert.AreEqual(MissionObjectiveState.Completed, mission.Objectives[PlateauLookingForCluesV1.Search].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[PlateauLookingForCluesV1.LetGo].State);
            Assert.AreEqual(MissionObjectiveState.Inactive, mission.Objectives[PlateauLookingForCluesV1.Kill].State);
            foreach (var role in new[] { "pendrik", "player-1", "player-2", "resisting" })
                Assert.IsNull(SceneCreature(plateau, role), $"{role} is gone");

            Talk(harness, crais, Clues, PlateauLookingForCluesV1.LetGo);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, crais, Clues, 0,
                PlateauLookingForCluesV1.Experience, PlateauLookingForCluesV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauLookingForCluesV1.StealthHelmet), "the first choice");
        }

        [TestMethod]
        public void LookingForCluesArrestMeansAFightWithPendrik()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var (plateau, crais) = FindPendrik(harness);
            var pendrik = SceneCreature(plateau, "pendrik");

            Choose(harness, pendrik, PlateauLookingForCluesV1.Arrest);
            Tick(harness, plateau, 250);
            var mission = harness.Client.Player.Missions[Clues];
            Assert.AreEqual(MissionObjectiveState.Completed, mission.Objectives[PlateauLookingForCluesV1.Search].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[PlateauLookingForCluesV1.Kill].State);
            Assert.AreEqual(MissionObjectiveState.Inactive, mission.Objectives[PlateauLookingForCluesV1.LetGo].State);
            foreach (var role in new[] { "pendrik", "player-1", "player-2" })
                Assert.IsNull(SceneCreature(plateau, role), $"{role} is gone");
            var resisting = SceneCreature(plateau, "resisting");
            Assert.IsNotNull(resisting, "Pendrik resists");
            Assert.AreEqual(PlateauCidNpcsV1.HostilePendrik, resisting.DbId);
            Assert.AreEqual(TargetCategory.Hostile, resisting.TargetCategory);
            Assert.IsTrue(Vector3.Distance(PlateauLookingForCluesV1.PendrikAt.ToVector(), resisting.Position) < 1.5f, $"{resisting.Position}");
            Assert.IsFalse(HandsIn(harness, crais, Clues, PlateauLookingForCluesV1.Killed), "not while he lives");

            harness.MoveTo(resisting.Position);
            resisting.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(plateau, resisting, harness.Client.Player);
            Tick(harness, plateau, 250);
            mission = harness.Client.Player.Missions[Clues];
            Assert.AreEqual(MissionObjectiveState.Completed, mission.Objectives[PlateauLookingForCluesV1.Kill].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[PlateauLookingForCluesV1.Killed].State);

            Talk(harness, crais, Clues, PlateauLookingForCluesV1.Killed);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, crais, Clues, 2,
                PlateauLookingForCluesV1.Experience, PlateauLookingForCluesV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauLookingForCluesV1.ReflectiveVest), "the third choice");
        }

        /// <summary>Looking For Clues up to Pendrik: Remick twice, Crais, Sampei, and the craps game is there.</summary>
        private static (MapChannel Plateau, Creature Crais) FindPendrik(WildernessRuntimeTestHarness harness)
        {
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Crais, Remick, Sampei);
            var crais = RequireNpc(plateau, Crais);
            var remick = RequireNpc(plateau, Remick);
            var sampei = RequireNpc(plateau, Sampei);
            SeedCompleted(harness, BlueFlu, Murder);

            harness.MoveTo(crais.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, crais.EntityId, Clues));
            harness.Drain();
            Assert.IsFalse(HandsIn(harness, sampei, Clues, PlateauLookingForCluesV1.Sampei), "Remick first");
            Talk(harness, remick, Clues, PlateauLookingForCluesV1.Remick);
            Talk(harness, remick, Clues, PlateauLookingForCluesV1.Weapon);
            Talk(harness, crais, Clues, PlateauLookingForCluesV1.Tell);
            Tick(harness, plateau, 250);
            Assert.IsNull(SceneCreature(plateau, "pendrik"), "no craps game before Sampei names him");
            Talk(harness, sampei, Clues, PlateauLookingForCluesV1.Sampei);
            Tick(harness, plateau, 250);

            var pendrik = SceneCreature(plateau, "pendrik");
            Assert.IsNotNull(pendrik, "the craps game gathers");
            Assert.AreEqual(PlateauCidNpcsV1.Pendrik, pendrik.DbId);
            Assert.AreEqual(TargetCategory.Friendly, pendrik.TargetCategory);
            AssertStandsOnNavMesh(plateau, pendrik.Position);
            foreach (var role in new[] { "player-1", "player-2" })
            {
                var player = SceneCreature(plateau, role);
                Assert.IsNotNull(player, role);
                Assert.AreEqual(PlateauCidNpcsV1.CrapsPlayer, player.DbId);
                AssertStandsOnNavMesh(plateau, player.Position);
            }
            Assert.IsNull(SceneCreature(plateau, "resisting"));
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[Clues].Objectives[PlateauLookingForCluesV1.Search].State);
            return (plateau, crais);
        }

        private static void Choose(WildernessRuntimeTestHarness harness, Creature npc, uint choice)
        {
            var npcs = new NpcManager(harness, harness.Manager);
            harness.MoveTo(npc.Position);
            harness.Drain();
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = npc.EntityId });
            npcs.PerformNPCChoice(harness.Client, new PerformNPCChoicePacket
            {
                EntityId = npc.EntityId, MissionId = Clues, ObjectiveId = PlateauLookingForCluesV1.Search, PlayerFlagId = 1,
                ChoiceIdx = (int)choice
            });
            harness.Drain();
        }

        private static void AssertStandsOnNavMesh(MapChannel plateau, Vector3 position)
        {
            var ground = plateau.NavMesh.GroundHeight(position);
            Assert.IsNotNull(ground, $"navmesh under {position}");
            Assert.IsTrue(System.Math.Abs(ground.Value - position.Y) < 1f, $"{position} on the ground {ground}");
        }

        /// <summary>As WildernessRuntimeTestHarness.Tick, on the Plateau.</summary>
        private static void Tick(WildernessRuntimeTestHarness harness, MapChannel plateau, long milliseconds)
        {
            harness.UtcNow = harness.UtcNow.AddMilliseconds(milliseconds);
            BehaviorManager.Instance.MapChannelThink(plateau, milliseconds);
            harness.Manager.Scenes.Tick(plateau);
            harness.Drain();
        }

        private static Creature SceneCreature(MapChannel plateau, string role) => Living(plateau)
            .SingleOrDefault(creature => creature.SpawnPool?.SceneActorRole == role);

        private static bool Offers(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.MissionDispense, out var offered) &&
            ((System.Collections.Generic.Dictionary<uint, MissionInfo>)offered).ContainsKey(mission);

        private static bool HandsIn(WildernessRuntimeTestHarness harness, Creature npc, uint mission, uint objective)
        {
            harness.MoveTo(npc.Position);
            harness.Drain();
            return harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, mission, objective, 1);
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
    }
}
