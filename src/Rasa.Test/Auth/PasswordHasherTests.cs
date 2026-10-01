using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Auth
{
    using Rasa.Services.Passwords;

    [TestClass]
    public class PasswordHasherTests
    {
        private sealed class Settings : IPasswordHashSettings
        {
            public string Pepper { get; set; } = string.Empty;
            public int Iterations { get; set; } = PasswordHasher.MinimumIterations;
        }

        private const string Salt = "fixture-salt";

        [TestMethod]
        public void CreatedHashVerifiesOnlyTheSamePasswordAndSalt()
        {
            var settings = new Settings();
            var stored = PasswordHasher.Create("password123", Salt, settings);

            Assert.IsTrue(stored.StartsWith("pbkdf2-sha256$100000$-$"), stored);
            Assert.AreEqual(23 + 64, stored.Length);
            Assert.IsTrue(PasswordHasher.Verify("password123", Salt, stored, settings));
            Assert.IsFalse(PasswordHasher.Verify("password124", Salt, stored, settings));
            Assert.IsFalse(PasswordHasher.Verify("password123", "other-salt", stored, settings));
            Assert.IsFalse(PasswordHasher.NeedsRehash(stored, settings));
        }

        [TestMethod]
        public void LegacyHashStillVerifiesAndAsksForARehash()
        {
            var settings = new Settings();
            var legacy = PasswordHasher.LegacyHash("password123", Salt);

            Assert.IsTrue(PasswordHasher.Verify("password123", Salt, legacy, settings));
            Assert.IsFalse(PasswordHasher.Verify("wrong", Salt, legacy, settings));
            Assert.IsTrue(PasswordHasher.NeedsRehash(legacy, settings));
        }

        [TestMethod]
        public void PepperedHashNeedsThatPepper()
        {
            var peppered = new Settings { Pepper = "server-secret" };
            var stored = PasswordHasher.Create("password123", Salt, peppered);

            Assert.IsTrue(stored.Contains("$p$"), stored);
            Assert.AreNotEqual(PasswordHasher.Create("password123", Salt, new Settings()), stored);
            Assert.IsTrue(PasswordHasher.Verify("password123", Salt, stored, peppered));
            Assert.IsFalse(PasswordHasher.Verify("password123", Salt, stored, new Settings()));
            Assert.IsFalse(PasswordHasher.Verify("password123", Salt, stored, new Settings { Pepper = "another" }));
        }

        [TestMethod]
        public void SettingsChangesAskForARehashButKeepOldHashesWorking()
        {
            var before = new Settings();
            var stored = PasswordHasher.Create("password123", Salt, before);

            var moreIterations = new Settings { Iterations = 150_000 };
            Assert.IsTrue(PasswordHasher.NeedsRehash(stored, moreIterations));
            Assert.IsTrue(PasswordHasher.Verify("password123", Salt, stored, moreIterations));

            var pepperAdded = new Settings { Pepper = "server-secret" };
            Assert.IsTrue(PasswordHasher.NeedsRehash(stored, pepperAdded));
            Assert.IsTrue(PasswordHasher.Verify("password123", Salt, stored, pepperAdded));
        }

        [TestMethod]
        public void IterationsDefaultAndFloor()
        {
            Assert.AreEqual(PasswordHasher.DefaultIterations, PasswordHasher.IterationsOf(new Settings { Iterations = 0 }));
            Assert.AreEqual(PasswordHasher.MinimumIterations, PasswordHasher.IterationsOf(new Settings { Iterations = 5 }));
        }

        [TestMethod]
        public void MalformedStoredHashesNeverVerify()
        {
            var settings = new Settings();

            Assert.IsFalse(PasswordHasher.Verify("password123", Salt, null, settings));
            Assert.IsFalse(PasswordHasher.Verify("password123", Salt, string.Empty, settings));
            Assert.IsFalse(PasswordHasher.Verify("password123", Salt, "pbkdf2-sha256$abc$-$zz", settings));
            Assert.IsFalse(PasswordHasher.Verify("password123", Salt, "pbkdf2-sha256$100000$x$" + new string('0', 64), settings));
        }
    }
}
