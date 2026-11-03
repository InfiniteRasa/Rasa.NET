using System.Collections.Generic;
using System.Text.Json;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions;
using Rasa.Services.Preloader.Missions.Wilderness;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Thrax Overseer Skeev in the Wilderness (spawn 630130) fights and dies as any mini-boss
    /// until a player's Orders from High Command run holds him (Skeev_free_when_unreserved).
    ///
    /// Orders from High Command (701) binds him as its public encounter with ManualCombat, so
    /// his finale fight waits for the ultimatum. The gate went on him at every spawn, held or
    /// not: to everyone not on the mission he was a hostile Bane mini-boss who never fought and
    /// took no damage, and respawning him changed nothing. The binding is now
    /// FreeWhenUnreserved: he is held only from the moment a run reserves him (accepting the
    /// mission) until the run lets him go, and the reservation is refused while he is in a
    /// fight. Everything else about the finale is as it was.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike: the binding's public encounter changes only
    /// while it is the one Orders_from_high_command wrote, and Down puts that back.
    /// </summary>
    public static class SkeevFreeWhenUnreserved
    {
        public const uint MissionId = 701;
        public const string ScriptKey = "wilderness.corman-finale";

        /// <summary>The public encounter as WildernessDaghdasUrnV1.Finale binds it.</summary>
        public static PublicEncounterBinding Before() =>
            new(MissionId, WildernessDaghdasUrnV1.SkeevSpawnId, "skeev", ScriptKey, OwnerLossPolicy: "Wait", ManualCombat: true);

        public static PublicEncounterBinding After() => Before() with { FreeWhenUnreserved = true };

        public static IEnumerable<string> Up()
        {
            yield return Swap(Before(), After());
        }

        public static IEnumerable<string> Down()
        {
            yield return Swap(After(), Before());
        }

        private static string Swap(PublicEncounterBinding from, PublicEncounterBinding to)
        {
            var was = InstanceMissionSql.S(Json(from));
            return $"UPDATE mission_scene_binding SET bindings = REPLACE(bindings, {was}, {InstanceMissionSql.S(Json(to))}) " +
                $"WHERE mission_id = {MissionId} AND script_key = '{ScriptKey}' AND INSTR(bindings, {was}) > 0;";
        }

        private static string Json(PublicEncounterBinding binding) =>
            "\"publicEncounter\":" + JsonSerializer.Serialize(binding, MissionContentCodec.Options);
    }
}
