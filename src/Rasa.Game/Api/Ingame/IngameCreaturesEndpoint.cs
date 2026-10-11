using System;
using System.Collections.Generic;

namespace Rasa.Api.Ingame
{
    using Game;
    using Managers;

    /// <summary>GET /ingame/creatures?search=...</summary>
    public sealed class IngameCreaturesEndpoint : IngameCreatureEndpointBase
    {
        public IngameCreaturesEndpoint(IngameSessionService sessions) : base(sessions)
        {
        }

        public override string Name => "ingame/creatures";

        protected override ApiResponse HandleAuthenticated(ApiRequest request, Client client)
        {
            request.QueryParameters.TryGetValue("search", out var search);
            search = search?.Trim() ?? string.Empty;

            using var unitOfWork = Server.GameUnitOfWorkFactory.CreateWorld();
            var rows = unitOfWork.Creatures.Get();
            var creatures = new List<IngameCreatureSummary>();

            foreach (var row in rows)
            {
                TryEntityClass(row.ClassId, out var entityClass);

                if (search.Length > 0 && !MatchesSearch(row.Id, row.Comment, row.ClassId, entityClass?.ClassName, search))
                    continue;

                creatures.Add(ToSummary(row, entityClass));
            }

            creatures.Sort((left, right) =>
            {
                var byComment = string.Compare(left.Comment, right.Comment, StringComparison.OrdinalIgnoreCase);
                if (byComment != 0)
                    return byComment;

                var byClass = string.Compare(left.ClassName, right.ClassName, StringComparison.OrdinalIgnoreCase);
                return byClass != 0 ? byClass : left.Id.CompareTo(right.Id);
            });

            return ApiResponse.Ok(ApiJson.Serialize(new IngameCreatureSearchResponse(search, creatures)));
        }

        private static bool MatchesSearch(uint id, string comment, uint classId, string className, string search) =>
            id.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
            || classId.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
            || (comment?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
            || (className?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
    }
}
