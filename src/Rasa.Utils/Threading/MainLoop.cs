using System.Threading;

namespace Rasa.Threading;

public class MainLoop
{
    public int LoopTime { get; }
    public bool Running { get; private set; }
    public ILoopable Object { get; }
    public Thread? LoopThread { get; private set; }

    private static long CurrentMs() => (DateTime.UtcNow - new DateTime(1970, 1, 1)).Ticks / TimeSpan.TicksPerMillisecond;

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
