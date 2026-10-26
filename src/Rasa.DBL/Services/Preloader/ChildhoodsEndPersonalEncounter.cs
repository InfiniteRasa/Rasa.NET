using System.Collections.Generic;
using System.Text.Json;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Wilderness;

namespace Rasa.Services.Preloader
{
    public static class ChildhoodsEndPersonalEncounter
    {
        private static readonly WildernessDaghdasUrnBindings Bindings = WildernessDaghdasUrnV1.AdoptedBindings();
        private static readonly MissionSceneDefinition Original = WildernessDaghdasUrnV1.RangerEncounter(Bindings.XanxPositions, Bindings.XanxHeadings);
        private static readonly SceneActorDefinition Personal = Original.Actors["anjuhi"] with
        {
            Kind = SceneActorKind.Creature, TemplateId = 98,
            Position = new ScenePosition(-474.76953f, 193.64453f, 519.91016f), Orientation = 0.45,
            OwnerOnly = true, ReplacesPublicSpawnId = 176, ReplaceWhilePresent = true
        };
        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
        private static string Json<T>(T value) => JsonSerializer.Serialize(value, MissionContentCodec.Options);
        private static string Actor(bool personal)
        {
            var actors = new Dictionary<string, SceneActorDefinition>(Original.Actors);
            if (personal)
            {
                actors["anjuhi"] = Personal;
                foreach (var role in new[] { "xanx-1", "xanx-2", "xanx-3" })
                    actors[role] = actors[role] with { OwnerOnly = true };
            }
            return "\"actors\":" + Json(actors);
        }
        private static string Encounter => "\"publicEncounter\":" + Json(Original.PublicEncounter);
        private static string Deaths(bool personal)
        {
            var deaths = new Dictionary<string, uint>(Original.DefeatSequences);
            if (personal) deaths.Add("anjuhi", 14);
            return "\"defeatSequences\":" + Json(deaths);
        }
        private static string Sequences(bool personal)
        {
            var sequences = new Dictionary<uint, SceneSequenceDefinition>(Original.Sequences);
            if (personal) sequences.Add(14, new SceneSequenceDefinition());
            return "\"sequences\":" + Json(sequences);
        }
        public static IEnumerable<string> Up()
        {
            yield return $"UPDATE mission_scene_binding SET bindings=REPLACE(REPLACE(REPLACE(REPLACE(bindings,{S(Actor(false))},{S(Actor(true))}),{S(Encounter)},'\"publicEncounter\":null,\"mapContextId\":1220'),{S(Deaths(false))},{S(Deaths(true))}),{S(Sequences(false))},{S(Sequences(true))}) " +
                $"WHERE mission_id=682 AND script_key='wilderness.daghdas-rangers' AND INSTR(bindings,{S(Actor(false))})>0 AND INSTR(bindings,{S(Encounter)})>0 AND INSTR(bindings,{S(Deaths(false))})>0 AND INSTR(bindings,{S(Sequences(false))})>0 AND INSTR(bindings,'\"mapContextId\"')=0;";
        }
        public static IEnumerable<string> Down()
        {
            yield return $"UPDATE mission_scene_binding SET bindings=REPLACE(REPLACE(REPLACE(REPLACE(bindings,{S(Actor(true))},{S(Actor(false))}),'\"publicEncounter\":null,\"mapContextId\":1220',{S(Encounter)}),{S(Deaths(true))},{S(Deaths(false))}),{S(Sequences(true))},{S(Sequences(false))}) " +
                $"WHERE mission_id=682 AND script_key='wilderness.daghdas-rangers' AND INSTR(bindings,{S(Actor(true))})>0 AND INSTR(bindings,'\"publicEncounter\":null,\"mapContextId\":1220')>0 AND INSTR(bindings,{S(Deaths(true))})>0 AND INSTR(bindings,{S(Sequences(true))})>0;";
        }
    }
}
