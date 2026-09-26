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
        public static MissionSceneDefinition Mission2005() => new MissionSceneDefinition
            {
                Script = "bootcamp.bomb-retry",
                StateVersion = 1,
                Actors = new()
                {
                    ["group-1-spawn-1-0"] = new SceneActorDefinition("group-1-spawn-1-0", SceneActorKind.Creature, 39, Position: new ScenePosition(-218f, 101.08475f, -78f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 0, MissionId: 2005, GroupId: 1, SpawnId: 1),
                    ["group-1-spawn-2-0"] = new SceneActorDefinition("group-1-spawn-2-0", SceneActorKind.Creature, 50, Position: new ScenePosition(-221f, 101.2538f, -74f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 0, MissionId: 2005, GroupId: 1, SpawnId: 2),
                    ["group-2-spawn-1-0"] = new SceneActorDefinition("group-2-spawn-1-0", SceneActorKind.Creature, 510209, Position: new ScenePosition(-223f, 101.269646f, -76f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 0, MissionId: 2005, GroupId: 2, SpawnId: 1),
                    ["bootcamp-dropship-debris"] = new SceneActorDefinition("bootcamp-dropship-debris", SceneActorKind.Object, 24586, Position: new ScenePosition(-225f, 101.12099f, -71f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 31, SharedKey: "bootcamp-dropship-debris", WindupMilliseconds: 1400, MissionId: 0, SpawnId: 0),
                },
                Sequences = new()
                {
                    [1] = new()
                    {
                        World = new()
                        {
                            new SetInteractionIntent("sequence-1-step-2-bootcamp-dropship-debris", "bootcamp-dropship-debris", false, IfPresent: true),
                        },
                        Character = new()
                        {
                            new MissionDeadlineIntent("sequence-1-step-1", 2005, DeadlineIntentKind.Satisfy, 0),
                        },
                        Timers = new()
                        {
                            new SequenceTimer("sequence-1-step-3", 5000, 2, ClockPolicy: SceneClockPolicy.WallClock, Cancel: false),
                        },
                    },
                    [2] = new()
                    {
                        Character = new()
                        {
                            new ObjectiveIntent("sequence-2-step-1", 2005, 1, MissionObjectiveState.Completed),
                        },
                        Timers = new()
                        {
                            new SequenceTimer("sequence-2-step-2", 2000, 3, ClockPolicy: SceneClockPolicy.WallClock, Cancel: false),
                        },
                    },
                    [3] = new()
                    {
                        World = new()
                        {
                            new EnsureActorIntent("sequence-3-step-1-group-1-spawn-1-0", "group-1-spawn-1-0"),
                            new EnsureActorIntent("sequence-3-step-1-group-1-spawn-2-0", "group-1-spawn-2-0"),
                            new EnsureActorIntent("sequence-3-step-2-group-2-spawn-1-0", "group-2-spawn-1-0"),
                        },
                        Character = new()
                        {
                            new ObjectiveIntent("sequence-3-step-3", 2005, 4, MissionObjectiveState.NotAssigned),
                            new ObjectiveIntent("sequence-3-step-4", 2005, 4, MissionObjectiveState.Incomplete),
                        },
                    },
                    [4] = new()
                    {
                        World = new()
                        {
                            new SetInteractionIntent("sequence-4-step-1-bootcamp-dropship-debris", "bootcamp-dropship-debris", true, IfPresent: true),
                            new RemoveActorIntent("sequence-4-step-4-group-1-spawn-1-0", "group-1-spawn-1-0"),
                            new RemoveActorIntent("sequence-4-step-4-group-1-spawn-2-0", "group-1-spawn-2-0"),
                            new RemoveActorIntent("sequence-4-step-4-group-2-spawn-1-0", "group-2-spawn-1-0"),
                        },
                        Timers = new()
                        {
                            new SequenceTimer("sequence-1-step-3", 0, 2, ClockPolicy: SceneClockPolicy.WallClock, Cancel: true),
                            new SequenceTimer("sequence-2-step-2", 0, 3, ClockPolicy: SceneClockPolicy.WallClock, Cancel: true),
                        },
                    },
                    [5] = new()
                    {
                        World = new()
                        {
                            new TransferIntent("sequence-5-step-1", 1220, new ScenePosition(884.11f, 305.8f, 347.81f), 1.5613),
                        },
                        Character = new()
                        {
                            new SetQualificationIntent("sequence-5-step-2", 1, true),
                            new SetEntitlementIntent("sequence-5-step-3", true),
                        },
                    },
                    [0] = new()
                    {
                        World = new()
                        {
                            new EnsureActorIntent("retry-wreck", "bootcamp-dropship-debris"),
                            new SetInteractionIntent("retry-enable-wreck", "bootcamp-dropship-debris", true, IfPresent: false),
                        },
                    },
                },
                Names = new() { ["fuse"] = 1, ["detonation"] = 2, ["exit"] = 3, ["reset"] = 4, ["transfer"] = 5 },
                Requirement = new NotRequirement(new CustomRequirement("character.starting-experience-completed")),
            };
    }
}
