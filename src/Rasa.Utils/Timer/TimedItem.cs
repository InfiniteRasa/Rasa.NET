using System;

namespace Rasa.Timer
{
    public class TimedItem
    {
        public string Name { get; }
        public bool Repeating { get; }
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
                return true;
            }

            CurrentTimer -= delta;
            return false;
        }
    }
}
