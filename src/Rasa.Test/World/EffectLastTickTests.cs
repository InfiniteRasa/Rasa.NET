using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    /// <summary>
    /// A damage or heal over time whose first tick is an interval in ticks at N, 2N, ... D: the
    /// last one falls on its expiry, and the worker tested expiry first, so it never ran - Ruin,
    /// Scourge, Reconstruction and Called Shot each delivered one tick fewer than their duration
    /// and interval give (BR-177). One that ticks at once (a medpack: 0, N, ... D - N) already had
    /// all of its ticks, and gets no extra one.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class EffectLastTickTests
    {
        private const int Interval = 1000;

        [TestMethod]
        public void ADamageOverTimeDeliversItsTickOnExpiry()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world);
            var creature = Spawn(world);
            var now = Environment.TickCount64;
            var effect = Dot(world, shooter, now + Interval, now + 3 * Interval);

            GameEffectManager.Instance.Attach(world.Map, creature, effect);
            Assert.IsTrue(effect.FirstTickDelayed);
            Sent(shooter);

            Assert.AreEqual(3, Run(world, shooter, creature, effect), "three seconds, a tick a second from the first second");
            Assert.IsFalse(creature.ActiveEffects.ContainsKey(effect.EffectId), "and gone");
            Assert.AreEqual(10000 - 3 * 10, creature.Attributes[Attributes.Health].Current);
        }

        [TestMethod]
        public void AnEffectThatTicksAtOnceHasNoExtraTickOnExpiry()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world);
            var creature = Spawn(world);
            var now = Environment.TickCount64;
            var effect = Dot(world, shooter, now, now + 3 * Interval);

            GameEffectManager.Instance.Attach(world.Map, creature, effect);
            Assert.IsFalse(effect.FirstTickDelayed);
            Sent(shooter);

            Assert.AreEqual(3, Run(world, shooter, creature, effect), "at 0, 1 and 2 seconds, and not at 3");
            Assert.IsFalse(creature.ActiveEffects.ContainsKey(effect.EffectId));
        }

        [TestMethod]
        public void ATickDueAfterTheExpiryIsNotItsOwn()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world);
            var creature = Spawn(world);
            var now = Environment.TickCount64;

            // Two and a half seconds, a tick a second: at 1 and 2, the next due at 3 is past it.
            var effect = Dot(world, shooter, now + Interval, now + 5 * Interval / 2);

            GameEffectManager.Instance.Attach(world.Map, creature, effect);
            Sent(shooter);

            Assert.AreEqual(2, Run(world, shooter, creature, effect));
        }

        [TestMethod]
        public void OnlyDamageAndHealOverTimeTickAtExpiry()
        {
            var now = Environment.TickCount64;
            GameEffect Due() => new GameEffect { TickIntervalMs = Interval, NextTickTick = now, ExpiresTick = now, FirstTickDelayed = true, TickDamageMax = 5 };

            Assert.IsTrue(GameEffectManager.TicksAtExpiry(Due()));
            Assert.IsTrue(GameEffectManager.TicksAtExpiry(new GameEffect { TickIntervalMs = Interval, NextTickTick = now, ExpiresTick = now, FirstTickDelayed = true, TickHealMax = 5 }));

            var started = Due();
            started.FirstTickDelayed = false;
            Assert.IsFalse(GameEffectManager.TicksAtExpiry(started), "ticked at once");

            var aura = Due();
            aura.AuraRadius = 10;
            Assert.IsFalse(GameEffectManager.TicksAtExpiry(aura));

            var scripted = Due();
            scripted.OnTick = (_, _, _) => { };
            Assert.IsFalse(GameEffectManager.TicksAtExpiry(scripted));

            var drain = Due();
            drain.AdrenalineDrainPercentPerSecond = 1;
            Assert.IsFalse(GameEffectManager.TicksAtExpiry(drain));

            var plain = Due();
            plain.TickDamageMax = 0;
            Assert.IsFalse(GameEffectManager.TicksAtExpiry(plain), "nothing to deliver");

            var late = Due();
            late.NextTickTick = now + GameEffectManager.ExpiryTickToleranceMs + 1;
            Assert.IsFalse(GameEffectManager.TicksAtExpiry(late));

            var skew = Due();
            skew.NextTickTick = now + 2;
            Assert.IsTrue(GameEffectManager.TicksAtExpiry(skew), "the clock read twice when it was made");
        }

        /// <summary>
        /// The worker's passes, a second apart, until the effect is gone: each pass moves the
        /// effect's clock back by a second instead of waiting one. Returns the ticks the client saw.
        /// </summary>
        private static int Run(WorldTestContext world, Client watcher, Creature creature, GameEffect effect)
        {
            var ticks = 0;

            for (var pass = 0; pass < 10 && creature.ActiveEffects.ContainsKey(effect.EffectId); pass++)
            {
                GameEffectManager.Instance.DoWork(world.Map, 100);
                ticks += Sent(watcher).OfType<GameEffectTickPacket>().Count(tick => tick.EffectId == effect.EffectId);

                effect.NextTickTick -= Interval;
                effect.ExpiresTick -= Interval;
            }

            return ticks;
        }

        private static GameEffect Dot(WorldTestContext world, Client shooter, long firstTick, long expires) => new GameEffect
        {
            TypeId = 207,
            EffectId = GameEffectManager.Instance.NextEffectId(world.Map),
            EffectLevel = 1,
            SourceId = shooter.Player.EntityId,
            Source = shooter.Player,
            SourceLevel = 1,
            TickDamageMin = 10,
            TickDamageMax = 10,
            TickDamageType = DamageType.Physical,
            TickIntervalMs = Interval,
            NextTickTick = firstTick,
            ExpiresTick = expires
        };

        private static List<PythonPacket> Sent(Client client) =>
            WorldTestContext.Drain(client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Select(message => message.Packet)
                .ToList();

        private static Creature Spawn(WorldTestContext world)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(0, 0, -5),
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 10000, 10000, 10000, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionWander;
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellsAt(world, creature.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        private static Client Watch(WorldTestContext world)
        {
            var client = world.CreateClient();
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Cells = CellsAt(world, client.Player.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            client.Player.State = CharacterState.Normal;
            foreach (var attribute in new[] { Attributes.Health, Attributes.Armor, Attributes.Power, Attributes.Regen })
                client.Player.Attributes[attribute] = new ActorAttributes(attribute, 100, 100, 100, 0, 0);
            WorldTestContext.Drain(client);
            return client;
        }

        private static uint[,] CellsAt(WorldTestContext world, Vector3 position)
        {
            var seed = CellManager.Instance.GetCellSeed(position);
            return CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
        }
    }
}
