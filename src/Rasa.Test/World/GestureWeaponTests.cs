using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Missions.Definitions;
using Rasa.Packets;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;
using Rasa.Structures.Missions;
using Rasa.Test.Missions;

namespace Rasa.Test.World
{
    [TestClass]
    [DoNotParallelize]
    public class GestureWeaponTests
    {
        [TestMethod]
        public void RequestGestureWeaponReadsLikeRequestGestureAndIsRouted()
        {
            var none = Decode(writer =>
            {
                writer.WriteTuple(2);
                writer.WriteUInt(63);
                writer.WriteNoneStruct();
            });
            Assert.AreEqual(63U, none.GestureId);
            Assert.AreEqual(0UL, none.TargetEntityId);

            var targeted = Decode(writer =>
            {
                writer.WriteTuple(2);
                writer.WriteUInt(66);
                writer.WriteULong(0x100000005UL);
            });
            Assert.AreEqual(66U, targeted.GestureId);
            Assert.AreEqual(0x100000005UL, targeted.TargetEntityId);

            Assert.AreEqual(747, (int)GameOpcode.RequestGestureWeapon);
            Assert.AreEqual(typeof(RequestGestureWeaponPacket),
                new PacketRouter<ClientPacketHandler, GameOpcode>().GetPacketType(GameOpcode.RequestGestureWeapon));
        }

        [TestMethod]
        [DataRow(63U, 765U, 3000)]
        [DataRow(65U, 766U, 3000)]
        [DataRow(66U, 764U, 2666)]
        public void AFlaggedSignalPlaysForEveryoneElseUnderGestureWeapon(uint argId, uint flagId, int windupMs)
        {
            using var context = Context();
            var watcher = context.CreateAdditionalClient(2);
            context.Client.Player.PlayerFlags[flagId] = 1;
            Clear(context, watcher);

            GestureManager.Instance.RequestGestureWeapon(context.Client,
                new RequestGestureWeaponPacket { GestureId = argId, TargetEntityId = watcher.Player.EntityId });

            var windup = MissionTestContext.Drain(watcher).OfType<PerformWindupPacket>().Single();
            Assert.AreEqual(ActionId.GestureWeapon, windup.ActionId);
            Assert.AreEqual(argId, windup.ActionArgId);
            Assert.AreEqual(watcher.Player.EntityId, windup.Arg);
            Assert.IsEmpty(context.Drain().OfType<PerformWindupPacket>().ToArray(), "The signaller's client plays its own.");

            var pending = context.Map.PerformRecovery.Single(action => action.Actor == context.Client.Player);
            Assert.AreEqual(ActionId.GestureWeapon, pending.ActionId);
            Assert.AreEqual(argId, pending.ActionArgId);

            context.Map.PerformRecovery.Remove(pending);
            GestureManager.Instance.PerformRecovery(context.Map, pending);
            var recovery = MissionTestContext.Drain(watcher).OfType<PerformRecoveryPacket>().Single();
            Assert.AreEqual(ActionId.GestureWeapon, recovery.ActionId);
            Assert.AreEqual(argId, recovery.ActionArgId);
            Assert.IsFalse(context.Client.Player.ActiveEffects.Values.Any(effect => effect.TypeId == Gestures.EffectTypeId),
                "No hand signal loops.");

            Assert.IsTrue(Gestures.TryGetWeapon(argId, out var info, out var flag));
            Assert.AreEqual(windupMs, info.WindupMs);
            Assert.AreEqual(flagId, flag);
        }

        [TestMethod]
        [DataRow("no-flag")]
        [DataRow("flag-cleared")]
        [DataRow("other-flag")]
        [DataRow("unknown-signal")]
        [DataRow("dead")]
        public void AnUnflaggedUnknownOrDeadSignalPlaysForNoOne(string change)
        {
            using var context = Context();
            var watcher = context.CreateAdditionalClient(2);
            var argId = change == "unknown-signal" ? 64U : 63U;
            if (change == "flag-cleared")
                context.Client.Player.PlayerFlags[765] = 0;
            else if (change == "other-flag")
                context.Client.Player.PlayerFlags[766] = 1;
            else if (change is "unknown-signal" or "dead")
                context.Client.Player.PlayerFlags[765] = 1;
            if (change == "dead")
                context.Client.Player.State = CharacterState.Dead;
            Clear(context, watcher);

            GestureManager.Instance.RequestGestureWeapon(context.Client, new RequestGestureWeaponPacket { GestureId = argId });

            Assert.IsEmpty(MissionTestContext.Drain(watcher).OfType<PerformWindupPacket>().ToArray());
            Assert.IsFalse(context.Map.PerformRecovery.Any(action => action.Actor == context.Client.Player));
        }

        [TestMethod]
        public void ASignalReplacesAPendingGestureAndAGestureReplacesAPendingSignal()
        {
            using var context = Context();
            var watcher = context.CreateAdditionalClient(2);
            context.Client.Player.PlayerFlags[765] = 1;
            Clear(context, watcher);

            GestureManager.Instance.RequestGesture(context.Client, new RequestGesturePacket { GestureId = 4 });
            GestureManager.Instance.RequestGestureWeapon(context.Client, new RequestGestureWeaponPacket { GestureId = 63 });
            Assert.AreEqual(ActionId.GestureWeapon,
                context.Map.PerformRecovery.Single(action => action.Actor == context.Client.Player).ActionId);

            GestureManager.Instance.RequestGesture(context.Client, new RequestGesturePacket { GestureId = 4 });
            Assert.AreEqual(ActionId.Gesture,
                context.Map.PerformRecovery.Single(action => action.Actor == context.Client.Player).ActionId);
        }

        [TestMethod]
        public void AnInterruptedSignalIsInterruptedUnderGestureWeapon()
        {
            using var context = Context();
            var watcher = context.CreateAdditionalClient(2);
            context.Client.Player.PlayerFlags[766] = 1;
            GestureManager.Instance.RequestGestureWeapon(context.Client, new RequestGestureWeaponPacket { GestureId = 65 });
            var pending = context.Map.PerformRecovery.Single(action => action.Actor == context.Client.Player);
            context.Map.PerformRecovery.Remove(pending);
            pending.IsInrerrupted = true;
            Clear(context, watcher);

            GestureManager.Instance.PerformRecovery(context.Map, pending);

            var interrupt = MissionTestContext.Drain(watcher).OfType<ActionInterruptPacket>().Single();
            Assert.AreEqual(ActionId.GestureWeapon, interrupt.ActionId);
            Assert.IsEmpty(MissionTestContext.Drain(watcher).OfType<PerformRecoveryPacket>().ToArray());
        }

        private static MissionTestContext Context() =>
            MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission>(),
                new Dictionary<uint, MissionRewardDefinition>());

        private static void Clear(MissionTestContext context, Client watcher)
        {
            context.Drain();
            MissionTestContext.Drain(watcher);
            context.Map.PerformRecovery.RemoveAll(action => action.Actor == context.Client.Player);
        }

        private static RequestGestureWeaponPacket Decode(Action<PythonWriter> write)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                write(new PythonWriter(writer));
            stream.Position = 0;
            var packet = new RequestGestureWeaponPacket();
            packet.Read(new PythonReader(new BinaryReader(stream)));
            return packet;
        }
    }
}
