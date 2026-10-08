using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.MapChannel.Server.PerformRecovery;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.World;

    /// <summary>
    /// How long a shot is in the air (ShotFlight): the client's own figure, the distance over the
    /// weapon class's velocity. A player's shot is held on the server for that long where it
    /// lands before the weapon can be fired again, and resolves as it is fired otherwise - as a
    /// swing does, and anything a creature fires, whose hit on a player names the creature in
    /// the armour and health updates so the bars wait for the shot on the client.
    ///
    /// The context's weapon has no velocity of its own (70 m/s), a refire of 800 ms and a reach
    /// of 80 m: with the 250 ms allowance its shots are held up to a flight of 550 ms, 38.5 m.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ShotFlightTests
    {
        private const EntityClasses WeaponClass = (EntityClasses)6048;

        [TestCleanup]
        public void Cleanup() => ToHit.Roll = Stuns.Roll;

        [TestMethod]
        public void TheFlightIsTheDistanceOverTheWeaponsVelocity()
        {
            Assert.AreEqual(1000, ShotFlight.Ms(70, 0), "no velocity of its own: DEFAULT_PROJECTILE_VELOCITY");
            Assert.AreEqual(500, ShotFlight.Ms(35, 70));
            Assert.AreEqual(1000, ShotFlight.Ms(45, 45), "an RPG");
            Assert.AreEqual(200, ShotFlight.Ms(20, 100), "a shotgun");
            Assert.AreEqual(500, ShotFlight.Ms(60, 120), "a Torqueshell rifle");
            Assert.AreEqual(0, ShotFlight.Ms(60, -1), "a laser is there at once");
            Assert.AreEqual(0, ShotFlight.Ms(0, 70));
            Assert.AreEqual(0, ShotFlight.Ms(float.NaN, 70));
        }

        [TestMethod]
        public void AShotIsHeldOnlyIfItLandsBeforeTheWeaponCanBeFiredAgain()
        {
            Assert.AreEqual(500, ShotFlight.HeldMs(35, 0, 800), "500 ms and the allowance are within 800");
            Assert.AreEqual(550, ShotFlight.HeldMs(38.5f, 0, 800), "to the millisecond");
            Assert.AreEqual(0, ShotFlight.HeldMs(40, 0, 800), "571 ms is not");
            Assert.AreEqual(889, ShotFlight.HeldMs(40, 45, 1500), "an RPG at the end of its reach");
            Assert.AreEqual(200, ShotFlight.HeldMs(20, 100, 1500), "a shotgun");
            Assert.AreEqual(0, ShotFlight.HeldMs(20, 0, 300), "a fast pistol at the end of its reach");
            Assert.AreEqual(0, ShotFlight.HeldMs(60, -1, 1000), "nothing to hold");
        }

        [TestMethod]
        public void APlayersShotLandsWhenItsFlightIsOver()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var map = context.World.Map;
            var creature = Spawn(context.World, new Vector3(0, 0, -28));

            Aim(context, creature);
            Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));

            var missile = map.QueuedMissiles.Single();
            Assert.AreEqual(400, missile.TriggerTime, "28 m at 70 m/s");
            Assert.IsTrue(missile.HeldForFlight);
            Sent(context.Client);

            // Three passes of the loop: still in the air.
            for (var pass = 0; pass < 3; pass++)
                MissileManager.Instance.DoWork(map, 100);

            Assert.AreEqual(1, map.QueuedMissiles.Count);
            Assert.AreEqual(10000, creature.Attributes[Attributes.Health].Current);
            Assert.IsEmpty(Sent(context.Client).OfType<WeaponAttackRecovery>().ToList());

            // The fourth: it lands, and that is when the clients hear of it.
            MissileManager.Instance.DoWork(map, 100);

            Assert.AreEqual(0, map.QueuedMissiles.Count);
            Assert.IsTrue(creature.Attributes[Attributes.Health].Current < 10000);

            var recovery = Sent(context.Client).OfType<WeaponAttackRecovery>().Single();
            Assert.AreEqual(creature.EntityId, recovery.Missile.Args.HitData.Single().EntityId);
        }

        [TestMethod]
        public void AShotThatWouldLandAfterTheNextCouldBeFiredResolvesOnTheNextPass()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var map = context.World.Map;
            var creature = Spawn(context.World, new Vector3(0, 0, -45));

            Aim(context, creature);
            Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));

            var missile = map.QueuedMissiles.Single();
            Assert.AreEqual(0, missile.TriggerTime, "643 ms and the allowance are past the 800 ms refire");
            Assert.IsFalse(missile.HeldForFlight);

            MissileManager.Instance.DoWork(map, 100);

            Assert.AreEqual(0, map.QueuedMissiles.Count);
            Assert.IsTrue(creature.Attributes[Attributes.Health].Current < 10000);
        }

        [TestMethod]
        public void TheVelocityIsTheWeaponClasss()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var map = context.World.Map;
            var creature = Spawn(context.World, new Vector3(0, 0, -18));
            var weaponClass = EntityClassManager.Instance.LoadedEntityClasses[WeaponClass].WeaponClassInfo;

            Aim(context, creature);

            weaponClass.Velocity = 45;
            Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));
            Assert.AreEqual(400, map.QueuedMissiles.Single().TriggerTime, "18 m at 45 m/s");

            map.QueuedMissiles.Clear();
            context.Client.Player.NextShotAt = 0;

            weaponClass.Velocity = 120;
            Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));
            Assert.AreEqual(150, map.QueuedMissiles.Single().TriggerTime, "18 m at 120 m/s");

            map.QueuedMissiles.Clear();
            context.Client.Player.NextShotAt = 0;

            weaponClass.Velocity = -1;
            Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));
            Assert.AreEqual(0, map.QueuedMissiles.Single().TriggerTime, "there at once");
            Assert.IsFalse(map.QueuedMissiles.Single().HeldForFlight);
        }

        [TestMethod]
        public void ASwingAndACreaturesShotAreNotHeld()
        {
            using var context = new WeaponAmmoContext();
            var map = context.World.Map;
            var player = context.Client.Player;
            var creature = Spawn(context.World, new Vector3(0, 0, -28));

            // A swing, whatever velocity the weapon has.
            MissileManager.Instance.MissileLaunch(map, new ActionData(player, ActionId.WeaponMelee, 1, creature.EntityId, 0), 10,
                melee: true, flightVelocity: 0, refireMs: 800);

            var swing = map.QueuedMissiles.Single();
            Assert.AreEqual(14, swing.TriggerTime, "the next pass, as before");
            Assert.IsFalse(swing.HeldForFlight);

            map.QueuedMissiles.Clear();

            // A creature's shot: its recovery is what starts it on the clients.
            MissileManager.Instance.MissileLaunch(map, new ActionData(creature, ActionId.WeaponAttack, 1, player.EntityId, 0), 10);

            var shot = map.QueuedMissiles.Single();
            Assert.AreEqual(14, shot.TriggerTime, "the next pass, as before");
            Assert.IsFalse(shot.HeldForFlight);
        }

        [TestMethod]
        public void AHeldShotWhoseShooterHasLeftTheMapLandsOnNothing()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var map = context.World.Map;
            var creature = Spawn(context.World, new Vector3(0, 0, -28));

            Aim(context, creature);
            Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));
            Assert.IsTrue(map.QueuedMissiles.Single().HeldForFlight);

            // Through a waypoint while it is in the air.
            context.Client.Player.MapChannel = null;
            Sent(context.Client);

            MissileManager.Instance.DoWork(map, 1000);
            context.Client.Player.MapChannel = map;

            Assert.AreEqual(0, map.QueuedMissiles.Count);
            Assert.AreEqual(10000, creature.Attributes[Attributes.Health].Current);
            Assert.IsEmpty(Sent(context.Client).OfType<WeaponAttackRecovery>().ToList());
        }

        [TestMethod]
        public void ACreaturesWeaponAttackNamesItselfInThePlayersArmourAndHealthUpdates()
        {
            using var context = new WeaponAmmoContext();
            var map = context.World.Map;
            var player = context.Client.Player;
            var creature = Spawn(context.World, new Vector3(0, 0, -10));

            Fit(context);
            player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 50, 50, 50, 0, 0);
            Sent(context.Client);

            var missile = new Missile
            {
                Source = creature,
                TargetActor = player,
                TargetEntityId = player.EntityId,
                DamageA = 60,
                ActionId = ActionId.WeaponAttack,
                ActionArgId = 1
            };

            Assert.AreEqual(creature.EntityId, MissileManager.StagedBy(missile));

            MissileManager.Instance.MissileTrigger(map, missile);

            var sent = Sent(context.Client);

            // The client leaves a bar alone when the update names somebody it can see, and moves
            // it when that somebody's damage is announced: as the shot is seen to land.
            Assert.AreEqual(creature.EntityId, sent.OfType<UpdateArmorPacket>().Single().WhoId);
            Assert.AreEqual(creature.EntityId, sent.OfType<UpdateHealthPacket>().Single().WhoId);
            Assert.AreEqual(player.EntityId, sent.OfType<WeaponAttackRecovery>().Single().Missile.Args.HitData.Single().EntityId,
                "and the recovery that announces it lists the hit");

            // A player's shot names nobody: the server has held that one for its flight itself.
            Assert.AreEqual(0UL, MissileManager.StagedBy(new Missile { Source = player, ActionId = ActionId.WeaponAttack, ActionArgId = 1 }));
            Assert.AreEqual(0UL, MissileManager.StagedBy(null));
        }

        private static void Aim(WeaponAmmoContext context, Creature creature)
        {
            Fit(context);
            context.Client.Player.Rotation = 0;
            context.Client.Player.Target = creature.EntityId;
        }

        /// <summary>The bars a player in a fight is expected to have; the context gives its player health alone.</summary>
        internal static void Fit(WeaponAmmoContext context)
        {
            foreach (var attribute in new[] { Attributes.Health, Attributes.Armor, Attributes.Power, Attributes.Regen })
                context.Client.Player.Attributes[attribute] = new ActorAttributes(attribute, 100, 100, 100, 0, 0);
        }

        private static List<PythonPacket> Sent(Client client) =>
            WorldTestContext.Drain(client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Select(message => message.Packet)
                .ToList();

        /// <summary>A hostile creature with ten thousand health and no armour, standing in the map's cells.</summary>
        internal static Creature Spawn(WorldTestContext world, Vector3 position)
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
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }
    }
}
