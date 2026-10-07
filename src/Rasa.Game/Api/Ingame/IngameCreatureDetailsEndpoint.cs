using System;
using System.Linq;

namespace Rasa.Api.Ingame
{
    using Game;

    /// <summary>GET /ingame/creatures/{creatureId}.</summary>
    public sealed class IngameCreatureDetailsEndpoint : IngameCreatureEndpointBase
    {
        private const string Prefix = "ingame/creatures/";

        public IngameCreatureDetailsEndpoint(IngameSessionService sessions) : base(sessions)
        {
        }

        public override string Name => "ingame/creatures/{id}";

        public override bool Matches(string endpointName) =>
            endpointName != null
            && endpointName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
            && endpointName.Length > Prefix.Length
            && endpointName.IndexOf('/', Prefix.Length) < 0;

        protected override ApiResponse HandleAuthenticated(ApiRequest request, Client client)
        {
            var idText = request.EndpointName.Substring(Prefix.Length);
            if (!uint.TryParse(idText, out var creatureId))
                return ApiResponse.Error(400, "creature id must be an unsigned integer");

            using var unitOfWork = Server.GameUnitOfWorkFactory.CreateWorld();
            var creature = unitOfWork.Creatures.Get().FirstOrDefault(row => row.Id == creatureId);
            if (creature == null)
                return ApiResponse.Error(404, $"unknown creature id {creatureId}");

            TryEntityClass(creature.ClassId, out var entityClass);
            var appearance = unitOfWork.Creatures.GetCreatureAppearances(creatureId);

            return ApiResponse.Ok(ApiJson.Serialize(ToDetails(creature, entityClass, appearance)));
        }
    }
}
