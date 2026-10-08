using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Models;
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Server.PerformRecovery;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Test.World;

    /// <summary>
    /// Whether a weapon's shot hits (ToHit): BASE_WEAPON_TO_HIT 100, less 7 at something walking
    /// and 15 at something running, less the Chaff around it. Rolled as the weapon is fired; a
    /// miss with nothing else to land is told to the clients at once rather than held for its
    /// flight (ShotFlight).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ToHitTests
    {
        [TestCleanup]
        public void Cleanup() => ToHit.Roll = Stuns.Roll;

        [TestMethod]
        public void AShotAtSomethingStandingDoesNotMissAndOneAtSomethingMovingMay()
        {
            using var world = new WorldTestContext();
            var shooter = world.CreateClient().Player;
            var creature = ShotFlightTests.Spawn(world, new Vector3(0, 0, -20));
            var shot = new Missile { Source = shooter, TargetActor = creature, TargetEntityId = creature.EntityId, ActionId = ActionId.WeaponAttack, ActionArgId = 1 };

            Assert.AreEqual(100, ToHit.ChanceOf(shot), "never moved");

            creature.Controller.LastMovement = new Movement(creature.Position, 2.5f, Movement.FastTurn, Vector2.Zero);
            creature.IsRunning = false;
            Assert.AreEqual(93, ToHit.ChanceOf(shot), "TARGET_WALKING_TOHIT_MODIFIER");

            creature.IsRunning = true;
            Assert.AreEqual(85, ToHit.ChanceOf(shot), "TARGET_RUNNING_TOHIT_MODIFIER");

            // Stopped: the gait it stopped in does not count.
            creature.Controller.LastMovement = new Movement(creature.Position, 0, Movement.FastTurn, Vector2.Zero);
            Assert.AreEqual(100, ToHit.ChanceOf(shot));

            // A player the same, by their run toggle.
            var target = world.CreateClient(z: -20).Player;
            var atPlayer = new Missile { Source = creature, TargetActor = target, TargetEntityId = target.EntityId, ActionId = ActionId.WeaponAttack, ActionArgId = 1 };

            Assert.AreEqual(100, ToHit.ChanceOf(atPlayer));

            target.MoveVelocity = 6.5f;
            target.IsRunning = true;
            Assert.AreEqual(85, ToHit.ChanceOf(atPlayer));

            target.IsRunning = false;
            Assert.AreEqual(93, ToHit.ChanceOf(atPlayer));
        }

        [TestMethod]
        public void ASwingIsNotRolledForAndChaffComesOffTheSameChance()
        {
            using var world = new WorldTestContext();
            var shooter = world.CreateClient().Player;
            var creature = ShotFlightTests.Spawn(world, new Vector3(0, 0, -3));

            creature.Controller.LastMovement = new Movement(creature.Position, 6f, Movement.FastTurn, Vector2.Zero);
            creature.IsRunning = true;

            var swing = new Missile { Source = shooter, TargetActor = creature, TargetEntityId = creature.EntityId, ActionId = ActionId.WeaponMelee, ActionArgId = 1, IsMelee = true };
            var shot = new Missile { Source = shooter, TargetActor = creature, TargetEntityId = creature.EntityId, ActionId = ActionId.WeaponAttack, ActionArgId = 1 };

            Assert.AreEqual(100, ToHit.ChanceOf(swing));
            Assert.AreEqual(100, ToHit.ChanceOf(new Missile { Source = shooter }), "a shot at nothing");
            Assert.AreEqual(85, ToHit.ChanceOf(shot));

            creature.ActiveEffects[1] = new GameEffect { EffectId = 1, MissPercent = 50, ExpiresTick = Environment.TickCount64 + 60000 };

            Assert.AreEqual(35, ToHit.ChanceOf(shot), "100 - 15 - Chaff's 50");
            Assert.AreEqual(100, ToHit.ChanceOf(swing), "a blow at arm's length is not turned by a cloud of foil");

            // The roll is for the chance of a miss, and is not made at all for a sure hit.
            var rolledFor = new List<int>();
            ToHit.Roll = percent => { rolledFor.Add(percent); return true; };

            Assert.IsTrue(ToHit.Misses(shot));
            Assert.IsFalse(ToHit.Misses(swing));
            CollectionAssert.AreEqual(new[] { 65 }, rolledFor);
        }

        [TestMethod]
        public void APlayerIsOnTheMoveWhenTheirLastMoveTookThemSomewhere()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var player = client.Player;

            CellManager.Instance.AddToWorld(client);
            player.PlaceAt(new Vector3(0, 0, 0));
            player.MoveBudget = 60;

            Assert.IsFalse(ToHit.IsMoving(player));

            Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(0, 0, 1), 6.5f, Movement.FastTurn, Vector2.Zero)));
            Assert.IsTrue(ToHit.IsMoving(player));

            player.IsRunning = true;
            Assert.AreEqual(ToHit.TargetRunning, ToHit.MovementModifier(player));
            player.IsRunning = false;
            Assert.AreEqual(ToHit.TargetWalking, ToHit.MovementModifier(player));

            // Says it is moving and has gone nowhere: standing.
            Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(0, 0, 1), 6.5f, Movement.FastTurn, Vector2.Zero)));
            Assert.IsFalse(ToHit.IsMoving(player));

            Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(0, 0, 2), 6.5f, Movement.FastTurn, Vector2.Zero)));
            Assert.IsTrue(ToHit.IsMoving(player));

            // Stopped, as the client says when it does.
            Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(0, 0, 2.5f), 0, Movement.FastTurn, Vector2.Zero)));
            Assert.IsFalse(ToHit.IsMoving(player));

            // Put somewhere: not moved there.
            Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(0, 0, 3), 6.5f, Movement.FastTurn, Vector2.Zero)));
            player.PlaceAt(new Vector3(5, 0, 5));
            Assert.IsFalse(ToHit.IsMoving(player));
        }

        [TestMethod]
        public void AWeaponsRollIsMadeAsItIsFiredAndAMissIsNotHeldForItsFlight()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var map = context.World.Map;
            var creature = ShotFlightTests.Spawn(context.World, new Vector3(0, 0, -28));

            creature.Controller.LastMovement = new Movement(creature.Position, 6f, Movement.FastTurn, Vector2.Zero);
            creature.IsRunning = true;
            ShotFlightTests.Fit(context);
            context.Client.Player.Rotation = 0;
            context.Client.Player.Target = creature.EntityId;

            // The roll goes for the creature: a hit, held for its 400 ms.
            var rolledFor = new List<int>();
            ToHit.Roll = percent => { rolledFor.Add(percent); return false; };

            Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));

            var hit = map.QueuedMissiles.Single();
            Assert.AreEqual(false, hit.Missed);
            Assert.AreEqual(400, hit.TriggerTime);
            CollectionAssert.AreEqual(new[] { 15 }, rolledFor, "running: 15 in a hundred, rolled once");

            map.QueuedMissiles.Clear();
            context.Client.Player.NextShotAt = 0;

            // And against it: told on the next pass, and not rolled for again as it lands.
            ToHit.Roll = percent => { rolledFor.Add(percent); return true; };

            Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));

            var miss = map.QueuedMissiles.Single();
            Assert.AreEqual(true, miss.Missed);
            Assert.AreEqual(0, miss.TriggerTime);
            Assert.IsFalse(miss.HeldForFlight);

            Sent(context.Client);
            ToHit.Roll = percent => { rolledFor.Add(percent); return false; };
            MissileManager.Instance.DoWork(map, 100);

            var recovery = Sent(context.Client).OfType<WeaponAttackRecovery>().Single();
            CollectionAssert.AreEqual(new[] { creature.EntityId }, recovery.Missile.Args.MisstEntities);
            CollectionAssert.AreEqual(new[] { MissileManager.MissTypeMiss }, recovery.Missile.Args.Missdata);
            Assert.IsEmpty(recovery.Missile.Args.HitData);
            Assert.AreEqual(10000, creature.Attributes[Attributes.Health].Current);
            Assert.AreEqual(2, rolledFor.Count, "once a shot");
        }

        [TestMethod]
        public void AMissWithASplashStillFliesAndGoesOffWhereItComesDown()
        {
            using var context = new WeaponAmmoContext();
            var map = context.World.Map;
            var player = context.Client.Player;
            var creature = ShotFlightTests.Spawn(context.World, new Vector3(0, 0, -28));
            var beside = ShotFlightTests.Spawn(context.World, new Vector3(2, 0, -28));

            ShotFlightTests.Fit(context);
            creature.Controller.LastMovement = new Movement(creature.Position, 6f, Movement.FastTurn, Vector2.Zero);
            creature.IsRunning = true;
            ToHit.Roll = _ => true;

            MissileManager.Instance.MissileLaunch(map, new ActionData(player, ActionId.WeaponAttack, 1, creature.EntityId, 0), 100,
                splashRadius: 5, flightVelocity: 0, refireMs: 1500);

            var missile = map.QueuedMissiles.Single();
            Assert.AreEqual(true, missile.Missed);
            Assert.AreEqual(400, missile.TriggerTime, "the blast is at the end of the flight");
            Assert.IsTrue(missile.HeldForFlight);

            MissileManager.Instance.DoWork(map, 400);

            Assert.AreEqual(10000, creature.Attributes[Attributes.Health].Current, "missed");
            Assert.IsTrue(beside.Attributes[Attributes.Health].Current < 10000, "caught by the blast");
        }

        [TestMethod]
        public void AMissileNotRolledForAsItWasLaunchedIsRolledForAsItLands()
        {
            using var context = new WeaponAmmoContext();
            var map = context.World.Map;
            var creature = ShotFlightTests.Spawn(context.World, new Vector3(0, 0, -10));

            ShotFlightTests.Fit(context);
            creature.ActiveEffects[1] = new GameEffect { EffectId = 1, MissPercent = 50, ExpiresTick = Environment.TickCount64 + 60000 };

            var rolledFor = new List<int>();
            ToHit.Roll = percent => { rolledFor.Add(percent); return true; };

            Sent(context.Client);
            MissileManager.Instance.MissileTrigger(map, new Missile
            {
                Source = context.Client.Player,
                TargetActor = creature,
                TargetEntityId = creature.EntityId,
                DamageA = 100,
                ActionId = ActionId.WeaponAttack,
                ActionArgId = 1
            });

            CollectionAssert.AreEqual(new[] { 50 }, rolledFor, "the Chaff around it");
            CollectionAssert.AreEqual(new[] { MissileManager.MissTypeMiss },
                Sent(context.Client).OfType<WeaponAttackRecovery>().Single().Missile.Args.Missdata);
            Assert.AreEqual(10000, creature.Attributes[Attributes.Health].Current);
        }

        private static List<PythonPacket> Sent(Client client) =>
            WorldTestContext.Drain(client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Select(message => message.Packet)
                .ToList();
    }
}
