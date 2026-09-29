using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// A weapon template's reach, attack type and melee (alt) damage, for Retune_weapon_template_stats.
    ///
    /// Range: the maxRange of the weapon class's attack action (action_level), the reach the
    /// client itself gives the attack - 4 m for blades, 10 m for propellant guns, 20 m for
    /// pistols, shotguns and staves, 30 m (40 m for 149/8) for machine guns, 40 m for grenade
    /// launchers, net and polarity guns, 50 m for injection guns, 60 m for rifles, rocket
    /// launchers, leech guns and Torqueshell rifles.
    ///
    /// Attack type: Melee for blades, whose attack is the blade itself (WEAPON_BLADE 418, 4 m);
    /// every other weapon fires, staves included (their attack reaches 20 m).
    ///
    /// Alt damage: the weapon's own damage (weaponclass.max_damage) times its type's ratio of
    /// melee to ranged damage in the strategy guide's weapon tables, which is the same at every
    /// level the guide lists (sum of its melee column over sum of its ranged column). The guide's
    /// damage is pre-launch and lower than the client's, so its melee column is not used as it
    /// stands; the ratio carries it over to the shipped damage, level and quality alike. Blades
    /// have no ranged column: their swing strikes for their own damage.
    /// </summary>
    public static class WeaponTemplateStats
    {
        /// <summary>Every template's placeholders: 80 m, ranged, 25 melee damage.</summary>
        public const int PlaceholderRange = 80;
        public const int PlaceholderAttackType = 2;
        public const int PlaceholderAltMaxDamage = 25;

        /// <summary>AttackType.Melee.</summary>
        public const int Melee = 1;

        /// <summary>WEAPON_BLADE: the blades' attack action.</summary>
        public const uint BladeActionId = 418;

        /// <summary>
        /// Weapon types by the weapon class's attack action and weapon_anim_condition_code, with
        /// the guide's melee : ranged ratio.
        /// </summary>
        public static readonly IReadOnlyList<(string Type, uint AttackActionId, int AnimCode, double MeleeRatio)> Types = new[]
        {
            ("Pistol", 1u, 1, 1.832),
            ("Pistol", 149u, 1, 1.832),
            ("Pistol", 149u, 23, 1.832),
            ("Rifle", 1u, 2, 1.153),
            ("Shotgun", 1u, 3, 0.665),
            ("Staff", 1u, 15, 0.929),
            ("Chaingun", 149u, 7, 5.339),
            ("Grenade launcher", 141u, 3, 0.692),
            ("Rocket launcher", 141u, 4, 0.278),
            ("Leech gun", 179u, 4, 5.745),
            ("Polarity gun", 249u, 7, 3.18),
            ("Propellant gun", 140u, 5, 3.834),
            ("Net gun", 398u, 3, 1.329),
            ("Injector gun", 399u, 1, 0.735),
            ("Torqueshell rifle", 230u, 2, 0.198),
            ("Blade", 418u, 6, 1.0),
        };
    }
}
