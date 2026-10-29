using System;
using System.Linq;
using Rasa.Data;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures;

namespace Rasa.Managers
{
    /// <summary>
    /// An assignment-issued item binding with a <see cref="MissionItemBinding.CounterObjectiveId"/>: while that objective
    /// is open, the client shows how many of the item the assignment holds as the objective's item counter ("n/x" with the
    /// item's name). The generic form of <see cref="HerbalRemedyProgress"/>, for objectives the client has no counter text
    /// for whose items a scene gives (no ItemAcquired event).
    /// </summary>
    internal static class IssuedItemProgress
    {
        internal static MissionInfo Apply(Manifestation player, Mission definition, MissionLog assignment, MissionInfo info)
        {
            if (assignment.State != MissionState.Active || player.Inventory?.PersonalInventory == null)
                return info;
            foreach (var binding in definition.Items.Values.Where(Counts))
            {
                var objective = info.ObjectivesList.SingleOrDefault(o => o.ObjectiveId == binding.CounterObjectiveId);
                if (objective == null || objective.State != MissionObjectiveState.Incomplete || ClassOf(binding) is not { } itemClass)
                    continue;
                objective.ItemCounters[itemClass] = new MissionObjectiveItemCounter
                {
                    CounterValue = Value(player, definition.MissionId, assignment, binding),
                    TargetValue = binding.MaximumQuantity
                };
            }
            return info;
        }

        internal static bool Counts(MissionItemBinding binding) =>
            binding.CounterObjectiveId != 0 && binding.Scope == MissionItemScope.AssignmentIssued;

        internal static uint? ClassOf(MissionItemBinding binding) =>
            ItemManager.Instance.GetItemTemplateById(binding.ItemTemplateId) is { } template ? (uint)template.Class : null;

        /// <summary>How many of the binding's item the assignment holds, at most its maximum.</summary>
        internal static uint Value(Manifestation player, uint missionId, MissionLog assignment, MissionItemBinding binding)
        {
            ulong quantity = 0;
            foreach (var id in player.Inventory.PersonalInventory.Where(id => id != 0).Distinct())
            {
                var item = EntityManager.Instance.GetItem(id);
                if (item?.ItemTemplateId == binding.ItemTemplateId && item.MissionOwnership is { } owner &&
                    owner.CharacterId == player.Id && owner.MissionId == missionId && owner.ItemKey == binding.ItemKey &&
                    owner.AssignmentId == assignment.AssignmentId && owner.Generation == assignment.Generation)
                    quantity += item.StackSize;
            }
            return (uint)Math.Min(quantity, binding.MaximumQuantity);
        }
    }
}
