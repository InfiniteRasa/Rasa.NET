namespace Rasa.Managers
{
    using Structures;

    /// <summary>
    /// The raids on the control points (BaneRaids, OutpostWalls) and the points' defences
    /// (ControlPointFields) joined: each was built to stand without the other, behind a hook the other
    /// fills, and this fills them once both are loaded.
    ///  - A point's gates are its own gate shields (control_point_link kind 7), not every faction field
    ///    within 120 m of it: where a raid's ships come down before the gates, and what "within the walls" is.
    ///  - A raider held back at a gate by a shield that stops it strikes at it with its own attack, through
    ///    the ordinary damage path (a missile at the field's entity, ForceFields.TakeHit), its cooldowns respected.
    ///  - A raider whose step a point's shield stops strikes at it whether or not it is fighting yet: the
    ///    raid is sent at the gate, where a creature of the open world strikes only in a fight.
    /// A shield brought down stands again whole ControlPointDefence.FieldRebuildSeconds later whatever the
    /// raid is doing: the raid does not hold it down, and its raiders strike at it again once it is up.
    /// </summary>
    public static class RaidDefences
    {
        /// <summary>Fills the raids' and the defences' hooks with each other's. From Server, after the control points and the placed fields; again is harmless.</summary>
        public static void Join()
        {
            OutpostWalls.FieldsOf = ControlPointFields.Of;
            BaneRaids.Instance.StrikeShield = ControlPointFields.Strike;
            ControlPointFields.StrikesFields = StrikesFields;
        }

        /// <summary>Who strikes at a point's shield that stops it: a fighting creature of no player's (ControlPointFields.DefaultStrikesFields), or one of a raid under way.</summary>
        public static bool StrikesFields(Creature creature) =>
            ControlPointFields.DefaultStrikesFields(creature) || BaneRaids.Instance.IsRaider(creature);
    }
}
