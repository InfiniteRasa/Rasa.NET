using System;
using System.Collections.Generic;
using System.Net;

namespace Rasa.Api
{
    /// <summary>
    /// One thing the REST API answers, at GET /&lt;Name&gt; (or POST, by <see cref="Method"/>). A
    /// new one is a class like those below, registered with <see cref="ApiServer.Register"/>
    /// (ApiHost does the server's own), and it is on, wants a key or not, and has a key of its
    /// own by its entry in ApiConfig.Rest.Endpoints, with no code for any of that.
    ///
    /// Handle runs on a thread of the listener's, not on the world loop: it may read what is
    /// kept for other threads (ServerStatus) and nothing of the world.
    /// </summary>
    public abstract class ApiEndpoint
    {
        /// <summary>The path, without its slash, and the key of its settings: lower case.</summary>
        public abstract string Name { get; }

        /// <summary>"GET", which HEAD goes with, or "POST", whose body is the request's <see cref="ApiRequest.Body"/>.</summary>
        public virtual string Method => "GET";

        /// <summary>
        /// An endpoint that changes something, or gives out what the server's keeper may not
        /// want given out. It is off until its own entry in ApiConfig.Rest.Endpoints turns it
        /// on, and the API being public does not make it public: only a Public of its own does.
        /// Its own key and the global key open it as they open any other.
        /// </summary>
        public virtual bool Sensitive => false;

        /// <summary>Whether the REST API key/public policy is applied before Handle runs.</summary>
        public virtual bool RequiresApiKey => true;

        /// <summary>Whether this endpoint handles a normalized path. Override for parameterized routes.</summary>
        public virtual bool Matches(string endpointName) =>
            string.Equals(Name?.Trim('/'), endpointName, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// The most a POST's body may come to for this endpoint, in bytes. One that takes more
        /// than <see cref="ApiServer.MaxBodyBytes"/> is asked for its key before its body is
        /// read, so nobody without one has the server hold a large body for them. It does
        /// nothing for an endpoint that is not asked for the key
        /// (<see cref="RequiresApiKey"/>): that one takes the usual cap.
        /// </summary>
        public virtual int MaxBodyBytes => ApiServer.MaxBodyBytes;

        public abstract ApiResponse Handle(ApiRequest request);
    }

    /// <summary>A request as the API read it.</summary>
    public sealed class ApiRequest
    {
        /// <summary>GET, HEAD or POST, in capitals.</summary>
        public string Method { get; set; } = "GET";

        /// <summary>The body of a POST, as text; empty for none.</summary>
        public string Body { get; set; } = "";

        /// <summary>The path asked for, without the query: "/serverstatus".</summary>
        public string Path { get; set; } = "/";

        /// <summary>What followed the question mark, unparsed; empty for nothing.</summary>
        public string Query { get; set; } = "";

        /// <summary>The headers, by name in any case. A header sent twice keeps its last value.</summary>
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Who asked.</summary>
        public IPAddress Remote { get; set; }

        /// <summary>Set by the server once every rule has let the request through to its endpoint, key and all.</summary>
        internal bool LetThrough { get; set; }

        /// <summary>The page that sent it, by a browser's Origin header ("null" for a page opened from disk); null for none.</summary>
        public string Origin
        {
            get
            {
                if (!Headers.TryGetValue("Origin", out var origin) || string.IsNullOrWhiteSpace(origin))
                    return null;

                origin = origin.Trim();

                // It is sent back in a header of the answer: nothing that is not plain text.
                foreach (var letter in origin)
                    if (letter < 0x21 || letter > 0x7e)
                        return null;

                return origin.Length <= 256 ? origin : null;
            }
        }

        /// <summary>The path as an endpoint's name: no slashes at either end, lower case.</summary>
        public string EndpointName => (Path ?? "").Trim('/').ToLowerInvariant();

        /// <summary>The URL query parsed into decoded name/value pairs.</summary>
        public Dictionary<string, string> QueryParameters
        {
            get
            {
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (string.IsNullOrEmpty(Query))
                    return values;

                foreach (var part in Query.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var equals = part.IndexOf('=');
                    var rawKey = equals >= 0 ? part.Substring(0, equals) : part;
                    var rawValue = equals >= 0 ? part.Substring(equals + 1) : string.Empty;
                    var key = Uri.UnescapeDataString(rawKey.Replace('+', ' '));
                    var value = Uri.UnescapeDataString(rawValue.Replace('+', ' '));
                    values[key] = value;
                }

                return values;
            }
        }

        /// <summary>
        /// The key the request carries: the X-API-Key header, or the token of
        /// "Authorization: Bearer". Null for neither.
        /// </summary>
        public string ApiKey
        {
            get
            {
                if (Headers.TryGetValue("X-API-Key", out var key) && !string.IsNullOrWhiteSpace(key))
                    return key.Trim();

                if (Headers.TryGetValue("Authorization", out var authorization) && authorization != null)
                {
                    authorization = authorization.Trim();

                    if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        var token = authorization.Substring(7).Trim();

                        return token.Length > 0 ? token : null;
                    }
                }

                return null;
            }
        }
    }

    /// <summary>An answer: a status and a JSON body.</summary>
    public sealed class ApiResponse
    {
        public int Status { get; }
        public string Json { get; }

        /// <summary>For a 405: the methods that are allowed.</summary>
        public string Allow { get; set; }

        /// <summary>Headers beyond the ones every answer has: what a browser asks before it lets a page read it.</summary>
        public List<KeyValuePair<string, string>> Headers { get; } = new List<KeyValuePair<string, string>>();

        public ApiResponse(int status, string json)
        {
            Status = status;
            Json = json ?? "{}";
        }

        public static ApiResponse Ok(string json) => new ApiResponse(200, json);

        /// <summary>{"error":"..."} under that status.</summary>
        public static ApiResponse Error(int status, string error) =>
            new ApiResponse(status, ServerStatus.Json(writer => writer.WriteString("error", error)));
    }

    /// <summary>
    /// GET /healthcheck: {"game_server_status":"healthy","app_server_status":"healthy"}, each
    /// "healthy" or "unhealthy" (ServerStatus). 200 when both are healthy and 503 when either is
    /// not, with the same body, so a monitor that only looks at the status line is right too.
    /// </summary>
    public sealed class HealthCheckEndpoint : ApiEndpoint
    {
        private readonly ServerStatus _status;

        public HealthCheckEndpoint(ServerStatus status) => _status = status;

        public override string Name => "healthcheck";

        public override ApiResponse Handle(ApiRequest request)
        {
            var health = _status.Health;

            return new ApiResponse(health.Game && health.App ? 200 : 503, ServerStatus.HealthJson(health));
        }
    }

    /// <summary>GET /serverstatus: {"uptimeseconds":45000,"currentconnections":3,"peakconnections":9,"maxconnections":1024}.</summary>
    public sealed class ServerStatusEndpoint : ApiEndpoint
    {
        private readonly ServerStatus _status;

        public ServerStatusEndpoint(ServerStatus status) => _status = status;

        public override string Name => "serverstatus";

        public override ApiResponse Handle(ApiRequest request) => ApiResponse.Ok(_status.StatusJson());
    }
}
