using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Game.Missions.Integration;

    /// <summary>
    /// How far from an NPC a talk is accepted (MissionInteractionPolicy.InRange): the client's
    /// 5 m between the two bodies, plus the server's allowance for having only their origins.
    /// </summary>
    [TestClass]
    public class ConversationReachTests
    {
        [TestMethod]
        public void TheClientsRangeIsAcceptedWithTheBodyAllowanceOverIt()
        {
            var npc = new Vector3(100, 10, 100);

            Assert.IsTrue(MissionInteractionPolicy.InRange(npc, npc), "standing on it");
            Assert.IsTrue(MissionInteractionPolicy.InRange(npc + new Vector3(5f, 0, 0), npc), "the client's own 5");
            Assert.IsTrue(MissionInteractionPolicy.InRange(npc + new Vector3(7.5f, 0, 0), npc), "5 between two bodies, 7.5 between their origins");
            Assert.IsTrue(MissionInteractionPolicy.InRange(npc + new Vector3(MissionInteractionPolicy.MaxReach, 0, 0), npc), "the edge");
        }

        [TestMethod]
        public void BeyondTheReachIsRefused()
        {
            var npc = new Vector3(100, 10, 100);

            Assert.IsFalse(MissionInteractionPolicy.InRange(npc + new Vector3(MissionInteractionPolicy.MaxReach + 0.01f, 0, 0), npc));
            Assert.IsFalse(MissionInteractionPolicy.InRange(npc + new Vector3(20f, 0, 0), npc), "RequestUseObject's 20 is not a talk's");
            Assert.IsFalse(MissionInteractionPolicy.InRange(npc + new Vector3(0, 0, -50f), npc));
        }

        [TestMethod]
        public void HeightCountsAndNothingNonFiniteIsInRange()
        {
            var npc = new Vector3(100, 10, 100);

            Assert.IsTrue(MissionInteractionPolicy.InRange(npc + new Vector3(0, 6f, 0), npc), "a balcony above the NPC");
            Assert.IsFalse(MissionInteractionPolicy.InRange(npc + new Vector3(0, 11f, 0), npc));
            Assert.IsFalse(MissionInteractionPolicy.InRange(new Vector3(float.NaN, 0, 0), npc));
            Assert.IsFalse(MissionInteractionPolicy.InRange(npc, new Vector3(float.PositiveInfinity, 0, 0)));
        }
    }
}
