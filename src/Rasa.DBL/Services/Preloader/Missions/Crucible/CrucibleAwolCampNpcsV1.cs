using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The Crucible set 6 NPCs, Ligo Crucible (map 1993), for the AWOL missions (Calendar Girl 1144, Stray Cats 1152, The Other
    /// Side Of The Coin 1153, Making Amends 1155, Sweet Revenge 1205): the upstream rows these missions talk to, four of them
    /// moved to their places. No new row: every NPC of the set is in the World DB (Awol Captain Cheung, Awol Lieutenant
    /// Deirdre, Awol Sergeant Breiburg, Lieutenant Sherer, Intelligence Officer D'Ambrose; Captain DeLisi and Top Khana Grupa
    /// Munit stand where sets 1 and 5 put them). The Crucible block's next free id stays 770030.
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0, World DB rows): 9154 "Awol Captain Cheung", 9163 "Awol Lieutenant Deirdre",
    ///    9153 "Awol Sergeant Breiburg", 9158 "Lieutenant Sherer", 9161 "Intelligence Officer D'Ambrose". Packages (client
    ///    objectiveconversation, 1.0; the World DB rows have them): (1152,1) and (1153,1) 1292 Cheung, (1144,1/3/8) 1304
    ///    Deirdre, (1155,3) and (1205,3) 1293 Breiburg, (1144,4/6/9) 1297 Sherer, (1144,5) and (1152,2) 1301 D'Ambrose (set 1
    ///    gave him 1301).
    ///  - Places: Cheung "the leader of the Awol Camp" (client 8440 "Captain Chueng, the leader of the Awol Camp"; TaRapedia
    ///    "Location AWOL Camp"), the camp "in the hills to the west of Staal" (client 9107; World DB map_region "Awol Camp"
    ///    at -953, 310, 255); Deirdre "at Staal ... working for one of the Brann syndicates" (client 9160, 8362); Sherer at
    ///    969, 152, 108 and D'Ambrose at 975, 144, 110, Outpost Intrepid (TaRapedia Calendar Girl, 0.5).
    ///
    /// Upstream data fixed here (NOTES-upstream-bugs, D3): Cheung and Deirdre stood in the placeholder cluster at the client's
    /// map label "Staal" (-16 to -11, 188, 323-327), in the lava gate yard east of the Staal prison (NOTES-crucible-staal-
    /// access.md: no walk route); Sherer and D'Ambrose in the one at "Outpost Intrepid" (942-944, 152, 176-181).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Cheung at the Awol Camp's region point, 10.8 m from Breiburg and Melching; Deirdre on the Staal Centrum floor 9 m
    ///    from the Staal Detention Facility waypoint (no source names her syndicate or her place in Staal); Sherer and
    ///    D'Ambrose at TaRapedia's x and z on the Outpost Intrepid floor (y 144.68: TaRapedia's y 152 has no floor there).
    ///  - Breiburg stays where upstream has him (the Awol Camp, 3.7 m from Melching).
    ///  - Every place on the server navmesh and a client floor, at least 2 m from a wall, nothing over the head, seen from 16
    ///    of 16 places 2 m round it, walked to from its area's waypoint (Cheung from the Awol Camp waypoint and Breiburg,
    ///    Deirdre from the Staal waypoint, the lower office teleporter 909 and Munit with no walk over a Staal floor hole,
    ///    Sherer and D'Ambrose from the Outpost Intrepid waypoint and DeLisi); 141 m or more from every hostile pool's circle
    ///    and 44 m or more from every map link (probe 2026-11-07).
    ///
    /// Plain SQL, valid on SQLite and MySQL; Down puts the upstream rows back as they were.
    /// </summary>
    public static class CrucibleAwolCampNpcsV1
    {
        public const uint MapContextId = CrucibleMissionDataV1.MapContextId;

        public const uint Cheung = 510015, CheungPackage = 1292;
        public const uint Deirdre = 510016, DeirdrePackage = 1304;
        public const uint Breiburg = 510017, BreiburgPackage = 1293;
        public const uint Sherer = CrucibleOutpostIntrepidNpcsV1.Sherer, ShererPackage = 1297;
        public const uint DAmbrose = CrucibleOutpostIntrepidNpcsV1.DAmbrose, DAmbrosePackage = CrucibleOutpostIntrepidNpcsV1.DAmbrosePackage;
        public const uint DeLisi = CrucibleOutpostIntrepidNpcsV1.DeLisi, DeLisiPackage = CrucibleOutpostIntrepidNpcsV1.DeLisiPackage;
        public const uint Munit = CrucibleOutpostIntrepidNpcsV1.Munit, MunitPackage = CrucibleOutpostIntrepidNpcsV1.MunitPackage;
        public const uint NextFreeId = CrucibleStaalSigalsNpcsV1.NextFreeId;

        /// <summary>The upstream pools moved: pool, the place before (World DB), the place now.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) Before, (double X, double Y, double Z) Now)[] Moves =
        {
            (Cheung, (-15.699999809265137, 187.78, 323.1000061035156), (-953, 309.56, 255)),
            (Deirdre, (-11.0, 188.52, 326.5), (-230, 180.86, 324)),
            (Sherer, (944.4000244140625, 152.0, 176.39999389648438), (969, 144.68, 108)),
            (DAmbrose, (942.0, 152.0, 180.6999969482422), (975, 144.68, 110))
        };

        /// <summary>The upstream pools set 6 talks to and leaves where they are.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] Kept =
        {
            (Breiburg, (-942.2000122070312, 309.510009765625, 246.89999389648438))
        };

        /// <summary>
        /// Where an NPC of set 6 stands: a moved pool's new place, a kept one's, DeLisi's (set 1) or Munit's (the Staal walks
        /// fix, CrucibleStaalWalksV1).
        /// </summary>
        public static (double X, double Y, double Z) At(uint pool) => pool switch
        {
            DeLisi => CrucibleOutpostIntrepidNpcsV1.At(DeLisi),
            Munit => CrucibleStaalWalksV1.Now(Munit).Value,
            _ when Kept.Any(kept => kept.Pool == pool) => Kept.Single(kept => kept.Pool == pool).At,
            _ => Moves.Single(move => move.Pool == pool).Now
        };

        public static IEnumerable<string> Up() => Moves.Select(move => Place(move.Pool, move.Now));

        public static IEnumerable<string> Down() => Moves.Reverse().Select(move => Place(move.Pool, move.Before));

        private static string Place(uint pool, (double X, double Y, double Z) at) =>
            $"UPDATE spawnpool SET pos_x = {N(at.X)}, pos_y = {N(at.Y)}, pos_z = {N(at.Z)} WHERE id = {pool};";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
