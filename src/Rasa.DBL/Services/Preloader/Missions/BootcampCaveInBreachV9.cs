using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions
{
    /// <summary>
    /// The cave-in across the Proving Grounds' bridge (adv_bootcamp, map context 1985), and the
    /// Thrax who blow it during Capture the Flag (1994).
    ///
    /// The doorwell west of the bridge, the mouth of the cavern entrance (TerraForeasCavernEntrance24mv01
    /// at 288, 128, 64), is blocked by a pile of rocks: UsableInertDestTerraCaveinV02 (29434,
    /// terra_foreas_cavern_cavein_door_destroyable_v02), a door whose closed state (31) is the
    /// pile and whose opening (31 to 91) plays its explosion (animation 10, effect 49574,
    /// vfx_terra_foreas_cavern_cavein_explode_v02). It stands where the GM stood in the middle of
    /// the doorwell, facing as the cavern entrance does (map yaw -90, rotation 3 pi / 2): the
    /// two cavern models are one set.
    ///
    /// Nothing stands at the bridge before. When the player, on Capture the Flag with the
    /// objective "Find a way out of the cave" open (after the promotion), comes off the west end
    /// of the bridge (area 440), the rocks explode and two seconds later six Thrax Infantry
    /// Initiates (510216, the bridge Thrax there were before) come out of the doorwell and run
    /// for the checkpoint on the bridge, fighting what they meet on the way: the player, the
    /// escorts, the two Infantrymen at the sandbag post and the AFS soldiers on the bridge. One
    /// wave; they do not come again. "Find a way out of the cave" still completes in the
    /// doorwell (area 439), so the player goes through once the way is open.
    ///
    /// The trigger is a second transition of objective 2, from incomplete to incomplete (as
    /// 1995's dropship charge), whose action starts scenario 5, the 1994 scene's breach. Coming
    /// off the bridge again starts it again, but every effect of it is already applied.
    ///
    /// The experience stands the rocks up from its start (sequence 0) and again when Capture the
    /// Flag is accepted (sequence 2), for a bootcamp begun before the rocks were; the rocks are
    /// one object shared with the 1994 scene (SharedKey), so an exploded pile stays open. Once
    /// Capture the Flag is rewarded (sequence 3) the pile is set open (91) however it stood.
    /// </summary>
    public static class BootcampCaveInBreachV9
    {
        private const string Revision = BootcampMissionDataV1.Revision;

        public const string CaveIn = "bootcamp-cave-in";
        public const uint CaveInClass = 29434;
        public const uint Closed = 31;
        public const uint Open = 91;
        public static readonly ScenePosition CaveInPosition = new(275.2656f, 121.2305f, 64.3711f);
        public const double CaveInRotation = 4.7124;

        public const uint ThraxTemplate = 510216;
        public const uint BreachScenario = 5;
        public const uint WaveSequence = 6;
        public const uint WaveGroup = 4;
        public const uint WaveDelayMs = 2000;
        public const uint ExplosionWindupMs = 1000;

        /// <summary>The west end of the bridge (ArchForeanBridgeCeremonialV01, x 320.8 to 353.2): off it, on the floor before the sandbag post.</summary>
        public const uint BridgeArea = 440;
        public const double BridgeAreaX = 317, BridgeAreaY = 120.61, BridgeAreaZ = 65, BridgeAreaRadius = 7;

        /// <summary>Two rows of three in the doorwell, on its floor (the cavern entrance's, 120.9 to 121.9).</summary>
        public static readonly (double X, double Y, double Z)[] Wave =
        {
            (273.5, 121.85, 61.5), (273.5, 121.7, 64.5), (273.5, 121.65, 67.5),
            (276.5, 120.9, 61.5), (276.5, 120.9, 64.5), (276.5, 120.9, 67.5)
        };

        /// <summary>Out of the doorwell, past the sandbag post, onto the bridge where the AFS soldiers stand.</summary>
        public const string ChargeRoute = "breach-charge";
        public static readonly ScenePosition[] Charge =
        {
            new(300f, 120.61f, 64.5f), new(329f, 121.7f, 64.5f)
        };

        /// <summary>The Thrax that stood at the bridge from the start (BootcampCaptureTheFlagContent): pools 510218 to 510220, respawn 200 as they stood.</summary>
        public static readonly (uint Id, double X, double Y, double Z)[] BridgeThraxPools =
        {
            (510218, 318, 120.80204, 64), (510219, 318, 120.84154, 68), (510220, 317.24707, 121.662315, 71.811775)
        };

        public static SceneActorDefinition CaveInActor() =>
            new(CaveIn, SceneActorKind.Object, CaveInClass, CaveInPosition, CaveInRotation,
                InitiallyInteractable: false, InitialObjectState: Closed, SharedKey: CaveIn);

        public static string ThraxRole(int index) => $"breach-thrax-{index + 1}";

        public static MissionSceneDefinition CaptureTheFlag()
        {
            var scene = BootcampBaseNpcScenesV8.CaptureTheFlag();

            scene.Actors[CaveIn] = CaveInActor();
            scene.Routes[ChargeRoute] = new SceneRoute(ChargeRoute,
                Charge.Select(point => new SceneWaypoint(point)).ToArray(), Speed: 7);

            var wave = new SceneSequenceDefinition();
            for (var index = 0; index < Wave.Length; index++)
            {
                var role = ThraxRole(index);
                var (x, y, z) = Wave[index];
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, ThraxTemplate,
                    new ScenePosition((float)x, (float)y, (float)z), CaveInRotation,
                    MissionId: 1994, GroupId: WaveGroup, SpawnId: (uint)index + 1);
                wave.World.Add(new EnsureActorIntent($"breach-spawn-{role}", role));
                wave.World.Add(new RunRouteIntent($"breach-charge-{role}", role, ChargeRoute, ResumeAfterCombat: true));
            }

            scene.Sequences[BreachScenario] = new SceneSequenceDefinition
            {
                World = new()
                {
                    new EnsureActorIntent("breach-ensure-cave-in", CaveIn),
                    new TransitionObjectStateIntent("breach-explode-cave-in", CaveIn, Open, ExplosionWindupMs)
                },
                Timers = new() { new SequenceTimer("breach-wave", WaveDelayMs, WaveSequence) }
            };
            scene.Sequences[WaveSequence] = wave;
            scene.Names["breach"] = BreachScenario;
            scene.Names["breach-wave"] = WaveSequence;
            return scene;
        }

        public static MissionExperienceDefinition Experience()
        {
            var experience = BootcampBaseNpcScenesV8.Experience();
            var scene = experience.Scene;

            scene.Actors[CaveIn] = CaveInActor();
            scene.Sequences[0].World.Add(new EnsureActorIntent("stage-cave-in", CaveIn));
            scene.Sequences[2].World.Insert(0, new EnsureActorIntent("capture-stage-cave-in", CaveIn));
            scene.Sequences[3].World.Add(new SetInteractionIntent("capture-open-cave-in", CaveIn, false, ObjectState: Open, IfPresent: true));
            return experience;
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var pool in BridgeThraxPools)
                migration.Sql($"delete from spawnpool where id = {pool.Id} and map_context_id = 1985;");

            migration.Sql((
                "insert into mission_area (mission_id, content_revision, area_id, requirement, map_context_id, shape, pos_x, pos_y, pos_z, radius, comment) " +
                $"values (1994, '{Revision}', {BridgeArea}, 1, 1985, 1, {F(BridgeAreaX)}, {F(BridgeAreaY)}, {F(BridgeAreaZ)}, {F(BridgeAreaRadius)}, '1994 west end of the bridge');"));
            migration.Sql(
                "insert into mission_objective_transition (mission_id, content_revision, objective_id, transition_id, requirement, sequence, from_state, to_state, comment) " +
                $"values (1994, '{Revision}', 2, 2, 1, 2, 1, 1, 'Bridge crossed');");
            migration.Sql(
                "insert into mission_trigger (mission_id, content_revision, objective_id, transition_id, trigger_id, requirement, kind, sequence, area_id, comment) " +
                $"values (1994, '{Revision}', 2, 2, 1, 1, 4, 1, {BridgeArea}, 'Come off the west end of the bridge');");
            migration.Sql(
                "insert into mission_scenario (mission_id, content_revision, scenario_id, requirement, start_policy, name, comment) " +
                $"values (1994, '{Revision}', {BreachScenario}, 1, 1, 'bootcamp-1994-breach', '1994 cave-in breach');");
            migration.Sql(
                "insert into mission_action (mission_id, content_revision, objective_id, transition_id, action_id, comment, kind, requirement, scenario_id, sequence) " +
                $"values (1994, '{Revision}', 2, 2, 1, 'Blow the cave-in', 5, 1, {BreachScenario}, 1);");
            migration.Sql(
                "insert into mission_spawn_group (mission_id, content_revision, spawn_group_id, requirement, map_context_id, enabled, comment, spawn_policy) " +
                $"values (1994, '{Revision}', {WaveGroup}, 1, 1985, 0, 'Thrax through the cave-in', 1);");
            for (var index = 0; index < Wave.Length; index++)
            {
                var (x, y, z) = Wave[index];
                migration.Sql((
                    "insert into mission_spawn (mission_id, content_revision, spawn_group_id, spawn_id, creature_id, pos_x, pos_y, pos_z, rotation, quantity) " +
                    $"values (1994, '{Revision}', {WaveGroup}, {index + 1}, {ThraxTemplate}, {F(x)}, {F(y)}, {F(z)}, {F(CaveInRotation)}, 1);"));
            }

            MissionDataMigration.UpdateScene(migration, 1994, Revision, CaptureTheFlag());
            MissionDataMigration.UpdateExperience(migration, Experience());
        }

        public static void Down(MigrationBuilder migration)
        {
            MissionDataMigration.UpdateExperience(migration, BootcampBaseNpcScenesV8.Experience());
            MissionDataMigration.UpdateScene(migration, 1994, Revision, BootcampBaseNpcScenesV8.CaptureTheFlag());

            const string mission = "mission_id = 1994 and content_revision = '" + Revision + "'";
            migration.Sql($"delete from mission_spawn where {mission} and spawn_group_id = {WaveGroup};");
            migration.Sql($"delete from mission_spawn_group where {mission} and spawn_group_id = {WaveGroup};");
            migration.Sql($"delete from mission_action where {mission} and objective_id = 2 and transition_id = 2;");
            migration.Sql($"delete from mission_scenario where {mission} and scenario_id = {BreachScenario};");
            migration.Sql($"delete from mission_trigger where {mission} and objective_id = 2 and transition_id = 2;");
            migration.Sql($"delete from mission_objective_transition where {mission} and objective_id = 2 and transition_id = 2;");
            migration.Sql($"delete from mission_area where {mission} and area_id = {BridgeArea};");

            foreach (var pool in BridgeThraxPools)
                migration.Sql((
                    "insert into spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                    "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                    "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                    "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                    $"values ({pool.Id}, 0, 0, 200, {F(pool.X)}, {F(pool.Y)}, {F(pool.Z)}, -1.570796, 1985, {ThraxTemplate}, 1, 1, " +
                    "0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.0);"));
        }

        private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
