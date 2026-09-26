using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions
{
    public static class BootcampExtractionDataV5
    {
        public const uint SoldierTemplate = 510227;
        public const uint AssaultTemplate = 510228;
        private const string Wreck = "bootcamp-dropship-debris";
        private const string Evacuation = "bootcamp-evacuation-ship";
        private const string Revision = BootcampMissionDataV1.Revision;
        private static readonly ScenePosition[] AssaultPositions =
        {
            new(-159, 85.079155f, 8), new(-155, 84.88563f, 8), new(-151, 84.88554f, 8),
            new(-155, 84.88563f, 10), new(-154, 84.866875f, 12), new(-150, 84.89034f, 12)
        };

        public static MissionSceneDefinition Scene(uint missionId)
        {
            var retry = missionId == 2005;
            if (missionId != 1995 && !retry)
                throw new ArgumentOutOfRangeException(nameof(missionId));
            var scene = retry ? BootcampFinaleDataV2.Retry() : BootcampFinaleDataV2.Reinforcements();
            if (!retry)
                scene.Audio = new MissionAudioDefinition { OfferAudioSetId = 2776 };
            var arrivalId = scene.Names["exit"];
            var resetId = scene.Names["reset"];
            var van = retry ? "group-2-spawn-1-0" : "group-3-spawn-1-0";
            var arrival = scene.Sequences[arrivalId];
            var checkIn = arrival.Character.ToList();
            scene.Actors[Evacuation] = EvacuationActor();
            scene.DefeatSequences = new();
            scene.Names["assault"] = 8;
            scene.Names["assault-cleared"] = 9;
            scene.Sequences[8] = new();
            scene.Sequences[9] = new()
            {
                World = new() { new SetInteractionIntent("unlock-van-check-in", van, true) },
                Character = checkIn
            };
            arrival.Character.Clear();
            arrival.World = new()
            {
                new RemoveActorIntent("clear-detonated-wreck", Wreck),
                new EnsureActorIntent("arrive-evacuation-ship", Evacuation),
                new TransitionObjectStateIntent("activate-arrival-beam", Evacuation, 56),
                new EnsureActorIntent("arrive-van-valkenberg", van),
                new SetInteractionIntent("lock-van-during-assault", van, false)
            };
            scene.Sequences[resetId].World.Add(new RemoveActorIntent("reset-evacuation-ship", Evacuation));

            var defenders = new[] { new ScenePosition(-218, 101.08475f, -78), new ScenePosition(-221, 101.2538f, -74) };
            for (var index = 0; index < defenders.Length; index++)
            {
                var role = $"reinforcement-{index + 1}";
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, SoldierTemplate,
                    defenders[index], MissionId: missionId, GroupId: retry ? 1U : 2U, SpawnId: (uint)index + 1);
                arrival.World.Add(new EnsureActorIntent($"arrive-{role}", role));
                scene.Sequences[resetId].World.Add(new RemoveActorIntent($"reset-{role}", role));
            }
            scene.Routes["assault-uphill"] = new SceneRoute("assault-uphill",
                new[] { new SceneWaypoint(new ScenePosition(-218, 101.08475f, -78)) }, Speed: 7);
            for (var index = 0; index < AssaultPositions.Length; index++)
            {
                var role = $"assault-{index + 1}";
                var defeated = (uint)(20 + index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, AssaultTemplate,
                    AssaultPositions[index], MissionId: missionId, GroupId: retry ? 3U : 4U, SpawnId: (uint)index + 1);
                scene.DefeatSequences[role] = defeated;
                scene.Sequences[defeated] = new();
                scene.Sequences[8].World.Add(new EnsureActorIntent($"spawn-{role}", role));
                scene.Sequences[8].World.Add(new RunRouteIntent($"advance-{role}", role, "assault-uphill",
                    ResumeAfterCombat: true));
                scene.Sequences[resetId].World.Add(new RemoveActorIntent($"reset-{role}", role));
            }
            return scene;
        }

        public static MissionExperienceDefinition Experience()
        {
            var experience = PreviousExperience();
            experience.Scene.Actors[Evacuation] = EvacuationActor();
            experience.Scene.Sequences[4].World.Add(
                new TransitionObjectStateIntent("activate-ready-evacuation-beam", Evacuation, 56));
            experience.ActorPolicies[SoldierTemplate] = new ActorGameplayPolicy
            {
                DefenseRadius = 40, DefenseTargetTag = "bootcamp-assault"
            };
            experience.ActorPolicies[510209] = new ActorGameplayPolicy { Invulnerable = true };
            experience.ActorPolicies[AssaultTemplate] = new ActorGameplayPolicy
            {
                Tags = new[] { "bootcamp-assault", "bootcamp-thrax" },
                RewardScenarioKills = true, TrackParticipation = true,
                Loot = experience.ActorPolicies[510226].Loot
            };
            return experience;
        }

        private static SceneActorDefinition EvacuationActor() =>
            new(Evacuation, SceneActorKind.Object, 10516, new ScenePosition(-225, 101.12099f, -71),
                InitiallyInteractable: false, InitialObjectState: 55, SharedKey: Evacuation);

        private static MissionExperienceDefinition PreviousExperience()
        {
            var experience = BootcampFinaleDataV2.Experience();
            experience.Scene.Sequences[4].World.Insert(0,
                new RemoveActorIntent("clear-ready-evacuation-pad", Wreck));
            return experience;
        }

        public static void Up(MigrationBuilder migration)
        {
            migration.Sql(
                "insert into creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                "action1, action2, action3, action4, action5, action6, action7, action8) values " +
                "(510227, 'Bootcamp extraction AFS reinforcement', 29423, 1, 8, 1200, 0, 7, 2, 2, 0, 0, 0, 0, 0, 0, 0), " +
                "(510228, 'Bootcamp extraction Thrax assault', 29769, 0, 13, 780, 7674, 7, 2, 510226, 0, 0, 0, 0, 0, 0, 0);");
            migration.Sql(
                "insert into creature_stat (id, body, mind, spirit, health, armor) values " +
                "(510227, 24, 24, 24, 1200, 200), (510228, 39, 39, 39, 780, 156);");
            migration.Sql(
                "insert into creature_appearance (id, slot_id, class_id, color) values " +
                "(510227, 13, 27220, 1), (510228, 13, 29884, 1);");
            foreach (var mission in new[] { 1995U, 2005U })
            {
                var scene = Scene(mission);
                var group = mission == 1995 ? 4U : 3U;
                migration.InsertData("mission_spawn_group",
                    new[] { "mission_id", "content_revision", "spawn_group_id", "requirement", "map_context_id",
                        "enabled", "spawn_policy", "comment" },
                    new object[] { mission, Revision, group,
                        (byte)(mission == 1995 ? MissionContentRequirement.Required : MissionContentRequirement.Optional), 1985U,
                        false, (byte)1, "Finite extraction assault" });
                foreach (var actor in scene.Actors.Values.Where(actor => actor.TemplateId == AssaultTemplate))
                    migration.InsertData("mission_spawn",
                        new[] { "mission_id", "content_revision", "spawn_group_id", "spawn_id", "creature_id",
                            "pos_x", "pos_y", "pos_z", "rotation", "quantity" },
                        new object[] { mission, Revision, group, actor.SpawnId, AssaultTemplate,
                            (double)actor.Position.X, (double)actor.Position.Y, (double)actor.Position.Z, 0.0, 1U });
                migration.Sql(
                    $"update mission_spawn set creature_id = {SoldierTemplate} where mission_id = {mission} " +
                    $"and content_revision = '{Revision}' and spawn_group_id = {(mission == 1995 ? 2 : 1)};");
                MissionDataMigration.UpdateScene(migration, mission, Revision, scene);
            }
            MissionDataMigration.UpdateExperience(migration, Experience());
        }

        public static void Down(MigrationBuilder migration)
        {
            foreach (var mission in new[] { 1995U, 2005U })
            {
                var group = mission == 1995 ? 4U : 3U;
                migration.Sql($"delete from mission_spawn where mission_id = {mission} and content_revision = '{Revision}' and spawn_group_id = {group};");
                migration.DeleteData("mission_spawn_group", new[] { "mission_id", "content_revision", "spawn_group_id" },
                    new object[] { mission, Revision, group });
                migration.Sql(
                    $"update mission_spawn set creature_id = case when spawn_id = 1 then 39 else 50 end " +
                    $"where mission_id = {mission} and content_revision = '{Revision}' and spawn_group_id = {(mission == 1995 ? 2 : 1)};");
                var previous = mission == 1995 ? BootcampFinaleDataV2.Reinforcements() : BootcampFinaleDataV2.Retry();
                if (mission == 1995)
                    previous.Audio = new MissionAudioDefinition { OfferAudioSetId = 2776 };
                MissionDataMigration.UpdateScene(migration, mission, Revision, previous);
            }
            MissionDataMigration.UpdateExperience(migration, PreviousExperience());
            migration.Sql("delete from creature_appearance where id in (510227, 510228);");
            migration.Sql("delete from creature_stat where id in (510227, 510228);");
            migration.Sql("delete from creature where id in (510227, 510228);");
        }
    }
}
