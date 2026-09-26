using System;
using System.Linq;
using Rasa.Game.Missions.Integration;
using Rasa.Game.Missions.World;
using Rasa.Managers;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Repositories.Char;
using Rasa.Repositories.UnitOfWork;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Game.Missions
{
    public sealed class MissionOfferAuthority
    {
        public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
        public const int PendingCapacity = 30;
        private readonly IGameUnitOfWorkFactory _factory;
        private readonly MissionApplication _missions;
        private readonly Func<DateTime> _utcNow;
        private readonly MissionRequirementService _requirements = new();

        internal MissionOfferAuthority(IGameUnitOfWorkFactory factory, MissionApplication missions, Func<DateTime> utcNow)
        { _factory = factory; _missions = missions; _utcNow = utcNow; }

        public bool TryOffer(Client client, uint missionId, MissionOfferSourceIdentity source, bool forceDialog = true)
        {
            if (client == null)
                return false;
            lock (client.SyncRoot)
            {
                try
                {
                    Action publish = null;
                    using (var unit = _factory.CreateChar())
                        unit.ExecuteTransaction(() => publish = PlanOffer(client, missionId, source, unit, forceDialog));
                    publish?.Invoke();
                    return true;
                }
                catch (Exception error) when (GameplayRejectionException.IsExpected(error))
                {
                    Logger.WriteLog(LogType.Error, $"Unable to offer mission {missionId}: {error.Message}");
                    return false;
                }
            }
        }

        internal Action PlanOffer(Client client, uint missionId, MissionOfferSourceIdentity source,
            ICharUnitOfWork unit, bool forceDialog = true)
        {
            var session = MissionSessionIdentity.Capture(client);
            if (session == null || !_missions.TryGetOperationalMission(missionId, out var definition) ||
                !unit.MissionOffers.IsOwnedBy(session.CharacterId, session.AccountId) ||
                !SourceIsCurrent(client, definition, source, unit, persisted: false))
                throw new GameplayRejectionException("Radio offer has no authorized recipient, revision or source.");
            if (!_missions.CanAdmit(client.Player, definition, unit, out var failure) ||
                !_missions.ArePrerequisitesSatisfied(client.Player, missionId, unit, out failure) ||
                !_missions.HasJournalCapacity(client.Player.Id, missionId, unit))
                throw new GameplayRejectionException($"Mission offer is ineligible or the journal is full: {failure}");
            var now = UtcNow();
            var entries = unit.MissionOffers.ForCharacter(session.CharacterId);
            var existing = entries.SingleOrDefault(entry => entry.MissionId == missionId);
            foreach (var entry in entries.Where(entry => entry != existing &&
                (entry.State != MissionOfferState.Pending || entry.ExpiresAtUtc <= now ||
                    Snapshot(entry).Session != session)))
                unit.MissionOffers.Remove(entry);
            if (existing?.State == MissionOfferState.Pending &&
                IsCurrent(client, definition, Snapshot(existing), unit, false) &&
                MatchesPredecessor(Snapshot(existing), unit))
            {
                var duplicate = Snapshot(existing);
                if (duplicate.Source != source)
                    throw new GameplayRejectionException("A different offer already occupies this mission's pending slot.");
                RegisterCreationGuard(client, definition, duplicate, unit);
                return null;
            }
            if (unit.MissionOffers.ForCharacter(session.CharacterId)
                .Count(entry => entry != existing && entry.State == MissionOfferState.Pending) >= PendingCapacity)
                throw new GameplayRejectionException("The recipient already has 30 pending mission offers.");
            var prior = unit.CharacterMissions.GetByCharacterAndMission(session.CharacterId, missionId);
            var row = existing ?? new CharacterMissionOfferEntry { CharacterId = session.CharacterId, MissionId = missionId };
            row.OfferId = Guid.NewGuid().ToString("N");
            row.ContentRevision = definition.ContentRevision;
            row.AccountId = session.AccountId;
            row.PlayerEntityId = session.PlayerEntityId;
            row.SessionId = session.SessionId;
            row.PlayerEpoch = session.PlayerEpoch;
            row.MapEpoch = session.MapEpoch;
            row.SourceKind = source.Kind;
            row.SourceKey = source.Key;
            row.SourceInstanceId = source.InstanceId;
            row.SourceGeneration = source.Generation;
            row.SourceAssignmentId = source.AssignmentId;
            row.SourceAssignmentGeneration = source.AssignmentGeneration;
            row.PartySource = source.Party;
            row.PriorAssignmentId = prior?.AssignmentId;
            row.PriorAssignmentGeneration = prior?.Generation ?? 0;
            row.PriorAssignmentRevision = prior?.ContentRevision;
            row.PriorHistoryId = unit.CharacterMissions.Runtime.LatestTerminal(session.CharacterId, missionId)?.AssignmentId;
            // MySQL datetime(6) persists microseconds, not DateTime's 100-nanosecond ticks.
            row.CreatedAtUtc = new DateTime(now.Ticks - now.Ticks % 10, DateTimeKind.Utc);
            row.ExpiresAtUtc = row.CreatedAtUtc.Add(Lifetime);
            row.State = MissionOfferState.Pending;
            row.ConsumedAssignmentId = null;
            row.ConsumedAssignmentGeneration = 0;
            row.Version++;
            if (existing == null)
                unit.MissionOffers.Add(row);
            var authorization = Snapshot(row);
            RegisterCreationGuard(client, definition, authorization, unit);
            return () =>
            {
                if (authorization.Session.IsCurrent(client))
                {
                    if (source.Kind == MissionOfferSourceKind.Party)
                        _missions.PublishSharedOffer(client, definition, source.Party.SourceEntityId);
                    else
                        _missions.PublishRadioOffer(client, definition, forceDialog);
                }
                else
                    Logger.WriteLog(LogType.Network, $"Radio offer {authorization.OfferId} committed but its session left before publication.");
            };
        }

        internal bool TryResolve(Client client, Mission definition, ICharUnitOfWork unit,
            out Authorization authorization)
        {
            var row = unit.MissionOffers.Get(client.Player.Id, definition.MissionId);
            authorization = row == null ? null : Snapshot(row);
            if (row?.State == MissionOfferState.Pending && IsCurrent(client, definition, authorization, unit, false) &&
                MatchesPredecessor(authorization, unit))
                return true;
            if (row?.State == MissionOfferState.Pending && row.SessionId == client.MissionSessionId)
            {
                row.State = MissionOfferState.Cancelled;
                row.Version++;
            }
            authorization = null;
            return false;
        }

        internal void Consume(ICharUnitOfWork unit, Authorization offer, CharacterMissionEntry assignment)
        {
            var row = unit.MissionOffers.Get(offer.Session.CharacterId, offer.MissionId);
            if (row?.State != MissionOfferState.Pending || Snapshot(row) != offer)
                throw new GameplayRejectionException("The pending mission offer changed before consumption.");
            row.State = MissionOfferState.Consumed;
            row.ConsumedAssignmentId = assignment.AssignmentId;
            row.ConsumedAssignmentGeneration = assignment.Generation;
            row.Version++;
        }

        internal void Cancel(ICharUnitOfWork unit, uint characterId, uint missionId)
        {
            var row = unit.MissionOffers.Get(characterId, missionId);
            if (row?.State == MissionOfferState.Pending)
            { row.State = MissionOfferState.Cancelled; row.Version++; }
        }

        internal void ValidateConsumed(Client client, Mission definition, Authorization offer,
            MissionLog assignment, ICharUnitOfWork unit)
        {
            var row = unit.MissionOffers.Read(offer.Session.CharacterId, offer.MissionId);
            var current = unit.CharacterMissions.Runtime.ReadAssignment(offer.Session.CharacterId, offer.MissionId);
            var everSucceeded = unit.CharacterMissions.Runtime.EverSucceeded(offer.Session.CharacterId, offer.MissionId);
            var lastReward = unit.CharacterMissions.Runtime.LastRewardedAtUtc(offer.Session.CharacterId, offer.MissionId);
            var pendingReward = unit.CharacterMissions.Runtime.HasPendingReward(offer.Session.CharacterId, offer.MissionId);
            if (row?.State != MissionOfferState.Consumed || Snapshot(row) != offer ||
                row.ConsumedAssignmentId != assignment.AssignmentId || row.ConsumedAssignmentGeneration != assignment.Generation ||
                current == null || !assignment.MatchesAssignment(current.AssignmentId, current.Generation, current.ContentRevision) ||
                current.MissionState != (uint)Data.MissionState.Active || pendingReward ||
                !IdentityIsCurrent(client, definition, offer, unit, true) ||
                !definition.RepeatPolicy.Allows(UtcNow(), everSucceeded, lastReward))
                throw new GameplayRejectionException("Radio offer authority changed at the commit boundary.");
        }

        private void RegisterCreationGuard(Client client, Mission definition, Authorization offer, ICharUnitOfWork unit) =>
            TransactionValidation.AtCommitBoundary(unit, () =>
            {
                var current = unit.MissionOffers.Read(offer.Session.CharacterId, offer.MissionId);
                if (current?.State != MissionOfferState.Pending || Snapshot(current) != offer ||
                    !MatchesPredecessor(offer, unit) ||
                    !_missions.CanAdmit(client.Player, definition, unit, out _) ||
                    !_missions.HasJournalCapacity(client.Player.Id, offer.MissionId, unit) ||
                    !_missions.ArePrerequisitesSatisfied(client.Player, offer.MissionId, unit, out _) ||
                    !IsCurrent(client, definition, offer, unit, true))
                    throw new GameplayRejectionException("Radio offer changed before its creation committed.");
            });

        private bool IsCurrent(Client client, Mission definition, Authorization offer, ICharUnitOfWork unit, bool persisted) =>
            IdentityIsCurrent(client, definition, offer, unit, persisted) &&
            _requirements.Evaluate(client.Player, SourceRequirement(definition, offer.Source), unit) &&
            offer.Session.IsCurrent(client) && UtcNow() < offer.ExpiresAtUtc;

        private bool IdentityIsCurrent(Client client, Mission definition, Authorization offer, ICharUnitOfWork unit, bool persisted) =>
            offer != null && offer.Session.IsCurrent(client) && offer.Revision == definition.ContentRevision &&
            _missions.TryGetOperationalMission(offer.MissionId, out var current) && ReferenceEquals(current, definition) &&
            offer.ExpiresAtUtc - offer.CreatedAtUtc == Lifetime &&
            unit.MissionOffers.IsOwnedBy(offer.Session.CharacterId, offer.Session.AccountId) &&
            SourceIdentityIsCurrent(client, definition, offer.Source, unit, persisted) &&
            offer.Session.IsCurrent(client) && UtcNow() < offer.ExpiresAtUtc;

        private static bool MatchesPredecessor(Authorization offer, ICharUnitOfWork unit)
        {
            var prior = unit.CharacterMissions.Runtime.ReadAssignment(offer.Session.CharacterId, offer.MissionId);
            return prior?.AssignmentId == offer.PriorAssignmentId && (prior?.Generation ?? 0) == offer.PriorGeneration &&
                prior?.ContentRevision == offer.PriorRevision &&
                unit.CharacterMissions.Runtime.LatestTerminal(offer.Session.CharacterId, offer.MissionId)?.AssignmentId ==
                    offer.PriorHistoryId;
        }

        internal MissionRequirement SourceRequirement(Mission mission, MissionOfferSourceIdentity source) =>
            source.Kind == MissionOfferSourceKind.Party ? null :
                mission.RadioSources.Single(candidate => candidate.Kind == source.Kind && candidate.Key == source.Key).Requirement;

        private bool SourceIsCurrent(Client client, Mission mission, MissionOfferSourceIdentity source,
            ICharUnitOfWork unit, bool persisted) =>
            SourceIdentityIsCurrent(client, mission, source, unit, persisted) &&
            _requirements.Evaluate(client.Player, SourceRequirement(mission, source), unit);

        private bool SourceIdentityIsCurrent(Client client, Mission mission, MissionOfferSourceIdentity source,
            ICharUnitOfWork unit, bool persisted)
        {
            if (source?.Kind == MissionOfferSourceKind.Party)
                return _missions.Sharing.SourceIsCurrent(client, mission, source, unit);
            if (!mission.AcceptanceChannel.HasFlag(MissionChannel.Radio) || source == null ||
                source.Party != null ||
                string.IsNullOrWhiteSpace(source.InstanceId) || source.InstanceId.Length > 96 ||
                !MissionInteractionPolicy.IsActivePlayer(client))
                return false;
            var authored = mission.RadioSources.SingleOrDefault(candidate => candidate.Kind == source.Kind && candidate.Key == source.Key);
            var map = client.Player.MapChannel;
            if (authored == null || authored.MapContextId.HasValue && authored.MapContextId != map.MapInfo.MapContextId ||
                authored.OwnedPrivateMap && (!map.IsPrivateInstance || map.OwnerCharacterId != client.Player.Id))
                return false;
            if (source.Kind == MissionOfferSourceKind.ServerEvent)
                return source.Generation == 0 && source.AssignmentId == null && source.AssignmentGeneration == 0;
            var store = unit.CharacterMissions.Runtime;
            var scene = persisted ? store.ReadScene(source.InstanceId) : store.Scene(source.InstanceId);
            if (source.Generation == 0 || scene == null || scene.Generation != source.Generation ||
                scene.ScriptKey != source.Key || scene.OwnerCharacterId != client.Player.Id ||
                scene.MapKey != PublicActorLeaseService.MapKey(map) || scene.Status is not ("Running" or "Waiting"))
                return false;
            if (scene.MissionId == 0)
                return source.AssignmentId == null && source.AssignmentGeneration == 0 && map.IsPrivateInstance &&
                    map.OwnerCharacterId == client.Player.Id;
            if (!_missions.TryGetOperationalMission(scene.MissionId, out var sourceMission) ||
                sourceMission.ContentRevision != scene.Release)
                return false;
            var assignment = persisted ? store.ReadAssignment(client.Player.Id, scene.MissionId) :
                unit.CharacterMissions.GetByCharacterAndMission(client.Player.Id, scene.MissionId);
            var participant = persisted ? store.ReadParticipant(scene.RunId, client.Player.Id) :
                store.Participants(scene.RunId).SingleOrDefault(entry => entry.CharacterId == client.Player.Id);
            return assignment != null && assignment.AssignmentId == source.AssignmentId &&
                assignment.Generation == source.AssignmentGeneration && scene.AssignmentId == source.AssignmentId &&
                assignment.MissionState is 0 or 1 &&
                (assignment.ContentRevision == scene.Release || assignment.ContentRevision == "legacy") &&
                participant?.Active == true && participant.AssignmentId == source.AssignmentId &&
                participant.AssignmentGeneration == source.AssignmentGeneration;
        }

        private DateTime UtcNow()
        {
            var now = _utcNow();
            if (now.Kind != DateTimeKind.Utc)
                throw new MissionRuleException("Mission offers require a UTC clock.");
            return now;
        }

        private static Authorization Snapshot(CharacterMissionOfferEntry row) =>
            new(new(row.SessionId, row.PlayerEpoch, row.CharacterId, row.PlayerEntityId, row.AccountId, row.MapEpoch),
                row.MissionId, row.ContentRevision, row.OfferId,
                new(row.SourceKind, row.SourceKey, row.SourceInstanceId, row.SourceGeneration,
                    row.SourceAssignmentId, row.SourceAssignmentGeneration, row.PartySource), row.CreatedAtUtc, row.ExpiresAtUtc,
                row.PriorAssignmentId, row.PriorAssignmentGeneration, row.PriorAssignmentRevision, row.PriorHistoryId);

        internal sealed record Authorization(MissionSessionIdentity Session, uint MissionId, string Revision, string OfferId,
            MissionOfferSourceIdentity Source, DateTime CreatedAtUtc, DateTime ExpiresAtUtc,
            string PriorAssignmentId, uint PriorGeneration, string PriorRevision, string PriorHistoryId);
    }
}
