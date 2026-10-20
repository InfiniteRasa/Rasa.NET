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
    /// A quarter of the Maw bites that land on a player hold them in its vines for four seconds:
    /// MAW_VINES, a root (MawVines).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MawVinesTests
    {
        private readonly List<Creature> _creatures = new List<Creature>();
        private Func<int> _roll;

        [TestInitialize]
        public void Initialize()
        {
            _roll = MawVines.Roll;
        }

        [TestCleanup]
        public void Cleanup()
        {
            MawVines.Roll = _roll;

            foreach (var creature in _creatures)
            {
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
                EntityManager.Instance.UnregisterActor(creature.EntityId);
                EntityManager.Instance.UnregisterEntity(creature.EntityId);
            }

            _creatures.Clear();
        }

        [TestMethod]
        [DataRow(0, true)]
        [DataRow(24, true)]
        [DataRow(25, false)]
        [DataRow(99, false)]
        public void AQuarterOfTheBitesHold(int roll, bool holds)
        {
            using var world = new WorldTestContext();
            var player = Watch(world);
            var maw = Spawn(world);
            MawVines.Roll = () => roll;

            Bite(world, maw, player.Player);

            Assert.AreEqual(holds, MawVines.Holds(player.Player));
        }

        [TestMethod]
        public void TheVinesHoldForFourSecondsAndAreARoot()
        {
            using var world = new WorldTestContext();
            var player = Watch(world);
            var maw = Spawn(world);
            MawVines.Roll = () => 0;
            var before = Environment.TickCount64;

            Bite(world, maw, player.Player);

            var vines = player.Player.ActiveEffects.Values.Single(e => e.TypeId == MawVines.TypeId);
            Assert.IsTrue(vines.IsRoot);
            Assert.IsFalse(vines.IsStun, "they can still shoot");
            Assert.AreEqual(1u, vines.EffectLevel, "EFFECT_CREATURE_MAW_VINES");
            Assert.AreEqual(maw.EntityId, vines.SourceId);
            Assert.IsTrue(vines.ExpiresTick >= before + MawVines.DurationMs && vines.ExpiresTick <= Environment.TickCount64 + MawVines.DurationMs);
            Assert.IsTrue(CrowdControl.IsRooted(player.Player));
        }

        [TestMethod]
        public void ABiteOnSomeoneHeldDoesNotHoldThemLonger()
        {
            using var world = new WorldTestContext();
            var player = Watch(world);
            var maw = Spawn(world);
            MawVines.Roll = () => 0;

            Bite(world, maw, player.Player);
            var first = player.Player.ActiveEffects.Values.Single(e => e.TypeId == MawVines.TypeId);

            Bite(world, maw, player.Player);

            Assert.AreSame(first, player.Player.ActiveEffects.Values.Single(e => e.TypeId == MawVines.TypeId));
        }

        [TestMethod]
        public void OnlyTheMawsBite()
        {
            using var world = new WorldTestContext();
            var player = Watch(world);
            var maw = Spawn(world);
            MawVines.Roll = () => 0;

            Bite(world, maw, player.Player, ActionId.CrMiasmaMelee);

            Assert.IsFalse(MawVines.Holds(player.Player));
        }

        private static void Bite(WorldTestContext world, Creature maw, Manifestation player, ActionId action = ActionId.CrMawMelee)
        {
            MissileManager.Instance.MissileTrigger(world.Map, new Missile
            {
                Source = maw,
                TargetActor = player,
                TargetEntityId = player.EntityId,
                DamageA = 10,
                DamageType = DamageType.Physical,
                ActionId = action,
                ActionArgId = 1,
                IsMelee = true,
                CritChance = 0
            });
        }

        private Creature Spawn(WorldTestContext world)
        {
            var creature = new Creature
            {
                Name = "Maw",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(0, 0, 2),
                EntityClass = (EntityClasses)6332,
                State = CharacterState.Idle,
                Level = 34,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 10000, 10000, 10000, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionWander;
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            _creatures.Add(creature);
            return creature;
        }

        private static Client Watch(WorldTestContext world)
        {
            var client = world.CreateClient();
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            client.Player.State = CharacterState.Normal;
            foreach (var attribute in new[] { Attributes.Health, Attributes.Armor, Attributes.Power, Attributes.Regen })
                client.Player.Attributes[attribute] = new ActorAttributes(attribute, 1000, 1000, 1000, 0, 0);
            WorldTestContext.Drain(client);
            return client;
        }
    }
}
