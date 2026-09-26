using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Memory
{
    [TestClass]
    [DoNotParallelize]
    public class ArrayPoolTrackerTests
    {
        [TestMethod]
        public void DuplicateReturnFailsOwnershipAssertion()
        {
            using var tracker = new ArrayPoolTracker(128);
            tracker.RecordRent(1, 128);
            tracker.RecordReturn(1, 128);
            tracker.RecordReturn(1, 128);

            Assert.ThrowsExactly<AssertFailedException>(() => tracker.AssertReturned());
        }

        [TestMethod]
        public void MissingReturnFailsOwnershipAssertion()
        {
            using var tracker = new ArrayPoolTracker(128);
            tracker.RecordRent(1, 128);

            Assert.ThrowsExactly<AssertFailedException>(() => tracker.AssertReturned());
        }

        [TestMethod]
        public void MatchingRentAndReturnPassOwnershipAssertion()
        {
            using var tracker = new ArrayPoolTracker(128);
            tracker.RecordRent(1, 128);
            tracker.RecordReturn(1, 128);

            tracker.AssertReturned();
        }
    }
}
