using System.Collections.Generic;
using Rasa.Missions.Content;
using Rasa.Missions.Content.Wilderness;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    public static partial class WildernessTwinPillarsV1
    {
        private static MissionSceneDefinition QualifiedScene() => new()
        {
            Requirement = new CustomRequirement("character.starting-experience-completed")
        };

        private static MissionItemBinding Issued(string key, uint template, uint quantity) =>
            new(key, template, MissionItemScope.AssignmentIssued, quantity,
                MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                MissionItemCleanupDisposition.Remove);

        private static MissionSceneDefinition QuarantineScene()
        {
            var scene = QualifiedScene();
            scene.Script = "data.sequence";
            scene.Actors["blood-analyzer"] = new SceneActorDefinition("blood-analyzer", SceneActorKind.Object,
                BloodAnalyzerClass, BloodAnalyzerRoot, BloodAnalyzerYaw,
                Conversation: new SceneObjectConversation(442, 2, 1486, 2));
            scene.Items = new List<MissionItemBinding> { Issued("blood-sample", BloodSampleTemplate, 1) };
            scene.Dialogue = new List<MissionDialogueTopicDefinition>
            {
                new(2, 1486, 1, transitionId: 1)
            };
            scene.Sequences[0] = new SceneSequenceDefinition
            {
                World = new List<WorldIntent> { new EnsureActorIntent("ensure-blood-analyzer", "blood-analyzer") }
            };
            return scene;
        }

        private static MissionSceneDefinition SmugglingScene()
        {
            var scene = QualifiedScene();
            var deliveryOutcomeAllowed = new NotRequirement(new MissionStateRequirement(791, Rasa.Data.MissionState.Completed));
            scene.Requirement = new AllRequirements(new MissionRequirement[] { scene.Requirement, deliveryOutcomeAllowed });
            scene.TurnInRequirement = deliveryOutcomeAllowed;
            scene.Script = WildernessSmugglerDeliveryScene.ScriptKey;
            scene.HiddenObjectiveIds = new List<uint> { 5 };
            scene.Items = new List<MissionItemBinding> { Issued("stim-dust", StimDustTemplate, 3) };
            scene.AcceptanceItems = new List<CharacterIntent>
            {
                new IssueMissionItemIntent("moore-stim-dust-parcels", 623, "stim-dust", StimDustTemplate, 3)
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (uint delivery = 1; delivery <= 3; delivery++)
                scene.Sequences[delivery] = new SceneSequenceDefinition();
            scene.Sequences[4] = new SceneSequenceDefinition
            {
                Character = new List<CharacterIntent>
                {
                    new ObjectiveIntent("finish-smuggler-delivery-batch", 623, 5, Rasa.Data.MissionObjectiveState.Completed),
                    new ObjectiveIntent("reveal-return-to-moore", 623, 4, Rasa.Data.MissionObjectiveState.NotAssigned),
                    new ObjectiveIntent("activate-return-to-moore", 623, 4, Rasa.Data.MissionObjectiveState.Incomplete)
                }
            };
            return scene;
        }

        private static MissionSceneDefinition SuspicionScene()
        {
            var scene = QualifiedScene();
            var outstanding = new AllRequirements(new MissionRequirement[]
            {
                new MissionStateRequirement(623, Rasa.Data.MissionState.Active, Accepted: true),
                new NotRequirement(new MissionStateRequirement(623, Rasa.Data.MissionState.Completed)),
                new AssignmentItemRequirement(623, "stim-dust")
            });
            scene.Requirement = new AllRequirements(new MissionRequirement[] { scene.Requirement, outstanding });
            scene.TurnInRequirement = new NotRequirement(new MissionStateRequirement(623, Rasa.Data.MissionState.Completed));
            scene.ObjectiveRequirements[1] = outstanding;
            scene.ObjectiveRequirements[2] = outstanding;
            scene.HiddenObjectiveIds = new List<uint> { 3 };
            scene.ObjectiveAggregations = new Dictionary<uint, MissionObjectiveAggregation>
            {
                [3] = new(new uint[] { 2 }, 1)
            };
            scene.Dialogue = new List<MissionDialogueTopicDefinition>
            {
                new(1, 219, 1, transitionId: 1, requirement: outstanding),
                new(2, 415, 1, MissionDialogueKind.Choice,
                    choices: new Dictionary<int, uint> { [1] = 1, [2] = 2 }, requirement: outstanding)
            };
            return scene;
        }
    }
}
