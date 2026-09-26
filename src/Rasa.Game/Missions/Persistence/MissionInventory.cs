namespace Rasa.Game.Missions.Persistence
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Managers;
    using Repositories.Char;
    using Data;
    using Structures.Char;

    internal static class MissionInventory
    {
        internal static Action<Client> PlanAcceptance(Client client, ICharUnitOfWork unit,
            MissionApplication manager, uint missionId)
        {
            if (!manager.TryGetOperationalMission(missionId, out var mission) || mission.AcceptanceItems.Count == 0)
                return Plan(client, unit, manager, missionId);
            var assignment = unit.CharacterMissions.GetByCharacterAndMission(client.Player.Id, missionId)
                ?? throw new Structures.GameplayRejectionException("Accepted mission has no assignment.");
            var planner = new MissionItemPlanner(client, unit, manager);
            foreach (var intent in mission.AcceptanceItems)
                planner.Apply(intent, assignment.AssignmentId, assignment.Generation);
            return planner.Publish;
        }

        internal static Action<Client> Plan(Client client, ICharUnitOfWork unit,
            MissionApplication manager, uint missionId) => Plan(client, unit, manager, new[] { missionId });

        internal static Action<Client> Plan(Client client, ICharUnitOfWork unit,
            MissionApplication manager, IEnumerable<uint> missionIds)
        {
            var ids = missionIds.Distinct()
                .Where(id => manager.TryGetOperationalMission(id, out var mission) && mission.Items.Count > 0).ToArray();
            if (ids.Length > 0)
            {
                var plan = new MissionItemPlanner(client, unit, manager);
                foreach (var id in ids)
                {
                    var assignment = unit.CharacterMissions.GetByCharacterAndMission(client.Player.Id, id);
                    if (assignment?.MissionState == (uint)MissionState.Failed)
                        plan.Cleanup(assignment, MissionItemTermination.Failure);
                    else if (assignment != null && (MissionState)assignment.MissionState is MissionState.Success or MissionState.Completed)
                        plan.Cleanup(assignment, MissionItemTermination.Completion);
                }
                return plan.Publish;
            }
            return null;
        }

        internal static Action<Client> PlanTermination(Client client, ICharUnitOfWork unit,
            MissionApplication manager, CharacterMissionEntry assignment, MissionItemTermination reason,
            bool removingAssignment = false, CharacterMissionEntry replacement = null)
        {
            if (!manager.TryGetOperationalMission(assignment.MissionId, out var mission) || mission.Items.Count == 0)
                return null;
            var plan = new MissionItemPlanner(client, unit, manager);
            plan.Cleanup(assignment, reason, removingAssignment, replacement);
            return plan.Publish;
        }
    }
}
