using System;
using System.Diagnostics;
using System.Threading;

namespace Rasa.Threading
{
    public class MainLoop
    {
        public int LoopTime { get; }
        public bool Running { get; private set; }
        public ILoopable Object { get; }
        public Thread LoopThread { get; private set; }

        /// <summary>
        /// The loop's clock: milliseconds on the Stopwatch, which only ever runs forward.
        ///
        /// It used to be DateTime.UtcNow, the wall clock, and every tick's delta was the
        /// difference of two readings of it - so the loop inherited every step the wall clock
        /// takes: an NTP or w32time correction, a VM snapshot restored, somebody setting the time.
        /// A step back of S ms gave a delta of about -S, and the sleep that paces the loop is
        /// LoopTime less the delta, so the loop slept for S: the world, every timer and every
        /// client frozen for as long as the correction was big. A step forward handed the next
        /// tick S ms at once, and everything driven by the delta - timers, creature movement,
        /// cooldowns, regeneration - jumped that far ahead in one go.
        /// </summary>
        private static readonly Stopwatch Clock = Stopwatch.StartNew();

        private static long CurrentMs() => Clock.ElapsedMilliseconds;

        /// <summary>
        /// The most one tick hands on as its delta. Even a clock that cannot step can see a gap
        /// no tick should act on: a debugger break, a process suspended and resumed, a machine
        /// that slept. A tick that really did take this long still advances the world by this
        /// much, so a slow server runs slow rather than skipping; what the clamp stops is a
        /// single tick moving every creature and every timer by minutes.
        /// </summary>
        private int MaxDeltaMs => Math.Max(1000, LoopTime * 10);

        private long _nextClampLogMs;

        public MainLoop(ILoopable obj, int loopTime)
        {
            Object = obj;
            LoopTime = loopTime;
        }

        public void Start()
        {
            if (Running)
                throw new Exception("Unable to start a running MainLoop!");

            Running = true;

            LoopThread = new Thread(Loop)
            {
                Priority = ThreadPriority.Highest
            };
            LoopThread.Start();
        }

        public void Stop()
        {
            if (!Running)
                throw new Exception("Unable to stop a not running MainLoop!");

            // No need to join the thread, setting Running to false will eventually stop the thread
            Running = false;
        }

        /// <summary>
        /// How the loop has been doing since the last time anyone asked: how many times it ran,
        /// how long the slowest single pass took, and over how long a stretch.
        /// </summary>
        public struct LoopMetrics
        {
            public int Loops;
            public double PeakMs;
            public double WindowMs;

            public double LoopsPerSecond => WindowMs > 0.0 ? Loops * 1000.0 / WindowMs : 0.0;
        }

        /// <summary>
        /// Measured on the Stopwatch rather than the wall clock. DateTime.UtcNow moves in steps of
        /// about 15 ms on Windows, and a pass that does its work in 3 ms would read as 0 or 15.
        /// </summary>
        private static double StopwatchMs(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

        private readonly object _metricsLock = new object();
        private long _metricsFrom = Stopwatch.GetTimestamp();
        private int _metricsLoops;
        private long _metricsPeakTicks;

        /// <summary>Reads the window and starts a new one.</summary>
        public LoopMetrics TakeMetrics()
        {
            lock (_metricsLock)
            {
                var now = Stopwatch.GetTimestamp();
                var metrics = new LoopMetrics
                {
                    Loops = _metricsLoops,
                    PeakMs = StopwatchMs(_metricsPeakTicks),
                    WindowMs = StopwatchMs(now - _metricsFrom)
                };

                _metricsLoops = 0;
                _metricsPeakTicks = 0;
                _metricsFrom = now;

                return metrics;
            }
        }

        /// <summary>Reads the window without disturbing it, for anyone who just wants a look.</summary>
        public LoopMetrics PeekMetrics()
        {
            lock (_metricsLock)
                return new LoopMetrics
                {
                    Loops = _metricsLoops,
                    PeakMs = StopwatchMs(_metricsPeakTicks),
                    WindowMs = StopwatchMs(Stopwatch.GetTimestamp() - _metricsFrom)
                };
        }

        private int _consecutiveFaults;
        private long _faultsSinceLog;
        private long _nextFaultLogMs;

        /// <summary>How long to stay quiet after logging a fault, while faults keep coming.</summary>
        private const long FaultLogQuietMs = 5000;

        /// <summary>
        /// A tick that throws once wants a log line. A tick that throws every time - a manager
        /// left in a state it cannot recover from - would write ten a second for as long as the
        /// server runs, bury whatever came before it, and fill the disk. So: the first one in
        /// full, then at most one every FaultLogQuietMs saying how many went by in between.
        /// </summary>
        private void ReportFault(Exception e)
        {
            _consecutiveFaults++;
            _faultsSinceLog++;

            var now = CurrentMs();

            if (_consecutiveFaults > 1 && now < _nextFaultLogMs)
                return;

            var repeat = _faultsSinceLog > 1 ? $" ({_faultsSinceLog} faults since the last of these)" : "";

            Logger.WriteLog(LogType.Error,
                $"Unhandled exception in the main loop of {Object.GetType().FullName}{repeat}. " +
                $"The tick was abandoned and the loop is still running: {e}");

            _faultsSinceLog = 0;
            _nextFaultLogMs = now + FaultLogQuietMs;
        }

        private void Loop()
        {
            var prevTime = CurrentMs();
            var prevSleepTime = 0;

            while (Running)
            {
                var realTime = CurrentMs();

                var delta = realTime - prevTime;
                var tickDelta = Math.Clamp(delta, 0, MaxDeltaMs);

                if (tickDelta != delta && realTime >= _nextClampLogMs)
                {
                    _nextClampLogMs = realTime + 60000;

                    Logger.WriteLog(LogType.Error,
                        $"The main loop of {Object.GetType().FullName} went {delta} ms between ticks; this tick advances the world by {tickDelta} ms. " +
                        "(A stall this long is a debugger break, a suspended process or a machine that slept - or a tick that did this much work.)");
                }

                var workFrom = Stopwatch.GetTimestamp();

                // The last line of defence for both servers. This runs on a dedicated thread, so
                // an exception escaping a tick does not merely stop the world - it goes unhandled
                // and .NET ends the process. Everything below this point is guarded in its own
                // right; this is what makes a gap that gets missed cost one tick rather than the
                // server, and it is the difference between a log line to act on and a game server
                // that is simply gone with nothing to say why.
                try
                {
                    Object.MainLoop(tickDelta);

                    _consecutiveFaults = 0;
                }
                catch (Exception e)
                {
                    ReportFault(e);
                }

                // The pass itself, not the pass plus the sleep that follows it: a loop that does
                // 10 ms of work and sleeps 90 is a healthy 100 ms iteration, and reporting 100
                // would hide the one that does 250 ms of work.
                var workTicks = Stopwatch.GetTimestamp() - workFrom;

                lock (_metricsLock)
                {
                    _metricsLoops++;

                    if (workTicks > _metricsPeakTicks)
                        _metricsPeakTicks = workTicks;
                }

                prevTime = realTime;

                if (delta <= LoopTime + prevSleepTime)
                {
                    prevSleepTime = LoopTime + prevSleepTime - (int)delta;
                    if (prevSleepTime < 10)
                        prevSleepTime = 10;
                }
                else
                    prevSleepTime = 10;

                Thread.Sleep(prevSleepTime);
            }
        }
    }
}
