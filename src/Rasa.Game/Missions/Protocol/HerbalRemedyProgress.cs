using System;
using System.Linq;
using Rasa.Data;
using Rasa.Missions.Scenes;
using Rasa.Structures;

namespace Rasa.Managers
{
    /// <summary>Retail objective 695.1 has no generic counter label; use its native item counter.</summary>
    internal static class HerbalRemedyProgress
    {
        internal static MissionInfo Apply(Manifestation player, Mission definition, MissionLog assignment, MissionInfo info)
        {
            if (definition.MissionId != 695 || assignment.State != MissionState.Active ||
                !definition.Items.TryGetValue("tinctu-herbs", out var binding) || binding.ItemTemplateId != 2326 ||
                binding.Scope != MissionItemScope.AssignmentIssued || binding.TurnInQuantity == 0)
                return info;
            var objective = info.ObjectivesList.SingleOrDefault(o => o.ObjectiveId == 1);
            if (objective == null || objective.State != MissionObjectiveState.Incomplete || player.Inventory?.PersonalInventory == null)
                return info;
            ulong quantity = 0;
            foreach (var id in player.Inventory.PersonalInventory.Where(id => id != 0).Distinct())
            {
                var item = EntityManager.Instance.GetItem(id);
                if (item?.ItemTemplateId == binding.ItemTemplateId && item.MissionOwnership is { } owner &&
                    owner.CharacterId == player.Id && owner.MissionId == 695 && owner.ItemKey == binding.ItemKey &&
                    owner.AssignmentId == assignment.AssignmentId && owner.Generation == assignment.Generation)
                    quantity += item.StackSize;
            }
            objective.ItemCounters[10513] = new MissionObjectiveItemCounter {
                CounterValue = (uint)Math.Min(quantity, binding.TurnInQuantity), TargetValue = binding.TurnInQuantity };
            return info;
        }
    }
}
