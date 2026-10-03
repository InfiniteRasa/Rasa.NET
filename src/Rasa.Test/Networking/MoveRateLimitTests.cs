using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using Rasa.Game;

    [TestClass]
    public class MoveRateLimitTests
    {
        /// <summary>Runs a bucket at a steady rate for a while; how many of the sends got a token.</summary>
        private static int Sent((string Bucket, double PerSecond, double Burst) limit, double perSecond, double seconds)
        {
            var tokens = limit.Burst;
            var tick = 0L;
            var sent = 0;
            var intervalMs = 1000d / perSecond;

            for (var i = 0; i * intervalMs < seconds * 1000; i++)
            {
                var now = (long)(i * intervalMs);
                var (taken, left) = Client.TakeToken(tokens, tick, now, limit.PerSecond, limit.Burst);
                tokens = left;
                tick = now;

                if (taken)
                    sent++;
            }

            return sent;
        }

        [TestMethod]
        public void AWalkingClientIsNeverDropped()
        {
            // Ten Moves a second walking, for a minute.
            Assert.AreEqual(600, Sent(Client.MoveLimit, 10, 60));

            // Twice that, turning and jumping about.
            Assert.AreEqual(1500, Sent(Client.MoveLimit, 25, 60));
        }

        [TestMethod]
        public void AMoveFloodIsHeldToTheRate()
        {
            // A thousand a second for ten seconds: the burst, then the rate.
            var sent = Sent(Client.MoveLimit, 1000, 10);

            Assert.IsTrue(sent <= Client.MoveLimit.Burst + Client.MoveLimit.PerSecond * 10 + 1, $"{sent} sent");
            Assert.IsTrue(sent >= Client.MoveLimit.PerSecond * 10, $"{sent} sent");
        }

        [TestMethod]
        public void PingsAreAnsweredAtAHumanRateAndNotAFlood()
        {
            Assert.AreEqual(60, Sent(Client.PingLimit, 1, 60));

            var sent = Sent(Client.PingLimit, 200, 10);
            Assert.IsTrue(sent <= Client.PingLimit.Burst + Client.PingLimit.PerSecond * 10 + 1, $"{sent} sent");
        }

        [TestMethod]
        public void TheBucketRefillsWhileIdleUpToItsBurstAndNoFurther()
        {
            // Empty, then an hour idle: the burst and no more.
            var (taken, left) = Client.TakeToken(0, 0, 3_600_000, 30, 60);
            Assert.IsTrue(taken);
            Assert.AreEqual(59, left, 1e-9);

            // Empty, and asked again at once: nothing to take.
            (taken, left) = Client.TakeToken(0.5, 1000, 1000, 30, 60);
            Assert.IsFalse(taken);
            Assert.AreEqual(0.5, left, 1e-9);

            // A clock that went backwards refills nothing rather than taking tokens away.
            (taken, left) = Client.TakeToken(5, 2000, 1000, 30, 60);
            Assert.IsTrue(taken);
            Assert.AreEqual(4, left, 1e-9);
        }
    }
}
