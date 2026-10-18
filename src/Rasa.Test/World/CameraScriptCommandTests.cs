using System;
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
    using Rasa.Packets.ClientMethod.Server;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.Char;

    /// <summary>The camera scripts' keyframes (CameraScriptTable) and .camerascript, which finds them.</summary>
    [TestClass]
    [DoNotParallelize]
    public class CameraScriptCommandTests
    {
        [TestMethod]
        public void EveryScriptOfTheClientsMapsIsThereWithItsKeyframes()
        {
            Assert.HasCount(129, CameraScriptTable.All);
            Assert.AreEqual(37, CameraScriptTable.Maps().Count());
            Assert.AreEqual(502, CameraScriptTable.All.Sum(s => s.Keyframes));

            // Numbered from 1 on each map, with no gaps.
            foreach (var (map, count) in CameraScriptTable.Maps())
                CollectionAssert.AreEqual(Enumerable.Range(1, count).Select(i => (uint)i).ToList(),
                    CameraScriptTable.OnMap(map).Select(s => s.ScriptId).OrderBy(id => id).ToList(), map);

            // The lengths read before the keyframes were kept: the arrive and hold times and the return.
            Assert.AreEqual(8000, CameraScriptTable.Find("adv_bootcamp", 1).LengthMs);
            Assert.AreEqual(50500, CameraScriptTable.Find("adv_arieki_ligo_thunderhead_quassostation", 1).LengthMs);
            Assert.AreEqual(44, CameraScriptTable.Find("adv_arieki_ligo_thunderhead_quassostation", 1).Keyframes);
            Assert.AreEqual(31250, CameraScriptTable.Find("adv_arieki_ligo_crucible_wbfacility", 1).LengthMs);
            Assert.IsTrue(CameraScriptTable.Maps().Where(m => m.MapName.StartsWith("adv_wargame", StringComparison.Ordinal))
                .SelectMany(m => CameraScriptTable.OnMap(m.MapName)).All(s => s.Keyframes == 1 && s.LengthMs == 1999998));

            // The Proving Grounds' one, as the .map has it.
            var bootcamp = CameraScriptTable.Find("adv_bootcamp", 1);
            Assert.AreEqual(new Vector3(-193.2891f, 155.0195f, 49.2565f), bootcamp.First.Position);
            Assert.AreEqual(new Vector3(-198.3662f, 139f, 1.3152f), bootcamp.Last.Position);
            Assert.AreEqual(4000, bootcamp.Last.HoldMs);

            foreach (var frame in CameraScriptTable.All.SelectMany(s => s.Frames))
            {
                Assert.AreEqual(1f, frame.Orientation.Length(), 0.01f, "a rotation");
                Assert.IsTrue(float.IsFinite(frame.Position.X) && float.IsFinite(frame.Position.Y) && float.IsFinite(frame.Position.Z));
            }
        }

        [TestMethod]
        public void TheCamerasLookDownAtWhatTheyShowAndTheFacingIsTheWayTheyLook()
        {
            var frames = CameraScriptTable.All.SelectMany(s => s.Frames).ToList();
            Assert.IsTrue(frames.Count(frame => frame.Forward.Y < 0) > frames.Count * 0.85, "most look down");

            // Unturned, the camera looks along +z: a facing of pi, as a model's rotation goes.
            var unturned = new CameraKeyframe(0, 0, 0, 1, 0, 0, 0, 1, 0, 0);
            Assert.AreEqual(Vector3.UnitZ, unturned.Forward);
            Assert.AreEqual(Math.PI, unturned.Facing, 1e-6);

            foreach (var frame in frames)
            {
                var r = frame.Facing;
                var level = Vector2.Normalize(new Vector2(frame.Forward.X, frame.Forward.Z));
                Assert.AreEqual(level.X, -Math.Sin(r), 1e-3);
                Assert.AreEqual(level.Y, -Math.Cos(r), 1e-3);
            }
        }

        [TestMethod]
        public void TheCommandListsFindsDescribesGoesToAndPlaysThem()
        {
            using var world = new WorldTestContext();
            world.Map.MapInfo = new MapInfo(1985, "adv_bootcamp", 1, 0);
            var client = world.CreateClient();
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, new GameAccountEntry { Level = (byte)GmLevel.Admin });
            client.Player.MapContextId = 1985;
            client.Player.PlaceAt(new Vector3(-190f, 120f, 40f));
            CellManager.Instance.AddToWorld(client);

            var commands = new ChatCommandsManager(null);
            commands.RegisterChatCommands();

            List<string> Say(string command)
            {
                WorldTestContext.Drain(client);
                commands.ProcessCommand(client, command);
                return Methods(client).OfType<SystemMessagePacket>().Select(message => message.TextMessage).ToList();
            }

            var listed = Say(".camerascript list");
            StringAssert.Contains(listed[0], "adv_bootcamp: 1 camera script(s)");
            StringAssert.Contains(listed[1], "1: 3 keyframe(s), 8 s, from (-193.3, 155, 49.3) to (-198.4, 139, 1.3)");
            StringAssert.Contains(listed[1], "m from you");

            var near = Say(".camerascript near");
            StringAssert.Contains(near[0], "the 1 camera script(s) nearest you, of 1");

            var info = Say(".camerascript info 1");
            Assert.HasCount(4, info);
            StringAssert.Contains(info[0], "3 keyframe(s), 8 s");
            StringAssert.Contains(info[3], "3: at (-198.4, 139, 1.3)");
            StringAssert.Contains(info[3], "holds 4 s");

            var went = Say(".camerascript goto 1 3");
            StringAssert.Contains(went.Single(), "keyframe 3 of 3");
            Assert.AreEqual(new Vector3(-198.3662f, 139f, 1.3152f), client.Player.Position);
            Assert.AreEqual(CameraScriptTable.Find("adv_bootcamp", 1).Last.Facing, client.Player.Rotation, 1e-5);

            StringAssert.Contains(Say(".camerascript goto 1 4").Single(), "keyframes 1 to 3");
            StringAssert.Contains(Say(".camerascript info 2").Single(), "adv_bootcamp has no camera script 2");

            var maps = Say(".camerascript maps");
            StringAssert.Contains(maps[0], "37 maps carry 129 camera scripts");
            Assert.IsTrue(maps.Any(line => line.Contains("adv_bootcamp: 1 (this map)")));

            WorldTestContext.Drain(client);
            commands.ProcessCommand(client, ".camerascript 1");
            var played = Methods(client);
            Assert.AreEqual(1u, played.OfType<RunCameraScriptPacket>().Single().ScriptId);
            StringAssert.Contains(played.OfType<SystemMessagePacket>().Single().TextMessage, "Playing camera script 1");
        }

        [TestMethod]
        public void GoingToACameraNeedsGameMaster()
        {
            using var world = new WorldTestContext();
            world.Map.MapInfo = new MapInfo(1985, "adv_bootcamp", 1, 0);
            var client = world.CreateClient();
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, new GameAccountEntry { Level = (byte)GmLevel.Observer });
            client.Player.PlaceAt(new Vector3(-190f, 120f, 40f));
            CellManager.Instance.AddToWorld(client);

            var commands = new ChatCommandsManager(null);
            commands.RegisterChatCommands();
            WorldTestContext.Drain(client);

            commands.ProcessCommand(client, ".camerascript goto 1");

            StringAssert.Contains(Methods(client).OfType<SystemMessagePacket>().Single().TextMessage, "needs GameMaster");
            Assert.AreEqual(new Vector3(-190f, 120f, 40f), client.Player.Position);
        }

        private static List<PythonPacket> Methods(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet).ToList();
    }
}
