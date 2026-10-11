using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.Json;
using Rasa.Data;
using Rasa.Game.Missions.World;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Repositories.World;
using Rasa.Structures.World;

namespace Rasa.Loot
{
    internal sealed class OrdinaryLootCoverage
    {
        private readonly IWorldUnitOfWork _world;
        private readonly HashSet<uint> _requiredMaps;
        private readonly ILookup<uint, CreatureEntry> _creatures;
        private readonly HashSet<(uint Map, uint Creature)> _bindings;
        private readonly List<string> _errors;
        private readonly Dictionary<(uint Map, uint Creature), ActorGameplayPolicy> _experiencePolicies = new();
        private ILookup<uint, SpawnPoolEntry> _pools;

        internal OrdinaryLootCoverage(IWorldUnitOfWork world, HashSet<uint> requiredMaps,
            IEnumerable<CreatureEntry> creatures, HashSet<(uint Map, uint Creature)> bindings, List<string> errors)
        {
            _world = world;
            _requiredMaps = requiredMaps;
            _creatures = creatures.ToLookup(row => row.Id);
            _bindings = bindings;
            _errors = errors;
        }

        internal void Validate()
        {
            _pools = Rows(() => Required(_world.Spawnpools, "spawnpool").Get(), "spawnpool").ToLookup(row => row.Id);
            var missionContent = Read(() => _world.MissionContent, "mission scene coverage", _errors);
            if (missionContent is not IMigratedMissionContentRepository migrated)
                _errors.Add("Ordinary loot coverage requires migrated mission scene/experience bindings; the supplied World repository cannot establish complete scene coverage.");
            else
                ValidateScenes(migrated);

            foreach (var pool in _pools.SelectMany(group => group).Where(pool => _requiredMaps.Contains(pool.MapContextId)))
                foreach (var slot in Slots(pool).Where(slot => slot.Maximum > 0))
                    Source(pool.MapContextId, slot.Creature, $"spawnpool {pool.Id}");

            foreach (var arrival in Rows(() => Required(_world.SpawnPoolArrivals, "spawnpool_arrival").GetArrivals(), "spawnpool_arrival"))
            {
                var pool = _pools[arrival.PoolId].ToArray();
                if (pool.Length != 1)
                    _errors.Add($"arrival {arrival.Id}, spawnpool {arrival.PoolId}: cannot determine a unique source map.");
                else if (_requiredMaps.Contains(pool[0].MapContextId))
                    foreach (var slot in Slots(pool[0]).Where(slot => slot.Maximum > 0))
                        Source(pool[0].MapContextId, slot.Creature, $"arrival {arrival.Id}, spawnpool {arrival.PoolId}");
            }

            var points = Rows(() => Required(_world.ControlPoints, "control_point").GetControlPoints(), "control_point").ToLookup(row => row.Id);
            foreach (var link in Rows(() => Required(_world.ControlPoints, "control_point_link").GetLinks(), "control_point_link")
                .Where(link => link.Kind is ControlPointLinkEntry.KindBanePool or ControlPointLinkEntry.KindAfsPool))
            {
                var point = points[link.ControlPointId].ToArray();
                if (point.Length != 1)
                {
                    _errors.Add($"control point {link.ControlPointId}, spawnpool {link.ObjectId}: cannot determine a unique source map.");
                    continue;
                }
                if (!_requiredMaps.Contains(point[0].MapContextId))
                    continue;
                var pool = _pools[link.ObjectId].ToArray();
                if (pool.Length != 1 || pool[0].MapContextId != point[0].MapContextId)
                    _errors.Add($"control point {link.ControlPointId}, map {point[0].MapContextId}, spawnpool {link.ObjectId}: missing, ambiguous or cross-map garrison pool.");
                else
                    foreach (var slot in Slots(pool[0]).Where(slot => slot.Maximum > 0))
                        Source(point[0].MapContextId, slot.Creature,
                            $"control point {link.ControlPointId}, spawnpool {link.ObjectId}");
            }
        }

