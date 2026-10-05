using System;

namespace Rasa.Auth
{
    using Data;
    using Repositories.Auth.Account;

    /// <summary>
    /// Making an account for somebody who asked from outside the console: a game server, for
    /// its REST API (CreateAccountRequestPacket). The details are checked (AccountRules), a
    /// user name or an e-mail already in use is said to be so, and anything else the database
    /// refuses is a failure with its reason in the log. The password is never logged.
    /// </summary>
    public static class AccountCreation
    {
        /// <summary>
        /// Makes the account in the repository's database. <paramref name="complete"/> is the
        /// unit of work's Complete.
        /// </summary>
        public static CreateAccountResult Create(IAuthAccountRepository accounts, Action complete, string email, string userName, string password, out uint accountId)
        {
            accountId = 0;

            if (AccountRules.Problem(email, userName, password) != null)
                return CreateAccountResult.Invalid;

            try
            {
                var taken = TakenBy(accounts, email, userName);

                if (taken != null)
                    return taken.Value;

                accounts.Create(email, userName, password);
                complete?.Invoke();

                accountId = accounts.FindByUserNameOrEmail(userName, email)?.Id ?? 0;

                return CreateAccountResult.Created;
            }
            catch (Exception e)
            {
                // Two requests for one name at the same moment: the second insert is the
                // unique index's to refuse.
                try
                {
                    var taken = TakenBy(accounts, email, userName);

                    if (taken != null)
                        return taken.Value;
                }
                catch (Exception)
                {
                    // The failure below is the one worth the log.
                }

                Logger.WriteLog(LogType.Error, $"Could not create account {userName}: {e.GetBaseException().Message}");

                return CreateAccountResult.Failed;
            }
        }

        private static CreateAccountResult? TakenBy(IAuthAccountRepository accounts, string email, string userName)
        {
            var existing = accounts.FindByUserNameOrEmail(userName, email);

            if (existing == null)
                return null;

            return string.Equals(existing.Username, userName, StringComparison.OrdinalIgnoreCase)
                ? CreateAccountResult.UsernameTaken
                : CreateAccountResult.EmailTaken;
        }
    }
}
