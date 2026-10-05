using System;

namespace Rasa.Api.Ingame
{
    using Data;
    using Game;

    /// <summary>GET /ingame/items/categories.</summary>
    public sealed class IngameItemCategoriesEndpoint : IngameItemEndpointBase
    {
        public IngameItemCategoriesEndpoint(IngameSessionService sessions) : base(sessions)
        {
        }

        public override string Name => "ingame/items/categories";

        protected override ApiResponse HandleAuthenticated(ApiRequest request, Client client) =>
            ApiResponse.Ok(ApiJson.Serialize(Enum.GetNames(typeof(InventoryCategory))));
    }
}
