namespace Rasa.Managers
{
    using Data;
    using Structures;

    /// <summary>
    /// Launcher splash: "Allows the use of Rocket and Grenade Launcher weapons, which do damage to
    /// a target and splash damage to nearby enemies" (the Launchers skill, uielement 1198).
    ///
    /// The client has no radius or share for it. The weapon's own AE fields are where a radius
    /// belongs (WeaponInfo sends aeType and aeRadius, and the tooltip prints the radius for a
    /// PBAE or TARGETED weapon), but ae_type is 0 and ae_radius a placeholder 1 on every
    /// itemtemplate_weapon row, launchers included. So a launcher row that does carry a real
    /// TARGETED or PBAE radius is used as it stands, and otherwise the numbers here are a choice:
    /// DefaultRadius metres around the target hit, for SplashPercent of the shot's damage.
    ///
    /// The splash is worked out before the crit roll, so only the target hit can crit. Each
    /// splashed creature takes its share as a hit of its own - resistance, armour, threat, and a
    /// grenade's stun chance - and is listed in the same WeaponAttackRecovery as the target, which
    /// the client's BaseWeaponAttack.DoHits floats one hit at a time. RocketLauncherAttack keeps
    /// its FX on the target (updateFXTargets = 0), so the splash needs no FX of its own.
    ///
    /// Only hostile creatures are splashed (AbilityManager.HostilesWithin): no player is caught
    /// by another player's rocket.
    /// </summary>
    public static class Splash
    {
        /// <summary>Metres around the target hit, when the weapon has no radius of its own.</summary>
        public const float DefaultRadius = 5f;

        /// <summary>Percent of the shot's damage each splashed creature takes.</summary>
        public const int SplashPercent = 50;

        /// <summary>aetypes: PBAE 1, TARGETED 2 are the radial ones (CONE 3, SPECIAL 4 are not).</summary>
        public const uint AePbae = 1;
        public const uint AeTargeted = 2;

        /// <summary>Whether the weapon splashes: rocket and grenade launchers.</summary>
        public static bool Splashes(WeaponInfo weaponInfo)
        {
            return weaponInfo != null
                && (weaponInfo.ToolType == ToolType.RocketLauncher || weaponInfo.ToolType == ToolType.GrenadeLauncher);
        }

        /// <summary>
        /// The splash radius of a weapon in metres: its own when the row says it is a radial AE
        /// with more than the placeholder 1, DefaultRadius for any other launcher, 0 for a weapon
        /// that does not splash.
        /// </summary>
        public static float RadiusOf(WeaponInfo weaponInfo)
        {
            if (!Splashes(weaponInfo))
                return 0;

            if ((weaponInfo.AeType == AePbae || weaponInfo.AeType == AeTargeted) && weaponInfo.AeRadius > 1)
                return weaponInfo.AeRadius;

            return DefaultRadius;
        }

        /// <summary>What each splashed creature takes of a shot's damage.</summary>
        public static int DamageOf(int damage)
        {
            return damage <= 0 ? 0 : damage * SplashPercent / 100;
        }
    }
}
