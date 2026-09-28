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
        public static MissionExperienceDefinition Experience() => new()
        {
            Key = "bootcamp",
            Revision = "deployment_11",
            MapContextId = 1985,
            PrivatePerCharacter = true,
            Scene = new MissionSceneDefinition
            {
                Script = "bootcamp.experience",
                StateVersion = 1,
                Actors = new()
                {
                    ["bootcamp-equipment-crate"] = new SceneActorDefinition("bootcamp-equipment-crate", SceneActorKind.Object, 29877, Position: new ScenePosition(398f, 122f, 173f), Orientation: 0, InitiallyInteractable: false, InitialObjectState: 200, LootMissionId: 1992, LootRewardId: 58, LootObjectiveId: 1, SharedKey: "bootcamp-equipment-crate", WindupMilliseconds: 100, MissionId: 0, SpawnId: 0),
                    ["practice-0"] = new SceneActorDefinition("practice-0", SceneActorKind.PracticeTarget, 29365, Position: new ScenePosition(386f, 120f, 184.7f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 97, SharedKey: "bootcamp-practice-0", MissionId: 0, SpawnId: 0),
                    ["practice-1"] = new SceneActorDefinition("practice-1", SceneActorKind.PracticeTarget, 29365, Position: new ScenePosition(380f, 120f, 186f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 97, SharedKey: "bootcamp-practice-1", MissionId: 0, SpawnId: 0),
                    ["practice-2"] = new SceneActorDefinition("practice-2", SceneActorKind.PracticeTarget, 29365, Position: new ScenePosition(375f, 120f, 186f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 97, SharedKey: "bootcamp-practice-2", MissionId: 0, SpawnId: 0),
                    ["group-1-spawn-1-0"] = new SceneActorDefinition("group-1-spawn-1-0", SceneActorKind.Creature, 510213, Position: new ScenePosition(368f, 120.21479f, 158f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 0, SharedKey: "bootcamp-forean-1-0", MissionId: 1994, GroupId: 1, SpawnId: 1, FollowOffset: new ScenePosition(-1.5f, 0f, -2f)),
                    ["group-1-spawn-2-0"] = new SceneActorDefinition("group-1-spawn-2-0", SceneActorKind.Creature, 510214, Position: new ScenePosition(372f, 119.956856f, 158f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 0, SharedKey: "bootcamp-forean-2-0", MissionId: 1994, GroupId: 1, SpawnId: 2, FollowOffset: new ScenePosition(0f, 0f, -2f)),
                    ["group-1-spawn-3-0"] = new SceneActorDefinition("group-1-spawn-3-0", SceneActorKind.Creature, 510215, Position: new ScenePosition(374f, 119.74777f, 164f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 0, SharedKey: "bootcamp-forean-3-0", MissionId: 1994, GroupId: 1, SpawnId: 3, FollowOffset: new ScenePosition(1.5f, 0f, -2f)),
                    ["group-3-spawn-1-0"] = new SceneActorDefinition("group-3-spawn-1-0", SceneActorKind.Creature, 510207, Position: new ScenePosition(93.2f, 109.2201f, 137.5f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 0, SharedKey: "bootcamp-youngblood", MissionId: 1994, GroupId: 3, SpawnId: 1),
                    ["bootcamp-conrad-corpse"] = new SceneActorDefinition("bootcamp-conrad-corpse", SceneActorKind.Object, 24990, Position: new ScenePosition(-99f, 86.41823f, 74f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 200, SharedKey: "bootcamp-conrad-corpse", MissionId: 0, SpawnId: 0),
                    ["bootcamp-dropship-debris"] = new SceneActorDefinition("bootcamp-dropship-debris", SceneActorKind.Object, 24586, Position: new ScenePosition(-225f, 101.12099f, -71f), Orientation: 0, InitiallyInteractable: true, InitialObjectState: 31, SharedKey: "bootcamp-dropship-debris", WindupMilliseconds: 1400, MissionId: 0, SpawnId: 0),
                    ["mcallister"] = new SceneActorDefinition("mcallister", SceneActorKind.PublicSpawn, 510203, Orientation: 0, InitiallyInteractable: true, InitialObjectState: 0, MissionId: 0, SpawnId: 0),
                },
                Routes = new()
                {
                    ["mcallister-departure"] = new SceneRoute("mcallister-departure", new[] { new SceneWaypoint(new ScenePosition(400f, 120f, 150f), Orientation: 2.175, PauseMilliseconds: 0) }, Speed: 6.5f, ResumeAtDestination: true),
                },
                Sequences = new()
                {
                    [0] = new()
                    {
                        World = new()
                        {
                            new EnsureActorIntent("stage-equipment-crate", "bootcamp-equipment-crate"),
                            new EnsureActorIntent("stage-practice-0", "practice-0"),
                            new EnsureActorIntent("stage-practice-1", "practice-1"),
                            new EnsureActorIntent("stage-practice-2", "practice-2"),
                            new EnsureActorIntent("stage-group-1-spawn-1-0", "group-1-spawn-1-0"),
                            new EnsureActorIntent("stage-group-1-spawn-2-0", "group-1-spawn-2-0"),
                            new EnsureActorIntent("stage-group-1-spawn-3-0", "group-1-spawn-3-0"),
                        },
                    },
                    [1] = new()
                    {
                        World = new()
                        {
                            new EnsureActorIntent("ensure-mcallister", "mcallister"),
                            new RunRouteIntent("mcallister-departure", "mcallister", "mcallister-departure", StartWaypoint: 0),
                        },
                    },
                    [2] = new()
                    {
                        World = new()
                        {
                            new EnsureActorIntent("escort-ensure-group-1-spawn-1-0", "group-1-spawn-1-0"),
                            new FollowActorIntent("escort-start-group-1-spawn-1-0", "group-1-spawn-1-0", 0, Enabled: true),
                            new EnsureActorIntent("escort-ensure-group-1-spawn-2-0", "group-1-spawn-2-0"),
                            new FollowActorIntent("escort-start-group-1-spawn-2-0", "group-1-spawn-2-0", 0, Enabled: true),
                            new EnsureActorIntent("escort-ensure-group-1-spawn-3-0", "group-1-spawn-3-0"),
                            new FollowActorIntent("escort-start-group-1-spawn-3-0", "group-1-spawn-3-0", 0, Enabled: true),
                        },
                    },
                    [3] = new()
                    {
                        World = new()
                        {
                            new FollowActorIntent("escort-stop-group-1-spawn-1-0", "group-1-spawn-1-0", 0, Enabled: false),
                            new FollowActorIntent("escort-stop-group-1-spawn-2-0", "group-1-spawn-2-0", 0, Enabled: false),
                            new FollowActorIntent("escort-stop-group-1-spawn-3-0", "group-1-spawn-3-0", 0, Enabled: false),
                        },
                    },
                },
            },
            MissionTriggers = new()
            {
                new ExperienceMissionTrigger(1992, "Accepted", 1),
                new ExperienceMissionTrigger(1994, "Accepted", 2),
                new ExperienceMissionTrigger(1994, "Rewarded", 3),
            },
            ActorPolicies = new()
            {
                [510207] = new()
                {
                    Invulnerable = true,
                    DefenseRadius = 18f,
                    DefenseTargetTag = "bootcamp-thrax",
                    Tags = new string[] {  },
                    RewardScenarioKills = false,
                    TrackParticipation = false,
                },
                [510210] = new()
                {
                    Invulnerable = false,
                    DefenseRadius = 0f,
                    Tags = new string[] { "bootcamp-thrax" },
                    RewardScenarioKills = true,
                    TrackParticipation = true,
                    Loot = new AuthoredLootProfile(new[] { new LootDrop(41666, 100, 1, 1), new LootDrop(28, 55, 12, 24), new LootDrop(56, 30, 8, 16), new LootDrop(44917, 15, 1, 1), new LootDrop(41665, 25, 1, 1) }),
                },
                [510216] = new()
                {
                    Invulnerable = false,
                    DefenseRadius = 0f,
                    Tags = new string[] { "bootcamp-thrax" },
                    RewardScenarioKills = true,
                    TrackParticipation = true,
                    Loot = new AuthoredLootProfile(new[] { new LootDrop(41666, 100, 1, 1), new LootDrop(28, 55, 12, 24), new LootDrop(56, 30, 8, 16), new LootDrop(44917, 15, 1, 1), new LootDrop(41665, 25, 1, 1) }),
                },
                [510221] = new()
                {
                    Invulnerable = false,
                    DefenseRadius = 0f,
                    Tags = new string[] { "bootcamp-thrax" },
                    RewardScenarioKills = true,
                    TrackParticipation = true,
                    Loot = new AuthoredLootProfile(new[] { new LootDrop(41666, 100, 1, 1), new LootDrop(28, 55, 12, 24), new LootDrop(56, 30, 8, 16), new LootDrop(44917, 15, 1, 1), new LootDrop(41665, 25, 1, 1) }),
                },
                [510222] = new()
                {
                    Invulnerable = false,
                    DefenseRadius = 0f,
                    Tags = new string[] { "bootcamp-thrax" },
                    RewardScenarioKills = true,
                    TrackParticipation = true,
                    Loot = new AuthoredLootProfile(new[] { new LootDrop(41666, 100, 1, 1), new LootDrop(28, 55, 12, 24), new LootDrop(56, 30, 8, 16), new LootDrop(44917, 15, 1, 1), new LootDrop(41665, 25, 1, 1) }),
                },
                [510223] = new()
                {
                    Invulnerable = false,
                    DefenseRadius = 0f,
                    Tags = new string[] { "bootcamp-thrax" },
                    RewardScenarioKills = true,
                    TrackParticipation = true,
                    Loot = new AuthoredLootProfile(new[] { new LootDrop(41666, 100, 1, 1), new LootDrop(28, 55, 12, 24), new LootDrop(56, 30, 8, 16), new LootDrop(44917, 15, 1, 1), new LootDrop(41665, 25, 1, 1) }),
                },
                [510224] = new()
                {
                    Invulnerable = false,
                    DefenseRadius = 0f,
                    Tags = new string[] { "bootcamp-thrax" },
                    RewardScenarioKills = true,
                    TrackParticipation = true,
                    Loot = new AuthoredLootProfile(new[] { new LootDrop(41666, 100, 1, 1), new LootDrop(28, 55, 12, 24), new LootDrop(56, 30, 8, 16), new LootDrop(44917, 15, 1, 1), new LootDrop(41665, 25, 1, 1) }),
                },
                [510225] = new()
                {
                    Invulnerable = false,
                    DefenseRadius = 0f,
                    Tags = new string[] { "bootcamp-thrax" },
                    RewardScenarioKills = true,
                    TrackParticipation = true,
                    Loot = new AuthoredLootProfile(new[] { new LootDrop(41666, 100, 1, 1), new LootDrop(28, 55, 12, 24), new LootDrop(56, 30, 8, 16), new LootDrop(44917, 15, 1, 1), new LootDrop(41665, 25, 1, 1) }),
                },
                [510226] = new()
                {
                    Invulnerable = false,
                    DefenseRadius = 0f,
                    Tags = new string[] { "bootcamp-thrax" },
                    RewardScenarioKills = true,
                    TrackParticipation = true,
                    Loot = new AuthoredLootProfile(new[] { new LootDrop(41666, 100, 1, 1), new LootDrop(28, 55, 12, 24), new LootDrop(56, 30, 8, 16), new LootDrop(44917, 15, 1, 1), new LootDrop(41665, 25, 1, 1) }),
                },
            }
        };
    }
}
