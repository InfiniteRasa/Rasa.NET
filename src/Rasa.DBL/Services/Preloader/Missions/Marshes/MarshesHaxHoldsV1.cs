using System.Collections.Generic;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Specialist Hax holds through A Taste of Their Own Medicine's hack (546; review list, tester, real client 2026-11-04:
    /// Hax died 22-24 s after wave 1 in three runs, with weapon kills and with GM kills near the room; only GM kills at the
    /// waves' spawn spots kept him alive).
    ///
    /// Why: Hax (760362, MarshesLogosResearchNpcsV1) has the escort stats, 4000 hit points (EscortNpcStats), and nothing heals
    /// him; half of each wave goes for him (the other half for the newbie medic) for the hack's 120 s.
    ///
    /// Now (ESTIMATE, as Trapped!'s Samuel, MarshesRetreadCityNpcsV1.SamuelHealth for to-fix 29): three times the escort's,
    /// 12000 hit points, creature_stat and max_hp. The armour stays the escort's (300). The row is Hax's, so the public Hax
    /// (548 and 718's giver) has them too; he does not fight. Not changed: the waves' targets. Trapped!'s spare (the waves go
    /// for the other wards first) is MarshesDefenceScene's; 546 is a data.sequence scene, whose wave members each have one
    /// fixed target.
    /// </summary>
    public static class MarshesHaxHoldsV1
    {
        public const uint HitPoints = 3 * MarshesLogosResearchNpcsV1.EscortHealth;
        public const uint HitPointsBefore = MarshesLogosResearchNpcsV1.EscortHealth;

        public static IEnumerable<string> Up() => Set(HitPoints);

        public static IEnumerable<string> Down() => Set(HitPointsBefore);

        private static IEnumerable<string> Set(uint health)
        {
            yield return $"UPDATE creature_stat SET health = {health} WHERE id = {MarshesLogosResearchNpcsV1.Hax};";
            yield return $"UPDATE creature SET max_hp = {health} WHERE id = {MarshesLogosResearchNpcsV1.Hax};";
        }
    }
}
