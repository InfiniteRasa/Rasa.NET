using System.Collections.Generic;
using Rasa.Structures.World;
using Rasa.Structures;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    /// <summary>
    /// Lurking In The Shadows (427), objective 6, "Kill Proctor Fulgor": a map pin and radar
    /// arrow on Fulgor (Wilderness_fulgor_indicator). The objective had none, so the map and radar
    /// showed nothing to go to. A player's account of the live mission (Zerias's blog, "Tabula
    /// Rasa mission management") has a yellow arrow coming up on the radar once Oliver has sent
    /// them after him.
    ///
    /// The pin stands on Fulgor's spawn (spawnpool 580010, creature 76), read from the table when
    /// the migration runs, with a 20 m radius: a boss that holds his ground around his pillbox.
    /// Where the live game put Fulgor is not known (WildernessAliaBranchesV1's evidence for 427), so
    /// the pin follows ours. It has no client name: indicator ids from MissionIndicator.UnnamedFrom
    /// up go out as None, and the client names the pin by its objective, "Kill Proctor Fulgor."
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Up puts the row in only where the objective
    /// exists, the spawn is there and the mission has no such row yet; Down takes out only this row.
    /// </summary>
    public static class WildernessFulgorIndicator
    {
        public const uint MissionId = 427;
        public const uint ObjectiveId = 6;
        public const uint IndicatorId = MissionIndicator.UnnamedFrom + ObjectiveId;
        public const uint FulgorSpawn = 580010;
        public const double Radius = 20;
        public const string Comment = "Proctor Fulgor at his spawn";

        public static IEnumerable<string> Up()
        {
            var revision = WildernessMissionDataV1.Revision;

            yield return "INSERT INTO mission_indicator (mission_id, content_revision, objective_id, indicator_id, requirement, " +
                "pos_x, pos_y, pos_z, radius, show_3d_effect, comment) " +
                $"SELECT {MissionId}, '{revision}', {ObjectiveId}, {IndicatorId}, {(byte)MissionContentRequirement.Required}, " +
                $"s.pos_x, s.pos_y, s.pos_z, {Radius}, 0, '{Comment}' FROM spawnpool s " +
                $"WHERE s.id = {FulgorSpawn} " +
                $"AND EXISTS (SELECT 1 FROM mission_objective_definition o WHERE o.mission_id = {MissionId} " +
                $"AND o.content_revision = '{revision}' AND o.objective_id = {ObjectiveId}) " +
                $"AND NOT EXISTS (SELECT 1 FROM mission_indicator i WHERE i.mission_id = {MissionId} " +
                $"AND i.content_revision = '{revision}' AND i.objective_id = {ObjectiveId});";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM mission_indicator WHERE mission_id = {MissionId} " +
                $"AND content_revision = '{WildernessMissionDataV1.Revision}' AND objective_id = {ObjectiveId} " +
                $"AND indicator_id = {IndicatorId};";
        }
    }
}
