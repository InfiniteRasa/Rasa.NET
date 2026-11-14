using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Ligo Ashen Desert fix job H: a delta on migration 20271110001500 (set 15, Indra Caverns; AshenIndraCavernsV1, live). It
    /// sorts after set 15 and before set 16 (20271110001600).
    ///
    /// To-fix row 33 (real client 2026-11-11, server-ashen17): no player reaches the Indra Caverns door (map_link 58, (321.87,
    /// 318.96, 319.60) r6, to Indra Caverns 2055), so set 15 (1559, 1360, 1395) cannot be started. The door stands on the Indra
    /// Pass waypoint's island, which has no walk-in: upstream N23 (fix G's client-floor flood: the nearest gap a 1.9 m step at
    /// (107, 431)) and N26 (the cave pocket). The real client walked from White Oasis Post to the valley's south entry (153.9,
    /// 312.3, 460.8) and to Surveyor Odesha (115, 450) but not onto the island (stops at (394.0, 301.2, -185.7) on the way from
    /// the north; the navmesh path ends at (235.2, 282.7, 320), 33 m below it). Set 15's door walk test started at set 13's old
    /// Odesha place on the island, which fix G had moved off.
    ///
    /// The fix (ESTIMATE, a deviation: retail had no such pad as far as we know): a pair of local teleporters, as Crucible set 2's
    /// 909/910 (CrucibleGrupaChiefsNpcsV1) and the Ashoka pads 541/613: the lower one in the valley by Odesha, walked to on the
    /// client floor from the valley's south entry (set 10's and fix G's way from the White Oasis Post waypoint); the upper one on
    /// the island, from which the door walk passes on the client floor both ways. A player who steps onto one (within 2 m) is
    /// held 3 s and set down on the other (DynamicObjectManager.LocalTeleporterWorker); he is not sent back until he has stepped
    /// off it (PlayerTransfer.ArrivalPad). The pads are drawn as ArchHumTeleporterV02 (class 29648, as the Ashoka and Stonewall
    /// pads 537-541 and MarshesRetreadArenaNpcsV1's), ids 913 and 914 (free in the World DB and the code; 911/912 are
    /// CrucibleHybridArcNpcsV1's). Not only for set 15's players: the client has no gate on a local teleporter.
    ///
    /// 1559's objective 1 ("Locate Indra Caverns") gets a second, unnamed marker on the lower pad (ESTIMATE), so the player finds
    /// the pad; the door's marker 261 stays.
    ///
    /// Plain SQL, valid on SQLite and MySQL; each INSERT guarded (only where the row is not there, the link and the marker only
    /// where their pad or objective is); Down takes out exactly these rows.
    /// </summary>
    public static class AshenFixHV1
    {
        public const uint MapContextId = AshenMissionDataV1.MapContextId;

        /// <summary>The pads: valley (lower) and island (upper); type 1 local teleporter, class 29648 ArchHumTeleporterV02.</summary>
        public const uint LowerPad = 913, UpperPad = 914, LocalTeleporter = 1, PadClass = 29648;
        public const string LowerDescription = "Local Teleporter: Indra Pass valley (fix H)";
        public const string UpperDescription = "Local Teleporter: Indra Caverns door (fix H)";

        /// <summary>
        /// Where the pads stand, on the client's floor (probe 2026-11-11 on the client collision with the 55-degree climb rule,
        /// ESTIMATE): the lower one 9 m south-east of Odesha (115, 450) on flat valley floor (normal y 0.985), walked to from the
        /// valley's south entry straight, 104 m outside every hostile pool's circle, on the navmesh joined to White Oasis Post;
        /// the upper one at set 13's old Odesha place on the island (flat, nothing over it, 79 m outside every hostile pool's
        /// circle, 58 m outside link 58's circle), from which the door walk passes and the Indra Pass waypoint (198) is walked to.
        /// </summary>
        public static readonly (double X, double Y, double Z) LowerPadAt = (121, 305.97, 443), UpperPadAt = (258, 315.49, 325);

        /// <summary>1559's objective 1 and its new marker on the lower pad (unnamed, the first: MissionIndicator.UnnamedFrom + 1).</summary>
        public const uint Mission = AshenIndraCavernsV1.MissionId, Objective = AshenIndraCavernsV1.Locate, Marker = 1;
        public const double MarkerRadius = 4;

        public const uint EvidenceId = 4;
        public const string Source = "repository:AshenFixHV1";
        public const string Note =
            "Fix job H (to-fix row 33): no walk-in to the Indra Caverns door's island (N23, N26; real client 2026-11-11). Local " +
            "teleporters 913 (valley by Odesha) and 914 (on the island, the door walk from it) linked both ways; marker 1559.1 on the " +
            "lower pad. ESTIMATE: retail had no such pad; the places.";

        public static IEnumerable<string> UpSql()
        {
            yield return Pad(LowerPad, LowerDescription, LowerPadAt);
            yield return Pad(UpperPad, UpperDescription, UpperPadAt);
            yield return Link(LowerPad, UpperPad);
            yield return Link(UpperPad, LowerPad);
            yield return "INSERT INTO mission_indicator (mission_id, content_revision, objective_id, indicator_id, requirement, pos_x, pos_y, pos_z, " +
                         "radius, show_3d_effect, comment) " +
                         $"SELECT o.mission_id, o.content_revision, o.objective_id, {MissionIndicator.UnnamedFrom + Marker}, {(byte)MissionContentRequirement.Required}, " +
                         $"{F(LowerPadAt.X)}, {F(LowerPadAt.Y)}, {F(LowerPadAt.Z)}, {F(MarkerRadius)}, 0, 'Ashen navigation' " +
                         $"FROM mission_objective_definition o WHERE {ObjectiveIs("o")} AND NOT EXISTS (SELECT 1 FROM mission_indicator x " +
                         $"WHERE {ObjectiveIs("x")} AND x.indicator_id = {MissionIndicator.UnnamedFrom + Marker});";
        }

        public static IEnumerable<string> DownSql()
        {
            yield return $"DELETE FROM mission_indicator WHERE mission_id = {Mission} AND content_revision = '{AshenMissionDataV1.Revision}' " +
                         $"AND objective_id = {Objective} AND indicator_id = {MissionIndicator.UnnamedFrom + Marker};";
            yield return $"DELETE FROM local_teleporter_link WHERE from_teleporter_id = {UpperPad} AND to_teleporter_id = {LowerPad};";
            yield return $"DELETE FROM local_teleporter_link WHERE from_teleporter_id = {LowerPad} AND to_teleporter_id = {UpperPad};";
            yield return $"DELETE FROM teleporter WHERE id = {UpperPad} AND map_context_id = {MapContextId} AND type = {LocalTeleporter};";
            yield return $"DELETE FROM teleporter WHERE id = {LowerPad} AND map_context_id = {MapContextId} AND type = {LocalTeleporter};";
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in UpSql())
                migration.Sql(sql);
            AshenMissionDataV1.Existing(migration, Mission).Evidence(EvidenceId, MissionEvidenceSourceKind.Reconstruction, Source, Note, 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {Mission} " +
                          $"AND content_revision = '{AshenMissionDataV1.Revision}' AND evidence_id = {EvidenceId};");
            foreach (var sql in DownSql())
                migration.Sql(sql);
        }

        private static string ObjectiveIs(string table) =>
            $"{table}.mission_id = {Mission} AND {table}.content_revision = '{AshenMissionDataV1.Revision}' AND {table}.objective_id = {Objective}";

        private static string Pad(uint id, string description, (double X, double Y, double Z) at) =>
            "INSERT INTO teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) " +
            $"SELECT {id}, {PadClass}, {LocalTeleporter}, '{description}', {F(at.X)}, {F(at.Y)}, {F(at.Z)}, 0, m.map_context_id " +
            $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM teleporter x WHERE x.id = {id});";

        private static string Link(uint from, uint to) =>
            "INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) " +
            $"SELECT t.id, {to} FROM teleporter t WHERE t.id = {from} AND EXISTS (SELECT 1 FROM teleporter u WHERE u.id = {to}) " +
            $"AND NOT EXISTS (SELECT 1 FROM local_teleporter_link x WHERE x.from_teleporter_id = {from});";

        private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
