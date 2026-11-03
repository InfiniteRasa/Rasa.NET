using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The report: a Sniper with Vortex, Enhance, Attack and Friend could neither learn Crit Wave (no
    /// plus beside it) nor drag it to the hotbar. The client gives a signature row no plus at all and
    /// shows its icon only at rank 1; nothing gave the rank (ManifestationManager.GrantSignatureSkill).
    /// </summary>
    [TestClass, DoNotParallelize]
    public class SignatureSkillTests
    {
        private const SkillId CritWave = (SkillId)149;

        [TestMethod]
        [DataRow(CharacterClass.Commando, CharacterClass.Grenadier, 47, 234)]
        [DataRow(CharacterClass.Commando, CharacterClass.Guardian, 92, 305)]
        [DataRow(CharacterClass.Ranger, CharacterClass.Sniper, 149, 281)]
        [DataRow(CharacterClass.Ranger, CharacterClass.Spy, 110, 252)]
        [DataRow(CharacterClass.Sapper, CharacterClass.Demolitionist, 20, 137)]
        [DataRow(CharacterClass.Sapper, CharacterClass.Engineer, 157, 260)]
        [DataRow(CharacterClass.Biotechnician, CharacterClass.Medic, 154, 193)]
        [DataRow(CharacterClass.Biotechnician, CharacterClass.Exobiologist, 156, 176)]
        public void AdvancingIntoATierFourClassGivesItsSignatureAbilityAtItsOneRankForNoPoints(
            CharacterClass from, CharacterClass into, int skill, int ability)
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var manager = Manager(harness);
            Profile(harness, from, 30);
            var points = manager.GetSkillPointsAvailable(harness.Client.Player);

            manager.SelectNewCharacterClass(harness.Client, new SelectNewCharacterClassPacket { ClassId = (uint)into });

            Assert.AreEqual((uint)into, harness.Client.Player.Class);
            var held = harness.Client.Player.Skills[(SkillId)skill];
            Assert.AreEqual((1, ability), (held.SkillLevel, held.AbilityId));
            Assert.AreEqual(points, manager.GetSkillPointsAvailable(harness.Client.Player), "given, not bought");
            Assert.AreEqual(1, harness.Client.Player.Skills.Count(entry => ManifestationManager.IsSignatureSkill(entry.Key)));
            using (var unit = harness.CreateChar())
            {
                var saved = unit.CharacterSkills.GetCharacterSkills(harness.Client.Player.Id).Single(row => row.SkillId == skill);
                Assert.AreEqual((1, ability), (saved.SkillLevel, saved.AbilityId));
            }

            var sent = harness.Drain().ToList();
            Assert.IsTrue(sent.OfType<SkillsPacket>().Any());
            Assert.IsTrue(sent.OfType<AbilitiesPacket>().Any(packet => packet.AbilityList.ContainsKey((SkillId)skill)),
                "the abilities list the skills window lets the player drag from");
        }

        /// <summary>A Sniper who advanced before the grant, or a clone: given it on entering the world, once.</summary>
        [TestMethod]
        public void ASniperWithoutItIsGivenItOnceAndATierThreeRangerNever()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var manager = Manager(harness);

            Profile(harness, CharacterClass.Ranger, 30);
            Assert.IsFalse(manager.GrantSignatureSkill(harness.Client), "Ranger is tier III");
            Assert.IsFalse(harness.Client.Player.Skills.ContainsKey(CritWave));

            Profile(harness, CharacterClass.Sniper, 30);
            Assert.AreEqual(CritWave, manager.SignatureSkillFor(harness.Client.Player));
            Assert.IsTrue(manager.GrantSignatureSkill(harness.Client));
            Assert.IsFalse(manager.GrantSignatureSkill(harness.Client), "once");
            using var unit = harness.CreateChar();
            Assert.AreEqual(1, unit.CharacterSkills.GetCharacterSkills(harness.Client.Player.Id).Count(row => row.SkillId == (uint)CritWave));
        }

        [TestMethod]
        public void ASignatureSkillCannotBeTrainedAndARespecKeepsIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var manager = Manager(harness);
            Profile(harness, CharacterClass.Sniper, 30);
            harness.Client.Player.Skills.Remove(CritWave);

            manager.LevelSkills(harness.Client, new LevelSkillsPacket
            {
                ListLenght = 1, SkillIds = new[] { (int)CritWave }, SkillLevels = new[] { 1 }
            });
            Assert.IsFalse(harness.Client.Player.Skills.ContainsKey(CritWave), "no points buy it");

            Assert.IsTrue(manager.GrantSignatureSkill(harness.Client));
            harness.Client.Player.Skills[(SkillId)150] = new SkillsData((SkillId)150, 0, 3);
            var after = ManifestationManager.SkillsAfterRespec(harness.Client.Player);
            Assert.IsTrue(after.ContainsKey(CritWave), "a respec gives back points, and it cost none");
            Assert.IsFalse(after.ContainsKey((SkillId)150));
        }

        private static ManifestationManager Manager(WildernessRuntimeTestHarness harness)
        {
            var manager = new ManifestationManager(harness, harness.Manager);
            manager.LoadSkillClasses();
            return manager;
        }

        private static void Profile(WildernessRuntimeTestHarness harness, CharacterClass characterClass, byte level)
        {
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    unit.Characters.UpdateCharacterClass(harness.Client.Player.Id, (uint)characterClass);
                    unit.Characters.UpdateCharacterProgression(harness.Client.Player.Id, harness.Client.Player.Experience, level);
                });
            harness.Client.Player.Class = (uint)characterClass;
            harness.Client.Player.Level = level;
            harness.Drain();
        }
    }
}
