using System.Collections.Generic;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// To-fix #18: Maligo Base's keypass officers, Watchman Zyrutha (pool 750088, Showdown 876) and Overseer Kythura
    /// (pool 750089, One Way Trip 877), come back after <see cref="OfficerRespawnSeconds"/> instead of
    /// PlateauMaligoBaseV1.OfficerRespawnSeconds (600): a squad member who needs the keypass after another killed the
    /// officer waited ten minutes. 60 s is ESTIMATE (no source gives their respawn). Plain SQL, the same on SQLite and
    /// MySQL (as PlateauWeaponsTraffickingMarkersV1). Down puts back 600.
    ///
    /// Note: spawnpool.respown_time is in tenths of a second, so 60 is 6 s and 600 was a minute already;
    /// PlateauMaligoOfficerSquadRespawnV1 sets 600 again.
    /// </summary>
    public static class PlateauMaligoOfficerRespawnV1
    {
        public const uint OfficerRespawnSeconds = 60;

        public static readonly uint[] Officers = { PlateauMaligoBaseV1.Zyrutha, PlateauMaligoBaseV1.Kythura };

        public static IEnumerable<string> Up() => new[] { Respawn(OfficerRespawnSeconds) };

        public static IEnumerable<string> Down() => new[] { Respawn(PlateauMaligoBaseV1.OfficerRespawnSeconds) };

        private static string Respawn(uint seconds) =>
            $"UPDATE spawnpool SET respown_time = {seconds} WHERE id IN ({string.Join(", ", Officers)});";
    }
}
