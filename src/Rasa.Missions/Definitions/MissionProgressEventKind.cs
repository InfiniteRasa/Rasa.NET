namespace Rasa.Data
{
    public enum MissionProgressEventKind
    {
        WaypointAcquired,
        LogosAcquired,
        CreatureKilled,
        MissionCompleted,
        ItemAcquired,
        ItemConsumed,
        InteractionUsed,
        AreaEntered,
        ItemEquipped,
        AbilityHit,
        ScenarioEvent,
        DeadlineElapsed,
        ObjectiveStateReached,
        ObjectHit,

        /// <summary>
        /// A creature of a kind was killed: the subject is a creature flag its class carries
        /// (creature_class_flag) - a species, as "Kill 40 Xanx" names one.
        /// </summary>
        CreatureFlagKilled,

        /// <summary>A creature of an entity class was killed: the subject is the class id.</summary>
        CreatureClassKilled
    }
}
