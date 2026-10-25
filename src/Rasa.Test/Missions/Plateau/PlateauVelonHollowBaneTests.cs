extern alias RasaGame;

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
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Plateau
{
    using static PlateauCampResistanceTests;
    using static PlateauTrinityBridgeLogosTests;

    /// <summary>
    /// Velon Hollow's Bane outpost and two missions into it, played through as a player plays them: Elder
    /// Hundra's Ancient Mysteries (1978: Velon Hollow reached, ten artifacts from the bosses, given to Hundra or
    /// to Receptive Officer Markin) and Outpost Cmdr. Russ's Mountain Spring Toxins (1979: the ponds, four
    /// chemical towers blown with his bombs, the water conversion machine overloaded).
    ///
    /// Why these tests: everything the missions use is in the squad's own copy of Velon Hollow (2136) - the
    /// bosses' pools, the drops, the scene's towers and machine - and the choice of 1978 is one of two talks
    /// that each must close the other. A drop that did not come in the copy, a map arrival that did not count,
    /// a tower that took no bomb, or a machine open before the towers would each break a mission.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauVelonHollowBaneTests
    {
        private const uint VelonHollow = PlateauVelonHollowBaneV1.MapContextId;
        private const uint AncientMysteries = PlateauAncientMysteriesV1.MissionId;
        private const uint MountainSpringToxins = PlateauMountainSpringToxinsV1.MissionId;
        private const uint BetterHalf = PlateauBetterHalfV1.MissionId;

        [TestMethod]
        public void TheOutpostsBaneAreAtTheInstancesLevelWithTheirOfficersAndTheMox()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            var copy = IntoVelonHollow(harness);

            var outpost = PlateauVelonHollowBaneV1.Outpost.Select(row => row.Pool).Distinct().ToArray();
            Spawn(copy, outpost);
            var bane = Here(copy).Where(creature => outpost.Contains(creature.SpawnPool?.DbId ?? 0)).ToArray();
            Assert.IsTrue(bane.Length > 0, "the outpost's pools stand");
            foreach (var creature in bane)
            {
                CollectionAssert.Contains(PlateauVelonHollowBaneV1.Outpost.Select(row => row.New).Distinct().ToArray(),
                    creature.DbId, "no Howling Maw creature");
                Assert.AreEqual(PlateauVelonHollowBaneV1.Level, creature.Level);
                Assert.AreEqual(TargetCategory.Hostile, creature.TargetCategory);
            }

            Spawn(copy, PlateauVelonHollowBaneV1.ControlRoomPool, PlateauVelonHollowBaneV1.TunnelPool, PlateauVelonHollowBaneV1.MoxPool);
            var officers = Here(copy).Where(creature => creature.DbId == PlateauVelonHollowBaneV1.Officer).ToArray();
            Assert.AreEqual(2, officers.Length, "one officer in the control room, one in the tunnel");
            foreach (var officer in officers)
            {
                Assert.IsNull(officer.Npc);
                Assert.AreEqual(PlateauVelonHollowBaneV1.BossLevel, officer.Level);
                Assert.AreEqual(PlateauVelonHollowBaneV1.OfficerName, officer.NameId);
                Assert.AreEqual((int)PlateauVelonHollowBaneV1.OfficerHitPoints, (int)officer.Attributes[Attributes.Health].Current);
                AssertStandsOnNavMesh(copy, officer.Position);
                Assert.AreEqual((int)PlateauVelonHollowBaneV1.Guards, Here(copy).Count(creature => creature.DbId == PlateauVelonHollowBaneV1.Soldier &&
                    creature.SpawnPool?.DbId == officer.SpawnPool.DbId), "his guards");
            }
            var mox = Here(copy).Single(creature => creature.DbId == PlateauVelonHollowBaneV1.MoxBoss);
            Assert.AreEqual(PlateauVelonHollowBaneV1.MoxName, mox.NameId);
            Assert.AreEqual((int)PlateauVelonHollowBaneV1.MoxHitPoints, (int)mox.Attributes[Attributes.Health].Current);
            Assert.AreEqual(TargetCategory.Hostile, mox.TargetCategory);
            AssertStandsOnNavMesh(copy, mox.Position);
        }

        /// <summary>Ancient Mysteries given to the Foreans: Hundra takes the artifacts; Markin then has nothing to say.</summary>
        [TestMethod]
        public void AncientMysteriesRecoversTenArtifactsFromTheBossesForHundra()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            var (hundra, markin) = HundraAndMarkin(harness, plateau);
            RecoverTheArtifacts(harness, plateau, hundra);

            AssertHandInTopic(Conversation(harness, markin), AncientMysteries, PlateauAncientMysteriesV1.Return);
            AssertHandInTopic(Conversation(harness, hundra), AncientMysteries, PlateauAncientMysteriesV1.Return);
            harness.MoveTo(hundra.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, hundra.EntityId, AncientMysteries, PlateauAncientMysteriesV1.Return, 1));
            harness.Drain();
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauAncientMysteriesV1.Artifact), "Hundra has them");
            Assert.IsTrue(harness.Client.Player.Missions[AncientMysteries].Completeable);
            Assert.IsFalse(Conversation(harness, markin).ContainsKey(ConversationType.ObjectiveComplete), "not to both");

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, hundra, AncientMysteries, null,
                PlateauAncientMysteriesV1.Experience, PlateauAncientMysteriesV1.Credits);
            Assert.AreEqual(PlateauAncientMysteriesV1.MedPacks, WildernessRanjaGorgeTests.Held(harness, PlateauAncientMysteriesV1.MedPack));
        }

        /// <summary>Ancient Mysteries given to the AFS: Markin takes the artifacts; the turn-in is still at Hundra.</summary>
        [TestMethod]
        public void AncientMysteriesCanGiveTheArtifactsToMarkinInstead()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            var (hundra, markin) = HundraAndMarkin(harness, plateau);
            RecoverTheArtifacts(harness, plateau, hundra);

            AssertHandInTopic(Conversation(harness, markin), AncientMysteries, PlateauAncientMysteriesV1.Return);
            harness.MoveTo(markin.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, markin.EntityId, AncientMysteries, PlateauAncientMysteriesV1.Return, 1));
            harness.Drain();
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauAncientMysteriesV1.Artifact), "Markin has them");
            Assert.IsTrue(harness.Client.Player.Missions[AncientMysteries].Completeable);
            Assert.IsFalse(Conversation(harness, hundra).ContainsKey(ConversationType.ObjectiveComplete), "not to both");

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, hundra, AncientMysteries, null,
                PlateauAncientMysteriesV1.Experience, PlateauAncientMysteriesV1.Credits);
        }

        [TestMethod]
        public void MountainSpringToxinsBlowsTheFourTowersThenOverloadsTheMachine()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, PlateauPerduNpcsV1.RussPool);
            var russ = Living(plateau).Single(creature => creature.SpawnPool?.DbId == PlateauPerduNpcsV1.RussPool && creature.Npc != null);
            Assert.IsFalse(Offers(harness, russ, MountainSpringToxins), "it waits for Village Of The Damned");
            SeedCompleted(harness, PlateauVillageOfTheDamnedV1.MissionId);

            var offer = WildernessRanjaGorgeTests.Offer(harness, russ, MountainSpringToxins);
            CollectionAssert.AreEqual(new[]
                {
                    PlateauMountainSpringToxinsV1.GravitonVest, PlateauMountainSpringToxinsV1.StealthVest,
                    PlateauMountainSpringToxinsV1.MechVest, PlateauMountainSpringToxinsV1.BioVest
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, russ, MountainSpringToxins);
            Assert.AreEqual(PlateauMountainSpringToxinsV1.Bombs, WildernessRanjaGorgeTests.Held(harness, PlateauMountainSpringToxinsV1.Bomb), "Russ's explosives");

            Assert.IsFalse(plateau.DynamicObjects.Any(obj => obj.SceneMissionId == MountainSpringToxins && obj.IsInWorld),
                "nothing of the scene on the Plateau, where Russ is");
            var copy = IntoVelonHollow(harness);
            var towers = Enumerable.Range(0, PlateauMountainSpringToxinsV1.Towers.Length)
                .Select(index => SceneObject(harness, copy, MountainSpringToxins, PlateauMountainSpringToxinsV1.TowerRole(index))).ToArray();
            for (var index = 0; index < towers.Length; index++)
            {
                Assert.AreEqual(PlateauMountainSpringToxinsV1.Towers[index].Class, (uint)towers[index].EntityClassId);
                Assert.AreEqual(PlateauMountainSpringToxinsV1.TowerRunningState, (uint)towers[index].StateId);
                AssertStandsOnNavMesh(copy, towers[index].Position, 0.5f);
            }
            Use(harness, copy, towers[0]);
            Assert.AreEqual(PlateauMountainSpringToxinsV1.TowerRunningState, (uint)towers[0].StateId, "no tower before the source is found");
            Assert.AreEqual(PlateauMountainSpringToxinsV1.Bombs, WildernessRanjaGorgeTests.Held(harness, PlateauMountainSpringToxinsV1.Bomb));

            var at = PlateauMountainSpringToxinsV1.PondsAt;
            var inside = new Vector3((float)at.X, (float)at.Y, (float)at.Z);
            harness.MoveTo(inside);
            Assert.IsTrue(new MissionAreaService(() => harness.Manager).RecordAcceptedMovement(harness.Client, inside + new Vector3(-150, 0, 0), inside),
                "the ponds reached");
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, MountainSpringToxins, PlateauMountainSpringToxinsV1.Find));

            var engine = SceneObject(harness, copy, MountainSpringToxins, PlateauMountainSpringToxinsV1.EngineRole);
            var mixer = SceneObject(harness, copy, MountainSpringToxins, PlateauMountainSpringToxinsV1.MixerRole);
            Assert.AreEqual(PlateauMountainSpringToxinsV1.EngineClass, (uint)engine.EntityClassId);
            Assert.AreEqual(PlateauMountainSpringToxinsV1.MixerClass, (uint)mixer.EntityClassId);
            AssertStandsOnNavMesh(copy, engine.Position, 0.5f);
            AssertStandsOnNavMesh(copy, mixer.Position, 0.5f);
            Use(harness, copy, engine);
            Assert.AreEqual(PlateauMountainSpringToxinsV1.EngineRunningState, (uint)engine.StateId, "no overload before the towers");

            for (var index = 0; index < towers.Length; index++)
            {
                var objective = PlateauMountainSpringToxinsV1.Towers[index].Objective;
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, MountainSpringToxins, objective));
                Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, MountainSpringToxins, PlateauMountainSpringToxinsV1.Overload));
                Use(harness, copy, towers[index]);
                Assert.AreEqual(PlateauMountainSpringToxinsV1.TowerDestroyedState, (uint)towers[index].StateId, $"tower {index + 1} blown");
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, MountainSpringToxins, objective));
                Assert.AreEqual(PlateauMountainSpringToxinsV1.Bombs - (uint)index - 1, WildernessRanjaGorgeTests.Held(harness, PlateauMountainSpringToxinsV1.Bomb));
            }
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, MountainSpringToxins, PlateauMountainSpringToxinsV1.Overload));
            Assert.IsFalse(harness.Client.Player.Missions[MountainSpringToxins].Completeable);

            Use(harness, copy, engine);
            Assert.AreEqual(PlateauMountainSpringToxinsV1.EngineDestroyedState, (uint)engine.StateId, "the machine overloaded");
            Assert.AreEqual(PlateauMountainSpringToxinsV1.MixerStoppedState, (uint)mixer.StateId, "the mixer stopped");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, MountainSpringToxins, PlateauMountainSpringToxinsV1.Overload));
            Assert.IsTrue(harness.Client.Player.Missions[MountainSpringToxins].Completeable);

            Return(harness, plateau);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, russ, MountainSpringToxins, 3,
                PlateauMountainSpringToxinsV1.Experience, PlateauMountainSpringToxinsV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauMountainSpringToxinsV1.BioVest), "the fourth choice");
        }

        /// <summary>
        /// A scene bound to its map (MissionSceneDefinition.MapContextId), as a mission given outside an instance and
        /// played inside it has it: it waits on the Plateau, starts in the squad's copy, keeps what was done there when
        /// the player goes out and back in, and goes with the player to a new copy once the old one is closed (the
        /// weekly reset), a blown tower still blown, nothing left of it on the Plateau.
        /// </summary>
        [TestMethod]
        public void MountainSpringToxinsSceneWaitsForVelonHollowAndGoesToTheSquadsNextCopy()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, PlateauPerduNpcsV1.RussPool);
            var russ = Living(plateau).Single(creature => creature.SpawnPool?.DbId == PlateauPerduNpcsV1.RussPool && creature.Npc != null);
            SeedCompleted(harness, PlateauVillageOfTheDamnedV1.MissionId);
            Accept(harness, russ, MountainSpringToxins);
            Assert.IsTrue(harness.Manager.Scenes.TryGetSceneMap(MountainSpringToxins, out var bound));
            Assert.AreEqual(VelonHollow, bound);
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            Assert.IsFalse(plateau.DynamicObjects.Any(obj => obj.SceneMissionId == MountainSpringToxins && obj.IsInWorld), "it waits on the Plateau");
            using (var unit = harness.CreateChar())
                Assert.AreEqual(Rasa.Game.Missions.SceneApplication.AwaitingMapKey(VelonHollow),
                    unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, MountainSpringToxins).Single().MapKey, "a key of no map");

            var copy = IntoVelonHollow(harness);
            FindTheSource(harness);
            var first = SceneObject(harness, copy, MountainSpringToxins, PlateauMountainSpringToxinsV1.TowerRole(0));
            Use(harness, copy, first);
            Assert.AreEqual(PlateauMountainSpringToxinsV1.TowerDestroyedState, (uint)first.StateId);

            Leave(harness, copy, plateau);
            Assert.IsFalse(plateau.DynamicObjects.Any(obj => obj.SceneMissionId == MountainSpringToxins && obj.IsInWorld), "not after it either");
            Assert.AreSame(copy, IntoVelonHollow(harness), "the squad's same copy");
            Assert.AreEqual(PlateauMountainSpringToxinsV1.Towers.Length, Enumerable.Range(0, PlateauMountainSpringToxinsV1.Towers.Length)
                .Count(index => FindSceneObject(harness, copy, MountainSpringToxins, PlateauMountainSpringToxinsV1.TowerRole(index)) != null), "one of each, no more");
            Assert.AreEqual(PlateauMountainSpringToxinsV1.TowerDestroyedState,
                (uint)SceneObject(harness, copy, MountainSpringToxins, PlateauMountainSpringToxinsV1.TowerRole(0)).StateId, "still blown");

            Leave(harness, copy, plateau);
            Close(harness, copy);
            var next = IntoVelonHollow(harness);
            Assert.AreNotSame(copy, next);
            Assert.AreNotEqual(Rasa.Game.Missions.World.PublicActorLeaseService.MapKey(copy), Rasa.Game.Missions.World.PublicActorLeaseService.MapKey(next));
            using (var unit = harness.CreateChar())
                Assert.AreEqual(Rasa.Game.Missions.World.PublicActorLeaseService.MapKey(next),
                    unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, MountainSpringToxins).Single().MapKey, "the run is in the new copy");
            Assert.AreEqual(PlateauMountainSpringToxinsV1.TowerDestroyedState,
                (uint)SceneObject(harness, next, MountainSpringToxins, PlateauMountainSpringToxinsV1.TowerRole(0)).StateId, "blown in the new copy too");
            var second = SceneObject(harness, next, MountainSpringToxins, PlateauMountainSpringToxinsV1.TowerRole(1));
            Assert.AreEqual(PlateauMountainSpringToxinsV1.TowerRunningState, (uint)second.StateId);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, MountainSpringToxins, PlateauMountainSpringToxinsV1.Towers[0].Objective));
            Use(harness, next, second);
            Assert.AreEqual(PlateauMountainSpringToxinsV1.TowerDestroyedState, (uint)second.StateId, "the new copy's tower is used");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, MountainSpringToxins, PlateauMountainSpringToxinsV1.Towers[1].Objective));
            Assert.AreEqual(PlateauMountainSpringToxinsV1.Bombs - 2, WildernessRanjaGorgeTests.Held(harness, PlateauMountainSpringToxinsV1.Bomb));
            Leave(harness, next, plateau);
            Close(harness, next);
        }

        /// <summary>
        /// The Better Half, the first hit decides: Quarsh kills Tala and fights the player with no clock; his death
        /// opens 3, and Clark hears it. A second hit runs nothing more.
        /// </summary>
        [TestMethod]
        public void BetterHalfFirstHitOnQuarshCostsTalasLife()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            var clark = AcceptBetterHalf(harness, plateau);
            var copy = IntoVelonHollow(harness);
            InTheirPlaces(harness, copy);
            var (quarsh, tala) = IntoTheControlRoom(harness, copy);

            Run(harness, copy, PlateauBetterHalfV1.EscapeMs / 2);
            Assert.AreSame(quarsh, SceneCreature(harness, copy, PlateauBetterHalfV1.QuarshRole), "Quarsh still there halfway");
            ShotDead(harness, copy, quarsh, tala);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, BetterHalf, PlateauBetterHalfV1.Decide));
            Hit(harness, copy, quarsh, damaged: true);
            Run(harness, copy, PlateauBetterHalfV1.EscapeMs);
            Assert.AreSame(quarsh, SceneCreature(harness, copy, PlateauBetterHalfV1.QuarshRole), "no clock: he does not run");
            Assert.IsNull(SceneCreature(harness, copy, PlateauBetterHalfV1.QuarshFleesRole));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, BetterHalf, PlateauBetterHalfV1.Alive), "the clock stopped");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, BetterHalf, PlateauBetterHalfV1.Decide));
            using (var unit = harness.CreateChar())
            {
                var runs = unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, BetterHalf).Select(scene => scene.RunId).ToArray();
                Assert.AreEqual(1, runs.SelectMany(run => unit.CharacterMissions.Runtime.Messages(run))
                    .Count(message => message.OperationKey == $"damaged-{PlateauBetterHalfV1.QuarshRole}"), "the first hit only");
            }

            Kill(harness, copy, quarsh);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, BetterHalf, PlateauBetterHalfV1.Decide));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, BetterHalf, PlateauBetterHalfV1.Dead));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, BetterHalf, PlateauBetterHalfV1.Alive));

            TellClark(harness, plateau, clark, PlateauBetterHalfV1.Dead);
        }

        /// <summary>
        /// The Better Half, no hit by the clock: Quarsh never turns on the player, a hit on his way out decides nothing; at the clock he runs into the hallway and is gone, Tala thanks the player, walks out of
        /// the room and is gone; 4, and Clark hears it.
        /// </summary>
        [TestMethod]
        public void BetterHalfClockOutLetsQuarshEscapeAndTalaLive()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            var clark = AcceptBetterHalf(harness, plateau);
            var copy = IntoVelonHollow(harness);
            InTheirPlaces(harness, copy);
            var (quarsh, tala) = IntoTheControlRoom(harness, copy);

            // Near him, the whole standoff: he never turns on the player.
            harness.MoveTo(quarsh.Position + new Vector3(4, 0, 4));
            var health = harness.Client.Player.Attributes[Attributes.Health].Current;
            Run(harness, copy, PlateauBetterHalfV1.EscapeMs - PlateauBetterHalfV1.BegsMs - 1000);
            Assert.AreSame(quarsh, SceneCreature(harness, copy, PlateauBetterHalfV1.QuarshRole), "not before the clock");
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, quarsh.Controller.CurrentAction, "passive in the standoff");
            Assert.AreEqual(health, harness.Client.Player.Attributes[Attributes.Health].Current, "nothing hit the player");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, BetterHalf, PlateauBetterHalfV1.Decide));

            Run(harness, copy, 1500);
            Assert.IsNull(SceneCreature(harness, copy, PlateauBetterHalfV1.QuarshRole), "Quarsh breaks off");
            var flees = SceneCreature(harness, copy, PlateauBetterHalfV1.QuarshFleesRole);
            Assert.IsNotNull(flees, "and runs");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, BetterHalf, PlateauBetterHalfV1.Decide));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, BetterHalf, PlateauBetterHalfV1.Alive));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, BetterHalf, PlateauBetterHalfV1.Dead));
            Hit(harness, copy, flees, damaged: false);   // on his way out: immune, Tala lives
            Run(harness, copy, PlateauBetterHalfV1.QuarshGoneMs - 1000);
            Assert.AreSame(tala, SceneCreature(harness, copy, PlateauBetterHalfV1.TalaRole), "Tala waits");
            Assert.AreEqual(0f, Flat(flees.Position, PlateauBetterHalfV1.QuarshExit), 1.5f, "Quarsh in the hallway");

            var thanks = Run(harness, copy, 1000);
            Assert.IsNull(SceneCreature(harness, copy, PlateauBetterHalfV1.QuarshFleesRole), "and gone");
            Assert.AreEqual(1, thanks.OfType<BarkPackage>().Count(bark => bark.BarkId == PlateauBetterHalfV1.TalaThanksBark), "Tala thanks the player");
            Run(harness, copy, PlateauBetterHalfV1.TalaLeavesMs + PlateauBetterHalfV1.TalaGoneMs - 1000);
            Assert.AreSame(tala, SceneCreature(harness, copy, PlateauBetterHalfV1.TalaRole));
            Assert.AreEqual(0f, Flat(tala.Position, PlateauBetterHalfV1.TalaExit), 1.5f, "Tala walked out of the room");
            Assert.IsFalse(tala.IsRunning, "walked, not ran");
            Run(harness, copy, 1500);
            Assert.IsNull(SceneCreature(harness, copy, PlateauBetterHalfV1.TalaRole), "and gone home");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, BetterHalf, PlateauBetterHalfV1.Alive));

            TellClark(harness, plateau, clark, PlateauBetterHalfV1.Alive);
        }

        /// <summary>
        /// The Better Half, Quarsh struck before the room (from the doorway): the same decision. 2 opens with it, Tala
        /// dies, he fights; the room then starts no standoff and no clock; his death ends 2 and opens 3.
        /// </summary>
        [TestMethod]
        public void BetterHalfFirstHitBeforeTheRoomIsTheSameDecision()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            var clark = AcceptBetterHalf(harness, plateau);
            var copy = IntoVelonHollow(harness);
            var (quarsh, tala) = InTheirPlaces(harness, copy);

            ShotDead(harness, copy, quarsh, tala);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, BetterHalf, PlateauBetterHalfV1.Find), "found her, too late");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, BetterHalf, PlateauBetterHalfV1.Decide), "the decision open");

            var at = PlateauBetterHalfV1.RoomAt;
            var inside = new Vector3((float)at.X, (float)at.Y, (float)at.Z);
            harness.MoveTo(inside);
            new MissionAreaService(() => harness.Manager).RecordAcceptedMovement(harness.Client, inside + new Vector3(-60, 0, 0), inside);
            var room = harness.Drain().Concat(Run(harness, copy, PlateauBetterHalfV1.EscapeMs + 2000)).ToList();
            Assert.IsFalse(room.OfType<BarkPackage>().Any(bark => bark.BarkId is PlateauBetterHalfV1.QuarshThreatBark or PlateauBetterHalfV1.TalaBegsBark),
                "no standoff after the decision");
            Assert.AreSame(quarsh, SceneCreature(harness, copy, PlateauBetterHalfV1.QuarshRole), "no clock: he does not run");
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, BetterHalf, PlateauBetterHalfV1.Alive));

            Kill(harness, copy, quarsh);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, BetterHalf, PlateauBetterHalfV1.Decide));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, BetterHalf, PlateauBetterHalfV1.Dead));
            TellClark(harness, plateau, clark, PlateauBetterHalfV1.Dead);
        }

        /// <summary>
        /// The Better Half, Quarsh killed at one blow before the room (no hit counted first, as a GM's .kill): his death
        /// alone opens 2 and ends it; 3 opens. Tala is left standing (he never shot her): a known edge.
        /// </summary>
        [TestMethod]
        public void BetterHalfQuarshKilledAtOneBlowBeforeTheRoomStillOpensThree()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            AcceptBetterHalf(harness, plateau);
            var copy = IntoVelonHollow(harness);
            var (quarsh, _) = InTheirPlaces(harness, copy);

            Kill(harness, copy, quarsh);
            Run(harness, copy, 250);
            Assert.IsNotNull(SceneCreature(harness, copy, PlateauBetterHalfV1.TalaRole), "dead before he could shoot: Tala is left standing");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, BetterHalf, PlateauBetterHalfV1.Find));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, BetterHalf, PlateauBetterHalfV1.Decide), "Sacrificed not lost");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, BetterHalf, PlateauBetterHalfV1.Dead));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, BetterHalf, PlateauBetterHalfV1.Alive));
        }

        /// <summary>A first-hit sequence names an actor of the scene that can be hit, and a sequence it has.</summary>
        [TestMethod]
        public void AFirstHitSequenceMustNameACreatureAndASequence()
        {
            static void Validate(string role, uint sequence)
            {
                var scene = new Rasa.Missions.Content.MissionSceneDefinition
                {
                    Script = "data.sequence",
                    Actors = new()
                    {
                        ["guard"] = new Rasa.Missions.Scenes.SceneActorDefinition("guard", Rasa.Missions.Scenes.SceneActorKind.Creature, 1, new(0, 0, 0)),
                        ["crate"] = new Rasa.Missions.Scenes.SceneActorDefinition("crate", Rasa.Missions.Scenes.SceneActorKind.Object, 2, new(0, 0, 0))
                    },
                    DamagedSequences = new() { [role] = sequence }
                };
                scene.Sequences[0] = new Rasa.Missions.Content.SceneSequenceDefinition();
                scene.Sequences[5] = new Rasa.Missions.Content.SceneSequenceDefinition();
                Rasa.Game.Missions.Content.MissionSceneValidation.Validate(321, "test", scene, new[] { 1U });
            }
            Validate("guard", 5);
            Assert.ThrowsExactly<Rasa.Missions.Runtime.MissionRuleException>(() => Validate("nobody", 5));
            Assert.ThrowsExactly<Rasa.Missions.Runtime.MissionRuleException>(() => Validate("crate", 5));
            Assert.ThrowsExactly<Rasa.Missions.Runtime.MissionRuleException>(() => Validate("guard", 6));
        }

        /// <summary>A scene actor's own health and armour are a creature's, and its health is more than none.</summary>
        [TestMethod]
        public void ASceneActorsOwnHealthIsACreaturesAndMoreThanNone()
        {
            static void Validate(Rasa.Missions.Scenes.SceneActorDefinition actor)
            {
                var scene = new Rasa.Missions.Content.MissionSceneDefinition
                {
                    Script = "data.sequence", Actors = new() { [actor.Role] = actor }
                };
                scene.Sequences[0] = new Rasa.Missions.Content.SceneSequenceDefinition();
                Rasa.Game.Missions.Content.MissionSceneValidation.Validate(321, "test", scene, new[] { 1U });
            }
            var guard = new Rasa.Missions.Scenes.SceneActorDefinition("guard", Rasa.Missions.Scenes.SceneActorKind.Creature, 1, new(0, 0, 0));
            Validate(guard with { HitPoints = 70, Armor = 0 });
            Assert.ThrowsExactly<Rasa.Missions.Runtime.MissionRuleException>(() => Validate(guard with { HitPoints = 0 }));
            Assert.ThrowsExactly<Rasa.Missions.Runtime.MissionRuleException>(() => Validate(guard with
                { Kind = Rasa.Missions.Scenes.SceneActorKind.Object, HitPoints = 70 }));
        }

        /// <summary>A bark names a bark and a creature to say it; a pose names a pose (NpcPose) and a creature to take it.</summary>
        [TestMethod]
        public void BarksAndPosesNeedACreatureAndAValue()
        {
            static void Validate(params Rasa.Missions.Scenes.WorldIntent[] world)
            {
                var scene = new Rasa.Missions.Content.MissionSceneDefinition
                {
                    Script = "data.sequence",
                    Actors = new()
                    {
                        ["guard"] = new Rasa.Missions.Scenes.SceneActorDefinition("guard", Rasa.Missions.Scenes.SceneActorKind.Creature, 1, new(0, 0, 0)),
                        ["crate"] = new Rasa.Missions.Scenes.SceneActorDefinition("crate", Rasa.Missions.Scenes.SceneActorKind.Object, 2, new(0, 0, 0))
                    }
                };
                scene.Sequences[0] = new Rasa.Missions.Content.SceneSequenceDefinition();
                scene.Sequences[5] = new Rasa.Missions.Content.SceneSequenceDefinition { World = world.ToList() };
                Rasa.Game.Missions.Content.MissionSceneValidation.Validate(321, "test", scene, new[] { 1U });
            }
            Validate(new Rasa.Missions.Scenes.BarkActorIntent("says", "guard", 848),
                new Rasa.Missions.Scenes.PoseActorIntent("works", "guard", 8));
            Assert.ThrowsExactly<Rasa.Missions.Runtime.MissionRuleException>(() => Validate(new Rasa.Missions.Scenes.BarkActorIntent("says", "guard", 0)));
            Assert.ThrowsExactly<Rasa.Missions.Runtime.MissionRuleException>(() => Validate(new Rasa.Missions.Scenes.BarkActorIntent("says", "crate", 848)));
            Assert.ThrowsExactly<Rasa.Missions.Runtime.MissionRuleException>(() => Validate(new Rasa.Missions.Scenes.PoseActorIntent("works", "guard", 99)));
            Assert.ThrowsExactly<Rasa.Missions.Runtime.MissionRuleException>(() => Validate(new Rasa.Missions.Scenes.PoseActorIntent("works", "crate", 8)));
        }

        /// <summary>Clark at the Wedge Rock; 1975 offered after Village Of The Damned, the four helmets, accepted; nothing of it on the Plateau.</summary>
        private static Creature AcceptBetterHalf(WildernessRuntimeTestHarness harness, MapChannel plateau)
        {
            Spawn(plateau, PlateauPerduNpcsV1.Clark);
            var clark = RequireNpc(plateau, PlateauPerduNpcsV1.Clark);
            Assert.IsFalse(Offers(harness, clark, BetterHalf), "it waits for Village Of The Damned");
            SeedCompleted(harness, PlateauVillageOfTheDamnedV1.MissionId);
            var offer = WildernessRanjaGorgeTests.Offer(harness, clark, BetterHalf);
            CollectionAssert.AreEqual(new[]
                {
                    PlateauBetterHalfV1.GravitonHelmet, PlateauBetterHalfV1.StealthHelmet,
                    PlateauBetterHalfV1.MechHelmet, PlateauBetterHalfV1.BioHelmet
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, clark, BetterHalf);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, BetterHalf, PlateauBetterHalfV1.Find));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, BetterHalf, PlateauBetterHalfV1.Decide));
            Assert.IsTrue(harness.Manager.Scenes.TryGetSceneMap(BetterHalf, out var bound));
            Assert.AreEqual(VelonHollow, bound);
            Assert.IsFalse(Here(plateau).Any(creature => creature.DbId is PlateauBetterHalfV1.Quarsh or PlateauBetterHalfV1.Tala),
                "nothing of the scene on the Plateau, where Clark is");
            return clark;
        }

        /// <summary>
        /// Into Velon Hollow, before the room: Tala works at her terminal and Quarsh stands behind her, neutral (the
        /// one Quarsh until the clock), both in the user's places and facings, on the navmesh, and nothing has run yet.
        /// </summary>
        private static (Creature Quarsh, Creature Tala) InTheirPlaces(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            Run(harness, copy, 250);
            var waiting = SceneCreature(harness, copy, PlateauBetterHalfV1.QuarshRole);
            var tala = SceneCreature(harness, copy, PlateauBetterHalfV1.TalaRole);
            Assert.IsNotNull(tala, "Tala is there on arrival");
            Assert.IsNotNull(waiting, "Quarsh is there on arrival");
            Assert.AreEqual(PlateauBetterHalfV1.Tala, tala.DbId);
            Assert.AreEqual(PlateauBetterHalfV1.TalaName, tala.NameId);
            Assert.AreEqual(TargetCategory.Friendly, tala.TargetCategory);
            Assert.AreEqual(NpcPose.AtConsole, tala.Pose, "working at the terminal");
            Assert.IsTrue(tala.PoseShown);
            Assert.AreEqual(PlateauBetterHalfV1.TalaFacing, tala.Rotation, 0.01);
            Assert.AreEqual(0f, Flat(tala.Position, PlateauBetterHalfV1.TalaAt), 0.5f);
            Assert.AreEqual(PlateauBetterHalfV1.Quarsh, waiting.DbId);
            Assert.AreEqual(TargetCategory.Neutral, waiting.TargetCategory, "seeks nobody");
            Assert.AreEqual(NpcPose.Standing, waiting.Pose, "keeps his place and facing");
            Assert.AreEqual(PlateauBetterHalfV1.QuarshFacing, waiting.Rotation, 0.01);
            Assert.AreEqual(0f, Flat(waiting.Position, PlateauBetterHalfV1.QuarshAt), 0.5f);
            AssertStandsOnNavMesh(copy, tala.Position);
            AssertStandsOnNavMesh(copy, waiting.Position);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, BetterHalf, PlateauBetterHalfV1.Find));
            return (waiting, tala);
        }

        /// <summary>
        /// Up into the upper control room, as MissionAreaService takes a step: 1 done, the same Quarsh holds Tala, he
        /// threatens and she begs, the clock on.
        /// </summary>
        private static (Creature Quarsh, Creature Tala) IntoTheControlRoom(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            var before = SceneCreature(harness, copy, PlateauBetterHalfV1.QuarshRole);
            var at = PlateauBetterHalfV1.RoomAt;
            var inside = new Vector3((float)at.X, (float)at.Y, (float)at.Z);
            harness.MoveTo(inside);
            Assert.IsTrue(new MissionAreaService(() => harness.Manager).RecordAcceptedMovement(harness.Client, inside + new Vector3(-60, 0, 0), inside),
                "the control room reached");
            var threat = harness.Drain().Concat(Run(harness, copy, 250)).ToList();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, BetterHalf, PlateauBetterHalfV1.Find));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, BetterHalf, PlateauBetterHalfV1.Decide));

            var quarsh = SceneCreature(harness, copy, PlateauBetterHalfV1.QuarshRole);
            var tala = SceneCreature(harness, copy, PlateauBetterHalfV1.TalaRole);
            Assert.IsNotNull(quarsh, "Quarsh holds her");
            Assert.IsNotNull(tala, "Tala is there");
            Assert.AreSame(before, quarsh, "the same Quarsh as on arrival: no new spawn at the room");
            Assert.AreEqual(PlateauBetterHalfV1.Quarsh, quarsh.DbId);
            Assert.AreEqual(PlateauBetterHalfV1.QuarshName, quarsh.NameId);
            Assert.AreEqual(PlateauBetterHalfV1.Level, quarsh.Level);
            Assert.AreEqual(TargetCategory.Neutral, quarsh.TargetCategory);
            Assert.AreEqual((int)PlateauBetterHalfV1.QuarshHitPoints, (int)quarsh.Attributes[Attributes.Health].Current);
            Assert.AreEqual(0f, Flat(quarsh.Position, PlateauBetterHalfV1.QuarshAt), 0.1f, "where he waited");
            Assert.AreEqual(PlateauBetterHalfV1.QuarshFacing, quarsh.Rotation, 0.01);
            Assert.AreEqual(NpcPose.None, tala.Pose, "no longer working");
            Assert.AreEqual(1, threat.OfType<BarkPackage>().Count(bark => bark.BarkId == PlateauBetterHalfV1.QuarshThreatBark), "Quarsh threatens");
            Assert.IsFalse(threat.OfType<BarkPackage>().Any(bark => bark.BarkId == PlateauBetterHalfV1.TalaBegsBark), "Tala after him");
            var begs = Run(harness, copy, PlateauBetterHalfV1.BegsMs);
            Assert.AreEqual(1, begs.OfType<BarkPackage>().Count(bark => bark.BarkId == PlateauBetterHalfV1.TalaBegsBark), "Tala begs");
            return (quarsh, tala);
        }

        /// <summary>
        /// The first hit on Quarsh: he shoots Tala, one blow kills her (her body stays where she fell), nobody earns
        /// her kill, and he turns on the player.
        /// </summary>
        private static void ShotDead(WildernessRuntimeTestHarness harness, MapChannel copy, Creature quarsh, Creature tala)
        {
            var experience = harness.Client.Player.Experience;
            Assert.AreEqual((int)PlateauBetterHalfV1.TalaHitPoints, (int)tala.Attributes[Attributes.Health].Current);
            Hit(harness, copy, quarsh, damaged: true);
            Run(harness, copy, 250);
            Assert.AreEqual(tala.EntityId, quarsh.Controller.ActionFighting.TargetEntityId, "he turns on Tala");
            for (var waited = 0; tala.State != CharacterState.Dead && waited < 20; waited++)
                Run(harness, copy, 250);
            Assert.AreEqual(CharacterState.Dead, tala.State, $"one of his blows kills her: health {tala.Attributes[Attributes.Health].Current}, armor {tala.Attributes[Attributes.Armor].Current}, his action {quarsh.Controller.CurrentAction}, target {quarsh.Controller.ActionFighting.TargetEntityId} (tala {tala.EntityId}), distance {Vector3.Distance(quarsh.Position, tala.Position)}");
            Assert.IsNull(SceneCreature(harness, copy, PlateauBetterHalfV1.TalaRole));
            Assert.IsTrue(MapInstanceScope.Contains(copy, tala), "her body stays");
            Assert.AreEqual(0UL, tala.CorpseLootEntityId, "no loot");
            Assert.AreEqual(experience, harness.Client.Player.Experience, "no kill of hers for the player");
            Run(harness, copy, 500);
            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, quarsh.Controller.CurrentAction, "then he fights");
            Assert.AreEqual(harness.Client.Player.EntityId, quarsh.Controller.ActionFighting.TargetEntityId, "the player");
        }

        /// <summary>A hit by the player, as weapon fire or an ability lands one (ActorManager.Damage); immune or not as said.</summary>
        private static void Hit(WildernessRuntimeTestHarness harness, MapChannel copy, Creature creature, bool damaged)
        {
            var before = creature.Attributes[Attributes.Health].Current + creature.Attributes[Attributes.Armor].Current;
            ActorManager.Instance.Damage(copy, creature, 10, harness.Client.Player);
            var after = creature.Attributes[Attributes.Health].Current + creature.Attributes[Attributes.Armor].Current;
            if (damaged)
                Assert.IsTrue(after < before, "the hit took something off him");
            else
                Assert.AreEqual(before, after, "immune");
        }

        /// <summary>The map run on in steps (BehaviorManager moves at most a second a think), blows landed, and what the player was sent.</summary>
        private static List<Rasa.Packets.PythonPacket> Run(WildernessRuntimeTestHarness harness, MapChannel map, long milliseconds)
        {
            var sent = new List<Rasa.Packets.PythonPacket>();
            for (var done = 0L; done < milliseconds; done += 250)
            {
                var step = System.Math.Min(250, milliseconds - done);
                harness.UtcNow = harness.UtcNow.AddMilliseconds(step);
                BehaviorManager.Instance.MapChannelThink(map, step);
                MissileManager.Instance.DoWork(map, step);   // the blows land, as the map's tick lands them
                harness.Manager.Scenes.Tick(map);
                sent.AddRange(harness.Drain());
            }
            return sent;
        }

        private static float Flat(Vector3 position, Rasa.Missions.Scenes.ScenePosition at) =>
            Vector2.Distance(new Vector2(position.X, position.Z), new Vector2(at.X, at.Z));

        /// <summary>Back at the Wedge Rock: Clark hears <paramref name="end"/> (only that one), the mission turned in for the fourth helmet.</summary>
        private static void TellClark(WildernessRuntimeTestHarness harness, MapChannel plateau, Creature clark, uint end)
        {
            Assert.IsFalse(harness.Client.Player.Missions[BetterHalf].Completeable);
            Return(harness, plateau);
            AssertHandInTopic(Conversation(harness, clark), BetterHalf, end);
            harness.MoveTo(clark.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, clark.EntityId, BetterHalf, end, 1));
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, BetterHalf, end));
            Assert.IsTrue(harness.Client.Player.Missions[BetterHalf].Completeable);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, clark, BetterHalf, 3,
                PlateauBetterHalfV1.Experience, PlateauBetterHalfV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauBetterHalfV1.BioHelmet), "the fourth choice");
        }

        /// <summary>The scene creature of this test's own run (a squad copy can still hold an earlier test's, see Here).</summary>
        private static Creature SceneCreature(WildernessRuntimeTestHarness harness, MapChannel map, string role)
        {
            using var unit = harness.CreateChar();
            var runs = unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, BetterHalf).Select(scene => scene.RunId).ToHashSet();
            return Here(map).SingleOrDefault(creature => creature.SpawnPool?.SceneActorRole == role && runs.Contains(creature.SpawnPool.SceneRunId));
        }

        /// <summary>A scene map is a mission's own and names a map: zero, or one on an experience, is refused.</summary>
        [TestMethod]
        public void ASceneMapMustBeAMissionsAndNotZero()
        {
            static void Validate(uint mission, uint? map) => Rasa.Game.Missions.Content.MissionSceneValidation.Validate(mission, "test",
                new Rasa.Missions.Content.MissionSceneDefinition { Script = "data.sequence", MapContextId = map }, new[] { 1U });
            Validate(321, null);
            Validate(321, VelonHollow);
            Assert.ThrowsExactly<Rasa.Missions.Runtime.MissionRuleException>(() => Validate(321, 0));
            Assert.ThrowsExactly<Rasa.Missions.Runtime.MissionRuleException>(() => Validate(0, VelonHollow));
        }

        /// <summary>Up to the ponds before the outpost, as MissionAreaService takes a step: 1979 objective 1.</summary>
        private static void FindTheSource(WildernessRuntimeTestHarness harness)
        {
            var at = PlateauMountainSpringToxinsV1.PondsAt;
            var inside = new Vector3((float)at.X, (float)at.Y, (float)at.Z);
            harness.MoveTo(inside);
            Assert.IsTrue(new MissionAreaService(() => harness.Manager).RecordAcceptedMovement(harness.Client, inside + new Vector3(-150, 0, 0), inside),
                "the ponds reached");
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, MountainSpringToxins, PlateauMountainSpringToxinsV1.Find));
        }

        /// <summary>Hundra and Markin at New Velon; 1978 offered after Village Of The Damned and accepted at Hundra.</summary>
        private static (Creature Hundra, Creature Markin) HundraAndMarkin(WildernessRuntimeTestHarness harness, MapChannel plateau)
        {
            Spawn(plateau, PlateauVelonHollowBaneV1.Hundra, PlateauVelonHollowBaneV1.MarkinPool);
            var hundra = RequireNpc(plateau, PlateauVelonHollowBaneV1.Hundra);
            var markin = RequireNpc(plateau, PlateauVelonHollowBaneV1.MarkinPool);
            Assert.AreEqual(PlateauVelonHollowBaneV1.HundraPackage, hundra.Npc.NpcPackageId);
            Assert.AreEqual(PlateauVelonHollowBaneV1.MarkinPackage, markin.Npc.NpcPackageId);
            Assert.AreEqual(PlateauVelonHollowBaneV1.MarkinName, markin.NameId);
            AssertStandsOnNavMesh(plateau, markin.Position);
            Assert.IsTrue(Vector3.Distance(hundra.Position, markin.Position) < 10f, "Markin by Hundra");

            Assert.IsFalse(Offers(harness, hundra, AncientMysteries), "it waits for Village Of The Damned");
            SeedCompleted(harness, PlateauVillageOfTheDamnedV1.MissionId);
            var offer = WildernessRanjaGorgeTests.Offer(harness, hundra, AncientMysteries);
            Assert.AreEqual(0, offer.SelectableReward.Count);
            CollectionAssert.AreEquivalent(new[] { (PlateauAncientMysteriesV1.MedPack, PlateauAncientMysteriesV1.MedPacks) },
                offer.FixedReward.FixedItems.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            Accept(harness, hundra, AncientMysteries);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, AncientMysteries, PlateauAncientMysteriesV1.Explore));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, AncientMysteries, PlateauAncientMysteriesV1.Recover));
            return (hundra, markin);
        }

        /// <summary>
        /// Into Velon Hollow (objective 2 on arrival), two officers, the Mox and two of the corrupted leaders
        /// killed, two artifacts on each corpse taken: ten, objective 1 done; then back to New Velon.
        /// </summary>
        private static void RecoverTheArtifacts(WildernessRuntimeTestHarness harness, MapChannel plateau, Creature hundra)
        {
            var copy = IntoVelonHollow(harness);
            Assert.IsTrue(harness.Client.MissionAreaService.RecordArrival(harness.Client), "arrived, as ManifestationManager.MissionArrival tells it");
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, AncientMysteries, PlateauAncientMysteriesV1.Explore), "Velon Hollow reached");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, AncientMysteries, PlateauAncientMysteriesV1.Recover));

            var pools = new[]
            {
                PlateauVelonHollowBaneV1.ControlRoomPool, PlateauVelonHollowBaneV1.TunnelPool, PlateauVelonHollowBaneV1.MoxPool,
                PlateauVelonElderBossesV1.Minash.Pool, PlateauVelonElderBossesV1.Kadali.Pool
            };
            // The squad's copy can be an earlier test's, its bosses killed there. A squad copy's pool comes back
            // only once all of it is dead, an hour after (SquadInstanceState.RespawnDue; no top-up): the guards
            // left of a boss's pool die, and the pools are back as after that hour.
            foreach (var pool in copy.SpawnPools.Where(pool => pools.Contains(pool.DbId)))
            {
                var left = Here(copy).Where(creature => creature.SpawnPool == pool).ToArray();
                if (left.Length > 0 && !left.Any(creature => PlateauAncientMysteriesV1.Bosses.Contains(creature.DbId)))
                    foreach (var straggler in left)
                        Kill(harness, copy, straggler);
                pool.ClearedAtUtcMs = 0;
            }
            Spawn(copy, pools);
            // One boss of each pool: a pool can stand a second one an earlier test's copy left (see Here).
            var bosses = pools.Select(pool => Here(copy).First(creature => creature.SpawnPool?.DbId == pool && PlateauAncientMysteriesV1.Bosses.Contains(creature.DbId)))
                .ToArray();

            var guard = Here(copy).First(creature => creature.DbId == PlateauVelonHollowBaneV1.Soldier);
            var none = Kill(harness, copy, guard);
            Assert.IsFalse(none != null && none.LootItems.Any(item => item.ItemTemplateId == PlateauAncientMysteriesV1.Artifact), "a guard carries none");

            for (var index = 0; index < bosses.Length; index++)
            {
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, AncientMysteries, PlateauAncientMysteriesV1.Recover), $"before boss {index + 1}");
                var corpse = Kill(harness, copy, bosses[index]);
                Assert.IsNotNull(corpse, $"boss {index + 1} leaves a corpse");
                Assert.AreEqual(PlateauAncientMysteriesV1.PerKill, (uint)corpse.LootItems.Where(item => item.ItemTemplateId == PlateauAncientMysteriesV1.Artifact)
                    .Sum(item => (long)item.ItemQuantity), $"two artifacts on boss {index + 1}");
                PickUp(harness, corpse);
                harness.Drain();
                Assert.AreEqual(PlateauAncientMysteriesV1.PerKill * ((uint)index + 1), WildernessRanjaGorgeTests.Held(harness, PlateauAncientMysteriesV1.Artifact));
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, AncientMysteries, PlateauAncientMysteriesV1.Recover));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, AncientMysteries, PlateauAncientMysteriesV1.Return));
            Assert.IsFalse(harness.Client.Player.Missions[AncientMysteries].Completeable);
            Return(harness, plateau);
        }

        /// <summary>Velon Hollow as the server loads it, then the player through the Plateau's door into the squad's copy.</summary>
        private static MapChannel IntoVelonHollow(WildernessRuntimeTestHarness harness)
        {
            if (!harness.Maps.MapChannelArray.ContainsKey(VelonHollow))
                LoadMap(harness, VelonHollow);
            var copy = ThroughTheDoor(harness, VelonHollow);
            Assert.IsTrue(copy.IsSquadInstance);
            harness.Manager.Scenes.Resume(harness.Client);   // as MapChannelManager.MapLoaded does (ResumeMissionScenes)
            harness.Drain();
            return copy;
        }

        /// <summary>Out of the copy to the Plateau, as MapChannelManager takes a player off a map (DetachMissionScenes).</summary>
        private static void Leave(WildernessRuntimeTestHarness harness, MapChannel copy, MapChannel plateau)
        {
            harness.Manager.Scenes.Detach(harness.Client, copy);
            Return(harness, plateau);
        }

        /// <summary>The copy closed once its hold is past, as the weekly reset closes an empty one (ResetSquadInstances).</summary>
        private static void Close(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            copy.HeldUntil = 0;   // its hold past: the harness's maps keep the wall clock (Environment.TickCount64)
            // An earlier test's player can still be listed in the squad's copy (its harness's Dispose does not take
            // them off it, see Here): nobody is in it but this test's player, who has left.
            copy.ClientList.RemoveAll(client => !ReferenceEquals(client, harness.Client));
            Assert.IsTrue(harness.Maps.CloseSquadInstance(copy), $"copy {copy.InstanceId} closed");
        }

        /// <summary>The living creatures of this copy only, as PlateauUstorYardTests.Here.</summary>
        private static IEnumerable<Creature> Here(MapChannel map) =>
            Living(map).Where(creature => ReferenceEquals(creature.RuntimeMapChannel, map));

        private static bool Offers(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.MissionDispense, out var offered) &&
            ((Dictionary<uint, MissionInfo>)offered).ContainsKey(mission);

        private static void Accept(WildernessRuntimeTestHarness harness, Creature npc, uint mission)
        {
            harness.MoveTo(npc.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, npc.EntityId, mission));
            harness.Drain();
        }

        private static MissionObjectiveState Objective(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].State;

        private static DynamicObject SceneObject(WildernessRuntimeTestHarness harness, MapChannel map, uint mission, string role)
        {
            var obj = FindSceneObject(harness, map, mission, role);
            Assert.IsNotNull(obj, $"{role} is there");
            return obj;
        }

        /// <summary>The object of this test's own run: a squad copy can still hold an earlier test's (see Here).</summary>
        private static DynamicObject FindSceneObject(WildernessRuntimeTestHarness harness, MapChannel map, uint mission, string role)
        {
            using var unit = harness.CreateChar();
            var runs = unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, mission).Select(scene => scene.RunId).ToHashSet();
            return map.DynamicObjects.SingleOrDefault(candidate => candidate.SceneMissionId == mission && candidate.SceneActorRole == role &&
                candidate.IsInWorld && runs.Contains(candidate.SceneRunId));
        }

        /// <summary>Kills the creature as the player and runs the map on; returns its corpse, if it has one.</summary>
        private static LootDispenser Kill(WildernessRuntimeTestHarness harness, MapChannel map, Creature creature)
        {
            harness.MoveTo(creature.Position);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(map, creature, harness.Client.Player);
            Tick(harness, map, 250);
            return map.LootDispensers.TryGetValue(creature.CorpseLootEntityId, out var loot) ? loot : null;
        }

        private static void AssertStandsOnNavMesh(MapChannel map, Vector3 position, float tolerance = 1f)
        {
            var ground = map.NavMesh.GroundHeight(position);
            Assert.IsNotNull(ground, $"navmesh under {position}");
            Assert.AreEqual(ground.Value, position.Y, tolerance, $"{position} on the ground");
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
        private static void Use(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            harness.Drain();
            var count = map.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = PlateauMountainSpringToxinsV1.UseArgument
            });
            if (map.PerformRecovery.Count > count)
            {
                var recovery = map.PerformRecovery.Last();
                map.PerformRecovery.Remove(recovery);
                ActorActionManager.Instance.PerformRecovery(map, recovery);
            }
            harness.Drain();
        }
    }
}
