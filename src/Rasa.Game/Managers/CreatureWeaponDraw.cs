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
    /// </summary>
    public static class CreatureWeaponDraw
    {
        public static bool IsArmed(Creature creature) =>
            creature?.AppearanceData != null && creature.AppearanceData.TryGetValue(EquipmentData.Weapon, out var weapon)
            && weapon != null && weapon.Class != 0;

        /// <summary>The creature draws, if it is armed and has not already.</summary>
        public static void Draw(MapChannel mapChannel, Creature creature)
        {
            if (mapChannel == null || creature == null || creature.WeaponDrawn || !IsArmed(creature))
                return;

            creature.WeaponDrawn = true;
            creature.InCombatMode = true;

            CellManager.Instance.CellCallMethod(mapChannel, creature, new WeaponReadyPacket(true));
            CellManager.Instance.CellCallMethod(mapChannel, creature, new RequestVisualCombatModePacket(true));
        }

        /// <summary>The creature puts it away, if it has it out and is alive to.</summary>
        public static void Stow(MapChannel mapChannel, Creature creature)
        {
            if (mapChannel == null || creature == null || !creature.WeaponDrawn
                || creature.State == CharacterState.Dead || creature.State == CharacterState.Dying)
                return;

            creature.WeaponDrawn = false;
            creature.InCombatMode = false;

            CellManager.Instance.CellCallMethod(mapChannel, creature, new WeaponReadyPacket(false));
            CellManager.Instance.CellCallMethod(mapChannel, creature, new RequestVisualCombatModePacket(false));
        }
    }
}
