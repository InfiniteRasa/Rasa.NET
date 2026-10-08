using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Structures;
    using Structures.Char;

    /// <summary>
    /// What a character leaves the world with, kept for when it comes back: health, armour and
    /// power, and the penalties a revive leaves (Rez Trauma and the no-healing, PlayerDeath).
    ///
    /// Loading a character used to put it on full health, armour and power (UpdateStatsValues
    /// with a full reset) with no effects at all, so logging out and in again was a free heal,
    /// and a way out of Rez Trauma's -60% and its six minutes - leaving dead sends a player to
    /// their hospital first (PlayerDeath.PlayerLeaving), which puts the trauma on, and the logout
    /// then cleared it with every other effect.
    ///
    /// - On leaving the world (a logout or a dropped connection, MapChannelManager.RemovePlayer,
    ///   before the effects are cleared) the three values go to the character's row, with the
    ///   wall-clock time each penalty wears off.
    /// - On loading, the row's values are held on the manifestation (Manifestation.LeftWith),
    ///   and nothing is saved over the row while they are (<see cref="Save"/>).
    /// - On arriving in the world, the values go back on once the stats have been worked out,
    ///   no higher than the maximum they come to now (<see cref="ApplyVitals"/>), and the
    ///   penalties go back on once the player's own client has its actor
    ///   (<see cref="RestorePenalties"/>).
    ///
    /// The penalties run on wall-clock time while the player is away, as the cooldowns do
    /// (ActionReuse): logging out does not end them, waiting does - as it would in the game.
    /// Adrenaline (Chi) is not kept; every arrival starts it at 0 (MapChannelManager).
    /// </summary>
    public static class RelogVitals
    {
        /// <summary>The values to save for a player leaving the world at wall-clock <paramref name="nowUnixMs"/>.</summary>
        public static SavedVitals Capture(Manifestation player, long nowUnixMs)
        {
            var saved = new SavedVitals();

            if (player == null)
                return saved;

            // Leaving dead revives first (PlayerDeath.PlayerLeaving); a player still at zero comes
            // back as one whose health was not saved - on full - rather than at zero.
            if (player.State != CharacterState.Dead && player.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current > 0)
                saved.Health = health.Current;

            if (player.Attributes.TryGetValue(Attributes.Armor, out var armor))
                saved.Armor = Math.Max(0, armor.Current);

            if (player.Attributes.TryGetValue(Attributes.Power, out var power))
                saved.Power = Math.Max(0, power.Current);

            // On them, or - a connection that dropped on a loading screen - set aside for the
            // arrival (EffectCarry), clocks stopped.
            var effects = player.ActiveEffects.Values.Where(e => !e.IsExpired)
                .Concat(player.CarriedEffects.Select(c => c.Effect))
                .ToList();

            var trauma = Longest(effects, PlayerDeath.RezSicknessTypeId);

            if (trauma != null)
            {
                saved.RezTraumaStacks = Math.Max(1, trauma.Stacks);
                saved.RezTraumaEndsAt = nowUnixMs + trauma.RemainingMs;
            }

            var noHeal = Longest(effects, PlayerDeath.RezSicknessNoHealTypeId);

            if (noHeal != null)
                saved.NoHealEndsAt = nowUnixMs + noHeal.RemainingMs;

            return saved;
        }

        private static GameEffect Longest(IEnumerable<GameEffect> effects, int typeId) =>
            effects.Where(e => e.TypeId == typeId && e.HasDuration && e.RemainingMs > 0)
                .OrderByDescending(e => e.RemainingMs)
                .FirstOrDefault();

        /// <summary>
        /// Writes what a player leaving the world has left. Replaces whatever was saved before.
        ///
        /// Not for a character that has yet to arrive (<see cref="Manifestation.LeftWith"/> still
        /// held): its health, armour and power are worked out when its client answers the login's
        /// Wonkavate with MapLoaded, and until then they are the zeros the manifestation was made
        /// with, with no effect on it. The map's worker has such a client on its list a tick
        /// after the character was chosen, so a connection that ended on the login loading
        /// screen - a crash, a closed client - was taken out with a logout's saves, and this one
        /// wrote the nothing over the row: no health saved, which is full health; armour and
        /// power 0; Rez Trauma and the no-healing gone. Done on purpose it was a heal and a way
        /// out of the death penalties. The row is what such a character left with, and stays.
        /// </summary>
        public static void Save(Client client)
        {
            var player = client?.Player;

            if (player == null || player.Id == 0 || player.LeftWith != null)
                return;

            var saved = Capture(player, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            // As with the cooldowns: a database that cannot be reached costs these, not the
            // rest of the removal.
            try
            {
                using var unitOfWork = Server.GameUnitOfWorkFactory.CreateChar();
                unitOfWork.Characters.UpdateCharacterVitals(player.Id, saved.Health, saved.Armor, saved.Power,
                    (uint)Math.Max(0, saved.RezTraumaStacks), saved.RezTraumaEndsAt, saved.NoHealEndsAt);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Could not save the health and death penalties of {player.FamilyName}: {e.Message}");
            }
        }

        /// <summary>
        /// On arrival from a login, after the stats have been worked out on full: health, armour
        /// and power back to what the character left with, no more than their maximum now and
        /// health at least 1. One that was not saved stays on full. Does nothing on any other
        /// arrival - only a load sets <see cref="Manifestation.LeftWith"/>.
        /// </summary>
        public static void ApplyVitals(Manifestation player)
        {
            var saved = player?.LeftWith;

            if (saved == null)
                return;

            Put(player, Attributes.Health, saved.Health, 1);
            Put(player, Attributes.Armor, saved.Armor, 0);
            Put(player, Attributes.Power, saved.Power, 0);
        }

        private static void Put(Manifestation player, Attributes id, int value, int floor)
        {
            if (value == CharacterEntry.VitalNotSaved || !player.Attributes.TryGetValue(id, out var attribute))
                return;

            attribute.Current = Math.Clamp(value, Math.Min(floor, attribute.CurrentMax), Math.Max(floor, attribute.CurrentMax));
        }

        /// <summary>
        /// On arrival from a login, with the player in the cells and their client holding their
        /// actor: the penalties still running at <paramref name="nowUnixMs"/> go back on for what
        /// they have left. Clears what was held, whether or not anything was due.
        /// </summary>
        public static void RestorePenalties(Client client, long nowUnixMs)
        {
            var player = client?.Player;
            var saved = player?.LeftWith;

            if (saved == null)
                return;

            player.LeftWith = null;

            PlayerDeath.RestorePenalties(player.MapChannel, player,
                saved.RezTraumaStacks,
                Math.Max(0, saved.RezTraumaEndsAt - nowUnixMs),
                Math.Max(0, saved.NoHealEndsAt - nowUnixMs));
        }
    }
}
