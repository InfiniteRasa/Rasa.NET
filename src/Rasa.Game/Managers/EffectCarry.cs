using System;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Structures;

    /// <summary>
    /// A player's timed buffs go with them across a map change instead of ending at the map
    /// line. Every effect used to be cleared on leaving a map (MapChannelManager.RemovePlayer,
    /// and a dropship ride), so a five-minute Rage, Medic Resistance or account-reward buff was
    /// gone at the first map link or dropship.
    ///
    /// On leaving, the buffs that can go (<see cref="Carries"/>) have their clocks stopped
    /// (GameEffect.Freeze) and are kept on the Manifestation while the effects are cleared as
    /// before. On arrival, once the player is in the world on the new map - Ingame, so that the
    /// attach sent around them reaches their own client as well as everyone else's: through a
    /// map link as the transfer completes, by dropship as the ship sets them down (phase 4) -
    /// each goes on again under a new effect id from the new map, its clock started with the
    /// time it had left: the loading screen and the ride cost nothing. The client's actor is new on every map, so the
    /// attach is a fresh one with that time as its duration, and it is announced as a newcomer
    /// would see it (AnnounceToNewcomers), which starts its visuals. One that was paused before
    /// the change is paused again. An aura goes without its copies - they were taken off the
    /// squad left behind - and its tick puts them on the squad at the other end.
    ///
    /// A logout carries nothing, and neither does a death: the dead have lost their effects
    /// already (GameEffectManager.DoWork).
    ///
    /// The penalties a revive leaves - Rez Trauma and the no-healing (PlayerDeath) - go too,
    /// although they are no buffs: left behind at the map line, a map link or a dropship was a
    /// way out of a death's price. A logout keeps them another way (RelogVitals).
    /// </summary>
    public static class EffectCarry
    {
        /// <summary>
        /// Whether an effect on the player goes with them: a buff with an end, told to the
        /// clients, their own rather than a copy of someone else's aura. Not one that drains
        /// adrenaline while it lasts (Sprint), not one bound to something that stays behind or
        /// ends with the map - the callbacks of a morph, a mind control, a bomb, a storm, a
        /// spotter, a Called Shot - nor a cloak (Detection is per map), nor a crowd control,
        /// nor a damage tick that credits someone else, nor one that keeps a place on the map to
        /// put its holder back at (ReturnTo): the armed Self Destruct, which has no callbacks and
        /// was carried, and on going off on the next map sent the player to the old map's
        /// coordinates - inside the terrain, as often as not.
        /// </summary>
        public static bool Carries(Actor holder, GameEffect effect)
        {
            if (holder == null || effect == null)
                return false;

            return (effect.IsBuff || PlayerDeath.IsDeathPenalty(effect.TypeId)) && effect.HasDuration
                && !effect.ServerOnly && !effect.IsSkillPassive && effect.Parent == null
                && effect.AdrenalineDrainPercentPerSecond <= 0
                && effect.OnTick == null && effect.OnExpired == null && effect.OnDetached == null && effect.OnDamaged == null
                && !effect.Hides && !effect.Blinds && !effect.IsStun && !effect.IsRoot && effect.MindControlPump == 0
                && !effect.ReturnTo.HasValue
                && (effect.TickDamageMax <= 0 || effect.SourceId == holder.EntityId)
                && !CritDeathManager.IsCritDeathType(effect.TypeId);
        }

        /// <summary>
        /// Before the player's effects are cleared on leaving a map: the ones that go have their
        /// clocks stopped and are kept for <see cref="Restore"/>. Any still kept from the change
        /// before - a ride left again before it set the player down - stay kept, clocks still
        /// stopped. The dead keep nothing.
        /// </summary>
        public static void Stash(Manifestation player)
        {
            if (player == null)
                return;

            if (player.State == CharacterState.Dead || player.State == CharacterState.Dying)
            {
                player.CarriedEffects.Clear();
                return;
            }

            var now = Environment.TickCount64;

            foreach (var effect in player.ActiveEffects.Values.OrderBy(e => e.EffectId).ToList())
            {
                if (effect.IsExpired || !Carries(player, effect))
                    continue;

                var wasPaused = effect.IsPaused;

                effect.Freeze(now);
                player.CarriedEffects.Add((effect, wasPaused));
            }
        }

        /// <summary>Nothing is carried: a logout, or anything else that is not a map change.</summary>
        public static void Drop(Manifestation player) => player?.CarriedEffects.Clear();

        /// <summary>
        /// On arrival, with the player in the new map's cells and Ingame: the kept buffs go on
        /// again with the time they had left. Returns how many.
        /// </summary>
        public static int Restore(Client client)
        {
            var player = client?.Player;
            var mapChannel = player?.MapChannel;

            if (player == null || player.CarriedEffects.Count == 0)
                return 0;

            var carried = player.CarriedEffects.ToList();
            player.CarriedEffects.Clear();

            if (mapChannel == null || player.State == CharacterState.Dead || player.State == CharacterState.Dying)
                return 0;

            var restored = 0;

            foreach (var (effect, wasPaused) in carried)
            {
                // What the map left behind: its id, its copies, and what ClearEffects put back.
                effect.EffectId = GameEffectManager.Instance.NextEffectId(mapChannel);
                effect.Holder = null;
                effect.Children.Clear();
                effect.MaxHealthApplied = 0;
                effect.AttributeApplied = 0;
                effect.AnnounceOnAttach = effect.AnnounceToNewcomers;

                effect.Thaw(Environment.TickCount64);

                GameEffectManager.Instance.Attach(mapChannel, player, effect, effect.AttachArgs.ToArray());

                if (!player.ActiveEffects.ContainsKey(effect.EffectId))
                    continue;

                if (wasPaused)
                    GameEffectManager.Instance.Pause(mapChannel, player, effect);

                restored++;
            }

            return restored;
        }
    }
}
