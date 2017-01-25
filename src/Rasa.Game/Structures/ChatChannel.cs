namespace Rasa.Structures
{
    public class ChatChannel
    {
        public char[] Name = new char[40];
        public bool IsDefaultChannel { get; set; }
        public int MapContextId { get; set; }
        public int InstanceId { get; set; }
        public int ChannelId { get; set; }                      // 1 - general, ...
        public ChatChannelPlayerLink FirstPlayer { get; set; }  // if null --> no player in chat
    }
}
