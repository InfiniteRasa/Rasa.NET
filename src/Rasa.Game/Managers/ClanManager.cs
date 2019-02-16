namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Client;
    using Database.Tables.Character;
    using Packets.MapChannel.Server;
    using System.Collections.Generic;
    using Rasa.Structures;
    using System;
    using System.Linq;

    public class ClanManager
    {
        /*   Clan Packets:
         * - DisplayClanMessage
         * - InviteToClan
         * - SetClanData
         * - SetClanMemberData
         * - ClanMembersRosterBegin
         * - ClanMembersRosterEnd
         *  - PlayerJoinedClan
         *  - PlayerLeftClan
         *  - ClanDisbanded
         *  - ClanDeleted
         *  - DisplayClanLeaderInfo
         *  - DisplayClanMemberInfoHeader
         *  - DisplayClanMemberInfo
         *  - ClanCreated
         *  - GetPvPClanStatus
         *  
         *    Clan Handlers:
         *  - GetPvPClanMembershipStatus
         *  - CreateClan
         */

        private static ClanManager _instance;
        private static readonly object InstanceLock = new object();
        public static ClanManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new ClanManager();
                    }
                }

                return _instance;
            }
        }

        private ClanManager()
        {
        }

        #region Handlers
        internal void GetPvPClanMembershipStatus(Client client)
        {
            // ToDo
        }

        internal void CreateClan(Client client, CreateClanPacket packet)
        {
            if (client == null)
                throw new ArgumentNullException("client");

            if (packet == null)
                throw new ArgumentNullException("packet");

            string clanName = packet.ClanName;
            bool isPvP = packet.IsPvP;

            if (string.IsNullOrWhiteSpace(clanName))
            {
                client.CallMethod(SysEntity.ClientClanManagerId, new DisplayClanMessagePacket((int)PlayerMessage.PmClanNameRequired, new Dictionary<string, string>()));
                return;
            }

            if(clanName.Length > ClanTable.MaxClanNameLength)
            {
                client.CallMethod(SysEntity.ClientClanManagerId, new DisplayClanMessagePacket((int)PlayerMessage.PmClanNameTooLong, new Dictionary<string, string>()));
                return;
            }

            // TODO: Verify content of clan name: PmInappropriateClanName

            if (ClanTable.ClanNameAlreadyExists(clanName))
            {
                client.CallMethod(SysEntity.ClientClanManagerId, new DisplayClanMessagePacket((int)PlayerMessage.PmClanNameNotAvailable, new Dictionary<string,string>()));
                return;
            }

            if (isPvP)
            {
                // TODO: Verify the creator is not on PvP timeout: PmClanCannotCreateUserInPvpTimeout
            }

            ClanEntry clan = ClanTable.CreateClan(clanName, isPvP);
            
            if(clan != null)
            {
                Manifestation player = client.MapClient.Player;
                var clanData = new ClanData(clan);

                client.CallMethod(SysEntity.ClientClanManagerId, new ClanCreatedPacket(clanData.Id));

                // This character created the clan, make them the leader
                uint clanLeaderRank = ClanTable.ClankRankLeader;              
                ClanMemberData clanMemberData = CreateClanMemberData(clanData, player.CharacterId, clanLeaderRank);

                AddMemberToClan(clanMemberData);
                SetClanData(client, clanData);
                SetClanMemberData(client, clanData);
            }
        }

        internal void ClanChangeRankTitle(Client client, ClanChangeRankTitlePacket packet)
        {
            if (client == null)
                throw new ArgumentNullException("client");

            if (packet == null)
                throw new ArgumentNullException("packet");

            uint characterId = client.MapClient.Player.CharacterId;
            ClanEntry clan = ClanTable.GetClanData(characterId);
            List<ClanMemberEntry> allMembers = ClanTable.GetAllClanMembersForClanId(clan.Id);
            ClanMemberEntry clanLeader = allMembers.FirstOrDefault(x => x.Rank == ClanTable.ClankRankLeader);

            if (clanLeader != null && clanLeader.CharacterId == characterId)
            {
                if(ClanTable.UpdateRankTitleForClanId(clan.Id, packet.Rank, packet.Title))
                {
                    clan = ClanTable.GetClanData(characterId);
                    SetClanData(client, new ClanData(clan));
                }
            }
            else
            {
                Logger.WriteLog(LogType.Error, $"ClanManager: Character ID {characterId} attempted to change rank title but is not the leader.");
            }
        }

        internal void InitializePlayerClanData(Client client)
        {
            if (client == null)
                throw new ArgumentNullException("client");

            Manifestation player = client.MapClient.Player;

            ClanEntry clan = ClanTable.GetClanData(player.CharacterId);
            
            if(clan != null)
            {
                var clanData = new ClanData(clan);
                SetClanData(client, clanData);
                SetClanMemberData(client, clanData);
            }
        }

        internal void InviteMemberToClan()
        {

        }

        #endregion

        #region Helper Functions

        private ClanMemberData CreateClanMemberData(ClanData clanData, uint characterId, uint rank = 0, string note = "")
        {
            CharacterEntry character = CharacterTable.GetCharacterById(characterId);
            GameAccountEntry account = GameAccountTable.GetAccount(character.AccountId);

            return new ClanMemberData
            {               
                CharacterId = character.Id,
                ContextId = character.MapContextId,
                Level = character.Level,
                CharacterName = character.Name,
                FamilyName = account.FamilyName,
                UserId = account.Id,
                ClanId = clanData.Id,
                Rank = rank,
                Note = note,
            };
        }

        private void SetClanMemberData(Client client, ClanData clanData)
        {
            Manifestation player = client.MapClient.Player;

            client.CallMethod(SysEntity.ClientClanManagerId, new ClanMembersRosterBeginPacket(clanData.Id));

            List<ClanMemberEntry> clanMemberEntries = ClanTable.GetAllClanMembersForClanId(clanData.Id);

            foreach (ClanMemberEntry member in clanMemberEntries)
            {
                ClanMemberData clanMemberData = CreateClanMemberData(clanData, member.CharacterId, member.Rank, member.Note);
                clanMemberData.IsOnline = CommunicatorManager.PlayersByCharacterId.ContainsKey(member.CharacterId);
                
                if (player.CharacterId == member.CharacterId)
                {
                    // The game is expecting the characterId to be the manifestationId for the current player                    
                    clanMemberData.CharacterId = player.Actor.EntityId;
                }

                client.CallMethod(SysEntity.ClientClanManagerId, new SetClanMemberDataPacket(SetClanMemberDataPacket.NameKey, clanMemberData));
            }

            client.CallMethod(SysEntity.ClientClanManagerId, new ClanMembersRosterEndPacket(clanData.Id));
        }

        private void SetClanData(Client client, ClanData clanData)
        {
            Manifestation player = client.MapClient.Player;            
            client.CallMethod(SysEntity.ClientClanManagerId, new SetClanDataPacket(SetClanDataPacket.NameKey, clanData));
            client.CallMethod(player.Actor.EntityId, new ClanIdPacket(clanData.Id));
        }

        private void AddMemberToClan(ClanMemberData clanMember)
        {
            ClanTable.InsertClanMemberData(clanMember.ClanId, clanMember.CharacterId, clanMember.Rank, clanMember.Note);
        }

        #endregion
    }
}
