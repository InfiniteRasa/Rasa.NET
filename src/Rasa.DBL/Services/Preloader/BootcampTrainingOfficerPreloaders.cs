using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The first NPC placed with a patrol (spawnpool_patrol): a Training Officer walking a beat
    /// at the Proving Grounds' refugee base (adv_bootcamp, map context 1985), where a GM walked
    /// it and read the ends and the stops off the server's .where lines.
    ///
    /// Creature 400005: NPC_Corman_Swapset_Male (6339) in the officer's uniform and cap that
    /// Lt Col Cimoch wears (creature 118), creaturenamelanguage 10603, "Training Officer".
    /// Friendly, level 5 and 555 health as the base's medic, no attack. It walks at 1 m/s and
    /// has no running speed: it goes nowhere but its beat.
    ///
    /// The beat is a straight line along x at z = 148.59, 8.04 m long, on open ground: the
    /// terrain is 119.58 under all of it and nothing stands on it. The GM's six points wander
    /// 0.5 m either side of that line; the steps are on it.
    ///
    ///  - step 0, the south end (383.832): the pool's own point. He turns about and walks north.
    ///  - step 1 (380.4414): he stops, turns to face yaw 0 - a quarter turn from the way he
    ///    walks - and stands 4 s.
    ///  - step 2 (376.8125): the same.
    ///  - step 3, the north end (375.7891): he turns about and walks the whole line back to
    ///    step 0 without stopping.
    ///
    /// Yaw is the server's: atan2(-dx, -dz) of the way faced, so walking north (x falling) is
    /// pi/2 and yaw 0 looks along -z.
    ///
    /// Ids follow the posted NPCs' (BootcampPostedNpcs, 400001-400004), under the 500000 the
    /// generated pools start at.
    /// </summary>
    public static class BootcampTrainingOfficer
    {
        public const uint CreatureId = 400005;
        public const uint PoolId = 400005;

        public const uint BootcampMapContextId = 1985;

        /// <summary>The beat's line, and the floor under it.</summary>
        public const double LineZ = 148.59;
        public const double Floor = 119.58;

        public const double SouthEndX = 383.832;
        public const double FirstStopX = 380.4414;
        public const double SecondStopX = 376.8125;
        public const double NorthEndX = 375.7891;

        /// <summary>The way he walks north, and the way he faces at a stop.</summary>
        public const double NorthboundYaw = 1.5707963;
        public const double StopFacing = 0.0;

        public const uint StopPauseMs = 4000;
    }

    public class BootcampTrainingOfficerCreaturePreloader : PreloaderBase, IPreloader
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
            yield return new object[] { BootcampTrainingOfficer.CreatureId, "Training Officer on patrol", 6339, 1, 5, 555, 10603, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0 };
        }
    }

    /// <summary>
    /// The officer's cap, boots, gloves, tunic, trousers and face, class and colour as on Lt Col
    /// Cimoch (creature 118): NPC_Clothing_Officer_3_Helmet, _1_Boots, _3_Gloves, _1_Torso,
    /// _1_Legs and AvatarSwap_Face_Caucasian_Long_1.
    /// </summary>
    public class BootcampTrainingOfficerAppearancePreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureAppearanceEntry.TableName, new[] { "id", "slot_id", "Class_id", "color" });
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { BootcampTrainingOfficer.CreatureId, 1, 26677, 4286886614 };
            yield return new object[] { BootcampTrainingOfficer.CreatureId, 2, 4021, 4294934528 };
            yield return new object[] { BootcampTrainingOfficer.CreatureId, 3, 26673, 22120 };
            yield return new object[] { BootcampTrainingOfficer.CreatureId, 15, 4023, 4294934528 };
            yield return new object[] { BootcampTrainingOfficer.CreatureId, 16, 4022, 4294934528 };
            yield return new object[] { BootcampTrainingOfficer.CreatureId, 17, 24008, 4286690539 };
        }
    }

    public class BootcampTrainingOfficerStatsPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureStatEntry.TableName, new[] { "id", "body", "mind", "spirit", "health", "armor" });
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { BootcampTrainingOfficer.CreatureId, 12, 12, 12, 555, 60 };
        }
    }

    /// <summary>His pool: one of him, on the south end of the beat and facing up it; respawn 20 s, as the base's staff.</summary>
    public class BootcampTrainingOfficerSpawnpoolPreloader : PreloaderBase, IPreloader
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
            yield return new object[]
            {
                BootcampTrainingOfficer.PoolId, 0, 0, 200,
                BootcampTrainingOfficer.SouthEndX, BootcampTrainingOfficer.Floor, BootcampTrainingOfficer.LineZ,
                BootcampTrainingOfficer.NorthboundYaw, BootcampTrainingOfficer.BootcampMapContextId,
                BootcampTrainingOfficer.CreatureId, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0.0
            };
        }
    }

    /// <summary>The four steps of his beat: the south end, the two stops on the way north, the north end.</summary>
    public class BootcampTrainingOfficerPatrolPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, SpawnPoolPatrolEntry.TableName, new[] { "pool_id", "step", "pos_x", "pos_y", "pos_z", "facing", "pause_ms" });
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return Step(0, BootcampTrainingOfficer.SouthEndX, null, 0);
            yield return Step(1, BootcampTrainingOfficer.FirstStopX, BootcampTrainingOfficer.StopFacing, BootcampTrainingOfficer.StopPauseMs);
            yield return Step(2, BootcampTrainingOfficer.SecondStopX, BootcampTrainingOfficer.StopFacing, BootcampTrainingOfficer.StopPauseMs);
            yield return Step(3, BootcampTrainingOfficer.NorthEndX, null, 0);
        }

        private static object[] Step(uint step, double x, double? facing, uint pauseMs) => new object[]
        {
            BootcampTrainingOfficer.PoolId, step, x, BootcampTrainingOfficer.Floor, BootcampTrainingOfficer.LineZ, facing, pauseMs
        };
    }
}
