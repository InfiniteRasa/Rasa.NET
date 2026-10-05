namespace Rasa.Data
{
    /// <summary>What became of a request to create an account (CreateAccountResponsePacket).</summary>
    public enum CreateAccountResult : byte
    {
        /// <summary>The account exists now.</summary>
        Created = 0,

        /// <summary>An account has that user name already, in any case.</summary>
        UsernameTaken = 1,

        /// <summary>An account has that e-mail address already, in any case.</summary>
        EmailTaken = 2,

        /// <summary>The details are not ones an account can have (AccountRules).</summary>
        Invalid = 3,

        /// <summary>The database would not take it; the Auth server's log says why.</summary>
        Failed = 4
    }
}
