using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Api.Ingame
{
    using Data;
    using Game;

    /// <summary>GET /ingame/creature-actions</summary>
    public sealed class IngameCreatureActionsEndpoint : IngameAuthenticatedEndpoint
    {
        public IngameCreatureActionsEndpoint(IngameSessionService sessions) : base(sessions)
        {
        }

        public override string Name => "ingame/creature-actions";

        protected override ApiResponse HandleAuthenticated(ApiRequest request, Client client)
        {
            using var unitOfWork = Server.GameUnitOfWorkFactory.CreateWorld();
            var rows = unitOfWork.Creatures.GetCreatureActions().Values;
            var actions = new List<IngameCreatureActionOption>();

            foreach (var row in rows)
            {
                var actionId = (ActionId)row.ActionId;
                var actionName = Enum.IsDefined(typeof(ActionId), actionId)
                    ? actionId.ToString()
                    : $"ActionId_{row.ActionId}";

                actions.Add(new IngameCreatureActionOption
                {
                    Id = row.Id,
                    Description = row.Description ?? string.Empty,
                    ActionId = row.ActionId,
                    ActionName = actionName,
                    ActionArgId = row.ActionArgId,
                    RangeMin = row.RangeMin,
                    RangeMax = row.RangeMax,
                    Cooldown = row.Cooldown,
                    Windup = row.Windup,
                    MinDamage = row.MinDamage,
                    MaxDamage = row.MaxDamage,
                    DamageType = row.DamageType
                });
            }

            actions.Sort((left, right) =>
            {
                var byDescription = string.Compare(left.Description, right.Description, StringComparison.OrdinalIgnoreCase);
                return byDescription != 0 ? byDescription : left.Id.CompareTo(right.Id);
            });

            return ApiResponse.Ok(ApiJson.Serialize(new IngameCreatureActionOptionsResponse(actions)));
        }
    }
}
