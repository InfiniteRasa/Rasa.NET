using System;

namespace Rasa.Repositories.AuthAccount
{
    using Structures;

    public class PasswordCheckFailedException : Exception
    {
        public PasswordCheckFailedException(AuthAccountEntry entry)
            : base($"User ({entry.Username}, {entry.Id}) tried to log in with an invalid password!")
        {
        }
    }
}