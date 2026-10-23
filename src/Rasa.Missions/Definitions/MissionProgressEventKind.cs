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
        CreatureClassKilled,

        /// <summary>
        /// A destructible object the server placed (world_destructible, PlacedDestructibles) was
        /// destroyed: the subject is its entity class - "Destroy 8 warnet nests". Shared with the
        /// destroyer's squad as a kill is.
        /// </summary>
        ObjectDestroyed,
        /// A spawn pool of a squad's instance has nothing left alive, queued or on its way: the
        /// subject is the spawn pool id. "Secure the outer Bane Outpost" is its pools cleared.
        /// Only a squad's instance keeps which of its pools are dead (SquadInstanceState).
        /// </summary>
        SpawnPoolCleared
    }
}
