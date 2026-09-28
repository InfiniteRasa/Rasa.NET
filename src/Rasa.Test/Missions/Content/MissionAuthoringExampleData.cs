using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Test.Missions.Content
{
    internal static class MissionAuthoringExampleData
    {
        internal static MissionContentFixture Ordinary()
        {
            var data = new MissionContentFixture();
            data.Definitions.Add(new MissionContentDefinitionEntry { MissionId = 1994, ContentRevision = "example-ordinary", Requirement = MissionContentRequirement.Optional, ClientNameTextId = 21235, GiverId = 510206, ReceiverId = 510207, Level = 1, GroupType = 1, CategoryId = 1, Shareable = false, RadioCompleteable = false, Comment = "Inactive kill collect talk example" });
            data.Objectives.Add(new MissionObjectiveDefinitionEntry { MissionId = 1994, ContentRevision = "example-ordinary", ObjectiveId = 1, Requirement = MissionContentRequirement.Optional, ClientNameTextId = 21313, ClientBodyTextId = 21314, Ordinal = 0, InitialState = 1, IsRequired = true, Comment = "Kill the bound creature" });
            data.Objectives.Add(new MissionObjectiveDefinitionEntry { MissionId = 1994, ContentRevision = "example-ordinary", ObjectiveId = 2, Requirement = MissionContentRequirement.Optional, ClientNameTextId = 21336, ClientBodyTextId = 21337, Ordinal = 1, InitialState = 4, IsRequired = true, Comment = "Collect the bound item class" });
            data.Objectives.Add(new MissionObjectiveDefinitionEntry { MissionId = 1994, ContentRevision = "example-ordinary", ObjectiveId = 3, Requirement = MissionContentRequirement.Optional, ClientNameTextId = 21615, ClientBodyTextId = 21616, Ordinal = 2, InitialState = 4, IsRequired = true, Comment = "Talk to the receiver" });
            data.Transitions.Add(new MissionObjectiveTransitionEntry { MissionId = 1994, ContentRevision = "example-ordinary", ObjectiveId = 1, TransitionId = 1, Requirement = MissionContentRequirement.Optional, Sequence = 1, FromState = 1, ToState = 2 });
            data.Transitions.Add(new MissionObjectiveTransitionEntry { MissionId = 1994, ContentRevision = "example-ordinary", ObjectiveId = 2, TransitionId = 1, Requirement = MissionContentRequirement.Optional, Sequence = 1, FromState = 1, ToState = 2 });
            data.Transitions.Add(new MissionObjectiveTransitionEntry { MissionId = 1994, ContentRevision = "example-ordinary", ObjectiveId = 3, TransitionId = 1, Requirement = MissionContentRequirement.Optional, Sequence = 1, FromState = 1, ToState = 2 });
            data.Triggers.Add(new MissionTriggerEntry { MissionId = 1994, ContentRevision = "example-ordinary", ObjectiveId = 1, TransitionId = 1, TriggerId = 1, Requirement = MissionContentRequirement.Optional, Kind = MissionTriggerKind.ProgressEvent, Sequence = 1, EventKind = 2, SubjectId = 510210, SourceSpawnResolved = true });
            data.Triggers.Add(new MissionTriggerEntry { MissionId = 1994, ContentRevision = "example-ordinary", ObjectiveId = 2, TransitionId = 1, TriggerId = 1, Requirement = MissionContentRequirement.Optional, Kind = MissionTriggerKind.ProgressEvent, Sequence = 1, EventKind = 4, SubjectId = 3147, InitialValue = 0, TargetValue = 1 });
            data.Triggers.Add(new MissionTriggerEntry { MissionId = 1994, ContentRevision = "example-ordinary", ObjectiveId = 3, TransitionId = 1, TriggerId = 1, Requirement = MissionContentRequirement.Optional, Kind = MissionTriggerKind.Conversation, Sequence = 1, NpcPackageId = 2561, PlayerFlagId = 1 });
            data.Actions.Add(new MissionActionEntry { MissionId = 1994, ContentRevision = "example-ordinary", ObjectiveId = 1, TransitionId = 1, ActionId = 1, Requirement = MissionContentRequirement.Optional, Kind = MissionActionKind.RevealObjective, Sequence = 1, TargetObjectiveId = 2 });
            data.Actions.Add(new MissionActionEntry { MissionId = 1994, ContentRevision = "example-ordinary", ObjectiveId = 1, TransitionId = 1, ActionId = 2, Requirement = MissionContentRequirement.Optional, Kind = MissionActionKind.ActivateObjective, Sequence = 2, TargetObjectiveId = 2, ObjectiveState = 1 });
            data.Actions.Add(new MissionActionEntry { MissionId = 1994, ContentRevision = "example-ordinary", ObjectiveId = 2, TransitionId = 1, ActionId = 1, Requirement = MissionContentRequirement.Optional, Kind = MissionActionKind.RevealObjective, Sequence = 1, TargetObjectiveId = 3 });
            data.Actions.Add(new MissionActionEntry { MissionId = 1994, ContentRevision = "example-ordinary", ObjectiveId = 2, TransitionId = 1, ActionId = 2, Requirement = MissionContentRequirement.Optional, Kind = MissionActionKind.ActivateObjective, Sequence = 2, TargetObjectiveId = 3, ObjectiveState = 1 });
            data.Rewards.Add(new MissionRewardDefinitionEntry { MissionId = 1994, ContentRevision = "example-ordinary", RewardId = 1, Requirement = MissionContentRequirement.Optional, Experience = 10, Credits = 0, Prestige = 0, SelectionCount = 0, Comment = "Synthetic example only" });
            data.Evidence.Add(new MissionEvidenceEntry { MissionId = 1994, ContentRevision = "example-ordinary", EvidenceId = 1, OwnerKind = MissionEvidenceOwnerKind.Mission, OwnerId = 1994, SourceKind = MissionEvidenceSourceKind.Reconstruction, SourceUri = "repository:docs/missions.md", Confidence = 0.0, ReconstructionNote = "Inactive server example; reuses reviewed client IDs, not a new native mission or translation." });
            return data;
        }

        internal static MissionSceneDefinition Escort() => new MissionSceneDefinition
            {
                Script = "example.escort",
                StateVersion = 1,
                Actors = new()
                {
                    ["guide"] = new SceneActorDefinition("guide", SceneActorKind.PublicSpawn, 510203),
                },
                Routes = new()
                {
                    ["outbound"] = new SceneRoute("outbound", new[] { new SceneWaypoint(new ScenePosition(398.0f, 120.1f, 150.0f)), new SceneWaypoint(new ScenePosition(400.0f, 120.0f, 150.0f), Orientation: 2.175) }, Speed: 6.5f),
                },
                Sequences = new()
                {
                },
                PublicEncounter = new PublicEncounterBinding(1990, 510203, "guide", "example.escort", OwnerLossPolicy: "Reset"),
            };
    }
}
