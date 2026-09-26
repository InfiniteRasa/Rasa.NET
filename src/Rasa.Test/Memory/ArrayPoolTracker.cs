using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Memory
{
    internal sealed class ArrayPoolTracker : EventListener
    {
        private readonly int _thread = Environment.CurrentManagedThreadId;
        private readonly int _trackedLength;
        private readonly bool _currentThreadOnly;
        private readonly Action<int> _onRent;
        private readonly object _lock = new();
        private readonly Dictionary<int, int> _rented = new();
        private readonly Dictionary<int, int> _returned = new();

        internal int LargestRent { get; private set; }

        internal bool AllReturned
        {
            get
            {
                lock (_lock)
                {
                    if (_rented.Count == 0)
                        return false;

                    foreach (var pair in _rented)
                        if (!_returned.TryGetValue(pair.Key, out var returns) || returns != pair.Value)
                            return false;

                    foreach (var pair in _returned)
                        if (!_rented.TryGetValue(pair.Key, out var rents) || rents != pair.Value)
                            return false;

                    return true;
                }
            }
        }

        internal ArrayPoolTracker(
            int trackedLength = 32768,
            bool currentThreadOnly = true,
            Action<int> onRent = null)
        {
            _trackedLength = trackedLength;
            _currentThreadOnly = currentThreadOnly;
            _onRent = onRent;
        }

        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name == "System.Buffers.ArrayPoolEventSource")
                EnableEvents(source, EventLevel.Verbose);
        }

        protected override void OnEventWritten(EventWrittenEventArgs data)
        {
            if (_rented == null || (_currentThreadOnly && Environment.CurrentManagedThreadId != _thread) ||
                data.Payload == null || data.Payload.Count < 2)
                return;

            var id = (int)data.Payload[0];
            var length = (int)data.Payload[1];
            if (data.EventName == "BufferRented")
                RecordRent(id, length);
            else if (data.EventName == "BufferReturned")
                RecordReturn(id, length);
        }

        internal void RecordRent(int id, int length)
        {
            lock (_lock)
            {
                LargestRent = Math.Max(LargestRent, length);
                if (length != _trackedLength)
                    return;

                Increment(_rented, id);
            }

            if (length == _trackedLength)
                _onRent?.Invoke(id);
        }

        internal void RecordReturn(int id, int length)
        {
            if (length != _trackedLength)
                return;

            lock (_lock)
                Increment(_returned, id);
        }

        private static void Increment(Dictionary<int, int> counts, int id)
        {
            counts.TryGetValue(id, out var count);
            counts[id] = count + 1;
        }

        internal void AssertReturned()
        {
            Dispose();
            lock (_lock)
            {
                Assert.IsTrue(_rented.Count > 0, "Expected a packet buffer to be rented.");

                foreach (var pair in _rented)
                {
                    _returned.TryGetValue(pair.Key, out var returns);
                    Assert.AreEqual(pair.Value, returns,
                        $"Array {pair.Key} was rented {pair.Value} time(s) and returned {returns} time(s).");
                }

                foreach (var pair in _returned)
                {
                    _rented.TryGetValue(pair.Key, out var rents);
                    Assert.AreEqual(rents, pair.Value,
                        $"Array {pair.Key} was rented {rents} time(s) and returned {pair.Value} time(s).");
                }
            }
        }
    }
}
