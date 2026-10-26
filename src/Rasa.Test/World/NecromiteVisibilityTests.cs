using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;
using Rasa.Structures;

namespace Rasa.Test.World
{
    [TestClass, DoNotParallelize]
    public class NecromiteVisibilityTests
    {
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void SelfDestructEffectsBelongToTheNecromiteAndDamageStillReachesItsVictims(bool defender)
        {
            using var world = new WorldTestContext();
            using var action = new ActionScope();
            var player = PlayerDeathTests.Player(world, 3, 0);
            var near = PlayerDeathTests.Player(world, -4, 0);
            var far = PlayerDeathTests.Player(world, 8, 0);
            var source = Spawn(world, 0, TargetCategory.Hostile);
            var soldier = defender ? Spawn(world, 2, TargetCategory.Friendly) : null;
            try
            {
                var missile = new Missile { ActionId = CreatureBombs.NecromiteSelfDestruct, ActionArgId = 1,
                    Source = source, CreatureAction = Attack(CreatureBombs.NecromiteSelfDestruct) };
                if (defender)
                    CreatureEffectAttacks.OnCreatureHit(world.Map, source, soldier, missile);
                else
                    CreatureEffectAttacks.OnHit(world.Map, source, player.Player, missile);

                Assert.AreEqual(CharacterState.Dead, source.State, "The Necromite is spent once it detonates.");
                var bomb = source.ActiveEffects.Values.Single(e => e.TypeId == CreatureBombs.NecromiteSelfDestructTypeId);
                Assert.AreSame(source, bomb.Holder);
                Assert.IsTrue(CreatureBombs.IsDeathBomb(bomb));
                Assert.IsNull(CreatureBombs.GroundBlast(world.Map, source, player.Player,
                    Attack(CreatureBombs.NecromiteSelfDestruct), action.Level), "A spent Necromite cannot rearm itself.");
                Assert.IsFalse(player.Player.ActiveEffects.Values.Any(e => e.TypeId == bomb.TypeId));
                if (soldier != null) Assert.IsFalse(soldier.ActiveEffects.Values.Any(e => e.TypeId == bomb.TypeId));
                var attaches = Drain(player).Where(m => m.Packet is GameEffectAttachedPacket p && p.EffectTypeId == bomb.TypeId).ToArray();
                Assert.AreEqual(1, attaches.Length);
                Assert.AreEqual(source.EntityId, attaches[0].EntityId, "The native opacity package must never attach to a player/defender.");
                Assert.IsTrue(((GameEffectAttachedPacket)attaches[0].Packet).Announced);

                GameEffectManager.Instance.DoWork(world.Map, 500);
                Assert.IsTrue(source.ActiveEffects.ContainsKey(bomb.EffectId), "The fuse survives the dead-actor effect pass.");
                CreatureBombs.Worker(world.Map);
                Assert.AreEqual(1000, player.Player.Attributes[Attributes.Health].Current, "The 500 ms fuse has not elapsed.");
                MakeDue(world.Map);
                CreatureBombs.Worker(world.Map);
                Assert.IsTrue(player.Player.Attributes[Attributes.Health].Current < 1000);
                Assert.IsTrue(near.Player.Attributes[Attributes.Health].Current < 1000, "The blast is centered on the Necromite, not its initial target.");
                Assert.AreEqual(1000, far.Player.Attributes[Attributes.Health].Current);
                if (soldier != null) Assert.IsTrue(soldier.Attributes[Attributes.Health].Current < 1000);
                Assert.IsFalse(source.ActiveEffects.ContainsKey(bomb.EffectId));
                var explosions = Drain(player).Where(m => m.Packet is GameEffectAnnounceDamagePacket p && p.MethodName == "DoExplosion").ToArray();
                Assert.AreEqual(1, explosions.Length);
                Assert.AreEqual(source.EntityId, explosions[0].EntityId);
                var health = player.Player.Attributes[Attributes.Health].Current;
                CreatureBombs.Worker(world.Map);
                Assert.AreEqual(health, player.Player.Attributes[Attributes.Health].Current, "A spent bomb cannot deal damage twice.");
            }
            finally { Remove(source); if (soldier != null) Remove(soldier); }
        }

        [TestMethod]
        public void MovingAwayDuringTheFuseEscapesTheBlastAndTheEffectHasAnExpiryBackstop()
        {
            using var world = new WorldTestContext();
            using var action = new ActionScope();
            var player = PlayerDeathTests.Player(world, 3, 0);
            var source = Spawn(world, 0, TargetCategory.Hostile);
            try
            {
                var bomb = CreatureBombs.GroundBlast(world.Map, source, player.Player, Attack(CreatureBombs.NecromiteSelfDestruct), action.Level);
                player.SetWorldPosition(new Vector3(10, 0, 0), 0);
                MakeDue(world.Map);
                CreatureBombs.Worker(world.Map);
                Assert.AreEqual(1000, player.Player.Attributes[Attributes.Health].Current);
                Assert.IsFalse(source.ActiveEffects.ContainsKey(bomb.EffectId));
                var orphan = new GameEffect { TypeId = CreatureBombs.NecromiteSelfDestructTypeId,
                    EffectId = GameEffectManager.Instance.NextEffectId(world.Map), ExpiresTick = Environment.TickCount64 - 1 };
                GameEffectManager.Instance.Attach(world.Map, source, orphan);
                GameEffectManager.Instance.DoWork(world.Map, 500);
                Assert.IsFalse(source.ActiveEffects.ContainsKey(orphan.EffectId));
            }
            finally { Remove(source); }
        }

