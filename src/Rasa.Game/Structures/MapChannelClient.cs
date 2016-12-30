namespace Rasa.Structures
{
    using Game;
    public class MapChannelClient
    {
        public uint ClientEntityId { get; set; }
        public MapChannel MapChannel { get; set; }
        public PlayerData Player { get; set; }
        public Client Client;
        public bool Disconected { get; set; }
        public bool LogoutActive { get; set; }
        public long LogoutRequestedLast { get; set; }
        public bool RemoveFromMap {get;set;}
        // chat
        public int JoinedChannels { get; set; }
        public int[] ChannelHashes = new int[14];
    }
}
