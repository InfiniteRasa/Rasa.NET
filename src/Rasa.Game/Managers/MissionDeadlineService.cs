using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Repositories.Char;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;

    public sealed class MissionDeadlineService
    {
        private static MissionDeadlineService _instance;
        private static readonly object InstanceLock = new();
        private readonly Func<IGameUnitOfWorkFactory> _gameUnitOfWorkFactory;
        private readonly Func<MissionApplication> _missionManager;
        private readonly Func<DateTime> _utcNow;
        internal Func<DateTime> Clock => _utcNow;

        internal static MissionDeadlineService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        _instance ??= new MissionDeadlineService();
                    }
                }

                return _instance;
            }
        }

        internal MissionDeadlineService(
            Func<IGameUnitOfWorkFactory> gameUnitOfWorkFactory = null,
            Func<MissionApplication> missionManager = null,
            Func<DateTime> utcNow = null)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory ?? (() => Server.GameUnitOfWorkFactory);
            _missionManager = missionManager ?? (() => MissionApplication.Instance);
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        internal bool Evaluate(Client client)
        {
            if (client?.Player?.MapChannel == null || client.State != ClientState.Ingame)
                return false;
            var missions = _missionManager();
            missions.Scenes.Resume(client);
            return missions.Scenes.Tick(client.Player.MapChannel, Game.Missions.SceneTickScope.Deadlines);
        }

        internal void SynchronizeMission(
            ICharUnitOfWork unitOfWork,
            uint characterId,
            Mission definition,
            CharacterMissionEntry durableMission,
            IReadOnlyDictionary<uint, CharacterMissionObjectiveEntry> durableObjectives)
        {
            if (unitOfWork == null || definition == null || durableMission == null)
                return;
            if (!HasDeadline(definition))
                return;

            var activeDeadline = GetActiveDeadlineObjective(
                definition,
                durableMission,
                durableObjectives,
                requireSingle: true);
            var existing = unitOfWork.CharacterMissionDeadlines.Get(
                characterId,
                definition.MissionId);
            if (activeDeadline.HasValue)
            {
                if (existing == null)
                {
                    unitOfWork.CharacterMissionDeadlines.Add(
                        new CharacterMissionDeadlineEntry(
                            characterId,
                            definition.MissionId,
                            _utcNow().AddSeconds(activeDeadline.Value.Rule.DurationSeconds.Value),
                            CharacterMissionDeadlineState.Active));
                }

                _missionManager().Scenes.SynchronizeDeadline(unitOfWork, durableMission,
                    unitOfWork.CharacterMissionDeadlines.Get(characterId, definition.MissionId), activeDeadline.Value.ObjectiveId);
                return;
            }

            if (existing?.State != CharacterMissionDeadlineState.Active)
                return;

            var completedDeadlineObjective = definition.Objectives.Values.Any(objective =>
                objective.GetExecutableTransitionsOrLegacyDefault().Any(transition =>
                    transition.ProgressRule?.Kind == MissionProgressEventKind.DeadlineElapsed) &&
                durableObjectives.TryGetValue(objective.ObjectiveId, out var durableObjective) &&
                durableObjective.ObjectiveState == (byte)MissionObjectiveState.Completed);
            unitOfWork.CharacterMissionDeadlines.SetState(
                characterId,
                definition.MissionId,
                completedDeadlineObjective
                    ? CharacterMissionDeadlineState.Satisfied
                    : CharacterMissionDeadlineState.Cancelled);
            _missionManager().Scenes.SynchronizeDeadline(unitOfWork, durableMission, existing, null);
        }

        internal static bool HasDeadline(Mission definition) =>
            definition.Objectives.Values.Any(objective =>
                objective.GetExecutableTransitionsOrLegacyDefault().Any(transition =>
                    transition.ProgressRule?.Kind == MissionProgressEventKind.DeadlineElapsed));

        private static (uint ObjectiveId, MissionProgressRule Rule)? GetActiveDeadlineObjective(
            Mission definition,
            CharacterMissionEntry durableMission,
            IReadOnlyDictionary<uint, CharacterMissionObjectiveEntry> durableObjectives,
            bool requireSingle)
        {
            if (definition == null ||
                durableMission?.MissionState != (uint)MissionState.Active)
                return null;

            var active = definition.Objectives.Values
                .Where(objective =>
                    objective.GetExecutableTransitionsOrLegacyDefault().Any(transition =>
                        transition.ProgressRule?.Kind == MissionProgressEventKind.DeadlineElapsed) &&
                    durableObjectives.TryGetValue(objective.ObjectiveId, out var durableObjective) &&
                    durableObjective.ObjectiveState == (byte)MissionObjectiveState.Incomplete)
                .Select(objective => (
                    objective.ObjectiveId,
                    objective.GetExecutableTransitionsOrLegacyDefault()
                        .Where(transition => transition.ProgressRule?.Kind == MissionProgressEventKind.DeadlineElapsed)
                        .OrderBy(transition => transition.Sequence)
                        .ThenBy(transition => transition.TransitionId)
                        .Select(transition => transition.ProgressRule)
                        .First()))
                .ToArray();
            if (active.Length == 0)
                return null;
            if (requireSingle && active.Length > 1)
                throw new GameplayRejectionException(
                    $"Mission {definition.MissionId} has multiple active deadline objectives.");
            return active[0];
        }
    }
}
