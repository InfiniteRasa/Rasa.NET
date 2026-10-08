using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Repositories.Char;
using Rasa.Repositories.UnitOfWork;
using Rasa.Repositories.World;
using Rasa.Structures;

namespace Rasa.Game.Missions.Persistence
{
    internal static class MissionLootPlanner
    {
        internal sealed record QualifiedDrop(uint CharacterId, MissionLog Assignment,
            MissionItemBinding Binding, uint Quantity);

        // Qualification lasts only as long as the existing corpse loot row, not as another durable ledger.
        private static readonly ConditionalWeakTable<LootItem, QualifiedDrop> Qualifications = new();

        internal static bool HasCandidates(Client client, MissionApplication missions) =>
            client.Player.Missions.Values.Any(log => log.State == MissionState.Active &&
                missions.TryGetOperationalMission(log.MissionId, out var mission) &&
                mission.Items.Values.Any(binding => binding.Drop != null));

        internal static void Attach(LootItem item, QualifiedDrop drop) => Qualifications.Add(item, drop);

        internal static IReadOnlyList<QualifiedDrop> Plan(Client client, LootDispenser loot,
            MissionApplication missions, ICharUnitOfWork unit, Func<int, int, int> roll)
        {
            var planned = new List<QualifiedDrop>();
            if (!IsCurrentSource(client, loot))
                return planned;
            var incoming = loot.LootItems.Where(item => !item.Taken && item.MayTake(client.Player.EntityId))
                .GroupBy(item => item.ItemTemplateId)
                .ToDictionary(group => group.Key, group => group.Sum(item => (long)item.ItemQuantity));
            foreach (var log in client.Player.Missions.Values.OrderBy(log => log.MissionId))
            {
                if (log.State != MissionState.Active ||
                    !missions.TryGetOperationalMission(log.MissionId, out var mission))
                    continue;
                foreach (var binding in mission.Items.Values.OrderBy(binding => binding.ItemKey, StringComparer.Ordinal))
                {
                    if (binding.Drop == null || binding.Drop.ChancePercent == 0 ||
                        binding.Drop.MapContextId != client.Player.MapContextId ||
                        !binding.Drop.CreatureIds.Contains(loot.Corpse.DbId))
                        continue;
                    var candidate = new QualifiedDrop(client.Player.Id, log, binding, 0);
                    if (!IsCurrentAssignment(client, candidate, missions, unit))
                        continue;
                    var target = Target(client, candidate, missions, unit);
                    if (target == 0)
                        continue;
                    InventoryManager.InventoryPlan.For(client, unit);
                    var needed = (long)target - HeldQuantity(client, binding.ItemTemplateId, unit) -
                        incoming.GetValueOrDefault(binding.ItemTemplateId);
                    if (needed <= 0 || binding.Drop.ChancePercent != 100 && roll(0, 100) >= binding.Drop.ChancePercent)
                        continue;
                    var quantity = (uint)Math.Min(needed, binding.Drop.Quantity);
                    var selected = candidate with { Quantity = quantity };
                    planned.Add(selected);
                    incoming[binding.ItemTemplateId] = incoming.GetValueOrDefault(binding.ItemTemplateId) + quantity;
                    TransactionValidation.AtCommitBoundary(unit, () =>
                    {
                        if (!MatchesSource(client, loot, binding) || !IsCurrentAssignment(client, selected, missions, unit))
                            throw new GameplayRejectionException("Mission loot assignment or source changed during creation.");
                    });
                }
            }
            return planned;
        }

        internal static void ValidateClaim(Client client, LootDispenser loot, IReadOnlyList<LootItem> items,
            MissionApplication missions, ICharUnitOfWork unit, Func<bool> admissionIsCurrent)
        {
            var incoming = items.Where(item => !Qualifications.TryGetValue(item, out _)).GroupBy(item => item.ItemTemplateId)
                .ToDictionary(group => group.Key, group => group.Sum(item => (long)item.ItemQuantity));
            foreach (var item in items)
            {
                if (!Qualifications.TryGetValue(item, out var drop))
                    continue;
                if (!MatchesSource(client, loot, drop.Binding) || !IsCurrentAssignment(client, drop, missions, unit) ||
                    item.ItemTemplateId != drop.Binding.ItemTemplateId || item.ItemQuantity != drop.Quantity ||
                    item.Item == null)
                    throw new GameplayRejectionException("Mission corpse loot no longer belongs to the current assignment or source.");
                InventoryManager.InventoryPlan.For(client, unit);
                var target = Target(client, drop, missions, unit);
                var planned = incoming.GetValueOrDefault(item.ItemTemplateId) + item.ItemQuantity;
                if (target == 0 || HeldQuantity(client, item.ItemTemplateId, unit) + planned > target)
                    throw new GameplayRejectionException("The current assignment no longer needs this corpse item quantity.");
                incoming[item.ItemTemplateId] = planned;
                TransactionValidation.AtCommitBoundary(unit, () =>
                {
                    if (!admissionIsCurrent() || !MatchesSource(client, loot, drop.Binding) ||
                        !IsCurrentAssignment(client, drop, missions, unit))
                        throw new GameplayRejectionException("Mission corpse loot admission changed before commit.");
                });
            }
        }

