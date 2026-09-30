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
using Rasa.Packets.ClientMethod.Server;
using Rasa.Packets.Communicator.Server;
using Rasa.Packets.Manifestation.Client;
using Rasa.Structures;
using Rasa.Structures.Missions;
using Rasa.Test.Missions;

namespace Rasa.Test.World
{
    [TestClass]
    [DoNotParallelize]
    public class ScriptableClientEventTests
    {
        [TestMethod]
        public void TheIdsAreTheClientsSixteen()
        {
            var names = new Dictionary<uint, string>
            {
                [1] = "MOVE_FORWARD", [2] = "MOVE_BACKWARD", [3] = "MOVE_LEFT", [4] = "MOVE_RIGHT",
                [5] = "EQUIPPED_WEAPON", [6] = "ARMED_ABILITY", [7] = "OPENED_RADIAL_MENU", [8] = "CROUCHED",
                [9] = "USED_STICKY_TARGETTING", [10] = "FIRE_WEAPON", [11] = "FIRE_WEAPON_ALT", [12] = "FIRE_RELOAD",
                [13] = "FIRE_ABILITY", [14] = "USE_OBJECT", [15] = "OPEN_MISSION_LOG", [16] = "EQUIPPED_WEARABLE"
            };

            var values = Enum.GetValues(typeof(ScriptableClientEvent)).Cast<ScriptableClientEvent>().ToArray();
            Assert.HasCount(16, values);
            foreach (var (id, name) in names)
            {
                Assert.IsTrue(ScriptableClientEvents.TryParse(name, out var parsed), name);
                Assert.AreEqual(id, (uint)parsed, name);
                Assert.IsTrue(ScriptableClientEvents.TryParse(id.ToString(), out parsed));
                Assert.AreEqual(id, (uint)parsed);
            }

            Assert.IsFalse(ScriptableClientEvents.TryParse("0", out _));
            Assert.IsFalse(ScriptableClientEvents.TryParse("17", out _));
            Assert.IsFalse(ScriptableClientEvents.TryParse("jump", out _));
        }

        [TestMethod]
        public void StartAndStopAreOneIdTuplesToTheClientMethodEntity()
        {
            foreach (var (packet, opcode) in new (ServerPythonPacket, int)[]
                     {
                         (new StartTrackingScriptableClientEventPacket(ScriptableClientEvent.Crouched), 644),
                         (new StopTrackingScriptableClientEventPacket(ScriptableClientEvent.Crouched), 645)
                     })
            {
                Assert.AreEqual(opcode, (int)packet.Opcode);
                using var reader = new PythonReader(new BinaryReader(new MemoryStream(MissionTestContext.Encode(packet))));
                Assert.AreEqual(1, reader.ReadTuple());
                Assert.AreEqual(8U, reader.ReadUInt());
            }
        }

        [TestMethod]
        public void TheReportReadsItsIdAndIsRouted()
        {
            Assert.AreEqual(8U, Decode(writer => { writer.WriteTuple(1); writer.WriteUInt(8); }).EventId);
            Assert.AreEqual(0U, Decode(writer => { writer.WriteTuple(1); writer.WriteNoneStruct(); }).EventId);

            Assert.AreEqual(643, (int)GameOpcode.ScriptableClientEvent);
            Assert.AreEqual(typeof(ScriptableClientEventPacket),
                new PacketRouter<ClientPacketHandler, GameOpcode>().GetPacketType(GameOpcode.ScriptableClientEvent));
        }

        [TestMethod]
        public void StartSendsAndTracksStopSendsAndForgets()
        {
            using var context = Context();
            context.Drain();

            Assert.IsTrue(ScriptableClientEvents.Start(context.Client, ScriptableClientEvent.OpenMissionLog));
            Assert.IsTrue(ScriptableClientEvents.Start(context.Client, ScriptableClientEvent.MoveForward));
            var started = context.Drain().OfType<StartTrackingScriptableClientEventPacket>().Select(p => p.EventId).ToArray();
            CollectionAssert.AreEqual(new[] { ScriptableClientEvent.OpenMissionLog, ScriptableClientEvent.MoveForward }, started);
            CollectionAssert.AreEqual(new[] { ScriptableClientEvent.MoveForward, ScriptableClientEvent.OpenMissionLog },
                ScriptableClientEvents.Tracked(context.Client.Player).ToArray());

            Assert.IsTrue(ScriptableClientEvents.Stop(context.Client, ScriptableClientEvent.MoveForward));
            Assert.AreEqual(ScriptableClientEvent.MoveForward,
                context.Drain().OfType<StopTrackingScriptableClientEventPacket>().Single().EventId);
            Assert.IsFalse(ScriptableClientEvents.IsTracking(context.Client.Player, ScriptableClientEvent.MoveForward));

            Assert.AreEqual(1, ScriptableClientEvents.StopAll(context.Client));
            Assert.IsEmpty(ScriptableClientEvents.Tracked(context.Client.Player).ToArray());

            Assert.IsFalse(ScriptableClientEvents.Start(context.Client, (ScriptableClientEvent)17));
            Assert.IsEmpty(context.Drain().OfType<StartTrackingScriptableClientEventPacket>().ToArray());
        }

