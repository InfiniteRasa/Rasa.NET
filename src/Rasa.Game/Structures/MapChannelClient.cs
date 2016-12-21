using Rasa.Game;

namespace Rasa.Structures
{
    public class MapChannelClient
    {
        public uint ClientEntityId { get; set; }
        public MapChannel MapChannel { get; set; }
        public CharacterData CharacterData { get; set; }
        public PlayerData Player{ get; set; }
        public Client Client { get; set; }
        public bool Disconected { get; set; }
    }
}
