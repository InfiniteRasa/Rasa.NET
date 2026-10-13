using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Game.Missions;
    using Rasa.Managers;
    using Rasa.Missions.Scenes;
    using Rasa.Services.Preloader;
    using Rasa.Services.Preloader.Missions;
    using Rasa.Structures;
    using Rasa.Test.Database;
    using Rasa.Test.World;

    /// <summary>
    /// The cave-in across the Proving Grounds' bridge and the Thrax who blow it during Capture the
    /// Flag (BootcampCaveInBreachV9, Add_bootcamp_cave_in_breach).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class BootcampCaveInBreachTests
    {
        /// <summary>The world before: three Thrax stood at the bridge from the start. For the tests that fight them.</summary>
        internal const string BridgeThraxStood = "20261202000000_Place_bootcamp_base_npcs";

        private const string Migration = "20261203000000_Add_bootcamp_cave_in_breach";
        private const uint ThraxName = 7674;

        [TestMethod]
        public void TheSeedTakesTheBridgeThraxAwayAndAddsTheCrossingAndDownPutsThemBack()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "world");

                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());

                using var world = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database);
                var migrator = world.GetService<IMigrator>();

                // As this migration left it: Fix_bootcamp_soldier_weapon_attacks, after it, gives the Field Gunner his machine gun's attack.
                migrator.Migrate(Migration);

                AssertBreach(world);

                migrator.Migrate(BridgeThraxStood);

                var thrax = world.SpawnPoolEntries.AsNoTracking().Where(pool => pool.Id >= 510218 && pool.Id <= 510220).OrderBy(pool => pool.Id).ToList();
                Assert.HasCount(3, thrax);
                Assert.IsTrue(thrax.All(pool => pool.Creature1Id == 510216 && pool.MapContextId == 1985 && pool.RespawnTime == 200));
                Assert.AreEqual(318.0, thrax[0].PosX, 0.0001);
                Assert.AreEqual(120.80204, thrax[0].PosY, 0.0001);
                Assert.AreEqual(71.811775, thrax[2].PosZ, 0.0001);
                Assert.AreEqual(-1.570796, thrax[1].Rotation, 0.0001);
                Assert.AreEqual(0, Count(world, "mission_area WHERE mission_id = 1994 AND area_id = 440"));
                Assert.AreEqual(0, Count(world, "mission_objective_transition WHERE mission_id = 1994 AND objective_id = 2 AND transition_id = 2"));
                Assert.AreEqual(0, Count(world, "mission_scenario WHERE mission_id = 1994 AND scenario_id = 5"));
                Assert.AreEqual(0, Count(world, "mission_spawn WHERE mission_id = 1994 AND spawn_group_id = 4"));
                Assert.IsFalse(Binding(world).Contains("bootcamp-cave-in"));
                Assert.IsFalse(Experience(world).Contains("bootcamp-cave-in"));
                Assert.AreEqual(0u, world.CreatureEntries.AsNoTracking().Single(row => row.Id == BootcampBaseNpcs.PistolInfantrymanId).Action1);
                Assert.AreEqual(0u, world.CreatureEntries.AsNoTracking().Single(row => row.Id == BootcampBaseNpcs.FieldGunnerId).Action1);

                migrator.Migrate(Migration);

                AssertBreach(world);
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }

        private static void AssertBreach(WorldContext world)
        {
            Assert.IsFalse(world.SpawnPoolEntries.AsNoTracking().Any(pool => pool.Id >= 510218 && pool.Id <= 510220), "no Thrax at the bridge");
            Assert.AreEqual(2, world.SpawnPoolEntries.AsNoTracking().Count(pool => pool.Id == 510216 || pool.Id == 510217), "the AFS checkpoint stays");

            Assert.AreEqual(1, Count(world, "mission_area WHERE mission_id = 1994 AND area_id = 440 AND shape = 1 AND pos_x = 317 AND pos_z = 65 AND radius = 7"));
            Assert.AreEqual(1, Count(world, "mission_objective_transition WHERE mission_id = 1994 AND objective_id = 2 AND transition_id = 2 AND from_state = 1 AND to_state = 1"));
            Assert.AreEqual(1, Count(world, "mission_trigger WHERE mission_id = 1994 AND objective_id = 2 AND transition_id = 2 AND kind = 4 AND area_id = 440"));
            Assert.AreEqual(1, Count(world, "mission_action WHERE mission_id = 1994 AND objective_id = 2 AND transition_id = 2 AND kind = 5 AND scenario_id = 5"));
            Assert.AreEqual(1, Count(world, "mission_scenario WHERE mission_id = 1994 AND scenario_id = 5 AND start_policy = 1"));
            Assert.AreEqual(1, Count(world, "mission_spawn_group WHERE mission_id = 1994 AND spawn_group_id = 4 AND enabled = 0 AND spawn_policy = 1"));
            Assert.AreEqual(6, Count(world, "mission_spawn WHERE mission_id = 1994 AND spawn_group_id = 4 AND creature_id = 510216"));

            // The cave-in exit is as it was: the objective still completes in the doorwell.
            Assert.AreEqual(1, Count(world, "mission_trigger WHERE mission_id = 1994 AND objective_id = 2 AND transition_id = 1 AND kind = 4 AND area_id = 439"));

            StringAssert.Contains(Binding(world), "\"breach-explode-cave-in\"");
            StringAssert.Contains(Experience(world), "\"stage-cave-in\"");
            StringAssert.Contains(Experience(world), "\"capture-open-cave-in\"");

            Assert.AreEqual(510218u, world.CreatureEntries.AsNoTracking().Single(row => row.Id == BootcampBaseNpcs.PistolInfantrymanId).Action1);
            Assert.AreEqual(2u, world.CreatureEntries.AsNoTracking().Single(row => row.Id == BootcampBaseNpcs.FieldGunnerId).Action1);
            Assert.AreEqual(0u, world.CreatureEntries.AsNoTracking().Single(row => row.Id == BootcampBaseNpcs.OceanaId).Action1);
        }

        private static int Count(WorldContext world, string from) => world.Database
            .SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM {from}").AsEnumerable().Single();

        private static string Binding(WorldContext world) => world.Database
            .SqlQueryRaw<string>("SELECT bindings AS Value FROM mission_scene_binding WHERE mission_id = 1994")
            .AsEnumerable().Single();

        private static string Experience(WorldContext world) => world.Database
            .SqlQueryRaw<string>("SELECT bindings AS Value FROM mission_experience_binding WHERE experience_key = 'bootcamp'")
            .AsEnumerable().Single();

        [TestMethod]
        public void TheScenesStandTheRocksUpBlowThemAndSendSixThrax()
        {
            var capture = BootcampCaveInBreachV9.CaptureTheFlag();
            var experience = BootcampCaveInBreachV9.Experience();

            foreach (var scene in new[] { capture, experience.Scene })
            {
                var rocks = scene.Actors[BootcampCaveInBreachV9.CaveIn];

                Assert.AreEqual(SceneActorKind.Object, rocks.Kind);
                Assert.AreEqual(29434u, rocks.TemplateId);
                Assert.AreEqual(31u, rocks.InitialObjectState, "a closed door: the pile");
                Assert.IsFalse(rocks.InitiallyInteractable);
                Assert.AreEqual(BootcampCaveInBreachV9.CaveIn, rocks.SharedKey, "one pile for both scenes");
                Assert.AreEqual(new ScenePosition(275.2656f, 121.2305f, 64.3711f), rocks.Position);
                Assert.AreEqual(4.7124, rocks.Orientation, 0.0001);
            }

            // Breach: the pile opens (its explosion), and two seconds on the wave.
            var breach = capture.Sequences[capture.Names["breach"]];
            Assert.AreEqual(5u, capture.Names["breach"]);
            Assert.IsInstanceOfType<EnsureActorIntent>(breach.World[0]);
            var explode = (TransitionObjectStateIntent)breach.World[1];
            Assert.AreEqual(91u, explode.State);
            Assert.AreEqual(BootcampCaveInBreachV9.CaveIn, explode.Role);
            Assert.AreEqual(capture.Names["breach-wave"], breach.Timers.Single().SequenceId);
            Assert.AreEqual(2000u, breach.Timers.Single().Milliseconds);

            var wave = capture.Sequences[capture.Names["breach-wave"]];
            var thrax = wave.World.OfType<EnsureActorIntent>().Select(intent => capture.Actors[intent.Role]).ToList();
            Assert.HasCount(6, thrax);
            Assert.IsTrue(thrax.All(actor => actor.TemplateId == 510216 && actor.Kind == SceneActorKind.Creature && actor.MissionId == 1994 && actor.GroupId == 4));
            Assert.IsTrue(thrax.All(actor => actor.Position.X > 273 && actor.Position.X < 277 && actor.Position.Z > 61 && actor.Position.Z < 68), "in the doorwell");
            var charges = wave.World.OfType<RunRouteIntent>().ToList();
            Assert.HasCount(6, charges);
            Assert.IsTrue(charges.All(charge => charge.ResumeAfterCombat && charge.Route == BootcampCaveInBreachV9.ChargeRoute));
            Assert.IsTrue(capture.Routes[BootcampCaveInBreachV9.ChargeRoute].Points.Last().Position.X > 320.8f, "onto the bridge");

            // The rest of Capture the Flag is as it was.
            var before = BootcampBaseNpcScenesV8.CaptureTheFlag();
            foreach (var sequence in before.Sequences)
                Assert.AreEqual(sequence.Value.World.Count, capture.Sequences[sequence.Key].World.Count);

            // The experience: up from its start and on accepting Capture the Flag; open once it is rewarded.
            Assert.IsTrue(experience.Scene.Sequences[0].World.OfType<EnsureActorIntent>().Any(intent => intent.Role == BootcampCaveInBreachV9.CaveIn));
            Assert.AreEqual(BootcampCaveInBreachV9.CaveIn, experience.Scene.Sequences[2].World[0].Role);
            var open = experience.Scene.Sequences[3].World.OfType<SetInteractionIntent>().Single(intent => intent.Role == BootcampCaveInBreachV9.CaveIn);
            Assert.AreEqual(91u, open.ObjectState);
            Assert.IsTrue(open.IfPresent);
        }

        [TestMethod]
        public void CrossingTheBridgeBlowsTheRocksAndSendsOneWaveOfSixThrax()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);
            harness.Client.Player.GmFlagAlwaysFriendly = true;

            var rocks = BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, BootcampCaveInBreachV9.CaveIn);
            Assert.IsNotNull(rocks, "the rocks stand in the doorwell from the start");
            Assert.AreEqual(UseObjectState.DoorStateClosed, rocks.StateId);
            Assert.AreEqual(new Vector3(275.2656f, 121.2305f, 64.3711f), rocks.Position);
            Assert.IsFalse(Thrax(harness).Any(), "no Thrax before");

            // Capture the Flag, and the promotion that opens "Find a way out of the cave".
            harness.SeedMission(harness.Client.Player.Id, 1992, (uint)MissionState.Completed, true);
            var deSimone = Creatures(harness).Single(actor => actor.DbId == BootcampRuntimeTestHarness.CorporalDeSimoneCreatureId);
            harness.MovePlayerTo(deSimone);
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Drain();
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, deSimone.EntityId, 1994));

            // On the bridge but not off it: nothing.
            Cross(harness, new Vector3(340f, 121.7f, 64.5f));
            Assert.AreEqual(UseObjectState.DoorStateClosed, rocks.StateId, "nothing before the promotion");

            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, deSimone.EntityId, 1994, 4, 1));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1994].Objectives[2].State);

            Cross(harness, new Vector3(340f, 121.7f, 64.5f));
            Assert.AreEqual(UseObjectState.DoorStateClosed, rocks.StateId, "still on the bridge");

            harness.Drain();
            Cross(harness, new Vector3(316f, 120.61f, 64.5f));

            Assert.AreEqual(UseObjectState.DoorStateOpen, rocks.StateId, "off the west end: the rocks blow");
            Assert.AreSame(rocks, BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, BootcampCaveInBreachV9.CaveIn), "the same pile");
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1994].Objectives[2].State,
                "the way out is still through the doorwell");
            Assert.IsFalse(Thrax(harness).Any(), "not yet");

            harness.UtcNow += TimeSpan.FromSeconds(2.5);
            harness.Manager.TickScenarios(harness.Client);

            var wave = Thrax(harness);
            Assert.HasCount(6, wave);
            foreach (var thrax in wave)
            {
                Assert.AreEqual(TargetCategory.Hostile, thrax.TargetCategory);
                Assert.IsTrue(Vector3.Distance(thrax.Position, rocks.Position) < 6f, $"out of the doorwell, at {thrax.Position}");
            }

            // Coming off the bridge again sends no more, and the dead are not stood up again.
            ActorManager.Instance.Damage(harness.BootcampMap, wave[0], 100000, harness.Client.Player);
            Assert.AreEqual(CharacterState.Dead, wave[0].State);
            Cross(harness, new Vector3(340f, 121.7f, 64.5f));
            Cross(harness, new Vector3(316f, 120.61f, 64.5f));
            harness.UtcNow += TimeSpan.FromSeconds(5);
            harness.Manager.TickScenarios(harness.Client);
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 60000);

            Assert.AreEqual(5, Thrax(harness).Count(thrax => thrax.State != CharacterState.Dead));

            // They come on toward the checkpoint.
            var start = wave.Skip(1).Select(thrax => thrax.Position.X).Average();
            for (var tick = 0; tick < 40; tick++)
                BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, 250);
            Assert.IsTrue(wave.Skip(1).Where(thrax => thrax.State != CharacterState.Dead).Select(thrax => thrax.Position.X).Average() > start + 3f,
                "east, out of the doorwell toward the bridge");
        }

        private static void Cross(BootcampRuntimeTestHarness.Harness harness, Vector3 to)
        {
            var from = harness.Client.Player.Position;
            harness.MovePlayerTo(to);
            CellManager.Instance.UpdateVisibility(harness.Client);
            new MissionAreaService(() => harness.Manager).RecordAcceptedMovement(harness.Client, from, to);
        }

        private static Creature[] Creatures(BootcampRuntimeTestHarness.Harness harness) =>
            harness.BootcampMap.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().ToArray();

        /// <summary>Thrax Infantry Initiates (510216); the route Thrax further west are other rows.</summary>
        private static Creature[] Thrax(BootcampRuntimeTestHarness.Harness harness) =>
            Creatures(harness).Where(creature => creature.DbId == BootcampCaveInBreachV9.ThraxTemplate && creature.NameId == ThraxName).ToArray();

        [TestMethod]
        public void TheMigrationMakesTheSameRowsOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(BridgeThraxStood, Migration);
            var down = migrator.GenerateScript(Migration, BridgeThraxStood);

            StringAssert.Contains(up, "delete from spawnpool where id = 510218 and map_context_id = 1985;");
            StringAssert.Contains(up, "values (1994, 'deployment_11', 440, 1, 1985, 1, 317, 120.61, 65, 7, '1994 west end of the bridge');");
            StringAssert.Contains(up, "values (1994, 'deployment_11', 2, 2, 1, 2, 1, 1, 'Bridge crossed');");
            StringAssert.Contains(up, "values (1994, 'deployment_11', 5, 1, 1, 'bootcamp-1994-breach', '1994 cave-in breach');");
            StringAssert.Contains(up, "values (1994, 'deployment_11', 4, 6, 510216, 276.5, 120.9, 67.5, 4.7124, 1);");
            StringAssert.Contains(up, "update creature set action1 = 510218 where id = 400007;");
            StringAssert.Contains(up, "UPDATE `mission_scene_binding`");
            StringAssert.Contains(up, "UPDATE `mission_experience_binding`");
            StringAssert.Contains(up, $"'{Migration}'");

            StringAssert.Contains(down, "values (510220, 0, 0, 200, 317.24707, 121.662315, 71.811775, -1.570796, 1985, 510216, 1, 1,");
            StringAssert.Contains(down, "delete from mission_area where mission_id = 1994 and content_revision = 'deployment_11' and area_id = 440;");
        }
    }
}
