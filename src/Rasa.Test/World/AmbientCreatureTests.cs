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
    /// POWERLEVEL_0_AMBIENT (AmbientCreatures): wildlife that is NEUTRAL, has one hit of health,
    /// fights nobody and is fought by no creature, and is worth nothing to the player who kills it.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AmbientCreatureTests
    {
        /// <summary>A class of our own, so no loaded class has its flags changed under it.</summary>
        private const EntityClasses AmbientClass = (EntityClasses)990162;

        [TestInitialize]
        public void Begin()
        {
            KillStreaks.Reset();
        }

        [TestCleanup]
        public void End()
        {
            KillStreaks.Reset();
            EntityClassManager.Instance.LoadedEntityClasses.Remove(AmbientClass);
        }

        [TestMethod]
        public void TheFlagOnTheClassOrOnTheCreatureMakesItAmbient()
        {
            using var world = new WorldTestContext();
            AddAmbientClass();

            Assert.IsTrue(AmbientCreatures.IsAmbient(Make(world, AmbientClass, TargetCategory.Hostile)));
            Assert.IsFalse(AmbientCreatures.IsAmbient(Make(world, EntityClasses.HumanBaseMale, TargetCategory.Neutral)), "neutral is not ambient");
            Assert.IsFalse(AmbientCreatures.IsAmbient((Creature)null));

            var flagged = Make(world, EntityClasses.HumanBaseMale, TargetCategory.Hostile);
            flagged.ExtraFlags.Add(CreatureFlag.Powerlevel0Ambient);
            Assert.IsTrue(AmbientCreatures.IsAmbient(flagged), "its own flag");

            // Asked of the class each time: a flag taken off holds at once.
            EntityClassManager.Instance.LoadedEntityClasses[AmbientClass].CreatureFlags.Clear();
            Assert.IsFalse(AmbientCreatures.IsAmbient(Make(world, AmbientClass, TargetCategory.Hostile)));
        }

        [TestMethod]
        public void ItIsMadeNeutralWithOneHitOfHealthAndNoArmour()
        {
            using var world = new WorldTestContext();
            AddAmbientClass();

            var ambient = Make(world, AmbientClass, TargetCategory.Hostile);

            Assert.IsTrue(AmbientCreatures.Apply(ambient));
            Assert.AreEqual(TargetCategory.Neutral, ambient.TargetCategory, "whatever its row said");
            Assert.AreEqual(1u, ambient.MaxHitPoints);
            Assert.AreEqual(1, ambient.Attributes[Attributes.Health].Current);
            Assert.AreEqual(1, ambient.Attributes[Attributes.Health].CurrentMax);
            Assert.AreEqual(1, ambient.Attributes[Attributes.Health].NormalMax);
            Assert.AreEqual(0, ambient.Attributes[Attributes.Armor].Current);
            Assert.AreEqual(0, ambient.Attributes[Attributes.Armor].CurrentMax);
            Assert.IsTrue(TargetCategories.PlayerMayAttack(ambient.TargetCategory), "a player may shoot it");

            // Anything else is left as it was.
            var other = Make(world, EntityClasses.HumanBaseMale, TargetCategory.Hostile);

            Assert.IsFalse(AmbientCreatures.Apply(other));
            Assert.AreEqual(TargetCategory.Hostile, other.TargetCategory);
            Assert.AreEqual(500, other.Attributes[Attributes.Health].Current);
            Assert.AreEqual(200, other.Attributes[Attributes.Armor].Current);
        }

        [TestMethod]
        public void ItFightsNobodyAndNoCreatureFightsIt()
        {
            using var world = new WorldTestContext();
            AddAmbientClass();
            var player = world.CreateClient().Player;
            var ambient = Register(Apply(Make(world, AmbientClass, TargetCategory.Hostile)));
            var hostile = Register(Make(world, EntityClasses.HumanBaseMale, TargetCategory.Hostile));
            var friendly = Register(Make(world, EntityClasses.HumanBaseMale, TargetCategory.Friendly));
            var neutral = Register(Make(world, EntityClasses.HumanBaseMale, TargetCategory.Neutral));

            try
            {
                // Shot by a player, it does not answer: not the player, not a creature of either side.
                Assert.IsFalse(BehaviorManager.MayFight(ambient, player.EntityId));
                Assert.IsFalse(BehaviorManager.CanSetActionFighting(ambient, player.EntityId));
                Assert.IsFalse(BehaviorManager.MayFight(ambient, hostile.EntityId));
                Assert.IsFalse(BehaviorManager.MayFight(ambient, friendly.EntityId));

                // Nor does any creature fight it, though NEUTRAL may be fought by anyone.
                Assert.IsFalse(BehaviorManager.MayFight(hostile, ambient.EntityId));
                Assert.IsFalse(BehaviorManager.MayFight(friendly, ambient.EntityId));
                Assert.IsFalse(BehaviorManager.MayFight(neutral, ambient.EntityId));

                // An ordinary neutral creature is as it was: it fights back, and may be fought.
                Assert.IsTrue(BehaviorManager.MayFight(neutral, player.EntityId));
                Assert.IsTrue(BehaviorManager.MayFight(hostile, neutral.EntityId));
            }
            finally
            {
                Unregister(ambient, hostile, friendly, neutral);
            }
        }

        [TestMethod]
        public void NoCreaturesHitLandsOnItAndItsLandsOnNothing()
        {
            using var world = new WorldTestContext();
            AddAmbientClass();
            var client = world.CreateClient();
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            client.Player.State = CharacterState.Normal;
            var ambient = Register(Apply(Make(world, AmbientClass, TargetCategory.Hostile)));
            var hostile = Register(Make(world, EntityClasses.HumanBaseMale, TargetCategory.Hostile));

            try
            {
                Assert.AreEqual(0, ActorManager.Instance.Damage(world.Map, ambient, 50, hostile), "a blast's splash, an effect's tick");
                Assert.AreEqual(1, ambient.Attributes[Attributes.Health].Current);
                Assert.AreNotEqual(CharacterState.Dead, ambient.State);

                Assert.AreEqual(0, ActorManager.Instance.Damage(world.Map, client.Player, 50, ambient));
                Assert.AreEqual(100, client.Player.Attributes[Attributes.Health].Current);

                Assert.IsTrue(AmbientCreatures.Spares(hostile, ambient));
                Assert.IsTrue(AmbientCreatures.Spares(ambient, client.Player));
                Assert.IsFalse(AmbientCreatures.Spares(client.Player, ambient), "a player's hit is all it takes");
                Assert.IsFalse(AmbientCreatures.Spares(hostile, client.Player));
            }
            finally
            {
                Unregister(ambient, hostile);
                WorldTestContext.Drain(client);
            }
        }

        [TestMethod]
        public void KillingOneIsWorthNothing()
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled, 82));
            AddAmbientClass();
            var killer = context.Client;

            // An ordinary creature first: its experience, its streak credit, its corpse to harvest.
            var ordinary = Corpse(context, EntityClasses.HumanBaseMale);
            var paid = Kill(context, ordinary);

            Assert.HasCount(1, paid.OfType<ExperienceChangedPacket>().ToList(), "the control");
            Assert.AreEqual(killer.Player.EntityId, ordinary.HarvestOwnerEntityId);
            var credit = KillStreaks.CreditOf(killer);
            Assert.IsTrue(credit > 0);
            var experience = killer.Player.Experience;

            // An ambient one: dead, and nothing more.
            var ambient = Corpse(context, AmbientClass);
            Assert.IsTrue(AmbientCreatures.Apply(ambient));
            var nothing = Kill(context, ambient);

            Assert.AreEqual(CharacterState.Dead, ambient.State);
            Assert.IsEmpty(nothing.OfType<ExperienceChangedPacket>().ToList(), "no experience");
            Assert.AreEqual(experience, killer.Player.Experience);
            Assert.AreEqual(credit, KillStreaks.CreditOf(killer), "not a kill in the streak");
            Assert.AreEqual(0UL, ambient.HarvestOwnerEntityId, "nobody's to harvest or loot");
            Assert.AreEqual(0, ambient.HarvestAttemptsLeft);
        }

        private static void AddAmbientClass()
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;

            classes.Remove(AmbientClass);
            classes.Add(AmbientClass, new EntityClass((uint)AmbientClass, "Ambient_Fixture", 0, 0, new List<AugmentationType> { AugmentationType.Creature }, true)
            {
                CreatureFlags = new List<CreatureFlag> { CreatureFlag.KingdomAnimal, CreatureFlag.Powerlevel0Ambient }
            });
        }

        private static Creature Apply(Creature creature)
        {
            Assert.IsTrue(AmbientCreatures.Apply(creature));
            return creature;
        }

        private static Creature Make(WorldTestContext world, EntityClasses entityClass, TargetCategory category)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = category,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(0, 0, 5),
                EntityClass = entityClass,
                State = CharacterState.Idle,
                Level = 1,
                MaxHitPoints = 500,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 500, 500, 500, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 200, 200, 200, 0, 0);
            return creature;
        }

        private static Creature Register(Creature creature)
        {
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            return creature;
        }

        private static void Unregister(params Creature[] creatures)
        {
            foreach (var creature in creatures)
            {
                EntityManager.Instance.UnregisterActor(creature.EntityId);
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
                EntityManager.Instance.UnregisterEntity(creature.EntityId);
                EntityManager.Instance.FreeEntity(creature.EntityId);
            }
        }

        /// <summary>A level 1 creature of this class where the killer stands, in a spawn pool, as KillStreakTests makes one.</summary>
        private static Creature Corpse(MissionTestContext context, EntityClasses entityClass) => new()
        {
            DbId = 9001,
            EntityClass = entityClass,
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