        private void ValidateScenes(IMigratedMissionContentRepository migrated)
        {
            var experiences = new List<(MissionExperienceBindingEntry Row, MissionExperienceDefinition Document)>();
            foreach (var row in Rows(migrated.GetExperiences, "mission_experience_binding").Where(row => row.Enabled))
            {
                var document = Decode<MissionExperienceDefinition>(row.Bindings, $"experience {row.ExperienceKey}");
                if (document == null)
                    continue;
                if (document.MapContextId != row.MapContextId || document.Key != row.ExperienceKey ||
                    !document.PrivatePerCharacter || document.Scene?.Actors == null || document.ActorPolicies == null)
                {
                    _errors.Add($"experience {row.ExperienceKey}, map {row.MapContextId}: inconsistent or incomplete scene bindings.");
                    continue;
                }
                experiences.Add((row, document));
                foreach (var policy in document.ActorPolicies)
                    if (!_experiencePolicies.TryAdd((row.MapContextId, policy.Key), policy.Value))
                        _errors.Add($"experience {row.ExperienceKey}, map {row.MapContextId}, creature {policy.Key}: ambiguous actor policy.");
            }
            foreach (var experience in experiences)
                foreach (var actor in experience.Document.Scene.Actors.Values)
                    SceneActor(actor, experience.Row.MapContextId, $"experience {experience.Row.ExperienceKey}");

            var enabled = Rows(migrated.GetEnabledDefinitions, "enabled mission definitions")
                .ToLookup(row => (row.MissionId, row.ContentRevision));
            foreach (var row in Rows(migrated.GetSceneBindings, "mission_scene_binding")
                .Where(row => enabled.Contains((row.MissionId, row.ContentRevision))))
            {
                var source = $"mission {row.MissionId}@{row.ContentRevision}";
                var document = Decode<MissionSceneDefinition>(row.Bindings, source);
                if (document?.Actors == null)
                {
                    _errors.Add($"{source}: missing scene actors; ordinary loot coverage cannot be established.");
                    continue;
                }
                if (document.Script != row.ScriptKey || document.StateVersion != row.StateVersion)
                    _errors.Add($"{source}: inconsistent scene script/version.");
                if (document.Script == null)
                    continue;
                uint? encounterMap = null;
                if (document.PublicEncounter is { } encounter)
                {
                    var pool = _pools[encounter.SpawnId].ToArray();
                    if (encounter.MissionId != row.MissionId || encounter.ScriptKey != document.Script ||
                        !document.Actors.TryGetValue(encounter.Role, out var anchor) ||
                        anchor?.Kind != SceneActorKind.PublicSpawn || anchor.TemplateId != encounter.SpawnId ||
                        pool.Length != 1)
                        _errors.Add($"{source}, public encounter spawn {encounter.SpawnId}: cannot establish its reserved source map.");
                    else
                        // PublicActorLeaseService.TryPrepare admits this run only on the reserved pool's map.
                        encounterMap = pool[0].MapContextId;
                }
                foreach (var actor in document.Actors.Values)
                    SceneActor(actor, encounterMap, source);
            }
        }

        private void SceneActor(SceneActorDefinition actor, uint? map, string source)
        {
            if (actor == null)
            {
                _errors.Add($"{source}: null scene actor.");
                return;
            }
            source += $", actor {actor.Role}";
            if (actor.Kind == SceneActorKind.PublicSpawn)
            {
                var pools = _pools[actor.TemplateId].ToArray();
                if (pools.Length != 1)
                    _errors.Add($"{source}, public spawn {actor.TemplateId}: cannot determine a unique source map.");
                else if (map.HasValue && map.Value != pools[0].MapContextId)
                    _errors.Add($"{source}, public spawn {actor.TemplateId}: scene map {map.Value} disagrees with source pool map {pools[0].MapContextId}.");
                else
                    foreach (var slot in Slots(pools[0]).Where(slot => slot.Maximum > 0))
                        Source(pools[0].MapContextId, slot.Creature, source, role: actor.GameplayPolicy, experiencePolicy: true);
                return;
            }
            if (actor.Kind != SceneActorKind.Creature)
                return;
            if (map is { } context)
                Source(context, actor.TemplateId, source, scenario: true, role: actor.GameplayPolicy, experiencePolicy: true);
            else if ((CreatureGameplayRules.RewardsKills(true, actor.GameplayPolicy) ||
                    actor.GameplayPolicy == null && _experiencePolicies.Any(entry =>
                        _requiredMaps.Contains(entry.Key.Map) && entry.Key.Creature == actor.TemplateId &&
                        entry.Value?.RewardScenarioKills == true && entry.Value.Loot == null && !entry.Value.Invulnerable)) &&
                actor.GameplayPolicy?.Loot == null && actor.GameplayPolicy?.Invulnerable != true)
            {
                var native = _creatures[actor.TemplateId].ToArray();
                if (native.Length != 1)
                    _errors.Add($"{source}, creature {actor.TemplateId}: missing or ambiguous native creature prevents source classification.");
                else if (TargetCategories.PlayerMayAttack(TargetCategories.FromFaction(native[0].Faction)))
                    _errors.Add($"{source}, creature {actor.TemplateId}: cannot determine an enforced source map from an owned experience or exact reserved public encounter. Scene actors are created on the current run map; mission_spawn_group/mission_spawn metadata is not an enforced map constraint, and mission areas or coordinates do not prove map coverage.");
            }
        }

