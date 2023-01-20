using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.UserOption
{
    using Context.Char;
    using Structures.Char;

    public class UserOptionRepository : IUserOptionRepository
    {
        private readonly CharContext _charContext;

        public UserOptionRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public void AddOrUpdate(uint accountId, uint optionId, string value)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.UserOptionEntries);
            var existingUserOption = query.Where(e => e.AccountId == accountId && e.OptionId == optionId).FirstOrDefault();

            if (existingUserOption != null)
            {
                existingUserOption.Value = value;
                _charContext.UserOptionEntries.Update(existingUserOption);
            }
            else
            {
                var newEntry = new UserOptionEntry
                {
                    AccountId = accountId,
                    OptionId = optionId,
                    Value = value
                };

                _charContext.UserOptionEntries.Add(newEntry);
            }
        }

        public List<UserOptionEntry> Get(uint accountId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.UserOptionEntries);
            var userOptionEntries = query.Where(e => e.AccountId == accountId).ToList();

            return userOptionEntries;
        }
    }
}
