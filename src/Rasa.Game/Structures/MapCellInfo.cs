using System.Collections.Generic;

namespace Rasa.Structures
{
    public class MapCellInfo
    {
        public Dictionary<uint, MapCell> Cells = new Dictionary<uint, MapCell>();
        public int TimeUpdateVisibility { get; set; }
    }
}
