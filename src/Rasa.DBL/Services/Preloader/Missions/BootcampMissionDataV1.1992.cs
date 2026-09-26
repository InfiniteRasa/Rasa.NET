using System.Collections.Generic;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions
{
    public static partial class BootcampMissionDataV1
    {
        public static MissionSceneDefinition Mission1992() => new MissionSceneDefinition
            {
                Script = "bootcamp.gearing-up",
                StateVersion = 1,
                Actors = new()
                {
                    ["group-1-spawn-1-0"] = new SceneActorDefinition("group-1-spawn-1-0", SceneActorKind.Creature, 510211, Position: new ScenePosition(384.7f, 119.4f, 186.8f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 0, MissionId: 1992, GroupId: 1, SpawnId: 1),
                    ["group-2-spawn-1-0"] = new SceneActorDefinition("group-2-spawn-1-0", SceneActorKind.Creature, 510212, Position: new ScenePosition(389.7f, 119.4f, 186.8f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 0, MissionId: 1992, GroupId: 2, SpawnId: 1),
                    ["bootcamp-equipment-crate"] = new SceneActorDefinition("bootcamp-equipment-crate", SceneActorKind.Object, 29877, Position: new ScenePosition(398f, 122f, 173f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 200, LootMissionId: 1992, LootRewardId: 58, LootObjectiveId: 1, SharedKey: "bootcamp-equipment-crate", WindupMilliseconds: 100, MissionId: 0, SpawnId: 0),
                    ["practice-0"] = new SceneActorDefinition("practice-0", SceneActorKind.PracticeTarget, 29365, Position: new ScenePosition(386f, 120f, 184.7f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 97, SharedKey: "bootcamp-practice-0", MissionId: 0, SpawnId: 0),
                    ["practice-1"] = new SceneActorDefinition("practice-1", SceneActorKind.PracticeTarget, 29365, Position: new ScenePosition(380f, 120f, 186f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 97, SharedKey: "bootcamp-practice-1", MissionId: 0, SpawnId: 0),
                    ["practice-2"] = new SceneActorDefinition("practice-2", SceneActorKind.PracticeTarget, 29365, Position: new ScenePosition(375f, 120f, 186f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 97, SharedKey: "bootcamp-practice-2", MissionId: 0, SpawnId: 0),
                },
                Sequences = new()
                {
                    [1] = new()
                    {
                        World = new()
                        {
                            new EnsureActorIntent("sequence-1-step-1", "bootcamp-equipment-crate"),
                            new SetInteractionIntent("sequence-1-step-1-availability", "bootcamp-equipment-crate", true, IfPresent: false),
                        },
                    },
                    [2] = new()
                    {
                        World = new()
                        {
                            new SetInteractionIntent("sequence-2-step-1-bootcamp-equipment-crate", "bootcamp-equipment-crate", false, IfPresent: true),
                        },
                        Character = new()
                        {
                            new GrantAbilityIntent("sequence-2-step-3", 1, 1, 1, null),
                            new GrantAbilityIntent("sequence-2-step-4", 19, 1, 1, null),
                        },
                    },
                    [3] = new()
                    {
                        World = new()
                        {
                            new SetInteractionIntent("sequence-3-step-1-practice-0", "practice-0", true, IfPresent: true),
                            new SetInteractionIntent("sequence-3-step-1-practice-1", "practice-1", true, IfPresent: true),
                            new SetInteractionIntent("sequence-3-step-1-practice-2", "practice-2", true, IfPresent: true),
                        },
                    },
                    [4] = new()
                    {
                        World = new()
                        {
                            new PresentationIntent("sequence-4-step-2", PresentationKind.Tutorial, 10000015),
                            new PresentationIntent("sequence-4-step-2-audio", PresentationKind.Audio, 0),
                            new SetInteractionIntent("sequence-4-step-3-practice-0", "practice-0", true, IfPresent: true),
                            new SetInteractionIntent("sequence-4-step-3-practice-1", "practice-1", true, IfPresent: true),
                            new SetInteractionIntent("sequence-4-step-3-practice-2", "practice-2", true, IfPresent: true),
                        },
                        Character = new()
                        {
                            new GrantAbilityIntent("sequence-4-step-1", 49, 194, 1, Slot: 0),
                        },
                    },
                },
                Names = new() { ["crate"] = 1, ["loadout"] = 2, ["firearm"] = 3, ["lightning"] = 4 },
            };
    }
}
