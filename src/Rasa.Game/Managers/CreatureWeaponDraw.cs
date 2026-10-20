namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// An armed creature draws its weapon when it starts a fight and puts it away when the fight
    /// is over.
    ///
    /// The client places a weapon by the TOOL_READY state (24), set by Actor.Recv_WeaponReady,
    /// whose SetWeaponReady transitions and redraws the actor's gear: the weapon's
    /// equipableClassAccessoryConnectionPointWeaponOverride row for WEAPON_POSITION_DRAWN or
    /// _STOWED. For the creatures' weapons that is the human NPC pistol in the right hand or in
    /// its hip holster, the Thrax rifle and pistol in the right hand or nowhere at all, the
    /// Hunter's shield deployed or holstered. Nothing ever sent WeaponReady to a creature, so
    /// they all fought with it put away.
    ///
    /// Drawing also puts it into its combat stance (RequestVisualCombatMode(true)), the half of
    /// the pair that was never sent - a creature giving up has always sent (false). A creature
    /// with its weapon out carries TOOL_READY and the combat stance in its ActorInfo, so a player
    /// who comes upon the fight sees it armed. A dead one keeps it out: the body lies with its gun.
    /// "Armed" is a class in the weapon slot of its appearance; one without is told nothing.
    ///
    /// An NPC whose pose has its weapon out (NpcPoses) holds it without the stance: TOOL_READY
    /// alone is the client's "ready for combat", a rifle across the chest. A fight adds the
    /// stance, and its end takes only the stance away.
    ///
    /// An unarmed creature takes the stance alone for the fight - every monster model has its
    /// Idle - Combat - Engaged and - Ready and its combat movement, and the Treeback, Miasma,
    /// Xanx, Strider and Thraxus Machina their non-combat to combat transitions. Without it the
    /// client put one into combat only for an attack (BaseActorAbility.Windup's
    /// EnterVisualCombatMode, which lets go 2.5 s later), so a monster whose attacks were further
    /// apart than that went back to its idle and its relaxed walk between them, and played its
    /// transitions into combat and out again on every swing. A Maw is left out: its transitions
    /// are its emerge and descend, which MawLurk plays as it comes up and goes down.
    /// </summary>
    public static class CreatureWeaponDraw
    {
        public static bool IsArmed(Creature creature) =>
            creature?.AppearanceData != null && creature.AppearanceData.TryGetValue(EquipmentData.Weapon, out var weapon)
            && weapon != null && weapon.Class != 0;

        /// <summary>Whether an unarmed creature holds the combat stance through a fight: all but a Maw.</summary>
        public static bool HoldsStance(Creature creature) => creature != null && !MawLurk.Is(creature);

        /// <summary>The creature draws, if it is armed and has not already; one that is not takes the stance.</summary>
        public static void Draw(MapChannel mapChannel, Creature creature)
        {
            if (mapChannel == null || creature == null)
                return;

            if (!IsArmed(creature))
            {
                TakeStance(mapChannel, creature);
                return;
            }

            // Out for its pose already: only the stance is new.
            var drawn = creature.WeaponDrawn;

            if (drawn && (creature.InCombatMode || !NpcPoses.KeepsWeaponOut(creature)))
                return;

            creature.WeaponDrawn = true;
            creature.InCombatMode = true;

            if (!drawn)
                CellManager.Instance.CellCallMethod(mapChannel, creature, new WeaponReadyPacket(true));

            CellManager.Instance.CellCallMethod(mapChannel, creature, new RequestVisualCombatModePacket(true));

            // A Hunter's weapon is its shield: up for the fight (HunterShield).
            HunterShield.Raise(mapChannel, creature);
        }

        /// <summary>The creature puts it away, if it has it out and is alive to; one with nothing out drops the stance.</summary>
        public static void Stow(MapChannel mapChannel, Creature creature)
        {
            if (mapChannel == null || creature == null
                || creature.State == CharacterState.Dead || creature.State == CharacterState.Dying)
                return;

            if (!creature.WeaponDrawn)
            {
                DropStance(mapChannel, creature);
                return;
            }

            // Out for its pose: it stays out, and only a stance it took for a fight goes.
            if (NpcPoses.KeepsWeaponOut(creature))
            {
                if (!creature.InCombatMode)
                    return;

                creature.InCombatMode = false;
                CellManager.Instance.CellCallMethod(mapChannel, creature, new RequestVisualCombatModePacket(false));
                HunterShield.Lower(mapChannel, creature);
                return;
            }

            creature.WeaponDrawn = false;
            creature.InCombatMode = false;
            HunterShield.Lower(mapChannel, creature);

            CellManager.Instance.CellCallMethod(mapChannel, creature, new WeaponReadyPacket(false));
            CellManager.Instance.CellCallMethod(mapChannel, creature, new RequestVisualCombatModePacket(false));
        }

        /// <summary>An unarmed creature into the combat stance for its fight, told to everyone around.</summary>
        private static void TakeStance(MapChannel mapChannel, Creature creature)
        {
            if (creature.InCombatMode || !HoldsStance(creature))
                return;

            creature.InCombatMode = true;
            CellManager.Instance.CellCallMethod(mapChannel, creature, new RequestVisualCombatModePacket(true));
        }

        /// <summary>Out of the stance: the fight is over.</summary>
        private static void DropStance(MapChannel mapChannel, Creature creature)
        {
            if (!creature.InCombatMode)
                return;

            creature.InCombatMode = false;
            CellManager.Instance.CellCallMethod(mapChannel, creature, new RequestVisualCombatModePacket(false));
        }
    }
}
