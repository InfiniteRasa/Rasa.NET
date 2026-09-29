namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Traitor (AA_SPY_TRAITOR 393, abilities.traitor): an enemy turns on its own for a while. The
    /// client's TraitorAction is aimed at one hostile creature, and only a BIOLOGICAL or MACHINA
    /// one; its target effect, TRAITOR_EFFECT (10000057, FX 1113), is announced on the target
    /// by the recovery. Per pump: DURATION 10-30 s, EFFECT_RADIUS 10-50 m.
    ///
    /// For the effect's duration the creature is FRIENDLY (Data.TargetCategory):
    /// - its hate is wiped and it stops what it was doing, so it no longer goes for the players;
    /// - FRIENDLY creatures never go for players, and seek HOSTILE creatures (TargetCategories.
    ///   Seeks), so it turns on the hostile creatures around it, whatever they are - within
    ///   EFFECT_RADIUS, which stands in for its aggro range - and they on it. NEUTRAL creatures it
    ///   leaves alone unless they attack it;
    /// - the clients are told its target category is FRIENDLY, so it shows as friendly, and
    ///   players' abilities (AbilityManager.IsHostile) leave it alone;
    /// - its MasterEntityId is the caster, so what it kills is the caster's
    ///   (CreatureManager.HandleCreatureKill), and its stance Aggressive, since a creature with a
    ///   master only looks for enemies when it is (BehaviorManager.ScansForEnemies).
    /// When the effect ends - run out, or the creature dead - it has its own target category and
    /// aggro range back, its hate wiped once more, and whoever it finds first is who it fights.
    ///
    /// Hack (AA_SAPPER_HACK 303, abilities.hack) is the same turn for machines: "Debuffs a single
    /// enemy mechanical target making it attack other enemy units within a given radius for a set
    /// time. Larger mechanicals such as Stalkers, Striders and Juggernauts are immune to this
    /// effect." HackAction allows a MECHANICAL or MACHINA creature; its effect is HACKED_EFFECT
    /// (223), announced by the client's targetGameEffect. Per pump: DURATION 10-30 s,
    /// EFFECT_RADIUS ("Hate Area") 10-50 m, range 20-50 m. A creature of SPECIES_STALKER,
    /// SPECIES_STRIDER or SPECIES_JUGGERNAUT takes nothing and the clients are told it is immune
    /// (GameEffectAttachFailed, EFFECT_ATTACH_FAIL_IMMUNE).
    /// </summary>
    public partial class AbilityManager
    {
        public const string TraitorModule = "abilities.traitor";
        public const int TraitorTypeId = 10000057;               // TRAITOR_EFFECT
        public const string HackModule = "abilities.hack";
        public const int HackedTypeId = 223;                     // HACKED_EFFECT

        /// <summary>Whether the client's TraitorAction would allow this target: a BIOLOGICAL or MACHINA creature.</summary>
        public static bool CanTurnTraitor(Creature creature)
        {
            if (creature == null)
                return false;

            var flags = CreatureManager.CreatureFlagsOf(creature);

            return flags.Contains((int)CreatureFlag.Biological) || flags.Contains((int)CreatureFlag.Machina);
        }

        /// <summary>Whether the client's HackAction would allow this target: a MECHANICAL or MACHINA creature.</summary>
        public static bool CanHack(Creature creature)
        {
            if (creature == null)
                return false;

            var flags = CreatureManager.CreatureFlagsOf(creature);

            return flags.Contains((int)CreatureFlag.Mechanical) || flags.Contains((int)CreatureFlag.Machina);
        }

        /// <summary>"Larger mechanicals such as Stalkers, Striders and Juggernauts are immune to this effect."</summary>
        public static bool IsHackImmune(Creature creature)
        {
            if (creature == null)
                return false;

            var flags = CreatureManager.CreatureFlagsOf(creature);

            return flags.Contains((int)CreatureFlag.SpeciesStalker) || flags.Contains((int)CreatureFlag.SpeciesStrider) || flags.Contains((int)CreatureFlag.SpeciesJuggernaut);
        }

        /// <summary>Turns the creature for DURATION; false when it cannot be turned.</summary>
        private bool AttachTraitor(MapChannel mapChannel, Manifestation player, Creature target, ActionLevelInfo info)
        {
            return CanTurnTraitor(target) && TurnCreature(mapChannel, player, target, info, TraitorTypeId) != null;
        }

        /// <summary>
        /// Hacks the machine for DURATION; false when it cannot be. An immune one is announced as
        /// immune to everyone who can see it.
        /// </summary>
        private bool AttachHack(MapChannel mapChannel, Manifestation player, Creature target, ActionLevelInfo info)
        {
            if (!CanHack(target) || !IsHostile(player, target))
                return false;

            if (IsHackImmune(target))
            {
                CellManager.Instance.CellCallMethod(mapChannel, target,
                    new GameEffectAttachFailedPacket(HackedTypeId, GameEffectAttachFailedPacket.FailReason.Immune, player.EntityId));
                return false;
            }

            return TurnCreature(mapChannel, player, target, info, HackedTypeId) != null;
        }

        /// <summary>
        /// Traitor, Hack and Mind Control's Enslavement alike: the creature fights for the player for
        /// DURATION under effect typeId. Its aggro range is EFFECT_RADIUS, or its own when
        /// keepAggroRange. Returns the attached effect, null when it cannot be turned.
        /// </summary>
        private GameEffect TurnCreature(MapChannel mapChannel, Manifestation player, Creature target, ActionLevelInfo info, int typeId, bool keepAggroRange = false)
        {
            if (!IsHostile(player, target))
                return null;

            var effect = NewEffect(mapChannel, player, info, typeId, info.Get(AbilityProperty.Duration, 10));

            effect.IsBuff = false;
            effect.AllowDetach = false;

            var category = target.TargetCategory;
            var aggroRange = target.AggroRange;
            var master = target.MasterEntityId;
            var stance = target.Stance;

            effect.OnDetached = (map, actor, e) =>
            {
                if (!(actor is Creature turned))
                    return;

                turned.TargetCategory = category;
                turned.AggroRange = aggroRange;
                turned.MasterEntityId = master;
                turned.Stance = stance;
                turned.Hate.Clear();

                if (map != null && turned.State != CharacterState.Dead)
                {
                    CellManager.Instance.CellCallMethod(turned, new TargetCategoryPacket(category));
                    BehaviorManager.Instance.StopFighting(turned);
                }
            };

            GameEffectManager.Instance.Attach(mapChannel, target, effect);

            target.TargetCategory = TargetCategory.Friendly;
            if (!keepAggroRange)
                target.AggroRange = info.Get(AbilityProperty.EffectRadius, 10);
            target.MasterEntityId = player.EntityId;
            target.Stance = MinionStance.Aggressive;   // a creature with a master only scans when aggressive
            target.Hate.Clear();

            CellManager.Instance.CellCallMethod(target, new TargetCategoryPacket(TargetCategory.Friendly));
            BehaviorManager.Instance.StopFighting(target);

            // The rest forget it was ever on their side.
            foreach (var cell in CellManager.CellsIn(mapChannel, target.Cells))
                foreach (var other in cell.CreatureList)
                    if (other != target)
                        other.Hate.Remove(target.EntityId);

            return effect;
        }
    }
}
