using System;

namespace Rasa.Api.Ingame
{
    using Game;

    /// <summary>GET /ingame/items/{templateId}.</summary>
    public sealed class IngameItemDetailsEndpoint : IngameItemEndpointBase
    {
        private const string Prefix = "ingame/items/";

        public IngameItemDetailsEndpoint(IngameSessionService sessions) : base(sessions)
        {
        }

        public override string Name => "ingame/items/{id}";

        public override bool Matches(string endpointName) =>
            endpointName != null
            && endpointName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
            && endpointName.Length > Prefix.Length
            && endpointName.IndexOf('/', Prefix.Length) < 0;

        protected override ApiResponse HandleAuthenticated(ApiRequest request, Client client)
        {
            var idText = request.EndpointName.Substring(Prefix.Length);
            if (!uint.TryParse(idText, out var templateId))
                return ApiResponse.Error(400, "item template id must be an unsigned integer");

            if (!TryItem(templateId, out var template, out var entityClass))
                return ApiResponse.Error(404, $"unknown item template id {templateId}");

            return ApiResponse.Ok(ApiJson.Serialize(ToDetails(template, entityClass)));
        }
    }
}
