using System;
using System.Collections.Generic;

namespace Rasa.Missions.Scenes
{
    public sealed record SceneDueWork(string RunId, uint Generation, string Name, DateTime DueAtUtc, uint SequenceId, uint? ObjectiveId = null);

    public sealed class SceneDueQueue
    {
        private readonly PriorityQueue<SceneDueWork, long> _queue = new();
        private readonly Dictionary<(string Run, string Name), SceneDueWork> _current = new();
        public void Schedule(SceneDueWork work)
        {
            _current[(work.RunId, work.Name)] = work;
            _queue.Enqueue(work, work.DueAtUtc.Ticks);
        }
        public void Cancel(string runId, string name) => _current.Remove((runId, name));
        public void Cancel(string runId)
        {
            foreach (var key in new List<(string Run, string Name)>(_current.Keys))
                if (key.Run == runId)
                    _current.Remove(key);
        }
        public IReadOnlyList<SceneDueWork> TakeDue(DateTime utcNow)
        {
            var due = new List<SceneDueWork>();
            while (_queue.TryPeek(out var work, out var ticks) && ticks <= utcNow.Ticks)
            {
                _queue.Dequeue();
                var key = (work.RunId, work.Name);
                if (!_current.TryGetValue(key, out var current) || !ReferenceEquals(work, current))
                    continue;
                _current.Remove(key);
                due.Add(work);
            }
            return due;
        }
    }
}
