using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Communicator.Server;
    using Packets.Party.Both;
    using Packets.Party.Client;
    using Packets.Party.Server;
    using Structures;

    /// <summary>
    /// Squads. Members are identified by account id (the client's userId) and stay in the
    /// party while they are out of the world: a member who logs out, drops or changes map is
    /// greyed out for the others and rejoins automatically when they next enter the world,
    /// unless HeldSpotMs passes first. Everything here runs on the main loop.
    /// </summary>
    public class PartyManager
    {
        /*   Party Packets:
         * - InviteUserToPartyByName', (targetName,))            => implemented
         * - SendJoinRequestToPartyByName', (targetName,))       => implemented
         * - InviteSquad', (targetName,))                        => implemented
         * - SendJoinRequestToSquadLeader', (targetName,))       => implemented
         * - CancelSquadInviteRequest', (targetName,))
         * - CancelSquadJoinRequest', (targetName,))
         * - PartyInvitationResponse', (accepted,))
         * - PartyJoinRequestResponse', (accepted, senderUserId)) => implemented
         * - LeaveParty', ())
         * - DisbandParty', ())
         * - KickUserFromParty', (name,))
         * - KickUserFromPartyById', (id,))
         * - MakeUserPartyLeader', (name,))                     => implemented
         * - MakeUserPartyLeaderById', (id,))                   => implemented
         * - ChangePartyLootMethod', (option,))
         * - ChangePartyLootThreshold', (quality,))
         * - AcceptPartyInvitesChanged', (value.lower() == 'true',))
         *  
         *   Party Handlers:
         * - SetCurrentPartyId(squadId, wasKicked = False)
         * - SquadMemberList(squadMembers, partyExclusiveMap)
         * - AddSquadMember(userId, entityId)
         * - RemoveSquadMember(userId, entityId)
         * - AddPartyMember(userId, name, classId, level, isAfk)
         * - RemovePartyMember(userId, wasKicked = False)
         * - PartyMemberList(partyList)
         * - UpdatePartyMemberInfo(userId, name, classId, level, isAfk)
         * - SetPartyLeader(userId)
         * - ChangePartyLootMethod(newOption)
         * - ChangePartyLootThreshold(newOption)
         * - InviteToParty(senderName, senderSquadInfo)
         * - JoinSquadRequestReceived(senderName, senderSquadInfo, senderUserId)
         * - InvitedPlayerToParty(inviteeName, isTargetAfk)
         * - JoinSquadRequestSent(inviteeName, isTargetAfk)
         * - SquadRequestCanceled(inviterName)
         * - SquadRequestSuccess(inviteeName)
         * - InviteSquadConfirmationRequest(inviteeName)
         * - RequestToJoinLeaderConfirmationRequest(inviteeName)
         * - SquadRequestDeclined(receiverName)
         * - PartyDisbanded()
         * - DisplayPartyMessage(msgId, args = { })              => implemented
         * - PartyMemberRoll(itemClassId, winnerUserId, rolls, isGreedRoll)
         * - PartyMemberLoot(userId, creatureEntityId, lootClassIds, moneyAmount)
         * - VoiceChatAvailable(isAvail)                        => implemented (always false)
         * - PartyMemberVoiceId(userId, voiceId)
         * - PartyMemberVoiceIds(memberList)
         * - VoiceChatConnectInfo(serverAddr, groupId, playerId, token)
         */

        #region Singleton

        private static PartyManager _instance;
        private static readonly object InstanceLock = new object();
        private uint _partyId = 1;
        private object _partyIdLock = new object();
        private List<uint> _freePartyIds = new List<uint>();
        public static PartyManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new PartyManager();
                    }
                }

                return _instance;
            }
        }

        private PartyManager()
        {
        }

        #endregion

        /// <summary>shared/gameconstants.py MAX_PARTY_SIZE.</summary>
        public const int MaxPartySize = 6;

        /// <summary>How long a member's spot is kept after they leave the world.</summary>
        public const long HeldSpotMs = 5 * 60 * 1000;

        /// <summary>
        /// Whether squad voice chat is offered. False: there is no voice server.
        ///
        /// The client's voice chat is complete and native - Talkback in tabula_rasa.exe, with the
        /// Sase 3200/6500, Speex, GSM and Clear codecs, and Sase6500_ncsoft.dll beside it - but it
        /// talks to a voice server of its own at an address this server would have to hand it, and
        /// no such server exists. Answering false is what keeps it dormant: the client's
        /// g_voiceAvailable starts at 0 and only Recv_VoiceChatAvailable assigns it, so it never
        /// sends RequestJoinVoiceChannel and never opens a session for push-to-talk to feed.
        ///
        /// Flipping this to true is not enough on its own. It also needs VoiceChatConnectInfo
        /// (serverAddr, groupId, playerId, token) in answer to RequestJoinVoiceChannel, handlers for
        /// RequestJoinVoiceChannel and RequestLeaveVoiceChannel, PartyMemberVoiceId(s) to map voice
        /// ids onto squad members for the speaking indicators, and a Talkback voice server to point
        /// it all at.
        /// </summary>
        public const bool VoiceChatAvailable = false;

        public uint GetPartyId
        {
            get
            {
                lock (_partyIdLock)
                {
                    if (_freePartyIds.Count > 0)
                    {
                        var freePartyId = _freePartyIds[0];

                        _freePartyIds.RemoveAt(0);

                        return freePartyId;
                    }

                    return _partyId++;
                }
            }
        }

        public void FreePartyId(uint id)
        {
            lock (_partyIdLock)
                if (!_freePartyIds.Contains(id))
                    _freePartyIds.Add(id);
        }

        internal Dictionary<uint, Party> Parties = new Dictionary<uint, Party>();

        /// <summary>
        /// Open invitations, keyed by invitee account id. PartyInvitationResponse carries only
        /// (accepted,) - no sender - so an invitee can hold one invitation at a time, and
        /// PM_CAN_ONLY_INVITE_ONE_PERSON_AT_A_TIME limits an inviter to one as well.
        /// </summary>
        private readonly Dictionary<uint, PendingInvite> _invites = new Dictionary<uint, PendingInvite>();

        private sealed class PendingInvite
        {
            public uint InviterId;
            public string InviterName;
            public uint InviteeId;
            public string InviteeName;

            /// <summary>
            /// The name the inviter's client is showing this invitation under - the player they
            /// named, who for a squad invitation is not the leader it was routed to. Every message
            /// that clears their pending indicator keys off it
            /// (client/party.py Recv_SquadRequestSuccess, Recv_SquadRequestDeclined).
            /// </summary>
            public string DisplayName;
        }

        /// <summary>
        /// Open join requests, keyed by the requester's account id. A leader can hold several, so
        /// PartyJoinRequestResponse carries the requester's account id; the requester holds one,
        /// because their revoke dialog is a single window.
        /// </summary>
        private readonly Dictionary<uint, PendingJoinRequest> _joinRequests = new Dictionary<uint, PendingJoinRequest>();

        private sealed class PendingJoinRequest
        {
            public uint RequesterId;
            public string RequesterName;
            public uint LeaderId;
            public string LeaderName;

            /// <summary>
            /// The name the requester's client is showing this request under - the player they
            /// asked, who may not be the leader it was routed to. Every message that clears their
            /// pending indicator has to use it.
            /// </summary>
            public string DisplayName;
        }

        #region Handlers

        internal void InviteUserToPartyByName(Client client, InviteUserToPartyByNamePacket packet)
        {
            if (!InWorld(client))
                return;

            var name = packet.FamilyName?.Trim() ?? string.Empty;
            var party = PartyOf(client);

            if (party != null && party.PartyLeaderId != client.AccountEntry.Id)
            {
                Message(client, PlayerMessage.PmYouAreNotPartyLeader);
                return;
            }

            var invitee = FindIngame(name);

            if (invitee == null)
            {
                Message(client, PlayerMessage.PmWhisperTargetNotInGame, "player", name);
                return;
            }

            var inviteeName = invitee.Player.FamilyName;

            if (invitee == client)
                return;

            if (party?.Find(invitee.AccountEntry.Id) != null)
            {
                Message(client, PlayerMessage.PmTheyAreAlreadyInYourParty, "invitee", inviteeName);
                return;
            }

            // Inviting someone who already has a squad means inviting the squad. The client
            // offers that rather than refusing: Recv_InviteSquadConfirmationRequest puts up
            // "<name> is already in a squad", and Accept sends InviteSquad with the same name.
            if (PartyOf(invitee) != null)
            {
                client.CallMethod(SysEntity.ClientPartyManagerId, new InviteSquadConfirmationRequestPacket(inviteeName));
                return;
            }

            Invite(client, invitee, inviteeName);
        }

        /// <summary>
        /// The answer to that offer: the same name again, now meaning the whole of that player's
        /// squad. The invitation goes to the leader of it, because that is who can accept on the
        /// squad's behalf - the mirror of SendJoinRequestToSquadLeader.
        /// </summary>
        internal void InviteSquad(Client client, InviteSquadPacket packet)
        {
            if (!InWorld(client))
                return;

            var name = packet.FamilyName?.Trim() ?? string.Empty;
            var party = PartyOf(client);

            // The invited squad joins this one, so the inviter has to be leading it.
            if (party != null && party.PartyLeaderId != client.AccountEntry.Id)
            {
                Message(client, PlayerMessage.PmYouAreNotPartyLeader);
                return;
            }

            var target = FindIngame(name);

            if (target == null || target == client)
            {
                Message(client, PlayerMessage.PmWhisperTargetNotInGame, "player", name);
                return;
            }

            var targetParty = PartyOf(target);

            // Squads change while a dialog sits on screen. Left theirs in the meantime: this is
            // an ordinary invitation now. Joined ours: nothing to do.
            if (targetParty == null)
            {
                Invite(client, target, name);
                return;
            }

            if (targetParty == party)
            {
                Message(client, PlayerMessage.PmTheyAreAlreadyInYourParty, "invitee", name);
                return;
            }

            var leader = FindIngame(targetParty.PartyLeaderId);

            if (leader == null)
            {
                Message(client, PlayerMessage.PmPartyRequestIsNoLongerValid);
                return;
            }

            // The inviter's window is showing the player they named, so that is the name every
            // message about this invitation has to carry back.
            Invite(client, leader, name);
        }

        internal void CancelSquadInviteRequest(Client client, CancelSquadInviteRequestPacket packet)
        {
            if (client.AccountEntry == null)
                return;

            var invite = _invites.Values.FirstOrDefault(i =>
                i.InviterId == client.AccountEntry.Id && string.Equals(i.DisplayName, packet.FamilyName?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (invite == null)
                return;

            _invites.Remove(invite.InviteeId);

            FindIngame(invite.InviteeId)?.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestCanceledPacket(invite.InviterName));
        }

        /// <summary>
        /// client/party.py SendJoinRequest: asking a player to be let into their squad. The whole
        /// of the requester's squad joins, so they have to be leading it, and the request goes to
        /// the leader of the squad they are asking to join.
        /// </summary>
        internal void SendJoinRequestToPartyByName(Client client, SendJoinRequestToPartyByNamePacket packet)
        {
            var target = FindJoinTarget(client, packet.FamilyName);

            if (target == null)
                return;

            var targetParty = PartyOf(target);

            // Asking a squad member rather than its leader: the client offers to send it on.
            if (targetParty != null && targetParty.PartyLeaderId != target.AccountEntry.Id)
            {
                client.CallMethod(SysEntity.ClientPartyManagerId, new RequestToJoinLeaderConfirmationRequestPacket(target.Player.FamilyName));
                return;
            }

            CreateJoinRequest(client, target, target.Player.FamilyName);
        }

        /// <summary>
        /// The answer to that offer: the same name again, now to be routed to the leader of that
        /// player's squad.
        /// </summary>
        internal void SendJoinRequestToSquadLeader(Client client, SendJoinRequestToSquadLeaderPacket packet)
        {
            var target = FindJoinTarget(client, packet.FamilyName);

            if (target == null)
                return;

            var targetParty = PartyOf(target);
            var leader = targetParty == null ? target : FindIngame(targetParty.PartyLeaderId);

            if (leader == null || leader == client)
            {
                Message(client, PlayerMessage.PmPartyRequestIsNoLongerValid);
                return;
            }

            // The requester's window is showing the player they asked, so that is the name every
            // message about this request has to carry back.
            CreateJoinRequest(client, leader, target.Player.FamilyName);
        }

        internal void CancelSquadJoinRequest(Client client, CancelSquadJoinRequestPacket packet)
        {
            if (client.AccountEntry == null || !_joinRequests.TryGetValue(client.AccountEntry.Id, out var request))
                return;

            if (!string.Equals(request.DisplayName, packet.FamilyName?.Trim(), StringComparison.OrdinalIgnoreCase))
                return;

            _joinRequests.Remove(request.RequesterId);

            // Clears the leader's merge window and its pending indicator.
            FindIngame(request.LeaderId)?.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestCanceledPacket(request.RequesterName));
        }

        /// <summary>The leader answering a join request; the client sends the requester's account id with it.</summary>
        internal void PartyJoinRequestResponse(Client client, PartyJoinRequestResponsePacket packet)
        {
            if (!InWorld(client))
                return;

            if (!_joinRequests.TryGetValue(packet.SenderUserId, out var request) || request.LeaderId != client.AccountEntry.Id)
            {
                Message(client, PlayerMessage.PmPartyRequestIsNoLongerValid);
                return;
            }

            _joinRequests.Remove(request.RequesterId);

            var requester = FindIngame(request.RequesterId);

            if (requester == null)
                return;

            // Closes the requester's revoke dialog and pending indicator, and says nothing itself.
            requester.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestSuccessPacket(request.DisplayName));

            if (!packet.Accepted)
            {
                requester.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestDeclinedPacket(client.Player.FamilyName));
                return;
            }

            var requesterParty = PartyOf(requester);
            var leaderParty = PartyOf(client);

            // Everything is checked again: squads change while a request sits on screen.
            if (requesterParty != null && requesterParty.PartyLeaderId != requester.AccountEntry.Id
                || leaderParty != null && leaderParty.PartyLeaderId != client.AccountEntry.Id
                || requesterParty != null && requesterParty == leaderParty)
            {
                Message(client, PlayerMessage.PmPartyRequestIsNoLongerValid);
                Message(requester, PlayerMessage.PmPartyRequestIsNoLongerValid);
                return;
            }

            if (JoiningSize(requesterParty) + (leaderParty?.Members.Count ?? 1) > MaxPartySize)
            {
                Message(client, PlayerMessage.PmPartyIsFull);
                Message(requester, PlayerMessage.PmPartyIsFull);
                return;
            }

            Message(client, PlayerMessage.PmPartyInvitationAccepted, "invitee", requester.Player.FamilyName);

            if (leaderParty == null)
            {
                if (requesterParty == null)
                {
                    CreateParty(client, requester);
                    return;
                }

                leaderParty = CreateParty(client);
            }

            if (requesterParty == null)
                AddMember(leaderParty, requester);
            else
                MergeInto(leaderParty, requesterParty);
        }

        internal void PartyInvitationResponse(Client client, PartyInvitationResponsePacket packet)
        {
            if (!InWorld(client))
                return;

            if (!_invites.Remove(client.AccountEntry.Id, out var invite))
            {
                if (packet.Response)
                    Message(client, PlayerMessage.PmPartyInvitationHasBeenRevoked);

                return;
            }

            var inviter = FindIngame(invite.InviterId);

            if (inviter == null)
            {
                if (packet.Response)
                    Message(client, PlayerMessage.PmPartyInvitationHasBeenRevoked);

                return;
            }

            if (!packet.Response)
            {
                // Keyed by the name the inviter's indicator carries, not the decliner's:
                // Recv_SquadRequestDeclined kills the indicator by the name it is given.
                inviter.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestDeclinedPacket(invite.DisplayName));
                return;
            }

            // The world may have moved on since the invitation was sent.
            var inviterParty = PartyOf(inviter);
            var inviteeParty = PartyOf(client);

            // Everything is checked again, and both sides have to still lead what they are
            // bringing: a squad invitation is answered by a leader on behalf of their squad.
            if (inviterParty != null && inviterParty.PartyLeaderId != inviter.AccountEntry.Id
                || inviteeParty != null && inviteeParty.PartyLeaderId != client.AccountEntry.Id
                || inviteeParty != null && inviteeParty == inviterParty)
            {
                Message(client, PlayerMessage.PmPartyInvitationHasBeenRevoked);
                inviter.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestSuccessPacket(invite.DisplayName));
                return;
            }

            if ((inviterParty?.Members.Count ?? 1) + JoiningSize(inviteeParty) > MaxPartySize)
            {
                Message(client, PlayerMessage.PmPartyIsFull);
                Message(inviter, PlayerMessage.PmPartyIsFull);
                inviter.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestSuccessPacket(invite.DisplayName));
                return;
            }

            inviter.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestSuccessPacket(invite.DisplayName));
            Message(inviter, PlayerMessage.PmPartyInvitationAccepted, "invitee", invite.DisplayName);

            if (inviterParty == null)
            {
                if (inviteeParty == null)
                {
                    CreateParty(inviter, client);
                    return;
                }

                inviterParty = CreateParty(inviter);
            }

            if (inviteeParty == null)
                AddMember(inviterParty, client);
            else
                MergeInto(inviterParty, inviteeParty);
        }

        internal void AcceptPartyInvitesChanged(Client client, AcceptPartyInvitesChangedPacket packet)
        {
            if (client.Player != null)
                client.Player.AcceptPartyInvites = packet.Accept;
        }

        internal void LeaveParty(Client client)
        {
            if (!InWorld(client))
                return;

            var party = PartyOf(client);

            if (party == null)
            {
                // Nothing to leave here, but the client thinks otherwise: bring it back in line.
                ResetClient(client, false);
                return;
            }

            RemoveMember(party, party.Find(client.AccountEntry.Id), false);
        }

        internal void DisbandParty(Client client)
        {
            if (!InWorld(client))
                return;

            var party = PartyOf(client);

            if (party == null)
            {
                ResetClient(client, false);
                return;
            }

            if (party.PartyLeaderId != client.AccountEntry.Id)
            {
                Message(client, PlayerMessage.PmYouAreNotPartyLeader);
                return;
            }

            Disband(party);
        }

        internal void KickUserFromParty(Client client, KickUserFromPartyPacket packet)
        {
            var party = LedParty(client);

            var target = party?.Members.Find(m => string.Equals(m.MemberName, packet.FamilyName, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                if (party != null)
                    Message(client, PlayerMessage.PmWhisperTargetNotInGame, "player", packet.FamilyName);

                return;
            }

            Kick(party, client, target);
        }

        internal void KickUserFromPartyById(Client client, KickUserFromPartyByIdPacket packet)
        {
            var party = LedParty(client);
            var target = party?.Find(packet.UserId);

            if (target != null)
                Kick(party, client, target);
        }

        /// <summary>
        /// client/party.py SendChangeLeader: the leader naming the member to hand the squad to.
        /// Typed names reach this one, so a name that is not in the squad is answered.
        /// </summary>
        internal void MakeUserPartyLeader(Client client, MakeUserPartyLeaderPacket packet)
        {
            var party = LedParty(client);

            if (party == null)
                return;

            var name = packet.FamilyName?.Trim() ?? string.Empty;
            var target = party.Members.Find(m => string.Equals(m.MemberName, name, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                Message(client, PlayerMessage.PmWhisperTargetNotInGame, "player", name);
                return;
            }

            HandLeadership(party, client, target);
        }

        /// <summary>
        /// client/party.py SendChangeLeaderById, from the party window. The id comes out of that
        /// window, so one that is not in the squad means the window is behind: it is resent
        /// rather than answered with a message about a player the leader never named.
        /// </summary>
        internal void MakeUserPartyLeaderById(Client client, MakeUserPartyLeaderByIdPacket packet)
        {
            var party = LedParty(client);

            if (party == null)
                return;

            var target = party.Find(packet.UserId);

            if (target == null)
            {
                SendPartyState(party, client);
                return;
            }

            HandLeadership(party, client, target);
        }

        internal void ChangePartyLootMethod(Client client, ChangePartyLootMethodPacket packet)
        {
            var party = LedParty(client);

            if (party == null)
                return;

            party.LootMethod = packet.PartyLootMethod;

            foreach (var member in OnlineClients(party))
                member.CallMethod(SysEntity.ClientPartyManagerId, new ChangePartyLootMethodPacket(packet.PartyLootMethod));
        }

        internal void ChangePartyLootThreshold(Client client, ChangePartyLootThresholdPacket packet)
        {
            var party = LedParty(client);

            if (party == null)
                return;

            party.LootThreshold = packet.PartyLootThreshold;

            foreach (var member in OnlineClients(party))
                member.CallMethod(SysEntity.ClientPartyManagerId, new ChangePartyLootThresholdPacket(packet.PartyLootThreshold));
        }

        #endregion

        #region World entry and exit

        /// <summary>
        /// MapChannelManager.RemovePlayer: logout, inactivity logout and dropped connection, where
        /// the member's spot is held rather than given up - and every map change, which is not
        /// leaving the world at all (see <see cref="MemberChangingMap"/>).
        /// </summary>
        public void RemovePlayer(Client client)
        {
            if (client.AccountEntry == null)
                return;

            // MapChannelManager.ChangeMap takes the player off the old map already Loading - a map
            // link, a summon, a GM teleport. Every one of those used to be a logout: the squad was
            // told they had logged out and then logged in again, a leader lost the lead to whoever
            // was next and did not get it back, their open invitations and join requests were
            // cancelled as if they had gone, and their own client was told on arrival that they had
            // left the squad before being put back in it.
            if (client.State == ClientState.Loading)
            {
                MemberChangingMap(client);
                return;
            }

            DropInvites(client.AccountEntry.Id);

            var party = FindPartyOfAccount(client.AccountEntry.Id);

            if (client.Player != null)
                client.Player.PartyId = 0;

            var member = party?.Find(client.AccountEntry.Id);

            if (member == null || !member.IsOnline)
                return;

            var entityId = member.EntityId;

            member.InvalidateMissionMembership();
            member.EntityId = 0;
            member.OfflineSinceTick = Environment.TickCount64;

            foreach (var other in OnlineClients(party))
                other.CallMethod(SysEntity.ClientPartyManagerId, new RemoveSquadMemberPacket(member.UserId, entityId));

            MessageParty(party, PlayerMessage.PmPartyMemberLoggedOut, "player", member.MemberName);

            // A squad whose leader is away cannot invite; leadership moves to someone present.
            if (party.PartyLeaderId == member.UserId)
                PassLeadership(party);
        }

        /// <summary>
        /// A member on their way to another map. As far as the squad goes they are still online: no
        /// spot is held, a leader keeps the lead, and invitations and join requests to and from them
        /// stand. Squad traffic keeps reaching them on the loading screen (<see cref="FindMember"/>).
        /// What the rest of the squad loses is their manifestation, which has just left this map, so
        /// they are greyed out until PlayerEnteredWorld puts them back when the new map has loaded.
        /// </summary>
        private void MemberChangingMap(Client client)
        {
            var party = FindPartyOfAccount(client.AccountEntry.Id);
            var member = party?.Find(client.AccountEntry.Id);

            if (member == null || !member.IsOnline)
                return;

            member.InvalidateMissionMembership();
            foreach (var other in OnlineClients(party))
                if (other != client)
                    other.CallMethod(SysEntity.ClientPartyManagerId, new RemoveSquadMemberPacket(member.UserId, member.EntityId));
        }

        /// <summary>
        /// MapChannelManager.MapLoaded, when a character enters the world (not on a dropship
        /// teleport, which keeps the same manifestation). Rejoins a held spot, or clears any
        /// party the client still remembers from before it left the world.
        /// </summary>
        public void PlayerEnteredWorld(Client client)
        {
            var party = FindPartyOfAccount(client.AccountEntry.Id);

            if (party == null)
            {
                ResetClient(client, false);
                return;
            }

            var member = party.Find(client.AccountEntry.Id);

            // Still online means this is the end of a map change, not a login: a member leaving the
            // world has their entity id cleared in RemovePlayer, and one changing maps does not.
            var changedMap = member.IsOnline;

            member.Refresh(client);
            member.OfflineSinceTick = 0;
            client.Player.PartyId = party.Id;

            foreach (var other in OnlineClients(party))
            {
                if (other == client)
                    continue;

                other.CallMethod(SysEntity.ClientPartyManagerId, new UpdatePartyMemberInfoPacket(member));
                other.CallMethod(SysEntity.ClientPartyManagerId, new AddSquadMemberPacket(member.UserId, member.EntityId));
            }

            // Arriving from another map, the member's client has the squad already: it was never
            // told it had left, and it went on hearing about the squad while the map loaded.
            // SendPartyState would start by clearing its party id, which it announces as having left
            // the squad.
            if (!changedMap)
            {
                // The returning member is not told they logged in; everyone else is.
                MessageParty(party, PlayerMessage.PmPartyMemberLoggedIn, "player", member.MemberName, client);

                SendPartyState(party, client);
            }

            if (party.Find(party.PartyLeaderId)?.IsOnline != true)
                PassLeadership(party);
        }

        /// <summary>
        /// Pushes a member's current name, class, level and AFK flag to the rest of their squad.
        ///
        /// The party window is driven entirely by these tuples: party.py keeps g_partyMembers as
        /// (name, classId, level, isAfk) per account id and only ever rewrites an entry from
        /// Recv_AddPartyMember or Recv_UpdatePartyMemberInfo. Until this existed the update was
        /// sent from one place - PlayerEnteredWorld - so anything that changed about a member
        /// while they stayed in the world was invisible to everyone else until they relogged:
        /// going AFK left them listed as present, and levelling left the old level on screen.
        ///
        /// Call it after changing anything in that tuple. Cheap and idempotent: no party, a held
        /// spot, or a squad of one all fall through without sending.
        /// </summary>
        public void MemberInfoChanged(Client client)
        {
            if (client?.AccountEntry == null || client.Player == null)
                return;

            var party = FindPartyOfAccount(client.AccountEntry.Id);
            var member = party?.Find(client.AccountEntry.Id);

            // A member whose spot is only being held has no live character to read.
            if (member == null || !member.IsOnline)
                return;

            member.Refresh(client);

            foreach (var other in OnlineClients(party))
            {
                // The recipient is never in their own g_partyMembers (see SendPartyState), so
                // has_key would miss and the call would do nothing. Skip it rather than send it.
                if (other == client)
                    continue;

                other.CallMethod(SysEntity.ClientPartyManagerId, new UpdatePartyMemberInfoPacket(member));
            }
        }

        /// <summary>Called every MapChannelWorker tick. Gives up spots held longer than HeldSpotMs.</summary>
        public void ExpireHeldMembers()
        {
            if (Parties.Count == 0)
                return;

            var now = Environment.TickCount64;

            foreach (var party in Parties.Values.ToList())
                foreach (var member in party.Members.Where(m => !m.IsOnline && now - m.OfflineSinceTick >= HeldSpotMs).ToList())
                    if (Parties.ContainsKey(party.Id))
                        RemoveMember(party, member, false);
        }

        #endregion

        #region Helper Functions

        /// <summary>
        /// Shared checks for both join-request entry points. Returns the player being asked, or
        /// null after telling the requester why not.
        /// </summary>
        private Client FindJoinTarget(Client client, string familyName)
        {
            if (!InWorld(client))
                return null;

            var name = familyName?.Trim() ?? string.Empty;
            var party = PartyOf(client);

            // The whole of the requester's squad joins, so a member cannot ask on its behalf.
            if (party != null && party.PartyLeaderId != client.AccountEntry.Id)
            {
                Message(client, PlayerMessage.PmYouAreNotPartyLeader);
                return null;
            }

            var target = FindIngame(name);

            if (target == null)
            {
                Message(client, PlayerMessage.PmWhisperTargetNotInGame, "player", name);
                return null;
            }

            if (target == client)
                return null;

            if (party?.Find(target.AccountEntry.Id) != null)
            {
                Message(client, PlayerMessage.PmTheyAreAlreadyInYourParty, "invitee", target.Player.FamilyName);
                return null;
            }

            return target;
        }

        /// <summary>
        /// Records an invitation and puts it on both screens. Shared by the two entry points: a
        /// plain invitation, where the recipient is the player who was named, and a squad
        /// invitation, where it is the leader of that player's squad.
        /// </summary>
        private void Invite(Client inviter, Client recipient, string displayName)
        {
            var inviterParty = PartyOf(inviter);
            var recipientParty = PartyOf(recipient);

            if (!recipient.Player.AcceptPartyInvites)
            {
                Message(inviter, PlayerMessage.PmPartyNotAcceptingInvite);
                return;
            }

            if (_invites.ContainsKey(recipient.AccountEntry.Id))
            {
                // Includes a repeat invitation from this inviter: every InviteToParty adds
                // another pending indicator on the recipient's screen.
                Message(inviter, PlayerMessage.PmUserAlreadyInvited, "name", displayName);
                return;
            }

            if (_invites.Values.Any(i => i.InviterId == inviter.AccountEntry.Id))
            {
                Message(inviter, PlayerMessage.PmCanOnlyInviteOnePersonAtATime);
                return;
            }

            // A squad invitation needs a seat for every one of its members, held spots included.
            if ((inviterParty?.Members.Count ?? 1) + JoiningSize(recipientParty) > MaxPartySize)
            {
                Message(inviter, PlayerMessage.PmPartyIsFull);
                return;
            }

            _invites[recipient.AccountEntry.Id] = new PendingInvite
            {
                InviterId = inviter.AccountEntry.Id,
                InviterName = inviter.Player.FamilyName,
                InviteeId = recipient.AccountEntry.Id,
                InviteeName = recipient.Player.FamilyName,
                DisplayName = displayName
            };

            recipient.CallMethod(SysEntity.ClientPartyManagerId, new InviteToPartyPacket(inviter.Player.FamilyName, SquadInfo(inviter)));
            inviter.CallMethod(SysEntity.ClientPartyManagerId, new InvitedPlayerToPartyPacket(displayName, recipient.Player.IsAFK));
        }

        private void CreateJoinRequest(Client requester, Client leader, string displayName)
        {
            var requesterParty = PartyOf(requester);
            var leaderParty = PartyOf(leader);

            if (JoiningSize(requesterParty) + (leaderParty?.Members.Count ?? 1) > MaxPartySize)
            {
                Message(requester, PlayerMessage.PmPartyIsFull);
                return;
            }

            // One request at a time: the requester has a single revoke dialog. A new one replaces
            // the old, which means telling whoever was asked that it is gone.
            if (_joinRequests.TryGetValue(requester.AccountEntry.Id, out var previous))
            {
                _joinRequests.Remove(previous.RequesterId);
                FindIngame(previous.LeaderId)?.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestCanceledPacket(previous.RequesterName));
            }

            _joinRequests[requester.AccountEntry.Id] = new PendingJoinRequest
            {
                RequesterId = requester.AccountEntry.Id,
                RequesterName = requester.Player.FamilyName,
                LeaderId = leader.AccountEntry.Id,
                LeaderName = leader.Player.FamilyName,
                DisplayName = displayName
            };

            leader.CallMethod(SysEntity.ClientPartyManagerId,
                new JoinSquadRequestReceivedPacket(requester.Player.FamilyName, SquadInfo(requester), requester.AccountEntry.Id));
            requester.CallMethod(SysEntity.ClientPartyManagerId, new JoinSquadRequestSentPacket(displayName, leader.Player.IsAFK));
        }

        /// <summary>How many seats a join request needs: the requester alone, or their whole squad.</summary>
        private static int JoiningSize(Party requesterParty) => requesterParty?.Members.Count ?? 1;

        /// <summary>
        /// Moves every member of one squad into another and drops the empty one. Members who are
        /// out of the world keep their held spot in the squad they end up in.
        /// </summary>
        private void MergeInto(Party party, Party source)
        {
            var moving = source.Members.ToList();
            // The members already there: the arrivals hear about each other from the member list
            // that follows, not from one AddPartyMember per arrival.
            var existing = OnlineClients(party);

            source.Members.Clear();
            Parties.Remove(source.Id);
            FreePartyId(source.Id);

            foreach (var member in moving)
                AddMemberEntry(party, member, existing);

            // State goes out after every member is in the list, so the arrivals see each other.
            foreach (var member in moving)
            {
                var client = member.IsOnline ? FindIngame(member.UserId) : null;

                if (client == null)
                    continue;

                client.Player.PartyId = party.Id;
                SendPartyState(party, client);
                Message(client, PlayerMessage.PmYouJoinedTheParty);
            }

            AdsChanged(party);
        }

        private void CreateParty(Client leader, Client member)
        {
            var party = new Party(GetPartyId, leader.AccountEntry.Id, new List<PartyMember>
            {
                new PartyMember(leader),
                new PartyMember(member)
            });

            Parties[party.Id] = party;
            leader.Player.PartyId = party.Id;
            member.Player.PartyId = party.Id;

            SendPartyState(party, leader);
            SendPartyState(party, member);
            Message(member, PlayerMessage.PmYouJoinedTheParty);

            AdsChanged(party);
        }

        /// <summary>A squad of one, for a leader who is about to be joined by another squad.</summary>
        private Party CreateParty(Client leader)
        {
            var party = new Party(GetPartyId, leader.AccountEntry.Id, new List<PartyMember> { new PartyMember(leader) });

            Parties[party.Id] = party;
            leader.Player.PartyId = party.Id;
            SendPartyState(party, leader);

            return party;
        }

        private void AddMember(Party party, Client client)
        {
            AddMemberEntry(party, new PartyMember(client), OnlineClients(party));

            client.Player.PartyId = party.Id;

            SendPartyState(party, client);
            Message(client, PlayerMessage.PmYouJoinedTheParty);

            AdsChanged(party);
        }

        /// <summary>Adds one member to a squad and tells the members it already had.</summary>
        private static void AddMemberEntry(Party party, PartyMember member, List<Client> tell)
        {
            member.InvalidateMissionMembership();
            foreach (var other in tell)
            {
                other.CallMethod(SysEntity.ClientPartyManagerId, new AddPartyMemberPacket(member));

                if (member.IsOnline)
                    other.CallMethod(SysEntity.ClientPartyManagerId, new AddSquadMemberPacket(member.UserId, member.EntityId));
            }

            party.Members.Add(member);
        }

        /// <summary>
        /// Everything a client needs to show the party it is in. Recv_SetCurrentPartyId raises
        /// if the client already has a party id, and a client that went back to character
        /// select still has the one it left with, so it is cleared first; with no party id on
        /// the client that clear does nothing visible.
        /// </summary>
        private void SendPartyState(Party party, Client client)
        {
            var others = LiveMembers(party).Where(m => m.UserId != client.AccountEntry.Id).ToList();

            client.CallMethod(SysEntity.ClientPartyManagerId, new SetCurrentPartyIdPacket(0));
            client.CallMethod(SysEntity.ClientPartyManagerId, new SetCurrentPartyIdPacket(party.Id));
            // The recipient is not in their own list: g_partyMembers holds everyone else
            // (party.py GetFullPartyMembersCopy adds the current player itself). Sending the
            // recipient too put them in their own party window.
            client.CallMethod(SysEntity.ClientPartyManagerId, new PartyMemberListPacket(others));
            // After the list: SetPartyLeader resolves the id against it.
            client.CallMethod(SysEntity.ClientPartyManagerId, new SetPartyLeaderPacket(party.PartyLeaderId));
            client.CallMethod(SysEntity.ClientPartyManagerId, new SquadMemberListPacket(
                others.Where(m => m.IsOnline).Select(m => (m.UserId, m.EntityId)).ToList()));
            // Said with the rest of the squad state, which is where the answer would have to go if
            // it were ever true: the client asks to join a voice channel the moment it hears yes.
            client.CallMethod(SysEntity.ClientPartyManagerId, new VoiceChatAvailablePacket(VoiceChatAvailable));
        }

        private void RemoveMember(Party party, PartyMember member, bool kicked)
        {
            if (member == null)
                return;

            member.InvalidateMissionMembership();
            party.Members.Remove(member);

            if (member.IsOnline)
            {
                var leaver = FindIngame(member.UserId);

                if (leaver != null)
                {
                    leaver.Player.PartyId = 0;
                    ResetClient(leaver, kicked);
                }
            }

            foreach (var other in OnlineClients(party))
                other.CallMethod(SysEntity.ClientPartyManagerId, new RemovePartyMemberPacket(member.UserId, kicked));

            if (party.Members.Count < 2)
            {
                Disband(party);
                AdsChanged(null, member.UserId);
                return;
            }

            if (party.PartyLeaderId == member.UserId)
                PassLeadership(party);

            // The leaver too: their own ad now recruits for a squad they are not in.
            AdsChanged(party, member.UserId);
        }

        private void Kick(Party party, Client leader, PartyMember target)
        {
            if (target.UserId == leader.AccountEntry.Id)
                return;

            RemoveMember(party, target, true);
        }

        private void Disband(Party party)
        {
            foreach (var member in OnlineClients(party))
            {
                member.Player.PartyId = 0;
                member.CallMethod(SysEntity.ClientPartyManagerId, new PartyDisbandedPacket());
            }

            var former = party.Members.Select(m => m.UserId).ToArray();

            foreach (var member in party.Members)
                member.InvalidateMissionMembership();
            party.Members.Clear();
            Parties.Remove(party.Id);
            FreePartyId(party.Id);

            AdsChanged(null, former);
        }

        /// <summary>
        /// Hands leadership to the first member in the world, in join order. With nobody in
        /// the world the current leader keeps it.
        /// </summary>
        private void PassLeadership(Party party)
        {
            var next = party.Members.FirstOrDefault(m => m.IsOnline && m.UserId != party.PartyLeaderId)
                       ?? party.Members.FirstOrDefault(m => m.IsOnline);

            if (next == null && party.Find(party.PartyLeaderId) == null)
                next = party.Members.FirstOrDefault();

            if (next != null && next.UserId != party.PartyLeaderId)
                SetLeader(party, next);
        }

        /// <summary>
        /// The leader handing the squad to another member. A member whose spot is only being
        /// held is refused: leading is done from the world, so a squad led by someone who is not
        /// in it has nobody who can invite, kick, set loot or hand it on again, and the only way
        /// out is for everyone to leave.
        /// </summary>
        private void HandLeadership(Party party, Client leader, PartyMember target)
        {
            if (target.UserId == leader.AccountEntry.Id)
                return;

            if (!target.IsOnline)
            {
                Message(leader, PlayerMessage.PmPartyMemberLoggedOut, "player", target.MemberName);
                return;
            }

            SetLeader(party, target);
        }

        private void SetLeader(Party party, PartyMember leader)
        {
            if (party.PartyLeaderId == leader.UserId)
                return;

            var previousLeaderId = party.PartyLeaderId;

            party.PartyLeaderId = leader.UserId;

            // The client reads leadership off this one call: SetPartyLeader with an id that is
            // not in that client's member list - which never contains itself - is how it learns
            // that it is the leader now. Everyone gets it, so both sides of the handover update.
            foreach (var member in OnlineClients(party))
                member.CallMethod(SysEntity.ClientPartyManagerId, new SetPartyLeaderPacket(leader.UserId));

            MoveJoinRequests(previousLeaderId, leader);
            AdsChanged(party, previousLeaderId);
        }

        /// <summary>
        /// Join requests are answered by whoever leads the squad, so a handover carries the open
        /// ones across: the old leader's merge window is closed and the new leader's opens, with
        /// the requester left looking at the name they asked for. If the new leader is not in the
        /// world the request is dropped instead and the requester's dialog closed.
        /// </summary>
        private void MoveJoinRequests(uint previousLeaderId, PartyMember leader)
        {
            var pending = _joinRequests.Values.Where(r => r.LeaderId == previousLeaderId).ToList();

            if (pending.Count == 0)
                return;

            var previousLeader = FindIngame(previousLeaderId);
            var newLeader = leader.IsOnline ? FindIngame(leader.UserId) : null;

            foreach (var request in pending)
            {
                previousLeader?.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestCanceledPacket(request.RequesterName));

                var requester = FindIngame(request.RequesterId);

                if (newLeader == null || requester == null)
                {
                    _joinRequests.Remove(request.RequesterId);

                    if (requester != null)
                    {
                        requester.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestSuccessPacket(request.DisplayName));
                        Message(requester, PlayerMessage.PmPartyRequestIsNoLongerValid);
                    }

                    continue;
                }

                request.LeaderId = newLeader.AccountEntry.Id;
                request.LeaderName = newLeader.Player.FamilyName;

                newLeader.CallMethod(SysEntity.ClientPartyManagerId,
                    new JoinSquadRequestReceivedPacket(request.RequesterName, SquadInfo(requester), request.RequesterId));
            }
        }

        /// <summary>SetCurrentPartyId(None) and SetPartyLeader(None): the client's "not in a squad" state.</summary>
        private static void ResetClient(Client client, bool kicked)
        {
            client.CallMethod(SysEntity.ClientPartyManagerId, new SetCurrentPartyIdPacket(0, kicked));
            client.CallMethod(SysEntity.ClientPartyManagerId, new SetPartyLeaderPacket(0));
        }

        private void DropInvites(uint accountId)
        {
            if (_joinRequests.TryGetValue(accountId, out var sentRequest))
            {
                _joinRequests.Remove(accountId);
                FindIngame(sentRequest.LeaderId)?.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestCanceledPacket(sentRequest.RequesterName));
            }

            foreach (var asked in _joinRequests.Values.Where(r => r.LeaderId == accountId).ToList())
            {
                _joinRequests.Remove(asked.RequesterId);

                var requester = FindIngame(asked.RequesterId);

                if (requester != null)
                {
                    requester.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestSuccessPacket(asked.DisplayName));
                    Message(requester, PlayerMessage.PmInviteeLoggedOut, "name", asked.LeaderName);
                }
            }

            if (_invites.Remove(accountId, out var received))
            {
                var inviter = FindIngame(received.InviterId);

                if (inviter != null)
                {
                    // Success is the only message that closes the inviter's revoke dialog as
                    // well as the pending indicator, and it prints nothing of its own.
                    inviter.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestSuccessPacket(received.DisplayName));
                    Message(inviter, PlayerMessage.PmInviteeLoggedOut, "name", received.DisplayName);
                }
            }

            foreach (var sent in _invites.Values.Where(i => i.InviterId == accountId).ToList())
            {
                _invites.Remove(sent.InviteeId);
                FindIngame(sent.InviteeId)?.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestCanceledPacket(sent.InviterName));
            }
        }

        /// <summary>
        /// The squad tuples the client's invite and join-request windows list for a player: their
        /// whole squad, or the player on their own.
        /// </summary>
        private List<PartyMember> SquadInfo(Client client)
        {
            var party = PartyOf(client);

            return party != null ? LiveMembers(party) : new List<PartyMember> { new PartyMember(client) };
        }

        /// <summary>The members with name, level and AFK read from the live character where there is one.</summary>
        private static List<PartyMember> LiveMembers(Party party)
        {
            foreach (var member in party.Members.Where(m => m.IsOnline))
            {
                var client = FindIngame(member.UserId);

                if (client != null)
                    member.Refresh(client);
            }

            return party.Members.ToList();
        }

        private static List<Client> OnlineClients(Party party)
        {
            var clients = new List<Client>();

            foreach (var member in party.Members)
            {
                if (!member.IsOnline)
                    continue;

                var client = FindMember(member.UserId);

                if (client != null)
                    clients.Add(client);
            }

            return clients;
        }

        internal Party PartyOf(Client client)
        {
            if (client?.Player == null || client.Player.PartyId == 0)
                return null;

            return Parties.TryGetValue(client.Player.PartyId, out var party) && party.Find(client.AccountEntry.Id) != null
                ? party
                : null;
        }

        internal bool TryGetLiveMembership(Client client, out Party party, out PartyMember member)
        {
            party = null;
            member = null;
            if (!Rasa.Game.Missions.Integration.MissionInteractionPolicy.IsActivePlayer(client))
                return false;
            party = PartyOf(client);
            member = party?.Find(client.AccountEntry.Id);
            return member?.IsOnline == true && member.EntityId == client.Player.EntityId &&
                member.CharacterId == client.Player.Id;
        }

        private Party FindPartyOfAccount(uint accountId) => Parties.Values.FirstOrDefault(p => p.Find(accountId) != null);

        /// <summary>The caller's party if they lead it; otherwise tells them why not and returns null.</summary>
        private Party LedParty(Client client)
        {
            if (!InWorld(client))
                return null;

            var party = PartyOf(client);

            if (party == null)
            {
                Message(client, PlayerMessage.PmActionFailedNoParty);
                return null;
            }

            if (party.PartyLeaderId != client.AccountEntry.Id)
            {
                Message(client, PlayerMessage.PmYouAreNotPartyLeader);
                return null;
            }

            return party;
        }

        /// <summary>
        /// Two things hang off squad membership and are told about every change to it, for each
        /// account involved. A looking-for-group ad recruits for the squad its placer leads, so it
        /// is dropped when they stop leading or the squad fills up; a pending summon is only ever
        /// between squadmates, so it is dropped when that stops being true. An account with
        /// neither costs two dictionary misses, which is cheaper than working out here who might
        /// be affected.
        /// </summary>
        private static void AdsChanged(Party party, params uint[] alsoAccounts)
        {
            var lfg = LookingForGroupManager.Instance;
            var summons = SummonManager.Instance;

            if (party != null)
                foreach (var member in party.Members.ToList())
                {
                    lfg.PartyChanged(member.UserId);
                    summons.PartyChanged(member.UserId);
                }

            foreach (var account in alsoAccounts)
            {
                lfg.PartyChanged(account);
                summons.PartyChanged(account);
            }
        }

        private static bool InWorld(Client client) =>
            client?.Player != null && client.AccountEntry != null && client.State == ClientState.Ingame;

        private static Client FindIngame(uint accountId) =>
            Server.Clients.Find(c => c.State == ClientState.Ingame && c.Player != null && c.AccountEntry != null && c.AccountEntry.Id == accountId);

        /// <summary>
        /// A squad member's connection while their character is in the world, including between
        /// maps - loading into the next one, or riding a dropship - where FindIngame does not see
        /// them. Squad traffic goes on reaching them there, which is what lets a map change leave
        /// their squad alone: the squad they arrive in is the one they left, and nothing has to be
        /// sent again. A dropship rider was never sent anything again either, and used to miss
        /// whatever happened to the squad during the ride.
        ///
        /// For squad broadcasts only, to members the squad counts online. A member logging in from a
        /// held spot is on a loading screen too, but not online, and is sent the whole squad on arrival.
        /// </summary>
        private static Client FindMember(uint accountId) =>
            Server.Clients.Find(c => (c.State == ClientState.Ingame || c.State == ClientState.Teleporting || c.State == ClientState.Loading)
                                     && c.Player != null && c.AccountEntry != null && c.AccountEntry.Id == accountId);

        private static Client FindIngame(string familyName) =>
            string.IsNullOrEmpty(familyName)
                ? null
                : Server.Clients.Find(c => c.State == ClientState.Ingame && c.Player != null && c.AccountEntry != null
                                           && string.Equals(c.Player.FamilyName, familyName, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// One message to everyone in the squad who is in the world, on the party manager's own
        /// channel. Recv_DisplayPartyMessage puts it in the chat window exactly as the per-client
        /// Message does - the id-aware handling that channel can do (voice-overs, big text, status
        /// icons) covers no party message - so this is about the shape of the call rather than what
        /// the player sees: a squad-wide announcement said once, by the manager that owns the squad.
        /// </summary>
        private static void MessageParty(Party party, PlayerMessage message, string key = null, string value = null, Client except = null)
        {
            var args = new Dictionary<string, string>();

            if (key != null)
                args[key] = value ?? string.Empty;

            foreach (var member in OnlineClients(party))
                if (member != except)
                    member.CallMethod(SysEntity.ClientPartyManagerId, new DisplayPartyMessagePacket(message, args));
        }

        private static void Message(Client client, PlayerMessage message, string key = null, string value = null)
        {
            var args = new Dictionary<string, string>();

            if (key != null)
                args[key] = value ?? string.Empty;

            client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(message, args, MsgFilterId.GeneralSystemMessages));
        }

        #endregion
    }
}
