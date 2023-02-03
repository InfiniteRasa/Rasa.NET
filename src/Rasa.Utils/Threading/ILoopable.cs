namespace Rasa.Threading;

public interface ILoopable
{
    void MainLoop(long delta);
}
