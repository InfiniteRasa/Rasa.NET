using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The first NPCs placed at a post with a pose (spawnpool_pose): four at the Proving Grounds'
    /// refugee base (adv_bootcamp, map context 1985), each where a GM stood and faced, read off
    /// the server's .where line.
    ///
    ///  - 400001, a Forean Warrior standing guard, his spear in his left hand. Redshirt_Forean_Spearman
    ///    (class 6043): the client's own name for the class is "Forean Warrior", so the row has no
    ///    name id. The spear is Weapon_Creature_Forean_Spear (6042), whose model attaches to the
    ///    left hand drawn or put away.
    ///  - 400002, an Infantryman standing with his arms at his sides and no weapon.
    ///    Redshirt_Human_Soldier_Light_Male (29423), the soldier boot camp's bridge squad is, with
    ///    no weapon row; creaturenamelanguage 8716, "Infantryman".
    ///  - 400003, an Infantryman with a rifle: the same, with the bridge squad's rifle (27220).
    ///    Two stand side by side holding it across the chest, which is the weapon-out pose: the
    ///    client's "Weapon - Rifle - Idle".
    ///
    /// None has an attack or a speed: they stand where they are put, as the base's staff do, and
    /// are safe ground. Level 5, the level of the base's medic.
    ///
    /// Ids from 400001, under the 500000 the generated pools start at: these are entered by
    /// hand, at a height a GM stood at, and each keeps it. With no speed they stand exactly on
    /// their pool's point and are not put on the navmesh (the game server's
    /// BehaviorManager.NeverMoves, SpawnPoolManager.SpawnPoint).
    ///
    /// Poses are the game server's NpcPose: 1 standing at its post, 2 with its weapon out.
    /// </summary>
    public static class BootcampPostedNpcs
    {
        public const uint ForeanWarriorId = 400001;
        public const uint InfantrymanId = 400002;
        public const uint InfantrymanWithRifleId = 400003;

        public const uint ForeanWarriorPoolId = 400001;
        public const uint InfantrymanPoolId = 400002;
        public const uint RiflemanWestPoolId = 400003;
        public const uint RiflemanEastPoolId = 400004;

        public const uint FirstId = 400001;
        public const uint LastId = 400004;

        public const uint BootcampMapContextId = 1985;

        public const byte Standing = 1;
        public const byte WeaponOut = 2;
    }

    public class BootcampPostedNpcCreaturePreloader : PreloaderBase, IPreloader
    {
        private static readonly string[] Columns =
        {
            "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed",
            "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8"
        };

        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureEntry.TableName, Columns);
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { BootcampPostedNpcs.ForeanWarriorId, "Forean Warrior at a post", 6043, 1, 5, 600, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { BootcampPostedNpcs.InfantrymanId, "Infantryman at a post, unarmed", 29423, 1, 5, 350, 8716, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { BootcampPostedNpcs.InfantrymanWithRifleId, "Infantryman at a post, rifle", 29423, 1, 5, 350, 8716, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        }
    }

    /// <summary>What each holds: the spear, nothing, the rifle. The two soldier classes are whole models and wear nothing else.</summary>
    public class BootcampPostedNpcAppearancePreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureAppearanceEntry.TableName, new[] { "id", "slot_id", "Class_id", "color" });
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { BootcampPostedNpcs.ForeanWarriorId, 13, 6042, 1 };
            yield return new object[] { BootcampPostedNpcs.InfantrymanWithRifleId, 13, 27220, 1 };
        }
    }

    public class BootcampPostedNpcStatsPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureStatEntry.TableName, new[] { "id", "body", "mind", "spirit", "health", "armor" });
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { BootcampPostedNpcs.ForeanWarriorId, 15, 15, 15, 600, 60 };
            yield return new object[] { BootcampPostedNpcs.InfantrymanId, 12, 12, 12, 350, 60 };
            yield return new object[] { BootcampPostedNpcs.InfantrymanWithRifleId, 12, 12, 12, 350, 60 };
        }
    }

    /// <summary>One pool each, one creature, on the spot and facing the way the .where line gave; respawn 20 s, as the base's staff.</summary>
    public class BootcampPostedNpcSpawnpoolPreloader : PreloaderBase, IPreloader
    {
        private static readonly string[] Columns =
        {
            "id", "mode", "anim_type", "respown_time", "pos_x", "pos_y", "pos_z", "rotation", "map_context_id",
            "creature_1_Id", "creature_1_min_count", "creature_1_max_count",
            "creature_2_Id", "creature_2_min_count", "creature_2_max_count",
            "creature_3_Id", "creature_3_min_count", "creature_3_max_count",
            "creature_4_Id", "creature_4_min_count", "creature_4_max_count",
            "creature_5_Id", "creature_5_min_count", "creature_5_max_count",
            "creature_6_Id", "creature_6_min_count", "creature_6_max_count",
            "radius"
        };

        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, SpawnPoolEntry.TableName, Columns);
        }

        /// <summary>The pools of these ids alone: a later migration's Down putting back pools it took out.</summary>
        public void Preload(MigrationBuilder migrationBuilder, ICollection<uint> ids)
        {
            Insert(migrationBuilder, SpawnPoolEntry.TableName, Columns, row => ids.Contains((uint)row[0]));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return Pool(BootcampPostedNpcs.ForeanWarriorPoolId, 404.0625f, 120.7656f, 110.1328f, 0.6549f, BootcampPostedNpcs.ForeanWarriorId);
            yield return Pool(BootcampPostedNpcs.InfantrymanPoolId, 383.5859f, 120.2812f, 118.5352f, 5.892f, BootcampPostedNpcs.InfantrymanId);
            yield return Pool(BootcampPostedNpcs.RiflemanWestPoolId, 384.4805f, 119.5273f, 140.3789f, 6.258f, BootcampPostedNpcs.InfantrymanWithRifleId);
            yield return Pool(BootcampPostedNpcs.RiflemanEastPoolId, 392.9609f, 119.7812f, 140.5195f, 0.0659f, BootcampPostedNpcs.InfantrymanWithRifleId);
        }

        private static object[] Pool(uint id, float x, float y, float z, float rotation, uint creatureId) => new object[]
        {
            id, 0, 0, 200, x, y, z, rotation, BootcampPostedNpcs.BootcampMapContextId,
            creatureId, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0.0
        };
    }

    /// <summary>The pose of each pool: the warrior and the unarmed infantryman standing, the two riflemen with the rifle out.</summary>
    public class BootcampPostedNpcPosePreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, SpawnPoolPoseEntry.TableName, new[] { "id", "pose" });
        }

        /// <summary>The poses of these pools alone: a later migration's Down putting back pools it took out.</summary>
        public void Preload(MigrationBuilder migrationBuilder, ICollection<uint> ids)
        {
            Insert(migrationBuilder, SpawnPoolPoseEntry.TableName, new[] { "id", "pose" }, row => ids.Contains((uint)row[0]));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { BootcampPostedNpcs.ForeanWarriorPoolId, BootcampPostedNpcs.Standing };
            yield return new object[] { BootcampPostedNpcs.InfantrymanPoolId, BootcampPostedNpcs.Standing };
            yield return new object[] { BootcampPostedNpcs.RiflemanWestPoolId, BootcampPostedNpcs.WeaponOut };
            yield return new object[] { BootcampPostedNpcs.RiflemanEastPoolId, BootcampPostedNpcs.WeaponOut };
        }
    }
}
