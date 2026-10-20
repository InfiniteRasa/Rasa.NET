using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;

    // The Respec Skills (122035) and Respec Attributes (122034) tokens were refused as not
    // implemented and kept. Respec Skills puts the skills back to a new character's - the Recruit
    // skills at rank 1 - and Respec Attributes gives back every Body, Mind and Spirit point; each
    // is saved with the token's use.
    [TestClass]
    [DoNotParallelize]
    public class RespecTokenTests
    {
        private const uint RespecSkillsTemplate = 122035;       // CONSUMABLE_RESET_SKILLS 501 level 1
        private const uint RespecAttributesTemplate = 122034;   // CONSUMABLE_RESET_ATTRIBS 502 level 1

        [TestMethod]
        public void RespecSkillsLeavesTheRecruitSkillsAtRankOneAndGivesTheRestBack()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            player.Level = 10;
            Train(harness, new Dictionary<SkillId, int>
            {
                [SkillId.Lightning] = 1, [SkillId.Sprint] = 3, [SkillId.Firearms] = 2,
                [SkillId.HandToHand] = 1, [SkillId.MotorAssistArmor] = 1,
                [SkillId.SpecialistTools] = 2, [(SkillId)22] = 1
            });
            var item = ToyTests.Grant(harness, RespecSkillsTemplate);
            var manager = ToyTests.CreateManager(harness, 501, 1, RespecSkillsTemplate);
            harness.Drain();

            Use(harness, manager, 501, item);

            var expected = new Dictionary<SkillId, int>
            {
                [SkillId.Lightning] = 1, [SkillId.Sprint] = 1, [SkillId.Firearms] = 1,
                [SkillId.HandToHand] = 1, [SkillId.MotorAssistArmor] = 1
            };
            CollectionAssert.AreEquivalent(expected, player.Skills.ToDictionary(s => s.Key, s => s.Value.SkillLevel));

            using (var unit = harness.Context.CreateChar())
            {
                CollectionAssert.AreEquivalent(expected, unit.CharacterSkills.GetCharacterSkills(player.Id)
                    .ToDictionary(s => (SkillId)s.SkillId, s => s.SkillLevel), "saved");
                Assert.IsNull(unit.Items.GetItem(item.Id), "the token is used up");
            }

            var sent = harness.Drain();
            Assert.IsTrue(sent.OfType<SkillsPacket>().Any());
            Assert.IsTrue(sent.OfType<AbilitiesPacket>().Any());
            // Level 10: 5 + 2 a level after the first, and 2 more at 5 - with the five Recruit ranks spent.
            Assert.AreEqual(5 + 18 + 2 - 5, sent.OfType<AvailableAllocationPointsPacket>().Last().AvailableSkillPoints);
            Assert.IsTrue(sent.OfType<AbilityRecoveryPacket>().Any(p => p.ActionId == ActionId.ConsumableResetSkills));
        }

        [TestMethod]
        public void RespecAttributesGivesBackEveryPointSpent()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            player.Level = 10;
            Spend(harness, 10, 9, 8);
            var item = ToyTests.Grant(harness, RespecAttributesTemplate);
            var manager = ToyTests.CreateManager(harness, 502, 1, RespecAttributesTemplate);
            harness.Drain();

            Use(harness, manager, 502, item);

            Assert.AreEqual(0, player.SpentBody + player.SpentMind + player.SpentSpirit);

            using (var unit = harness.Context.CreateChar())
            {
                var row = unit.Characters.Get(player.Id);
                Assert.AreEqual((0, 0, 0), (row.Body, row.Mind, row.Spirit), "saved");
                Assert.IsNull(unit.Items.GetItem(item.Id), "the token is used up");
            }

            var sent = harness.Drain();
            Assert.IsTrue(sent.OfType<AttributeInfoPacket>().Any());
            Assert.AreEqual(27, sent.OfType<AvailableAllocationPointsPacket>().Last().AvailableAttributePoints, "3 a level after the first");
        }

        [TestMethod]
        [DataRow(RespecSkillsTemplate, 501U)]
        [DataRow(RespecAttributesTemplate, 502U)]
        public void ATokenWithNothingToGiveBackIsRefusedAndKept(uint template, uint actionId)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            Train(harness, CharacterManager.StartingSkills.ToDictionary(skill => skill, _ => 1));
            Spend(harness, 0, 0, 0);
            var item = ToyTests.Grant(harness, template);
            var manager = ToyTests.CreateManager(harness, actionId, 1, template);

            manager.RequestPerformAbility(harness.Client, ToyTests.Request((int)actionId, 1, item.EntityId));

            Assert.IsFalse(harness.BootcampMap.PerformRecovery.Any(queued => (uint)queued.ActionId == actionId));
            using var unit = harness.Context.CreateChar();
            Assert.IsNotNull(unit.Items.GetItem(item.Id));
        }

        [TestMethod]
        public void PointsSpentBackDuringTheWindupKeepTheToken()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            player.Level = 10;
            Spend(harness, 3, 0, 0);
            var item = ToyTests.Grant(harness, RespecAttributesTemplate);
            var manager = ToyTests.CreateManager(harness, 502, 1, RespecAttributesTemplate);

            manager.RequestPerformAbility(harness.Client, ToyTests.Request(502, 1, item.EntityId));
            var pending = harness.BootcampMap.PerformRecovery.Single(queued => queued.ActionId == ActionId.ConsumableResetAttribs);
            Spend(harness, 0, 0, 0);
            ToyTests.Land(harness, manager, pending);

            using var unit = harness.Context.CreateChar();
            Assert.IsNotNull(unit.Items.GetItem(item.Id), "nothing to give back by the time it landed");
        }

        #region Fixture

        private static void Use(BootcampRuntimeTestHarness.Harness harness, AbilityManager manager, uint actionId, Item item)
        {
            manager.RequestPerformAbility(harness.Client, ToyTests.Request((int)actionId, 1, item.EntityId));
            var pending = harness.BootcampMap.PerformRecovery.Single(queued => (uint)queued.ActionId == actionId);
            ToyTests.Land(harness, manager, pending);
        }

        /// <summary>The player's skills, in memory and saved, as LevelSkills leaves them.</summary>
        private static void Train(BootcampRuntimeTestHarness.Harness harness, Dictionary<SkillId, int> skills)
        {
            var player = harness.Client.Player;

            using (var unit = harness.Context.CreateChar())
            {
                var held = unit.CharacterSkills.GetCharacterSkills(player.Id).Select(s => s.SkillId).ToList();

                unit.ExecuteTransaction(() =>
                {
                    if (held.Count > 0)
                        unit.CharacterSkills.Delete(player.Id, held);

                    foreach (var (skillId, level) in skills)
                        unit.CharacterSkills.AddOrUpdate(player.Id, (uint)skillId, 1000 + (int)skillId, level);
                });
            }

            player.Skills = skills.ToDictionary(s => s.Key, s => new SkillsData(s.Key, 1000 + (int)s.Key, s.Value));
        }

        /// <summary>Attribute points spent, in memory and saved.</summary>
        private static void Spend(BootcampRuntimeTestHarness.Harness harness, int body, int mind, int spirit)
        {
            var player = harness.Client.Player;
            player.SpentBody = body;
            player.SpentMind = mind;
            player.SpentSpirit = spirit;

            using var unit = harness.Context.CreateChar();
            unit.ExecuteTransaction(() => unit.Characters.UpdateCharacterAttributes(player.Id, body, mind, spirit));
        }

        #endregion
    }
}
