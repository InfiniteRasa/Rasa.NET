using System;
using System.Collections.Generic;

namespace Rasa.Timer
{
    public class Timer
    {
        private readonly Dictionary<string, TimedItem> _timedItems = new();

        public void Add(string name, long timer, bool repeating, Action action)
        {
            lock (_timedItems)
            {
                if (_timedItems.ContainsKey(name))
                    _timedItems.Remove(name);

                _timedItems.Add(name, new TimedItem(name, timer, repeating, action));
            }
        }

        public void Remove(string name)
        {
            lock (_timedItems)
            {
                if (_timedItems.ContainsKey(name))
                    _timedItems.Remove(name);
            }
        }

        /// <summary>
        /// Fires whatever is due. Driven by a server's main loop, so nothing here may throw:
        /// the loop thread has no handler above it and an escaping exception ends the process.
        ///
        /// Each callback is caught and logged against the timer's name, and the rest of the
        /// pass still runs - one broken timer must not stop the others from firing.
        ///
        /// The callbacks run after the lock is released, not under it. The lock guards the
        /// dictionary; holding it while running arbitrary code made it the outer lock of
        /// whatever that code took. Rasa.Auth's Client is the case that bit: its "login" and
        /// "timeout" callbacks call Close(), which takes the client's own lock and then
        /// Timer.Remove - while a socket thread closing the same client takes the client's
        /// lock first and then wants this one for the same Remove. Opposite orders on two
        /// threads, and the auth loop, which runs every client's timers under lock(Clients),
        /// stopped for good. With nothing called out under it, this lock is always the
        /// innermost one taken, and no ordering can form around it.
        ///
        /// It also means a callback may Add, Remove or reset any timer, its own included,
        /// without touching a dictionary that is being walked: the due items are collected
        /// first and the walk is over before any of them runs.
        /// </summary>
        public void Update(long delta)
        {
            List<TimedItem> due = null;

            lock (_timedItems)
            {
                foreach (var item in _timedItems.Values)
                {
                    if (!item.Update(delta))
                        continue;

                    if (due == null)
                        due = new();

                    due.Add(item);
                }

                if (due == null)
                    return;

                // A non-repeating timer is spent once it has fired, and goes before its
                // callback runs: one that throws would otherwise be left behind to throw again
                // on every tick, and a callback that re-adds a timer under the same name is
                // adding a new one that must stay.
                foreach (var item in due)
                    if (!item.Repeating && _timedItems.TryGetValue(item.Name, out var current) && ReferenceEquals(current, item))
                        _timedItems.Remove(item.Name);
            }

            foreach (var item in due)
            {
                try
                {
                    item.Action?.Invoke();
                }
                catch (Exception e)
                {
                    ReportFault(item.Name, item, e);
                }
            }
        }

        /// <summary>How long a timer stays quiet after a fault of its own has been logged.</summary>
        private const long FaultLogQuietMs = 5000;

        /// <summary>
        /// Per timer, because they fail independently and at their own intervals. A repeating
        /// timer on a 100 ms period whose callback always throws would otherwise write ten lines
        /// a second for as long as the server runs. The first is written in full; after that, at
        /// most one every FaultLogQuietMs, saying how many it stands for.
        /// </summary>
        private static void ReportFault(string name, TimedItem item, Exception e)
        {
            item.FaultsSinceLog++;

            var now = Environment.TickCount64;

            // Not "have I counted more than one": the counter is reset every time one is
            // written, so testing it would let the very next fault through and log every time.
            if (item.FaultLogged && now < item.NextFaultLogTick)
                return;

            var repeat = item.FaultsSinceLog > 1 ? $" ({item.FaultsSinceLog} faults since the last of these)" : "";

            Logger.WriteLog(LogType.Error, $"Timer '{name}' threw and was skipped{repeat}: {e}");

            item.FaultLogged = true;
            item.FaultsSinceLog = 0;
            item.NextFaultLogTick = now + FaultLogQuietMs;
        }

        public void ResetTimer(string name)
        {
            lock (_timedItems)
            {
                if (_timedItems.ContainsKey(name))
                    _timedItems[name].ResetTimer();
            }
        }

        public bool IsTriggered(string name)
        {
            lock (_timedItems)
                if (_timedItems.ContainsKey(name))
                    return _timedItems[name].Triggered;

            return false;
        }
    }
}
