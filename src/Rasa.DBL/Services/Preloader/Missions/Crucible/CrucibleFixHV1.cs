using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Ligo Crucible fix job H (to-fix row 64), a delta on migration 20271108000900 (set 7): its data class stays as it is.
    ///
    /// The Ultra Reconstructor's point pool 770038 (Your Part of the Bargain 1529, Staal Junkyard 2138) stood at (-14, 335.14,
    /// 244), the north centre (TaRapedia "near the upper centre of the map", 0.5). The real client could not walk there
    /// (2026-11-07): the navmesh walk north from (290.5, 267.2, -2.0) climbs a junk slope that stops the client at (296.7,
    /// 273.5, 3.8) (its client floor rises 1.06 m a metre over 12 m), and every navmesh walk to the old place climbs the rock
    /// ledge at (37.7, 330.4, 212.6) (the client floor jumps 3.26 m within 0.5 m); the boss stood on that ledge, out of sight
    /// from below. Probe 2026-11-07 (navmesh and client files): the walks to every other Junkyard place climb at most 0.81 m a
    /// metre over 12 m and 0.97 m within 0.5 m, and the real client walked them.
    ///
    /// Now the pool stands at (164, 265.44, -76) (ESTIMATE, confidence 0.3), in the open middle of the Junkyard (no navmesh wall
    /// within 20 m): on the navmesh and a client floor; seen from 16 of 16 places 20 m round and from each point 5 to 2 m from it
    /// of the walks from the arrival and from Cirkin; both walks client-climbable (0.83 m a metre over 12 m at most, 0.6 m
    /// within 0.5 m); no lava within 20 m; 287 m from the door's circle, 159 m from Cirkin, 79 m from every other pool's circle.
    /// It stays a point pool and the scene's public-spawn actor (the actor names the pool, not a place). 1529's marker 9
    /// (objective 3) moves with it. The north centre is left (deviation, listed for the review).
    /// </summary>
    public static class CrucibleFixHV1
    {
        public const uint Pool = CrucibleStaalJunkyardNpcsV1.UltraReconstructor;
        public static readonly (double X, double Y, double Z) Before = CrucibleStaalJunkyardNpcsV1.UltraReconstructorAt;
        public static readonly (double X, double Y, double Z) Now = (164, 265.44, -76);

        /// <summary>1529's marker on the Ultra Reconstructor: objective 3, the ninth (two pools and six blocks before it).</summary>
        public const uint MarkerObjective = CruciblePartOfTheBargainV1.EnergyMatrix, Marker = MissionIndicator.UnnamedFrom + 9;

        /// <summary>1529's evidence row (set 7 wrote 1-3).</summary>
        public const uint Evidence = 4;

        public const string Source = "repository:CrucibleFixHV1";

        public const string Note =
            "Fix job H (to-fix row 64): the Ultra Reconstructor's point pool 770038 stood on a rock ledge at (-14, 335.14, 244); every " +
            "navmesh walk there climbed the ledge (the client floor jumps 3.26 m at (37.7, 330.4, 212.6)) or a junk slope the real " +
            "client cannot climb at (296.7, 273.5, 3.8). Moved to (164, 265.44, -76), the open middle of the Junkyard (ESTIMATE): " +
            "walked from the arrival and from Cirkin with no climb the client fails, seen from the walks' ends, no lava within 20 m. " +
            "Still a point pool and the scene's public-spawn actor; marker 9 on it. TaRapedia's 'upper centre' is left.";

        public static void Up(MigrationBuilder migration)
        {
            migration.Sql(Place(Now));
            migration.Sql(MarkerAt(Now));
            CrucibleMissionDataV1.Existing(migration, CruciblePartOfTheBargainV1.MissionId)
                .Evidence(Evidence, MissionEvidenceSourceKind.Reconstruction, Source, Note, 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {CruciblePartOfTheBargainV1.MissionId} " +
                          $"AND content_revision = '{CrucibleMissionDataV1.Revision}' AND evidence_id = {Evidence};");
            migration.Sql(MarkerAt(Before));
            migration.Sql(Place(Before));
        }

        private static string Place((double X, double Y, double Z) at) =>
            $"UPDATE spawnpool SET pos_x = {N(at.X)}, pos_y = {N(at.Y)}, pos_z = {N(at.Z)} WHERE id = {Pool};";

        private static string MarkerAt((double X, double Y, double Z) at) =>
            $"UPDATE mission_indicator SET pos_x = {N(at.X)}, pos_y = {N(at.Y)}, pos_z = {N(at.Z)} " +
            $"WHERE mission_id = {CruciblePartOfTheBargainV1.MissionId} AND content_revision = '{CrucibleMissionDataV1.Revision}' " +
            $"AND objective_id = {MarkerObjective} AND indicator_id = {Marker};";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
