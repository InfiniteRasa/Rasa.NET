using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The drill sergeant's recruits at the Proving Grounds (adv_bootcamp, map context 1985): two
    /// of the client's UsableStatelessNPCtraineesPTV01 (29418, ambient_m_soldier_trainee_6group_v01),
    /// ambient_npc 16 and 17.
    ///
    /// One figure is six recruits in casual clothes, women and men of mixed looks (the model's
    /// faces: avatar_hum_f_face_asian, _f_face_cauc, _m_face_african, _m_face_asian, _m_face_cauc),
    /// standing in three columns 1.46 m apart (model x -1.497, 0, 1.416) and two
    /// rows 1.48 m apart (model z 0 and 1.48, the front row at z 0), facing their -z as the other
    /// ambient figures do. The six do one animation together, the client picking each next one
    /// by weight (animationFamilyObject (277, 48907)): standing at ease (5.3 s, weight 4),
    /// jumping jacks (4.6 s, 4), push-ups (16 s, 1) and a salute (4 s, 1). The two figures
    /// pick on their own, so the two squads of six exercise apart.
    ///
    /// Put side by side, the second squad's west column a column's width from the first's east
    /// one, they are one rectangle of six columns and two rows: between the drill sergeant's
    /// line (BootcampAmbientGuards, z 148.59) and the gate wall, facing him (rotation pi), its
    /// middle on his (x 379.3246). The front row is 2.09 m from his line and the back row at
    /// z 145.02; the floor there is 119.58 and clear, and the wall's inner face, which leans over
    /// the ground nearer it, is 4.3 m up at z 144.5, over the back row's push-ups (their hips
    /// go 0.36 m back).
    /// </summary>
    public static class BootcampTrainees
    {
        public const uint TraineesClass = 29418;

        public const uint WestId = 16;
        public const uint EastId = 17;

        /// <summary>A squad's columns and rows on its model.</summary>
        public static readonly double[] ColumnsX = { -1.497, 0.0, 1.416 };
        public const double ColumnSpacing = 1.4565;
        public const double BackRowZ = 1.48;

        public const double WestX = 377.0993;
        public const double EastX = 381.4688;
        public const double FrontRowZ = 146.5;
        public const double Floor = BootcampTrainingOfficer.Floor;
        public const double Rotation = 3.1416;

        /// <summary>The two rows, as ambient_npc rows: id, map, class, x, y, z, rotation, comment.</summary>
        public static IEnumerable<object[]> Rows()
        {
            yield return Row(WestId, WestX, "west squad");
            yield return Row(EastId, EastX, "east squad");
        }

        /// <summary>The inserts, SQL that SQLite and MySQL both take.</summary>
        public static IEnumerable<string> UpStatements()
        {
            foreach (var row in Rows())
            {
                yield return string.Format(CultureInfo.InvariantCulture,
                    "insert into {0} (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, comment) values ({1}, {2}, {3}, {4}, {5}, {6}, {7}, '{8}');",
                    AmbientNpcEntry.TableName, row[0], row[1], row[2], row[3], row[4], row[5], row[6], row[7]);
            }
        }

        public static IEnumerable<string> DownStatements()
        {
            yield return $"delete from {AmbientNpcEntry.TableName} where id in ({WestId}, {EastId});";
        }

        private static object[] Row(uint id, double x, string which) => new object[]
        {
            id, BootcampPostedNpcs.BootcampMapContextId, TraineesClass, x, Floor, FrontRowZ, Rotation,
            "Proving Grounds: six recruits drilling before the drill sergeant, " + which
        };
    }
}
