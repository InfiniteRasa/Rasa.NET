using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Game
{
    /// <summary>
    /// A shutdown announced ahead of time (the console's <c>exit &lt;minutes&gt; [reason]</c>): when
    /// it is due, and which warnings the players have been given.
    ///
    /// Players are told as soon as it is set, with the time it gives them, and again as the time
    /// left passes each of <see cref="WarningSeconds"/> - an hour, half an hour, a quarter, ten
    /// minutes, then each minute from five, and thirty and ten seconds before the end. In the
    /// last <see cref="RefuseArrivalsSeconds"/> nobody new is let into the world.
    ///
    /// Ticks are Environment.TickCount64; the schedule holds no clock of its own, so it can be
    /// worked through in a test second by second.
    /// </summary>
    public class ShutdownSchedule
    {
        public static readonly IReadOnlyList<int> WarningSeconds = new[] { 3600, 1800, 900, 600, 300, 240, 180, 120, 60, 30, 10 };

        /// <summary>How long before the end new connections to the world are refused.</summary>
        public const int RefuseArrivalsSeconds = 60;

        /// <summary>The longest delay accepted: a day.</summary>
        public const double MaxMinutes = 24 * 60;

        private int _lastWarnedSeconds = int.MaxValue;

        public ShutdownSchedule(long nowTick, long delayMs, string reason)
        {
            DeadlineTick = nowTick + Math.Max(0, delayMs);
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        }

        public long DeadlineTick { get; }

        /// <summary>What the console gave as the reason, if anything; shown with every warning.</summary>
        public string Reason { get; }

        public bool IsDue(long nowTick) => nowTick >= DeadlineTick;

        /// <summary>Whole seconds left, rounded up: 0 only once it is due.</summary>
        public int SecondsLeft(long nowTick) =>
            (int)Math.Min(int.MaxValue, Math.Max(0, (DeadlineTick - nowTick + 999) / 1000));

        public bool RefusesArrivals(long nowTick) => SecondsLeft(nowTick) <= RefuseArrivalsSeconds;

        /// <summary>
        /// The warning to give the players at <paramref name="nowTick"/>, or null for none. The first
        /// call gives the time left as it is; after that, one for each of <see cref="WarningSeconds"/>
        /// passed - only the latest, if a slow tick passed more than one at once. Nothing once due.
        /// </summary>
        public string WarningDue(long nowTick)
        {
            if (IsDue(nowTick))
                return null;

            var left = SecondsLeft(nowTick);

            if (_lastWarnedSeconds == int.MaxValue)
            {
                _lastWarnedSeconds = left;
                return Warning(left);
            }

            var passed = WarningSeconds.Where(s => s < _lastWarnedSeconds && left <= s).ToList();

            if (passed.Count == 0)
                return null;

            _lastWarnedSeconds = passed.Min();
            return Warning(_lastWarnedSeconds);
        }

        public string Warning(int secondsLeft) =>
            $"The server is shutting down in {Describe(secondsLeft)}"
            + (Reason == null ? "." : $" ({Reason}).")
            + " Your character is saved when it goes; find a safe place and log out.";

        public const string CancelledMessage = "The server shutdown has been cancelled.";

        /// <summary>"5 minutes", "1 minute 30 seconds", "10 seconds".</summary>
        public static string Describe(int seconds)
        {
            seconds = Math.Max(0, seconds);

            var hours = seconds / 3600;
            var minutes = seconds % 3600 / 60;
            var rest = seconds % 60;
            var parts = new List<string>();

            if (hours > 0)
                parts.Add(Plural(hours, "hour"));
            if (minutes > 0)
                parts.Add(Plural(minutes, "minute"));
            if (rest > 0 || parts.Count == 0)
                parts.Add(Plural(rest, "second"));

            return string.Join(" ", parts);
        }

        private static string Plural(int count, string unit) => $"{count} {unit}{(count == 1 ? "" : "s")}";
    }
}
