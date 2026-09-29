namespace Rasa.Managers
{
    using Data;
    using Structures;

    /// <summary>
    /// The weapons that hit a cone in front of the shooter rather than one target: "Shotguns and
    /// propellant guns are both examples of weapons that do cone damage" (uielement 5374). "Because
    /// they do damage to an area in front of you, you will not be able to lock onto a target while
    /// this type of weapon is equipped."
    ///
    /// The client learns that a weapon is a cone weapon from the aeType it is sent (WeaponInfo and
    /// the item tooltip): aetypes.CONE puts targeting in cone mode, which drops the locked target
    /// and shows the cone reticle over aeRadius degrees, and BaseWeaponAttack turns the body to
    /// the camera before firing (useClientYaw). ae_type is 0 and ae_radius a placeholder 1 on
    /// every itemtemplate_weapon row, so without this a shotgun is aimed like a rifle. These are
    /// sent as CONE and HalfAngleOf for the two families instead.
    ///
    /// The angle is a choice: a row's own ae_radius when it is a CONE with more than the
    /// placeholder 1, otherwise DefaultHalfAngle - 45, the most common CONE_RADIUS in the
    /// abilities' data, which the client draws the same way. The server reads it as degrees
    /// either side of the aim, as it does the abilities' cones. The reach is the attack action's
    /// own maxRange (20 for every shotgun argument of WEAPON_ATTACK, 10 for WEAPON_FLAMETHROWER).
    /// </summary>
    public static class ConeWeapons
    {
        /// <summary>aetypes.CONE.</summary>
        public const uint AeCone = 3;

        /// <summary>Degrees either side of the aim, when the weapon has no cone of its own.</summary>
        public const float DefaultHalfAngle = 45f;

        /// <summary>Allowance on the reach for positions a tick old, as the abilities allow.</summary>
        public const float RangeSlack = 2.5f;

        /// <summary>Reach when the action has no range in the action tables: a shotgun's and a propellant gun's.</summary>
        public const float ShotgunRange = 20f;
        public const float PropellantRange = 10f;

        /// <summary>Whether the weapon fires over a cone: shotguns, and propellant guns (WEAPON_FLAMETHROWER).</summary>
        public static bool IsCone(WeaponInfo weaponInfo, WeaponClassInfo weaponClass)
        {
            return weaponInfo?.ToolType == ToolType.Shotgun || weaponClass?.WeaponAttackActionId == ActionId.WeaponFlamethrower;
        }

        /// <summary>Degrees either side of the aim: the row's own CONE radius when it has a real one, DefaultHalfAngle otherwise.</summary>
        public static float HalfAngleOf(WeaponInfo weaponInfo)
        {
            return weaponInfo != null && weaponInfo.AeType == AeCone && weaponInfo.AeRadius > 1 ? weaponInfo.AeRadius : DefaultHalfAngle;
        }

        /// <summary>
        /// The aeType and aeRadius the client is sent for a weapon: CONE and the half-angle for a
        /// cone weapon, the row's own otherwise (0 meaning none).
        /// </summary>
        public static (uint AeType, uint AeRadius) AeOf(WeaponInfo weaponInfo, WeaponClassInfo weaponClass)
        {
            if (weaponInfo == null)
                return (0, 0);

            if (IsCone(weaponInfo, weaponClass))
                return (AeCone, (uint)HalfAngleOf(weaponInfo));

            return (weaponInfo.AeType, weaponInfo.AeRadius);
        }

        /// <summary>How far the attack reaches: its action level's maxRange, or the family's own figure.</summary>
        public static float RangeOf(ActionId actionId, uint actionArgId)
        {
            if (AbilityManager.Instance.TryGetLevel(actionId, actionArgId, out var level) && level.MaxRange > 0)
                return level.MaxRange;

            return actionId == ActionId.WeaponFlamethrower ? PropellantRange : ShotgunRange;
        }
    }
}
