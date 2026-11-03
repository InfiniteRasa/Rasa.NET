using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Test.Missions;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.World
{
    /// <summary>
    /// The adrenaline bar is 1000 for every character (ManifestationManager.AdrenalineBarMax):
    /// action_cost gives adrenaline in tenths of a percent of the bar, and the client checks a
    /// cost against the bar in the same units. With the bar at the power formula's total, a
    /// 230 bar paid 100 for Rage I and 200 for Rage II - not the "10%" and "20%" of the tooltip.
    /// </summary>
    [TestClass, DoNotParallelize]
    public class AdrenalineBarTests
    {
        private const SkillId Rage = (SkillId)147;   // T2_SOLDIER_RAGE

        [TestMethod]
        [DataRow(1u, 900)]
        [DataRow(2u, 800)]
        public void RageTakesTheShareOfTheBarItsTooltipGives(uint pump, int left)
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var abilities = harness.LoadAbilities();
            var player = harness.Client.Player;
            player.Skills[Rage] = new SkillsData(Rage, (int)ActionId.AaSoldierRage, (int)pump);

            foreach (var logos in AbilityLogos.Of(ActionId.AaSoldierRage))
                if (!player.Logos.Contains(logos))
                    player.Logos.Add(logos);

            player.Attributes[Attributes.Chi] = new ActorAttributes(Attributes.Chi, ManifestationManager.AdrenalineBarMax, ManifestationManager.AdrenalineBarMax, ManifestationManager.AdrenalineBarMax, 0, 0);
            player.Attributes[Attributes.Power] = new ActorAttributes(Attributes.Power, 500, 500, 500, 0, 0);

            lock (Server.Clients)
                Server.Clients.Add(harness.Client);

            try
            {
                abilities.RequestPerformAbility(harness.Client, ToyTests.Request((int)ActionId.AaSoldierRage, pump, 0));
                var pending = harness.Map.PerformRecovery.Single(entry => entry.ActionId == ActionId.AaSoldierRage);
                harness.Map.PerformRecovery.Remove(pending);
                abilities.PerformRecovery(harness.Map, pending);

                Assert.AreEqual(left, player.Attributes[Attributes.Chi].Current, $"{(ManifestationManager.AdrenalineBarMax - left) / 10}% of the bar");
            }
            finally
            {
                lock (Server.Clients)
                    Server.Clients.Remove(harness.Client);
            }
        }

        /// <summary>No adrenaline cost is more than the bar, and the waves' 1000 - "Adrenaline: 100%" - is all of it.</summary>
        [TestMethod]
        public void EveryAdrenalineCostIsAShareOfTheBar()
        {
            var adrenaline = new ActionCostPreloader().Rows()
                .Where(row => Convert.ToInt32(row[3]) == (int)Attributes.Chi)
                .Select(row => Convert.ToInt32(row[4]))
                .ToList();

            Assert.IsTrue(adrenaline.All(cost => cost > 0 && cost <= ManifestationManager.AdrenalineBarMax));
            Assert.AreEqual(ManifestationManager.AdrenalineBarMax, adrenaline.Max(), "a signature wave takes a full bar");
        }
    }
}
