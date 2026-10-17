using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Game.Missions;
    using Rasa.Managers;
    using Rasa.Missions.Scenes;
    using Rasa.Services.Preloader;
    using Rasa.Services.Preloader.Missions;
    using Rasa.Structures;

    /// <summary>
    /// The Proving Grounds' cave-in seen from across the bridge (BootcampCaveInLandmarkV10), and
    /// the six Thrax through it taking the sandbag post before the bridge
    /// (BootcampBreachAssaultV12).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class BootcampBreachTests
    {
        private static readonly Vector3 Doorwell = new(276.5f, 120.9f, 64.5f);

        #region The rocks seen from across the bridge

        [TestMethod]
        public void BothScenesGiveTheRocksAViewRadiusAndChangeNothingElse()
        {
            var capture = BootcampCaveInLandmarkV10.CaptureTheFlag();
            var experience = BootcampCaveInLandmarkV10.Experience();

            foreach (var scene in new[] { capture, experience.Scene })
            {
                var rocks = scene.Actors[BootcampCaveInBreachV9.CaveIn];
                Assert.AreEqual(150f, rocks.ViewRadius);
                Assert.AreEqual(BootcampCaveInBreachV9.CaveInActor(), rocks with { ViewRadius = 0 }, "the same pile, as far as anything else goes");
            }

            Assert.HasCount(BootcampCaveInBreachV9.CaptureTheFlag().Actors.Count, capture.Actors);
            Assert.HasCount(BootcampCaveInBreachV9.Experience().Scene.Actors.Count, experience.Scene.Actors);
        }

        [TestMethod]
        public void FromTheFarEndOfTheBridgeThePlayerHasTheRocks()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            var map = harness.BootcampMap;
            var rocks = BootcampRuntimeTestHarness.FindScenarioObject(map, BootcampCaveInBreachV9.CaveIn);

            Assert.AreEqual(150f, rocks.ViewRadius, "stood up by the experience with its radius");

            // The far end of the bridge: 76 m off, beyond the rocks' cells.
            harness.MovePlayerTo(new Vector3(351f, 121.7f, 64.5f));
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.IsTrue(CellManager.TryGetCellCoordinates(rocks.CellPosition, out var x, out var z));
            Assert.IsFalse(CellManager.Instance.GetClientsInCells(map, CellManager.Instance.CreateCellMatrix(map, x, z)).Contains(harness.Client),
                "not through its cells");

            Landmarks.Worker(map);

            Assert.IsTrue(Landmarks.HeldFromAfar(harness.Client, rocks), "in view all the same");
        }

        /// <summary>
        /// A pile already standing when a scene takes it up - Capture the Flag's breach binding
        /// the one the experience put down - is seen as that scene says, so one put down before
        /// its definition had a radius is given it.
        /// </summary>
        [TestMethod]
        public void APileAlreadyStandingIsGivenTheRadiusWhenASceneTakesItUp()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);
            harness.Client.Player.GmFlagAlwaysFriendly = true;
            var rocks = BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, BootcampCaveInBreachV9.CaveIn);
            rocks.ViewRadius = 0;

            Breach(harness);

            Assert.AreSame(rocks, BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, BootcampCaveInBreachV9.CaveIn), "the same pile");
            Assert.AreEqual(UseObjectState.DoorStateOpen, rocks.StateId, "blown by Capture the Flag's scene");
            Assert.AreEqual(150f, rocks.ViewRadius);
        }

        #endregion

        #region The Thrax take the post

        [TestMethod]
        public void EachThraxHasItsOwnRouteToALineBeforeThePostAndThenOntoTheBridge()
        {
            var capture = BootcampBreachAssaultV12.CaptureTheFlag();
            var wave = capture.Sequences[BootcampCaveInBreachV9.WaveSequence];
            var charges = wave.World.OfType<RunRouteIntent>().ToList();

            Assert.IsFalse(capture.Routes.ContainsKey(BootcampCaveInBreachV9.ChargeRoute), "the one route they shared");
            Assert.HasCount(6, charges);
            Assert.HasCount(6, charges.Select(charge => charge.Route).Distinct().ToList(), "one each, so they do not end on one spot");
            Assert.IsTrue(charges.All(charge => charge.ResumeAfterCombat), "routes through fights");
            Assert.AreEqual(150f, capture.Actors[BootcampCaveInBreachV9.CaveIn].ViewRadius, "on the landmark's scene");

            var posts = new[] { new Vector3(314.9492f, 120.5312f, 59.4219f), new Vector3(316.2148f, 120.5312f, 60.2383f) };
            var shown = new Vector3(294.2773f, 120.5312f, 60.9844f);

            foreach (var charge in charges)
            {
                var route = capture.Routes[charge.Route];
                Assert.HasCount(2, route.Points);

                var line = route.Points[0];
                Assert.AreEqual(25f, line.EngageRadius);
                Assert.AreEqual(5000u, line.PauseMilliseconds);
                Assert.IsLessThan(3.5f, Vector3.Distance(Flat(Position(line)), Flat(shown)), "around where the GM stood");
                foreach (var post in posts)
                    Assert.IsLessThan(line.EngageRadius, Vector3.Distance(Position(line), post), "the post's Infantrymen in reach");

                var bridge = route.Points[1];
                Assert.AreEqual(20f, bridge.EngageRadius);
                Assert.IsTrue(bridge.Position.X > 320.8f && bridge.Position.X < 353.2f, "on the bridge");
            }

            // The rest of the wave and of Capture the Flag - Youngblood's squad and the quiet fort
            // among it - is as it was.
            var before = BootcampCaveInFortSquadV11.CaptureTheFlag();
            var beforeWave = before.Sequences[BootcampCaveInBreachV9.WaveSequence].World;
            Assert.HasCount(beforeWave.Count, wave.World);
            for (var i = 0; i < wave.World.Count; i++)
                Assert.AreEqual(Json(beforeWave[i] is RunRouteIntent run ? run with { Route = ((RunRouteIntent)wave.World[i]).Route } : beforeWave[i]), Json(wave.World[i]));
            Assert.AreEqual(Json(before.Actors), Json(capture.Actors));
            foreach (var sequence in before.Sequences.Where(s => s.Key != BootcampCaveInBreachV9.WaveSequence))
                Assert.AreEqual(Json(sequence.Value), Json(capture.Sequences[sequence.Key]));
            foreach (var route in before.Routes.Where(r => r.Key != BootcampCaveInBreachV9.ChargeRoute))
                Assert.AreEqual(Json(route.Value), Json(capture.Routes[route.Key]));
            Assert.HasCount(before.Routes.Count - 1 + 6, capture.Routes);
        }

        /// <summary>
        /// Migrated, Capture the Flag's scene has the wave's routes on top of everything before
        /// them - Youngblood's squad, the quiet fort, the rocks' radius - and Down gives back the
        /// one route alone.
        /// </summary>
        [TestMethod]
        public void TheMigratedSceneHasTheWavesRoutesAndKeepsTheSquadAndTheRadius()
        {
            const string before = "20270110000000_Bootcamp_cave_in_with_fort_squad";
            const string migration = "20270110000100_Bootcamp_breach_assault";
            var directory = System.IO.Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(directory);

            try
            {
                var database = System.IO.Path.Combine(directory, "world");

                using (var context = Rasa.Test.Database.PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.SqliteWorldContext), database))
                    Rasa.Test.Database.MigratedDatabaseTemplates.Migrate(context, () => Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.Migrate(context.Database));

                using var world = (Rasa.Context.World.WorldContext)Rasa.Test.Database.PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.SqliteWorldContext), database);
                var migrator = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>(world);

                migrator.Migrate(migration);
                var scene = Scene(world);
                Assert.AreEqual(Json(BootcampBreachAssaultV12.CaptureTheFlag()), Json(scene), "the assault's scene, whole");
                Assert.IsTrue(BootcampFortSquadV10.Squad.All(soldier => scene.Actors.ContainsKey(soldier.Role)), "the squad kept");
                Assert.AreEqual(150f, scene.Actors[BootcampCaveInBreachV9.CaveIn].ViewRadius, "the radius kept");
                Assert.HasCount(6, scene.Routes.Keys.Where(key => key.StartsWith(BootcampCaveInBreachV9.ChargeRoute + "-")).ToList());

                migrator.Migrate(before);
                Assert.AreEqual(Json(BootcampCaveInFortSquadV11.CaptureTheFlag()), Json(Scene(world)), "the one route back, nothing else");
            }
            finally
            {
                try { System.IO.Directory.Delete(directory, true); } catch (System.IO.IOException) { }
            }
        }

        private static Rasa.Missions.Content.MissionSceneDefinition Scene(Rasa.Context.World.WorldContext world) =>
            System.Text.Json.JsonSerializer.Deserialize<Rasa.Missions.Content.MissionSceneDefinition>(
                Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.SqlQueryRaw<string>(world.Database,
                    "SELECT bindings AS Value FROM mission_scene_binding WHERE mission_id = 1994").AsEnumerable().Single(),
                Rasa.Missions.Content.MissionContentCodec.Options);

        [TestMethod]
        public void EveryPointIsOnTheWalkableMeshAndReachedFromTheDoorwell()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            var map = harness.BootcampMap;
            var capture = BootcampBreachAssaultV12.CaptureTheFlag();

            foreach (var route in capture.Routes.Values.Where(route => route.Key.StartsWith(BootcampCaveInBreachV9.ChargeRoute)))
            {
                var previous = Doorwell;
                foreach (var point in route.Points)
                {
                    var destination = Position(point);
                    var path = map.NavMesh.FindPath(previous, destination, out var complete);

                    // As SceneRouteController.Start holds every route to.
                    Assert.IsTrue(complete && path.Count > 0, $"{route.Key}: a way to {destination}");
                    Assert.IsLessThanOrEqualTo(0.5f, Vector3.Distance(path[^1], destination), $"{route.Key}: {destination} on the mesh");
                    Assert.IsLessThan(0.5f, Math.Abs(path[^1].Y - destination.Y), $"{route.Key}: {destination} at the floor's height");
                    previous = destination;
                }
            }
        }

        [TestMethod]
        public void TheWaveStopsAtThePostToFightItAndPushesOnToTheBridgeOnlyOnceItIsTaken()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            var map = harness.BootcampMap;
            SpawnPoolManager.Instance.SpawnPoolWorker(map, 0);
            harness.Client.Player.GmFlagAlwaysFriendly = true;

            // The sandbag post's two Infantrymen, and the Rifleman and Shaman Initiate at the front
            // before it, a few metres off the line (BootcampFrontLine).
            var infantry = BootcampCaveInBreachTests.Creatures(harness).Where(creature => creature.DbId == BootcampBaseNpcs.PistolInfantrymanId).ToList();
            var front = BootcampCaveInBreachTests.Creatures(harness).Where(creature =>
                creature.DbId == BootcampFrontLine.RiflemanId || creature.DbId == BootcampFrontLine.ShamanId).ToList();
            Assert.HasCount(2, infantry, "the post's two Infantrymen");
            Assert.HasCount(2, front, "the two at the front");
            var defenders = infantry.Concat(front).ToList();
            foreach (var soldier in defenders)
                soldier.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1_000_000, 1_000_000, 1_000_000, 0, 0);

            Breach(harness);

            var wave = BootcampCaveInBreachTests.Thrax(harness);
            Assert.HasCount(6, wave);

            Creature On(Creature thrax) => thrax.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting
                ? defenders.FirstOrDefault(soldier => soldier.EntityId == thrax.Controller.ActionFighting.TargetEntityId)
                : null;
            void Hold(string while_, Func<bool> each)
            {
                for (var tick = 0; tick < 80; tick++)
                {
                    Step(harness);
                    each();
                    Assert.IsTrue(wave.All(thrax => thrax.Position.X < 312f), $"none past the post {while_} (tick {tick}): {string.Join(", ", wave.Select(t => t.Position.X))}");
                }
            }

            // Out of the doorwell to the line, and into the fight there.
            var fought = false;
            Hold("while it stands", () => fought |= wave.Any(thrax => On(thrax) != null));
            Assert.IsTrue(fought, "the defenders taken on");

            // One falls: those who fought him come back to the line and hold again - its first five
            // seconds long gone - and take on another, not run on past the rest.
            var first = defenders.OrderByDescending(soldier => wave.Count(thrax => On(thrax) == soldier)).First();
            var turned = wave.Where(thrax => On(thrax) == first).ToList();
            Assert.IsNotEmpty(turned, "some on him when he falls");
            ActorManager.Instance.Damage(map, first, 10_000_000, turned[0]);
            defenders.Remove(first);

            var retaken = false;
            Hold("while the rest stand", () => retaken |= turned.Any(thrax => On(thrax) != null));
            Assert.IsTrue(retaken, "another taken on by those who felled the first");

            // The front falls: the post's Infantrymen behind the sandbags are taken on from the line.
            foreach (var soldier in front.Where(defenders.Contains).ToList())
            {
                ActorManager.Instance.Damage(map, soldier, 10_000_000, wave[0]);
                defenders.Remove(soldier);
            }

            var post = false;
            Hold("while the post stands", () => post |= wave.Any(thrax => infantry.Contains(On(thrax))));
            Assert.IsTrue(post, "the post's Infantrymen taken on");

            // The post falls, and nothing else is left to fight: on to the bridge.
            foreach (var soldier in infantry)
                ActorManager.Instance.Damage(map, soldier, 10_000_000, wave[0]);
            foreach (var friend in BootcampCaveInBreachTests.Creatures(harness).Where(c => c.TargetCategory == TargetCategory.Friendly && c.Position.X > 280f && c.Position.X < 340f))
                ActorManager.Instance.Damage(map, friend, 10_000_000, wave[0]);

            for (var tick = 0; tick < 200 && !wave.All(OnTheBridge); tick++)
                Step(harness);

            Assert.IsTrue(wave.Where(thrax => thrax.State != CharacterState.Dead).All(OnTheBridge),
                $"up onto the bridge: {string.Join(", ", wave.Select(t => t.Position.X))}");
        }

        #endregion

        private static void Breach(BootcampRuntimeTestHarness.Harness harness)
        {
            harness.SeedMission(harness.Client.Player.Id, 1992, (uint)MissionState.Completed, true);
            var deSimone = BootcampCaveInBreachTests.Creatures(harness).Single(actor => actor.DbId == BootcampRuntimeTestHarness.CorporalDeSimoneCreatureId);
            harness.MovePlayerTo(deSimone);
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Drain();
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, deSimone.EntityId, 1994));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, deSimone.EntityId, 1994, 4, 1));

            BootcampCaveInBreachTests.Cross(harness, new Vector3(340f, 121.7f, 64.5f));
            BootcampCaveInBreachTests.Cross(harness, new Vector3(316f, 120.61f, 64.5f));
            harness.UtcNow += TimeSpan.FromSeconds(2.5);
            harness.Manager.TickScenarios(harness.Client);
        }

        private static void Step(BootcampRuntimeTestHarness.Harness harness)
        {
            harness.UtcNow += TimeSpan.FromMilliseconds(250);
            harness.Manager.TickScenarios(harness.Client);
            BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, 250);
        }

        private static bool OnTheBridge(Creature thrax) => thrax.Position.X > 320.8f;

        private static Vector3 Position(SceneWaypoint point) => new(point.Position.X, point.Position.Y, point.Position.Z);

        private static Vector3 Flat(Vector3 point) => new(point.X, 0, point.Z);

        private static string Json<T>(T document) => System.Text.Json.JsonSerializer.Serialize(document, Rasa.Missions.Content.MissionContentCodec.Options);
    }
}
