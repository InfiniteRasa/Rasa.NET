using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Rasa.Auth
{
    /// <summary>
    /// Failed logins per address, and the logins being verified per address.
    ///
    /// Every attempt is a database read and a password hash, and nothing slowed a guesser down:
    /// one connection per guess, as fast as they could open them. After MaxFailures within
    /// FailureWindow the address is refused without a look at the database for Block. Kept per
    /// address rather than per account, so a stranger cannot lock someone else out by getting
    /// their password wrong.
    ///
    /// The hash runs off the main loop (LoginAttempt), so this is reached from the main loop and
    /// from the thread pool, under one lock. One address has one login verified at a time
    /// (<see cref="TryBegin"/>): the next waits until the last is settled, its failure counted,
    /// so a guesser opening connections in parallel is held to the same count as one opening
    /// them in turn, and cannot keep more than one hash running.
    /// </summary>
    public sealed class LoginThrottle
    {
        public const int MaxFailures = 5;
        public static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan Block = TimeSpan.FromMinutes(5);

        private readonly object _lock = new();
        private readonly Dictionary<IPAddress, (int Count, DateTime WindowStart, DateTime BlockedUntil)> _failures = new();
        private readonly HashSet<IPAddress> _inFlight = new();

        public bool IsBlocked(IPAddress address)
        {
            lock (_lock)
                return _failures.TryGetValue(address, out var record) && record.BlockedUntil > DateTime.UtcNow;
        }

        /// <summary>Whether a login from this address may be verified now; false while another from it is.</summary>
        public bool TryBegin(IPAddress address)
        {
            lock (_lock)
                return _inFlight.Add(address);
        }

        public bool InFlight(IPAddress address)
        {
            lock (_lock)
                return _inFlight.Contains(address);
        }

        /// <summary>
        /// A verified login's outcome counted - a wrong name or password a failure, a right one
        /// clearing the address's record - and the address free for its next login. Called by the
        /// verification itself as it finishes, whether or not its connection is still there.
        /// </summary>
        public void Settle(IPAddress address, LoginOutcome outcome)
        {
            lock (_lock)
            {
                try
                {
                    switch (outcome)
                    {
                        case LoginOutcome.UnknownUser:
                        case LoginOutcome.WrongPassword:
                            RecordFailure(address);
                            break;

                        case LoginOutcome.Ok:
                            _failures.Remove(address);
                            break;
                    }
                }
                finally
                {
                    // Whatever the count did, the address is not left waiting for good.
                    _inFlight.Remove(address);
                }
            }
        }

        public void RecordFailure(IPAddress address)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;

                // Forget addresses whose window and block are both over, so the table stays the size
                // of the current trouble rather than of every address that ever mistyped.
                if (_failures.Count > 1024)
                    foreach (var stale in _failures.Where(f => f.Value.BlockedUntil < now && now - f.Value.WindowStart > FailureWindow).Select(f => f.Key).ToList())
                        _failures.Remove(stale);

                var (count, windowStart, blockedUntil) = _failures.TryGetValue(address, out var record)
                    ? record
                    : (0, now, DateTime.MinValue);

                if (now - windowStart > FailureWindow)
                    (count, windowStart) = (0, now);

                count++;

                if (count >= MaxFailures && blockedUntil < now)
                {
                    blockedUntil = now + Block;
                    (count, windowStart) = (0, now);

                    Logger.WriteLog(LogType.Security, $"{MaxFailures} failed logins from {address} within {FailureWindow.TotalMinutes:F0} min; refusing its logins for {Block.TotalMinutes:F0} min.");
                }

                _failures[address] = (count, windowStart, blockedUntil);
            }
        }

        public void RecordSuccess(IPAddress address)
        {
            lock (_lock)
                _failures.Remove(address);
        }
    }
}
