using System;
using System.Text.Json;

namespace Rasa.Api.Ingame
{
    /// <summary>
    /// POST /ingame/session/exchange with {"exchangeCode":"..."}. The code must have been
    /// created moments earlier by .ingameapiauth on an Admin-or-higher live game connection.
    /// </summary>
    public sealed class IngameSessionExchangeEndpoint : ApiEndpoint
    {
        private readonly IngameSessionService _sessions;

        public IngameSessionExchangeEndpoint(IngameSessionService sessions) => _sessions = sessions;

        public override string Name => "ingame/session/exchange";
        public override string Method => "POST";
        public override bool RequiresApiKey => false;
        public override bool Sensitive => true;

        public override ApiResponse Handle(ApiRequest request)
        {
            if (!request.Headers.TryGetValue("Content-Type", out var contentType)
                || !(contentType ?? "").TrimStart().StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
                return ApiResponse.Error(415, "content type must be application/json");

            string exchangeCode;
            try
            {
                using var document = JsonDocument.Parse(request.Body ?? "");
                if (document.RootElement.ValueKind != JsonValueKind.Object
                    || !document.RootElement.TryGetProperty("exchangeCode", out var value)
                    || value.ValueKind != JsonValueKind.String)
                    return ApiResponse.Error(400, "exchangeCode is required");

                exchangeCode = value.GetString()?.Trim();
            }
            catch (JsonException)
            {
                return ApiResponse.Error(400, "the body is not JSON");
            }

            if (string.IsNullOrWhiteSpace(exchangeCode))
                return ApiResponse.Error(400, "exchangeCode is required");

            if (!_sessions.TryExchange(request.Remote, exchangeCode, out var token, out var error))
                return ApiResponse.Error(401, error);

            return ApiResponse.Ok(ApiJson.Serialize(new { token }));
        }
    }
}
