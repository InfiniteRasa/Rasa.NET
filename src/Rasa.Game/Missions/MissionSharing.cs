using System;
using System.Linq;
using System.Numerics;
using Rasa.Data;
using Rasa.Game.Missions.Integration;
using Rasa.Managers;
using Rasa.Missions.Definitions;
using Rasa.Repositories.Char;
using Rasa.Repositories.UnitOfWork;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Game.Missions
{
    public sealed class MissionSharing
    {
        public const float MaxRange = 20;
        private const string SourceKey = "party";
        private readonly IGameUnitOfWorkFactory _factory;
        private readonly MissionApplication _missions;

        internal MissionSharing(IGameUnitOfWorkFactory factory, MissionApplication missions)
        { _factory = factory; _missions = missions; }

        internal bool CanShare(Mission definition) =>
            definition.IsOperational && definition.Shareable == true &&
            (!_missions.Scenes.Owns(definition.MissionId) && !_missions.PublicActors.HasBinding(definition.MissionId) ||
                _missions.PublicActors.SupportsPartyJoining(definition.MissionId) &&
                Content.MissionSceneValidation.SupportsSharedAssignment(definition));

        public bool TryShare(Client sender, uint missionId)
        {
            if (sender == null)
                return Reject("Mission sharing has no sender.");
            MissionSessionIdentity session;
            Party party;
            PartyMember member;
            CharacterMissionEntry assignment;
            string instanceId;
            uint generation;
            Guid sourceMembership;
            Client[] recipients;
            lock (sender.SyncRoot)
            {
                if (!PartyManager.Instance.TryGetLiveMembership(sender, out party, out member) ||
                    !_missions.TryGetOperationalMission(missionId, out var definition) || !CanShare(definition) ||
                    sender.Player.MapChannel.IsPrivateInstance)
                    return Reject($"Mission {missionId} has no active shareable party source.");
                session = MissionSessionIdentity.Capture(sender);
                var map = sender.Player.MapChannel;
                using var unit = _factory.CreateChar();
                assignment = unit.CharacterMissions.Runtime.ReadAssignment(session.CharacterId, missionId);
                if (!OwnsAssignment(sender, definition, assignment) ||
                    !unit.MissionOffers.IsOwnedBy(session.CharacterId, session.AccountId))
                    return Reject($"Mission {missionId} sharing requires the sender's current active assignment.");
                sourceMembership = member.MembershipId;
                instanceId = party.LifetimeId.ToString("N");
                generation = 0;
                if (_missions.Scenes.Owns(missionId))
                {
                    if (!_missions.PublicActors.TryGetJoinRun(map, assignment, unit, out var run))
                        return Reject($"Mission {missionId} has no live joinable source run.");
                    instanceId = run.RunId;
                    generation = run.Generation;
                }
                recipients = map.ClientList.Where(client => client != sender &&
                    PartyManager.Instance.TryGetLiveMembership(client, out var candidateParty, out _) &&
                    ReferenceEquals(party, candidateParty)).ToArray();
            }

            var offered = false;
            foreach (var recipient in recipients)
            {
                if (!session.IsCurrent(sender) ||
                    !PartyManager.Instance.TryGetLiveMembership(recipient, out var currentParty, out var recipientMember) ||
                    !ReferenceEquals(party, currentParty))
                    continue;
                var source = new MissionOfferSourceIdentity(MissionOfferSourceKind.Party, SourceKey,
                    instanceId, generation, AssignmentId: assignment.AssignmentId,
                    AssignmentGeneration: assignment.Generation,
                    Party: new(party.Id, party.LifetimeId, sourceMembership, recipientMember.MembershipId,
                        session.CharacterId, session.AccountId, session.PlayerEntityId,
                        session.SessionId, session.PlayerEpoch, session.MapEpoch));
                // The sender lock is released before the authority mutates each recipient independently.
                offered |= _missions.Offers.TryOffer(recipient, missionId, source);
            }
            return offered || Reject($"Mission {missionId} has no eligible nearby party recipient.");
        }

        public bool TryAccept(Client recipient, ulong sourcePlayerEntityId, uint missionId) =>
            _missions.TryAcceptSharedMission(recipient, sourcePlayerEntityId, missionId);

        internal bool SourceIsCurrent(Client recipient, Mission definition, MissionOfferSourceIdentity source,
            ICharUnitOfWork unit)
        {
            var identity = source?.Party;
            if (source?.Kind != MissionOfferSourceKind.Party || source.Key != SourceKey || identity == null ||
                !CanShare(definition) || !MissionInteractionPolicy.IsActivePlayer(recipient))
                return false;
            var map = recipient.Player.MapChannel;
            var sender = map.ClientList.SingleOrDefault(client => client?.Player?.EntityId == identity.SourceEntityId);
            var session = new MissionSessionIdentity(identity.SourceSessionId, identity.SourcePlayerEpoch,
                identity.SourceCharacterId, identity.SourceEntityId, identity.SourceAccountId, identity.SourceMapEpoch);
            if (sender == recipient || !session.IsCurrent(sender) ||
                !PairIsCurrent(sender, recipient, identity))
                return false;
            var assignment = unit.CharacterMissions.Runtime.ReadAssignment(identity.SourceCharacterId, definition.MissionId);
            return OwnsAssignment(sender, definition, assignment) &&
                assignment.AssignmentId == source.AssignmentId && assignment.Generation == source.AssignmentGeneration &&
                unit.MissionOffers.IsOwnedBy(identity.SourceCharacterId, identity.SourceAccountId) &&
                (_missions.Scenes.Owns(definition.MissionId)
                    ? _missions.PublicActors.TryGetJoinRun(map, assignment, unit, out var run) &&
                        run.RunId == source.InstanceId && run.Generation == source.Generation
                    : source.InstanceId == identity.PartyLifetime.ToString("N") && source.Generation == 0) &&
                session.IsCurrent(sender) && PairIsCurrent(sender, recipient, identity);
        }

        private static bool OwnsAssignment(Client sender, Mission definition, CharacterMissionEntry assignment) =>
            sender?.Player is { } player &&
            assignment?.MissionState == (uint)MissionState.Active && assignment.Generation > 0 &&
            !string.IsNullOrWhiteSpace(assignment.AssignmentId) &&
            assignment.ContentRevision == definition.ContentRevision &&
            player.Missions.TryGetValue(definition.MissionId, out var runtime) &&
            runtime.State == MissionState.Active &&
            runtime.MatchesAssignment(assignment.AssignmentId, assignment.Generation, assignment.ContentRevision);

        private static bool PairIsCurrent(Client sender, Client recipient, MissionPartyOfferSource identity) =>
            PartyManager.Instance.TryGetLiveMembership(sender, out var party, out var sourceMember) &&
            PartyManager.Instance.TryGetLiveMembership(recipient, out var recipientParty, out var recipientMember) &&
            ReferenceEquals(party, recipientParty) && party.Id == identity.PartyId &&
            party.LifetimeId == identity.PartyLifetime && sourceMember.MembershipId == identity.SourceMembership &&
            recipientMember.MembershipId == identity.RecipientMembership &&
            !sender.Player.MapChannel.IsPrivateInstance &&
            ReferenceEquals(sender.Player.MapChannel, recipient.Player.MapChannel) &&
            InRange(sender.Player.Position, recipient.Player.Position);

        private static bool InRange(Vector3 source, Vector3 recipient)
        {
            if (!float.IsFinite(source.X) || !float.IsFinite(source.Y) || !float.IsFinite(source.Z) ||
                !float.IsFinite(recipient.X) || !float.IsFinite(recipient.Y) || !float.IsFinite(recipient.Z))
                return false;
            var x = (double)source.X - recipient.X;
            var y = (double)source.Y - recipient.Y;
            var z = (double)source.Z - recipient.Z;
            return x * x + y * y + z * z <= MaxRange * MaxRange;
        }

        private static bool Reject(string reason)
        {
            Logger.WriteLog(LogType.Error, reason);
            return false;
        }
    }
}
