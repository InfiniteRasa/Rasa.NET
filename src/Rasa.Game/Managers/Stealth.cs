namespace Rasa.Managers
{
    using System.Linq;
    using Structures;

    /// <summary>
    /// What gives a cloaked player away. The cloak itself is a GameEffect with Hides on it
    /// (Cloak Wave); this is the one place that takes it off when the player does something a
    /// cloak cannot survive:
    ///
    ///  - firing a weapon or launching an ability's missile (MissileManager.MissileLaunch), and
    ///  - taking damage that lands (AbilityManager.OnPlayerDamaged).
    ///
    /// A friendly ability, a heal or standing still leave it on, which is what a cloak for
    /// getting out of a fight is for. Nothing in the client's data says when a cloak breaks, so
    /// this is a choice rather than a reading.
    /// </summary>
    public static class Stealth
    {
        /// <summary>Takes every cloak off a player, if they had one.</summary>
        public static void Break(MapChannel mapChannel, Manifestation player)
        {
            if (player == null || mapChannel == null)
                return;

            foreach (var cloak in player.ActiveEffects.Values.Where(e => e.Hides).ToList())
                GameEffectManager.Instance.DettachEffect(mapChannel, player, cloak);
        }
    }
}
