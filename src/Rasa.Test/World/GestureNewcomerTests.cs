using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Missions.Definitions;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;
using Rasa.Structures.Missions;
using Rasa.Test.Missions;

namespace Rasa.Test.World
{
    /// <summary>
    /// A looping gesture (/dance, /sit, /liedown) shown to a client that meets the gesturer while
    /// the pose is held: GameEffectManager.NewcomerPackets re-sends the GESTURE_EFFECT, and
    /// GestureEffect.OnAnnounceAttach(target, actionId, actionArgId, actionTargetId) needs the
    /// three arguments after the tooltip dictionary, as GestureEffectAttachedPacket sends them.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class GestureNewcomerTests
    {
        private const uint Dance = 26, Sit = 83;

        [TestMethod]
        public void ANewcomerGetsTheGestureWithItsActionArgumentsAndNoTarget()
        {
            using var context = Context();
            var newcomer = context.CreateAdditionalClient(2);

            Hold(context, Dance, 0);

            var attach = GameEffectManager.EffectsForNewcomer(context.Client.Player, newcomer.Player)
                .Single(packet => packet.EffectTypeId == Gestures.EffectTypeId);

            Assert.IsTrue(attach.Announced, "announced: the newcomer's client starts the pose at once");
            Assert.AreEqual(Dance, attach.EffectLevel);
            CollectionAssert.AreEqual(new object[] { (uint)ActionId.Gesture, Dance, null }, attach.Args);
            Assert.IsNull(attach.Duration, "held until detached: no timer");
        }

        [TestMethod]
        public void ATargetedGestureCarriesItsTargetToTheNewcomer()
        {
            using var context = Context();
            var target = context.CreateAdditionalClient(2);
            var newcomer = context.CreateAdditionalClient(3);

            Hold(context, Sit, target.Player.EntityId);

            var attach = GameEffectManager.EffectsForNewcomer(context.Client.Player, newcomer.Player)
                .Single(packet => packet.EffectTypeId == Gestures.EffectTypeId);

            CollectionAssert.AreEqual(new object[] { (uint)ActionId.Gesture, Sit, target.Player.EntityId }, attach.Args);
        }

        [TestMethod]
        public void TheNewcomersAttachEndsWithTheSameArgumentsAsTheOneSentInRange()
        {
            using var context = Context();
            var watcher = context.CreateAdditionalClient(2);
            var newcomer = context.CreateAdditionalClient(3);
            context.Drain();
            MissionTestContext.Drain(watcher);

            Hold(context, Dance, 0);

            var inRange = MissionTestContext.Drain(watcher).OfType<GestureEffectAttachedPacket>().Single();
            var later = GameEffectManager.EffectsForNewcomer(context.Client.Player, newcomer.Player)
                .Single(packet => packet.EffectTypeId == Gestures.EffectTypeId);

            Assert.AreEqual(inRange.EffectId, later.EffectId);
            Assert.AreEqual(inRange.ActionArgId, later.EffectLevel);
            CollectionAssert.AreEqual(new object[] { (uint)ActionId.Gesture, inRange.ActionArgId, null }, later.Args);
        }

        /// <summary>Requests the gesture and runs its recovery, which attaches the looping effect.</summary>
        private static void Hold(MissionTestContext context, uint gestureId, ulong targetId)
        {
            GestureManager.Instance.RequestGesture(context.Client, new RequestGesturePacket { GestureId = gestureId, TargetEntityId = targetId });

            var pending = context.Map.PerformRecovery.Single(action => action.Actor == context.Client.Player);
            context.Map.PerformRecovery.Remove(pending);
            GestureManager.Instance.PerformRecovery(context.Map, pending);

            Assert.IsTrue(context.Client.Player.ActiveEffects.Values.Any(effect => effect.TypeId == Gestures.EffectTypeId), "the pose is held");
        }

        private static MissionTestContext Context() =>
            MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission>(),
                new Dictionary<uint, MissionRewardDefinition>());
    }
}
