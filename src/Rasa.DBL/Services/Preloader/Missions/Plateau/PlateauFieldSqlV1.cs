using System.Globalization;
using System.Text.Json;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Plain SQL for a mission's sealed force fields (world_destructible, sealed = 1) and the TakeDownField actions that
    /// take them down (mission_action kind 14), for migrations with no target model of their own (as
    /// PlateauGiantKillerSquadV1's): EF's InsertData needs one. The same text on SQLite and MySQL.
    /// </summary>
    public static class PlateauFieldSqlV1
    {
        /// <summary>A sealed force field of <paramref name="classId"/>, Bane side, at a gate's origin and yaw.</summary>
        public static string Field(uint id, uint map, uint classId, double x, double y, double z, double yaw, string comment) =>
            "INSERT INTO world_destructible (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, hit_points, creature_id, " +
            "creature_count, side, comment, sealed) VALUES (" +
            string.Join(", ", id, map, classId, D(x), D(y), D(z), D(yaw), 0, 0, 0, WorldDestructibleEntry.SideBane, Quote(comment), 1) + ");";

        public static string RemoveField(uint id, uint classId) =>
            $"DELETE FROM world_destructible WHERE id = {id} AND class_id = {classId};";

        /// <summary>
        /// Action <paramref name="actionId"/> of <paramref name="objective"/>'s transition <paramref name="transition"/>: the field
        /// of row <paramref name="rowId"/> goes down in the player's copy.
        /// </summary>
        public static string TakeDown(uint missionId, uint objective, uint transition, uint actionId, uint rowId, string operationKey, string comment)
        {
            var intent = JsonSerializer.Serialize<CharacterIntent>(new TakeDownFieldIntent(operationKey, rowId), MissionContentCodec.Options);
            // MySQL reads a backslash in a string literal as an escape, and Quote only doubles apostrophes: keep both out.
            if (intent.Contains('\\') || intent.Contains('\''))
                throw new System.ArgumentException($"TakeDownField intent JSON with a backslash or an apostrophe: {intent}", nameof(operationKey));
            return "INSERT INTO mission_action (mission_id, content_revision, requirement, objective_id, transition_id, action_id, kind, " +
                   "sequence, item_intent, comment) VALUES (" +
                   string.Join(", ", missionId, Quote(PlateauMissionDataV1.Revision), (byte)MissionContentRequirement.Required, objective,
                       transition, actionId, (byte)MissionActionKind.TakeDownField, actionId, Quote(intent), Quote(comment)) + ");";
        }

        public static string RemoveTakeDown(uint missionId, uint objective, uint transition, uint actionId) =>
            $"DELETE FROM mission_action WHERE mission_id = {missionId} AND content_revision = '{PlateauMissionDataV1.Revision}' " +
            $"AND objective_id = {objective} AND transition_id = {transition} AND action_id = {actionId} " +
            $"AND kind = {(byte)MissionActionKind.TakeDownField};";

        private static string D(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string Quote(string text) => "'" + text.Replace("'", "''") + "'";
    }
}