        private void Source(uint map, uint creature, string source, bool scenario = false, ActorGameplayPolicy role = null,
            bool experiencePolicy = false)
        {
            if (!_requiredMaps.Contains(map))
                return;
            var native = _creatures[creature].ToArray();
            if (native.Length != 1)
            {
                _errors.Add($"{source}, map {map}, creature {creature}: missing or ambiguous native creature.");
                return;
            }
            var policy = role ?? (experiencePolicy ? _experiencePolicies.GetValueOrDefault((map, creature)) : null);
            if (!TargetCategories.PlayerMayAttack(TargetCategories.FromFaction(native[0].Faction)) ||
                policy?.Invulnerable == true || policy?.Loot != null ||
                !CreatureGameplayRules.RewardsKills(scenario, role, policy))
                return;
            if (!_bindings.Contains((map, creature)))
                _errors.Add($"{source}, map {map}, creature {creature}: required ordinary loot profile binding is missing.");
        }

        private IReadOnlyList<T> Rows<T>(Func<List<T>> read, string source) where T : class
        {
            var rows = Read(read, source, _errors) ?? new List<T>();
            if (rows.Any(row => row == null))
                _errors.Add($"{source}: null coverage row.");
            return rows.Where(row => row != null).ToArray();
        }

        private static T Required<T>(T repository, string source) where T : class =>
            repository ?? throw new NotSupportedException($"Missing {source} coverage repository.");

        internal static T Read<T>(Func<T> read, string source, List<string> errors) where T : class
        {
            try
            {
                var value = read();
                if (value == null)
                    errors.Add($"Ordinary loot {source}: repository returned null.");
                return value;
            }
            catch (DbException error)
            {
                errors.Add($"Ordinary loot {source}: database read failed ({error.Message}); apply the required World migrations.");
                return null;
            }
            catch (NotSupportedException error)
            {
                errors.Add($"Ordinary loot {source}: repository is unsupported ({error.Message}).");
                return null;
            }
            catch (LegacyFlatLootArchiveException error)
            {
                errors.Add($"Ordinary loot {source}: {error.Message}");
                return null;
            }
        }

        private T Decode<T>(string json, string source) where T : class
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json))
                    throw new JsonException("Missing binding document.");
                return JsonSerializer.Deserialize<T>(json, MissionContentCodec.Options) ??
                    throw new JsonException("Null binding document.");
            }
            catch (Exception error) when (error is JsonException or ArgumentException or NotSupportedException)
            {
                _errors.Add($"{source}: scene bindings cannot be read for ordinary loot coverage ({error.Message}).");
                return null;
            }
        }

        private static IEnumerable<(uint Creature, byte Maximum)> Slots(SpawnPoolEntry pool)
        {
            yield return (pool.Creature1Id, pool.Creature1MaxCount);
            yield return (pool.Creature2Id, pool.Creature2MaxCount);
            yield return (pool.Creature3Id, pool.Creature3MaxCount);
            yield return (pool.Creature4Id, pool.Creature4MaxCount);
            yield return (pool.Creature5Id, pool.Creature5MaxCount);
            yield return (pool.Creature6Id, pool.Creature6MaxCount);
        }
    }
}
