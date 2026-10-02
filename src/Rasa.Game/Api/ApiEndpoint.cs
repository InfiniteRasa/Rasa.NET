using System;
using System.Collections.Generic;
using System.Net;

namespace Rasa.Api
{
    /// <summary>
    /// One thing the REST API answers, at GET /&lt;Name&gt;. A new one is a class like those below,
    /// registered with <see cref="ApiServer.Register"/> (ApiHost does the server's own), and it
    /// is on, wants a key or not, and has a key of its own by its entry in
    /// ApiConfig.Rest.Endpoints, with no code for any of that.
    ///
    /// Handle runs on a thread of the listener's, not on the world loop: it may read what is
    /// kept for other threads (ServerStatus) and nothing of the world.
    /// </summary>
    public abstract class ApiEndpoint
    {
        /// <summary>The path, without its slash, and the key of its settings: lower case.</summary>
        public abstract string Name { get; }

        public abstract ApiResponse Handle(ApiRequest request);
    }

    /// <summary>A request as the API read it.</summary>
    public sealed class ApiRequest
    {
        /// <summary>GET or HEAD, in capitals.</summary>
        public string Method { get; set; } = "GET";

        /// <summary>The path asked for, without the query: "/serverstatus".</summary>
        public string Path { get; set; } = "/";

        /// <summary>What followed the question mark, unparsed; empty for nothing.</summary>
        public string Query { get; set; } = "";

        /// <summary>The headers, by name in any case. A header sent twice keeps its last value.</summary>
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Who asked.</summary>
        public IPAddress Remote { get; set; }

        /// <summary>The path as an endpoint's name: no slashes at either end, lower case.</summary>
        public string EndpointName => (Path ?? "").Trim('/').ToLowerInvariant();

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

    /// <summary>GET /serverstatus: {"uptimehours":12.5,"currentconnections":3,"peakconnections":9,"maxconnections":1024}.</summary>
    public sealed class ServerStatusEndpoint : ApiEndpoint
    {
        private readonly ServerStatus _status;

        public ServerStatusEndpoint(ServerStatus status) => _status = status;

        public override string Name => "serverstatus";

        public override ApiResponse Handle(ApiRequest request) => ApiResponse.Ok(_status.StatusJson());
    }
}
