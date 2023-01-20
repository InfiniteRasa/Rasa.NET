using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Communicator.Server;
    using Packets.Party.Client;
    using Packets.Party.Server;
    using Rasa.Packets.Party.Both;
    using Structures;

    public class PartyManager
    {
        /*   Party Packets:
         * - InviteUserToPartyByName', (targetName,))
         * - SendJoinRequestToPartyByName', (targetName,))
         * - InviteSquad', (targetName,))
         * - SendJoinRequestToSquadLeader', (targetName,))
         * - CancelSquadInviteRequest', (targetName,))
         * - CancelSquadJoinRequest', (targetName,))
         * - PartyInvitationResponse', (accepted,))
         * - PartyJoinRequestResponse', (accepted, senderUserId))
         * - LeaveParty', ())
         * - DisbandParty', ())
         * - KickUserFromParty', (name,))
         * - KickUserFromPartyById', (id,))
         * - MakeUserPartyLeader', (name,))
         * - MakeUserPartyLeaderById', (id,))
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
         * - DisplayPartyMessage(msgId, args = { })
         * - PartyMemberRoll(itemClassId, winnerUserId, rolls, isGreedRoll)
         * - PartyMemberLoot(userId, creatureEntityId, lootClassIds, moneyAmount)
         * - VoiceChatAvailable(isAvail)
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

        internal void ChangePartyLootMethod(Client client, ChangePartyLootMethodPacket packet)
        {
            var party = Parties[client.Player.PartyId];
            var clients = Server.Clients.FindAll(c => c.Player.PartyId == client.Player.PartyId);

            party.LootMethod = packet.PartyLootMethod;

            foreach (var partyMember in clients)
                partyMember.CallMethod(SysEntity.ClientPartyManagerId, new ChangePartyLootMethodPacket(packet.PartyLootMethod));
        }

        internal void ChangePartyLootThreshold(Client client, ChangePartyLootThresholdPacket packet)
        {
            var clients = Server.Clients.FindAll(c => c.Player.PartyId == client.Player.PartyId);
            var party = Parties[client.Player.PartyId];

            party.LootThreshold = packet.PartyLootThreshold;

            foreach (var partyMember in clients)
                partyMember.CallMethod(SysEntity.ClientPartyManagerId, new ChangePartyLootThresholdPacket(packet.PartyLootThreshold));
        }

        internal void DisbandParty(Client client)
        {
            var partyMembers = Server.Clients.FindAll(c => c.Player.PartyId == client.Player.PartyId);

            foreach (var partyMember in partyMembers)
            {
                partyMember.Player.PartyId = 0;
                partyMember.CallMethod(SysEntity.ClientPartyManagerId, new SetCurrentPartyIdPacket(0));
            }

            Parties.Remove(client.Player.PartyId);
        }

        internal void InviteUserToPartyByName(Client client, InviteUserToPartyByNamePacket packet)
        {
            var inviteeClient = Server.Clients.Find(c => c.AccountEntry.FamilyName == packet.FamilyName && c.State == ClientState.Ingame);

            List<PartyMember> SenderSquadInfo = new List<PartyMember>();

            if (client.Player.PartyId != 0)
                SenderSquadInfo = Parties[client.Player.PartyId].Members;
            else
                SenderSquadInfo.Add(new PartyMember(client));

            inviteeClient.CallMethod(SysEntity.ClientPartyManagerId, new InviteToPartyPacket(client.Player.FamilyName, SenderSquadInfo));
            inviteeClient.Player.PartyInviterId = client.Player.EntityId;

            client.CallMethod(SysEntity.ClientPartyManagerId, new InvitedPlayerToPartyPacket(packet.FamilyName, inviteeClient.Player.IsAFK));
        }

        internal void CancelSquadJoinRequest(Client client, CancelSquadJoinRequestPacket packet)
        {
            Logger.WriteLog(LogType.AI, $"CancelSquadJoinRequest ToDo");
        }

        internal void CancelSquadInviteRequest(Client client, CancelSquadInviteRequestPacket packet)
        {
            var inviteeClient = Server.Clients.Find(c => c.AccountEntry.FamilyName == packet.FamilyName && c.State == ClientState.Ingame);

            inviteeClient.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestCanceledPacket(client.Player.FamilyName));
        }

        internal void PartyInvitationResponse(Client client, PartyInvitationResponsePacket packet)
        {
            var receiverClient = Server.Clients.Find(c => c.Player.EntityId == client.Player.PartyInviterId && c.State == ClientState.Ingame);

            client.Player.PartyInviterId = 0;

            if (packet.Response)
            {
                List<PartyMember> SenderSquadInfo = new List<PartyMember>();

                foreach (var onlineMember in Server.Clients)
                    SenderSquadInfo.Add(new PartyMember(onlineMember));

                receiverClient.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestSuccessPacket(client.Player.FamilyName));

                if (receiverClient.Player.PartyId == 0)
                    CreateParty(receiverClient, client);
                else
                    AddNewPartyMember(receiverClient, client);
            }
            else
                receiverClient.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestDeclinedPacket(client.Player.FamilyName));
        }

        internal void InviteSquad(Client client, InviteSquadPacket packet)
        {

        }

        internal void KickUserFromParty(Client client, KickUserFromPartyPacket packet)
        {

        }

        internal void KickUserFromPartyById(Client client, KickUserFromPartyByIdPacket packet)
        {
            var partyMembers = Server.Clients.FindAll(c => c.Player.PartyId == client.Player.PartyId);
            var kickedMember = partyMembers.Find(c => c.Player.EntityId == packet.MemberId);

            foreach (var partyMember in partyMembers)
            {
                if (partyMember == kickedMember)
                    continue;

                partyMember.CallMethod(SysEntity.ClientPartyManagerId, new RemovePartyMemberPacket(packet.MemberId, true));
            }

            kickedMember.Player.PartyId = 0;
            kickedMember.CallMethod(SysEntity.ClientPartyManagerId, new SetCurrentPartyIdPacket(0, true));

            var party = Parties[client.Player.PartyId];
            var memberToRemove = party.Members.Find(m => m.MemberId == packet.MemberId);

            party.Members.Remove(memberToRemove);

            if (party.Members.Count == 1)
            {
                client.Player.PartyId = 0;

                Parties.Remove(client.Player.PartyId);

                client.CallMethod(SysEntity.ClientPartyManagerId, new SetCurrentPartyIdPacket(0));
            }
        }

        internal void LeaveParty(Client client)
        {
            var party = Parties[client.Player.PartyId];
            var memberToRemove = party.Members.Find(m => m.MemberId == client.Player.EntityId);
            var partyMembers = Server.Clients.FindAll(c => c.Player.PartyId == party.Id);

            foreach(var partyMember in partyMembers)
            {
                if (partyMember == client)
                    continue;

                partyMember.CallMethod(SysEntity.ClientPartyManagerId, new RemovePartyMemberPacket(client.Player.EntityId));
            }

            party.Members.Remove(memberToRemove);

            if (party.Members.Count == 1)
            {
                var lastPartyMember = Server.Clients.Find(c => c.Player.EntityId == party.Members[0].MemberId);

                lastPartyMember.CallMethod(SysEntity.ClientPartyManagerId, new SetCurrentPartyIdPacket(0));
                lastPartyMember.Player.PartyId = 0;

                Parties.Remove(client.Player.PartyId);
            }

            client.CallMethod(SysEntity.ClientPartyManagerId, new SetCurrentPartyIdPacket(0));
            client.Player.PartyId = 0;
        }

        #region Helper Functions
        public void CreateParty(Client leader, Client member)
        {
            var partyId = GetPartyId;
            var partyMembers = new List<PartyMember>();
            partyMembers.Add(new PartyMember(leader));
            partyMembers.Add(new PartyMember(member));

            var party = new Party(partyId, leader.AccountEntry.Id, partyMembers);

            Parties.Add(partyId, party);

            leader.Player.PartyId = partyId;
            member.Player.PartyId = partyId;

            leader.CallMethod(SysEntity.ClientPartyManagerId, new SetCurrentPartyIdPacket(partyId));
            leader.CallMethod(SysEntity.ClientPartyManagerId, new SetPartyLeaderPacket(party.PartyLeaderId));
            leader.CallMethod(SysEntity.ClientPartyManagerId, new PartyMemberListPacket(partyMembers));

            member.CallMethod(SysEntity.ClientPartyManagerId, new SetCurrentPartyIdPacket(partyId));
            member.CallMethod(SysEntity.ClientPartyManagerId, new PartyMemberListPacket(partyMembers));
        }

        public void AddNewPartyMember(Client leader, Client newMember)
        {
            var partyId = leader.Player.PartyId;
            var partyMembers = Server.Clients.FindAll(c => c.Player.PartyId == partyId);
            var newPartyMember = new PartyMember(newMember);

            foreach (var partyMember in partyMembers)
                partyMember.CallMethod(SysEntity.ClientPartyManagerId, new AddPartyMemberPacket(newPartyMember));

            newMember.Player.PartyId = partyId;

            Parties[partyId].Members.Add(newPartyMember);

            newMember.CallMethod(SysEntity.ClientPartyManagerId, new SetCurrentPartyIdPacket(partyId));
            newMember.CallMethod(SysEntity.ClientPartyManagerId, new PartyMemberListPacket(Parties[partyId].Members));
        }

        #endregion
    }
}
