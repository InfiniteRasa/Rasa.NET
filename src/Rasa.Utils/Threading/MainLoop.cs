using System;
using System.Threading;

namespace Rasa.Threading
{
    public class MainLoop
    {
        public const int MinSleepInternal = 10; // Milliseconds

        public int LoopTime { get; }
        public bool Running { get; private set; }
        public bool ContinuousUnderLoad { get; private set; }
        public ILoopable Object { get; }
        public Thread LoopThread { get; private set; }

        private static long CurrentMs()
        {
            return (DateTime.UtcNow - new DateTime(1970, 1, 1)).Ticks / TimeSpan.TicksPerMillisecond;
        }

        public MainLoop(ILoopable obj, int loopTime, bool continuousUnderLoad = false)
        {
            Object = obj;
            LoopTime = loopTime;
            ContinuousUnderLoad = continuousUnderLoad;
        }

        public void Start()
        {
            Running = true;

            LoopThread = new Thread(Loop);
            LoopThread.Start();
        }

        public void Stop()
        {
            // No need to join the thread, setting Running to false will eventually stop the thread
            Running = false;
        }

        private void Loop()
        {
            var prevTime = CurrentMs();
            var prevSleepTime = 0;

            while (Running)
            {
                var realTime = CurrentMs();

                var delta = realTime - prevTime;

                Object.MainLoop(delta);

                prevTime = realTime;

                if (delta <= LoopTime + prevSleepTime)
                {
                    prevSleepTime = LoopTime + prevSleepTime - (int) delta;
                    if (prevSleepTime < MinSleepInternal)
                        prevSleepTime = MinSleepInternal;
                }
                else if (!ContinuousUnderLoad)
                    prevSleepTime = MinSleepInternal;
                else
                {
                    prevSleepTime = 0;
                    continue;
                }

                Thread.Sleep(prevSleepTime);
            }
        }
    }
}
