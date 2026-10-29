using System.Collections.Generic;
using System.Linq;
using Rasa.Structures;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// To-fix #20: a marker on a pool an objective clears is as wide as the pool's spawn radius (spawnpool.radius), so the
    /// map shows all the ground its creatures stand on. Holy Vengeance (1977) marked its outposts with
    /// PlateauHolyVengeanceV1.OutpostRadius (15 m): the outer pools 531225 and 531226 are 30 m wide, the inner 531224 40 m
    /// (the control room's 750075 and the tunnel's 750076, 4 m, keep 15 m). No other Plateau mission has a marker smaller
    /// than the pool it clears (PlateauMissionTests.PoolMarkersCoverThePoolsSpawnRadius). Plain SQL, the same on SQLite and
    /// MySQL (as PlateauWeaponsTraffickingMarkersV1). Down puts back 15 m.
    /// </summary>
    public static class PlateauPoolMarkersV1
    {
        /// <summary>Holy Vengeance's markers (indicator above MissionIndicator.UnnamedFrom), their pools and the pools' radius.</summary>
        public static readonly (uint Indicator, uint Pool, double Radius)[] HolyVengeance =
        {
            (1, PlateauHolyVengeanceV1.OuterPools[0], 30),
            (2, PlateauHolyVengeanceV1.OuterPools[1], 30),
            (3, PlateauHolyVengeanceV1.InnerPools[0], 40)
        };

        public static IEnumerable<string> Up() =>
            HolyVengeance.Select(marker => Radius(marker.Indicator, marker.Radius));

        public static IEnumerable<string> Down() =>
            HolyVengeance.Select(marker => Radius(marker.Indicator, PlateauHolyVengeanceV1.OutpostRadius));

        private static string Radius(uint indicator, double radius) =>
            $"UPDATE mission_indicator SET radius = {F(radius)} WHERE mission_id = {PlateauHolyVengeanceV1.MissionId} " +
            $"AND content_revision = '{PlateauMissionDataV1.Revision}' AND indicator_id = {MissionIndicator.UnnamedFrom + indicator};";
    }
}
