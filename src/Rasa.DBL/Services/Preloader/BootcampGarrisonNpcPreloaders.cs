using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The second lot of people for the Proving Grounds' refugee base (adv_bootcamp, map context
    /// 1985), after BootcampPostedNpcs. Each is where a GM stood and faced, read off the server's
    /// .where line, but for the two seated men, who are on the map's own chairs.
    ///
    /// Six more Infantrymen holding their rifles (creature 400003, pose 2, as the two of the first
    /// lot): one either side of the command tent's opening, and four at the corners of the Eloh
    /// hologram platform by Major McAllister. Pools 400101 to 400106, clear of the ids the
    /// base's other hand-placed NPCs are numbered up from. With no speed they stand
    /// exactly on their pool's point, at the height the GM stood at (the game server's
    /// BehaviorManager.NeverMoves), like the first four.
    ///
    /// Six of the client's ambient figures (ambient_npc), ids 1 to 6:
    ///  - two at the firing range, UsableStatelessNPCMaleFiringRange (29425): a soldier in grey
    ///    light armour with a harness at the thigh, firing a pistol and reloading. The model is
    ///    the soldier and his own target, 14.25 m in front of him on a bone of its own. With him
    ///    on the firing step of the middle and the west lane, where the GM stood, the target is
    ///    on the lane's far platform, about 0.4 m short of its back edge. Gearing Up's Practice
    ///    Dummies of those two lanes (practice-1 and practice-2) are 1.4 to 1.5 m in front of it.
    ///  - two seated, both in a dark vest and grey fatigue trousers:
    ///    UsableStatelessNPCMaleSittingTalkingV01 (25583), black-haired, leaning forward and
    ///    talking, and UsableStatelessNPCMaleCleanWeaponSittingV01 (25622), his fair head shaved,
    ///    cleaning a pistol. The map has three chairs behind the firing line
    ///    (ArchHumGenObjChairV02); these two are put on the two that face each other, at each
    ///    chair's own position and facing, so that they sit on them: both models sit at that
    ///    height, hips 0.57 to 0.59 m over the origin.
    ///  - one reading a tablet, UsableStatelessNPCMaleStandingTabletV01 (25634): grey uniform and
    ///    cap, on the command deck.
    ///  - one standing, UsableStatelessNPCMaleStandingV01 (25600): arms at his sides, in a grey
    ///    T-shirt and fatigue trousers, behind the firing line.
    ///
    /// Captain Delessio's pool is moved by the migration itself.
    /// </summary>
    public static class BootcampGarrisonNpcs
    {
        public const uint FirstPoolId = 400101;
        public const uint LastPoolId = 400106;

        public const uint FirstAmbientId = 1;
        public const uint LastAmbientId = 6;

        public const uint FiringRangeClass = 29425;
        public const uint SittingTalkingClass = 25583;
        public const uint CleanWeaponSittingClass = 25622;
        public const uint StandingTabletClass = 25634;
        public const uint StandingClass = 25600;

        /// <summary>Captain Delessio's pool, and where it stood and stands.</summary>
        public const uint DelessioPoolId = BootcampWorldContentSeedData.CaptainDelessioCreatureId;
        public const string DelessioWas = "pos_x = 401.0, pos_y = 122.0, pos_z = 170.0, rotation = 1.674436";
        public const string DelessioIs = "pos_x = 400.4883, pos_y = 122.0, pos_z = 171.1992, rotation = 0.4381";
    }

    /// <summary>Six pools of one rifleman each, as BootcampPostedNpcSpawnpoolPreloader's.</summary>
    public class BootcampGarrisonSpawnpoolPreloader : PreloaderBase, IPreloader
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
            // either side of the command tent's opening
            yield return Pool(400101, 402.3125f, 122f, 169.4883f, 1.4922f);
            yield return Pool(400102, 402.457f, 122f, 162.8281f, 1.5864f);

            // the corners of the Eloh hologram platform by Major McAllister
            yield return Pool(400103, 395.5156f, 126.2812f, 57.3164f, 5.4176f);
            yield return Pool(400104, 381.6289f, 126.2812f, 57.0977f, 0.6487f);
            yield return Pool(400105, 396.2031f, 126.3242f, 72.8164f, 3.2609f);
            yield return Pool(400106, 382.457f, 126.3633f, 73.3242f, 2.8477f);
        }

        private static object[] Pool(uint id, float x, float y, float z, float rotation) => new object[]
        {
            id, 0, 0, 200, x, y, z, rotation, BootcampPostedNpcs.BootcampMapContextId,
            BootcampPostedNpcs.InfantrymanWithRifleId, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0.0
        };
    }

    /// <summary>All six with the rifle out.</summary>
    public class BootcampGarrisonPosePreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, SpawnPoolPoseEntry.TableName, new[] { "id", "pose" });
        }

        protected override IEnumerable<object[]> GetRows()
        {
            for (var id = BootcampGarrisonNpcs.FirstPoolId; id <= BootcampGarrisonNpcs.LastPoolId; id++)
                yield return new object[] { id, BootcampPostedNpcs.WeaponOut };
        }
    }

    public class BootcampAmbientNpcPreloader : PreloaderBase, IPreloader
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
            yield return Figure(1, BootcampGarrisonNpcs.FiringRangeClass, 380.7695, 119.5938, 173.1484, 3.1054, "Proving Grounds: soldier firing a pistol, middle lane");
            yield return Figure(2, BootcampGarrisonNpcs.FiringRangeClass, 375.418, 119.5938, 173.2656, 3.1541, "Proving Grounds: soldier firing a pistol, west lane");

            // On the map's chairs, as the map has them: 0.12 m and 0.25 m from where the GM stood.
            yield return Figure(3, BootcampGarrisonNpcs.SittingTalkingClass, 379.5813, 119.5292, 167.9491, 3.1816, "Proving Grounds: seated behind the range, talking");
            yield return Figure(4, BootcampGarrisonNpcs.CleanWeaponSittingClass, 379.0926, 119.5292, 169.427, 5.1738, "Proving Grounds: seated behind the range, cleaning a pistol");

            yield return Figure(5, BootcampGarrisonNpcs.StandingTabletClass, 393.7969, 122.0, 166.1016, 2.4299, "Proving Grounds: officer with a tablet, command deck");
            yield return Figure(6, BootcampGarrisonNpcs.StandingClass, 376.1602, 119.5273, 169.9258, 3.0802, "Proving Grounds: standing behind the range");
        }

        private static object[] Figure(uint id, uint classId, double x, double y, double z, double rotation, string comment) => new object[]
        {
            id, BootcampPostedNpcs.BootcampMapContextId, classId, x, y, z, rotation, comment
        };
    }
}
