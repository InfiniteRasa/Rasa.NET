using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rasa.Api
{
    using Config;

    /// <summary>
    /// The REST API (ApiConfig.Rest): GET /&lt;endpoint&gt; over HTTP on a port of its own, or
    /// over HTTPS when ApiConfig.Rest.Tls is on, answered in JSON, one request a connection.
    ///
    /// Who is answered, in this order:
    ///  - an address not on AllowedIps (when there is such a list) gets 403;
    ///  - a path that names no endpoint, or one switched off, gets 404;
    ///  - a method the endpoint is not for gets 405: GET and HEAD for most, POST for one that
    ///    takes a body (ApiEndpoint.Method);
    ///  - an endpoint that is not public (its own Public, else the API's) wants a key, in the
    ///    X-API-Key header or as "Authorization: Bearer": its own ApiKey or the API's, whichever
    ///    are set. Without one of them, 401;
    ///  - then the endpoint answers.
    ///
    /// An endpoint that changes something (ApiEndpoint.Sensitive, /addaccount) is off until its
    /// own entry under Endpoints turns it on, and is public only by a Public of its own.
    ///
    /// A POST's body is read by its Content-Length, up to <see cref="MaxBodyBytes"/>: more is
    /// 413, and one sent in chunks, with no length, is 411.
    ///
    /// It is HTTP written by hand on a TcpListener, not HttpListener: that one goes through
    /// http.sys on Windows, which wants an administrator or a URL reservation for any address
    /// but localhost.
    ///
    /// With TLS on, the port speaks TLS and nothing else (ApiTls, SslStream): a connection that
    /// does not complete the handshake is closed, an address not on AllowedIps is closed before
    /// it, and without a certificate that loads the API is off. It is never on in the clear by
    /// mistake. The certificate's files are looked at again every <see cref="TlsRecheckMs"/>
    /// and on every config reload; a renewed one is taken up, and one that will not load
    /// leaves the one in use in use. Without TLS a key crosses the network as it is.
    /// </summary>
    public sealed class ApiServer : TcpService
    {
        /// <summary>The most a request's line and headers may come to, in bytes.</summary>
        public const int MaxRequestBytes = 8192;

        /// <summary>The most a POST's body may come to, in bytes.</summary>
        public const int MaxBodyBytes = 8192;

        private readonly Dictionary<string, ApiEndpoint> _endpoints = new Dictionary<string, ApiEndpoint>(StringComparer.OrdinalIgnoreCase);

        /// <summary>How often the certificate's files are looked at for a change, in milliseconds.</summary>
        public const int TlsRecheckMs = 60000;

        private volatile RestApiConfig _config = new RestApiConfig();
        private volatile IpAllowList _allowed = IpAllowList.Open;
        private volatile ApiTls _tls;
        private readonly object _tlsSync = new object();
        private long _tlsChecked;

        /// <summary>The clock the certificate's recheck goes by, in milliseconds; replaceable for tests.</summary>
        public Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>The certificate in use; null without TLS.</summary>
        public ApiTls Tls => _tls;

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

        /// <summary>Whether a registered endpoint is currently enabled by the REST configuration.</summary>
        public bool IsEndpointEnabled(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || _config?.Enabled != true)
                return false;

            name = name.Trim('/').ToLowerInvariant();
            var endpoint = ResolveEndpoint(name);
            if (endpoint == null)
                return false;

            var settings = EndpointConfigOf(_config, name)
                ?? EndpointConfigOf(_config, endpoint.Name.Trim('/'));
            return IsEnabled(endpoint, settings);
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
                _tls = null;
                _described = null;
                return;
            }

            // The certificate before the port: with TLS asked for and none to be had, the API
            // stays shut.
            var tlsOn = config.Tls?.Enabled == true;

            if (!tlsOn)
            {
                _tls = null;
            }
            else if (RefreshTls(config, force: true) == null)
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
            var tls = _tls;

            lines.Add(tls == null
                ? "plain HTTP (Tls is off): keys cross the network as they are."
                : $"HTTPS, {ProtocolsText(tls.Protocols)}, certificate {tls.Certificate.Subject} valid to {tls.Certificate.NotAfter:yyyy-MM-dd}.");

            if (!ApiTls.TryProtocols(config.Tls?.MinimumProtocol, out _))
                lines.Add($"Tls.MinimumProtocol \"{config.Tls?.MinimumProtocol}\" is neither Tls12 nor Tls13; Tls12 it is.");

            foreach (var entry in allowed.Invalid)
                lines.Add($"AllowedIps entry \"{entry}\" is no address and no range; it allows nobody.");

            foreach (var name in Endpoints)
            {
                ApiEndpoint registered;

                lock (_endpoints)
                    _endpoints.TryGetValue(name, out registered);

                var endpoint = EndpointConfigOf(config, name);
                var state = !IsEnabled(registered, endpoint) ? "off"
                    : registered?.RequiresApiKey == false ? "endpoint auth"
                    : IsPublic(config, registered, endpoint) ? "public"
                    : HasKey(config, endpoint) ? "key"
                    : "key, and none is set: it answers nobody until an ApiKey is set or it is made public";

                lines.Add($"{(registered?.Method == "POST" ? "POST " : "")}/{name} ({state})");
            }

            lines.Add(allowed.AllowsAll ? "any address may ask." : $"{allowed.Count} allowed address(es) or range(s).");

            var described = $"{LocalEndPoint}|{string.Join("|", lines)}";

            if (described == _described)
                return;

            _described = described;

            foreach (var line in lines)
                Logger.WriteLog(line.StartsWith("AllowedIps", StringComparison.Ordinal) || line.StartsWith("Tls.", StringComparison.Ordinal)
                    ? LogType.Error
                    : LogType.Network, $"{Label}: {line}");
        }

        private string _described;

        private static string ProtocolsText(SslProtocols protocols) =>
            protocols == SslProtocols.Tls13 ? "TLS 1.3 only" : "TLS 1.2 or newer";

        /// <summary>
        /// The certificate to shake hands with: the one in use, unless the settings or the
        /// files have changed since it was loaded - looked into on every config reload
        /// (<paramref name="force"/>) and otherwise every <see cref="TlsRecheckMs"/> - when it
        /// is loaded again. One that will not load is reported, and the one in use stays; null
        /// only when there is none at all.
        /// </summary>
        private ApiTls RefreshTls(RestApiConfig config, bool force)
        {
            var now = Now();
            var current = _tls;

            if (!force && current != null && now - Interlocked.Read(ref _tlsChecked) < TlsRecheckMs)
                return current;

            lock (_tlsSync)
            {
                current = _tls;

                if (!force && current != null && now - Interlocked.Read(ref _tlsChecked) < TlsRecheckMs)
                    return current;

                Interlocked.Exchange(ref _tlsChecked, now);

                var signature = ApiTls.SignatureOf(config.Tls);

                if (current != null && current.Signature == signature)
                    return current;

                var loaded = ApiTls.Load(config.Tls, out var problem);

                if (loaded == null)
                {
                    // Said once for each state of the files, not once a minute.
                    if (problem != _tlsProblem)
                        Logger.WriteLog(LogType.Error, current == null
                            ? $"{Label}: TLS is on and {problem}. The API is off: it is not opened without TLS."
                            : $"{Label}: {problem}. The certificate already loaded stays in use.");

                    _tlsProblem = problem;

                    return current;
                }

                _tlsProblem = null;

                var certificate = loaded.Certificate;
                var today = DateTime.Now;

                if (current != null)
                    Logger.WriteLog(LogType.Network, $"{Label}: certificate loaded again: {certificate.Subject}, valid to {certificate.NotAfter:yyyy-MM-dd}.");

                if (today > certificate.NotAfter || today < certificate.NotBefore)
                    Logger.WriteLog(LogType.Error,
                        $"{Label}: the certificate {certificate.Subject} is valid from {certificate.NotBefore:yyyy-MM-dd} to {certificate.NotAfter:yyyy-MM-dd}, which today is not. Clients that check will refuse it.");

                _tls = loaded;

                return loaded;
            }
        }

        private string _tlsProblem;

        private static ApiEndpointConfig EndpointConfigOf(RestApiConfig config, string name)
        {
            if (config.Endpoints == null)
                return null;

            foreach (var entry in config.Endpoints)
            {
                var key = entry.Key?.Trim('/');
                if (string.IsNullOrEmpty(key))
                    continue;

                if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                    return entry.Value;
            }

            return null;
        }

        private ApiEndpoint ResolveEndpoint(string name)
        {
            lock (_endpoints)
            {
                if (_endpoints.TryGetValue(name, out var exact))
                    return exact;

                return _endpoints.Values.FirstOrDefault(endpoint => endpoint.Matches(name));
            }
        }

        /// <summary>On unless its entry says otherwise; a sensitive one, off unless its entry says otherwise.</summary>
        private static bool IsEnabled(ApiEndpoint endpoint, ApiEndpointConfig settings) =>
            endpoint != null && (settings?.Enabled ?? !endpoint.Sensitive);

        /// <summary>Its own Public, else the API's; a sensitive one, only its own.</summary>
        private static bool IsPublic(RestApiConfig config, ApiEndpoint endpoint, ApiEndpointConfig settings) =>
            endpoint?.Sensitive == true ? settings?.Public == true : settings?.Public ?? config.Public;

        private static bool Accepts(ApiEndpoint endpoint, string method) =>
            endpoint.Method == "POST" ? method == "POST" : method == "GET" || method == "HEAD";

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

            var name = request.EndpointName;
            var endpoint = ResolveEndpoint(name);
            var settings = endpoint == null ? null : EndpointConfigOf(config, name) ?? EndpointConfigOf(config, endpoint.Name.Trim('/'));

            if (!IsEnabled(endpoint, settings))
                return ApiResponse.Error(404, "not found");

            if (!Accepts(endpoint, request.Method))
                return new ApiResponse(405, ServerStatus.Json(writer => writer.WriteString("error", "method not allowed")))
                {
                    Allow = endpoint.Method == "POST" ? "POST" : "GET, HEAD"
                };

            if (endpoint.RequiresApiKey && !IsPublic(config, endpoint, settings))
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
            var config = _config;
            SslStream secure = null;

            if (config.Tls?.Enabled == true)
            {
                // Nobody who is not allowed gets as far as a handshake.
                if (!_allowed.Allows(remote))
                {
                    Refused($"{remote}, which is not on AllowedIps");
                    return;
                }

                var tls = RefreshTls(config, force: false);

                if (tls == null)
                    return;

                secure = new SslStream(stream, leaveInnerStreamOpen: false);

                try
                {
                    await secure.AuthenticateAsServerAsync(tls.Options, limit).ConfigureAwait(false);
                }
                catch (Exception e) when (e is AuthenticationException || e is IOException || e is InvalidOperationException
                                          || e is System.ComponentModel.Win32Exception)
                {
                    // Plain HTTP sent to the HTTPS port, a protocol too old, a scanner.
                    Refused($"{remote}: no TLS handshake ({e.Message})");
                    await secure.DisposeAsync().ConfigureAwait(false);
                    return;
                }

                stream = secure;
            }

            var buffer = new byte[MaxRequestBytes];
            var length = await ReadHead(stream, buffer, limit).ConfigureAwait(false);

            if (length == 0)
                return;

            var headEnd = EndOfHead(buffer, length);
            var request = Parse(Encoding.ASCII.GetString(buffer, 0, headEnd < 0 ? length : headEnd));
            ApiResponse response;

            if (request == null)
            {
                response = ApiResponse.Error(400, "bad request");
            }
            else
            {
                request.Remote = remote;
                response = await ReadBody(stream, request, buffer, headEnd, length, limit).ConfigureAwait(false) ?? Respond(request);
            }

            var bytes = Render(response, request?.Method == "HEAD");

            await stream.WriteAsync(bytes, 0, bytes.Length, limit).ConfigureAwait(false);
            await stream.FlushAsync(limit).ConfigureAwait(false);

            // The TLS goodbye, so the client knows the answer was whole.
            if (secure != null)
            {
                await secure.ShutdownAsync().ConfigureAwait(false);
                await secure.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Reads a request up to the blank line that ends its headers, and whatever of its body
        /// came with them. The number of bytes read: 0 when the connection closed first, and a
        /// full buffer with no blank line in it for a request longer than
        /// <see cref="MaxRequestBytes"/>, which does not parse.
        /// </summary>
        private static async Task<int> ReadHead(Stream stream, byte[] buffer, CancellationToken limit)
        {
            var length = 0;

            while (length < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer, length, buffer.Length - length, limit).ConfigureAwait(false);

                if (read <= 0)
                    break;

                length += read;

                if (EndOfHead(buffer, length) >= 0)
                    break;
            }

            return length;
        }

        /// <summary>
        /// The body of a POST into <see cref="ApiRequest.Body"/>: what arrived with the headers
        /// and the rest by Content-Length. Null when it is read (or there is none to read);
        /// otherwise the answer to a body that cannot be taken.
        /// </summary>
        private static async Task<ApiResponse> ReadBody(Stream stream, ApiRequest request, byte[] buffer, int headEnd, int length, CancellationToken limit)
        {
            if (request.Method != "POST")
                return null;

            if (request.Headers.ContainsKey("Transfer-Encoding"))
                return ApiResponse.Error(411, "length required");

            if (!request.Headers.TryGetValue("Content-Length", out var text))
                return null;

            if (!int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var wanted))
                return ApiResponse.Error(400, "bad request");

            if (wanted > MaxBodyBytes)
                return ApiResponse.Error(413, "request body too large");

            var body = new byte[wanted];
            var have = Math.Min(wanted, length - headEnd);

            Buffer.BlockCopy(buffer, headEnd, body, 0, have);

            // A client that waits to be told to go on before it sends the body.
            if (have < wanted && request.Headers.TryGetValue("Expect", out var expect)
                && string.Equals(expect, "100-continue", StringComparison.OrdinalIgnoreCase))
            {
                var goOn = Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");

                await stream.WriteAsync(goOn, 0, goOn.Length, limit).ConfigureAwait(false);
                await stream.FlushAsync(limit).ConfigureAwait(false);
            }

            while (have < wanted)
            {
                var read = await stream.ReadAsync(body, have, wanted - have, limit).ConfigureAwait(false);

                if (read <= 0)
                    return ApiResponse.Error(400, "bad request");

                have += read;
            }

            request.Body = Encoding.UTF8.GetString(body);

            return null;
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
            201 => "Created",
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            405 => "Method Not Allowed",
            409 => "Conflict",
            411 => "Length Required",
            413 => "Content Too Large",
            415 => "Unsupported Media Type",
            500 => "Internal Server Error",
            503 => "Service Unavailable",
            504 => "Gateway Timeout",
            _ => "Status"
        };
    }
}
