using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Rasa.Missions.Runtime;
using ProgressCandidate = Rasa.Missions.Runtime.MissionProgressCandidate;
using System.Text.Json;
using Rasa.Missions.Content;
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

    internal sealed class MissionProtocolAdapter
    {
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        private readonly MissionContentCatalog _catalog;
        private readonly MissionJournalAdapter _journal;
        private readonly Func<DateTime> _utcNow;
        private readonly Action<PythonPacket> _beforeMissionPacketPublication;
        private readonly Func<Mission, bool> _canShare;
        internal MissionProtocolAdapter(IGameUnitOfWorkFactory factory, MissionContentCatalog catalog,
            MissionJournalAdapter journal, Func<DateTime> clock, Action<PythonPacket> beforePublication,
            Func<Mission, bool> canShare)
        {
            _gameUnitOfWorkFactory = factory; _catalog = catalog; _journal = journal;
            _utcNow = clock; _beforeMissionPacketPublication = beforePublication;
            _canShare = canShare;
        }
        public IReadOnlyDictionary<uint, MissionInfo> BuildStatusSnapshot(Manifestation player)
        {
            var snapshot = new Dictionary<uint, MissionInfo>();
            foreach (var entry in player.Missions)
            {
                if (!_catalog.TryGetOperational(entry.Key, out var definition) ||
                    !MissionApplication.IsPublishedState(entry.Value.State))
                    continue;

                snapshot.Add(entry.Key, BuildPublishedMissionInfo(
                    player,
                    definition,
                    entry.Value));
            }

            return snapshot;
        }

        internal void PublishMissionStatus(Client client, uint missionId, string description)
        {
            if (client?.Player == null ||
                !client.Player.Missions.TryGetValue(missionId, out var runtimeMission) ||
                !_catalog.TryGetOperational(missionId, out var definition) ||
                !MissionApplication.IsPublishedState(runtimeMission.State))
                return;

            PublishMissionPacket(
                client,
                new MissionStatusInfoPacket(
                    new Dictionary<uint, MissionInfo>
                    {
                        [missionId] = BuildPublishedMissionInfo(
                            client.Player,
                            definition,
                            runtimeMission)
                    }),
                description);
        }

        internal MissionInfo BuildPublishedMissionInfo(
            Manifestation player,
            Mission definition,
            MissionLog runtimeMission)
        {
            uint activeDeadlineObjectiveId = 0;
            uint? timeRemaining = null;
            if (player != null &&
                definition != null &&
                runtimeMission != null &&
                runtimeMission.State == MissionState.Active &&
                definition.Objectives.Values.Any(objective =>
                    objective.GetExecutableTransitionsOrLegacyDefault().Any(transition =>
                        transition.ProgressRule?.Kind == MissionProgressEventKind.DeadlineElapsed)))
            {
                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
                var deadline = unitOfWork.CharacterMissionDeadlines.Get(
                    player.Id,
                    definition.MissionId);
                if (deadline?.State == CharacterMissionDeadlineState.Active)
                {
                    var timedObjectives = definition.Objectives.Values
                        .Where(objective =>
                            objective.GetExecutableTransitionsOrLegacyDefault().Any(transition =>
                                transition.ProgressRule?.Kind == MissionProgressEventKind.DeadlineElapsed) &&
                            runtimeMission.Objectives.TryGetValue(objective.ObjectiveId, out var runtimeObjective) &&
                            runtimeObjective.State == MissionObjectiveState.Incomplete)
                        .Select(objective => objective.ObjectiveId)
                        .ToArray();
                    if (timedObjectives.Length == 1)
                    {
                        activeDeadlineObjectiveId = timedObjectives[0];
                        var remaining = deadline.DueAtUtc - _utcNow();
                        timeRemaining = remaining <= TimeSpan.Zero
                            ? 0U
                            : checked((uint)Math.Ceiling(remaining.TotalSeconds));
                    }
                }
            }

            return WithRewardInfo(definition, definition.CreateInfo(
                runtimeMission.State,
                runtimeMission.Completeable,
                runtimeMission.Objectives,
                objectiveId =>
                    objectiveId == activeDeadlineObjectiveId
                        ? timeRemaining
                        : null));
        }

        internal MissionInfo BuildOfferInfo(Mission definition, MissionState state = MissionState.NotAssigned)
        {
            var info = WithRewardInfo(definition, definition.CreateInfo(state, false, definition.CreateInitialObjectiveLogs()));
            info.AudioSetId = checked((int)(_catalog.SceneBindings.GetValueOrDefault(definition.MissionId)?
                .Audio?.OfferAudioSetId ?? 0));
            return info;
        }

        internal void PublishAudio(Client client, uint missionId, MissionAudioEvent audioEvent)
        {
            if (_catalog.SceneBindings.GetValueOrDefault(missionId)?.Audio?.Events is { } events &&
                events.TryGetValue(audioEvent, out var audioSetId))
                PlayVoice(client, missionId, audioSetId);
        }

        internal void PublishAnnouncementAudio(Client client, uint missionId, uint greetingId)
        {
            if (_catalog.SceneBindings.GetValueOrDefault(missionId)?.Audio?.Announcements is { } announcements &&
                announcements.TryGetValue(greetingId, out var audioSetId))
                PlayVoice(client, missionId, audioSetId);
        }

        private static void PlayVoice(Client client, uint missionId, uint audioSetId) =>
            MissionApplication.TryPublish(
                () => CommunicatorManager.Instance.PlayTutorialAudio(client, audioSetId),
                $"mission {missionId} voice {audioSetId}");

        private MissionInfo WithRewardInfo(Mission definition, MissionInfo info)
        {
            info.MissionConstantData.Shareable = _canShare(definition);
            if (_catalog.Rewards.TryGetValue(definition.MissionId, out var reward))
                info.MissionConstantData.RewardInfo = reward.CreateInfo();
            return info;
        }

        public void PublishInitialState(Client client)
        {
            PublishMissionPacket(
                client,
                new MissionStatusInfoPacket(BuildStatusSnapshot(client.Player)),
                "mission status snapshot");
        }

        internal void PublishMissionPacket(
            Client client,
            PythonPacket packet,
            string description)
        {
            MissionApplication.TryPublish(
                () =>
                {
                    _beforeMissionPacketPublication?.Invoke(packet);
                    client.CallMethod(client.Player.EntityId, packet);
                },
                description);
        }

        internal bool PublishCharacterFlags(Client client)
        {
            try
            {
                return client.FlagProjection.PublishPending(client, packet =>
                {
                    _beforeMissionPacketPublication?.Invoke(packet);
                    client.CallMethod(client.Player.EntityId, packet);
                });
            }
            catch (Exception error) when (error is IOException || error is SocketException ||
                error is InvalidOperationException)
            {
                Logger.WriteLog(LogType.Error,
                    $"Unable to publish character {client.Player.Id} flags; committed snapshot remains pending: {error}");
                return false;
            }
        }

    }
}
