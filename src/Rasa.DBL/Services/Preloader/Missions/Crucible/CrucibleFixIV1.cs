using System.Collections.Generic;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Ligo Crucible fix job I (to-fix row 69, triage items 21 and 24), a delta on migrations 20271108001600 (set 11a) and
    /// 20271108001700 (set 12a): their data classes stay as they are.
    ///
    /// Row 69: the Ward door marker (CrucibleIncurablesWardNpcsV1.DoorWayAt (-216, 180.83, 400), r6; Sadistic Sabotage 1445
    /// objective 1 and Rehabilitation at Gunpoint 1402 objectives 1-3; Lost in the Asylum 1444 has none on the Crucible) stood
    /// 19.7 m from the door's point (map link 97, (-216.04, 180.5, 419.7), r6): 13.7 m outside its circle (the comment said 8 m).
    /// A player at the marker sees no door (real client 2026-11-08). Now it stands at (-216.8, 180.88, 405.2) (ESTIMATE): on the
    /// navmesh walk from Tempa (-307, 180.79, 326) to the door, 14.5 m from the door's point (8.5 m outside its circle), on the
    /// navmesh and a client floor (180.5), seen from 16 of 16 places 2 m round it (probe 2026-11-08). Its radius 6 to 3, so its
    /// circle stays 5 m and more outside the door's (5.5 m; CrucibleGuardTests.NoCrucibleMarkerCircleTouchesAMapLinkCircle).
    ///
    /// Item 24: Control the Brain 1521's Spare Parts Container (-16, 1.51, -164). From the north the real client stops at a step
    /// (-15.4, 1.2, -158.8), 5.3 m off, and a use from there does nothing; from the south (-16.7, 1.2, -166.7) the use works
    /// (tester, 2026-11-08). The container stays (2 m south would put the north stop 7 m off); objective 4's marker (r3) moves
    /// onto its south side, (-16.4, 1.46, -166.6) (ESTIMATE: on the navmesh, 2.6 m from the container, which stays inside it),
    /// so the player comes to it from the south. The walk from Brian at the Brain to the marker ends within ClientUseReach of
    /// the container, south of it (CrucibleChaukasTests).
    /// </summary>
    public static class CrucibleFixIV1
    {
        /// <summary>
        /// The use reach the real client showed (2026-11-08: a use 5.3 m off did nothing, 2.8 m off worked): an actor's use range
        /// 3 (client manifestation.py GetUseRange; the doubling for a manifestation did not hold at 5.3 m). ESTIMATE.
        /// </summary>
        public const float ClientUseReach = 3f;

        public static readonly (double X, double Y, double Z) DoorWayBefore = CrucibleIncurablesWardNpcsV1.DoorWayAt;
        public static readonly (double X, double Y, double Z) DoorWayNow = (-216.8, 180.88, 405.2);
        public const double DoorRadiusBefore = CrucibleSadisticSabotageV1.DoorMarkerRadius, DoorRadiusNow = 3;

        /// <summary>The Ward door's markers: mission, objective, indicator (1445's first; 1402's twelfth to fourteenth, after the bosses and the nine pools).</summary>
        public static readonly (uint Mission, uint Objective, uint Indicator)[] DoorMarkers =
        {
            (CrucibleSadisticSabotageV1.MissionId, CrucibleSadisticSabotageV1.Report, MissionIndicator.UnnamedFrom + 1),
            (CrucibleRehabilitationAtGunpointV1.MissionId, CrucibleRehabilitationAtGunpointV1.KillJaikev, MissionIndicator.UnnamedFrom + 12),
            (CrucibleRehabilitationAtGunpointV1.MissionId, CrucibleRehabilitationAtGunpointV1.KillParvatee, MissionIndicator.UnnamedFrom + 13),
            (CrucibleRehabilitationAtGunpointV1.MissionId, CrucibleRehabilitationAtGunpointV1.Execute, MissionIndicator.UnnamedFrom + 14)
        };

        public static readonly (double X, double Y, double Z) ContainerMarkerBefore =
            ((double)CrucibleControlTheBrainV1.ContainerAt.X, (double)CrucibleControlTheBrainV1.ContainerAt.Y, (double)CrucibleControlTheBrainV1.ContainerAt.Z);
        public static readonly (double X, double Y, double Z) ContainerMarkerNow = (-16.4, 1.46, -166.6);

        /// <summary>1521's marker on the container: objective 4, the sixth (the mouth, three pools and Brian before it).</summary>
        public const uint ContainerObjective = CrucibleControlTheBrainV1.FindParts, ContainerMarker = MissionIndicator.UnnamedFrom + 6;

        /// <summary>The evidence rows (sets 11a and 12a wrote 1-3 of each).</summary>
        public const uint Evidence = 4;

        public const string Source = "repository:CrucibleFixIV1";

        public const string DoorNote =
            "Fix job I (to-fix row 69): the Ward door marker stood at (-216, 180.83, 400) r6, 19.7 m from the door's point (link 97, " +
            "-216.04, 180.5, 419.7, r6); a player there saw no door. Moved to (-216.8, 180.88, 405.2) r3 (ESTIMATE), on the walk from " +
            "Tempa, 8.5 m outside the door's circle (its own circle 5.5 m outside), on the navmesh and a client floor, seen 16/16.";

        public const string ContainerNote =
            "Fix job I (triage item 24): from the north the real client stops at a step (-15.4, 1.2, -158.8), 5.3 m from the Spare " +
            "Parts Container, and a use there does nothing; from the south (-16.7, 1.2, -166.7) it works. Objective 4's marker (r3) " +
            "moved from the container onto its south side (-16.4, 1.46, -166.6) (ESTIMATE); the container stays.";

        public static readonly uint[] Missions =
        {
            CrucibleSadisticSabotageV1.MissionId, CrucibleRehabilitationAtGunpointV1.MissionId, CrucibleControlTheBrainV1.MissionId
        };

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in Marker(DoorWayNow, DoorRadiusNow, ContainerMarkerNow))
                migration.Sql(sql);
            CrucibleMissionDataV1.Existing(migration, CrucibleSadisticSabotageV1.MissionId)
                .Evidence(Evidence, MissionEvidenceSourceKind.Reconstruction, Source, DoorNote, 0.3);
            CrucibleMissionDataV1.Existing(migration, CrucibleRehabilitationAtGunpointV1.MissionId)
                .Evidence(Evidence, MissionEvidenceSourceKind.Reconstruction, Source, DoorNote, 0.3);
            CrucibleMissionDataV1.Existing(migration, CrucibleControlTheBrainV1.MissionId)
                .Evidence(Evidence, MissionEvidenceSourceKind.Reconstruction, Source, ContainerNote, 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            foreach (var mission in Missions)
                migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {mission} " +
                              $"AND content_revision = '{CrucibleMissionDataV1.Revision}' AND evidence_id = {Evidence};");
            foreach (var sql in Marker(DoorWayBefore, DoorRadiusBefore, ContainerMarkerBefore))
                migration.Sql(sql);
        }

        private static IEnumerable<string> Marker((double X, double Y, double Z) door, double radius, (double X, double Y, double Z) container)
        {
            foreach (var (mission, objective, indicator) in DoorMarkers)
                yield return Update(mission, objective, indicator, door, radius);
            yield return Update(CrucibleControlTheBrainV1.MissionId, ContainerObjective, ContainerMarker, container, CrucibleControlTheBrainV1.ContainerRadius);
        }

        private static string Update(uint mission, uint objective, uint indicator, (double X, double Y, double Z) at, double radius) =>
            $"UPDATE mission_indicator SET pos_x = {N(at.X)}, pos_y = {N(at.Y)}, pos_z = {N(at.Z)}, radius = {N(radius)} " +
            $"WHERE mission_id = {mission} AND content_revision = '{CrucibleMissionDataV1.Revision}' " +
            $"AND objective_id = {objective} AND indicator_id = {indicator};";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
