using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Repositories.World;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Loot
{
    internal sealed class OrdinaryLootCatalog
    {
        private static OrdinaryLootCatalog _current;
        private readonly IReadOnlyDictionary<(uint Map, uint Creature), OrdinaryLootProfile> _bindings;
        private readonly HashSet<uint> _requiredMaps;
        private readonly IReadOnlyDictionary<string, OrdinaryLootProfile> _profiles;

        internal static OrdinaryLootCatalog Current => Volatile.Read(ref _current) ??
            throw new InvalidOperationException("The ordinary loot catalog has not passed startup validation.");

        private OrdinaryLootCatalog(Dictionary<(uint Map, uint Creature), OrdinaryLootProfile> bindings,
            IEnumerable<uint> requiredMaps, Dictionary<string, OrdinaryLootProfile> profiles)
        {
            _bindings = new ReadOnlyDictionary<(uint Map, uint Creature), OrdinaryLootProfile>(bindings);
            _requiredMaps = new HashSet<uint>(requiredMaps);
            _profiles = new ReadOnlyDictionary<string, OrdinaryLootProfile>(profiles);
        }

        /// <summary>The profile of this key, bound to a creature or not (a lootable container names one: WorldContainers), or null.</summary>
        internal OrdinaryLootProfile Profile(string profileKey) =>
            profileKey != null && _profiles.TryGetValue(profileKey, out var profile) ? profile : null;

        internal static void Publish(OrdinaryLootCatalog catalog) =>
            Volatile.Write(ref _current, catalog ?? throw new ArgumentNullException(nameof(catalog)));

        internal static OrdinaryLootCatalog Load(IWorldUnitOfWork world)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            return new Validation(world).Load();
        }

        internal static OrdinaryLootCatalog Load(IWorldUnitOfWork world, OrdinaryLootContentSnapshot content)
        {
            ArgumentNullException.ThrowIfNull(world);
            ArgumentNullException.ThrowIfNull(content);
            return new Validation(world).Load(content);
        }

        internal OrdinaryLootProfile Resolve(uint mapContextId, uint creatureId)
        {
            if (_bindings.TryGetValue((mapContextId, creatureId), out var profile))
                return profile;
            if (_requiredMaps.Contains(mapContextId))
                throw new GameplayRejectionException(
                    $"Ordinary loot: required map {mapContextId}, creature {creatureId} has no profile binding.");
            return null;
        }

        private sealed class Validation
        {
            private readonly IWorldUnitOfWork _world;
            private readonly List<string> _errors = new();

            internal Validation(IWorldUnitOfWork world) => _world = world;

            internal OrdinaryLootCatalog Load()
            {
                var snapshot = Read(() => Required(_world.OrdinaryLoot, "ordinary loot").Get(), "ordinary loot tables");
                FailIfInvalid();
                return Load(snapshot);
            }

            internal OrdinaryLootCatalog Load(OrdinaryLootContentSnapshot snapshot)
            {
                var profiles = Rows(snapshot.Profiles, "loot_profile");
                var groups = Rows(snapshot.Groups, "ordinary_loot_group");
                var items = Rows(snapshot.Items, "loot_item");
                var bindings = Rows(snapshot.Bindings, "creature_loot_binding");
                var policies = Rows(snapshot.MapPolicies, "loot_map_policy");

                var profileKeys = new HashSet<string>(StringComparer.Ordinal);
                foreach (var profile in profiles)
                {
                    Key(profile.ProfileKey, 64, $"profile {profile.ProfileKey ?? "<null>"}");
                    if (!profileKeys.Add(profile.ProfileKey))
                        Error($"profile {profile.ProfileKey}: duplicate profile key.");
                    if (profile.Description == null || profile.Provenance == null || profile.EvidenceNotes == null)
                        Error($"profile {profile.ProfileKey}: description, provenance and evidence notes must be strings.");
                    if (profile.ContentRevision == null || profile.ContentRevision.Length > 32)
                        Error($"profile {profile.ProfileKey}: content revision must be a string of at most 32 characters.");
                }
                var groupKeys = new HashSet<(string Profile, string Group)>();
                foreach (var group in groups)
                {
                    var label = $"profile {group.ProfileKey}, group {group.GroupKey}";
                    Key(group.ProfileKey, 64, label);
                    Key(group.GroupKey, 32, label);
                    if (!profileKeys.Contains(group.ProfileKey))
                        Error($"{label}: missing profile.");
                    if (!groupKeys.Add((group.ProfileKey, group.GroupKey)))
                        Error($"{label}: duplicate group key.");
                    if (!Enum.IsDefined(typeof(LootGroupKind), group.Kind))
                        Error($"{label}: undefined kind {group.Kind}.");
                    if (group.ChanceBasisPoints is < 0 or > 10000)
                        Error($"{label}: chance must be in 0..10000.");
                    if (group.MinimumRolls < 0 || group.MaximumRolls < group.MinimumRolls ||
                        group.MaximumRolls == int.MaxValue)
                        Error($"{label}: roll bounds must be nonnegative, ordered and RNG-safe.");
                    if (group.Kind == LootGroupKind.Junk && group.MaximumRolls > 1)
                        Error($"{label}: Junk groups may select at most one type.");
                }

                var itemKeys = new HashSet<(string Profile, string Group, uint Template)>();
                foreach (var item in items)
                {
                    var label = ItemLabel(item);
                    Key(item.ProfileKey, 64, label);
                    Key(item.GroupKey, 32, label);
                    if (!groupKeys.Contains((item.ProfileKey, item.GroupKey)))
                        Error($"{label}: missing group.");
                    if (!itemKeys.Add((item.ProfileKey, item.GroupKey, item.ItemTemplateId)))
                        Error($"{label}: duplicate candidate key.");
                    if (item.ItemTemplateId == 0)
                        Error($"{label}: template ID must be positive.");
                    if (item.Weight <= 0)
                        Error($"{label}: weight must be positive.");
                    if (item.MinimumQuantity <= 0 || item.MaximumQuantity < item.MinimumQuantity ||
                        item.MaximumQuantity >= int.MaxValue)
                        Error($"{label}: quantity bounds must be positive, ordered and safe for inclusive integer RNG.");
                }

                var candidates = items.ToLookup(item => (item.ProfileKey, item.GroupKey));
                foreach (var group in groups)
                {
                    var entries = candidates[(group.ProfileKey, group.GroupKey)].ToArray();
                    if (entries.Length == 0)
                        Error($"profile {group.ProfileKey}, group {group.GroupKey}: group has no candidates.");
                    if (entries.Sum(item => (long)item.Weight) > int.MaxValue)
                        Error($"profile {group.ProfileKey}, group {group.GroupKey}: total weight exceeds the integer RNG range.");
                }
                foreach (var grouped in groups.GroupBy(group => group.ProfileKey, StringComparer.Ordinal))
                {
                    if (grouped.Count(group => group.Kind == LootGroupKind.Junk) > 1)
                        Error($"profile {grouped.Key}: multiple Junk groups bypass the one-type cap.");
                    if (grouped.Sum(group => (long)group.MaximumRolls) > 32)
                        Error($"profile {grouped.Key}: aggregate maximum rolls exceed the 32-roll safety bound.");
                }

                var maps = ReadRows(() => Required(_world.MapInfos, "map_info").Get(), "map_info").Select(row => row.Id).ToHashSet();
                var creatures = ReadRows(() => Required(_world.Creatures, "creature").Get(), "creature");
                var creatureIds = creatures.Select(row => row.Id).ToHashSet();
                var bindingKeys = new HashSet<(uint Map, uint Creature)>();
                foreach (var binding in bindings)
                {
                    var label = $"map {binding.MapContextId}, creature {binding.CreatureId}, profile {binding.ProfileKey}";
                    Key(binding.ProfileKey, 64, label);
                    if (!profileKeys.Contains(binding.ProfileKey))
                        Error($"{label}: missing profile.");
                    if (binding.MapContextId == 0 || !maps.Contains(binding.MapContextId))
                        Error($"{label}: missing native map.");
                    if (binding.CreatureId == 0 || !creatureIds.Contains(binding.CreatureId))
                        Error($"{label}: missing native creature.");
                    if (!bindingKeys.Add((binding.MapContextId, binding.CreatureId)))
                        Error($"{label}: duplicate binding.");
                }
                var policyMaps = new HashSet<uint>();
                foreach (var policy in policies)
                {
                    if (policy.MapContextId == 0 || !maps.Contains(policy.MapContextId))
                        Error($"map policy {policy.MapContextId}: missing native map.");
                    if (!policyMaps.Add(policy.MapContextId))
                        Error($"map policy {policy.MapContextId}: duplicate policy.");
                }
                if (items.Count > 0)
                    ValidateNativeItems(items, groups);
                var requiredMaps = policies.Where(policy => policy.RequireProfiles).Select(policy => policy.MapContextId).ToHashSet();
                if (requiredMaps.Count > 0)
                    new OrdinaryLootCoverage(_world, requiredMaps, creatures, bindingKeys, _errors).Validate();
                FailIfInvalid();

                var immutableProfiles = profiles.ToDictionary(profile => profile.ProfileKey, profile =>
                    new OrdinaryLootProfile(profile.ProfileKey, groups.Where(group => group.ProfileKey == profile.ProfileKey)
                        .OrderBy(group => group.SortOrder).ThenBy(group => group.GroupKey, StringComparer.Ordinal)
                        .Select(group => new OrdinaryLootGroup(group.GroupKey, group.Kind, group.ChanceBasisPoints,
                            group.MinimumRolls, group.MaximumRolls, candidates[(group.ProfileKey, group.GroupKey)]
                                .OrderBy(item => item.SortOrder).ThenBy(item => item.ItemTemplateId)
                                .Select(item => new OrdinaryLootCandidate(item.ItemTemplateId, item.Weight,
                                    checked((uint)item.MinimumQuantity), checked((uint)item.MaximumQuantity))).ToArray()))
                        .ToArray()), StringComparer.Ordinal);
                return new OrdinaryLootCatalog(bindings.ToDictionary(binding => (binding.MapContextId, binding.CreatureId),
                    binding => immutableProfiles[binding.ProfileKey]), requiredMaps, immutableProfiles);
            }

            private void ValidateNativeItems(IReadOnlyList<LootItemEntry> items, IReadOnlyList<OrdinaryLootGroupEntry> groups)
            {
                var equipment = Read(() => Required(_world.Equipment, "equipment"), "native equipment repository");
                if (equipment == null)
                    return;
                var templates = ReadRows(equipment.GetItemTemplates, "itemtemplate").ToLookup(row => row.Id);
                var links = ReadRows(equipment.GetItemTemplateClasses, "itemtemplate_itemclass").ToLookup(row => row.ItemTemplateId);
                var classes = ReadRows(equipment.GetItemClasses, "itemclass").ToLookup(row => row.Id);
                var entities = ReadRows(() => Required(_world.EntityClasses, "entity_class").Get(), "entity_class")
                    .ToLookup(row => row.Id);
                var weapons = ReadRows(equipment.GetWeaponItems, "itemtemplate_weapon").ToLookup(row => row.Id);
                var weaponClasses = ReadRows(equipment.GetWeaponClasses, "weaponclass").ToLookup(row => row.Id);
                var kinds = groups.ToLookup(group => (group.ProfileKey, group.GroupKey));
                foreach (var item in items)
                {
                    var label = ItemLabel(item);
                    var native = templates[item.ItemTemplateId].ToArray();
                    if (native.Length != 1)
                        Error($"{label}: expected one native template, found {native.Length}.");
                    else
                    {
                        var quality = (LootQuality)native[0].QualityId;
                        if (!Enum.IsDefined(typeof(LootQuality), quality))
                            Error($"{label}: unknown native quality {native[0].QualityId}.");
                        if (quality == LootQuality.Mission)
                            Error($"{label}: Mission-quality templates are not ordinary loot.");
                        foreach (var group in kinds[(item.ProfileKey, item.GroupKey)])
                            if ((quality == LootQuality.Junk) != (group.Kind == LootGroupKind.Junk))
                                Error($"{label}: Junk templates must belong only to Junk groups; Junk groups require Junk templates.");
                    }
                    var linked = links[item.ItemTemplateId].ToArray();
                    if (linked.Length != 1)
                    {
                        Error($"{label}: expected one native item-class link, found {linked.Length}.");
                        continue;
                    }
                    var classId = linked[0].ItemClass;
                    var entity = entities[classId].ToArray();
                    List<AugmentationType> augmentations = null;
                    if (entity.Length != 1)
                        Error($"{label}: expected one native entity class {classId}, found {entity.Length}.");
                    else
                    {
                        if (entity[0].AugList != null)
                            augmentations = EntityClassManager.ParseAugmentations(entity[0].AugList);
                        if (augmentations?.Contains(AugmentationType.Item) != true)
                            Error($"{label}: native class {classId} is missing the inventory-capable Item augmentation.");
                    }
                    if (native.Length == 1)
                    {
                        var category = (InventoryCategory)native[0].InventoryCategory;
                        if (category == 0 && augmentations != null)
                            category = ItemManager.DefaultInventoryCategory(entity[0].ClassName, augmentations);
                        if ((int)category < 1 || (int)category > InventoryManager.PersonalCategoryCount)
                            Error($"{label}: native class {classId} has unusable inventory category {native[0].InventoryCategory} (effective {(int)category}).");
                    }
                    if (weapons[item.ItemTemplateId].Any() && weaponClasses[classId].Count() != 1)
                        Error($"{label}: native class {classId} requires one weapon class for its weapon template metadata, found {weaponClasses[classId].Count()}.");
                    var itemClasses = classes[classId].ToArray();
                    if (itemClasses.Length != 1)
                        Error($"{label}: expected one native item class {classId}, found {itemClasses.Length}.");
                    else
                    {
                        if (itemClasses[0].StackSize == 0 || item.MaximumQuantity > itemClasses[0].StackSize)
                            Error($"{label}: quantity exceeds usable native class {classId} stack size {itemClasses[0].StackSize}.");
                        if (itemClasses[0].HidenInventoryFlag != 0)
                            Error($"{label}: native class {classId} is hidden from inventory and is not usable ordinary loot.");
                    }
                }
            }

            private void Key(string key, int length, string label)
            {
                try { OrdinaryLootGroup.ValidateKey(key, length, nameof(key)); }
                catch (ArgumentException error) { Error($"{label}: {error.Message}"); }
            }

            private void Error(string diagnostic) => _errors.Add(diagnostic);
            private void FailIfInvalid()
            {
                if (_errors.Count > 0)
                    throw new LootContentException(_errors);
            }

            private T Read<T>(Func<T> read, string source) where T : class =>
                OrdinaryLootCoverage.Read(read, source, _errors);

            private IReadOnlyList<T> ReadRows<T>(Func<List<T>> read, string source) where T : class =>
                Rows(Read(read, source) ?? new List<T>(), source);

            private IReadOnlyList<T> Rows<T>(IEnumerable<T> rows, string source) where T : class
            {
                var result = new List<T>();
                foreach (var row in rows)
                    if (row == null) Error($"{source}: null row.");
                    else result.Add(row);
                return result;
            }

            private static T Required<T>(T repository, string source) where T : class =>
                repository ?? throw new NotSupportedException($"Missing {source} repository.");

            private static string ItemLabel(LootItemEntry item) =>
                $"profile {item.ProfileKey}, group {item.GroupKey}, template {item.ItemTemplateId}";
        }
    }
}
