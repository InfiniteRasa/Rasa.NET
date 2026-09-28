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
        public static MissionSceneDefinition Mission1994() => new MissionSceneDefinition
            {
                Script = "bootcamp.capture-the-flag",
                StateVersion = 1,
                Actors = new()
                {
                    ["group-1-spawn-1-0"] = new SceneActorDefinition("group-1-spawn-1-0", SceneActorKind.Creature, 510213, Position: new ScenePosition(368f, 120.21479f, 158f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 0, SharedKey: "bootcamp-forean-1-0", MissionId: 1994, GroupId: 1, SpawnId: 1, FollowOffset: new ScenePosition(-1.5f, 0f, -2f)),
                    ["group-1-spawn-2-0"] = new SceneActorDefinition("group-1-spawn-2-0", SceneActorKind.Creature, 510214, Position: new ScenePosition(372f, 119.956856f, 158f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 0, SharedKey: "bootcamp-forean-2-0", MissionId: 1994, GroupId: 1, SpawnId: 2, FollowOffset: new ScenePosition(0f, 0f, -2f)),
                    ["group-1-spawn-3-0"] = new SceneActorDefinition("group-1-spawn-3-0", SceneActorKind.Creature, 510215, Position: new ScenePosition(374f, 119.74777f, 164f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 0, SharedKey: "bootcamp-forean-3-0", MissionId: 1994, GroupId: 1, SpawnId: 3, FollowOffset: new ScenePosition(1.5f, 0f, -2f)),
                    ["group-2-spawn-1-0"] = new SceneActorDefinition("group-2-spawn-1-0", SceneActorKind.Creature, 510210, Position: new ScenePosition(95.1f, 109.577324f, 150.8f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 0, MissionId: 1994, GroupId: 2, SpawnId: 1),
                    ["group-3-spawn-1-0"] = new SceneActorDefinition("group-3-spawn-1-0", SceneActorKind.Creature, 510207, Position: new ScenePosition(93.2f, 109.2201f, 137.5f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 0, SharedKey: "bootcamp-youngblood", MissionId: 1994, GroupId: 3, SpawnId: 1),
                },
                Sequences = new()
                {
                    [1] = new()
                    {
                        Character = new()
                        {
                            new GrantRewardIntent("sequence-1-step-1", 1994, 59),
                        },
                    },
                    [2] = new()
                    {
                        World = new()
                        {
                            new EnsureActorIntent("sequence-2-step-1-group-1-spawn-1-0", "group-1-spawn-1-0"),
                            new EnsureActorIntent("sequence-2-step-1-group-1-spawn-2-0", "group-1-spawn-2-0"),
                            new EnsureActorIntent("sequence-2-step-1-group-1-spawn-3-0", "group-1-spawn-3-0"),
                            new FollowActorIntent("sequence-2-step-2-group-1-spawn-1-0", "group-1-spawn-1-0", 0, Enabled: true),
                            new FollowActorIntent("sequence-2-step-2-group-1-spawn-2-0", "group-1-spawn-2-0", 0, Enabled: true),
                            new FollowActorIntent("sequence-2-step-2-group-1-spawn-3-0", "group-1-spawn-3-0", 0, Enabled: true),
                            new EnsureActorIntent("sequence-2-step-3-group-2-spawn-1-0", "group-2-spawn-1-0"),
                        },
                    },
                    [3] = new()
                    {
                        Timers = new()
                        {
                            new SequenceTimer("sequence-3-step-1", 7000, 4, ClockPolicy: SceneClockPolicy.WallClock, Cancel: false),
                        },
                    },
                    [4] = new()
                    {
                        World = new()
                        {
                            new EnsureActorIntent("sequence-4-step-1-group-3-spawn-1-0", "group-3-spawn-1-0"),
                        },
                        Character = new()
                        {
                            new ObjectiveIntent("sequence-4-step-2", 1994, 3, MissionObjectiveState.NotAssigned),
                            new ObjectiveIntent("sequence-4-step-3", 1994, 3, MissionObjectiveState.Incomplete),
                        },
                    },
                },
                Names = new() { ["promotion"] = 1, ["assault"] = 2, ["delay"] = 3, ["arrival"] = 4 },
            };
    }
}
