using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Rasa.Api.Ingame
{
    using Data;
    using Game;
    using Managers;

    // npcgreetinglanguage lives on the game CLIENT. These endpoints edit the
    // World database's creature -> existing client-line assignment, not its text.
    public sealed record IngameNpcGreetingRow(
        uint CreatureId, string Comment, uint ClassId, string ClassName, uint NameId,
        uint GreetingId, bool Important);

    public sealed class IngameNpcGreetingsEndpoint : IngameCreatureEndpointBase
    {
        public IngameNpcGreetingsEndpoint(IngameSessionService sessions) : base(sessions) { }
        public override string Name => "ingame/npc-greetings";

        protected override ApiResponse HandleAuthenticated(ApiRequest request, Client client)
        {
            request.QueryParameters.TryGetValue("search", out var search);
            search = (search ?? "").Trim();
            using var unitOfWork = Server.GameUnitOfWorkFactory.CreateWorld();
            var greetings = unitOfWork.Creatures.GetNpcGreetings().ToDictionary(g => g.Id);
            var records = new List<IngameNpcGreetingRow>();
            foreach (var creature in unitOfWork.Creatures.Get())
            {
                if (!TryEntityClass(creature.ClassId, out var entityClass) || !IsNpc(entityClass))
                    continue;
                var comment = creature.Comment ?? "";
                var name = entityClass.ClassName ?? "";
                if (search.Length > 0 &&
                    creature.Id.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0 &&
                    comment.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0 &&
                    name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0 &&
                    creature.NameId.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                greetings.TryGetValue(creature.Id, out var greeting);
                records.Add(new IngameNpcGreetingRow(creature.Id, comment, creature.ClassId,
                    name, creature.NameId, greeting?.GreetingId ?? 0, greeting?.Important ?? false));
            }
            records.Sort((a, b) =>
            {
                var byName = string.Compare(a.Comment, b.Comment, StringComparison.OrdinalIgnoreCase);
                return byName != 0 ? byName : a.CreatureId.CompareTo(b.CreatureId);
            });
            return ApiResponse.Ok(ApiJson.Serialize(new { npcs = records, defaultGreetingId = NpcGreetings.Default }));
        }
    }

    /// <summary>Atomic assignment of an existing client greeting to an NPC row.</summary>
    public sealed class IngameNpcGreetingUpdateEndpoint : IngameCreatureEndpointBase
    {
        public IngameNpcGreetingUpdateEndpoint(IngameSessionService sessions) : base(sessions) { }
        public override string Name => "ingame/npc-greetings/update";
        public override string Method => "POST";
        protected override string RequiredCommand => ".greeting";
        public override int MaxBodyBytes => 8192;

        internal static string Validate(uint creatureId, uint greetingId, bool important)
        {
            if (creatureId == 0) return "creatureId must be positive";
            if (greetingId == 0 && important) return "a default greeting cannot be marked Important";
            if (greetingId != 0 && !NpcGreetings.IsLine(greetingId))
                return $"greeting {greetingId} has no client npcgreetinglanguage entry";
            return null;
        }

        protected override ApiResponse HandleAuthenticated(ApiRequest request, Client client)
        {
            if (!request.Headers.TryGetValue("Content-Type", out var contentType) ||
                !contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
                return ApiResponse.Error(415, "content type must be application/json");

            try
            {
                using var document = JsonDocument.Parse(request.Body ?? "");
                var json = document.RootElement;
                if (json.ValueKind != JsonValueKind.Object ||
                    !TryNumber(json, "creatureId", out var creatureId) ||
                    !TryNumber(json, "expectedGreetingId", out var expectedGreetingId) ||
                    !TryBoolean(json, "expectedImportant", out var expectedImportant) ||
                    !TryNumber(json, "greetingId", out var greetingId) ||
                    !TryBoolean(json, "important", out var important))
                    return ApiResponse.Error(400, "creatureId, expectedGreetingId, expectedImportant, greetingId, and important are required with correct types");

                var error = Validate(creatureId, greetingId, important);
                if (error != null) return ApiResponse.Error(400, error);

                using var unitOfWork = Server.GameUnitOfWorkFactory.CreateWorld();
                var creature = unitOfWork.Creatures.Get().FirstOrDefault(row => row.Id == creatureId);
                if (creature == null) return ApiResponse.Error(404, $"unknown creature ID {creatureId}");
                if (!TryEntityClass(creature.ClassId, out var entityClass) || !IsNpc(entityClass))
                    return ApiResponse.Error(400, "selected creature is not an NPC");

                // The repository checks the expected values inside the transaction.
                // A false result is a real stale edit, not a display-metadata mismatch.
                var updated = unitOfWork.Creatures.UpdateNpcGreetingEditor(
                    creatureId, expectedGreetingId, expectedImportant, greetingId, important);
                if (!updated)
                    return ApiResponse.Error(409, "NPC greeting changed since it was loaded; reload the NPC before saving");

                // Loaded templates determine future spawns. NPC objects are also
                // referenced by some already spawned clones; no explicit despawn is required.
                if (CreatureManager.Instance.LoadedCreatures.TryGetValue(creatureId, out var loaded)
                    && loaded.Npc != null)
                {
                    loaded.Npc.GreetingId = greetingId;
                    loaded.Npc.GreetingImportant = greetingId != 0 && important;
                }
                return ApiResponse.Ok(ApiJson.Serialize(new
                {
                    result = "updated", creatureId, greetingId, important = greetingId != 0 && important
                }));
            }
            catch (JsonException e) { return ApiResponse.Error(400, $"invalid JSON: {e.Message}"); }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"NPC greeting save failed: {e}");
                return ApiResponse.Error(500, "NPC greeting could not be saved; check server logs");
            }
        }

        private static bool TryNumber(JsonElement json, string key, out uint value)
        {
            value = 0;
            return json.TryGetProperty(key, out var entry) && entry.ValueKind == JsonValueKind.Number && entry.TryGetUInt32(out value);
        }
        private static bool TryBoolean(JsonElement json, string key, out bool value)
        {
            value = false;
            if (!json.TryGetProperty(key, out var entry) || entry.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return false;
            value = entry.GetBoolean();
            return true;
        }
    }
}
