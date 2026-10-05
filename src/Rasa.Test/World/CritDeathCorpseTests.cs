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
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    /// <summary>
    /// A Critical Death window opens on a creature with 1 to 8 percent of its health left, and both
    /// ways out of it - left to run out, or finished - kill the creature with that health still on
    /// it. Dead is at zero whoever does the killing: a body with health left was never put on the
    /// corpse clock, and was thought for as if it were alive.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CritDeathCorpseTests
    {
        private const int NearDeath = 5;

        [TestMethod]
        public void ACreatureLeftToDieInItsWindowIsACorpseAtZeroHealthAndGoesLikeAnyOther()
        {
            using var world = new WorldTestContext();
            // Not in a cell's client list, so the kills pay it nothing.
            var opener = world.CreateClient(x: 200);
            var watcher = Watch(world, x: 60);
            var normal = Spawn(world, "Ordinary", 0);
            var crit = Spawn(world, "LeftToDie", 10);

            // An ordinary kill: the damage brings the health to zero, then the kill.
            normal.Attributes[Attributes.Health].Current = 0;
            CreatureManager.Instance.HandleCreatureKill(world.Map, normal, opener.Player);

            OpenWindow(world, crit, opener.Player);
            WorldTestContext.Drain(watcher);

            // Nobody finishes it: the window runs out.
            CritDeathManager.PreDeathOf(crit).ExpiresTick = Environment.TickCount64 - 1;
            GameEffectManager.Instance.DoWork(world.Map, 500);

            AssertCorpseAtZero(crit, watcher);
            Assert.IsFalse(crit.CritKilled);

            Think(world, LootDispenserManager.EmptyCorpseMs);

            Assert.IsFalse(InWorld(world, normal), "The ordinary corpse is gone after an empty corpse's time.");
            Assert.IsFalse(InWorld(world, crit), "So is the one that died in its window.");
        }

        [TestMethod]
        public void ACreatureFinishedInItsWindowIsACorpseAtZeroHealthAndGoesLikeAnyOther()
        {
            using var world = new WorldTestContext();
            // In reach of the creature, alive, and - not being in a cell's client list - paid nothing for the kill.
            var finisher = world.CreateClient(x: 12);
            finisher.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            finisher.Player.Cells = CellsAt(world, finisher.Player.Position);
            var watcher = Watch(world, x: 60);
            var crit = Spawn(world, "Finished", 10);

            OpenWindow(world, crit, finisher.Player);

            // The melee press, its windup, and the finishing animation played out.
            CritDeathManager.Instance.RequestCritDeathFinish(finisher, new RequestCritDeathFinishPacket
            {
                ActionId = ActionId.CriticalDeathFinisher,
                ActionArgId = 1,
                TargetId = crit.EntityId
            });
            var press = world.Map.PerformRecovery.Single(action => action.ActionId == ActionId.CriticalDeathFinisher);
            world.Map.PerformRecovery.Remove(press);
            CritDeathManager.Instance.PerformRecovery(world.Map, press);
            Assert.IsNull(CritDeathManager.PreDeathOf(crit), "The window is taken off for the finishing animation.");
            Assert.AreEqual(CharacterState.Dying, crit.State);
            WorldTestContext.Drain(watcher);

            crit.ActiveEffects.Values.Single(effect => effect.TypeId == CritDeathManager.DeathTypeId).ExpiresTick =
                Environment.TickCount64 - 1;
            GameEffectManager.Instance.DoWork(world.Map, 500);

            AssertCorpseAtZero(crit, watcher);
            Assert.IsTrue(crit.CritKilled);

            Think(world, LootDispenserManager.EmptyCorpseMs);

            Assert.IsFalse(InWorld(world, crit));
        }

        [TestMethod]
        public void ADeadCreatureWithHealthLeftOnItIsStillACorpseToTheBehaviorWorker()
        {
            using var world = new WorldTestContext();
            var body = Spawn(world, "MarkedDead", 0);
            body.State = CharacterState.Dead;

            // A player within its aggro range, alive and in its cells.
            var near = Watch(world, x: 4);
            near.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);

            Think(world, 6000);

            Assert.AreEqual(6000L, body.Controller.DeadTime, "The corpse clock runs.");
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, body.Controller.CurrentAction);
            Assert.AreEqual(0, body.Hate.Count);

            Think(world, LootDispenserManager.EmptyCorpseMs - 6000);

            Assert.IsFalse(InWorld(world, body));
        }

        private static void AssertCorpseAtZero(Creature creature, Client watcher)
        {
            Assert.AreEqual(CharacterState.Dead, creature.State);
            Assert.AreEqual(0, creature.Attributes[Attributes.Health].Current);
            Assert.AreEqual(0, creature.Attributes[Attributes.Health].RefreshAmount);
            Assert.AreEqual(0, creature.Attributes[Attributes.Armor].Current);
            Assert.AreEqual(0, creature.Attributes[Attributes.Armor].RefreshAmount);

            // Told to the clients, and before the state change, as the damage paths do it.
            var sent = WorldTestContext.Drain(watcher).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Where(message => message.EntityId == creature.EntityId).Select(message => message.Packet).ToList();
            var health = sent.FindIndex(packet => packet is UpdateHealthPacket);
            var state = sent.FindIndex(packet => packet is StateChangePacket);
            Assert.IsTrue(health >= 0, "UpdateHealth is sent.");
            Assert.AreEqual(0, ((UpdateHealthPacket)sent[health]).Health.Current);
            Assert.IsTrue(state > health, "And then the state change.");
        }

        /// <summary>Stunned at five percent by a player: the window opens.</summary>
        private static void OpenWindow(WorldTestContext world, Creature creature, Manifestation opener)
        {
            Stuns.Apply(world.Map, creature, opener, Stuns.StunTypeId, 3000, DamageType.Physical);

            Assert.AreEqual(CharacterState.Dying, creature.State, "The window opens.");
            Assert.AreEqual(NearDeath, creature.Attributes[Attributes.Health].Current);
        }

        private static void Think(WorldTestContext world, long milliseconds)
        {
            for (var elapsed = 0L; elapsed < milliseconds; elapsed += 250)
                BehaviorManager.Instance.MapChannelThink(world.Map, 250);
        }

        private static bool InWorld(WorldTestContext world, Creature creature) =>
            world.Map.MapCellInfo.Cells.Values.Any(cell => cell.CreatureList.Contains(creature));

        /// <summary>A hostile creature at five percent of its health, with armour, in the map's cells.</summary>
        private static Creature Spawn(WorldTestContext world, string name, float x)
        {
            var creature = new Creature
            {
                Name = name,
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(x, 0, 0),
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, NearDeath, 2, 5);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 100, 100, 40, 2, 5);
            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionWander;
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellsAt(world, creature.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        /// <summary>A client standing in the map's cells, so what is sent about the creatures near it reaches it.</summary>
        private static Client Watch(WorldTestContext world, float x)
        {
            var client = world.CreateClient(x: x);
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
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
