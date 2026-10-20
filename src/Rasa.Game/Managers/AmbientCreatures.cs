namespace Rasa.Managers
{
    using Data;
    using Structures;

    /// <summary>
    /// Ambient creatures: the wildlife of a map, there to be seen and not to fight. A creature is
    /// one when its class carries POWERLEVEL_0_AMBIENT (creature flag 22), or the creature itself
    /// does (ExtraFlags). The client's Ambient_* classes (Skitterin, Lumin, Swamp Rat ...) were
    /// made for it: no attack animations, no abilities.
    ///
    /// - It is NEUTRAL (TargetCategory), whatever its row says: the client names it in the neutral
    ///   colour and offers Attack on it, and nobody goes looking for it (TargetCategories.Seeks).
    /// - It has 1 health and no armour (<see cref="Apply"/>), so the first hit that lands kills it.
    /// - It fights nobody. NEUTRAL alone would fight back whoever attacked it; an ambient creature
    ///   does not, nor does it answer an assist, a taunt or an order (BehaviorManager.MayFight).
    /// - No creature fights it, on either side, a player's own creatures included: none picks it
    ///   as a target (BehaviorManager.MayFight), and no creature's shot, blast or effect damages
    ///   it (ActorManager.Damage, MissileManager).
    /// - A player may kill it, and gets nothing for it: no experience, adrenaline, kill streak,
    ///   loot, harvest or mission credit (CreatureManager.HandleCreatureKill). Its spawn pool
    ///   brings it back as any other.
    ///
    /// Its health and category are set as it is made (CreatureManager.CreateCreature and
    /// CreateScenarioCreature); the rest is asked of the class each time, so a flag changed
    /// through the game tools holds for fights at once and for health at the next spawn.
    /// </summary>
    public static class AmbientCreatures
    {
        /// <summary>An ambient creature's health: one hit.</summary>
        public const int Health = 1;

        /// <summary>Whether the creature is ambient: POWERLEVEL_0_AMBIENT on its class or on itself.</summary>
        public static bool IsAmbient(Creature creature)
        {
            return CreatureManager.HasFlag(creature, CreatureFlag.Powerlevel0Ambient);
        }

        /// <summary>Whether the entity is an ambient creature.</summary>
        public static bool IsAmbient(ulong entityId)
        {
            return EntityManager.Instance.Creatures.TryGetValue(entityId, out var creature) && IsAmbient(creature);
        }

        /// <summary>Whether this is a creature's hit on an ambient one, or an ambient one's on anything: neither lands.</summary>
        public static bool Spares(Actor source, Actor target)
        {
            return (source is Creature attacker && IsAmbient(attacker))
                || (source is Creature && target is Creature struck && IsAmbient(struck));
        }

        /// <summary>
        /// Makes a new ambient creature what it is: NEUTRAL, with <see cref="Health"/> health and
        /// no armour. Nothing for a creature that is not ambient. Returns whether it was.
        /// </summary>
        public static bool Apply(Creature creature)
        {
            if (!IsAmbient(creature))
                return false;

            creature.TargetCategory = TargetCategory.Neutral;
            creature.MaxHitPoints = Health;
            creature.Attributes[Attributes.Health] = CreatureHealth.NewAttribute(Health);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, CombatRegen.RegenPeriodSeconds);

            return true;
        }
    }
}
