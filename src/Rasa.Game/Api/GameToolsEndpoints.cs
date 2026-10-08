using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Rasa.Structures.World;

namespace Rasa.Api
{
    public sealed class MonsterFlags
    {
        public MonsterFlags(uint classId, string className, IEnumerable<uint> flags)
        {
            ClassId = classId;
            ClassName = className ?? "";
            Flags = (flags ?? Enumerable.Empty<uint>()).Distinct().OrderBy(flag => flag).ToList();
        }
        public uint ClassId { get; }
        public string ClassName { get; }
        public IReadOnlyList<uint> Flags { get; }
    }

    public sealed class StoreAnswer
    {
        public const int MostKept = 200;
        public List<string> Problems { get; } = new();
        public List<string> Warnings { get; } = new();
        public int FailureStatus { get; set; } = 400;
        public bool Done => Problems.Count == 0;
        public bool Full => Problems.Count >= MostKept;
        public void Remark(string warning)
        {
            if (Warnings.Count < MostKept) Warnings.Add(warning);
        }
    }

    public interface IMonsterFlagStore
    {
        IReadOnlyList<MonsterFlags> Read();
        StoreAnswer Replace(IReadOnlyList<MonsterFlags> classes);
    }

    public interface IWeightedLootStore
    {
        OrdinaryLootContentSnapshot Read();
        /// <summary>Replaces named profiles' groups/items and upserts bindings/policies, preserving unnamed content.</summary>
        StoreAnswer Replace(OrdinaryLootContentSnapshot content);
    }

    public abstract class GameToolsEndpoint : ApiEndpoint
    {
        public const int BodyBytes = 8 * 1024 * 1024;
        public const int ProblemsListed = 20;
        public override bool Sensitive => true;
        protected static ApiResponse NotReady() => ApiResponse.Error(503, "the game server is not ready for this yet");

        protected static JsonDocument Body(ApiRequest request, out ApiResponse refusal)
        {
            refusal = null;
            if (!request.Headers.TryGetValue("Content-Type", out var type)
                || !(type ?? "").TrimStart().StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
            {
                refusal = ApiResponse.Error(415, "content type must be application/json");
                return null;
            }
            try
            {
                var document = JsonDocument.Parse(request.Body ?? "", new JsonDocumentOptions { MaxDepth = 16 });
                if (document.RootElement.ValueKind == JsonValueKind.Object) return document;
                document.Dispose();
                refusal = ApiResponse.Error(400, "the body must be a JSON object");
            }
            catch (JsonException) { refusal = ApiResponse.Error(400, "the body is not JSON"); }
            return null;
        }

        protected static bool Property(JsonElement element, string name, out JsonElement value)
        {
            if (element.ValueKind == JsonValueKind.Object)
                foreach (var property in element.EnumerateObject())
                    if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = property.Value;
                        return true;
                    }
            value = default;
            return false;
        }

        protected static bool Whole(JsonElement element, string name, uint most, out uint value)
        {
            value = 0;
            return Property(element, name, out var property) && property.ValueKind == JsonValueKind.Number
                && property.TryGetUInt32(out value) && value <= most;
        }

        protected static string Text(JsonElement element, string name) =>
            Property(element, name, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString() ?? "" : "";

        protected static ApiResponse Refused(string error, IReadOnlyList<string> problems, int status = 400) =>
            new(status, ServerStatus.Json(writer =>
            {
                writer.WriteString("error", error);
                Strings(writer, "problems", problems);
            }));

        protected static void Strings(Utf8JsonWriter writer, string name, IReadOnlyList<string> lines)
        {
            writer.WriteStartArray(name);
            foreach (var line in lines.Take(ProblemsListed)) writer.WriteStringValue(line);
            if (lines.Count >= StoreAnswer.MostKept)
                writer.WriteStringValue($"and more: looking stopped at {StoreAnswer.MostKept}");
            else if (lines.Count > ProblemsListed)
                writer.WriteStringValue($"and {lines.Count - ProblemsListed} more");
            writer.WriteEndArray();
        }
        protected static bool Enough(List<string> problems) => problems.Count >= StoreAnswer.MostKept;
    }

