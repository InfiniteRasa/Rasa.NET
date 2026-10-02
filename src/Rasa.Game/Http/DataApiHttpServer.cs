using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Rasa.Http
{
    using Config;
    using Game;
    using Data;
    using Managers;
    using Structures;

    /// <summary>
    /// Small read-only HTTP server for development/tooling data. It intentionally
    /// exposes no mutation routes; resource-specific changes remain on the
    /// existing authenticated game and GM command paths.
    /// </summary>
    public sealed class DataApiHttpServer
    {
        private static readonly Lazy<DataApiHttpServer> LazyInstance =
            new(() => new DataApiHttpServer());

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        private readonly object _lock = new();
        private readonly object _authLock = new();
        private readonly Dictionary<string, PendingChallenge> _pendingChallenges = new(StringComparer.Ordinal);
        private TcpListener _listener;
        private CancellationTokenSource _cancellation;
        private Task _acceptTask;
        private string _bindAddress;
        private int _port;
        private int _backlog;
        private byte[] _jwtSecret;
        private int _tokenLifetimeSeconds;

        public static DataApiHttpServer Instance => LazyInstance.Value;

        private DataApiHttpServer()
        {
        }

        public void Apply(DataApiConfig config)
        {
            config ??= new DataApiConfig();

            lock (_lock)
            {
                if (!config.Enabled)
                {
                    StopLocked();
                    Logger.WriteLog(LogType.Initialize, "Data API is disabled (DataApiConfig.Enabled)." );
                    return;
                }

                var bindAddress = string.IsNullOrWhiteSpace(config.BindAddress)
                    ? "0.0.0.0"
                    : config.BindAddress.Trim();
                var port = config.Port;
                var backlog = config.Backlog > 0 ? config.Backlog : 64;
                var jwtSecret = config.JwtSecret ?? string.Empty;
                var tokenLifetimeSeconds = config.TokenLifetimeSeconds;

                if (port <= 0 || port > 65535)
                {
                    StopLocked();
                    Logger.WriteLog(LogType.Error, $"Invalid DataApiConfig.Port: {port}. Data API is off.");
                    return;
                }

                if (Encoding.UTF8.GetByteCount(jwtSecret) < 32)
                {
                    StopLocked();
                    Logger.WriteLog(LogType.Error,
                        "DataApiConfig.JwtSecret must be at least 32 UTF-8 bytes. Data API is off.");
                    return;
                }

                if (tokenLifetimeSeconds <= 0)
                {
                    StopLocked();
                    Logger.WriteLog(LogType.Error,
                        $"Invalid DataApiConfig.TokenLifetimeSeconds: {tokenLifetimeSeconds}. Data API is off.");
                    return;
                }

                var jwtSecretBytes = Encoding.UTF8.GetBytes(jwtSecret);
                if (_listener != null
                    && string.Equals(_bindAddress, bindAddress, StringComparison.OrdinalIgnoreCase)
                    && _port == port
                    && _backlog == backlog
                    && _tokenLifetimeSeconds == tokenLifetimeSeconds
                    && _jwtSecret != null
                    && CryptographicOperations.FixedTimeEquals(_jwtSecret, jwtSecretBytes))
                {
                    CryptographicOperations.ZeroMemory(jwtSecretBytes);
                    return;
                }

                StopLocked();

                if (!TryResolveBindAddress(bindAddress, out var address))
                {
                    Logger.WriteLog(LogType.Error, $"Invalid DataApiConfig.BindAddress: {bindAddress}. Data API is off.");
                    return;
                }

                try
                {
                    _listener = new TcpListener(address, port);
                    _listener.Start(backlog);
                    _bindAddress = bindAddress;
                    _port = port;
                    _backlog = backlog;
                    _jwtSecret = jwtSecretBytes;
                    _tokenLifetimeSeconds = tokenLifetimeSeconds;
                    _cancellation = new CancellationTokenSource();
                    _acceptTask = Task.Run(() => AcceptLoop(_listener, _cancellation.Token));

                    Logger.WriteLog(LogType.Network,
                        $"*** Data API listening on {bindAddress}:{port} (read-only)");
                }
                catch (Exception e)
                {
                    StopLocked();
                    Logger.WriteLog(LogType.Error, $"Unable to start Data API on {bindAddress}:{port}: {e}");
                }
            }
        }

        public void Stop()
        {
            lock (_lock)
                StopLocked();
        }

        private void StopLocked()
        {
            try
            {
                _cancellation?.Cancel();
            }
            catch
            {
            }

            try
            {
                _listener?.Stop();
            }
            catch
            {
            }

            _listener = null;
            lock (_authLock)
            {
                _pendingChallenges.Clear();
            }

            _cancellation?.Dispose();
            _cancellation = null;
            _acceptTask = null;
            _bindAddress = null;
            _port = 0;
            _backlog = 0;
            if (_jwtSecret != null)
            {
                CryptographicOperations.ZeroMemory(_jwtSecret);
                _jwtSecret = null;
            }
            _tokenLifetimeSeconds = 0;
        }

        private static bool TryResolveBindAddress(string value, out IPAddress address)
        {
            if (value == "*" || value == "+" || value == "0.0.0.0")
            {
                address = IPAddress.Any;
                return true;
            }

            if (string.Equals(value, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                address = IPAddress.Loopback;
                return true;
            }

            return IPAddress.TryParse(value, out address);
        }

        private void AcceptLoop(TcpListener listener, CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                try
                {
                    var client = listener.AcceptTcpClient();
                    _ = Task.Run(() => HandleClient(client));
                }
                catch (SocketException)
                {
                    if (!cancellation.IsCancellationRequested)
                        Logger.WriteLog(LogType.Error, "Data API accept failed.");
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception e)
                {
                    if (!cancellation.IsCancellationRequested)
                        Logger.WriteLog(LogType.Error, $"Data API accept failed: {e}");
                }
            }
        }

        public bool AuthorizeChallenge(Client client, string challenge)
        {
            if (client?.Socket == null || string.IsNullOrWhiteSpace(challenge))
                return false;

            lock (_lock)
            {
                if (_listener == null)
                    return false;
            }

            if (!ChatCommandsManager.Instance.CanUseCommand(client, ".giveitem"))
                return false;

            lock (_authLock)
            {
                if (!_pendingChallenges.TryGetValue(challenge, out var pending)
                    || !pending.RemoteAddress.Equals(client.Socket.RemoteAddress))
                    return false;

                pending.Client = client;
                return true;
            }
        }

        private void HandleClient(TcpClient client)
        {
            using (client)
            {
                client.ReceiveTimeout = 5000;
                client.SendTimeout = 5000;

                try
                {
                    using var stream = client.GetStream();
                    using var reader = new StreamReader(stream, Encoding.ASCII, false, 4096, true);

                    var requestLine = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(requestLine))
                        return;

                    var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    string header;
                    while (!string.IsNullOrEmpty(header = reader.ReadLine()))
                    {
                        var colon = header.IndexOf(':');
                        if (colon <= 0)
                            continue;

                        headers[header.Substring(0, colon).Trim()] = header.Substring(colon + 1).Trim();
                    }

                    var requestParts = requestLine.Split(' ');
                    if (requestParts.Length < 2)
                    {
                        WriteJson(stream, 400, new ErrorResponse("Malformed request."));
                        return;
                    }

                    if (!string.Equals(requestParts[0], "GET", StringComparison.OrdinalIgnoreCase))
                    {
                        WriteJson(stream, 405, new ErrorResponse("This API is read-only; only GET is supported."), "Allow: GET\r\n");
                        return;
                    }

                    var remoteAddress = (client.Client.RemoteEndPoint as IPEndPoint)?.Address;
                    Route(stream, requestParts[1], headers, remoteAddress);
                }
                catch (IOException)
                {
                    // The caller went away or timed out. Nothing to do.
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Data API request failed: {e}");
                }
            }
        }

        private void Route(NetworkStream stream, string rawTarget, Dictionary<string, string> headers, IPAddress remoteAddress)
        {
            var queryIndex = rawTarget.IndexOf('?');
            var rawPath = queryIndex >= 0 ? rawTarget.Substring(0, queryIndex) : rawTarget;
            var rawQuery = queryIndex >= 0 ? rawTarget.Substring(queryIndex + 1) : string.Empty;
            var path = Uri.UnescapeDataString(rawPath).TrimEnd('/');

            if (path.Length == 0)
                path = "/";

            if (string.Equals(path, "/api/session/challenge", StringComparison.OrdinalIgnoreCase))
            {
                if (remoteAddress == null)
                {
                    WriteJson(stream, 400, new ErrorResponse("Unable to identify the caller."));
                    return;
                }

                WriteJson(stream, 200, new ChallengeResponse(CreateChallenge(remoteAddress)));
                return;
            }

            if (string.Equals(path, "/api/session/exchange", StringComparison.OrdinalIgnoreCase))
            {
                var query = ParseQuery(rawQuery);
                if (!query.TryGetValue("challenge", out var challenge) || string.IsNullOrWhiteSpace(challenge))
                {
                    WriteJson(stream, 400, new ErrorResponse("challenge is required."));
                    return;
                }

                if (!TryExchangeChallenge(remoteAddress, challenge, out var token, out var exchangeError))
                {
                    WriteJson(stream, 409, new ErrorResponse(exchangeError));
                    return;
                }

                WriteJson(stream, 200, new SessionResponse(token));
                return;
            }

            if (!TryAuthenticate(headers, remoteAddress, out var authenticatedClient))
            {
                WriteJson(stream, 401, new ErrorResponse("A valid in-game Data API session is required."));
                return;
            }

            if (!ChatCommandsManager.Instance.CanUseCommand(authenticatedClient, ".giveitem"))
            {
                WriteJson(stream, 403, new ErrorResponse("This account does not have permission to use .giveitem."));
                return;
            }

            if (string.Equals(path, "/api/items/categories", StringComparison.OrdinalIgnoreCase))
            {
                WriteJson(stream, 200, Enum.GetNames(typeof(InventoryCategory)));
                return;
            }

            if (string.Equals(path, "/api/items", StringComparison.OrdinalIgnoreCase))
            {
                HandleItemSearch(stream, ParseQuery(rawQuery));
                return;
            }

            const string detailPrefix = "/api/items/";
            if (path.StartsWith(detailPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var idText = path.Substring(detailPrefix.Length);
                if (!uint.TryParse(idText, out var templateId))
                {
                    WriteJson(stream, 400, new ErrorResponse("Item template id must be an unsigned integer."));
                    return;
                }

                HandleItemDetails(stream, templateId);
                return;
            }

            WriteJson(stream, 404, new ErrorResponse("Not found."));
        }

        private string CreateChallenge(IPAddress remoteAddress)
        {
            var challenge = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
            lock (_authLock)
                _pendingChallenges[challenge] = new PendingChallenge(remoteAddress);
            return challenge;
        }

        private bool TryExchangeChallenge(IPAddress remoteAddress, string challenge, out string token, out string error)
        {
            token = null;
            error = "The in-game session has not approved this challenge yet.";

            if (remoteAddress == null)
                return false;

            byte[] jwtSecret;
            int tokenLifetimeSeconds;
            lock (_lock)
            {
                if (_listener == null || _jwtSecret == null)
                {
                    error = "The Data API is not available.";
                    return false;
                }

                jwtSecret = (byte[])_jwtSecret.Clone();
                tokenLifetimeSeconds = _tokenLifetimeSeconds;
            }

            try
            {
                Client client;

                lock (_authLock)
                {
                    if (!_pendingChallenges.TryGetValue(challenge, out var pending)
                        || !pending.RemoteAddress.Equals(remoteAddress)
                        || pending.Client == null)
                        return false;

                    client = pending.Client;
                    if (client.State != ClientState.Ingame
                        || client.AccountEntry == null
                        || !ChatCommandsManager.Instance.CanUseCommand(client, ".giveitem"))
                    {
                        _pendingChallenges.Remove(challenge);
                        error = "The approving game session is no longer authorized.";
                        return false;
                    }

                    _pendingChallenges.Remove(challenge);
                }

                token = CreateJwt(
                    client.AccountEntry.Id,
                    client.ConnectionId,
                    remoteAddress,
                    jwtSecret,
                    tokenLifetimeSeconds);
                return true;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(jwtSecret);
            }
        }

        private bool TryAuthenticate(Dictionary<string, string> headers, IPAddress remoteAddress, out Client client)
        {
            client = null;
            if (remoteAddress == null
                || !headers.TryGetValue("Authorization", out var authorization)
                || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return false;

            var token = authorization.Substring("Bearer ".Length).Trim();
            if (token.Length == 0)
                return false;

            byte[] jwtSecret;
            lock (_lock)
            {
                if (_listener == null || _jwtSecret == null)
                    return false;
                jwtSecret = (byte[])_jwtSecret.Clone();
            }

            try
            {
                if (!TryValidateJwt(token, remoteAddress, jwtSecret, out var accountId, out var connectionId))
                    return false;

                lock (Server.Clients)
                {
                    client = Server.Clients.FirstOrDefault(candidate =>
                        candidate.ConnectionId == connectionId
                        && candidate.State == ClientState.Ingame
                        && candidate.AccountEntry?.Id == accountId
                        && candidate.Socket?.RemoteAddress != null
                        && candidate.Socket.RemoteAddress.Equals(remoteAddress));
                }

                if (client == null || !ChatCommandsManager.Instance.CanUseCommand(client, ".giveitem"))
                {
                    client = null;
                    return false;
                }

                return true;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(jwtSecret);
            }
        }

        private static string CreateJwt(
            uint accountId,
            Guid connectionId,
            IPAddress remoteAddress,
            byte[] secret,
            int lifetimeSeconds)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var headerJson = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["alg"] = "HS256",
                ["typ"] = "JWT"
            });
            var payloadJson = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["iss"] = "rasa-data-api",
                ["sub"] = accountId.ToString(),
                ["cid"] = connectionId.ToString("N"),
                ["ip"] = remoteAddress.ToString(),
                ["iat"] = now,
                ["exp"] = checked(now + lifetimeSeconds)
            });

            var header = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            var payload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
            var signingInput = $"{header}.{payload}";

            using var hmac = new HMACSHA256(secret);
            var signature = hmac.ComputeHash(Encoding.ASCII.GetBytes(signingInput));
            return $"{signingInput}.{Base64UrlEncode(signature)}";
        }

        private static bool TryValidateJwt(
            string token,
            IPAddress remoteAddress,
            byte[] secret,
            out uint accountId,
            out Guid connectionId)
        {
            accountId = 0;
            connectionId = Guid.Empty;

            try
            {
                var parts = token.Split('.');
                if (parts.Length != 3)
                    return false;

                var headerBytes = Base64UrlDecode(parts[0]);
                var payloadBytes = Base64UrlDecode(parts[1]);
                var suppliedSignature = Base64UrlDecode(parts[2]);

                using (var header = JsonDocument.Parse(headerBytes))
                {
                    if (!header.RootElement.TryGetProperty("alg", out var algorithm)
                        || !string.Equals(algorithm.GetString(), "HS256", StringComparison.Ordinal))
                        return false;
                }

                using var hmac = new HMACSHA256(secret);
                var expectedSignature = hmac.ComputeHash(Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}"));
                if (suppliedSignature.Length != expectedSignature.Length
                    || !CryptographicOperations.FixedTimeEquals(suppliedSignature, expectedSignature))
                    return false;

                using var payload = JsonDocument.Parse(payloadBytes);
                var root = payload.RootElement;

                if (!root.TryGetProperty("iss", out var issuer)
                    || !string.Equals(issuer.GetString(), "rasa-data-api", StringComparison.Ordinal)
                    || !root.TryGetProperty("sub", out var subject)
                    || !uint.TryParse(subject.GetString(), out accountId)
                    || !root.TryGetProperty("cid", out var connection)
                    || !Guid.TryParseExact(connection.GetString(), "N", out connectionId)
                    || !root.TryGetProperty("ip", out var ip)
                    || !string.Equals(ip.GetString(), remoteAddress.ToString(), StringComparison.Ordinal)
                    || !root.TryGetProperty("exp", out var expiration)
                    || !expiration.TryGetInt64(out var expiresAt))
                    return false;

                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return expiresAt > now;
            }
            catch (Exception)
            {
                accountId = 0;
                connectionId = Guid.Empty;
                return false;
            }
        }

        private static string Base64UrlEncode(byte[] value) =>
            Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] Base64UrlDecode(string value)
        {
            var base64 = value.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;
                case 3:
                    base64 += "=";
                    break;
                case 1:
                    throw new FormatException("Invalid Base64URL value.");
            }

            return Convert.FromBase64String(base64);
        }

        private static void HandleItemSearch(NetworkStream stream, Dictionary<string, string> query)
        {
            if (!query.TryGetValue("category", out var categoryText) || string.IsNullOrWhiteSpace(categoryText))
            {
                WriteJson(stream, 400, new ErrorResponse("category is required."));
                return;
            }

            if (!Enum.TryParse(categoryText, true, out InventoryCategory category)
                || !Enum.IsDefined(typeof(InventoryCategory), category))
            {
                WriteJson(stream, 400, new ErrorResponse(
                    $"Unknown category '{categoryText}'. Expected one of: {string.Join(", ", Enum.GetNames(typeof(InventoryCategory)))}."));
                return;
            }

            query.TryGetValue("search", out var search);
            search = search?.Trim() ?? string.Empty;

            var items = new List<ItemSummary>();

            foreach (var pair in ItemManager.Instance.ItemTemplateItemClass)
            {
                var templateId = pair.Key;
                var classId = pair.Value;

                if (!EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(classId, out var entityClass))
                    continue;

                if (!entityClass.ItemTemplates.TryGetValue(templateId, out var template))
                    continue;

                if (template.InventoryCategory != category)
                    continue;

                var name = entityClass.ClassName ?? string.Empty;
                if (search.Length > 0 && name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                items.Add(ToSummary(template, entityClass));
            }

            items.Sort((left, right) =>
            {
                var byName = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
                return byName != 0 ? byName : left.TemplateId.CompareTo(right.TemplateId);
            });

            WriteJson(stream, 200, new ItemSearchResponse(category.ToString(), search, items));
        }

        private static void HandleItemDetails(NetworkStream stream, uint templateId)
        {
            if (!ItemManager.Instance.ItemTemplateItemClass.TryGetValue(templateId, out var classId)
                || !EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(classId, out var entityClass)
                || !entityClass.ItemTemplates.TryGetValue(templateId, out var template))
            {
                WriteJson(stream, 404, new ErrorResponse($"Unknown item template id {templateId}."));
                return;
            }

            WriteJson(stream, 200, ToDetails(template, entityClass));
        }

        private static ItemSummary ToSummary(ItemTemplate template, EntityClass entityClass)
        {
            return new ItemSummary(
                template.ItemTemplateId,
                entityClass.ClassId,
                entityClass.ClassName ?? string.Empty,
                template.InventoryCategory.ToString(),
                entityClass.ItemClassInfo?.StackSize ?? 0,
                template.QualityId);
        }

        private static ItemDetails ToDetails(ItemTemplate template, EntityClass entityClass)
        {
            var itemClass = entityClass.ItemClassInfo;
            var equipable = entityClass.EquipableClassInfo;
            var weaponClass = entityClass.WeaponClassInfo;
            var weapon = template.WeaponInfo;

            var requirements = template.ItemInfo?.Requirements == null
                ? new Dictionary<string, int>()
                : template.ItemInfo.Requirements.ToDictionary(pair => pair.Key.ToString(), pair => pair.Value);

            return new ItemDetails
            {
                TemplateId = template.ItemTemplateId,
                ItemClassId = entityClass.ClassId,
                Name = entityClass.ClassName ?? string.Empty,
                Category = template.InventoryCategory.ToString(),
                StackSize = itemClass?.StackSize ?? 0,
                QualityId = template.QualityId,
                InventoryIconStringId = itemClass?.InventoryIconStringId ?? 0,
                LootValue = itemClass?.LootValue ?? 0,
                MaxHitPoints = itemClass?.MaxHitPoints ?? 0,
                IsConsumable = itemClass?.IsConsumableFlag != 0,
                BuyPrice = template.BuyPrice,
                SellPrice = template.SellPrice,
                ArmorValue = template.ArmorValue,
                BoundToCharacter = template.BoundToCharacter,
                BindOnEquip = template.HasBoEFlag,
                Sellable = template.HasSellableFlag,
                CharacterUnique = template.HasCharacterUniqueFlag,
                AccountUnique = template.HasAccountUniqueFlag,
                NotTradable = template.NotTradable,
                NotPlaceableInLockbox = template.NotPlaceableInLockbox,
                EquipmentSlot = equipable?.EquipmentSlotId.ToString(),
                RequiredSkillId = template.EquipableInfo?.SkillId,
                RequiredSkillLevel = template.EquipableInfo?.SkillLevel,
                Requirements = requirements,
                Weapon = weaponClass == null && weapon == null
                    ? null
                    : new WeaponDetails
                    {
                        MinDamage = weaponClass?.MinDamage,
                        MaxDamage = weaponClass?.MaxDamage,
                        DamageType = weaponClass?.DamageType,
                        AmmoClassId = weaponClass == null ? null : (uint?)weaponClass.AmmoClassId,
                        ClipSize = weaponClass?.ClipSize,
                        AmmoPerShot = weapon?.AmmoPerShot,
                        Range = weapon?.Range,
                        Windup = weapon?.Windup,
                        Recovery = weapon?.Recovery,
                        Refire = weapon?.Refire,
                        ReloadTime = weapon?.ReloadTime
                    }
            };
        }

        private static Dictionary<string, string> ParseQuery(string rawQuery)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(rawQuery))
                return result;

            foreach (var part in rawQuery.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var equals = part.IndexOf('=');
                var rawKey = equals >= 0 ? part.Substring(0, equals) : part;
                var rawValue = equals >= 0 ? part.Substring(equals + 1) : string.Empty;
                var key = Uri.UnescapeDataString(rawKey.Replace('+', ' '));
                var value = Uri.UnescapeDataString(rawValue.Replace('+', ' '));
                result[key] = value;
            }

            return result;
        }

        private static void WriteJson(NetworkStream stream, int statusCode, object value, string extraHeaders = "")
        {
            var body = JsonSerializer.Serialize(value, JsonOptions);
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            var reason = statusCode switch
            {
                200 => "OK",
                400 => "Bad Request",
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "Not Found",
                405 => "Method Not Allowed",
                409 => "Conflict",
                _ => "Error"
            };

            var headers = Encoding.ASCII.GetBytes(
                $"HTTP/1.1 {statusCode} {reason}\r\n"
                + "Content-Type: application/json; charset=utf-8\r\n"
                + $"Content-Length: {bodyBytes.Length}\r\n"
                + "Connection: close\r\n"
                + "Cache-Control: no-store\r\n"
                + extraHeaders
                + "\r\n");

            stream.Write(headers, 0, headers.Length);
            stream.Write(bodyBytes, 0, bodyBytes.Length);
            stream.Flush();
        }

        private sealed class PendingChallenge
        {
            public PendingChallenge(IPAddress remoteAddress)
            {
                RemoteAddress = remoteAddress;
            }

            public IPAddress RemoteAddress { get; }
            public Client Client { get; set; }
        }

        private sealed record ErrorResponse(string Error);
        private sealed record ChallengeResponse(string Challenge);
        private sealed record SessionResponse(string Token);
        private sealed record ItemSearchResponse(string Category, string Search, List<ItemSummary> Items);
        private sealed record ItemSummary(uint TemplateId, uint ItemClassId, string Name, string Category, uint StackSize, int QualityId);

        private sealed class ItemDetails
        {
            public uint TemplateId { get; set; }
            public uint ItemClassId { get; set; }
            public string Name { get; set; }
            public string Category { get; set; }
            public uint StackSize { get; set; }
            public int QualityId { get; set; }
            public uint InventoryIconStringId { get; set; }
            public uint LootValue { get; set; }
            public int MaxHitPoints { get; set; }
            public bool IsConsumable { get; set; }
            public int BuyPrice { get; set; }
            public int SellPrice { get; set; }
            public int ArmorValue { get; set; }
            public bool BoundToCharacter { get; set; }
            public bool BindOnEquip { get; set; }
            public bool Sellable { get; set; }
            public bool CharacterUnique { get; set; }
            public bool AccountUnique { get; set; }
            public bool NotTradable { get; set; }
            public bool NotPlaceableInLockbox { get; set; }
            public string EquipmentSlot { get; set; }
            public int? RequiredSkillId { get; set; }
            public int? RequiredSkillLevel { get; set; }
            public Dictionary<string, int> Requirements { get; set; }
            public WeaponDetails Weapon { get; set; }
        }

        private sealed class WeaponDetails
        {
            public int? MinDamage { get; set; }
            public int? MaxDamage { get; set; }
            public byte? DamageType { get; set; }
            public uint? AmmoClassId { get; set; }
            public uint? ClipSize { get; set; }
            public uint? AmmoPerShot { get; set; }
            public uint? Range { get; set; }
            public uint? Windup { get; set; }
            public uint? Recovery { get; set; }
            public uint? Refire { get; set; }
            public uint? ReloadTime { get; set; }
        }
    }
}
