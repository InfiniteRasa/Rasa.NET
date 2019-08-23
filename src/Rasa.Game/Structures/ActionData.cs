namespace Rasa.Structures
{
    using Data;

    public class ActionData
    {
        public Actor Actor { get; set; }
        public ActionId ActionId { get; set; }
        public uint ActionArgId { get; set; }
        public uint Args { get; set; }
        public long TargetId { get; set; }
        public int ItemId { get; set; }
        public long WaitTime { get; set; }
        public long PassedTime { get; set; }

        public bool IsInrerrupted = false;

        public ActionData(Actor actor, ActionId actionId, uint actionArgId, long waitTime)
        {
            Actor = actor;
            ActionId = actionId;
            ActionArgId = actionArgId;
            WaitTime = waitTime;
        }

        public ActionData(Actor actor, ActionId actionId, uint actionArgId, uint args, long waitTime)
        {
            Actor = actor;
            ActionId = actionId;
            ActionArgId = actionArgId;
            Args = args;
            WaitTime = waitTime;
        }
    }
}
