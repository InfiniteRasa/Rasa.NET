using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    /// <summary>
    /// Outpost Commander Rogers's white glow (CreatureHalos): LT_EFFECT at level 1, whose FX is
    /// vfx_enemy_halo, on him as he comes into the world and on no one else.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureHaloTests
    {
        private const uint Rogers = 100;

        [TestMethod]
        public void RogersComesIntoTheWorldWithTheHaloAndOthersDoNot()
        {
            using var world = new WorldTestContext();
            var rogers = Placed(world, Rogers, new Vector3(5, 0, 5));
            var trooper = Placed(world, 101, new Vector3(8, 0, 5));

            var halo = rogers.ActiveEffects.Values.Single(e => e.TypeId == CreatureHalos.TypeId);
            Assert.AreEqual(1L, (long)halo.EffectLevel, "the level its FX is at");
            Assert.IsFalse(halo.HasDuration, "for as long as he is there");
            Assert.IsFalse(halo.AllowDetach, "the client's LtEffect cannot be taken off");
            Assert.IsFalse(CreatureHalos.HasHalo(trooper));

            // Coming into the world again (a respawn into the same object) does not put on a second.
            CreatureHalos.Put(world.Map, rogers);
            Assert.AreEqual(1, rogers.ActiveEffects.Values.Count(e => e.TypeId == CreatureHalos.TypeId));
        }

        /// <summary>Whoever meets him after he came in is shown it with him, announced, so that its FX plays.</summary>
        [TestMethod]
        public void AnyoneWhoMeetsHimLaterSeesIt()
        {
            using var world = new WorldTestContext();
            var rogers = Placed(world, Rogers, new Vector3(5, 0, 5));
            var viewer = world.CreateClient();
            WorldTestContext.Drain(viewer);

            CreatureManager.Instance.CellIntroduceCreaturesToClient(viewer, new() { rogers });

            var attached = WorldTestContext.Drain(viewer).Select(p => p.Message).OfType<CallMethodMessage>()
                .Where(c => c.EntityId == rogers.EntityId).Select(c => c.Packet).OfType<GameEffectAttachedPacket>()
                .Single(p => p.EffectTypeId == CreatureHalos.TypeId);

            Assert.AreEqual(1u, attached.EffectLevel);
            Assert.IsTrue(attached.Announced, "the client plays an effect's FX on its announce (BaseGameEffect.DoAnnounceAttachVisuals)");
        }

        /// <summary>A creature of the creature table's id, brought into the world the way a spawn pool brings one in.</summary>
        private static Creature Placed(WorldTestContext world, uint dbId, Vector3 position)
        {
            var creature = new Creature
            {
                DbId = dbId,
                Name = "Fixture",
                TargetCategory = TargetCategory.Friendly,
                MapContextId = world.Map.MapInfo.MapContextId,
                Position = position,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 5,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 555, 555, 555, 0, 0);

            CellManager.Instance.AddToWorld(world.Map, creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);

            return creature;
        }
    }
}
