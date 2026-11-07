using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The bot pools by the Chaukas Robotics Facility moved clear of its door, and Merchant Vargo moved clear of the Staal
    /// Centrum crafting station (to-fix rows 44 and 43), a delta on migrations 20271108000200 (set 3: the Warden Bot pools),
    /// 20271108000300 (1147's markers), 20271108000400 (set 4: 1203/1204's markers), 20271108000500 (Vargo's place and
    /// markers) and 20271108000600 (set 5: Reconstructor pool 770028 and 1202's markers). The live World DB has applied some
    /// of them: their data classes stay as they are.
    ///
    /// Row 44: the navmesh walk from Warden pool 770010 (-493.6, 581.6) to 770011 (-578.6, 561.6) passes (-531.1, 250.5, 576.9),
    /// 5.3 m from the facility's door, map link 98 (-528.6, 249.44, 581.6) r6; a player who hunted between the pools went
    /// through the door into map 2146. The walks 770010 to Reconstructor pool 770017 come as near (probe 2026-11-07). A Warden
    /// Bot also chased the player from its pool to 5.3 m from the door point; a pool creature follows a fight up to
    /// BehaviorManager.MaxChaseDistance (60 m) from its home.
    /// Row 43: at 20271108000500's place (-190.5, 180.79, 267.2) the real client walk from Munit ended on Vargo's south side
    /// (-193.8, 180.5, 264.6), at the crafting station (kraftwerk 22, its box -194.4..-192.1 x 262.1..265.8, 2 m high), and
    /// the station was in the line: "You cannot see your target from here". Our client floor and sight reader also finds that
    /// line blocked (eye 1.6 m, aim 1.5 m).
    ///
    /// Now (ESTIMATE, probe 2026-11-07):
    ///  - The three Warden Bot pools and Reconstructor pool 770028 on the open ground south, west and east of the door,
    ///    each circle 70 m or more outside every map link's circle (a chase from the pool cannot come within 10 m of a door);
    ///    every navmesh walk between two pools of one objective (1147, 1203, 1204, 1202) and from the Awol Camp and Labbna
    ///    waypoints to them keeps 38 m or more from every link circle. Each on the navmesh and a client floor, 3 m or more from a
    ///    wall, 24 m or more from the other hostile pools' circles. The markers of 1147, 1203, 1204 and 1202 on them go with them.
    ///  - Vargo 7.4 m clear of the crafting station's box (more than the 5 m talk reach), at (-186, 180.78, 270): seen from
    ///    16 of 16 places 2 m round and from each navmesh walk's end (5 to 2 m) from Munit, the lower office teleporter, Cugin
    ///    and Vitto; 30.5 m from Roscoe. His markers (1146 objectives 3 and 6) on him.
    /// </summary>
    public static class CrucibleWardenPoolsAndVargoV1
    {
        public const uint Vargo = CrucibleGrupaChiefsNpcsV1.Vargo;

        /// <summary>The evidence row each mission gets (its next free evidence id).</summary>
        public static readonly (uint Mission, uint Evidence)[] EvidenceIds =
        {
            (CrucibleGameWardensV1.MissionId, 5), (CrucibleSalvageDutyV1.MissionId, 4), (CrucibleSalvageDutyV1.MoreMissionId, 4),
            (CrucibleArchitectsOfDestructionV1.MissionId, 4), (CrucibleProtectionRacketV1.MissionId, 5)
        };

        /// <summary>The pools moved: pool, the place before (sets 3 and 5, 20271108000500), the place now.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) Before, (double X, double Y, double Z) Now)[] Moves =
        {
            (770009, CrucibleWardensAndLashersNpcsV1.WardenPools[0].At, (-575, 250.95, 491)),
            (770010, CrucibleWardensAndLashersNpcsV1.WardenPools[1].At, (-499, 251.03, 434)),
            (770011, CrucibleWardensAndLashersNpcsV1.WardenPools[2].At, (-606, 248.07, 534)),
            (770028, CrucibleStaalSigalsNpcsV1.ReconstructorPools[1].At, (-454, 250.1, 495)),
            (Vargo, CrucibleStaalWalksV1.Now(Vargo).Value, (-186, 180.78, 270))
        };

        /// <summary>
        /// Every Crucible marker on a moved pool: mission, objective, indicator, pool. 1147: CrucibleMarkersClearOfLinksV2's
        /// nine; 1203/1204: one per part on each bot pool (CrucibleSalvageDutyV1.Collect's order); 1202: one on each
        /// Reconstructor pool; 1146: objectives 3 and 6 on Vargo (CrucibleStaalWalksV1).
        /// </summary>
        public static IEnumerable<(uint Mission, uint Objective, uint Indicator, uint Pool)> Markers
        {
            get
            {
                var wardens = CrucibleWardensAndLashersNpcsV1.WardenPools;
                var all = new List<(uint, uint, uint, uint)>();
                foreach (var (objective, indicator, at) in CrucibleMarkersClearOfLinksV2.WardenMarkers)
                    all.Add((CrucibleGameWardensV1.MissionId, objective, indicator, wardens.Single(pool => pool.At == at).Pool));
                foreach (var mission in new[] { CrucibleSalvageDutyV1.MissionId, CrucibleSalvageDutyV1.MoreMissionId })
                {
                    var marker = 0U;
                    foreach (var part in CrucibleSalvageDutyV1.Parts.Where(part => part.Mission == mission))
                        foreach (var (pool, _) in CrucibleSalvageDutyV1.BotPools)
                            all.Add((mission, part.Objective, MissionIndicator.UnnamedFrom + ++marker, pool));
                }
                var index = 0U;
                foreach (var (pool, _) in CrucibleArchitectsOfDestructionV1.Pools)
                    all.Add((CrucibleArchitectsOfDestructionV1.MissionId, CrucibleArchitectsOfDestructionV1.Destroy, MissionIndicator.UnnamedFrom + ++index, pool));
                all.AddRange(CrucibleStaalWalksV1.Markers.Where(marker => marker.Pool == Vargo));
                return all.Where(marker => Moves.Any(move => move.Pool == marker.Item4));
            }
        }

        /// <summary>Where a pool stands after this migration: a moved one's new place, else null.</summary>
        public static (double X, double Y, double Z)? Now(uint pool) =>
            Moves.Any(move => move.Pool == pool) ? Moves.Single(move => move.Pool == pool).Now : null;

        public static void Up(MigrationBuilder migration)
        {
            foreach (var (pool, _, now) in Moves)
                migration.Sql(Place(pool, now));
            foreach (var (mission, objective, indicator, pool) in Markers)
                migration.Sql(Marker(mission, objective, indicator, Now(pool).Value));

            const string Source = "repository:CrucibleWardenPoolsAndVargoV1";
            const string Pools = "To-fix row 44: the navmesh walk between Warden Bot pools 770010 and 770011 passed 5.3 m from the " +
                                 "Chaukas Robotics Facility door (map link 98) and a player went through it; ";
            Evidence(migration, CrucibleGameWardensV1.MissionId, Source, Pools +
                "the three Warden Bot pools moved south, west and east of the door, each 70 m or more outside the door's circle; the nine markers on them.");
            Evidence(migration, CrucibleSalvageDutyV1.MissionId, Source, Pools +
                "the Warden Bot pools moved 70 m or more from the door; the markers on them.");
            Evidence(migration, CrucibleSalvageDutyV1.MoreMissionId, Source, Pools +
                "the Warden Bot pools moved 70 m or more from the door; the markers on them.");
            Evidence(migration, CrucibleArchitectsOfDestructionV1.MissionId, Source,
                "To-fix row 44: Reconstructor pool 770028 moved to (-454, 250.1, 495), 70 m or more outside the Chaukas Robotics " +
                "Facility door's circle; its marker on it.");
            Evidence(migration, CrucibleProtectionRacketV1.MissionId, Source,
                "To-fix row 43: from the end of the walk from Munit the Staal Centrum crafting station (kraftwerk 22) hid Vargo; " +
                "moved to (-186, 180.78, 270), 7.4 m clear of the station; his markers on him.");
        }

        public static void Down(MigrationBuilder migration)
        {
            foreach (var (mission, evidence) in EvidenceIds)
                migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {mission} AND content_revision = '{CrucibleMissionDataV1.Revision}' " +
                              $"AND evidence_id = {evidence};");
            foreach (var (mission, objective, indicator, pool) in Markers)
                migration.Sql(Marker(mission, objective, indicator, Moves.Single(move => move.Pool == pool).Before));
            foreach (var (pool, before, _) in Moves)
                migration.Sql(Place(pool, before));
        }

        private static void Evidence(MigrationBuilder migration, uint mission, string source, string note) =>
            CrucibleMissionDataV1.Existing(migration, mission).Evidence(EvidenceIds.Single(entry => entry.Mission == mission).Evidence,
                MissionEvidenceSourceKind.Reconstruction, source, note, 0.3);

        private static string Place(uint pool, (double X, double Y, double Z) at) =>
            $"UPDATE spawnpool SET pos_x = {N(at.X)}, pos_y = {N(at.Y)}, pos_z = {N(at.Z)} WHERE id = {pool};";

        private static string Marker(uint mission, uint objective, uint indicator, (double X, double Y, double Z) at) =>
            $"UPDATE mission_indicator SET pos_x = {N(at.X)}, pos_y = {N(at.Y)}, pos_z = {N(at.Z)} " +
            $"WHERE mission_id = {mission} AND content_revision = '{CrucibleMissionDataV1.Revision}' AND objective_id = {objective} " +
            $"AND indicator_id = {indicator};";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
