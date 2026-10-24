extern alias RasaGame;

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
    /// Field Lieutenant Rapp's three missions in Ustor Yard, played through as a player plays them: Silent But
    /// Deadly (1187: the generator, the virus, the station patrol, the Bane computer), Interception! (1189: the
    /// patrol on the pad, the second patrol on its timer, the tanks and the pumps) and Monkey Wrench (1188: the
    /// officer's key, the key slot, the keyboard, the radio turn-in).
    ///
    /// Why these tests: the yard is a squad instance (1502) the server had no door to, and everything the
    /// missions use is the squad copy's own - Rapp, the officer's pool and the scene's objects and Bane. A door
    /// that did not lead in, a scene step that came before its objective, a second patrol that did not land, or
    /// a key that did not drop in the copy would each break the chain; so the door is the real link, the
    /// objects are used and shot as Final Assault's (PlateauTemporalChamberTests), the Bane killed as Looking For
    /// Clues' Pendrik (PlateauCidTests), and the timer run on the harness clock.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauUstorYardTests
    {
        private const uint Ustor = PlateauUstorYardV1.MapContextId;
        private const uint SilentButDeadly = PlateauSilentButDeadlyV1.MissionId;
        private const uint Interception = PlateauInterceptionV1.MissionId;
        private const uint MonkeyWrench = PlateauMonkeyWrenchV1.MissionId;
        private const uint OutOfThinAir = PlateauOutOfThinAirV1.MissionId;

        [TestMethod]
        public void TheUstorDoorLeadsToRappAndBackToThePlateau()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            var links = harness.World.MapLinkEntries.AsNoTracking()
                .Where(row => row.Id == PlateauUstorYardV1.IntoUstor || row.Id == PlateauUstorYardV1.OutOfUstor).ToArray();
            var into = links.Single(row => row.Id == PlateauUstorYardV1.IntoUstor);
            var back = links.Single(row => row.Id == PlateauUstorYardV1.OutOfUstor);
            Assert.AreEqual((PlateauMissionDataV1.MapContextId, Ustor), (into.MapContextId, into.DestMapContextId));
            Assert.AreEqual((Ustor, PlateauMissionDataV1.MapContextId), (back.MapContextId, back.DestMapContextId));
            var plateauArrival = new Vector3((float)back.DestPosX, (float)back.DestPosY, (float)back.DestPosZ);
            AssertStandsOnNavMesh(plateau, plateauArrival);
            Assert.IsTrue(Vector2.Distance(new(plateauArrival.X, plateauArrival.Z), new((float)into.PosX, (float)into.PosZ)) > into.Radius,
                "set down outside the Plateau's door");

            var copy = IntoUstor(harness);
            var arrival = harness.Client.Player.Position;
            AssertStandsOnNavMesh(copy, arrival);
            Assert.IsTrue(Vector2.Distance(new(arrival.X, arrival.Z), new((float)back.PosX, (float)back.PosZ)) > back.Radius,
                "set down outside Ustor's door");
            Assert.IsTrue(copy.NavMesh.FindPath(arrival, new Vector3((float)back.PosX, (float)back.PosY, (float)back.PosZ), out var toDoor) != null && toDoor,
                "the door is walked to");

            var rapp = Rapp(copy);
            Assert.AreEqual(PlateauUstorYardV1.RappPackage, rapp.Npc.NpcPackageId);
            Assert.AreEqual(PlateauUstorYardV1.RappName, rapp.NameId);
            AssertStandsOnNavMesh(copy, rapp.Position);
            Assert.IsTrue(Vector3.Distance(arrival, rapp.Position) < 50f, "Rapp by the east entrance");
            Assert.IsTrue(copy.NavMesh.FindPath(arrival, rapp.Position, out var toRapp) != null && toRapp, "Rapp is walked to");
        }

        [TestMethod]
        public void SilentButDeadlyTakesTheGeneratorTheVirusThePatrolAndTheComputer()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            var copy = IntoUstor(harness);
            var rapp = Rapp(copy);

            var offer = WildernessRanjaGorgeTests.Offer(harness, rapp, SilentButDeadly);
            CollectionAssert.AreEqual(new[]
                {
                    PlateauSilentButDeadlyV1.ElectricInjectorGun, PlateauSilentButDeadlyV1.PulsePistol,
                    PlateauSilentButDeadlyV1.IncendiaryNetGun, PlateauSilentButDeadlyV1.SonicCannon
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(copy.DynamicObjects.Any(obj => obj.SceneMissionId == SilentButDeadly), "nothing before the mission");
            Accept(harness, rapp, SilentButDeadly);

            var generator = SceneObject(copy, SilentButDeadly, PlateauSilentButDeadlyV1.GeneratorRole);
            Assert.AreEqual(PlateauSilentButDeadlyV1.GeneratorClass, (uint)generator.EntityClassId);
            Assert.AreEqual(PlateauSilentButDeadlyV1.IntactState, (uint)generator.StateId);
            AssertStandsOnNavMesh(copy, generator.Position, 0.5f);
            Assert.IsNull(FindSceneObject(copy, SilentButDeadly, PlateauSilentButDeadlyV1.ConsoleRole), "no console before the generator");
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, SilentButDeadly, PlateauSilentButDeadlyV1.Upload));

            Damage(harness, copy, generator, (int)PlateauSilentButDeadlyV1.GeneratorHitPoints);
            Assert.AreEqual(PlateauSilentButDeadlyV1.DestroyedState, (uint)generator.StateId, "the generator blown");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, SilentButDeadly, PlateauSilentButDeadlyV1.Generator));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, SilentButDeadly, PlateauSilentButDeadlyV1.Upload));

            var console = SceneObject(copy, SilentButDeadly, PlateauSilentButDeadlyV1.ConsoleRole);
            Assert.AreEqual(PlateauSilentButDeadlyV1.ConsoleClass, (uint)console.EntityClassId);
            AssertStandsOnNavMesh(copy, console.Position, 0.5f);
            for (var index = 0; index < PlateauSilentButDeadlyV1.PatrolAt.Length; index++)
                Assert.IsNull(SceneCreature(copy, PlateauSilentButDeadlyV1.PatrolRole(index)), "no patrol before the upload");

            Use(harness, copy, console);
            Assert.AreEqual(PlateauSilentButDeadlyV1.ConsoleSetState, (uint)console.StateId, "the virus uploaded");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, SilentButDeadly, PlateauSilentButDeadlyV1.Upload));
            Tick(harness, copy, 250);
            var patrol = Enumerable.Range(0, PlateauSilentButDeadlyV1.PatrolAt.Length)
                .Select(index => SceneCreature(copy, PlateauSilentButDeadlyV1.PatrolRole(index))).ToArray();
            Assert.IsNull(FindSceneObject(copy, SilentButDeadly, PlateauSilentButDeadlyV1.ComputerRole), "no computer while the patrol lives");
            for (var index = 0; index < patrol.Length; index++)
            {
                Assert.IsNotNull(patrol[index], $"patrol {index + 1} comes");
                Assert.AreEqual(PlateauUstorYardV1.Security, patrol[index].DbId);
                Assert.AreEqual(TargetCategory.Hostile, patrol[index].TargetCategory);
                AssertStandsOnNavMesh(copy, patrol[index].Position);
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, SilentButDeadly, PlateauSilentButDeadlyV1.Patrol), $"before kill {index + 1}");
                Kill(harness, copy, patrol[index]);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, SilentButDeadly, PlateauSilentButDeadlyV1.Patrol));
            Tick(harness, copy, 250);

            var computer = SceneObject(copy, SilentButDeadly, PlateauSilentButDeadlyV1.ComputerRole);
            Assert.AreEqual(PlateauSilentButDeadlyV1.ComputerClass, (uint)computer.EntityClassId);
            AssertStandsOnNavMesh(copy, computer.Position, 0.5f);
            Assert.IsFalse(harness.Client.Player.Missions[SilentButDeadly].Completeable);
            Use(harness, copy, computer);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, SilentButDeadly, PlateauSilentButDeadlyV1.Computer));
            Assert.IsTrue(harness.Client.Player.Missions[SilentButDeadly].Completeable);

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, rapp, SilentButDeadly, 3,
                PlateauSilentButDeadlyV1.Experience, PlateauSilentButDeadlyV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauSilentButDeadlyV1.SonicCannon), "the fourth choice");
        }

        /// <summary>
        /// Interception! with the first patrol killed in time: the second patrol still lands when the timer
        /// ends, the tanks and pumps are open once the first is down.
        /// </summary>
        [TestMethod]
        public void InterceptionTakesThePatrolThenTheTanksAndPumpsAndTheSecondPatrolLandsAnyway()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            var (copy, rapp) = AcceptInterception(harness);

            var (first, entered) = IntoChemicalStorage(harness, copy);
            Assert.IsNull(FindSceneObject(copy, Interception, PlateauInterceptionV1.TankRole(0)), "no tank while the patrol lives");
            foreach (var bot in first)
                Kill(harness, copy, bot);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Interception, PlateauInterceptionV1.TakeOut));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Interception, PlateauInterceptionV1.Tanks));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Interception, PlateauInterceptionV1.Pumps));
            Tick(harness, copy, 250);
            Assert.IsTrue(PlateauInterceptionV1.SecondRoles.All(role => SceneCreature(copy, role) == null), "the second patrol is not here yet");

            SecondPatrolLands(harness, copy, entered);

            for (var index = 0; index < PlateauInterceptionV1.TanksAt.Length; index++)
            {
                var tank = SceneObject(copy, Interception, PlateauInterceptionV1.TankRole(index));
                Assert.AreEqual(PlateauInterceptionV1.TankClass, (uint)tank.EntityClassId);
                AssertStandsOnNavMesh(copy, tank.Position, 0.5f);
                Use(harness, copy, tank);
                Assert.AreEqual(PlateauInterceptionV1.TankUsedState, (uint)tank.StateId, $"tank {index + 1} gathered");
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Interception, PlateauInterceptionV1.Tanks));
            Assert.IsFalse(harness.Client.Player.Missions[Interception].Completeable, "the pumps are still to go");

            for (var index = 0; index < PlateauInterceptionV1.PumpsAt.Length; index++)
            {
                var pump = SceneObject(copy, Interception, PlateauInterceptionV1.PumpRole(index));
                Assert.AreEqual(PlateauInterceptionV1.PumpClass, (uint)pump.EntityClassId);
                AssertStandsOnNavMesh(copy, pump.Position, 0.5f);
                Damage(harness, copy, pump, (int)PlateauInterceptionV1.PumpHitPoints);
                Assert.AreEqual(PlateauInterceptionV1.DestroyedState, (uint)pump.StateId, $"pump {index + 1} blown");
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Interception, PlateauInterceptionV1.Pumps));
            Assert.IsTrue(harness.Client.Player.Missions[Interception].Completeable);

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, rapp, Interception, 2,
                PlateauInterceptionV1.Experience, PlateauInterceptionV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauInterceptionV1.EmpBomb), "the third choice");
        }

        /// <summary>Late: the second patrol lands on the first, and objective 2 still completes on the first's last kill.</summary>
        [TestMethod]
        public void InterceptionLateBringsTheSecondPatrolOnTheFirstWithNoFail()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            var (copy, _) = AcceptInterception(harness);

            var (first, entered) = IntoChemicalStorage(harness, copy);
            SecondPatrolLands(harness, copy, entered);
            Assert.IsTrue(first.All(bot => CreatureManager.IsLivingOnMap(copy, bot)), "the first patrol is still up");
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[Interception].State, "no fail");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Interception, PlateauInterceptionV1.TakeOut));

            foreach (var bot in first)
                Kill(harness, copy, bot);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Interception, PlateauInterceptionV1.TakeOut));
            Tick(harness, copy, 250);
            Assert.IsNotNull(FindSceneObject(copy, Interception, PlateauInterceptionV1.TankRole(0)), "the tanks are open");
        }

        [TestMethod]
        public void MonkeyWrenchTakesTheOfficersKeyToTheSlotThenTheKeyboardAndEndsByRadio()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            SeedCompleted(harness, SilentButDeadly, Interception);
            var copy = IntoUstor(harness);
            var rapp = Rapp(copy);

            var offer = WildernessRanjaGorgeTests.Offer(harness, rapp, MonkeyWrench);
            CollectionAssert.AreEqual(new[]
                {
                    PlateauMonkeyWrenchV1.StealArmor, PlateauMonkeyWrenchV1.RegenBonus,
                    PlateauMonkeyWrenchV1.DebuffElectric, PlateauMonkeyWrenchV1.ResistElectric
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, rapp, MonkeyWrench);
            Assert.IsTrue(harness.Manager.LoadedMissions[MonkeyWrench].RadioCompletable == true);

            Spawn(copy, PlateauUstorYardV1.OfficerPool);
            var officer = Here(copy).Single(creature => creature.DbId == PlateauUstorYardV1.Officer);
            Assert.IsNull(officer.Npc, "the officer is no NPC");
            Assert.AreEqual(PlateauUstorYardV1.SecurityLevel, officer.Level);
            Assert.AreEqual(PlateauUstorYardV1.OfficerName, officer.NameId);
            Assert.AreEqual((int)PlateauUstorYardV1.OfficerHitPoints, (int)officer.Attributes[Attributes.Health].Current);
            AssertStandsOnNavMesh(copy, officer.Position);
            Assert.AreEqual((int)PlateauUstorYardV1.Guards, Here(copy).Count(creature => creature.DbId == PlateauUstorYardV1.Security), "his guards");

            var slot = SceneObject(copy, MonkeyWrench, PlateauMonkeyWrenchV1.SlotRole);
            var keyboard = SceneObject(copy, MonkeyWrench, PlateauMonkeyWrenchV1.KeyboardRole);
            Assert.AreEqual(PlateauMonkeyWrenchV1.SlotClass, (uint)slot.EntityClassId);
            Assert.AreEqual(PlateauMonkeyWrenchV1.KeyboardClass, (uint)keyboard.EntityClassId);
            AssertStandsOnNavMesh(copy, slot.Position, 0.5f);
            AssertStandsOnNavMesh(copy, keyboard.Position, 0.5f);
            Use(harness, copy, slot);
            Use(harness, copy, keyboard);
            Assert.AreEqual(PlateauMonkeyWrenchV1.SlotReadyState, (uint)slot.StateId, "the slot waits for the key");
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, MonkeyWrench, PlateauMonkeyWrenchV1.Keyboard));

            var corpse = Kill(harness, copy, officer);
            Assert.IsTrue(corpse.LootItems.Any(item => item.ItemTemplateId == PlateauMonkeyWrenchV1.Key), "the key on the officer");
            PickUp(harness, corpse);
            harness.Drain();
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauMonkeyWrenchV1.Key));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, MonkeyWrench, PlateauMonkeyWrenchV1.Access));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, MonkeyWrench, PlateauMonkeyWrenchV1.EnterKey));

            Use(harness, copy, slot);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauMonkeyWrenchV1.Key), "the key in the slot");
            Assert.AreEqual(PlateauMonkeyWrenchV1.SlotSetState, (uint)slot.StateId);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, MonkeyWrench, PlateauMonkeyWrenchV1.EnterKey));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, MonkeyWrench, PlateauMonkeyWrenchV1.Keyboard));

            Use(harness, copy, keyboard);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, MonkeyWrench, PlateauMonkeyWrenchV1.Keyboard));
            Assert.IsTrue(harness.Client.Player.Missions[MonkeyWrench].Completeable);
            Assert.IsFalse(Conversation(harness, rapp).ContainsKey(ConversationType.MissionComplete), "no NPC turn-in");
            harness.Drain();

            var before = harness.Context.ReadRewardTotals();
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, MonkeyWrench, 1, null));
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[MonkeyWrench].State);
            Assert.AreEqual(before.Credits + PlateauMonkeyWrenchV1.Credits, harness.Context.ReadRewardTotals().Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauMonkeyWrenchV1.RegenBonus), "the second choice");
        }

        [TestMethod]
        public void OutOfThinAirTakesThePatrolsStimPackToThePrisonerWhoWalksOutByTheWestExit()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            SeedCompleted(harness, SilentButDeadly, Interception);
            var copy = IntoUstor(harness);
            var prisoner = Prisoner(harness, copy);
            Assert.AreEqual(PlateauMaligoPrisonerV1.PrisonerPackage, prisoner.Npc.NpcPackageId);
            Assert.AreEqual(PlateauMaligoPrisonerV1.PrisonerName, prisoner.NameId);
            AssertStandsOnNavMesh(copy, prisoner.Position);
            Assert.IsFalse(Offers(harness, prisoner, OutOfThinAir), "waits for Monkey Wrench");
            SeedCompleted(harness, MonkeyWrench);

            var offer = WildernessRanjaGorgeTests.Offer(harness, prisoner, OutOfThinAir);
            CollectionAssert.AreEqual(new[]
                {
                    PlateauOutOfThinAirV1.BioVest, PlateauOutOfThinAirV1.GravitonLegs,
                    PlateauOutOfThinAirV1.ReflectiveVest, PlateauOutOfThinAirV1.MechGloves
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, prisoner, OutOfThinAir);
            Tick(harness, copy, 250);

            // 2: the patrol south-east of the middle pond; one of them has the stim pack.
            var pond = new Vector3(204, 174.3f, -36);
            var patrol = Enumerable.Range(0, PlateauOutOfThinAirScene.PatrolSize)
                .Select(index => SceneCreature(copy, PlateauOutOfThinAirScene.PatrolRole(index))).ToArray();
            foreach (var thrax in patrol)
            {
                Assert.IsNotNull(thrax, "the patrol is there");
                Assert.AreEqual(PlateauMaligoPrisonerV1.Patrol, thrax.DbId);
                Assert.AreEqual(TargetCategory.Hostile, thrax.TargetCategory);
                AssertStandsOnNavMesh(copy, thrax.Position);
                Assert.IsTrue(thrax.Position.X > pond.X && thrax.Position.Z < pond.Z && Vector3.Distance(pond, thrax.Position) < 30,
                    $"south-east of the pond: {thrax.Position}");
                Assert.IsTrue(copy.NavMesh.FindPath(prisoner.Position, thrax.Position, out var reached) != null && reached, "walked to");
            }
            harness.MoveTo(prisoner.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, prisoner.EntityId, OutOfThinAir,
                PlateauOutOfThinAirV1.GiveIt, 1), "no stim pack yet");

            var corpse = Kill(harness, copy, patrol[0]);
            Assert.IsNotNull(corpse, "the Thrax leaves a corpse");
            Assert.IsTrue(corpse.LootItems.Any(item => item.ItemTemplateId == PlateauOutOfThinAirV1.Stim), "the stim pack on him");
            PickUp(harness, corpse);
            harness.Drain();
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauOutOfThinAirV1.Stim));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, OutOfThinAir, PlateauOutOfThinAirV1.StimPack));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, OutOfThinAir, PlateauOutOfThinAirV1.GiveIt));

            // A reload halfway keeps the stim pack and the objectives.
            WildernessRanjaGorgeTests.Reload(harness);
            Tick(harness, copy, 250);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauOutOfThinAirV1.Stim), "after the reload");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, OutOfThinAir, PlateauOutOfThinAirV1.GiveIt), "after the reload");

            // 3, 1: she takes it and walks out; then the radio.
            AssertHandInTopic(Conversation(harness, prisoner), OutOfThinAir, PlateauOutOfThinAirV1.GiveIt);
            harness.MoveTo(prisoner.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, prisoner.EntityId, OutOfThinAir, PlateauOutOfThinAirV1.GiveIt, 1));
            harness.Drain();
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauOutOfThinAirV1.Stim), "she has it");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, OutOfThinAir, PlateauOutOfThinAirV1.Escort));
            for (var index = 0; index < PlateauOutOfThinAirV1.Route.Length; index++)
                AssertStandsOnNavMesh(copy, new Vector3(PlateauOutOfThinAirV1.Route[index].X, PlateauOutOfThinAirV1.Route[index].Y,
                    PlateauOutOfThinAirV1.Route[index].Z));

            var start = prisoner.Position;
            var furthest = 0f;
            for (var tick = 0; tick < 400 && Objective(harness, OutOfThinAir, PlateauOutOfThinAirV1.Escort) != MissionObjectiveState.Completed; tick++)
            {
                if (prisoner.State != CharacterState.Dead && Here(copy).Contains(prisoner))
                {
                    furthest = System.Math.Max(furthest, Vector3.Distance(start, prisoner.Position));
                    harness.MoveTo(prisoner.Position + Vector3.UnitZ);
                }
                Tick(harness, copy, 1000);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, OutOfThinAir, PlateauOutOfThinAirV1.Escort),
                $"she stalled at {prisoner.Position}");
            Assert.IsTrue(furthest > 150, $"she walked the way out ({furthest:F0} m from her rock)");
            // The scene ends at the exit; her lease reset walks her back to her rock (as Pierre), there for the next player.
            var exit = PlateauOutOfThinAirV1.Route[^1];
            for (var tick = 0; tick < 90 && !Here(copy).Any(creature => creature.SpawnPool?.DbId == PlateauMaligoPrisonerV1.PrisonerPool &&
                     Vector3.Distance(ToVector(PlateauMaligoPrisonerV1.PrisonerAt), creature.Position) < 2 && creature.IsInteractable); tick++)
                Tick(harness, copy, 1000);
            Assert.IsFalse(Here(copy).Any(creature => creature.SpawnPool?.DbId == PlateauMaligoPrisonerV1.PrisonerPool &&
                Vector3.Distance(new Vector3(exit.X, exit.Y, exit.Z), creature.Position) < 5), "not left at the exit");
            Assert.IsTrue(Here(copy).Any(creature => creature.SpawnPool?.DbId == PlateauMaligoPrisonerV1.PrisonerPool &&
                Vector3.Distance(ToVector(PlateauMaligoPrisonerV1.PrisonerAt), creature.Position) < 2 && creature.IsInteractable),
                "back on her rock");
            Assert.IsTrue(harness.Client.Player.Missions[OutOfThinAir].Completeable);
            Assert.IsFalse(Conversation(harness, prisoner).ContainsKey(ConversationType.MissionComplete), "no NPC turn-in");

            var before = harness.Context.ReadRewardTotals();
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, OutOfThinAir, 0, null));
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[OutOfThinAir].State);
            Assert.AreEqual(before.Credits + PlateauOutOfThinAirV1.Credits, harness.Context.ReadRewardTotals().Credits);
            Assert.AreEqual(before.Experience + PlateauOutOfThinAirV1.Experience, harness.Context.ReadRewardTotals().Experience);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauOutOfThinAirV1.BioVest), "the first choice");
        }

        [TestMethod]
        public void OutOfThinAirFailsWhenThePrisonerDiesOnTheWay()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            SeedCompleted(harness, SilentButDeadly, Interception, MonkeyWrench);
            var copy = IntoUstor(harness);
            var prisoner = Prisoner(harness, copy);
            WildernessRanjaGorgeTests.Offer(harness, prisoner, OutOfThinAir);
            Accept(harness, prisoner, OutOfThinAir);
            Tick(harness, copy, 250);
            PickUp(harness, Kill(harness, copy, SceneCreature(copy, PlateauOutOfThinAirScene.PatrolRole(1))));
            harness.Drain();
            harness.MoveTo(prisoner.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, prisoner.EntityId, OutOfThinAir, PlateauOutOfThinAirV1.GiveIt, 1));
            harness.Drain();
            for (var tick = 0; tick < 6; tick++)
            {
                harness.MoveTo(prisoner.Position + Vector3.UnitZ);
                Tick(harness, copy, 1000);
            }
            Assert.IsTrue(Vector3.Distance(ToVector(PlateauMaligoPrisonerV1.PrisonerAt), prisoner.Position) > 3, "she is on her way");

            prisoner.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(copy, prisoner, null);
            Tick(harness, copy, 250);
            var mission = harness.Client.Player.Missions[OutOfThinAir];
            Assert.AreEqual(MissionObjectiveState.Failed, mission.Objectives[PlateauOutOfThinAirV1.Escort].State);
            Assert.AreEqual(MissionState.Failed, mission.State);
            Assert.IsFalse(mission.Completeable);

            // She comes back to her rock (upstream's release through the respawn), for the mission to be taken again.
            AwaitRelease(harness, copy);
            Assert.IsTrue(Offers(harness, Prisoner(harness, copy), OutOfThinAir), "she offers it again");
        }

        [TestMethod]
        public void OutOfThinAirAbandonedTakesThePatrolAndIsOfferedAgain()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            SeedCompleted(harness, SilentButDeadly, Interception, MonkeyWrench);
            var copy = IntoUstor(harness);
            var prisoner = Prisoner(harness, copy);
            WildernessRanjaGorgeTests.Offer(harness, prisoner, OutOfThinAir);
            Accept(harness, prisoner, OutOfThinAir);
            Tick(harness, copy, 250);
            var patrol = Enumerable.Range(0, PlateauOutOfThinAirScene.PatrolSize)
                .Select(index => SceneCreature(copy, PlateauOutOfThinAirScene.PatrolRole(index))).ToArray();
            Assert.IsTrue(patrol.All(thrax => thrax != null), "the patrol is there");

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, OutOfThinAir));
            Tick(harness, copy, 250);
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(OutOfThinAir), "gone from the log");
            Assert.IsFalse(patrol.Any(thrax => Here(copy).Contains(thrax)), "the patrol is gone");
            AwaitRelease(harness, copy);
            Assert.IsTrue(Offers(harness, Prisoner(harness, copy), OutOfThinAir), "she offers it again");
        }

        /// <summary>Ustor Yard as the server loads it, then the player through the Plateau's door into the squad's copy.</summary>
        private static MapChannel IntoUstor(WildernessRuntimeTestHarness harness)
        {
            if (!harness.Maps.MapChannelArray.ContainsKey(Ustor))
                LoadMap(harness, Ustor);
            var copy = ThroughTheDoor(harness, Ustor);
            Assert.IsTrue(copy.IsSquadInstance);
            return copy;
        }

        /// <summary>
        /// Until the prisoner's public lease is released and she stands on her rock again, as upstream's Escape Velocity
        /// tests wait for Pierre (WildernessEscapeVelocityTests.AwaitRelease): the scene's reset, then her pool's respawn.
        /// A squad copy outlives a test, so the tests after a death or an abandon wait for her too.
        /// </summary>
        private static void AwaitRelease(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            bool Back() => harness.Manager.PublicActors.Handle(copy, PlateauMaligoPrisonerV1.PrisonerPool) == null &&
                Here(copy).Any(creature => creature.SpawnPool?.DbId == PlateauMaligoPrisonerV1.PrisonerPool && creature.Npc != null &&
                    creature.IsInteractable && creature.State != CharacterState.Dead);
            var all = copy.SpawnPools.ToArray();
            try
            {
                copy.SpawnPools.Clear();
                copy.SpawnPools.AddRange(all.Where(pool => pool.DbId == PlateauMaligoPrisonerV1.PrisonerPool));
                for (var tick = 0; tick < 1800 && !Back(); tick++)
                {
                    SpawnPoolManager.Instance.SpawnPoolWorker(copy, 250);
                    Tick(harness, copy, 250);
                }
            }
            finally
            {
                copy.SpawnPools.Clear();
                copy.SpawnPools.AddRange(all);
            }
            Assert.IsTrue(Back(), "the prisoner is released and back on her rock");
        }

        /// <summary>
        /// The prisoner on her rock. A squad copy outlives a test: one of an earlier test has the combat gate of that
        /// test's mission manager, so it is bound again to this one's (as a respawn binds it, ActorAvailable).
        /// </summary>
        private static Creature Prisoner(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            if (!Here(copy).Any(creature => creature.DbId == PlateauMaligoPrisonerV1.Prisoner))
                Spawn(copy, PlateauMaligoPrisonerV1.PrisonerPool);
            harness.Manager.PublicActors.ActorAvailable(copy);
            var prisoner = Here(copy).SingleOrDefault(creature => creature.SpawnPool?.DbId == PlateauMaligoPrisonerV1.PrisonerPool && creature.Npc != null);
            Assert.IsNotNull(prisoner, "the prisoner stands in the copy");
            return prisoner;
        }

        private static bool Offers(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.MissionDispense, out var offered) &&
            ((System.Collections.Generic.Dictionary<uint, MissionInfo>)offered).ContainsKey(mission);

        private static Vector3 ToVector((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);

        private static Creature Rapp(MapChannel copy)
        {
            if (!Here(copy).Any(creature => creature.DbId == PlateauUstorYardV1.Rapp))
                Spawn(copy, PlateauUstorYardV1.RappPool);
            var rapp = Here(copy).SingleOrDefault(creature => creature.SpawnPool?.DbId == PlateauUstorYardV1.RappPool && creature.Npc != null);
            Assert.IsNotNull(rapp, "Rapp stands in the copy");
            return rapp;
        }

        /// <summary>
        /// The living creatures of this copy only. An earlier test's creatures can still be listed in the cells of
        /// this test's copy (seen: the second patrol of the test before; the harness's Dispose takes out only the
        /// creatures of its own map); those have the earlier copy as their map. A test that fails half-way can
        /// still leave creatures this does not tell apart (seen: seven guards for three after a failed run).
        /// </summary>
        private static System.Collections.Generic.IEnumerable<Creature> Here(MapChannel map) =>
            Living(map).Where(creature => ReferenceEquals(creature.RuntimeMapChannel, map));

        private static (MapChannel Copy, Creature Rapp) AcceptInterception(WildernessRuntimeTestHarness harness)
        {
            SeedCompleted(harness, SilentButDeadly);
            var copy = IntoUstor(harness);
            var rapp = Rapp(copy);
            var offer = WildernessRanjaGorgeTests.Offer(harness, rapp, Interception);
            CollectionAssert.AreEqual(new[]
                {
                    (PlateauInterceptionV1.MedPack, 3U), (PlateauInterceptionV1.FragGrenade, 3U),
                    (PlateauInterceptionV1.EmpBomb, 1U), (PlateauInterceptionV1.CryoGrenade, 3U)
                },
                offer.SelectableReward.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            Accept(harness, rapp, Interception);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Interception, PlateauInterceptionV1.Intercept));
            return (copy, rapp);
        }

        /// <summary>Into Chemical Storage, as MissionAreaService takes a step: objective 1 done, the first patrol on the pad.</summary>
        private static (Creature[] First, System.DateTime Entered) IntoChemicalStorage(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            Tick(harness, copy, 250);
            Assert.IsNull(SceneCreature(copy, PlateauInterceptionV1.FirstRole(0)), "no patrol before Chemical Storage");
            var at = PlateauInterceptionV1.StorageAt;
            var inside = new Vector3((float)at.X + 20, 177.4f, (float)at.Z);
            var outside = inside + new Vector3(80, 0, -80);
            harness.MoveTo(inside);
            Assert.IsTrue(new MissionAreaService(() => harness.Manager).RecordAcceptedMovement(harness.Client, outside, inside),
                "Chemical Storage entered");
            var entered = harness.UtcNow;
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Interception, PlateauInterceptionV1.Intercept));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Interception, PlateauInterceptionV1.TakeOut));
            Tick(harness, copy, 250);

            var first = Enumerable.Range(0, PlateauInterceptionV1.FirstPatrolAt.Length)
                .Select(index => SceneCreature(copy, PlateauInterceptionV1.FirstRole(index))).ToArray();
            for (var index = 0; index < first.Length; index++)
            {
                Assert.IsNotNull(first[index], $"patrol 1 bot {index + 1} on the pad");
                Assert.AreEqual(PlateauUstorYardV1.Security, first[index].DbId);
                Assert.AreEqual(TargetCategory.Hostile, first[index].TargetCategory);
                AssertStandsOnNavMesh(copy, first[index].Position);
            }
            Assert.IsTrue(PlateauInterceptionV1.SecondRoles.All(role => SceneCreature(copy, role) == null), "only the first patrol lands");
            return (first, entered);
        }

        /// <summary>
        /// The timer, started on entering Chemical Storage at <paramref name="entered"/>, runs out: the second patrol, four
        /// Thrax and two Kael at level 32 and twice the hit points, lands.
        /// </summary>
        private static void SecondPatrolLands(WildernessRuntimeTestHarness harness, MapChannel copy, System.DateTime entered)
        {
            Tick(harness, copy, (long)(entered.AddMilliseconds(PlateauInterceptionV1.SecondaryPatrolMs - 1000) - harness.UtcNow).TotalMilliseconds);
            Assert.IsTrue(PlateauInterceptionV1.SecondRoles.All(role => SceneCreature(copy, role) == null), "not before the timer");
            Tick(harness, copy, 2000);
            Tick(harness, copy, 250);
            for (var index = 0; index < PlateauInterceptionV1.SecondPatrol.Length; index++)
            {
                var bot = SceneCreature(copy, PlateauInterceptionV1.SecondRole(index));
                Assert.IsNotNull(bot, $"patrol 2 bot {index + 1} lands");
                Assert.AreEqual(PlateauInterceptionV1.SecondPatrol[index].Creature, bot.DbId);
                Assert.AreEqual(PlateauUstorYardV1.PatrolLevel, bot.Level);
                Assert.AreEqual(TargetCategory.Hostile, bot.TargetCategory);
                Assert.AreEqual(bot.DbId == PlateauUstorYardV1.PatrolKael ? (int)PlateauUstorYardV1.PatrolKaelHitPoints : (int)PlateauUstorYardV1.PatrolThraxHitPoints,
                    (int)bot.Attributes[Attributes.Health].Current, $"patrol 2 bot {index + 1} hit points");
                AssertStandsOnNavMesh(copy, bot.Position);
            }
        }

        private static void Accept(WildernessRuntimeTestHarness harness, Creature npc, uint mission)
        {
            harness.MoveTo(npc.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, npc.EntityId, mission));
            harness.Drain();
        }

        private static MissionObjectiveState Objective(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].State;

        private static DynamicObject FindSceneObject(MapChannel map, uint mission, string role) =>
            map.DynamicObjects.SingleOrDefault(obj => obj.SceneMissionId == mission && obj.SceneActorRole == role);

        private static DynamicObject SceneObject(MapChannel map, uint mission, string role)
        {
            var obj = FindSceneObject(map, mission, role);
            Assert.IsNotNull(obj, $"{role} is there");
            return obj;
        }

        private static Creature SceneCreature(MapChannel map, string role) => Here(map)
            .SingleOrDefault(creature => creature.SpawnPool?.SceneActorRole == role);

        /// <summary>Kills the creature as the player and runs the scene on; returns its corpse, if it has one.</summary>
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

        /// <summary>As PlateauCidTests.Tick, on the map given.</summary>
        private static void Tick(WildernessRuntimeTestHarness harness, MapChannel map, long milliseconds)
        {
            harness.UtcNow = harness.UtcNow.AddMilliseconds(milliseconds);
            BehaviorManager.Instance.MapChannelThink(map, System.Math.Min(milliseconds, 1000));
            harness.Manager.Scenes.Tick(map);
            harness.Drain();
        }

        /// <summary>A use of the object from where it stands, and its recovery if one is due (PlateauTemporalChamberTests.Use).</summary>
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

        /// <summary>A weapon hit on the object, as PlateauTemporalChamberTests.Damage.</summary>
        private static void Damage(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj, int damage)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            var action = new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0) { TargetId = obj.EntityId };
            MissileManager.Instance.MissileLaunch(map, action, damage);
            MissileManager.Instance.DoWork(map, 1000);
            harness.Drain();
        }
    }
}
