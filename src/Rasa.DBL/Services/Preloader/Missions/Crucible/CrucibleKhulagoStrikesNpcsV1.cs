using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The Crucible set 9 NPC rows, for Pump Up The Jam (1224) and Strike Zone (1214) at Prometheus Outpost: no new row
    /// (the jamming towers and the beacon spots are scene objects), one upstream row moved, Captain Tiersky, 1224's giver.
    ///
    /// Sourced: the World DB row 510021 "Captain Tiersky - Prometheus Outpost" (client creaturenamelanguage 9214, no package:
    /// 1224 has no objective conversation); TaRapedia Captain Tiersky and TR MMO DB Captain Tiersky: "Location Prometheus
    /// Outpost", the giver of Pump Up The Jam (0.5). Major Keplinger 510031 (1214's giver) stands where set 8a moved him.
    ///
    /// Upstream data fixed here (NOTES-upstream-bugs, D3): Tiersky (404.2, 169.77, 387.9) stands in the placeholder cluster on
    /// the Prometheus Outpost control point (400.1, 170.4, 387.9), inside the control point's model
    /// (UsableControlPointElohV01, arch_eloh_controlpoint_v02_PVE: 12 x 17 m, 2.6 m high), as Keplinger and Madias stood
    /// before set 8a (CrucibleKardashAndPrometheusNpcsV1). His pool stays the control point's garrison link (kind 2).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3): Tiersky by the CP gate 16 m north of Keplinger (no source gives his place),
    /// (379, 168.36, 400): on the navmesh and a client floor (0.28 m over it), 7.3 m from a wall, nothing over the head, seen
    /// from 16 of 16 places 2 m round him, 24 m from the control point's centre, 57 m outside every hostile automatic pool's
    /// circle, 570 m and more from every map link, walked to from the Prometheus Outpost waypoint (57 m) with no lava on the
    /// walk (probe 2026-11-07).
    ///
    /// Plain SQL, valid on SQLite and MySQL; Down puts the upstream row back as it was.
    /// </summary>
    public static class CrucibleKhulagoStrikesNpcsV1
    {
        public const uint Tiersky = 510021;

        /// <summary>Set 9 takes no id of the Crucible block: the next free one stays set 8b's.</summary>
        public const uint NextFreeId = CruciblePrometheusOutpostNpcsV1.NextFreeId;

        /// <summary>The upstream pool moved: pool, the place before (World DB), the place now.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) Before, (double X, double Y, double Z) Now)[] Moves =
        {
            (Tiersky, (404.20001220703125, 169.77, 387.8999938964844), (379, 168.36, 400))
        };

        public static (double X, double Y, double Z) TierskyAt => Moves.Single(move => move.Pool == Tiersky).Now;

        public static IEnumerable<string> Up() => Moves.Select(move => Place(move.Pool, move.Now));

        public static IEnumerable<string> Down() => Moves.Reverse().Select(move => Place(move.Pool, move.Before));

        private static string Place(uint pool, (double X, double Y, double Z) at) =>
            $"UPDATE spawnpool SET pos_x = {N(at.X)}, pos_y = {N(at.Y)}, pos_z = {N(at.Z)} WHERE id = {pool};";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
