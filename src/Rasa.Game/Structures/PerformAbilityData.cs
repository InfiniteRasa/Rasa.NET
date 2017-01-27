namespace Rasa.Structures
{
    public class PerformAbilityData
    {
        public MapChannelClient MapClient { get; set; }
        public int ActionId { get; set; }
        public int ActionArgId { get; set; }
        public long TargetId { get; set; }
        public int ItemId { get; set; }

        public PerformAbilityData(MapChannelClient mapClient, int actionId, int actionArgId, long targetId, int itemId)
        {
            MapClient = mapClient;
            ActionId = actionId;
            ActionArgId = actionArgId;
            TargetId = targetId;
            ItemId = itemId;
        }
    }
}
