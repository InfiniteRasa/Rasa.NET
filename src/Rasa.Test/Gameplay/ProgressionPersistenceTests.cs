using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Manifestation.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Test.World;

    [TestClass]
    [DoNotParallelize]
    public class ProgressionPersistenceTests
    {
        [TestMethod]
        public void CurrencySaveFailureLeavesRuntimeStorageAndPacketsUntouched()
        {
            using var context = new WeaponAmmoContext();
            var manager = new CharacterManager(context);
            context.Client.Player.Credits[CurencyType.Credits] = 100;
            using (var database = context.Open())
            {
                database.CharacterEntries.Single().Credit = 100;
                database.SaveChanges();
            }
            context.BeforeSave = _ => throw new DbUpdateException("Injected currency failure.");

            Assert.IsFalse(manager.UpdateCharacter(
                context.Client, CharacterUpdate.Credits, 7));

            Assert.AreEqual(100, context.Client.Player.Credits[CurencyType.Credits]);
            using (var verify = context.Open())
                Assert.AreEqual(100, verify.CharacterEntries.AsNoTracking().Single().Credit);
            Assert.AreEqual(0, WorldTestContext.Drain(context.Client).Count);
        }

        [TestMethod]
        public void FailedCreditGainDoesNotPublishARewardMessage()
        {
            using var context = new WeaponAmmoContext();
            context.Client.Player.Credits[CurencyType.Credits] = 100;
            using (var database = context.Open())
            {
                database.CharacterEntries.Single().Credit = 100;
                database.SaveChanges();
            }
            context.BeforeSave = _ => throw new DbUpdateException("Injected reward failure.");

            Assert.IsFalse(new ManifestationManager(context)
                .GainCredits(context.Client, 7));

            Assert.AreEqual(100, context.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(0, WorldTestContext.Drain(context.Client).Count);
        }

        [TestMethod]
        public void ExperienceOverflowOrSaveFailureCannotPartiallyAdvance()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            context.Client.Player.Experience = 100;
            using (var database = context.Open())
            {
                database.CharacterEntries.Single().Experience = 100;
                database.SaveChanges();
            }
            var saves = context.SaveAttempts;

            manager.GainExperience(context.Client, uint.MaxValue);

            Assert.AreEqual(100u, context.Client.Player.Experience);
            Assert.AreEqual(saves, context.SaveAttempts);
            Assert.AreEqual(0, WorldTestContext.Drain(context.Client).Count);

            context.BeforeSave = _ => throw new DbUpdateException("Injected experience failure.");
            manager.GainExperience(context.Client, 10);
            Assert.AreEqual(100u, context.Client.Player.Experience);
            using (var verify = context.Open())
                Assert.AreEqual(100u, verify.CharacterEntries.AsNoTracking().Single().Experience);
            Assert.AreEqual(0, WorldTestContext.Drain(context.Client).Count);
        }

        /// <summary>
        /// Experience from a kill that reaches 5, 15 or 30 has to save the clone credit it hands
        /// out. When only the copy in memory got it, the row no longer matched the player, and
        /// every later award was refused as "durable progression changed" until a relog - which
        /// loaded the row and dropped the credit.
        /// </summary>
        [TestMethod]
        [DataRow(4, 42000u, 2000u, 5, 1u, DisplayName = "level 5")]
        [DataRow(14, 886000u, 1000u, 15, 1u, DisplayName = "level 15")]
        [DataRow(29, 9836000u, 2000u, 30, 1u, DisplayName = "level 30")]
        [DataRow(1, 0u, 900000u, 15, 2u, DisplayName = "1 to 15 in one award: both 5 and 15")]
        [DataRow(5, 69000u, 1000u, 6, 0u, DisplayName = "level 6 pays nothing")]
        public void KillExperienceSavesTheCloneCreditsItHandsOut(
            int level, uint experience, uint award, int levelAfter, uint credits)
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var player = context.Client.Player;
            player.Level = (byte)level;
            player.Experience = experience;
            player.CloneCredits = 0;
            player.Attributes = Enum.GetValues<Attributes>().ToDictionary(
                attribute => attribute,
                attribute => new ActorAttributes(attribute, 100, 100, 100, 0, 0));
            using (var database = context.Open())
            {
                var row = database.CharacterEntries.Single();
                row.Level = (byte)level;
                row.Experience = experience;
                row.CloneCredits = 0;
                database.SaveChanges();
            }

            manager.GainExperience(context.Client, award);

            Assert.AreEqual((byte)levelAfter, player.Level);
            Assert.AreEqual(experience + award, player.Experience);
            Assert.AreEqual(credits, player.CloneCredits);
            using (var verify = context.Open())
            {
                var row = verify.CharacterEntries.AsNoTracking().Single();
                Assert.AreEqual((byte)levelAfter, row.Level);
                Assert.AreEqual(experience + award, row.Experience);
                Assert.AreEqual(credits, row.CloneCredits, "The saved clone credits.");
            }
            CollectionAssert.AreEqual(
                Enumerable.Range(1, (int)credits).Select(count => (uint)count).ToList(),
                WorldTestContext.Drain(context.Client)
                    .Select(packet => packet.Message).OfType<CallMethodMessage>()
                    .Select(message => message.Packet).OfType<CloneCreditsPacket>()
                    .Select(packet => packet.CloneCredits).ToList());

            // The next kill: the row and the player still agree, so it is not refused.
            manager.GainExperience(context.Client, 100);

            Assert.AreEqual(experience + award + 100, player.Experience);
            using (var verify = context.Open())
            {
                var row = verify.CharacterEntries.AsNoTracking().Single();
                Assert.AreEqual(experience + award + 100, row.Experience);
                Assert.AreEqual(credits, row.CloneCredits);
            }
        }

        [TestMethod]
        public void TheCloneCreditLevelsAreTheOnesTheTableLists()
        {
            for (var level = 0; level <= ManifestationManager.MaxPlayerLevel; level++)
                Assert.AreEqual(
                    ManifestationManager.CloneCreditLevels.Contains((byte)level),
                    ManifestationManager.IsCloneCreditLevel(level),
                    $"level {level}");
        }

        [TestMethod]
        public void AttributeAllocationPersistsBeforeRuntimeAndPackets()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            context.Client.Player.Level = 2;
            context.Client.Player.Attributes = Enum.GetValues<Attributes>().ToDictionary(
                attribute => attribute,
                attribute => new ActorAttributes(attribute, 0, 0, 0, 0, 0));
            using (var database = context.Open())
            {
                database.CharacterEntries.Single().Level = 2;
                database.SaveChanges();
            }
            context.BeforeSave = _ =>
            {
                Assert.AreEqual(0, context.Client.Player.SpentBody);
                Assert.AreEqual(0, WorldTestContext.Drain(context.Client).Count);
                throw new DbUpdateException("Injected attribute failure.");
            };

            manager.AllocateAttributePoints(context.Client,
                new AllocateAttributePointsPacket { Body = 1, Mind = 1, Spirit = 1 });

            Assert.AreEqual(0, context.Client.Player.SpentBody);
            Assert.AreEqual(0, context.Client.Player.SpentMind);
            Assert.AreEqual(0, context.Client.Player.SpentSpirit);
            using var verify = context.Open();
            var character = verify.CharacterEntries.AsNoTracking().Single();
            Assert.AreEqual(0, character.Body);
            Assert.AreEqual(0, WorldTestContext.Drain(context.Client)
                .Select(packet => packet.Message).OfType<CallMethodMessage>().Count());
        }

        [TestMethod]
        public void SkillTrainingRollsBackTheWholeBatchBeforeRuntimePublication()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            context.Client.Player.Level = 15;
            context.Client.Player.Class = (uint)CharacterClass.Recruit;
            var requirements = (Dictionary<SkillId, (CharacterClass Class, int Level)>)
                typeof(ManifestationManager)
                    .GetField("_skillClasses", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(manager);
            requirements[(SkillId)49] = (CharacterClass.Recruit, 1);
            requirements[(SkillId)165] = (CharacterClass.Recruit, 1);
            var start = context.SaveAttempts;
            context.AfterSave = _ =>
            {
                if (context.SaveAttempts == start + 2)
                    throw new DbUpdateException("Injected skill batch failure.");
            };

            manager.LevelSkills(context.Client, new LevelSkillsPacket
            {
                ListLenght = 2,
                SkillIds = new[] { 49, 165 },
                SkillLevels = new[] { 1, 1 }
            });

            Assert.AreEqual(0, context.Client.Player.Skills.Count);
            Assert.AreEqual(0, WorldTestContext.Drain(context.Client).Count);
            using (var verify = context.CreateChar())
                Assert.AreEqual(0, verify.CharacterSkills
                    .GetCharacterSkills(context.Client.Player.Id).Count);

            context.AfterSave = null;
            manager.LevelSkills(context.Client, new LevelSkillsPacket
            {
                ListLenght = 2,
                SkillIds = new[] { 49, 165 },
                SkillLevels = new[] { 1, 1 }
            });
            Assert.AreEqual(2, context.Client.Player.Skills.Count);
        }
    }
}
