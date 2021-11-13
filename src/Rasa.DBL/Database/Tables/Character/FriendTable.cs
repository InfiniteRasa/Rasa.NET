using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    public class FriendTable
    {
        private static readonly MySqlCommand AddFriendCommand = new MySqlCommand("INSERT INTO friends (account_Id, friend_account_id) VALUES (@AccountId, @FriendAccountId)");
        private static readonly MySqlCommand RemoveFriendCommand = new MySqlCommand("DELETE FROM friends WHERE account_id = @AccountId AND friend_account_id = @FriendAccountId");
        private static readonly MySqlCommand GetFriendsCommand = new MySqlCommand("SELECT friend_account_id FROM friends WHERE account_id = @AccountId");

        public static void Initialize()
        {
            AddFriendCommand.Connection = GameDatabaseAccess.CharConnection;
            AddFriendCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            AddFriendCommand.Parameters.Add("@FriendAccountId", MySqlDbType.UInt32);
            AddFriendCommand.Prepare();

            GetFriendsCommand.Connection = GameDatabaseAccess.CharConnection;
            GetFriendsCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            GetFriendsCommand.Prepare();

            RemoveFriendCommand.Connection = GameDatabaseAccess.CharConnection;
            RemoveFriendCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            RemoveFriendCommand.Parameters.Add("@FriendAccountId", MySqlDbType.UInt32);
            RemoveFriendCommand.Prepare();
        }

        public static void AddFriend(uint accountId, uint friendAccountId)
        {

            lock (GameDatabaseAccess.CharLock)
            {
                AddFriendCommand.Parameters["@AccountId"].Value = accountId;
                AddFriendCommand.Parameters["@FriendAccountId"].Value = friendAccountId;
                AddFriendCommand.ExecuteNonQuery();
            }
        }

        public static List<uint> GetFriends(uint accountId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var friendAccountIds = new List<uint>();

                GetFriendsCommand.Parameters["@AccountId"].Value = accountId;
                
                using (var reader = GetFriendsCommand.ExecuteReader())
                    while (reader.Read())
                        friendAccountIds.Add(reader.GetUInt32("friend_account_id"));

                return friendAccountIds;
            }
        }

        public static void RemoveFriend(uint accountId, uint friendAccountId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                RemoveFriendCommand.Parameters["@AccountId"].Value = accountId;
                RemoveFriendCommand.Parameters["@FriendAccountId"].Value = friendAccountId;
                RemoveFriendCommand.ExecuteNonQuery();
            }
        }
    }
}
