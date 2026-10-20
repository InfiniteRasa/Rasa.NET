extern alias RasaGame;

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Managers;

    /// <summary>
    /// The Proving Grounds' map has a shared channel as well as each recruit's own copy. One who
    /// comes onto the shared channel - a game master's .teleport to 1985 - is there without the
    /// Bootcamp experience, which runs on its character's own copy only (SceneApplication).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class BootcampSharedChannelTests
    {
        [TestMethod]
        public void OneOnTheSharedChannelIsThereWithoutTheExperience()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            var client = harness.Client;
            var own = harness.BootcampMap;
            Assert.IsTrue(own.IsPrivateInstance);

            var shared = harness.Maps.FindByContextId(BootcampRuntimeTestHarness.BootcampMapContextId);
            Assert.IsNotNull(shared);
            Assert.IsFalse(shared.IsPrivateInstance);

            CellManager.Instance.RemoveFromWorld(client);
            own.ClientList.Remove(client);
            client.Player.MapChannel = shared;
            client.Player.RuntimeMapChannel = shared;
            shared.ClientList.Add(client);
            CellManager.Instance.AddToWorld(client);
            client.State = RasaGame::Rasa.Data.ClientState.Ingame;

            // It used to throw "requires its character's private map", which dropped the client.
            MissionApplication.Instance.Scenes.Resume(client);
            MissionApplication.Instance.Scenes.MissionChanged(client, 1992, "Accepted");

            Assert.IsFalse(shared.ClientList.Any(other => other != client), "nobody else here");
            Assert.AreSame(shared, client.Player.MapChannel);
        }
    }
}
