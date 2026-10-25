using System.Collections.Generic;
using System.Text.Json;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Wilderness;
namespace Rasa.Services.Preloader
{
    /// <summary>Each Too Close For Comfort assignment owns its ranger instead of reserving a shared spawn.</summary>
    public static class SolisPersonalEscort
    {
        private static string S(string value) => "'"+value.Replace("'","''")+"'";
        private static readonly MissionSceneDefinition Original=WildernessAliaOpeningV1.SolisEscort();
        private static readonly SceneActorDefinition PublicRanger=Original.Actors["ranger"];
        private static readonly SceneActorDefinition PersonalRanger=PublicRanger with
        {
            Kind=SceneActorKind.Creature,TemplateId=WildernessOpeningWorldV1.RangerCreatureId,
            Position=new ScenePosition(806,301.80417f,499),Orientation=3.11
        };
        private static string Actor(bool personal)=>JsonSerializer.Serialize(personal?PersonalRanger:PublicRanger,MissionContentCodec.Options);
        private static string Encounter=>"\"publicEncounter\":"+JsonSerializer.Serialize(Original.PublicEncounter,MissionContentCodec.Options);
        private static string Sequences(bool personal)
        {
            var sequences=new Dictionary<uint,SceneSequenceDefinition>(Original.Sequences);
            if(personal)sequences.Add(2,new SceneSequenceDefinition());
            return "\"sequences\":"+JsonSerializer.Serialize(sequences,MissionContentCodec.Options)+
                (personal?",\"defeatSequences\":{\"ranger\":2}":"");
        }
        public static IEnumerable<string> Up()
        {
            yield return $"UPDATE mission_scene_binding SET bindings=REPLACE(REPLACE(REPLACE(bindings,{S(Actor(false))},{S(Actor(true))}),{S(Encounter)},'\"publicEncounter\":null'),{S(Sequences(false))},{S(Sequences(true))}) " +
                $"WHERE mission_id=1407 AND script_key='wilderness.solis-escort' AND INSTR(bindings,{S(Actor(false))})>0 AND INSTR(bindings,{S(Encounter)})>0 AND INSTR(bindings,{S(Sequences(false))})>0 AND INSTR(bindings,'\"defeatSequences\"')=0;";
            yield return "UPDATE spawnpool SET mode=2,creature_1_min_count=0,creature_1_max_count=0 WHERE id=630001 AND map_context_id=1220 AND creature_1_Id=630001 AND mode=0 AND creature_1_min_count=1 AND creature_1_max_count=1 " +
                $"AND EXISTS (SELECT 1 FROM mission_scene_binding WHERE mission_id=1407 AND script_key='wilderness.solis-escort' AND INSTR(bindings,{S(Actor(true))})>0 AND INSTR(bindings,'\"publicEncounter\":null')>0);";
        }
        public static IEnumerable<string> Down()
        {
            yield return $"UPDATE mission_scene_binding SET bindings=REPLACE(REPLACE(REPLACE(bindings,{S(Actor(true))},{S(Actor(false))}),'\"publicEncounter\":null',{S(Encounter)}),{S(Sequences(true))},{S(Sequences(false))}) " +
                $"WHERE mission_id=1407 AND script_key='wilderness.solis-escort' AND INSTR(bindings,{S(Actor(true))})>0 AND INSTR(bindings,'\"publicEncounter\":null')>0 AND INSTR(bindings,{S(Sequences(true))})>0;";
            yield return "UPDATE spawnpool SET mode=0,creature_1_min_count=1,creature_1_max_count=1 WHERE id=630001 AND map_context_id=1220 AND creature_1_Id=630001 AND mode=2 AND creature_1_min_count=0 AND creature_1_max_count=0;";
        }
    }
}
