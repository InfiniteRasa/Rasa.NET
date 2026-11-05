using System.Collections.Generic;
using System.Text.Json;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Eight scene creatures of four Marshes missions stood off the server navmesh (MissionGroundTests: more than 1.5 m
    /// sideways or 1.1 m up or down from the nearest walkable point; 2026-11-04, on origin/development 23351216). Each
    /// moves to that nearest navmesh point, or to a point within 0.5 m of it, where the client files also have a floor
    /// (MarshesClientFloorTests' check: a walkable surface at most 0.8 m under it, none from 0.6 to 0.9 m over it). The places
    /// are ESTIMATES from the shipped navmesh and the client files (MapGeometry), not from the client's own data:
    ///  - 548's block C prisoners (Logos Research Facility) stood where the client has no floor (to-fix 24's reported
    ///    pair): 3 m along the cell row to the navmesh, 0.3-0.4 m over the client floor there.
    ///  - 664's wave-3-4 (Bane Supply Depot) stood 2 m over the client floor: down to the navmesh, 0.66 m over it.
    ///  - 891's wave-2-3, 3-3, 3-4 and 3-6 (Village Ruins) stood 1.6-4 m over the client floor: onto the navmesh (wave-2-3
    ///    0.1 m under its nearest point, which is 0.83 m over the floor). wave-2-3 and 3-4 now stand within 0.5 m of
    ///    wave-1-4 (56.4, 57.97, -207.2), in later waves.
    ///  - 920's ward-3 (Marshes) stood on a client floor but 1.8 m off the navmesh: 1.8 m north, on both.
    /// Each actor's position is rewritten in place in its scene's bindings (REPLACE of its JSON), so nothing else of the
    /// scene changes; down writes the old position back.
    /// </summary>
    public static class MarshesOnTheNavmeshV1
    {
        public static readonly (uint Mission, string Role, uint Template, ScenePosition Before, ScenePosition Now)[] Moves =
        {
            (MarshesFreedomsJustAnotherWordV1.MissionId, "prisoner-c-1", 760369, new(712f, 289.17f, -275f), new(715.07f, 289.13f, -274.95f)),
            (MarshesFreedomsJustAnotherWordV1.MissionId, "prisoner-c-2", 760369, new(744f, 289.17f, -275f), new(741.07f, 289.13f, -274.95f)),
            (MarshesSurvivorsV2.MissionId, "wave-3-4", 531045, new(158f, 134.02f, 290f), new(158f, 132.7f, 290f)),
            (MarshesNeverSurrenderV1.MissionId, "wave-2-3", 531045, new(56.4f, 57.97f, -205.2f), new(56.4f, 57.87f, -207.25f)),
            (MarshesNeverSurrenderV1.MissionId, "wave-3-3", 531045, new(138f, 62.69f, -206.8f), new(138.37f, 61.57f, -206.92f)),
            (MarshesNeverSurrenderV1.MissionId, "wave-3-4", 531045, new(58.4f, 57.97f, -207.2f), new(56.51f, 57.65f, -207.67f)),
            (MarshesNeverSurrenderV1.MissionId, "wave-3-6", 531046, new(56.4f, 57.97f, -209.2f), new(56.4f, 56.44f, -209.2f)),
            (MarshesHeadEastV2.MissionId, "ward-3", 760359, new(95f, 216.88f, 531f), new(95f, 216.88f, 529.2f))
        };

        public static IEnumerable<string> Up()
        {
            foreach (var move in Moves)
                yield return Place(move.Mission, move.Role, move.Template, move.Before, move.Now);
        }

        public static IEnumerable<string> Down()
        {
            foreach (var move in Moves)
                yield return Place(move.Mission, move.Role, move.Template, move.Now, move.Before);
        }

        /// <summary>The actor's JSON as MissionContentCodec writes it: its key, role, kind, template and position.</summary>
        public static string Actor(string role, uint template, ScenePosition at) =>
            $"\"{role}\":{{\"role\":\"{role}\",\"kind\":\"Creature\",\"templateId\":{template},\"position\":" +
            JsonSerializer.Serialize(at, MissionContentCodec.Options);

        private static string Place(uint mission, string role, uint template, ScenePosition from, ScenePosition to) =>
            $"UPDATE mission_scene_binding SET bindings = REPLACE(bindings, '{Actor(role, template, from)}', '{Actor(role, template, to)}') " +
            $"WHERE mission_id = {mission} AND content_revision = '{MarshesMissionDataV1.Revision}';";
    }
}
