
namespace Rasa.Api.Ingame
{
    /// <summary>GET /ingame/session/challenge.</summary>
    public sealed class IngameSessionChallengeEndpoint : ApiEndpoint
    {
        private readonly IngameSessionService _sessions;

        public IngameSessionChallengeEndpoint(IngameSessionService sessions) => _sessions = sessions;

        public override string Name => "ingame/session/challenge";
        public override bool RequiresApiKey => false;
        public override bool Sensitive => true;

        public override ApiResponse Handle(ApiRequest request)
        {
            var challenge = _sessions.CreateChallenge(request.Remote);
            return challenge == null
                ? ApiResponse.Error(503, "in-game API authentication is not available")
                : ApiResponse.Ok(ApiJson.Serialize(new { challenge }));
        }
    }
}
