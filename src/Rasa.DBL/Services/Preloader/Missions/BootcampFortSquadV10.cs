using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions
{
    /// <summary>
    /// Denzil's Caldera Outpost, the first fort the player takes back in the Proving Grounds
    /// (adv_bootcamp, map context 1985), once Capture the Flag's Bane boss (Tizzik Gi) is dead.
    ///
    /// Captain Youngblood no longer comes alone. Seven seconds after the kill he comes in through
    /// the east gate (ArchHumOutpostFenceGateV02 at 132, 128 to 144: the way the player came up
    /// from the cave) with six soldiers, all of them set down together down the slope east of the
    /// gate, out of sight of the fort's floor, and running in:
    ///  - four Riflemen (8717), the AFS light soldier's model with the rifle the bridge soldiers
    ///    hold (27220, its shot 72003);
    ///  - a Shotgunner (8718), the same model with Weapon_Human_Redshirt_Shotgun_Physical (20560),
    ///    whose shot is WEAPON_ATTACK 121 (weaponclass), a short-range attack of its own;
    ///  - a Field Gunner (8720), the heavy soldier with the machine gun (20535, its shot 510219).
    /// Two Riflemen take the gate, either side of it inside the wall, and the Field Gunner the
    /// sandbag nest behind its south side; Youngblood stops where he stood before (93.2, 137.5),
    /// facing the boss's place in the middle of the fort, with a Rifleman either side of him and
    /// the Shotgunner behind. Each guards his post as Youngblood does (a defender of the Bane
    /// tagged bootcamp-thrax within 20 m of it), fights what comes at him on the way in, and
    /// stands where he stopped when the bootcamp map is made again.
    ///
    /// And from the kill on the fort spawns no more Bane: its seven pools (BootcampCombatContent:
    /// four about the boss's place, three inside the east gate) are quieted on the player's map,
    /// at the kill and again with Youngblood, who holds the fort for the rest of bootcamp, so a
    /// map made again never spawns them. What of them was standing is left to the squad. The
    /// Thrax outside the walls - at the west breach, down the road east - are as they were: they
    /// are the way on.
    ///
    /// The squad, like Youngblood, are the experience's actors (shared keys), so they stand there
    /// through the rest of bootcamp, as he does.
    /// </summary>
    public static class BootcampFortSquadV10
    {
        private const string Revision = BootcampMissionDataV1.Revision;

        public const string Youngblood = "group-3-spawn-1-0";
        public const uint SquadGroup = 5;
        public const float RunSpeed = 7f;
        public const float DefenseRadius = 20f;
        public const string BaneTag = "bootcamp-thrax";

        public const uint RiflemanTemplate = 510290, ShotgunnerTemplate = 510291, FieldGunnerTemplate = 510292;
        public const uint ShotgunAction = 510290;
        public const uint Rifle = 27220, Shotgun = 20560, MachineGun = 20535;
        public const uint RifleShot = 72003;
        public const uint LightSoldier = 29423, HeavySoldier = 29433;
        public const uint RiflemanName = 8717, ShotgunnerName = 8718, FieldGunnerName = 8720;

        /// <summary>The fort's own Bane pools: about the boss's place, and inside the east gate.</summary>
        public static readonly uint[] FortPools = { 510244, 510245, 510246, 510247, 510248, 510249, 510250 };

        public const double East = 4.7124, West = 1.5708, North = 3.1416, South = 0;

        /// <summary>Youngblood's way in: from the slope to where he stood before, facing the boss's place.</summary>
        public static readonly ScenePosition YoungbloodStart = new(167f, 106.09538f, 136f);
        public static readonly ScenePosition YoungbloodPost = new(93.2f, 109.2201f, 137.5f);
        public const double YoungbloodFacing = North;

        public sealed record Soldier(string Role, uint Template, uint SpawnId, ScenePosition Start, ScenePosition Post, double Facing, string Comment)
        {
            public string Route => $"fort-post-{Role}";
            public string SharedKey => $"bootcamp-{Role}";
        }

        /// <summary>
        /// Where each is set down (the column behind Youngblood on the slope, all on the checked-in
        /// mesh) and where each stands.
        /// </summary>
        public static readonly Soldier[] Squad =
        {
            new("fort-rifleman-1", RiflemanTemplate, 1, new(167f, 105.67804f, 138.5f), new(126f, 109.19544f, 141.5f), East, "Rifleman at the east gate, north side"),
            new("fort-rifleman-2", RiflemanTemplate, 2, new(168f, 104.78549f, 133.5f), new(126f, 109.19188f, 131f), East, "Rifleman at the east gate, south side"),
            new("fort-field-gunner", FieldGunnerTemplate, 3, new(171f, 103.27199f, 136f), new(118.5f, 109.181854f, 128.5f), 4.2053, "Field Gunner in the sandbags inside the east gate"),
            new("fort-rifleman-3", RiflemanTemplate, 4, new(169f, 104.96255f, 138.5f), new(88.5f, 109.20938f, 137.5f), West, "Rifleman at Youngblood's west"),
            new("fort-rifleman-4", RiflemanTemplate, 5, new(169f, 103.721664f, 133.5f), new(98f, 109.29778f, 137.5f), East, "Rifleman at Youngblood's east"),
            new("fort-shotgunner", ShotgunnerTemplate, 6, new(169f, 104.04952f, 136f), new(93.2f, 109.20427f, 133f), South, "Shotgunner behind Youngblood"),
        };

        public const string YoungbloodRoute = "fort-post-youngblood";

        public static MissionSceneDefinition CaptureTheFlag()
        {
            var scene = BootcampCaveInBreachV9.CaptureTheFlag();
            AddSquad(scene);

            // The kill: the fort is quiet from now. Youngblood holds it as he arrives.
            scene.Sequences[scene.Names["delay"]].World ??= new();
            scene.Sequences[scene.Names["delay"]].World.Add(new QuietSpawnPoolsIntent("fort-quiet-at-the-kill", Youngblood, FortPools));

            var arrival = scene.Sequences[scene.Names["arrival"]];
            var ensureYoungblood = arrival.World.FindIndex(intent => intent is EnsureActorIntent && intent.Role == Youngblood);
            var squad = new List<WorldIntent> { new RunRouteIntent("fort-youngblood-runs-in", Youngblood, YoungbloodRoute, ResumeAfterCombat: true) };
            foreach (var soldier in Squad)
            {
                squad.Add(new EnsureActorIntent($"fort-arrive-{soldier.Role}", soldier.Role));
                squad.Add(new RunRouteIntent($"fort-runs-in-{soldier.Role}", soldier.Role, soldier.Route, ResumeAfterCombat: true));
            }
            squad.Add(new QuietSpawnPoolsIntent("fort-quiet-while-held", Youngblood, FortPools));
            arrival.World.InsertRange(ensureYoungblood + 1, squad);
            return scene;
        }

        public static MissionExperienceDefinition Experience()
        {
            var experience = BootcampCaveInBreachV9.Experience();
            AddSquad(experience.Scene);
            foreach (var template in new[] { RiflemanTemplate, ShotgunnerTemplate, FieldGunnerTemplate })
                experience.ActorPolicies[template] = new ActorGameplayPolicy
                {
                    DefenseRadius = DefenseRadius,
                    DefenseTargetTag = BaneTag
                };
            return experience;
        }

        /// <summary>Youngblood set down on the slope with a way in, and the squad each with theirs.</summary>
        private static void AddSquad(MissionSceneDefinition scene)
        {
            scene.Actors[Youngblood] = scene.Actors[Youngblood] with { Position = YoungbloodStart, Orientation = West };
            scene.Routes[YoungbloodRoute] = Route(YoungbloodRoute, YoungbloodPost, YoungbloodFacing);
            foreach (var soldier in Squad)
            {
                scene.Actors[soldier.Role] = new SceneActorDefinition(soldier.Role, SceneActorKind.Creature, soldier.Template,
                    soldier.Start, West, SharedKey: soldier.SharedKey, MissionId: 1994, GroupId: SquadGroup, SpawnId: soldier.SpawnId);
                scene.Routes[soldier.Route] = Route(soldier.Route, soldier.Post, soldier.Facing);
            }
        }

        private static SceneRoute Route(string key, ScenePosition post, double facing) =>
            new(key, new[] { new SceneWaypoint(post, Orientation: facing) }, Speed: RunSpeed, ResumeAtDestination: true);

        public static void Up(MigrationBuilder migration)
        {
            migration.Sql(
                "insert into creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) " +
                $"values ({ShotgunAction}, 'Bootcamp fort Shotgunner shotgun', 1, 121, 0.5, 12, 1500, 0, 14, 22, 1);");
            migration.Sql(
                "insert into creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                "action1, action2, action3, action4, action5, action6, action7, action8) values " +
                $"({RiflemanTemplate}, 'Bootcamp fort Rifleman, Youngblood''s squad', {LightSoldier}, 1, 10, 1200, {RiflemanName}, 7, 2, {RifleShot}, 0, 0, 0, 0, 0, 0, 0), " +
                $"({ShotgunnerTemplate}, 'Bootcamp fort Shotgunner, Youngblood''s squad', {LightSoldier}, 1, 10, 1200, {ShotgunnerName}, 7, 2, {ShotgunAction}, 0, 0, 0, 0, 0, 0, 0), " +
                $"({FieldGunnerTemplate}, 'Bootcamp fort Field Gunner, Youngblood''s squad', {HeavySoldier}, 1, 10, 1200, {FieldGunnerName}, 7, 2, {BootcampSoldierWeapons.MachineGun}, 0, 0, 0, 0, 0, 0, 0);");
            migration.Sql(
                "insert into creature_stat (id, body, mind, spirit, health, armor) values " +
                $"({RiflemanTemplate}, 24, 24, 24, 1200, 200), ({ShotgunnerTemplate}, 24, 24, 24, 1200, 200), ({FieldGunnerTemplate}, 24, 24, 24, 1200, 200);");
            migration.Sql(
                "insert into creature_appearance (id, slot_id, class_id, color) values " +
                $"({RiflemanTemplate}, 13, {Rifle}, 1), ({ShotgunnerTemplate}, 13, {Shotgun}, 1), ({FieldGunnerTemplate}, 13, {MachineGun}, 1);");

            migration.Sql(
                "insert into mission_spawn_group (mission_id, content_revision, spawn_group_id, requirement, map_context_id, enabled, comment, spawn_policy) " +
                $"values (1994, '{Revision}', {SquadGroup}, 1, 1985, 0, 'Youngblood''s squad', 1);");
            foreach (var soldier in Squad)
                migration.Sql(
                    "insert into mission_spawn (mission_id, content_revision, spawn_group_id, spawn_id, creature_id, pos_x, pos_y, pos_z, rotation, quantity) " +
                    $"values (1994, '{Revision}', {SquadGroup}, {soldier.SpawnId}, {soldier.Template}, {F(soldier.Start.X)}, {F(soldier.Start.Y)}, {F(soldier.Start.Z)}, {F(West)}, 1);");

            migration.Sql(SceneSql(CaptureTheFlag()));
            migration.Sql(ExperienceSql(Experience()));
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql(ExperienceSql(BootcampCaveInBreachV9.Experience()));
            migration.Sql(SceneSql(BootcampCaveInBreachV9.CaptureTheFlag()));

            const string mission = "mission_id = 1994 and content_revision = '" + Revision + "'";
            migration.Sql($"delete from mission_spawn where {mission} and spawn_group_id = {SquadGroup};");
            migration.Sql($"delete from mission_spawn_group where {mission} and spawn_group_id = {SquadGroup};");
            migration.Sql($"delete from creature_appearance where id between {RiflemanTemplate} and {FieldGunnerTemplate};");
            migration.Sql($"delete from creature_stat where id between {RiflemanTemplate} and {FieldGunnerTemplate};");
            migration.Sql($"delete from creature where id between {RiflemanTemplate} and {FieldGunnerTemplate};");
            migration.Sql($"delete from creature_action where id = {ShotgunAction};");
        }

        /// <summary>
        /// The bindings as plain SQL, the migration having no model of its own to write them through
        /// (MissionDataMigration's UpdateData needs one): numbers and plain names only, nothing a SQL
        /// string of either provider needs escaped.
        /// </summary>
        internal static string SceneSql(MissionSceneDefinition scene) =>
            $"update mission_scene_binding set script_key = '{scene.Script}', state_version = {scene.StateVersion}, bindings = '{Plain(scene)}' " +
            $"where mission_id = 1994 and content_revision = '{Revision}';";

        internal static string ExperienceSql(MissionExperienceDefinition experience) =>
            $"update mission_experience_binding set map_context_id = {experience.MapContextId}, enabled = 1, bindings = '{Plain(experience)}' " +
            $"where experience_key = '{experience.Key}';";

        private static string Plain<T>(T document)
        {
            var json = JsonSerializer.Serialize(document, MissionContentCodec.Options);
            if (json.Contains('\'') || json.Contains('\\'))
                throw new InvalidOperationException("A Proving Grounds binding cannot be written as a plain SQL string.");
            return json;
        }

        private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
