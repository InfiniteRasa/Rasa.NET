using System;

namespace Rasa.Repositories.AuthAccount
{
    using Structures;

    public class AccountLockedException : Exception
    {
        public AccountLockedException(AuthAccountEntry entry)
            : base($"User ({entry.Username}, {entry.Id}) tried to log in, but he/she is locked.")
        {
        }
    }
}