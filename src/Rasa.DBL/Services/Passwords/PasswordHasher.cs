using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Rasa.Services.Passwords
{
    /// <summary>
    /// Account password hashes: PBKDF2-HMAC-SHA256 over the password, salted with the account's
    /// own random salt (the salt column), and peppered with a secret from the configuration
    /// when one is set.
    ///
    /// Stored as <c>pbkdf2-sha256$&lt;iterations&gt;$&lt;p or -&gt;$&lt;64 hex&gt;</c>: the iteration
    /// count and whether a pepper went in are kept with each hash, so raising the count or
    /// adding a pepper later still verifies every older hash, and the account is rehashed to the
    /// current settings at its next login (<see cref="NeedsRehash"/>).
    ///
    /// The format before this - 64 hex digits, a single SHA-256 of "salt:password" - is still
    /// verified, and replaced at the account's next login.
    /// </summary>
    public static class PasswordHasher
    {
        public const string Scheme = "pbkdf2-sha256";

        /// <summary>OWASP's figure for PBKDF2-HMAC-SHA256 (2023), used when none is configured.</summary>
        public const int DefaultIterations = 600_000;

        /// <summary>Fewer than this is a misconfiguration, not a choice; it is raised to it.</summary>
        public const int MinimumIterations = 100_000;

        private const int HashBytes = 32;

        public static int IterationsOf(IPasswordHashSettings settings)
        {
            var iterations = settings?.Iterations ?? 0;

            if (iterations <= 0)
                return DefaultIterations;

            return Math.Max(iterations, MinimumIterations);
        }

        private static string PepperOf(IPasswordHashSettings settings) => settings?.Pepper ?? string.Empty;

        /// <summary>A hash of the password in the current format, under the current settings.</summary>
        public static string Create(string password, string salt, IPasswordHashSettings settings)
        {
            var iterations = IterationsOf(settings);
            var pepper = PepperOf(settings);

            return Format(iterations, pepper.Length > 0, Derive(password, salt, iterations, pepper));
        }

        /// <summary>
        /// Whether the password matches the stored hash, in either format. False for a hash made
        /// with a pepper when none is configured now - and, unavoidably, for one made with a
        /// different pepper than the one configured now.
        /// </summary>
        public static bool Verify(string password, string salt, string stored, IPasswordHashSettings settings)
        {
            if (string.IsNullOrEmpty(stored))
                return false;

            byte[] expected;
            byte[] actual;

            if (TryParse(stored, out var iterations, out var peppered, out var hash))
            {
                var pepper = PepperOf(settings);

                if (peppered && pepper.Length == 0)
                    return false;

                expected = hash;
                actual = Derive(password, salt, iterations, peppered ? pepper : string.Empty);
            }
            else
            {
                expected = Encoding.ASCII.GetBytes(stored);
                actual = Encoding.ASCII.GetBytes(LegacyHash(password, salt));
            }

            // In time that does not depend on how much of the hash matched.
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }

        /// <summary>
        /// Whether a hash that has just verified should be replaced with one under the current
        /// settings: the old SHA-256 format, a different iteration count, or no pepper where one
        /// is now configured.
        /// </summary>
        public static bool NeedsRehash(string stored, IPasswordHashSettings settings)
        {
            if (!TryParse(stored, out var iterations, out var peppered, out _))
                return true;

            return iterations != IterationsOf(settings) || peppered != (PepperOf(settings).Length > 0);
        }

        /// <summary>The work a real check costs, for a login whose account does not exist.</summary>
        public static void Burn(string password, IPasswordHashSettings settings)
        {
            Derive(password, "0000000000000000000000000000000000000000", IterationsOf(settings), PepperOf(settings));
        }

        /// <summary>The format before PBKDF2: lowercase hex of SHA-256("salt:password").</summary>
        public static string LegacyHash(string password, string salt)
        {
            var digest = SHA256.HashData(Encoding.UTF8.GetBytes($"{salt}:{password ?? string.Empty}"));
            return Convert.ToHexString(digest).ToLowerInvariant();
        }

        private static byte[] Derive(string password, string salt, int iterations, string pepper)
        {
            var secret = Encoding.UTF8.GetBytes(password ?? string.Empty);

            // The pepper keys an HMAC of the password rather than being appended to it, so it
            // cannot run into the password's own characters.
            if (pepper.Length > 0)
                secret = HMACSHA256.HashData(Encoding.UTF8.GetBytes(pepper), secret);

            return Rfc2898DeriveBytes.Pbkdf2(secret, Encoding.UTF8.GetBytes(salt ?? string.Empty), iterations, HashAlgorithmName.SHA256, HashBytes);
        }

        private static string Format(int iterations, bool peppered, byte[] hash) =>
            $"{Scheme}${iterations.ToString(CultureInfo.InvariantCulture)}${(peppered ? "p" : "-")}${Convert.ToHexString(hash).ToLowerInvariant()}";

        private static bool TryParse(string stored, out int iterations, out bool peppered, out byte[] hash)
        {
            iterations = 0;
            peppered = false;
            hash = null;

            var parts = stored?.Split('$');

            if (parts == null || parts.Length != 4 || parts[0] != Scheme)
                return false;

            if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out iterations) || iterations <= 0)
                return false;

            if (parts[2] != "p" && parts[2] != "-")
                return false;

            peppered = parts[2] == "p";

            if (parts[3].Length != HashBytes * 2)
                return false;

            try
            {
                hash = Convert.FromHexString(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            return true;
        }
    }
}
