using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text.Json;
using Rasa.Repositories.World;
using Rasa.Structures.World;

namespace Rasa.Api
{
    public sealed class WeightedLootEndpoint : GameToolsEndpoint
    {
        public const string Format = "rasa-weighted-loot-profiles";
        public const int Version = 2;
        internal static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            MaxDepth = 16
        };
        public IWeightedLootStore Store { get; set; }
        public override string Name => "lootpools";
        public override ApiResponse Handle(ApiRequest request)
        {
            if (Store == null) return NotReady();
            try
            {
                var content = Store.Read();
                return ApiResponse.Ok(JsonSerializer.Serialize(new
                {
                    format = Format, version = Version, source = "server",
                    profiles = content.Profiles, groups = content.Groups, items = content.Items,
                    bindings = content.Bindings, mapPolicies = content.MapPolicies
                }, JsonOptions));
            }
            catch (LegacyFlatLootArchiveException error)
            {
                Logger.WriteLog(LogType.Error, $"Weighted loot GET blocked: {error.Message}");
                return Refused("Inactive flat archives block weighted loot readiness; nothing was activated.",
                    new[] { error.Message }, 503);
            }
            catch (DbException error)
            {
                Logger.WriteLog(LogType.Error, $"Weighted loot GET failed: {error}");
                return ApiResponse.Error(500, "The World database could not read weighted loot; check the server log and retry.");
            }
        }
    }

    public sealed class UpdateWeightedLootEndpoint : GameToolsEndpoint
    {
        private static readonly (string Collection, (string Field, TypeCode Type)[] Fields)[] RowContract =
        {
            ("profiles", new[] { ("profileKey", TypeCode.String), ("description", TypeCode.String),
                ("contentRevision", TypeCode.String), ("provenance", TypeCode.String), ("evidenceNotes", TypeCode.String) }),
            ("groups", new[] { ("profileKey", TypeCode.String), ("groupKey", TypeCode.String),
                ("kind", TypeCode.Int32), ("chanceBasisPoints", TypeCode.Int32), ("minimumRolls", TypeCode.Int32),
                ("maximumRolls", TypeCode.Int32), ("sortOrder", TypeCode.Int32) }),
            ("items", new[] { ("profileKey", TypeCode.String), ("groupKey", TypeCode.String),
                ("itemTemplateId", TypeCode.UInt32), ("weight", TypeCode.Int32), ("minimumQuantity", TypeCode.Int32),
                ("maximumQuantity", TypeCode.Int32), ("sortOrder", TypeCode.Int32) }),
            ("bindings", new[] { ("mapContextId", TypeCode.UInt32), ("creatureId", TypeCode.UInt32), ("profileKey", TypeCode.String) }),
            ("mapPolicies", new[] { ("mapContextId", TypeCode.UInt32), ("requireProfiles", TypeCode.Boolean) })
        };

        public IWeightedLootStore Store { get; set; }
        public override string Name => "updatelootpools";
        public override string Method => "POST";
        public override int MaxBodyBytes => BodyBytes;
        public override ApiResponse Handle(ApiRequest request)
        {
            using var document = Body(request, out var refusal);
            if (document == null) return refusal;
            var root = document.RootElement;
            if (Text(root, "format") == "rasa-loot-tables" || Property(root, "assignments", out _) ||
                HasFlatFields(root))
                return ApiResponse.Error(400, "Legacy rasa-loot-tables v1 flat percent projects are not weighted content. " +
                    "Export and review the old configuration, then explicitly author weighted profiles, group admission in basis points, " +
                    "candidate weights and map/creature bindings. No automatic percent-to-weight conversion or archive write is supported.");
            if (Text(root, "format") != WeightedLootEndpoint.Format ||
                !Whole(root, "version", uint.MaxValue, out var version) || version != WeightedLootEndpoint.Version)
                return ApiResponse.Error(400, $"Expected format {WeightedLootEndpoint.Format}, version {WeightedLootEndpoint.Version}; load a current weighted project.");
            foreach (var name in new[] { "profiles", "groups", "items", "bindings", "mapPolicies" })
                if (!Property(root, name, out var rows) || rows.ValueKind != JsonValueKind.Array)
                    return ApiResponse.Error(400, $"{name} must be an array; include all five canonical collections, even when empty.");
            var problems = RowProblems(root);
            if (problems.Count != 0)
                return Refused("The weighted project contains missing fields or a wrong field type; nothing was changed. " +
                    "Use GET to retain existing metadata; omission is not an instruction to erase it.", problems);
            if (Store == null) return NotReady();
            WeightedDocument edit;
            try
            {
                edit = root.Deserialize<WeightedDocument>(WeightedLootEndpoint.JsonOptions);
            }
            catch (JsonException error)
            {
                return Refused("The weighted project contains a wrong field type; nothing was changed.",
                    new[] { $"{error.Path}: keys and metadata must be strings, IDs and bounds integers, and requireProfiles a boolean." });
            }
            var content = new OrdinaryLootContentSnapshot(edit.Profiles, edit.Groups, edit.Items, edit.Bindings, edit.MapPolicies);
            var answer = Store.Replace(content);
            if (!answer.Done) return Refused("Weighted loot was not saved; nothing was changed.", answer.Problems, answer.FailureStatus);
            Logger.WriteLog(LogType.Command,
                $"REST API: weighted loot edit committed for {request.Remote}: {content.Profiles.Count} profiles, " +
                $"{content.Groups.Count} groups, {content.Items.Count} candidates, {content.Bindings.Count} bindings.");
            return ApiResponse.Ok(ServerStatus.Json(writer =>
            {
                writer.WriteString("result", "updated");
                writer.WriteNumber("profiles", content.Profiles.Count);
                writer.WriteNumber("groups", content.Groups.Count);
                writer.WriteNumber("items", content.Items.Count);
                writer.WriteNumber("bindings", content.Bindings.Count);
                writer.WriteNumber("mapPolicies", content.MapPolicies.Count);
            }));
        }

        private static List<string> RowProblems(JsonElement root)
        {
            var problems = new List<string>();
            DuplicateProperties(root, "$", problems);
            if (problems.Count != 0) return problems;
            foreach (var (collection, fields) in RowContract)
            {
                Property(root, collection, out var rows);
                var index = 0;
                foreach (var row in rows.EnumerateArray())
                {
                    var at = $"{collection}[{index++}]";
                    if (row.ValueKind != JsonValueKind.Object)
                        problems.Add($"{at}: must be an object with all documented fields.");
                    else
                    {
                        DuplicateProperties(row, at, problems);
                        if (Enough(problems)) return problems;
                        foreach (var (field, type) in fields)
                        {
                            var present = Property(row, field, out var value);
                            var valid = present && (type switch
                            {
                                TypeCode.String => value.ValueKind == JsonValueKind.String,
                                TypeCode.Int32 => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out _),
                                TypeCode.UInt32 => value.ValueKind == JsonValueKind.Number && value.TryGetUInt32(out _),
                                TypeCode.Boolean => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
                                _ => false
                            });
                            if (!valid)
                            {
                                var expected = type == TypeCode.String ? "a string" : type == TypeCode.Boolean ?
                                    "a boolean" : type == TypeCode.UInt32 ? "an unsigned 32-bit integer" : "a signed 32-bit integer";
                                problems.Add($"{at}.{field}: must explicitly be {expected}; missing and null are not defaults.");
                            }
                            if (Enough(problems)) return problems;
                        }
                    }
                    if (Enough(problems)) return problems;
                }
            }
            return problems;
        }

        private static void DuplicateProperties(JsonElement value, string at, List<string> problems)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in value.EnumerateObject())
            {
                if (!names.Add(property.Name))
                    problems.Add($"{at}.{property.Name}: property must occur once (case-insensitive); duplicate values are ambiguous.");
                if (Enough(problems)) return;
            }
        }

        private static bool HasFlatFields(JsonElement root)
        {
            foreach (var name in new[] { "groups", "items" })
                if (Property(root, name, out var rows) && rows.ValueKind == JsonValueKind.Array)
                    foreach (var row in rows.EnumerateArray())
                        if (Property(row, "chance", out _) || Property(row, "id", out _) || Property(row, "items", out _))
                            return true;
            return false;
        }

        private sealed class WeightedDocument
        {
            public List<LootProfileEntry> Profiles { get; set; }
            public List<OrdinaryLootGroupEntry> Groups { get; set; }
            public List<LootItemEntry> Items { get; set; }
            public List<CreatureLootBindingEntry> Bindings { get; set; }
            public List<LootMapPolicyEntry> MapPolicies { get; set; }
        }
    }
}
