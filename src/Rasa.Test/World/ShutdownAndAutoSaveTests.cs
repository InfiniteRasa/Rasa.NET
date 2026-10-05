extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Structures;
    using ClientState = RasaGame::Rasa.Data.ClientState;

    // The console's exit countdown (ShutdownSchedule), the removal it ends with
    // (MapChannelManager.RemoveAllFlaggedPlayers), and saving players while they play (AutoSave).
    [TestClass]
    [DoNotParallelize]
    public class ShutdownAndAutoSaveTests
    {
        private const long Start = 10_000_000;

        private int _interval;
        private Action<Client> _save;

        [TestInitialize]
        public void KeepAutoSaveSettings()
        {
            _interval = AutoSave.IntervalMinutes;
            _save = AutoSave.SaveAction;
        }

        [TestCleanup]
        public void RestoreAutoSaveSettings()
        {
            AutoSave.IntervalMinutes = _interval;
            AutoSave.SaveAction = _save;
        }

        private static List<(int SecondsLeft, string Message)> RunDown(ShutdownSchedule schedule, long stepMs)
        {
            var given = new List<(int, string)>();

            for (var now = Start; !schedule.IsDue(now); now += stepMs)
            {
                var warning = schedule.WarningDue(now);

                if (warning != null)
                    given.Add((schedule.SecondsLeft(now), warning));
            }

            return given;
        }

        [TestMethod]
        public void AFiveMinuteCountdownWarnsAtTheStartEachMinuteAndAtThirtyAndTenSeconds()
        {
            var given = RunDown(new ShutdownSchedule(Start, 5 * 60_000, null), 1000);

            CollectionAssert.AreEqual(new[] { 300, 240, 180, 120, 60, 30, 10 }, given.Select(g => g.SecondsLeft).ToArray());
            StringAssert.StartsWith(given[0].Message, "The server is shutting down in 5 minutes.");
            StringAssert.StartsWith(given[4].Message, "The server is shutting down in 1 minute.");
            StringAssert.StartsWith(given[6].Message, "The server is shutting down in 10 seconds.");
        }

        [TestMethod]
        public void TheFirstWarningGivesTheTimeAsItIsAndTheReason()
        {
            var schedule = new ShutdownSchedule(Start, 7 * 60_000 + 30_000, "  patch day ");
            var given = RunDown(schedule, 1000);

            StringAssert.StartsWith(given[0].Message, "The server is shutting down in 7 minutes 30 seconds (patch day).");
            Assert.AreEqual(300, given[1].SecondsLeft, "then the next of the fixed warnings");
            Assert.AreEqual("patch day", schedule.Reason);
        }

        [TestMethod]
        public void ASlowTickGivesOnlyTheLatestWarningItPassed()
        {
            var schedule = new ShutdownSchedule(Start, 300_000, null);

            Assert.IsNotNull(schedule.WarningDue(Start));
            StringAssert.StartsWith(schedule.WarningDue(Start + 250_000), "The server is shutting down in 1 minute.");
            Assert.IsNull(schedule.WarningDue(Start + 251_000));
            Assert.IsNull(schedule.WarningDue(Start + 300_000), "nothing once due");
            Assert.IsTrue(schedule.IsDue(Start + 300_000));
        }

        [TestMethod]
        public void NewArrivalsAreRefusedInTheLastMinute()
        {
            var schedule = new ShutdownSchedule(Start, 120_000, null);

            Assert.IsFalse(schedule.RefusesArrivals(Start + 59_000));
            Assert.IsTrue(schedule.RefusesArrivals(Start + 60_000));
            Assert.IsTrue(new ShutdownSchedule(Start, 0, null).IsDue(Start), "exit with no minutes is due at once");
        }

        [TestMethod]
        public void TimesAreWrittenOut()
        {
            Assert.AreEqual("1 second", ShutdownSchedule.Describe(1));
            Assert.AreEqual("0 seconds", ShutdownSchedule.Describe(0));
            Assert.AreEqual("1 minute 30 seconds", ShutdownSchedule.Describe(90));
            Assert.AreEqual("1 hour", ShutdownSchedule.Describe(3600));
            Assert.AreEqual("2 hours 1 minute", ShutdownSchedule.Describe(7260));
        }

        [TestMethod]
        public void AutoSaveStaggersTheFirstSaveThenSavesEachInterval()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var saved = new List<Client>();
            AutoSave.SaveAction = saved.Add;
            AutoSave.IntervalMinutes = 5;

            Assert.AreEqual(0, AutoSave.Worker(world.Map, Start), "first seen: scheduled, not saved");
            var due = client.Player.NextAutoSaveTick;
            Assert.IsTrue(due > Start && due <= Start + 300_000, "within the first interval");

            Assert.AreEqual(0, AutoSave.Worker(world.Map, due - 1));
            Assert.AreEqual(1, AutoSave.Worker(world.Map, due));
            Assert.AreSame(client, saved.Single());
            Assert.AreEqual(due + 300_000, client.Player.NextAutoSaveTick);
            Assert.AreEqual(0, AutoSave.Worker(world.Map, due + 1));
        }

        [TestMethod]
        public void AutoSaveSkipsPlayersNotStandingInTheWorldAndCanBeTurnedOff()
        {
            using var world = new WorldTestContext();
            var clients = Enumerable.Range(0, 5).Select(_ => world.CreateClient()).ToList();
            var saved = new List<Client>();
            AutoSave.SaveAction = saved.Add;
            AutoSave.IntervalMinutes = 5;

            foreach (var client in clients)
                client.Player.NextAutoSaveTick = Start;

            clients[0].State = ClientState.Loading;
            clients[1].Player.RemoveFromMap = true;
            clients[2].Player.Disconected = true;
            clients[3].Player.State = CharacterState.Dead;

            Assert.AreEqual(1, AutoSave.Worker(world.Map, Start));
            Assert.AreSame(clients[4], saved.Single());

            AutoSave.IntervalMinutes = 0;
            clients[4].Player.NextAutoSaveTick = Start;
            Assert.AreEqual(0, AutoSave.Worker(world.Map, Start), "off");
        }

        [TestMethod]
        public void AutoSaveSpreadsACrowdOverPasses()
        {
            using var world = new WorldTestContext();
            var clients = Enumerable.Range(0, AutoSave.MaxPerMapPerPass + 3).Select(_ => world.CreateClient()).ToList();
            var saved = new List<Client>();
            AutoSave.SaveAction = saved.Add;
            AutoSave.IntervalMinutes = 5;

            foreach (var client in clients)
                client.Player.NextAutoSaveTick = Start;

            Assert.AreEqual(AutoSave.MaxPerMapPerPass, AutoSave.Worker(world.Map, Start));
            Assert.AreEqual(3, AutoSave.Worker(world.Map, Start + AutoSave.PassIntervalMs));
            Assert.AreEqual(clients.Count, saved.Distinct().Count());
        }

        [TestMethod]
        public void AutoSaveCarriesOnPastAFailedSave()
        {
            using var world = new WorldTestContext();
            var first = world.CreateClient();
            var second = world.CreateClient();
            var saved = new List<Client>();
            AutoSave.SaveAction = c => { if (c == first) throw new InvalidOperationException("database gone"); saved.Add(c); };
            AutoSave.IntervalMinutes = 5;

            first.Player.NextAutoSaveTick = Start;
            second.Player.NextAutoSaveTick = Start;

            Assert.AreEqual(2, AutoSave.Worker(world.Map, Start));
            Assert.AreSame(second, saved.Single());
            Assert.AreEqual(Start + 300_000, first.Player.NextAutoSaveTick, "tried again next interval, not every pass");
        }

        [TestMethod]
        public void TheShutdownRemovesEveryFlaggedPlayerAtOnceLingeringOrNot()
        {
            using var world = new WorldTestContext();
            var maps = new MapChannelManager(null);
            maps.MapChannelArray.Add(1220, world.Map);
            var clients = Enumerable.Range(0, 4).Select(_ => world.CreateClient()).ToList();

            foreach (var client in clients)
            {
                CellManager.Instance.AddToWorld(client);
                client.State = ClientState.Disconnected;
                client.Player.Disconected = true;
                client.Player.RemoveFromMap = true;
            }

            clients[0].Player.LingerUntil = Environment.TickCount64 + 60_000;

            Assert.AreEqual(4, maps.RemoveAllFlaggedPlayers());
            Assert.AreEqual(0, world.Map.ClientList.Count);
            Assert.IsFalse(clients.Any(c => EntityManager.Instance.Players.ContainsKey(c.Player.EntityId)));
        }
    }
}
