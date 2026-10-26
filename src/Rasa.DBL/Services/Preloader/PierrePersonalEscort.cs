using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Wilderness;

namespace Rasa.Services.Preloader
{
    public static class PierrePersonalEscort
    {
        private static readonly MissionSceneDefinition Original = WildernessLandingZoneV1.EscapeVelocityScene(held: true);
        private static readonly SceneActorDefinition Personal = Original.Actors["pierre"] with
        {
            Kind = SceneActorKind.Creature, Position = new ScenePosition(-279, 170.1f, 87),
            Orientation = -1.42855361, OwnerOnly = true, ReplacesPublicSpawnId = 630070,
            HoldCombatUntilRoute = true
        };
        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
        private static string Json<T>(T value) => JsonSerializer.Serialize(value, MissionContentCodec.Options);
        private static string Actor(bool personal) => Json(personal ? Personal : Original.Actors["pierre"]);
        private static string Encounter => "\"publicEncounter\":" + Json(Original.PublicEncounter);
        private static string Routes(bool personal)
        {
            var routes = new Dictionary<string, SceneRoute>(Original.Routes);
            if (personal)
            {
                // Stay west of the control-point orb. The mesh includes the stairs and terrain,
                // but not this runtime object's collision volume.
                var points = routes["landing-zone"].Points.ToList();
                points.InsertRange(points.Count - 2, new[]
                {
                    new SceneWaypoint(new ScenePosition(187, 163.5f, -40)),
                    new SceneWaypoint(new ScenePosition(187, 163.5f, -54)),
                    new SceneWaypoint(new ScenePosition(187, 163.5f, -64))
                });
                routes["landing-zone"] = routes["landing-zone"] with { Points = points, Speed = 6 };
                routes["boarding"] = routes["boarding"] with { Speed = 6 };
            }
            return "\"routes\":" + Json(routes);
        }
        private static string Sequences(bool personal)
        {
            var sequences = new Dictionary<uint, SceneSequenceDefinition>(Original.Sequences);
            if (personal) sequences.Add(3, new SceneSequenceDefinition());
            return "\"sequences\":" + Json(sequences) + (personal ? ",\"defeatSequences\":{\"pierre\":3}" : "");
        }
        public static IEnumerable<string> Up()
        {
            yield return $"UPDATE mission_scene_binding SET bindings=REPLACE(REPLACE(REPLACE(REPLACE(bindings,{S(Actor(false))},{S(Actor(true))}),{S(Encounter)},'\"publicEncounter\":null'),{S(Routes(false))},{S(Routes(true))}),{S(Sequences(false))},{S(Sequences(true))}) " +
                $"WHERE mission_id=666 AND script_key='wilderness.escape-velocity' AND INSTR(bindings,{S(Actor(false))})>0 AND INSTR(bindings,{S(Encounter)})>0 AND INSTR(bindings,{S(Routes(false))})>0 AND INSTR(bindings,{S(Sequences(false))})>0 AND INSTR(bindings,'\"defeatSequences\"')=0;";
        }
        public static IEnumerable<string> Down()
        {
            yield return $"UPDATE mission_scene_binding SET bindings=REPLACE(REPLACE(REPLACE(REPLACE(bindings,{S(Actor(true))},{S(Actor(false))}),'\"publicEncounter\":null',{S(Encounter)}),{S(Routes(true))},{S(Routes(false))}),{S(Sequences(true))},{S(Sequences(false))}) " +
                $"WHERE mission_id=666 AND script_key='wilderness.escape-velocity' AND INSTR(bindings,{S(Actor(true))})>0 AND INSTR(bindings,{S(Routes(true))})>0 AND INSTR(bindings,{S(Sequences(true))})>0 AND INSTR(bindings,'\"publicEncounter\":null')>0;";
        }
    }
}
