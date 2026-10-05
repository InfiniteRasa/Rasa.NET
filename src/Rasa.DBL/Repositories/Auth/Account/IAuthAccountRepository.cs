using System.Net;

namespace Rasa.Repositories.Auth.Account
{
    using Structures.Auth;

    public interface IAuthAccountRepository
    {
        void Create(string email, string userName, string password);

        /// <summary>
        /// The account that has this user name or this e-mail address, either compared without
        /// regard to case; the one with the user name when there are two. Null for none.
        /// </summary>
        AuthAccountEntry FindByUserNameOrEmail(string userName, string email);

        /// <summary>
        /// Searches an account by its username and verifies a password match.
        /// </summary>
        /// <param name="name">the username</param>
        /// <param name="password">the users password</param>
        /// <returns>the account entry</returns>
        /// <exception cref="EntityNotFoundException">No account with the specified username exists</exception>
        /// <exception cref="PasswordCheckFailedException">the provided password does not match the stored password</exception>
        /// <exception cref="AccountLockedException">the account of this user is locked</exception>
        AuthAccountEntry GetByUserName(string name, string password);

        void UpdateLoginData(uint id, IPAddress remoteAddress);

        void UpdateLastServer(uint id, byte lastServerId);

        /// <summary>
        /// Sets the locked flag on the account with this username - an exact match, otherwise the
        /// first case-insensitive one.
        /// </summary>
        /// <returns>the updated account, or null when no account has that username</returns>
        AuthAccountEntry SetLocked(string userName, bool locked);
    }
}