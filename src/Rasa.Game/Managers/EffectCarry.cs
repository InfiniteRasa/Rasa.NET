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
    /// before. On arrival, once the player has been introduced to the new map, each goes on
    /// again under a new effect id from the new map, its clock started with the time it had
    /// left: the loading screen costs nothing. The client's actor is new on every map, so the
    /// attach is a fresh one with that time as its duration, and it is announced as a newcomer
    /// would see it (AnnounceToNewcomers), which starts its visuals. One that was paused before
    /// the change is paused again. An aura goes without its copies - they were taken off the
    /// squad left behind - and its tick puts them on the squad at the other end.
    ///
    /// A logout carries nothing, and neither does a death: the dead have lost their effects
    /// already (GameEffectManager.DoWork).
    /// </summary>
    public static class EffectCarry
    {
        /// <summary>
        /// Whether an effect on the player goes with them: a buff with an end, told to the
        /// clients, their own rather than a copy of someone else's aura. Not one that drains
        /// adrenaline while it lasts (Sprint), not one bound to something that stays behind or
        /// ends with the map - the callbacks of a morph, a mind control, a bomb, a storm, a
        /// spotter, a Called Shot - nor a cloak (Detection is per map), nor a crowd control,
        /// nor a damage tick that credits someone else.
        /// </summary>
        public static bool Carries(Actor holder, GameEffect effect)
        {
            if (holder == null || effect == null)
                return false;

            return effect.IsBuff && effect.HasDuration
                && !effect.ServerOnly && !effect.IsSkillPassive && effect.Parent == null
                && effect.AdrenalineDrainPercentPerSecond <= 0
                && effect.OnTick == null && effect.OnExpired == null && effect.OnDetached == null && effect.OnDamaged == null
                && !effect.Hides && !effect.Blinds && !effect.IsStun && !effect.IsRoot && effect.MindControlPump == 0
                && (effect.TickDamageMax <= 0 || effect.SourceId == holder.EntityId)
                && !CritDeathManager.IsCritDeathType(effect.TypeId);
        }

        /// <summary>
        /// Before the player's effects are cleared on leaving a map: the ones that go have their
        /// clocks stopped and are kept for <see cref="Restore"/>. Anything kept from before is dropped.
        /// </summary>
        public static void Stash(Manifestation player)
        {
            if (player == null)
                return;

            player.CarriedEffects.Clear();

            if (player.State == CharacterState.Dead || player.State == CharacterState.Dying)
                return;

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
        /// On arrival, with the player in the new map's cells: the kept buffs go on again with
        /// the time they had left. Returns how many.
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
