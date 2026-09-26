using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Managers;
    using Rasa.Structures;

    [TestClass]
    public class MissionArchitectureContractTests
    {
        [TestMethod]
        public void ObjectiveRulesBelongToTheIndependentMissionAssembly()
        {
            var assembly = typeof(MissionProgressRule).Assembly;
            Assert.AreEqual("Rasa.Missions", assembly.GetName().Name);
            Assert.IsFalse(assembly.GetReferencedAssemblies().Any(reference =>
                reference.Name == "Rasa.Game" || reference.Name == "Rasa.DBL" ||
                reference.Name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)));
        }

        [TestMethod]
        public void PublicEscortAdmissionHasAnExactActorLeaseBoundary()
        {
            Assert.IsNotNull(typeof(MissionApplication).Assembly.GetType(
                "Rasa.Game.Missions.World.PublicActorLeaseService"),
                "Public escort starts must reserve the existing actor before mission publication.");
        }
    }
}
