using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    /// <summary>
    /// The Ranja egg clusters of Sacs And Violence (860) in their idle state, and the Fithik that
    /// hatch from them (Ranja_egg_clusters_hatch; the hatching itself is the server's,
    /// FithikEggClusters).
    ///
    /// The clusters (UsableCrSpawnerDestFithikEggClusterV01, 10180) were set down in state 0,
    /// which a creature spawner does not have, so the client played nothing on them. Their idle
    /// state is USE_CS_STATE_IDLE (187), whose animation (Usable Active,
    /// arch_forean_fithiceggcluster_idle_v01) is the cluster pulsing.
    ///
    /// The hatchling is the client's own egg-cluster Fithik, Bane_Fithik_Wingless_EggCluster
    /// (21499): CREATURE_BIRTH at that class (155/21499) plays "Creature Birth - Fithik Egg
    /// Cluster", 7.2 s of it climbing out. It bites with WEAPON_MELEE 34
    /// (Weapon_Creature_Fithik), which its skeleton (14000) animates. Level, health and damage are
    /// ours: a little under the mission's 10 and its Fithik Hive Monarch (89), as three come out
    /// at once.
    /// </summary>
    public static class RanjaEggClusterHatching
    {
        public const uint MissionId = 860;
        public const uint IdleState = 187;              // USE_CS_STATE_IDLE

        public const uint HatchlingCreatureId = 552001;
        public const uint HatchlingClass = 21499;       // Bane_Fithik_Wingless_EggCluster
        public const uint HatchlingAction = 73101;
        public const uint HatchlingLevel = 8;
        public const uint HatchlingHealth = 400;

        /// <summary>The scene as WildernessRanjaGorge left it, with each cluster in its idle state.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = WildernessRanjaGorgeV1.EggClusters(WildernessRanjaGorgeV1.ProvisionalEggClusters());

            foreach (var role in scene.Actors.Keys.ToList())
                scene.Actors[role] = scene.Actors[role] with { InitialObjectState = IdleState };

            return scene;
        }

        public static void Up(MigrationBuilder migration)
        {
            migration.Sql(
                "insert into creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) " +
                $"values ({HatchlingAction}, 'Fithik Hatchling weapon 174/34', 174, 34, 0.5, 3.0, 1500, 0, 8, 12, 1);");
            migration.Sql(
                "insert into creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8) " +
                $"values ({HatchlingCreatureId}, 'Fithik Hatchling - Ranja egg clusters', {HatchlingClass}, 0, {HatchlingLevel}, {HatchlingHealth}, 0, 9, 5, {HatchlingAction}, 0, 0, 0, 0, 0, 0, 0);");
            migration.Sql(
                "insert into creature_stat (id, body, mind, spirit, health, armor) " +
                $"values ({HatchlingCreatureId}, 15, 15, 15, {HatchlingHealth}, 50);");

            MissionDataMigration.UpdateScene(migration, MissionId, WildernessMissionDataV1.Revision, Scene());
        }

        public static void Down(MigrationBuilder migration)
        {
            MissionDataMigration.UpdateScene(migration, MissionId, WildernessMissionDataV1.Revision,
                WildernessRanjaGorgeV1.EggClusters(WildernessRanjaGorgeV1.ProvisionalEggClusters()));

            migration.Sql($"delete from creature_stat where id = {HatchlingCreatureId};");
            migration.Sql($"delete from creature where id = {HatchlingCreatureId};");
            migration.Sql($"delete from creature_action where id = {HatchlingAction};");
        }
    }
}
