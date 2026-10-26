using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
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
    using Rasa.Navigation;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Test.Database;

    /// <summary>
    /// The Lightbenders' shot (1, 149) is a charged shot (CreatureWindups.IsChargedShot): it lands
    /// at the end of the client's 1400 ms windup, and not on a target that has gone out of its
    /// shooter's sight meanwhile; its rows' cooldown is the client's 4433 ms cycle
    /// (Lightbender_charged_shot, LightbenderChargedShot).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class LightbenderChargedShotTests
    {
        private const string Previous = "20270202000000_Omega_labs_missile";
        private const string Migration = "20270203000000_Lightbender_charged_shot";

        private const long Tick = 250;

        private static readonly FieldInfo Actions = typeof(AbilityManager).GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic);

        [TestMethod]
        public void TheShotLandsWhenTheChargeIsDoneAndNotBefore()
        {
            using var world = new WorldTestContext();
            using var _ = WeaponAttackData();
            var player = Watch(world, new Vector3(30, 0, 0));
            var sniper = Spawn(world);

            try
            {
                var missile = Fire(world, sniper, player);

                Assert.IsTrue(missile.AfterWindup);
                Assert.AreEqual((long)LightbenderChargedShot.WindupMs, missile.TriggerTime, "the windup, and no flight: the beam is there at once");
                Assert.IsTrue(CreatureWindups.IsWindingUp(sniper), "it stands while it charges");

                MissileManager.Instance.DoWork(world.Map, LightbenderChargedShot.WindupMs - 1);
                Assert.Contains(missile, world.Map.QueuedMissiles, "not before the charge is done");

                MissileManager.Instance.DoWork(world.Map, 1);
                Assert.DoesNotContain(missile, world.Map.QueuedMissiles);
                CollectionAssert.Contains(missile.Args.HitEntities, player.Player.EntityId);
                Assert.IsEmpty(missile.Args.MisstEntities);
            }
            finally
            {
                world.Map.QueuedMissiles.Clear();
            }
        }

        [TestMethod]
        public void ATargetThatGoesBehindTheHillWhileItChargesIsMissed()
        {
            using var world = new WorldTestContext();
            using var _ = WeaponAttackData();
            world.Map.Cover = Ridge();
            var player = Watch(world, new Vector3(30, 0, 0));
            var sniper = Spawn(world);

            try
            {
                Assert.IsTrue(BehaviorManager.Sees(world.Map.Cover, sniper, player.Player), "in the open to start with");
                var missile = Fire(world, sniper, player);

                // Behind the ridge, still in range.
                player.Player.Position = new Vector3(30, 0, 30);
                Assert.IsFalse(BehaviorManager.Sees(world.Map.Cover, sniper, player.Player));

                MissileManager.Instance.DoWork(world.Map, LightbenderChargedShot.WindupMs);

                Assert.IsEmpty(missile.Args.HitEntities);
                CollectionAssert.AreEqual(new[] { player.Player.EntityId }, missile.Args.MisstEntities.ToList());
                CollectionAssert.AreEqual(new[] { MissileManager.MissTypeMiss }, missile.Args.Missdata.ToList());
            }
            finally
            {
                world.Map.QueuedMissiles.Clear();
                world.Map.Cover = null;
            }
        }

        [TestMethod]
        public void OnlyTheLightbendersShotIsCharged()
        {
            Assert.IsTrue(CreatureWindups.IsChargedShot(ActionId.WeaponAttack, 149));
            Assert.IsFalse(CreatureWindups.IsChargedShot(ActionId.WeaponAttack, 1), "a rifle's shot lands as it did");
            Assert.IsFalse(CreatureWindups.IsChargedShot(ActionId.WeaponMachinegun, 149), "149 is the argument, not the action");

            using var world = new WorldTestContext();
            using var _ = WeaponAttackData();
            var sniper = Spawn(world);
            var rifle = new CreatureAction { ActionId = ActionId.WeaponAttack, ActionArgId = 1, RangeMax = 30, Cooldown = 800 };

            Assert.IsNull(CreatureWindups.Begin(world.Map, sniper, rifle, 20f));
        }

        [TestMethod]
        public void TheRowsTakeTheClientsCycleOnceAndTheDownPutsThePlaceholderBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            Assert.AreEqual(800L, Cooldown(database, 30), "Proctor Fulgor's");
            Assert.AreEqual(800L, Cooldown(database, 630052), "the Light Bender Snipers'");

            migrator.Migrate(Migration);
            Assert.AreEqual(4433L, Cooldown(database, 30));
            Assert.AreEqual(4433L, Cooldown(database, 630052));
            Assert.AreEqual(0L, database.Context.Database.SqlQueryRaw<long>(
                "SELECT COUNT(*) AS Value FROM creature_action WHERE action_id = 1 AND action_arg_id = 149 AND cooldown <> 4433").AsEnumerable().Single(),
                "every row of the shot");

            foreach (var sql in LightbenderChargedShot.Up())
                Assert.AreEqual(0, database.Context.Database.ExecuteSqlRaw(sql), "twice");

            migrator.Migrate(Previous);
            Assert.AreEqual(800L, Cooldown(database, 30));
            Assert.AreEqual(800L, Cooldown(database, 630052));
        }

        [TestMethod]
        public void OnMySqlTheRowsChangeAndChangeBack()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in LightbenderChargedShot.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in LightbenderChargedShot.Down())
                StringAssert.Contains(down, sql);
        }

        private static long Cooldown(OrdinaryLootTestDatabase database, uint id) =>
            database.Context.Database.SqlQueryRaw<long>($"SELECT cooldown AS Value FROM creature_action WHERE id = {id}").AsEnumerable().Single();

        /// <summary>One think of the sniper's fight: its shot, queued, with the to-hit roll made a hit.</summary>
        private static Missile Fire(WorldTestContext world, Creature sniper, Client player)
        {
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(sniper, player.Player.EntityId));
            sniper.Controller.ActionFighting.Opened = true;

            BehaviorManager.Instance.MapChannelThink(world.Map, Tick);

            var missile = world.Map.QueuedMissiles.Single(m => m.Source == sniper);
            missile.Missed = false;
            return missile;
        }

        /// <summary>
        /// A 10 m ridge two metres wide along x = 14 to 16, from z = 10 on: open ground between
        /// (0, 0) and (30, 0), the ridge between (0, 0) and (30, 30).
        /// </summary>
        private static CoverMesh Ridge()
        {
            const int columns = 101;
            var cover = CoverMesh.Build(Array.Empty<float>(), Array.Empty<int>());
            var heights = new float[columns * columns];

            for (var row = 55; row < columns; row++)
                heights[row * columns + 57] = 10f;

            cover.SetTerrain(-100, -100, 2, columns, columns, heights);
            return cover;
        }

        /// <summary>The client's actionArguments (1, 149) for as long as the test runs.</summary>
        private static IDisposable WeaponAttackData()
        {
            var actions = (Dictionary<ActionId, ActionInfo>)Actions.GetValue(AbilityManager.Instance);
            actions.TryGetValue(ActionId.WeaponAttack, out var previous);

            var info = new ActionInfo { ActionId = ActionId.WeaponAttack, Module = "weapons.baseweaponattack" };
            info.Levels[1] = new ActionLevelInfo { ActionId = ActionId.WeaponAttack, Level = 1, WindupMs = 500, RecoveryMs = 1028, MaxRange = 20 };
            info.Levels[149] = new ActionLevelInfo
            {
                ActionId = ActionId.WeaponAttack, Level = 149, WindupMs = LightbenderChargedShot.WindupMs,
                RecoveryMs = LightbenderChargedShot.RecoveryMs, MaxRange = 60
            };
            actions[ActionId.WeaponAttack] = info;

            return new Restore(() =>
            {
                if (previous != null)
                    actions[ActionId.WeaponAttack] = previous;
                else
                    actions.Remove(ActionId.WeaponAttack);
            });
        }

        private static Creature Spawn(WorldTestContext world)
        {
            var creature = new Creature
            {
                Name = "Light Bender Sniper",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = Vector3.Zero,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 7,
                AggroRange = 0,
                RunSpeed = 9,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            creature.HomePos.Position = creature.Position;
            creature.HomePos.MapContextid = creature.MapContextId;
            creature.Attributes[Attributes.Health] = CreatureHealth.NewAttribute(600);
            creature.Actions.Add(new CreatureAction
            {
                ActionId = ActionId.WeaponAttack, ActionArgId = 149, RangeMin = 0.5, RangeMax = 60,
                Cooldown = LightbenderChargedShot.Cooldown, MinDamage = 15, MaxDamage = 25
            });
            BehaviorManager.StartWandering(creature, false);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        private static Client Watch(WorldTestContext world, Vector3 at)
        {
            var client = world.CreateClient(x: at.X, z: at.Z);
            client.Player.Position = at;
            client.Player.State = CharacterState.Normal;
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100000, 100000, 100000, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            var seed = CellManager.Instance.GetCellSeed(at);
            client.Player.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            return client;
        }

        private sealed class Restore : IDisposable
        {
            private readonly Action _undo;

            public Restore(Action undo) => _undo = undo;

            public void Dispose() => _undo();
        }
    }
}
