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
    /// The REST API: GET /&lt;endpoint&gt; over HTTP, or HTTPS with <see cref="Tls"/>, answered in JSON.
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
        /// HMAC secret for JWT-backed API authentication such as the in-game API. Empty uses a
        /// random process-local secret; set this to keep tokens valid across server restarts.
        /// A configured value must be at least 32 UTF-8 bytes.
        /// </summary>
        public string JwtSecret { get; set; } = "";

        /// <summary>Lifetime of JWTs issued by the REST API, in seconds.</summary>
        public int JwtTokenLifetimeSeconds { get; set; } = 86400;

        /// <summary>
        /// The addresses the API answers: single addresses ("203.0.113.7") and ranges
        /// ("10.0.0.0/8"). Missing or empty answers every address.
        /// </summary>
        public List<string> AllowedIps { get; set; } = new List<string>();

        /// <summary>
        /// The web pages that may use the API from a browser, by their origin: "null" for a
        /// page opened from a file on disk, as gametools' editors are; "http://tools.example"
        /// for one served from there; "*" for any. Missing or empty, as it is unless asked for,
        /// no page may: a browser will not let one send a key, or read an answer.
        ///
        /// It opens only the endpoints that want a key. One that answers without a key is never
        /// opened to pages, whatever is listed here: any page a player's browser happened to
        /// load could otherwise use it from inside the network.
        /// </summary>
        public List<string> AllowedOrigins { get; set; } = new List<string>();

        /// <summary>
        /// The endpoints' own settings, by name ("healthcheck", "serverstatus", "addaccount",
        /// "ingame/session/exchange", and so on).
        /// An endpoint with no entry is on, and goes by <see cref="Public"/> and
        /// <see cref="ApiKey"/>. One that changes something, or gives out what the server's
        /// keeper may not want given out ("addaccount", which makes logins; gametools' four,
        /// "monsterflags", "updatemonsterflags", "lootpools", "updatelootpools"; "usersonline",
        /// which names the accounts and characters online; "kickuser", which disconnects one)
        /// is the other way about: off with no entry, on only by an Enabled of true in its own,
        /// and public only by a Public of true in its own, whatever <see cref="Public"/> says.
        /// </summary>
        public Dictionary<string, ApiEndpointConfig> Endpoints { get; set; } = new Dictionary<string, ApiEndpointConfig>();

        /// <summary>HTTPS in place of HTTP on the same port.</summary>
        public ApiTlsConfig Tls { get; set; } = new ApiTlsConfig();
    }

    /// <summary>
    /// TLS for the REST API (Api.ApiTls). Enabled, the port speaks HTTPS and nothing else: a
    /// certificate that cannot be loaded leaves the API off, never on in the clear.
    ///
    /// The files are looked at again while the server runs, so a renewed certificate is taken
    /// up without a restart; one that will not load leaves the one in use in use.
    /// </summary>
    public class ApiTlsConfig
    {
        /// <summary>Whether the API is HTTPS.</summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// The certificate: a PKCS#12 file (.pfx, .p12) with its private key inside, or a PEM
        /// file (which may hold the chain after the certificate, and the key).
        /// </summary>
        public string CertificatePath { get; set; } = "";

        /// <summary>The password of the PKCS#12 file, or of an encrypted PEM key. Empty for none.</summary>
        public string CertificatePassword { get; set; } = "";

        /// <summary>The PEM private key, when it is not in the certificate's file. Empty for a PKCS#12 file.</summary>
        public string KeyPath { get; set; } = "";

        /// <summary>The oldest protocol accepted: "Tls12" (TLS 1.2 and 1.3) or "Tls13" (TLS 1.3 alone).</summary>
        public string MinimumProtocol { get; set; } = "Tls12";
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
