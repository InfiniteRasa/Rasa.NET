using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using Rasa.Api;
    using Rasa.Api.Ingame;

    [TestClass]
    public class NpcGreetingApiTests
    {
        [TestMethod]
        public void ReadAndWriteRequireGameSessionNotApiKey()
        {
            var sessions = new IngameSessionService();
            var read = new IngameNpcGreetingsEndpoint(sessions);
            var write = new IngameNpcGreetingUpdateEndpoint(sessions);
            Assert.AreEqual("ingame/npc-greetings", read.Name);
            Assert.AreEqual("ingame/npc-greetings/update", write.Name);
            Assert.AreEqual("POST", write.Method);
            Assert.IsTrue(write.Sensitive);
            Assert.IsFalse(read.RequiresApiKey);
            Assert.IsFalse(write.RequiresApiKey);
            Assert.AreEqual(401, read.Handle(new ApiRequest { Method = "GET", Remote = IPAddress.Loopback }).Status);
            Assert.AreEqual(401, write.Handle(new ApiRequest { Method = "POST", Remote = IPAddress.Loopback }).Status);
        }

        [TestMethod]
        public void GreetingAssignmentsRejectMissingClientLinesAndImportantDefaults()
        {
            Assert.AreEqual("creatureId must be positive", IngameNpcGreetingUpdateEndpoint.Validate(0, 0, false));
            Assert.IsNull(IngameNpcGreetingUpdateEndpoint.Validate(30, 0, false));
            Assert.IsNull(IngameNpcGreetingUpdateEndpoint.Validate(30, 859, true));
            StringAssert.Contains(IngameNpcGreetingUpdateEndpoint.Validate(30, 2, false), "no client");
            StringAssert.Contains(IngameNpcGreetingUpdateEndpoint.Validate(30, 0, true), "cannot be marked");
        }
    }
}
