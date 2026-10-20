using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace Rasa.Api
{
    using Data;
    using Managers;
    using Structures.Char;

    /// <summary>A petition as the REST API gives it: every column, with the names its ids stand for.</summary>
    public sealed class PetitionRecord
    {
        public PetitionRecord(PetitionEntry entry, string accountName, string familyName, string characterName)
        {
            Entry = entry;
            AccountName = accountName;
            FamilyName = familyName;
            CharacterName = characterName;
        }

        public PetitionEntry Entry { get; }

        /// <summary>The account's login name; null for an account that is no longer there.</summary>
        public string AccountName { get; }

        public string FamilyName { get; }

        /// <summary>The character's name; null for a character since deleted.</summary>
        public string CharacterName { get; }
    }

    /// <summary>
    /// GET /getopenpetitions and GET /getallpetitions: the petitions players have filed (Help,
    /// Report Bug, /petition and /bug), newest first - the open ones, or all of them whatever
    /// their status. The body:
    ///
    ///   {"count":1,"petitions":[{"id":12,"type":"helprequest","status":"open","accountId":3,
    ///   "accountName":"atomsk","familyName":"Hart","characterId":40,"characterName":"Ellie",
    ///   "summary":"...","body":"...","mapContextId":1220,"position":{"x":1.5,"y":2,"z":-3},
    ///   "resolution":"","noticePending":false,"createdAt":"2026-10-14T09:30:00Z"}]}
    ///
    /// They name accounts and carry what players wrote, so both are
    /// <see cref="ApiEndpoint.Sensitive"/>: off until their own entry in ApiConfig.Rest.Endpoints
    /// turns them on, and public only by a Public of their own. Read from the character
    /// database on the listener's thread (<see cref="Read"/>, set by Server).
    /// </summary>
    public sealed class PetitionsEndpoint : ApiEndpoint
    {
        private readonly bool _openOnly;

        public PetitionsEndpoint(bool openOnly) => _openOnly = openOnly;

        /// <summary>The petitions of a status, or of every status for null. Unset until the server has started.</summary>
        public Func<PetitionStatus?, IReadOnlyList<PetitionRecord>> Read { get; set; }

        public override string Name => _openOnly ? "getopenpetitions" : "getallpetitions";

        public override bool Sensitive => true;

        public override ApiResponse Handle(ApiRequest request)
        {
            var read = Read;

            if (read == null)
                return ApiResponse.Error(503, "the game server is not ready");

            return ApiResponse.Ok(Json(read(_openOnly ? PetitionStatus.Open : null)));
        }

        public static string Json(IReadOnlyList<PetitionRecord> petitions) =>
            ServerStatus.Json(writer =>
            {
                writer.WriteNumber("count", petitions.Count);
                writer.WriteStartArray("petitions");

                foreach (var record in petitions)
                {
                    var petition = record.Entry;

                    writer.WriteStartObject();
                    writer.WriteNumber("id", petition.Id);
                    writer.WriteString("type", Named((PetitionType)petition.Type));
                    writer.WriteString("status", Named((PetitionStatus)petition.Status));
                    writer.WriteNumber("accountId", petition.AccountId);
                    writer.WriteString("accountName", record.AccountName);
                    writer.WriteString("familyName", record.FamilyName);
                    writer.WriteNumber("characterId", petition.CharacterId);
                    writer.WriteString("characterName", record.CharacterName);
                    writer.WriteString("summary", petition.Summary ?? "");
                    writer.WriteString("body", petition.Body ?? "");
                    writer.WriteNumber("mapContextId", petition.MapContextId);
                    writer.WriteStartObject("position");
                    writer.WriteNumber("x", petition.PosX);
                    writer.WriteNumber("y", petition.PosY);
                    writer.WriteNumber("z", petition.PosZ);
                    writer.WriteEndObject();
                    writer.WriteString("resolution", petition.Resolution ?? "");
                    writer.WriteBoolean("noticePending", petition.NoticePending);
                    writer.WriteString("createdAt", DateTime.SpecifyKind(petition.CreatedAt, DateTimeKind.Utc)
                        .ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            });

        /// <summary>A type or a status by its name, in lower case ("helprequest", "open"); its number for one with none.</summary>
        private static string Named<T>(T value) where T : struct, Enum =>
            Enum.IsDefined(value) ? value.ToString().ToLowerInvariant() : Convert.ToUInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>What /respondpetition or /closepetition is to do, and who asked.</summary>
    public sealed class PetitionOrder
    {
        public uint PetitionId { get; set; }

        /// <summary>The admin who asked, as the caller names them.</summary>
        public string Admin { get; set; }

        /// <summary>The answer, for /respondpetition; null for /closepetition.</summary>
        public string Response { get; set; }

        /// <summary>The address the request came from.</summary>
        public string From { get; set; }
    }

    /// <summary>
    /// POST /respondpetition and POST /closepetition: a game master's answer to a petition, and
    /// its close, as the player who filed it sees them in game. The body is JSON:
    ///
    ///   {"petitionId":12,"admin":"Atomsk","response":"The quest is fixed; try it again."}
    ///   {"petitionId":12,"adminId":"Atomsk"}
    ///
    /// petitionId a number, or a number in a string. /respondpetition takes admin, the one
    /// answering, and response, up to <see cref="PetitionManager.MaxResponseLength"/>
    /// characters: it is written as the petition's resolution, in place of any before it, and
    /// the petition stays open. /closepetition takes adminId, the one closing it (a name or a
    /// number), and marks it resolved, keeping its answer. Only an open petition is answered or
    /// closed. The player is told in their chat at once when they are in the world, and when
    /// they next arrive in it otherwise (PetitionManager.Tell).
    ///
    /// Every answer of its own is 200: {"responded":true,"petitionId":12,"status":"open",
    /// "playerTold":true}, playerTold false for a player who will be told when they arrive, or
    /// {"responded":false,"error":"..."} ("closed" for /closepetition). The admin goes in the
    /// server log's line for it, with the address, and the request in the API log (api_log).
    ///
    /// Done on the world loop, which the clients belong to (<see cref="Perform"/>, set by
    /// Server); this thread waits up to <see cref="WaitMs"/>. It changes something, so it is
    /// <see cref="ApiEndpoint.Sensitive"/>.
    /// </summary>
    public sealed class PetitionActionEndpoint : ApiEndpoint
    {
        /// <summary>How long the request waits for the world loop.</summary>
        public const int WaitMs = 10000;

        public const int MaxAdminLength = 64;

        private readonly bool _close;

        public PetitionActionEndpoint(bool close) => _close = close;

        /// <summary>Does it on the world loop; null when the loop did not get to it in time. Unset until the server has started.</summary>
        public Func<PetitionOrder, PetitionResult> Perform { get; set; }

        public override string Name => _close ? "closepetition" : "respondpetition";

        public override string Method => "POST";

        public override bool Sensitive => true;

        /// <summary>The field that names the admin: "adminId" to close, "admin" to answer.</summary>
        private string AdminField => _close ? "adminId" : "admin";

        private string Verb => _close ? "closed" : "responded";

        public override ApiResponse Handle(ApiRequest request)
        {
            var order = Read(request, out var problem);

            if (order == null)
                return Refused(problem);

            var perform = Perform;

            if (perform == null)
                return Refused("the game server is not ready");

            order.From = ApiAudit.Address(request.Remote);

            var result = perform(order);

            if (result == null)
                return Refused("the game server did not answer in time");

            switch (result.Answer)
            {
                case PetitionAnswer.Done:
                    Logger.WriteLog(LogType.Command,
                        $"Petition #{order.PetitionId} {(_close ? "resolved" : "answered")} by {order.Admin} through the REST API from {order.From}"
                        + (_close ? "." : $": {order.Response}"));

                    return ApiResponse.Ok(ServerStatus.Json(writer =>
                    {
                        writer.WriteBoolean(Verb, true);
                        writer.WriteNumber("petitionId", order.PetitionId);
                        writer.WriteString("status", result.Status.ToString().ToLowerInvariant());
                        writer.WriteBoolean("playerTold", result.Told);
                    }));

                case PetitionAnswer.NoSuchPetition:
                    return Refused("no such petition");

                case PetitionAnswer.NotOpen:
                    return Refused($"the petition is {result.Status.ToString().ToLowerInvariant()}, not open");

                default:
                    return Refused("the petition could not be saved");
            }
        }

        /// <summary>The order in the body; null and what is wrong with it.</summary>
        public PetitionOrder Read(ApiRequest request, out string problem)
        {
            problem = null;

            if (!request.Headers.TryGetValue("Content-Type", out var type)
                || !(type ?? "").TrimStart().StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
            {
                problem = "content type must be application/json";
                return null;
            }

            JsonDocument document;

            try
            {
                document = JsonDocument.Parse(request.Body ?? "", new JsonDocumentOptions { MaxDepth = 8 });
            }
            catch (JsonException)
            {
                problem = "the body is not JSON";
                return null;
            }

            using (document)
            {
                var root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    problem = "the body must be a JSON object";
                    return null;
                }

                if (!Present(root, "petitionId", out var id) || !Whole(id, out var petitionId))
                {
                    problem = "petitionId must be a whole number above 0";
                    return null;
                }

                var order = new PetitionOrder { PetitionId = petitionId };

                if (!Present(root, AdminField, out var admin) || !Named(admin, out var name))
                {
                    problem = $"{AdminField} is required";
                    return null;
                }

                if (name.Length > MaxAdminLength)
                {
                    problem = $"{AdminField} is longer than {MaxAdminLength} characters";
                    return null;
                }

                order.Admin = name;

                if (_close)
                    return order;

                if (!Present(root, "response", out var response) || response.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(response.GetString()))
                {
                    problem = "response is required";
                    return null;
                }

                order.Response = response.GetString().Trim();

                if (order.Response.Length > PetitionManager.MaxResponseLength)
                {
                    problem = $"response is longer than {PetitionManager.MaxResponseLength} characters";
                    return null;
                }

                return order;
            }
        }

        /// <summary>On the world loop: answers or closes the petition the order names.</summary>
        public static PetitionResult Act(PetitionOrder order) =>
            order.Response == null
                ? PetitionManager.Instance.Close(order.PetitionId)
                : PetitionManager.Instance.Respond(order.PetitionId, order.Response);

        private ApiResponse Refused(string error) =>
            ApiResponse.Ok(ServerStatus.Json(writer =>
            {
                writer.WriteBoolean(Verb, false);
                writer.WriteString("error", error);
            }));

        /// <summary>Whether the object has the property with a value that is not null.</summary>
        private static bool Present(JsonElement root, string name, out JsonElement value) =>
            root.TryGetProperty(name, out value) && value.ValueKind != JsonValueKind.Null;

        private static bool Whole(JsonElement value, out uint id)
        {
            id = 0;

            if (value.ValueKind == JsonValueKind.Number)
                return value.TryGetUInt32(out id) && id > 0;

            return value.ValueKind == JsonValueKind.String
                   && uint.TryParse(value.GetString()?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out id)
                   && id > 0;
        }

        /// <summary>An admin's name or id: text that is not blank, or a number.</summary>
        private static bool Named(JsonElement value, out string name)
        {
            name = value.ValueKind switch
            {
                JsonValueKind.String => value.GetString()?.Trim(),
                JsonValueKind.Number => value.GetRawText(),
                _ => null
            };

            return !string.IsNullOrEmpty(name);
        }
    }
}
