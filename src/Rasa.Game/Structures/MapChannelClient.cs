﻿namespace Rasa.Structures
{
    using Game;

    public class MapChannelClient
    {
        public MapChannel MapChannel { get; set; }
        public bool Disconected { get; set; }
        public bool LogoutActive { get; set; }
        public bool RemoveFromMap {get;set;}
        // chat
        public int JoinedChannels { get; set; }
        public int[] ChannelHashes = new int[14];
        // gm flags
        public bool GmFlagAlwaysFriendly { get; set; }
    }
}
