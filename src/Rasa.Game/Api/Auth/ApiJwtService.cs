using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Rasa.Api.Auth
{
    /// <summary>
    /// Small HS256 JWT signer/validator used by API authentication schemes. The configured
    /// secret is optional: when it is empty a process-local random secret is generated and kept
    /// until shutdown. A configured secret must be at least 32 UTF-8 bytes.
    /// </summary>
    public sealed class ApiJwtService
    {
        private readonly object _sync = new object();
        private readonly string _issuer;
        private byte[] _secret;
        private string _configuredSecret;

        public ApiJwtService(string issuer)
        {
            _issuer = string.IsNullOrWhiteSpace(issuer) ? throw new ArgumentException("An issuer is required.", nameof(issuer)) : issuer;
        }

        public bool ApplySecret(string configuredSecret)
        {
            configuredSecret ??= string.Empty;

            lock (_sync)
            {
                if (configuredSecret.Length == 0)
                {
                    if (_secret != null && _configuredSecret == string.Empty)
                        return true;

                    ReplaceSecret(RandomNumberGenerator.GetBytes(32), string.Empty);
                    return true;
                }

                var bytes = Encoding.UTF8.GetBytes(configuredSecret);
                if (bytes.Length < 32)
                {
                    CryptographicOperations.ZeroMemory(bytes);
                    return false;
                }

                if (_secret != null && string.Equals(_configuredSecret, configuredSecret, StringComparison.Ordinal))
                {
                    CryptographicOperations.ZeroMemory(bytes);
                    return true;
                }

                ReplaceSecret(bytes, configuredSecret);
                return true;
            }
        }

        public string CreateToken(IReadOnlyDictionary<string, object> claims, int lifetimeSeconds)
        {
            if (lifetimeSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(lifetimeSeconds));

            byte[] secret;
            lock (_sync)
                secret = _secret == null ? null : (byte[])_secret.Clone();

            if (secret == null)
                throw new InvalidOperationException("JWT signing is not configured.");

            try
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var payload = new Dictionary<string, object>
                {
                    ["iss"] = _issuer,
                    ["iat"] = now,
                    ["exp"] = checked(now + lifetimeSeconds)
                };

                if (claims != null)
                    foreach (var claim in claims)
                        payload[claim.Key] = claim.Value;

                var header = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["alg"] = "HS256",
                    ["typ"] = "JWT"
                })));
                var body = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
                var signingInput = $"{header}.{body}";

                using var hmac = new HMACSHA256(secret);
                var signature = hmac.ComputeHash(Encoding.ASCII.GetBytes(signingInput));
                return $"{signingInput}.{Base64UrlEncode(signature)}";
            }
            finally
            {
                CryptographicOperations.ZeroMemory(secret);
            }
        }

        public bool TryValidate(string token, out JsonElement payload)
        {
            payload = default;

            if (string.IsNullOrWhiteSpace(token))
                return false;

            byte[] secret;
            lock (_sync)
                secret = _secret == null ? null : (byte[])_secret.Clone();

            if (secret == null)
                return false;

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

                using var document = JsonDocument.Parse(payloadBytes);
                var root = document.RootElement;

                if (!root.TryGetProperty("iss", out var issuer)
                    || !string.Equals(issuer.GetString(), _issuer, StringComparison.Ordinal)
                    || !root.TryGetProperty("exp", out var expiration)
                    || !expiration.TryGetInt64(out var expiresAt)
                    || expiresAt <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    return false;

                payload = root.Clone();
                return true;
            }
            catch (Exception)
            {
                payload = default;
                return false;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(secret);
            }
        }

        private void ReplaceSecret(byte[] secret, string configuredSecret)
        {
            if (_secret != null)
                CryptographicOperations.ZeroMemory(_secret);

            _secret = secret;
            _configuredSecret = configuredSecret;
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
    }
}
