using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Structures
{
    /// <summary>
    /// A creature's aggro table: how much it hates each actor that has hurt it, healed its
    /// enemies or been noticed by it, keyed by entity id. What it fights is whoever tops the
    /// table and can still be fought (Managers.Threat.ChooseTarget); what raises an entry is
    /// Managers.Threat's business. A plain dictionary under a lock - the behaviour worker reads
    /// it while missiles and effect ticks on the same map write to it.
    /// </summary>
    public class HateTable
    {
        private readonly Dictionary<ulong, double> _hate = new Dictionary<ulong, double>();
        private readonly object _lock = new object();

        public void Add(ulong entityId, double amount)
        {
            if (entityId == 0 || amount <= 0 || double.IsNaN(amount))
                return;

            lock (_lock)
                _hate[entityId] = (_hate.TryGetValue(entityId, out var hate) ? hate : 0) + amount;
        }

        /// <summary>
        /// Moves up to amount of the hate held for one actor onto another (Reality Ripper's hate
        /// transfer). The first keeps what is left and stays on the table.
        /// </summary>
        public void Move(ulong from, ulong to, double amount)
        {
            if (from == 0 || to == 0 || from == to || amount <= 0 || double.IsNaN(amount))
                return;

            lock (_lock)
            {
                if (!_hate.TryGetValue(from, out var held) || held <= 0)
                    return;

                var moved = Math.Min(held, amount);

                _hate[from] = held - moved;
                _hate[to] = (_hate.TryGetValue(to, out var hate) ? hate : 0) + moved;
            }
        }

        /// <summary>Makes sure an actor is on the table at all, with at least this much.</summary>
        public void Ensure(ulong entityId, double atLeast)
        {
            if (entityId == 0)
                return;

            lock (_lock)
                if (!_hate.TryGetValue(entityId, out var hate) || hate < atLeast)
                    _hate[entityId] = atLeast;
        }

        public double Of(ulong entityId)
        {
            lock (_lock)
                return _hate.TryGetValue(entityId, out var hate) ? hate : 0;
        }

        public bool Contains(ulong entityId)
        {
            lock (_lock)
                return _hate.ContainsKey(entityId);
        }

        public void Remove(ulong entityId)
        {
            lock (_lock)
                _hate.Remove(entityId);
        }

        public void Clear()
        {
            lock (_lock)
                _hate.Clear();
        }

        public int Count
        {
            get
            {
                lock (_lock)
                    return _hate.Count;
            }
        }

        /// <summary>The entries, most hated first.</summary>
        public List<KeyValuePair<ulong, double>> Ranked()
        {
            lock (_lock)
                return _hate.OrderByDescending(e => e.Value).ToList();
        }

        /// <summary>Top's working list; the loop thread's alone, and empty between calls.</summary>
        private readonly List<KeyValuePair<ulong, double>> _ranked = new List<KeyValuePair<ulong, double>>();

        /// <summary>Most hated first, keeping the table's order among equals - what OrderByDescending gave.</summary>
        private static void SortMostHatedFirst(List<KeyValuePair<ulong, double>> entries)
        {
            for (var i = 1; i < entries.Count; i++)
            {
                var entry = entries[i];
                var j = i - 1;

                while (j >= 0 && entries[j].Value < entry.Value)
                {
                    entries[j + 1] = entries[j];
                    j--;
                }

                entries[j + 1] = entry;
            }
        }

        /// <summary>
        /// Drops every entry <paramref name="gone"/> says can never be fought again. Allocates only
        /// when there is something to drop.
        /// </summary>
        public void RemoveWhere(Func<ulong, bool> gone)
        {
            List<ulong> drop = null;

            lock (_lock)
            {
                foreach (var entry in _hate)
                    if (gone(entry.Key))
                        (drop ??= new List<ulong>()).Add(entry.Key);

                if (drop != null)
                    foreach (var id in drop)
                        _hate.Remove(id);
            }
        }

        /// <summary>
        /// Who the creature should be fighting: the most hated entry that <paramref name="canFight"/>
        /// allows, except that the current target keeps it unless someone hates it more by
        /// <paramref name="takeoverPercent"/> - so two attackers doing about the same do not have
        /// the creature turning back and forth every think. 0 when nobody on the table can be
        /// fought.
        /// </summary>
        public ulong Top(Func<ulong, bool> canFight, ulong current, int takeoverPercent)
        {
            // Asked of every fighting creature on every think. It used to rank the table with
            // OrderByDescending(...).ToList() and search that with a closure; the ranking is
            // now an insertion sort into a list the table keeps (a handful of entries, and
            // stable, so ties fall as they did), and the search a loop. canFight is called
            // outside the table's lock, as before.
            var ranked = _ranked;

            ranked.Clear();

            lock (_lock)
                foreach (var entry in _hate)
                    ranked.Add(entry);

            SortMostHatedFirst(ranked);

            var best = default(KeyValuePair<ulong, double>);

            foreach (var entry in ranked)
                if (canFight(entry.Key))
                {
                    best = entry;
                    break;
                }

            ranked.Clear();

            if (best.Key == 0)
                return 0;

            if (current == 0 || current == best.Key || !canFight(current))
                return best.Key;

            var held = Of(current);

            return best.Value * 100.0 > held * Math.Max(100, takeoverPercent) ? best.Key : current;
        }
    }
}
