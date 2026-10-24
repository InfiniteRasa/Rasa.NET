using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    /// <summary>Something a cell's list holds, which keeps which cells' lists hold it (CellMembership).</summary>
    public interface ICellMember
    {
        /// <summary>Null until it is first put on a cell's list.</summary>
        CellMembership CellMembership { get; set; }
    }

    /// <summary>
    /// A cell's list of creatures or of clients (MapCell.CreatureList, MapCell.ClientList): a
    /// List that tells each thing put on it or taken off it, so that each knows which cells hold
    /// it (CellMembership) and taking it out of the world needs to look only there.
    ///
    /// Every way of changing the list there is in List is here, hiding List's own, so a change
    /// made through the cell's list - by CellManager, by any manager, by a test putting a corpse
    /// on a second cell by hand - is told of. What List does is unchanged: each of these does
    /// List's own, in the same order, with the same result. A change made through the list taken
    /// as a plain List, IList or ICollection would not be told of; nothing does that.
    /// </summary>
    public sealed class CellList<T> : List<T> where T : class, ICellMember
    {
        private readonly MapCell _cell;

        public CellList(MapCell cell)
        {
            _cell = cell;
        }

        private void Entered(T item)
        {
            if (item != null)
                (item.CellMembership ??= new CellMembership()).Entered(_cell);
        }

        private void Left(T item)
        {
            item?.CellMembership?.Left(_cell);
        }

        public new T this[int index]
        {
            get => base[index];
            set
            {
                var was = base[index];
                base[index] = value;
                Left(was);
                Entered(value);
            }
        }

        public new void Add(T item)
        {
            base.Add(item);
            Entered(item);
        }

        public new void AddRange(IEnumerable<T> items)
        {
            var from = Count;
            base.AddRange(items);

            for (var i = from; i < Count; i++)
                Entered(base[i]);
        }

        public new void Insert(int index, T item)
        {
            base.Insert(index, item);
            Entered(item);
        }

        public new void InsertRange(int index, IEnumerable<T> items)
        {
            var before = Count;
            base.InsertRange(index, items);

            for (var i = index; i < index + Count - before; i++)
                Entered(base[i]);
        }

        public new bool Remove(T item)
        {
            var index = IndexOf(item);

            if (index < 0)
                return false;

            RemoveAt(index);
            return true;
        }

        public new void RemoveAt(int index)
        {
            var was = base[index];
            base.RemoveAt(index);
            Left(was);
        }

        public new int RemoveAll(Predicate<T> match)
        {
            ArgumentNullException.ThrowIfNull(match);

            return base.RemoveAll(item =>
            {
                if (!match(item))
                    return false;

                Left(item);
                return true;
            });
        }

        public new void RemoveRange(int index, int count)
        {
            var gone = GetRange(index, count);
            base.RemoveRange(index, count);

            foreach (var item in gone)
                Left(item);
        }

        public new void Clear()
        {
            foreach (var item in this)
                Left(item);

            base.Clear();
        }
    }
}
