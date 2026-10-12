using System;
using System.Net;
using System.Threading;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Auth
{
    using Rasa.Auth;
    using Rasa.Repositories;
    using Rasa.Repositories.Auth;
    using Rasa.Repositories.Auth.Account;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures.Auth;

    /// <summary>
    /// A login's password hash runs off the auth server's main loop (LoginAttempt), where it held
    /// up every connection and every timer, one login after another (BR-220); and one address has
    /// one login verified at a time, settled with the failure count before the next begins
    /// (LoginThrottle), so verifying in parallel does not loosen the wrong-password block.
    /// </summary>
    [TestClass]
    public class LoginOffLoopTests
    {
        private static readonly IPAddress Address = IPAddress.Parse("10.0.0.7");

        [TestMethod]
        public void TheVerificationRunsOffTheCallersThreadAndSettlesTheAddressWhenDone()
        {
            var accounts = new Accounts();
            var throttle = new LoginThrottle();

            using (accounts.Gate)
            {
                Assert.IsTrue(throttle.TryBegin(Address));

                var login = LoginAttempt.Start(new Factory(accounts), throttle, "player", "right", Address);

                // The hash is held up; the caller is not.
                Assert.IsFalse(login.IsCompleted);
                Assert.IsTrue(throttle.InFlight(Address));
                Assert.IsFalse(throttle.TryBegin(Address), "one login an address at a time");

                accounts.Gate.Set();

                Assert.IsTrue(login.Wait(TimeSpan.FromSeconds(10)));
                Assert.AreEqual(LoginOutcome.Ok, login.Result.Outcome);
                Assert.AreEqual(42u, login.Result.Account.Id);
                Assert.IsFalse(throttle.InFlight(Address), "free for the next");
                Assert.AreEqual(1, accounts.LoginsRecorded);
                Assert.AreEqual(1, accounts.Completed);
            }
        }

        [TestMethod]
        public void EachOutcomeComesBackAsItIs()
        {
            var accounts = new Accounts();

            accounts.Gate.Set();

            Assert.AreEqual(LoginOutcome.Ok, LoginAttempt.Verify(new Factory(accounts), "player", "right", Address).Outcome);

            var unknown = LoginAttempt.Verify(new Factory(accounts), "nobody", "right", Address);
            Assert.AreEqual(LoginOutcome.UnknownUser, unknown.Outcome);
            StringAssert.Contains(unknown.Message, "nobody");

            Assert.AreEqual(LoginOutcome.WrongPassword, LoginAttempt.Verify(new Factory(accounts), "player", "wrong", Address).Outcome);
            Assert.AreEqual(LoginOutcome.Locked, LoginAttempt.Verify(new Factory(accounts), "locked", "right", Address).Outcome);

            var broken = LoginAttempt.Verify(new Factory(accounts), "broken", "right", Address);
            Assert.AreEqual(LoginOutcome.Error, broken.Outcome);
            StringAssert.Contains(broken.Message, "database gone");

            Assert.AreEqual(1, accounts.LoginsRecorded, "only the right one is recorded");
        }

        [TestMethod]
        public void WrongPasswordsSettledOffTheLoopStillBlockTheAddress()
        {
            var accounts = new Accounts();
            var throttle = new LoginThrottle();

            accounts.Gate.Set();

            for (var i = 0; i < LoginThrottle.MaxFailures; i++)
            {
                Assert.IsFalse(throttle.IsBlocked(Address), $"not yet, after {i}");
                Assert.IsTrue(throttle.TryBegin(Address));
                Assert.AreEqual(LoginOutcome.WrongPassword, LoginAttempt.Start(new Factory(accounts), throttle, "player", "wrong", Address).Result.Outcome);
            }

            Assert.IsTrue(throttle.IsBlocked(Address));
            Assert.IsFalse(throttle.InFlight(Address));

            // A locked account or a server fault is not the guesser's failure, and a right
            // password clears the count.
            var other = IPAddress.Parse("10.0.0.8");

            throttle.TryBegin(other);
            throttle.Settle(other, LoginOutcome.Locked);
            throttle.TryBegin(other);
            throttle.Settle(other, LoginOutcome.Error);
            for (var i = 0; i < LoginThrottle.MaxFailures - 1; i++)
                throttle.Settle(other, LoginOutcome.WrongPassword);
            throttle.Settle(other, LoginOutcome.Ok);
            throttle.Settle(other, LoginOutcome.WrongPassword);
            Assert.IsFalse(throttle.IsBlocked(other));
        }

        private sealed class Accounts : IAuthAccountRepository
        {
            public ManualResetEventSlim Gate { get; } = new ManualResetEventSlim(false);
            public int LoginsRecorded;
            public int Completed;

            public AuthAccountEntry GetByUserName(string name, string password)
            {
                Gate.Wait(TimeSpan.FromSeconds(10));

                var entry = new AuthAccountEntry { Id = 42, Username = name };

                return name switch
                {
                    "nobody" => throw new EntityNotFoundException(AuthAccountEntry.TableName, nameof(AuthAccountEntry.Username), name),
                    "broken" => throw new InvalidOperationException("database gone"),
                    "locked" => throw new AccountLockedException(entry),
                    _ when password != "right" => throw new PasswordCheckFailedException(entry),
                    _ => entry
                };
            }

            public void UpdateLoginData(uint id, IPAddress remoteAddress) => LoginsRecorded++;

            public void Create(string email, string userName, string password) => throw new NotSupportedException();
            public AuthAccountEntry FindByUserNameOrEmail(string userName, string email) => throw new NotSupportedException();
            public void UpdateLastServer(uint id, byte lastServerId) => throw new NotSupportedException();
            public AuthAccountEntry SetLocked(string userName, bool locked) => throw new NotSupportedException();
        }

        private sealed class Factory : IAuthUnitOfWorkFactory, IAuthUnitOfWork
        {
            private readonly Accounts _accounts;

            public Factory(Accounts accounts)
            {
                _accounts = accounts;
            }

            public IAuthUnitOfWork Create() => this;
            public IAuthAccountRepository AuthAccountRepository => _accounts;
            public void Complete() => _accounts.Completed++;
            public void Reject() { }
            public IDbContextTransaction BeginTransaction() => throw new NotSupportedException();
            public void Dispose() { }
        }
    }
}