    public sealed class MonsterFlagsEndpoint : GameToolsEndpoint
    {
        public const string Schema = "rasa.creature_flags/1";
        public IMonsterFlagStore Store { get; set; }
        public override string Name => "monsterflags";
        public override ApiResponse Handle(ApiRequest request)
        {
            if (Store == null) return NotReady();
            var classes = Store.Read();
            return ApiResponse.Ok(ServerStatus.Json(writer =>
            {
                writer.WriteString("schema", Schema);
                writer.WriteString("source", "server");
                writer.WriteStartArray("creatures");
                foreach (var entry in classes)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("class_id", entry.ClassId);
                    writer.WriteString("class_name", entry.ClassName);
                    writer.WriteStartArray("flags");
                    foreach (var flag in entry.Flags) writer.WriteNumberValue(flag);
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }));
        }
    }

    public sealed class UpdateMonsterFlagsEndpoint : GameToolsEndpoint
    {
        public const int MostClasses = 20000;
        public const int MostFlags = 512;
        public IMonsterFlagStore Store { get; set; }
        public override string Name => "updatemonsterflags";
        public override string Method => "POST";
        public override int MaxBodyBytes => BodyBytes;
        public override ApiResponse Handle(ApiRequest request)
        {
            using var document = Body(request, out var refusal);
            if (document == null) return refusal;
            if (!Property(document.RootElement, "creatures", out var list) || list.ValueKind != JsonValueKind.Array)
                return ApiResponse.Error(400, "the body must have a creatures array");
            var problems = new List<string>();
            var classes = new Dictionary<uint, MonsterFlags>();
            var index = 0;
            foreach (var entry in list.EnumerateArray())
            {
                if (Enough(problems)) break;
                var at = $"creatures[{index++}]";
                if (index > MostClasses) { problems.Add($"more than {MostClasses} classes"); break; }
                if (!Whole(entry, "class_id", uint.MaxValue, out var classId) || classId == 0)
                {
                    problems.Add($"{at}: class_id must be a whole number above 0");
                    continue;
                }
                if (!Property(entry, "flags", out var flagList) || flagList.ValueKind != JsonValueKind.Array)
                {
                    problems.Add($"{at} (class {classId}): flags must be an array");
                    continue;
                }
                var flags = new List<uint>();
                foreach (var flag in flagList.EnumerateArray())
                {
                    if (flag.ValueKind != JsonValueKind.Number || !flag.TryGetUInt32(out var id) || id == 0 || flags.Count >= MostFlags)
                    {
                        problems.Add($"{at} (class {classId}): flags must be up to {MostFlags} whole numbers above 0");
                        flags = null;
                        break;
                    }
                    flags.Add(id);
                }
                if (flags == null) continue;
                if (classes.ContainsKey(classId))
                {
                    problems.Add($"{at}: class {classId} is named twice");
                    continue;
                }
                classes[classId] = new MonsterFlags(classId, Text(entry, "class_name"), flags);
            }
            if (problems.Count > 0)
                return Refused("the body is not a list of classes and their flags; nothing was changed", problems);
            if (Store == null) return NotReady();
            var named = classes.Values.OrderBy(entry => entry.ClassId).ToList();
            var answer = Store.Replace(named);
            if (!answer.Done) return Refused("nothing was changed", answer.Problems);
            var rows = named.Sum(entry => entry.Flags.Count);
            Logger.WriteLog(LogType.Command, $"REST API: creature flags of {named.Count} class(es) replaced ({rows} flags) for {request.Remote}.");
            return ApiResponse.Ok(ServerStatus.Json(writer =>
            {
                writer.WriteString("result", "updated");
                writer.WriteNumber("classes", named.Count);
                writer.WriteNumber("flags", rows);
                if (answer.Warnings.Count > 0) Strings(writer, "warnings", answer.Warnings);
            }));
        }
    }
}
