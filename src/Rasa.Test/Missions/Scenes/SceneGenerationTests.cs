using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Missions.Scenes;

namespace Rasa.Test.Missions.Scenes
{
    [TestClass]
    public class SceneGenerationTests
    {
        [TestMethod]
        public void CancellingOneRunPreservesOtherRunsNamedWaits()
        {
            var queue = new SceneDueQueue();
            queue.Schedule(new SceneDueWork("first", 1, "arrival", DateTime.UnixEpoch, 1));
            queue.Schedule(new SceneDueWork("second", 1, "arrival", DateTime.UnixEpoch, 1));
            queue.Cancel("first");
            Assert.AreEqual("second", queue.TakeDue(DateTime.UnixEpoch).Single().RunId);
            Assert.AreEqual(0, queue.TakeDue(DateTime.UnixEpoch).Count);
        }

        [TestMethod]
        public void RestartedGenerationCannotFireTheOldGenerationTimer()
        {
            var queue = new SceneDueQueue();
            queue.Schedule(new SceneDueWork("run", 1, "arrival", DateTime.UnixEpoch, 1));
            queue.Schedule(new SceneDueWork("run", 2, "arrival", DateTime.UnixEpoch.AddSeconds(2), 1));
            Assert.AreEqual(0, queue.TakeDue(DateTime.UnixEpoch.AddSeconds(1)).Count);
            Assert.AreEqual(2U, queue.TakeDue(DateTime.UnixEpoch.AddSeconds(2)).Single().Generation);
        }
    }
}
