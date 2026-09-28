using System;
using System.Collections.Generic;
using System.Linq;
using Rasa.Missions.Scenes;
using Rasa.Structures;
using Rasa.Structures.Missions;
using Rasa.Structures.World;

namespace Rasa.Missions.Definitions
{
    public static class MissionItemValidation
    {
        public static bool IsItemIntent(CharacterIntent intent) =>
            intent is IssueMissionItemIntent or ConsumeMissionItemIntent or RemoveMissionItemsIntent;

        public static string BindingError(MissionItemBinding binding)
        {
            if (binding == null || string.IsNullOrWhiteSpace(binding.ItemKey) || binding.ItemKey.Length > 64 ||
                binding.ItemTemplateId == 0 || binding.MaximumQuantity == 0 ||
                !Enum.IsDefined(typeof(MissionItemScope), binding.Scope) ||
                !Enum.IsDefined(typeof(MissionItemCleanupDisposition), binding.Completion) ||
                !Enum.IsDefined(typeof(MissionItemCleanupDisposition), binding.Failure) ||
                !Enum.IsDefined(typeof(MissionItemCleanupDisposition), binding.Abandonment))
                return "Invalid mission item binding, scope, quantity or terminal disposition.";
            if (binding.Scope == MissionItemScope.CharacterOwned &&
                (binding.Completion != MissionItemCleanupDisposition.Retain ||
                 binding.Failure != MissionItemCleanupDisposition.Retain ||
                 binding.Abandonment != MissionItemCleanupDisposition.Retain))
                return "Character-owned costs cannot have automatic cleanup.";
            return null;
        }

        public static string IntentError(uint missionId, IReadOnlyDictionary<string, MissionItemBinding> bindings,
            CharacterIntent intent)
        {
            var (id, key) = intent switch
            {
                IssueMissionItemIntent issue => (issue.MissionId, issue.ItemKey),
                ConsumeMissionItemIntent consume => (consume.MissionId, consume.ItemKey),
                RemoveMissionItemsIntent remove => (remove.MissionId, remove.ItemKey),
                _ => (0U, null)
            };
            if (id != missionId || id == 0 || key == null || !bindings.TryGetValue(key, out var binding) ||
                string.IsNullOrWhiteSpace(intent.OperationKey) || intent.OperationKey.Length > 160)
                return "Mission item operation must name its mission, item binding and stable operation key.";
            if (BindingError(binding) is { } error)
                return error;
            return intent switch
            {
                IssueMissionItemIntent issue when binding.Scope == MissionItemScope.AssignmentIssued &&
                    issue.ItemTemplateId == binding.ItemTemplateId && issue.Quantity > 0 &&
                    issue.Quantity <= binding.MaximumQuantity => null,
                ConsumeMissionItemIntent consume when consume.Scope == binding.Scope && consume.Quantity > 0 &&
                    consume.Quantity <= binding.MaximumQuantity => null,
                RemoveMissionItemsIntent when binding.Scope == MissionItemScope.AssignmentIssued => null,
                _ => "Mission item operation does not match its authored template, scope or allowed quantity."
            };
        }

        public static bool ActionMatches(MissionActionDefinition action) => action.Kind switch
        {
            MissionActionKind.IssueMissionItem => action.ItemIntent is IssueMissionItemIntent,
            MissionActionKind.ConsumeMissionItem => action.ItemIntent is ConsumeMissionItemIntent,
            MissionActionKind.RemoveMissionItems => action.ItemIntent is RemoveMissionItemsIntent,
            _ => action.ItemIntent == null
        };

        public static IEnumerable<string> Errors(Mission mission, IEnumerable<CharacterIntent> sceneIntents = null)
        {
            foreach (var binding in mission.Items.Values)
                if (BindingError(binding) is { } error)
                    yield return error;
            var actions = mission.Objectives.Values.SelectMany(objective => objective.GetExecutableTransitionsOrLegacyDefault())
                .SelectMany(transition => transition.Actions).ToArray();
            foreach (var action in actions)
                if (!ActionMatches(action) || action.ItemIntent != null && action.MissionId != mission.MissionId)
                    yield return $"Mission item action {action.ActionId} has an invalid intent discriminator.";
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var intent in mission.AcceptanceItems.Concat(actions.Where(action => action.ItemIntent != null)
                    .Select(action => action.ItemIntent)).Concat((sceneIntents ?? Array.Empty<CharacterIntent>()).Where(IsItemIntent)))
            {
                if (IntentError(mission.MissionId, mission.Items, intent) is { } error)
                    yield return error;
                else if (!keys.Add(intent.OperationKey))
                    yield return $"Mission item operation key is duplicated: {intent.OperationKey}.";
            }
        }
    }
}
