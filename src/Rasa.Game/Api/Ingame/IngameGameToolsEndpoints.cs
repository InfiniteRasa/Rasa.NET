using System;
using System.Text.Json;

namespace Rasa.Api.Ingame
{
    using Game;

    /// <summary>
    /// JWT adapters over the same handlers and store instances used by the API-key GameTools routes.
    /// Do not duplicate weighted-loot validation, transactions, or class-flag semantics here.
    /// </summary>
    public abstract class IngameGameToolsEndpoint : IngameAuthenticatedEndpoint
    {
        private readonly ApiEndpoint _operation;
        private readonly string _path;
        private readonly bool _write;

        protected IngameGameToolsEndpoint(IngameSessionService sessions, ApiEndpoint operation, string path, bool write)
            : base(sessions)
        {
            _operation = operation;
            _path = path;
            _write = write;
        }

        public override string Name => _path;
        public override string Method => _operation.Method;
        public override int MaxBodyBytes => _operation.MaxBodyBytes;
        protected override string RequiredCommand => _write ? ".giveitem" : null;

        protected override ApiResponse HandleAuthenticated(ApiRequest request, Client client) => _operation.Handle(request);
    }

    public sealed class IngameMonsterFlagsEndpoint : IngameGameToolsEndpoint
    {
        public IngameMonsterFlagsEndpoint(IngameSessionService sessions, MonsterFlagsEndpoint operation)
            : base(sessions, operation, "ingame/monsterflags", false) { }

        protected override ApiResponse HandleAuthenticated(ApiRequest request, Client client)
        {
            // Keep the external GameTools GET contract exactly unchanged. Add definitions
            // only on this JWT-backed projection, using the same underlying class list.
            var response = base.HandleAuthenticated(request, client);
            if (response.Status != 200) return response;
            using var document = JsonDocument.Parse(response.Json);
            return ApiResponse.Ok(ServerStatus.Json(writer =>
            {
                foreach (var property in document.RootElement.EnumerateObject())
                    property.WriteTo(writer);
                writer.WriteStartArray("flagDefinitions");
                foreach (var value in Enum.GetValues<Rasa.Data.CreatureFlag>())
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("id", (int)value);
                    writer.WriteString("name", value.ToString());
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }));
        }
    }

    public sealed class IngameUpdateMonsterFlagsEndpoint : IngameGameToolsEndpoint
    {
        public IngameUpdateMonsterFlagsEndpoint(IngameSessionService sessions, UpdateMonsterFlagsEndpoint operation)
            : base(sessions, operation, "ingame/updatemonsterflags", true) { }
    }

    public sealed class IngameWeightedLootEndpoint : IngameGameToolsEndpoint
    {
        public IngameWeightedLootEndpoint(IngameSessionService sessions, WeightedLootEndpoint operation)
            : base(sessions, operation, "ingame/lootpools", false) { }
    }

    public sealed class IngameUpdateWeightedLootEndpoint : IngameGameToolsEndpoint
    {
        public IngameUpdateWeightedLootEndpoint(IngameSessionService sessions, UpdateWeightedLootEndpoint operation)
            : base(sessions, operation, "ingame/updatelootpools", true) { }
    }
}
