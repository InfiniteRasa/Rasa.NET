namespace Rasa.Services.Passwords
{
    /// <summary>
    /// How account passwords are hashed: read on every hash, so a configuration reload applies
    /// to the next login. The auth server supplies it from appsettings.json (PasswordHashConfig).
    /// </summary>
    public interface IPasswordHashSettings
    {
        /// <summary>
        /// A secret mixed into every password hash and stored nowhere in the database, so a copy
        /// of the account table alone cannot be cracked. Empty for none. Changing it once
        /// accounts are hashed with it locks every one of them out: there is no way back from a
        /// hash to the password to rehash it under the new value.
        /// </summary>
        string Pepper { get; }

        /// <summary>PBKDF2 iterations for new and rehashed passwords.</summary>
        int Iterations { get; }
    }
}
