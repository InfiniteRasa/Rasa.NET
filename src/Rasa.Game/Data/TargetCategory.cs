namespace Rasa.Data
{
    /// <summary>
    /// generated/client/targetdata: how an entity presents to whoever looks at it. It is what
    /// Recv_TargetCategory sets on the client, and what the client's targeting, overhead names,
    /// colours and right-click menu go by:
    ///
    /// - HOSTILE: an enemy - red, attacked on right-click, picked by hostile targeting;
    /// - FRIENDLY: an ally - picked by friendly targeting, never attacked;
    /// - OBJECT: a usable (a Hortimonculus plant, a footlocker);
    /// - NEUTRAL: attackable - right-click offers Attack and hostile targeting picks a neutral
    ///   creature (actor.py, targeting.py) - with an overhead colour and name option of its own;
    /// - DECORATION, DECORATIONPROXY, IGNORE: scenery, not targeted at all.
    ///
    /// The server kept this as a two-value "faction" (Bane 0, AFS 1), which were HOSTILE and
    /// FRIENDLY by number; the creature table's faction column holds the category. What each
    /// category means to a fight is in <see cref="TargetCategories"/>.
    /// </summary>
    public enum TargetCategory
    {
        Hostile         = 0,
        Friendly        = 1,
        Object          = 2,
        Neutral         = 3,
        Decoration      = 4,
        DecorationProxy = 5,
        Ignore          = 6
    }

    /// <summary>
    /// What a target category means to a fight, on the server's side of it:
    ///
    /// - HOSTILE and FRIENDLY are at war: each goes looking for the other (the aggro scan), and a
    ///   player - always FRIENDLY, whatever Polymorph has made them look like - is attacked by
    ///   HOSTILE creatures;
    /// - NEUTRAL goes looking for nobody and nobody goes looking for it, but anyone may attack it
    ///   - players, their abilities, creatures on either side - and it fights back whoever does;
    /// - OBJECT, DECORATION, DECORATIONPROXY and IGNORE take no part: never attacked, never
    ///   attacking, never noticed.
    ///
    /// A player is FRIENDLY to creatures (Manifestation.CombatCategory) and is held to
    /// <see cref="MayFightPlayer"/> rather than <see cref="MayFight"/>, which differ in one
    /// place: a FRIENDLY creature never fights a FRIENDLY player. Polymorph does not change
    /// this - the disguise is a look, not a side, so a fight a player is in survives it.
    /// </summary>
    public static class TargetCategories
    {
        /// <summary>Whether an entity of this category can be in a fight at all.</summary>
        public static bool IsCombatant(TargetCategory category)
        {
            return category == TargetCategory.Hostile || category == TargetCategory.Friendly || category == TargetCategory.Neutral;
        }

        /// <summary>Whether one of seeker's category goes looking for one of target's: the aggro scan. HOSTILE and FRIENDLY only, at each other.</summary>
        public static bool Seeks(TargetCategory seeker, TargetCategory target)
        {
            return (seeker == TargetCategory.Hostile && target == TargetCategory.Friendly)
                || (seeker == TargetCategory.Friendly && target == TargetCategory.Hostile);
        }

        /// <summary>
        /// Whether one may fight the other once there is a reason to - a hit, an order, an assist:
        /// two combatants that are not on the same side. NEUTRAL fights and is fought by anyone.
        /// </summary>
        public static bool MayFight(TargetCategory attacker, TargetCategory target)
        {
            if (!IsCombatant(attacker) || !IsCombatant(target))
                return false;

            return attacker != target || attacker == TargetCategory.Neutral;
        }

        /// <summary>
        /// Whether a creature of this category may fight a player counting as playerCategory once
        /// there is a reason to (it was attacked, it was ordered): any combatant, bar a FRIENDLY
        /// creature against a FRIENDLY player. Going looking for one is <see cref="Seeks"/>.
        /// </summary>
        public static bool MayFightPlayer(TargetCategory creature, TargetCategory playerCategory)
        {
            if (!IsCombatant(creature))
                return false;

            return !(creature == TargetCategory.Friendly && playerCategory == TargetCategory.Friendly);
        }

        /// <summary>
        /// Whether a player's summon or ally goes looking for a creature of this category - a
        /// turret's aim, a crab mine's prey: the same as a FRIENDLY creature's scan, HOSTILE only.
        /// NEUTRAL is left alone until someone attacks it.
        /// </summary>
        public static bool AlliesSeek(TargetCategory target) => Seeks(TargetCategory.Friendly, target);

        /// <summary>Whether a player may attack an entity of this category: HOSTILE or NEUTRAL, as the client's own Attack option and hostile targeting allow.</summary>
        public static bool PlayerMayAttack(TargetCategory target)
        {
            return target == TargetCategory.Hostile || target == TargetCategory.Neutral;
        }
    }
}
