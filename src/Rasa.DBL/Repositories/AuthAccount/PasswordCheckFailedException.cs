using System;

namespace Rasa.Repositories.AuthAccount
{
    public class PasswordCheckFailedException : Exception
    {
        public PasswordCheckFailedException(string userName)
            : base($"The provided password für account {userName} was wrong.")
        {
        }
    }
}