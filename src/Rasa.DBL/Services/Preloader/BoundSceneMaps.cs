using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Two instance missions' scenes bound to the instance they are played in (Bound_scene_maps).
    ///
    /// A scene without a bound map (MissionSceneDefinition.BoundMapContextId) runs on the map the
    /// mission was accepted on, and both of these are taken outside their instance:
    ///
    /// - Mining for Information (1276, Timora Mines): Base Cmdr. Jayjack gives it in the Foreas
    ///   Base command centre on Concordia Divide (1148), so its Eloh hologram was put down on the
    ///   Divide at the Eloh Shelter's coordinates, over a hundred metres from any ground there,
    ///   and never in Timora Mines (1348): the mission could not be done.
    /// - Kill the Light part II (1792, Warnet Caverns): offered by radio wherever Kill the Light is
    ///   rewarded, Captain LaFontaine's at River Base Krimm (1244). Its scene's map (1384) refuses
    ///   world effects off it, so taken anywhere but in the caverns its bio-bomb was never placed.
    ///
    /// Bound, a mission taken elsewhere waits: its scene starts when the player first comes into
    /// the instance, and goes with them to a later copy of it.
    ///
    /// The bound map is added to the stored scene as the last property of its JSON
    /// (mission_scene_binding.bindings, camelCase as MissionContentCodec writes it), only where
    /// the scene has none; Down takes off exactly what Up added. The JSON is joined with ||
    /// on SQLite and CONCAT on MySQL, where || is OR.
    /// </summary>
    public static class BoundSceneMaps
    {
        public const string Key = "\"boundMapContextId\":";

        /// <summary>The scenes bound: (mission, its content revision, the instance's map).</summary>
        public static readonly (uint MissionId, string Revision, uint MapContextId)[] Scenes =
        {
            (Missions.Timora.TimoraMinesMissionsV1.MiningForInformation, Missions.Timora.TimoraMinesMissionsV1.Revision, TimoraMinesContent.Map),
            (WarnetCavernsMissions.KillTheLightII, WarnetCavernsMissions.Revision, WarnetCavernsContent.Map)
        };

        public static IEnumerable<string> Up(bool mySql)
        {
            foreach (var (missionId, revision, map) in Scenes)
            {
                var suffix = $",{Key}{map}}}";
                var head = "SUBSTR(bindings, 1, LENGTH(bindings) - 1)";
                var joined = mySql ? $"CONCAT({head}, '{suffix}')" : $"{head} || '{suffix}'";
                yield return $"UPDATE mission_scene_binding SET bindings = {joined} " +
                    $"WHERE mission_id = {missionId} AND content_revision = '{revision}' " +
                    $"AND bindings LIKE '%}}' AND bindings NOT LIKE '%{Key}%';";
            }
        }

        public static IEnumerable<string> Down()
        {
            foreach (var (missionId, revision, map) in Scenes)
                yield return $"UPDATE mission_scene_binding SET bindings = REPLACE(bindings, ',{Key}{map}}}', '}}') " +
                    $"WHERE mission_id = {missionId} AND content_revision = '{revision}' AND bindings LIKE '%,{Key}{map}}}';";
        }
    }
}
