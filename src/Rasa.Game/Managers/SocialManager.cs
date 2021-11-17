using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.Character;
    using Game;
    using Packets.Social.Client;
    using Packets.Social.Server;
    using Structures;

    public class SocialManager
    {
        /*      Social Packets:
         * - AddFriend
         * - AddFriendByName
         * - FriendList
         * - FriendLoggedOff
         * - InviteFriendToJoin
         * - InvitedToAddAndJoinFriend
         * - InvitedToJoinFriend
         * - JoinFriendCancelled
         * - JoinFriendDeclined 
         * - RemoveFriend
         * - RemoveFriendByName
         * - RespondToAddAndJoinFriend
         * - RespondToJoinFriend
         * 
         *      Social Handlers:
         * - SetSocialContactList(friendList, ignoreList)
         * - IgnoreAdded(args)
         * - IgnoreRemoved(userId)
         * - FriendAdded(args)
         * - FriendRemoved(userId)
         * - FriendStatusUpdate(args)
         * - FriendLoggedOut(userId)
         * - FriendLoggedIn(args)
         */

        #region Singleton

        private static SocialManager _instance;
        private static readonly object InstanceLock = new object();
        public static SocialManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new SocialManager();
                    }
                }

                return _instance;
            }
        }

        private SocialManager()
        {
        }

        #endregion

        internal void AddFriendByName(Client client, AddFriendByNamePacket packet)
        {
            var account = GameAccountTable.GetAccountByFamilyName(packet.FamilyName);

            if (account == null || account.FamilyName == client.AccountEntry.FamilyName)
            {
                CommunicatorManager.Instance.AddFriendAck(client, packet.FamilyName, false);
                return;
            }

            foreach (var friendAccountId in client.MapClient.Player.Friends)
                if (friendAccountId == account.Id)
                {
                    CommunicatorManager.Instance.AddFriendAck(client, packet.FamilyName, false);
                    return;
                }

            AddFriend(client, account.Id);

            RemoveIgnoredPlayer(client, account.Id);
        }

        internal void AddIgnore(Client client, AddIgnorePacket packet)
        {
            var account = GameAccountTable.GetAccount(packet.AccountId);

            IgnoreById(client, account);
        }

        internal void AddIgnoreByName(Client client, AddIgnoreByNamePacket packet)
        {
            var account = GameAccountTable.GetAccountByFamilyName(packet.FamilyName);

            IgnoreById(client, account);
        }
        
        internal void RemoveFriend(Client client, RemoveFriendPacket packet)
        {
            RemoveFriend(client, packet.AccountId);
        }

        internal void RemoveIgnore(Client client, RemoveIgnorePacket packet)
        {
            RemoveIgnoredPlayer(client, packet.AccountId);
        }

        internal void SetSocialContactList(Client client)
        {
            var friendIds = FriendTable.GetFriends(client.AccountEntry.Id);
            var ignoredIds = IgnoredTable.GetIgnored(client.AccountEntry.Id);
            var frinedList = new List<Friend>();
            var ignoreList = new List<IgnoredPlayer>();


            foreach (var id in friendIds)
            {
                var friend = GetFriendById(id);

                if (friend != null)
                {
                    frinedList.Add(friend);
                    client.MapClient.Player.Friends.Add(id);
                }
            }

            foreach (var id in ignoredIds)
            {
                var ignored = GetIgnoredById(id);

                if (ignored != null)
                {
                    ignoreList.Add(ignored);
                    client.MapClient.Player.IgnoredPlayers.Add(id);
                }
            }

            client.CallMethod(SysEntity.ClientSocialManagerId, new SetSocialContactListPacket(frinedList, ignoreList));
        }

        #region Helper Functions

        internal void AddFriend(Client client, uint accountId)
        {
            var friend = GetFriendById(accountId);
            client.CallMethod(SysEntity.ClientSocialManagerId, new FriendAddedPacket(friend));
            client.MapClient.Player.Friends.Add(accountId);
            FriendTable.AddFriend(client.AccountEntry.Id, accountId);
        }
        
        internal void AddIgnoredPlayer(Client client, uint accountId)
        {
            var ignored = GetIgnoredById(accountId);
            client.CallMethod(SysEntity.ClientSocialManagerId, new IgnoreAddedPacket(ignored));
            client.MapClient.Player.IgnoredPlayers.Add(accountId);
            IgnoredTable.AddIgnored(client.AccountEntry.Id, accountId);
        }

        internal void FriendLoggedIn(Client client)
        {
            var notifyFriends = Server.Clients.FindAll(c => c.MapClient.Player.Friends.Contains(client.AccountEntry.Id));

            foreach (var notifyClient in notifyFriends)
            {
                var friend = new Friend(client);

                notifyClient.CallMethod(SysEntity.ClientSocialManagerId, new FriendLoggedInPacket(friend));
            }
        }

        internal void FriendLoggedOut(Client client)
        {
            var notifyFriends = Server.Clients.FindAll(c => c.MapClient.Player.Friends.Contains(client.AccountEntry.Id));

            foreach (var notifyClient in notifyFriends)
            {
                notifyClient.CallMethod(SysEntity.ClientSocialManagerId, new FriendLoggedOutPacket(client.AccountEntry.Id));
            }
        }

        internal void FriendStatusUpdate(Client client, Friend friend)
        {
            // ToDo
        }
        
        internal Friend GetFriendById(uint accountId)
        {
            var client = Server.Clients.Find(c => c.AccountEntry.Id == accountId && c.State == ClientState.Ingame);

            Friend friend = null;

            if (client != null)
            {
                friend = new Friend(client);
                return friend;
            }
            
            // friend is offline
            friend = new Friend
            {
                UserId = accountId,
                FamilyName = GameAccountTable.GetAccount(accountId).FamilyName,
                IsOnline = false
            };

            return friend;

        }

        internal IgnoredPlayer GetIgnoredById(uint accountId)
        {
            var client = Server.Clients.Find(c => c.AccountEntry.Id == accountId);
            IgnoredPlayer ignoredPlayer = null;

            if (client != null)
            {
                ignoredPlayer = new IgnoredPlayer(client);
                return ignoredPlayer;
            }

            // ignoredPlayer is offline
            ignoredPlayer = new IgnoredPlayer
            {
                UserId = accountId,
                FamilyName = GameAccountTable.GetAccount(accountId).FamilyName,
                IsOnline = false
            };

            return ignoredPlayer;
        }
        
        internal void IgnoreById(Client client, GameAccountEntry account)
        {
            if (account == null || account.FamilyName == client.AccountEntry.FamilyName)
            {
                CommunicatorManager.Instance.AddIgnoreAck(client, account.FamilyName, false);
                return;
            }
            
            foreach (var ignoredId in client.MapClient.Player.IgnoredPlayers)
                if (ignoredId == account.Id)
                {
                    CommunicatorManager.Instance.AddIgnoreAck(client, account.FamilyName, false);
                    return;
                }

            AddIgnoredPlayer(client, account.Id);

            RemoveFriend(client, account.Id);
        }

        internal void RemoveFriend(Client client, uint accountId)
        {
            var friend = client.MapClient.Player.Friends.Contains(accountId);

            if (friend)
            {
                client.CallMethod(SysEntity.ClientSocialManagerId, new FriendRemovedPacket(accountId));

                client.MapClient.Player.Friends.RemoveAll(remove => remove == accountId);

                FriendTable.RemoveFriend(client.AccountEntry.Id, accountId);
            }
        }

        internal void RemoveIgnoredPlayer(Client client, uint accountId)
        {
            var ignored = client.MapClient.Player.IgnoredPlayers.Contains(accountId);

            if (ignored)
            {
                client.CallMethod(SysEntity.ClientSocialManagerId, new IgnoreRemovedPacket(accountId));

                client.MapClient.Player.IgnoredPlayers.RemoveAll(remove => remove == accountId);

                IgnoredTable.RemoveIgnored(client.AccountEntry.Id, accountId);
            }
        }
        
        #endregion
    }
}
