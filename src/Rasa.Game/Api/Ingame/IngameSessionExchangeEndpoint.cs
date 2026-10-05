
namespace Rasa.Api.Ingame
{
    /// <summary>GET /ingame/session/exchange?challenge=...</summary>
    public sealed class IngameSessionExchangeEndpoint : ApiEndpoint
    {
        private readonly IngameSessionService _sessions;

        public IngameSessionExchangeEndpoint(IngameSessionService sessions) => _sessions = sessions;

        public override string Name => "ingame/session/exchange";
        public override bool RequiresApiKey => false;
        public override bool Sensitive => true;

        public override ApiResponse Handle(ApiRequest request)
        {
            var query = request.QueryParameters;
            if (!query.TryGetValue("challenge", out var challenge) || string.IsNullOrWhiteSpace(challenge))
                return ApiResponse.Error(400, "challenge is required");

            if (!_sessions.TryExchange(request.Remote, challenge, out var token, out var error))
                return ApiResponse.Error(409, error);

            return ApiResponse.Ok(ApiJson.Serialize(new { token }));
        }
    }
}
