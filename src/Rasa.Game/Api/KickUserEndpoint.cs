using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace Rasa.Api
{
    using Managers;

    /// <summary>Who /kickuser is to kick, why, and who asked.</summary>
    public sealed class KickOrder
    {
        /// <summary>The account, by id; or null, and <see cref="FamilyName"/>.</summary>
        public uint? AccountId { get; set; }

        /// <summary>The account, by its family name; or null, and <see cref="AccountId"/>.</summary>
        public string FamilyName { get; set; }

        /// <summary>What the player is told; null for no reason.</summary>
        public string Reason { get; set; }

        /// <summary>The admin who asked, as the caller names them.</summary>
        public string Admin { get; set; }

        /// <summary>The address the request came from.</summary>
        public string From { get; set; }
    }

    /// <summary>What came of a kick: the account kicked, or why none was.</summary>
    public sealed class KickOutcome
    {
        public bool Kicked { get; private set; }
        public string Error { get; private set; }
        public uint AccountId { get; private set; }
        public string FamilyName { get; private set; }

        public static KickOutcome Done(uint accountId, string familyName) =>
            new KickOutcome { Kicked = true, AccountId = accountId, FamilyName = familyName ?? "" };

        public static KickOutcome Failed(string error) => new KickOutcome { Error = error };
    }

    /// <summary>
    /// POST /kickuser: disconnects an account that is online, as the console's kick does
    /// (Moderation.Kick) - every connection of it, in the world or at character selection,
    /// after telling the player why. The body is JSON:
    ///
    ///   {"accountId":12,"reason":"griefing","admin":"Atomsk"} or
    ///   {"familyName":"Hart","reason":"griefing","admin":"Atomsk"}
    ///
    /// accountId (a number, or a number in a string) or familyName, one and not both; reason,
    /// which may be left out; admin, the one asking, which may not. Every answer of its own is
    /// 200: {"kicked":true,"accountId":12,"familyName":"Hart"} for a kick, and
    /// {"kicked":false,"error":"not online"} when there was none, the error saying why. The
    /// API's own refusals - address, path, method, key - are the API's, as for any endpoint.
    ///
    /// The kick is done on the world loop, which the clients belong to (<see cref="Kick"/>,
    /// set by Server); this thread waits for it up to <see cref="WaitMs"/>. It changes
    /// something, so it is <see cref="ApiEndpoint.Sensitive"/>: off until its own entry in
    /// ApiConfig.Rest.Endpoints turns it on, public only by a Public of its own.
    /// </summary>
    public sealed class KickUserEndpoint : ApiEndpoint
    {
        /// <summary>How long the request waits for the world loop to do the kick.</summary>
        public const int WaitMs = 10000;

        public const int MaxReasonLength = 256;
        public const int MaxAdminLength = 64;

        /// <summary>
        /// Does the kick on the world loop and gives what came of it; null when the loop did not
        /// get to it in time. Unset until the server has started.
        /// </summary>
        public Func<KickOrder, KickOutcome> Kick { get; set; }

        public override string Name => "kickuser";

        public override string Method => "POST";

        public override bool Sensitive => true;

        public override ApiResponse Handle(ApiRequest request)
        {
            var order = Read(request, out var problem);

            if (order == null)
                return Answer(KickOutcome.Failed(problem));

            var kick = Kick;

            if (kick == null)
                return Answer(KickOutcome.Failed("the game server is not ready"));

            order.From = ApiAudit.Address(request.Remote);

            return Answer(kick(order) ?? KickOutcome.Failed("the game server did not answer in time"));
        }

        /// <summary>
        /// On the world loop: kicks the account the order names, if it is online. An admin
        /// through the API may kick any account, as the console may.
        /// </summary>
        public static KickOutcome Perform(KickOrder order)
        {
            var targets = order.AccountId.HasValue ? Moderation.OnlineAccount(order.AccountId.Value) : Moderation.Online(order.FamilyName);

            if (targets.Count == 0)
                return KickOutcome.Failed("not online");

            var account = targets[0].AccountEntry;

            Moderation.Kick(targets, order.Reason, $"{order.Admin} through the REST API from {order.From}");

            return KickOutcome.Done(account.Id, account.FamilyName);
        }

        /// <summary>The order in the body; null and what is wrong with it.</summary>
        public static KickOrder Read(ApiRequest request, out string problem)
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

                var order = new KickOrder();
                var hasId = Present(root, "accountId", out var id);
                var hasName = Present(root, "familyName", out var name);

                if (hasId == hasName)
                {
                    problem = hasId ? "give accountId or familyName, not both" : "accountId or familyName is required";
                    return null;
                }

                if (hasId)
                {
                    if (!AccountId(id, out var accountId))
                    {
                        problem = "accountId must be a whole number above 0";
                        return null;
                    }

                    order.AccountId = accountId;
                }
                else
                {
                    if (name.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(name.GetString()))
                    {
                        problem = "familyName must be a name";
                        return null;
                    }

                    order.FamilyName = name.GetString().Trim();
                }

                if (Present(root, "reason", out var reason))
                {
                    if (reason.ValueKind != JsonValueKind.String)
                    {
                        problem = "reason must be text";
                        return null;
                    }

                    order.Reason = reason.GetString().Trim();

                    if (order.Reason.Length > MaxReasonLength)
                    {
                        problem = $"reason is longer than {MaxReasonLength} characters";
                        return null;
                    }

                    if (order.Reason.Length == 0)
                        order.Reason = null;
                }

                if (!Present(root, "admin", out var admin) || admin.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(admin.GetString()))
                {
                    problem = "admin is required";
                    return null;
                }

                order.Admin = admin.GetString().Trim();

                if (order.Admin.Length > MaxAdminLength)
                {
                    problem = $"admin is longer than {MaxAdminLength} characters";
                    return null;
                }

                return order;
            }
        }

        /// <summary>Whether the object has the property with a value that is not null.</summary>
        private static bool Present(JsonElement root, string name, out JsonElement value) =>
            root.TryGetProperty(name, out value) && value.ValueKind != JsonValueKind.Null;

        private static bool AccountId(JsonElement value, out uint id)
        {
            id = 0;

            if (value.ValueKind == JsonValueKind.Number)
                return value.TryGetUInt32(out id) && id > 0;

            return value.ValueKind == JsonValueKind.String
                   && uint.TryParse(value.GetString()?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out id)
                   && id > 0;
        }

        private static ApiResponse Answer(KickOutcome outcome) =>
            ApiResponse.Ok(ServerStatus.Json(writer =>
            {
                writer.WriteBoolean("kicked", outcome.Kicked);

                if (outcome.Kicked)
                {
                    writer.WriteNumber("accountId", outcome.AccountId);
                    writer.WriteString("familyName", outcome.FamilyName);
                }
                else
                {
                    writer.WriteString("error", outcome.Error ?? "not kicked");
                }
            }));
    }
}
