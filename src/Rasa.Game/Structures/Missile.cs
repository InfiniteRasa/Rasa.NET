namespace Rasa.Structures
{
    using Data;

    public class Missile
    {
        public long DamageA { get; set; }
        public int DamageB { get; set; }
        public ActionId ActionId { get; set; }
        public uint ActionArgId { get; set; }
        public bool IsAbility { get; set; }         // set to true to use PerformAbility instead of Windup/Recovery
        public ulong TargetEntityId { get; set; }    // the entityId of the destination (it is possible that the object does no more exist on arrival)
        public Actor TargetActor { get; set; }
        public Actor Source { get; set; }
        public long TriggerTime { get; set; }       // amount of milliseconds left before the missile is triggered, is decreased on every tick
    }
}
