using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Test.Missions;

    [TestClass]
    [DoNotParallelize]
    public class SkyClockTests
    {
        private const string Wilderness = "adv_foreas_concordia_wilderness";

        private long _now;

        [TestInitialize]
        public void Initialize()
        {
            _now = 9_000_000;
            SkyClock.Now = () => _now;
        }

        [TestCleanup]
        public void Cleanup()
        {
            SkyClock.Now = () => System.Environment.TickCount64;
        }

        private static MapChannel Channel(uint mapContextId, string name) => new MapChannel
        {
            MapInfo = new MapInfo(mapContextId, name, 1556, 0),
            ClientList = new List<Rasa.Game.Client>(),
            PlayerLimit = 128
        };

        private static List<SetSkyTimePacket> SkyTimes(Rasa.Game.Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Where(message => message.EntityId == (ulong)SysEntity.ClientGameMapId)
                .Select(message => message.Packet).OfType<SetSkyTimePacket>().ToList();

        [TestMethod]
        public void ASkyRunsFromTheMomentItsChannelIsMade()
        {
            var before = System.Environment.TickCount64;
            var map = Channel(1220, Wilderness);

            // Made now, by the machine's own tick.
            Assert.IsTrue(map.SkyStartedTick >= before && map.SkyStartedTick <= System.Environment.TickCount64);

            map.SkyStartedTick = _now;
            Assert.AreEqual(0, SkyClock.RunningSeconds(map));

            _now += 999;
            Assert.AreEqual(0, SkyClock.RunningSeconds(map), "whole seconds");

            _now += 1;
            Assert.AreEqual(1, SkyClock.RunningSeconds(map));

            _now += 5_400_000;
            Assert.AreEqual(5401, SkyClock.RunningSeconds(map));

            // A clock that reads before the start is no negative time.
            map.SkyStartedTick = _now + 60_000;
            Assert.AreEqual(0, SkyClock.RunningSeconds(map));
            Assert.AreEqual(0, SkyClock.RunningSeconds(null));
        }

        [TestMethod]
        public void APlayerComingOntoAMapIsSentHowLongItsSkyHasRun()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var client = harness.Client;
            var map = client.Player.MapChannel;

            map.SkyStartedTick = _now - 4_321_000;
            harness.Drain();

            ManifestationManager.Instance.AssignPlayer(client);

            Assert.AreEqual(4321, harness.Drain().OfType<SetSkyTimePacket>().Single().RunningTime);

            // Later, and again on arriving: the clock has run on, and the value is no constant.
            _now += 600_000;
            ManifestationManager.Instance.AssignPlayer(client);

            Assert.AreEqual(4921, harness.Drain().OfType<SetSkyTimePacket>().Single().RunningTime);
        }

        [TestMethod]
        public void EveryoneOnAMapIsSentOneClockAndACopyOfTheMapHasItsOwn()
        {
            using var world = new WorldTestContext();
            var first = world.CreateClient();
            var second = world.CreateClient();
            var copy = Channel(1220, Wilderness);
            var visitor = world.CreateClient();

            world.Map.ClientList.Remove(visitor);
            visitor.Player.MapChannel = copy;
            copy.ClientList.Add(visitor);

            world.Map.SkyStartedTick = _now - 7_200_000;
            copy.SkyStartedTick = _now - 90_000;

            foreach (var client in new[] { first, second, visitor })
            {
                WorldTestContext.Drain(client);
                SkyClock.Send(client);
            }

            Assert.AreEqual(7200, SkyTimes(first).Single().RunningTime);
            Assert.AreEqual(7200, SkyTimes(second).Single().RunningTime);
            Assert.AreEqual(90, SkyTimes(visitor).Single().RunningTime, "a copy of the map opens at its own start");
        }

        [TestMethod]
        public void TheTimeOfDayIsTheMapsStartPlusTheRunningTimeOverItsDay()
        {
            var map = Channel(1220, Wilderness);

            Assert.IsTrue(SkyClock.TryGetDay(map, out var day));
            Assert.AreEqual(new SkyClock.Day(10800, 3375), day, "the Wilderness's day is three hours and its sky starts at 07:30");

            // As the client works it out: (start + running) % dayLength / dayLength.
            map.SkyStartedTick = _now;
            Assert.IsTrue(SkyClock.TryGetTimeOfDay(map, out var timeOfDay, out _));
            Assert.AreEqual(3375.0 / 10800.0, timeOfDay, 1e-9);
            Assert.AreEqual("07:30", SkyClock.ClockText(timeOfDay));

            _now += 2_025_000;
            SkyClock.TryGetTimeOfDay(map, out timeOfDay, out _);
            Assert.AreEqual(0.5, timeOfDay, 1e-9);
            Assert.AreEqual("12:00", SkyClock.ClockText(timeOfDay));

            // Round the day and on: three hours later it is noon again.
            _now += 10_800_000;
            SkyClock.TryGetTimeOfDay(map, out timeOfDay, out _);
            Assert.AreEqual(0.5, timeOfDay, 1e-9);

            // The constant the server used to send, whatever the hour: 14:18 on this map, on every arrival.
            map.SkyStartedTick = _now - 6_666_666_000;
            SkyClock.TryGetTimeOfDay(map, out timeOfDay, out _);
            Assert.AreEqual("14:18", SkyClock.ClockText(timeOfDay));

            Assert.AreEqual("00:00", SkyClock.ClockText(0));
            Assert.AreEqual("23:59", SkyClock.ClockText(0.99999));
        }

        [TestMethod]
        public void SettingATimeOfDayRunsTheSkyOnToItAndTellsEveryoneOnTheMap()
        {
            using var world = new WorldTestContext();
            var first = world.CreateClient();
            var second = world.CreateClient();
            var elsewhere = world.CreateClient();
            var other = Channel(2051, "adv_foreas_howlingmaw1");

            world.Map.ClientList.Remove(elsewhere);
            elsewhere.Player.MapChannel = other;
            other.ClientList.Add(elsewhere);

            // 07:30 + 1000 s = 3375 + 1000 of 10800.
            world.Map.SkyStartedTick = _now - 1_000_000;
            other.SkyStartedTick = _now - 50_000;

            foreach (var client in new[] { first, second, elsewhere })
                WorldTestContext.Drain(client);

            // Noon is 1025 s ahead.
            Assert.IsTrue(SkyClock.TrySetTimeOfDay(world.Map, 0.5));
            Assert.AreEqual(2025, SkyClock.RunningSeconds(world.Map));
            Assert.AreEqual(2025, SkyTimes(first).Single().RunningTime);
            Assert.AreEqual(2025, SkyTimes(second).Single().RunningTime);
            Assert.AreEqual(0, SkyTimes(elsewhere).Count, "another map was told");
            Assert.AreEqual(50, SkyClock.RunningSeconds(other));

            // Back to 07:30 is on, not back: the rest of this day and the start of the next.
            Assert.IsTrue(SkyClock.TrySetTimeOfDay(world.Map, 3375.0 / 10800.0));
            Assert.AreEqual(10800, SkyClock.RunningSeconds(world.Map));
            SkyClock.TryGetTimeOfDay(world.Map, out var timeOfDay, out _);
            Assert.AreEqual("07:30", SkyClock.ClockText(timeOfDay));

            // The time it already is moves nothing.
            Assert.IsTrue(SkyClock.TrySetTimeOfDay(world.Map, 3375.0 / 10800.0));
            Assert.AreEqual(10800, SkyClock.RunningSeconds(world.Map));

            // And it runs on from there.
            _now += 60_000;
            Assert.AreEqual(10860, SkyClock.RunningSeconds(world.Map));

            Assert.IsFalse(SkyClock.TrySetTimeOfDay(world.Map, 1.0));
            Assert.IsFalse(SkyClock.TrySetTimeOfDay(world.Map, -0.1));
            Assert.IsFalse(SkyClock.TrySetTimeOfDay(world.Map, double.NaN));
        }

        [TestMethod]
        public void AGameMasterReadsAndSetsTheTimeOfDayOfTheMapTheyAreOn()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var client = harness.Client;
            var map = client.Player.MapChannel;

            client.AccountEntry.Level = (byte)GmLevel.Admin;

            var commands = new ChatCommandsManager(null);
            commands.RegisterChatCommands();

            // The harness's map goes by a name of its own; the map it stands for is Bootcamp, whose
            // day is three hours and whose sky starts at midnight: 2700 s on is 06:00.
            map.MapInfo.MapName = "adv_bootcamp";
            Assert.AreEqual(new SkyClock.Day(10800, 0), SkyClock.Days[map.MapInfo.MapName]);
            map.SkyStartedTick = _now - 2_700_000;
            harness.Drain();

            commands.ProcessCommand(client, ".skytime");

            var sent = harness.Drain();
            var said = sent.OfType<SystemMessagePacket>().Single().TextMessage;

            StringAssert.Contains(said, "(2700 s)");
            StringAssert.Contains(said, "180 min");
            StringAssert.Contains(said, "06:00");
            Assert.AreEqual(0, sent.OfType<SetSkyTimePacket>().Count(), "reading the clock set it");

            commands.ProcessCommand(client, ".setskytime 18:30");

            sent = harness.Drain();
            Assert.AreEqual(8325, sent.OfType<SetSkyTimePacket>().Single().RunningTime);
            Assert.AreEqual(8325, SkyClock.RunningSeconds(map));
            StringAssert.Contains(sent.OfType<SystemMessagePacket>().Single().TextMessage, "18:30");

            // An hour alone is that hour; and an earlier hour is tomorrow's.
            commands.ProcessCommand(client, ".setskytime 6");

            sent = harness.Drain();
            Assert.AreEqual(13500, sent.OfType<SetSkyTimePacket>().Single().RunningTime);

            // Not a time of day: said so, and the sky left alone.
            foreach (var wrong in new[] { ".setskytime", ".setskytime 25:00", ".setskytime 12:60", ".setskytime noon", ".setskytime 1:2:3" })
            {
                commands.ProcessCommand(client, wrong);

                sent = harness.Drain();
                StringAssert.StartsWith(sent.OfType<SystemMessagePacket>().Single().TextMessage, "usage:", wrong);
                Assert.AreEqual(0, sent.OfType<SetSkyTimePacket>().Count(), wrong);
            }

            Assert.AreEqual(13500, SkyClock.RunningSeconds(map));
        }

        [TestMethod]
        public void AMapWhoseDayIsNotKnownStillHasARunningSky()
        {
            var map = Channel(1991, "test_lridout_outpostcombat");

            map.SkyStartedTick = _now - 30_000;

            Assert.IsFalse(SkyClock.TryGetDay(map, out _));
            Assert.IsFalse(SkyClock.TryGetTimeOfDay(map, out _, out _));
            Assert.IsFalse(SkyClock.TrySetTimeOfDay(map, 0.5));
            Assert.AreEqual(30, SkyClock.RunningSeconds(map));

            SkyClock.SetRunningSeconds(map, 500);
            Assert.AreEqual(500, SkyClock.RunningSeconds(map));
        }

        [TestMethod]
        public void TheTableHasTheClientsDayForItsMaps()
        {
            Assert.AreEqual(77, SkyClock.Days.Count);
            Assert.AreEqual(new SkyClock.Day(10800, 0), SkyClock.Days["adv_bootcamp"]);
            Assert.AreEqual(new SkyClock.Day(10800, 7000), SkyClock.Days["adv_foreas_howlingmaw1"]);
            Assert.AreEqual(new SkyClock.Day(360000, 0), SkyClock.Days["adv_foreas_concordia_palisades_elohtemples"]);
            Assert.AreEqual(new SkyClock.Day(3600, 0), SkyClock.Days["adv_arieki_torden_abyss_omegalabs"]);

            foreach (var (name, day) in SkyClock.Days)
            {
                Assert.IsTrue(day.Length >= 3600, name);
                Assert.IsTrue(day.Start >= 0 && day.Start < day.Length, name);
            }
        }
    }
}
