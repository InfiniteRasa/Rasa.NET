namespace Rasa.Structures
{
    public class ChatChannel
    {
        public char[] Name = new char[40];
        public bool IsDefaultChannel { get; set; }
        public uint MapContextId { get; set; }
        public int InstanceId { get; set; }
        public uint ChannelId { get; set; }                      // 1 - general, ...
        public ChatChannelPlayerLink FirstPlayer { get; set; }  // if null --> no player in chat
    }
}
