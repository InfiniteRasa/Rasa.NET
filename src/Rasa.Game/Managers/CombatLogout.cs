using System;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Structures;

    /// <summary>
    /// A connection that drops in the middle of a fight leaves its character in the world until
    /// the fight would have let it go: the way out of a fight is not quicker by closing the game
    /// than by the Logout button.
    ///
    /// The Logout button waits out a countdown (MapChannelManager.LogoutDelayMs) the server holds
    /// the client to; a dropped connection - Alt+F4, a pulled cable - was taken out of the world on
    /// the map worker's next tick, and every creature let go of it the moment it dropped
    /// (Threat.CanFight). A player about to die closed the game and lived.
    ///
    /// Now a player who drops while in combat (Manifestation.InCombat) stays until combat would
    /// have run out for them (CombatExpiresAt, CombatRegen.CombatTimeoutMs after the last hit),
    /// and never less than the logout countdown, nor longer than one combat timeout from the drop
    /// - the body cannot be hit back into combat, as EnterCombat only counts a connected player.
    /// While it stays it can be fought and killed as before (Manifestation.IsLingering): the
    /// creatures keep at it, and a death is a death - the removal that follows takes the body to
    /// its hospital with Rez Trauma (PlayerDeath.PlayerLeaving), which the logout now keeps
    /// (RelogVitals). Everything else treats it as gone, as before: it triggers no map link,
    /// region or mission, and nothing is sent to it.
    ///
    /// Out of combat, or dropping while already logging out, dead, or on a loading screen, the
    /// character leaves on the next tick as it always has.
    ///
    /// The account cannot log in again while the body is there (Server.TakeOverSessions refuses
    /// it as AlreadyLoggedIn) - at most one combat timeout.
    /// </summary>
    public static class CombatLogout
    {
        /// <summary>
        /// When a player whose connection dropped at <paramref name="now"/> may leave the world;
        /// 0 for at once. <paramref name="stateBefore"/> is the connection's state before it closed.
        /// </summary>
        public static long LingerUntil(Manifestation player, ClientState stateBefore, long now)
        {
            if (player == null || player.MapChannel == null || stateBefore != ClientState.Ingame)
                return 0;

            // Already on the way out by the Logout button, dead, or out of the fight.
            if (player.RemoveFromMap || player.State == CharacterState.Dead || player.State == CharacterState.Dying || !player.InCombat)
                return 0;

            var until = Math.Max(now + MapChannelManager.LogoutDelayMs, player.CombatExpiresAt);

            return Math.Min(until, now + CombatRegen.CombatTimeoutMs);
        }

        /// <summary>Whether the map worker should leave this player in the world for now.</summary>
        public static bool Holds(Manifestation player) => player != null && player.IsLingering;
    }
}
