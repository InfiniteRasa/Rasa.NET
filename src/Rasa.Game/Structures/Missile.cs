namespace Rasa.Structures
{
    using Data;

    public class Missile
    {
        public int DamageA { get; set; }
        public int DamageB { get; set; }
        public ActionId ActionId { get; set; }
        public uint ActionArgId { get; set; }
        public bool IsAbility { get; set; }         // set to true to use PerformAbility instead of Windup/Recovery
        public ulong TargetEntityId { get; set; }    // the entityId of the destination (it is possible that the object does no more exist on arrival)
        public Actor TargetActor { get; set; }
        internal DynamicObject TargetObject { get; set; }
        public Actor Source { get; set; }
        /// <summary>Percent of DamageA that skips armour and comes straight off health (Torqueshell and Injection Guns skills).</summary>
        public int ArmorBypassPercent { get; set; }
        public long TriggerTime { get; set; }       // amount of milliseconds left before the missile is triggered, is decreased on every tick
        public MissileArgs Args = new MissileArgs();
    }
}
