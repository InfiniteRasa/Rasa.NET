namespace Rasa.Structures
{
    public class ChatChannelPlayerLink
    {
        public ulong EntityId { get; set; }
        public ChatChannelPlayerLink Previous { get; set; }
        public ChatChannelPlayerLink Next { get; set; }
    }
}
