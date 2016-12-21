using System.Collections.Generic;

namespace Rasa.Structures
{
    public class MapCellInfo
    {
        public Dictionary<uint, MapCell> Cells = new Dictionary<uint, MapCell>();
        public int LoadedCellCount { get; set; }
        public int LoadedCellLimit { get; set; }
        public List<MapCell> LoadedCellList = new List<MapCell>();
        public int TimeUpdateVisibility { get; set; }
    }
}
