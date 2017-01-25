using System;

namespace Rasa.Timer
{
    public class TimedItem
    {
        public string Name { get; }
        public bool Repeating { get; }
        public bool Triggered { get; private set; }
        public long Timer { get; }
        public long CurrentTimer { get; private set; }
        public Action Action { get; }

        public TimedItem(string name, long timer, bool repeating, Action action)
        {
            Name = name;
            Repeating = repeating;
            CurrentTimer = Timer = timer;
            Action = action;
        }

        public bool Update(long delta)
        {
            if (CurrentTimer <= delta)
            {
                CurrentTimer = Timer - (delta - CurrentTimer);
                return (Triggered = true);
            }

            CurrentTimer -= delta;
            return (Triggered = false);
        }

        public void ResetTimer()
        {
            CurrentTimer = Timer;
        }
    }
}
