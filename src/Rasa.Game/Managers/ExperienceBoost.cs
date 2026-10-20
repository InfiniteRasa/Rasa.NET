using System;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Structures;

    /// <summary>
    /// The Experience Boosters (CONSUMABLE_ADD_EFFECT 503, abilities.addeffect): XP_BOOST_EFFECT
    /// (10000053) on the player, GAME_EFFECT_ARG1 percent more experience from kills for
    /// GAME_EFFECT_ARG2 seconds. "Use this item to gain a 50% bonus to XP earned from killing
    /// enemies for 30 minutes": the Experience Booster (122036) and the Veteran one (130396), 50%
    /// for 30 minutes, and the Hyper Experience Booster (131966), 2000% for 60 minutes. The
    /// client's XPBoostEffect is the icon and nothing more: the bonus is the server's.
    ///
    /// - It is to kill experience, and only to the player's own part of it: a squad's kill is
    ///   split first (KillShares.AwardExperience) and each member's part is raised by their own
    ///   boost, after their item modules' bonus. A member with none gains nothing from another's.
    /// - A second booster while one is on does not stack. The boost starts again with the new
    ///   booster's time, at the higher of the two percents, so a weaker booster used during a
    ///   stronger one costs nothing of it.
    /// - It goes with the player across a map change, as other timed buffs do (EffectCarry), and
    ///   through a death (PlayerDeath.KeptInDeath).
    /// - Logging out keeps it: the time it has left is saved with the character
    ///   (character.xp_boost_*, RelogVitals) and goes on again at the next login. The time away
    ///   does not count.
    /// </summary>
    public static class ExperienceBoost
    {
        /// <summary>gameeffectdata XP_BOOST_EFFECT: gameeffects.xpboost.XPBoostEffect.</summary>
        public const int TypeId = 10000053;

        /// <summary>The boost on the player, or null: the one with the most time left, should there be two.</summary>
        public static GameEffect On(Manifestation player) =>
            player?.ActiveEffects.Values
                .Where(effect => effect.TypeId == TypeId && !effect.IsExpired)
                .OrderByDescending(effect => effect.RemainingMs)
                .FirstOrDefault();

        /// <summary>The percent more the player's boost gives them; 0 with none.</summary>
        public static int PercentOf(Manifestation player) => Math.Max(0, On(player)?.ExperienceBoostPercent ?? 0);

        /// <summary>A part of a kill's experience, raised by the player's boost.</summary>
        public static uint Boosted(Manifestation player, uint experience)
        {
            var percent = PercentOf(player);

            if (percent <= 0 || experience == 0)
                return experience;

            return (uint)Math.Min(uint.MaxValue, Math.Round(experience * (100.0 + percent) / 100.0));
        }

        /// <summary>
        /// A booster used: its boost on the player, in place of any they have - with its time, and
        /// the higher of the two percents. The client's AddEffectAbility announces it from the
        /// recovery's hit (AnnounceGameEffectAttach), so the attach does not.
        /// </summary>
        public static GameEffect Use(MapChannel mapChannel, Manifestation player, ActionLevelInfo info)
        {
            var percent = info.Get(AbilityProperty.GameEffectArg1);
            var seconds = info.Get(AbilityProperty.GameEffectArg2);

            return Put(mapChannel, player, Math.Max(percent, PercentOf(player)), info.Level, seconds * 1000L, announce: false);
        }

        /// <summary>
        /// The boost on the player for durationMs, any they have taken off first. announce: the
        /// attach announces itself - a boost put back at a login, which no ability announces.
        /// </summary>
        public static GameEffect Put(MapChannel mapChannel, Manifestation player, int percent, uint level, long durationMs, bool announce)
        {
            if (mapChannel == null || player == null || percent <= 0 || durationMs <= 0)
                return null;

            foreach (var old in player.ActiveEffects.Values.Where(effect => effect.TypeId == TypeId).ToList())
                GameEffectManager.Instance.DettachEffect(mapChannel, player, old);

            var boost = new GameEffect
            {
                TypeId = TypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = Math.Max(1u, level),
                SourceId = player.EntityId,
                Source = player,
                SourceLevel = player.Level,
                ExpiresTick = Environment.TickCount64 + durationMs,
                ExperienceBoostPercent = percent,
                IsBuff = true,
                AllowDetach = true,
                AnnounceOnAttach = announce
            };

            GameEffectManager.Instance.Attach(mapChannel, player, boost);

            return player.ActiveEffects.ContainsKey(boost.EffectId) ? boost : null;
        }
    }
}
