using System;
using System.Collections.Generic;

namespace Rasa.Api.Ingame
{
    using Data;
    using Game;
    using Managers;

    /// <summary>GET /ingame/items?category=...&amp;search=...</summary>
    public sealed class IngameItemsEndpoint : IngameItemEndpointBase
    {
        public IngameItemsEndpoint(IngameSessionService sessions) : base(sessions)
        {
        }

        public override string Name => "ingame/items";

        protected override ApiResponse HandleAuthenticated(ApiRequest request, Client client)
        {
            var query = request.QueryParameters;
            if (!query.TryGetValue("category", out var categoryText) || string.IsNullOrWhiteSpace(categoryText))
                return ApiResponse.Error(400, "category is required");

            if (!Enum.TryParse(categoryText, true, out InventoryCategory category)
                || !Enum.IsDefined(typeof(InventoryCategory), category))
                return ApiResponse.Error(400,
                    $"unknown category '{categoryText}'; expected one of: {string.Join(", ", Enum.GetNames(typeof(InventoryCategory)))}");

            query.TryGetValue("search", out var search);
            search = search?.Trim() ?? string.Empty;

            var items = new List<IngameItemSummary>();
            foreach (var pair in ItemManager.Instance.ItemTemplateItemClass)
            {
                if (!TryItem(pair.Key, out var template, out var entityClass)
                    || template.InventoryCategory != category)
                    continue;

                var name = entityClass.ClassName ?? string.Empty;
                if (search.Length > 0 && name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                items.Add(ToSummary(template, entityClass));
            }

            items.Sort((left, right) =>
            {
                var byName = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
                return byName != 0 ? byName : left.TemplateId.CompareTo(right.TemplateId);
            });

            return ApiResponse.Ok(ApiJson.Serialize(new IngameItemSearchResponse(category.ToString(), search, items)));
        }
    }
}
