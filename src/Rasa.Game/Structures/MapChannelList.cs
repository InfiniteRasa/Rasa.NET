using System.Collections.Generic;

namespace Rasa.Structures
{
    public class MapChannelList
    {
        public Dictionary<int,MapChannel> MapChannelArray = new Dictionary<int, MapChannel>();
        public int MapChannelCount { get; set; }
    }
}
