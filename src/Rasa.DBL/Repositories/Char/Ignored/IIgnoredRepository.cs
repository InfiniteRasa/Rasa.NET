using System.Collections.Generic;

namespace Rasa.Repositories.Char.Ignored
{
    public interface IIgnoredRepository
    {
        void AddIgnored(uint accountId, uint ignoredAccountId);
        List<uint> GetIgnored(uint accountId);
        void RemoveIgnored(uint accountId, uint ignoredAccountId);
    }
}
