using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Navigation;
    using Rasa.Structures;

    /// <summary>
    /// The Bane's fire stops at walls and hills: a shot lands only on a target its shooter still
    /// sees as it lands, and what goes out round a point - a cone, a ring, a bomb, a shriek, an
    /// arc of lightning - reaches only those in the clear of it (Cover.Reaches).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class BaneLineOfFireTests
    {
        private const long Tick = 250;

        private static readonly FieldInfo Actions = typeof(AbilityManager).GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic);

        [TestMethod]
        public void AShotAtATargetThatGoesBehindAHillAsItFliesMissesAndDoesNoDamage()
        {
            using var world = new WorldTestContext();
            using var _ = RifleData();
            world.Map.Cover = Ridge();
            var player = Watch(world, new Vector3(30, 0, 0));
            var bane = Spawn(world, Vector3.Zero);

            try
            {
                var missile = Fire(world, bane, player);

                // Behind the ridge before it lands, still in range. It used to land a tenth of
                // itself through the hill (Cover.MinModifier).
                player.Player.Position = new Vector3(30, 0, 30);
                Assert.IsFalse(BehaviorManager.Sees(world.Map, bane, player.Player));

                MissileManager.Instance.DoWork(world.Map, Tick);

                Assert.IsEmpty(missile.Args.HitEntities);
                CollectionAssert.AreEqual(new[] { player.Player.EntityId }, missile.Args.MisstEntities.ToList());
                CollectionAssert.AreEqual(new[] { MissileManager.MissTypeMiss }, missile.Args.Missdata.ToList());
                Assert.AreEqual(100000, player.Player.Attributes[Attributes.Health].Current, "no damage through the hill");
            }
            finally
            {
                world.Map.QueuedMissiles.Clear();
                world.Map.Cover = null;
            }
        }

        [TestMethod]
        public void AShotAtATargetInTheOpenStillLands()
        {
            using var world = new WorldTestContext();
            using var _ = RifleData();
            world.Map.Cover = Ridge();
            var player = Watch(world, new Vector3(30, 0, 0));
            var bane = Spawn(world, Vector3.Zero);

            try
            {
                var missile = Fire(world, bane, player);

                MissileManager.Instance.DoWork(world.Map, Tick);

                CollectionAssert.Contains(missile.Args.HitEntities, player.Player.EntityId);
                Assert.IsEmpty(missile.Args.MisstEntities);
                Assert.IsTrue(player.Player.Attributes[Attributes.Health].Current < 100000);
            }
            finally
            {
                world.Map.QueuedMissiles.Clear();
                world.Map.Cover = null;
            }
        }

        [TestMethod]
        public void ARingOrAConeDoesNotGoThroughAWall()
        {
            using var world = new WorldTestContext();
            var bane = Spawn(world, new Vector3(10, 0, 0));
            var open = Watch(world, new Vector3(6, 0, 0)).Player;
            var behind = Watch(world, new Vector3(14, 0, 0)).Player;
            world.Map.Cover = Wall();

            try
            {
                var ring = new CreatureArea(0, 0, 8, 0);
                var caught = CreatureAreaAttacks.PlayersCaught(world.Map, bane, ring, null);

                CollectionAssert.Contains(caught, open);
                CollectionAssert.DoesNotContain(caught, behind, "a ring round the creature, through the wall");

                var aroundTarget = new CreatureArea(0, 0, 0, 10);
                caught = CreatureAreaAttacks.PlayersCaught(world.Map, bane, aroundTarget, null, new Vector3(8, 0, 0));

                CollectionAssert.Contains(caught, open);
                CollectionAssert.DoesNotContain(caught, behind, "a ring round a point, through the wall");

                var cone = new CreatureArea(60, 10, 0, 0);
                caught = CreatureAreaAttacks.PlayersCaught(world.Map, bane, cone, null, new Vector3(20, 0, 0));

                CollectionAssert.DoesNotContain(caught, behind, "a cone, through the wall");

                world.Map.Cover = null;
                caught = CreatureAreaAttacks.PlayersCaught(world.Map, bane, ring, null);
                CollectionAssert.Contains(caught, behind, "no wall");
            }
            finally
            {
                world.Map.Cover = null;
            }
        }

        [TestMethod]
        public void ABlastOrAShriekDoesNotGoThroughAWallOrAHill()
        {
            using var world = new WorldTestContext();
            var bane = Spawn(world, new Vector3(10, 0, 0));
            var open = Watch(world, new Vector3(6, 0, 0)).Player;
            var behind = Watch(world, new Vector3(14, 0, 0)).Player;
            world.Map.Cover = Wall();

            try
            {
                var caught = CreatureBombs.Caught(world.Map, bane, bane.Position, 8);

                CollectionAssert.Contains(caught, open);
                CollectionAssert.DoesNotContain(caught, behind, "the wall");

                // Over a ridge: a blast at (30, 0, 10) on its near side, a player on its far side.
                world.Map.Cover = Ridge();
                var over = Watch(world, new Vector3(30, 0, 18)).Player;
                var near = Watch(world, new Vector3(25, 0, 10)).Player;
                caught = CreatureBombs.Caught(world.Map, bane, new Vector3(30, 0, 10), 10);

                CollectionAssert.Contains(caught, near);
                CollectionAssert.DoesNotContain(caught, over, "the hill");
            }
            finally
            {
                world.Map.Cover = null;
            }
        }

        [TestMethod]
        public void LightningDoesNotArcThroughAWall()
        {
            using var world = new WorldTestContext();
            var bane = Spawn(world, new Vector3(0, 0, 0));
            var target = Watch(world, new Vector3(10, 0, 0)).Player;
            var open = Watch(world, new Vector3(10, 0, 3)).Player;
            var behind = Watch(world, new Vector3(12.5f, 0, 0)).Player;
            world.Map.Cover = Wall();

            try
            {
                // The one behind the wall is the nearer, and the arc goes to the one it reaches.
                var arcs = CreatureLightning.ArcTo(world.Map, bane, target, 8);

                CollectionAssert.AreEqual(new[] { open }, arcs);
            }
            finally
            {
                world.Map.Cover = null;
            }
        }

        /// <summary>A wall 4 m high across x = 11.75, from z = -10 to 10.</summary>
        private static CoverMesh Wall() =>
            CoverMesh.Build(new float[] { 11.75f, 0, -10, 11.75f, 4, -10, 11.75f, 4, 10, 11.75f, 0, 10 }, new[] { 0, 1, 2, 0, 2, 3 });

        /// <summary>
        /// Two 10 m ridges two metres wide: one along x = 14 to 16 from z = 10 on, one along
        /// z = 14 to 16 from x = 20 on. Open ground between (0, 0) and (30, 0) and between
        /// (30, 10) and (25, 10); a ridge between (0, 0) and (30, 30), and between (30, 10) and
        /// (30, 18).
        /// </summary>
        private static CoverMesh Ridge()
        {
            const int columns = 101;
            var cover = CoverMesh.Build(Array.Empty<float>(), Array.Empty<int>());
            var heights = new float[columns * columns];

            for (var row = 55; row < columns; row++)
                heights[row * columns + 57] = 10f;

            for (var column = 60; column < columns; column++)
                heights[57 * columns + column] = 10f;

            cover.SetTerrain(-100, -100, 2, columns, columns, heights);
            return cover;
        }

        /// <summary>One think of the Bane's fight: its rifle shot, queued, with the to-hit roll made a hit.</summary>
        private static Missile Fire(WorldTestContext world, Creature bane, Client player)
        {
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(bane, player.Player.EntityId));
            bane.Controller.ActionFighting.Opened = true;

            BehaviorManager.Instance.MapChannelThink(world.Map, Tick);

            var missile = world.Map.QueuedMissiles.Single(m => m.Source == bane);
            missile.Missed = false;
            return missile;
        }

        /// <summary>The client's actionArguments (1, 1), a rifle's shot, for as long as the test runs.</summary>
        private static IDisposable RifleData()
        {
            var actions = (Dictionary<ActionId, ActionInfo>)Actions.GetValue(AbilityManager.Instance);
            actions.TryGetValue(ActionId.WeaponAttack, out var previous);

            var info = new ActionInfo { ActionId = ActionId.WeaponAttack, Module = "weapons.baseweaponattack" };
            info.Levels[1] = new ActionLevelInfo { ActionId = ActionId.WeaponAttack, Level = 1, WindupMs = 500, RecoveryMs = 1028, MaxRange = 60 };
            actions[ActionId.WeaponAttack] = info;

            return new Restore(() =>
            {
                if (previous != null)
                    actions[ActionId.WeaponAttack] = previous;
                else
                    actions.Remove(ActionId.WeaponAttack);
            });
        }

        private static Creature Spawn(WorldTestContext world, Vector3 at)
        {
            var creature = new Creature
            {
                Name = "Thrax Soldier",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = at,
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
                ActionId = ActionId.WeaponAttack, ActionArgId = 1, RangeMin = 0.5, RangeMax = 60,
                Cooldown = 800, MinDamage = 15, MaxDamage = 25
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
