using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Content
{
    using Rasa.Data;
    using Rasa.Game.Missions.Content;
    using Rasa.Managers;
    using Rasa.Missions.Scenes;
    using Rasa.Structures;
    using Rasa.Structures.Missions;

    [TestClass]
    [DoNotParallelize]
    public class MissionAuthoringLocalityTests
    {
        [TestMethod]
        public void DataOnlyExampleCompletesKillCollectAndTalkWithoutAManagerBranch()
        {
            var data = MissionAuthoringExampleData.Ordinary();
            Assert.IsFalse(data.Definitions.Single().Enabled);
            var snapshot = new MissionContentLoader().Load(data.CreateWorldUnitOfWork().MissionContent);
            var definitions = MissionDefinitionCatalog.CreateDefinitions(snapshot,
                new MissionValidationReport(Array.Empty<MissionValidationDiagnostic>(), Array.Empty<uint>()));
            using var context = MissionTestContext.WithCustomDefinitions(definitions);
            var giver = context.AddNpc(510206);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 1994));
            Assert.IsTrue(context.Manager.RecordProgress(context.Client, MissionProgressEvent.Creature(510210)));
            Assert.IsTrue(context.Manager.RecordProgress(context.Client, MissionProgressEvent.ItemAcquired(3147, 1)));
            var receiver = context.AddNpc(510207, npcPackageId: 2561);
            Assert.IsTrue(context.Manager.CompleteOfferedObjective(context.Client, receiver.EntityId, 1994, 3, 1));
            Assert.IsTrue(context.Client.Player.Missions[1994].Completeable);
        }

        [TestMethod]
        public void ScriptedExampleUsesARegisteredScriptAndExistingWorldIntents()
        {
            var scene = MissionAuthoringExampleData.Escort();
            var run = new SceneRun(Guid.NewGuid().ToString("N"), "example-escort",
                scene.Script, 1, 1, 0, "{}", SceneStatus.Running, 1, 1990);
            var result = new SceneRuntime(new SceneScriptRegistry()).Evaluate(run,
                scene.Bindings(run.Release), new SceneObservation(SceneEventKind.Started, 1), DateTime.UnixEpoch);
            Assert.IsTrue(result.Accepted, result.Rejection);
            Assert.IsTrue(result.Decision.WorldIntents.OfType<RunRouteIntent>().Any());
            Assert.IsTrue(result.Decision.WorldIntents.OfType<SetInteractionIntent>().Any());
        }

    }
}
