using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Missions.Content;
    using Rasa.Missions.Scenes;
    using Rasa.Repositories.World;
    using Rasa.Services.Preloader;
    using Rasa.Services.Preloader.Missions;
    using Rasa.Structures;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;

    /// <summary>The Proving Grounds' third lot of people (BootcampBaseNpcs, BootcampBaseNpcScenesV8, Place_bootcamp_base_npcs).</summary>
    [TestClass]
    [DoNotParallelize]
    public class BootcampBaseNpcTests
    {
        private const string Before = "20261201000000_Add_bootcamp_trainees";
        private const string Migration = "20261202000000_Place_bootcamp_base_npcs";

        private static readonly uint[] QuestNpcs = { 510203, 510204, 510205, 510206, 510207, 510208, 510209 };

        [TestMethod]
        public void TheSeedPlacesThePeopleMovesTheOthersAndDownPutsItAllBack()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "world");

                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());

                using var world = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database);
                var classes = world.EntityClassEntries.AsNoTracking().ToDictionary(entry => entry.Id, entry => entry.ClassName);
                var spawnpools = new SpawnpoolRepository(world);
                var migrator = world.GetService<IMigrator>();

                // As this migration left it: Add_bootcamp_cave_in_breach, after it, arms the checkpoint.
                migrator.Migrate(Migration);

                AssertPlaced(world, classes, spawnpools);

                migrator.Migrate(Before);

                // Down: none of it, and the others where they were.
                Assert.IsFalse(world.CreatureEntries.AsNoTracking().Any(row => row.Id >= 400006 && row.Id <= 400008));
                Assert.IsFalse(world.SpawnPoolEntries.AsNoTracking().Any(row => row.Id >= 400006 && row.Id <= 400009));
                Assert.IsFalse(spawnpools.GetPatrols().Any(row => row.PoolId == BootcampBaseNpcs.FieldGunnerPoolId));
                Assert.IsFalse(new AmbientNpcRepository(world).Get().Any(row => row.Id >= 18));
                Assert.IsFalse(world.Set<Rasa.Structures.World.CreatureActionEntry>().AsNoTracking().Any(row => row.Id == 510218));

                var shooters = new AmbientNpcRepository(world).Get().Where(row => row.Id <= 2).ToList();
                Assert.AreEqual(380.7695, shooters[0].PosX, 0.0001);
                Assert.AreEqual(173.1484, shooters[0].PosZ, 0.0001);
                Assert.AreEqual(3.1054, shooters[0].Rotation, 0.0001);
                Assert.AreEqual(375.418, shooters[1].PosX, 0.0001);
                Assert.AreEqual(173.2656, shooters[1].PosZ, 0.0001);
                Assert.AreEqual(3.1541, shooters[1].Rotation, 0.0001);

                var desimone = world.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == BootcampBaseNpcs.DeSimonePoolId);
                Assert.AreEqual(391.5, desimone.PosX, 0.0001);
                Assert.AreEqual(164.8, desimone.PosZ, 0.0001);

                var escorts = world.CreatureEntries.AsNoTracking().Where(row => row.Id >= 510213 && row.Id <= 510215).OrderBy(row => row.Id).ToList();
                CollectionAssert.AreEqual(new uint[] { 7034, 7035, 7036 }, escorts.Select(row => row.ClassId).ToArray());
                CollectionAssert.AreEqual(new uint[] { 5, 510214, 28 }, escorts.Select(row => row.Action1).ToArray());
                Assert.AreEqual(17u, escorts[0].Action2);
                CollectionAssert.AreEquivalent(new uint[] { 6042, 6164, 6453 },
                    world.CreatureAppearanceEntries.AsNoTracking().Where(row => row.Id >= 510213 && row.Id <= 510215 && row.SlotId == 13).Select(row => row.ClassId).ToArray());

                CollectionAssert.AreEqual(new[] { 368.0, 372.0, 374.0 }, Spawns(world, "pos_x"));
                CollectionAssert.AreEqual(new[] { 120.21479, 119.956856, 119.74777 }, Spawns(world, "pos_y"));
                Assert.AreEqual("Forean companions", SpawnGroup(world));

                Assert.AreEqual(21, world.CreatureAppearanceEntries.AsNoTracking()
                    .Count(row => QuestNpcs.Contains(row.Id) && row.Color == BootcampBaseNpcs.OfficerBlue));
                Assert.IsTrue(Binding(world, 1992).Contains("\"practice-1\""));
                Assert.IsTrue(Experience(world).Contains("\"practice-2\""));

                // And up again.
                migrator.Migrate(Migration);

                AssertPlaced(world, classes, spawnpools);
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }

        private static void AssertPlaced(WorldContext world, System.Collections.Generic.Dictionary<uint, string> classes, SpawnpoolRepository spawnpools)
        {
            var creatures = world.CreatureEntries.AsNoTracking().Where(row => row.Id >= 400006 && row.Id <= 400008).OrderBy(row => row.Id).ToList();

            CollectionAssert.AreEqual(new[] { "NPC_Forean_Shaman", "Redshirt_Human_Soldier_Light_Male", "Redshirt_Human_Soldier_Heavy_Male" },
                creatures.Select(row => classes[row.ClassId]).ToArray());
            CollectionAssert.AreEqual(new uint[] { 10580, 8716, 8720 }, creatures.Select(row => row.NameId).ToArray());

            foreach (var creature in creatures)
            {
                Assert.AreEqual(1u, creature.Faction, "friendly");
                Assert.AreEqual(0u, creature.Action1, "no attack: safe ground");
                Assert.AreEqual(0u, creature.RunSpeed);
            }

            CollectionAssert.AreEqual(new uint[] { 0, 0, 1 }, creatures.Select(row => row.WalkSpeed).ToArray(), "the Field Gunner walks");
            CollectionAssert.AreEquivalent(
                new[] { (400006u, "Weapon_Creature_Forean_Staff"), (400007u, "Weapon_Human_Redshirt_Pistol_Physical"), (400008u, "Weapon_Human_Redshirt_MachineGun_Physical") },
                world.CreatureAppearanceEntries.AsNoTracking().Where(row => row.Id >= 400006 && row.Id <= 400008)
                    .AsEnumerable().Select(row => (row.Id, classes[row.ClassId])).ToList());

            var pools = world.SpawnPoolEntries.AsNoTracking().Where(row => row.Id >= 400006 && row.Id <= 400009).OrderBy(row => row.Id).ToList();
            CollectionAssert.AreEqual(new uint[] { 400006, 400007, 400007, 400008 }, pools.Select(row => row.Creature1Id).ToArray());
            Assert.IsTrue(pools.All(row => row.MapContextId == 1985));
            Assert.AreEqual(372.0, pools[0].PosX, 0.0001);
            Assert.AreEqual(165.5352, pools[0].PosZ, 0.0001);
            Assert.AreEqual(5.3184, pools[0].Rotation, 0.0001);
            Assert.AreEqual(314.9492, pools[1].PosX, 0.0001);
            Assert.AreEqual(2.3668, pools[2].Rotation, 0.0001);

            var poses = spawnpools.GetPoses().ToDictionary(row => row.Id, row => row.Pose);
            Assert.AreEqual((byte)NpcPose.Standing, poses[400006]);
            Assert.AreEqual((byte)NpcPose.Standing, poses[400007]);
            Assert.AreEqual((byte)NpcPose.Standing, poses[400008]);
            Assert.IsFalse(poses.ContainsKey(400009), "he walks");

            var patrol = spawnpools.GetPatrols().Where(row => row.PoolId == BootcampBaseNpcs.FieldGunnerPoolId).OrderBy(row => row.Step).ToList();
            Assert.HasCount(2, patrol);
            Assert.AreEqual(pools[3].PosX, patrol[0].PosX, 0.0001, "made on the first step");
            Assert.AreEqual(327.3203, patrol[1].PosX, 0.0001);
            CollectionAssert.AreEqual(new double?[] { 1.5987, 1.4857 }, patrol.Select(row => row.Facing).ToArray());
            CollectionAssert.AreEqual(new uint[] { 4000, 4000 }, patrol.Select(row => row.PauseMs).ToArray());

            var figures = new AmbientNpcRepository(world).Get().Where(row => row.Id >= 18 && row.Id <= 21).ToList();
            CollectionAssert.AreEqual(
                new[] { "UsableStatelessNPCFemaleDrinkingSittingBarstoolV01", "UsableStatelessNPCMaleDrinkingStandingV01",
                    "UsableStatelessNPCMaleStandingOperatingConsoleV01", "UsableStatelessNPCMaleStandingTabletV01" },
                figures.Select(row => classes[row.ClassId]).ToArray());
            Assert.AreEqual(119.5292 - 0.37, figures[0].PosY, 0.0001, "the barstool sitter on the chair");
            Assert.AreEqual(0.2772, figures[0].Rotation, 0.0001, "facing as the chair does");

            var shooters = new AmbientNpcRepository(world).Get().Where(row => row.Id <= 2).ToList();
            Assert.AreEqual(BootcampBaseNpcs.MiddleLaneX, shooters[0].PosX, 0.0001);
            Assert.AreEqual(BootcampBaseNpcs.WestLaneX, shooters[1].PosX, 0.0001);
            Assert.IsTrue(shooters.All(row => Math.Abs(row.PosZ - BootcampBaseNpcs.ShooterZ) < 0.0001 && row.Rotation == BootcampBaseNpcs.ShooterRotation));
            Assert.AreEqual(119.5938, shooters[0].PosY, 0.0001, "on the firing step as before");

            var desimone = world.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == BootcampBaseNpcs.DeSimonePoolId);
            Assert.AreEqual(383.0781, desimone.PosX, 0.0001);
            Assert.AreEqual(119.5273, desimone.PosY, 0.0001);
            Assert.AreEqual(155.4141, desimone.PosZ, 0.0001);
            Assert.AreEqual(4.2675, desimone.Rotation, 0.0001);

            // The escorts: Infantrymen with pistols, where the GM stood for them.
            var escorts = world.CreatureEntries.AsNoTracking().Where(row => row.Id >= 510213 && row.Id <= 510215).ToList();
            Assert.IsTrue(escorts.All(row => row.ClassId == 29423 && row.NameId == 8716 && row.Action1 == 510218 && row.Action2 == 0 && row.Faction == 1));
            Assert.IsTrue(world.CreatureAppearanceEntries.AsNoTracking()
                .Where(row => row.Id >= 510213 && row.Id <= 510215 && row.SlotId == 13).AsEnumerable().All(row => row.ClassId == 6271));
            Assert.AreEqual(1u, world.Set<Rasa.Structures.World.CreatureActionEntry>().AsNoTracking().Single(row => row.Id == 510218).ActionId);

            CollectionAssert.AreEqual(new[] { 374.4727, 374.5039, 372.6797 }, Spawns(world, "pos_x"));
            CollectionAssert.AreEqual(new[] { 119.5273, 119.5273, 119.5273 }, Spawns(world, "pos_y"));
            CollectionAssert.AreEqual(new[] { 5.4425, 5.422, 5.254 }, Spawns(world, "rotation"));
            Assert.AreEqual("AFS escort", SpawnGroup(world));

            // #68562c, all three pieces on all seven; no blue left on them.
            Assert.AreEqual(21, world.CreatureAppearanceEntries.AsNoTracking()
                .Count(row => QuestNpcs.Contains(row.Id) && row.Color == BootcampBaseNpcs.OfficerOlive));
            Assert.AreEqual(0, world.CreatureAppearanceEntries.AsNoTracking()
                .Count(row => QuestNpcs.Contains(row.Id) && row.Color == BootcampBaseNpcs.OfficerBlue));
            var olive = new Color(BootcampBaseNpcs.OfficerOlive);
            Assert.AreEqual((0x68, 0x56, 0x2c), (olive.Red, olive.Green, olive.Blue));

            // The scenes as stored.
            Assert.IsFalse(Binding(world, 1992).Contains("\"practice-1\""));
            Assert.IsFalse(Binding(world, 1992).Contains("\"practice-2\""));
            Assert.IsTrue(Binding(world, 1992).Contains("\"practice-0\""));
            Assert.IsFalse(Experience(world).Contains("\"practice-1\""));
            Assert.IsTrue(Experience(world).Contains("400.2461"));
        }

        /// <summary>A column of Capture the Flag's escort spawns, by spawn id.</summary>
        private static double[] Spawns(WorldContext world, string column) => world.Database
            .SqlQueryRaw<double>($"SELECT {column} AS Value FROM mission_spawn WHERE mission_id = 1994 AND spawn_group_id = 1 ORDER BY spawn_id")
            .AsEnumerable().ToArray();

        private static string SpawnGroup(WorldContext world) => world.Database
            .SqlQueryRaw<string>("SELECT comment AS Value FROM mission_spawn_group WHERE mission_id = 1994 AND spawn_group_id = 1")
            .AsEnumerable().Single();

        private static string Binding(WorldContext world, uint missionId) => world.Database
            .SqlQueryRaw<string>("SELECT bindings AS Value FROM mission_scene_binding WHERE mission_id = {0}", missionId)
            .AsEnumerable().Single();

        private static string Experience(WorldContext world) => world.Database
            .SqlQueryRaw<string>("SELECT bindings AS Value FROM mission_experience_binding WHERE experience_key = 'bootcamp'")
            .AsEnumerable().Single();

        [TestMethod]
        public void TheScenesLoseTwoPracticeTargetsMoveTheEscortsAndEndMcAllistersWalkOnTheDeck()
        {
            var gearing = BootcampBaseNpcScenesV8.GearingUp();
            var experience = BootcampBaseNpcScenesV8.Experience();

            foreach (var scene in new[] { gearing, experience.Scene })
            {
                Assert.IsTrue(scene.Actors.ContainsKey("practice-0"));
                Assert.IsFalse(scene.Actors.ContainsKey("practice-1"));
                Assert.IsFalse(scene.Actors.ContainsKey("practice-2"));
                Assert.IsFalse(scene.Sequences.Values.SelectMany(sequence => sequence.World ?? new()).Any(intent => intent.Role is "practice-1" or "practice-2"));
                Assert.IsTrue(scene.Sequences.Values.SelectMany(sequence => sequence.World ?? new()).Any(intent => intent.Role == "practice-0"));
            }

            Assert.AreEqual(2774u, gearing.Audio.OfferAudioSetId, "Gearing Up keeps its offer audio");

            foreach (var scene in new[] { BootcampBaseNpcScenesV8.CaptureTheFlag(), experience.Scene })
            {
                var escorts = new[] { 1, 2, 3 }.Select(spawn => scene.Actors[$"group-1-spawn-{spawn}-0"]).ToList();

                CollectionAssert.AreEqual(new uint[] { 510213, 510214, 510215 }, escorts.Select(actor => actor.TemplateId).ToArray());
                Assert.AreEqual(new ScenePosition(374.4727f, 119.5273f, 159.6797f), escorts[0].Position);
                Assert.AreEqual(new ScenePosition(372.6797f, 119.5273f, 166.4805f), escorts[2].Position);
                CollectionAssert.AreEqual(new[] { 5.4425, 5.422, 5.254 }, escorts.Select(actor => actor.Orientation).ToArray());
                Assert.IsTrue(escorts.All(actor => actor.FollowOffset != null), "still follow the player");
            }

            var departure = experience.Scene.Routes["mcallister-departure"];
            Assert.HasCount(1, departure.Points);
            Assert.AreEqual(BootcampBaseNpcScenesV8.McAllisterEnd, departure.Points[0].Position);
            Assert.AreEqual(0.4992, departure.Points[0].Orientation, 0.0001);
            Assert.IsTrue(departure.ResumeAtDestination);
            Assert.AreEqual(6.5f, departure.Speed);

            // The rest of the experience is the one before.
            var before = BootcampBaseNpcScenesV8.ExperienceBefore();
            CollectionAssert.AreEquivalent(before.Scene.Actors.Keys.Except(BootcampBaseNpcScenesV8.RemovedPracticeTargets).ToList(), experience.Scene.Actors.Keys.ToList());
            CollectionAssert.AreEquivalent(before.Scene.Sequences.Keys.ToList(), experience.Scene.Sequences.Keys.ToList());
        }

        [TestMethod]
        public void TheFiringRangeTargetsStandBetweenTheirLanesFarSandbags()
        {
            foreach (var laneX in new[] { BootcampBaseNpcs.MiddleLaneX, BootcampBaseNpcs.WestLaneX })
            {
                // A figure faces its -z, (-sin r, -cos r) at rotation r; the target is 14.252 m along it.
                var r = BootcampBaseNpcs.ShooterRotation;
                var targetX = laneX - Math.Sin(r) * BootcampBaseNpcs.TargetDistance;
                var targetZ = BootcampBaseNpcs.ShooterZ - Math.Cos(r) * BootcampBaseNpcs.TargetDistance;

                Assert.AreEqual(laneX, targetX, 0.001);
                Assert.AreEqual(BootcampBaseNpcs.TargetZ, targetZ, 0.001);
                Assert.IsTrue(BootcampBaseNpcs.ShooterZ > 170.28 && BootcampBaseNpcs.ShooterZ < 174.28, "on the firing step");
                Assert.IsTrue(targetZ > 183.84 && targetZ < 187.84, "on the far platform");
            }

            // The gaps at z 187.5, read off the sandbags' collision: middle 378.90 to 382.36, west 373.60 to 376.98.
            Assert.AreEqual((378.90 + 382.36) / 2, BootcampBaseNpcs.MiddleLaneX, 0.01);
            Assert.AreEqual((373.60 + 376.98) / 2, BootcampBaseNpcs.WestLaneX, 0.01);
        }

        [TestMethod]
        public void OceanaThePostsAndThePatrollingGunnerStandAtTheProvingGrounds()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            harness.Client.Player.GmFlagAlwaysFriendly = true;
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);

            Rasa.Structures.Creature Of(uint poolId) => harness.BootcampMap.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList).Distinct()
                .Single(creature => creature.SpawnPool?.DbId == poolId);

            var oceana = Of(BootcampBaseNpcs.OceanaPoolId);
            var west = Of(BootcampBaseNpcs.PistolPostWestPoolId);
            var east = Of(BootcampBaseNpcs.PistolPostEastPoolId);
            var gunner = Of(BootcampBaseNpcs.FieldGunnerPoolId);

            Assert.AreEqual(10580u, oceana.NameId);
            Assert.AreEqual(NpcPose.Standing, oceana.Pose);
            Assert.AreEqual(NpcPose.Standing, west.Pose);
            Assert.AreEqual(NpcPose.Standing, east.Pose);
            Assert.AreEqual(6271u, west.AppearanceData[EquipmentData.Weapon].Class);

            foreach (var npc in new[] { oceana, west, east })
            {
                Assert.IsTrue(BehaviorManager.NeverMoves(npc));
                Assert.AreEqual(npc.SpawnPool.Position, npc.Position);
                Assert.AreEqual(TargetCategory.Friendly, npc.TargetCategory);
            }

            Assert.AreEqual(8720u, gunner.NameId);
            Assert.AreEqual(1f, gunner.WalkSpeed);
            Assert.HasCount(2, gunner.Patrol);
            Assert.AreEqual(NpcPose.None, gunner.Pose);
            Assert.AreEqual(20535u, gunner.AppearanceData[EquipmentData.Weapon].Class);

            // He sets off for the west end once he has stood his 4 s.
            var start = gunner.Position;

            for (var tick = 0; tick < 80; tick++)
                BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, 250);

            Assert.IsTrue(gunner.Position.X < start.X - 5f, $"walked west, at {gunner.Position}");
            Assert.AreEqual(63.7f, gunner.Position.Z, 0.5f, "along the road");
        }

        [TestMethod]
        public void TheMigrationMakesTheSameRowsOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "'Field Officer Oceana', 7035, 1, 5, 600, 10580,");
            StringAssert.Contains(up, "INSERT INTO `spawnpool_patrol`");
            StringAssert.Contains(up, "INSERT INTO `ambient_npc`");
            StringAssert.Contains(up, "values (510218, 'Bootcamp AFS escort pistol', 1, 1, 0.5, 24, 1300, 0, 8, 12, 1);");
            StringAssert.Contains(up, "update creature set comment = 'Bootcamp AFS escort, pistol', class_id = 29423, name_id = 8716, action1 = 510218, action2 = 0 where id = 510213;");
            StringAssert.Contains(up, "update ambient_npc set pos_x = 380.63, pos_z = 173.248, rotation = 3.1416 where id = 1;");
            StringAssert.Contains(up, "update spawnpool set pos_x = 383.0781, pos_y = 119.5273, pos_z = 155.4141, rotation = 4.2675 where id = 510206;");
            StringAssert.Contains(up, $"set color = {BootcampBaseNpcs.OfficerOlive} where id between 510203 and 510209 and slot_id in (2, 15, 16) and color = 4294934528;");
            StringAssert.Contains(up, "UPDATE `mission_scene_binding`");
            StringAssert.Contains(up, "update mission_spawn set pos_x = 374.4727, pos_y = 119.5273, pos_z = 159.6797, rotation = 5.4425 where mission_id = 1994");
            StringAssert.Contains(up, "UPDATE `mission_experience_binding`");
            StringAssert.Contains(up, $"'{Migration}'");

            StringAssert.Contains(down, "update creature set comment = 'Bootcamp Forean Guardsman Initiate', class_id = 7034, name_id = 7874, action1 = 5, action2 = 17 where id = 510213;");
            StringAssert.Contains(down, "update ambient_npc set pos_x = 380.7695, pos_z = 173.1484, rotation = 3.1054 where id = 1;");
            StringAssert.Contains(down, "delete from ambient_npc where id between 18 and 21;");
        }
    }
}
