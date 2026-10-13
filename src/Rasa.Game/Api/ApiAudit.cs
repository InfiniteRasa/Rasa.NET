using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Rasa.Api
{
    using Repositories.UnitOfWork;
    using Structures.Char;

    /// <summary>
    /// The REST API's log: every request the API is sent, kept in the character database's
    /// api_log table (<see cref="IStore"/>). A row says when, the address it came from, the
    /// method, the path and query asked for, the status of the answer, the body of a POST and
    /// the start of the answer.
    ///
    /// Every request is on it, the ones turned away too - an address not allowed, a path that
    /// is no endpoint, a wrong key, a body that could not be read - with the status they were
    /// given. What is not: a connection that sent nothing, or never finished a TLS handshake,
    /// asked for nothing. The status port is not the REST API and is not on it.
    ///
    /// A body is kept as it was sent, but what is secret in it is written over with
    /// <see cref="Hidden"/> first: the value of any field whose name has "password", "secret",
    /// "token" or "apikey" in it or ends in "code" - /addaccount's password, the one-time
    /// exchangeCode of /ingame/session/exchange. The answer is kept the same way, so the token
    /// that exchange answers with is not kept either. A key sent in a header is never kept. A
    /// body longer than ApiLogEntry.MaxBodyLength is kept up to there, with its whole length.
    ///
    /// The row is written once the answer has gone out, on the listener's thread. Nothing waits
    /// on the log or fails with it: a store that cannot be written costs the row and says so in
    /// the server log.
    /// </summary>
    public sealed class ApiAudit
    {
        /// <summary>What a secret's value is written over with.</summary>
        public const string Hidden = "***";

        /// <summary>Where the log is kept. The call may throw; it is not retried.</summary>
        public interface IStore
        {
            /// <summary>Adds a row. Returns its id.</summary>
            uint Add(ApiLogEntry entry);
        }

        /// <summary>Where the rows go; null keeps none. Set by <see cref="Load"/>.</summary>
        public IStore Store { get; private set; }

        /// <summary>The wall clock rows are dated by, UTC. Replaceable for tests.</summary>
        public Func<DateTime> UtcNow { get; set; } = () => DateTime.UtcNow;

        /// <summary>From now on rows are kept in <paramref name="store"/>.</summary>
        public void Load(IStore store)
        {
            Store = store;
        }

        /// <summary>
        /// Puts a request and its answer on the log. <paramref name="request"/> is null for one
        /// that could not be read. Returns the row's id; 0 when none was written - no store, or
        /// the store failed.
        /// </summary>
        public uint Record(ApiRequest request, IPAddress remote, ApiResponse response)
        {
            var store = Store;

            if (store == null)
                return 0;

            remote ??= request?.Remote;

            try
            {
                var body = request != null && request.Method == "POST" ? request.Body ?? "" : "";

                return store.Add(new ApiLogEntry
                {
                    CreatedAt = UtcNow(),
                    Address = Cut(Address(remote), ApiLogEntry.MaxAddressLength),
                    Method = Cut(request?.Method, ApiLogEntry.MaxMethodLength),
                    Path = Cut(request?.Path, ApiLogEntry.MaxPathLength),
                    Query = Cut(request?.Query, ApiLogEntry.MaxQueryLength),
                    Status = response?.Status ?? 0,
                    Body = Cut(Redact(body), ApiLogEntry.MaxBodyLength),
                    BodyLength = body.Length,
                    Response = Cut(Redact(response?.Json), ApiLogEntry.MaxResponseLength)
                });
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error,
                    $"REST API log: a request from {remote} to {request?.Path} could not be recorded: {e.GetBaseException().Message}");

                return 0;
            }
        }

        /// <summary>An address as it is kept: an IPv4 address that came in over IPv6 as the IPv4 one.</summary>
        public static string Address(IPAddress remote) =>
            remote == null ? "" : (remote.IsIPv4MappedToIPv6 ? remote.MapToIPv4() : remote).ToString();

        /// <summary>Whether a field of this name is a secret, whose value is not kept.</summary>
        public static bool IsSecret(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            var plain = name.Replace("_", "").Replace("-", "").ToLowerInvariant();

            return plain.Contains("password") || plain.Contains("secret") || plain.Contains("token")
                   || plain.Contains("apikey") || plain.EndsWith("code", StringComparison.Ordinal);
        }

        /// <summary>
        /// A body with its secrets' values written over. JSON is read as JSON, at any depth; a
        /// body with no secret in it is kept as it was. One that is not JSON - broken, or a form
        /// - has its secrets found by their names as text.
        /// </summary>
        public static string Redact(string body)
        {
            if (string.IsNullOrEmpty(body))
                return "";

            try
            {
                using var document = JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 64 });

                if (!HasSecret(document.RootElement))
                    return body;

                using var stream = new MemoryStream();

                using (var writer = new Utf8JsonWriter(stream))
                    Write(writer, document.RootElement);

                return Encoding.UTF8.GetString(stream.ToArray());
            }
            catch (JsonException)
            {
                var text = FieldInText.Replace(body, field =>
                    IsSecret(field.Groups["name"].Value) ? field.Groups["field"].Value + "\"" + Hidden + "\"" : field.Value);

                return FieldInForm.Replace(text, field =>
                    IsSecret(Uri.UnescapeDataString(field.Groups["name"].Value.Replace('+', ' '))) ? field.Groups["field"].Value + Hidden : field.Value);
            }
        }

        /// <summary>A field and its value in a body that is not JSON that reads: "name": "value", or a bare value.</summary>
        private static readonly Regex FieldInText = new Regex(
            @"(?<field>""(?<name>(?:[^""\\]|\\.)*)""\s*:\s*)(?<value>""(?:[^""\\]|\\.)*""?|[^,}\]\s]*)",
            RegexOptions.Compiled);

        /// <summary>A field of a form: name=value.</summary>
        private static readonly Regex FieldInForm = new Regex(
            @"(?<field>(?:^|&)(?<name>[^=&]*)=)(?<value>[^&]*)",
            RegexOptions.Compiled);

        private static bool HasSecret(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                        if (IsSecret(property.Name) || HasSecret(property.Value))
                            return true;

                    return false;

                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                        if (HasSecret(item))
                            return true;

                    return false;

                default:
                    return false;
            }
        }

        private static void Write(Utf8JsonWriter writer, JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    writer.WriteStartObject();

                    foreach (var property in element.EnumerateObject())
                    {
                        writer.WritePropertyName(property.Name);

                        if (IsSecret(property.Name) && property.Value.ValueKind != JsonValueKind.Null)
                            writer.WriteStringValue(Hidden);
                        else
                            Write(writer, property.Value);
                    }

                    writer.WriteEndObject();
                    break;

                case JsonValueKind.Array:
                    writer.WriteStartArray();

                    foreach (var item in element.EnumerateArray())
                        Write(writer, item);

                    writer.WriteEndArray();
                    break;

                default:
                    element.WriteTo(writer);
                    break;
            }
        }

        private static string Cut(string text, int length) =>
            string.IsNullOrEmpty(text) ? "" : text.Length <= length ? text : text.Substring(0, length);

        /// <summary>The live server's store: the character database.</summary>
        public sealed class ServerStore : IStore
        {
            private readonly IGameUnitOfWorkFactory _factory;

            public ServerStore(IGameUnitOfWorkFactory factory)
            {
                _factory = factory;
            }

            public uint Add(ApiLogEntry entry)
            {
                using var unitOfWork = _factory.CreateChar();
                return unitOfWork.ApiLogs.Add(entry);
            }
        }
    }
}
