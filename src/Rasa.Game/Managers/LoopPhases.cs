using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Rasa.Managers
{
    /// <summary>
    /// Where a pass of the world loop goes: the time and the allocations of each part of it, for
    /// the console's 'perf'. The loop's own metrics (MainLoop.LoopMetrics) say how long the
    /// slowest pass took and how much a pass allocates; this says which part of it that was -
    /// every worker the map channel worker runs through its Guard, by name and summed over the
    /// maps, and the two halves of servicing the clients (reading what they sent, which is where
    /// a Move's visibility and broadcast are done, and flushing what they are sent).
    ///
    /// Off unless GameConfig.PhaseTimings or 'perf phases on' turns it on, and then on the loop
    /// thread only: a part begun with nothing on costs the one read of <see cref="Enabled"/>, and
    /// nothing is kept. On, a part costs two Stopwatch readings, two of the thread's allocation
    /// counter and a dictionary lookup by its name, about a tenth of a microsecond.
    ///
    /// The window runs from when it was turned on or last read: 'perf' reads it and starts the
    /// next, so two readings a minute apart describe that minute.
    /// </summary>
    public static class LoopPhases
    {
        /// <summary>Whether the parts are timed. Set from the config and from the console; read by the loop.</summary>
        public static volatile bool Enabled;

        /// <summary>Where a part began: zero when nothing is being timed.</summary>
        public readonly struct Mark
        {
            internal readonly long Ticks;
            internal readonly long Bytes;

            internal Mark(long ticks, long bytes)
            {
                Ticks = ticks;
                Bytes = bytes;
            }
        }

        /// <summary>One part's sums over the window.</summary>
        public sealed class Totals
        {
            public string Name { get; internal set; }
            public long Calls { get; internal set; }
            public long Ticks { get; internal set; }
            public long PeakTicks { get; internal set; }
            public long Bytes { get; internal set; }

            public double Ms => StopwatchMs(Ticks);
            public double PeakMs => StopwatchMs(PeakTicks);
        }

        /// <summary>A window's numbers, as <see cref="Take"/> hands them out.</summary>
        public sealed class Window
        {
            public double WindowMs { get; internal set; }
            public long Passes { get; internal set; }
            public long PassTicks { get; internal set; }
            public long PassBytes { get; internal set; }
            public long PeakPassTicks { get; internal set; }
            public long PeakPassBytes { get; internal set; }

            /// <summary>Every part, slowest in all first.</summary>
            public List<Totals> Parts { get; internal set; } = new();
        }

        private static readonly object Sync = new();
        private static Dictionary<string, Totals> _parts = new();
        private static long _from = Stopwatch.GetTimestamp();
        private static long _passes, _passTicks, _passBytes, _peakPassTicks, _peakPassBytes;

        private static double StopwatchMs(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

        /// <summary>The start of a part; nothing when the parts are not being timed.</summary>
        public static Mark Begin() =>
            Enabled ? new Mark(Stopwatch.GetTimestamp(), GC.GetAllocatedBytesForCurrentThread()) : default;

        /// <summary>The end of a part begun with <see cref="Begin"/>, added to its name's sums. Nothing for an empty mark.</summary>
        public static void End(string name, Mark from)
        {
            if (from.Ticks == 0)
                return;

            var ticks = Stopwatch.GetTimestamp() - from.Ticks;
            var bytes = GC.GetAllocatedBytesForCurrentThread() - from.Bytes;

            lock (Sync)
            {
                if (!_parts.TryGetValue(name, out var totals))
                    _parts.Add(name, totals = new Totals { Name = name });

                totals.Calls++;
                totals.Ticks += ticks;
                totals.Bytes += bytes;

                if (ticks > totals.PeakTicks)
                    totals.PeakTicks = ticks;
            }
        }

        /// <summary>The end of a whole pass of the world, begun with <see cref="Begin"/>: what the parts are shared out of.</summary>
        public static void EndPass(Mark from)
        {
            if (from.Ticks == 0)
                return;

            var ticks = Stopwatch.GetTimestamp() - from.Ticks;
            var bytes = GC.GetAllocatedBytesForCurrentThread() - from.Bytes;

            lock (Sync)
            {
                _passes++;
                _passTicks += ticks;
                _passBytes += bytes;
                _peakPassTicks = Math.Max(_peakPassTicks, ticks);
                _peakPassBytes = Math.Max(_peakPassBytes, bytes);
            }
        }

        /// <summary>The window so far, and a new one begun.</summary>
        public static Window Take()
        {
            lock (Sync)
            {
                var now = Stopwatch.GetTimestamp();
                var window = new Window
                {
                    WindowMs = StopwatchMs(now - _from),
                    Passes = _passes,
                    PassTicks = _passTicks,
                    PassBytes = _passBytes,
                    PeakPassTicks = _peakPassTicks,
                    PeakPassBytes = _peakPassBytes,
                    Parts = _parts.Values.OrderByDescending(part => part.Ticks).ToList()
                };

                Reset(now);

                return window;
            }
        }

        /// <summary>Everything counted so far forgotten; the window starts now.</summary>
        public static void Reset()
        {
            lock (Sync)
                Reset(Stopwatch.GetTimestamp());
        }

        private static void Reset(long now)
        {
            _parts = new Dictionary<string, Totals>();
            _from = now;
            _passes = _passTicks = _passBytes = _peakPassTicks = _peakPassBytes = 0;
        }

        /// <summary>
        /// A window as lines for the console: the passes, then the <paramref name="top"/> parts
        /// that took longest, each with its share of a pass in time and in bytes.
        /// </summary>
        public static List<string> Describe(Window window, int top = 15)
        {
            var lines = new List<string>();
            var passes = Math.Max(1, window.Passes);

            lines.Add($"Phases over {window.WindowMs / 1000.0:0.0} s: {window.Passes} pass(es) of the world, "
                + $"{StopwatchMs(window.PassTicks) / passes:0.000} ms and {window.PassBytes / passes / 1024.0:0.0} KB a pass, "
                + $"slowest {StopwatchMs(window.PeakPassTicks):0.00} ms, most allocated {window.PeakPassBytes / 1024.0:0.0} KB.");

            if (window.Parts.Count == 0)
            {
                lines.Add("   Nothing timed: 'perf phases on' to start.");
                return lines;
            }

            foreach (var part in window.Parts.Take(top))
                lines.Add($"   {part.Name,-46} {part.Ms / passes,8:0.000} ms/pass {part.Bytes / passes / 1024.0,8:0.0} KB/pass "
                    + $"{part.Calls,8} calls, slowest {part.PeakMs:0.00} ms");

            if (window.Parts.Count > top)
                lines.Add($"   ... and {window.Parts.Count - top} more, {window.Parts.Skip(top).Sum(part => part.Ms) / passes:0.000} ms/pass between them.");

            return lines;
        }
    }
}
