using System.Collections.Generic;

namespace Rasa.Structures
{
    /// <summary>
    /// Which cells' lists hold a creature or a client, kept by the lists themselves as it is put
    /// on and taken off them (CellList): so that taking it out of the world looks in the cell it is
    /// on, rather than every cell of the map.
    ///
    /// Every creature taken out of the world (RemoveCreatureFromWorld, a Maw going down) used to
    /// go through every cell of its map to be sure of finding every list it was on, and every
    /// player leaving the world went through every cell of every map (DetachClient, once a map).
    /// A map that has been played on has tens of thousands of cells - every cell ever within two
    /// of a creature, an object or a player - and that was 0.4 ms a creature and 3.7 ms a player
    /// on maps of 20,000 cells, ten of them.
    ///
    /// What it holds is every entry: one cell almost always, and each further entry as well - a
    /// second cell, or the same cell twice - since the server has guarded against a creature or a
    /// client found on more than one list, and goes on doing so. A cell it names is the cell put on,
    /// which is only the answer for a map if it is that map's.
    /// </summary>
    public sealed class CellMembership
    {
        private MapCell _cell;
        private int _count;

        /// <summary>Entries on cells other than <see cref="_cell"/>, one an entry; null while there are none.</summary>
        private List<MapCell> _others;

        /// <summary>Put on <paramref name="cell"/>'s list once more.</summary>
        internal void Entered(MapCell cell)
        {
            if (_count == 0)
            {
                _cell = cell;
                _count = 1;
            }
            else if (ReferenceEquals(_cell, cell))
                _count++;
            else
                (_others ??= new List<MapCell>()).Add(cell);
        }

        /// <summary>Taken off <paramref name="cell"/>'s list once.</summary>
        internal void Left(MapCell cell)
        {
            if (_count > 0 && ReferenceEquals(_cell, cell))
            {
                if (--_count > 0)
                    return;

                _cell = null;

                // The next cell it is on, with every entry of it, takes the first's place.
                if (_others == null || _others.Count == 0)
                    return;

                _cell = _others[0];

                for (var i = _others.Count - 1; i >= 0; i--)
                    if (ReferenceEquals(_others[i], _cell))
                    {
                        _others.RemoveAt(i);
                        _count++;
                    }

                return;
            }

            if (_others == null)
                return;

            for (var i = 0; i < _others.Count; i++)
                if (ReferenceEquals(_others[i], cell))
                {
                    _others.RemoveAt(i);
                    return;
                }
        }

        /// <summary>How many lists' entries hold it, all cells together.</summary>
        internal int Entries => _count + (_others?.Count ?? 0);

        /// <summary>
        /// Whether it is on one cell's list at most - then <paramref name="cell"/> is that cell when
        /// it is one of <paramref name="map"/>'s, and otherwise null: on none of this map's - or on
        /// several cells' lists, when it says false and the cells are to be searched.
        /// </summary>
        internal bool TryGetCell(MapChannel map, out MapCell cell)
        {
            cell = null;

            if (_others != null && _others.Count > 0)
                return false;

            if (_count > 0 && map.MapCellInfo.Cells.TryGetValue(_cell.CellSeed, out var listed) && ReferenceEquals(listed, _cell))
                cell = _cell;

            return true;
        }
    }
}
