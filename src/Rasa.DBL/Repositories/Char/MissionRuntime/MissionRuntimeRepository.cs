using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Repositories.Char.MissionRuntime
{
    using Context.Char;
    using Structures.Char;

    public sealed class MissionRuntimeRepository
    {
        private readonly CharContext _context;
        public MissionRuntimeRepository(CharContext context) => _context = context;

        public CharacterMissionEntry ReadAssignment(uint characterId, uint missionId) =>
            _context.CharacterMissionEntries.AsNoTracking().SingleOrDefault(entry =>
                entry.CharacterId == characterId && entry.MissionId == missionId);

        public IReadOnlyList<CharacterMissionHistoryEntry> History(uint characterId) =>
            _context.Set<CharacterMissionHistoryEntry>().AsNoTracking()
                .Where(entry => entry.CharacterId == characterId).OrderBy(entry => entry.AssignmentGeneration).ToArray();
        public CharacterMissionHistoryEntry ReadHistory(string assignmentId) =>
            _context.Set<CharacterMissionHistoryEntry>().AsNoTracking()
                .SingleOrDefault(entry => entry.AssignmentId == assignmentId);

        public bool EverSucceeded(uint characterId, uint missionId) =>
            _context.Set<CharacterMissionHistoryEntry>()
                .Any(entry => entry.CharacterId == characterId && entry.MissionId == missionId &&
                    (entry.Rewarded || entry.Outcome == 1 || entry.Outcome == 4));

        public bool HasHistory(uint characterId, uint missionId) => EverSucceeded(characterId, missionId);

        public bool HasPendingReward(uint characterId, uint missionId) =>
            _context.Set<CharacterMissionHistoryEntry>().Any(entry => entry.CharacterId == characterId &&
                entry.MissionId == missionId && entry.Outcome == 1 && !entry.Rewarded);

        public CharacterMissionHistoryEntry LatestTerminal(uint characterId, uint missionId) =>
            _context.Set<CharacterMissionHistoryEntry>().AsNoTracking().Where(entry =>
                entry.CharacterId == characterId && entry.MissionId == missionId)
                .OrderByDescending(entry => entry.AssignmentGeneration)
                .ThenByDescending(entry => entry.CompletedAtUtc).ThenByDescending(entry => entry.AssignmentId).FirstOrDefault();

        public DateTime? LastRewardedAtUtc(uint characterId, uint missionId) =>
            _context.Set<CharacterMissionHistoryEntry>().Where(entry =>
                entry.CharacterId == characterId && entry.MissionId == missionId && entry.Rewarded)
                .Max(entry => entry.RewardedAtUtc ?? (DateTime?)entry.CompletedAtUtc);

        public bool WasRewarded(string assignmentId) =>
            _context.Set<CharacterMissionHistoryEntry>().Any(entry => entry.AssignmentId == assignmentId && entry.Rewarded) ||
            _context.Set<MissionReceiptEntry>().Any(entry =>
                entry.OwnerId == assignmentId && entry.OperationKey == "mission-reward");

        public bool HasRewardWindow(uint characterId, uint missionId, DateTime windowStartUtc, string exceptAssignmentId = null) =>
            _context.Set<CharacterMissionHistoryEntry>().Any(entry => entry.CharacterId == characterId &&
                entry.MissionId == missionId && entry.RewardWindowStartUtc == windowStartUtc &&
                entry.AssignmentId != exceptAssignmentId);

        public void FinalizeReward(string assignmentId, uint generation, DateTime utcNow, DateTime? windowStartUtc)
        {
            var history = _context.Set<CharacterMissionHistoryEntry>().Find(assignmentId);
            var receipt = _context.Set<MissionReceiptEntry>().Find(assignmentId, generation, "mission-reward");
            if (history == null || !history.Rewarded || history.AssignmentGeneration != generation || receipt == null)
                throw new InvalidOperationException("Reward finalization requires this assignment's history and receipt.");
            history.RewardedAtUtc = utcNow;
            history.RewardWindowStartUtc = windowStartUtc;
            receipt.CreatedAtUtc = utcNow;
        }

        public void Archive(CharacterMissionEntry mission, DateTime utcNow, DateTime? rewardWindowStartUtc = null)
        {
            if (mission.MissionState is not (1 or 2 or 3 or 4) ||
                string.IsNullOrEmpty(mission.AssignmentId) || mission.Generation == 0)
                throw new InvalidOperationException("Only an identified terminal assignment can be archived.");
            var existing = _context.Set<CharacterMissionHistoryEntry>().Find(mission.AssignmentId);
            if (existing == null)
                _context.Add(new CharacterMissionHistoryEntry
                {
                    CharacterId = mission.CharacterId, MissionId = mission.MissionId,
                    AssignmentId = mission.AssignmentId, ContentRevision = mission.ContentRevision,
                    AssignmentGeneration = mission.Generation,
                    CompletedAtUtc = utcNow, Rewarded = mission.MissionState == 4, Outcome = mission.MissionState,
                    RewardedAtUtc = mission.MissionState == 4 ? utcNow : null,
                    RewardWindowStartUtc = mission.MissionState == 4 ? rewardWindowStartUtc : null
                });
            else
            {
                if (existing.CharacterId != mission.CharacterId || existing.MissionId != mission.MissionId ||
                    existing.AssignmentGeneration != mission.Generation)
                    throw new InvalidOperationException("Terminal history assignment ownership changed.");
                if (!existing.Rewarded && mission.MissionState == 4)
                {
                    existing.Rewarded = true;
                    existing.Outcome = 4;
                    existing.RewardedAtUtc = utcNow;
                    existing.RewardWindowStartUtc = rewardWindowStartUtc;
                }
            }
            foreach (var step in _context.CharacterMissionScenarioStepEntries
                .Where(entry => entry.CharacterId == mission.CharacterId && entry.MissionId == mission.MissionId).ToArray())
                if (!HasReceipt(mission.AssignmentId, 0, "legacy:" + step.StepKey))
                    _context.Add(new MissionReceiptEntry
                    {
                        OwnerId = mission.AssignmentId, OperationKey = "legacy:" + step.StepKey,
                        Kind = "Legacy", CreatedAtUtc = utcNow
                    });
        }

        public MissionSceneEntry Scene(string runId) => _context.Set<MissionSceneEntry>().Find(runId);
        public MissionSceneEntry ReadScene(string runId) =>
            _context.Set<MissionSceneEntry>().AsNoTracking().SingleOrDefault(entry => entry.RunId == runId);
        public MissionSceneParticipantEntry ReadParticipant(string runId, uint characterId) =>
            _context.Set<MissionSceneParticipantEntry>().AsNoTracking()
                .SingleOrDefault(entry => entry.RunId == runId && entry.CharacterId == characterId);
        public MissionSceneEntry AssignmentScene(string assignmentId) =>
            _context.Set<MissionSceneEntry>().Local.SingleOrDefault(entry => entry.AssignmentId == assignmentId) ??
            _context.Set<MissionSceneEntry>().SingleOrDefault(entry => entry.AssignmentId == assignmentId);
        public MissionTimerEntry Timer(string runId, string name) => _context.Set<MissionTimerEntry>().Find(runId, name);
        public IReadOnlyList<MissionSceneEntry> Scenes(string mapKey) =>
            _context.Set<MissionSceneEntry>().Where(entry => entry.MapKey == mapKey).ToArray();
        public IReadOnlyList<MissionSceneEntry> Scenes(uint characterId, uint missionId) =>
            _context.Set<MissionSceneEntry>()
                .Where(entry => entry.OwnerCharacterId == characterId && entry.MissionId == missionId).ToArray();
        public IReadOnlyList<MissionSceneEntry> ScenesForCharacter(uint characterId) =>
            _context.Set<MissionSceneEntry>().Where(entry => entry.OwnerCharacterId == characterId)
                .OrderBy(entry => entry.RunId).ToArray();
        public MissionActorLeaseEntry Lease(string mapKey, string spawnKey) =>
            _context.Set<MissionActorLeaseEntry>().Find(mapKey, spawnKey);
        public MissionActorLeaseEntry ReadLease(string mapKey, string spawnKey) =>
            _context.Set<MissionActorLeaseEntry>().AsNoTracking()
                .SingleOrDefault(entry => entry.MapKey == mapKey && entry.SpawnKey == spawnKey);
        public IReadOnlyList<MissionActorLeaseEntry> Leases(string runId) =>
            _context.Set<MissionActorLeaseEntry>().Where(entry => entry.RunId == runId).ToArray();
        public IReadOnlyList<MissionSceneParticipantEntry> Participants(string runId) =>
            _context.Set<MissionSceneParticipantEntry>().Where(entry => entry.RunId == runId).ToArray();
        public IReadOnlyList<MissionSceneParticipantEntry> Participations(string assignmentId)
        {
            var entries = _context.Set<MissionSceneParticipantEntry>();
            entries.Where(entry => entry.AssignmentId == assignmentId).Load();
            return entries.Local.Where(entry => entry.AssignmentId == assignmentId).ToArray();
        }
        public IReadOnlyList<MissionTimerEntry> Timers(string runId) =>
            _context.Set<MissionTimerEntry>().Where(entry => entry.RunId == runId).ToArray();
        public IReadOnlyList<MissionWorldEffectEntry> Effects(string runId) =>
            _context.Set<MissionWorldEffectEntry>().Where(entry => entry.RunId == runId).ToArray();
        public IReadOnlyList<MissionWorldEffectEntry> ForwardedEffects(string sourceRunId) =>
            _context.Set<MissionWorldEffectEntry>().Where(entry => entry.SourceRunId == sourceRunId).ToArray();
        public MissionActorStateEntry ActorState(string runId, string role, uint generation) =>
            _context.Set<MissionActorStateEntry>().Find(runId, role, generation);
        public IReadOnlyList<MissionActorStateEntry> ActorStates(string runId) =>
            _context.Set<MissionActorStateEntry>().Where(entry => entry.RunId == runId).ToArray();
        public IReadOnlyList<MissionSceneMessageEntry> Messages(string runId) =>
            _context.Set<MissionSceneMessageEntry>().Where(entry => entry.RunId == runId).OrderBy(entry => entry.Id).ToArray();
        public IReadOnlyList<string> DefeatedSharedActors(uint characterId, uint mapContextId) =>
            (from actor in _context.Set<MissionActorStateEntry>()
             join scene in _context.Set<MissionSceneEntry>() on actor.RunId equals scene.RunId
             where actor.OwnerCharacterId == characterId && actor.MapContextId == mapContextId &&
                 actor.Generation == scene.Generation && actor.SharedKey != null && actor.Outcome == "Defeated"
             select actor.SharedKey).Distinct().ToArray();
        public bool HasReceipt(string ownerId, uint generation, string key) =>
            _context.Set<MissionReceiptEntry>().Any(entry => entry.OwnerId == ownerId &&
                entry.Generation == generation && entry.OperationKey == key);
        public bool HasOutcome(string eventId) => _context.Set<MissionOutcomeEntry>().Any(entry => entry.EventId == eventId);
        public IReadOnlyList<MissionCreditDeliveryEntry> Deliveries(uint characterId) =>
            _context.Set<MissionCreditDeliveryEntry>()
                .Where(entry => entry.CharacterId == characterId && entry.Status == "Pending").ToArray();

        public void Add<T>(T entry) where T : class => _context.Add(entry);
        public void Remove<T>(T entry) where T : class => _context.Remove(entry);
        public void Flush() => _context.SaveChanges();
    }
}
