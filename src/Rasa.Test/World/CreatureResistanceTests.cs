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
    /// RESIST_ and VULNERABLE_ (CreatureResistances): +50 and -50 to the creature's resistance to
    /// that damage type, through the client's formula with whatever the effects on it add; the
    /// control RESIST_ flags are a 50% chance to turn the effect away.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureResistanceTests
    {
        private Func<int, bool> _roll;

        [TestInitialize]
        public void Begin() => _roll = CreatureResistances.Roll;

        [TestCleanup]
        public void End() => CreatureResistances.Roll = _roll;

        [TestMethod]
        public void EachFlagIsFiftyEitherWay()
        {
            Assert.AreEqual(50, CreatureResistances.ResistOf(With(CreatureFlag.ResistFire), DamageType.Fire));
            Assert.AreEqual(-50, CreatureResistances.ResistOf(With(CreatureFlag.VulnerableFire), DamageType.Fire));
            Assert.AreEqual(0, CreatureResistances.ResistOf(With(CreatureFlag.ResistFire, CreatureFlag.VulnerableFire), DamageType.Fire), "both: even");
            Assert.AreEqual(0, CreatureResistances.ResistOf(With(CreatureFlag.ResistFire), DamageType.Ice), "another type");
            Assert.AreEqual(50, CreatureResistances.ResistOf(With(CreatureFlag.ResistLight), DamageType.Laser), "LIGHT is Laser");
            Assert.AreEqual(-50, CreatureResistances.ResistOf(With(CreatureFlag.VulnerableEnergy), DamageType.Electrical), "ENERGY is Electrical");
            Assert.AreEqual(50, CreatureResistances.ResistOf(With(CreatureFlag.ResistPhysical), 0), "no type is Physical");
            Assert.AreEqual(0, CreatureResistances.ResistOf(With(CreatureFlag.ResistStun), DamageType.Stun), "a control flag is a chance, not a resistance");
            Assert.AreEqual(0, CreatureResistances.ResistOf(new Manifestation(), DamageType.Fire), "a player has no creature flags");
        }

        [TestMethod]
        public void AResistantCreatureTakesHalfAndAVulnerableOneHalfAgain()
        {
            Assert.AreEqual(50, GameEffectManager.ApplyResist(With(CreatureFlag.ResistFire), 100, out var resisted, DamageType.Fire));
            Assert.AreEqual(50, resisted);

            Assert.AreEqual(150, GameEffectManager.ApplyResist(With(CreatureFlag.VulnerableFire), 100, out resisted, DamageType.Fire));
            Assert.AreEqual(-50, resisted, "made bigger, not resisted");

            Assert.AreEqual(100, GameEffectManager.ApplyResist(With(CreatureFlag.ResistFire), 100, out _, DamageType.Sonic), "another type");
            Assert.AreEqual(100, GameEffectManager.ApplyResist(With(), 100, out _, DamageType.Fire), "no flag");

            // A full Polarity Field on the type brings a resistant creature to even, and makes a
            // vulnerable one take twice as much.
            var resistant = With(CreatureFlag.ResistFire);
            Polarity(resistant, DamageType.Fire, -50);
            Assert.AreEqual(100, GameEffectManager.ApplyResist(resistant, 100, out _, DamageType.Fire));

            var vulnerable = With(CreatureFlag.VulnerableFire);
            Polarity(vulnerable, DamageType.Fire, -50);
            Assert.AreEqual(200, GameEffectManager.ApplyResist(vulnerable, 100, out _, DamageType.Fire));
        }

        [TestMethod]
        public void AControlResistanceTurnsTheEffectAwayHalfTheTime()
        {
            using var world = new WorldTestContext();
            var player = world.CreateClient().Player;
            var asked = new List<int>();

            try
            {
                // The roll comes up: resisted, and the stun does nothing.
                CreatureResistances.Roll = chance => { asked.Add(chance); return true; };
                var hardy = Spawn(world, CreatureFlag.ResistStun);
                Assert.IsFalse(Stuns.Apply(world.Map, hardy, player, Stuns.StunTypeId, 3000, DamageType.Physical));
                Assert.IsFalse(Stuns.IsStunned(hardy));
                CollectionAssert.AreEqual(new[] { CreatureResistances.ControlResistPercent }, asked, "a 50% chance");

                // A creature without the flag is not rolled for.
                asked.Clear();
                Assert.IsTrue(Stuns.Apply(world.Map, Spawn(world), player, Stuns.StunTypeId, 3000, DamageType.Physical));
                Assert.IsEmpty(asked);

                // Nor is a stun-resistant one rolled for against a root.
                Assert.IsTrue(CrowdControl.Root(world.Map, Spawn(world, CreatureFlag.ResistStun), player, CrowdControl.NetGunRootTypeId, 3000));
                Assert.IsEmpty(asked);

                // The roll fails: it lands.
                CreatureResistances.Roll = _ => false;
                var unlucky = Spawn(world, CreatureFlag.ResistStun);
                Assert.IsTrue(Stuns.Apply(world.Map, unlucky, player, Stuns.StunTypeId, 3000, DamageType.Physical));
                Assert.IsTrue(Stuns.IsStunned(unlucky));

                // A resisted slow on a damage over time: the damage goes on without the slow.
                CreatureResistances.Roll = _ => true;
                var slowable = Spawn(world, CreatureFlag.ResistSnare);
                var ruin = new GameEffect
                {
                    TypeId = 207,
                    IsBuff = false,
                    EffectId = GameEffectManager.Instance.NextEffectId(world.Map),
                    EffectLevel = 5,
                    Source = player,
                    SourceId = player.EntityId,
                    TickDamageMin = 10,
                    TickDamageMax = 10,
                    TickIntervalMs = 1000,
                    MovementModifierPercent = 80,
                    ExpiresTick = Environment.TickCount64 + 10000
                };
                GameEffectManager.Instance.Attach(world.Map, slowable, ruin);
                Assert.IsTrue(slowable.ActiveEffects.ContainsKey(ruin.EffectId));
                Assert.AreEqual(0, ruin.MovementModifierPercent);
            }
            finally
            {
                foreach (var creature in world.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().ToList())
                {
                    EntityManager.Instance.UnregisterActor(creature.EntityId);
                    EntityManager.Instance.UnregisterCreature(creature.EntityId);
                    EntityManager.Instance.UnregisterEntity(creature.EntityId);
                    EntityManager.Instance.FreeEntity(creature.EntityId);
                }
            }
        }

        private static void Polarity(Creature creature, DamageType type, int modifier)
        {
            var effect = new GameEffect { EffectId = 9000 + creature.ActiveEffects.Count, IsBuff = false, ResistDamageType = type, ResistModifier = modifier };
            creature.ActiveEffects[effect.EffectId] = effect;
        }

        private static Creature With(params CreatureFlag[] flags)
        {
            var creature = new Creature { EntityClass = EntityClasses.HumanBaseMale };
            creature.ExtraFlags.AddRange(flags);
            return creature;
        }

        private static Creature Spawn(WorldTestContext world, params CreatureFlag[] flags)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(0, 0, 5),
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                RunSpeed = 6,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.ExtraFlags.AddRange(flags);
            creature.HomePos.Position = creature.Position;
            creature.HomePos.MapContextid = creature.MapContextId;
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionWander;
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
