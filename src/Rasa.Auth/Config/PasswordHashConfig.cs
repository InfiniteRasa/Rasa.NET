namespace Rasa.Config
{
    using Services.Passwords;

    /// <summary>
    /// appsettings.json's PasswordHashConfig: how account passwords are hashed (PasswordHasher).
    /// </summary>
    public class PasswordHashConfig : IPasswordHashSettings
    {
        /// <summary>
        /// A secret mixed into every password hash on top of each account's own salt, and kept
        /// out of the database: a stolen copy of the account table cannot be cracked without it.
        /// Use a long random value. Empty for none.
        ///
        /// Set it once. Accounts hashed without a pepper are given this one at their next login;
        /// but changing or removing it afterwards locks out every account hashed with it, since a
        /// hash cannot be turned back into the password to redo.
        /// </summary>
        public string Pepper { get; set; } = string.Empty;

        /// <summary>
        /// PBKDF2-HMAC-SHA256 iterations. 0 for the default (600,000); anything under 100,000 is
        /// raised to 100,000. Changing it is safe: each hash records its own count, and accounts
        /// are rehashed to the new one at their next login.
        /// </summary>
        public int Iterations { get; set; }
    }
}