        [TestMethod]
        public void OnlyATrackedKnownEventReachesTheHandlers()
        {
            using var context = Context();
            var heard = new List<(Client, ScriptableClientEvent)>();
            Action<Client, ScriptableClientEvent> listener = (client, eventId) => heard.Add((client, eventId));
            ScriptableClientEvents.Occurred += listener;
            try
            {
                Report(context.Client, 8);
                Assert.IsEmpty(heard, "Not tracked.");

                ScriptableClientEvents.Start(context.Client, ScriptableClientEvent.Crouched);
                Report(context.Client, 8);
                Report(context.Client, 8);
                Report(context.Client, 99);
                Assert.HasCount(2, heard, "Every report counts; unknown ids are dropped.");
                Assert.AreSame(context.Client, heard[0].Item1);
                Assert.AreEqual(ScriptableClientEvent.Crouched, heard[0].Item2);

                ScriptableClientEvents.Stop(context.Client, ScriptableClientEvent.Crouched);
                Report(context.Client, 8);
                Assert.HasCount(2, heard);
            }
            finally
            {
                ScriptableClientEvents.Occurred -= listener;
            }
        }

        [TestMethod]
        public void AThrowingHandlerDoesNotStopTheOthers()
        {
            using var context = Context();
            var heard = 0;
            Action<Client, ScriptableClientEvent> bad = (_, _) => throw new InvalidOperationException("test");
            Action<Client, ScriptableClientEvent> good = (_, _) => heard++;
            ScriptableClientEvents.Occurred += bad;
            ScriptableClientEvents.Occurred += good;
            try
            {
                ScriptableClientEvents.Start(context.Client, ScriptableClientEvent.FireWeapon);
                Report(context.Client, 10);
                Assert.AreEqual(1, heard);
            }
            finally
            {
                ScriptableClientEvents.Occurred -= bad;
                ScriptableClientEvents.Occurred -= good;
            }
        }

        [TestMethod]
        public void TheCommandTracksEchoesAndStops()
        {
            using var context = Context();
            context.Client.AccountEntry.Level = (byte)GmLevel.Observer;
            var commands = new ChatCommandsManager(new NpcManager(context, context.Manager));
            commands.RegisterChatCommands();
            context.Drain();

            commands.ProcessCommand(context.Client, ".clientevent track crouched MOVE_FORWARD 15 bogus");
            var packets = context.Drain();
            CollectionAssert.AreEquivalent(
                new[] { ScriptableClientEvent.Crouched, ScriptableClientEvent.MoveForward, ScriptableClientEvent.OpenMissionLog },
                packets.OfType<StartTrackingScriptableClientEventPacket>().Select(p => p.EventId).ToArray());
            Assert.IsTrue(packets.OfType<SystemMessagePacket>().Any(m => m.TextMessage.Contains("bogus")));

            Report(context.Client, 8);
            Assert.IsTrue(context.Drain().OfType<SystemMessagePacket>().Any(m => m.TextMessage == "Client event 8 Crouched."));

            commands.ProcessCommand(context.Client, ".clientevent track all");
            Assert.HasCount(16, context.Drain().OfType<StartTrackingScriptableClientEventPacket>().ToArray());

            commands.ProcessCommand(context.Client, ".clientevent stop all");
            Assert.HasCount(16, context.Drain().OfType<StopTrackingScriptableClientEventPacket>().ToArray());
            Assert.IsFalse(context.Client.Player.EchoClientEvents);

            context.Client.AccountEntry.Level = 0;
            commands.ProcessCommand(context.Client, ".clientevent track 8");
            Assert.IsEmpty(context.Drain().OfType<StartTrackingScriptableClientEventPacket>().ToArray());
        }

        private static void Report(Client client, uint eventId)
        {
            var handler = new ClientPacketHandler();
            handler.RegisterClient(client);
            new PacketRouter<ClientPacketHandler, GameOpcode>().RoutePacket(handler, new ScriptableClientEventPacket { EventId = eventId });
        }

        private static MissionTestContext Context() =>
            MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission>(),
                new Dictionary<uint, MissionRewardDefinition>());

        private static ScriptableClientEventPacket Decode(Action<PythonWriter> write)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                write(new PythonWriter(writer));
            stream.Position = 0;
            var packet = new ScriptableClientEventPacket();
            packet.Read(new PythonReader(new BinaryReader(stream)));
            return packet;
        }
    }
}
