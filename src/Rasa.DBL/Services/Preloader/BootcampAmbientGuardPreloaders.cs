using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The Proving Grounds' Training Officer and its eight rifle guards (adv_bootcamp, map context
    /// 1985) as the client's own ambient figures for them, in place of the creatures that stood
    /// in for them: an Infantryman class with the rifle out (BootcampPostedNpcs, BootcampGarrisonNpcs)
    /// leans forward in the combat-ready rifle stance, and the officer was an avatar swapset in
    /// Lt Col Cimoch's uniform.
    ///
    /// Each guard, UsableStatelessNPCMaleGuard (26397, ambient_m_guard_rifle_v01): a soldier
    /// standing at ease, the rifle held loosely across his chest, breathing, looking about and
    /// shifting his weight (m_ambient_guard_rifle_breathe/look/weightshift_v01). He stands on his
    /// own spot. Each is put on the spot of the rifleman pool it replaces, facing the same way:
    /// the model faces its -z, as the firing range's soldier does, whose target is 14.25 m down
    /// his -z (AmbientNpcTests), so a figure's rotation is the facing of the server's yaw.
    ///
    /// The officer, UsableStatelessNPCDrillSGTV01 (29422, ambient_m_drillseargent_v01): a drill
    /// sergeant whose two animations (ambient_m_drillseargent_walk_v01 and _v02, 20 s and 22 s)
    /// walk him himself. Read off the animations' root bone (Bip01 Master): he stands facing his
    /// +z - turned about from the other figures, the bone's rest is a half turn - walks 1.36 m to
    /// his +x and stands there 7 s, turns and walks 3.69 m to -2.33, and walks back to where he
    /// started; both animations walk the same line and differ in how long he stands. So the
    /// line is along his x, 3.69 m long, its middle 0.49 m to his -x. He is put on the Training
    /// Officer's beat (BootcampTrainingOfficer): on its line at z 148.59 and its floor 119.58,
    /// the middle of his line at the middle of the beat (379.81), and facing yaw 0, the way the
    /// officer faced at his stops - a rotation of pi, his +x then being the world's -x.
    ///
    /// The figures are not creatures (AmbientNpcEntry): no name, no target, nothing to say. The
    /// creature rows 400003 and 400005 stay, with no pool.
    /// </summary>
    public static class BootcampAmbientGuards
    {
        public const uint MaleGuardClass = 26397;
        public const uint DrillSergeantClass = 29422;

        public const uint DrillSergeantId = 7;
        public const uint FirstGuardId = 8;
        public const uint LastGuardId = 15;

        public const uint FirstId = DrillSergeantId;
        public const uint LastId = LastGuardId;

        /// <summary>The drill sergeant's line on his model, read off his animations' root bone.</summary>
        public const double PaceToPlusX = 1.36;
        public const double PaceToMinusX = -2.332;

        /// <summary>The middle of the officer's beat, and where the drill sergeant stands so that his line's middle is on it.</summary>
        public const double BeatMiddleX = (BootcampTrainingOfficer.SouthEndX + BootcampTrainingOfficer.NorthEndX) / 2;
        public const double DrillSergeantX = 379.3246;
        public const double DrillSergeantRotation = 3.1416;

        /// <summary>The rifleman pools whose spots the guards take, in the order of the guards' ids.</summary>
        public static readonly uint[] RiflemanPoolIds =
        {
            BootcampPostedNpcs.RiflemanWestPoolId, BootcampPostedNpcs.RiflemanEastPoolId,
            400101, 400102, 400103, 400104, 400105, 400106
        };

        /// <summary>Every pool the migration takes out: the riflemen's and the officer's.</summary>
        public static readonly uint[] PoolIds =
        {
            BootcampPostedNpcs.RiflemanWestPoolId, BootcampPostedNpcs.RiflemanEastPoolId, BootcampTrainingOfficer.PoolId,
            400101, 400102, 400103, 400104, 400105, 400106
        };
    }

    public class BootcampAmbientGuardPreloader : PreloaderBase, IPreloader
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
            yield return Figure(BootcampAmbientGuards.DrillSergeantId, BootcampAmbientGuards.DrillSergeantClass,
                BootcampAmbientGuards.DrillSergeantX, BootcampTrainingOfficer.Floor, BootcampTrainingOfficer.LineZ,
                BootcampAmbientGuards.DrillSergeantRotation, "Proving Grounds: drill sergeant pacing, the Training Officer's beat");

            // Where the riflemen stood (BootcampPostedNpcSpawnpoolPreloader, BootcampGarrisonSpawnpoolPreloader).
            yield return Guard(8, 384.4805, 119.5273, 140.3789, 6.258, "west of the bridge");
            yield return Guard(9, 392.9609, 119.7812, 140.5195, 0.0659, "east of the bridge");
            yield return Guard(10, 402.3125, 122.0, 169.4883, 1.4922, "command tent, north of the opening");
            yield return Guard(11, 402.457, 122.0, 162.8281, 1.5864, "command tent, south of the opening");
            yield return Guard(12, 395.5156, 126.2812, 57.3164, 5.4176, "Eloh hologram platform");
            yield return Guard(13, 381.6289, 126.2812, 57.0977, 0.6487, "Eloh hologram platform");
            yield return Guard(14, 396.2031, 126.3242, 72.8164, 3.2609, "Eloh hologram platform");
            yield return Guard(15, 382.457, 126.3633, 73.3242, 2.8477, "Eloh hologram platform");
        }

        private static object[] Guard(uint id, double x, double y, double z, double rotation, string where) =>
            Figure(id, BootcampAmbientGuards.MaleGuardClass, x, y, z, rotation, "Proving Grounds: rifle guard at ease, " + where);

        private static object[] Figure(uint id, uint classId, double x, double y, double z, double rotation, string comment) => new object[]
        {
            id, BootcampPostedNpcs.BootcampMapContextId, classId, x, y, z, rotation, comment
        };
    }
}