        private static bool MatchesSource(Client client, LootDispenser loot, MissionItemBinding binding) =>
            IsCurrentSource(client, loot) && binding.Drop.MapContextId == client.Player.MapContextId &&
            binding.Drop.CreatureIds.Contains(loot.Corpse.DbId);

        private static bool IsCurrentSource(Client client, LootDispenser loot)
        {
            var player = client.Player;
            var map = player.MapChannel;
            var corpse = loot.Corpse;
            return map != null && !map.IsPrivateInstance && map.OwnerCharacterId == 0 &&
                ReferenceEquals(map, loot.Map) && loot.IsLootable && !loot.FullyLooted &&
                map.LootDispensers.TryGetValue(loot.EntityId, out var dispenser) && ReferenceEquals(loot, dispenser) &&
                client.PendingTransfer == null &&
                client.State == ClientState.Ingame && player.State != CharacterState.Dead &&
                player.MapContextId == map.MapInfo.MapContextId && corpse != null &&
                corpse.CorpseLootEntityId == loot.EntityId &&
                corpse.State == CharacterState.Dead && corpse.MapContextId == player.MapContextId &&
                EntityManager.Instance.Creatures.TryGetValue(corpse.EntityId, out var registered) &&
                ReferenceEquals(corpse, registered) &&
                map.MapCellInfo.Cells.Values.Any(cell => cell.CreatureList.Contains(corpse));
        }

        private static bool IsCurrentAssignment(Client client, QualifiedDrop drop, MissionApplication missions,
            ICharUnitOfWork unit)
        {
            var expected = drop.Assignment;
            if (client.Player.Id != drop.CharacterId || expected.AssignmentId?.Length != 32 ||
                !client.Player.Missions.TryGetValue(expected.MissionId, out var runtime) ||
                runtime.State != MissionState.Active ||
                !runtime.MatchesAssignment(expected.AssignmentId, expected.Generation, expected.ContentRevision) ||
                !missions.TryGetOperationalMission(expected.MissionId, out var mission) ||
                !mission.Items.TryGetValue(drop.Binding.ItemKey, out var binding) || binding != drop.Binding ||
                MissionItemValidation.BindingError(binding) != null)
                return false;
            var current = unit.CharacterMissions.Runtime.ReadAssignment(client.Player.Id, expected.MissionId);
            return current != null && current.MissionState == (uint)MissionState.Active &&
                expected.MatchesAssignment(current.AssignmentId, current.Generation, current.ContentRevision) &&
                (current.ContentRevision == mission.ContentRevision || current.ContentRevision is "legacy" or "unversioned") &&
                unit.CharacterMissionItems.GetQuarantine(client.Player.Id, current.AssignmentId) == null;
        }

        private static uint Target(Client client, QualifiedDrop drop, MissionApplication missions, ICharUnitOfWork unit)
        {
            var binding = drop.Binding;
            var objective = unit.CharacterMissionProgress.Get(client.Player.Id,
                drop.Assignment.MissionId, binding.Drop.ObjectiveId);
            if (objective?.ObjectiveState == (byte)MissionObjectiveState.Completed)
                return binding.TurnInQuantity;
            if (objective?.ObjectiveState != (byte)MissionObjectiveState.Incomplete ||
                !missions.TryGetOperationalMission(drop.Assignment.MissionId, out var mission) ||
                !mission.Objectives.TryGetValue(binding.Drop.ObjectiveId, out var definition))
                return 0;
            var template = ItemManager.Instance.GetItemTemplateById(binding.ItemTemplateId);
            if (template == null)
                return 0;
            var itemClass = (uint)template.Class;
            return definition.GetExecutableTransitionsOrLegacyDefault()
                .Where(transition => transition.ProgressRule?.Kind == MissionProgressEventKind.ItemAcquired &&
                    transition.ProgressRule.Subjects.Contains(itemClass))
                .SelectMany(transition => transition.ItemCounters.Values)
                .Where(counter => counter.ItemClassId == itemClass)
                .Select(counter => counter.TargetValue).DefaultIfEmpty(0U).Max();
        }

        private static long HeldQuantity(Client client, uint templateId, ICharUnitOfWork unit)
        {
            var ids = unit.CharacterInventories.GetItems(client.AccountEntry.Id)
                .Where(row => row.CharacterId == client.Player.Id && row.InventoryType == (uint)InventoryType.Personal)
                .Select(row => row.ItemId).ToArray();
            var protectedIds = unit.CharacterMissionItems.GetOwners(ids).Select(owner => owner.ItemId).ToHashSet();
            return unit.Items.GetItems(ids).Where(item => item.ItemTemplateId == templateId &&
                !protectedIds.Contains(item.ItemId)).Sum(item => (long)item.StackSize);
        }

