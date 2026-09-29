using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// A weapon's alt attack and its rate of fire, from the client's own action data.
    ///
    /// The alt attack is the weapon's melee swing: WEAPON_MELEE (action 174) at the arg whose
    /// animation family names the weapon (generated.client.constant.animationfamily):
    /// 174/2 WEAPON_MELEE_STAFF_RESOLVE, 174/3 WEAPON_MELEE_MACHINE_GUNS_WINDUP/_RESOLVE,
    /// 174/4 ..._PISTOLS_, 174/5 ..._RIFLES_, 174/6 ..._SHOTGUNS_, 174/7 ..._PROPELLANT_GUNS_,
    /// 174/8 ..._ROCKET_LAUNCHERS_ and 174/287 WEAPON_MELEE_SWORD_. A weapon takes the swing of
    /// its weapon class's weapon_anim_condition_code (PISTOLS 1, RIFLES 2, SHOTGUNS 3,
    /// ROCKETLAUNCHERS 4, FLAMETHROWER 5, SWORD 6, MACHINEGUN 7, STAFF 15, S3_PISTOL 23) - the
    /// stance the client holds it in. Six families have no swing named for them and so take the
    /// one of the stance they share: grenade launchers and net guns the shotgun's, leech guns the
    /// rocket launcher's, polarity guns the machine gun's, injection guns the pistol's and
    /// Torqueshell rifles the rifle's. Its reach (alt_range) is that action's maxRange.
    ///
    /// The rate of fire (refire) is the weapon class's attack action as the client paces it
    /// (client/actions/baseactoraction.py): windupDelayMs, then recoveryDelayMs, with the reuse
    /// timer set at perform to recovery + reuseTimeMs - so windup + recovery + reuse from one
    /// shot to the next. The constant-fire weapons (leech, polarity and propellant guns) are not
    /// paced by their action: holding the trigger is one action, and the effect's pulses come at
    /// the refire (ConstantFire), for which the client has no figure. They keep theirs.
    ///
    /// The 200 tools (TOOL_PDA 14, HEALINGDISC 17) are untouched.
    /// </summary>
    public static class WeaponSwings
    {
        /// <summary>WEAPON_MELEE.</summary>
        public const uint MeleeActionId = 174;

        /// <summary>The placeholders every template had: alt 1/133 (a pistol shot) at 80 m, refire 800 ms.</summary>
        public const uint PlaceholderAltActionId = 1;
        public const uint PlaceholderAltActionArgId = 133;
        public const int PlaceholderAltRange = 80;
        public const int PlaceholderRefire = 800;

        /// <summary>The WEAPON_MELEE arg of each weapon_anim_condition_code.</summary>
        public static readonly IReadOnlyDictionary<int, uint> SwingByAnimCode = new Dictionary<int, uint>
        {
            [1] = 4,        // PISTOLS: pistols, injection guns
            [23] = 4,       // S3_PISTOL: the 149/10 pistols
            [2] = 5,        // RIFLES: rifles, Torqueshell rifles
            [3] = 6,        // SHOTGUNS: shotguns, grenade launchers, net guns
            [4] = 8,        // ROCKETLAUNCHERS: rocket launchers, leech guns
            [5] = 7,        // FLAMETHROWER: propellant guns
            [6] = 287,      // SWORD: blades
            [7] = 3,        // MACHINEGUN: machine guns, polarity guns
            [15] = 2,       // STAFF: staves
        };

        /// <summary>WEAPON_FLAMETHROWER, WEAPON_DENSITYGUN, WEAPON_POLARITYGUN: fired as constant fire.</summary>
        public static readonly uint[] ConstantFireActionIds = { 140, 179, 249 };
    }
}
