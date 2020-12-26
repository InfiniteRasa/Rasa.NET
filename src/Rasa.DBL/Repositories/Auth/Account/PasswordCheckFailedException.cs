namespace Rasa.Repositories.Auth.Account
{
    using System;
    using Structures;
    using Structures.Auth;

    public class PasswordCheckFailedException : Exception
    {
        public PasswordCheckFailedException(AuthAccountEntry entry)
            : base($"User ({entry.Username}, {entry.Id}) tried to log in with an invalid password!")
        {
        }
    }
}