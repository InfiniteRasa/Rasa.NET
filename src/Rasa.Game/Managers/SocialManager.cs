using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Social.Client;
    using Packets.Social.Server;
    using Rasa.Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;
    using System.Net.Sockets;

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
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
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
                            _instance = new SocialManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        private SocialManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
        }

        #endregion

        internal void AddFriendByName(Client client, AddFriendByNamePacket packet)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var account = unitOfWork.GameAccounts.Get(packet.FamilyName);

            if (account == null || account.FamilyName == client.AccountEntry.FamilyName)
            {
                CommunicatorManager.Instance.AddFriendAck(client, packet.FamilyName, false);
                return;
            }

            foreach (var friendAccountId in client.Player.Friends)
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
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var account = unitOfWork.GameAccounts.Get(packet.AccountId);

            IgnoreById(client, account);
        }

        internal void AddIgnoreByName(Client client, AddIgnoreByNamePacket packet)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var account = unitOfWork.GameAccounts.Get(packet.FamilyName);

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
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var friendIds = unitOfWork.Friends.GetFriends(client.AccountEntry.Id);
            var ignoredIds = unitOfWork.Ignoreds.GetIgnored(client.AccountEntry.Id);
            var frinedList = new List<Friend>();
            var ignoreList = new List<IgnoredPlayer>();


            foreach (var id in friendIds)
            {
                var friend = GetFriendById(id);

                if (friend != null)
                {
                    frinedList.Add(friend);
                    client.Player.Friends.Add(id);
                }
            }

            foreach (var id in ignoredIds)
            {
                var ignored = GetIgnoredById(id);

                if (ignored != null)
                {
                    ignoreList.Add(ignored);
                    client.Player.IgnoredPlayers.Add(id);
                }
            }

            client.CallMethod(SysEntity.ClientSocialManagerId, new SetSocialContactListPacket(frinedList, ignoreList));
        }

        #region Helper Functions

        internal void AddFriend(Client client, uint accountId)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var friend = GetFriendById(accountId);

            client.CallMethod(SysEntity.ClientSocialManagerId, new FriendAddedPacket(friend));
            client.Player.Friends.Add(accountId);
            unitOfWork.Friends.AddFriend(client.AccountEntry.Id, accountId);
        }
        
        internal void AddIgnoredPlayer(Client client, uint accountId)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var ignored = GetIgnoredById(accountId);

            client.CallMethod(SysEntity.ClientSocialManagerId, new IgnoreAddedPacket(ignored));
            client.Player.IgnoredPlayers.Add(accountId);
            unitOfWork.Ignoreds.AddIgnored(client.AccountEntry.Id, accountId);
        }

        internal void FriendLoggedIn(Client client)
        {
            var notifyFriends = Server.Clients.FindAll(c => c.Player.Friends.Contains(client.AccountEntry.Id));

            foreach (var notifyClient in notifyFriends)
            {
                var friend = new Friend(client);

                notifyClient.CallMethod(SysEntity.ClientSocialManagerId, new FriendLoggedInPacket(friend));
            }
        }

        internal void FriendLoggedOut(Client client)
        {
            var notifyFriends = Server.Clients.FindAll(c => c.Player.Friends.Contains(client.AccountEntry.Id));

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

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var friendEntry = unitOfWork.GameAccounts.Get(client.AccountEntry.Id);
            // friend is offline
            friend = new Friend
            {
                UserId = accountId,
                FamilyName = friendEntry.FamilyName,
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

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var ignoredEntry = unitOfWork.GameAccounts.Get(client.AccountEntry.Id);
            // ignoredPlayer is offline
            ignoredPlayer = new IgnoredPlayer
            {
                UserId = accountId,
                FamilyName = ignoredEntry.FamilyName,
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
            
            foreach (var ignoredId in client.Player.IgnoredPlayers)
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
            var friend = client.Player.Friends.Contains(accountId);

            if (friend)
            {
                client.CallMethod(SysEntity.ClientSocialManagerId, new FriendRemovedPacket(accountId));

                client.Player.Friends.RemoveAll(remove => remove == accountId);

                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
                unitOfWork.Friends.RemoveFriend(client.AccountEntry.Id, accountId);
            }
        }

        internal void RemoveIgnoredPlayer(Client client, uint accountId)
        {
            var ignored = client.Player.IgnoredPlayers.Contains(accountId);

            if (ignored)
            {
                client.CallMethod(SysEntity.ClientSocialManagerId, new IgnoreRemovedPacket(accountId));

                client.Player.IgnoredPlayers.RemoveAll(remove => remove == accountId);

                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
                unitOfWork.Ignoreds.RemoveIgnored(client.AccountEntry.Id, accountId);
            }
        }
        
        #endregion
    }
}
