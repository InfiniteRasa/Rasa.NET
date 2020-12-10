using System;

namespace Rasa.Repositories.AuthAccount
{
    public class AccountLockedException : Exception
    {
        public AccountLockedException(string userName)
            : base($"The account of user {userName} is locked.")
        {
        }
    }
}