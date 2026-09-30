namespace Rasa.Data
{
    /// <summary>
    /// Being in combat, and what it costs.
    ///
    /// The numbers are the client's, from <c>shared/gameconstants.py</c> - shared, not client,
    /// which is the same fingerprint as the weapon heat constants: defined there and read by
    /// nothing in the client's own Python, because they were written for the server.
    ///
    /// <code>
    /// IN_COMBAT_REGEN_MODIFIER = 0.2
    /// NO_COMBAT_REGEN_MODIFIER = 1 / IN_COMBAT_REGEN_MODIFIER
    /// </code>
    ///
    /// Regeneration is entirely client-predicted: <c>ActorAttribute._EvaluatePredictedRefresh</c>
    /// adds <c>elapsed * refreshAmount / refreshPeriod</c> and the server never ticks health at
    /// all - it only sets the rate. So the modifier is applied by changing the rate we send.
    ///
    /// It is applied to the *period* rather than the amount, and that is what the second constant
    /// is for. Both fields are integers on the wire, so scaling an amount of 2 by 0.2 gives 0.4
    /// and truncates to nothing - regeneration would stop dead rather than slow to a fifth.
    /// Multiplying the period by <c>NO_COMBAT_REGEN_MODIFIER</c>, which is exactly 5, gives the
    /// ratio the constant asks for with no rounding at all. That the reciprocal was worth naming
    /// is the hint that this is how it was meant to be used.
    ///
    /// <c>PlayerEnteredCombat</c> and <c>PlayerExitedCombat</c> carry no arguments and do not
    /// touch this: all they do is toggle the combat indicator on the player's own status window.
    /// The regen change rides along as an ordinary attribute update.
    /// </summary>
    public static class CombatRegen
    {
        /// <summary>IN_COMBAT_REGEN_MODIFIER. Kept for reference; the period below is what is applied.</summary>
        public const double InCombatModifier = 0.2;

        /// <summary>
        /// The base refresh period, in seconds. <c>refreshPeriod</c> shares units with
        /// <c>gameclient.Time()</c>, which is seconds - <c>wargame.py</c> divides a millisecond
        /// duration by 1000 before adding it to that clock.
        /// </summary>
        public const int RegenPeriodSeconds = 1;

        /// <summary>
        /// The refresh period while in combat: NO_COMBAT_REGEN_MODIFIER, which is 1/0.2 = 5, times
        /// the base. Five times as long between ticks is one fifth the rate.
        /// </summary>
        public const int InCombatRegenPeriodSeconds = 5;

        /// <summary>
        /// How long a player stays in combat after the last damage dealt or taken.
        ///
        /// Ours. Nothing in the client says what puts a player in combat or for how long - the
        /// two packets carry no arguments and there is no timeout anywhere in the shipped
        /// constants. Fifteen seconds keeps a player in combat across a lull between pulls
        /// without holding them there long after a fight is over.
        /// </summary>
        public const long CombatTimeoutMs = 15000;

        /// <summary>
        /// Body armour does not regenerate at all while its wearer is in combat: "Body Armor
        /// doesn't regenerate during combat, and the moments of respite during a bonus experience
        /// run are few and far between" (strategy guide). IN_COMBAT_REGEN_MODIFIER is left to
        /// health. Players only; creature armour keeps the fifth (CreatureArmor).
        /// </summary>
        public const bool ArmorRegeneratesInCombat = false;
    }
}
