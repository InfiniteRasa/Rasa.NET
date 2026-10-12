namespace Rasa.Config
{
    /// <summary>
    /// appsettings.json's WeaponChecks: what the server does with a weapon shot at a target the
    /// shooter is not facing, one past the weapon's reach, or one wholly behind cover
    /// (Managers.WeaponChecks). Each is "off", "log" or "refuse", as the movement checks are, and
    /// picked up by a config reload.
    /// </summary>
    public class WeaponChecksConfig
    {
        /// <summary>The target is more than <see cref="Managers.WeaponChecks.FacingHalfAngle"/> degrees from the way the shooter faces.</summary>
        public string Facing { get; set; } = MovementChecksConfig.Log;

        /// <summary>The target is past twice the weapon's range, where its damage has already dropped to the floor. Logged by default: the shot lands for that floor, and a locked target walking off makes one honestly. Refused, the player is told it is out of range.</summary>
        public string Range { get; set; } = MovementChecksConfig.Log;

        /// <summary>Not one point on the target is in the clear from the shooter's eyes. Never refused by default: the server's mesh is not the client's.</summary>
        public string Sight { get; set; } = MovementChecksConfig.Log;
    }
}
