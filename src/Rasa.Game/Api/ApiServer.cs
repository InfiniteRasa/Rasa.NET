using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rasa.Api
{
    using Config;

    /// <summary>
    /// The REST API (ApiConfig.Rest): GET /&lt;endpoint&gt; over plain HTTP on a port of its own,
    /// answered in JSON, one request a connection.
    ///
    /// Who is answered, in this order:
    ///  - an address not on AllowedIps (when there is such a list) gets 403;
    ///  - anything but GET and HEAD gets 405;
    ///  - a path that names no endpoint, or one switched off, gets 404;
    ///  - an endpoint that is not public (its own Public, else the API's) wants a key, in the
    ///    X-API-Key header or as "Authorization: Bearer": its own ApiKey or the API's, whichever
    ///    are set. Without one of them, 401;
    ///  - then the endpoint answers.
    ///
    /// It is HTTP written by hand on a TcpListener, not HttpListener: that one goes through
    /// http.sys on Windows, which wants an administrator or a URL reservation for any address
    /// but localhost. There is no TLS here, so a key crosses the network as it is: where that
    /// matters, bind it to 127.0.0.1 and put a proxy that has TLS in front.
    /// </summary>
    public sealed class ApiServer : TcpService
    {
        /// <summary>The most a request's line and headers may come to, in bytes.</summary>
        public const int MaxRequestBytes = 8192;

        private readonly Dictionary<string, ApiEndpoint> _endpoints = new Dictionary<string, ApiEndpoint>(StringComparer.OrdinalIgnoreCase);

        private volatile RestApiConfig _config = new RestApiConfig();
        private volatile IpAllowList _allowed = IpAllowList.Open;

        protected override string Label => "REST API";

        /// <summary>The endpoints' names, in order.</summary>
        public IReadOnlyList<string> Endpoints
        {
            get
            {
                lock (_endpoints)
                    return _endpoints.Keys.OrderBy(name => name, StringComparer.Ordinal).ToList();
            }
        }

        /// <summary>Adds an endpoint, in place of any of the same name.</summary>
        public void Register(ApiEndpoint endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint?.Name))
                throw new ArgumentException("An endpoint needs a name.", nameof(endpoint));

            lock (_endpoints)
                _endpoints[endpoint.Name.Trim('/').ToLowerInvariant()] = endpoint;
        }

        /// <summary>
        /// Takes the settings: on a config reload as at the start. Keys, Public, the endpoints'
        /// entries and the allow list count from the next request; the listener is opened,
        /// moved or closed to match.
        /// </summary>
        public void Apply(RestApiConfig config)
        {
            config ??= new RestApiConfig();

            var allowed = IpAllowList.Parse(config.AllowedIps);

            _config = config;
            _allowed = allowed;

            if (!config.Enabled)
            {
                Stop();
                _described = null;
                return;
            }

            if (!Listen(config.BindAddress, config.Port))
            {
                _described = null;
                return;
            }

            // Said when it opens and when a reload changed any of it, not on every reload.
            var lines = new List<string>();

            foreach (var entry in allowed.Invalid)
                lines.Add($"AllowedIps entry \"{entry}\" is no address and no range; it allows nobody.");

            foreach (var name in Endpoints)
            {
                var endpoint = EndpointConfigOf(config, name);
                var state = endpoint?.Enabled == false ? "off"
                    : IsPublic(config, endpoint) ? "public"
                    : HasKey(config, endpoint) ? "key"
                    : "key, and none is set: it answers nobody until an ApiKey is set or it is made public";

                lines.Add($"/{name} ({state})");
            }

            lines.Add(allowed.AllowsAll ? "any address may ask." : $"{allowed.Count} allowed address(es) or range(s).");

            var described = $"{LocalEndPoint}|{string.Join("|", lines)}";

            if (described == _described)
                return;

            _described = described;

            foreach (var line in lines)
                Logger.WriteLog(line.StartsWith("AllowedIps", StringComparison.Ordinal) ? LogType.Error : LogType.Network, $"{Label}: {line}");
        }

        private string _described;

        private static ApiEndpointConfig EndpointConfigOf(RestApiConfig config, string name)
        {
            if (config.Endpoints == null)
                return null;

            foreach (var entry in config.Endpoints)
                if (string.Equals(entry.Key?.Trim('/'), name, StringComparison.OrdinalIgnoreCase))
                    return entry.Value;

            return null;
        }

        private static bool IsPublic(RestApiConfig config, ApiEndpointConfig endpoint) => endpoint?.Public ?? config.Public;

        private static bool HasKey(RestApiConfig config, ApiEndpointConfig endpoint) =>
            !string.IsNullOrEmpty(endpoint?.ApiKey) || !string.IsNullOrEmpty(config.ApiKey);

        /// <summary>Whether two keys are the same, in a time that does not tell how much of one was right.</summary>
        private static bool SameKey(string configured, string presented)
        {
            if (string.IsNullOrEmpty(configured) || string.IsNullOrEmpty(presented))
                return false;

            // Hashed first, so the comparison is of equal lengths whatever was sent.
            var a = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
            var b = SHA256.HashData(Encoding.UTF8.GetBytes(presented));

            return CryptographicOperations.FixedTimeEquals(a, b);
        }

        /// <summary>The answer to a request: every rule of the class comment, and then the endpoint.</summary>
        public ApiResponse Respond(ApiRequest request)
        {
            var config = _config;

            if (!_allowed.Allows(request.Remote))
            {
                Refused($"{request.Remote}, which is not on AllowedIps");
                return ApiResponse.Error(403, "forbidden");
            }

            if (request.Method != "GET" && request.Method != "HEAD")
                return new ApiResponse(405, ServerStatus.Json(writer => writer.WriteString("error", "method not allowed"))) { Allow = "GET, HEAD" };

            var name = request.EndpointName;
            ApiEndpoint endpoint;

            lock (_endpoints)
                _endpoints.TryGetValue(name, out endpoint);

            var settings = endpoint == null ? null : EndpointConfigOf(config, name);

            if (endpoint == null || settings?.Enabled == false)
                return ApiResponse.Error(404, "not found");

            if (!IsPublic(config, settings))
            {
                var key = request.ApiKey;

                // Both compared whichever matches: no telling from the time which one it was.
                var own = SameKey(settings?.ApiKey, key);
                var global = SameKey(config.ApiKey, key);

                if (!own && !global)
                {
                    Refused($"{request.Remote} at /{name}: {(key == null ? "no key" : "a wrong key")}");
                    return ApiResponse.Error(401, "unauthorized");
                }
            }

            try
            {
                return endpoint.Handle(request) ?? ApiResponse.Error(500, "internal error");
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"{Label}: /{name} failed: {e}");
                return ApiResponse.Error(500, "internal error");
            }
        }

        protected override async Task Exchange(Stream stream, IPAddress remote, CancellationToken limit)
        {
            var head = await ReadHead(stream, limit).ConfigureAwait(false);

            if (head == null)
                return;

            var request = Parse(head);
            ApiResponse response;

            if (request == null)
            {
                response = ApiResponse.Error(400, "bad request");
            }
            else
            {
                request.Remote = remote;
                response = Respond(request);
            }

            var bytes = Render(response, request?.Method == "HEAD");

            await stream.WriteAsync(bytes, 0, bytes.Length, limit).ConfigureAwait(false);
            await stream.FlushAsync(limit).ConfigureAwait(false);
        }

        /// <summary>
        /// Reads a request up to the blank line that ends its headers. Null when the connection
        /// closed first; a request longer than <see cref="MaxRequestBytes"/> comes back as it
        /// stands, which does not parse.
        /// </summary>
        private static async Task<string> ReadHead(Stream stream, CancellationToken limit)
        {
            var buffer = new byte[MaxRequestBytes];
            var length = 0;

            while (length < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer, length, buffer.Length - length, limit).ConfigureAwait(false);

                if (read <= 0)
                    return length == 0 ? null : Encoding.ASCII.GetString(buffer, 0, length);

                length += read;

                if (EndOfHead(buffer, length) >= 0)
                    break;
            }

            return Encoding.ASCII.GetString(buffer, 0, length);
        }

        private static int EndOfHead(byte[] buffer, int length)
        {
            for (var i = 3; i < length; i++)
                if (buffer[i] == '\n' && buffer[i - 1] == '\r' && buffer[i - 2] == '\n' && buffer[i - 3] == '\r')
                    return i + 1;

            return -1;
        }

        /// <summary>
        /// A request from its text: "GET /path?query HTTP/1.1", then its headers, then a blank
        /// line. Null for anything that is not that.
        /// </summary>
        public static ApiRequest Parse(string head)
        {
            if (string.IsNullOrEmpty(head))
                return null;

            var end = head.IndexOf("\r\n\r\n", StringComparison.Ordinal);

            if (end < 0)
                return null;

            var lines = head.Substring(0, end).Split(new[] { "\r\n" }, StringSplitOptions.None);
            var first = lines[0].Split(' ');

            if (first.Length != 3 || first[0].Length == 0 || !first[1].StartsWith("/", StringComparison.Ordinal)
                || !first[2].StartsWith("HTTP/1.", StringComparison.Ordinal))
                return null;

            var target = first[1];
            var question = target.IndexOf('?');

            var request = new ApiRequest
            {
                Method = first[0].ToUpperInvariant(),
                Path = question < 0 ? target : target.Substring(0, question),
                Query = question < 0 ? "" : target.Substring(question + 1)
            };

            for (var i = 1; i < lines.Length; i++)
            {
                var colon = lines[i].IndexOf(':');

                if (colon <= 0)
                    return null;

                request.Headers[lines[i].Substring(0, colon).Trim()] = lines[i].Substring(colon + 1).Trim();
            }

            return request;
        }

        /// <summary>A response as it goes on the wire; without its body for a HEAD.</summary>
        public static byte[] Render(ApiResponse response, bool headOnly)
        {
            var body = Encoding.UTF8.GetBytes(response.Json);
            var head = new StringBuilder();

            head.Append("HTTP/1.1 ").Append(response.Status).Append(' ').Append(ReasonOf(response.Status)).Append("\r\n");
            head.Append("Content-Type: application/json; charset=utf-8\r\n");
            head.Append("Content-Length: ").Append(body.Length).Append("\r\n");
            head.Append("Cache-Control: no-store\r\n");

            if (!string.IsNullOrEmpty(response.Allow))
                head.Append("Allow: ").Append(response.Allow).Append("\r\n");

            head.Append("Connection: close\r\n\r\n");

            var headBytes = Encoding.ASCII.GetBytes(head.ToString());

            if (headOnly)
                return headBytes;

            var all = new byte[headBytes.Length + body.Length];

            Buffer.BlockCopy(headBytes, 0, all, 0, headBytes.Length);
            Buffer.BlockCopy(body, 0, all, headBytes.Length, body.Length);

            return all;
        }

        private static string ReasonOf(int status) => status switch
        {
            200 => "OK",
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            405 => "Method Not Allowed",
            500 => "Internal Server Error",
            503 => "Service Unavailable",
            _ => "Status"
        };
    }
}