        /// <summary>
        /// The World tables that mission and actor loot is checked against, each read the first
        /// time it is asked for and kept for as long as this is: the mission catalog checks
        /// every mission with a drop as it loads, and the item templates alone are some thirty
        /// thousand rows.
        /// </summary>
        internal sealed class WorldReferences
        {
            private readonly IWorldUnitOfWork _unit;
            private HashSet<uint> _creatures;
            private HashSet<uint> _maps;
            private HashSet<uint> _templates;
            private List<Rasa.Structures.World.ItemTemplateItemClassEntry> _linkRows;
            private HashSet<uint> _classed;
            private Dictionary<uint, uint> _links;
            private Dictionary<uint, uint> _classes;
            private HashSet<uint> _entityClasses;

            internal WorldReferences(IWorldUnitOfWork unit)
            {
                _unit = unit;
            }

            internal IWorldUnitOfWork Unit => _unit;

            /// <summary>creature.id of every creature row.</summary>
            internal HashSet<uint> Creatures => _creatures ??= _unit.Creatures.Get().Select(entry => entry.Id).ToHashSet();

            /// <summary>The map context ids there are.</summary>
            internal HashSet<uint> Maps => _maps ??= _unit.MapInfos.Get().Select(entry => entry.Id).ToHashSet();

            /// <summary>The item template ids there are.</summary>
            internal HashSet<uint> Templates => _templates ??= _unit.Equipment.GetItemTemplates().Select(entry => entry.Id).ToHashSet();

            private List<Rasa.Structures.World.ItemTemplateItemClassEntry> LinkRows => _linkRows ??= _unit.Equipment.GetItemTemplateClasses();

            /// <summary>The item templates that have an item class.</summary>
            internal HashSet<uint> ClassedTemplates => _classed ??= LinkRows.Select(entry => entry.ItemTemplateId).ToHashSet();

            /// <summary>Item template to its item class.</summary>
            internal Dictionary<uint, uint> TemplateClasses =>
                _links ??= LinkRows.ToDictionary(entry => entry.ItemTemplateId, entry => entry.ItemClass);

            /// <summary>Item class to its stack size.</summary>
            internal Dictionary<uint, uint> StackSizes =>
                _classes ??= _unit.Equipment.GetItemClasses().ToDictionary(entry => entry.Id, entry => entry.StackSize);

            /// <summary>The entity class ids there are.</summary>
            internal HashSet<uint> EntityClasses => _entityClasses ??= _unit.EntityClasses.Get().Select(entry => entry.Id).ToHashSet();
        }

        internal static void ValidateReferences(Mission mission, IWorldUnitOfWork unit) =>
            ValidateReferences(mission, new WorldReferences(unit));

        /// <param name="references">Shared by the caller across missions, so the tables are read once and not once a mission.</param>
        internal static void ValidateReferences(Mission mission, WorldReferences references)
        {
            var bindings = mission.Items.Values.Where(binding => binding.Drop != null).ToArray();
            if (bindings.Length == 0)
                return;
            var unit = references.Unit;
            if (unit.Creatures == null)
                throw new MissionRuleException("Mission loot validation requires the World creature repository.");
            var creatures = references.Creatures;
            if (unit.MapInfos == null)
                throw new MissionRuleException("Mission loot validation requires the World map repository.");
            var maps = references.Maps;
            if (unit.Equipment == null)
                throw new MissionRuleException("Mission loot validation requires the World equipment repository.");
            var templates = references.Templates;
            var links = references.TemplateClasses;
            var classes = references.StackSizes;
            if (unit.EntityClasses == null)
                throw new MissionRuleException("Mission loot validation requires the World entity-class repository.");
            var entityClasses = references.EntityClasses;
            foreach (var binding in bindings)
            {
                if (MissionItemValidation.BindingError(binding) is { } error)
                    throw new MissionRuleException($"Mission {mission.MissionId}: {error}");
                if (!maps.Contains(binding.Drop.MapContextId) || binding.Drop.CreatureIds.Any(id => !creatures.Contains(id)))
                    throw new MissionRuleException($"Mission {mission.MissionId}: item {binding.ItemKey} has a missing drop source or map.");
                if (!templates.Contains(binding.ItemTemplateId) ||
                    !links.TryGetValue(binding.ItemTemplateId, out var itemClass) ||
                    !entityClasses.Contains(itemClass) || !classes.TryGetValue(itemClass, out var stackSize) ||
                    binding.Drop.Quantity > stackSize)
                    throw new MissionRuleException($"Mission {mission.MissionId}: item {binding.ItemKey} has an invalid drop template or stack size.");
                if (!mission.Objectives.TryGetValue(binding.Drop.ObjectiveId, out var objective) ||
                    !objective.GetExecutableTransitionsOrLegacyDefault().Any(transition =>
                        transition.ProgressRule?.Kind == MissionProgressEventKind.ItemAcquired &&
                        transition.ProgressRule.Subjects.Contains(itemClass) &&
                        transition.ItemCounters.TryGetValue(itemClass, out var counter) && counter.TargetValue > 0))
                    throw new MissionRuleException($"Mission {mission.MissionId}: item {binding.ItemKey} drop does not match its acquisition objective.");
            }
        }
    }
}
