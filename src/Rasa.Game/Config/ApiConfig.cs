using System.Collections.Generic;

namespace Rasa.Config
{
    /// <summary>
    /// The server's two status listeners (Rasa.Api): a REST API on one port, and a plain TCP
    /// port that answers anything sent to it with the whole server status. Both are off when
    /// the section is missing. Everything here is picked up by a config reload; a change of
    /// BindAddress or Port restarts that listener.
    /// </summary>
    public class ApiConfig
    {
        /// <summary>
        /// How long the world loop may go without a tick before the game server is reported
        /// unhealthy, in seconds. A tick is a tenth of a second; a long save is a few.
        /// </summary>
        public int LoopStallSeconds { get; set; } = 15;

        /// <summary>The REST API.</summary>
        public RestApiConfig Rest { get; set; } = new RestApiConfig();

        /// <summary>The status port.</summary>
        public StatusPortConfig StatusPort { get; set; } = new StatusPortConfig();
    }

    /// <summary>
    /// The REST API: GET /&lt;endpoint&gt; over plain HTTP, answered in JSON.
    ///
    /// An endpoint that is not public wants a key, sent as the X-API-Key header or as
    /// "Authorization: Bearer &lt;key&gt;": its own <see cref="ApiEndpointConfig.ApiKey"/> or the
    /// global <see cref="ApiKey"/>, whichever are set. With neither set it answers nobody.
    /// </summary>
    public class RestApiConfig
    {
        /// <summary>Whether the API listens at all.</summary>
        public bool Enabled { get; set; }

        /// <summary>The local address the listener binds to. Empty or "0.0.0.0" binds every interface.</summary>
        public string BindAddress { get; set; } = "0.0.0.0";

        /// <summary>The TCP port the API listens on.</summary>
        public int Port { get; set; } = 8104;

        /// <summary>
        /// Whether the endpoints answer without a key. An endpoint with a
        /// <see cref="ApiEndpointConfig.Public"/> of its own goes by that instead.
        /// </summary>
        public bool Public { get; set; }

        /// <summary>A key every endpoint accepts. Empty for none.</summary>
        public string ApiKey { get; set; } = "";

        /// <summary>
        /// The addresses the API answers: single addresses ("203.0.113.7") and ranges
        /// ("10.0.0.0/8"). Missing or empty answers every address.
        /// </summary>
        public List<string> AllowedIps { get; set; } = new List<string>();

        /// <summary>
        /// The endpoints' own settings, by name ("healthcheck", "serverstatus"). An endpoint
        /// with no entry is on, and goes by <see cref="Public"/> and <see cref="ApiKey"/>.
        /// </summary>
        public Dictionary<string, ApiEndpointConfig> Endpoints { get; set; } = new Dictionary<string, ApiEndpointConfig>();
    }

    /// <summary>One endpoint of the REST API.</summary>
    public class ApiEndpointConfig
    {
        /// <summary>Whether the endpoint exists. Off, it is a 404.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Whether this endpoint answers without a key; left out, <see cref="RestApiConfig.Public"/> decides.</summary>
        public bool? Public { get; set; }

        /// <summary>A key for this endpoint alone. Empty for none: the global key still works.</summary>
        public string ApiKey { get; set; } = "";
    }

    /// <summary>
    /// The status port: a TCP port that answers the first bytes a connection sends with the
    /// whole server status as one line of JSON, and closes. No key; who may ask is
    /// <see cref="AllowedIps"/>.
    /// </summary>
    public class StatusPortConfig
    {
        /// <summary>Whether the port listens at all.</summary>
        public bool Enabled { get; set; }

        /// <summary>The local address the listener binds to. Empty or "0.0.0.0" binds every interface.</summary>
        public string BindAddress { get; set; } = "0.0.0.0";

        /// <summary>The TCP port.</summary>
        public int Port { get; set; } = 8105;

        /// <summary>
        /// The addresses the port answers: single addresses and ranges, as for the REST API.
        /// Missing or empty answers every address; any other connection is closed unanswered.
        /// </summary>
        public List<string> AllowedIps { get; set; } = new List<string>();
    }
}
