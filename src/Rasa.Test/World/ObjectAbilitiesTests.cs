using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
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
    /// An object's blast (ObjectAbilities): OBJ_KNOCKBACK and OBJ_DAMAGE hit everyone within their
    /// RADIUS_AROUND_SOURCE for their damage, throw them KNOCKBACK_DISTANCE away from the object,
    /// and are told to the clients as (hitdata, sourceId).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ObjectAbilitiesTests
    {
        private const ulong ObjectId = 990001;

        private readonly List<Creature> _creatures = new List<Creature>();
        private readonly Dictionary<ActionId, ActionInfo> _previous = new Dictionary<ActionId, ActionInfo>();

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var creature in _creatures)
            {
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
                EntityManager.Instance.UnregisterActor(creature.EntityId);
                EntityManager.Instance.UnregisterEntity(creature.EntityId);
            }

            _creatures.Clear();

            foreach (var (id, previous) in _previous)
            {
                if (previous != null)
                    Actions()[id] = previous;
                else
                    Actions().Remove(id);
            }

            _previous.Clear();
        }

        [TestMethod]
        public void TheExplosionColumnHitsAndThrowsEveryoneInItsRadiusAwayFromIt()
        {
            Seed();

            using var world = new WorldTestContext();
            var near = Player(world, 5);
            var far = Player(world, 9);
            var creature = Creature(world, new Vector3(0, 0, 3));

            var hits = ObjectAbilities.Perform(world.Map, Vector3.Zero, ObjectId, ActionId.ObjKnockback, 1);

            CollectionAssert.AreEquivalent(new[] { near.Player.EntityId, creature.EntityId }, hits.Select(h => h.EntityId).ToList(), "8 m: not the player at 9");
            Assert.IsTrue(hits.All(h => h.Amount is >= 20 and <= 31 && h.DamageType == DamageType.Physical), "20-31, physical: the argument gives no type");

            Assert.AreEqual(1000 - hits.Single(h => h.EntityId == near.Player.EntityId).Amount, near.Player.Attributes[Attributes.Health].Current);
            Assert.AreEqual(1000, far.Player.Attributes[Attributes.Health].Current);

            Assert.AreEqual(15f, near.Player.Position.X, 0.05f, "thrown 10 m away from the object");
            Assert.IsTrue(near.Player.ActiveEffects.Values.Any(e => e.TypeId == CrowdControl.KnockbackTypeId && e.SourceId == ObjectId), "the object named as its source");
            Assert.IsTrue(creature.ActiveEffects.Values.Any(e => e.TypeId == CrowdControl.KnockbackTypeId), "the creature too");
            Assert.AreEqual(9f, far.Player.Position.X, 0.001f);

            var perform = Sent(far).OfType<PerformObjectAbilityPacket>().Single();
            Assert.AreEqual(ActionId.ObjKnockback, perform.ActionId);
            Assert.AreEqual(1u, perform.ActionArgId);
            CollectionAssert.AreEqual(hits.Select(h => h.EntityId).ToList(), perform.Hits, "a rawInfo for each hit, in their order");
            Assert.AreSame(hits, perform.DamageHits);
            Assert.AreEqual(ObjectId, perform.DamageSourceId);
        }

        [TestMethod]
        public void AHornitoBurnsWithoutThrowingAndARuleKeepsWhoItHits()
        {
            Seed();

            using var world = new WorldTestContext();
            var player = Player(world, 4);
            var creature = Creature(world, new Vector3(0, 0, -4));

            var hits = ObjectAbilities.Perform(world.Map, Vector3.Zero, ObjectId, ActionId.ObjDamage, 7, mayHit: actor => actor is Manifestation);

            Assert.AreEqual(player.Player.EntityId, hits.Single().EntityId, "the rule leaves the creature out");
            Assert.AreEqual(DamageType.Fire, hits.Single().DamageType, "DAMAGE_TYPE 2");
            Assert.IsTrue(hits.Single().Amount is >= 50 and <= 100);
            Assert.AreEqual(4f, player.Player.Position.X, 0.001f, "no KNOCKBACK_DISTANCE: not thrown");
            Assert.AreEqual(10000, creature.Attributes[Attributes.Health].Current);

            // Not an object blast, or an argument the data has not got: nothing.
            Assert.IsEmpty(ObjectAbilities.Perform(world.Map, Vector3.Zero, ObjectId, ActionId.ObjKnockback, 99));
            Assert.IsEmpty(ObjectAbilities.Perform(world.Map, Vector3.Zero, ObjectId, ActionId.WeaponAttack, 1));
        }

        /// <summary>OBJ_KNOCKBACK argument 1 and OBJ_DAMAGE argument 7 as the world database has them.</summary>
        private void Seed()
        {
            Add(ActionId.ObjKnockback, "OBJ_KNOCKBACK", ObjectAbilities.KnockbackModule, 1,
                (AbilityProperty.RadiusAroundSource, 8), (AbilityProperty.DamageAmountMin, 20), (AbilityProperty.DamageAmountMax, 31), (AbilityProperty.KnockbackDistance, 10));
            Add(ActionId.ObjDamage, "OBJ_DAMAGE", ObjectAbilities.DamageModule, 7,
                (AbilityProperty.RadiusAroundSource, 6), (AbilityProperty.DamageAmountMin, 50), (AbilityProperty.DamageAmountMax, 100), (AbilityProperty.DamageType, 2));
        }

        private void Add(ActionId id, string name, string module, uint level, params (AbilityProperty Property, int Value)[] properties)
        {
            var actions = Actions();

            if (!_previous.ContainsKey(id))
            {
                actions.TryGetValue(id, out var previous);
                _previous[id] = previous;
            }

            var info = new ActionLevelInfo { ActionId = id, Level = level };
            foreach (var (property, value) in properties)
                info.Properties[property] = value;

            var action = new ActionInfo { ActionId = id, Name = name, Module = module };
            action.Levels[level] = info;
            actions[id] = action;
        }

        private static Dictionary<ActionId, ActionInfo> Actions() =>
            (Dictionary<ActionId, ActionInfo>)typeof(AbilityManager)
                .GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(AbilityManager.Instance)!;

        /// <summary>A player x metres along from the object, in the map's cells so what is sent near it reaches it.</summary>
        private static Client Player(WorldTestContext world, float x)
        {
            var client = world.CreateClient(x: x);
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            client.Player.State = CharacterState.Normal;
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            foreach (var attribute in new[] { Attributes.Power, Attributes.Regen })
                client.Player.Attributes[attribute] = new ActorAttributes(attribute, 100, 100, 100, 0, 0);
            WorldTestContext.Drain(client);
            return client;
        }

        private Creature Creature(WorldTestContext world, Vector3 position)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = position,
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
            var seed = CellManager.Instance.GetCellSeed(position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            _creatures.Add(creature);
            return creature;
        }

        private static List<PythonPacket> Sent(Client client) =>
            WorldTestContext.Drain(client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Select(message => message.Packet)
                .ToList();
    }
}
