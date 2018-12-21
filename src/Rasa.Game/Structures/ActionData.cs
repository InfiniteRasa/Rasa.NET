namespace Rasa.Structures
{
    using Data;
    using Game;

    public class ActionData
    {
        public Client Client { get; set; }
        public ActionId ActionId { get; set; }
        public int ActionArgId { get; set; }
        public uint Args { get; set; }
        public long TargetId { get; set; }
        public int ItemId { get; set; }
        public long WaitTime { get; set; }
        public long PassedTime { get; set; }

        public ActionData(Client client, ActionId actionId, int actionArgId)
        {
            Client = client;
            ActionId = actionId;
            ActionArgId = actionArgId;
        }

        public ActionData(Client client, ActionId actionId, int actionArgId, uint args)
        {
            Client = client;
            ActionId = actionId;
            ActionArgId = actionArgId;
            Args = args;
        }

        public ActionData(Client client, ActionId actionId, int actionArgId, uint args, long waitTime)
        {
            Client = client;
            ActionId = actionId;
            ActionArgId = actionArgId;
            Args = args;
            WaitTime = waitTime;
        }
    }
}
