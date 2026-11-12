using System;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// A Marshes mission's scene binding (mission_scene_binding.bindings) as its writer stored it, and the statements that
    /// change it in place: a later fix to an applied mission changes its binding, not the mission's other rows.
    /// </summary>
    public static class MarshesBindings
    {
        /// <summary>
        /// The bindings JSON <paramref name="write"/> stores for <paramref name="mission"/>: read from the insert it makes
        /// (MarshesMissionDataV1.Enable, MissionContentCodec), so it is the stored text to the byte.
        /// </summary>
        public static string Written(Action<MigrationBuilder> write, uint mission)
        {
            var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            write(builder);
            var sql = builder.Operations.OfType<SqlOperation>().Select(operation => operation.Sql)
                .Single(text => text.StartsWith("INSERT INTO mission_scene_binding", StringComparison.Ordinal) && text.Contains($"SELECT {mission}, "));
            var start = sql.IndexOf("'{", StringComparison.Ordinal);
            var end = sql.IndexOf("}' FROM (SELECT 1 AS one)", StringComparison.Ordinal);
            if (start < 0 || end < start)
                throw new InvalidOperationException($"No bindings literal in the insert of mission {mission}.");
            return sql.Substring(start + 1, end - start).Replace("''", "'");
        }

        public static MissionSceneDefinition Read(string json) => JsonSerializer.Deserialize<MissionSceneDefinition>(json, MissionContentCodec.Options);

        public static string Json(MissionSceneDefinition scene) => JsonSerializer.Serialize(scene, MissionContentCodec.Options);

        /// <summary>The whole binding of <paramref name="mission"/> set to <paramref name="json"/> (the same twice).</summary>
        public static void Write(MigrationBuilder migration, uint mission, string json) =>
            MarshesSql.Update(migration, "mission_scene_binding", mission, MarshesMissionDataV1.Revision, "bindings", json);

        /// <summary>An actor's JSON as MissionContentCodec writes it: its key, role, kind, template and position.</summary>
        public static string Actor(string role, SceneActorKind kind, uint template, ScenePosition at) =>
            $"\"{role}\":{{\"role\":\"{role}\",\"kind\":\"{kind}\",\"templateId\":{template},\"position\":" +
            JsonSerializer.Serialize(at, MissionContentCodec.Options);

        /// <summary>One actor's position rewritten in place in its mission's binding (nothing else of the scene changes).</summary>
        public static string Place(uint mission, string role, SceneActorKind kind, uint template, ScenePosition from, ScenePosition to) =>
            $"UPDATE mission_scene_binding SET bindings = REPLACE(bindings, '{Actor(role, kind, template, from)}', '{Actor(role, kind, template, to)}') " +
            $"WHERE mission_id = {mission} AND content_revision = '{MarshesMissionDataV1.Revision}';";
    }
}
