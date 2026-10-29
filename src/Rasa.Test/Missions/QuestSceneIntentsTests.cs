using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;

namespace Rasa.Test.Missions
{
    /// <summary>
    /// The zone quest batches fight and escort as they were playtested: their attack orders go past the leash, their
    /// escorts stop to fight when hit (QuestSceneIntents). Those options are off for any other scene, so content that
    /// made a plain AttackActorIntent or RunRouteIntent with ResumeAfterCombat would lose them without a failing
    /// playthrough: no zone quest file makes one.
    /// </summary>
    [TestClass]
    public class QuestSceneIntentsTests
    {
        private static readonly string[] QuestContent =
        {
            "src/Rasa.Missions/Content/Plateau",
            "src/Rasa.DBL/Services/Preloader/Missions/Plateau",
            "src/Rasa.Missions/Content/Pools",
            "src/Rasa.DBL/Services/Preloader/Missions/Pools",
        };

        [TestMethod]
        public void QuestAttackOrdersGoPastTheLeashAndEscortsFightWhenHit()
        {
            var attack = QuestSceneIntents.Attack("fight", "enemy");
            Assert.AreEqual(new AttackActorIntent("fight", "enemy", null, HoldTarget: false, PastLeash: true), attack);
            Assert.IsTrue(QuestSceneIntents.Attack("shoot", "enemy", "victim", holdTarget: true) is
                { TargetRole: "victim", HoldTarget: true, PastLeash: true });
            Assert.AreEqual(new RunRouteIntent("walk", "escort", "out", ResumeAfterCombat: true, FightWhenHit: true),
                QuestSceneIntents.Escort("walk", "escort", "out"));
        }

        [TestMethod]
        public void ZoneQuestContentMakesItsAttackOrdersAndEscortsOnlyThroughQuestSceneIntents()
        {
            var root = FindRepositoryRoot();
            var files = QuestContent.SelectMany(folder =>
                Directory.EnumerateFiles(Path.Combine(root, folder), "*.cs", SearchOption.AllDirectories)).ToArray();
            Assert.IsNotEmpty(files);

            var raw = files.SelectMany(file => File.ReadLines(file).Select((line, index) => (file, line, index)))
                .Where(entry => entry.line.Contains("new AttackActorIntent(") || entry.line.Contains("ResumeAfterCombat"))
                .Select(entry => $"{Path.GetRelativePath(root, entry.file)}:{entry.index + 1}: {entry.line.Trim()}")
                .ToArray();

            Assert.IsEmpty(raw, "use QuestSceneIntents.Attack / Escort:\n" + string.Join("\n", raw));
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln")))
                directory = directory.Parent;

            Assert.IsNotNull(directory, "Could not locate the repository root from the test output directory.");
            return directory.FullName;
        }
    }
}
