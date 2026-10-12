using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Structures;

    /// <summary>
    /// A creature kept from its think - stunned, winding up, channelling, charging - is still in
    /// its fight. LastAgression, which AggressionTime (5 s) of ends a fight, used to grow through
    /// those thinks, so a chain of stuns longer than that had the creature give up its target and
    /// its hate table the moment it could act again.
    /// </summary>
    [TestClass]
    public class BusyCreatureAggressionTests
    {
        private const long Tick = 250;

        [TestMethod]
        public void AFightSurvivesAStunLongerThanTheAggressionTime()
        {
            using var world = new WorldTestContext();
            var enemy = Watch(world, x: 2);
            var creature = Spawn(world, x: 0);

            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(creature, enemy.Player.EntityId));
            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, creature.Controller.CurrentAction);

            // Stunned for longer than AggressionTime, think by think.
            Assert.IsTrue(Stuns.Apply(world.Map, creature, enemy.Player, Stuns.StunTypeId, 60_000, DamageType.Physical));

            var ticks = (int)(creature.AggressionTime * 2 / Tick);

            for (var tick = 0; tick < ticks; tick++)
                BehaviorManager.Instance.MapChannelThink(world.Map, Tick);

            Assert.IsTrue(Stuns.IsStunned(creature));
            Assert.IsTrue(creature.LastAgression <= creature.AggressionTime, "the stun did not count against the fight");

            // The stun ends: the next think is a fighting one, and it fights on.
            foreach (var stun in creature.ActiveEffects.Values.Where(effect => effect.IsStun).ToList())
                stun.ExpiresTick = 0;

            Assert.IsFalse(Stuns.IsStunned(creature));

            BehaviorManager.Instance.MapChannelThink(world.Map, Tick);

            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, creature.Controller.CurrentAction, "the fight was given up when the stun let go");
            Assert.AreEqual(enemy.Player.EntityId, creature.Controller.ActionFighting.TargetEntityId);
        }

        private static Creature Spawn(WorldTestContext world, float x)
        {
            var creature = new Creature
            {
                Name = "Patient",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(x, 0, 0),
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AggroRange = 0,
                RunSpeed = 6,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            creature.HomePos.Position = creature.Position;
            creature.HomePos.MapContextid = creature.MapContextId;
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
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
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
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
