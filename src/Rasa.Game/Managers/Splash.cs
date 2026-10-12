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
    /// Only what the shooter may attack is splashed (AbilityManager.VictimsWithin): hostile
    /// creatures, and their enemies across a wargame (Pvp) - no other player is caught by
    /// another player's rocket.
    ///
    /// A creature's launcher splashes the same: DefaultRadius around its target, for
    /// SplashPercent, with no crit. A creature has no weapon item, only the action and argument
    /// it attacks with, and the client's weapon classes for those name no radius either. Its
    /// launchers are the pairs the client plays as a rocket launcher - WEAPON_ROCKETLAUNCHER
    /// (141: the NeoBot's missile, the AFS Mech's missiles) and WEAPON_GROUNDTARGET (411, the Bane
    /// Mortar), both RocketLauncherAttack - and the Bane Grenade (WEAPON_ATTACK 229, the Thrax
    /// Grenadier's), which the client plays as a plain shot. What it splashes is what it may fight
    /// (CreatureAreaAttacks.FoesAround): players, and creatures of another side.
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

        /// <summary>The Bane Grenade's argument to WEAPON_ATTACK (Weapon_Creature_Bane_Grenade).</summary>
        public const uint BaneGrenadeArgId = 229;

        /// <summary>Whether a creature's attack is a launcher's: a rocket, a mortar shell or a Bane grenade.</summary>
        public static bool Splashes(CreatureAction action)
        {
            return action != null
                && (action.ActionId == ActionId.WeaponRocketlauncher || action.ActionId == ActionId.WeaponGroundtarget
                    || action.ActionId == ActionId.WeaponAttack && action.ActionArgId == BaneGrenadeArgId);
        }

        /// <summary>The splash radius of a creature's attack: DefaultRadius for a launcher's, 0 for any other.</summary>
        public static float RadiusOf(CreatureAction action) => Splashes(action) ? DefaultRadius : 0;

        /// <summary>What each splashed creature takes of a shot's damage.</summary>
        public static int DamageOf(int damage)
        {
            return damage <= 0 ? 0 : damage * SplashPercent / 100;
        }
    }
}
