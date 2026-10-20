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
    using Rasa.Structures;

    /// <summary>
    /// A Linker fed by another has its boost looked after by the map it is on (CreatureBuffs.Worker).
    /// The fed were picked out by context id, so every copy of the map - a squad's instance, a
    /// private one - refreshed every copy's: the boost was attached and ended through another
    /// copy's effect ids and players.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class LinkerChannelMapCopyTests
    {
        [TestMethod]
        public void AFedLinkersBoostIsKeptOnlyByItsOwnCopyOfTheMap()
        {
            using var world = new WorldTestContext();
            var fed = Spawn(world, 0);
            var linker = Spawn(world, 3);
            var copy = new MapChannel { MapInfo = world.Map.MapInfo, ClientList = new List<Client>(), PlayerLimit = 128 };

            try
            {
                CreatureBuffs.Feed(fed, linker, Environment.TickCount64 + CreatureBuffs.ChannelBoostMs);

                // Another copy of the same map leaves it alone.
                CreatureBuffs.Worker(copy);
                Assert.IsNull(Boost(fed), "not given its boost by another copy of the map");

                // Its own gives it the boost.
                CreatureBuffs.Worker(world.Map);
                Assert.AreEqual(CreatureBuffs.ChannelBoostFor(1), Boost(fed)?.DamageDealtPercent);

                // And another copy does not end it.
                CreatureBuffs.Worker(copy);
                Assert.IsNotNull(Boost(fed), "not ended by another copy of the map");
            }
            finally
            {
                foreach (var creature in new[] { fed, linker })
                    CellManager.Instance.RemoveCreatureFromWorld(world.Map, creature);
            }

            // Out of the world, nothing of the channel is kept: put back, it is fed by nobody.
            Assert.AreEqual(0, CreatureBuffs.ChannelersOf(fed));
        }

        private static GameEffect Boost(Creature creature) =>
            creature.ActiveEffects.Values.FirstOrDefault(effect => effect.ServerOnly && effect.ActionId == CreatureBuffs.LinkerChannel);

        private static Creature Spawn(WorldTestContext world, float x)
        {
            var creature = new Creature
            {
                Name = "Linker",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(x, 0, 5),
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }
    }
}
