using System;
using System.Net;
using System.Threading.Tasks;

namespace Rasa.Auth
{
    using Repositories;
    using Repositories.Auth.Account;
    using Repositories.UnitOfWork;
    using Structures.Auth;

    /// <summary>
    /// One login's database work: the account looked up, its password verified - PBKDF2 at the
    /// configured iterations, 600,000 by default, which takes a good fraction of a second - and,
    /// when it is right, the login recorded. It runs off the main loop (<see cref="Start"/>):
    /// on it, under the Clients lock, each login held up every connection and every timer, and
    /// the k-th of N logins arriving together waited for k hashes (BR-220). The client applies
    /// the outcome on the main loop when it is done (Client.FinishLogin).
    ///
    /// It catches everything, so the task never faults: what went wrong comes back as
    /// <see cref="LoginOutcome.Error"/> with the message to log. Its address is settled with
    /// the LoginThrottle before the client sees the outcome.
    /// </summary>
    public sealed class LoginAttempt
    {
        public LoginOutcome Outcome { get; private init; }

        /// <summary>The account, for <see cref="LoginOutcome.Ok"/>.</summary>
        public AuthAccountEntry Account { get; private init; }

        /// <summary>What to log: the repository's own words for a refusal, the exception for an error.</summary>
        public string Message { get; private init; }

        /// <summary>
        /// Verifies on the thread pool, and settles the outcome with the throttle as it finishes
        /// (LoginThrottle.Settle): the failure counted and the address free for its next login,
        /// whether or not the connection that asked is still there to be answered.
        /// </summary>
        public static Task<LoginAttempt> Start(IAuthUnitOfWorkFactory factory, LoginThrottle throttle, string userName, string password, IPAddress address) =>
            Task.Run(() =>
            {
                var attempt = Verify(factory, userName, password, address);

                throttle.Settle(address, attempt.Outcome);

                return attempt;
            });

        public static LoginAttempt Verify(IAuthUnitOfWorkFactory factory, string userName, string password, IPAddress address)
        {
            try
            {
                using var unitOfWork = factory.Create();

                var account = unitOfWork.AuthAccountRepository.GetByUserName(userName, password);

                unitOfWork.AuthAccountRepository.UpdateLoginData(account.Id, address);
                unitOfWork.Complete();

                return new LoginAttempt { Outcome = LoginOutcome.Ok, Account = account };
            }
            catch (EntityNotFoundException)
            {
                return new LoginAttempt { Outcome = LoginOutcome.UnknownUser, Message = $"User ({userName}) tried to log in with an invalid username!" };
            }
            catch (PasswordCheckFailedException e)
            {
                return new LoginAttempt { Outcome = LoginOutcome.WrongPassword, Message = e.Message };
            }
            catch (AccountLockedException e)
            {
                return new LoginAttempt { Outcome = LoginOutcome.Locked, Message = e.Message };
            }
            catch (Exception e)
            {
                return new LoginAttempt { Outcome = LoginOutcome.Error, Message = $"Error logging in {userName}: {e}" };
            }
        }
    }

    public enum LoginOutcome
    {
        Ok,
        UnknownUser,
        WrongPassword,
        Locked,
        Error
    }
}
