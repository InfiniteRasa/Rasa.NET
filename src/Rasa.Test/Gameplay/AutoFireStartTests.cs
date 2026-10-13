extern alias RasaGame;

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Config;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Structures;
    using ClientState = RasaGame::Rasa.Data.ClientState;

    // The fire button held down. The client sends StartAutoFire once, when the button goes down,
    // then AutoFireKeepAlive every 2.5 s, and nothing more until StopAutoFire when it comes up
    // (manifestation.py StartAutoFire; gameui.py StartPrimaryAction returns at once while
    // IsAutoFiring). What it does itself before it sends is one of three things, locally and
    // without a request of its own: draws the weapon if it is stowed, reloads it if the clip is
    // empty, or fires the first shot. Whichever it was, it is then "auto-firing", and every shot
    // after that is the server's to fire.
    //
    // The weapon here has a refire of 800 ms and a reload of 1500 ms, and the context's ticks are
    // the map channel worker's: the auto-fire list first, then the map's queued actions.
    [TestClass]
    [DoNotParallelize]
    public class AutoFireStartTests
    {
        private WeaponChecksConfig _previous;

        [TestInitialize]
        public void Initialize()
        {
            // A shot at a target out of the weapon's reach is refused: logged only as shipped
            // (BR-163), but a server can choose to refuse it, and the fire must carry on.
            _previous = WeaponChecks.Config;
            WeaponChecks.Config = new WeaponChecksConfig { Range = "refuse" };
        }

        [TestCleanup]
        public void Cleanup()
        {
            WeaponChecks.Config = _previous;
        }

        [TestMethod]
        public void AFirstShotThatGoesIsFollowedByOneEveryRefire()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);

            Press(manager, context);

            Assert.AreEqual(1, Shots(context), "the first shot, at once");
            Assert.IsTrue(context.Client.Player.AutoFireCombatMode);

            Tick(manager, context, 7);
            Assert.AreEqual(1, Shots(context), "nothing before the refire is up");

            Tick(manager, context);
            Assert.AreEqual(2, Shots(context), "the second, 800 ms after the first");
            Assert.AreEqual(5u, context.Weapon.CurrentAmmo);
        }

        [TestMethod]
        public void HeldWithTheWeaponStowedItIsDrawnAndThenFired()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            context.Client.Player.WeaponReady = false;

            Press(manager, context);

            Assert.IsTrue(context.Client.Player.WeaponReady, "drawn");
            Assert.AreEqual(0, Shots(context), "and not fired in the same breath");

            var draw = context.World.Map.PerformRecovery.Single(action => action.ActionId == ActionId.WeaponDraw);

            // While the draw plays, nothing.
            Tick(manager, context, Ticks(draw.WaitTime));
            Assert.AreEqual(0, Shots(context), "not while the draw is playing");
            Assert.IsEmpty(context.World.Map.PerformRecovery, "the draw is over");

            Tick(manager, context);
            Assert.AreEqual(1, Shots(context), "the weapon is fired once it is drawn, with the button still down");
            Assert.AreEqual(6u, context.Weapon.CurrentAmmo);
            Assert.IsTrue(context.Client.Player.AutoFireCombatMode, "the stance of someone firing");

            // And from there as any held fire goes.
            Tick(manager, context, 7);
            Assert.AreEqual(1, Shots(context));
            Tick(manager, context);
            Assert.AreEqual(2, Shots(context));
        }

        [TestMethod]
        public void HeldWithAnEmptyClipItIsReloadedAndThenFired()
        {
            using var context = new WeaponAmmoContext(clip: 0);
            var manager = new ManifestationManager(context);
            context.AddAmmo(30);

            Press(manager, context);

            var reload = context.World.Map.PerformRecovery.Single(action => action.ActionId == ActionId.WeaponReload);

            Assert.AreEqual(0, Shots(context));
            Assert.AreEqual(1500, reload.WaitTime);

            Tick(manager, context, Ticks(reload.WaitTime) - 1);
            Assert.AreEqual(0u, context.Weapon.CurrentAmmo, "still reloading");
            Assert.AreEqual(0, Shots(context));

            // The tick the reload ends on: the auto-fire list is walked before the map's queue,
            // so the clip is filled after the fire has had its turn.
            Tick(manager, context);
            Assert.AreEqual(20u, context.Weapon.CurrentAmmo, "reloaded");
            Assert.AreEqual(0, Shots(context));

            // And the one after it, not a refire later.
            Tick(manager, context);
            Assert.AreEqual(1, Shots(context), "the weapon is fired once it is loaded, with the button still down");
            Assert.AreEqual(19u, context.Weapon.CurrentAmmo);
        }

        [TestMethod]
        public void HeldWhileAReloadIsStillGoingItIsFiredWhenTheReloadIsOver()
        {
            // The client holds a press back until its own reload has played out; the server's
            // ends on a tick of its own, which can be the later of the two.
            using var context = new WeaponAmmoContext(clip: 3);
            var manager = new ManifestationManager(context);
            context.AddAmmo(30);

            manager.RequestWeaponReload(context.Client, false);

            var reload = context.World.Map.PerformRecovery.Single(action => action.ActionId == ActionId.WeaponReload);

            Tick(manager, context, Ticks(reload.WaitTime) - 2);

            Press(manager, context);
            Assert.AreEqual(0, Shots(context), "not out of a clip that is being changed");

            Tick(manager, context, 2);
            Assert.AreEqual(20u, context.Weapon.CurrentAmmo, "reloaded");
            Assert.AreEqual(0, Shots(context));

            Tick(manager, context);
            Assert.AreEqual(1, Shots(context), "fired once the reload is over, with the button still down");
        }

        [TestMethod]
        public void HeldOnATargetOutOfReachItIsFiredOnceTheTargetIsInReach()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var target = context.World.CreateClient(0, -200).Player;
            context.Client.Player.Rotation = 0;
            context.Client.Player.Target = target.EntityId;

            // The weapon's range is 80, which reaches 165 m. A shot at another player puts no
            // missile on the map here, so the clip is what shows it.
            Press(manager, context);
            Assert.AreEqual(7u, context.Weapon.CurrentAmmo);

            Tick(manager, context, 8);
            Assert.AreEqual(7u, context.Weapon.CurrentAmmo, "tried again a refire on, and still out of reach");

            target.Position = new System.Numerics.Vector3(0, 0, -100);

            Tick(manager, context, 7);
            Assert.AreEqual(7u, context.Weapon.CurrentAmmo);

            Tick(manager, context);
            Assert.AreEqual(6u, context.Weapon.CurrentAmmo, "fired once it can reach, with the button still down");
        }

        [TestMethod]
        public void SomeoneElsesDrawDoesNotHoldTheFireBack()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var target = context.World.CreateClient(0, -200).Player;
            context.Client.Player.Rotation = 0;
            context.Client.Player.Target = target.EntityId;

            // Another player on the map, part-way through drawing.
            context.World.Map.PerformRecovery.Add(new ActionData(target, ActionId.WeaponDraw, 1, 5000));

            Press(manager, context);
            target.Position = new System.Numerics.Vector3(0, 0, -100);

            Tick(manager, context, 7);
            Assert.AreEqual(7u, context.Weapon.CurrentAmmo);

            Tick(manager, context);
            Assert.AreEqual(6u, context.Weapon.CurrentAmmo, "a refire after the press, whatever anyone else is doing");
        }

        [TestMethod]
        public void AnInterruptedReloadDoesNotHoldTheFireBack()
        {
            // An interrupted reload stays in the map's queue until the next tick sees to it, and
            // is already over as far as firing goes.
            using var context = new WeaponAmmoContext(clip: 3);
            var manager = new ManifestationManager(context);
            var target = context.World.CreateClient(0, -200).Player;
            context.AddAmmo(30);
            context.Client.Player.Rotation = 0;
            context.Client.Player.Target = target.EntityId;

            manager.RequestWeaponReload(context.Client, false);
            context.World.Map.PerformRecovery.Single(action => action.ActionId == ActionId.WeaponReload).IsInrerrupted = true;

            Press(manager, context);
            target.Position = new System.Numerics.Vector3(0, 0, -100);

            Tick(manager, context, 7);
            Assert.AreEqual(3u, context.Weapon.CurrentAmmo, "not reloaded, and not fired yet");

            Tick(manager, context);
            Assert.AreEqual(2u, context.Weapon.CurrentAmmo, "a refire after the press, not the reload's time after it");
        }

        [TestMethod]
        public void HeldWhileStunnedItIsFiredWhenTheStunIsOver()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var player = context.Client.Player;

            player.ActiveEffects[9001] = new GameEffect { TypeId = 9001, EffectId = 9001, IsStun = true, ExpiresTick = Environment.TickCount64 + 60000 };

            Press(manager, context);
            Assert.AreEqual(0, Shots(context));

            Tick(manager, context, 8);
            Assert.AreEqual(0, Shots(context), "nothing is fired by the stunned");

            player.ActiveEffects.Remove(9001);

            Tick(manager, context, 8);
            Assert.AreEqual(1, Shots(context), "fired once the stun is over, with the button still down");
        }

        [TestMethod]
        public void PressedBeforeTheLastShotsRefireIsUpItIsFiredWhenTheRefireIsUp()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);

            Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));

            // The shot clock five seconds short of the next shot (it lets one go 250 ms early).
            context.Client.Player.NextShotAt = Environment.TickCount64 + 5250;

            Press(manager, context);
            Assert.AreEqual(1, Shots(context));
            Assert.IsTrue(context.Client.Player.AutoFireCombatMode);

            Tick(manager, context, 45);
            Assert.AreEqual(1, Shots(context), "not before the clock allows");

            Tick(manager, context, 10);
            Assert.AreEqual(2, Shots(context), "and then, with the button still down");
        }

        [TestMethod]
        public void LetGoBeforeTheDrawIsOverNothingIsFired()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            context.Client.Player.WeaponReady = false;

            Press(manager, context);
            Tick(manager, context, 2);

            manager.StopAutoFire(context.Client);
            Assert.IsFalse(context.Client.Player.AutoFireCombatMode);

            Tick(manager, context, 20);
            Assert.AreEqual(0, Shots(context));
            Assert.AreEqual(7u, context.Weapon.CurrentAmmo);
        }

        [TestMethod]
        public void WithNothingInHandNoFireIsStarted()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var hand = context.Client.Player.Inventory.EquippedInventory;
            var weapon = hand[13];

            hand[13] = 0;
            Press(manager, context);

            Assert.IsFalse(context.Client.Player.AutoFireCombatMode);

            // Armed afterwards: the press that found nothing to fire left nothing running.
            hand[13] = weapon;
            Tick(manager, context, 20);
            Assert.AreEqual(0, Shots(context));
        }

        [TestMethod]
        public void TheDeadStartNoFire()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);

            context.Client.Player.State = CharacterState.Dead;
            Press(manager, context);

            Assert.IsFalse(context.Client.Player.AutoFireCombatMode);

            // Up again: the press made while dead left nothing running.
            context.Client.Player.State = CharacterState.Normal;
            Tick(manager, context, 20);
            Assert.AreEqual(0, Shots(context));
        }

        [TestMethod]
        public void OutOfTheWorldNoFireIsStarted()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);

            context.Client.State = ClientState.Loading;
            Press(manager, context);

            Assert.IsFalse(context.Client.Player.AutoFireCombatMode);

            context.Client.State = ClientState.Ingame;
            Tick(manager, context, 20);
            Assert.AreEqual(0, Shots(context));
        }

        [TestMethod]
        public void AFireThatIsNotKeptAliveStops()
        {
            // A press whose first shot did not go is kept alive like any other: without the
            // client's keep-alives it is over in ten seconds, and nothing is fired after that.
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var target = context.World.CreateClient(0, -200).Player;
            context.Client.Player.Rotation = 0;
            context.Client.Player.Target = target.EntityId;

            manager.StartAutoFire(context.Client, 0);

            Tick(manager, context, (int)(AutoFireTimer.DefaultMaxAliveTime / TickMs));

            target.Position = new System.Numerics.Vector3(0, 0, -100);

            Tick(manager, context, 20);
            Assert.AreEqual(7u, context.Weapon.CurrentAmmo);
        }

        [TestMethod]
        public void TheTickThatFindsAShotDueIsNotChargedToTheNextShot()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);

            // A refire that is not a whole number of ticks.
            context.Weapon.ItemTemplate.WeaponInfo.Refire = 350;

            Press(manager, context);
            Assert.AreEqual(1, Shots(context));

            Tick(manager, context, 3);
            Assert.AreEqual(1, Shots(context), "300 ms: not yet");

            Tick(manager, context);
            Assert.AreEqual(2, Shots(context), "the second at 400 ms, the first tick past 350");

            Tick(manager, context, 2);
            Assert.AreEqual(2, Shots(context), "600 ms: not yet");

            // 350 after the second came due, not 400 after the tick that fired it.
            Tick(manager, context);
            Assert.AreEqual(3, Shots(context), "the third at 700 ms, not 800");

            Tick(manager, context, 3);
            Assert.AreEqual(3, Shots(context), "1,000 ms: not yet");

            Tick(manager, context);
            Assert.AreEqual(4, Shots(context), "the fourth at 1,100 ms: the weapon's 350, not the grid's 400");
        }

        [TestMethod]
        public void AReloadInTheMiddleOfAHeldFireIsFollowedByAShotAsSoonAsItIsOver()
        {
            using var context = new WeaponAmmoContext(clip: 1);
            var manager = new ManifestationManager(context);
            context.AddAmmo(30);

            Press(manager, context);
            Assert.AreEqual(1, Shots(context));
            Assert.AreEqual(0u, context.Weapon.CurrentAmmo);

            // The refire comes up on an empty clip: a reload is asked for in place of the shot.
            Tick(manager, context, 8);

            var reload = context.World.Map.PerformRecovery.Single(action => action.ActionId == ActionId.WeaponReload);

            Assert.AreEqual(1, Shots(context));
            Assert.AreEqual(1500, reload.WaitTime);

            // The reload ends with the queue pass of its 15th tick, the one that asked for it
            // included; the walk of that tick found the clip still empty.
            Tick(manager, context, 14);
            Assert.AreEqual(20u, context.Weapon.CurrentAmmo, "reloaded");
            Assert.AreEqual(1, Shots(context));

            // The walk after it fires - not a whole refire after the reload was asked for.
            Tick(manager, context);
            Assert.AreEqual(2, Shots(context), "fired the tick after the reload ended");
            Assert.AreEqual(19u, context.Weapon.CurrentAmmo);
        }

        private const long TickMs = 100;

        /// <summary>The button going down, as the client sends it: StartAutoFire, then the first keep-alive.</summary>
        private static void Press(ManifestationManager manager, WeaponAmmoContext context)
        {
            manager.StartAutoFire(context.Client, 0);
            manager.AutoFireKeepAlive(context.Client, 2500);
        }

        private static int Shots(WeaponAmmoContext context) => context.World.Map.QueuedMissiles.Count;

        /// <summary>How many ticks it takes a queued action to come due.</summary>
        private static int Ticks(long waitTime) => (int)Math.Ceiling(waitTime / (double)TickMs);

        /// <summary>
        /// Ticks of the map channel worker, as far as a held fire goes and in its order: the
        /// auto-fire list, then the map's queued actions (ActorActionManager.DoWork), of which a
        /// reload that has come due fills the clip and an interrupted one is due at once.
        ///
        /// The shot clock goes by the machine's time, which these ticks do not take, so it is
        /// wound back before each: the timer's own delay is what spaces the shots here.
        /// </summary>
        private static void Tick(ManifestationManager manager, WeaponAmmoContext context, int ticks = 1)
        {
            var queue = context.World.Map.PerformRecovery;

            for (var tick = 0; tick < ticks; tick++)
            {
                context.Client.Player.NextShotAt = 0;

                manager.AutoFireTimerDoWork(TickMs);

                for (var i = queue.Count - 1; i >= 0; i--)
                {
                    var action = queue[i];

                    if (action.IsInrerrupted)
                        action.PassedTime = action.WaitTime;

                    action.PassedTime += TickMs;

                    if (action.PassedTime < action.WaitTime)
                        continue;

                    queue.RemoveAt(i);

                    if (action.ActionId == ActionId.WeaponReload)
                        manager.WeaponReload(action);
                }
            }
        }
    }
}
