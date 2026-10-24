namespace Rasa.Structures
{
    /// <summary>
    /// Which cell's list a creature or a client was put on, kept by CellManager as it puts them
    /// on one and takes them off: so that taking them out of the world looks in that one cell,
    /// rather than every cell of the map.
    ///
    /// Every creature taken out of the world (RemoveCreatureFromWorld, a Maw going down) used to
    /// go through every cell of its map to be sure of finding every list it was on, and every
    /// player leaving the world went through every cell of every map (DetachClient, once a map).
    /// A map that has been played on has tens of thousands of cells - every cell ever within two
    /// of a creature, an object or a player - and that was 0.4 ms a creature and 3.7 ms a player
    /// on a map of 20,000 cells, ten maps.
    ///
    /// It is only ever an answer when it is sure. Something put on a list it was not told of -
    /// a second cell while it is still on the first, or a list it is then found taken off - and
    /// it no longer answers: the cells are searched as before, for good. Something it was never
    /// told of at all - a test putting a creature on a list by hand - has none, and is searched
    /// for as before. What it names, it names as the cell it was put on: the cell is looked up
    /// in the map in question, and is only the answer if it is that map's.
    /// </summary>
    public sealed class CellMembership
    {
        private MapCell _cell;
        private int _count;
        private bool _unsure;

        /// <summary>Put on <paramref name="cell"/>'s list once more.</summary>
        internal void Entered(MapCell cell)
        {
            if (_unsure || cell == null)
                return;

            if (_count == 0)
            {
                _cell = cell;
                _count = 1;
            }
            else if (ReferenceEquals(_cell, cell))
                _count++;
            else
                _unsure = true;
        }

        /// <summary>Taken off <paramref name="cell"/>'s list, <paramref name="removed"/> times.</summary>
        internal void Left(MapCell cell, int removed)
        {
            if (_unsure || removed <= 0)
                return;

            // Off a list it was not known to be on: there is something it was not told of.
            if (!ReferenceEquals(_cell, cell) || removed > _count)
            {
                _unsure = true;
                return;
            }

            _count -= removed;

            if (_count == 0)
                _cell = null;
        }

        /// <summary>
        /// Whether it can say which of <paramref name="map"/>'s cells this is on: then
        /// <paramref name="cell"/> is that cell, or null for none of them.
        /// </summary>
        internal bool TryGetCell(MapChannel map, out MapCell cell)
        {
            cell = null;

            if (_unsure)
                return false;

            if (_count > 0 && map.MapCellInfo.Cells.TryGetValue(_cell.CellSeed, out var listed) && ReferenceEquals(listed, _cell))
                cell = _cell;

            return true;
        }

        /// <summary>For the tests: whether it would answer.</summary>
        internal bool Sure => !_unsure;
    }
}
