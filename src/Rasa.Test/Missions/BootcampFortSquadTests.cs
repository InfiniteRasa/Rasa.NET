using System;
using System.Collections.Generic;
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
    using Rasa.Game.Missions.World;
    using Rasa.Managers;
    using Rasa.Missions.Scenes;
    using Rasa.Services.Preloader.Missions;
    using Rasa.Structures;
    using Rasa.Test.Database;

    /// <summary>
    /// Capture the Flag's fort once its Bane boss is dead (BootcampFortSquadV10,
    /// Youngblood_fort_squad): Youngblood comes in through the east gate with his squad, who take
    /// their posts, and the fort spawns no more Bane.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class BootcampFortSquadTests
    {
        private const string Before = "20270108000000_Meadow_boargar";
        private const string Migration = "20270109000000_Youngblood_fort_squad";
        private const uint CaveInAreaId = 439;

        [TestMethod]
        public void YoungbloodComesInThroughTheEastGateWithSixAndTheFortIsQuietFromTheKill()
        {
            var capture = BootcampFortSquadV10.CaptureTheFlag();
            var experience = BootcampFortSquadV10.Experience();

            foreach (var scene in new[] { capture, experience.Scene })
            {
                var youngblood = scene.Actors[BootcampFortSquadV10.Youngblood];
                Assert.IsTrue(youngblood.Position.X > 160f, "set down east of the gate (x 132), down the slope");
                var post = scene.Routes[BootcampFortSquadV10.YoungbloodRoute].Points.Single();
                Assert.AreEqual(new ScenePosition(93.2f, 109.2201f, 137.5f), post.Position, "where he stood before");
                Assert.AreEqual(Math.PI, post.Orientation, 0.001, "facing the boss's place");

                var squad = BootcampFortSquadV10.Squad.Select(soldier => scene.Actors[soldier.Role]).ToList();
                Assert.AreEqual(4, squad.Count(actor => actor.TemplateId == BootcampFortSquadV10.RiflemanTemplate));
                Assert.AreEqual(1, squad.Count(actor => actor.TemplateId == BootcampFortSquadV10.ShotgunnerTemplate));
                Assert.AreEqual(1, squad.Count(actor => actor.TemplateId == BootcampFortSquadV10.FieldGunnerTemplate));
                Assert.IsTrue(squad.All(actor => actor.Kind == SceneActorKind.Creature && actor.SharedKey != null &&
                    actor.MissionId == 1994 && actor.GroupId == 5 && actor.Position.X > 160f));
                Assert.IsTrue(BootcampFortSquadV10.Squad.All(soldier => scene.Routes[soldier.Route].ResumeAtDestination));
            }

            // The gate: two Riflemen either side inside it, the Field Gunner in the sandbags behind.
            var posts = BootcampFortSquadV10.Squad.ToDictionary(soldier => soldier.Role, soldier => soldier.Post);
            Assert.AreEqual(2, BootcampFortSquadV10.Squad.Count(soldier => soldier.Template == BootcampFortSquadV10.RiflemanTemplate &&
                soldier.Post.X > 120f && soldier.Post.X < 132f && soldier.Post.Z > 128f && soldier.Post.Z < 144f));
            Assert.IsTrue(posts["fort-field-gunner"].X < 120.4f, "behind the sandbags (120.4)");
            // Youngblood: a Rifleman either side, the Shotgunner behind.
            Assert.AreEqual(3, BootcampFortSquadV10.Squad.Count(soldier =>
                Vector2.Distance(new Vector2(soldier.Post.X, soldier.Post.Z), new Vector2(93.2f, 137.5f)) < 5.5f));

            // The kill quiets the fort; his arrival holds it.
            var delay = capture.Sequences[capture.Names["delay"]].World.OfType<QuietSpawnPoolsIntent>().Single();
            var arrival = capture.Sequences[capture.Names["arrival"]].World;
            var held = arrival.OfType<QuietSpawnPoolsIntent>().Single();
            foreach (var quiet in new[] { delay, held })
            {
                Assert.AreEqual(BootcampFortSquadV10.Youngblood, quiet.Role);
                CollectionAssert.AreEquivalent(new uint[] { 510244, 510245, 510246, 510247, 510248, 510249, 510250 }, quiet.SpawnPools);
            }
            var ensure = arrival.FindIndex(intent => intent is EnsureActorIntent && intent.Role == BootcampFortSquadV10.Youngblood);
            Assert.IsTrue(arrival.IndexOf(held) > ensure, "after Youngblood stands: rebuilt with him");
            Assert.AreEqual(7, arrival.OfType<RunRouteIntent>().Count(route => route.ResumeAfterCombat));
            Assert.AreEqual(7, arrival.OfType<EnsureActorIntent>().Count());

            foreach (var template in new[] { 510290U, 510291U, 510292U })
            {
                Assert.AreEqual(20f, experience.ActorPolicies[template].DefenseRadius);
                Assert.AreEqual("bootcamp-thrax", experience.ActorPolicies[template].DefenseTargetTag);
                Assert.IsFalse(experience.ActorPolicies[template].Invulnerable);
            }
        }

        [TestMethod]
        public void TheMigrationAddsTheSquadAndDownTakesItAway()
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
                migrator.Migrate(Migration);

                var creatures = world.CreatureEntries.AsNoTracking().Where(row => row.Id >= 510290 && row.Id <= 510292).OrderBy(row => row.Id).ToList();
                Assert.HasCount(3, creatures);
                Assert.AreEqual((29423u, 8717u, 72003u), (creatures[0].ClassId, creatures[0].NameId, creatures[0].Action1), "Rifleman");
                Assert.AreEqual((29423u, 8718u, 510290u), (creatures[1].ClassId, creatures[1].NameId, creatures[1].Action1), "Shotgunner");
                Assert.AreEqual((29433u, 8720u, 510219u), (creatures[2].ClassId, creatures[2].NameId, creatures[2].Action1), "Field Gunner");
                Assert.IsTrue(creatures.All(row => row.Faction == 1 && row.RunSpeed == 7));
                Assert.AreEqual(1, Count(world, "creature_action WHERE id = 510290 AND action_id = 1 AND action_arg_id = 121"));
                Assert.AreEqual(1, Count(world, "creature_appearance WHERE id = 510290 AND slot_id = 13 AND class_id = 27220"));
                Assert.AreEqual(1, Count(world, "creature_appearance WHERE id = 510291 AND slot_id = 13 AND class_id = 20560"));
                Assert.AreEqual(1, Count(world, "creature_appearance WHERE id = 510292 AND slot_id = 13 AND class_id = 20535"));
                Assert.AreEqual(1, Count(world, "mission_spawn_group WHERE mission_id = 1994 AND spawn_group_id = 5 AND enabled = 0 AND spawn_policy = 1"));
                Assert.AreEqual(6, Count(world, "mission_spawn WHERE mission_id = 1994 AND spawn_group_id = 5"));
                StringAssert.Contains(Binding(world), "\"fort-quiet-while-held\"");
                StringAssert.Contains(Experience(world), "\"fort-post-fort-shotgunner\"");

                // The fort's pools are the seven Bane pools inside its walls (x 60 to 132, z 88 to 200).
                var inside = world.SpawnPoolEntries.AsNoTracking()
                    .Where(pool => pool.MapContextId == 1985 && pool.PosX > 60 && pool.PosX < 132 && pool.PosZ > 88 && pool.PosZ < 200)
                    .ToList()
                    .Where(pool => world.CreatureEntries.AsNoTracking().Single(row => row.Id == pool.Creature1Id).Faction == 0)
                    .Select(pool => pool.Id).OrderBy(id => id).ToArray();
                CollectionAssert.AreEqual(BootcampFortSquadV10.FortPools, inside);

                migrator.Migrate(Before);

                Assert.AreEqual(0, Count(world, "creature WHERE id BETWEEN 510290 AND 510292"));
                Assert.AreEqual(0, Count(world, "creature_action WHERE id = 510290"));
                Assert.AreEqual(0, Count(world, "mission_spawn WHERE mission_id = 1994 AND spawn_group_id = 5"));
                Assert.IsFalse(Binding(world).Contains("fort-"));
                Assert.IsFalse(Experience(world).Contains("fort-"));

                migrator.Migrate(Migration);
                Assert.AreEqual(3, Count(world, "creature WHERE id BETWEEN 510290 AND 510292"));
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }

        [TestMethod]
        public void TheMigrationMakesTheSameRowsOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "values (510290, 'Bootcamp fort Shotgunner shotgun', 1, 121, 0.5, 12, 1500, 0, 14, 22, 1);");
            StringAssert.Contains(up, "(510292, 'Bootcamp fort Field Gunner, Youngblood''s squad', 29433, 1, 10, 1200, 8720, 7, 2, 510219,");
            StringAssert.Contains(up, "values (1994, 'deployment_11', 5, 3, 510292, 171, 103.27199, 136, 1.5708, 1);");
            StringAssert.Contains(up, "update mission_scene_binding set script_key = 'bootcamp.capture-the-flag', state_version = 1, bindings = '{");
            StringAssert.Contains(up, "\"fort-quiet-while-held\"");
            StringAssert.Contains(up, "update mission_experience_binding set map_context_id = 1985, enabled = 1, bindings = '{");
            StringAssert.Contains(down, "where experience_key = 'bootcamp';");
            StringAssert.Contains(down, "delete from creature where id between 510290 and 510292;");
        }

        [TestMethod]
        public void TheSquadRunsInTakesItsPostsAndTheFortSpawnsNoMoreBane()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            var map = harness.BootcampMap;
            SpawnPoolManager.Instance.SpawnPoolWorker(map, 0);
            harness.Client.Player.GmFlagAlwaysFriendly = true;
            Assert.IsTrue(FortThrax(map).Any(), "the fort's Bane stand before");

            var tizzik = AssaultTheFort(harness);
            ActorManager.Instance.Damage(map, tizzik, 1000000, harness.Client.Player);
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1994].Objectives[1].State);

            // From the kill: the fort's pools are quiet, the rest of the map's are not.
            var pools = map.SpawnPools.Where(pool => pool.ScenarioKey == null).ToList();
            Assert.IsTrue(pools.Where(pool => BootcampFortSquadV10.FortPools.Contains(pool.DbId)).All(pool => pool.Quieted));
            Assert.HasCount(7, pools.Where(pool => pool.Quieted).ToList());

            // What stood is left to be fought; here it is cleared out of the way.
            foreach (var thrax in FortThrax(map))
                ActorManager.Instance.Damage(map, thrax, 1000000, harness.Client.Player);
            Assert.IsNull(Squad(map).FirstOrDefault(), "nobody yet");

            harness.UtcNow += TimeSpan.FromSeconds(7);
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Client));

            var squad = Squad(map);
            Assert.HasCount(7, squad, "Youngblood and six");
            Assert.IsTrue(squad.All(actor => actor.Position.X > 160f), "on the slope east of the gate");

            for (var tick = 0; tick < 120; tick++)
            {
                BehaviorManager.Instance.MapChannelThink(map, 250);
                harness.UtcNow += TimeSpan.FromMilliseconds(250);
                harness.Manager.TickScenarios(harness.Client);
            }

            AssertAtPosts(map);
            Assert.IsTrue(Soldiers(map).All(CreatureGameplayRules.IsDefender));
            Assert.IsTrue(Soldiers(map).All(soldier => soldier.Controller.CurrentAction != BehaviorManager.BehaviorActionScriptedMove),
                "at their posts, not walked any more");

            // Long past the pools' respawn: no Bane in the fort again; outside it the way on is as it was.
            for (var pass = 0; pass < 5; pass++)
                SpawnPoolManager.Instance.SpawnPoolWorker(map, 600000);
            Assert.IsFalse(FortThrax(map).Any(thrax => thrax.State != CharacterState.Dead));
            Assert.IsTrue(map.SpawnPools.Single(pool => pool.DbId == 510251).AliveCreatures > 0, "the west breach's Thrax are there");

            // A map made again: they stand at their posts from the start, and the fort stays empty.
            harness.ReconnectFresh();
            map = harness.BootcampMap;
            SpawnPoolManager.Instance.SpawnPoolWorker(map, 0);
            SpawnPoolManager.Instance.SpawnPoolWorker(map, 600000);
            Assert.IsTrue(map.SpawnPools.Where(pool => BootcampFortSquadV10.FortPools.Contains(pool.DbId)).All(pool => pool.Quieted));
            Assert.IsFalse(FortThrax(map).Any());
            Assert.HasCount(7, Squad(map));
            AssertAtPosts(map);
            Assert.IsTrue(Squad(map).All(actor => !actor.IsRunning));
        }

        [TestMethod]
        public void AtTheirPostsTheyFightTheBaneThatComeNear()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            var map = harness.BootcampMap;
            SpawnPoolManager.Instance.SpawnPoolWorker(map, 0);
            harness.Client.Player.GmFlagAlwaysFriendly = true;

            ActorManager.Instance.Damage(map, AssaultTheFort(harness), 1000000, harness.Client.Player);
            foreach (var thrax in FortThrax(map))
                ActorManager.Instance.Damage(map, thrax, 1000000, harness.Client.Player);
            harness.UtcNow += TimeSpan.FromSeconds(7);
            harness.Manager.TickScenarios(harness.Client);
            harness.ReconnectFresh();
            map = harness.BootcampMap;
            AssertAtPosts(map);

            // A Thrax walks in at the gate.
            var pool = new SpawnPool
            {
                DbId = 999001, MapContextId = map.MapInfo.MapContextId, RuntimeMapChannel = map,
                Position = new Vector3(129f, 109.2f, 136f), SpawnSlot = new List<SpawnPoolSlot> { new(510225, 1, 1) }
            };
            map.SpawnPools.Add(pool);
            var intruder = CreatureManager.Instance.CreateScenarioCreature(pool, 510225, pool.Position, 0);
            CellManager.Instance.AddToWorld(map, intruder);

            for (var tick = 0; tick < 8; tick++)
                BehaviorManager.Instance.MapChannelThink(map, 250);

            var gate = Soldiers(map).Where(soldier => soldier.HomePos.Position.X > 115f).ToList();
            Assert.HasCount(3, gate);
            Assert.IsTrue(gate.Any(soldier => soldier.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting &&
                soldier.Controller.ActionFighting.TargetEntityId == intruder.EntityId), "the gate's soldiers take it on");
        }

        private static Creature AssaultTheFort(BootcampRuntimeTestHarness.Harness harness)
        {
            var map = harness.BootcampMap;
            harness.SeedMission(harness.Client.Player.Id, 1992, (uint)MissionState.Completed, true);
            var deSimone = Creatures(map).Single(actor => actor.DbId == BootcampRuntimeTestHarness.CorporalDeSimoneCreatureId);
            harness.MovePlayerTo(deSimone);
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Drain();
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, deSimone.EntityId, 1994));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, deSimone.EntityId, 1994, 4, 1));
            Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Area(1994, CaveInAreaId)));
            harness.Drain();
            harness.MovePlayerTo(new Vector3(95f, 109.25f, 145f));
            return BootcampRuntimeTestHarness.FindCreature(map, BootcampRuntimeTestHarness.TizzikGiCreatureId)
                ?? throw new AssertFailedException("No Tizzik Gi.");
        }

        private static void AssertAtPosts(MapChannel map)
        {
            var youngblood = BootcampRuntimeTestHarness.FindCreature(map, BootcampRuntimeTestHarness.CaptainYoungbloodCreatureId);
            Assert.IsTrue(Vector3.Distance(youngblood.Position, new Vector3(93.2f, 109.2201f, 137.5f)) < 0.5f, $"Youngblood at {youngblood.Position}");
            foreach (var soldier in BootcampFortSquadV10.Squad)
            {
                var post = new Vector3(soldier.Post.X, soldier.Post.Y, soldier.Post.Z);
                Assert.IsTrue(Soldiers(map).Any(creature => creature.DbId == soldier.Template && Vector3.Distance(creature.Position, post) < 0.5f),
                    $"{soldier.Comment}: nobody at {post}");
            }
        }

        private static Creature[] Creatures(MapChannel map) =>
            map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().ToArray();

        private static Creature[] Soldiers(MapChannel map) =>
            Creatures(map).Where(creature => creature.DbId is >= 510290 and <= 510292 && creature.State != CharacterState.Dead).ToArray();

        private static Creature[] Squad(MapChannel map) =>
            Soldiers(map).Concat(Creatures(map).Where(creature => creature.DbId == BootcampRuntimeTestHarness.CaptainYoungbloodCreatureId)).ToArray();

        private static Creature[] FortThrax(MapChannel map) =>
            Creatures(map).Where(creature => creature.SpawnPool?.ScenarioKey == null &&
                BootcampFortSquadV10.FortPools.Contains(creature.SpawnPool?.DbId ?? 0) && creature.State != CharacterState.Dead).ToArray();

        private static int Count(WorldContext world, string from) => world.Database
            .SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM {from}").AsEnumerable().Single();

        private static string Binding(WorldContext world) => world.Database
            .SqlQueryRaw<string>("SELECT bindings AS Value FROM mission_scene_binding WHERE mission_id = 1994")
            .AsEnumerable().Single();

        private static string Experience(WorldContext world) => world.Database
            .SqlQueryRaw<string>("SELECT bindings AS Value FROM mission_experience_binding WHERE experience_key = 'bootcamp'")
            .AsEnumerable().Single();
    }
}
