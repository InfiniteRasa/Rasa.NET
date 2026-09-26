using System;
using System.Text.Json;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Repositories.Char;
using Rasa.Repositories.UnitOfWork;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Game.Missions.Persistence
{
    internal enum MissionItemTermination { Completion, Failure, Abandonment, Replacement }

    internal sealed class MissionItemPlanner
    {
        private readonly Client _client;
        private readonly ICharUnitOfWork _unit;
        private readonly MissionApplication _missions;
        private readonly InventoryManager.InventoryPlan _inventory;

        internal MissionItemPlanner(Client client, ICharUnitOfWork unit, MissionApplication missions)
        {
            _client = client;
            _unit = unit;
            _missions = missions;
            _inventory = InventoryManager.InventoryPlan.For(client, unit);
        }

        internal void Apply(CharacterIntent intent, string assignmentId, uint generation)
        {
            var (missionId, itemKey) = intent switch
            {
                IssueMissionItemIntent issue => (issue.MissionId, issue.ItemKey),
                ConsumeMissionItemIntent consume => (consume.MissionId, consume.ItemKey),
                RemoveMissionItemsIntent remove => (remove.MissionId, remove.ItemKey),
                _ => throw new GameplayRejectionException("Not a mission item operation.")
            };
            if (string.IsNullOrWhiteSpace(intent.OperationKey) || intent.OperationKey.Length > 160 ||
                !_missions.TryGetOperationalMission(missionId, out var mission) ||
                itemKey == null || !mission.Items.TryGetValue(itemKey, out var binding))
                throw new GameplayRejectionException("Mission item operation has no authored binding or valid operation key.");
            if (MissionItemValidation.IntentError(missionId, mission.Items, intent) is { } intentError)
                throw new GameplayRejectionException(intentError);
            var assignment = _unit.CharacterMissions.GetByCharacterAndMission(_client.Player.Id, missionId);
            if (assignment == null || assignment.AssignmentId != assignmentId || assignment.Generation != generation ||
                string.IsNullOrWhiteSpace(assignmentId) || assignmentId.Length != 32 || generation == 0 ||
                assignment != null && assignment.ContentRevision != mission.ContentRevision &&
                    assignment.ContentRevision is not ("legacy" or "unversioned"))
                throw new GameplayRejectionException("Mission item assignment or generation changed.");
            RequireNotQuarantined(assignment);
            _inventory.RequireAssignment(missionId, assignmentId, generation, mission.Items);
            var assignmentRevision = assignment.ContentRevision;
            TransactionValidation.Add(_unit, () =>
            {
                var current = _unit.CharacterMissions.Runtime.ReadAssignment(_client.Player.Id, missionId);
                if (current?.AssignmentId != assignmentId || current.Generation != generation ||
                    current.ContentRevision != assignmentRevision)
                    throw new GameplayRejectionException("Mission item assignment changed during persistence.");
            });
            var payload = JsonSerializer.Serialize(intent, MissionContentCodec.Options);
            var receipt = _unit.CharacterMissionItems.GetReceipt(_client.Player.Id, assignmentId, intent.OperationKey);
            if (receipt != null)
            {
                if (receipt.MissionId != missionId || receipt.Generation != generation || receipt.Payload != payload)
                    throw new GameplayRejectionException("Mission item operation key was reused with different ownership or content.");
                return;
            }
            if (intent is not RemoveMissionItemsIntent && assignment.MissionState != (uint)MissionState.Active)
                throw new GameplayRejectionException("New item issuance and costs require an active assignment.");
            var owner = new MissionItemOwnership(_client.Player.Id, missionId, assignmentId, generation, itemKey);
            switch (intent)
            {
                case IssueMissionItemIntent issue:
                    if (binding.Scope != MissionItemScope.AssignmentIssued || issue.ItemTemplateId != binding.ItemTemplateId ||
                        issue.Quantity == 0 || (ulong)_inventory.OwnedQuantity(owner) + issue.Quantity > binding.MaximumQuantity)
                        throw new GameplayRejectionException("Mission item issuance exceeds its authored scope/template/quantity.");
                    _inventory.Grant(issue.ItemTemplateId, issue.Quantity, owner);
                    break;
                case ConsumeMissionItemIntent consume:
                    if (consume.Scope != binding.Scope || consume.Quantity == 0 || consume.Quantity > binding.MaximumQuantity)
                        throw new GameplayRejectionException("Mission item consumption exceeds its authored scope/quantity.");
                    _inventory.Consume(binding.ItemTemplateId, consume.Quantity,
                        consume.Scope == MissionItemScope.AssignmentIssued ? owner : null);
                    break;
                case RemoveMissionItemsIntent:
                    if (binding.Scope != MissionItemScope.AssignmentIssued)
                        throw new GameplayRejectionException("Cleanup cannot remove character-owned items.");
                    _inventory.Remove(owner);
                    break;
                default:
                    throw new GameplayRejectionException("Unsupported mission item operation.");
            }
            _unit.CharacterMissionItems.AddReceipt(new CharacterMissionItemReceiptEntry
            {
                CharacterId = _client.Player.Id, MissionId = missionId, AssignmentId = assignmentId, Generation = generation,
                OperationKey = intent.OperationKey, Payload = payload
            });
        }

        internal void Publish(Client client) => _inventory.Publish(client);

        internal void Cleanup(CharacterMissionEntry assignment, MissionItemTermination reason, bool removingAssignment = false,
            CharacterMissionEntry replacement = null)
        {
            if (assignment?.CharacterId != _client.Player.Id ||
                !_missions.TryGetOperationalMission(assignment.MissionId, out var mission) ||
                assignment.Generation == 0 || assignment.AssignmentId?.Length != 32 ||
                assignment.ContentRevision != mission.ContentRevision && assignment.ContentRevision is not ("legacy" or "unversioned"))
                throw new GameplayRejectionException("Mission item cleanup has no authoritative assignment.");
            RequireNotQuarantined(assignment);
            _inventory.RequireAssignment(assignment.MissionId, assignment.AssignmentId, assignment.Generation, mission.Items);
            var assignmentId = assignment.AssignmentId;
            var generation = assignment.Generation;
            var missionId = assignment.MissionId;
            var assignmentRevision = assignment.ContentRevision;
            TransactionValidation.Add(_unit, () =>
            {
                var current = _unit.CharacterMissions.Runtime.ReadAssignment(_client.Player.Id, missionId);
                if (replacement != null && current?.AssignmentId == replacement.AssignmentId &&
                    current.Generation == replacement.Generation && current.ContentRevision == replacement.ContentRevision)
                    return;
                if (current == null ? !removingAssignment && reason != MissionItemTermination.Abandonment :
                    current.AssignmentId != assignmentId || current.Generation != generation || current.ContentRevision != assignmentRevision)
                    throw new GameplayRejectionException("Mission cleanup assignment changed during persistence.");
            });
            foreach (var binding in mission.Items.Values)
            {
                if (MissionItemValidation.BindingError(binding) is { } error)
                    throw new GameplayRejectionException(error);
                var disposition = reason switch
                {
                    MissionItemTermination.Completion => binding.Completion,
                    MissionItemTermination.Failure => binding.Failure,
                    MissionItemTermination.Abandonment => binding.Abandonment,
                    MissionItemTermination.Replacement => MissionItemCleanupDisposition.Remove,
                    _ => throw new GameplayRejectionException("Unknown mission item cleanup disposition.")
                };
                if (binding.Scope == MissionItemScope.AssignmentIssued && disposition == MissionItemCleanupDisposition.Remove)
                    _inventory.Remove(new MissionItemOwnership(assignment.CharacterId, assignment.MissionId,
                        assignment.AssignmentId, assignment.Generation, binding.ItemKey));
            }
        }

        private void RequireNotQuarantined(CharacterMissionEntry assignment)
        {
            if (_unit.CharacterMissionItems.GetQuarantine(assignment.CharacterId, assignment.AssignmentId) is { } quarantine)
                throw new GameplayRejectionException($"Mission item ownership is quarantined: {quarantine.Reason}");
        }
    }
}
