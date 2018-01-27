namespace Rasa.Structures
{
    using Game;

    public class PerformAbilityData
    {
        public Client Client { get; set; }
        public int ActionId { get; set; }
        public int ActionArgId { get; set; }
        public long TargetId { get; set; }
        public int ItemId { get; set; }

        public PerformAbilityData(Client client, int actionId, int actionArgId, long targetId, int itemId)
        {
            Client = client;
            ActionId = actionId;
            ActionArgId = actionArgId;
            TargetId = targetId;
            ItemId = itemId;
        }
    }
}
