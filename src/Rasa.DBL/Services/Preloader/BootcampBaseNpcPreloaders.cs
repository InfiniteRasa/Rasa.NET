using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The third lot of people for the Proving Grounds' refugee base (adv_bootcamp, map context
    /// 1985), each where a GM stood and faced, read off the server's .where lines, but for the
    /// woman on the chair.
    ///
    /// Creatures, ids after BootcampPostedNpcs and BootcampTrainingOfficer:
    ///  - 400006 Field Officer Oceana (creaturenamelanguage 10580): the Forean shaman initiate's
    ///    model, NPC_Forean_Shaman (7035) with its staff (6164), standing by the range. Pool 400006.
    ///  - 400007 an Infantryman with a pistol: Redshirt_Human_Soldier_Light_Male (29423), name
    ///    8716, Weapon_Human_Redshirt_Pistol_Physical (6271), standing at the sandbag post west
    ///    of the bridge. Pools 400007 and 400008.
    ///  - 400008 a Field Gunner (8720): Redshirt_Human_Soldier_Heavy_Male (29433) with
    ///    Weapon_Human_Redshirt_MachineGun_Physical (20535) carried, walking 16 m along the road
    ///    by the bridge (pool 400009, spawnpool_patrol): he stands 4 s at each end, facing the
    ///    way the GM faced there, and walks back.
    /// All friendly, level 5, no attack: safe ground, as the base's other soldiers.
    ///
    /// Ambient figures (ambient_npc, AmbientNpcEntry), ids after BootcampTrainees:
    ///  - 18 UsableStatelessNPCFemaleDrinkingSittingBarstoolV01 (25571), on the third of the
    ///    map's chairs behind the firing line (ArchHumGenObjChairV02 at 373.9338, 169.8417,
    ///    facing 0.2772), at its position and facing as the two seated men are on the other two.
    ///    The model sits on a barstool: hips 0.963 m over its origin, feet on a rung 0.31 to
    ///    0.41 m up. The chairs' sitters have their hips 0.57 to 0.59 m up, so she is put 0.37 m
    ///    under the chair's floor (119.5292): hips 0.593 m over it, toes 0.06 m into it.
    ///  - 19 UsableStatelessNPCMaleDrinkingStandingV01 (25557), standing by her.
    ///  - 20 UsableStatelessNPCMaleStandingOperatingConsoleV01 (26417), at the computer tower in
    ///    the command tent.
    ///  - 21 UsableStatelessNPCMaleStandingTabletV01 (25634), a man reading a data pad by the
    ///    holofence south-east of the beat.
    ///
    /// And moved: the two soldiers at the firing range (ambient 1 and 2) so that the target
    /// their model carries 14.252 m in front of them stands in the middle of the gap between
    /// their lane's two sandbag walls at the far end, Corporal DeSimone's pool, and the spot
    /// Major McAllister walks to when Gearing Up is accepted (BootcampBaseNpcScenes).
    /// </summary>
    public static class BootcampBaseNpcs
    {
        public const uint OceanaId = 400006;
        public const uint PistolInfantrymanId = 400007;
        public const uint FieldGunnerId = 400008;

        public const uint OceanaPoolId = 400006;
        public const uint PistolPostWestPoolId = 400007;
        public const uint PistolPostEastPoolId = 400008;
        public const uint FieldGunnerPoolId = 400009;

        public const uint FirstId = 400006;
        public const uint LastCreatureId = 400008;
        public const uint LastPoolId = 400009;

        public const uint FirstAmbientId = 18;
        public const uint LastAmbientId = 21;

        public const uint DrinkingWomanClass = 25571;
        public const uint DrinkingManClass = 25557;
        public const uint ConsoleClass = 26417;
        public const uint TabletClass = 25634;

        /// <summary>The third chair behind the firing line, and how far under it the barstool sitter goes.</summary>
        public const double ChairX = 373.9338;
        public const double ChairFloor = 119.5292;
        public const double ChairZ = 169.8417;
        public const double ChairFacing = 0.2772;
        public const double BarstoolDrop = 0.37;

        /// <summary>The Field Gunner's two ends.</summary>
        public const double GunnerEastX = 343.4336, GunnerEastY = 121.7031, GunnerEastZ = 64.0469, GunnerEastFacing = 1.5987;
        public const double GunnerWestX = 327.3203, GunnerWestY = 121.707, GunnerWestZ = 63.3125, GunnerWestFacing = 1.4857;
        public const uint GunnerPauseMs = 4000;

        /// <summary>
        /// The firing range: the middle of the gap between each lane's far sandbag walls
        /// (ArchHumGenObjSandbags01V08, read from their collision 0.5 m over the floor), at
        /// z 187.5 - 0.4 m short of the far platform's back edge, where the walls flank it. The
        /// soldier stands 14.252 m short of it facing straight down the lane (rotation pi), on
        /// the firing step (z 170.28 to 174.28).
        /// </summary>
        public const double TargetZ = 187.5;
        public const double TargetDistance = 14.252;
        public const double MiddleLaneX = 380.63;
        public const double WestLaneX = 375.29;
        public const double ShooterZ = 173.248;   // TargetZ - TargetDistance
        public const double ShooterRotation = 3.1416;

        /// <summary>Where the two soldiers stood before (BootcampAmbientNpcPreloader).</summary>
        public const string MiddleShooterWas = "pos_x = 380.7695, pos_z = 173.1484, rotation = 3.1054";
        public const string WestShooterWas = "pos_x = 375.418, pos_z = 173.2656, rotation = 3.1541";

        /// <summary>Corporal DeSimone's pool, where it stood and stands.</summary>
        public const uint DeSimonePoolId = BootcampWorldContentSeedData.CorporalDeSimoneCreatureId;
        public const string DeSimoneWas = "pos_x = 391.5, pos_y = 119.7, pos_z = 164.8, rotation = 0.0";
        public const string DeSimoneIs = "pos_x = 383.0781, pos_y = 119.5273, pos_z = 155.4141, rotation = 4.2675";

        /// <summary>
        /// The quest NPCs' officer boots, tunic and trousers (NPC_Clothing_Officer_1, slots 2, 15
        /// and 16): from blue, (0, 128, 255), to #68562c. A colour is stored as the game server's
        /// Color.Hue, alpha, blue, green, red from the high byte down.
        /// </summary>
        public const uint FirstQuestNpcId = BootcampWorldContentSeedData.MajorMcAllisterCreatureId;
        public const uint LastQuestNpcId = BootcampWorldContentSeedData.CorporalVanValkenbergCreatureId;
        public const uint OfficerBlue = 4294934528;
        public const uint OfficerOlive = 0xFF2C5668;
    }

    public class BootcampBaseNpcCreaturePreloader : PreloaderBase, IPreloader
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
            yield return new object[] { BootcampBaseNpcs.OceanaId, "Field Officer Oceana", 7035, 1, 5, 600, 10580, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { BootcampBaseNpcs.PistolInfantrymanId, "Infantryman at a post, pistol", 29423, 1, 5, 350, 8716, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { BootcampBaseNpcs.FieldGunnerId, "Field Gunner on patrol", 29433, 1, 5, 600, 8720, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0 };
        }
    }

    public class BootcampBaseNpcAppearancePreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureAppearanceEntry.TableName, new[] { "id", "slot_id", "Class_id", "color" });
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { BootcampBaseNpcs.OceanaId, 13, 6164, 1 };
            yield return new object[] { BootcampBaseNpcs.PistolInfantrymanId, 13, 6271, 1 };
            yield return new object[] { BootcampBaseNpcs.FieldGunnerId, 13, 20535, 1 };
        }
    }

    public class BootcampBaseNpcStatsPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureStatEntry.TableName, new[] { "id", "body", "mind", "spirit", "health", "armor" });
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { BootcampBaseNpcs.OceanaId, 15, 15, 15, 600, 60 };
            yield return new object[] { BootcampBaseNpcs.PistolInfantrymanId, 12, 12, 12, 350, 60 };
            yield return new object[] { BootcampBaseNpcs.FieldGunnerId, 15, 15, 15, 600, 60 };
        }
    }

    /// <summary>One creature a pool, respawn 20 s as the base's staff; the Field Gunner's on the east end of his walk.</summary>
    public class BootcampBaseNpcSpawnpoolPreloader : PreloaderBase, IPreloader
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

        protected override IEnumerable<object[]> GetRows()
        {
            yield return Pool(BootcampBaseNpcs.OceanaPoolId, 372.0, 119.5273, 165.5352, 5.3184, BootcampBaseNpcs.OceanaId);
            yield return Pool(BootcampBaseNpcs.PistolPostWestPoolId, 314.9492, 120.5312, 59.4219, 1.872, BootcampBaseNpcs.PistolInfantrymanId);
            yield return Pool(BootcampBaseNpcs.PistolPostEastPoolId, 316.2148, 120.5312, 60.2383, 2.3668, BootcampBaseNpcs.PistolInfantrymanId);
            yield return Pool(BootcampBaseNpcs.FieldGunnerPoolId, BootcampBaseNpcs.GunnerEastX, BootcampBaseNpcs.GunnerEastY,
                BootcampBaseNpcs.GunnerEastZ, BootcampBaseNpcs.GunnerEastFacing, BootcampBaseNpcs.FieldGunnerId);
        }

        private static object[] Pool(uint id, double x, double y, double z, double rotation, uint creatureId) => new object[]
        {
            id, 0, 0, 200, x, y, z, rotation, BootcampPostedNpcs.BootcampMapContextId,
            creatureId, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0.0
        };
    }

    /// <summary>Oceana and the two at the sandbag post stand at their posts; the Field Gunner walks, so has no pose.</summary>
    public class BootcampBaseNpcPosePreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, SpawnPoolPoseEntry.TableName, new[] { "id", "pose" });
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { BootcampBaseNpcs.OceanaPoolId, BootcampPostedNpcs.Standing };
            yield return new object[] { BootcampBaseNpcs.PistolPostWestPoolId, BootcampPostedNpcs.Standing };
            yield return new object[] { BootcampBaseNpcs.PistolPostEastPoolId, BootcampPostedNpcs.Standing };
        }
    }

    public class BootcampBaseNpcPatrolPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, SpawnPoolPatrolEntry.TableName, new[] { "pool_id", "step", "pos_x", "pos_y", "pos_z", "facing", "pause_ms" });
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[]
            {
                BootcampBaseNpcs.FieldGunnerPoolId, 0, BootcampBaseNpcs.GunnerEastX, BootcampBaseNpcs.GunnerEastY, BootcampBaseNpcs.GunnerEastZ,
                BootcampBaseNpcs.GunnerEastFacing, BootcampBaseNpcs.GunnerPauseMs
            };
            yield return new object[]
            {
                BootcampBaseNpcs.FieldGunnerPoolId, 1, BootcampBaseNpcs.GunnerWestX, BootcampBaseNpcs.GunnerWestY, BootcampBaseNpcs.GunnerWestZ,
                BootcampBaseNpcs.GunnerWestFacing, BootcampBaseNpcs.GunnerPauseMs
            };
        }
    }

    public class BootcampBaseNpcAmbientPreloader : PreloaderBase, IPreloader
    {
        private static readonly string[] Columns =
        {
            "id", "map_context_id", "class_id", "pos_x", "pos_y", "pos_z", "rotation", "comment"
        };

        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, AmbientNpcEntry.TableName, Columns);
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return Figure(18, BootcampBaseNpcs.DrinkingWomanClass,
                BootcampBaseNpcs.ChairX, BootcampBaseNpcs.ChairFloor - BootcampBaseNpcs.BarstoolDrop, BootcampBaseNpcs.ChairZ,
                BootcampBaseNpcs.ChairFacing, "Proving Grounds: woman on a chair behind the range, drinking");
            yield return Figure(19, BootcampBaseNpcs.DrinkingManClass, 372.6641, 119.5273, 168.5625, 4.3429, "Proving Grounds: man standing behind the range, drinking");
            yield return Figure(20, BootcampBaseNpcs.ConsoleClass, 410.5547, 122.0977, 164.6367, 3.1177, "Proving Grounds: man at a console, command tent");
            yield return Figure(21, BootcampBaseNpcs.TabletClass, 394.9414, 120.0, 145.6875, 4.4624, "Proving Grounds: man reading a data pad by the holofence");
        }

        private static object[] Figure(uint id, uint classId, double x, double y, double z, double rotation, string comment) => new object[]
        {
            id, BootcampPostedNpcs.BootcampMapContextId, classId, x, y, z, rotation, comment
        };
    }
}
