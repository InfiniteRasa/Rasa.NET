using System;
using System.Collections.Generic;

namespace Rasa.Timer
{
    public class Timer
    {
        private readonly Dictionary<string, TimedItem> _timedItems = new Dictionary<string, TimedItem>();

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
                if (_timedItems.ContainsKey(name))
                    _timedItems.Remove(name);
        }

        public void Update(long delta)
        {
            lock (_timedItems)
                foreach (var item in _timedItems)
                    if (item.Value.Update(delta))
                        item.Value.Action?.Invoke();
        }

        public void ResetTimer(string name)
        {
            lock (_timedItems)
                if (_timedItems.ContainsKey(name))
                    _timedItems[name].ResetTimer();
        }
    }
}
