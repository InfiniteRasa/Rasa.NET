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

            var friends = FriendTable.GetFriends(client.AccountEntry.Id);
            var friendAlready = false;

            foreach (var friendId in friends)
                if (friendId == account.Id)
                {
                    friendAlready = true;
                    break;
                }

            if (friendAlready)
            {
                CommunicatorManager.Instance.AddFriendAck(client, packet.FamilyName, false);
                return;
            }

            var friend = GetFriendById(account.Id);

            client.CallMethod(SysEntity.ClientSocialManagerId, new FriendAddedPacket(friend));
            FriendTable.AddFriend(client.AccountEntry.Id, account.Id);

            // check and remove player from ignoredPlayers
            var ignoredPlayer = GetIgnoredById(account.Id);

            if (ignoredPlayer != null)
            {
                client.CallMethod(SysEntity.ClientSocialManagerId, new IgnoreRemovedPacket(ignoredPlayer.UserId));
                IgnoredTable.RemoveIgnored(client.AccountEntry.Id, account.Id);
            }
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
            client.CallMethod(SysEntity.ClientSocialManagerId, new FriendRemovedPacket(packet.AccountId));
            FriendTable.RemoveFriend(client.AccountEntry.Id, packet.AccountId);
        }

        internal void RemoveIgnore(Client client, RemoveIgnorePacket packet)
        {
            client.CallMethod(SysEntity.ClientSocialManagerId, new IgnoreRemovedPacket(packet.AccountId));
            IgnoredTable.RemoveIgnored(client.AccountEntry.Id, packet.AccountId);
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

                if(friend != null)
                    frinedList.Add(friend);
            }

            foreach (var id in ignoredIds)
            {
                var ignored = GetIgnoredById(id);

                if (ignored != null)
                    ignoreList.Add(ignored);
            }

            client.CallMethod(SysEntity.ClientSocialManagerId, new SetSocialContactListPacket(frinedList, ignoreList));
        }

        #region Helper Functions
        internal Friend GetFriendById(uint accountId)
        {
            Friend friend = null;

            var listOfCharacters = CharacterTable.GetCharactersByAccountId(accountId);

            // account have no characters created
            if (listOfCharacters.Count == 0)
                return friend;

            var onlineCharacter = false;
            var familyName = GameAccountTable.GetAccountByCharacterId(accountId).FamilyName;

            foreach (var character in listOfCharacters)
            {
                if (character.IsOnline == true)
                {
                    onlineCharacter = true;
                    friend = new Friend(character, familyName);
                    break;
                }
            }

            if (onlineCharacter)
                return friend;

            // friend is offline, send first character in list
            friend = new Friend(listOfCharacters[0], familyName);

            return friend;

        }

        internal IgnoredPlayer GetIgnoredById(uint accountId)
        {
            IgnoredPlayer ignored = null;

            var listOfCharacters = CharacterTable.GetCharactersByAccountId(accountId);

            // account have no characters created
            if (listOfCharacters.Count == 0)
                return ignored;

            var onlineCharacter = false;
            var familyName = GameAccountTable.GetAccountByCharacterId(accountId).FamilyName;

            foreach (var character in listOfCharacters)
            {
                if (character.IsOnline == true)
                {
                    onlineCharacter = true;
                    ignored = new IgnoredPlayer(character, familyName);
                    break;
                }
            }

            if (onlineCharacter)
                return ignored;

            // ignoredPlayer is offline, send first character in list
            ignored = new IgnoredPlayer(listOfCharacters[0], familyName);

            return ignored;

        }
        
        internal void IgnoreById(Client client, GameAccountEntry account)
        {
            if (account == null || account.FamilyName == client.AccountEntry.FamilyName)
            {
                CommunicatorManager.Instance.AddIgnoreAck(client, account.FamilyName, false);
                return;
            }

            var ignoredPlayers = IgnoredTable.GetIgnored(client.AccountEntry.Id);
            var ignoredAlready = false;

            foreach (var ignoredId in ignoredPlayers)
                if (ignoredId == account.Id)
                {
                    ignoredAlready = true;
                    break;
                }

            if (ignoredAlready)
            {
                CommunicatorManager.Instance.AddIgnoreAck(client, account.FamilyName, false);
                return;
            }

            var ignored = GetIgnoredById(account.Id);

            client.CallMethod(SysEntity.ClientSocialManagerId, new IgnoreAddedPacket(ignored));
            IgnoredTable.AddIgnored(client.AccountEntry.Id, account.Id);

            // check and remove player from friends
            var friend = GetFriendById(account.Id);

            if (friend != null)
            {
                client.CallMethod(SysEntity.ClientSocialManagerId, new FriendRemovedPacket(friend.UserId));
                FriendTable.RemoveFriend(client.AccountEntry.Id, account.Id);
            }
        }
        #endregion
    }
}
