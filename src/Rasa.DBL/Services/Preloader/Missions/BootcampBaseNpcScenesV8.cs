using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions
{
    /// <summary>
    /// The Proving Grounds' third lot of people (BootcampBaseNpcs), as far as the mission scenes
    /// have them:
    ///  - Gearing Up (1992) and the experience lose practice-1 and practice-2, the two practice
    ///    targets stood in the middle and west lanes of the range, in front of the target the
    ///    firing range soldiers' own model carries. practice-0, in the east lane, is the
    ///    player's: Gearing Up's objectives take a hit on any practice target (class 29365).
    ///  - Capture the Flag's escorts (1994 group 1, in the 1994 scene and the experience) are
    ///    three Infantrymen with pistols instead of three Forean initiates, standing where the
    ///    GM stood for them and facing his way; their creature rows (510213 to 510215) are
    ///    changed in place, so the scenes keep their roles, templates and follow offsets.
    ///  - Major McAllister's departure, when Gearing Up is accepted, ends on the command deck
    ///    where the GM stood for him, facing his way.
    /// </summary>
    public static class BootcampBaseNpcScenesV8
    {
        private const string Revision = BootcampMissionDataV1.Revision;

        public static readonly string[] RemovedPracticeTargets = { "practice-1", "practice-2" };

        /// <summary>The escorts' spots and facings, by spawn id.</summary>
        public static readonly (uint SpawnId, double X, double Y, double Z, double Facing)[] Escorts =
        {
            (1, 374.4727, 119.5273, 159.6797, 5.4425),
            (2, 374.5039, 119.5273, 160.4258, 5.422),
            (3, 372.6797, 119.5273, 166.4805, 5.254)
        };

        /// <summary>Where they stood (BootcampCaptureTheFlagContent).</summary>
        public static readonly (uint SpawnId, double X, double Y, double Z, double Facing)[] EscortsWere =
        {
            (1, 368, 120.21479, 158, 0),
            (2, 372, 119.956856, 158, 0),
            (3, 374, 119.74777, 164, 0)
        };

        public static readonly ScenePosition McAllisterEnd = new(400.2461f, 122f, 158.8594f);
        public const double McAllisterFacing = 0.4992;

        public static MissionSceneDefinition GearingUp()
        {
            var scene = GearingUpBefore();
            WithoutPracticeTargets(scene);
            return scene;
        }

        public static MissionSceneDefinition CaptureTheFlag()
        {
            var scene = CaptureTheFlagBefore();
            MoveEscorts(scene, Escorts);
            return scene;
        }

        public static MissionExperienceDefinition Experience()
        {
            var experience = ExperienceBefore();
            WithoutPracticeTargets(experience.Scene);
            MoveEscorts(experience.Scene, Escorts);

            var departure = experience.Scene.Routes["mcallister-departure"];
            experience.Scene.Routes["mcallister-departure"] = departure with
            {
                Points = new[] { new SceneWaypoint(McAllisterEnd, Orientation: McAllisterFacing, PauseMilliseconds: 0) }
            };
            return experience;
        }

        /// <summary>The scenes as they were: BootcampAudioAndCreditsV3's 1992 and 1994, BootcampCorpseDialogueV6's experience.</summary>
        public static MissionSceneDefinition GearingUpBefore()
        {
            var scene = BootcampMissionDataV1.Mission1992();
            scene.Audio = new MissionAudioDefinition { OfferAudioSetId = 2774 };
            return scene;
        }

        public static MissionSceneDefinition CaptureTheFlagBefore()
        {
            var scene = BootcampMissionDataV1.Mission1994();
            scene.Audio = new MissionAudioDefinition { OfferAudioSetId = 2775 };
            return scene;
        }

        public static MissionExperienceDefinition ExperienceBefore() => BootcampCorpseDialogueV6.Experience();

        private static void WithoutPracticeTargets(MissionSceneDefinition scene)
        {
            foreach (var role in RemovedPracticeTargets)
                scene.Actors.Remove(role);

            foreach (var sequence in scene.Sequences.Values)
                sequence.World?.RemoveAll(intent => RemovedPracticeTargets.Contains(intent.Role));
        }

        private static void MoveEscorts(MissionSceneDefinition scene, (uint SpawnId, double X, double Y, double Z, double Facing)[] spots)
        {
            foreach (var spot in spots)
            {
                var role = $"group-1-spawn-{spot.SpawnId}-0";
                scene.Actors[role] = scene.Actors[role] with
                {
                    Position = new ScenePosition((float)spot.X, (float)spot.Y, (float)spot.Z),
                    Orientation = spot.Facing
                };
            }
        }

        public static void Up(MigrationBuilder migration)
        {
            MissionDataMigration.UpdateScene(migration, 1992, Revision, GearingUp());
            MissionDataMigration.UpdateScene(migration, 1994, Revision, CaptureTheFlag());
            MissionDataMigration.UpdateExperience(migration, Experience());
            Spawns(migration, Escorts, "AFS escort");
        }

        public static void Down(MigrationBuilder migration)
        {
            Spawns(migration, EscortsWere, "Forean companions");
            MissionDataMigration.UpdateExperience(migration, ExperienceBefore());
            MissionDataMigration.UpdateScene(migration, 1994, Revision, CaptureTheFlagBefore());
            MissionDataMigration.UpdateScene(migration, 1992, Revision, GearingUpBefore());
        }

        private static void Spawns(MigrationBuilder migration, (uint SpawnId, double X, double Y, double Z, double Facing)[] spots, string comment)
        {
            foreach (var spot in spots)
                migration.Sql(System.FormattableString.Invariant(
                    $"update mission_spawn set pos_x = {spot.X}, pos_y = {spot.Y}, pos_z = {spot.Z}, rotation = {spot.Facing} ") +
                    $"where mission_id = 1994 and content_revision = '{Revision}' and spawn_group_id = 1 and spawn_id = {spot.SpawnId};");

            migration.Sql($"update mission_spawn_group set comment = '{comment}' " +
                $"where mission_id = 1994 and content_revision = '{Revision}' and spawn_group_id = 1;");
        }
    }
}
