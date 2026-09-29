namespace Rasa.Data
{
    /// <summary>
    /// The weapon overheat model, as the client defines it.
    ///
    /// The mechanic is described in the client's own help, ID_HELP_HELPMENU_WEAPONJAM_TEXT:
    ///
    /// <para>"Always keep an eye on your current weapon in the weapon tray. As the weapon begins
    /// to overheat, the weapon icon will turn red. Slow down your rate of fire or switch to
    /// another weapon to avoid a jam. Some creatures have been known to jam weapons. Reload your
    /// weapon [%(Reload)s] to clear the jam."</para>
    ///
    /// The numbers come from <c>shared/gameconstants.py</c> - shared, not client, which is why
    /// they are here at all: the client never reads two of the three, so they were written for
    /// the server that is missing. The client's half of the model is in
    /// <c>Manifestation._UpdateWeaponHeat</c> and <c>AddWeaponHeat</c>:
    ///
    /// <code>
    /// coolAmt = weapon.coolRate * deltaTime * self.GetCoolRateModifier()
    /// newHeat = max(0, currentHeat - coolAmt) + heatAmount
    /// durabilityMod = (100 + (100 - weapon.GetCondition())) / 100.0
    /// heatAmount = weapon.heatPerShot * durabilityMod
    /// </code>
    ///
    /// <c>deltaTime</c> is in seconds - <c>gameclient.Time()</c> is added to second counts
    /// everywhere else it appears - and <c>GetCondition()</c> is hit points as a percentage of
    /// maximum, so a pristine weapon heats at face value and a ruined one at double.
    ///
    /// That the ceiling is 1000 is corroborated independently: the weapon drawer's heat meter is
    /// an animation scrubbed to <c>min(int(GetWeaponHeat(weapon)), kMaxAnimTime)</c> with
    /// <c>kMaxAnimTime = 1000</c>, and it plays the overheat warning exactly when it saturates.
    /// </summary>
    public static class WeaponHeat
    {
        /// <summary>HEAT_CAPACITY. Reaching it is what jams the weapon.</summary>
        public const double Capacity = 1000.0;

        /// <summary>
        /// MIN_HEAT_FOR_DURABILITY_LOSS. A weapon attack wears the weapon only while its barrel
        /// is at or above this (Managers.Durability.WearWeapon). At the placeholder heat numbers
        /// every template carries no weapon gets this hot, so none wears until they are corrected.
        /// </summary>
        public const double MinHeatForDurabilityLoss = 500.0;

        /// <summary>
        /// DEFAULT_HEAT_PER_SHOT. The fallback for a weapon whose template says nothing, which
        /// after the retune is no weapon at all - it is kept because it is the one number in the
        /// shipped constants that says what a normal weapon was supposed to feel like, and it
        /// puts the 10-per-shot the templates now carry in context.
        /// </summary>
        public const double DefaultHeatPerShot = 30.0;

        /// <summary>
        /// Heat added by one shot from a weapon in this condition.
        /// </summary>
        /// <param name="heatPerShot">The weapon template's heat_per_shot.</param>
        /// <param name="conditionPercent">Current hit points as a percentage of maximum, 0 to 100.</param>
        public static double PerShot(double heatPerShot, double conditionPercent)
        {
            if (heatPerShot <= 0)
                heatPerShot = DefaultHeatPerShot;

            // A condition outside 0..100 would scale the shot by a nonsense factor; clamp rather
            // than trust it, since it is arithmetic on two values from different tables.
            if (conditionPercent < 0)
                conditionPercent = 0;

            if (conditionPercent > 100)
                conditionPercent = 100;

            return heatPerShot * ((100.0 + (100.0 - conditionPercent)) / 100.0);
        }

        /// <summary>Heat shed in <paramref name="elapsedMs"/> at this weapon's cool rate.</summary>
        public static double Cooling(double coolRate, long elapsedMs)
        {
            if (coolRate <= 0 || elapsedMs <= 0)
                return 0;

            return coolRate * (elapsedMs / 1000.0);
        }
    }
}
