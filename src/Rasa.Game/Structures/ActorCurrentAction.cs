namespace Rasa.Structures
{
    public class ActorCurrentAction
    {
        public int ActionId { get; set; }
        public int ActionArgId { get; set; }
        public uint TargetEntityId { get; set; }
        public delegate void ActorActionUpdateCallback(MapChannel channel, Actor actor, int newActionState);
    }
}
