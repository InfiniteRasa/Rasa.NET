using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;

namespace Rasa.Api.Ingame
{
    using Auth;
    using Config;
    using Data;
    using Game;
    using Managers;

    /// <summary>
    /// Bridges the REST API to an already-authenticated game connection. The game client asks
    /// for a short-lived, one-time exchange code through .ingameapiauth; the injected UI sends
    /// that code to /ingame/session/exchange and receives a stateless JWT tied to the live game
    /// connection. Endpoint-specific authorization is deliberately handled by the endpoint.
    /// </summary>
    public sealed class IngameSessionService
    {
        /// <summary>
        /// Prefix used only on the private system-message payload carrying an exchange code to
        /// the injected client UI. The UI consumes this message before the stock chat window.
        /// </summary>
        public const string ExchangeMessagePrefix = "__RASA_INGAME_API_EXCHANGE__:";

        private static readonly TimeSpan ExchangeCodeLifetime = TimeSpan.FromSeconds(60);

        private readonly object _sync = new object();
        private readonly Dictionary<string, PendingExchange> _pending = new Dictionary<string, PendingExchange>(StringComparer.Ordinal);
        private readonly ApiJwtService _jwt = new ApiJwtService("rasa-ingame-api");
        private bool _available;
        private int _tokenLifetimeSeconds = 86400;

        public void Apply(RestApiConfig config)
        {
            config ??= new RestApiConfig();

            lock (_sync)
            {
                _available = false;
                _pending.Clear();

                if (!config.Enabled)
                    return;

                if (config.JwtTokenLifetimeSeconds <= 0)
                {
                    Logger.WriteLog(LogType.Error, "REST API: JwtTokenLifetimeSeconds must be greater than zero; in-game authentication is off.");
                    return;
                }

                if (!_jwt.ApplySecret(config.JwtSecret))
                {
                    Logger.WriteLog(LogType.Error, "REST API: JwtSecret must be at least 32 UTF-8 bytes when set; in-game authentication is off.");
                    return;
                }

                _tokenLifetimeSeconds = config.JwtTokenLifetimeSeconds;
                _available = true;
            }
        }

        /// <summary>
        /// Creates a one-time exchange code for a live Admin-or-higher game connection.
        /// The code is intentionally not an API session itself and expires quickly.
        /// </summary>
        public bool TryCreateExchangeCode(Client client, out string exchangeCode)
        {
            exchangeCode = null;

            if (!IsEligibleSessionClient(client))
                return false;

            lock (_sync)
            {
                if (!_available)
                    return false;

                RemoveExpiredLocked();

                exchangeCode = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                _pending[exchangeCode] = new PendingExchange(
                    client,
                    client.Socket.RemoteAddress,
                    DateTime.UtcNow.Add(ExchangeCodeLifetime));
                return true;
            }
        }

        /// <summary>
        /// Consumes a code created by .ingameapiauth and issues a JWT for the same live game
        /// connection. The HTTP request must come from the same remote address as the game
        /// connection, and the account must still be Admin-or-higher when the exchange occurs.
        /// </summary>
        public bool TryExchange(IPAddress remoteAddress, string exchangeCode, out string token, out string error)
        {
            token = null;
            error = "The in-game API exchange code is invalid or has expired.";

            if (remoteAddress == null || string.IsNullOrWhiteSpace(exchangeCode))
                return false;

            Client client;
            int lifetime;

            lock (_sync)
            {
                if (!_available)
                {
                    error = "In-game API authentication is not available.";
                    return false;
                }

                RemoveExpiredLocked();

                if (!_pending.TryGetValue(exchangeCode, out var pending))
                    return false;

                // A code is one-shot even when somebody tries to exchange it incorrectly.
                _pending.Remove(exchangeCode);

                if (!pending.RemoteAddress.Equals(remoteAddress))
                    return false;

                client = pending.Client;
                if (!IsEligibleSessionClient(client))
                {
                    error = "The approving game session is no longer available or no longer has Admin access.";
                    return false;
                }

                lifetime = _tokenLifetimeSeconds;
            }

            token = _jwt.CreateToken(new Dictionary<string, object>
            {
                ["sub"] = client.AccountEntry.Id.ToString(),
                ["cid"] = client.ConnectionId.ToString("N"),
                ["ip"] = remoteAddress.ToString()
            }, lifetime);
            return true;
        }

        /// <summary>
        /// Validates a JWT and resolves it back to the same currently-connected game client.
        /// Every in-game API session has an Admin-or-higher baseline; individual endpoints may
        /// impose an additional command permission such as .giveitem.
        /// </summary>
        public bool TryAuthenticate(ApiRequest request, out Client client)
        {
            client = null;
            if (request?.Remote == null
                || !request.Headers.TryGetValue("Authorization", out var authorization)
                || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return false;

            lock (_sync)
                if (!_available)
                    return false;

            var token = authorization.Substring("Bearer ".Length).Trim();
            if (!_jwt.TryValidate(token, out var payload)
                || !TrySessionClaims(payload, request.Remote, out var accountId, out var connectionId))
                return false;

            lock (Server.Clients)
            {
                client = Server.Clients.FirstOrDefault(candidate =>
                    candidate.ConnectionId == connectionId
                    && candidate.AccountEntry?.Id == accountId
                    && candidate.Socket?.RemoteAddress != null
                    && candidate.Socket.RemoteAddress.Equals(request.Remote)
                    && IsEligibleSessionClient(candidate));
            }

            return client != null;
        }

        private static bool IsEligibleSessionClient(Client client) =>
            client?.Socket?.RemoteAddress != null
            && client.State == ClientState.Ingame
            && ChatCommandsManager.HasLevel(client, GmLevel.Admin);

        private void RemoveExpiredLocked()
        {
            var now = DateTime.UtcNow;
            foreach (var code in _pending
                .Where(pair => pair.Value.ExpiresAtUtc <= now || !IsEligibleSessionClient(pair.Value.Client))
                .Select(pair => pair.Key)
                .ToArray())
                _pending.Remove(code);
        }

        private static bool TrySessionClaims(JsonElement payload, IPAddress remoteAddress, out uint accountId, out Guid connectionId)
        {
            accountId = 0;
            connectionId = Guid.Empty;

            return payload.TryGetProperty("sub", out var subject)
                && uint.TryParse(subject.GetString(), out accountId)
                && payload.TryGetProperty("cid", out var connection)
                && Guid.TryParseExact(connection.GetString(), "N", out connectionId)
                && payload.TryGetProperty("ip", out var ip)
                && string.Equals(ip.GetString(), remoteAddress.ToString(), StringComparison.Ordinal);
        }

        private sealed class PendingExchange
        {
            public PendingExchange(Client client, IPAddress remoteAddress, DateTime expiresAtUtc)
            {
                Client = client;
                RemoteAddress = remoteAddress;
                ExpiresAtUtc = expiresAtUtc;
            }

            public Client Client { get; }
            public IPAddress RemoteAddress { get; }
            public DateTime ExpiresAtUtc { get; }
        }
    }
}
