using System.Collections.Generic;
using System.Text.Json;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// These Old Bones (1976): Elder Hathen's walk back from the Mausoleum of the Beholden (temple 1) and the Shrine of the
    /// Penitent (temple 2) on the Velon Hollow navmesh upstream ships since f887edda (the escort routes reconnected).
    ///
    /// PlateauTheseOldBonesV1.WayBackAt took each end from a path on our local Velon Hollow navmesh, which is no longer
    /// used. On the upstream mesh two ends are off it: temple 1's (-11.6, 64.7, -486) is 0.78 m to the side of its nearest
    /// walkable point, temple 2's (-21.2, 80.5, -283.2) 0.55 m over it. SceneRouteController.Start asks that a leg's path
    /// end within 0.5 m of its waypoint, so the walk back from either temple, when it is the last one secured, was refused
    /// and Hathen stood until the walk-back timeout took him. Temple 3's end is on the mesh and does not change.
    ///
    /// The new ends are corners of the upstream mesh's path from the temple towards the hospital, about 100 m along it as
    /// before (104 m and 100 m), each on a client collision floor (0.15 m and 0.13 m over it; the old temple 1 end had no
    /// client floor under it, the old temple 2 end stood 0.49 m over it).
    ///
    /// Data only, plain SQL valid on SQLite and MySQL: each route's waypoint is changed in the stored scene only where it is
    /// as V1 wrote it, and Down puts back only what Up changed.
    /// </summary>
    public static class PlateauTheseOldBonesWayBackV2
    {
        public const uint MissionId = PlateauTheseOldBonesV1.MissionId;

        /// <summary>The new ends of the walk back from temples 1-3 (temple 3's is V1's).</summary>
        public static readonly ScenePosition[] WayBackAt =
        {
            new(-11.4f, 63.5f, -486.6f), new(-20.9f, 79.95f, -283f), PlateauTheseOldBonesV1.WayBackAt[2]
        };

        /// <summary>The temples whose end moves.</summary>
        public static readonly uint[] Moved = { 1, 2 };

        public static IEnumerable<string> Up()
        {
            foreach (var temple in Moved)
                yield return Move(temple, PlateauTheseOldBonesV1.WayBackAt[temple - 1], WayBackAt[temple - 1]);
        }

        public static IEnumerable<string> Down()
        {
            foreach (var temple in Moved)
                yield return Move(temple, WayBackAt[temple - 1], PlateauTheseOldBonesV1.WayBackAt[temple - 1]);
        }

        /// <summary>The route's waypoint as the scene codec writes it into mission_scene_binding.bindings.</summary>
        public static string RouteJson(uint temple, ScenePosition at) =>
            $"\"key\":\"{Rasa.Missions.Content.Plateau.PlateauTheseOldBonesScene.WayBack(temple)}\",\"points\":[{{\"position\":" +
            JsonSerializer.Serialize(at, MissionContentCodec.Options);

        private static string Move(uint temple, ScenePosition from, ScenePosition to)
        {
            var before = RouteJson(temple, from);
            var after = RouteJson(temple, to);
            return $"UPDATE mission_scene_binding SET bindings = REPLACE(bindings, '{before}', '{after}') " +
                $"WHERE mission_id = {MissionId} AND content_revision = '{PlateauMissionDataV1.Revision}' " +
                $"AND INSTR(bindings, '{before}') > 0;";
        }
    }
}
