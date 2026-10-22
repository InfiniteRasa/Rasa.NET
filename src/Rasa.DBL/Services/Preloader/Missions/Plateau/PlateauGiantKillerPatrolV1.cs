using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Giant Killer (939): Goliath's pool (520051) walks a beat, so Goliath leads his two to four
    /// Kael guards (PlateauGiantKillerV1) as a group patrol (the game server's PatrolGroups): slot 1,
    /// Goliath 1/1, walks the steps; the guards of slot 2 follow him two abreast.
    ///
    /// Sourced: Ellatha (boss NPC Goliath) "Patrols east of Camp Resistance along the north ridge,
    /// near -250, -350"; TaRapedia (Giant_Killer walkthrough) "The pack tends to patrol between the
    /// ruins and about halfway to Camp Resistence. They are best attacked at the furthest point west
    /// of their patrol area."
    ///
    /// Reconstructed (ESTIMATE): a beat walked there and back from the pool point (-440, -230), the
    /// west end, to (-254, -346) near the ruins, the east end, 261 m each way. Measured on the
    /// Plateau navmesh and the client's geometry (terrain and entity collision meshes, as
    /// StandingHeights measures a floor): every leg is a clear straight line on the navmesh with at
    /// least 5.5 m to a wall on each side, room for the guards at 3 m to either side; the east end
    /// is 4 m short of (-250, -350), where there is 3.8 m. The steps are the floor's height, with a
    /// step wherever the floor leaves the straight line by more than 0.3 m. Goliath stands 10 s at
    /// each end (ESTIMATE, user 2026-10-22). No spawnpool_patrol_group row: Goliath comes back with
    /// the pool's top-up (respawn_time 500 unchanged, user 2026-10-22).
    ///
    /// Down deletes the pool's beat and evidence 4.
    /// </summary>
    public static class PlateauGiantKillerPatrolV1
    {
        public const uint Pool = PlateauGiantKillerV1.GoliathPool;
        public const uint EndPauseMs = 10000;
        public const uint EvidenceId = 4;

        /// <summary>The way out, west end to east end: x, floor height, z.</summary>
        public static readonly (double X, double Y, double Z)[] WayOut =
        {
            (-440.0, 442.37, -230.0),   // west end, the pool point
            (-436.7, 442.50, -232.2),
            (-434.2, 443.33, -233.9),
            (-429.1, 443.12, -237.2),
            (-425.0, 442.37, -239.9),
            (-397.4, 443.10, -258.1),
            (-372.4, 441.87, -274.6),
            (-364.0, 442.86, -280.1),
            (-358.2, 442.87, -284.0),
            (-354.0, 443.79, -286.7),
            (-350.7, 443.22, -288.9),
            (-349.0, 443.62, -290.0),
            (-346.0, 443.37, -292.0),
            (-340.8, 442.44, -287.3),
            (-339.3, 442.78, -285.9),
            (-324.0, 442.37, -272.0),
            (-314.0, 442.62, -266.0),
            (-310.0, 443.87, -266.0),
            (-302.0, 443.62, -266.0),
            (-291.4, 442.30, -276.6),
            (-287.2, 442.30, -280.8),
            (-285.0, 443.04, -283.0),
            (-282.0, 442.80, -286.0),
            (-279.9, 442.82, -289.4),
            (-277.4, 443.77, -293.7),
            (-276.0, 443.62, -296.0),
            (-272.8, 442.62, -303.3),
            (-267.9, 442.63, -314.3),
            (-265.9, 443.12, -318.9),
            (-262.7, 442.44, -326.2),
            (-254.0, 442.87, -346.0),   // east end, near the ruins
        };

        /// <summary>
        /// The circuit: the way out, then the same points back without the two ends again, so the
        /// walk from the last step to the first is the final leg back to the west end.
        /// </summary>
        public static (uint Step, double X, double Y, double Z, uint PauseMs)[] Steps()
        {
            var steps = new (uint, double, double, double, uint)[WayOut.Length * 2 - 2];
            var last = WayOut.Length - 1;

            for (var i = 0; i < steps.Length; i++)
            {
                var point = WayOut[i <= last ? i : 2 * last - i];
                var pause = i == 0 || i == last ? EndPauseMs : 0u;
                steps[i] = ((uint)i, point.X, point.Y, point.Z, pause);
            }

            return steps;
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var (step, x, y, z, pause) in Steps())
                migration.Sql($"INSERT INTO {SpawnPoolPatrolEntry.TableName} (pool_id, step, pos_x, pos_y, pos_z, facing, pause_ms) " +
                              $"VALUES ({Pool}, {step}, {N(x)}, {N(y)}, {N(z)}, NULL, {pause});");

            // Plain SQL: the migration has no target model for InsertData (PlateauMissionDataV1.Evidence).
            migration.Sql("INSERT INTO mission_evidence (mission_id, content_revision, evidence_id, owner_kind, owner_id, " +
                          "source_kind, source_uri, local_client_path, confidence, reconstruction_note) VALUES " +
                          $"({PlateauGiantKillerV1.MissionId}, '{PlateauMissionDataV1.Revision}', {EvidenceId}, " +
                          $"{(byte)MissionEvidenceOwnerKind.Mission}, {PlateauGiantKillerV1.MissionId}, " +
                          $"{(byte)MissionEvidenceSourceKind.Reconstruction}, 'repository:PlateauGiantKillerPatrolV1', NULL, 0.3, " +
                          "'Pool520051 walks a beat (group patrol: Goliath leads, the Kael follow): there and back from (-440,-230) " +
                          "to (-254,-346), 261m each way, 31 points measured on the navmesh and client floor, >=5.5m clear each " +
                          "side; 10s at each end. Route from Ellatha (near -250,-350) and TaRapedia (ruins to halfway to the camp).');");
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {PlateauGiantKillerV1.MissionId} " +
                          $"AND content_revision = '{PlateauMissionDataV1.Revision}' AND evidence_id = {EvidenceId};");
            migration.Sql($"DELETE FROM {SpawnPoolPatrolEntry.TableName} WHERE pool_id = {Pool};");
        }

        private static string N(double value) => value.ToString("0.0##", CultureInfo.InvariantCulture);
    }
}
