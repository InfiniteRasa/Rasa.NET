namespace Rasa.Api.Ingame
{
    using Game;
    using Managers;

    /// <summary>
    /// Base for endpoints authenticated by a live in-game JWT rather than the REST API key.
    /// A derived endpoint may name a GM command whose permission level is also required.
    /// </summary>
    public abstract class IngameAuthenticatedEndpoint : ApiEndpoint
    {
        private readonly IngameSessionService _sessions;

        protected IngameAuthenticatedEndpoint(IngameSessionService sessions) => _sessions = sessions;

        public override bool RequiresApiKey => false;
        public override bool Sensitive => true;

        protected virtual string RequiredCommand => null;

        public sealed override ApiResponse Handle(ApiRequest request)
        {
            if (!_sessions.TryAuthenticate(request, out var client))
                return ApiResponse.Error(401, "a valid in-game API session is required");

            if (!string.IsNullOrEmpty(RequiredCommand)
                && !ChatCommandsManager.Instance.CanUseCommand(client, RequiredCommand))
                return ApiResponse.Error(403, $"this account does not have permission to use {RequiredCommand}");

            return HandleAuthenticated(request, client);
        }

        protected abstract ApiResponse HandleAuthenticated(ApiRequest request, Client client);
    }
}
