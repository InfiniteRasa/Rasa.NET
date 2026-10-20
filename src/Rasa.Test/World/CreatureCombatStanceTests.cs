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
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    /// <summary>
    /// An unarmed creature holds the combat stance for the whole of a fight - RequestVisualCombatMode
    /// (true) as it starts, (false) as it ends - where the client used to put it in combat only for
    /// 2.5 s after each attack (CreatureWeaponDraw). A Maw is left out.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureCombatStanceTests
    {
        private readonly List<Creature> _creatures = new List<Creature>();

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
        }

        [TestMethod]
        public void AnUnarmedCreatureTakesTheStanceAsItsFightStartsAndDropsItAsItEnds()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 30);
            var boargar = Spawn(world, (EntityClasses)6031);

            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(boargar, watcher.Player.EntityId));

            var sent = Sent(watcher, boargar);
            Assert.IsTrue(sent.OfType<RequestVisualCombatModePacket>().Single().CombatMode);
            Assert.AreEqual(0, sent.OfType<WeaponReadyPacket>().Count(), "nothing to draw");
            Assert.IsTrue(boargar.InCombatMode);
            Assert.IsFalse(boargar.WeaponDrawn);

            // A change of target within the fight says nothing more.
            CreatureWeaponDraw.Draw(world.Map, boargar);
            Assert.AreEqual(0, Sent(watcher, boargar).Count);

            BehaviorManager.Instance.StopFighting(boargar);

            sent = Sent(watcher, boargar);
            Assert.IsFalse(sent.OfType<RequestVisualCombatModePacket>().Single().CombatMode);
            Assert.AreEqual(0, sent.OfType<WeaponReadyPacket>().Count());
            Assert.IsFalse(boargar.InCombatMode);

            CreatureWeaponDraw.Stow(world.Map, boargar);
            Assert.AreEqual(0, Sent(watcher, boargar).OfType<RequestVisualCombatModePacket>().Count(), "out of it already");
        }

        [TestMethod]
        public void APlayerWhoComesUponTheFightSeesItInTheStance()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 30);
            var xanx = Spawn(world, (EntityClasses)7510);

            Assert.IsFalse(new ActorInfoPacket(xanx).CombatMode);

            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(xanx, watcher.Player.EntityId));

            Assert.IsTrue(new ActorInfoPacket(xanx).CombatMode);
        }

        [TestMethod]
        public void ALeashTakesTheStanceAway()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 30);
            var miasma = Spawn(world, (EntityClasses)6170);
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(miasma, watcher.Player.EntityId));
            Sent(watcher, miasma);

            BehaviorManager.Instance.Leash(world.Map, miasma);

            Assert.IsFalse(miasma.InCombatMode);
            Assert.IsFalse(Sent(watcher, miasma).OfType<RequestVisualCombatModePacket>().Single().CombatMode);
        }

        [TestMethod]
        public void AMawTakesNoStance()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 30);
            var maw = Spawn(world, (EntityClasses)6332);

            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(maw, watcher.Player.EntityId));

            Assert.IsFalse(maw.InCombatMode);
            Assert.AreEqual(0, Sent(watcher, maw).OfType<RequestVisualCombatModePacket>().Count());
        }

        [TestMethod]
        public void AnArmedCreatureDrawsAndTakesTheStanceAsBefore()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 30);
            var thrax = Spawn(world, EntityClasses.HumanBaseMale, weapon: 27220);

            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(thrax, watcher.Player.EntityId));

            var sent = Sent(watcher, thrax);
            Assert.IsTrue(sent.OfType<WeaponReadyPacket>().Single().WeaponReady);
            Assert.IsTrue(sent.OfType<RequestVisualCombatModePacket>().Single().CombatMode);

            BehaviorManager.Instance.StopFighting(thrax);

            sent = Sent(watcher, thrax);
            Assert.IsFalse(sent.OfType<WeaponReadyPacket>().Single().WeaponReady);
            Assert.IsFalse(sent.OfType<RequestVisualCombatModePacket>().Single().CombatMode);
        }

        private static List<PythonPacket> Sent(Client client, Creature about) =>
            WorldTestContext.Drain(client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Where(message => message.EntityId == about.EntityId)
                .Select(message => message.Packet)
                .ToList();

        private Creature Spawn(WorldTestContext world, EntityClasses entityClass, uint weapon = 0)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(10, 0, 0),
                EntityClass = entityClass,
                State = CharacterState.Idle,
                Level = 1,
                AggroRange = 0,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            if (weapon != 0)
                creature.AppearanceData[EquipmentData.Weapon] = new AppearanceData { SlotId = EquipmentData.Weapon, Class = weapon, Color = new Color(1), Hue2 = new Color(1) };

            creature.HomePos.Position = creature.Position;
            creature.HomePos.MapContextid = creature.MapContextId;
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            BehaviorManager.StartWandering(creature, false);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            _creatures.Add(creature);
            return creature;
        }

        private static Client Watch(WorldTestContext world, float x)
        {
            var client = world.CreateClient(x: x);
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            client.Player.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            WorldTestContext.Drain(client);
            return client;
        }
    }
}
