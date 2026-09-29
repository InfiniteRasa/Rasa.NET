using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Game.Server;
    using Packets.Summon.Client;
    using Packets.Summon.Server;
    using Structures;

    /// <summary>
    /// Summoning: moving one player to another's position on request.
    ///
    /// Two directions, both consenting and both gated on the two players being in the same squad:
    ///
    ///   InviteFriendToJoin(name)      "summon them to me"   -> InvitedToAddAndJoinFriend -> they accept
    ///   RequestInvitationToJoin(name) "take me to them"     -> RequestToJoin             -> they accept
    ///
    /// The 1.16.5.0 client cannot reach either one. communicator.py has SummonFriend and GotoFriend
    /// ("invite the given friend to be teleported to your position" / "request to join your friends
    /// position"), but no slash command in generated.client.slashcommand maps to them and no UI
    /// calls gameui.OnInviteFriendToJoin - the social window's friend menu offers only
    /// ID_SOCIAL_SQUAD_INVITE, which is the ordinary party invite. So nothing here runs today.
    ///
    /// It is built anyway for the summon stone: a world object a squad interacts with to pull the
    /// rest of the squad to it. That needs no client change - <see cref="Summon"/> is the entry
    /// point, and a usable-object handler can call it for each squad member.
    /// </summary>
    public class SummonManager
    {
        /*   Summon packets (8.5):
         * - InviteFriendToJoin', (playerName,))                          => implemented
         * - RequestInvitationToJoin', (playerName,))                     => implemented
         * - RespondToJoinFriend', (invitationId, response))              => implemented
         * - RespondToAddAndJoinFriend', (invitationId, friendName, response)) => implemented
         * - RespondToRequestToJoin', (requestId, response))              => implemented
         *
         *   Summon handlers (client/augmentations/manifestation.py):
         * - InvitedToJoinFriend(invitationId, inviterName)               => implemented (unanswerable, see below)
         * - InvitedToAddAndJoinFriend(invitationId, inviterName)         => implemented
         * - RequestToJoin(requestId, friendName)                         => implemented
         * - InvitationDeclined(inviteeName)                              => implemented
         * - InvitationCancelled(inviterName)                             => implemented
         * - JoinFriendDeclined(friendName)                               => implemented
         * - JoinFriendCancelled(friendName, requestId = 0)               => implemented
         * - CannotInvite(inviteeName, msgId)                             => implemented
         * - CannotJoin(playerName, msgId)                                => implemented
         */

        #region Singleton

        private static SummonManager _instance;
        private static readonly object InstanceLock = new object();

        public static SummonManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new SummonManager();
                    }
                }

                return _instance;
            }
        }

        private SummonManager()
        {
        }

        #endregion

        /// <summary>
        /// Which prompt a summon puts up.
        ///
        /// InvitedToJoinFriend is the one meant for a player already on the target's friends list,
        /// and it cannot be answered in the 1.16.5.0 client: Recv_InvitedToJoinFriend stores the id
        /// and raises a pending indicator whose only action is ActivateFriendRequestDialog, which
        /// returns unless the manifestation has tmp_invitationName - a field nothing in the client
        /// ever assigns. The original devs left a TR_LOG_WARNING there saying exactly that. The
        /// target would get an indicator that opens nothing and no way to accept or decline.
        ///
        /// InvitedToAddAndJoinFriend builds its dialog from the arguments it is handed, so it works
        /// for everyone; its wording mentions adding to the friends list, which is true for a
        /// stranger and harmlessly redundant for a friend (accepting adds nobody twice). So it is
        /// used for both until a client fixes the other one - flip this then.
        /// </summary>
        public const bool UseWorkingPromptForFriends = true;

        private uint _nextId = 1;

        /// <summary>Open summons and join requests, both keyed by the id the client echoes back.</summary>
        private readonly Dictionary<uint, PendingSummon> _summons = new Dictionary<uint, PendingSummon>();
        private readonly Dictionary<uint, PendingSummon> _joinRequests = new Dictionary<uint, PendingSummon>();

        /// <summary>
        /// One pending move. <see cref="DestinationId"/> is whoever is standing still - the summoner
        /// in one direction, the player being travelled to in the other - and <see cref="TravellerId"/>
        /// is the one who moves. Positions are not captured here: they are read from the destination
        /// player when the answer arrives, so a summoner who walked on is still summoning to where
        /// they actually are.
        /// </summary>
        private sealed class PendingSummon
        {
            public uint Id;
            public uint DestinationId;
            public string DestinationName;
            public uint TravellerId;
            public string TravellerName;
        }

        #region Handlers

        /// <summary>"Summon them to me." The named player is asked; accepting moves them.</summary>
        internal void InviteFriendToJoin(Client client, InviteFriendToJoinPacket packet)
        {
            if (!InWorld(client))
                return;

            var name = packet.PlayerName ?? string.Empty;
            var target = FindIngame(name);

            if (!Eligible(client, target, name, summoning: true))
                return;

            if (_summons.Values.Any(s => s.DestinationId == client.AccountEntry.Id && s.TravellerId == target.AccountEntry.Id))
            {
                CannotInvite(client, target.Player.FamilyName, PlayerMessage.PmAlreadyInvitedFriend);
                return;
            }

            // The client holds one summon at a time (manifestation.py keeps a single
            // tmp_invitationId and auto-declines a second), so a target who already has one is
            // refused here rather than sent a prompt that answers itself.
            if (_summons.Values.Any(s => s.TravellerId == target.AccountEntry.Id))
            {
                CannotInvite(client, target.Player.FamilyName, PlayerMessage.PmCannotInviteNotAFriend);
                return;
            }

            Summon(client, target);
        }

        /// <summary>"Take me to them." The named player is asked; accepting moves the asker.</summary>
        internal void RequestInvitationToJoin(Client client, RequestInvitationToJoinPacket packet)
        {
            if (!InWorld(client))
                return;

            var name = packet.PlayerName ?? string.Empty;
            var target = FindIngame(name);

            if (!Eligible(client, target, name, summoning: false))
                return;

            if (_joinRequests.Values.Any(r => r.TravellerId == client.AccountEntry.Id && r.DestinationId == target.AccountEntry.Id))
            {
                CannotJoin(client, target.Player.FamilyName, PlayerMessage.PmJoinRequested);
                return;
            }

            var request = Record(_joinRequests, target, client);

            // "<name> wishes to join you at your current location", with Accept and Decline. The
            // dialog is named per request id, so several can be open at once - unlike a summon.
            target.CallMethod(target.Player.EntityId, new RequestToJoinPacket(request.Id, client.Player.FamilyName));
        }

        /// <summary>The answer to a summon, from a client that fixed InvitedToJoinFriend.</summary>
        internal void RespondToJoinFriend(Client client, RespondToJoinFriendPacket packet)
        {
            AnswerSummon(client, packet.InvitationId, packet.Accepted, befriend: false);
        }

        /// <summary>The answer to a summon as the shipped client sends it: accepting also befriends.</summary>
        internal void RespondToAddAndJoinFriend(Client client, RespondToAddAndJoinFriendPacket packet)
        {
            AnswerSummon(client, packet.InvitationId, packet.Accepted, befriend: true);
        }

        /// <summary>The answer to "take me to you".</summary>
        internal void RespondToRequestToJoin(Client client, RespondToRequestToJoinPacket packet)
        {
            if (!InWorld(client) || !_joinRequests.TryGetValue(packet.RequestId, out var request))
                return;

            // The id is the client's, but it is a number off the wire: only the player the request
            // was addressed to may answer it.
            if (request.DestinationId != client.AccountEntry.Id)
                return;

            _joinRequests.Remove(request.Id);

            var traveller = FindIngame(request.TravellerId);

            if (traveller == null)
                return;

            if (!packet.Accepted)
            {
                traveller.CallMethod(traveller.Player.EntityId, new JoinFriendDeclinedPacket(client.Player.FamilyName));
                return;
            }

            // Squads change while a dialog sits on screen.
            if (!SameSquad(client, traveller))
            {
                CannotJoin(traveller, client.Player.FamilyName, PlayerMessage.PmCannotJoinNotAFriend);
                return;
            }

            MoveTo(traveller, client);
        }

        #endregion

        #region World entry and exit

        /// <summary>
        /// Drops anything involving this player when they leave the world, and takes the dialogs
        /// off the other party's screen. Called from MapChannelManager.RemovePlayer.
        /// </summary>
        public void RemovePlayer(Client client)
        {
            if (client?.AccountEntry == null)
                return;

            Cancel(client.AccountEntry.Id);
        }

        /// <summary>
        /// Called by PartyManager whenever a squad changes. A summon is only ever between
        /// squadmates, so one that is no longer between squadmates is dropped rather than left to
        /// fail when it is answered.
        /// </summary>
        internal void PartyChanged(uint accountId)
        {
            foreach (var summon in _summons.Values.Where(s => Involves(s, accountId)).ToList())
                if (!StillSquadmates(summon))
                    CancelSummon(summon);

            foreach (var request in _joinRequests.Values.Where(r => Involves(r, accountId)).ToList())
                if (!StillSquadmates(request))
                    CancelJoinRequest(request);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Asks <paramref name="target"/> to be moved to <paramref name="summoner"/>. This is the
        /// entry point a summon stone calls - once per squad member - and the whole of what such an
        /// object needs from this manager. Returns false when the summon was refused, having told
        /// the summoner why.
        /// </summary>
        public bool Summon(Client summoner, Client target)
        {
            if (!InWorld(summoner) || !Eligible(summoner, target, target?.Player?.FamilyName ?? string.Empty, summoning: true))
                return false;

            var summon = Record(_summons, summoner, target);

            if (UseWorkingPromptForFriends || !target.Player.Friends.Contains(summoner.AccountEntry.Id))
                target.CallMethod(target.Player.EntityId, new InvitedToAddAndJoinFriendPacket(summon.Id, summoner.Player.FamilyName));
            else
                target.CallMethod(target.Player.EntityId, new InvitedToJoinFriendPacket(summon.Id, summoner.Player.FamilyName));

            return true;
        }

        #endregion

        #region Helper Functions

        /// <summary>
        /// Shared checks for both directions. Returns true when the summon may go ahead; otherwise
        /// tells the caller why, in the direction they asked.
        /// </summary>
        private bool Eligible(Client client, Client target, string name, bool summoning)
        {
            if (target == null || target == client)
            {
                Refuse(client, summoning, name, PlayerMessage.PmWhisperTargetNotInGame);
                return false;
            }

            if (!SameSquad(client, target))
            {
                // "...is unable to accept your invitation at this time" / "You can't join <name>,
                // they aren't a friend or are ignoring you" - the client's own refusals for these
                // two packets. Neither names squads, but they are what it has.
                Refuse(client, summoning, target.Player.FamilyName,
                    summoning ? PlayerMessage.PmCannotInviteNotAFriend : PlayerMessage.PmCannotJoinNotAFriend);
                return false;
            }

            return true;
        }

        private void Refuse(Client client, bool summoning, string name, PlayerMessage message)
        {
            if (summoning)
                CannotInvite(client, name, message);
            else
                CannotJoin(client, name, message);
        }

        private static void CannotInvite(Client client, string name, PlayerMessage message) =>
            client.CallMethod(client.Player.EntityId, new CannotInvitePacket(name, message));

        private static void CannotJoin(Client client, string name, PlayerMessage message) =>
            client.CallMethod(client.Player.EntityId, new CannotJoinPacket(name, message));

        private PendingSummon Record(Dictionary<uint, PendingSummon> into, Client destination, Client traveller)
        {
            var summon = new PendingSummon
            {
                Id = _nextId++,
                DestinationId = destination.AccountEntry.Id,
                DestinationName = destination.Player.FamilyName,
                TravellerId = traveller.AccountEntry.Id,
                TravellerName = traveller.Player.FamilyName
            };

            into[summon.Id] = summon;

            return summon;
        }

        private void AnswerSummon(Client client, uint invitationId, bool accepted, bool befriend)
        {
            if (!InWorld(client) || !_summons.TryGetValue(invitationId, out var summon))
                return;

            if (summon.TravellerId != client.AccountEntry.Id)
                return;

            _summons.Remove(summon.Id);

            var summoner = FindIngame(summon.DestinationId);

            if (summoner == null)
                return;

            if (!accepted)
            {
                summoner.CallMethod(summoner.Player.EntityId, new InvitationDeclinedPacket(client.Player.FamilyName));
                return;
            }

            if (!SameSquad(client, summoner))
            {
                CannotJoin(client, summoner.Player.FamilyName, PlayerMessage.PmCannotJoinNotAFriend);
                return;
            }

            // The move first: it is what the player accepted for, and a friend list that refuses
            // the row should not cost them the summon. Then the rest of what the prompt promised -
            // and only if they are not friends already, since AddFriend does not check.
            MoveTo(client, summoner);

            if (befriend && !client.Player.Friends.Contains(summoner.AccountEntry.Id))
                SocialManager.Instance.AddFriend(client, summoner.AccountEntry.Id);
        }

        /// <summary>
        /// Moves <paramref name="traveller"/> to where <paramref name="destination"/> is standing,
        /// by the same route as the .teleport command (MapChannelManager.ChangeMap). The position
        /// is read now rather than when the summon was raised, so the destination player having
        /// walked on does not matter.
        /// </summary>
        private static void MoveTo(Client traveller, Client destination)
        {
            MapChannelManager.Instance.ChangeMap(
                traveller,
                destination.Player.MapContextId,
                destination.Player.Position,
                destination.Movement.ViewDirection.X);
        }

        private void Cancel(uint accountId)
        {
            foreach (var summon in _summons.Values.Where(s => Involves(s, accountId)).ToList())
                CancelSummon(summon);

            foreach (var request in _joinRequests.Values.Where(r => Involves(r, accountId)).ToList())
                CancelJoinRequest(request);
        }

        /// <summary>The prompt is on the traveller's screen, so they are the one told it is gone.</summary>
        private void CancelSummon(PendingSummon summon)
        {
            _summons.Remove(summon.Id);

            FindIngame(summon.TravellerId)?.CallMethod(
                FindIngame(summon.TravellerId).Player.EntityId, new InvitationCancelledPacket(summon.DestinationName));
        }

        /// <summary>
        /// The prompt is on the destination player's screen. JoinFriendCancelled with a non-zero
        /// request id is the only thing that takes that dialog off the screen, so the id goes with it.
        /// </summary>
        private void CancelJoinRequest(PendingSummon request)
        {
            _joinRequests.Remove(request.Id);

            var destination = FindIngame(request.DestinationId);

            destination?.CallMethod(destination.Player.EntityId, new JoinFriendCancelledPacket(request.TravellerName, request.Id));
        }

        private static bool Involves(PendingSummon summon, uint accountId) =>
            summon.DestinationId == accountId || summon.TravellerId == accountId;

        private bool StillSquadmates(PendingSummon summon)
        {
            var destination = FindIngame(summon.DestinationId);
            var traveller = FindIngame(summon.TravellerId);

            return destination != null && traveller != null && SameSquad(destination, traveller);
        }

        /// <summary>
        /// Both in one squad. Summoning is a free move across the world, so it is kept to people
        /// who have already agreed to play together rather than to anyone who knows a name.
        /// </summary>
        private static bool SameSquad(Client a, Client b)
        {
            var party = PartyManager.Instance.PartyOf(a);

            return party != null && party == PartyManager.Instance.PartyOf(b);
        }

        private static bool InWorld(Client client) =>
            client?.Player != null && client.AccountEntry != null && client.State == ClientState.Ingame;

        private static Client FindIngame(uint accountId) =>
            Server.Clients.Find(c => c.State == ClientState.Ingame && c.Player != null
                                     && c.AccountEntry != null && c.AccountEntry.Id == accountId);

        private static Client FindIngame(string familyName) =>
            string.IsNullOrEmpty(familyName)
                ? null
                : Server.Clients.Find(c => c.State == ClientState.Ingame && c.Player != null && c.AccountEntry != null
                                           && string.Equals(c.Player.FamilyName, familyName, System.StringComparison.Ordinal))
                  ?? Server.Clients.Find(c => c.State == ClientState.Ingame && c.Player != null && c.AccountEntry != null
                                              && string.Equals(c.Player.FamilyName, familyName, System.StringComparison.OrdinalIgnoreCase));

        #endregion
    }
}
