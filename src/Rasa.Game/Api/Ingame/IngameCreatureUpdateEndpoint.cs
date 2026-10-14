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

        // Keep concurrency tokens scoped to fields the editor actually owns. Display
        // metadata (className, meshId, slot, flags, augmentations, etc.) may be absent
        // from the Rust round-trip or change independently without changing a template.
        private static readonly string[] SnapshotFields =
        {
            "id", "classId", "comment", "faction", "level", "maxHitPoints", "nameId",
            "runSpeed", "walkSpeed", "actions", "randomBodyHue", "bodyHue", "bodyHue2", "appearance"
        };

        internal static bool TryReadExpectedSnapshot(JsonElement json, out IngameCreatureDetails snapshot, out string error)
        {
            snapshot = null;
            error = "expected creature snapshot must be an object";
            if (json.ValueKind != JsonValueKind.Object)
                return false;

            foreach (var field in SnapshotFields)
            {
                if (!json.TryGetProperty(field, out _))
                {
                    error = $"expected.{field} is required; reload this creature before saving";
                    return false;
                }
            }

            if (json.GetProperty("actions").ValueKind != JsonValueKind.Array ||
                json.GetProperty("appearance").ValueKind != JsonValueKind.Array ||
                json.GetProperty("randomBodyHue").ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                error = "expected actions/appearance must be arrays and randomBodyHue must be boolean";
                return false;
            }

            foreach (var entry in json.GetProperty("appearance").EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object ||
                    !entry.TryGetProperty("slotId", out _) ||
                    !entry.TryGetProperty("classId", out _) ||
                    !entry.TryGetProperty("color", out _) ||
                    !entry.TryGetProperty("hue2", out _))
                {
                    error = "expected appearance entries require slotId, classId, color and hue2";
                    return false;
                }
            }

            try
            {
                snapshot = json.Deserialize<IngameCreatureDetails>(JsonOptions);
            }
            catch (JsonException)
            {
                error = "expected creature snapshot has invalid field types; reload before saving";
                return false;
            }

            if (snapshot == null || snapshot.Comment == null || snapshot.Actions?.Count != 8 ||
                snapshot.Appearance == null || snapshot.Appearance.Any(entry => entry == null))
            {
                error = "expected creature snapshot has invalid actions or appearance; reload before saving";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>Names only truly edited fields that differ from the last GET.</summary>
        internal static List<string> ChangedEditableFields(IngameCreatureDetails expected, IngameCreatureDetails current)
        {
            var changed = new List<string>();
            if (expected.Id != current.Id || expected.ClassId != current.ClassId)
                changed.Add("creature identity");
            if (expected.Comment != current.Comment) changed.Add("comment");
            if (expected.Faction != current.Faction) changed.Add("faction");
            if (expected.Level != current.Level) changed.Add("level");
            if (expected.MaxHitPoints != current.MaxHitPoints) changed.Add("maxHitPoints");
            if (expected.NameId != current.NameId) changed.Add("nameId");
            if (expected.RunSpeed != current.RunSpeed) changed.Add("runSpeed");
            if (expected.WalkSpeed != current.WalkSpeed) changed.Add("walkSpeed");
            if (!expected.Actions.SequenceEqual(current.Actions)) changed.Add("actions");

            // Appearance order and descriptive labels are not meaningful, but an
            // added/removed slot or a change in its class/colors is meaningful.
            if (expected.Appearance.Count != current.Appearance.Count ||
                expected.Appearance.Select(a => a.SlotId).Distinct().Count() != expected.Appearance.Count)
                changed.Add("appearance slots");
            foreach (var entry in expected.Appearance)
            {
                var existing = current.Appearance.FirstOrDefault(a => a.SlotId == entry.SlotId);
                if (existing == null || existing.ClassId != entry.ClassId ||
                    existing.Color != entry.Color)
                    changed.Add($"appearance slot {entry.SlotId}");
            }
            return changed;
        }

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
                // Body hues and secondary appearance hues are not persisted by the legacy schema.
                // Reject incompatible old clients instead of silently reporting a successful save.
                if (!input.RandomBodyHue || input.BodyHue.HasValue || input.BodyHue2.HasValue)
                    return ApiResponse.Error(400, "Manual body hues cannot be saved without additional database columns; use server spawn coloring");

                if (input.Appearance.Count > 64)
                    return ApiResponse.Error(400, "a creature may have at most 64 appearance entries");
                var slots = new HashSet<uint>();
                foreach (var appearance in input.Appearance)
                {
                    if (appearance == null)
                        return ApiResponse.Error(400, "appearance entry cannot be null");
                    if (appearance.Hue2 != CurrentAppearanceHue2)
                        return ApiResponse.Error(400, "Secondary appearance hue cannot be saved; only appearance color is stored");
                    if (!slots.Add(appearance.SlotId))
                        return ApiResponse.Error(400, $"duplicate appearance slot {appearance.SlotId}");
                }

                // The client round-trips the editable values, not every display-only
                // field on GET (for example appearance.meshId). Comparing raw JSON
                // would reject an untouched creature whenever those fields differ.
                if (!TryReadExpectedSnapshot(expected, out var expectedDetails, out var expectedError))
                    return ApiResponse.Error(400, expectedError);

                lock (Editing)
                {
                    using var uow = Server.GameUnitOfWorkFactory.CreateWorld();
                    var originalRow = uow.Creatures.Get().FirstOrDefault(row => row.Id == input.Id);
                    if (originalRow == null)
                        return ApiResponse.Error(404, $"unknown creature id {input.Id}");
                    TryEntityClass(originalRow.ClassId, out var entityClass);
                    var original = ToDetails(originalRow, entityClass, uow.Creatures.GetCreatureAppearances(input.Id));
                    var changedFields = ChangedEditableFields(expectedDetails, original);
                    if (changedFields.Count != 0)
                        return ApiResponse.Error(409,
                            $"creature changed since it was loaded ({string.Join(", ", changedFields)}); reload before saving");
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
                        Color = a.Color
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
                        Action8 = input.Actions[7]
                    };
                    try
                    {
                        uow.Creatures.UpdateCreatureEditor(updated, rows);
                    }
                    catch (Exception error)
                    {
                        Logger.WriteLog(LogType.Error, $"Creature editor save {input.Id} failed: {error}");
                        if (error is InvalidOperationException &&
                            error.Message.Contains("database verification failed", StringComparison.Ordinal))
                            return ApiResponse.Error(500, error.Message);
                        return ApiResponse.Error(500, "creature database save failed; check server log (including schema compatibility)");
                    }

                    // Return a fresh DB read, not an echo of the user's payload.
                    // Repository verification already checks the persisted values *before*
                    // committing the transaction; this read also keeps the wire contract
                    // honest and supplies an authoritative new concurrency snapshot.
                    IngameCreatureDetails persistedDetails;
                    try
                    {
                        using var verified = Server.GameUnitOfWorkFactory.CreateWorld();
                        var persistedRow = verified.Creatures.Get().FirstOrDefault(row => row.Id == input.Id);
                        if (persistedRow == null)
                            throw new InvalidOperationException($"Creature {input.Id} disappeared after save");
                        persistedDetails = ToDetails(persistedRow, entityClass,
                            verified.Creatures.GetCreatureAppearances(input.Id));
                    }
                    catch (Exception error)
                    {
                        Logger.WriteLog(LogType.Error, $"Creature editor post-save read {input.Id} failed: {error}");
                        return ApiResponse.Error(500, "creature was written but could not be verified by reloading; check server log");
                    }

                    CreatureManager.Instance.PublishCreatureEditor(updated, rows, actions);
                    Logger.WriteLog(LogType.Command, $"Creature editor saved creature {input.Id} ({updated.ClassId})");
                    return ApiResponse.Ok(ApiJson.Serialize(new
                    {
                        result = "updated",
                        creature = persistedDetails,
                        note = "Committed and reloaded from World DB; future spawns use these settings. Existing actors are not reset."
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
