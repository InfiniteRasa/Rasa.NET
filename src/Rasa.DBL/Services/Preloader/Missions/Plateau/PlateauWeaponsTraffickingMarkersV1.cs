using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Rasa.Structures.World;
using Rasa.Structures;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Weapons Trafficking (904): a map marker for each prototype (objectives 2-6) on its footlocker in the Lamna Armory
    /// (squad instance 1465), as the other instance missions mark theirs. The places are the crates' own
    /// (PlateauWeaponsTraffickingV1.Prototypes), which are ESTIMATE; the radius is an object's (8 m, as Silent But
    /// Deadly's). Indicator ids 2-6 above MissionIndicator.UnnamedFrom, one per objective, past objective 1's on the
    /// Armory door. Plain SQL, the same on SQLite and MySQL, for a migration with no target model of its own (as
    /// PlateauFieldSqlV1): EF's InsertData needs one. The rows are those PlateauMissionDataV1.Indicator writes.
    ///
    /// Down takes out only these rows.
    /// </summary>
    public static class PlateauWeaponsTraffickingMarkersV1
    {
        public const double ObjectRadius = 8;

        public static IEnumerable<string> Up() =>
            PlateauWeaponsTraffickingV1.Prototypes.Select(prototype =>
                "INSERT INTO mission_indicator (mission_id, content_revision, requirement, objective_id, indicator_id, pos_x, pos_y, " +
                "pos_z, radius, show_3d_effect, comment) VALUES (" +
                string.Join(", ", PlateauWeaponsTraffickingV1.MissionId, Quote(PlateauMissionDataV1.Revision),
                    (byte)MissionContentRequirement.Required, prototype.Objective, MissionIndicator.UnnamedFrom + prototype.Objective,
                    D(prototype.At.X), D(prototype.At.Y), D(prototype.At.Z), D(ObjectRadius), 0, Quote("Plateau navigation")) + ");");

        public static IEnumerable<string> Down() => new[]
        {
            $"DELETE FROM mission_indicator WHERE mission_id = {PlateauWeaponsTraffickingV1.MissionId} " +
            $"AND content_revision = '{PlateauMissionDataV1.Revision}' " +
            $"AND indicator_id BETWEEN {MissionIndicator.UnnamedFrom + PlateauWeaponsTraffickingV1.Prototypes[0].Objective} " +
            $"AND {MissionIndicator.UnnamedFrom + PlateauWeaponsTraffickingV1.Prototypes[^1].Objective};"
        };

        private static string D(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string Quote(string text) => "'" + text.Replace("'", "''") + "'";
    }
}
