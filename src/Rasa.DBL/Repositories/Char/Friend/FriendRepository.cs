using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.Friend
{
    using Context.Char;
    using Structures.Char;

    public class FriendRepository : IFriendRepository
    {
        private readonly CharContext _charContext;

        public FriendRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public void AddFriend(uint accountId, uint friendAccountId)
        {
            var entry = new FriendEntry(accountId, friendAccountId);

            try
            {
                _charContext.FriendEntries.Add(entry);
                _charContext.SaveChanges();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Error adding friend:");
                Logger.WriteLog(LogType.Error, e);
            }
        }

        public List<uint> GetFriends(uint accountId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.FriendEntries);
            var entries = query.Where(e => e.AccountId == accountId).Select(e => e.FriendAccountId).ToList();

            return entries;
        }

        public void RemoveFriend(uint accountId, uint friendAccountId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.FriendEntries);
            var entry = query.Where(e => e.AccountId == accountId && e.FriendAccountId == friendAccountId).FirstOrDefault();

            _charContext.Remove(entry);
            _charContext.SaveChanges();
        }
    }
}
