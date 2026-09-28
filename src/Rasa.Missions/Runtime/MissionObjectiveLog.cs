using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Rasa.Structures
{
    using Data;

    public sealed class MissionObjectiveLog
    {
        private readonly Dictionary<uint, uint> _counters;
        private readonly Dictionary<uint, uint> _itemCounters;

        public uint ObjectiveId { get; }
        public MissionObjectiveState State { get; internal set; }
        public IReadOnlyDictionary<uint, uint> Counters { get; }
        public IReadOnlyDictionary<uint, uint> ItemCounters { get; }

        public MissionObjectiveLog(
            uint objectiveId,
            MissionObjectiveState state,
            IReadOnlyDictionary<uint, uint> counters,
            IReadOnlyDictionary<uint, uint> itemCounters)
        {
            ObjectiveId = objectiveId;
            State = state;
            _counters = new Dictionary<uint, uint>(counters ?? new Dictionary<uint, uint>());
            _itemCounters = new Dictionary<uint, uint>(
                itemCounters ?? new Dictionary<uint, uint>());
            Counters = new ReadOnlyDictionary<uint, uint>(_counters);
            ItemCounters = new ReadOnlyDictionary<uint, uint>(_itemCounters);
        }

        internal void SetCounter(uint counterId, uint value) => _counters[counterId] = value;
        internal void SetItemCounter(uint itemClassId, uint value) => _itemCounters[itemClassId] = value;
    }
}
