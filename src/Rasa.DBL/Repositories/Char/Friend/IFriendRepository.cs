using System.Collections.Generic;

namespace Rasa.Repositories.Char.Friend
{
    public interface IFriendRepository
    {
        void AddFriend(uint accountId, uint friendAccountId);
        List<uint> GetFriends(uint accountId);
        void RemoveFriend(uint accountId, uint friendAccountId);
    }
}
