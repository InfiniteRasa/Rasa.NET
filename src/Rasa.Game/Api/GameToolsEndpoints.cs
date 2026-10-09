using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Rasa.Api
{
    using Managers;

    /// <summary>A creature class and the flags it has (creature_class_flag).</summary>
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

    /// <summary>
    /// What came of asking a store to change something: done, with whatever is worth a remark,
    /// or refused, with what is wrong. A refusal changes nothing at all.
    /// </summary>
    public sealed class StoreAnswer
    {
        /// <summary>
        /// The most problems, or remarks, that are kept. A body can be megabytes of what is
        /// wrong; past this many, checking stops, and the answer lists the first few anyway.
        /// </summary>
        public const int MostKept = 200;

        public List<string> Problems { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();

        public bool Done => Problems.Count == 0;

        /// <summary>Whether there are problems enough that looking for more tells nobody anything.</summary>
        public bool Full => Problems.Count >= MostKept;

        /// <summary>A remark, unless there are <see cref="MostKept"/> already.</summary>
        public void Remark(string warning)
        {
            if (Warnings.Count < MostKept)
                Warnings.Add(warning);
        }
    }

    /// <summary>The creature classes' flags, as gametools' endpoints read and write them. Server gives the endpoints the real one (Managers.MonsterFlagStore).</summary>
    public interface IMonsterFlagStore
    {
        /// <summary>Every creature class with the flags it has, by class id.</summary>
        IReadOnlyList<MonsterFlags> Read();

        /// <summary>Gives each of these classes these flags in place of the ones it has. A class not named keeps its own.</summary>
        StoreAnswer Replace(IReadOnlyList<MonsterFlags> classes);
    }

    /// <summary>The loot pools, as gametools' endpoints read and write them. Server gives the endpoints the real one (Managers.LootPoolStore).</summary>
    public interface ILootPoolStore
    {
        LootPoolSet Read();

        /// <summary>Puts this set in place of every pool and every assignment there is.</summary>
        StoreAnswer Replace(LootPoolSet set);
    }

    /// <summary>
    /// What gametools' four endpoints share. They are the server's side of the editors in the
    /// repository's gametools folder - the Monster Flag Editor and the Loot Table Editor - and
    /// each speaks the editor's own file: what GET gives is a file the editor loads, and a file
    /// the editor saves is a body POST takes, with whatever else is in it passed over.
    ///
    /// All four are <see cref="ApiEndpoint.Sensitive"/>: off until ApiConfig.Rest.Endpoints
    /// turns each on, and never public because the API is. The editors are pages opened from
    /// disk, so the server's keeper also lists "null" in ApiConfig.Rest.AllowedOrigins, and a
    /// browser will send them a key only for an endpoint that wants one.
    /// </summary>
    public abstract class GameToolsEndpoint : ApiEndpoint
    {
        /// <summary>The most a body of the editors' may come to: a loot project of some tens of thousands of rows.</summary>
        public const int BodyBytes = 8 * 1024 * 1024;

        /// <summary>How many problems an answer lists before it says only how many more there are.</summary>
        public const int ProblemsListed = 20;

        public override bool Sensitive => true;

        protected static ApiResponse NotReady() => ApiResponse.Error(503, "the game server is not ready for this yet");

        /// <summary>The body as JSON, which it has to be sent as; null and the answer that says why not.</summary>
        protected static JsonDocument Body(ApiRequest request, out ApiResponse refusal)
        {
            refusal = null;

            // Not a form and not text, as for /addaccount: a page has to ask before it sends
            // this, and is told yes only if its origin is allowed.
            if (!request.Headers.TryGetValue("Content-Type", out var type)
                || !(type ?? "").TrimStart().StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
            {
                refusal = ApiResponse.Error(415, "content type must be application/json");
                return null;
            }

            try
            {
                var document = JsonDocument.Parse(request.Body ?? "", new JsonDocumentOptions { MaxDepth = 16 });

                if (document.RootElement.ValueKind == JsonValueKind.Object)
                    return document;

                document.Dispose();
                refusal = ApiResponse.Error(400, "the body must be a JSON object");
            }
            catch (JsonException)
            {
                refusal = ApiResponse.Error(400, "the body is not JSON");
            }

            return null;
        }

        /// <summary>A property by its name in any case; false if the object has none.</summary>
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

        /// <summary>A whole number from 0 to <paramref name="most"/> out of a property; false if it is missing or is not one.</summary>
        protected static bool Whole(JsonElement element, string name, uint most, out uint value)
        {
            value = 0;

            return Property(element, name, out var property) && property.ValueKind == JsonValueKind.Number
                && property.TryGetUInt32(out value) && value <= most;
        }

        protected static string Text(JsonElement element, string name) =>
            Property(element, name, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString() ?? "" : "";

        /// <summary>400 with what is wrong: {"error":"...","problems":["...", ...]}, the first <see cref="ProblemsListed"/> of them.</summary>
        protected static ApiResponse Refused(string error, IReadOnlyList<string> problems) =>
            new ApiResponse(400, ServerStatus.Json(writer =>
            {
                writer.WriteString("error", error);
                Strings(writer, "problems", problems);
            }));

        /// <summary>
        /// A list of text, cut off after <see cref="ProblemsListed"/> with a last line saying
        /// how many more - or, of a list that stopped at <see cref="StoreAnswer.MostKept"/>,
        /// that there are more.
        /// </summary>
        protected static void Strings(Utf8JsonWriter writer, string name, IReadOnlyList<string> lines)
        {
            writer.WriteStartArray(name);

            foreach (var line in lines.Take(ProblemsListed))
                writer.WriteStringValue(line);

            if (lines.Count >= StoreAnswer.MostKept)
                writer.WriteStringValue($"and more: looking stopped at {StoreAnswer.MostKept}");
            else if (lines.Count > ProblemsListed)
                writer.WriteStringValue($"and {lines.Count - ProblemsListed} more");

            writer.WriteEndArray();
        }

        /// <summary>Whether a list of problems is as long as one is kept.</summary>
        protected static bool Enough(List<string> problems) => problems.Count >= StoreAnswer.MostKept;
    }

    /// <summary>
    /// GET /monsterflags: every creature class and the flags it has, in the Monster Flag
    /// Editor's file:
    ///   {"schema":"rasa.creature_flags/1","source":"server",
    ///    "creatures":[{"class_id":6032,"class_name":"Bane_Amoeboid_v1","flags":[5,71]}, ...]}
    /// A class with no flags is listed with none. 503 until the server has its store.
    /// </summary>
    public sealed class MonsterFlagsEndpoint : GameToolsEndpoint
    {
        public const string Schema = "rasa.creature_flags/1";

        public IMonsterFlagStore Store { get; set; }

        public override string Name => "monsterflags";

        public override ApiResponse Handle(ApiRequest request)
        {
            var store = Store;

            if (store == null)
                return NotReady();

            var classes = store.Read();

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

                    foreach (var flag in entry.Flags)
                        writer.WriteNumberValue(flag);

                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            }));
        }
    }

    /// <summary>
    /// POST /updatemonsterflags: gives creature classes their flags. The body is the Monster
    /// Flag Editor's file, or as much of it as matters:
    ///   {"creatures":[{"class_id":6032,"flags":[5,71,142]}, ...]}
    /// Each class named has its flags replaced by the ones given - none, for an empty list - in
    /// the world database and in the running server, where every creature of the class has
    /// them from then on; a client is told a creature's flags when the creature is shown to it,
    /// so one already in sight is seen with the new ones the next time it is. A class not named
    /// is not touched.
    ///   200 {"result":"updated","classes":2,"flags":7}
    ///   400 {"error":"...","problems":[...]}   the body is not that, or names a class that is
    ///                                          not a creature's or a flag there is not;
    ///                                          nothing was changed
    ///   415                                    the body was not sent as application/json
    ///   503                                    the server has not its store yet
    /// </summary>
    public sealed class UpdateMonsterFlagsEndpoint : GameToolsEndpoint
    {
        /// <summary>The most classes one request may name, and the most flags a class may be given.</summary>
        public const int MostClasses = 20000;
        public const int MostFlags = 512;

        public IMonsterFlagStore Store { get; set; }

        public override string Name => "updatemonsterflags";
        public override string Method => "POST";
        public override int MaxBodyBytes => BodyBytes;

        public override ApiResponse Handle(ApiRequest request)
        {
            using var document = Body(request, out var refusal);

            if (document == null)
                return refusal;

            if (!Property(document.RootElement, "creatures", out var list) || list.ValueKind != JsonValueKind.Array)
                return ApiResponse.Error(400, "the body must have a creatures array");

            var problems = new List<string>();
            var classes = new Dictionary<uint, MonsterFlags>();
            var index = 0;

            foreach (var entry in list.EnumerateArray())
            {
                if (Enough(problems))
                    break;

                var at = $"creatures[{index++}]";

                if (index > MostClasses)
                {
                    problems.Add($"more than {MostClasses} classes");
                    break;
                }

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

                if (flags == null)
                    continue;

                if (classes.ContainsKey(classId))
                {
                    problems.Add($"{at}: class {classId} is named twice");
                    continue;
                }

                classes[classId] = new MonsterFlags(classId, Text(entry, "class_name"), flags);
            }

            if (problems.Count > 0)
                return Refused("the body is not a list of classes and their flags; nothing was changed", problems);

            var store = Store;

            if (store == null)
                return NotReady();

            var named = classes.Values.OrderBy(entry => entry.ClassId).ToList();
            var answer = store.Replace(named);

            if (!answer.Done)
                return Refused("nothing was changed", answer.Problems);

            var rows = named.Sum(entry => entry.Flags.Count);

            Logger.WriteLog(LogType.Command, $"REST API: creature flags of {named.Count} class(es) replaced ({rows} flags) for {request.Remote}.");

            return ApiResponse.Ok(ServerStatus.Json(writer =>
            {
                writer.WriteString("result", "updated");
                writer.WriteNumber("classes", named.Count);
                writer.WriteNumber("flags", rows);

                if (answer.Warnings.Count > 0)
                    Strings(writer, "warnings", answer.Warnings);
            }));
        }
    }

    /// <summary>
    /// GET /lootpools: every loot pool and which creature rows have which, in the Loot Table
    /// Editor's project file:
    ///   {"format":"rasa-loot-tables","version":1,"source":"server",
    ///    "groups":[{"id":1,"name":"Thrax junk","note":"",
    ///               "items":[{"itemTemplateId":28,"chance":5,"minQuantity":1,"maxQuantity":3}]}],
    ///    "assignments":[{"creatureId":25580,"groupId":1}]}
    /// chance is a percent. 503 until the server has its store.
    /// </summary>
    public sealed class LootPoolsEndpoint : GameToolsEndpoint
    {
        public const string Format = "rasa-loot-tables";

        public ILootPoolStore Store { get; set; }

        public override string Name => "lootpools";

        public override ApiResponse Handle(ApiRequest request)
        {
            var store = Store;

            if (store == null)
                return NotReady();

            var set = store.Read();

            return ApiResponse.Ok(ServerStatus.Json(writer =>
            {
                writer.WriteString("format", Format);
                writer.WriteNumber("version", 1);
                writer.WriteString("source", "server");
                writer.WriteStartArray("groups");

                foreach (var pool in set.Pools)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("id", pool.Id);
                    writer.WriteString("name", pool.Name);
                    writer.WriteString("note", pool.Note);
                    writer.WriteStartArray("items");

                    foreach (var item in pool.Items)
                    {
                        writer.WriteStartObject();
                        writer.WriteNumber("itemTemplateId", item.TemplateId);
                        writer.WriteNumber("chance", item.Chance);
                        writer.WriteNumber("minQuantity", item.Minimum);
                        writer.WriteNumber("maxQuantity", item.Maximum);
                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteStartArray("assignments");

                foreach (var (creatureId, poolId) in set.Assignments)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("creatureId", creatureId);
                    writer.WriteNumber("groupId", poolId);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            }));
        }
    }

    /// <summary>
    /// POST /updatelootpools: the loot pools, all of them. The body is the Loot Table Editor's
    /// project file (<see cref="LootPoolsEndpoint"/>), and it takes the place of every pool and
    /// every assignment there is - in the world database, in one transaction, and in the running
    /// server, where the next kill rolls it. An empty file leaves no pools.
    ///   200 {"result":"updated","groups":3,"items":41,"assignments":12,"warnings":[...]}
    ///   400 {"error":"...","problems":[...]}   the body is not that, or names an item or a
    ///                                          creature the server has not got; nothing was
    ///                                          changed
    ///   415                                    the body was not sent as application/json
    ///   503                                    the server has not its store yet
    /// A chance is a percent from 0 to 100, kept to four decimal places; quantities are from 1
    /// to 100000 with the maximum no less than the minimum.
    /// </summary>
    public sealed class UpdateLootPoolsEndpoint : GameToolsEndpoint
    {
        public const uint QuantityLimit = 100000;
        public const int NameLength = 100;
        public const int NoteLength = 200;

        public ILootPoolStore Store { get; set; }

        public override string Name => "updatelootpools";
        public override string Method => "POST";
        public override int MaxBodyBytes => BodyBytes;

        public override ApiResponse Handle(ApiRequest request)
        {
            using var document = Body(request, out var refusal);

            if (document == null)
                return refusal;

            var root = document.RootElement;

            if (!Property(root, "groups", out var groups) || groups.ValueKind != JsonValueKind.Array)
                return ApiResponse.Error(400, "the body must have a groups array");

            var problems = new List<string>();
            var pools = new Dictionary<uint, LootPool>();
            var index = 0;

            foreach (var group in groups.EnumerateArray())
            {
                if (Enough(problems))
                    break;

                var at = $"groups[{index++}]";

                if (!Whole(group, "id", uint.MaxValue, out var id) || id == 0)
                {
                    problems.Add($"{at}: id must be a whole number above 0");
                    continue;
                }

                if (pools.ContainsKey(id))
                {
                    problems.Add($"{at}: group {id} is there twice");
                    continue;
                }

                var name = Text(group, "name").Trim();
                var note = (Property(group, "note", out _) ? Text(group, "note") : Text(group, "comment")).Trim();

                if (name.Length == 0 || name.Length > NameLength)
                    problems.Add($"{at} (group {id}): name must be 1 to {NameLength} letters");

                if (note.Length > NoteLength)
                    problems.Add($"{at} (group {id}): note must be no more than {NoteLength} letters");

                var items = new List<LootPoolItem>();
                var seen = new HashSet<uint>();

                if (Property(group, "items", out var itemList))
                {
                    if (itemList.ValueKind != JsonValueKind.Array)
                    {
                        problems.Add($"{at} (group {id}): items must be an array");
                    }
                    else
                    {
                        var row = 0;

                        foreach (var item in itemList.EnumerateArray())
                        {
                            if (Enough(problems))
                                break;

                            var where = $"{at} (group {id}) items[{row++}]";

                            if (!Whole(item, "itemTemplateId", uint.MaxValue, out var template) || template == 0)
                            {
                                problems.Add($"{where}: itemTemplateId must be a whole number above 0");
                                continue;
                            }

                            if (!seen.Add(template))
                            {
                                problems.Add($"{where}: item {template} is in the group twice");
                                continue;
                            }

                            if (!Property(item, "chance", out var chanceValue) || chanceValue.ValueKind != JsonValueKind.Number
                                || !chanceValue.TryGetDouble(out var chance) || double.IsNaN(chance) || chance < 0 || chance > 100)
                            {
                                problems.Add($"{where} (item {template}): chance must be a percent from 0 to 100");
                                continue;
                            }

                            uint minimum = 1, maximum = 1;

                            if ((Property(item, "minQuantity", out _) && (!Whole(item, "minQuantity", QuantityLimit, out minimum) || minimum == 0))
                                || (Property(item, "maxQuantity", out _) && !Whole(item, "maxQuantity", QuantityLimit, out maximum)))
                            {
                                problems.Add($"{where} (item {template}): minQuantity and maxQuantity must be whole numbers from 1 to {QuantityLimit}");
                                continue;
                            }

                            if (!Property(item, "maxQuantity", out _))
                                maximum = minimum;

                            if (maximum < minimum)
                            {
                                problems.Add($"{where} (item {template}): maxQuantity is less than minQuantity");
                                continue;
                            }

                            items.Add(new LootPoolItem(template, LootPools.TidyChance(chance), minimum, maximum));
                        }
                    }
                }

                pools[id] = new LootPool(id, name, note, items);
            }

            var assignments = new List<(uint, uint)>();

            if (Property(root, "assignments", out var links))
            {
                if (links.ValueKind != JsonValueKind.Array)
                {
                    problems.Add("assignments must be an array");
                }
                else
                {
                    var row = 0;

                    foreach (var link in links.EnumerateArray())
                    {
                        if (Enough(problems))
                            break;

                        var at = $"assignments[{row++}]";

                        if (!Whole(link, "creatureId", uint.MaxValue, out var creature) || creature == 0
                            || !Whole(link, "groupId", uint.MaxValue, out var pool) || pool == 0)
                        {
                            problems.Add($"{at}: creatureId and groupId must be whole numbers above 0");
                            continue;
                        }

                        if (!pools.ContainsKey(pool))
                        {
                            problems.Add($"{at}: creature {creature} is given group {pool}, which is not in the file");
                            continue;
                        }

                        assignments.Add((creature, pool));
                    }
                }
            }

            if (problems.Count > 0)
                return Refused("the body is not a loot project; nothing was changed", problems);

            var store = Store;

            if (store == null)
                return NotReady();

            var set = new LootPoolSet(pools.Values, assignments);
            var answer = store.Replace(set);

            if (!answer.Done)
                return Refused("nothing was changed", answer.Problems);

            Logger.WriteLog(LogType.Command,
                $"REST API: loot pools replaced for {request.Remote}: {set.Pools.Count} pool(s), {set.ItemCount} item(s), {set.Assignments.Count} assignment(s) on {set.CreatureCount} creature(s).");

            return ApiResponse.Ok(ServerStatus.Json(writer =>
            {
                writer.WriteString("result", "updated");
                writer.WriteNumber("groups", set.Pools.Count);
                writer.WriteNumber("items", set.ItemCount);
                writer.WriteNumber("assignments", set.Assignments.Count);

                if (answer.Warnings.Count > 0)
                    Strings(writer, "warnings", answer.Warnings);
            }));
        }
    }
}
