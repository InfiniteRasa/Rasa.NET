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

        private readonly List<EntityClasses> _classes = new List<EntityClasses>();

        /// <summary>A human NPC's body: mesh 4257, which animationdata.meshSkeleton puts on skeleton 1.</summary>
        private const EntityClasses HumanNpc = (EntityClasses)990001;

        /// <summary>A body on another skeleton: mesh 13798, skeleton 13867 (the Thrax).</summary>
        private const EntityClasses ThraxNpc = (EntityClasses)990002;

        /// <summary>The bodies and the weapon classes, with their weapon class rows' draw and stow args.</summary>
        private void Classes()
        {
            Add(HumanNpc, 4257);
            Add(ThraxNpc, 13798);
            Weapon(6271, 1, 1);      // the Redshirt pistol
            Weapon(20535, 7, 7);     // a machine gun
            Weapon(27220, 2, 2);     // a rifle
            Weapon(10171, 16, 17);   // a weapon whose draw no human skeleton animates
        }

        private void Add(EntityClasses id, uint mesh)
        {
            if (EntityClassManager.Instance.LoadedEntityClasses.ContainsKey(id))
                return;

            EntityClassManager.Instance.LoadedEntityClasses.Add(id, new EntityClass((uint)id, "fixture", mesh, 0, new List<AugmentationType>(), true));
            _classes.Add(id);
        }

        private void Weapon(uint id, byte draw, byte stow)
        {
            Add((EntityClasses)id, 0);
            EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)id].WeaponClassInfo =
                new WeaponClassInfo(new Rasa.Structures.World.WeaponClassEntry { Id = id, DrawActionId = draw, StowActionId = stow, ClipSize = 1 });
        }

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var id in _classes)
                EntityClassManager.Instance.LoadedEntityClasses.Remove(id);

            _classes.Clear();

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

            Assert.IsFalse(MawLurk.Lurks(maw), "a Maw of no pool: by class alone, as upstream");
            Assert.IsFalse(maw.InCombatMode);
            Assert.AreEqual(0, Sent(watcher, maw).OfType<RequestVisualCombatModePacket>().Count());
        }

        /// <summary>Seymour, too, by class alone: no pool, as before the Smart Mine was a lurker.</summary>
        [TestMethod]
        public void SeymourWithNoPoolTakesNoStance()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 30);
            var seymour = Spawn(world, (EntityClasses)22980);

            Assert.IsFalse(MawLurk.Lurks(seymour), "the fixture: it does not lurk");
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(seymour, watcher.Player.EntityId));

            Assert.IsFalse(seymour.InCombatMode);
            Assert.AreEqual(0, Sent(watcher, seymour).OfType<RequestVisualCombatModePacket>().Count());
        }

        /// <summary>
        /// A Necromite (7528) that does not lurk - a Thrax's summoned one (570003), a scripted one,
        /// one of no pool - takes the stance on any map (fix M): only a Smart Mine of an automatic
        /// pool, which lurks, fights without it.
        /// </summary>
        [TestMethod]
        [DataRow("summoned")]
        [DataRow("scripted")]
        [DataRow("no pool")]
        [DataRow("scripted pool")]
        public void ANecromiteThatDoesNotLurkTakesTheStance(string kind)
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 30);
            var necromite = Spawn(world, (EntityClasses)7528);
            var automatic = new SpawnPool { Mode = SpawnPoolManager.ModeAutomatic };
            switch (kind)
            {
                case "summoned":
                    necromite.SpawnPool = automatic;
                    necromite.MasterEntityId = 7;
                    // A minion fights only out of passive and with an attack (BehaviorManager.TrySetActionFighting).
                    necromite.Stance = MinionStance.Aggressive;
                    necromite.Actions.Add(new CreatureAction { ActionId = ActionId.WeaponAttack, RangeMax = 30 });
                    break;
                case "scripted":
                    necromite.SpawnPool = automatic;
                    necromite.IsScripted = true;
                    break;
                case "scripted pool":
                    necromite.SpawnPool = new SpawnPool { Mode = 1 };
                    break;
            }

            Assert.IsTrue(MawLurk.Is(necromite), "the fixture: a lurking class");
            Assert.IsFalse(MawLurk.Lurks(necromite), "the fixture: it does not lurk");
            Assert.IsTrue(CreatureWeaponDraw.HoldsStance(necromite));
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(necromite, watcher.Player.EntityId));

            Assert.IsTrue(necromite.InCombatMode);
            Assert.IsTrue(Sent(watcher, necromite).OfType<RequestVisualCombatModePacket>().Single().CombatMode);
        }

        [TestMethod]
        public void ALurkingSmartMineTakesNoStance()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 30);
            var mine = Spawn(world, (EntityClasses)7528);
            mine.SpawnPool = new SpawnPool { Mode = SpawnPoolManager.ModeAutomatic };

            Assert.IsTrue(MawLurk.Lurks(mine), "the fixture: a minefield's Smart Mine");
            Assert.IsFalse(CreatureWeaponDraw.HoldsStance(mine));
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(mine, watcher.Player.EntityId));

            Assert.IsFalse(mine.InCombatMode);
            Assert.AreEqual(0, Sent(watcher, mine).OfType<RequestVisualCombatModePacket>().Count());
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

        /// <summary>
        /// A human NPC draws and puts away as a player does: WEAPON_DRAW and WEAPON_STOW at its
        /// weapon class's args, ahead of the WeaponReady that keeps the weapon where it should be
        /// whatever the client does with the action.
        /// </summary>
        [TestMethod]
        [DataRow(6271u, 1u, 1u)]     // the Redshirt pistol
        [DataRow(20535u, 7u, 7u)]    // a machine gun
        [DataRow(27220u, 2u, 2u)]    // a rifle
        public void AHumanNpcPlaysItsDrawAndItsStow(uint weapon, uint drawArg, uint stowArg)
        {
            using var world = new WorldTestContext();
            Classes();
            var watcher = Watch(world, x: 30);
            var soldier = Spawn(world, HumanNpc, weapon: weapon);

            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(soldier, watcher.Player.EntityId));

            var sent = Sent(watcher, soldier);
            var draw = sent.OfType<PerformRecoveryPacket>().Single();
            Assert.AreEqual((PerformType.TwoArgs, ActionId.WeaponDraw, drawArg), (draw.PerformType, draw.ActionId, draw.ActionArgId));
            var ready = sent.OfType<WeaponReadyPacket>().Single();
            Assert.IsTrue(ready.WeaponReady);
            Assert.IsLessThan(sent.IndexOf(ready), sent.IndexOf(draw), "the draw, then the weapon ready");

            BehaviorManager.Instance.StopFighting(soldier);

            sent = Sent(watcher, soldier);
            var stow = sent.OfType<PerformRecoveryPacket>().Single();
            Assert.AreEqual((PerformType.TwoArgs, ActionId.WeaponStow, stowArg), (stow.PerformType, stow.ActionId, stow.ActionArgId));
            var away = sent.OfType<WeaponReadyPacket>().Single();
            Assert.IsFalse(away.WeaponReady);
            Assert.IsLessThan(sent.IndexOf(away), sent.IndexOf(stow));
        }

        /// <summary>Not on a human skeleton, or a weapon whose draw the skeleton has no animation for: WeaponReady alone, as before.</summary>
        [TestMethod]
        public void NoDrawIsPlayedWhereTheSkeletonHasNoneForIt()
        {
            using var world = new WorldTestContext();
            Classes();
            var watcher = Watch(world, x: 30);
            var thrax = Spawn(world, ThraxNpc, weapon: 6271);
            var odd = Spawn(world, HumanNpc, weapon: 10171);

            Assert.AreEqual(1u, CreatureWeaponDraw.ArgFor(Spawn(world, HumanNpc, weapon: 6271), true), "the same pistol on a human body");
            Assert.IsNull(CreatureWeaponDraw.ArgFor(thrax, true));
            Assert.IsNull(CreatureWeaponDraw.ArgFor(odd, true));
            Assert.IsNull(CreatureWeaponDraw.ArgFor(odd, false));

            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(thrax, watcher.Player.EntityId));
            var sent = Sent(watcher, thrax);
            Assert.IsEmpty(sent.OfType<PerformRecoveryPacket>().ToArray());
            Assert.IsTrue(sent.OfType<WeaponReadyPacket>().Single().WeaponReady);
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
