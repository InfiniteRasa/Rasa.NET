using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Missions.Definitions;
using Rasa.Packets.Communicator.Server;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;
using Rasa.Structures.Missions;
using Rasa.Test.Missions;

namespace Rasa.Test.World
{
    /// <summary>
    /// The AFK pose (Gestures.AfkLoop, gesture 86): played by the server, to everyone the
    /// player's own client included, when the player goes AFK; ended when they come back.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AfkPoseTests
    {
        private const uint Sit = 83;

        [TestMethod]
        public void GoingAfkPlaysThePoseForEveryoneThePlayerIncluded()
        {
            using var context = Context();
            var watcher = context.CreateAdditionalClient(2);
            Clear(context, watcher);

            new ManifestationManager(context).SetAfk(context.Client, true);

            foreach (var client in new[] { context.Client, watcher })
            {
                var windup = MissionTestContext.Drain(client).OfType<PerformWindupPacket>().Single();
                Assert.AreEqual((ActionId.Gesture, Gestures.AfkLoop, 0UL), (windup.ActionId, windup.ActionArgId, windup.Arg));
            }

            var pending = context.Map.PerformRecovery.Single(action => action.Actor == context.Client.Player);
            Assert.IsTrue(pending.ServerStarted);
            Assert.AreEqual(2000L, pending.WaitTime, "the client's windupDelayMs");

            Recover(context);

            foreach (var client in new[] { context.Client, watcher })
            {
                var sent = MissionTestContext.Drain(client);
                var recovery = sent.OfType<PerformRecoveryPacket>().Single();
                Assert.AreEqual((ActionId.Gesture, Gestures.AfkLoop), (recovery.ActionId, recovery.ActionArgId), "the recovery announces the pose");
                Assert.AreEqual(Gestures.AfkLoop, sent.OfType<GestureEffectAttachedPacket>().Single().ActionArgId);
            }

            var held = Held(context).Single();
            Assert.AreEqual(Gestures.AfkLoop, held.EffectLevel);
            CollectionAssert.AreEqual(new object[] { (uint)ActionId.Gesture, Gestures.AfkLoop, null }, held.AttachArgs, "for anyone who arrives later");
        }

        [TestMethod]
        public void ComingBackEndsAHeldPoseForEveryone()
        {
            using var context = Context();
            var watcher = context.CreateAdditionalClient(2);
            var manager = new ManifestationManager(context);
            manager.SetAfk(context.Client, true);
            Recover(context);
            var effectId = Held(context).Single().EffectId;
            Clear(context, watcher);

            // Moving: the movement path's activity.
            manager.NotifyPlayerActivity(context.Client);

            Assert.IsFalse(context.Client.Player.IsAFK);
            Assert.IsEmpty(Held(context));
            foreach (var client in new[] { context.Client, watcher })
                Assert.AreEqual(effectId, MissionTestContext.Drain(client).OfType<GameEffectDetachedPacket>().Single().EffectId);
        }

        [TestMethod]
        public void ComingBackDuringTheWindupInterruptsItForEveryone()
        {
            using var context = Context();
            var watcher = context.CreateAdditionalClient(2);
            var manager = new ManifestationManager(context);
            manager.SetAfk(context.Client, true);
            Clear(context, watcher, keepPending: true);

            // /afk again.
            manager.ToggleAfk(context.Client);

            Assert.IsFalse(context.Map.PerformRecovery.Any(action => action.Actor == context.Client.Player), "no recovery left to send");
            foreach (var client in new[] { context.Client, watcher })
            {
                var interrupt = MissionTestContext.Drain(client).OfType<ActionInterruptPacket>().Single();
                Assert.AreEqual((context.Client.Player.EntityId, ActionId.Gesture, Gestures.AfkLoop), (interrupt.SourceId, interrupt.ActionId, interrupt.ActionArgId));
            }
            Assert.IsEmpty(Held(context));
        }

        [TestMethod]
        public void TheIdleTimerPlaysItWithTheWarning()
        {
            using var context = Context();
            var manager = new ManifestationManager(context);
            context.Client.Player.LastActivityTick = Environment.TickCount64 - ManifestationManager.InactiveWarningMs - 1000;
            context.Drain();

            manager.CheckInactivity(context.Map);

            Assert.IsTrue(context.Client.Player.IsAFK);
            var sent = context.Drain();
            Assert.AreEqual(Gestures.AfkLoop, sent.OfType<PerformWindupPacket>().Single().ActionArgId);
            Assert.AreEqual(1, sent.OfType<PlayerInactiveWarningPacket>().Count());
        }

        [TestMethod]
        public void SomeoneSittingStaysSittingAndKeepsSittingWhenTheyComeBack()
        {
            using var context = Context();
            var manager = new ManifestationManager(context);
            GestureManager.Instance.RequestGesture(context.Client, new RequestGesturePacket { GestureId = Sit });
            Recover(context);
            var sitting = Held(context).Single();
            context.Drain();

            manager.SetAfk(context.Client, true);
            Assert.IsEmpty(context.Drain().OfType<PerformWindupPacket>().ToList());
            Assert.IsFalse(context.Map.PerformRecovery.Any(action => action.Actor == context.Client.Player));

            manager.SetAfk(context.Client, false);
            Assert.AreSame(sitting, Held(context).Single(), "/sit is not the AFK pose");
            Assert.IsEmpty(context.Drain().OfType<GameEffectDetachedPacket>().ToList());
        }

        [TestMethod]
        public void NoPoseForTheDeadTheCrouchedOrTheMorphed()
        {
            foreach (var (why, set) in new (string, Action<Manifestation>)[]
                     {
                         ("dead", player => player.State = CharacterState.Dead),
                         ("crouched", player => player.IsCrouching = true),
                         ("morphed", player => player.MorphWeapon = new MorphWeaponItem()),
                     })
            {
                using var context = Context();
                set(context.Client.Player);
                context.Drain();

                new ManifestationManager(context).SetAfk(context.Client, true);

                Assert.IsTrue(context.Client.Player.IsAFK, why);
                Assert.IsEmpty(context.Drain().OfType<PerformWindupPacket>().ToList(), why);
                Assert.IsFalse(context.Map.PerformRecovery.Any(action => action.Actor == context.Client.Player), why);
            }
        }

        /// <summary>Runs the player's pending gesture recovery now.</summary>
        private static void Recover(MissionTestContext context)
        {
            var pending = context.Map.PerformRecovery.Single(action => action.Actor == context.Client.Player);
            context.Map.PerformRecovery.Remove(pending);
            GestureManager.Instance.PerformRecovery(context.Map, pending);
        }

        private static List<GameEffect> Held(MissionTestContext context) =>
            context.Client.Player.ActiveEffects.Values.Where(effect => effect.TypeId == Gestures.EffectTypeId).ToList();

        private static void Clear(MissionTestContext context, Client watcher, bool keepPending = false)
        {
            context.Drain();
            MissionTestContext.Drain(watcher);

            if (!keepPending)
                context.Map.PerformRecovery.RemoveAll(action => action.Actor == context.Client.Player);
        }

        private static MissionTestContext Context() =>
            MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission>(),
                new Dictionary<uint, MissionRewardDefinition>());
    }
}
