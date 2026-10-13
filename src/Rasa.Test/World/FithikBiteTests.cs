using System;
using System.Collections.Generic;
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
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Test.Database;
    using Rasa.Test.TestSupport;

    /// <summary>
    /// A Fithik bites with its weapon, Weapon_Creature_Fithik's 174/34, within the client's 3 m
    /// (Fithik_bites). The region Fithiks had only their lunge and acid spray, and stood five
    /// metres off for ten seconds and more between them; the Hive Monarch and the Hive Master bit
    /// from nine metres away.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class FithikBiteTests
    {
        private const string Migration = "20261217000000_Fithik_bites";
        private const string Previous = "20261216000000_Bind_control_point_turret_loot";

        private const long Tick = 250;

        private static readonly uint[] FithikClasses = { 4313, 10323, 20698, 21499, 24069, 24070 };

        [TestMethod]
        public void EveryFithikBitesWithinThreeMetresAndDownPutsTheOldRowsBack()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "world");

                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());

                using var world = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database);

                AssertEveryFithikBites(world);

                var rows = world.CreatureActionEntries.AsNoTracking().ToDictionary(row => row.Id);
                foreach (var bite in FithikBites.Bites)
                {
                    var row = rows[bite.Id];
                    Assert.AreEqual((174u, 34u, 0.5, 3.0, 800u, 0u, bite.MinDamage, bite.MaxDamage, 1u),
                        (row.ActionId, row.ActionArgId, row.RangeMin, row.RangeMax, row.Cooldown, row.Windup, row.MinDamage, row.MaxDamage, row.DamageType), row.Description);
                }

                // The statements are guarded: run again by hand, nothing changes.
                foreach (var statement in FithikBites.Up())
                    world.Database.ExecuteSqlRaw(statement);

                Assert.AreEqual(FithikBites.Bites.Length, world.CreatureActionEntries.AsNoTracking().Count(row => row.Id >= FithikBites.IdMin && row.Id <= FithikBites.IdMax));
                AssertEveryFithikBites(world);

                var migrator = world.GetService<IMigrator>();
                migrator.Migrate(Previous);

                var creatures = world.CreatureEntries.AsNoTracking().ToDictionary(row => row.Id);
                Assert.IsFalse(world.CreatureActionEntries.AsNoTracking().Any(row => row.Id >= FithikBites.IdMin && row.Id <= FithikBites.IdMax));
                foreach (var bite in FithikBites.Bites)
                    Assert.AreEqual(0u, creatures[bite.Creature].Action4, $"creature {bite.Creature}: its slot empty, as it was");
                Assert.AreEqual(10.0, world.CreatureActionEntries.AsNoTracking().Single(row => row.Id == FithikBites.BossBite).RangeMax, "the bosses' 10 m, as it was");

                migrator.Migrate(Migration);

                AssertEveryFithikBites(world);
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
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            StringAssert.Contains(up, "SELECT 74003, 'Fithik Palisades weapon 174/34', 174, 34, 0.5, 3.0, 800, 0, 62, 93, 1 FROM creature c WHERE c.id = 540006 AND NOT EXISTS (SELECT 1 FROM creature_action a WHERE a.id = 74003);");
            StringAssert.Contains(up, "UPDATE creature SET action4 = 74001 WHERE id = 531022 AND action4 = 0;");
            StringAssert.Contains(up, "UPDATE creature_action SET range_max = 3.0 WHERE id = 44 AND action_id = 174 AND action_arg_id = 34 AND range_max = 10.0;");
            StringAssert.Contains(up, $"'{Migration}'");
            StringAssert.Contains(down, "UPDATE creature SET action4 = 0 WHERE id = 540027 AND action4 = 74005;");
            StringAssert.Contains(down, "DELETE FROM creature_action WHERE id BETWEEN 74001 AND 74005;");
            StringAssert.Contains(down, "UPDATE creature_action SET range_max = 10.0 WHERE id = 44 AND action_id = 174 AND action_arg_id = 34 AND range_max = 3.0;");
        }

        /// <summary>
        /// A Palisades Fithik's three attacks, as the region rows give them, two metres from its
        /// target: it bites as the bite comes off cooldown, and still sprays and lunges as theirs
        /// do. Without the bite it did two things in nine seconds.
        /// </summary>
        [TestMethod]
        public void ARegionFithikBitesBetweenItsSprayAndItsLunge()
        {
            var used = Fight(withBite: true, start: 2f, seconds: 9);

            Assert.IsGreaterThanOrEqualTo(5, used.Count(action => action.ActionId == ActionId.WeaponMelee), Describe(used));
            Assert.AreEqual(1, used.Count(action => action.ActionId == ActionId.CrFithikAcidSpray), Describe(used));
            Assert.AreEqual(1, used.Count(action => action.ActionId == ActionId.CrFithikLunge), Describe(used));

            var before = Fight(withBite: false, start: 2f, seconds: 9);

            Assert.HasCount(2, before, "the lunge and the spray, and nothing else: " + Describe(before));
        }

        /// <summary>
        /// A region Fithik twenty metres from its target opens with its lunge (KaelRushingBlow): it
        /// winds up, leaps, and its target is first hurt with the Fithik beside it. The lunge used
        /// to land from where the Fithik stood.
        /// </summary>
        [TestMethod]
        public void ARegionFithikLungesInFromTwentyMetresAndHitsBesideItsTarget()
        {
            var trace = new List<(float Distance, int Health)>();
            var used = Fight(withBite: true, start: 20f, seconds: 4, trace);

            Assert.AreEqual(ActionId.CrFithikLunge, used.First().ActionId, Describe(used));

            var firstHurt = trace.FindIndex(step => step.Health < 100000);

            Assert.IsGreaterThanOrEqualTo(0, firstHurt, "never hurt: " + string.Join(" ", trace.Select(t => $"{t.Distance:0.0}/{t.Health}")) + " used " + Describe(used));
            Assert.IsLessThanOrEqualTo(3f, trace[firstHurt].Distance, $"hurt from {trace[firstHurt].Distance:0.0} m");
            Assert.IsGreaterThanOrEqualTo(3, firstHurt, "not before the windup is through");
        }

        /// <summary>
        /// The bosses' bite: from eight metres it comes in, and bites within three. At the 10 m
        /// row 44 had, it stood where it was and bit from eight.
        /// </summary>
        [TestMethod]
        public void TheHiveMonarchComesInToBite()
        {
            var bitFrom = BossBites(FithikBites.RangeMax);

            Assert.IsNotEmpty(bitFrom, "it never bit");
            Assert.IsTrue(bitFrom.All(distance => distance < (float)FithikBites.RangeMax), "bit from " + string.Join(", ", bitFrom.Select(d => d.ToString("0.0"))));

            var before = BossBites(FithikBites.BossBiteWas);

            Assert.IsTrue(before.All(distance => distance > 7.5f), "it bit from where it stood: " + string.Join(", ", before.Select(d => d.ToString("0.0"))));
        }

        private static List<float> BossBites(double reach)
        {
            using var world = new WorldTestContext();
            var enemy = Watch(world, x: 8f);
            var bite = Bite(15, 20);
            bite.RangeMax = reach;
            var boss = Spawn(world, x: 0, bite);

            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(boss, enemy.Player.EntityId));
            boss.Controller.ActionFighting.Opened = true;

            var bitFrom = new List<float>();

            for (var think = 0; think < 40; think++)
            {
                var timer = bite.CooldownTimer;
                BehaviorManager.Instance.MapChannelThink(world.Map, Tick);

                if (bite.CooldownTimer > timer)
                    bitFrom.Add(Vector3.Distance(boss.Position, enemy.Player.Position));
            }

            return bitFrom;
        }

        private static void AssertEveryFithikBites(WorldContext world)
        {
            var actions = world.CreatureActionEntries.AsNoTracking().ToDictionary(row => row.Id);

            var fithiks = world.CreatureEntries.AsNoTracking().ToList()
                .Where(row => FithikClasses.Contains(row.ClassId)).ToList();

            Assert.HasCount(9, fithiks, "the Fithiks the world has");

            foreach (var creature in fithiks)
            {
                var slots = new[] { creature.Action1, creature.Action2, creature.Action3, creature.Action4,
                    creature.Action5, creature.Action6, creature.Action7, creature.Action8 }.Where(id => id != 0).Select(id => actions[id]).ToList();
                var bites = slots.Where(row => row.ActionId == 174 && row.ActionArgId == 34).ToList();

                Assert.HasCount(1, bites, $"creature {creature.Id} ({creature.Comment}): one Weapon_Creature_Fithik bite");
                Assert.IsLessThanOrEqualTo(3.0, bites[0].RangeMax, $"creature {creature.Id} ({creature.Comment}): its bite reaches {bites[0].RangeMax} m");
                Assert.IsFalse(slots.Any(row => row.ActionId == 1 && row.ActionArgId == 133), $"creature {creature.Id}: no AFS pistol");
            }
        }

        private static List<CreatureAction> Fight(bool withBite, float start, int seconds) => Fight(withBite, start, seconds, null);

        private static List<CreatureAction> Fight(bool withBite, float start, int seconds, List<(float Distance, int Health)> trace)
        {
            using var world = new WorldTestContext();
            var enemy = Watch(world, x: start);
            var actions = new List<CreatureAction>
            {
                new() { ActionId = ActionId.CrFithikLunge, ActionArgId = 1, RangeMin = 1.0, RangeMax = 30.0, Cooldown = 15000, WindupTime = 1200, MinDamage = 84, MaxDamage = 106 },
                new() { ActionId = ActionId.CrFithikAcidSpray, ActionArgId = 1, RangeMin = 0.5, RangeMax = 5.0, Cooldown = 10000, MinDamage = 64, MaxDamage = 106 }
            };

            if (withBite)
                actions.Add(Bite(62, 93));

            var fithik = Spawn(world, x: 0, actions.ToArray());

            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(fithik, enemy.Player.EntityId));
            fithik.Controller.ActionFighting.Opened = true;

            var used = new List<CreatureAction>();

            // The lunge is a leap (KaelRushingBlow), timed on its own clock: a quarter second a think.
            var clock = 1_000_000L;
            KaelRushingBlow.Now = () => clock;

            try
            {
                for (var think = 0; think < seconds * 1000 / Tick; think++)
                {
                    var timers = fithik.Actions.Select(action => action.CooldownTimer).ToList();
                    BehaviorManager.Instance.MapChannelThink(world.Map, Tick);

                    // A windup is held on the real clock (CreatureWindups.IsWindingUp); these thinks are a quarter
                    // second each, so its end is brought on with them.
                    fithik.Controller.WindupUntil -= Tick;
                    clock += Tick;
                    KaelRushingBlow.Worker(world.Map);
                    MissileManager.Instance.DoWork(world.Map, Tick);

                    // Used this think: its cooldown started again.
                    used.AddRange(fithik.Actions.Where((action, slot) => action.CooldownTimer > timers[slot]));

                    trace?.Add((Vector3.Distance(fithik.Position, enemy.Player.Position), enemy.Player.Attributes[Attributes.Health].Current));
                }
            }
            finally
            {
                KaelRushingBlow.Now = () => Environment.TickCount64;
                world.Map.QueuedMissiles.Clear();
            }

            return used;
        }

        private static CreatureAction Bite(uint min, uint max) => new()
        {
            ActionId = ActionId.WeaponMelee, ActionArgId = FithikBites.FithikArg, RangeMin = FithikBites.RangeMin, RangeMax = FithikBites.RangeMax,
            Cooldown = FithikBites.Cooldown, MinDamage = min, MaxDamage = max
        };

        private static string Describe(IEnumerable<CreatureAction> used) =>
            string.Join(", ", used.Select(action => $"{(uint)action.ActionId}/{action.ActionArgId}"));

        private static Creature Spawn(WorldTestContext world, float x, params CreatureAction[] actions)
        {
            var creature = new Creature
            {
                Name = "Fithik",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(x, 0, 0),
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 23,
                AggroRange = 0,
                RunSpeed = 9,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            creature.HomePos.Position = creature.Position;
            creature.HomePos.MapContextid = creature.MapContextId;
            creature.Attributes[Attributes.Health] = CreatureHealth.NewAttribute(100000);
            creature.Actions.AddRange(actions);
            BehaviorManager.StartWandering(creature, false);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellsAt(world, creature.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        private static Client Watch(WorldTestContext world, float x)
        {
            var client = world.CreateClient(x: x);
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100000, 100000, 100000, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            client.Player.Cells = CellsAt(world, client.Player.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            return client;
        }

        private static uint[,] CellsAt(WorldTestContext world, Vector3 position)
        {
            var seed = CellManager.Instance.GetCellSeed(position);
            return CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
        }
    }
}
