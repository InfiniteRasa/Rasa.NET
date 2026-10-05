using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Structures;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    /// <summary>
    /// The Wilderness missions' indicators, keyed as indicators without a client name.
    ///
    /// An indicator's id is sent to the client, whose map and radar look it up in
    /// missionobjectiveindicatorlanguage for the marker's tooltip. WildernessMissionDataV1 keyed
    /// its rows 1, 2, 3.. within each mission, and those ids are other missions' names: Mortar By
    /// Numbers' four mortars read "Possible Food Crate Location", "Warnet Valley", "Calla Ferns"
    /// and "AFS Hydro-electric Plant", and the first indicator of every mission "Bane Base".
    ///
    /// The rows move up by <see cref="MissionIndicator.UnnamedFrom"/>. For an id from there on None
    /// is sent in place of the id, and the client shows the objective's own name ("Destroy Mortar
    /// #1"). Bootcamp's rows are keyed by the client's ids (430..439) and stay as they are. No
    /// mission_action of the revision refers to an indicator, so only mission_indicator changes.
    /// </summary>
    public static class WildernessUnnamedIndicatorsV1
    {
        public static void Up(MigrationBuilder migration) => migration.Sql(
            $"UPDATE mission_indicator SET indicator_id = indicator_id + {MissionIndicator.UnnamedFrom} " +
            $"WHERE content_revision = '{WildernessMissionDataV1.Revision}' " +
            $"AND indicator_id < {MissionIndicator.UnnamedFrom};");

        public static void Down(MigrationBuilder migration) => migration.Sql(
            $"UPDATE mission_indicator SET indicator_id = indicator_id - {MissionIndicator.UnnamedFrom} " +
            $"WHERE content_revision = '{WildernessMissionDataV1.Revision}' " +
            $"AND indicator_id >= {MissionIndicator.UnnamedFrom};");
    }
}
