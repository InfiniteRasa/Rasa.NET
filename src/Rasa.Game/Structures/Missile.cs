namespace Rasa.Structures
{
    using Game;

    public class Missile
    {
        public int DamageA { get; set; }
        public int DamageB { get; set; }
        public int ActionId { get; set; }
        public int ActionArgId { get; set; }
        public bool IsAbility { get; set; }         // set to true to use PerformAbility instead of Windup/Recovery
        public uint TargetEntityId { get; set; }    // the entityId of the destination (it is possible that the object does no more exist on arrival)
        public Client Source { get; set; }
        public int TriggerTime { get; set; }       // amount of milliseconds left before the missile is triggered, is decreased on every tick
    }
}
