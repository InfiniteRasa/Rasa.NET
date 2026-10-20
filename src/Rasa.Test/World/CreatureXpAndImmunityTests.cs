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
    using Rasa.Missions;
    using Rasa.Packets;
    using Rasa.Packets.Manifestation.Server;
    using Rasa.Structures;
    using Rasa.Test.Missions;

    /// <summary>
    /// The XP_MODIFIER_* flags scale a kill's experience (CreatureExperience); the IMMUNE_* flags
    /// make a creature take nothing from a damage type, and turn away the effects that would knock
    /// it back, stun, root, slow, blind, frighten or confuse it (CreatureImmunities).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureXpAndImmunityTests
    {
        [TestInitialize]
        public void Begin() => KillStreaks.Reset();

        [TestCleanup]
        public void End() => KillStreaks.Reset();

        #region Experience

        [TestMethod]
        public void EachXpModifierIsTheFactorItsNameSays()
        {
            var expected = new Dictionary<CreatureFlag, int>
            {
                [CreatureFlag.XpModifier0None] = 0,
                [CreatureFlag.XpModifier1Quarter] = 25,
                [CreatureFlag.XpModifier2Half] = 50,
                [CreatureFlag.XpModifier3Normal] = 100,
                [CreatureFlag.XpModifier3aOneAndAHalf] = 150,
                [CreatureFlag.XpModifier4Double] = 200,
                [CreatureFlag.XpModifier4aTriple] = 300,
                [CreatureFlag.XpModifier5Quadruple] = 400
            };

            foreach (var (flag, percent) in expected)
                Assert.AreEqual(percent, CreatureExperience.PercentOf(With(flag)), flag.ToString());

            Assert.AreEqual(100, CreatureExperience.PercentOf(With()), "no flag: as any other");
            Assert.AreEqual(100, CreatureExperience.PercentOf(null));
            Assert.AreEqual(25, CreatureExperience.PercentOf(With(CreatureFlag.XpModifier4Double, CreatureFlag.XpModifier1Quarter)), "the smallest of several");

            Assert.AreEqual(200u, CreatureExperience.Apply(With(CreatureFlag.XpModifier4Double), 100));
            Assert.AreEqual(25u, CreatureExperience.Apply(With(CreatureFlag.XpModifier1Quarter), 101), "rounded to the nearest");
            Assert.AreEqual(2u, CreatureExperience.Apply(With(CreatureFlag.XpModifier2Half), 3), "a half rounds up");
            Assert.AreEqual(0u, CreatureExperience.Apply(With(CreatureFlag.XpModifier0None), 100));
            Assert.AreEqual(107u, CreatureExperience.Apply(With(), 107));
        }

        [TestMethod]
        public void ADoubleCreaturesKillPaysTwiceAndANoneCreaturesNothing()
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled, 82));
            var killer = context.Client;

            // A level 1 kill is 100 give or take 10: doubled, 180 to 220.
            var doubled = Kill(context, Corpse(context, CreatureFlag.XpModifier4Double));
            var gained = doubled.OfType<ExperienceChangedPacket>().Single().XPInfo.BaseGained;
            Assert.IsTrue(gained >= 180 && gained <= 220, $"doubled: {gained}");
            var credit = KillStreaks.CreditOf(killer);
            Assert.IsTrue(credit > 0, "a kill in the streak");

            // Worth nothing: no experience, no part of the streak - and still the killer's corpse.
            var experience = killer.Player.Experience;
            var none = Corpse(context, CreatureFlag.XpModifier0None);
            var nothing = Kill(context, none);

            Assert.IsEmpty(nothing.OfType<ExperienceChangedPacket>().ToList());
            Assert.AreEqual(experience, killer.Player.Experience);
            Assert.AreEqual(credit, KillStreaks.CreditOf(killer));
            Assert.AreEqual(killer.Player.EntityId, none.HarvestOwnerEntityId, "its loot and harvest are not experience");
        }

        #endregion

        #region Damage

        [TestMethod]
        public void ADamageImmunityTakesNothingOfThatType()
        {
            using var world = new WorldTestContext();
            var player = world.CreateClient().Player;
            var creature = Spawn(world, CreatureFlag.ImmuneFire, CreatureFlag.ImmuneEnergy, CreatureFlag.ImmuneLight);

            try
            {
                Assert.IsTrue(DamageImmunity.IsImmune(creature, DamageType.Fire));
                Assert.IsTrue(DamageImmunity.IsImmune(creature, DamageType.Electrical), "IMMUNE_ENERGY is the Electrical type");
                Assert.IsTrue(DamageImmunity.IsImmune(creature, DamageType.Laser), "IMMUNE_LIGHT is the Laser type");
                Assert.IsFalse(DamageImmunity.IsImmune(creature, DamageType.Physical));
                Assert.IsFalse(DamageImmunity.IsImmune(creature, 0), "no type is Physical");
                Assert.IsTrue(DamageImmunity.IsImmune(Spawn(world, CreatureFlag.ImmunePhysical), 0));
                Assert.IsFalse(CreatureImmunities.IsImmune(player, DamageType.Fire), "a player has no creature flags");

                Assert.AreEqual(0, ActorManager.Instance.Damage(world.Map, creature, 50, player, out var outcome, DamageType.Fire));
                Assert.IsTrue(outcome.Immune, "shown as Immune");
                Assert.AreEqual(1000, creature.Attributes[Attributes.Health].Current);

                Assert.AreEqual(50, ActorManager.Instance.Damage(world.Map, creature, 50, player, out _, DamageType.Sonic));
                Assert.AreEqual(950, creature.Attributes[Attributes.Health].Current);
            }
            finally
            {
                RemoveAll(world);
            }
        }

        #endregion

        #region Control

        [TestMethod]
        public void WhatAnEffectDoesIsReadOffIt()
        {
            Assert.AreEqual(DamageType.KnockBack, CreatureImmunities.ControlOf(new GameEffect { IsBuff = false, TypeId = CrowdControl.KnockbackTypeId, IsStun = true }));
            Assert.AreEqual(DamageType.KnockBack, CreatureImmunities.ControlOf(new GameEffect { IsBuff = false, TypeId = CrowdControl.CritSonicTypeId, IsStun = true }));
            Assert.AreEqual(DamageType.Stun, CreatureImmunities.ControlOf(new GameEffect { IsBuff = false, TypeId = Stuns.StunTypeId, IsStun = true }));
            Assert.AreEqual(DamageType.Root, CreatureImmunities.ControlOf(new GameEffect { IsBuff = false, TypeId = CrowdControl.NetGunRootTypeId, IsRoot = true }));
            Assert.AreEqual(DamageType.Blind, CreatureImmunities.ControlOf(new GameEffect { IsBuff = false, TypeId = 1, Blinds = true }));
            Assert.AreEqual(DamageType.Snare, CreatureImmunities.ControlOf(new GameEffect { IsBuff = false, TypeId = 1, MovementModifierPercent = 80 }));
            Assert.AreEqual(DamageType.Fear, CreatureImmunities.ControlOf(MindControl(AbilityManager.MindControlFrighten)));
            Assert.AreEqual(DamageType.Confusion, CreatureImmunities.ControlOf(MindControl(AbilityManager.MindControlConfusion)));
            Assert.AreEqual(DamageType.Confusion, CreatureImmunities.ControlOf(MindControl(AbilityManager.MindControlSubversion)));
            Assert.AreEqual(DamageType.Sleep, CreatureImmunities.ControlOf(new GameEffect { IsBuff = false, TypeId = 1, ControlKind = DamageType.Sleep }));

            Assert.AreEqual((DamageType)0, CreatureImmunities.ControlOf(MindControl(AbilityManager.MindControlEnslavement)), "a turn, not a fear or a confusion");
            Assert.AreEqual((DamageType)0, CreatureImmunities.ControlOf(new GameEffect { TypeId = 1, IsStun = true, IsBuff = true }), "a buff");
            Assert.AreEqual((DamageType)0, CreatureImmunities.ControlOf(new GameEffect { IsBuff = false, TypeId = 1, MovementModifierPercent = 80, Environmental = true }), "the world's own");
            Assert.AreEqual((DamageType)0, CreatureImmunities.ControlOf(new GameEffect { IsBuff = false, TypeId = 1, TickDamageMax = 5 }), "damage alone");
        }

        [TestMethod]
        public void AnImmuneCreatureTurnsTheEffectAwayAndGoesOnAsItWas()
        {
            using var world = new WorldTestContext();
            var player = world.CreateClient().Player;

            try
            {
                // Stun: not on it, not stopped, no Critical Death window.
                var unstunnable = Spawn(world, CreatureFlag.ImmuneStun);
                Assert.IsFalse(Stuns.Apply(world.Map, unstunnable, player, Stuns.StunTypeId, 3000, DamageType.Physical));
                Assert.IsFalse(Stuns.IsStunned(unstunnable));
                Assert.IsTrue(Stuns.Apply(world.Map, Spawn(world), player, Stuns.StunTypeId, 3000, DamageType.Physical), "the control");

                // Knockback: not thrown.
                var rooted = Spawn(world, CreatureFlag.ImmuneKnockback);
                Assert.IsTrue(CrowdControl.Knockback(world.Map, rooted, player, 10f, CrowdControl.KnockbackTypeId, DamageType.Physical));
                Assert.IsNull(rooted.KnockbackTo);
                Assert.IsFalse(Stuns.IsStunned(rooted));
                Assert.IsFalse(CrowdControl.Pull(world.Map, rooted, player, 1000), "nor dragged by a Vortex");

                // Root: not held.
                var unrootable = Spawn(world, CreatureFlag.ImmuneRoot);
                Assert.IsFalse(CrowdControl.Root(world.Map, unrootable, player, CrowdControl.NetGunRootTypeId, 3000));
                Assert.IsFalse(CrowdControl.IsRooted(unrootable));
                var immobile = Spawn(world, CreatureFlag.ImmobilizeImmunity);
                Assert.IsFalse(CrowdControl.Root(world.Map, immobile, player, CrowdControl.NetGunRootTypeId, 3000), "IMMOBILIZE_IMMUNITY is the same");
                Assert.IsFalse(CrowdControl.IsRooted(immobile));
                Assert.IsTrue(CrowdControl.Root(world.Map, Spawn(world), player, CrowdControl.NetGunRootTypeId, 3000), "the control");

                // Slow: not slowed - unless it rides on damage, which keeps its damage and loses the slow.
                var unslowable = Spawn(world, CreatureFlag.ImmuneSnare);
                CrowdControl.Slow(world.Map, unslowable, player, CrowdControl.CritVirulentTypeId, 50, 4000);
                Assert.IsEmpty(unslowable.ActiveEffects.Values.ToList());

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
                GameEffectManager.Instance.Attach(world.Map, unslowable, ruin);
                Assert.IsTrue(unslowable.ActiveEffects.ContainsKey(ruin.EffectId), "its damage goes on");
                Assert.AreEqual(0, ruin.MovementModifierPercent, "without the slow");

                // Fear: Frighten does not take.
                var fearless = Spawn(world, CreatureFlag.ImmuneFear);
                var frighten = MindControl(AbilityManager.MindControlFrighten);
                frighten.EffectId = GameEffectManager.Instance.NextEffectId(world.Map);
                frighten.Source = player;
                frighten.SourceId = player.EntityId;
                GameEffectManager.Instance.Attach(world.Map, fearless, frighten);
                Assert.IsFalse(fearless.ActiveEffects.ContainsKey(frighten.EffectId));
            }
            finally
            {
                RemoveAll(world);
            }
        }

        #endregion

        private static GameEffect MindControl(int pump) => new GameEffect
        {
            TypeId = AbilityManager.MindControlTypeId,
            IsBuff = false,
            MindControlPump = pump,
            ExpiresTick = Environment.TickCount64 + 15000
        };

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

        private static void RemoveAll(WorldTestContext world)
        {
            foreach (var creature in world.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().ToList())
            {
                EntityManager.Instance.UnregisterActor(creature.EntityId);
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
                EntityManager.Instance.UnregisterEntity(creature.EntityId);
                EntityManager.Instance.FreeEntity(creature.EntityId);
            }
        }

        /// <summary>A level 1 creature with these flags where the killer stands, in a spawn pool, as KillStreakTests makes one.</summary>
        private static Creature Corpse(MissionTestContext context, params CreatureFlag[] flags)
        {
            var creature = new Creature
            {
                DbId = 9001,
                EntityClass = EntityClasses.HumanBaseMale,
                MapContextId = context.Map.MapInfo.MapContextId,
                Position = context.Client.Player.Position,
                Level = 1,
                State = CharacterState.Idle,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                Attributes = Enum.GetValues<Attributes>().ToDictionary(
                    id => id, id => new ActorAttributes(id, 100, 100, 0, 0, 0)),
                SpawnPool = new SpawnPool
                {
                    MapContextId = context.Map.MapInfo.MapContextId,
                    AliveCreatures = 1,
                    RespawnTime = 1000,
                    UpdateTimer = 1000
                }
            };
            creature.ExtraFlags.AddRange(flags);
            return creature;
        }

        /// <summary>The creature killed by the context's player, and what their client was sent for it.</summary>
        private static List<PythonPacket> Kill(MissionTestContext context, Creature creature)
        {
            CellManager.Instance.AddToWorld(context.Map, creature);
            MissionTestContext.Drain(context.Client);

            try
            {
                new CreatureManager(context, new ManifestationManager(context), context.Manager)
                    .HandleCreatureKill(context.Map, creature, context.Client.Player);

                return MissionTestContext.Drain(context.Client);
            }
            finally
            {
                CellManager.Instance.RemoveCreatureFromWorld(context.Map, creature);
            }
        }
    }
}
