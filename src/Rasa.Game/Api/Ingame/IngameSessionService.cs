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

    /// <summary>
    /// Bridges an HTTP caller to an already-authenticated game connection. Challenges are
    /// approved from inside the game, then exchanged for a JWT tied to that live connection and
    /// remote address. Endpoint authorization is deliberately not done here.
    /// </summary>
    public sealed class IngameSessionService
    {
        private readonly object _sync = new object();
        private readonly Dictionary<string, PendingChallenge> _pending = new Dictionary<string, PendingChallenge>(StringComparer.Ordinal);
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
                    Logger.WriteLog(LogType.Error, $"REST API: JwtTokenLifetimeSeconds must be greater than zero; in-game authentication is off.");
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

        public string CreateChallenge(IPAddress remoteAddress)
        {
            if (remoteAddress == null)
                return null;

            lock (_sync)
            {
                if (!_available)
                    return null;

                var challenge = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
                _pending[challenge] = new PendingChallenge(remoteAddress);
                return challenge;
            }
        }

        public bool AuthorizeChallenge(Client client, string challenge)
        {
            if (client?.Socket?.RemoteAddress == null
                || client.State != ClientState.Ingame
                || client.AccountEntry == null
                || string.IsNullOrWhiteSpace(challenge))
                return false;

            lock (_sync)
            {
                if (!_available
                    || !_pending.TryGetValue(challenge, out var pending)
                    || !pending.RemoteAddress.Equals(client.Socket.RemoteAddress))
                    return false;

                pending.Client = client;
                return true;
            }
        }

        public bool TryExchange(IPAddress remoteAddress, string challenge, out string token, out string error)
        {
            token = null;
            error = "The in-game session has not approved this challenge yet.";

            if (remoteAddress == null || string.IsNullOrWhiteSpace(challenge))
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

                if (!_pending.TryGetValue(challenge, out var pending)
                    || !pending.RemoteAddress.Equals(remoteAddress)
                    || pending.Client == null)
                    return false;

                client = pending.Client;
                if (client.State != ClientState.Ingame || client.AccountEntry == null)
                {
                    _pending.Remove(challenge);
                    error = "The approving game session is no longer available.";
                    return false;
                }

                _pending.Remove(challenge);
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
                    && candidate.State == ClientState.Ingame
                    && candidate.AccountEntry?.Id == accountId
                    && candidate.Socket?.RemoteAddress != null
                    && candidate.Socket.RemoteAddress.Equals(request.Remote));
            }

            return client != null;
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

        private sealed class PendingChallenge
        {
            public PendingChallenge(IPAddress remoteAddress) => RemoteAddress = remoteAddress;
            public IPAddress RemoteAddress { get; }
            public Client Client { get; set; }
        }
    }
}