        [TestMethod]
        [DataRow((int)CreatureBombs.LinkerGroundBlast)]
        [DataRow((int)CreatureBombs.PredatorMissile)]
        public void OtherTargetedBombsStillAttachToTheirTarget(int id)
        {
            using var world = new WorldTestContext();
            var player = PlayerDeathTests.Player(world, 3, 0);
            var source = Spawn(world, 0, TargetCategory.Hostile);
            try
            {
                var info = new ActionLevelInfo { ActionId = (ActionId)id, Level = 1 };
                info.Properties[AbilityProperty.EffectRadius] = 5;
                var bomb = CreatureBombs.GroundBlast(world.Map, source, player.Player, Attack((ActionId)id), info);
                Assert.AreSame(player.Player, bomb.Holder);
                Assert.AreEqual(CharacterState.Idle, source.State);
                Assert.IsFalse(bomb.AnnounceOnAttach);
                CreatureBombs.Worker(world.Map);
                Assert.IsFalse(player.Player.ActiveEffects.ContainsKey(bomb.EffectId));
            }
            finally { Remove(source); }
        }

        private static CreatureAction Attack(ActionId id) => new CreatureAction
            { ActionId = id, ActionArgId = 1, MinDamage = 50, MaxDamage = 50 };

        private static List<CallMethodMessage> Drain(Rasa.Game.Client client) =>
            WorldTestContext.Drain(client).Select(p => p.Message).OfType<CallMethodMessage>().ToList();

        private static void MakeDue(MapChannel map)
        {
            var pending = (IEnumerable)typeof(CreatureBombs).GetField("Bombs", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            foreach (var bomb in pending)
                if (ReferenceEquals(bomb.GetType().GetField("MapChannel").GetValue(bomb), map))
                    bomb.GetType().GetField("GoesOffAt").SetValue(bomb, Environment.TickCount64 - 1);
        }

        private static Creature Spawn(WorldTestContext world, float x, TargetCategory category)
        {
            var creature = new Creature { Name = "Necromite regression", Position = new Vector3(x, 0, 0),
                MapContextId = world.Map.MapInfo.MapContextId, RuntimeMapChannel = world.Map,
                EntityClass = EntityClasses.HumanBaseMale, TargetCategory = category, State = CharacterState.Idle,
                Level = 1, AppearanceData = new Dictionary<EquipmentData, AppearanceData>(), Actions = new List<CreatureAction>() };
            // Reward persistence is outside this fixture; keep the normal kill path without XP storage.
            creature.ExtraFlags.Add(CreatureFlag.XpModifier0None);
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            if (category == TargetCategory.Hostile)
            {
                // Match a summoned Necromite's no-loot lifecycle without loading unrelated world content.
                var type = typeof(CreatureSummons).GetNestedType("Summoned", BindingFlags.NonPublic);
                var summon = Activator.CreateInstance(type, true);
                type.GetField("Creature").SetValue(summon, creature);
                type.GetField("MapChannel").SetValue(summon, world.Map);
                ((IList)typeof(CreatureSummons).GetField("Summons", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)).Add(summon);
            }
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        private static void Remove(Creature creature)
        {
            var summons = (IList)typeof(CreatureSummons).GetField("Summons", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            foreach (var summon in summons.Cast<object>().ToArray())
                if (ReferenceEquals(summon.GetType().GetField("Creature").GetValue(summon), creature)) summons.Remove(summon);
            EntityManager.Instance.UnregisterCreature(creature.EntityId);
            EntityManager.Instance.UnregisterActor(creature.EntityId);
            EntityManager.Instance.UnregisterEntity(creature.EntityId);
        }

        private sealed class ActionScope : IDisposable
        {
            private readonly Dictionary<ActionId, ActionInfo> _actions = (Dictionary<ActionId, ActionInfo>)typeof(AbilityManager)
                .GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(AbilityManager.Instance);
            private readonly ActionInfo _previous;
            public ActionLevelInfo Level { get; } = new ActionLevelInfo { ActionId = CreatureBombs.NecromiteSelfDestruct, Level = 1 };
            public ActionScope()
            {
                _actions.TryGetValue(CreatureBombs.NecromiteSelfDestruct, out _previous);
                Level.Properties[AbilityProperty.DelayTimeMs] = 500;
                Level.Properties[AbilityProperty.EffectRadius] = 5;
                var info = new ActionInfo { ActionId = CreatureBombs.NecromiteSelfDestruct, Module = CreatureEffectAttacks.NecromiteSelfDestructModule };
                info.Levels[1] = Level;
                _actions[info.ActionId] = info;
            }
            public void Dispose()
            {
                if (_previous == null) _actions.Remove(CreatureBombs.NecromiteSelfDestruct);
                else _actions[CreatureBombs.NecromiteSelfDestruct] = _previous;
            }
        }
    }
}
