using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Packets;
    using Packets.MapChannel.Server.PerformRecovery;
    using Structures;

    /// <summary>
    /// What a creature's attack lands as. Nothing in the client marks a creature with a damage
    /// type: the type comes from the weapon the attack plays, and an attack names its weapon by
    /// the action and action argument it is performed with. So the type is looked for in three
    /// places, in order:
    ///
    ///  1. The creature_action row, when it states one. Content can say what an attack deals
    ///     whatever the client data implies.
    ///  2. The action's own DAMAGE_TYPE property, for the creature abilities that carry one -
    ///     CR_MOX_ENERGY_ATTACK is electrical in its ability data, and any creature ability wired
    ///     later brings its type with it.
    ///  3. CreatureWeaponDamage, the pair table mined from the client's weapon classes.
    ///
    /// Anything left over is physical, which is what every creature attack was until now. The
    /// answer is kept on the action so it is worked out once.
    /// </summary>
    public static class CreatureAttacks
    {
        public static DamageType DamageTypeOf(CreatureAction action)
        {
            if (action.DamageType != 0)
                return (DamageType)action.DamageType;

            if (action.ResolvedDamageType != 0)
                return action.ResolvedDamageType;

            var resolved = Resolve(action);

            action.ResolvedDamageType = resolved;

            return resolved;
        }

        /// <summary>
        /// The creature attacks that are blows, not shots: WEAPON_MELEE (174), whose arguments are
        /// the melee swings of the creature weapons (WEAPON_ATTACK_MELEE_THRAX_SOLDIER and the
        /// rest), and the creature abilities the client data names as melee - CR_MIASMA_MELEE,
        /// CR_FILCHER_MELEE, CR_LOPER_MELEE, CR_HOWLER_MELEE_ATTACK, CR_MAW_MELEE,
        /// CR_AMOEBOID_MELEE, CR_XANX_MELEE, CR_FLAREGASHER_MELEE, CR_GRANITOUR_MELEE,
        /// CR_ATTA_SOLDIER_MELEE and CR_ATTA_GRUB_MELEE. Only WEAPON_MELEE used to count, so the
        /// abilities landed as shots: a crouched player took no more of them
        /// (CROUCHED_MELEE_DAMAGE_TAKEN) and was no likelier to be critted
        /// (CROUCHED_MELEE_TO_BE_CRIT_MOD), while cover, a smoke screen and chaff cut them.
        ///
        /// Two more are counted as blows though the data does not name them melee: the Shield
        /// Drone's strike, CR_SHIELD_DRONE_ATTACK (0.5-3 m on every row; the guide gives the drone
        /// "Offense: None" and has players close in to melee it), and the Warden Bot's shock,
        /// CR_WARDEN_BOT_SHOCK (0.5-5 m). Both are contact strikes: they no longer need a line of
        /// sight to land, and cover and a smoke screen do not cut them. Ours, not the client's.
        /// </summary>
        private static readonly HashSet<ActionId> MeleeActions = new HashSet<ActionId>
        {
            ActionId.WeaponMelee,
            ActionId.CrMiasmaMelee, ActionId.CrFilcherMelee, ActionId.CrLoperMelee, ActionId.CrHowlerMeleeAttack,
            ActionId.CrMawMelee, ActionId.CrAmoeboidMelee, ActionId.CrXanxMelee, ActionId.CrFlaregasherMelee,
            ActionId.CrGranitourMelee, ActionId.CrAttaSoldierMelee, ActionId.CrAttaGrubMelee,
            ActionId.CrShieldDroneAttack, ActionId.CrWardenBotShock
        };

        public static bool IsMelee(CreatureAction action) => action != null && MeleeActions.Contains(action.ActionId);

        /// <summary>
        /// ARMOR_PIERCE_PERCENT of the attack's argument: the share of its damage that goes past
        /// armour, as a weapon's bypass does (MissileManager). The Flaregasher's melee gives 25 at
        /// every argument; nothing else a creature performs has it. 0 for none.
        /// </summary>
        public static int ArmorPierceOf(CreatureAction action)
        {
            if (action == null || AbilityManager.Instance == null || !AbilityManager.Instance.TryGetLevel(action.ActionId, action.ActionArgId, out var info))
                return 0;

            return Math.Clamp(info.Get(AbilityProperty.ArmorPiercePercent), 0, 100);
        }

        /// <summary>The player modules creature actions use whose class is DamageBase: hitdata (rawInfo, onHitData).</summary>
        private static readonly HashSet<string> DamageBaseModules = new HashSet<string>
        {
            "abilities.knockback", "abilities.stun", "abilities.tectonicstrike", "abilities.shrapnel",
            "abilities.deathdamage", "abilities.rushingblow"
        };

        public const string LightningModule = "abilities.lightning";
        public const string KaelRushingBlowModule = "abilities.ai.kaelrushingblowability";
        public const string LinkerHandBlastModule = "abilities.ai.linkerhandblastability";

        /// <summary>
        /// The hitdata shape an action's client class unpacks, by its module (RecoveryShape).
        /// No module, or a weapon's, is the weapon shape; a creature class under abilities.ai
        /// that does not say otherwise indexes a bare rawInfo, as does a class with no DoAbility
        /// of its own, which reads nothing.
        /// </summary>
        public static RecoveryShape ShapeOfModule(string module)
        {
            if (string.IsNullOrEmpty(module) || module.StartsWith("weapons."))
                return RecoveryShape.Weapon;

            if (module == LightningModule)
                return RecoveryShape.DamageArcs;

            if (DamageBaseModules.Contains(module))
                return RecoveryShape.Damage;

            if (module == KaelRushingBlowModule)
                return RecoveryShape.EntityRawInfo;

            if (module == LinkerHandBlastModule)
                return RecoveryShape.Drain;

            return RecoveryShape.RawInfo;
        }

        /// <summary>The recovery for a missile: a creature's ability in its class's shape, anything else as a weapon attack.</summary>
        public static ServerPythonPacket RecoveryFor(Missile missile)
        {
            if (TryGetAbilityShape(missile, out var shape))
                return new CreatureAbilityRecovery(missile, shape);

            return new WeaponAttackRecovery(missile);
        }

        /// <summary>
        /// Whether a missile is a weapon attack to the clients: its recovery the weapon's
        /// (RecoveryFor), read by a BaseWeaponAttack - a shot or a swing, a player's or a
        /// creature's - rather than by a creature ability's own class.
        /// </summary>
        public static bool IsWeaponAttack(Missile missile) => missile != null && !TryGetAbilityShape(missile, out _);

        private static bool TryGetAbilityShape(Missile missile, out RecoveryShape shape)
        {
            shape = RecoveryShape.Weapon;

            if (missile.Source is Creature && AbilityManager.Instance != null
                && AbilityManager.Instance.TryGetAction(missile.ActionId, missile.ActionArgId, out var module, out _))
                shape = ShapeOfModule(module);

            return shape != RecoveryShape.Weapon;
        }

        private static DamageType Resolve(CreatureAction action)
        {
            if (AbilityManager.Instance.TryGetLevel(action.ActionId, action.ActionArgId, out var level))
            {
                var stated = level.Get(AbilityProperty.DamageType);

                if (stated != 0)
                    return (DamageType)stated;
            }

            return CreatureWeaponDamage.Of((uint)action.ActionId, action.ActionArgId);
        }
    }
}
