using System.Net;

namespace Rasa.Repositories.AuthAccount
{
    using Structures;

    public interface IAuthAccountRepository
    {
        void Create(string email, string userName, string password);

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
    }
}