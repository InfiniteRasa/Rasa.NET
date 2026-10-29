using System.Collections.Generic;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Spymaster Timlin and Lieutenant Feldman at the Snake Pit and Specialist Allen in the field, Valverde Pools (map
    /// 1304), for Looking For Logos? (1308), Mapping The Pools (1025) and Bane Investigation (932). The server did not
    /// have them.
    ///
    /// Sourced: client names 4385 "Spymaster Timlin", 4387 "Specialist Allen", 4376 "Lieutenant Feldman"
    /// (creaturenamelanguage); Timlin's package 343 (objectiveconversation 1025,1/2/9). Places: IGN TR Vault NPC 21 Timlin
    /// (-969.6, 915.7, 413.0), NPC 23 Allen (-784.7, 935.8, 571.9), NPC 17 Feldman (-933.3, 920.4, 384.5); TaRapedia
    /// Timlin (-970, 915, 414), Allen (-788, 936, 577), Feldman (-934, 920, 381). TaRapedia: Allen and Feldman level 40,
    /// AFS, male; Timlin level 50, female on the page, but the client's 1025 texts say "he" (user, 2026-10-25: male).
    /// The three points are on the navmesh's main region (0.1 m to 0.4 m above it) and are walked to from the Snake Pit.
    ///
    /// Reconstructed (ESTIMATE): the Snake Pit's officer uniform (PoolsSnakePitNpcsV1), Allen's too; level 40 for Timlin
    /// as the other two (the class's level, not TaRapedia's 50); the packages of Allen and Feldman server-only ids (the
    /// client has none for them, as Riggins's); the height of each place from the navmesh.
    ///
    /// Ids: creatures and pools 760202-760204, free before. Plain SQL, valid on SQLite and MySQL; down takes out only
    /// these rows.
    /// </summary>
    public static class PoolsSnakePitContactsV1
    {
        public const uint Timlin = 760202, TimlinName = 4385, TimlinPackage = 343;
        public const uint Allen = 760203, AllenName = 4387, AllenPackage = 760203;
        public const uint Feldman = 760204, FeldmanName = 4376, FeldmanPackage = 760204;

        public static readonly (double X, double Y, double Z) TimlinAt = (-969.6, 915.8, 413.0);
        public static readonly (double X, double Y, double Z) AllenAt = (-784.7, 936.0, 571.9);
        public static readonly (double X, double Y, double Z) FeldmanAt = (-933.3, 920.8, 384.5);

        public static IEnumerable<string> Up()
        {
            foreach (var sql in PoolsSnakePitNpcsV1.Npc(Timlin, "Spymaster Timlin - The Snake Pit", TimlinName, TimlinPackage, TimlinAt))
                yield return sql;
            foreach (var sql in PoolsSnakePitNpcsV1.Npc(Allen, "Specialist Allen - Valverde Pools", AllenName, AllenPackage, AllenAt))
                yield return sql;
            foreach (var sql in PoolsSnakePitNpcsV1.Npc(Feldman, "Lieutenant Feldman - The Snake Pit", FeldmanName, FeldmanPackage, FeldmanAt))
                yield return sql;
        }

        public static IEnumerable<string> Down()
        {
            foreach (var id in new[] { Feldman, Allen, Timlin })
            {
                yield return $"DELETE FROM spawnpool WHERE id = {id};";
                yield return $"DELETE FROM npc_package WHERE id = {id};";
                yield return $"DELETE FROM creature_appearance WHERE id = {id};";
                yield return $"DELETE FROM creature WHERE id = {id};";
            }
        }
    }
}
