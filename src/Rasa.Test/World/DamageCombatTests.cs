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

    // Only a missile put the player it hit in combat (MissileManager). Everything else that does
    // damage - a creature's bomb, miasma or lightning, a damage-over-time tick, constant fire, a
    // player's own blasts - goes through ActorManager.Damage, which marked nobody: a player could
    // stand in a Bane's miasma at full regeneration, out of combat, and log out in the middle of
    // it. A hit from someone now puts both ends in combat; the world's own damage, with no
    // source, marks nobody.
    [TestClass]
    [DoNotParallelize]
    public class DamageCombatTests
    {
        [TestMethod]
        public void ACreaturesHitThatIsNotAMissilePutsThePlayerInCombat()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            var creature = Spawn(world);

            Assert.AreEqual(10, ActorManager.Instance.Damage(world.Map, client.Player, 10, creature, DamageType.Fire));

            AssertInCombat(client);
        }

        [TestMethod]
        public void ADamageOverTimeTickPutsThePlayerInCombat()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            var creature = Spawn(world);
            var now = Environment.TickCount64;

            GameEffectManager.Instance.Attach(world.Map, client.Player, new GameEffect
            {
                TypeId = 207,
                EffectId = GameEffectManager.Instance.NextEffectId(world.Map),
                EffectLevel = 1,
                SourceId = creature.EntityId,
                Source = creature,
                SourceLevel = 1,
                TickDamageMin = 10,
                TickDamageMax = 10,
                TickDamageType = DamageType.Physical,
                TickIntervalMs = 1000,
                NextTickTick = now,
                ExpiresTick = now + 5000
            });
            WorldTestContext.Drain(client);
            Assert.IsFalse(client.Player.InCombat, "attached, not yet ticked");

            GameEffectManager.Instance.DoWork(world.Map, 100);

            Assert.AreEqual(90, client.Player.Attributes[Attributes.Health].Current, "the tick landed");
            AssertInCombat(client);
        }

        [TestMethod]
        public void APlayersDamageThatIsNotAMissilePutsThemInCombat()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            var creature = Spawn(world);

            Assert.AreEqual(10, ActorManager.Instance.Damage(world.Map, creature, 10, client.Player, DamageType.Electrical));

            AssertInCombat(client);
        }

        [TestMethod]
        public void DamageWithNoSourceIsNoFight()
        {
            using var world = new WorldTestContext();
            var client = Player(world);

            // Lava, a tesla coil, a placed blast.
            Assert.AreEqual(10, ActorManager.Instance.Damage(world.Map, client.Player, 10, null, DamageType.Fire));

            Assert.IsFalse(client.Player.InCombat);
            Assert.IsFalse(Sent(client).OfType<PlayerEnteredCombatPacket>().Any());
        }

        [TestMethod]
        public void AnotherHitKeepsThemInCombatLonger()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            var creature = Spawn(world);

            ActorManager.Instance.Damage(world.Map, client.Player, 10, creature);
            client.Player.CombatExpiresAt = Environment.TickCount64 + 1;
            WorldTestContext.Drain(client);

            ActorManager.Instance.Damage(world.Map, client.Player, 10, creature);

            Assert.IsTrue(client.Player.InCombat);
            Assert.IsTrue(client.Player.CombatExpiresAt > Environment.TickCount64 + CombatRegen.CombatTimeoutMs - 1000, "the clock starts again");
            Assert.IsFalse(Sent(client).OfType<PlayerEnteredCombatPacket>().Any(), "already in combat: not told again");
        }

        #region Fixture

        private static void AssertInCombat(Client client)
        {
            Assert.IsTrue(client.Player.InCombat);
            Assert.IsTrue(client.Player.CombatExpiresAt > Environment.TickCount64, "for a while yet");
            Assert.AreEqual(1, Sent(client).OfType<PlayerEnteredCombatPacket>().Count(), "and told");
        }

        private static Client Player(WorldTestContext world)
        {
            var client = world.CreateClient();
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Cells = CellsAt(world, client.Player.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            client.Player.State = CharacterState.Normal;
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            client.Player.Attributes[Attributes.Power] = new ActorAttributes(Attributes.Power, 100, 100, 100, 0, 0);
            client.Player.Attributes[Attributes.Regen] = new ActorAttributes(Attributes.Regen, 100, 100, 100, 0, 0);
            WorldTestContext.Drain(client);
            return client;
        }

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

        private static uint[,] CellsAt(WorldTestContext world, Vector3 position)
        {
            var seed = CellManager.Instance.GetCellSeed(position);
            return CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
        }

        private static List<PythonPacket> Sent(Client client) =>
            WorldTestContext.Drain(client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Select(message => message.Packet)
                .ToList();

        #endregion
    }
}
