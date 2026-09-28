using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Rasa.Missions.Runtime;
using ProgressCandidate = Rasa.Missions.Runtime.MissionProgressCandidate;
using System.Text.Json;
using Rasa.Game.Missions.Content;
using Rasa.Repositories.World;
using Rasa.Game.Missions.Persistence;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets;
    using Packets.MapChannel.Server;
    using Packets.Mission.Server;
    using Repositories.Char;
    using Repositories.Char.CharacterMissionProgress;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;
    using Structures.Missions;
    using Structures.World;

    internal sealed class MissionJournalAdapter
    {
        private readonly MissionContentCatalog _catalog;
        internal MissionJournalAdapter(MissionContentCatalog catalog) => _catalog = catalog;
        internal void Hydrate(
            Manifestation player,
            IReadOnlyList<CharacterMissionEntry> rows,
            CharacterMissionProgressSnapshot progress)
        {
            player.Missions = BuildHydration(player.Id, rows, progress).Missions;
            foreach (var row in rows.Where(row => row.MissionState is 1 or 4))
            {
                player.MissionHistory[row.MissionId] = (MissionState)row.MissionState;
                player.MissionSuccessHistory.Add(row.MissionId);
            }
        }

        internal void HydrateAndClearInvalid(
            Manifestation player,
            ICharUnitOfWork unitOfWork)
        {
            HydrationResult result = null;
            IReadOnlyList<CharacterMissionHistoryEntry> history = null;
            unitOfWork.ExecuteTransaction(() =>
            {
                history = unitOfWork.CharacterMissions.Runtime.History(player.Id);
                result = BuildHydration(
                    player.Id,
                    unitOfWork.CharacterMissions.Get(player.Id),
                    unitOfWork.CharacterMissionProgress.Get(player.Id));
                foreach (var missionId in result.InvalidMissionIds)
                    unitOfWork.CharacterMissions.Remove(player.Id, missionId);
            });
            player.Missions = result.Missions;
            ApplyHistory(player, history);
            foreach (var row in result.Missions.Values.Where(row => row.State is MissionState.Success or MissionState.Completed))
                player.MissionSuccessHistory.Add(row.MissionId);
        }

        internal static void ApplyHistory(Manifestation player, IReadOnlyList<CharacterMissionHistoryEntry> history)
        {
            player.MissionHistory = history.GroupBy(entry => entry.MissionId).ToDictionary(group => group.Key,
                group => (MissionState)group.OrderByDescending(entry => entry.AssignmentGeneration)
                    .ThenByDescending(entry => entry.CompletedAtUtc).ThenByDescending(entry => entry.AssignmentId).First().Outcome);
            player.MissionSuccessHistory = history.Where(entry => entry.Rewarded || entry.Outcome is 1 or 4)
                .Select(entry => entry.MissionId).ToHashSet();
            player.MissionRewardTimes = history.Where(entry => entry.Rewarded).GroupBy(entry => entry.MissionId)
                .ToDictionary(group => group.Key, group => DateTime.SpecifyKind(
                    group.Max(entry => entry.RewardedAtUtc ?? entry.CompletedAtUtc), DateTimeKind.Utc));
        }
        internal bool TryHydrateMission(
            uint characterId,
            CharacterMissionEntry row,
            CharacterMissionProgressSnapshot progress,
            out MissionLog mission) =>
            BuildHydration(characterId, new[] { row }, progress).Missions.TryGetValue(row.MissionId, out mission);

        private HydrationResult BuildHydration(
            uint characterId,
            IReadOnlyList<CharacterMissionEntry> rows,
            CharacterMissionProgressSnapshot progress)
        {
            var hydrated = new Dictionary<uint, MissionLog>();
            var invalid = new List<uint>();
            foreach (var row in rows)
            {
                if (!_catalog.TryGetOperational(row.MissionId, out var definition))
                {
                    if (row.ContentRevision is not ("legacy" or "unversioned"))
                    {
                        Logger.WriteLog(LogType.Error,
                            $"Quarantined assignment {row.AssignmentId} for character {characterId}: mission {row.MissionId}@{row.ContentRevision} is unavailable; saved state was preserved.");
                        continue;
                    }
                    Logger.WriteLog(LogType.Error,
                        $"Cleared mission {row.MissionId} for character {characterId}: definition is not operational.");
                    invalid.Add(row.MissionId);
                    continue;
                }

                if (row.ContentRevision is not ("legacy" or "unversioned") &&
                    row.ContentRevision != definition.ContentRevision)
                {
                    Logger.WriteLog(LogType.Error,
                        $"Quarantined assignment {row.AssignmentId}: pinned revision {row.ContentRevision} differs from {definition.ContentRevision}; drain/reset or migrate explicitly.");
                    continue;
                }

                var state = (MissionState)row.MissionState;
                if (!MissionApplication.IsPublishedState(state))
                {
                    Logger.WriteLog(LogType.Error,
                        $"Cleared mission {row.MissionId} for character {characterId}: unsupported state {row.MissionState}.");
                    invalid.Add(row.MissionId);
                    continue;
                }

                if (!TryHydrateObjectives(definition, progress, out var objectives))
                {
                    Logger.WriteLog(LogType.Error,
                        $"Cleared mission {row.MissionId} for character {characterId}: objective state is incomplete or invalid.");
                    invalid.Add(row.MissionId);
                    continue;
                }
                var derivedCompleteable =
                    state == MissionState.Active &&
                    definition.Objectives.Values
                        .Where(objective => objective.IsRequired.Value)
                        .All(objective =>
                            objectives[objective.ObjectiveId].State ==
                            MissionObjectiveState.Completed);
                if (state == MissionState.Active && row.Completeable != derivedCompleteable)
                {
                    Logger.WriteLog(LogType.Error,
                        $"Cleared mission {row.MissionId} for character {characterId}: completable state does not match objectives.");
                    invalid.Add(row.MissionId);
                    continue;
                }

                hydrated[row.MissionId] = new MissionLog(
                    row.MissionId,
                    state,
                    derivedCompleteable,
                    objectives, row.AssignmentId, row.Generation, row.ContentRevision);
            }

            return new HydrationResult(hydrated, invalid);
        }

        internal void Hydrate(Manifestation player, IReadOnlyList<CharacterMissionEntry> rows) =>
            Hydrate(player, rows, CharacterMissionProgressSnapshot.Empty);

        private sealed class HydrationResult
        {
            internal Dictionary<uint, MissionLog> Missions { get; }
            internal IReadOnlyList<uint> InvalidMissionIds { get; }

            internal HydrationResult(
                Dictionary<uint, MissionLog> missions,
                IReadOnlyList<uint> invalidMissionIds)
            {
                Missions = missions;
                InvalidMissionIds = invalidMissionIds;
            }
        }


        private static bool TryHydrateObjectives(
            Mission definition,
            CharacterMissionProgressSnapshot progress,
            out IReadOnlyDictionary<uint, MissionObjectiveLog> objectives)
        {
            objectives = null;
            if (definition.Objectives.Count == 0)
            {
                if (progress.Missions.TryGetValue(definition.MissionId, out var emptyProgress) &&
                    emptyProgress.Objectives.Count != 0)
                    return false;
                objectives = new Dictionary<uint, MissionObjectiveLog>();
                return true;
            }
            if (!progress.Missions.TryGetValue(definition.MissionId, out var missionProgress))
                return false;
            if (missionProgress.Objectives.Keys.Except(definition.Objectives.Keys).Any() ||
                definition.Objectives.Keys.Except(missionProgress.Objectives.Keys).Any())
                return false;

            var result = new Dictionary<uint, MissionObjectiveLog>();
            foreach (var objectiveDefinition in definition.Objectives.Values)
            {
                if (!missionProgress.Objectives.TryGetValue(
                        objectiveDefinition.ObjectiveId, out var objectiveProgress) ||
                    !Enum.IsDefined(typeof(MissionObjectiveState), (int)objectiveProgress.State) ||
                    objectiveDefinition.Counters.Keys.Except(objectiveProgress.Counters.Keys).Any() ||
                    objectiveProgress.Counters.Keys.Except(objectiveDefinition.Counters.Keys).Any() ||
                    objectiveDefinition.ItemCounters.Keys.Except(objectiveProgress.ItemCounters.Keys).Any() ||
                    objectiveProgress.ItemCounters.Keys.Except(objectiveDefinition.ItemCounters.Keys).Any())
                    return false;

                result.Add(
                    objectiveDefinition.ObjectiveId,
                    new MissionObjectiveLog(
                        objectiveDefinition.ObjectiveId,
                        (MissionObjectiveState)objectiveProgress.State,
                        objectiveProgress.Counters,
                        objectiveProgress.ItemCounters));
            }

            objectives = result;
            return true;
        }


    }
}
