using System.Collections.Generic;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Valverde Chasm's mission creature: The Stainless Steel Bloodhound's (867) Modified Bane Sentinel, a
    /// template the mission's scene spawns by Field Sgt. Garde (510183, already in the NPC seed).
    ///
    /// Native: name 8537 "Modified Bane Sentinel" (creaturenamelanguage); Garde's 4536: "we managed to
    /// capture the Sentinel intact. Our R&amp;D guys at Fort Defiance reprogrammed it."
    /// Sourced: TaRapedia: it floats "slooooowly" beside the stream; patch notes (17 January 2008): the
    /// escort "only drops junk loot if killed", so it can be killed.
    /// Reconstructed (ESTIMATE): the client's Bane_Sentinel (7344), as Photo Ops' sentinels; friendly
    /// (faction 1), level 30, 3000 hit points, armor 150, no weapon.
    ///
    /// Ids: creature 650042, free before. Plain SQL.
    /// </summary>
    public static class PlateauChasmNpcsV1
    {
        public const uint Sentinel = 650042;
        public const uint SentinelClass = PlateauSurveillanceSentinelsV1.SentinelClass;
        public const uint SentinelName = 8537;
        public const uint Level = 30, HitPoints = 3000, Armor = 150;

        public static IEnumerable<string> Up()
        {
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                "action1, action2, action3, action4, action5, action6, action7, action8) " +
                $"VALUES ({Sentinel}, 'The Stainless Steel Bloodhound Modified Bane Sentinel', {SentinelClass}, 1, {Level}, " +
                $"{HitPoints}, {SentinelName}, 7, 2, 0, 0, 0, 0, 0, 0, 0, 0);";
            yield return PlateauCreatureSqlV1.Stat(Sentinel, HitPoints, Armor);
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM creature_stat WHERE id = {Sentinel};";
            yield return $"DELETE FROM creature WHERE id = {Sentinel};";
        }
    }
}
