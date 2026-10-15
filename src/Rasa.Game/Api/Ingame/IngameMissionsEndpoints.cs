using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Rasa.Api.Ingame
{
    using Game;
    using Repositories.World;
    using Structures.Char;
    using Structures.World;

    /// <summary>Read-only mission catalog with the authenticated character's durable completion status.</summary>
    internal static class IngameMissionCatalog
    {
        // A mission has no direct map column: gather *evidence* from content and the NPC's
        // native spawn locations. Do not claim a map for a mission without evidence.
        internal static Dictionary<uint, SortedSet<uint>> FindMissionMaps(
            IEnumerable<MissionContentDefinitionEntry> definitions,
            IEnumerable<NpcMissionEntry> legacy,
            IEnumerable<MissionAreaEntry> areas,
            IEnumerable<MissionSpawnGroupEntry> groups,
            IEnumerable<MissionScenarioStepEntry> steps,
            IEnumerable<SpawnPoolEntry> pools)
        {
            var result = new Dictionary<uint, SortedSet<uint>>();
            void Add(uint missionId, uint map)
            {
                if (map == 0) return;
                if (!result.TryGetValue(missionId, out var set))
                    result[missionId] = set = new SortedSet<uint>();
                set.Add(map);
            }

            // Ignore historical, disabled revisions. The running mission engine
            // selects one enabled revision per mission.
            var revisions = definitions.GroupBy(m => m.MissionId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.Enabled)
                    .ThenByDescending(m => m.ContentRevision, StringComparer.Ordinal).First().ContentRevision);
            bool IsSelected(uint id, string revision) =>
                !revisions.TryGetValue(id, out var selected) || selected == revision;
            foreach (var row in areas)
                if (IsSelected(row.MissionId, row.ContentRevision)) Add(row.MissionId, row.MapContextId);
            foreach (var row in groups)
                if (IsSelected(row.MissionId, row.ContentRevision)) Add(row.MissionId, row.MapContextId);
            foreach (var row in steps)
                if (row.MapContextId.HasValue && IsSelected(row.MissionId, row.ContentRevision))
                    Add(row.MissionId, row.MapContextId.Value);

            var npcLocations = new Dictionary<uint, HashSet<uint>>();
            foreach (var pool in pools)
            {
                foreach (var creature in new[] { pool.Creature1Id, pool.Creature2Id, pool.Creature3Id,
                             pool.Creature4Id, pool.Creature5Id, pool.Creature6Id })
                {
                    if (creature == 0) continue;
                    if (!npcLocations.TryGetValue(creature, out var locations))
                        npcLocations[creature] = locations = new HashSet<uint>();
                    locations.Add(pool.MapContextId);
                }
            }
            void AddNpc(uint missionId, uint npcId)
            {
                if (npcLocations.TryGetValue(npcId, out var locations))
                    foreach (var map in locations) Add(missionId, map);
            }
            foreach (var row in definitions)
            {
                if (row.GiverId.HasValue) AddNpc(row.MissionId, row.GiverId.Value);
                if (row.ReceiverId.HasValue) AddNpc(row.MissionId, row.ReceiverId.Value);
            }
            foreach (var row in legacy)
            {
                AddNpc(row.Id, row.GiverId);
                AddNpc(row.Id, row.ReciverId);
            }
            return result;
        }

        // A mission content row may omit its giver even though the original
        // npc_mission assignment still knows the NPC. Prefer a declared current
        // giver; otherwise use the matching legacy assignment if one exists.
        internal static uint MissionGiverId(MissionContentDefinitionEntry definition, NpcMissionEntry legacy)
        {
            if (definition?.GiverId is uint id && id != 0) return id;
            return legacy?.GiverId ?? 0u;
        }

        /// <summary>Actual spawnpool positions for the selected mission giver (not an inferred objective marker).</summary>
        internal sealed record MissionGiverLocation(uint spawnPoolId, uint mapContextId,
            double x, double y, double z);

        internal static List<MissionGiverLocation> GiverLocations(uint giverId, IEnumerable<SpawnPoolEntry> pools)
        {
            if (giverId == 0) return new List<MissionGiverLocation>();
            return pools.Where(pool =>
                    pool.Creature1Id == giverId || pool.Creature2Id == giverId ||
                    pool.Creature3Id == giverId || pool.Creature4Id == giverId ||
                    pool.Creature5Id == giverId || pool.Creature6Id == giverId)
                .OrderBy(pool => pool.MapContextId).ThenBy(pool => pool.Id)
                .Select(pool => new MissionGiverLocation(pool.Id, pool.MapContextId,
                    pool.PosX, pool.PosY, pool.PosZ)).ToList();
        }

        // A rewarded mission can be archived and absent from the current journal.
        // Use the durable Character DB; do not use player.Missions (the client-facing journal).
        internal static HashSet<uint> CompletedMissionIds(
            IEnumerable<CharacterMissionEntry> assignments,
            IEnumerable<CharacterMissionHistoryEntry> history)
        {
            var result = history.Where(h => h.Rewarded || h.Outcome == 4)
                .Select(h => h.MissionId).ToHashSet();
            foreach (var entry in assignments)
                if (entry.MissionState == 4) result.Add(entry.MissionId);
            return result;
        }

        internal static List<object> LinkedPrerequisites(
            IEnumerable<MissionPrerequisiteEntry> entries, uint missionId, string revision)
        {
            return entries.Where(p => p.MissionId == missionId && p.ContentRevision == revision &&
                    p.RequiredMissionId.HasValue && p.RequiredMissionId.Value != 0 &&
                    p.Kind is MissionPrerequisiteKind.MissionCompleted or MissionPrerequisiteKind.MissionAccepted)
                .OrderBy(p => p.PrerequisiteId)
                .Select(p => (object)new {
                    missionId = p.RequiredMissionId.Value,
                    kind = p.Kind.ToString(),
                    requiredMissionState = p.RequiredMissionState,
                    requirement = p.Requirement.ToString(),
                    comment = p.Comment
                }).ToList();
        }

        internal static string FriendlyName(string comment, uint id) =>
            string.IsNullOrWhiteSpace(comment) ? $"Mission {id}" : comment.Trim();

        // Only scalar database properties. EF navigation properties would recursively serialize
        // the entire world graph (or trigger a reference cycle).
        private static bool IsScalarType(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            return underlying.IsPrimitive || underlying.IsEnum || underlying == typeof(string)
                || underlying == typeof(decimal) || underlying == typeof(DateTime);
        }

        internal static List<Dictionary<string, object>> Rows<T>(IEnumerable<T> entries)
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && IsScalarType(p.PropertyType))
                .ToArray();
            return entries.Select(entry =>
            {
                var row = new Dictionary<string, object>(StringComparer.Ordinal);
                foreach (var property in properties)
                {
                    var value = property.GetValue(entry);
                    row[JsonNamingPolicy.CamelCase.ConvertName(property.Name)] =
                        value != null && value.GetType().IsEnum ? value.ToString() : value;
                }
                return row;
            }).ToList();
        }
    }

    public sealed class IngameMissionsEndpoint : IngameAuthenticatedEndpoint
    {
        public IngameMissionsEndpoint(IngameSessionService sessions) : base(sessions) { }
        public override string Name => "ingame/missions";

        protected override ApiResponse HandleAuthenticated(ApiRequest request, Client client)
        {
            using var world = Server.GameUnitOfWorkFactory.CreateWorld();
            var content = world.MissionContent;
            var definitions = content.GetDefinitions().GroupBy(m => m.MissionId)
                .Select(g => g.OrderByDescending(m => m.Enabled)
                    .ThenByDescending(m => m.ContentRevision, StringComparer.Ordinal).First())
                .ToList();
            var legacy = world.NpcMissions.Get();
            using var characters = Server.GameUnitOfWorkFactory.CreateChar();
            var characterId = client.Player.Id;
            var playerAssignments = characters.CharacterMissions.Get(characterId);
            var completed = IngameMissionCatalog.CompletedMissionIds(playerAssignments,
                characters.CharacterMissions.Runtime.History(characterId));
            var playerStates = playerAssignments.ToDictionary(m => m.MissionId, m => m.MissionState);
            var prerequisites = content.GetPrerequisites();
            var mapNames = world.MapInfos.Get().ToDictionary(m => m.Id, m => m.MapName ?? "");
            var maps = IngameMissionCatalog.FindMissionMaps(definitions, legacy,
                content.GetAreas(), content.GetSpawnGroups(), content.GetScenarioSteps(), world.Spawnpools.Get());

            var catalog = definitions.Select(m => new
            {
                id = m.MissionId,
                name = IngameMissionCatalog.FriendlyName(m.Comment, m.MissionId),
                nameTextId = m.ClientNameTextId,
                revision = m.ContentRevision,
                enabled = m.Enabled,
                level = m.Level,
                source = "mission_content_definition",
                playerCompleted = completed.Contains(m.MissionId),
                playerMissionState = playerStates.TryGetValue(m.MissionId, out var state) ? (uint?)state : null,
                prerequisites = IngameMissionCatalog.LinkedPrerequisites(prerequisites, m.MissionId, m.ContentRevision)
            }).ToDictionary(m => m.id, m => (object)m);
            foreach (var m in legacy)
            {
                if (!catalog.ContainsKey(m.Id))
                    catalog.Add(m.Id, new
                    {
                        id = m.Id, name = IngameMissionCatalog.FriendlyName(m.Comment, m.Id),
                        nameTextId = 0u, revision = "", enabled = true, level = m.Level,
                        source = "npc_mission (legacy)",
                        playerCompleted = completed.Contains(m.Id),
                        playerMissionState = playerStates.TryGetValue(m.Id, out var state) ? (uint?)state : null,
                        prerequisites = Array.Empty<object>()
                    });
            }

            var missions = new List<object>();
            foreach (var pair in catalog.OrderBy(p => p.Key))
            {
                var id = pair.Key;
                var associatedMaps = maps.TryGetValue(id, out var ids) ? ids : new SortedSet<uint>();
                // Duplicate multi-map missions into each map in the client, not the database.
                missions.Add(new { summary = pair.Value, mapIds = associatedMaps.ToArray() });
            }
            return ApiResponse.Ok(ApiJson.Serialize(new
            {
                maps = mapNames.OrderBy(m => m.Value).Select(m => new { id = m.Key, name = m.Value }),
                missions
            }));
        }
    }

    public sealed class IngameMissionDetailsEndpoint : IngameAuthenticatedEndpoint
    {
        private const string Prefix = "ingame/missions/";
        public IngameMissionDetailsEndpoint(IngameSessionService sessions) : base(sessions) { }
        public override string Name => "ingame/missions/{id}";
        public override bool Matches(string endpointName) =>
            endpointName != null && endpointName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
            && endpointName.Length > Prefix.Length && endpointName.IndexOf('/', Prefix.Length) < 0;

        protected override ApiResponse HandleAuthenticated(ApiRequest request, Client client)
        {
            if (!uint.TryParse(request.EndpointName.Substring(Prefix.Length), out var id) || id == 0)
                return ApiResponse.Error(400, "mission id must be a positive integer");
            using var world = Server.GameUnitOfWorkFactory.CreateWorld();
            var content = world.MissionContent;
            var definition = content.GetDefinitions().Where(m => m.MissionId == id)
                .OrderByDescending(m => m.Enabled)
                .ThenByDescending(m => m.ContentRevision, StringComparer.Ordinal).FirstOrDefault();
            var legacy = world.NpcMissions.Get().FirstOrDefault(m => m.Id == id);
            if (definition == null && legacy == null) return ApiResponse.Error(404, $"unknown mission {id}");

            var rev = definition?.ContentRevision;
            var giverId = IngameMissionCatalog.MissionGiverId(definition, legacy);
            var giverLocations = IngameMissionCatalog.GiverLocations(giverId, world.Spawnpools.Get());
            using var characters = Server.GameUnitOfWorkFactory.CreateChar();
            var characterId = client.Player.Id;
            var assignments = characters.CharacterMissions.Get(characterId);
            var completed = IngameMissionCatalog.CompletedMissionIds(assignments,
                characters.CharacterMissions.Runtime.History(characterId));
            var assignment = assignments.FirstOrDefault(m => m.MissionId == id);
            var linkedPrerequisites = definition == null ? new List<object>() :
                IngameMissionCatalog.LinkedPrerequisites(content.GetPrerequisites(), id, rev);
            List<Dictionary<string, object>> Filter<T>(IEnumerable<T> entries, Func<T, uint> missionId, Func<T, string> revision) =>
                IngameMissionCatalog.Rows(entries.Where(m => missionId(m) == id && (rev == null || revision(m) == rev)));

            var sections = new Dictionary<string, object>
            {
                ["definition"] = definition == null ? (object)Array.Empty<object>() : IngameMissionCatalog.Rows(new[] { definition }),
                ["legacyNpcMission"] = legacy == null ? (object)Array.Empty<object>() : IngameMissionCatalog.Rows(new[] { legacy }),
                ["objectives"] = Filter(content.GetObjectives(), m => m.MissionId, m => m.ContentRevision),
                ["transitions"] = Filter(content.GetTransitions(), m => m.MissionId, m => m.ContentRevision),
                ["triggers"] = Filter(content.GetTriggers(), m => m.MissionId, m => m.ContentRevision),
                ["actions"] = Filter(content.GetActions(), m => m.MissionId, m => m.ContentRevision),
                ["prerequisites"] = Filter(content.GetPrerequisites(), m => m.MissionId, m => m.ContentRevision),
                ["rewards"] = Filter(content.GetRewards(), m => m.MissionId, m => m.ContentRevision),
                ["rewardItems"] = Filter(content.GetRewardItems(), m => m.MissionId, m => m.ContentRevision),
                ["indicators"] = Filter(content.GetIndicators(), m => m.MissionId, m => m.ContentRevision),
                ["areas"] = Filter(content.GetAreas(), m => m.MissionId, m => m.ContentRevision),
                ["spawnGroups"] = Filter(content.GetSpawnGroups(), m => m.MissionId, m => m.ContentRevision),
                ["spawns"] = Filter(content.GetSpawns(), m => m.MissionId, m => m.ContentRevision),
                ["scenarios"] = Filter(content.GetScenarios(), m => m.MissionId, m => m.ContentRevision),
                ["scenarioSteps"] = Filter(content.GetScenarioSteps(), m => m.MissionId, m => m.ContentRevision),
                ["evidence"] = Filter(content.GetEvidence(), m => m.MissionId, m => m.ContentRevision),
                ["legacyRewards"] = IngameMissionCatalog.Rows(world.NpcMissionRewards.Get(id))
            };
            if (content is IMigratedMissionContentRepository migrated)
            {
                sections["repeatPolicies"] = Filter(migrated.GetRepeatPolicies(), m => m.MissionId, m => m.ContentRevision);
                sections["channelPolicies"] = Filter(migrated.GetChannelPolicies(), m => m.MissionId, m => m.ContentRevision);
                sections["sceneBindings"] = Filter(migrated.GetSceneBindings(), m => m.MissionId, m => m.ContentRevision);
            }
            return ApiResponse.Ok(ApiJson.Serialize(new {
                id, sections,
                playerCompleted = completed.Contains(id),
                playerMissionState = (uint?)assignment?.MissionState,
                giverId,
                giverLocations,
                prerequisites = linkedPrerequisites
            }));
        }
    }
}
