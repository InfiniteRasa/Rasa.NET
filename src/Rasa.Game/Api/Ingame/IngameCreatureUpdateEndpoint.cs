using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Rasa.Api.Ingame
{
    using Data;
    using Game;
    using Managers;
    using Structures.World;

    /// <summary>
    /// POST /ingame/creatures/update. The current GET details are an optimistic concurrency
    /// token. Changes are committed together; only future creature spawns use the new template.
    /// </summary>
    public sealed class IngameCreatureUpdateEndpoint : IngameCreatureEndpointBase
    {
        private static readonly object Editing = new();
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public IngameCreatureUpdateEndpoint(IngameSessionService sessions) : base(sessions) { }
        public override string Name => "ingame/creatures/update";
        public override string Method => "POST";
        protected override string RequiredCommand => ".giveitem";
        public override int MaxBodyBytes => 256 * 1024;

        protected override ApiResponse HandleAuthenticated(ApiRequest request, Client client)
        {
            if (!request.Headers.TryGetValue("Content-Type", out var contentType) ||
                !contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
                return ApiResponse.Error(415, "content type must be application/json");

            try
            {
                using var document = JsonDocument.Parse(request.Body ?? "");
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object ||
                    !root.TryGetProperty("creature", out var creatureJson) || creatureJson.ValueKind != JsonValueKind.Object ||
                    !root.TryGetProperty("expected", out var expected) || expected.ValueKind != JsonValueKind.Object)
                    return ApiResponse.Error(400, "creature and expected GET snapshot objects are required");

                foreach (var field in new[] { "id", "classId", "comment", "faction", "level", "maxHitPoints",
                    "nameId", "runSpeed", "walkSpeed", "actions", "randomBodyHue", "bodyHue", "bodyHue2", "appearance" })
                    if (!creatureJson.TryGetProperty(field, out _))
                        return ApiResponse.Error(400, $"creature.{field} is required");

                if (creatureJson.GetProperty("appearance").ValueKind != JsonValueKind.Array ||
                    creatureJson.GetProperty("actions").ValueKind != JsonValueKind.Array ||
                    creatureJson.GetProperty("randomBodyHue").ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    return ApiResponse.Error(400, "appearance and actions must be arrays and randomBodyHue must be boolean");
                foreach (var item in creatureJson.GetProperty("appearance").EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object ||
                        !item.TryGetProperty("slotId", out _) ||
                        !item.TryGetProperty("classId", out _) ||
                        !item.TryGetProperty("color", out _) ||
                        !item.TryGetProperty("hue2", out _))
                        return ApiResponse.Error(400, "each appearance entry requires slotId, classId, color and hue2");
                }

                var input = creatureJson.Deserialize<IngameCreatureDetails>(JsonOptions);
                if (input == null || input.Id == 0)
                    return ApiResponse.Error(400, "a valid creature ID is required");
                if (input.Comment == null || input.Comment.Length > 50)
                    return ApiResponse.Error(400, "creature comment must be at most 50 characters");
                if (input.Faction > 6)
                    return ApiResponse.Error(400, "faction/target category must be 0..6");
                if (input.Actions == null || input.Actions.Count != 8)
                    return ApiResponse.Error(400, "exactly 8 action IDs are required (0 means unused)");
                if (input.Appearance == null)
                    return ApiResponse.Error(400, "appearance must be an array");
                if (input.RandomBodyHue)
                {
                    if (input.BodyHue.HasValue || input.BodyHue2.HasValue)
                        return ApiResponse.Error(400, "random spawn colors require null bodyHue and bodyHue2");
                }
                else if (!input.BodyHue.HasValue || !input.BodyHue2.HasValue)
                    return ApiResponse.Error(400, "both body hues must be supplied for manual colors");

                if (input.Appearance.Count > 64)
                    return ApiResponse.Error(400, "a creature may have at most 64 appearance entries");
                var slots = new HashSet<uint>();
                foreach (var appearance in input.Appearance)
                {
                    if (appearance == null)
                        return ApiResponse.Error(400, "appearance entry cannot be null");
                    if (!slots.Add(appearance.SlotId))
                        return ApiResponse.Error(400, $"duplicate appearance slot {appearance.SlotId}");
                }

                lock (Editing)
                {
                    using var uow = Server.GameUnitOfWorkFactory.CreateWorld();
                    var originalRow = uow.Creatures.Get().FirstOrDefault(row => row.Id == input.Id);
                    if (originalRow == null)
                        return ApiResponse.Error(404, $"unknown creature id {input.Id}");
                    TryEntityClass(originalRow.ClassId, out var entityClass);
                    var original = ToDetails(originalRow, entityClass, uow.Creatures.GetCreatureAppearances(input.Id));
                    using var current = JsonDocument.Parse(ApiJson.Serialize(original));
                    if (!JsonElement.DeepEquals(expected, current.RootElement))
                        return ApiResponse.Error(409, "creature changed since it was loaded; reload before saving");
                    if (input.ClassId != original.ClassId)
                        return ApiResponse.Error(400, "entity class identity is read-only");

                    // Keep legacy native rows intact when their slots/classes are absent from
                    // our current enums/catalog; only newly introduced values require validation.
                    foreach (var entry in input.Appearance)
                    {
                        var prior = original.Appearance.FirstOrDefault(x => x.SlotId == entry.SlotId);
                        if (prior == null && !Enum.IsDefined(typeof(EquipmentData), entry.SlotId))
                            return ApiResponse.Error(400, $"unknown equipment slot {entry.SlotId}");
                        if ((prior == null || prior.ClassId != entry.ClassId) &&
                            (entry.ClassId == 0 || !TryEntityClass(entry.ClassId, out _)))
                            return ApiResponse.Error(400,
                                $"appearance slot {entry.SlotId} has unknown entity class {entry.ClassId}");
                    }

                    // Reject bogus action IDs instead of allowing the next CreatureInit to crash.
                    var actions = uow.Creatures.GetCreatureActions();
                    foreach (var actionId in input.Actions)
                        if (actionId != 0 && !actions.ContainsKey(actionId))
                            return ApiResponse.Error(400, $"unknown creature_action row ID {actionId}");

                    var rows = input.Appearance.Select(a => new CreatureAppearanceEntry
                    {
                        Id = input.Id,
                        SlotId = a.SlotId,
                        ClassId = a.ClassId,
                        Color = a.Color,
                        Hue2 = a.Hue2
                    }).ToList();
                    var updated = new CreatureEntry
                    {
                        Id = input.Id,
                        ClassId = original.ClassId,
                        Comment = input.Comment,
                        Faction = input.Faction,
                        Level = input.Level,
                        MaxHitPoints = input.MaxHitPoints,
                        NameId = input.NameId,
                        RunSpeed = input.RunSpeed,
                        WalkSpeed = input.WalkSpeed,
                        Action1 = input.Actions[0],
                        Action2 = input.Actions[1],
                        Action3 = input.Actions[2],
                        Action4 = input.Actions[3],
                        Action5 = input.Actions[4],
                        Action6 = input.Actions[5],
                        Action7 = input.Actions[6],
                        Action8 = input.Actions[7],
                        BodyHue = input.RandomBodyHue ? null : input.BodyHue,
                        BodyHue2 = input.RandomBodyHue ? null : input.BodyHue2
                    };
                    try
                    {
                        uow.Creatures.UpdateCreatureEditor(updated, rows);
                    }
                    catch (Exception error)
                    {
                        Logger.WriteLog(LogType.Error, $"Creature editor save {input.Id} failed: {error}");
                        return ApiResponse.Error(500, "creature database save failed; check server log");
                    }

                    CreatureManager.Instance.PublishCreatureEditor(updated, rows, actions);
                    Logger.WriteLog(LogType.Command, $"Creature editor saved creature {input.Id} ({updated.ClassId})");
                    return ApiResponse.Ok(ApiJson.Serialize(new
                    {
                        result = "updated",
                        creature = ToDetails(updated, entityClass, rows),
                        note = "Saved; future spawns use these settings. Existing actors are not reset."
                    }));
                }
            }
            catch (JsonException)
            {
                return ApiResponse.Error(400, "invalid creature JSON payload");
            }
        }
    }
}
